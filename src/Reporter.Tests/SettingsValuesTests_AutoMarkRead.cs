using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the auto-mark-as-read mode evaluation in <see cref="SettingsValues"/>.
/// </summary>
public class SettingsValuesTests_AutoMarkRead
{
    /// <summary>
    /// Verifies that <see cref="SettingsValues.IsAutoMarkReadEnabled"/> returns <c>false</c>
    /// only for the "off" mode and <c>true</c> for every other value, including <c>null</c>
    /// and unknown legacy modes. This evaluation gates the local auto-read switch in the
    /// article detail view (usability finding: the switch must visibly reflect that the
    /// global setting is disabled).
    /// </summary>
    /// <param name="mode">The persisted auto-mark-as-read mode.</param>
    /// <param name="expected">The expected evaluation result.</param>
    [Theory]
    [InlineData(null, true)]
    [InlineData(SettingsValues.AutoMarkReadOnOpen, true)]
    [InlineData(SettingsValues.AutoMarkReadOnScroll, true)]
    [InlineData(SettingsValues.AutoMarkReadOff, false)]
    [InlineData("unknown", true)]
    public void IsAutoMarkReadEnabled_EvaluatesMode(string? mode, bool expected)
    {
        Assert.Equal(expected, SettingsValues.IsAutoMarkReadEnabled(mode));
    }
}
