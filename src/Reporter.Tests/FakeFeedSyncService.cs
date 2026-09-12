// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="IFeedSyncService"/> fake that counts calls and can be
/// configured to throw or block inside <see cref="SyncAllAsync"/>.
/// </summary>
public sealed class FakeFeedSyncService : IFeedSyncService
{
    /// <summary>
    /// Gets or sets the exception thrown by <see cref="SyncAllAsync"/>, or <c>null</c> for none.
    /// </summary>
    public Exception? SyncAllException { get; set; }

    /// <summary>
    /// Gets or sets an optional blocker that <see cref="SyncAllAsync"/> awaits before returning.
    /// </summary>
    public TaskCompletionSource<bool>? SyncAllBlocker { get; set; }

    /// <summary>
    /// Gets the number of <see cref="SyncAllAsync"/> calls.
    /// </summary>
    public int SyncAllCallCount => _syncAllCallCount;

    private int _syncAllCallCount;

    /// <inheritdoc />
    public Task<SyncResult> SyncFeedAsync(Guid feedId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SyncResult("OK", 0));
    }

    /// <inheritdoc />
    public async Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _syncAllCallCount);

        if (SyncAllException is not null)
        {
            throw SyncAllException;
        }

        if (SyncAllBlocker is not null)
        {
            await SyncAllBlocker.Task;
        }

        return new SyncResult("OK", 0);
    }
}
