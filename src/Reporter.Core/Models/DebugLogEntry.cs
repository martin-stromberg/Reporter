// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Represents a session debug log entry in the domain model. The entries are
/// reset on every application start and only written while debug collection is enabled.
/// </summary>
public class DebugLogEntry
{
    /// <summary>
    /// Gets the unique identifier of the debug log entry.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the entry was recorded.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the severity level of the entry (see <c>DebugLogLevel</c>).
    /// </summary>
    public string? Level { get; init; }

    /// <summary>
    /// Gets the category of the entry (see <c>DebugLogCategory</c>).
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets the log message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets optional details such as an exception dump (<c>exception.ToString()</c>).
    /// </summary>
    public string? Details { get; init; }
}
