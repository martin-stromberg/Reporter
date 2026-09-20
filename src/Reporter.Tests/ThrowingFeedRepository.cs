// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// An <see cref="IFeedRepository"/> whose members all fail to simulate a
/// repository failure.
/// </summary>
public sealed class ThrowingFeedRepository : IFeedRepository
{
    private static readonly InvalidOperationException Failure = new("Simulated repository failure.");

    /// <inheritdoc />
    public Task<IReadOnlyList<Feed>> GetAllAsync() => Task.FromException<IReadOnlyList<Feed>>(Failure);

    /// <inheritdoc />
    public Task<Feed?> GetByIdAsync(Guid id) => Task.FromException<Feed?>(Failure);

    /// <inheritdoc />
    public Task AddAsync(Feed feed) => Task.FromException(Failure);

    /// <inheritdoc />
    public Task UpdateAsync(Feed feed) => Task.FromException(Failure);

    /// <inheritdoc />
    public Task DeleteAsync(Guid id) => Task.FromException(Failure);

    /// <inheritdoc />
    public Task<IReadOnlyList<FeedListItem>> GetAllWithDetailsAsync() => Task.FromException<IReadOnlyList<FeedListItem>>(Failure);

    /// <inheritdoc />
    public Task<Feed?> GetByUrlAsync(string url) => Task.FromException<Feed?>(Failure);
}
