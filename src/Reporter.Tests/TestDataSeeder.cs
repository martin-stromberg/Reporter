// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Entities = Reporter.Data.Entities;

namespace Reporter.Tests;

/// <summary>
/// Provides shared seeding helpers for tests backed by a <see cref="TestDbContextFactory"/>.
/// </summary>
public static class TestDataSeeder
{
    /// <summary>
    /// Seeds a feed with default values and returns its identifier.
    /// </summary>
    /// <param name="factory">The test context factory.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the feed identifier.</returns>
    public static async Task<Guid> SeedFeedAsync(TestDbContextFactory factory)
    {
        var feedId = Guid.NewGuid();
        await using var context = factory.CreateDbContext();
        context.Feeds.Add(new Entities.Feed
        {
            Id = feedId,
            Url = "https://example.com/feed",
            Title = "Example Feed",
        });
        await context.SaveChangesAsync();
        return feedId;
    }
}
