// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Extensions.Time.Testing;
using Reporter.Core.Models;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the start page layout option of the <see cref="SettingsViewModel"/> class.
/// </summary>
public class SettingsViewModelTests_UnreadLayout : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModelTests_UnreadLayout"/> class.
    /// </summary>
    public SettingsViewModelTests_UnreadLayout()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _viewModel = new SettingsViewModel(
            _settingsRepository,
            new KeywordRepository(_factory),
            new FakeAutoRefreshService(),
            new FakeAppThemeService(),
            new FakeTimeProvider());
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that the list and card layouts are offered in this order.
    /// </summary>
    [Fact]
    public void UnreadLayoutOptions_OfferListAndCards()
    {
        Assert.Equal(
            [SettingsValues.UnreadLayoutList, SettingsValues.UnreadLayoutCards],
            _viewModel.UnreadLayoutOptions.Select(o => o.Value));
    }

    /// <summary>
    /// Verifies that loading selects the list layout by default.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_WithDefaultSettings_SelectsListLayout()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(SettingsValues.UnreadLayoutList, _viewModel.SelectedUnreadLayout?.Value);
    }

    /// <summary>
    /// Verifies that loading selects a persisted card layout.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_WithPersistedCards_SelectsCardLayout()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadListLayout: SettingsValues.UnreadLayoutCards);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(SettingsValues.UnreadLayoutCards, _viewModel.SelectedUnreadLayout?.Value);
    }

    /// <summary>
    /// Verifies that selecting a layout persists it immediately.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SelectedUnreadLayout_Change_PersistsImmediately()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.SelectedUnreadLayout = _viewModel.UnreadLayoutOptions.First(o => o.Value == SettingsValues.UnreadLayoutCards);
        await TestWaitHelper.WaitUntilAsync(async () => (await _settingsRepository.GetAsync()).UnreadListLayout == SettingsValues.UnreadLayoutCards);

        Assert.Equal(SettingsValues.UnreadLayoutCards, (await _settingsRepository.GetAsync()).UnreadListLayout);
    }
}
