// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Resources.Strings;
using Reporter.Core.ViewModels;

namespace Reporter.Views;

/// <summary>
/// Page for displaying application settings.
/// </summary>
public partial class SettingsPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for the page.</param>
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is SettingsViewModel viewModel)
        {
            viewModel.NotificationAuthorizationDenied += OnNotificationAuthorizationDenied;
            viewModel.DebugReportFailed += OnDebugReportFailed;
            viewModel.LoadCommand.Execute(null);
        }
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        if (BindingContext is SettingsViewModel viewModel)
        {
            viewModel.NotificationAuthorizationDenied -= OnNotificationAuthorizationDenied;
            viewModel.DebugReportFailed -= OnDebugReportFailed;
        }

        base.OnDisappearing();
    }

    private async Task OnNotificationAuthorizationDenied()
    {
        var openSettings = await DisplayAlertAsync(
            AppResources.NotificationDeniedTitle,
            AppResources.NotificationDeniedMessage,
            AppResources.NotificationDeniedOpenSettings,
            AppResources.ButtonCancel);
        if (openSettings)
        {
            AppInfo.Current.ShowSettingsUI();
        }
    }

    private async Task OnDebugReportFailed()
    {
        await DisplayAlertAsync(
            AppResources.DebugReportFailedTitle,
            AppResources.DebugReportFailedMessage,
            AppResources.ButtonOk);
    }

    private void OnOpenNotificationSettingsClicked(object? sender, EventArgs e)
    {
        AppInfo.Current.ShowSettingsUI();
    }
}
