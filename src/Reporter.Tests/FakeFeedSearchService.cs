// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="IFeedSearchService"/> fake that returns configured
/// results or throws a configured exception.
/// </summary>
public sealed class FakeFeedSearchService : IFeedSearchService
{
    /// <summary>
    /// Gets or sets the results returned by <see cref="SearchAsync"/>.
    /// </summary>
    public IReadOnlyList<FeedSearchResult> NextResults { get; set; } = [];

    /// <summary>
    /// Gets or sets the exception thrown by <see cref="SearchAsync"/>, or <c>null</c> for none.
    /// </summary>
    public Exception? NextException { get; set; }

    /// <summary>
    /// Gets or sets a gate that keeps <see cref="SearchAsync"/> in flight until it is
    /// completed, simulating a slow search. Takes precedence over <see cref="NextResults"/>.
    /// </summary>
    public TaskCompletionSource<IReadOnlyList<FeedSearchResult>>? PendingResult { get; set; }

    /// <summary>
    /// Gets the query passed to the last <see cref="SearchAsync"/> call.
    /// </summary>
    public string? LastQuery { get; private set; }

    /// <summary>
    /// Gets the number of <see cref="SearchAsync"/> calls.
    /// </summary>
    public int CallCount { get; private set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<FeedSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastQuery = query;
        if (PendingResult is not null)
        {
            return PendingResult.Task;
        }

        return NextException is not null
            ? Task.FromException<IReadOnlyList<FeedSearchResult>>(NextException)
            : Task.FromResult(NextResults);
    }
}
