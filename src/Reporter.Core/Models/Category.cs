// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Represents a feed category in the domain model.
/// </summary>
public class Category
{
    /// <summary>
    /// Gets the unique identifier of the category.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the name of the category.
    /// </summary>
    public required string Name { get; init; }
}
