// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Reporter.Core.Resources.Strings;

namespace Reporter.E2ETests;

/// <summary>
/// FlaUI smoke tests against the real <c>Reporter.exe</c>. The app under test is
/// fully real — real view models, services and SQLite — only the network side is
/// replaced by the in-process <see cref="StubFeedServer"/> via
/// <c>REPORTER_FEEDSEARCH_ENDPOINT</c>, and the database is isolated via
/// <c>REPORTER_DB_PATH</c>. All tests share one app instance through
/// <see cref="E2ETestCollection"/> and therefore run serially.
/// </summary>
[Collection(E2ETestCollection.CollectionName)]
public sealed class SmokeTests
{
    private static readonly TimeSpan ShortWait = TimeSpan.FromSeconds(2);

    private readonly ReporterAppFixture _fixture;
    private readonly E2EPageHelpers _page;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmokeTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared app/stub-server fixture.</param>
    public SmokeTests(ReporterAppFixture fixture)
    {
        _fixture = fixture;
        _page = new E2EPageHelpers(fixture);
    }

    // The window is re-resolved for every call: UIA proxies can go stale when
    // dialogs and popups come and go.
    private Window Window => _fixture.GetMainWindow();

    private AutomationElement WaitForElementByName(string name, ControlType? controlType = null)
        => UiRetry.WaitForElementByName(Window, name, controlType);

    private AutomationElement WaitForElementInScopeByName(string name, ControlType? controlType = null)
        => UiRetry.WaitForElementInScope(
            _fixture.App,
            _fixture.Automation,
            cf => controlType is null
                ? cf.ByName(name)
                : cf.ByName(name).And(cf.ByControlType(controlType.Value)),
            description: $"name '{name}'");

    private AutomationElement? TryFindElementInScopeByName(
        string name,
        ControlType? controlType = null,
        TimeSpan? timeout = null)
        => UiRetry.TryFindElementInScope(
            _fixture.App,
            _fixture.Automation,
            cf => controlType is null
                ? cf.ByName(name)
                : cf.ByName(name).And(cf.ByControlType(controlType.Value)),
            timeout ?? ShortWait);

    // Types a URL into the sheet's entry. The fallback SetText path focuses the
    // entry first and types keystrokes.
    private void EnterUrl(string url)
    {
        UiRetry.SetText(_page.WaitForUrlEntry(), url);
    }

    // Adds a feed via the UI: opens the sheet, types the stub feed URL and taps
    // the direct-add button. Returns the feed title used for later assertions.
    private string AddFeedViaUi(string stubName)
    {
        _page.SelectTab(AppResources.TabFeeds);
        _page.OpenAddSheet();
        var feedUrl = $"{_fixture.Server.BaseUrl}/feeds/{stubName}.xml";
        EnterUrl(feedUrl);
        var directAdd = WaitForElementByName(AppResources.ButtonDirectAdd, ControlType.Button);
        UiRetry.InvokeOrClick(directAdd);

        var cardTitle = $"{stubName}.xml";
        WaitForCard(cardTitle);
        return cardTitle;
    }

    // Finds a card by its title (see UiRetry.WaitForCard for the Group-first
    // lookup with fallback).
    private AutomationElement WaitForCard(string title)
        => UiRetry.WaitForCard(Window, title, TimeSpan.FromSeconds(5));

    // Taps a feed card by its title (SemanticProperties.Description -> UIA
    // name) and waits until the action sheet exposes the expected entry. The
    // card is activated by a real mouse click that can be swallowed while the
    // add sheet is still closing or the card re-renders, so the whole open
    // sequence is retried before the test gives up.
    private void OpenFeedActions(string feedTitle, string expectedEntryName)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            UiRetry.InvokeOrClick(WaitForCard(feedTitle));
            if (TryFindElementInScopeByName(expectedEntryName, timeout: TimeSpan.FromSeconds(6)) is not null)
            {
                return;
            }
        }

        // Same failure signature as before: the regular scope wait reports the
        // missing sheet entry after the last retry.
        WaitForElementInScopeByName(expectedEntryName);
    }

    // Waits until an element has keyboard focus.
    private void WaitForFocus(AutomationElement element, string description)
    {
        var focused = UiRetry.WaitFor(
            () => element.Properties.HasKeyboardFocus.ValueOrDefault == true,
            TimeSpan.FromSeconds(5));
        Assert.True(focused, $"Element did not receive keyboard focus: {description}");
    }

    // Best-effort UI reset between tests: dismiss open popups and dialogs, close
    // the search results view, dismiss the add sheet and land on the Feeds tab.
    private void ResetUiState()
    {
        // Action-sheet popups carry a full-window "light dismiss" button.
        var lightDismiss = UiRetry.TryFindElementInScope(
            _fixture.App,
            _fixture.Automation,
            cf => cf.ByAutomationId("Light Dismiss"),
            ShortWait);
        if (lightDismiss is not null)
        {
            UiRetry.InvokeOrClick(lightDismiss);
        }

        foreach (var buttonName in new[]
        {
            AppResources.ButtonNo,
            AppResources.ButtonOk,
            AppResources.ButtonCancel,
        })
        {
            var button = TryFindElementInScopeByName(buttonName, ControlType.Button);
            if (button is not null)
            {
                UiRetry.InvokeOrClick(button);
                break;
            }
        }

        var closeResults = TryFindElementInScopeByName(AppResources.ButtonCloseSearchResults, ControlType.Button);
        if (closeResults is not null)
        {
            UiRetry.InvokeOrClick(closeResults);
        }

        // The dismiss overlay is a Group; the control type filter is required —
        // the same localized name ("Schließen"/"Close") is carried by the OS
        // titlebar close button, which must never be activated here.
        var dismiss = TryFindElementInScopeByName(AppResources.AccessibilityDismissSheet, ControlType.Group);
        if (dismiss is not null)
        {
            UiRetry.InvokeOrClick(dismiss);
        }

        _page.SelectTab(AppResources.TabFeeds);
    }

    /// <summary>
    /// The app starts (on the Unread page) and the Feeds tab renders the feed
    /// list — either real feed cards (SemanticProperties.Description is mapped
    /// to the UIA name) or, on an empty database, the empty-state placeholder.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public void AppStarts_FeedListRenders()
    {
        _page.SelectTab(AppResources.TabFeeds);
        var rendered = UiRetry.WaitFor(
            () => Window.FindFirstDescendant(cf =>
                cf.ByHelpText(AppResources.AccessibilityTapForActions)
                    .Or(cf.ByName(AppResources.PlaceholderFeeds))) is not null);
        Assert.True(rendered, "Neither feed cards nor the empty placeholder were rendered.");
    }

    /// <summary>
    /// The "+" button opens the add sheet and the URL entry gets keyboard focus.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public void AddButton_OpensSheet_FocusesUrlEntry()
    {
        _page.SelectTab(AppResources.TabFeeds);
        _page.OpenAddSheet();
        WaitForFocus(_page.WaitForUrlEntry(), "NewUrlEntry");
        ResetUiState();
    }

    /// <summary>
    /// A direct add persists the feed: the card appears in the list and the URL
    /// is stored in the isolated database.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task DirectAdd_FeedAppearsInListAndDatabase()
    {
        var feedUrl = $"{_fixture.Server.BaseUrl}/feeds/direct-add.xml";
        var cardTitle = AddFeedViaUi("direct-add");
        Assert.NotNull(WaitForCard(cardTitle));
        Assert.True(
            await FeedDbAssertions.FeedExistsAsync(_fixture.DatabasePath, feedUrl),
            $"No feed row with URL '{feedUrl}' found in {_fixture.DatabasePath}.");
    }

    /// <summary>
    /// The feed card action sheet offers "Rename"; the prompt accepts a new
    /// title and the card shows it afterwards.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public void FeedActionSheet_Rename_UpdatesTitle()
    {
        var oldTitle = AddFeedViaUi("rename-target");
        OpenFeedActions(oldTitle, AppResources.ButtonRename);

        // Action sheet options render as list items inside a popup, not buttons.
        var rename = WaitForElementInScopeByName(AppResources.ButtonRename);
        UiRetry.InvokeOrClick(rename);

        var promptEntry = UiRetry.WaitForElementInScope(
            _fixture.App,
            _fixture.Automation,
            cf => cf.ByControlType(ControlType.Edit),
            description: "rename prompt text box");
        UiRetry.SetText(promptEntry, "Smoke Renamed Feed");

        var ok = WaitForElementInScopeByName(AppResources.ButtonOk, ControlType.Button);
        UiRetry.InvokeOrClick(ok);

        WaitForCard("Smoke Renamed Feed");
    }

    // Creates a category on the Categories tab via the UI (entry + save button)
    // and waits for its card before returning to the Feeds tab.
    private void AddCategoryViaUi(string categoryName)
    {
        _page.SelectTab(AppResources.TabCategories);
        var entry = UiRetry.WaitForElementByName(Window, AppResources.LabelCategoryName, ControlType.Edit);
        UiRetry.SetText(entry, categoryName);
        var save = WaitForElementByName(AppResources.ButtonSave, ControlType.Button);
        UiRetry.InvokeOrClick(save);
        WaitForCard(categoryName);
        _page.SelectTab(AppResources.TabFeeds);
    }

    // Whether the feed card currently shows the given category name. The card is
    // re-resolved on every poll because it is re-rendered on model changes.
    private bool CardShowsCategory(string feedTitle, string categoryName)
        => WaitForCard(feedTitle).FindFirstDescendant(cf => cf.ByName(categoryName)) is not null;

    // Drives the feed card action sheet's "Change Category" option and picks
    // the given entry — a real category or the built-in "None" pseudo entry.
    private void ChangeFeedCategoryViaActionSheet(string feedTitle, string optionName)
    {
        OpenFeedActions(feedTitle, AppResources.ButtonChangeCategory);
        var changeCategory = WaitForElementInScopeByName(AppResources.ButtonChangeCategory);
        UiRetry.InvokeOrClick(changeCategory);

        var option = WaitForElementInScopeByName(optionName);
        UiRetry.InvokeOrClick(option);
    }

    /// <summary>
    /// The feed card action sheet offers "Change Category": assigning a real
    /// category shows its name on the card, and choosing the built-in "None"
    /// entry clears the assignment again.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public void FeedActionSheet_ChangeCategory_IncludingNone()
    {
        const string categoryName = "E2E Smoke Category";
        AddCategoryViaUi(categoryName);

        var title = AddFeedViaUi("category-target");

        // First direction: assigning the real category shows it on the card.
        ChangeFeedCategoryViaActionSheet(title, categoryName);
        Assert.True(
            UiRetry.WaitFor(() => CardShowsCategory(title, categoryName), TimeSpan.FromSeconds(5)),
            $"The card '{title}' does not show the assigned category '{categoryName}'.");

        // Second direction: the "None" pseudo entry clears the assignment again.
        ChangeFeedCategoryViaActionSheet(title, AppResources.CategoryNone);
        Assert.True(
            UiRetry.WaitFor(() => !CardShowsCategory(title, categoryName), TimeSpan.FromSeconds(5)),
            $"The card '{title}' still shows the category '{categoryName}' after clearing it.");
    }

    /// <summary>
    /// Searching a known site URL returns the stub directory result; tapping it
    /// and confirming the subscribe alert persists the feed in the database.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task Search_SubscribesResult_PersistsFeed()
    {
        _page.SelectTab(AppResources.TabFeeds);
        _page.OpenAddSheet();
        EnterUrl(_fixture.Server.BaseUrl);

        var search = WaitForElementByName(AppResources.ButtonSearch, ControlType.Button);
        UiRetry.InvokeOrClick(search);

        var resultCard = WaitForCard("Stub Search Hit");
        UiRetry.InvokeOrClick(resultCard);

        var yes = WaitForElementInScopeByName(AppResources.ButtonYes, ControlType.Button);
        UiRetry.InvokeOrClick(yes);

        WaitForCard("Stub Search Hit");
        Assert.True(
            await FeedDbAssertions
                .FeedExistsAsync(_fixture.DatabasePath, $"{_fixture.Server.BaseUrl}/feeds/search-hit.xml")
                ,
            "The subscribed stub feed was not persisted.");
    }

    /// <summary>
    /// Searching a URL that is unknown to the stub directory falls back to HTML
    /// discovery and surfaces the feed linked via
    /// <c>&lt;link rel="alternate"&gt;</c> on the stub site.
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    public void Search_SiteUrl_DiscoversFeedViaLinkTag()
    {
        _page.SelectTab(AppResources.TabFeeds);
        _page.OpenAddSheet();
        EnterUrl($"{_fixture.Server.BaseUrl}/site");

        var search = WaitForElementByName(AppResources.ButtonSearch, ControlType.Button);
        UiRetry.InvokeOrClick(search);

        WaitForCard("Stub Site Feed");
        ResetUiState();
    }
}
