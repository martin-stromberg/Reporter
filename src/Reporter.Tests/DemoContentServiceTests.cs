// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="DemoContentService"/> class.
/// </summary>
public class DemoContentServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly CategoryRepository _categoryRepository;
    private readonly FeedRepository _feedRepository;
    private readonly FakeDebugLogService _debugLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoContentServiceTests"/> class.
    /// </summary>
    public DemoContentServiceTests()
    {
        _factory = new TestDbContextFactory();
        _categoryRepository = new CategoryRepository(_factory);
        _feedRepository = new FeedRepository(_factory, new FakeItemContentStore());
        _debugLogService = new FakeDebugLogService();
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private DemoContentService CreateService(bool isFirstRun = true, bool demoSeedSuppressed = false)
        => new(
            new FirstRunState { IsFirstRun = isFirstRun, DemoSeedSuppressed = demoSeedSuppressed },
            _categoryRepository,
            _feedRepository,
            _debugLogService);

    /// <summary>
    /// Verifies that a first run creates the "News" category and the demo feed
    /// assigned to it.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EnsureSeededAsync_FirstRun_CreatesNewsCategoryAndDemoFeed()
    {
        var service = CreateService();

        await service.EnsureSeededAsync();

        var categories = await _categoryRepository.GetAllAsync();
        var newsCategory = Assert.Single(categories);
        Assert.Equal(DemoContentService.DemoCategoryName, newsCategory.Name);

        var feed = await _feedRepository.GetByUrlAsync(DemoContentService.DemoFeedUrl);
        Assert.NotNull(feed);
        Assert.Equal(newsCategory.Id, feed.CategoryId);
    }

    /// <summary>
    /// Verifies that the seeded demo feed carries the expected fixed defaults.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EnsureSeededAsync_FirstRun_SeedsExpectedFeedDefaults()
    {
        var service = CreateService();

        await service.EnsureSeededAsync();

        var feed = await _feedRepository.GetByUrlAsync(DemoContentService.DemoFeedUrl);
        Assert.NotNull(feed);
        Assert.Equal(DemoContentService.DemoFeedTitle, feed.Title);
        Assert.Equal(FeedHealth.Ok, feed.HealthStatus);
        Assert.Null(feed.LastCheckedAt);
        Assert.Null(feed.HealthLastChange);
        Assert.False(feed.NotificationsEnabled);
        Assert.Null(feed.FaviconUrl);
        Assert.Null(feed.LastMessageKind);
        Assert.Null(feed.LastMessage);
    }

    /// <summary>
    /// Verifies that a subsequent run seeds nothing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EnsureSeededAsync_NotFirstRun_CreatesNothing()
    {
        var service = CreateService(isFirstRun: false);

        await service.EnsureSeededAsync();

        Assert.Empty(await _categoryRepository.GetAllAsync());
        Assert.Empty(await _feedRepository.GetAllAsync());
    }

    /// <summary>
    /// Verifies that a suppressed seed creates nothing even on the first run.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EnsureSeededAsync_SeedSuppressed_CreatesNothing()
    {
        var service = CreateService(demoSeedSuppressed: true);

        await service.EnsureSeededAsync();

        Assert.Empty(await _categoryRepository.GetAllAsync());
        Assert.Empty(await _feedRepository.GetAllAsync());
    }

    /// <summary>
    /// Verifies that an existing "News" category is reused instead of
    /// triggering the unique index on <c>categories.name</c>.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EnsureSeededAsync_ExistingNewsCategory_ReusesCategory()
    {
        var existing = new Category { Id = Guid.NewGuid(), Name = "News" };
        await _categoryRepository.AddAsync(existing);
        var service = CreateService();

        await service.EnsureSeededAsync();

        var feed = await _feedRepository.GetByUrlAsync(DemoContentService.DemoFeedUrl);
        Assert.NotNull(feed);
        Assert.Equal(existing.Id, feed.CategoryId);
        Assert.Single(
            await _categoryRepository.GetAllAsync(),
            c => string.Equals(c.Name, "News", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that a repeated call stays idempotent via the URL check.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EnsureSeededAsync_CalledTwice_IsIdempotent()
    {
        var service = CreateService();

        await service.EnsureSeededAsync();
        await service.EnsureSeededAsync();

        Assert.Single(await _feedRepository.GetAllAsync());
        Assert.Single(await _categoryRepository.GetAllAsync());
    }

    /// <summary>
    /// Verifies that an already existing demo feed skips the seed entirely.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EnsureSeededAsync_ExistingDemoFeed_SkipsSeed()
    {
        await _feedRepository.AddAsync(new Feed
        {
            Id = Guid.NewGuid(),
            Url = DemoContentService.DemoFeedUrl,
            Title = "Existing Demo Feed",
            NotificationsEnabled = true,
        });
        var service = CreateService();

        await service.EnsureSeededAsync();

        Assert.Empty(await _categoryRepository.GetAllAsync());
        Assert.Single(await _feedRepository.GetAllAsync());
    }

    /// <summary>
    /// Verifies that an already cancelled token aborts the seed before any
    /// write instead of running it to completion.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EnsureSeededAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.EnsureSeededAsync(cts.Token));

        Assert.Empty(await _categoryRepository.GetAllAsync());
        Assert.Empty(await _feedRepository.GetAllAsync());
    }

    /// <summary>
    /// Verifies that a successful first-run seed writes an informational
    /// lifecycle entry to the session debug log.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task EnsureSeededAsync_FirstRun_LogsInfoEntry()
    {
        var service = CreateService();

        await service.EnsureSeededAsync();

        var entry = Assert.Single(_debugLogService.LoggedEntries);
        Assert.Equal(DebugLogCategory.Lifecycle, entry.Category);
        Assert.Equal("Demo content seeded", entry.Message);
        Assert.Equal(DebugLogLevel.Info, entry.Level);
    }
}
