// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reporter.Data;

/// <summary>
/// Design-time factory for <see cref="ContentDbContext"/> used by EF Core tooling.
/// </summary>
public class ContentDbContextFactory : IDesignTimeDbContextFactory<ContentDbContext>
{
    /// <summary>
    /// Creates a new <see cref="ContentDbContext"/> instance for design-time tooling.
    /// </summary>
    /// <param name="args">Command-line arguments passed by the EF Core tooling.</param>
    /// <returns>A new <see cref="ContentDbContext"/> instance.</returns>
    public ContentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContentDbContext>();
        optionsBuilder.UseSqlite("Data Source=reporter-content.db");
        return new ContentDbContext(optionsBuilder.Options);
    }
}
