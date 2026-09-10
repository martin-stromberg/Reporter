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
        });

        var result = await _repository.GetAsync();

        Assert.Equal(7, result.RetentionDays);
        Assert.Equal(10, result.AutoMarkReadDelaySeconds);
        Assert.False(result.NotificationsEnabled);
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
}
