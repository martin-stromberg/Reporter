// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Services;

/// <summary>
/// Implements <see cref="IDeviceInfoProvider"/> by reading <see cref="AppInfo"/> and
/// <see cref="DeviceInfo"/>.
/// </summary>
public class DeviceInfoProvider : IDeviceInfoProvider
{
    /// <inheritdoc />
    public AppDeviceInfo GetSnapshot()
    {
        return new AppDeviceInfo
        {
            AppName = AppInfo.Current.Name,
            AppVersion = AppInfo.Current.VersionString,
            AppBuild = AppInfo.Current.BuildString,
            DeviceModel = DeviceInfo.Current.Model,
            DeviceManufacturer = DeviceInfo.Current.Manufacturer,
            Platform = DeviceInfo.Current.Platform.ToString(),
            OsVersion = DeviceInfo.Current.VersionString,
        };
    }
}
