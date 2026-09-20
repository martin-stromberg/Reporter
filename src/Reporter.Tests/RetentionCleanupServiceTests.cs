// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="RetentionCleanupService"/> class.
/// </summary>
public class RetentionCleanupServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly FakeItemContentStore _contentStore;
    private readonly ItemRepository _itemRepository;
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly RetentionCleanupService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionCleanupServiceTests"/> class.
    /// </summary>
    public RetentionCleanupServiceTests()
    {
        _factory = new TestDbContextFactory();
        _contentStore = new FakeItemContentStore();
        _itemRepository = new ItemRepository(_factory, _contentStore);
        _settingsRepository = new SettingsRepository(_factory);
        _keywordRepository = new KeywordRepository(_factory);
        _service = new RetentionCleanupService(_settingsRepository, _itemRepository, new KeywordFilter(_keywordRepository, new KeywordMatcher()), _contentStore);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    private async Task SetRetentionDaysAsync(int retentionDays)
    {
        var settings = await _settingsRepository.GetAsync();
        await _settingsRepository.SaveAsync(new Settings
        {
            Id = settings.Id,
            RetentionDays = retentionDays,
            AutoMarkReadMode = settings.AutoMarkReadMode,
            AutoMarkReadDelaySeconds = settings.AutoMarkReadDelaySeconds,
            NotificationsEnabled = settings.NotificationsEnabled,
            NotificationSummaryEnabled = settings.NotificationSummaryEnabled,
            QuietHoursStart = settings.QuietHoursStart,
            QuietHoursEnd = settings.QuietHoursEnd,
            AutoRefreshEnabled = settings.AutoRefreshEnabled,
            RefreshIntervalMinutes = settings.RefreshIntervalMinutes,
            RefreshOnStartupEnabled = settings.RefreshOnStartupEnabled,
            UnreadSortOrder = settings.UnreadSortOrder,
            Theme = settings.Theme,
            DebugCollectionEnabled = settings.DebugCollectionEnabled,
        });
    }

    /// <summary>
    /// Verifies that CleanupAsync deletes expired read items but keeps saved and unread items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CleanupAsync_DeletesExpiredButKeepsSaved()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var expiredRead = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Expired Read",
            GuidOrHash = "expired-read",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow.AddDays(-40),
        };
        var expiredSaved = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Expired Saved",
            GuidOrHash = "expired-saved",
            IsRead = true,
            IsSavedForLater = true,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow.AddDays(-40),
        };
        var unreadOld = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Unread Old",
            GuidOrHash = "unread-old",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
        };
        await _itemRepository.AddAsync(expiredRead);
        await _itemRepository.AddAsync(expiredSaved);
        await _itemRepository.AddAsync(unreadOld);

        var deleted = await _service.CleanupAsync();

        Assert.Equal(1, deleted);
        Assert.Null(await _itemRepository.GetByIdAsync(expiredRead.Id));
        Assert.NotNull(await _itemRepository.GetByIdAsync(expiredSaved.Id));
        Assert.NotNull(await _itemRepository.GetByIdAsync(unreadOld.Id));
    }

    /// <summary>
    /// Verifies that CleanupAsync skips deletion when the retention period is zero or negative.
    /// </summary>
    /// <param name="retentionDays">The configured retention days.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CleanupAsync_ZeroOrNegativeRetentionDays_Skips(int retentionDays)
    {
        await SetRetentionDaysAsync(retentionDays);
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var expiredRead = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Expired Read",
            GuidOrHash = "expired-read",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow.AddDays(-40),
        };
        await _itemRepository.AddAsync(expiredRead);

        var deleted = await _service.CleanupAsync();

        Assert.Equal(0, deleted);
        Assert.NotNull(await _itemRepository.GetByIdAsync(expiredRead.Id));
    }

    /// <summary>
    /// Verifies that CleanupAsync uses the configured retention days as the cutoff.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CleanupAsync_RespectsConfiguredRetentionDays()
    {
        await SetRetentionDaysAsync(10);
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var olderThanRetention = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Older Than Retention",
            GuidOrHash = "older",
            IsRead = true,
            IsSavedForLater = false,
            ReadAt = DateTime.UtcNow.AddDays(-15),
        };
        var withinRetention = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Within Retention",
            GuidOrHash = "within",
            IsRead = true,
            IsSavedForLater = false,
            ReadAt = DateTime.UtcNow.AddDays(-5),
        };
        await _itemRepository.AddAsync(olderThanRetention);
        await _itemRepository.AddAsync(withinRetention);

        var deleted = await _service.CleanupAsync();

        Assert.Equal(1, deleted);
        Assert.Null(await _itemRepository.GetByIdAsync(olderThanRetention.Id));
        Assert.NotNull(await _itemRepository.GetByIdAsync(withinRetention.Id));
    }

    /// <summary>
    /// Verifies that CleanupAsync propagates cancellation to the repository calls.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CleanupAsync_CancelledToken_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.CleanupAsync(cts.Token));
    }

    /// <summary>
    /// Verifies that CleanupAsync deletes read, expired items matching a keyword while keeping non-matching items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CleanupAsync_DeletesKeywordMatchedExpired()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Werbung" });
        var matched = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Jetzt Werbung sichern",
            GuidOrHash = "matched",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow.AddDays(-1),
        };
        var notMatched = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Normaler Artikel",
            GuidOrHash = "not-matched",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow.AddDays(-1),
        };
        await _itemRepository.AddAsync(matched);
        await _itemRepository.AddAsync(notMatched);

        var deleted = await _service.CleanupAsync();

        Assert.Equal(1, deleted);
        Assert.Null(await _itemRepository.GetByIdAsync(matched.Id));
        Assert.NotNull(await _itemRepository.GetByIdAsync(notMatched.Id));
    }

    /// <summary>
    /// Verifies that CleanupAsync never deletes unread or saved-for-later items even when they match a keyword.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CleanupAsync_KeywordMatch_KeepsUnreadAndSaved()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Werbung" });
        var unreadMatch = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Werbung ungelesen",
            GuidOrHash = "unread-match",
            IsRead = false,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
        };
        var savedMatch = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Werbung gespeichert",
            GuidOrHash = "saved-match",
            IsRead = true,
            IsSavedForLater = true,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow.AddDays(-1),
        };
        await _itemRepository.AddAsync(unreadMatch);
        await _itemRepository.AddAsync(savedMatch);

        var deleted = await _service.CleanupAsync();

        Assert.Equal(0, deleted);
        Assert.NotNull(await _itemRepository.GetByIdAsync(unreadMatch.Id));
        Assert.NotNull(await _itemRepository.GetByIdAsync(savedMatch.Id));
    }

    /// <summary>
    /// Verifies that the keyword deletion rule uses PublishedAt instead of ReadAt as the expiration timestamp.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Sponsoring" });
        var matched = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Sponsoring-Artikel",
            GuidOrHash = "sponsored",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-90),
            ReadAt = DateTime.UtcNow,
        };
        await _itemRepository.AddAsync(matched);

        var deleted = await _service.CleanupAsync();

        Assert.Equal(1, deleted);
        Assert.Null(await _itemRepository.GetByIdAsync(matched.Id));
    }

    /// <summary>
    /// Verifies that a feed-scoped keyword only deletes matching items of its own
    /// feed while the same matching text in another feed is kept.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CleanupAsync_FeedKeywordMatch_DeletesOnlyOwnFeed()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var otherFeedId = await TestDataSeeder.SeedFeedAsync(_factory, "https://example.com/other");
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Werbung", FeedId = feedId });
        var ownMatch = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Jetzt Werbung sichern",
            GuidOrHash = "own-match",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow,
        };
        var otherMatch = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = otherFeedId,
            Title = "Jetzt Werbung sichern",
            GuidOrHash = "other-match",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow,
        };
        await _itemRepository.AddAsync(ownMatch);
        await _itemRepository.AddAsync(otherMatch);

        var deleted = await _service.CleanupAsync();

        Assert.Equal(1, deleted);
        Assert.Null(await _itemRepository.GetByIdAsync(ownMatch.Id));
        Assert.NotNull(await _itemRepository.GetByIdAsync(otherMatch.Id));
    }

    /// <summary>
    /// Verifies that a global keyword still deletes matching items across feeds.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CleanupAsync_GlobalKeyword_StillDeletesAcrossFeeds()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var otherFeedId = await TestDataSeeder.SeedFeedAsync(_factory, "https://example.com/other");
        await _keywordRepository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "Werbung" });
        var firstMatch = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Jetzt Werbung sichern",
            GuidOrHash = "first-match",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow,
        };
        var secondMatch = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = otherFeedId,
            Title = "Mehr Werbung",
            GuidOrHash = "second-match",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow,
        };
        await _itemRepository.AddAsync(firstMatch);
        await _itemRepository.AddAsync(secondMatch);

        var deleted = await _service.CleanupAsync();

        Assert.Equal(2, deleted);
        Assert.Null(await _itemRepository.GetByIdAsync(firstMatch.Id));
        Assert.Null(await _itemRepository.GetByIdAsync(secondMatch.Id));
    }

    /// <summary>
    /// Verifies that CleanupAsync skips the keyword-candidate query entirely
    /// when no keywords are configured (neither global nor feed-scoped),
    /// because no match is possible anyway.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CleanupAsync_NoKeywords_SkipsKeywordCandidateQuery()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var candidate = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Alter Artikel",
            GuidOrHash = "candidate",
            IsRead = true,
            IsSavedForLater = false,
            PublishedAt = DateTime.UtcNow.AddDays(-60),
            ReadAt = DateTime.UtcNow,
        };
        await _itemRepository.AddAsync(candidate);
        var counting = new CountingCandidatesItemRepository(_itemRepository);
        var service = new RetentionCleanupService(
            _settingsRepository,
            counting,
            new KeywordFilter(_keywordRepository, new KeywordMatcher()),
            _contentStore);

        var deleted = await service.CleanupAsync();

        Assert.Equal(0, deleted);
        Assert.Equal(0, counting.ExpiredKeywordCandidatesCallCount);
        Assert.NotNull(await _itemRepository.GetByIdAsync(candidate.Id));
    }

    /// <summary>
    /// Verifies that CleanupAsync removes content store entries whose item no
    /// longer exists (e.g. left behind by the feed cascade or a partial
    /// failure), keeps the contents of existing items and still returns only
    /// the number of deleted items.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CleanupAsync_RemovesOrphanedContent()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var orphan = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Orphaned",
            GuidOrHash = "orphan",
            IsRead = false,
            IsSavedForLater = false,
            ContentHtml = "<p>orphan</p>",
        };
        var kept = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Kept",
            GuidOrHash = "kept",
            IsRead = false,
            IsSavedForLater = false,
            ContentHtml = "<p>kept</p>",
        };
        await _itemRepository.AddRangeAsync(new List<Item> { orphan, kept });

        // Das Item direkt in der Nutzerdatenbank loeschen, ohne den
        // Content-Speicher zu bereinigen — so entsteht eine Waise.
        await using (var context = _factory.CreateDbContext())
        {
            await context.Items.Where(i => i.Id == orphan.Id).ExecuteDeleteAsync();
        }

        var deleted = await _service.CleanupAsync();

        Assert.Equal(0, deleted);
        Assert.Null(await _contentStore.GetAsync(orphan.Id));
        Assert.Equal("<p>kept</p>", await _contentStore.GetAsync(kept.Id));
    }

    /// <summary>
    /// Verifies that CleanupAsync still removes orphaned content store entries
    /// when the retention period is zero or negative, because the sweep is
    /// independent of the retention deadline.
    /// </summary>
    /// <param name="retentionDays">The configured retention days.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CleanupAsync_ZeroOrNegativeRetentionDays_StillRemovesOrphanedContent(int retentionDays)
    {
        await SetRetentionDaysAsync(retentionDays);
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var orphan = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Orphaned",
            GuidOrHash = "orphan",
            IsRead = false,
            IsSavedForLater = false,
            ContentHtml = "<p>orphan</p>",
        };
        var kept = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Kept",
            GuidOrHash = "kept",
            IsRead = false,
            IsSavedForLater = false,
            ContentHtml = "<p>kept</p>",
        };
        await _itemRepository.AddRangeAsync(new List<Item> { orphan, kept });

        // Das Item direkt in der Nutzerdatenbank loeschen, ohne den
        // Content-Speicher zu bereinigen — so entsteht eine Waise.
        await using (var context = _factory.CreateDbContext())
        {
            await context.Items.Where(i => i.Id == orphan.Id).ExecuteDeleteAsync();
        }

        var deleted = await _service.CleanupAsync();

        Assert.Equal(0, deleted);
        Assert.Null(await _contentStore.GetAsync(orphan.Id));
        Assert.Equal("<p>kept</p>", await _contentStore.GetAsync(kept.Id));
    }

    /// <summary>
    /// Verifies that CleanupAsync removes image-only content store entries
    /// whose item no longer exists, because <c>GetItemIdsAsync</c> covers
    /// image-only rows as well.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CleanupAsync_RemovesOrphanedImages()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var orphan = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Orphaned",
            GuidOrHash = "orphan",
            IsRead = false,
            IsSavedForLater = false,
            Image = new ItemImage([1], "image/png", null),
        };
        var kept = new Item
        {
            Id = Guid.NewGuid(),
            FeedId = feedId,
            Title = "Kept",
            GuidOrHash = "kept",
            IsRead = false,
            IsSavedForLater = false,
            Image = new ItemImage([2], "image/png", null),
        };
        await _itemRepository.AddRangeAsync(new List<Item> { orphan, kept });

        // Das Item direkt in der Nutzerdatenbank loeschen, ohne den
        // Content-Speicher zu bereinigen — so entsteht eine Waise.
        await using (var context = _factory.CreateDbContext())
        {
            await context.Items.Where(i => i.Id == orphan.Id).ExecuteDeleteAsync();
        }

        var deleted = await _service.CleanupAsync();

        Assert.Equal(0, deleted);
        Assert.Null(await _contentStore.GetImageAsync(orphan.Id));
        Assert.NotNull(await _contentStore.GetImageAsync(kept.Id));
    }

    /// <summary>
    /// A <see cref="DelegatingItemRepository"/> that counts the keyword-candidate
    /// queries to observe whether the keyword deletion rule ran at all.
    /// </summary>
    private sealed class CountingCandidatesItemRepository : DelegatingItemRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CountingCandidatesItemRepository"/> class.
        /// </summary>
        /// <param name="inner">The repository to delegate to.</param>
        public CountingCandidatesItemRepository(IItemRepository inner)
            : base(inner)
        {
        }

        /// <summary>
        /// Gets the number of <c>GetExpiredKeywordCandidatesAsync</c> calls so far.
        /// </summary>
        public int ExpiredKeywordCandidatesCallCount { get; private set; }

        /// <inheritdoc />
        public override Task<IReadOnlyList<Item>> GetExpiredKeywordCandidatesAsync(
            DateTime cutoff,
            CancellationToken cancellationToken = default)
        {
            ExpiredKeywordCandidatesCallCount++;
            return base.GetExpiredKeywordCandidatesAsync(cutoff, cancellationToken);
        }
    }
}
