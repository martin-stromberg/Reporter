// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// An <see cref="IItemRepository"/> decorator that delegates every call to an inner
/// repository. Test doubles derive from this class and override only the members
/// whose behavior they need to change.
/// </summary>
public class DelegatingItemRepository : IItemRepository
{
    private readonly IItemRepository _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingItemRepository"/> class.
    /// </summary>
    /// <param name="inner">The repository to delegate to.</param>
    public DelegatingItemRepository(IItemRepository inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<Item>> GetAllAsync() => _inner.GetAllAsync();

    /// <inheritdoc />
    public virtual Task<Item?> GetByIdAsync(Guid id) => _inner.GetByIdAsync(id);

    /// <inheritdoc />
    public virtual Task AddAsync(Item item) => _inner.AddAsync(item);

    /// <inheritdoc />
    public virtual Task UpdateAsync(Item item) => _inner.UpdateAsync(item);

    /// <inheritdoc />
    public virtual Task DeleteAsync(Guid id) => _inner.DeleteAsync(id);

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<Item>> GetUnreadByDateAsync() => _inner.GetUnreadByDateAsync();

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<ItemListItem>> GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null) =>
        _inner.GetUnreadByDateAsync(page, pageSize, categoryId);

    /// <inheritdoc />
    public virtual Task<int> GetUnreadCountAsync(Guid? categoryId = null) => _inner.GetUnreadCountAsync(categoryId);

    /// <inheritdoc />
    public virtual Task MarkAllAsReadAsync(Guid? categoryId = null) => _inner.MarkAllAsReadAsync(categoryId);

    /// <inheritdoc />
    public virtual Task ToggleSavedForLaterAsync(Guid id) => _inner.ToggleSavedForLaterAsync(id);

    /// <inheritdoc />
    public virtual Task MarkAsReadAsync(Guid id) => _inner.MarkAsReadAsync(id);

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<Item>> GetByFeedAsync(Guid feedId) => _inner.GetByFeedAsync(feedId);

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<Item>> GetByCategoryAsync(Guid categoryId) => _inner.GetByCategoryAsync(categoryId);

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<ItemListItem>> GetSavedForLaterAsync(int page, int pageSize) =>
        _inner.GetSavedForLaterAsync(page, pageSize);

    /// <inheritdoc />
    public virtual Task AddRangeAsync(IReadOnlyList<Item> items) => _inner.AddRangeAsync(items);

    /// <inheritdoc />
    public virtual Task<int> DeleteExpiredAsync(DateTime cutoff, CancellationToken cancellationToken = default) =>
        _inner.DeleteExpiredAsync(cutoff, cancellationToken);

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<Item>> GetExpiredKeywordCandidatesAsync(DateTime cutoff, CancellationToken cancellationToken = default) =>
        _inner.GetExpiredKeywordCandidatesAsync(cutoff, cancellationToken);

    /// <inheritdoc />
    public virtual Task<int> DeleteRangeAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default) =>
        _inner.DeleteRangeAsync(ids, cancellationToken);
}
