// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reporter.Core.Interfaces;
using Reporter.Data;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="DatabaseMigrationService"/> class.
/// </summary>
public class DatabaseMigrationServiceTests : IDisposable
{
    private readonly RecordingReporterDbContextFactory _factory;
    private readonly List<string> _steps = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseMigrationServiceTests"/> class.
    /// </summary>
    public DatabaseMigrationServiceTests()
    {
        _factory = new RecordingReporterDbContextFactory(() => _steps.Add("reporter-schema"));
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that the legacy content copy runs before the reporter.db schema
    /// migration — dropping <c>items.content_html</c> ahead of the copy would
    /// lose the stored contents — and that the schema is actually migrated.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MigrateAsync_MigratesLegacyContentBeforeReporterSchema()
    {
        var service = new DatabaseMigrationService(
            new RecordingContentMigrationService(() => _steps.Add("legacy-content")),
            _factory);

        await service.MigrateAsync();

        Assert.Equal(["legacy-content", "reporter-schema"], _steps);

        await using var context = _factory.CreateDbContext();
        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());
    }

    /// <summary>
    /// Verifies that a failing legacy content copy propagates and blocks the
    /// reporter.db schema migration: continuing would drop
    /// <c>items.content_html</c> before its values were preserved.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task MigrateAsync_LegacyCopyFailure_DoesNotMigrateSchema()
    {
        var service = new DatabaseMigrationService(
            new RecordingContentMigrationService(() =>
            {
                // Simuliert den Fehlerfall der Legacy-Kopie.
                throw new InvalidOperationException("copy failed");
            }),
            _factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateAsync());

        await using var context = _factory.CreateDbContext();
        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
    }

    /// <summary>
    /// Records the invocation of <see cref="IContentMigrationService.MigrateLegacyContentAsync"/>.
    /// </summary>
    private sealed class RecordingContentMigrationService : IContentMigrationService
    {
        private readonly Action _onMigrate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingContentMigrationService"/> class.
        /// </summary>
        /// <param name="onMigrate">The callback invoked on every migration call.</param>
        public RecordingContentMigrationService(Action onMigrate)
        {
            _onMigrate = onMigrate;
        }

        /// <inheritdoc />
        public Task MigrateLegacyContentAsync(CancellationToken cancellationToken = default)
        {
            _onMigrate();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Provides a <see cref="ReporterDbContext"/> over a fresh, unmigrated
    /// shared in-memory database and records each context creation — the
    /// service creates the context immediately before the schema migration.
    /// </summary>
    private sealed class RecordingReporterDbContextFactory : IDbContextFactory<ReporterDbContext>, IDisposable
    {
        private readonly SqliteConnection _keepAliveConnection;
        private readonly DbContextOptions<ReporterDbContext> _options;
        private readonly Action _onCreate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingReporterDbContextFactory"/> class.
        /// </summary>
        /// <param name="onCreate">The callback invoked on every context creation.</param>
        public RecordingReporterDbContextFactory(Action onCreate)
        {
            _onCreate = onCreate;
            var connectionString = $"Data Source=reporter-migration-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Default Timeout=30";
            _keepAliveConnection = new SqliteConnection(connectionString);
            _keepAliveConnection.Open();
            _options = new DbContextOptionsBuilder<ReporterDbContext>()
                .UseSqlite(connectionString)
                .Options;
        }

        /// <inheritdoc />
        public ReporterDbContext CreateDbContext()
        {
            _onCreate();
            return new ReporterDbContext(_options);
        }

        /// <inheritdoc />
        public Task<ReporterDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            _onCreate();
            return Task.FromResult(new ReporterDbContext(_options));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _keepAliveConnection.Dispose();
        }
    }
}
