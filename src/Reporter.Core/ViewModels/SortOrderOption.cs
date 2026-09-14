// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.ViewModels;

/// <summary>
/// Represents a selectable sort order option for the unread articles list.
/// </summary>
public class SortOrderOption
{
    /// <summary>
    /// Gets the persisted sort order value ("desc" or "asc").
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the localized display label.
    /// </summary>
    public required string Label { get; init; }
}
