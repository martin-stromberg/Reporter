// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;

namespace Reporter.Core.Interfaces;

/// <summary>
/// Decides which newly stored items trigger a local notification and delegates
/// the display to <see cref="ILocalNotificationService"/>.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Evaluates the configured notification rules (global switch, per-feed switch,
    /// quiet hours and keyword filters) for the items stored during a feed sync
    /// and shows notifications for the remaining items.
    /// </summary>
    /// <param name="feed">The feed whose items were synchronized.</param>
    /// <param name="newItems">The items that were newly stored during the sync.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyNewItemsAsync(Feed feed, IReadOnlyList<Item> newItems, CancellationToken cancellationToken = default);
}
