// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Data;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="ContentDbContextFactory"/> class.
/// </summary>
public class ContentDbContextFactoryTests
{
    /// <summary>
    /// Verifies that the design-time factory creates a <see cref="ContentDbContext"/> with SQLite configured.
    /// </summary>
    [Fact]
    public void CreateDbContext_ReturnsContextWithSqliteProvider()
    {
        var factory = new ContentDbContextFactory();
        using var context = factory.CreateDbContext([]);

        Assert.IsType<ContentDbContext>(context);
        Assert.Contains("Sqlite", context.Database.ProviderName);
    }
}
