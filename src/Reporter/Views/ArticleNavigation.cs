// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Diagnostics;
using Reporter.Core.Models;

namespace Reporter.Views;

/// <summary>
/// Shared navigation to the article detail page for the start page views.
/// </summary>
internal static class ArticleNavigation
{
    /// <summary>
    /// Opens the article detail page for the given item.
    /// </summary>
    /// <param name="item">The article to open; <c>null</c> is ignored.</param>
    /// <returns>A task that represents the asynchronous navigation.</returns>
    public static async Task OpenAsync(ItemListItem? item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync($"articledetail?itemId={item.Id}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ArticleNavigation.OpenAsync failed: {ex}");
        }
    }
}
