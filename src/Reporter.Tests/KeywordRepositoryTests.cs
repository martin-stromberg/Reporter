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
}
