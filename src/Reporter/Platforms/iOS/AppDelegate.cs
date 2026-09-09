using Foundation;

namespace Reporter;

/// <summary>
/// The iOS application delegate for the .NET MAUI application.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <summary>
    /// Creates the <see cref="MauiApp"/> for this platform.
    /// </summary>
    /// <returns>The configured <see cref="MauiApp"/>.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
