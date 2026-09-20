// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

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

    /// <summary>
    /// Gets the optional feed this keyword is scoped to; <c>null</c> marks a global keyword.
    /// </summary>
    public Guid? FeedId { get; init; }
}
