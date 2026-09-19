// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="ItemContentRepository"/> class backed by
/// an in-memory SQLite content database.
/// </summary>
public class ItemContentRepositoryTests : IDisposable
{
    private readonly TestContentDbContextFactory _contentFactory;
    private readonly ItemContentRepository _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemContentRepositoryTests"/> class.
    /// </summary>
    public ItemContentRepositoryTests()
    {
        _contentFactory = new TestContentDbContextFactory();
        _store = new ItemContentRepository(_contentFactory);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _contentFactory.Dispose();
    }

    /// <summary>
    /// Verifies that SetAsync stores a content that GetAsync returns.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsContent()
    {
        var itemId = Guid.NewGuid();

        await _store.SetAsync(itemId, "<p>body</p>");

        Assert.Equal("<p>body</p>", await _store.GetAsync(itemId));
    }

    /// <summary>
    /// Verifies that GetAsync returns null for an item without stored content.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAsync_UnknownItem_ReturnsNull()
    {
        Assert.Null(await _store.GetAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Verifies that SetAsync overwrites an existing content (upsert semantics).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetAsync_ExistingItem_OverwritesContent()
    {
        var itemId = Guid.NewGuid();
        await _store.SetAsync(itemId, "<p>old</p>");

        await _store.SetAsync(itemId, "<p>new</p>");

        Assert.Equal("<p>new</p>", await _store.GetAsync(itemId));
    }

    /// <summary>
    /// Verifies that SetAsync with a null content removes a stored entry.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetAsync_NullContent_RemovesEntry()
    {
        var itemId = Guid.NewGuid();
        await _store.SetAsync(itemId, "<p>body</p>");

        await _store.SetAsync(itemId, null);

        Assert.Null(await _store.GetAsync(itemId));
    }

    /// <summary>
    /// Verifies that GetRangeAsync returns only the identifiers with a stored
    /// content.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetRangeAsync_ReturnsOnlyStoredContents()
    {
        var withContent = Guid.NewGuid();
        var withoutContent = Guid.NewGuid();
        await _store.SetAsync(withContent, "<p>body</p>");

        var result = await _store.GetRangeAsync(new List<Guid> { withContent, withoutContent });

        Assert.Single(result);
        Assert.Equal("<p>body</p>", result[withContent]);
    }

    /// <summary>
    /// Verifies that SetRangeAsync stores and updates multiple contents in one
    /// batch and removes entries with a null content.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetRangeAsync_UpsertsAndRemovesEntries()
    {
        var added = Guid.NewGuid();
        var updated = Guid.NewGuid();
        var removed = Guid.NewGuid();
        await _store.SetRangeAsync(new List<ItemContentEntry>
        {
            new(updated, "<p>old</p>"),
            new(removed, "<p>gone</p>"),
        });

        await _store.SetRangeAsync(new List<ItemContentEntry>
        {
            new(added, "<p>added</p>"),
            new(updated, "<p>updated</p>"),
            new(removed, null),
        });

        Assert.Equal("<p>added</p>", await _store.GetAsync(added));
        Assert.Equal("<p>updated</p>", await _store.GetAsync(updated));
        Assert.Null(await _store.GetAsync(removed));
    }

    /// <summary>
    /// Verifies that SetRangeAsync accepts entries with a duplicated item
    /// identifier and applies the last occurrence instead of failing on a
    /// duplicate primary key.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetRangeAsync_DuplicateItemId_LastEntryWins()
    {
        var itemId = Guid.NewGuid();

        await _store.SetRangeAsync(new List<ItemContentEntry>
        {
            new(itemId, "<p>first</p>"),
            new(itemId, "<p>last</p>"),
        });

        Assert.Equal("<p>last</p>", await _store.GetAsync(itemId));
    }

    /// <summary>
    /// Verifies that SetRangeAsync applies a removal entry when it is the last
    /// occurrence of a duplicated item identifier.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetRangeAsync_DuplicateItemIdWithNull_RemovesEntry()
    {
        var itemId = Guid.NewGuid();
        await _store.SetAsync(itemId, "<p>stored</p>");

        await _store.SetRangeAsync(new List<ItemContentEntry>
        {
            new(itemId, "<p>readded</p>"),
            new(itemId, null),
        });

        Assert.Null(await _store.GetAsync(itemId));
    }

    /// <summary>
    /// Verifies that DeleteAsync removes a stored content and is a no-op for
    /// unknown identifiers.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_RemovesContent()
    {
        var itemId = Guid.NewGuid();
        await _store.SetAsync(itemId, "<p>body</p>");

        await _store.DeleteAsync(itemId);
        await _store.DeleteAsync(itemId);

        Assert.Null(await _store.GetAsync(itemId));
    }

    /// <summary>
    /// Verifies that DeleteRangeAsync removes exactly the supplied identifiers.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteRangeAsync_RemovesOnlyGivenIds()
    {
        var first = Guid.NewGuid();
        var kept = Guid.NewGuid();
        await _store.SetRangeAsync(new List<ItemContentEntry>
        {
            new(first, "<p>first</p>"),
            new(kept, "<p>kept</p>"),
        });

        await _store.DeleteRangeAsync(new List<Guid> { first });

        Assert.Null(await _store.GetAsync(first));
        Assert.Equal("<p>kept</p>", await _store.GetAsync(kept));
    }

    /// <summary>
    /// Verifies that GetItemIdsAsync returns all identifiers with a stored
    /// content.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetItemIdsAsync_ReturnsAllStoredIds()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        await _store.SetRangeAsync(new List<ItemContentEntry>
        {
            new(first, "<p>first</p>"),
            new(second, "<p>second</p>"),
        });

        var ids = await _store.GetItemIdsAsync();

        Assert.Equal(2, ids.Count);
        Assert.Contains(first, ids);
        Assert.Contains(second, ids);
    }

    /// <summary>
    /// Verifies that SetAsync stores an image that GetImageAsync returns with
    /// data, content type and origin URL.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetAsync_WithImage_PersistsImage()
    {
        var itemId = Guid.NewGuid();
        var image = new ItemImage([1, 2, 3], "image/png", "https://example.com/img.png");

        await _store.SetAsync(itemId, "<p>body</p>", image);

        var stored = await _store.GetImageAsync(itemId);
        Assert.NotNull(stored);
        Assert.Equal(image.Data, stored.Data);
        Assert.Equal("image/png", stored.ContentType);
        Assert.Equal("https://example.com/img.png", stored.Url);
        Assert.Equal("<p>body</p>", await _store.GetAsync(itemId));
    }

    /// <summary>
    /// Verifies that GetImageAsync returns null for an item without a stored image.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetImageAsync_UnknownItem_ReturnsNull()
    {
        Assert.Null(await _store.GetImageAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// Verifies that GetImageIdsAsync returns exactly the identifiers with a
    /// stored image, including image-only rows, and filters out identifiers
    /// that were not part of the lookup.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetImageIdsAsync_ReturnsStoredImageIds()
    {
        var withImage = Guid.NewGuid();
        var imageOnly = Guid.NewGuid();
        var withoutImage = Guid.NewGuid();
        var notQueried = Guid.NewGuid();
        await _store.SetAsync(withImage, "<p>body</p>", new ItemImage([1], "image/png", null));
        await _store.SetAsync(imageOnly, null, new ItemImage([2], "image/png", null));
        await _store.SetAsync(withoutImage, "<p>body</p>");
        await _store.SetAsync(notQueried, null, new ItemImage([3], "image/png", null));

        var ids = await _store.GetImageIdsAsync(new List<Guid> { withImage, imageOnly, withoutImage });

        Assert.Equal(2, ids.Count);
        Assert.Contains(withImage, ids);
        Assert.Contains(imageOnly, ids);
    }

    /// <summary>
    /// Verifies that SetAsync with a null content and an image creates an
    /// image-only row that keeps GetItemIdsAsync coverage.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetAsync_NullContentWithImage_KeepsRow()
    {
        var itemId = Guid.NewGuid();

        await _store.SetAsync(itemId, null, new ItemImage([1], "image/png", null));

        Assert.Null(await _store.GetAsync(itemId));
        Assert.NotNull(await _store.GetImageAsync(itemId));
        Assert.Contains(itemId, await _store.GetItemIdsAsync());
    }

    /// <summary>
    /// Verifies that an image-only SetRangeAsync entry writes the image columns.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetRangeAsync_ImageOnlyEntry_UpsertsImageColumns()
    {
        var itemId = Guid.NewGuid();
        var image = new ItemImage([7, 8], "image/webp", "https://example.com/i.webp");

        await _store.SetRangeAsync(new List<ItemContentEntry>
        {
            new(itemId, null, image),
        });

        var stored = await _store.GetImageAsync(itemId);
        Assert.NotNull(stored);
        Assert.Equal(image.Data, stored.Data);
        Assert.Equal("image/webp", stored.ContentType);
        Assert.Equal("https://example.com/i.webp", stored.Url);
    }

    /// <summary>
    /// Verifies that a content-only SetRangeAsync entry leaves a stored image
    /// untouched.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetRangeAsync_ContentOnlyEntry_KeepsImage()
    {
        var itemId = Guid.NewGuid();
        var image = new ItemImage([9], "image/png", null);
        await _store.SetAsync(itemId, "<p>old</p>", image);

        await _store.SetRangeAsync(new List<ItemContentEntry>
        {
            new(itemId, "<p>new</p>"),
        });

        Assert.Equal("<p>new</p>", await _store.GetAsync(itemId));
        Assert.Equal(image.Data, (await _store.GetImageAsync(itemId))?.Data);
    }

    /// <summary>
    /// Verifies that SetAsync with a null content and an image keeps the stored
    /// content — the semantics that make the image backfill non-destructive.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetAsync_NullContentWithImage_KeepsStoredContent()
    {
        var itemId = Guid.NewGuid();
        await _store.SetAsync(itemId, "<p>body</p>");

        await _store.SetAsync(itemId, null, new ItemImage([1], "image/png", null));

        Assert.Equal("<p>body</p>", await _store.GetAsync(itemId));
        Assert.NotNull(await _store.GetImageAsync(itemId));
    }

    /// <summary>
    /// Verifies that an image-only SetRangeAsync entry keeps the stored content.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetRangeAsync_ImageOnlyEntry_KeepsStoredContent()
    {
        var itemId = Guid.NewGuid();
        await _store.SetAsync(itemId, "<p>body</p>");

        await _store.SetRangeAsync(new List<ItemContentEntry>
        {
            new(itemId, null, new ItemImage([2], "image/png", null)),
        });

        Assert.Equal("<p>body</p>", await _store.GetAsync(itemId));
        Assert.NotNull(await _store.GetImageAsync(itemId));
    }

    /// <summary>
    /// Verifies that SetAsync with a null content removes the stored content
    /// but keeps a stored image, leaving an image-only row behind.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SetAsync_NullContent_KeepsStoredImage()
    {
        var itemId = Guid.NewGuid();
        var image = new ItemImage([3], "image/png", null);
        await _store.SetAsync(itemId, "<p>body</p>", image);

        await _store.SetAsync(itemId, null);

        Assert.Null(await _store.GetAsync(itemId));
        Assert.Equal(image.Data, (await _store.GetImageAsync(itemId))?.Data);
        Assert.Contains(itemId, await _store.GetItemIdsAsync());
    }

    /// <summary>
    /// Verifies that DeleteAsync removes the stored image together with the row.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_RemovesImage()
    {
        var itemId = Guid.NewGuid();
        await _store.SetAsync(itemId, "<p>body</p>", new ItemImage([1], "image/png", null));

        await _store.DeleteAsync(itemId);

        Assert.Null(await _store.GetImageAsync(itemId));
        Assert.Empty(await _store.GetImageIdsAsync(new List<Guid> { itemId }));
    }

    /// <summary>
    /// Verifies that DeleteRangeAsync removes the stored images of the
    /// supplied identifiers.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteRangeAsync_RemovesImage()
    {
        var first = Guid.NewGuid();
        var kept = Guid.NewGuid();
        await _store.SetAsync(first, null, new ItemImage([1], "image/png", null));
        await _store.SetAsync(kept, null, new ItemImage([2], "image/png", null));

        await _store.DeleteRangeAsync(new List<Guid> { first });

        Assert.Null(await _store.GetImageAsync(first));
        Assert.NotNull(await _store.GetImageAsync(kept));
    }
}
