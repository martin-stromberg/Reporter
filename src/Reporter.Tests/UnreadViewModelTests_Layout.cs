// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the start page layout (list vs. cards) of the <see cref="UnreadViewModel"/> class.
/// </summary>
public class UnreadViewModelTests_Layout : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly UnreadViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnreadViewModelTests_Layout"/> class.
    /// </summary>
    public UnreadViewModelTests_Layout()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _viewModel = new UnreadViewModel(
            new ItemRepository(_factory, new FakeItemContentStore()),
            new CategoryRepository(_factory),
            new FakeFeedSyncService(),
            _settingsRepository,
            new FakeNetworkStatusService());
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that the list layout is used when nothing else is persisted.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_WithDefaultSettings_UsesListLayout()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsListLayout);
        Assert.False(_viewModel.IsCardLayout);
    }

    /// <summary>
    /// Verifies that a persisted card layout is applied on load.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_WithPersistedCardLayout_UsesCardLayout()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadListLayout: SettingsValues.UnreadLayoutCards);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsCardLayout);
    }

    /// <summary>
    /// Verifies that an unknown persisted layout value falls back to the list layout.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LoadCommand_WithUnknownLayout_FallsBackToList()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadListLayout: "grid");

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(SettingsValues.UnreadLayoutList, _viewModel.ListLayout);
    }

    /// <summary>
    /// Verifies that the toggle switches to the card layout and persists the choice.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ToggleLayoutCommand_FromList_PersistsCardLayout()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.ToggleLayoutCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsCardLayout);
        Assert.Equal(SettingsValues.UnreadLayoutCards, (await _settingsRepository.GetAsync()).UnreadListLayout);
    }

    /// <summary>
    /// Verifies that the toggle keeps all other persisted settings unchanged.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ToggleLayoutCommand_KeepsOtherSettings()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadSortOrder: SettingsValues.SortOrderAscending, notificationsEnabled: false);
        await _viewModel.LoadCommand.ExecuteAsync(null);

        await _viewModel.ToggleLayoutCommand.ExecuteAsync(null);

        var settings = await _settingsRepository.GetAsync();
        Assert.Equal(SettingsValues.SortOrderAscending, settings.UnreadSortOrder);
        Assert.False(settings.NotificationsEnabled);
    }

    /// <summary>
    /// Verifies that the toggle's screen-reader description names the target layout.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task LayoutToggleDescription_InCardLayout_NamesListLayout()
    {
        await TestSettingsHelper.SaveAsync(_settingsRepository, unreadListLayout: SettingsValues.UnreadLayoutCards);

        await _viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(AppResources.UnreadLayoutSwitchToList, _viewModel.LayoutToggleDescription);
    }
}
