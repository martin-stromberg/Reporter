// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Xml;

namespace Reporter.Core.Services;

/// <summary>
/// Provides feed sync error category constants and helpers.
/// </summary>
public static class FeedSyncErrorKind
{
    /// <summary>
    /// Category for a failed request to an unencrypted <c>http</c> feed URL that
    /// produced no HTTP response — e.g. blocked or refused cleartext traffic.
    /// </summary>
    public const string InsecureHttpBlocked = "InsecureHttpBlocked";

    /// <summary>
    /// Category for a feed server that answered with a non-success HTTP status code.
    /// </summary>
    public const string HttpStatus = "HttpStatus";

    /// <summary>
    /// Category for a network or connection failure without an HTTP response.
    /// </summary>
    public const string Network = "Network";

    /// <summary>
    /// Category for a feed document that could not be parsed.
    /// </summary>
    public const string Parse = "Parse";

    /// <summary>
    /// Category for any other failure.
    /// </summary>
    public const string Unknown = "Unknown";

    /// <summary>
    /// Maps a sync exception to a persisted error category. An
    /// <see cref="HttpRequestException"/> carrying a status code means the server
    /// answered and maps to <see cref="HttpStatus"/>; without a status code an
    /// <c>http</c> feed URL maps to <see cref="InsecureHttpBlocked"/> (blocked or
    /// refused cleartext) and any other URL maps to <see cref="Network"/>.
    /// </summary>
    /// <param name="ex">The exception thrown while synchronizing the feed.</param>
    /// <param name="feedUrl">The URL of the feed being synchronized.</param>
    /// <returns>The error category.</returns>
    public static string Classify(Exception ex, string feedUrl)
    {
        if (ex is HttpRequestException httpRequestException)
        {
            if (httpRequestException.StatusCode.HasValue)
            {
                return HttpStatus;
            }

            if (Uri.TryCreate(feedUrl, UriKind.Absolute, out var feedUri) &&
                feedUri.Scheme == Uri.UriSchemeHttp)
            {
                return InsecureHttpBlocked;
            }

            return Network;
        }

        if (ex is XmlException)
        {
            return Parse;
        }

        return Unknown;
    }
}
