// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the unread list layout of the <see cref="Settings"/> model,
/// <see cref="SettingsValues"/> and its persistence.
/// </summary>
public class SettingsTests_UnreadLayout : IDisposable
{
    private readonly TestDbContextFactory _factory = new();

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that only "cards" is kept and every other value normalizes to "list".
    /// </summary>
    /// <param name="input">The persisted value.</param>
    /// <param name="expected">The normalized value.</param>
    [Theory]
    [InlineData("cards", "cards")]
    [InlineData("list", "list")]
    [InlineData(null, "list")]
    [InlineData("grid", "list")]
    public void NormalizeUnreadLayout_ReturnsSupportedValue(string? input, string expected)
    {
        Assert.Equal(expected, SettingsValues.NormalizeUnreadLayout(input));
    }

    /// <summary>
    /// Verifies that WithUnreadListLayout changes only the layout.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task WithUnreadListLayout_KeepsAllOtherValues()
    {
        var original = await new SettingsRepository(_factory).GetAsync();

        var copy = original.WithUnreadListLayout(SettingsValues.UnreadLayoutCards);

        Assert.Equal(SettingsValues.UnreadLayoutCards, copy.UnreadListLayout);
        Assert.Equivalent(original, copy.WithUnreadListLayout(original.UnreadListLayout!), strict: true);
    }

    /// <summary>
    /// Verifies that the repository persists the layout and reads it back.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SaveAsync_PersistsUnreadListLayout()
    {
        var repository = new SettingsRepository(_factory);
        var settings = await repository.GetAsync();

        await repository.SaveAsync(settings.WithUnreadListLayout(SettingsValues.UnreadLayoutCards));

        Assert.Equal(SettingsValues.UnreadLayoutCards, (await repository.GetAsync()).UnreadListLayout);
    }

    /// <summary>
    /// Verifies that the seeded settings row defaults to the list layout.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetAsync_WithSeededSettings_DefaultsToList()
    {
        var settings = await new SettingsRepository(_factory).GetAsync();

        Assert.Equal(SettingsValues.UnreadLayoutList, settings.UnreadListLayout);
    }
}
