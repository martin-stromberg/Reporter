// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.ViewModels;

/// <summary>
/// Represents a selectable background refresh interval option.
/// </summary>
public class RefreshIntervalOption
{
    /// <summary>
    /// Gets the interval in minutes.
    /// </summary>
    public required int Minutes { get; init; }

    /// <summary>
    /// Gets the localized display label.
    /// </summary>
    public required string Label { get; init; }
}
