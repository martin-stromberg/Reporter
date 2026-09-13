// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Services;

/// <summary>
/// Indicates that the feed search is unavailable because both the feed directory
/// and the client-side autodiscovery failed.
/// </summary>
public class FeedSearchUnavailableException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSearchUnavailableException"/> class.
    /// </summary>
    public FeedSearchUnavailableException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSearchUnavailableException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public FeedSearchUnavailableException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedSearchUnavailableException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public FeedSearchUnavailableException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
