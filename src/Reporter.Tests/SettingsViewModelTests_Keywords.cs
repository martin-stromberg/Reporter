using Reporter.Core.Resources.Strings;
using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the keyword management of the <see cref="SettingsViewModel"/> class.
/// </summary>
public class SettingsViewModelTests_Keywords : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly FakeAutoRefreshService _autoRefreshService;
    private readonly FakeAppThemeService _appThemeService;
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModelTests_Keywords"/> class.
    /// </summary>
    public SettingsViewModelTests_Keywords()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _keywordRepository = new KeywordRepository(_factory);
        _autoRefreshService = new FakeAutoRefreshService();
        _appThemeService = new FakeAppThemeService();
        _viewModel = new SettingsViewModel(_settingsRepository, _keywordRepository, _autoRefreshService, _appThemeService);
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that a valid keyword is added to the repository and the keywords collection.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddKeyword_Valid_AddsToRepository()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewKeywordText = "  Werbung  ";

        await _viewModel.AddKeywordCommand.ExecuteAsync(null);

        Assert.Single(_viewModel.Keywords);
        Assert.Equal("Werbung", _viewModel.Keywords[0].KeywordText);
        Assert.Equal(string.Empty, _viewModel.NewKeywordText);
        Assert.False(_viewModel.HasError);
        var persisted = await _keywordRepository.GetAllAsync();
        Assert.Single(persisted);
        Assert.Equal("Werbung", persisted[0].KeywordText);
    }

    /// <summary>
    /// Verifies that a case-insensitive duplicate keyword shows an error without inserting.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddKeyword_DuplicateCaseInsensitive_ShowsError()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewKeywordText = "Werbung";
        await _viewModel.AddKeywordCommand.ExecuteAsync(null);

        _viewModel.NewKeywordText = "WERBUNG";
        await _viewModel.AddKeywordCommand.ExecuteAsync(null);

        Assert.True(_viewModel.HasError);
        Assert.Equal(AppResources.ErrorKeywordDuplicate, _viewModel.ErrorMessage);
        Assert.Single(_viewModel.Keywords);
        Assert.Single(await _keywordRepository.GetAllAsync());
    }

    /// <summary>
    /// Verifies that an empty keyword shows an error without inserting.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddKeyword_Empty_ShowsError()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewKeywordText = "   ";

        await _viewModel.AddKeywordCommand.ExecuteAsync(null);

        Assert.True(_viewModel.HasError);
        Assert.Equal(AppResources.ErrorKeywordEmpty, _viewModel.ErrorMessage);
        Assert.Empty(_viewModel.Keywords);
        Assert.Empty(await _keywordRepository.GetAllAsync());
    }

    /// <summary>
    /// Verifies that a keyword longer than 500 characters shows an error without inserting.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddKeyword_TooLong_ShowsError()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewKeywordText = new string('x', 501);

        await _viewModel.AddKeywordCommand.ExecuteAsync(null);

        Assert.True(_viewModel.HasError);
        Assert.Equal(AppResources.ErrorKeywordTooLong, _viewModel.ErrorMessage);
        Assert.Empty(_viewModel.Keywords);
        Assert.Empty(await _keywordRepository.GetAllAsync());
    }

    /// <summary>
    /// Verifies that RemoveKeywordCommand deletes the keyword from the repository and the collection.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task RemoveKeyword_DeletesFromRepository()
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
        _viewModel.NewKeywordText = "Werbung";
        await _viewModel.AddKeywordCommand.ExecuteAsync(null);
        var keyword = _viewModel.Keywords[0];

        await _viewModel.RemoveKeywordCommand.ExecuteAsync(keyword);

        Assert.Empty(_viewModel.Keywords);
        Assert.Empty(await _keywordRepository.GetAllAsync());
    }
}
