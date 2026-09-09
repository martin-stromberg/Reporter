namespace Reporter.Data.Entities;

/// <summary>
/// Represents a feed category.
/// </summary>
public class Category
{
    /// <summary>
    /// Gets or sets the unique identifier of the category.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
