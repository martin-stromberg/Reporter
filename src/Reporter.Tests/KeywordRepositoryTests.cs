// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Core.Models;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="KeywordRepository"/> class.
/// </summary>
public class KeywordRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly KeywordRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeywordRepositoryTests"/> class.
    /// </summary>
    public KeywordRepositoryTests()
    {
        _factory = new TestDbContextFactory();
        _repository = new KeywordRepository(_factory);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that a keyword can be added and retrieved by id.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsKeyword()
    {
        var keyword = new Keyword { Id = Guid.NewGuid(), KeywordText = "test" };
        await _repository.AddAsync(keyword);

        var result = await _repository.GetByIdAsync(keyword.Id);

        Assert.NotNull(result);
        Assert.Equal("test", result.KeywordText);
    }

    /// <summary>
    /// Verifies that GetAllAsync returns all added keywords ordered by text.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAllAsync_ReturnsKeywordsOrderedByText()
    {
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "b" });
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "a" });

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0].KeywordText);
        Assert.Equal("b", result[1].KeywordText);
    }

    /// <summary>
    /// Verifies that UpdateAsync persists changes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var keyword = new Keyword { Id = Guid.NewGuid(), KeywordText = "old" };
        await _repository.AddAsync(keyword);

        await _repository.UpdateAsync(new Keyword { Id = keyword.Id, KeywordText = "new" });
        var result = await _repository.GetByIdAsync(keyword.Id);

        Assert.NotNull(result);
        Assert.Equal("new", result.KeywordText);
    }

    /// <summary>
    /// Verifies that DeleteAsync removes the keyword.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_RemovesKeyword()
    {
        var keyword = new Keyword { Id = Guid.NewGuid(), KeywordText = "delete" };
        await _repository.AddAsync(keyword);

        await _repository.DeleteAsync(keyword.Id);
        var result = await _repository.GetByIdAsync(keyword.Id);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetByIdAsync returns null for a non-existing id.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetByFeedAsync with a null feed id returns only the global keywords.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetByFeedAsync_Null_ReturnsOnlyGlobalKeywords()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "global" });
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "feed-only", FeedId = feedId });

        var result = await _repository.GetByFeedAsync(null);

        var keyword = Assert.Single(result);
        Assert.Equal("global", keyword.KeywordText);
        Assert.Null(keyword.FeedId);
    }

    /// <summary>
    /// Verifies that GetByFeedAsync returns only the keywords of the specified feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetByFeedAsync_FeedId_ReturnsFeedKeywords()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var otherFeedId = await TestDataSeeder.SeedFeedAsync(_factory, "https://example.com/other");
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "feed-only", FeedId = feedId });
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "global" });
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "other-feed", FeedId = otherFeedId });

        var result = await _repository.GetByFeedAsync(feedId);

        var keyword = Assert.Single(result);
        Assert.Equal("feed-only", keyword.KeywordText);
        Assert.Equal(feedId, keyword.FeedId);
    }

    /// <summary>
    /// Verifies that GetEffectiveForFeedAsync returns the union of global and
    /// feed keywords without keywords of other feeds.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetEffectiveForFeedAsync_ReturnsGlobalAndFeedUnion()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var otherFeedId = await TestDataSeeder.SeedFeedAsync(_factory, "https://example.com/other");
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "feed-only", FeedId = feedId });
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "global" });
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "other-feed", FeedId = otherFeedId });

        var result = await _repository.GetEffectiveForFeedAsync(feedId);

        Assert.Equal(2, result.Count);
        Assert.Equal("feed-only", result[0].KeywordText);
        Assert.Equal("global", result[1].KeywordText);
    }

    /// <summary>
    /// Verifies that the same keyword text may exist in two different feeds.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddAsync_SameTextInDifferentFeeds_Succeeds()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        var otherFeedId = await TestDataSeeder.SeedFeedAsync(_factory, "https://example.com/other");
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "sport", FeedId = feedId });

        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "sport", FeedId = otherFeedId });

        Assert.Equal(2, (await _repository.GetAllAsync()).Count);
    }

    /// <summary>
    /// Verifies that the composite unique index rejects a duplicate keyword text
    /// within the same feed.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddAsync_DuplicateInSameFeed_Throws()
    {
        var feedId = await TestDataSeeder.SeedFeedAsync(_factory);
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "sport", FeedId = feedId });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "sport", FeedId = feedId }));
    }

    /// <summary>
    /// Verifies that the filtered unique index keeps the global uniqueness of
    /// keyword texts.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddAsync_DuplicateGlobal_Throws()
    {
        await _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "sport" });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => _repository.AddAsync(new Keyword { Id = Guid.NewGuid(), KeywordText = "sport" }));
    }
}
