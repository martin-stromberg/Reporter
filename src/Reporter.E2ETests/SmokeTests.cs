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

    // Types a URL into the sheet's entry. The fallback SetText path focuses the
    // entry first and types keystrokes.
    private void EnterUrl(string url)
    {
        UiRetry.SetText(_page.WaitForUrlEntry(), url);
    }

    // Waits until an element has keyboard focus.
    private void WaitForFocus(AutomationElement element, string description)
    {
        var focused = UiRetry.WaitFor(
            () => element.Properties.HasKeyboardFocus.ValueOrDefault == true,
            TimeSpan.FromSeconds(5));
        Assert.True(focused, $"Element did not receive keyboard focus: {description}");
    }

    // Best-effort UI reset between tests: dismiss open popups, dialogs and
    // sheets, then land on the Feeds tab.
    private void ResetUiState()
    {
        _page.DismissPopups();
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
                cf.ByHelpText(AppResources.AccessibilityOpenFeedDetails)
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
        var cardTitle = _page.AddFeedViaUi("direct-add");
        Assert.NotNull(_page.WaitForCard(cardTitle));
        Assert.True(
            await FeedDbAssertions.FeedExistsAsync(_fixture.DatabasePath, feedUrl),
            $"No feed row with URL '{feedUrl}' found in {_fixture.DatabasePath}.");
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

        var search = _page.WaitForElementByName(AppResources.ButtonSearch, ControlType.Button);
        UiRetry.InvokeOrClick(search);

        var resultCard = _page.WaitForCard("Stub Search Hit");
        UiRetry.InvokeOrClick(resultCard);

        // The tap on the result card can be swallowed while the results list is
        // still settling; retry it once when the confirmation dialog does not
        // appear in time.
        var yes = _page.TryFindElementInScopeByName(
            AppResources.ButtonYes,
            ControlType.Button,
            TimeSpan.FromSeconds(8));
        if (yes is null)
        {
            UiRetry.InvokeOrClick(_page.WaitForCard("Stub Search Hit"));
            yes = _page.WaitForElementInScopeByName(AppResources.ButtonYes, ControlType.Button);
        }

        UiRetry.InvokeOrClick(yes);

        _page.WaitForCard("Stub Search Hit");
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

        var search = _page.WaitForElementByName(AppResources.ButtonSearch, ControlType.Button);
        UiRetry.InvokeOrClick(search);

        _page.WaitForCard("Stub Site Feed");
        ResetUiState();
    }
}
