// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="INotificationService"/> fake that records notification requests.
/// </summary>
public sealed class FakeNotificationService : INotificationService
{
    /// <summary>
    /// Gets the recorded <see cref="NotifyNewItemsAsync"/> calls in call order.
    /// </summary>
    /// <value>The recorded <see cref="NotifyNewItemsAsync"/> calls in call order.</value>
    public List<NotificationCall> Calls { get; } = new();

    /// <summary>
    /// Gets or sets the exception thrown by <see cref="NotifyNewItemsAsync"/> when set.
    /// </summary>
    /// <value>The exception thrown by <see cref="NotifyNewItemsAsync"/> when set.</value>
    public Exception? Exception { get; set; }

    /// <inheritdoc />
    public Task NotifyNewItemsAsync(Feed feed, IReadOnlyList<Item> newItems, CancellationToken cancellationToken = default)
    {
        Calls.Add(new NotificationCall(feed, newItems));
        return Exception is not null ? Task.FromException(Exception) : Task.CompletedTask;
    }
}

/// <summary>
/// A recorded <see cref="INotificationService.NotifyNewItemsAsync"/> call.
/// </summary>
/// <param name="Feed">The feed the notification belongs to.</param>
/// <param name="Items">The new items that triggered the notification.</param>
/// <returns>The recorded notification call.</returns>
public sealed record NotificationCall(Feed Feed, IReadOnlyList<Item> Items);
