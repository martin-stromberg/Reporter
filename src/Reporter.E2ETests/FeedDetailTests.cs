// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Reporter.Core.Resources.Strings;

namespace Reporter.E2ETests;

/// <summary>
/// FlaUI tests for the feed detail page: tapping a feed card on the feed list
/// navigates to the paged article list of that feed, and the feed-level
/// actions (refresh, rename, category, edit, error details, delete) live in
/// the detail page's action sheet. Like <see cref="SmokeTests"/> the app under
/// test is fully real; only the network is stubbed via <see cref="StubFeedServer"/>.
/// All tests share one app instance through <see cref="E2ETestCollection"/> and
/// therefore run serially.
/// </summary>
[Collection(E2ETestCollection.CollectionName)]
public sealed class FeedDetailTests : IDisposable
{
    private readonly ReporterAppFixture _fixture;
    private readonly E2EPageHelpers _page;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeedDetailTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared app/stub-server fixture.</param>
    public FeedDetailTests(ReporterAppFixture fixture)
    {
        _fixture = fixture;
        _page = new E2EPageHelpers(fixture);

        // Feeds left behind by earlier suite classes accumulate in the shared
        // database and grow the feed list beyond the viewport — the scroll
        // scan in WaitForFeedCard cannot reach a virtualized-away card
        // reliably, so every test starts from an empty feed list.
        FeedDbAssertions.DeleteAllFeedsAsync(_fixture.DatabasePath).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Resets the UI after every test — including failed ones, whose leftover
    /// popups would otherwise cascade into the next test.
    /// </summary>
    public void Dispose()
    {
        try
        {
            ResetUiState();
        }
        catch (Exception)
        {
            // A broken app is already reported by the failing test itself; the
            // cleanup must not mask that failure.
        }
    }

    // The window is re-resolved for every call: UIA proxies can go stale when
    // dialogs and popups come and go.
    private Window Window => _fixture.GetMainWindow();

    // Triggers a full sync on the Unread tab so the freshly added feed downloads
    // its articles, then returns to the Feeds tab.
    private void SyncAllViaUnreadTab()
    {
        _page.SelectTab(AppResources.TabUnread);
        var refresh = _page.WaitForElementByName(AppResources.ButtonRefresh);
        UiRetry.InvokeOrClick(refresh);
        _page.SelectTab(AppResources.TabFeeds);
    }

    // Creates a category on the Categories tab via the UI (entry + save button)
    // and waits for its card before returning to the Feeds tab.
    private void AddCategoryViaUi(string categoryName)
    {
        _page.SelectTab(AppResources.TabCategories);
        var entry = UiRetry.WaitForElementByName(Window, AppResources.LabelCategoryName, ControlType.Edit);
        UiRetry.SetText(entry, categoryName);
        var save = _page.WaitForElementByName(AppResources.ButtonSave, ControlType.Button);
        UiRetry.InvokeOrClick(save);
        _page.WaitForCard(categoryName);
        _page.SelectTab(AppResources.TabFeeds);
    }

    // Whether the feed card currently shows the given category name. The card is
    // re-resolved on every poll because it is re-rendered on model changes.
    private bool CardShowsCategory(string feedTitle, string categoryName)
        => _page.WaitForFeedCard(feedTitle).FindFirstDescendant(cf => cf.ByName(categoryName)) is not null;

    // Best-effort UI reset between tests: dismiss open popups, dialogs and
    // sheets, then land on the Feeds tab (SelectTab pops a pushed detail page
    // via its back button).
    private void ResetUiState()
    {
        _page.DismissPopups();

        // The tests add one to two feeds each; removing them again keeps the
        // shared feed list short so the next test's card renders inside the
        // viewport instead of beyond the reach of the scroll scan.
        FeedDbAssertions.DeleteAllFeedsAsync(_fixture.DatabasePath).GetAwaiter().GetResult();

        // The paging/search fixtures add dozens of articles to the shared
        // database; marking them read keeps the unread list short enough for
        // the other E2E tests to still find their articles.
        FeedDbAssertions.MarkAllItemsReadAsync(_fixture.DatabasePath).GetAwaiter().GetResult();

        _page.SelectTab(AppResources.TabFeeds);
    }

    // Whether any Text element in the app's UI scope carries the given text as
    // part of its name — needed for alert messages whose UIA name is the whole
    // multi-line body, so an exact ByName match cannot find the embedded
    // localized error kind.
    private bool ScopedTextContains(string text)
    {
        try
        {
            var desktop = _fixture.Automation.GetDesktop();
            foreach (var topLevel in desktop.FindAllChildren(cf => cf.ByProcessId(_fixture.App.ProcessId)))
            {
                foreach (var element in topLevel.FindAllDescendants(cf => cf.ByControlType(ControlType.Text)))
                {
                    if (element.Name?.Contains(text, StringComparison.Ordinal) == true)
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception)
        {
            // Transient UIA tree unavailability while dialogs open or close.
        }

        return false;
    }

    /// <summary>
    /// Tapping a feed card navigates to the feed detail page and shows the
    /// feed's articles instead of the old action sheet.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public void FeedDetail_Navigation_ShowsFeedItems()
    {
        _page.AddFeedViaUi("nav-target");
        SyncAllViaUnreadTab();

        _page.OpenFeedDetail("Stub Feed nav-target");

        _page.WaitForCard("nav-target article", TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// The title search on the detail page filters the article list: the
    /// matching article is found and a previously rendered, non-matching
    /// article disappears.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public void FeedDetail_Search_FiltersItems()
    {
        _page.AddFeedViaUi("paged-feed");
        SyncAllViaUnreadTab();
        _page.OpenFeedDetail("Stub Feed paged-feed");
        _page.WaitForCard("Paged article 25", TimeSpan.FromSeconds(10));
        _page.WaitForCard("Paged article 24", TimeSpan.FromSeconds(10));

        var searchBar = UiRetry.WaitForElement(
            Window,
            cf => cf.ByAutomationId("FeedDetailSearchBar").Or(cf.ByName(AppResources.PlaceholderFeedDetailSearch)),
            description: "feed detail search bar");
        UiRetry.SetText(searchBar, "Paged article 25");

        _page.WaitForCard("Paged article 25", TimeSpan.FromSeconds(10));
        Assert.True(
            UiRetry.WaitFor(
                () => UiRetry.TryFindElementByName(Window, "Paged article 24", timeout: TimeSpan.FromMilliseconds(500)) is null,
                TimeSpan.FromSeconds(5)),
            "Non-matching articles are still visible after filtering.");
    }

    /// <summary>
    /// The detail action sheet offers "Rename"; the prompt accepts a new title
    /// and both the detail header and the feed card show it afterwards.
    /// Replaces the former <c>SmokeTests.FeedActionSheet_Rename_UpdatesTitle</c>.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public void FeedDetail_Rename_UpdatesTitle()
    {
        _page.AddFeedViaUi("rename-target");
        _page.OpenFeedDetail("rename-target.xml");
        _page.OpenFeedDetailActions(AppResources.ButtonRename);

        var rename = _page.WaitForElementInScopeByName(AppResources.ButtonRename);
        UiRetry.InvokeOrClick(rename);

        var promptEntry = UiRetry.WaitForElementInScope(
            _fixture.App,
            _fixture.Automation,
            cf => cf.ByControlType(ControlType.Edit),
            description: "rename prompt text box");
        UiRetry.SetText(promptEntry, "Detail Renamed Feed");

        var ok = _page.WaitForElementInScopeByName(AppResources.ButtonOk, ControlType.Button);
        UiRetry.InvokeOrClick(ok);

        _page.WaitForElementByName("Detail Renamed Feed");

        _page.NavigateBackToFeedList();
        _page.WaitForFeedCard("Detail Renamed Feed");
    }

    /// <summary>
    /// The detail action sheet offers "Change Category": assigning a real
    /// category shows its name on the feed card, and choosing the built-in
    /// "None" entry clears the assignment again.
    /// Replaces the former <c>SmokeTests.FeedActionSheet_ChangeCategory_IncludingNone</c>.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public void FeedDetail_ChangeCategory_IncludingNone()
    {
        const string categoryName = "E2E Detail Category";
        AddCategoryViaUi(categoryName);
        var title = _page.AddFeedViaUi("category-target");
        _page.OpenFeedDetail(title);
        _page.OpenFeedDetailActions(AppResources.ButtonChangeCategory);

        var changeCategory = _page.WaitForElementInScopeByName(AppResources.ButtonChangeCategory);
        UiRetry.InvokeOrClick(changeCategory);
        var option = _page.WaitForElementInScopeByName(categoryName);
        UiRetry.InvokeOrClick(option);

        _page.NavigateBackToFeedList();
        Assert.True(
            UiRetry.WaitFor(() => CardShowsCategory(title, categoryName), TimeSpan.FromSeconds(5)),
            $"The card '{title}' does not show the assigned category '{categoryName}'.");

        // Second direction: the "None" pseudo entry clears the assignment again.
        _page.OpenFeedDetail(title);
        _page.OpenFeedDetailActions(AppResources.ButtonChangeCategory);
        var changeCategoryAgain = _page.WaitForElementInScopeByName(AppResources.ButtonChangeCategory);
        UiRetry.InvokeOrClick(changeCategoryAgain);
        var none = _page.WaitForElementInScopeByName(AppResources.CategoryNone);
        UiRetry.InvokeOrClick(none);

        _page.NavigateBackToFeedList();
        Assert.True(
            UiRetry.WaitFor(() => !CardShowsCategory(title, categoryName), TimeSpan.FromSeconds(5)),
            $"The card '{title}' still shows the category '{categoryName}' after clearing it.");
    }

    /// <summary>
    /// The detail action sheet offers "Delete"; confirming the alert removes
    /// the feed and returns to the feed list.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public void FeedDetail_Delete_ReturnsToList()
    {
        var title = _page.AddFeedViaUi("delete-target");
        _page.OpenFeedDetail(title);
        _page.OpenFeedDetailActions(AppResources.ButtonDelete);

        var delete = _page.WaitForElementInScopeByName(AppResources.ButtonDelete);
        UiRetry.InvokeOrClick(delete);

        var yes = _page.WaitForElementInScopeByName(AppResources.ButtonYes, ControlType.Button);
        UiRetry.InvokeOrClick(yes);

        // The detail page navigates back to the feed list on its own.
        _page.WaitForElementByName(AppResources.ActionAddFeed, ControlType.Button);

        // Switching tabs forces the appearing reload so a stale card cannot
        // linger in the still-rendered collection when the assertion polls.
        _page.SelectTab(AppResources.TabUnread);
        _page.SelectTab(AppResources.TabFeeds);
        Assert.True(
            UiRetry.WaitFor(
                () => UiRetry.TryFindElementByName(Window, title, timeout: TimeSpan.FromMilliseconds(500)) is null,
                TimeSpan.FromSeconds(15)),
            $"The deleted feed card '{title}' is still rendered in the feed list.");
    }

    /// <summary>
    /// The detail action sheet offers "Edit"; the sheet persists the changed
    /// feed URL into the database.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task FeedDetail_Edit_PersistsChanges()
    {
        var title = _page.AddFeedViaUi("edit-target");
        _page.OpenFeedDetail(title);
        _page.OpenFeedDetailActions(AppResources.ButtonEdit);

        var edit = _page.WaitForElementInScopeByName(AppResources.ButtonEdit);
        UiRetry.InvokeOrClick(edit);

        var urlEntry = _page.WaitForEditUrlEntry();
        var newUrl = $"{_fixture.Server.BaseUrl}/feeds/edit-result.xml";
        UiRetry.SetText(urlEntry, newUrl);

        var save = _page.WaitForElementByName(AppResources.ButtonSave, ControlType.Button);
        UiRetry.InvokeOrClick(save);

        Assert.True(
            await FeedDbAssertions.FeedExistsAsync(_fixture.DatabasePath, newUrl),
            $"No feed row with URL '{newUrl}' found in {_fixture.DatabasePath} — the edit was not persisted.");
    }

    /// <summary>
    /// The detail action sheet offers "Refresh"; the action synchronizes only
    /// this feed so its articles appear in the list and in the database.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task FeedDetail_RefreshAction_SyncsFeed()
    {
        var title = _page.AddFeedViaUi("refresh-target");
        _page.OpenFeedDetail(title);
        _page.OpenFeedDetailActions(AppResources.ButtonRefresh);

        var refresh = _page.WaitForElementInScopeByName(AppResources.ButtonRefresh);
        UiRetry.InvokeOrClick(refresh);

        _page.WaitForCard("refresh-target article", TimeSpan.FromSeconds(15));
        Assert.True(
            await FeedDbAssertions.ItemExistsAsync(_fixture.DatabasePath, "refresh-target article"),
            "No item row for the refreshed feed found — the single-feed sync did not run.");
    }

    /// <summary>
    /// Scrolling the detail article list to its end loads the next page: the
    /// oldest article of the 25-item feed only renders after the scroll.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public void FeedDetail_InfiniteScroll_LoadsNextPage()
    {
        _page.AddFeedViaUi("scroll-feed");
        SyncAllViaUnreadTab();
        _page.OpenFeedDetail("Stub Feed scroll-feed");
        _page.WaitForCard("Scroll article 25", TimeSpan.FromSeconds(10));

        var list = UiRetry.WaitForElement(
            Window,
            cf => cf.ByControlType(ControlType.List),
            description: "feed detail article list");

        var loaded = false;
        for (var attempt = 0; attempt < 15 && !loaded; attempt++)
        {
            _page.ScrollListDown(list);
            loaded = UiRetry.WaitFor(
                () => UiRetry.TryFindElementByName(Window, "Scroll article 01", timeout: TimeSpan.FromMilliseconds(400)) is not null,
                TimeSpan.FromSeconds(1));
        }

        Assert.True(loaded, "The second page of the feed detail list was not loaded after scrolling.");
    }

    /// <summary>
    /// "Show error details" only appears in the action sheet for feeds with
    /// error health: a feed whose URL answers 404 gains the entry after a
    /// failed refresh and the alert shows the localized error text, while a
    /// healthy feed does not offer the entry at all.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public void FeedDetail_ErrorDetails_OnlyForErrorFeed()
    {
        // A URL outside /feeds/ hits the stub server's 404 fallback, so the
        // feed can be stored but never synchronized.
        _page.SelectTab(AppResources.TabFeeds);
        _page.OpenAddSheet();
        UiRetry.SetText(_page.WaitForUrlEntry(), $"{_fixture.Server.BaseUrl}/broken-feed");
        var directAdd = _page.WaitForElementByName(AppResources.ButtonDirectAdd, ControlType.Button);
        UiRetry.InvokeOrClick(directAdd);
        _page.WaitForFeedCard("broken-feed");

        _page.OpenFeedDetail("broken-feed");
        _page.OpenFeedDetailActions(AppResources.ButtonRefresh);
        var refresh = _page.WaitForElementInScopeByName(AppResources.ButtonRefresh);
        UiRetry.InvokeOrClick(refresh);

        // The failed sync surfaces the generic sync error on the page and
        // flips the feed health to Error, which enables the details entry.
        _page.WaitForElementByName(AppResources.SyncStatusError);

        _page.OpenFeedDetailActions(AppResources.ButtonShowErrorDetails);
        var details = _page.WaitForElementInScopeByName(AppResources.ButtonShowErrorDetails);
        UiRetry.InvokeOrClick(details);

        // The alert body is one multi-line Text element (localized kind plus
        // the stored technical message), so the localized kind can only be
        // verified as part of the text, not via an exact name lookup.
        Assert.True(
            UiRetry.WaitFor(
                () => ScopedTextContains(AppResources.FeedErrorKindHttpStatus),
                TimeSpan.FromSeconds(10)),
            "The error details alert does not show the localized HTTP error text.");
        var ok = _page.WaitForElementInScopeByName(AppResources.ButtonOk, ControlType.Button);
        UiRetry.InvokeOrClick(ok);

        // A healthy feed must not offer the error details entry.
        _page.NavigateBackToFeedList();
        var healthyTitle = _page.AddFeedViaUi("healthy-target");
        _page.OpenFeedDetail(healthyTitle);
        _page.OpenFeedDetailActions(AppResources.ButtonRename);
        Assert.Null(
            _page.TryFindElementInScopeByName(AppResources.ButtonShowErrorDetails, timeout: TimeSpan.FromSeconds(3)));
    }
}
