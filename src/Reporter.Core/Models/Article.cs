namespace Reporter.Core.Models;

/// <summary>
/// Represents a feed article.
/// </summary>
public class Article
{
    /// <summary>
    /// Gets the unique identifier of the article.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the title of the article.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets a value indicating whether the article has been read.
    /// </summary>
    public required bool IsRead { get; init; }
}
