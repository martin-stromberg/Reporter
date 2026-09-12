// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Foundation;
using UIKit;
using UserNotifications;

namespace Reporter;

/// <summary>
/// The iOS application delegate for the .NET MAUI application.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    // Die native Delegate-Property von UNUserNotificationCenter ist weak — die Instanz
    // muss verwaltet gehalten werden, sonst kann der GC sie freigeben.
    private readonly NotificationDelegate _notificationDelegate = new();

    /// <summary>
    /// Creates the <see cref="MauiApp"/> for this platform.
    /// </summary>
    /// <returns>The configured <see cref="MauiApp"/>.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    /// <inheritdoc />
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        UNUserNotificationCenter.Current.Delegate = _notificationDelegate;
        return base.FinishedLaunching(application, launchOptions);
    }
}
