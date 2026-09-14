// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Collects the debug report data (application and device information, settings,
/// feed health, sync history and the session debug log) and hands it over to the
/// system mail client via <see cref="IEmailService"/>.
/// </summary>
public interface IDebugReportService
{
    /// <summary>
    /// Gets a value indicating whether sending a debug report is supported on the
    /// current platform (a mail client is available).
    /// </summary>
    /// <value><c>true</c> when a debug report can be handed to the system mail client; otherwise <c>false</c>.</value>
    bool IsSupported { get; }

    /// <summary>
    /// Collects the debug report and opens the system mail client with a pre-filled draft.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> when the compose window was opened, and <c>false</c> when composing is not supported or failed.</returns>
    Task<bool> SendReportAsync(CancellationToken cancellationToken = default);
}
