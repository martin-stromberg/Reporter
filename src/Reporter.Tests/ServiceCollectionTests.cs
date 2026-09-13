// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reporter.Core.Interfaces;
using Reporter.Core.Services;
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

    /// <summary>
    /// Verifies that the feed search service can be resolved when an <see cref="HttpClient"/>
    /// is registered, mirroring the <c>MauiProgram</c> registration.
    /// </summary>
    [Fact]
    public void AddReporterServices_ResolvesFeedSearchService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<HttpClient>(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            .AddSingleton<IFeedSearchService, FeedSearchService>();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IFeedSearchService>());
    }
}
