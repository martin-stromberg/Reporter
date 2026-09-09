namespace Reporter.Data.Entities;

/// <summary>
/// Represents a feed synchronization log entry.
/// </summary>
public class SyncLog
{
    /// <summary>
    /// Gets or sets the unique identifier of the sync log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the optional feed identifier the sync log belongs to.
    /// </summary>
    public Guid? FeedId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the synchronization started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the synchronization finished.
    /// </summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Gets or sets the sync status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the sync message or error details.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the optional feed the sync log belongs to.
    /// </summary>
    public Feed? Feed { get; set; }
}
