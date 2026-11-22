// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.E2ETests;

/// <summary>
/// Central names for xUnit category traits used by the test suite.
/// </summary>
internal static class TestCategories
{
    /// <summary>The desktop end-to-end suite.</summary>
    public const string E2E = "E2E";

    /// <summary>Tests that interact with real operating system resources.</summary>
    public const string OsInterface = "OsInterface";

    /// <summary>The explicit exception for unstable OS-sensitive tests.</summary>
    public const string Flaky = "Flaky";
}
