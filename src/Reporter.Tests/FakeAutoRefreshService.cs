using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="IAutoRefreshService"/> fake that records applied settings.
/// </summary>
public sealed class FakeAutoRefreshService : IAutoRefreshService
{
    /// <summary>
    /// Gets the settings passed to <see cref="ApplySettingsAsync"/> in call order.
    /// </summary>
    /// <value>The settings passed to <see cref="ApplySettingsAsync"/> in call order.</value>
    public List<Settings> AppliedSettings { get; } = new();

    /// <summary>
    /// Gets the number of <see cref="StartAsync"/> calls.
    /// </summary>
    public int StartCallCount { get; private set; }

    /// <summary>
    /// Gets the number of <see cref="StopAsync"/> calls.
    /// </summary>
    public int StopCallCount { get; private set; }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        StartCallCount++;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ApplySettingsAsync(Settings settings)
    {
        AppliedSettings.Add(settings);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        StopCallCount++;
        return Task.CompletedTask;
    }
}
