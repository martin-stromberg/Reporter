// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using System.Security.Cryptography;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.Services;

/// <summary>
/// Synchronizes RSS/Atom feeds, stores new items and updates feed health.
/// </summary>
public class FeedSyncService : IFeedSyncService
{
    private readonly IFeedRepository _feedRepository;
    private readonly IItemRepository _itemRepository;
    private readonly ISyncLogRepository _syncLogRepository;
    private readonly HttpClient _httpClient;
    private readonly INotificationService _notificationService;
    private readonly INetworkStatusService _networkStatusService;
    private readonly IKeywordFilter _keywordFilter;
    private readonly IFeedIconService _feedIconService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSyncService"/> class.
    /// </summary>
    /// <param name="feedRepository">The feed repository.</param>
    /// <param name="itemRepository">The item repository.</param>
    /// <param name="syncLogRepository">The sync log repository.</param>
    /// <param name="httpClient">The HTTP client used to retrieve feeds.</param>
    /// <param name="notificationService">The notification service invoked for newly stored items.</param>
    /// <param name="networkStatusService">The network connectivity status service.</param>
    /// <param name="keywordFilter">The keyword filter used to discard matching items before storing.</param>
    /// <param name="feedIconService">The service used to backfill the favicon of feeds that have none.</param>
    public FeedSyncService(
        IFeedRepository feedRepository,
        IItemRepository itemRepository,
        ISyncLogRepository syncLogRepository,
        HttpClient httpClient,
        INotificationService notificationService,
        INetworkStatusService networkStatusService,
        IKeywordFilter keywordFilter,
        IFeedIconService feedIconService)
    {
        _feedRepository = feedRepository;
        _itemRepository = itemRepository;
        _syncLogRepository = syncLogRepository;
        _httpClient = httpClient;
        _notificationService = notificationService;
        _networkStatusService = networkStatusService;
        _keywordFilter = keywordFilter;
        _feedIconService = feedIconService;
    }

    /// <inheritdoc />
    public async Task<SyncResult> SyncFeedAsync(Guid feedId, CancellationToken cancellationToken = default)
    {
        if (!_networkStatusService.IsOnline)
        {
            return new SyncResult(FeedHealth.Error, 0, AppResources.OfflineHint);
        }

        var log = new SyncLog
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            StartedAt = DateTime.UtcNow,
            Status = FeedHealth.Ok,
        };
        await _syncLogRepository.AddAsync(log);

        var feed = await _feedRepository.GetByIdAsync(feedId);
        if (feed is null)
        {
            const string Message = "Feed not found.";
            await UpdateLogAsync(log, FeedHealth.Error, Message).ConfigureAwait(false);
            return new SyncResult(FeedHealth.Error, 0, Message);
        }

        try
        {
            return await RunSyncAsync(feed, log, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var message = $"Synchronization failed: {ex.Message}";
            await UpdateFeedHealthAsync(feed, FeedHealth.Error).ConfigureAwait(false);
            await UpdateLogAsync(log, FeedHealth.Error, message).ConfigureAwait(false);
            return new SyncResult(FeedHealth.Error, 0, message);
        }
    }

    /// <inheritdoc />
    public async Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        if (!_networkStatusService.IsOnline)
        {
            return new SyncResult(FeedHealth.Error, 0, AppResources.OfflineHint);
        }

        var feeds = await _feedRepository.GetAllAsync().ConfigureAwait(false);
        if (feeds.Count == 0)
        {
            return new SyncResult(FeedHealth.Ok, 0, "No feeds configured.");
        }

        var totalNew = 0;
        var hasError = false;
        var hasWarning = false;
        var messages = new List<string>();

        foreach (var feed in feeds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await SyncFeedAsync(feed.Id, cancellationToken).ConfigureAwait(false);
            totalNew += result.NewItems;
            hasError = hasError || result.Status == FeedHealth.Error;
            hasWarning = hasWarning || result.Status == FeedHealth.Warning;

            if (!string.IsNullOrEmpty(result.Message))
            {
                messages.Add($"{feed.Title}: {result.Message}");
            }
        }

        var status = hasError ? FeedHealth.Error : hasWarning ? FeedHealth.Warning : FeedHealth.Ok;
        var combinedMessage = messages.Count > 0 ? string.Join("; ", messages) : null;
        return new SyncResult(status, totalNew, combinedMessage);
    }

    private async Task<SyncResult> RunSyncAsync(Feed feed, SyncLog log, CancellationToken cancellationToken)
    {
        var existingItems = await _itemRepository.GetByFeedAsync(feed.Id).ConfigureAwait(false);
        var existingCount = existingItems.Count;
        var lastPublishedAt = existingItems
            .Where(i => i.PublishedAt.HasValue)
            .Select(i => i.PublishedAt!.Value)
            .DefaultIfEmpty()
            .Max();

        await using var stream = await _httpClient.GetStreamAsync(feed.Url, cancellationToken).ConfigureAwait(false);
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using var reader = XmlReader.Create(stream, settings);
        var syndicationFeed = await Task.Run(() => SyndicationFeed.Load(reader), cancellationToken).ConfigureAwait(false);

        var feedItems = syndicationFeed.Items.ToList();

        var keywordTexts = await _keywordFilter.GetKeywordTextsAsync().ConfigureAwait(false);
        var (newItemEntities, filteredCount) = CollectNewItems(feed, feedItems, existingItems, keywordTexts, cancellationToken);

        var newItems = newItemEntities.Count;
        if (newItems > 0)
        {
            await _itemRepository.AddRangeAsync(newItemEntities).ConfigureAwait(false);
        }

        var status = DetermineStatus(newItems, feedItems.Count, existingCount, lastPublishedAt);
        var filteredSuffix = filteredCount > 0 ? $", {filteredCount} filtered" : string.Empty;
        var message = status == FeedHealth.Warning
            ? $"Synchronized {feedItems.Count} items, {newItems} new{filteredSuffix}. Health warning triggered."
            : $"Synchronized {feedItems.Count} items, {newItems} new{filteredSuffix}.";

        var resolvedTitle = ResolveFeedTitle(feed, syndicationFeed);

        // Feeds stored before favicon discovery existed (or added while offline)
        // get their icon backfilled on the first successful sync; the lookup is
        // strictly isolated inside the icon service.
        var faviconUrl = feed.FaviconUrl ?? await TryFindFaviconUrlAsync(feed.Url, syndicationFeed, cancellationToken).ConfigureAwait(false);

        await UpdateFeedHealthAsync(feed, status, resolvedTitle, faviconUrl).ConfigureAwait(false);
        await UpdateLogAsync(log, status, message).ConfigureAwait(false);

        if (newItemEntities.Count > 0)
        {
            try
            {
                await _notificationService.NotifyNewItemsAsync(feed, newItemEntities, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Ein Fehler im Benachrichtigungspfad darf das Sync-Ergebnis nicht verfaelschen.
                Debug.WriteLine($"FeedSyncService notification failed: {ex}");
            }
        }

        return new SyncResult(status, newItems, message);
    }

    private (List<Item> NewItems, int FilteredCount) CollectNewItems(
        Feed feed,
        IReadOnlyList<SyndicationItem> feedItems,
        IReadOnlyList<Item> existingItems,
        IReadOnlyList<string> keywordTexts,
        CancellationToken cancellationToken)
    {
        var newItemEntities = new List<Item>();
        var filteredCount = 0;
        var knownKeys = new HashSet<string>(
            existingItems.Select(i => i.GuidOrHash),
            StringComparer.Ordinal);

        foreach (var feedItem in feedItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var link = feedItem.Links.FirstOrDefault()?.Uri?.ToString();
            var publishedAt = feedItem.PublishDate == DateTimeOffset.MinValue
                ? (DateTime?)null
                : feedItem.PublishDate.UtcDateTime;
            var guidOrHash = NormalizeGuidOrHash(feedItem, link, publishedAt);

            if (!knownKeys.Add(guidOrHash))
            {
                continue;
            }

            var title = feedItem.Title?.Text;
            var contentHtml = GetContentHtml(feedItem);

            if (_keywordFilter.MatchesAny(title, contentHtml, keywordTexts))
            {
                filteredCount++;
                continue;
            }

            newItemEntities.Add(new Item
            {
                Id = Guid.NewGuid(),
                FeedId = feed.Id,
                Title = title ?? string.Empty,
                Link = link,
                PublishedAt = publishedAt,
                GuidOrHash = guidOrHash,
                IsRead = false,
                IsSavedForLater = false,
                ContentHtml = contentHtml,
            });
        }

        return (newItemEntities, filteredCount);
    }

    private static string? ResolveFeedTitle(Feed feed, SyndicationFeed syndicationFeed)
    {
        var documentTitle = syndicationFeed.Title?.Text;
        var isPlaceholderTitle = string.IsNullOrWhiteSpace(feed.Title) ||
            string.Equals(feed.Title, feed.Url, StringComparison.OrdinalIgnoreCase) ||
            IsHostPlaceholderTitle(feed) ||
            FeedTitleFallback.IsFileNamePlaceholderTitle(feed.Title, feed.Url);
        return isPlaceholderTitle && !string.IsNullOrWhiteSpace(documentTitle)
            ? documentTitle
            : null;
    }

    // Direct-add and search fallback titles (FeedTitleFallback) can equal the
    // URL host or file name, so a title equal to the feed URL's host counts as
    // an auto-generated placeholder as well.
    private static bool IsHostPlaceholderTitle(Feed feed)
    {
        return Uri.TryCreate(feed.Url, UriKind.Absolute, out var feedUri) &&
            string.Equals(feed.Title, feedUri.Host, StringComparison.OrdinalIgnoreCase);
    }

    private static string DetermineStatus(int newItems, int fetchedCount, int existingCount, DateTime lastPublishedAt)
    {
        if (fetchedCount < existingCount * 0.5 && existingCount > 0)
        {
            return FeedHealth.Warning;
        }

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        if (newItems == 0 && lastPublishedAt != default && lastPublishedAt < thirtyDaysAgo)
        {
            return FeedHealth.Warning;
        }

        return FeedHealth.Ok;
    }

    private async Task UpdateFeedHealthAsync(Feed feed, string status, string? resolvedTitle = null, string? faviconUrl = null)
    {
        var healthLastChange = feed.HealthLastChange;
        if (FeedHealth.Changed(feed.HealthStatus, status))
        {
            healthLastChange = DateTime.UtcNow;
        }

        await _feedRepository.UpdateAsync(new Feed
        {
            Id = feed.Id,
            Url = feed.Url,
            Title = resolvedTitle ?? feed.Title,
            CategoryId = feed.CategoryId,
            LastCheckedAt = DateTime.UtcNow,
            HealthStatus = status,
            HealthLastChange = healthLastChange,
            NotificationsEnabled = feed.NotificationsEnabled,
            FaviconUrl = faviconUrl ?? feed.FaviconUrl,
        }).ConfigureAwait(false);
    }

    // Resolves the feed's website from the document's "alternate" site link and
    // looks up its favicon; the icon service falls back to the feed URL's
    // authority and is strictly isolated, so a failure never affects the sync
    // result.
    private async Task<string?> TryFindFaviconUrlAsync(string feedUrl, SyndicationFeed syndicationFeed, CancellationToken cancellationToken)
    {
        var siteUrl = syndicationFeed.Links
            .FirstOrDefault(l => string.Equals(l.RelationshipType, "alternate", StringComparison.OrdinalIgnoreCase))
            ?.Uri?.AbsoluteUri;

        return await _feedIconService.TryFindFaviconUrlAsync(feedUrl, siteUrl, cancellationToken).ConfigureAwait(false);
    }

    private async Task UpdateLogAsync(SyncLog log, string status, string? message)
    {
        await _syncLogRepository.UpdateAsync(new SyncLog
        {
            Id = log.Id,
            FeedId = log.FeedId,
            StartedAt = log.StartedAt,
            FinishedAt = DateTime.UtcNow,
            Status = status,
            Message = message,
        }).ConfigureAwait(false);
    }

    private static string? GetContentHtml(SyndicationItem item)
    {
        if (item.Content is TextSyndicationContent textContent)
        {
            return textContent.Text;
        }

        if (item.Summary is TextSyndicationContent summary)
        {
            return summary.Text;
        }

        return null;
    }

    private static string NormalizeGuidOrHash(SyndicationItem item, string? link, DateTime? publishedAt)
    {
        var id = item.Id;
        if (!string.IsNullOrWhiteSpace(id) && id.Length <= 500)
        {
            return id;
        }

        var title = item.Title?.Text ?? string.Empty;
        var value = $"{title}|{link}|{publishedAt:O}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(hash);
    }
}
