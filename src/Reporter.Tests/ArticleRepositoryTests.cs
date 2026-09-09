using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains unit tests for the <see cref="ArticleRepository"/> class.
/// </summary>
public class ArticleRepositoryTests
{
    /// <summary>
    /// Verifies that <see cref="ArticleRepository.GetUnreadAsync"/> returns an empty list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetUnreadAsync_ReturnsEmptyList()
    {
        var repository = new ArticleRepository();
        var result = await repository.GetUnreadAsync();
        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that <see cref="ArticleRepository.GetArticlesAsync"/> returns an empty list.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetArticlesAsync_ReturnsEmptyList()
    {
        var repository = new ArticleRepository();
        var result = await repository.GetArticlesAsync();
        Assert.Empty(result);
    }
}
