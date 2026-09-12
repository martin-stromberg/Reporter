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
}
