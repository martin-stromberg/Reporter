// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// Tests for <see cref="ItemListItem"/>.
/// </summary>
public sealed class ItemListItemTests
{
    /// <summary>
    /// Verifies that <see cref="ItemListItem.CopyWith"/> carries the local image
    /// loader and the remote image URL over to the copy so the article card
    /// keeps showing the thumbnail after a state toggle.
    /// </summary>
    [Fact]
    public void CopyWith_PreservesLocalImageLoader()
    {
        Func<CancellationToken, Task<Stream>> loader = _ => Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));
        var item = new ItemListItem
        {
            Id = Guid.NewGuid(),
            FeedId = Guid.NewGuid(),
            Title = "Article",
            IsRead = false,
            IsSavedForLater = false,
            ImageUrl = "https://example.com/img.png",
            LocalImageLoader = loader,
        };

        var copy = item.CopyWith(isRead: true);

        Assert.Same(loader, copy.LocalImageLoader);
        Assert.True(copy.HasLocalImage);
        Assert.Equal("https://example.com/img.png", copy.ImageUrl);
        Assert.True(copy.IsRead);
    }

    /// <summary>
    /// Verifies that <see cref="ItemListItem.HasLocalImage"/> only reports true
    /// when a local image loader is present.
    /// </summary>
    [Fact]
    public void HasLocalImage_ReflectsLocalImageLoader()
    {
        var without = new ItemListItem
        {
            Id = Guid.NewGuid(),
            FeedId = Guid.NewGuid(),
            Title = "Article",
            IsRead = false,
            IsSavedForLater = false,
        };
        var empty = without.CopyWith();
        var withImage = new ItemListItem
        {
            Id = Guid.NewGuid(),
            FeedId = Guid.NewGuid(),
            Title = "Article",
            IsRead = false,
            IsSavedForLater = false,
            LocalImageLoader = _ => Task.FromResult<Stream>(new MemoryStream([7])),
        };

        Assert.False(without.HasLocalImage);
        Assert.False(empty.HasLocalImage);
        Assert.True(withImage.HasLocalImage);
    }
}
