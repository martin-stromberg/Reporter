// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Data;

namespace Reporter.Tests;

/// <summary>
/// Provides an <see cref="IDbContextFactory{TContext}"/> implementation for tests backed by an in-memory SQLite database.
/// </summary>
/// <remarks>
/// Creates the <see cref="ReporterDbContext"/> schema (<c>reporter.db</c>) via
/// <c>EnsureCreated</c>.
/// </remarks>
public sealed class TestDbContextFactory : TestDbContextFactoryBase<ReporterDbContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestDbContextFactory"/> class and creates the database schema.
    /// </summary>
    public TestDbContextFactory()
        : base("reporter-tests")
    {
    }

    /// <inheritdoc />
    protected override ReporterDbContext CreateContext(DbContextOptions<ReporterDbContext> options)
    {
        return new ReporterDbContext(options);
    }

    /// <inheritdoc />
    protected override void InitializeSchema(ReporterDbContext context)
    {
        context.Database.EnsureCreated();
    }
}
