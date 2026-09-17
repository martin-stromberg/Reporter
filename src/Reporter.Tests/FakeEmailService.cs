// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="IEmailService"/> fake that records composed e-mails.
/// </summary>
public sealed class FakeEmailService : IEmailService
{
    /// <summary>
    /// Gets or sets a value indicating whether the fake reports platform support for e-mail compose.
    /// </summary>
    /// <value>The value returned by <see cref="IsSupported"/>.</value>
    public bool IsSupported { get; set; } = true;

    /// <summary>
    /// Gets or sets the value returned by <see cref="ComposeAsync"/>.
    /// </summary>
    /// <value>The value returned by <see cref="ComposeAsync"/>.</value>
    public bool ComposeResult { get; set; } = true;

    /// <summary>
    /// Gets or sets the exception thrown by <see cref="ComposeAsync"/>, or <see langword="null"/> to not throw.
    /// </summary>
    /// <value>The exception thrown by <see cref="ComposeAsync"/>.</value>
    public Exception? ComposeException { get; set; }

    /// <summary>
    /// Gets the recorded <see cref="ComposeAsync"/> calls in call order.
    /// </summary>
    /// <value>The recorded <see cref="ComposeAsync"/> calls in call order.</value>
    public List<ComposedEmail> ComposedEmails { get; } = new();

    /// <summary>
    /// Gets the number of recorded <see cref="ComposeAsync"/> calls.
    /// </summary>
    /// <value>The number of recorded <see cref="ComposeAsync"/> calls.</value>
    public int ComposeCallCount => ComposedEmails.Count;

    /// <inheritdoc />
    public Task<bool> ComposeAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        ComposedEmails.Add(new ComposedEmail(recipient, subject, body));
        if (ComposeException is not null)
        {
            throw ComposeException;
        }

        return Task.FromResult(ComposeResult);
    }
}

/// <summary>
/// A recorded <see cref="IEmailService.ComposeAsync"/> call.
/// </summary>
/// <param name="Recipient">The recipient address.</param>
/// <param name="Subject">The e-mail subject.</param>
/// <param name="Body">The plain-text e-mail body.</param>
/// <returns>The recorded e-mail compose call.</returns>
public sealed record ComposedEmail(string Recipient, string Subject, string Body);
