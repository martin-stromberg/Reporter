// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Data.Entities;

/// <summary>
/// Represents a session debug log entry record.
/// </summary>
public class DebugLogEntry
{
    /// <summary>
    /// Gets or sets the unique identifier of the debug log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the entry was recorded.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the severity level of the entry.
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// Gets or sets the category of the entry.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets optional details such as an exception dump.
    /// </summary>
    public string? Details { get; set; }
}
