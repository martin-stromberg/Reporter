// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Reflection;
using Xunit.v3;

namespace Reporter.Tests;

/// <summary>
/// Tests the machine-readable test classification used to separate the
/// deterministic suite from operating-system-sensitive tests.
/// </summary>
public sealed class TestClassificationConventionTests
{
    /// <summary>
    /// The OS-interface attribute produces the category trait used by local
    /// and CI test filters.
    /// </summary>
    [Fact]
    public void OsInterfaceAttribute_GetTraits_ReturnsOsInterfaceCategory()
    {
        var traits = new OsInterfaceAttribute().GetTraits();

        Assert.Contains(KeyValuePair.Create("Category", TestCategories.OsInterface), traits);
    }

    /// <summary>
    /// The flaky attribute produces the category trait reserved for known
    /// unstable OS-sensitive tests.
    /// </summary>
    [Fact]
    public void FlakyAttribute_GetTraits_ReturnsFlakyCategory()
    {
        var traits = new FlakyAttribute().GetTraits();

        Assert.Contains(KeyValuePair.Create("Category", TestCategories.Flaky), traits);
    }

    /// <summary>
    /// A flaky marker is only valid as an exception inside the OS-interface
    /// lane. It must never be used to hide deterministic test failures.
    /// </summary>
    [Fact]
    public void FlakyTests_AlwaysDeclareOsInterface()
    {
        var violations = typeof(TestClassificationConventionTests).Assembly
            .GetTypes()
            .SelectMany(type => type
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(method => (type, method)))
            .Where(test => IsTestMethod(test.method))
            .Where(test => HasCategory(test.type, TestCategories.Flaky) || HasCategory(test.method, TestCategories.Flaky))
            .Where(test => !HasCategory(test.type, TestCategories.OsInterface) && !HasCategory(test.method, TestCategories.OsInterface))
            .Select(test => $"{test.type.FullName}.{test.method.Name}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Flaky tests must also declare Category=OsInterface: " + string.Join(", ", violations));
    }

    private static bool IsTestMethod(MethodInfo method)
    {
        return method.GetCustomAttributes(inherit: true).Any(attribute => attribute is FactAttribute or TheoryAttribute);
    }

    private static bool HasCategory(MemberInfo member, string category)
    {
        return member.GetCustomAttributes(inherit: true)
            .OfType<ITraitAttribute>()
            .SelectMany(attribute => attribute.GetTraits())
            .Any(trait => trait.Key == "Category" && trait.Value == category);
    }
}

