// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;
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

    /// <summary>
    /// Seeds a feed through the repository with the specified URL and notification
    /// flag and returns its identifier.
    /// </summary>
    /// <param name="feedRepository">The feed repository.</param>
    /// <param name="url">The feed URL.</param>
    /// <param name="notificationsEnabled">A value indicating whether notifications are enabled for the feed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the feed identifier.</returns>
    public static async Task<Guid> SeedFeedAsync(IFeedRepository feedRepository, string url = "https://example.com/rss", bool notificationsEnabled = true)
    {
        var feedId = Guid.NewGuid();
        await feedRepository.AddAsync(new Feed
        {
            Id = feedId,
            Url = url,
            Title = "Test Feed",
            NotificationsEnabled = notificationsEnabled,
        });
        return feedId;
    }
}
