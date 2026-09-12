// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Reporter.Data;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="ReporterDbContextFactory"/> class.
/// </summary>
public class ReporterDbContextFactoryTests
{
    /// <summary>
    /// Verifies that the design-time factory creates a <see cref="ReporterDbContext"/> with SQLite configured.
    /// </summary>
    [Fact]
    public void CreateDbContext_ReturnsContextWithSqliteProvider()
    {
        var factory = new ReporterDbContextFactory();
        using var context = factory.CreateDbContext([]);

        Assert.IsType<ReporterDbContext>(context);
        Assert.IsAssignableFrom<DbContext>(context);
    }
}
