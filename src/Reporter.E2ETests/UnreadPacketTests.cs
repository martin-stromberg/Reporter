// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using System.Globalization;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Reporter.Core.Resources.Strings;

namespace Reporter.E2ETests;

/// <summary>
/// FlaUI tests for the reading-packet flow on the Unread page: the unread list
/// is restricted to the first calendar-day packet in the active sort order,
/// "all read" marks only that packet and the list automatically loads the next
/// packet. Like <see cref="SmokeTests"/> the app under test is fully real; only
/// the network is stubbed via <see cref="StubFeedServer"/>. All tests share one
/// app instance through <see cref="E2ETestCollection"/> and therefore run
/// serially.
/// </summary>
[Collection(E2ETestCollection.CollectionName)]
public sealed class UnreadPacketTests : IDisposable
{
    private readonly ReporterAppFixture _fixture;
    private readonly E2EPageHelpers _page;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnreadPacketTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared app/stub-server fixture.</param>
    public UnreadPacketTests(ReporterAppFixture fixture)
    {
        _fixture = fixture;
        _page = new E2EPageHelpers(fixture);

        // Unread leftovers from earlier suite classes must not shadow these
        // tests' packets: an undated leftover becomes the anchor day in
        // ascending order, dated leftovers become it in descending order.
        // Every test therefore starts from a fully read, feed-less database
        // with the default sort order.
        FeedDbAssertions.MarkAllItemsReadAsync(_fixture.DatabasePath).GetAwaiter().GetResult();
        FeedDbAssertions.DeleteAllFeedsAsync(_fixture.DatabasePath).GetAwaiter().GetResult();
        FeedDbAssertions.SetUnreadSortOrderAsync(_fixture.DatabasePath, "desc").GetAwaiter().GetResult();
    }

    /// <summary>
    /// Resets the UI and the database after every test — including failed
    /// ones, whose leftover popups, dated unread articles or a flipped sort
    /// order would otherwise cascade into the next test class.
    /// </summary>
    public void Dispose()
    {
        try
        {
            _page.DismissPopups();
            FeedDbAssertions.MarkAllItemsReadAsync(_fixture.DatabasePath).GetAwaiter().GetResult();
            FeedDbAssertions.DeleteAllFeedsAsync(_fixture.DatabasePath).GetAwaiter().GetResult();
            FeedDbAssertions.SetUnreadSortOrderAsync(_fixture.DatabasePath, "desc").GetAwaiter().GetResult();
            _page.SelectTab(AppResources.TabFeeds);
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

    // The local calendar day the given fixture pubDate (always 12:00 GMT in
    // day-packet-feed.xml) lands on, formatted exactly like the ViewModel
    // formats the packet day label.
    private static string PacketDayLabel(int year, int month, int day)
        => DateOnly.FromDateTime(
                DateTime.SpecifyKind(new DateTime(year, month, day, 12, 0, 0), DateTimeKind.Utc)
                    .ToLocalTime()
                    .Date)
            .ToString("d", CultureInfo.CurrentCulture);

    // Adds a stub feed via the UI and triggers a full sync on the Unread tab
    // so its articles land in the database and the unread list re-renders.
    private void AddFeedAndSync(string stubName)
    {
        _page.AddFeedViaUi(stubName);
        _page.SelectTab(AppResources.TabUnread);
        var refresh = _page.WaitForElementByName(AppResources.ButtonRefresh);
        UiRetry.InvokeOrClick(refresh);
    }

    // Taps the header's mark-all button and waits for the confirmation action
    // sheet whose title carries the packet scope (count + day label).
    private void OpenMarkAllReadSheet(string expectedTitle)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var markAll = _page.WaitForElementByName(AppResources.UnreadMarkPacketReadHint);
            UiRetry.InvokeOrClick(markAll);
            if (UiRetry.TryFindElementInScope(
                    _fixture.App,
                    _fixture.Automation,
                    cf => cf.ByName(expectedTitle),
                    TimeSpan.FromSeconds(6)) is not null)
            {
                return;
            }

            _page.DismissPopups();
        }

        UiRetry.WaitForElementInScope(
            _fixture.App,
            _fixture.Automation,
            cf => cf.ByName(expectedTitle),
            description: $"mark-all action sheet title '{expectedTitle}'");
    }

    // Activates the action sheet's "mark packet read" entry. Sheet options
    // render as list items inside a popup (see OpenFeedDetailActions), so the
    // ListItem filter is preferred, with the Button type as fallback for
    // renderers that expose options as buttons.
    private void ConfirmMarkAllRead()
    {
        var entry = UiRetry.TryFindElementInScope(
                        _fixture.App,
                        _fixture.Automation,
                        cf => cf.ByName(AppResources.ButtonMarkPacketRead)
                            .And(cf.ByControlType(ControlType.ListItem)),
                        TimeSpan.FromSeconds(5))
                    ?? _page.WaitForElementInScopeByName(AppResources.ButtonMarkPacketRead, ControlType.Button);
        UiRetry.InvokeOrClick(entry);
    }

    /// <summary>
    /// Descending default order: the day-packet feed (6 + 3 + 3 articles over
    /// three days) shows only the newest day's packet. The confirmation sheet
    /// carries the packet count and day label; confirming marks exactly that
    /// day read in the database while the older days stay unread and the list
    /// automatically loads the follow packet.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task MarkAllRead_Descending_MarksOnlyNewestDay_ThenLoadsNextPacket()
    {
        AddFeedAndSync("day-packet-feed");
        Assert.True(
            await FeedDbAssertions.ItemExistsAsync(_fixture.DatabasePath, "Packet day C article 3"),
            "The day-packet stub feed was not synced.");

        // Newest day (A, 6 articles) is the anchor and fills the packet alone —
        // day B must not be pulled in.
        _page.WaitForCard("Packet day A article 1", TimeSpan.FromSeconds(15));
        Assert.True(
            UiRetry.WaitFor(
                () => UiRetry.TryFindElementByName(Window, "Packet day B article 1", timeout: TimeSpan.FromMilliseconds(500)) is null,
                TimeSpan.FromSeconds(5)),
            "An article of the older day B is visible although the packet should be limited to day A.");

        var expectedTitle = string.Format(
            CultureInfo.CurrentCulture,
            AppResources.MarkAllReadPacketTitleFormat,
            6,
            PacketDayLabel(2026, 3, 10));
        OpenMarkAllReadSheet(expectedTitle);
        ConfirmMarkAllRead();

        Assert.True(
            await FeedDbAssertions.ItemHasReadFlagAsync(_fixture.DatabasePath, "Packet day A article 1", isRead: true),
            "'Packet day A article 1' was not marked read.");
        Assert.True(
            await FeedDbAssertions.ItemHasReadFlagAsync(_fixture.DatabasePath, "Packet day A article 6", isRead: true),
            "'Packet day A article 6' was not marked read.");
        Assert.True(
            await FeedDbAssertions.ItemHasReadFlagAsync(_fixture.DatabasePath, "Packet day B article 1", isRead: false),
            "'Packet day B article 1' must stay unread — the packet only covers day A.");
        Assert.True(
            await FeedDbAssertions.ItemHasReadFlagAsync(_fixture.DatabasePath, "Packet day C article 1", isRead: false),
            "'Packet day C article 1' must stay unread — the packet only covers day A.");

        // The fresh load resolves the next packet: day B (3 < 5) extends to day C.
        _page.WaitForCard("Packet day B article 1", TimeSpan.FromSeconds(15));
        Assert.True(
            UiRetry.WaitFor(
                () => UiRetry.TryFindElementByName(Window, "Packet day A article 1", timeout: TimeSpan.FromMilliseconds(500)) is null,
                TimeSpan.FromSeconds(5)),
            "A marked day-A article is still rendered in the unread list.");
    }

    /// <summary>
    /// Ascending order: the same feed anchors on the oldest day (C, 3
    /// articles); because it holds fewer than five articles the packet extends
    /// to day B. The confirmation sheet shows both day labels; confirming marks
    /// only the extended packet read and leaves day A unread.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task MarkAllRead_Ascending_MarksExtendedOldestPacket()
    {
        await FeedDbAssertions.SetUnreadSortOrderAsync(_fixture.DatabasePath, "asc");
        AddFeedAndSync("day-packet-feed");
        Assert.True(
            await FeedDbAssertions.ItemExistsAsync(_fixture.DatabasePath, "Packet day C article 3"),
            "The day-packet stub feed was not synced.");

        // Oldest day (C) anchors the packet; the extension pulls day B in, so
        // the list shows C and B articles but no day-A article.
        _page.WaitForCard("Packet day C article 1", TimeSpan.FromSeconds(15));
        Assert.True(
            UiRetry.WaitFor(
                () => UiRetry.TryFindElementByName(Window, "Packet day A article 1", timeout: TimeSpan.FromMilliseconds(500)) is null,
                TimeSpan.FromSeconds(5)),
            "An article of the newest day A is visible although the packet should cover days C and B only.");

        var expectedTitle = string.Format(
            CultureInfo.CurrentCulture,
            AppResources.MarkAllReadPacketTitleFormat,
            6,
            $"{PacketDayLabel(2026, 3, 8)} + {PacketDayLabel(2026, 3, 9)}");
        OpenMarkAllReadSheet(expectedTitle);
        ConfirmMarkAllRead();

        Assert.True(
            await FeedDbAssertions.ItemHasReadFlagAsync(_fixture.DatabasePath, "Packet day C article 3", isRead: true),
            "'Packet day C article 3' was not marked read.");
        Assert.True(
            await FeedDbAssertions.ItemHasReadFlagAsync(_fixture.DatabasePath, "Packet day B article 1", isRead: true),
            "'Packet day B article 1' was not marked read — the extension day belongs to the packet.");
        Assert.True(
            await FeedDbAssertions.ItemHasReadFlagAsync(_fixture.DatabasePath, "Packet day A article 1", isRead: false),
            "'Packet day A article 1' must stay unread — it is outside the C+B packet.");

        // The next packet is day A alone (6 articles).
        _page.WaitForCard("Packet day A article 1", TimeSpan.FromSeconds(15));
    }

    /// <summary>
    /// Marking a single article read does not pull the follow day in: the
    /// paged feed ships one article per day, so the descending packet covers
    /// the two newest days (1 &lt; 5, extended). After the newest article is
    /// marked read via the row's mark-read button (the same
    /// <c>MarkReadCommand</c> the swipe gesture invokes), only the remaining
    /// packet article stays visible — the third day must not appear.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    [Trait("Category", "E2E")]
    public async Task MarkRead_SingleArticle_DoesNotPromoteFollowDay()
    {
        AddFeedAndSync("paged-feed");
        Assert.True(
            await FeedDbAssertions.ItemExistsAsync(_fixture.DatabasePath, "Paged article 25"),
            "The paged stub feed was not synced.");

        // Packet = {25, 24} (each day holds 1 article < 5, so the packet extends).
        _page.WaitForCard("Paged article 25", TimeSpan.FromSeconds(15));
        _page.WaitForCard("Paged article 24", TimeSpan.FromSeconds(15));
        Assert.True(
            UiRetry.WaitFor(
                () => UiRetry.TryFindElementByName(Window, "Paged article 23", timeout: TimeSpan.FromMilliseconds(500)) is null,
                TimeSpan.FromSeconds(5)),
            "'Paged article 23' is visible although it is outside the two-day packet.");

        // Mark the newest article read through its row button — the button and
        // the swipe gesture both end in MarkReadCommand.
        var markRead = FindMarkReadButtonForCard("Paged article 25");
        Assert.NotNull(markRead);
        UiRetry.InvokeOrClick(markRead);

        Assert.True(
            await FeedDbAssertions.ItemHasReadFlagAsync(_fixture.DatabasePath, "Paged article 25", isRead: true),
            "'Paged article 25' was not marked read.");
        _page.WaitForCard("Paged article 24", TimeSpan.FromSeconds(15));
        Assert.True(
            UiRetry.WaitFor(
                () => UiRetry.TryFindElementByName(Window, "Paged article 23", timeout: TimeSpan.FromMilliseconds(500)) is null,
                TimeSpan.FromSeconds(5)),
            "'Paged article 23' was promoted into the list although the packet must not grow on single removals.");
    }

    // Locates the mark-read button inside a specific article row: the card's
    // accessibility name sits on the tap grid, the button is a sibling under
    // the same row container — so the search walks up from the title element
    // until an ancestor contains the button.
    private AutomationElement? FindMarkReadButtonForCard(string title)
    {
        AutomationElement? element = null;
        UiRetry.WaitFor(
            () =>
            {
                element = UiRetry.TryFindElementByName(Window, title, timeout: TimeSpan.FromMilliseconds(500));
                return element is not null;
            },
            TimeSpan.FromSeconds(10));

        while (element is not null)
        {
            var button = UiRetry.TryFindElement(
                element,
                cf => cf.ByName(AppResources.ArticleMarkAsRead),
                TimeSpan.FromMilliseconds(300));
            if (button is not null)
            {
                return button;
            }

            element = element.Parent;
        }

        return null;
    }
}
