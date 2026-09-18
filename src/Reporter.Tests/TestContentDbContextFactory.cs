// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Data;

namespace Reporter.Tests;

/// <summary>
/// Provides an <see cref="IDbContextFactory{TContext}"/> implementation for tests backed by an in-memory SQLite database.
/// </summary>
/// <remarks>
/// Creates the <see cref="ContentDbContext"/> schema (<c>reporter-content.db</c>) via EF Core
/// migrations instead of <c>EnsureCreated</c>:
/// <c>Migrate()</c> produces the migrations history table so that <c>MigrateAsync</c> calls
/// (<see cref="ItemContentMigrationService"/>) stay idempotent like in the production environment.
/// </remarks>
public sealed class TestContentDbContextFactory : TestDbContextFactoryBase<ContentDbContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestContentDbContextFactory"/> class and creates the database schema.
    /// </summary>
    public TestContentDbContextFactory()
        : base("reporter-content-tests")
    {
    }

    /// <inheritdoc />
    protected override ContentDbContext CreateContext(DbContextOptions<ContentDbContext> options)
    {
        return new ContentDbContext(options);
    }

    /// <inheritdoc />
    protected override void InitializeSchema(ContentDbContext context)
    {
        context.Database.Migrate();
    }
}
