// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using Reporter.Core.Localization;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="AppCulture"/> class.
/// </summary>
public class AppCultureTests
{
    /// <summary>
    /// Verifies that ResolveCulture maps "de" and "en" to the corresponding culture
    /// and returns <see langword="null"/> for "system" and unknown values.
    /// </summary>
    /// <param name="language">The persisted language value.</param>
    /// <param name="expectedName">The expected culture name, or <see langword="null"/>.</param>
    [Theory]
    [InlineData("de", "de")]
    [InlineData("en", "en")]
    [InlineData("system", null)]
    [InlineData(null, null)]
    [InlineData("fr", null)]
    [InlineData("", null)]
    public void ResolveCulture_ReturnsExpected(string? language, string? expectedName)
    {
        var result = AppCulture.ResolveCulture(language);

        Assert.Equal(expectedName, result?.Name);
    }

    /// <summary>
    /// Verifies that Apply sets the current and default thread cultures for a known language.
    /// </summary>
    [Fact]
    public void Apply_SetsCultures()
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;
        var previousDefaultUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        var previousDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        try
        {
            AppCulture.Apply("de");

            Assert.Equal("de", CultureInfo.CurrentUICulture.Name);
            Assert.Equal("de", CultureInfo.CurrentCulture.Name);
            Assert.Equal("de", CultureInfo.DefaultThreadCurrentUICulture?.Name);
            Assert.Equal("de", CultureInfo.DefaultThreadCurrentCulture?.Name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.DefaultThreadCurrentUICulture = previousDefaultUiCulture;
            CultureInfo.DefaultThreadCurrentCulture = previousDefaultCulture;
        }
    }

    /// <summary>
    /// Verifies that Apply leaves the process-wide cultures untouched for "system".
    /// </summary>
    [Fact]
    public void Apply_System_DoesNotChangeCultures()
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;

        AppCulture.Apply("system");

        Assert.Equal(previousUiCulture, CultureInfo.CurrentUICulture);
        Assert.Equal(previousCulture, CultureInfo.CurrentCulture);
    }
}
