// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="IDeviceInfoProvider"/> fake with a settable snapshot.
/// </summary>
public sealed class FakeDeviceInfoProvider : IDeviceInfoProvider
{
    /// <summary>
    /// Gets or sets the snapshot returned by <see cref="GetSnapshot"/>.
    /// </summary>
    /// <value>The snapshot returned by <see cref="GetSnapshot"/>.</value>
    public AppDeviceInfo Info { get; set; } = new()
    {
        AppName = "Reporter",
        AppVersion = "1.2.3",
        AppBuild = "42",
        DeviceModel = "Pixel 8",
        DeviceManufacturer = "Google",
        Platform = "Android",
        OsVersion = "34",
    };

    /// <inheritdoc />
    public AppDeviceInfo GetSnapshot()
    {
        return Info;
    }
}
