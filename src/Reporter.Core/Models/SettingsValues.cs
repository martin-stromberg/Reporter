// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Central constants for the persisted settings values.
/// </summary>
public static class SettingsValues
{
    /// <summary>
    /// The auto-mark-as-read mode that marks articles as read when they are opened.
    /// </summary>
    public const string AutoMarkReadOnOpen = "on_open";

    /// <summary>
    /// The auto-mark-as-read mode that marks articles as read while scrolling (legacy seed default).
    /// </summary>
    public const string AutoMarkReadOnScroll = "on_scroll";

    /// <summary>
    /// The auto-mark-as-read mode that disables automatic marking.
    /// </summary>
    public const string AutoMarkReadOff = "off";

    /// <summary>
    /// The theme value that follows the operating system appearance.
    /// </summary>
    public const string ThemeSystem = "system";

    /// <summary>
    /// The theme value for the light appearance.
    /// </summary>
    public const string ThemeLight = "light";

    /// <summary>
    /// The theme value for the dark appearance.
    /// </summary>
    public const string ThemeDark = "dark";

    /// <summary>
    /// The language value that follows the operating system language.
    /// </summary>
    public const string LanguageSystem = "system";

    /// <summary>
    /// The language value for German.
    /// </summary>
    public const string LanguageGerman = "de";

    /// <summary>
    /// The language value for English.
    /// </summary>
    public const string LanguageEnglish = "en";

    /// <summary>
    /// The sort order value that lists the newest articles first.
    /// </summary>
    public const string SortOrderDescending = "desc";

    /// <summary>
    /// The sort order value that lists the oldest articles first.
    /// </summary>
    public const string SortOrderAscending = "asc";

    /// <summary>
    /// Determines whether the automatic mark-as-read feature is active for the given persisted mode.
    /// </summary>
    /// <param name="autoMarkReadMode">The persisted auto-mark-as-read mode, or <c>null</c>.</param>
    /// <returns><c>true</c> for every mode except <see cref="AutoMarkReadOff"/>; otherwise <c>false</c>.</returns>
    public static bool IsAutoMarkReadEnabled(string? autoMarkReadMode)
    {
        return autoMarkReadMode != AutoMarkReadOff;
    }
}
