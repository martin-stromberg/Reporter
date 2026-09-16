// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.Services;

/// <summary>
/// Evaluates the configured notification rules (global switch, per-feed switch,
/// quiet hours and keyword filters) for newly stored items and delegates the
/// display to <see cref="ILocalNotificationService"/>.
/// </summary>
public class NotificationService : INotificationService
{
    private const int MaxSummaryTitlesLength = 160;

    private readonly ISettingsRepository _settingsRepository;
    private readonly IKeywordFilter _keywordFilter;
    private readonly ILocalNotificationService _localNotificationService;
    private readonly IDebugLogService? _debugLogService;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="settingsRepository">The settings repository.</param>
    /// <param name="keywordFilter">The keyword filter used to exclude matching items.</param>
    /// <param name="localNotificationService">The platform notification service.</param>
    /// <param name="timeProvider">The time provider used for the quiet-hours evaluation.</param>
    /// <param name="debugLogService">The optional session debug log service used to record suppressed notifications.</param>
    public NotificationService(
        ISettingsRepository settingsRepository,
        IKeywordFilter keywordFilter,
        ILocalNotificationService localNotificationService,
        TimeProvider? timeProvider = null,
        IDebugLogService? debugLogService = null)
    {
        _settingsRepository = settingsRepository;
        _keywordFilter = keywordFilter;
        _localNotificationService = localNotificationService;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _debugLogService = debugLogService;
    }

    /// <inheritdoc />
    public async Task NotifyNewItemsAsync(Feed feed, IReadOnlyList<Item> newItems, CancellationToken cancellationToken = default)
    {
        if (!feed.NotificationsEnabled || newItems.Count == 0)
        {
            return;
        }

        var settings = await _settingsRepository.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!settings.NotificationsEnabled)
        {
            _ = _debugLogService?.LogAsync(DebugLogCategory.Notification, "Notification suppressed: globally disabled", $"Feed '{feed.Title}', {newItems.Count} new items", DebugLogLevel.Info);
            return;
        }

        if (IsQuietHoursActive(settings))
        {
            _ = _debugLogService?.LogAsync(DebugLogCategory.Notification, "Notification suppressed: quiet hours", $"Feed '{feed.Title}', {newItems.Count} new items", DebugLogLevel.Info);
            return;
        }

        var keywordTexts = await _keywordFilter.GetKeywordTextsAsync().ConfigureAwait(false);
        var candidates = newItems
            .Where(i => !_keywordFilter.MatchesAny(i.Title, i.ContentHtml, keywordTexts))
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        if (settings.NotificationSummaryEnabled)
        {
            var titles = Truncate(string.Join(", ", candidates.Select(i => i.Title)), MaxSummaryTitlesLength);
            var body = string.Format(CultureInfo.CurrentCulture, AppResources.NotificationSummaryFormat, candidates.Count, titles);
            var identifier = BuildSummaryIdentifier(feed.Id, candidates);
            var userInfo = new Dictionary<string, string> { ["feedId"] = feed.Id.ToString() };
            await _localNotificationService.ShowAsync(feed.Title, body, identifier, userInfo, cancellationToken).ConfigureAwait(false);
            return;
        }

        foreach (var item in candidates)
        {
            await _localNotificationService.ShowAsync(feed.Title, item.Title, item.Id.ToString(), BuildItemUserInfo(item), cancellationToken).ConfigureAwait(false);
        }
    }

    private static IReadOnlyDictionary<string, string> BuildItemUserInfo(Item item)
    {
        var userInfo = new Dictionary<string, string> { ["itemId"] = item.Id.ToString() };
        if (item.Link is not null)
        {
            userInfo["link"] = item.Link;
        }

        return userInfo;
    }

    private bool IsQuietHoursActive(Settings settings)
    {
        if (settings.QuietHoursStart is not { } start || settings.QuietHoursEnd is not { } end || start == end)
        {
            return false;
        }

        var now = _timeProvider.GetLocalNow().TimeOfDay;
        return start < end
            ? now >= start && now < end
            : now >= start || now < end;
    }

    private static string BuildSummaryIdentifier(Guid feedId, IReadOnlyList<Item> items)
    {
        var joined = string.Join("|", items.Select(i => i.Id.ToString()).OrderBy(id => id, StringComparer.Ordinal));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(joined));
        return $"{feedId}-{Convert.ToHexString(hash)}";
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength - 1), "…");
    }
}
