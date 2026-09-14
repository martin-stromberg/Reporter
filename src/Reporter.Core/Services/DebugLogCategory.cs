// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Services;

/// <summary>
/// Provides category constants for session debug log entries.
/// </summary>
public static class DebugLogCategory
{
    /// <summary>
    /// Category for application lifecycle events (session start, suspend, resume).
    /// </summary>
    public const string Lifecycle = "Lifecycle";

    /// <summary>
    /// Category for feed synchronization and auto-refresh errors.
    /// </summary>
    public const string Sync = "Sync";

    /// <summary>
    /// Category for unhandled and unobserved exceptions.
    /// </summary>
    public const string Exception = "Exception";

    /// <summary>
    /// Category for settings-related events.
    /// </summary>
    public const string Settings = "Settings";

    /// <summary>
    /// Category for debug report related events.
    /// </summary>
    public const string Report = "Report";
}
