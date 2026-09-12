// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Applies the configured appearance theme to the running application.
/// </summary>
public interface IAppThemeService
{
    /// <summary>
    /// Applies the specified theme value ("system", "light" or "dark").
    /// Unknown values fall back to the system theme.
    /// </summary>
    /// <param name="theme">The theme value to apply.</param>
    void ApplyTheme(string? theme);
}
