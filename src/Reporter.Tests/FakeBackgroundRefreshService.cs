// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="IBackgroundRefreshService"/> fake that records applied
/// settings and can be configured to throw inside <see cref="ApplySettingsAsync"/>.
/// </summary>
public sealed class FakeBackgroundRefreshService : IBackgroundRefreshService
{
    /// <summary>
    /// Gets the settings passed to <see cref="ApplySettingsAsync"/> in call order.
    /// </summary>
    /// <value>The settings passed to <see cref="ApplySettingsAsync"/> in call order.</value>
    public List<Settings> AppliedSettings { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the fake reports platform support for background refresh.
    /// </summary>
    /// <value>The value returned by <see cref="IsSupported"/>.</value>
    public bool IsSupported { get; set; } = true;

    /// <summary>
    /// Gets or sets the exception thrown by <see cref="ApplySettingsAsync"/>, or <c>null</c> for none.
    /// </summary>
    /// <value>The exception thrown by <see cref="ApplySettingsAsync"/>, or <c>null</c> for none.</value>
    public Exception? ApplySettingsException { get; set; }

    /// <inheritdoc />
    public Task ApplySettingsAsync(Settings settings, CancellationToken cancellationToken = default)
    {
        AppliedSettings.Add(settings);

        if (ApplySettingsException is not null)
        {
            throw ApplySettingsException;
        }

        return Task.CompletedTask;
    }
}
