// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Interfaces;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="IBackupExclusionService"/> fake that records the
/// excluded paths. The real implementation only acts on iOS.
/// </summary>
public sealed class FakeBackupExclusionService : IBackupExclusionService
{
    /// <summary>
    /// Gets the recorded <see cref="ExcludeFromBackup"/> paths in call order.
    /// </summary>
    /// <value>The recorded file paths.</value>
    public List<string> ExcludedPaths { get; } = new();

    /// <inheritdoc />
    public void ExcludeFromBackup(string filePath)
    {
        ExcludedPaths.Add(filePath);
    }
}
