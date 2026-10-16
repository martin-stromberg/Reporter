// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.ViewModels;

/// <summary>
/// Represents a selectable layout option for the unread articles start page.
/// </summary>
public class UnreadLayoutOption
{
    /// <summary>
    /// Gets the persisted layout value ("list" or "cards").
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the localized display label.
    /// </summary>
    public required string Label { get; init; }
}
