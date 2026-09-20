// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// An <see cref="IKeywordRepository"/> decorator that delegates every call to an inner
/// repository. Test doubles derive from this class and override only the members
/// whose behavior they need to change.
/// </summary>
public class DelegatingKeywordRepository : IKeywordRepository
{
    private readonly IKeywordRepository _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegatingKeywordRepository"/> class.
    /// </summary>
    /// <param name="inner">The repository to delegate to.</param>
    public DelegatingKeywordRepository(IKeywordRepository inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<Keyword>> GetAllAsync() => _inner.GetAllAsync();

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<Keyword>> GetByFeedAsync(Guid? feedId) => _inner.GetByFeedAsync(feedId);

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<Keyword>> GetEffectiveForFeedAsync(Guid feedId) => _inner.GetEffectiveForFeedAsync(feedId);

    /// <inheritdoc />
    public virtual Task<bool> AnyAsync() => _inner.AnyAsync();

    /// <inheritdoc />
    public virtual Task<Keyword?> GetByIdAsync(Guid id) => _inner.GetByIdAsync(id);

    /// <inheritdoc />
    public virtual Task AddAsync(Keyword keyword) => _inner.AddAsync(keyword);

    /// <inheritdoc />
    public virtual Task UpdateAsync(Keyword keyword) => _inner.UpdateAsync(keyword);

    /// <inheritdoc />
    public virtual Task DeleteAsync(Guid id) => _inner.DeleteAsync(id);
}
