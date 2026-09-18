// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Reporter.Core.Resources.Strings;

namespace Reporter.E2ETests;

/// <summary>
/// E2E proof that an external link tapped inside the article WebView is
/// cancelled in-app and handed to the system browser: the stub server counts
/// the hit on <c>/external-link</c> and the detail page stays open. Runs
/// serially inside the <c>E2E</c> collection against the shared
/// <see cref="ReporterAppFixture"/>.
/// </summary>
[Collection(E2ETestCollection.CollectionName)]
public sealed class ArticleLinkTests
{
    private const string ItemTitle = "link-feed article";

    private readonly ReporterAppFixture _fixture;
    private readonly E2EPageHelpers _page;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleLinkTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared app/stub-server fixture.</param>
    public ArticleLinkTests(ReporterAppFixture fixture)
    {
        _fixture = fixture;
        _page = new E2EPageHelpers(fixture);
    }

    private Window Window => _fixture.GetMainWindow();

    /// <summary>
    /// Adds the <c>link-feed</c> stub feed via the UI, syncs it on the Unread
    /// tab, opens the article detail view and activates the external link in
    /// the WebView. Asserts that the system browser fetched the stub URL and
    /// that the detail view was left open (no in-app navigation).
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task ExternalLinkInArticle_OpensSystemBrowser()
    {
        _page.SelectTab(AppResources.TabFeeds);
        _page.OpenAddSheet();
        UiRetry.SetText(_page.WaitForUrlEntry(), $"{_fixture.Server.BaseUrl}/feeds/link-feed.xml");
        var directAdd = UiRetry.WaitForElementByName(Window, AppResources.ButtonDirectAdd, ControlType.Button);
        UiRetry.InvokeOrClick(directAdd);

        _page.SelectTab(AppResources.TabUnread);
        var refresh = UiRetry.WaitForElementByName(Window, AppResources.ButtonRefresh);
        UiRetry.InvokeOrClick(refresh);

        Assert.True(
            await FeedDbAssertions.ItemExistsAsync(_fixture.DatabasePath, ItemTitle),
            $"No item row titled '{ItemTitle}' found in {_fixture.DatabasePath} — the stub feed was not synced.");

        // The unread CollectionView re-renders while the sync finishes; a card
        // proxy found during re-render can go stale and swallow the tap, so the
        // tap is retried until the detail page actually opens.
        AutomationElement? openInBrowser = null;
        for (var attempt = 0; attempt < 4 && openInBrowser is null; attempt++)
        {
            var card = UiRetry.WaitForCard(Window, ItemTitle, TimeSpan.FromSeconds(10));
            UiRetry.InvokeOrClick(card);
            openInBrowser = UiRetry.TryFindElementByName(
                Window, AppResources.ArticleOpenInBrowser, timeout: TimeSpan.FromSeconds(5));
        }

        Assert.NotNull(openInBrowser);

        // WebView2 exposes its DOM subtree through UIA; the link renders as a
        // Hyperlink element. While the WebView is still rendering the link is
        // absent from the UIA tree, so the lookup returns null and the attempt
        // retries. InvokeOrClick falls back to a real mouse click on the
        // element centre when no invoke pattern is offered.
        AutomationElement? link = null;
        var fetched = false;
        for (var attempt = 0; attempt < 4 && !fetched; attempt++)
        {
            link = UiRetry.TryFindElement(
                Window,
                cf => cf.ByName("external link").And(cf.ByControlType(ControlType.Hyperlink)),
                TimeSpan.FromSeconds(10))
                ?? UiRetry.TryFindElement(
                    Window,
                    cf => cf.ByControlType(ControlType.Hyperlink),
                    TimeSpan.FromSeconds(10));
            if (link is null)
            {
                continue;
            }

            UiRetry.InvokeOrClick(link);
            fetched = UiRetry.WaitFor(() => _fixture.Server.ExternalLinkHitCount > 0, TimeSpan.FromSeconds(8));
        }

        Assert.True(
            fetched,
            $"The system browser did not fetch /external-link. {(link is null ? "no hyperlink found" : DescribeLinkState(link))}");

        // No in-app navigation: the detail view is still open.
        Assert.NotNull(
            UiRetry.TryFindElementByName(Window, AppResources.ArticleOpenInBrowser, timeout: TimeSpan.FromSeconds(5)));
    }

    // Builds a diagnostic snapshot for the hit-count assertion: the link's UIA
    // state plus whether the offline alert or the open-in-browser error row
    // became visible — the two outcomes that explain a missing external hit.
    private string DescribeLinkState(AutomationElement link)
    {
        var parts = new List<string>();
        try
        {
            parts.Add($"link name='{link.Name}' rect={link.BoundingRectangle} offscreen={link.Properties.IsOffscreen.Value}");
            parts.Add(link.Patterns.Invoke.IsSupported ? "invoke=supported" : "invoke=unsupported");
        }
        catch (Exception ex)
        {
            parts.Add($"link probe failed: {ex.GetType().Name}");
        }

        var alert = UiRetry.TryFindElementInScope(
            _fixture.App,
            _fixture.Automation,
            cf => cf.ByName(AppResources.ArticleOfflineLinksDisabled),
            TimeSpan.FromSeconds(1));
        parts.Add(alert is null ? "offline-alert=absent" : "offline-alert=SHOWN");

        var error = UiRetry.TryFindElementByName(Window, AppResources.ErrorOpenInBrowserFailed, timeout: TimeSpan.FromSeconds(1));
        parts.Add(error is null ? "error-row=absent" : "error-row=SHOWN");

        return string.Join(", ", parts);
    }
}
