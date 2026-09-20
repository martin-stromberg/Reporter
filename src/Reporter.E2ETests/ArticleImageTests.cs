// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Reporter.Core.Resources.Strings;

namespace Reporter.E2ETests;

/// <summary>
/// E2E proof that a feed item with an image enclosure is synced together with a
/// locally stored copy of the image: the image bytes land in the isolated
/// <c>reporter-content.db</c>, the article card shows a thumbnail and the
/// article detail renders an image. A second test covers a broken image
/// endpoint: the sync must still succeed and simply store no image. Runs
/// serially inside the <c>E2E</c> collection against the shared
/// <see cref="ReporterAppFixture"/>.
/// </summary>
[Collection(E2ETestCollection.CollectionName)]
public sealed class ArticleImageTests
{
    private const string ItemTitle = "image-feed article";
    private const string BrokenItemTitle = "broken-image-feed article";

    // One short poll per subtree search so the caller can re-resolve the
    // search root between polls instead of waiting on a stale proxy.
    private static readonly TimeSpan SinglePollTimeout = TimeSpan.FromMilliseconds(300);

    private readonly ReporterAppFixture _fixture;
    private readonly E2EPageHelpers _page;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleImageTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared app/stub-server fixture.</param>
    public ArticleImageTests(ReporterAppFixture fixture)
    {
        _fixture = fixture;
        _page = new E2EPageHelpers(fixture);
    }

    private Window Window => _fixture.GetMainWindow();

    /// <summary>
    /// Adds the <c>image-feed</c> stub feed via the UI, syncs it on the Unread
    /// tab and asserts that the image enclosure was downloaded into the content
    /// database, that the article card renders a thumbnail and that the detail
    /// view exposes an image element.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task ArticleImage_StoredLocally_AndShownOnCardAndDetail()
    {
        var feedUrl = $"{_fixture.Server.BaseUrl}/feeds/image-feed.xml";
        AddFeedViaUi(feedUrl);
        Assert.True(
            await FeedDbAssertions.FeedExistsAsync(_fixture.DatabasePath, feedUrl),
            $"No feed row with URL '{feedUrl}' found in {_fixture.DatabasePath} — the direct add did not persist the feed.");

        _page.SelectTab(AppResources.TabUnread);
        var refresh = UiRetry.WaitForElementByName(Window, AppResources.ButtonRefresh);
        UiRetry.InvokeOrClick(refresh);

        Assert.True(
            await FeedDbAssertions.ItemExistsAsync(_fixture.DatabasePath, ItemTitle),
            $"No item row titled '{ItemTitle}' found in {_fixture.DatabasePath} — the stub feed was not synced.");

        Assert.True(
            await FeedDbAssertions.ItemImageExistsAsync(
                _fixture.DatabasePath,
                _fixture.ContentDatabasePath,
                ItemTitle),
            $"No image_data row for '{ItemTitle}' found in {_fixture.ContentDatabasePath} — the article image was not downloaded.");

        // The card must render a thumbnail: the local image bytes are bound to
        // an Image element inside the card subtree. Both card and thumbnail are
        // re-resolved from the window on every poll — the CollectionView can
        // re-render the card while HasLocalImage hydrates, which stale-mates an
        // element proxy resolved earlier and would hide the thumbnail from a
        // subtree search for the rest of the wait.
        var card = UiRetry.WaitForCard(Window, ItemTitle, TimeSpan.FromSeconds(10));
        AutomationElement? thumbnail = null;
        var thumbnailFound = UiRetry.WaitFor(
            () =>
            {
                var scope = UiRetry.TryFindElementByName(
                                Window, ItemTitle, ControlType.Group, SinglePollTimeout)
                            ?? UiRetry.TryFindElementByName(Window, ItemTitle, timeout: SinglePollTimeout)
                            ?? card;
                thumbnail = TryFindCardThumbnail(scope)
                            ?? (scope.Parent is { } parent ? TryFindCardThumbnail(parent) : null);
                if (thumbnail is not null)
                {
                    card = scope;
                }

                return thumbnail is not null;
            },
            TimeSpan.FromSeconds(15));
        Assert.True(thumbnailFound && thumbnail is not null, "The article card did not render a thumbnail (ArticleCardThumbnail) in time.");
        Assert.False(thumbnail!.Properties.IsOffscreen.ValueOrDefault, "The card thumbnail is offscreen.");

        // Detail view: the WebView exposes the article image as a UIA Image
        // element once the page finished rendering. A swallowed click or a
        // stale card proxy is retried after re-resolving the card.
        AutomationElement? openInBrowser = null;
        for (var attempt = 0; attempt < 4 && openInBrowser is null; attempt++)
        {
            try
            {
                UiRetry.InvokeOrClick(card);
            }
            catch (Exception)
            {
                // Stale proxy or click lost while the list re-rendered — retry.
            }

            openInBrowser = UiRetry.TryFindElementByName(
                Window, AppResources.ArticleOpenInBrowser, timeout: TimeSpan.FromSeconds(5));
            if (openInBrowser is null)
            {
                card = UiRetry.TryFindElementByName(
                           Window, ItemTitle, ControlType.Group, TimeSpan.FromSeconds(3))
                       ?? card;
            }
        }

        Assert.NotNull(openInBrowser);

        // The article image lives inside the WebView2 DOM subtree, which UIA
        // exposes as a Document element (the same subtree that surfaces the
        // external Hyperlink in ArticleLinkTests). Scoping the lookup to that
        // Document keeps leftover card thumbnails and decorative images out of
        // the assertion; the >= 40 px height filter additionally rejects tiny
        // or collapsed images. Without a Document the window scope is used so
        // the check stays meaningful on earlier render states.
        var detailImageVisible = UiRetry.WaitFor(
            () =>
            {
                try
                {
                    var scope = (AutomationElement?)Window.FindFirstDescendant(
                        cf => cf.ByControlType(ControlType.Document)) ?? Window;
                    return scope
                        .FindAllDescendants(cf => cf.ByControlType(ControlType.Image))
                        .Any(e => !e.Properties.IsOffscreen.ValueOrDefault && e.BoundingRectangle.Height >= 40);
                }
                catch (Exception)
                {
                    return false;
                }
            },
            TimeSpan.FromSeconds(15));
        Assert.True(detailImageVisible, "No visible image element found in the article detail view.");
    }

    /// <summary>
    /// Adds the <c>broken-image-feed</c> stub feed whose enclosure URL answers
    /// with a non-image body. The sync must still store the item and must not
    /// write an image row — the card falls back to the next image source.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task BrokenImage_DoesNotFailSync_StoresNoImage()
    {
        var feedUrl = $"{_fixture.Server.BaseUrl}/feeds/broken-image-feed.xml";
        AddFeedViaUi(feedUrl);
        Assert.True(
            await FeedDbAssertions.FeedExistsAsync(_fixture.DatabasePath, feedUrl),
            $"No feed row with URL '{feedUrl}' found in {_fixture.DatabasePath} — the direct add did not persist the feed.");

        _page.SelectTab(AppResources.TabUnread);
        var refresh = UiRetry.WaitForElementByName(Window, AppResources.ButtonRefresh);
        UiRetry.InvokeOrClick(refresh);

        Assert.True(
            await FeedDbAssertions.ItemExistsAsync(_fixture.DatabasePath, BrokenItemTitle),
            $"No item row titled '{BrokenItemTitle}' found in {_fixture.DatabasePath} — a rejected image download must not break the sync.");

        // Give the image pipeline a short grace period, then prove no image
        // row was written for the item.
        Assert.False(
            await FeedDbAssertions.ItemImageExistsAsync(
                _fixture.DatabasePath,
                _fixture.ContentDatabasePath,
                BrokenItemTitle,
                TimeSpan.FromSeconds(3)),
            $"An image_data row exists for '{BrokenItemTitle}' although the endpoint returned a non-image body.");

        // The card still renders — the thumbnail slot falls back (remote URL /
        // favicon / feed initial).
        var card = UiRetry.WaitForCard(Window, BrokenItemTitle, TimeSpan.FromSeconds(10));
        Assert.NotNull(card);
    }

    // Searches a single subtree for the rendered card thumbnail with one short
    // poll, so callers can re-resolve the search root between attempts.
    private static AutomationElement? TryFindCardThumbnail(AutomationElement root)
    {
        return UiRetry.TryFindElement(
            root,
            cf => cf.ByAutomationId("ArticleCardThumbnail").And(cf.ByControlType(ControlType.Image)),
            SinglePollTimeout);
    }

    // Opens the add sheet on the Feeds tab, enters the stub feed URL and
    // confirms via direct add — the same flow the smoke and link tests use.
    private void AddFeedViaUi(string feedUrl)
    {
        _page.SelectTab(AppResources.TabFeeds);
        _page.OpenAddSheet();
        UiRetry.SetText(_page.WaitForUrlEntry(), feedUrl);
        var directAdd = UiRetry.WaitForElementByName(Window, AppResources.ButtonDirectAdd, ControlType.Button);
        UiRetry.InvokeOrClick(directAdd);
    }
}
