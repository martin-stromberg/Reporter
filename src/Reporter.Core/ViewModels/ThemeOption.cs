// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.ViewModels;

/// <summary>
/// Represents a selectable appearance theme option.
/// </summary>
public class ThemeOption
{
    /// <summary>
    /// Gets the persisted theme value ("system", "light" or "dark").
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the localized display label.
    /// </summary>
    public required string Label { get; init; }
}
