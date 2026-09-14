// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Microsoft.Maui.ApplicationModel.Communication;
using Reporter.Core.Interfaces;

namespace Reporter.Services;

/// <summary>
/// Implements <see cref="IEmailService"/> via the system mail client exposed by
/// <see cref="Email"/>. Platform failures (for example no configured mail account)
/// are mapped to <c>false</c>.
/// </summary>
public class EmailService : IEmailService
{
    /// <inheritdoc />
    public bool IsSupported => Email.Default.IsComposeSupported;

    /// <inheritdoc />
    public async Task<bool> ComposeAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var message = new EmailMessage
            {
                Subject = subject,
                Body = body,
                BodyFormat = EmailBodyFormat.PlainText,
                To = new List<string> { recipient },
            };
            await Email.Default.ComposeAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"EmailService.ComposeAsync failed: {ex}");
            return false;
        }
    }
}
