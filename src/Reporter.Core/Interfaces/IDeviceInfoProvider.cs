// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Abstracts the platform facility for application, device and operating system
/// information (for example <c>AppInfo</c>/<c>DeviceInfo</c>), so that core services
/// can include them in the debug report without a MAUI dependency.
/// </summary>
public interface IDeviceInfoProvider
{
    /// <summary>
    /// Captures the current application and device information.
    /// </summary>
    /// <returns>An <see cref="AppDeviceInfo"/> snapshot.</returns>
    AppDeviceInfo GetSnapshot();
}
