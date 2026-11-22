// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Xunit.v3;

namespace Reporter.Tests;

/// <summary>
/// Marks a known unstable test. Flaky tests must also be marked with
/// <see cref="OsInterfaceAttribute"/>; deterministic failures must never be
/// hidden behind this category.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class FlakyAttribute : Attribute, ITraitAttribute
{
    /// <inheritdoc />
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
    {
        return [KeyValuePair.Create("Category", TestCategories.Flaky)];
    }
}

