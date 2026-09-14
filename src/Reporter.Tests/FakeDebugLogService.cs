// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="IDebugLogService"/> fake that records log calls.
/// </summary>
public sealed class FakeDebugLogService : IDebugLogService
{
    /// <summary>
    /// Gets or sets a value indicating whether the fake reports collection as enabled.
    /// </summary>
    /// <value>The value returned by <see cref="IsEnabled"/>.</value>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets the number of recorded <see cref="BeginSessionAsync"/> calls.
    /// </summary>
    /// <value>The number of recorded <see cref="BeginSessionAsync"/> calls.</value>
    public int BeginSessionCallCount { get; private set; }

    /// <summary>
    /// Gets the recorded <see cref="SetEnabled"/> arguments in call order.
    /// </summary>
    /// <value>The recorded <see cref="SetEnabled"/> arguments in call order.</value>
    public List<bool> SetEnabledCalls { get; } = new();

    /// <summary>
    /// Gets the recorded <see cref="LogAsync"/> calls in call order.
    /// </summary>
    /// <value>The recorded <see cref="LogAsync"/> calls in call order.</value>
    public List<LoggedEntry> LoggedEntries { get; } = new();

    /// <inheritdoc />
    public Task BeginSessionAsync(CancellationToken cancellationToken = default)
    {
        BeginSessionCallCount++;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        SetEnabledCalls.Add(enabled);
    }

    /// <inheritdoc />
    public Task LogAsync(string category, string message, string? details = null, string level = DebugLogLevel.Info, CancellationToken cancellationToken = default)
    {
        LoggedEntries.Add(new LoggedEntry(category, message, details, level));
        return Task.CompletedTask;
    }
}

/// <summary>
/// A recorded <see cref="IDebugLogService.LogAsync"/> call.
/// </summary>
/// <param name="Category">The entry category.</param>
/// <param name="Message">The entry message.</param>
/// <param name="Details">The optional details, or <see langword="null"/>.</param>
/// <param name="Level">The entry severity level.</param>
/// <returns>The recorded log entry.</returns>
public sealed record LoggedEntry(string Category, string Message, string? Details, string Level);
