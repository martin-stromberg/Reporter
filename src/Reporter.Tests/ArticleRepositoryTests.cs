using Reporter.Data.Repositories;

namespace Reporter.Tests;

public class ArticleRepositoryTests
{
    [Fact]
    public async Task GetUnreadAsync_ReturnsEmptyList()
    {
        var repository = new ArticleRepository();
        var result = await repository.GetUnreadAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetArticlesAsync_ReturnsEmptyList()
    {
        var repository = new ArticleRepository();
        var result = await repository.GetArticlesAsync();
        Assert.Empty(result);
    }
}
