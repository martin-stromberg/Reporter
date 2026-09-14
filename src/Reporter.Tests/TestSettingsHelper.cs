// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// Provides a shared helper to update the singleton settings record in tests
/// without repeating the field-for-field copy of <see cref="Settings"/>.
/// </summary>
public static class TestSettingsHelper
{
    /// <summary>
    /// Loads the persisted settings and saves them back with the given overrides applied.
    /// A <see langword="null"/> argument keeps the persisted value.
    /// </summary>
    /// <param name="repository">The settings repository.</param>
    /// <param name="notificationsEnabled">The notifications switch override, or <see langword="null"/> to keep the persisted value.</param>
    /// <param name="notificationSummaryEnabled">The summary-mode switch override, or <see langword="null"/> to keep the persisted value.</param>
    /// <param name="quietHoursStart">The quiet-hours start override, or <see langword="null"/> to keep the persisted value.</param>
    /// <param name="quietHoursEnd">The quiet-hours end override, or <see langword="null"/> to keep the persisted value.</param>
    /// <param name="language">The language selection override, or <see langword="null"/> to keep the persisted value.</param>
    /// <param name="refreshOnStartupEnabled">The startup-refresh switch override, or <see langword="null"/> to keep the persisted value.</param>
    /// <param name="unreadSortOrder">The unread sort order override, or <see langword="null"/> to keep the persisted value.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task SaveAsync(
        ISettingsRepository repository,
        bool? notificationsEnabled = null,
        bool? notificationSummaryEnabled = null,
        TimeSpan? quietHoursStart = null,
        TimeSpan? quietHoursEnd = null,
        string? language = null,
        bool? refreshOnStartupEnabled = null,
        string? unreadSortOrder = null)
    {
        var settings = await repository.GetAsync();
        await repository.SaveAsync(new Settings
        {
            Id = settings.Id,
            RetentionDays = settings.RetentionDays,
            AutoMarkReadMode = settings.AutoMarkReadMode,
            AutoMarkReadDelaySeconds = settings.AutoMarkReadDelaySeconds,
            NotificationsEnabled = notificationsEnabled ?? settings.NotificationsEnabled,
            NotificationSummaryEnabled = notificationSummaryEnabled ?? settings.NotificationSummaryEnabled,
            QuietHoursStart = quietHoursStart ?? settings.QuietHoursStart,
            QuietHoursEnd = quietHoursEnd ?? settings.QuietHoursEnd,
            AutoRefreshEnabled = settings.AutoRefreshEnabled,
            RefreshIntervalMinutes = settings.RefreshIntervalMinutes,
            RefreshOnStartupEnabled = refreshOnStartupEnabled ?? settings.RefreshOnStartupEnabled,
            UnreadSortOrder = unreadSortOrder ?? settings.UnreadSortOrder,
            Theme = settings.Theme,
            Language = language ?? settings.Language,
        });
    }
}
