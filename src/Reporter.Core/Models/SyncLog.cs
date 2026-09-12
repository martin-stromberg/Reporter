// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Represents a feed synchronization log entry in the domain model.
/// </summary>
public class SyncLog
{
    /// <summary>
    /// Gets the unique identifier of the sync log entry.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the optional feed identifier the sync log belongs to.
    /// </summary>
    public Guid? FeedId { get; init; }

    /// <summary>
    /// Gets the timestamp when the synchronization started.
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the synchronization finished.
    /// </summary>
    public DateTime? FinishedAt { get; init; }

    /// <summary>
    /// Gets the sync status.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets the sync message or error details.
    /// </summary>
    public string? Message { get; init; }
}
