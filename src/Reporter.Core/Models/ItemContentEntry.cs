// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Transports a single item content for batch write operations on the
/// content store (<see cref="Interfaces.IItemContentStore.SetRangeAsync"/>).
/// </summary>
/// <param name="ItemId">The identifier of the item the content belongs to.</param>
/// <param name="ContentHtml">The HTML content; a <c>null</c> or empty value removes a stored entry only when <paramref name="Image"/> is <c>null</c> — with an image the stored content stays untouched.</param>
/// <param name="Image">The optional article image to store, or <c>null</c> to leave a stored image untouched.</param>
/// <returns>A new <see cref="ItemContentEntry"/> instance.</returns>
public record ItemContentEntry(Guid ItemId, string? ContentHtml, ItemImage? Image = null);
