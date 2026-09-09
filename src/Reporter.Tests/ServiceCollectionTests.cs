using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reporter.Core.Interfaces;
using Reporter.Data;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests verifying that repository services can be resolved from a configured service collection.
/// </summary>
public class ServiceCollectionTests
{
    /// <summary>
    /// Verifies that all repository interfaces can be resolved after registration.
    /// </summary>
    [Fact]
    public void AddReporterRepositories_ResolvesAllRepositories()
    {
        var services = new ServiceCollection();
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddDbContextFactory<ReporterDbContext>(options => options.UseSqlite(connection))
            .AddSingleton<IFeedRepository, FeedRepository>()
            .AddSingleton<ICategoryRepository, CategoryRepository>()
            .AddSingleton<IItemRepository, ItemRepository>()
            .AddSingleton<IKeywordRepository, KeywordRepository>()
            .AddSingleton<ISettingsRepository, SettingsRepository>()
            .AddSingleton<ISyncLogRepository, SyncLogRepository>();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IFeedRepository>());
        Assert.NotNull(provider.GetRequiredService<ICategoryRepository>());
        Assert.NotNull(provider.GetRequiredService<IItemRepository>());
        Assert.NotNull(provider.GetRequiredService<IKeywordRepository>());
        Assert.NotNull(provider.GetRequiredService<ISettingsRepository>());
        Assert.NotNull(provider.GetRequiredService<ISyncLogRepository>());

        connection.Dispose();
    }
}
