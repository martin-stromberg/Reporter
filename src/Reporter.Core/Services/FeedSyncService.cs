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
    private readonly IItemImageService _imageService;
    private readonly IItemContentStore _contentStore;
    private readonly IDebugLogService? _debugLogService;
    private readonly SemaphoreSlim _syncAllLock = new(1, 1);

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
    /// <param name="imageService">The service used to resolve and download article images.</param>
    /// <param name="contentStore">The store used to backfill missing item contents.</param>
    /// <param name="debugLogService">The optional session debug log service used to record sync failures.</param>
    public FeedSyncService(
        IFeedRepository feedRepository,
        IItemRepository itemRepository,
        ISyncLogRepository syncLogRepository,
        HttpClient httpClient,
        INotificationService notificationService,
        INetworkStatusService networkStatusService,
        IKeywordFilter keywordFilter,
        IFeedIconService feedIconService,
        IItemImageService imageService,
        IItemContentStore contentStore,
        IDebugLogService? debugLogService = null)
    {
        _feedRepository = feedRepository;
        _itemRepository = itemRepository;
        _syncLogRepository = syncLogRepository;
        _httpClient = httpClient;
        _notificationService = notificationService;
        _networkStatusService = networkStatusService;
        _keywordFilter = keywordFilter;
        _feedIconService = feedIconService;
        _imageService = imageService;
        _contentStore = contentStore;
        _debugLogService = debugLogService;
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
            var errorKind = FeedSyncErrorKind.Classify(ex, feed.Url);
            await UpdateFeedHealthAsync(feed, FeedHealth.Error, new FeedHealthUpdate(ErrorKind: errorKind, ErrorMessage: message)).ConfigureAwait(false);
            await UpdateLogAsync(log, FeedHealth.Error, message).ConfigureAwait(false);
            _ = _debugLogService?.LogAsync(
                DebugLogCategory.Sync,
                $"Synchronization failed for feed '{feed.Title}'",
                ex.ToString(),
                DebugLogLevel.Error);
            return new SyncResult(FeedHealth.Error, 0, message);
        }
    }

    /// <inheritdoc />
    public async Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        // Parallele Gesamt-Syncs (z. B. Start-Abruf gleichzeitig mit einem
        // OS-Hintergrundabruf) wuerden dieselben neuen Artikel doppelt
        // einspielen und doppelt benachrichtigen.
        await _syncAllLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
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
        finally
        {
            _syncAllLock.Release();
        }
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
        using var feedReader = PrepareFeedReader(reader);
        var syndicationFeed = await Task.Run(() => SyndicationFeed.Load(feedReader), cancellationToken).ConfigureAwait(false);

        var feedItems = syndicationFeed.Items.ToList();

        var keywordTexts = await _keywordFilter.GetKeywordTextsAsync(feed.Id).ConfigureAwait(false);
        var imageItemIds = await _contentStore
            .GetImageIdsAsync(existingItems.Select(i => i.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);
        var collected = CollectNewItems(
            new CollectContext(feed, feedItems, existingItems, keywordTexts, imageItemIds),
            cancellationToken);

        var failedImageDownloads = await DownloadImagesAsync(
            collected.ImageCandidates,
            collected.NewItems.ToDictionary(i => i.Id),
            collected.ContentBackfill,
            cancellationToken).ConfigureAwait(false);
        if (failedImageDownloads > 0)
        {
            _ = _debugLogService?.LogAsync(
                DebugLogCategory.Sync,
                $"Image download failed for {failedImageDownloads} item(s) of feed '{feed.Title}'",
                level: DebugLogLevel.Warning);
        }

        var newItems = collected.NewItems.Count;
        if (newItems > 0)
        {
            await _itemRepository.AddRangeAsync(collected.NewItems).ConfigureAwait(false);
        }

        if (collected.ContentBackfill.Count > 0)
        {
            await _contentStore.SetRangeAsync(collected.ContentBackfill).ConfigureAwait(false);
        }

        var status = DetermineStatus(newItems, feedItems.Count, existingCount, lastPublishedAt);
        var filteredSuffix = collected.FilteredCount > 0 ? $", {collected.FilteredCount} filtered" : string.Empty;
        var message = status == FeedHealth.Warning
            ? $"Synchronized {feedItems.Count} items, {newItems} new{filteredSuffix}. Health warning triggered."
            : $"Synchronized {feedItems.Count} items, {newItems} new{filteredSuffix}.";

        var resolvedTitle = ResolveFeedTitle(feed, syndicationFeed);

        // Feeds stored before favicon discovery existed (or added while offline)
        // get their icon backfilled on the first successful sync; the lookup is
        // strictly isolated inside the icon service.
        var faviconUrl = feed.FaviconUrl ?? await TryFindFaviconUrlAsync(feed.Url, syndicationFeed, cancellationToken).ConfigureAwait(false);

        await UpdateFeedHealthAsync(feed, status, new FeedHealthUpdate(resolvedTitle, faviconUrl)).ConfigureAwait(false);
        await UpdateLogAsync(log, status, message).ConfigureAwait(false);

        if (collected.NewItems.Count > 0)
        {
            try
            {
                await _notificationService.NotifyNewItemsAsync(feed, collected.NewItems, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Ein Fehler im Benachrichtigungspfad darf das Sync-Ergebnis nicht verfaelschen.
                Debug.WriteLine($"FeedSyncService notification failed: {ex}");
                _ = _debugLogService?.LogAsync(
                    DebugLogCategory.Sync,
                    $"Notification failed for feed '{feed.Title}'",
                    ex.ToString(),
                    DebugLogLevel.Warning);
            }
        }

        return new SyncResult(status, newItems, message);
    }

    private CollectResult CollectNewItems(CollectContext context, CancellationToken cancellationToken)
    {
        var newItemEntities = new List<Item>();
        var contentBackfill = new List<ItemContentEntry>();
        var imageCandidates = new List<(Guid ItemId, string ImageUrl)>();
        var filteredCount = 0;
        var knownKeys = new HashSet<string>(
            context.ExistingItems.Select(i => i.GuidOrHash),
            StringComparer.Ordinal);

        // Items ohne gespeicherten Inhalt (z. B. nach einem Restore ohne
        // Content-Datenbank) erhalten den frisch gelesenen Inhalt nachgeladen;
        // die Keyword-Filterung entfaellt, weil das Item den Filter beim
        // urspruenglichen Speichern bereits passiert hat.
        var backfillCandidates = context.ExistingItems
            .Where(i => i.ContentHtml is null)
            .GroupBy(i => i.GuidOrHash, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.Ordinal);

        // Bestandsitems ohne gespeichertes Bild werden Bildkandidaten — das ist
        // zugleich der implizite Retry fehlgeschlagener Downloads: ohne
        // gespeichertes Bild bleibt das Item Kandidat beim naechsten Sync.
        var imageBackfillCandidates = context.ExistingItems
            .Where(i => !context.ImageItemIds.Contains(i.Id))
            .GroupBy(i => i.GuidOrHash, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.Ordinal);

        foreach (var feedItem in context.FeedItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var link = feedItem.Links.FirstOrDefault()?.Uri?.ToString();
            var publishedAt = feedItem.PublishDate == DateTimeOffset.MinValue
                ? (DateTime?)null
                : feedItem.PublishDate.UtcDateTime;
            var guidOrHash = NormalizeGuidOrHash(feedItem, link, publishedAt);

            if (!knownKeys.Add(guidOrHash))
            {
                if (backfillCandidates.Remove(guidOrHash, out var existingItemId) &&
                    GetContentHtml(feedItem) is { } backfillContent)
                {
                    contentBackfill.Add(new ItemContentEntry(existingItemId, backfillContent));
                }

                if (imageBackfillCandidates.Remove(guidOrHash, out var imageItemId) &&
                    _imageService.ResolveImageUrl(feedItem, GetContentHtml(feedItem), link, context.Feed.Url) is { } backfillImageUrl)
                {
                    imageCandidates.Add((imageItemId, backfillImageUrl));
                }

                continue;
            }

            var title = feedItem.Title?.Text;
            var contentHtml = GetContentHtml(feedItem);

            if (_keywordFilter.MatchesAny(title, contentHtml, context.KeywordTexts))
            {
                filteredCount++;
                continue;
            }

            var newItem = new Item
            {
                Id = Guid.NewGuid(),
                FeedId = context.Feed.Id,
                Title = title ?? string.Empty,
                Link = link,
                PublishedAt = publishedAt,
                GuidOrHash = guidOrHash,
                IsRead = false,
                IsSavedForLater = false,
                ContentHtml = contentHtml,
            };
            newItemEntities.Add(newItem);

            if (_imageService.ResolveImageUrl(feedItem, contentHtml, link, context.Feed.Url) is { } imageUrl)
            {
                imageCandidates.Add((newItem.Id, imageUrl));
            }
        }

        return new CollectResult(newItemEntities, filteredCount, contentBackfill, imageCandidates);
    }

    // Sequentielle Bild-Downloads: jeder Kandidat erhaelt ein ItemImage oder
    // null — Download-Fehler sind im Image-Service isoliert, ein
    // OperationCanceledException bricht den Sync ab. Neue Items bekommen das
    // Bild per set-Accessor zugewiesen; Backfill-Treffer werden pro ItemId zu
    // genau einem Eintrag in contentBackfill zusammengefuehrt, damit ein Item
    // ohne Inhalt UND ohne Bild (Restore-Szenario) nicht zwei Eintraege
    // erzeugt, von denen die "letzter gewinnt"-Dedup in SetRangeAsync einen
    // verwerfen wuerde.
    private async Task<int> DownloadImagesAsync(
        IReadOnlyList<(Guid ItemId, string ImageUrl)> imageCandidates,
        IReadOnlyDictionary<Guid, Item> newItemsById,
        List<ItemContentEntry> contentBackfill,
        CancellationToken cancellationToken)
    {
        var backfillIndex = contentBackfill
            .Select((entry, index) => (entry.ItemId, index))
            .ToDictionary(x => x.ItemId, x => x.index);

        var failed = 0;
        foreach (var (itemId, imageUrl) in imageCandidates)
        {
            var image = await _imageService.TryDownloadImageAsync(imageUrl, cancellationToken).ConfigureAwait(false);
            if (image is null)
            {
                failed++;
                continue;
            }

            if (newItemsById.TryGetValue(itemId, out var newItem))
            {
                newItem.Image = image;
                continue;
            }

            if (backfillIndex.TryGetValue(itemId, out var index))
            {
                var existing = contentBackfill[index];
                contentBackfill[index] = new ItemContentEntry(itemId, existing.ContentHtml, image);
            }
            else
            {
                backfillIndex[itemId] = contentBackfill.Count;
                contentBackfill.Add(new ItemContentEntry(itemId, null, image));
            }
        }

        return failed;
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

    private async Task UpdateFeedHealthAsync(Feed feed, string status, FeedHealthUpdate update)
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
            Title = update.ResolvedTitle ?? feed.Title,
            CategoryId = feed.CategoryId,
            LastCheckedAt = DateTime.UtcNow,
            HealthStatus = status,
            HealthLastChange = healthLastChange,
            NotificationsEnabled = feed.NotificationsEnabled,
            FaviconUrl = update.FaviconUrl ?? feed.FaviconUrl,
            LastErrorKind = update.ErrorKind,
            LastErrorMessage = update.ErrorMessage,
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

    // Atom 0.3 documents are normalized to Atom 1.0 on the fly so that
    // SyndicationFeed.Load remains the single parse entry point; the peek only
    // consumes preamble/whitespace and works on the non-seekable HTTP stream.
    private static XmlReader PrepareFeedReader(XmlReader reader)
    {
        reader.MoveToContent();
        return reader.LocalName == "feed" && reader.NamespaceURI == Atom03NormalizingXmlReader.NamespaceUri
            ? new Atom03NormalizingXmlReader(reader)
            : reader;
    }

    /// <summary>
    /// Bundles the inputs of <see cref="CollectNewItems"/> so the signature
    /// stays stable when further backfill channels are added.
    /// </summary>
    /// <param name="Feed">The feed entity being synchronized.</param>
    /// <param name="FeedItems">The parsed syndication items of the downloaded feed.</param>
    /// <param name="ExistingItems">The items already stored for this feed.</param>
    /// <param name="KeywordTexts">The active keyword filter texts.</param>
    /// <param name="ImageItemIds">Ids of existing items that already have a stored image.</param>
    /// <returns>A context record carrying all inputs of <see cref="CollectNewItems"/>.</returns>
    private sealed record CollectContext(
        Feed Feed,
        IReadOnlyList<SyndicationItem> FeedItems,
        IReadOnlyList<Item> ExistingItems,
        IReadOnlyList<string> KeywordTexts,
        IReadOnlySet<Guid> ImageItemIds);

    /// <summary>
    /// The outcome of <see cref="CollectNewItems"/>: the new item entities, the
    /// keyword-filtered count and the backfill work items for content and image.
    /// </summary>
    /// <param name="NewItems">The newly created item entities.</param>
    /// <param name="FilteredCount">The number of feed items removed by the keyword filter.</param>
    /// <param name="ContentBackfill">Existing items whose stored content must be refetched.</param>
    /// <param name="ImageCandidates">Items whose article image should be downloaded.</param>
    /// <returns>A result record carrying the outcome of <see cref="CollectNewItems"/>.</returns>
    private sealed record CollectResult(
        List<Item> NewItems,
        int FilteredCount,
        List<ItemContentEntry> ContentBackfill,
        List<(Guid ItemId, string ImageUrl)> ImageCandidates);
}
