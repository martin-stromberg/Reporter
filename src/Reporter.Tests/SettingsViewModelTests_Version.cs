// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.ViewModels;
using Reporter.Data.Repositories;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="SettingsViewModel.AppVersionText"/> label.
/// </summary>
public class SettingsViewModelTests_Version : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly SettingsRepository _settingsRepository;
    private readonly KeywordRepository _keywordRepository;
    private readonly FakeAutoRefreshService _autoRefreshService;
    private readonly FakeAppThemeService _appThemeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModelTests_Version"/> class.
    /// </summary>
    public SettingsViewModelTests_Version()
    {
        _factory = new TestDbContextFactory();
        _settingsRepository = new SettingsRepository(_factory);
        _keywordRepository = new KeywordRepository(_factory);
        _autoRefreshService = new FakeAutoRefreshService();
        _appThemeService = new FakeAppThemeService();
    }

    /// <summary>
    /// Disposes the test factory.
    /// </summary>
    public void Dispose()
    {
        _factory.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="SettingsViewModel.AppVersionText"/> formats version and build number.
    /// </summary>
    [Fact]
    public void AppVersionText_FormatsVersionAndBuild()
    {
        var deviceInfoProvider = new FakeDeviceInfoProvider();
        deviceInfoProvider.Info = new Reporter.Core.Models.AppDeviceInfo { AppVersion = "0.3.1", AppBuild = "7" };
        var viewModel = new SettingsViewModel(
            _settingsRepository,
            _keywordRepository,
            _autoRefreshService,
            _appThemeService,
            deviceInfoProvider: deviceInfoProvider);
        Assert.Equal("Version 0.3.1 (Build 7)", viewModel.AppVersionText);
    }

    /// <summary>
    /// Verifies that <see cref="SettingsViewModel.AppVersionText"/> is empty without a device info provider.
    /// </summary>
    [Fact]
    public void AppVersionText_WithoutDeviceInfoProvider_IsEmpty()
    {
        var viewModel = new SettingsViewModel(
            _settingsRepository,
            _keywordRepository,
            _autoRefreshService,
            _appThemeService);
        Assert.Equal(string.Empty, viewModel.AppVersionText);
    }
}
