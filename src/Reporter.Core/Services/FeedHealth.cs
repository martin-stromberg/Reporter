// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Services;

/// <summary>
/// Provides feed health status constants and helpers.
/// </summary>
public static class FeedHealth
{
    /// <summary>
    /// Status value for a healthy feed.
    /// </summary>
    public const string Ok = "OK";

    /// <summary>
    /// Status value for a feed with warnings.
    /// </summary>
    public const string Warning = "Warning";

    /// <summary>
    /// Status value for a feed with errors.
    /// </summary>
    public const string Error = "Error";

    /// <summary>
    /// Determines whether the health status has changed.
    /// </summary>
    /// <param name="current">The current status.</param>
    /// <param name="next">The next status.</param>
    /// <returns><c>true</c> if the values differ; otherwise, <c>false</c>.</returns>
    public static bool Changed(string? current, string? next) => current != next;
}
