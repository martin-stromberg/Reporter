// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// An in-memory <see cref="IItemContentStore"/> fake backed by a dictionary,
/// applying the same upsert semantics as the EF implementation: only non-empty
/// contents are stored and a <c>null</c> content removes the entry.
/// </summary>
public sealed class FakeItemContentStore : IItemContentStore
{
    private readonly Dictionary<Guid, string> _contents = new();

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
    public Task SetAsync(Guid itemId, string? contentHtml, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(contentHtml))
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
    public Task SetRangeAsync(IReadOnlyList<ItemContentEntry> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.ContentHtml))
            {
                _contents.Remove(entry.ItemId);
            }
            else
            {
                _contents[entry.ItemId] = entry.ContentHtml;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        _contents.Remove(itemId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteRangeAsync(IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken = default)
    {
        foreach (var itemId in itemIds)
        {
            _contents.Remove(itemId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> GetItemIdsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(_contents.Keys.ToList());
    }
}
