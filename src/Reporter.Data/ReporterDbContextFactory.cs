// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reporter.Data;

/// <summary>
/// Design-time factory for <see cref="ReporterDbContext"/> used by EF Core tooling.
/// </summary>
public class ReporterDbContextFactory : IDesignTimeDbContextFactory<ReporterDbContext>
{
    /// <summary>
    /// Creates a new <see cref="ReporterDbContext"/> instance for design-time tooling.
    /// </summary>
    /// <param name="args">Command-line arguments passed by the EF Core tooling.</param>
    /// <returns>A new <see cref="ReporterDbContext"/> instance.</returns>
    public ReporterDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReporterDbContext>();
        optionsBuilder.UseSqlite("Data Source=reporter.db");
        return new ReporterDbContext(optionsBuilder.Options);
    }
}
