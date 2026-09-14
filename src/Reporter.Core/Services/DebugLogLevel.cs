// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Services;

/// <summary>
/// Provides severity level constants for session debug log entries.
/// </summary>
public static class DebugLogLevel
{
    /// <summary>
    /// Level for informational entries such as lifecycle events.
    /// </summary>
    public const string Info = "Info";

    /// <summary>
    /// Level for warning entries.
    /// </summary>
    public const string Warning = "Warning";

    /// <summary>
    /// Level for error entries.
    /// </summary>
    public const string Error = "Error";
}
