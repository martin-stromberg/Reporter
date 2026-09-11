namespace Reporter.Core.ViewModels;

/// <summary>
/// Represents a selectable auto-mark-as-read delay option.
/// </summary>
public class AutoMarkReadDelayOption
{
    /// <summary>
    /// Gets the delay in seconds.
    /// </summary>
    public required int Seconds { get; init; }

    /// <summary>
    /// Gets the localized display label.
    /// </summary>
    public required string Label { get; init; }
}
