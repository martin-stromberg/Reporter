// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Reporter.Core.Interfaces;
using Reporter.Core.Models;
using Reporter.Core.Services;
using Reporter.Core.Resources.Strings;

namespace Reporter.Core.ViewModels;

/// <summary>
/// Feed search part of <see cref="FeedsViewModel"/>: resolves the entered input,
/// runs the feed search and manages the results view including subscribe and close.
/// </summary>
public partial class FeedsViewModel
{
    private readonly IFeedSearchService _feedSearchService;

    private string _searchErrorMessage = string.Empty;
    private bool _isSearching;
    private bool _showSearchResults;
    private ObservableCollection<FeedSearchResult> _searchResults = [];

    /// <summary>
    /// Gets the command that searches the feed directory and the entered website for feeds.
    /// </summary>
    public AsyncRelayCommand SearchCommand { get; }

    /// <summary>
    /// Gets the command that persists the entered URL directly without searching —
    /// unlike <see cref="SearchCommand"/> it stays available while offline.
    /// </summary>
    public AsyncRelayCommand DirectAddCommand { get; }

    /// <summary>
    /// Gets the command that subscribes to a feed search result.
    /// </summary>
    public AsyncRelayCommand<FeedSearchResult?> SubscribeResultCommand { get; }

    /// <summary>
    /// Gets the command that leaves the search results view and returns to the feed list.
    /// </summary>
    public RelayCommand CloseSearchResultsCommand { get; }

    /// <summary>
    /// Gets or sets the feed search results shown in the results list.
    /// </summary>
    public ObservableCollection<FeedSearchResult> SearchResults
    {
        get => _searchResults;
        set => SetProperty(ref _searchResults, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the search results view is visible.
    /// Stays <see langword="true"/> for an empty result so the empty view can be shown.
    /// </summary>
    public bool ShowSearchResults
    {
        get => _showSearchResults;
        set => SetProperty(ref _showSearchResults, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether a feed search is in progress.
    /// </summary>
    public bool IsSearching
    {
        get => _isSearching;
        set
        {
            if (SetProperty(ref _isSearching, value))
            {
                SearchCommand.NotifyCanExecuteChanged();
                DirectAddCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the current search error message, shown above the feed list.
    /// </summary>
    public string SearchErrorMessage
    {
        get => _searchErrorMessage;
        set
        {
            if (SetProperty(ref _searchErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasSearchError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a search error message is present.
    /// </summary>
    public bool HasSearchError => _searchErrorMessage.Length > 0;

    /// <summary>
    /// Gets or sets the callback that asks the user whether an entered URL should be
    /// added directly when the search found nothing or is unavailable.
    /// Wired up by the page code-behind so the view model stays free of UI dependencies.
    /// </summary>
    public Func<string, Task<bool>>? ConfirmDirectAddAsync { get; set; }

    private async Task SearchAsync()
    {
        var input = NewUrl.Trim();
        if (input.Length == 0)
        {
            return;
        }

        // Searching replaces the add form with the results view; in edit mode
        // that would silently discard an in-progress edit (e.g. via the entry's
        // ReturnCommand), so the search only runs from add mode.
        if (!IsOnline || IsEditMode)
        {
            return;
        }

        if (!TryResolveSearchUrl(input, out var searchUrl, out var isDirectUrl))
        {
            SearchResults.Clear();
            ShowSearchResults = true;
            ShowAddForm = false;
            return;
        }

        IsSearching = true;
        SearchErrorMessage = string.Empty;
        SearchResults.Clear();
        ShowSearchResults = false;

        var offerDirectAdd = false;
        try
        {
            var results = await _feedSearchService.SearchAsync(searchUrl);
            if (IsStaleInput(input))
            {
                return;
            }

            foreach (var result in results)
            {
                SearchResults.Add(result);
            }

            ShowSearchResults = true;
            ShowAddForm = false;
            offerDirectAdd = results.Count == 0 && isDirectUrl;
        }
        catch (Exception ex)
        {
            offerDirectAdd = HandleSearchFailure(ex, input, isDirectUrl);
        }
        finally
        {
            IsSearching = false;
        }

        // The confirmation runs outside the try/catch so a failing dialog is
        // neither re-invoked nor reported as a search failure.
        if (offerDirectAdd)
        {
            await OfferDirectAddAsync(input);
        }
    }

    // Reports a failed search on the search error channel, unless the input was
    // edited in the meantime — a stale failure of the abandoned query is
    // discarded like stale results. Returns whether the direct-add confirmation
    // should be offered for the entered URL.
    private bool HandleSearchFailure(Exception ex, string input, bool isDirectUrl)
    {
        if (ex is not FeedSearchUnavailableException)
        {
            Debug.WriteLine($"SearchAsync failed: {ex}");
        }

        if (IsStaleInput(input))
        {
            return false;
        }

        SearchErrorMessage = isDirectUrl
            ? AppResources.FeedSearchUnavailable
            : AppResources.FeedSearchUnavailableRetry;
        SearchResults.Clear();
        ShowSearchResults = false;
        return isDirectUrl;
    }

    // The URL may be edited while a search is in flight — late results or errors
    // of the abandoned query must not resurface, so both outcomes are discarded.
    private bool IsStaleInput(string input)
    {
        return !string.Equals(NewUrl.Trim(), input, StringComparison.Ordinal);
    }

    private static bool TryResolveSearchUrl(string input, out string searchUrl, out bool isDirectUrl)
    {
        if (IsValidFeedUrl(input))
        {
            searchUrl = input;
            isDirectUrl = true;
            return true;
        }

        isDirectUrl = false;
        return TryNormalizeDomainUrl(input, out searchUrl);
    }

    private static bool TryNormalizeDomainUrl(string input, out string normalizedUrl)
    {
        normalizedUrl = string.Empty;

        if (input.Contains("://", StringComparison.Ordinal) || input.Any(char.IsWhiteSpace))
        {
            return false;
        }

        if (!Uri.TryCreate("https://" + input, UriKind.Absolute, out var uri) ||
            !uri.Host.Contains('.', StringComparison.Ordinal))
        {
            return false;
        }

        normalizedUrl = "https://" + input;
        return true;
    }

    private async Task OfferDirectAddAsync(string input)
    {
        if (ConfirmDirectAddAsync is null)
        {
            return;
        }

        bool confirmed;
        try
        {
            confirmed = await ConfirmDirectAddAsync(input);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ConfirmDirectAddAsync failed: {ex}");
            return;
        }

        if (!confirmed)
        {
            SearchResults.Clear();
            ShowSearchResults = false;
            return;
        }

        if (!await TryPersistNewFeedAsync(input, FeedTitleFallback.GetFallbackTitle(input)))
        {
            // The sheet stays open so the duplicate error is the only visible
            // message in its hint block and the user can correct the URL; the
            // results view closes so the sheet reopens above the feed list.
            SearchResults.Clear();
            ShowSearchResults = false;
            ShowAddForm = true;
            return;
        }

        await FinishAddFlowAsync();
    }

    private async Task DirectAddAsync()
    {
        // Same precondition as SearchAsync: adding while an edit is in progress
        // would create a new feed from the edit form's URL and silently discard
        // the edit, so the direct add only runs from add mode.
        if (IsEditMode)
        {
            return;
        }

        var input = NewUrl.Trim();
        if (input.Length == 0 || !TryResolveSearchUrl(input, out var url, out _))
        {
            ErrorMessage = AppResources.ErrorFeedUrlInvalid;
            return;
        }

        if (await TryPersistNewFeedAsync(url, FeedTitleFallback.GetFallbackTitle(url)))
        {
            await FinishAddFlowAsync();
        }
    }

    // Shared persist step of the add flows: rejects duplicates and stores the
    // feed with the fixed defaults. Both error channels are cleared so a stale
    // search error never stays next to the duplicate message in the sheet.
    private async Task<bool> TryPersistNewFeedAsync(string url, string title)
    {
        var existing = await _feedRepository.GetByUrlAsync(url);
        if (existing is not null)
        {
            ErrorMessage = AppResources.ErrorFeedDuplicate;
            SearchErrorMessage = string.Empty;
            return false;
        }

        ErrorMessage = string.Empty;
        SearchErrorMessage = string.Empty;

        await _feedRepository.AddAsync(new Feed
        {
            Id = Guid.NewGuid(),
            Url = url,
            Title = title,
            CategoryId = null,
            LastCheckedAt = null,
            HealthStatus = FeedHealth.Ok,
            HealthLastChange = null,
            NotificationsEnabled = true,
        });
        return true;
    }

    // Shared completion of every successful add flow: leaves the search results
    // view, resets the sheet and reloads the feed list.
    private async Task FinishAddFlowAsync()
    {
        SearchResults.Clear();
        ShowSearchResults = false;
        ResetForm();
        await LoadAsync();
    }

    private void CloseSearchResults()
    {
        SearchResults.Clear();
        ShowSearchResults = false;
    }

    private async Task SubscribeResultAsync(FeedSearchResult? result)
    {
        if (result is null)
        {
            return;
        }

        var title = !string.IsNullOrWhiteSpace(result.Title)
            ? result.Title.Trim()
            : FeedTitleFallback.GetFallbackTitle(result.FeedUrl);

        if (!await TryPersistNewFeedAsync(result.FeedUrl, title))
        {
            return;
        }

        await FinishAddFlowAsync();
    }
}
