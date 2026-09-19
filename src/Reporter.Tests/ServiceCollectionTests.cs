// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
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

        AddTestRepositories(services, connection);

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IFeedRepository>());
        Assert.NotNull(provider.GetRequiredService<ICategoryRepository>());
        Assert.NotNull(provider.GetRequiredService<IItemRepository>());
        Assert.NotNull(provider.GetRequiredService<IItemContentStore>());
        Assert.NotNull(provider.GetRequiredService<IContentMigrationService>());
        Assert.NotNull(provider.GetRequiredService<IKeywordRepository>());
        Assert.NotNull(provider.GetRequiredService<ISettingsRepository>());
        Assert.NotNull(provider.GetRequiredService<ISyncLogRepository>());
        Assert.NotNull(provider.GetRequiredService<IDebugLogRepository>());

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

    /// <summary>
    /// Verifies that the feed sync service can be resolved with its full constructor
    /// dependency set, mirroring the <c>MauiProgram</c> registration.
    /// </summary>
    [Fact]
    public void AddReporterServices_ResolvesFeedSyncService()
    {
        var services = new ServiceCollection();
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        AddTestRepositories(services, connection);
        services
            .AddSingleton<HttpClient>(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            .AddSingleton<IFeedSyncService, FeedSyncService>()
            .AddSingleton<IFeedIconService, FeedIconService>()
            .AddSingleton<IItemImageService, ItemImageService>()
            .AddSingleton<IKeywordMatcher, KeywordMatcher>()
            .AddSingleton<IKeywordFilter, KeywordFilter>()
            .AddSingleton<INotificationService, NotificationService>()
            .AddSingleton<ILocalNotificationService, FakeLocalNotificationService>()
            .AddSingleton<INetworkStatusService, FakeNetworkStatusService>()
            .AddSingleton<IDebugLogService, FakeDebugLogService>();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IFeedSyncService>());
        Assert.NotNull(provider.GetRequiredService<IFeedIconService>());
        Assert.NotNull(provider.GetRequiredService<IItemImageService>());

        connection.Dispose();
    }

    /// <summary>
    /// Verifies that the debug log and debug report services can be resolved with their
    /// full constructor dependency set, mirroring the <c>MauiProgram</c> registration.
    /// </summary>
    [Fact]
    public void AddReporterServices_ResolvesDebugServices()
    {
        var services = new ServiceCollection();
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        AddTestRepositories(services, connection);
        services
            .AddSingleton<IDebugLogService, DebugLogService>()
            .AddSingleton<IEmailService, FakeEmailService>()
            .AddSingleton<IDeviceInfoProvider, FakeDeviceInfoProvider>()
            .AddSingleton<INetworkStatusService, FakeNetworkStatusService>()
            .AddSingleton<IDebugReportService, DebugReportService>();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IDebugLogService>());
        Assert.NotNull(provider.GetRequiredService<IDebugReportService>());

        connection.Dispose();
    }

    /// <summary>
    /// Verifies that the auto refresh service can be resolved with its full
    /// constructor dependency set including the background refresh gateway,
    /// mirroring the <c>MauiProgram</c> registration.
    /// </summary>
    [Fact]
    public void AddReporterServices_ResolvesAutoRefreshService()
    {
        var services = new ServiceCollection();
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        AddTestRepositories(services, connection);
        services
            .AddSingleton<IFeedSyncService, FakeFeedSyncService>()
            .AddSingleton<INetworkStatusService, FakeNetworkStatusService>()
            .AddSingleton<IBackgroundRefreshService, FakeBackgroundRefreshService>()
            .AddSingleton<IAutoRefreshService, AutoRefreshService>();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IAutoRefreshService>());

        connection.Dispose();
    }

    /// <summary>
    /// Verifies that the scheduled sync runner can be resolved with its full
    /// constructor dependency set, mirroring the <c>MauiProgram</c> registration.
    /// </summary>
    [Fact]
    public void AddReporterServices_ResolvesScheduledSyncRunner()
    {
        var services = new ServiceCollection();
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        AddTestRepositories(services, connection);
        services
            .AddSingleton<IFeedSyncService, FakeFeedSyncService>()
            .AddSingleton<IBackgroundRefreshService, FakeBackgroundRefreshService>()
            .AddSingleton<IScheduledSyncRunner, ScheduledSyncRunner>();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IScheduledSyncRunner>());

        connection.Dispose();
    }

    /// <summary>
    /// Verifies that the demo content service can be resolved with a registered
    /// <see cref="FirstRunState"/> and the repository set, mirroring the
    /// <c>MauiProgram</c> registration.
    /// </summary>
    [Fact]
    public void AddReporterServices_ResolvesDemoContentService()
    {
        var services = new ServiceCollection();
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        AddTestRepositories(services, connection);
        services
            .AddSingleton(new FirstRunState { IsFirstRun = false, DemoSeedSuppressed = false })
            .AddSingleton<IDemoContentService, DemoContentService>()
            .AddSingleton<IDebugLogService, FakeDebugLogService>();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IDemoContentService>());

        connection.Dispose();
    }

    /// <summary>
    /// Verifies that the backup exclusion service and the <see cref="DatabasePath"/>
    /// and <see cref="ContentDatabasePath"/> carriers can be resolved, mirroring the
    /// <c>MauiProgram</c> registration, and that the fake records the included and
    /// excluded paths like <c>App.OnStart</c> calls it.
    /// </summary>
    [Fact]
    public void AddReporterServices_ResolvesBackupExclusion()
    {
        var services = new ServiceCollection();
        services
            .AddSingleton(new DatabasePath(Path.Combine(Path.GetTempPath(), "reporter.db")))
            .AddSingleton(new ContentDatabasePath(Path.Combine(Path.GetTempPath(), "reporter-content.db")))
            .AddSingleton<IBackupExclusionService, FakeBackupExclusionService>();

        var provider = services.BuildServiceProvider();

        var service = Assert.IsType<FakeBackupExclusionService>(provider.GetRequiredService<IBackupExclusionService>());
        var databasePath = provider.GetRequiredService<DatabasePath>();
        var contentDatabasePath = provider.GetRequiredService<ContentDatabasePath>();

        service.IncludeInBackup(databasePath.FilePath);
        service.ExcludeFromBackup(contentDatabasePath.FilePath);
        Assert.Equal(databasePath.FilePath, Assert.Single(service.IncludedPaths));
        Assert.Equal(contentDatabasePath.FilePath, Assert.Single(service.ExcludedPaths));
    }

    private static void AddTestRepositories(ServiceCollection services, SqliteConnection connection)
    {
        services.AddDbContextFactory<ReporterDbContext>(options => options.UseSqlite(connection))
            .AddDbContextFactory<ContentDbContext>(options => options.UseSqlite("DataSource=:memory:"))
            .AddSingleton<IFeedRepository, FeedRepository>()
            .AddSingleton<ICategoryRepository, CategoryRepository>()
            .AddSingleton<IItemRepository, ItemRepository>()
            .AddSingleton<IItemContentStore, ItemContentRepository>()
            .AddSingleton<IContentMigrationService, ItemContentMigrationService>()
            .AddSingleton<IKeywordRepository, KeywordRepository>()
            .AddSingleton<ISettingsRepository, SettingsRepository>()
            .AddSingleton<ISyncLogRepository, SyncLogRepository>()
            .AddSingleton<IDebugLogRepository, DebugLogRepository>();
    }
}
