// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.ViewModels;

/// <summary>
/// Represents a selectable app language option.
/// </summary>
public class LanguageOption
{
    /// <summary>
    /// Gets the persisted language value ("system", "de" or "en").
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the localized display label.
    /// </summary>
    public required string Label { get; init; }
}
