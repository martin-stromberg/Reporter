// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Describes a reading packet of unread items: the anchor calendar day plus the
/// optional next occupied day added when the anchor day holds fewer articles
/// than the packet threshold.
/// </summary>
public record UnreadPacket
{
    /// <summary>
    /// Gets the inclusive UTC lower bound of the packet's publication window;
    /// <see langword="null"/> together with <see cref="ToUtc"/> marks a packet
    /// that covers undated items only.
    /// </summary>
    public required DateTime? FromUtc { get; init; }

    /// <summary>
    /// Gets the exclusive UTC upper bound of the packet's publication window;
    /// <see langword="null"/> together with <see cref="FromUtc"/> marks a packet
    /// that covers undated items only.
    /// </summary>
    public required DateTime? ToUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether items without a publication date
    /// (the pseudo day) belong to the packet.
    /// </summary>
    public required bool IncludeUndated { get; init; }

    /// <summary>
    /// Gets the local calendar days covered by the packet — element 0 is the
    /// anchor day, element 1 the follow day after a below-threshold expansion;
    /// a <see langword="null"/> entry stands for the undated pseudo day.
    /// </summary>
    public required IReadOnlyList<DateOnly?> Days { get; init; }
}
