// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Android.App;
using Android.Runtime;

namespace Reporter;

/// <summary>
/// The main Android application class for the .NET MAUI application.
/// </summary>
[Application]
public class MainApplication : MauiApplication
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainApplication"/> class.
    /// </summary>
    /// <param name="handle">The native handle.</param>
    /// <param name="ownership">The handle ownership.</param>
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    /// <summary>
    /// Creates the <see cref="MauiApp"/> for this platform.
    /// </summary>
    /// <returns>The configured <see cref="MauiApp"/>.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
