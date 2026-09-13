// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="HttpMessageHandler"/> fake that produces responses
/// through a configurable factory.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeHttpMessageHandler"/> class.
    /// </summary>
    /// <param name="responseFactory">The factory used to create responses.</param>
    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_responseFactory(request));
    }
}
