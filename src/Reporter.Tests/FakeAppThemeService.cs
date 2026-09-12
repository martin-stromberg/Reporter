// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="IAppThemeService"/> fake that records applied theme values.
/// </summary>
public sealed class FakeAppThemeService : IAppThemeService
{
    /// <summary>
    /// Gets the theme values passed to <see cref="ApplyTheme"/> in call order.
    /// </summary>
    /// <value>The theme values passed to <see cref="ApplyTheme"/> in call order.</value>
    public List<string?> AppliedThemes { get; } = new();

    /// <inheritdoc />
    public void ApplyTheme(string? theme)
    {
        AppliedThemes.Add(theme);
    }
}
