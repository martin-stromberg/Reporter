// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Net;
using System.Net.Http.Headers;

namespace Reporter.Tests;

/// <summary>
/// Builds <see cref="HttpResponseMessage"/> instances for tests that stub HTTP
/// downloads through <see cref="FakeHttpMessageHandler"/>.
/// </summary>
public static class TestHttpResponses
{
    /// <summary>
    /// Builds a <c>200 OK</c> response that carries <paramref name="data"/> as
    /// an <c>image/png</c> body.
    /// </summary>
    /// <param name="data">The image bytes to return.</param>
    /// <returns>The HTTP response message.</returns>
    public static HttpResponseMessage Png(byte[] data)
    {
        var content = new ByteArrayContent(data);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }
}
