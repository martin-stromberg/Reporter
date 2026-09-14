// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Net;
using System.Xml;
using Reporter.Core.Services;

namespace Reporter.Tests;

/// <summary>
/// Contains tests for the <see cref="FeedSyncErrorKind"/> classification helper.
/// </summary>
public class FeedSyncErrorKindTests
{
    /// <summary>
    /// Verifies that an <see cref="HttpRequestException"/> without a status code
    /// against an <c>http</c> feed URL maps to
    /// <see cref="FeedSyncErrorKind.InsecureHttpBlocked"/>.
    /// </summary>
    [Fact]
    public void Classify_HttpRequestExceptionOnHttpUrl_ReturnsInsecureHttpBlocked()
    {
        var result = FeedSyncErrorKind.Classify(new HttpRequestException("blocked"), "http://example.com/feed");

        Assert.Equal(FeedSyncErrorKind.InsecureHttpBlocked, result);
    }

    /// <summary>
    /// Verifies that an <see cref="HttpRequestException"/> carrying a status code
    /// on an <c>https</c> feed URL maps to <see cref="FeedSyncErrorKind.HttpStatus"/>.
    /// </summary>
    [Fact]
    public void Classify_HttpRequestExceptionWithStatusCode_ReturnsHttpStatus()
    {
        var result = FeedSyncErrorKind.Classify(
            new HttpRequestException("not found", null, HttpStatusCode.NotFound),
            "https://example.com/feed");

        Assert.Equal(FeedSyncErrorKind.HttpStatus, result);
    }

    /// <summary>
    /// Verifies that an <see cref="HttpRequestException"/> carrying a status code
    /// on an <c>http</c> feed URL also maps to <see cref="FeedSyncErrorKind.HttpStatus"/>:
    /// the server answered, so the request was not blocked client-side.
    /// </summary>
    [Fact]
    public void Classify_HttpRequestExceptionWithStatusCodeOnHttpUrl_ReturnsHttpStatus()
    {
        var result = FeedSyncErrorKind.Classify(
            new HttpRequestException("not found", null, HttpStatusCode.NotFound),
            "http://example.com/feed");

        Assert.Equal(FeedSyncErrorKind.HttpStatus, result);
    }

    /// <summary>
    /// Verifies that an <see cref="HttpRequestException"/> without a status code
    /// on an <c>https</c> feed URL maps to <see cref="FeedSyncErrorKind.Network"/>.
    /// </summary>
    [Fact]
    public void Classify_HttpRequestExceptionWithoutStatusCode_ReturnsNetwork()
    {
        var result = FeedSyncErrorKind.Classify(new HttpRequestException("no route"), "https://example.com/feed");

        Assert.Equal(FeedSyncErrorKind.Network, result);
    }

    /// <summary>
    /// Verifies that an <see cref="XmlException"/> maps to
    /// <see cref="FeedSyncErrorKind.Parse"/>.
    /// </summary>
    [Fact]
    public void Classify_XmlException_ReturnsParse()
    {
        var result = FeedSyncErrorKind.Classify(new XmlException("invalid document"), "https://example.com/feed");

        Assert.Equal(FeedSyncErrorKind.Parse, result);
    }

    /// <summary>
    /// Verifies that any other exception maps to <see cref="FeedSyncErrorKind.Unknown"/>.
    /// </summary>
    [Fact]
    public void Classify_OtherException_ReturnsUnknown()
    {
        var result = FeedSyncErrorKind.Classify(new InvalidOperationException("boom"), "https://example.com/feed");

        Assert.Equal(FeedSyncErrorKind.Unknown, result);
    }
}
