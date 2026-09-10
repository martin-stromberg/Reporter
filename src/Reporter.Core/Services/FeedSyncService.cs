using System.Security.Cryptography;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSyncService"/> class.
    /// </summary>
    /// <param name="feedRepository">The feed repository.</param>
    /// <param name="itemRepository">The item repository.</param>
    /// <param name="syncLogRepository">The sync log repository.</param>
    /// <param name="httpClient">The HTTP client used to retrieve feeds.</param>
    public FeedSyncService(
        IFeedRepository feedRepository,
        IItemRepository itemRepository,
        ISyncLogRepository syncLogRepository,
        HttpClient httpClient)
    {
        _feedRepository = feedRepository;
        _itemRepository = itemRepository;
        _syncLogRepository = syncLogRepository;
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<SyncResult> SyncFeedAsync(Guid feedId, CancellationToken cancellationToken = default)
    {
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
        catch (Exception ex)
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
        var newItems = 0;

        foreach (var feedItem in feedItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var link = feedItem.Links.FirstOrDefault()?.Uri?.ToString();
            var publishedAt = feedItem.PublishDate == DateTimeOffset.MinValue
                ? (DateTime?)null
                : feedItem.PublishDate.UtcDateTime;
            var guidOrHash = NormalizeGuidOrHash(feedItem, link, publishedAt);

            var existing = await _itemRepository.GetByGuidOrHashAsync(feed.Id, guidOrHash).ConfigureAwait(false);
            if (existing is not null)
            {
                continue;
            }

            var item = new Item
            {
                Id = Guid.NewGuid(),
                FeedId = feed.Id,
                Title = feedItem.Title?.Text ?? string.Empty,
                Link = link,
                PublishedAt = publishedAt,
                GuidOrHash = guidOrHash,
                IsRead = false,
                IsSavedForLater = false,
                ContentHtml = GetContentHtml(feedItem),
            };

            await _itemRepository.AddAsync(item).ConfigureAwait(false);
            newItems++;
        }

        var status = DetermineStatus(newItems, feedItems.Count, existingCount, lastPublishedAt);
        var message = status == FeedHealth.Warning
            ? $"Synchronized {feedItems.Count} items, {newItems} new. Health warning triggered."
            : $"Synchronized {feedItems.Count} items, {newItems} new.";

        await UpdateFeedHealthAsync(feed, status).ConfigureAwait(false);
        await UpdateLogAsync(log, status, message).ConfigureAwait(false);
        return new SyncResult(status, newItems, message);
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

    private async Task UpdateFeedHealthAsync(Feed feed, string status)
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
            Title = feed.Title,
            CategoryId = feed.CategoryId,
            LastCheckedAt = DateTime.UtcNow,
            HealthStatus = status,
            HealthLastChange = healthLastChange,
        }).ConfigureAwait(false);
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
