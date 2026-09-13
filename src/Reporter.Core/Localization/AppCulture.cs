// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using Reporter.Core.Models;

namespace Reporter.Core.Localization;

/// <summary>
/// Applies the persisted language selection to the process-wide culture settings.
/// </summary>
public static class AppCulture
{
    /// <summary>
    /// Maps a persisted language value to the corresponding <see cref="CultureInfo"/>.
    /// </summary>
    /// <param name="language">The persisted language value ("system", "de" or "en").</param>
    /// <returns>
    /// The <see cref="CultureInfo"/> for "de" or "en"; <see langword="null"/> for "system",
    /// <see langword="null"/> or any unknown value.
    /// </returns>
    public static CultureInfo? ResolveCulture(string? language)
    {
        return language switch
        {
            SettingsValues.LanguageGerman => new CultureInfo(SettingsValues.LanguageGerman),
            SettingsValues.LanguageEnglish => new CultureInfo(SettingsValues.LanguageEnglish),
            _ => null,
        };
    }

    /// <summary>
    /// Applies the persisted language value to <see cref="CultureInfo.CurrentUICulture"/>,
    /// <see cref="CultureInfo.CurrentCulture"/>, <see cref="CultureInfo.DefaultThreadCurrentUICulture"/>
    /// and <see cref="CultureInfo.DefaultThreadCurrentCulture"/>. Does nothing for "system",
    /// <see langword="null"/> or unknown values, so the process keeps the system culture.
    /// </summary>
    /// <param name="language">The persisted language value ("system", "de" or "en").</param>
    public static void Apply(string? language)
    {
        var culture = ResolveCulture(language);
        if (culture is null)
        {
            return;
        }

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
    }
}
