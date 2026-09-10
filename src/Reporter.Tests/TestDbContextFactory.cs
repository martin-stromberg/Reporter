using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Reporter.Data;

namespace Reporter.Tests;

/// <summary>
/// Provides an <see cref="IDbContextFactory{TContext}"/> implementation for tests backed by an in-memory SQLite connection.
/// </summary>
public sealed class TestDbContextFactory : IDbContextFactory<ReporterDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ReporterDbContext> _options;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDbContextFactory"/> class and creates the database schema.
    /// </summary>
    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ReporterDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new ReporterDbContext(_options);
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a new <see cref="ReporterDbContext"/> instance sharing the in-memory connection.
    /// </summary>
    /// <returns>A new <see cref="ReporterDbContext"/> instance.</returns>
    public ReporterDbContext CreateDbContext()
    {
        return new ReporterDbContext(_options);
    }

    /// <summary>
    /// Disposes the underlying SQLite connection.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _connection.Dispose();
        _disposed = true;
    }
}
