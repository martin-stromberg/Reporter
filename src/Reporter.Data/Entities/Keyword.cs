// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Data.Entities;

/// <summary>
/// Represents a keyword for filtering or tagging.
/// </summary>
public class Keyword
{
    /// <summary>
    /// Gets or sets the unique identifier of the keyword.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the keyword text.
    /// </summary>
    public string KeywordText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional feed this keyword is scoped to; <c>null</c> marks a global keyword.
    /// </summary>
    public Guid? FeedId { get; set; }

    /// <summary>
    /// Gets or sets the navigation to the associated feed.
    /// </summary>
    public Feed? Feed { get; set; }
}
