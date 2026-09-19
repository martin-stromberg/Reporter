// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// An in-memory <see cref="IItemContentStore"/> fake backed by dictionaries,
/// applying the same field-wise upsert semantics as the EF implementation:
/// a supplied image writes the image while a <c>null</c> image leaves it
/// untouched, and a <c>null</c> content removes the stored content only when
/// no image is supplied in the same call.
/// </summary>
public sealed class FakeItemContentStore : IItemContentStore
{
    private readonly Dictionary<Guid, string> _contents = new();
    private readonly Dictionary<Guid, ItemImage> _images = new();

    /// <inheritdoc />
    public Task<string?> GetAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_contents.GetValueOrDefault(itemId));
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<Guid, string>> GetRangeAsync(IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken = default)
    {
        var result = itemIds
            .Where(_contents.ContainsKey)
            .ToDictionary(id => id, id => _contents[id]);
        return Task.FromResult<IReadOnlyDictionary<Guid, string>>(result);
    }

    /// <inheritdoc />
    public Task<ItemImage?> GetImageAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_images.TryGetValue(itemId, out var image) ? image : null);
    }

    /// <inheritdoc />
    public Task<IReadOnlySet<Guid>> GetImageIdsAsync(IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlySet<Guid>>(itemIds.Where(_images.ContainsKey).ToHashSet());
    }

    /// <inheritdoc />
    public Task SetAsync(Guid itemId, string? contentHtml, ItemImage? image = null, CancellationToken cancellationToken = default)
    {
        if (image is not null)
        {
            _images[itemId] = image;
            if (!string.IsNullOrEmpty(contentHtml))
            {
                _contents[itemId] = contentHtml;
            }
        }
        else if (string.IsNullOrEmpty(contentHtml))
        {
            _contents.Remove(itemId);
        }
        else
        {
            _contents[itemId] = contentHtml;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SetRangeAsync(IReadOnlyList<ItemContentEntry> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
        {
            await SetAsync(entry.ItemId, entry.ContentHtml, entry.Image, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        _contents.Remove(itemId);
        _images.Remove(itemId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteRangeAsync(IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken = default)
    {
        foreach (var itemId in itemIds)
        {
            _contents.Remove(itemId);
            _images.Remove(itemId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> GetItemIdsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(_contents.Keys.Union(_images.Keys).ToList());
    }
}
