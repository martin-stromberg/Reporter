// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Reporter.Core.Models;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.Services;

/// <summary>
/// Validates a keyword text entered by the user: non-empty after trimming,
/// within the length limit and not already present in the target scope.
/// Shared by the global keyword list and the per-feed keyword list.
/// </summary>
public static class KeywordValidator
{
    /// <summary>
    /// The maximum allowed length of a keyword text.
    /// </summary>
    public const int MaxTextLength = 500;

    /// <summary>
    /// Validates the entered keyword text.
    /// </summary>
    /// <param name="text">The entered keyword text.</param>
    /// <param name="existing">The keywords already present in the target scope.</param>
    /// <param name="errorMessage">The localized validation error; empty when the text is valid.</param>
    /// <returns><see langword="true"/> when the text can be added; otherwise <see langword="false"/>.</returns>
    public static bool TryValidate(string? text, IEnumerable<Keyword> existing, out string errorMessage)
    {
        var trimmed = text?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            errorMessage = AppResources.ErrorKeywordEmpty;
            return false;
        }

        if (trimmed.Length > MaxTextLength)
        {
            errorMessage = AppResources.ErrorKeywordTooLong;
            return false;
        }

        if (existing.Any(k => string.Equals(k.KeywordText, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = AppResources.ErrorKeywordDuplicate;
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
