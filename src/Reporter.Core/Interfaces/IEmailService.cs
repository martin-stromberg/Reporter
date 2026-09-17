// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Abstracts the platform facility for composing e-mail in the system mail client
/// (for example <c>Microsoft.Maui.ApplicationModel.Communication.Email</c>), so that
/// core services can hand over a pre-filled message without a MAUI dependency.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Gets a value indicating whether the current platform supports composing e-mail.
    /// </summary>
    /// <value><c>true</c> when the platform can open a mail compose window; otherwise <c>false</c>.</value>
    bool IsSupported { get; }

    /// <summary>
    /// Opens the system mail client with a pre-filled e-mail draft. The user reviews
    /// and sends the message themselves.
    /// </summary>
    /// <param name="recipient">The recipient e-mail address.</param>
    /// <param name="subject">The e-mail subject.</param>
    /// <param name="body">The plain-text e-mail body.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> when the compose window was opened, and <c>false</c> when composing is not supported or failed.</returns>
    Task<bool> ComposeAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}
