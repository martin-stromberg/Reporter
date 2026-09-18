// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Reporter.Tests;

/// <summary>
/// Base class for <see cref="IDbContextFactory{TContext}"/> implementations used
/// by tests, backed by a shared in-memory SQLite database.
/// </summary>
/// <typeparam name="TContext">The <see cref="DbContext"/> type produced by the factory.</typeparam>
/// <remarks>
/// The database uses the shared in-memory cache (<c>Mode=Memory;Cache=Shared</c>): a keep-alive
/// connection holds the database open while every <see cref="DbContext"/> works on its own
/// physical connection. This avoids the flaky "SQLite Error 5: unable to delete/modify
/// user-function due to active statements" failure that occurred when several contexts shared
/// a single <see cref="SqliteConnection"/> and function registration collided with active readers.
/// <c>DefaultTimeout</c> makes concurrent access retry briefly instead of failing with SQLITE_BUSY.
/// </remarks>
public abstract class TestDbContextFactoryBase<TContext> : IDbContextFactory<TContext>, IDisposable
    where TContext : DbContext
{
    private readonly SqliteConnection _keepAliveConnection;
    private readonly DbContextOptions<TContext> _options;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDbContextFactoryBase{TContext}"/> class and creates the database schema.
    /// </summary>
    /// <param name="databaseName">The name prefix of the shared in-memory database.</param>
    protected TestDbContextFactoryBase(string databaseName)
    {
        var connectionString = $"Data Source={databaseName}-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Default Timeout=30";
        _keepAliveConnection = new SqliteConnection(connectionString);
        _keepAliveConnection.Open();

        _options = new DbContextOptionsBuilder<TContext>()
            .UseSqlite(connectionString)
            .Options;

        using var context = CreateContext(_options);
        InitializeSchema(context);
    }

    /// <summary>
    /// Creates a new <typeparamref name="TContext"/> instance with its own connection to the shared in-memory database.
    /// </summary>
    /// <returns>A new <typeparamref name="TContext"/> instance.</returns>
    public TContext CreateDbContext()
    {
        return CreateContext(_options);
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

    /// <summary>
    /// Creates a new <typeparamref name="TContext"/> instance for the specified options.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    /// <returns>A new <typeparamref name="TContext"/> instance.</returns>
    protected abstract TContext CreateContext(DbContextOptions<TContext> options);

    /// <summary>
    /// Creates the database schema for the context (<c>EnsureCreated</c> or <c>Migrate</c>).
    /// </summary>
    /// <param name="context">The context whose schema is initialized.</param>
    protected abstract void InitializeSchema(TContext context);
}
