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
}
