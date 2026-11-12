// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Reporter.Core.Resources.Strings;

namespace Reporter.E2ETests;

/// <summary>
/// E2E proof that feed item tags (<c>&lt;app:tags&gt;</c> extension elements
/// and RSS <c>&lt;category&gt;</c> elements) are persisted during sync and
/// rendered as a subdued tag line at the end of the article detail WebView.
/// Runs serially inside the <c>E2E</c> collection against the shared
/// <see cref="ReporterAppFixture"/>.
/// </summary>
[Collection(E2ETestCollection.CollectionName)]
public sealed class ArticleTagsTests : IDisposable
{
    private const string TaggedItemTitle = "tag-feed tagged article";
    private const string TagTerm = "Stadtbahnen";

    private readonly ReporterAppFixture _fixture;
    private readonly E2EPageHelpers _page;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleTagsTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared app/stub-server fixture.</param>
    public ArticleTagsTests(ReporterAppFixture fixture)
    {
        _fixture = fixture;
        _page = new E2EPageHelpers(fixture);

        // Feeds left behind by earlier suite classes accumulate in the shared
        // database and grow the feed list beyond the viewport — the scroll
        // scan cannot reach a virtualized-away card reliably, so every test
        // starts from an empty feed list.
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
            _page.DismissPopups();
            FeedDbAssertions.DeleteAllFeedsAsync(_fixture.DatabasePath).GetAwaiter().GetResult();
            FeedDbAssertions.MarkAllItemsReadAsync(_fixture.DatabasePath).GetAwaiter().GetResult();
            _page.SelectTab(AppResources.TabFeeds);
        }
        catch (Exception)
        {
            // A broken app is already reported by the failing test itself; the
            // cleanup must not mask that failure.
        }
    }

    private Window Window => _fixture.GetMainWindow();

    /// <summary>
    /// Adds the <c>tag-feed</c> stub feed via the UI, syncs it on the Unread
    /// tab and opens the tagged article: the tag line renders inside the
    /// article detail WebView below the article text.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task ArticleDetail_Tags_ShownBelowArticleText()
    {
        AddFeedAndSync();

        Assert.True(
            await FeedDbAssertions.ItemExistsAsync(_fixture.DatabasePath, TaggedItemTitle),
            $"No item row titled '{TaggedItemTitle}' found in {_fixture.DatabasePath} — the stub feed was not synced.");

        // The unread CollectionView re-renders while the sync finishes; a card
        // proxy found during re-render can go stale and swallow the tap, so the
        // tap is retried until the detail page actually opens.
        AutomationElement? openInBrowser = null;
        for (var attempt = 0; attempt < 4 && openInBrowser is null; attempt++)
        {
            var card = UiRetry.WaitForCard(Window, TaggedItemTitle, TimeSpan.FromSeconds(10));
            UiRetry.InvokeOrClick(card);
            openInBrowser = UiRetry.TryFindElementByName(
                Window, AppResources.ArticleOpenInBrowser, timeout: TimeSpan.FromSeconds(5));
        }

        Assert.NotNull(openInBrowser);

        // WebView2 exposes its DOM subtree through UIA; the tag paragraph
        // renders as a Text element inside the Document subtree. Scoping the
        // lookup to the Document keeps list cards and chrome text out of the
        // assertion; while the WebView is still rendering the element is
        // absent, so the lookup retries until the deadline.
        var tagLineVisible = UiRetry.WaitFor(
            () =>
            {
                try
                {
                    var scope = (AutomationElement?)Window.FindFirstDescendant(
                        cf => cf.ByControlType(ControlType.Document)) ?? Window;
                    return scope
                        .FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
                        .Any(e => e.Name?.Contains(TagTerm, StringComparison.Ordinal) == true
                                  && !e.Properties.IsOffscreen.ValueOrDefault);
                }
                catch (Exception)
                {
                    return false;
                }
            },
            TimeSpan.FromSeconds(15));
        Assert.True(tagLineVisible, "No visible tag line found in the article detail view.");
    }

    /// <summary>
    /// Adds the <c>tag-feed</c> stub feed via the UI and syncs it on the
    /// Unread tab: the tags extracted from <c>&lt;app:tags&gt;</c> and
    /// <c>&lt;category&gt;</c> land in the <c>items.tags</c> column of the
    /// app database.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task ArticleDetail_Tags_PersistedInDatabase()
    {
        AddFeedAndSync();

        Assert.True(
            await FeedDbAssertions.ItemTagsContainAsync(_fixture.DatabasePath, TaggedItemTitle, TagTerm),
            $"The tags column of '{TaggedItemTitle}' does not contain '{TagTerm}' in {_fixture.DatabasePath} — the feed tags were not persisted.");
        Assert.True(
            await FeedDbAssertions.ItemTagsContainAsync(_fixture.DatabasePath, TaggedItemTitle, "Politik"),
            $"The tags column of '{TaggedItemTitle}' does not contain 'Politik' in {_fixture.DatabasePath} — the RSS category was not persisted.");
    }

    // Adds the tag-feed via the UI and triggers a full sync on the Unread tab
    // so the freshly added feed downloads its tagged articles.
    private void AddFeedAndSync()
    {
        _page.SelectTab(AppResources.TabFeeds);
        _page.OpenAddSheet();
        UiRetry.SetText(_page.WaitForUrlEntry(), $"{_fixture.Server.BaseUrl}/feeds/tag-feed.xml");
        var directAdd = UiRetry.WaitForElementByName(Window, AppResources.ButtonDirectAdd, ControlType.Button);
        UiRetry.InvokeOrClick(directAdd);

        _page.SelectTab(AppResources.TabUnread);
        var refresh = UiRetry.WaitForElementByName(Window, AppResources.ButtonRefresh);
        UiRetry.InvokeOrClick(refresh);
    }
}
