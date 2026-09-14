// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="SettingsRepository"/> class.
/// </summary>
public class SettingsRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsRepositoryTests"/> class.
    /// </summary>
    public SettingsRepositoryTests()
    {
        _factory = new TestDbContextFactory();
        _repository = new SettingsRepository(_factory);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that GetAsync returns the seeded singleton settings record.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAsync_ReturnsSeededSettings()
    {
        var result = await _repository.GetAsync();

        Assert.NotNull(result);
        Assert.Equal(Settings.DefaultId, result.Id);
        Assert.True(result.RetentionDays > 0);
    }

    /// <summary>
    /// Verifies that GetAsync creates a default settings record when none exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAsync_CreatesDefaultRecordWhenMissing()
    {
        await using (var context = _factory.CreateDbContext())
        {
            context.Settings.RemoveRange(context.Settings);
            await context.SaveChangesAsync();
        }

        var result = await _repository.GetAsync();

        Assert.NotNull(result);
        Assert.Equal(Settings.DefaultId, result.Id);
    }

    /// <summary>
    /// Verifies that SaveAsync always updates the singleton record.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveAsync_OverwritesSameRecord()
    {
        await _repository.SaveAsync(new Settings
        {
            Id = Settings.DefaultId,
            RetentionDays = 7,
            AutoMarkReadDelaySeconds = 10,
            NotificationsEnabled = false,
            NotificationSummaryEnabled = true,
            AutoRefreshEnabled = false,
            RefreshIntervalMinutes = 15,
            RefreshOnStartupEnabled = false,
            Theme = "dark",
        });

        var result = await _repository.GetAsync();

        Assert.Equal(7, result.RetentionDays);
        Assert.Equal(10, result.AutoMarkReadDelaySeconds);
        Assert.False(result.NotificationsEnabled);
        Assert.True(result.NotificationSummaryEnabled);
    }

    /// <summary>
    /// Verifies that SaveAsync ignores a different id and updates the singleton record.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveAsync_IgnoresDifferentIdAndUpdatesDefault()
    {
        await _repository.SaveAsync(new Settings
        {
            Id = Guid.NewGuid(),
            RetentionDays = 14,
            AutoMarkReadDelaySeconds = 20,
            NotificationsEnabled = true,
            NotificationSummaryEnabled = false,
            AutoRefreshEnabled = true,
            RefreshIntervalMinutes = 60,
            RefreshOnStartupEnabled = true,
            Theme = "light",
        });

        var result = await _repository.GetAsync();

        Assert.Equal(Settings.DefaultId, result.Id);
        Assert.Equal(14, result.RetentionDays);
        Assert.Equal(20, result.AutoMarkReadDelaySeconds);
        Assert.True(result.NotificationsEnabled);
    }

    /// <summary>
    /// Verifies that GetAsync always returns a single record even after multiple calls.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAsync_AlwaysReturnsSingleRecord()
    {
        await _repository.GetAsync();
        await _repository.GetAsync();

        await using var context = _factory.CreateDbContext();
        var count = context.Settings.Count();

        Assert.Equal(1, count);
    }

    /// <summary>
    /// Verifies that SaveAsync persists the new auto-refresh and theme fields.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveAsync_PersistsNewFields()
    {
        await _repository.SaveAsync(new Settings
        {
            Id = Settings.DefaultId,
            RetentionDays = 30,
            AutoMarkReadDelaySeconds = 5,
            NotificationsEnabled = true,
            NotificationSummaryEnabled = true,
            AutoRefreshEnabled = false,
            RefreshIntervalMinutes = 240,
            RefreshOnStartupEnabled = false,
            Theme = "dark",
        });

        var result = await _repository.GetAsync();

        Assert.False(result.AutoRefreshEnabled);
        Assert.Equal(240, result.RefreshIntervalMinutes);
        Assert.Equal("dark", result.Theme);
        Assert.True(result.NotificationSummaryEnabled);
    }

    /// <summary>
    /// Verifies that SaveAsync persists the startup-refresh switch and the unread
    /// sort order and GetAsync reads them back.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveAsync_PersistsStartupRefreshAndSortOrder()
    {
        await _repository.SaveAsync(new Settings
        {
            Id = Settings.DefaultId,
            RetentionDays = 30,
            AutoMarkReadDelaySeconds = 5,
            NotificationsEnabled = true,
            NotificationSummaryEnabled = false,
            AutoRefreshEnabled = true,
            RefreshIntervalMinutes = 30,
            RefreshOnStartupEnabled = false,
            UnreadSortOrder = SettingsValues.SortOrderAscending,
            Theme = "system",
        });

        var result = await _repository.GetAsync();

        Assert.False(result.RefreshOnStartupEnabled);
        Assert.Equal(SettingsValues.SortOrderAscending, result.UnreadSortOrder);
    }

    /// <summary>
    /// Verifies that SaveAsync persists the language column and GetAsync reads it back.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveAsync_PersistsLanguage()
    {
        await _repository.SaveAsync(new Settings
        {
            Id = Settings.DefaultId,
            RetentionDays = 30,
            AutoMarkReadDelaySeconds = 5,
            NotificationsEnabled = true,
            NotificationSummaryEnabled = false,
            AutoRefreshEnabled = true,
            RefreshIntervalMinutes = 30,
            RefreshOnStartupEnabled = true,
            Theme = "system",
            Language = "de",
        });

        var result = await _repository.GetAsync();

        Assert.Equal("de", result.Language);
    }

    /// <summary>
    /// Verifies that SaveAsync persists the notification summary flag in both directions.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveAsync_PersistsNotificationSummaryEnabled()
    {
        await TestSettingsHelper.SaveAsync(_repository, notificationSummaryEnabled: true);

        Assert.True((await _repository.GetAsync()).NotificationSummaryEnabled);

        await TestSettingsHelper.SaveAsync(_repository, notificationSummaryEnabled: false);

        Assert.False((await _repository.GetAsync()).NotificationSummaryEnabled);
    }
}
