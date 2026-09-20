// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// An <see cref="IFeedRepository"/> decorator that delegates every call to an inner
/// repository. Test doubles derive from this class and override only the members
/// whose behavior they need to change.
/// </summary>
public class DelegatingFeedRepository : IFeedRepository
{
    private readonly IFeedRepository _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingFeedRepository"/> class.
    /// </summary>
    /// <param name="inner">The repository to delegate to.</param>
    public DelegatingFeedRepository(IFeedRepository inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<Feed>> GetAllAsync() => _inner.GetAllAsync();

    /// <inheritdoc />
    public virtual Task<Feed?> GetByIdAsync(Guid id) => _inner.GetByIdAsync(id);

    /// <inheritdoc />
    public virtual Task AddAsync(Feed feed) => _inner.AddAsync(feed);

    /// <inheritdoc />
    public virtual Task UpdateAsync(Feed feed) => _inner.UpdateAsync(feed);

    /// <inheritdoc />
    public virtual Task DeleteAsync(Guid id) => _inner.DeleteAsync(id);

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<FeedListItem>> GetAllWithDetailsAsync() => _inner.GetAllWithDetailsAsync();

    /// <inheritdoc />
    public virtual Task<Feed?> GetByUrlAsync(string url) => _inner.GetByUrlAsync(url);
}
