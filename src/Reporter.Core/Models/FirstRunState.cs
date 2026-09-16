// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Carries the first-run detection result captured in <c>MauiProgram</c> into
/// the service container: the database file did not exist before this start.
/// </summary>
public class FirstRunState
{
    /// <summary>
    /// Gets a value indicating whether the application database was freshly
    /// created by this start (the file did not exist beforehand).
    /// </summary>
    public bool IsFirstRun { get; init; }

    /// <summary>
    /// Gets a value indicating whether the demo content seed is suppressed,
    /// for example by the <c>REPORTER_DISABLE_DEMO_SEED</c> environment
    /// variable in E2E or CI runs.
    /// </summary>
    public bool DemoSeedSuppressed { get; init; }

    /// <summary>
    /// Gets a value indicating whether the demo content should be seeded
    /// during this start.
    /// </summary>
    public bool ShouldSeedDemoContent => IsFirstRun && !DemoSeedSuppressed;
}
