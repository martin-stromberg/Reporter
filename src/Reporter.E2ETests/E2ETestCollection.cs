// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.E2ETests;

/// <summary>
/// xunit collection definition that shares one <see cref="ReporterAppFixture"/>
/// (app process, stub server, temp database) across all E2E tests. Tests within a
/// collection never run in parallel, so the suite executes serially against the
/// single app instance. No test ordering is enforced — each test owns its data.
/// </summary>
[CollectionDefinition(CollectionName)]
public sealed class E2ETestCollection : ICollectionFixture<ReporterAppFixture>
{
    /// <summary>
    /// The xunit collection name referenced by <c>[Collection]</c> attributes.
    /// </summary>
    public const string CollectionName = "E2E";
}
