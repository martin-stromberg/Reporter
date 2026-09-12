// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reporter.Data;

namespace Reporter.Tests;

/// <summary>
/// Provides an <see cref="IDbContextFactory{TContext}"/> implementation for tests backed by an in-memory SQLite database.
/// </summary>
/// <remarks>
/// The database uses the shared in-memory cache (<c>Mode=Memory;Cache=Shared</c>): a keep-alive
/// connection holds the database open while every <see cref="DbContext"/> works on its own
/// physical connection. This avoids the flaky "SQLite Error 5: unable to delete/modify
/// user-function due to active statements" failure that occurred when several contexts shared
/// a single <see cref="SqliteConnection"/> and function registration collided with active readers.
/// <c>DefaultTimeout</c> makes concurrent access retry briefly instead of failing with SQLITE_BUSY.
/// </remarks>
public sealed class TestDbContextFactory : IDbContextFactory<ReporterDbContext>, IDisposable
{
    private readonly SqliteConnection _keepAliveConnection;
    private readonly DbContextOptions<ReporterDbContext> _options;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDbContextFactory"/> class and creates the database schema.
    /// </summary>
    public TestDbContextFactory()
    {
        var connectionString = $"Data Source=reporter-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Default Timeout=30";
        _keepAliveConnection = new SqliteConnection(connectionString);
        _keepAliveConnection.Open();

        _options = new DbContextOptionsBuilder<ReporterDbContext>()
            .UseSqlite(connectionString)
            .Options;

        using var context = new ReporterDbContext(_options);
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a new <see cref="ReporterDbContext"/> instance with its own connection to the shared in-memory database.
    /// </summary>
    /// <returns>A new <see cref="ReporterDbContext"/> instance.</returns>
    public ReporterDbContext CreateDbContext()
    {
        return new ReporterDbContext(_options);
    }

    /// <summary>
    /// Disposes the keep-alive connection and drops the in-memory database.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _keepAliveConnection.Dispose();
        _disposed = true;
    }
}
