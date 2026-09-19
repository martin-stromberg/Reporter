// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Transports the locally stored article image across the content store,
/// repositories and domain models as a single value object.
/// </summary>
/// <param name="Data">The image binary data.</param>
/// <param name="ContentType">The MIME type of the image, or <c>null</c> when unknown.</param>
/// <param name="Url">The origin URL the image was downloaded from, or <c>null</c> when unknown.</param>
/// <returns>A new <see cref="ItemImage"/> instance.</returns>
public record ItemImage(byte[] Data, string? ContentType, string? Url);
