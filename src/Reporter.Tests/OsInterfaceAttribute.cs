// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Xunit.v3;

namespace Reporter.Tests;

/// <summary>
/// Marks a test whose assertion or reliable teardown depends on real operating
/// system resources.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class OsInterfaceAttribute : Attribute, ITraitAttribute
{
    /// <inheritdoc />
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        return [KeyValuePair.Create("Category", TestCategories.OsInterface)];
    }
}

