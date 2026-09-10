namespace Reporter.Core.Models;

/// <summary>
/// Represents a feed category together with the number of assigned feeds.
/// </summary>
public class CategoryWithCount
{
    /// <summary>
    /// Gets the unique identifier of the category.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the name of the category.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the number of feeds assigned to this category.
    /// </summary>
    public required int FeedCount { get; init; }
}
