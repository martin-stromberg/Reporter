// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Interfaces;

/// <summary>
/// Seeds the demo category and feed on the very first application start.
/// </summary>
public interface IDemoContentService
{
    /// <summary>
    /// Seeds the demo content when the application runs on a freshly created
    /// database. The call is a no-op on subsequent starts and when the seed
    /// is suppressed via <c>REPORTER_DISABLE_DEMO_SEED</c>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);
}
