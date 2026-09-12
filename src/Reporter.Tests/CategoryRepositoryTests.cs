// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Core.Models;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="CategoryRepository"/> class.
/// </summary>
public class CategoryRepositoryTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly CategoryRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryRepositoryTests"/> class.
    /// </summary>
    public CategoryRepositoryTests()
    {
        _factory = new TestDbContextFactory();
        _repository = new CategoryRepository(_factory);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that a category can be added and retrieved by id.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsCategory()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "News" };
        await _repository.AddAsync(category);

        var result = await _repository.GetByIdAsync(category.Id);

        Assert.NotNull(result);
        Assert.Equal("News", result.Name);
    }

    /// <summary>
    /// Verifies that GetAllAsync returns all added categories ordered by name.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAllAsync_ReturnsCategoriesOrderedByName()
    {
        await _repository.AddAsync(new Category { Id = Guid.NewGuid(), Name = "B" });
        await _repository.AddAsync(new Category { Id = Guid.NewGuid(), Name = "A" });

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Name);
        Assert.Equal("B", result[1].Name);
    }

    /// <summary>
    /// Verifies that UpdateAsync persists changes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Old" };
        await _repository.AddAsync(category);

        await _repository.UpdateAsync(new Category { Id = category.Id, Name = "New" });
        var result = await _repository.GetByIdAsync(category.Id);

        Assert.NotNull(result);
        Assert.Equal("New", result.Name);
    }

    /// <summary>
    /// Verifies that DeleteAsync removes the category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_RemovesCategory()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "ToDelete" };
        await _repository.AddAsync(category);

        await _repository.DeleteAsync(category.Id);
        var result = await _repository.GetByIdAsync(category.Id);

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
    /// Verifies that DeleteAsync for a non-existing id does not throw.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_NonExisting_DoesNotThrow()
    {
        await _repository.DeleteAsync(Guid.NewGuid());
    }

    /// <summary>
    /// Verifies that GetAllWithFeedCountAsync returns the correct number of feeds per category.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAllWithFeedCountAsync_ReturnsCountOfFeedsPerCategory()
    {
        var categoryA = new Category { Id = Guid.NewGuid(), Name = "A" };
        var categoryB = new Category { Id = Guid.NewGuid(), Name = "B" };
        await _repository.AddAsync(categoryA);
        await _repository.AddAsync(categoryB);

        using var context = _factory.CreateDbContext();
        context.Feeds.AddRange(
            new Data.Entities.Feed { Id = Guid.NewGuid(), Url = "https://a.example/feed", Title = "A Feed", CategoryId = categoryA.Id },
            new Data.Entities.Feed { Id = Guid.NewGuid(), Url = "https://b1.example/feed", Title = "B Feed 1", CategoryId = categoryB.Id },
            new Data.Entities.Feed { Id = Guid.NewGuid(), Url = "https://b2.example/feed", Title = "B Feed 2", CategoryId = categoryB.Id });
        await context.SaveChangesAsync();

        var result = await _repository.GetAllWithFeedCountAsync();

        Assert.Equal(2, result.Count);
        var a = result.Single(c => c.Name == "A");
        var b = result.Single(c => c.Name == "B");
        Assert.Equal(1, a.FeedCount);
        Assert.Equal(2, b.FeedCount);
    }

    /// <summary>
    /// Verifies that updating a category to a duplicate name throws a database exception.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsDbUpdateException()
    {
        var first = new Category { Id = Guid.NewGuid(), Name = "First" };
        var second = new Category { Id = Guid.NewGuid(), Name = "Second" };
        await _repository.AddAsync(first);
        await _repository.AddAsync(second);

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => _repository.UpdateAsync(new Category { Id = second.Id, Name = "First" }));
    }

    /// <summary>
    /// Verifies that deleting a category with assigned feeds sets the feed's category to null.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_WithAssignedFeeds_SetsCategoryIdToNull()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "ToDelete" };
        await _repository.AddAsync(category);

        using (var context = _factory.CreateDbContext())
        {
            context.Feeds.Add(new Data.Entities.Feed { Id = Guid.NewGuid(), Url = "https://example.com/feed", Title = "Feed", CategoryId = category.Id });
            await context.SaveChangesAsync();
        }

        await _repository.DeleteAsync(category.Id);

        using var after = _factory.CreateDbContext();
        var feed = await after.Feeds.SingleAsync();
        Assert.Null(feed.CategoryId);
    }
}
