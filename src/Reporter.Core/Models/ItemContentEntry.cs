// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Transports a single item content for batch write operations on the
/// content store (<see cref="Interfaces.IItemContentStore.SetRangeAsync"/>).
/// </summary>
/// <param name="ItemId">The identifier of the item the content belongs to.</param>
/// <param name="ContentHtml">The HTML content, or <c>null</c> to remove a stored entry.</param>
/// <returns>A new <see cref="ItemContentEntry"/> instance.</returns>
public record ItemContentEntry(Guid ItemId, string? ContentHtml);
