// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Represents a snapshot of application and device information used in the debug report.
/// </summary>
public class AppDeviceInfo
{
    /// <summary>
    /// Gets the application name.
    /// </summary>
    public string? AppName { get; init; }

    /// <summary>
    /// Gets the application version string.
    /// </summary>
    public string? AppVersion { get; init; }

    /// <summary>
    /// Gets the application build string.
    /// </summary>
    public string? AppBuild { get; init; }

    /// <summary>
    /// Gets the device model.
    /// </summary>
    public string? DeviceModel { get; init; }

    /// <summary>
    /// Gets the device manufacturer.
    /// </summary>
    public string? DeviceManufacturer { get; init; }

    /// <summary>
    /// Gets the platform the application is running on.
    /// </summary>
    public string? Platform { get; init; }

    /// <summary>
    /// Gets the operating system version string.
    /// </summary>
    public string? OsVersion { get; init; }
}
