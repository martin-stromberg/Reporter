namespace Reporter.Core.Models;

/// <summary>
/// Represents a keyword for filtering or tagging in the domain model.
/// </summary>
public class Keyword
{
    /// <summary>
    /// Gets the unique identifier of the keyword.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the keyword text.
    /// </summary>
    public required string KeywordText { get; init; }
}
