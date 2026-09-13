// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="FeedListItem"/> class.
/// </summary>
public class FeedListItemTests
{
    /// <summary>
    /// Regression guard for the FeedsPage offline gap (review finding): offline,
    /// a feed with a stored favicon must still show the initial-letter avatar
    /// instead of an empty tile. The visibility decision itself lives in the
    /// FeedsPage.xaml trigger cascade, which this unit-test project cannot
    /// exercise (no MAUI dependencies) — what is pinned here is the model-level
    /// invariant the fallback relies on: <see cref="FeedListItem.FeedInitial"/>
    /// yields a non-empty letter even when <see cref="FeedListItem.FaviconUrl"/>
    /// is set, so the offline fallback circle always has content to render.
    /// </summary>
    [Fact]
    public void FeedInitial_FeedWithFavicon_StillProvidesFallbackLetter()
    {
        var item = new FeedListItem
        {
            Id = Guid.NewGuid(),
            Title = "Example Feed",
            Url = "https://example.com/rss",
            UnreadCount = 0,
            NotificationsEnabled = true,
            FaviconUrl = "https://example.com/favicon.ico",
        };

        Assert.Equal("E", item.FeedInitial);
    }

    /// <summary>
    /// Verifies that both list-item models derive the same avatar initial so the
    /// fallback circle never renders empty for a titled feed.
    /// </summary>
    [Fact]
    public void FeedInitial_BothListItems_DeriveSameLetter()
    {
        var feedItem = new FeedListItem
        {
            Id = Guid.NewGuid(),
            Title = "example",
            Url = "https://example.com/rss",
            UnreadCount = 0,
            NotificationsEnabled = true,
        };
        var itemItem = new ItemListItem
        {
            Id = Guid.NewGuid(),
            FeedId = feedItem.Id,
            Title = "Article",
            IsRead = false,
            IsSavedForLater = false,
            FeedTitle = "example",
        };

        Assert.Equal("E", feedItem.FeedInitial);
        Assert.Equal(feedItem.FeedInitial, itemItem.FeedInitial);
    }

    /// <summary>
    /// Verifies that an empty feed title falls back to <c>"?"</c> so the avatar
    /// circle always shows a glyph.
    /// </summary>
    [Fact]
    public void FeedInitial_EmptyTitle_FallsBackToQuestionMark()
    {
        var item = new FeedListItem
        {
            Id = Guid.NewGuid(),
            Title = " ",
            Url = "https://example.com/rss",
            UnreadCount = 0,
            NotificationsEnabled = true,
        };

        Assert.Equal("?", item.FeedInitial);
    }
}
