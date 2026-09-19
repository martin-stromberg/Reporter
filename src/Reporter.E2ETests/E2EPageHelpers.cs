// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using Reporter.Core.Resources.Strings;

namespace Reporter.E2ETests;

/// <summary>
/// Shared page-level helpers for the E2E tests on top of <see cref="UiRetry"/>:
/// tab selection with page-anchor waits and the add-sheet flow used by both
/// <see cref="SmokeTests"/> and <see cref="ArticleLinkTests"/>.
/// </summary>
public sealed class E2EPageHelpers
{
    private static readonly TimeSpan ShortWait = TimeSpan.FromSeconds(2);

    private readonly ReporterAppFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="E2EPageHelpers"/> class.
    /// </summary>
    /// <param name="fixture">The shared app/stub-server fixture.</param>
    public E2EPageHelpers(ReporterAppFixture fixture)
    {
        _fixture = fixture;
    }

    // The window is re-resolved for every call: UIA proxies can go stale when
    // dialogs and popups come and go.
    private Window Window => _fixture.GetMainWindow();

    /// <summary>
    /// Waits for an element in the main window by its UIA name, optionally
    /// restricted to a control type.
    /// </summary>
    /// <param name="name">The UIA name.</param>
    /// <param name="controlType">An optional control type filter.</param>
    /// <returns>The found element.</returns>
    public AutomationElement WaitForElementByName(string name, ControlType? controlType = null)
        => UiRetry.WaitForElementByName(Window, name, controlType);

    /// <summary>
    /// Waits for an element anywhere in the app's UI scope (including popups)
    /// by its UIA name, optionally restricted to a control type.
    /// </summary>
    /// <param name="name">The UIA name.</param>
    /// <param name="controlType">An optional control type filter.</param>
    /// <returns>The found element.</returns>
    public AutomationElement WaitForElementInScopeByName(string name, ControlType? controlType = null)
        => UiRetry.WaitForElementInScope(
            _fixture.App,
            _fixture.Automation,
            cf => controlType is null
                ? cf.ByName(name)
                : cf.ByName(name).And(cf.ByControlType(controlType.Value)),
            description: $"name '{name}'");

    /// <summary>
    /// Like <see cref="WaitForElementInScopeByName"/> but returns
    /// <see langword="null"/> instead of throwing on timeout.
    /// </summary>
    /// <param name="name">The UIA name.</param>
    /// <param name="controlType">An optional control type filter.</param>
    /// <param name="timeout">An optional timeout overriding the 2 s default.</param>
    /// <returns>The found element or <see langword="null"/>.</returns>
    public AutomationElement? TryFindElementInScopeByName(
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

    /// <summary>
    /// Waits for a card by its title (see <see cref="UiRetry.WaitForCard"/> for
    /// the Group-first lookup with fallback). Works for feed cards and article
    /// cards alike — both map SemanticProperties.Description to the UIA name.
    /// </summary>
    /// <param name="title">The card title (UIA name).</param>
    /// <param name="timeout">An optional timeout overriding the 5 s default.</param>
    /// <returns>The found card element.</returns>
    public AutomationElement WaitForCard(string title, TimeSpan? timeout = null)
        => UiRetry.WaitForCard(Window, title, timeout ?? TimeSpan.FromSeconds(5));

    /// <summary>
    /// Adds a feed via the UI: selects the Feeds tab, opens the add sheet,
    /// types the stub feed URL and taps the direct-add button.
    /// </summary>
    /// <param name="stubName">The stub fixture name without the .xml suffix.</param>
    /// <returns>The feed card title used for later assertions.</returns>
    public string AddFeedViaUi(string stubName)
    {
        SelectTab(AppResources.TabFeeds);
        OpenAddSheet();
        UiRetry.SetText(WaitForUrlEntry(), $"{_fixture.Server.BaseUrl}/feeds/{stubName}.xml");
        var directAdd = WaitForElementByName(AppResources.ButtonDirectAdd, ControlType.Button);
        UiRetry.InvokeOrClick(directAdd);

        var cardTitle = $"{stubName}.xml";
        WaitForFeedCard(cardTitle);
        return cardTitle;
    }

    /// <summary>
    /// Dismisses leftover popups between tests: action-sheet light-dismiss
    /// layers, open alert/prompt buttons, the search results view and sheet
    /// overlays. Best effort — nothing here must throw.
    /// </summary>
    public void DismissPopups()
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
    }

    /// <summary>
    /// Scrolls the given list one viewport down — the scroll pattern when it
    /// actually moves the content, otherwise the mouse wheel at the list
    /// centre (providers can accept Scroll as a silent no-op).
    /// </summary>
    /// <param name="list">The list element to scroll.</param>
    public void ScrollListDown(AutomationElement list)
    {
        var needsWheel = true;
        if (list.Patterns.Scroll.TryGetPattern(out var scroll))
        {
            try
            {
                var before = scroll.VerticalScrollPercent.ValueOrDefault;
                scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement);
                Thread.Sleep(250);
                needsWheel = scroll.VerticalScrollPercent.ValueOrDefault <= before;
            }
            catch (Exception)
            {
                // Providers that reject Scroll — a list that currently has
                // nothing to scroll or a stale proxy — fall back to the
                // mouse-wheel path below.
            }
        }

        if (!needsWheel)
        {
            return;
        }

        var bounds = list.BoundingRectangle;
        if (bounds.IsEmpty)
        {
            return;
        }

        Mouse.MoveTo(bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));
        Mouse.Scroll(-5);
    }

    /// <summary>
    /// Selects a tab (also via its text label) and waits for a stable anchor of
    /// the target page so the active page is really rendered. On the narrow
    /// window only the first tabs render in the Shell tab strip; the rest live
    /// behind the NavigationView overflow button ("Mehr"/"More") and only enter
    /// the UIA tree once that flyout is opened. A pushed page left over from a
    /// previous test (e.g. an article detail) keeps covering the tab root, so
    /// the whole select-then-anchor sequence is retried and the pushed page is
    /// popped via its back button when the anchor stays away.
    /// </summary>
    /// <param name="tabTitle">The localized tab title.</param>
    public void SelectTab(string tabTitle)
    {
        var anchor = tabTitle switch
        {
            var t when t == AppResources.TabFeeds => AppResources.ActionAddFeed,
            var t when t == AppResources.TabCategories => AppResources.LabelCategoryName,
            var t when t == AppResources.TabUnread => AppResources.ButtonMarkAllRead,
            _ => null,
        };

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        while (true)
        {
            var selected = UiRetry.SelectTab(Window, tabTitle);
            Assert.True(
                selected,
                $"Tab '{tabTitle}' was not found. App running: {!_fixture.App.HasExited}");

            if (anchor is null ||
                UiRetry.TryFindElementByName(Window, anchor, timeout: TimeSpan.FromSeconds(8)) is not null)
            {
                return;
            }

            if (!TryPopPushedPage() && DateTime.UtcNow >= deadline)
            {
                // Same failure signature as before: the regular anchor wait
                // reports the missing element after the last retry.
                UiRetry.WaitForElementByName(Window, anchor);
                return;
            }
        }
    }

    /// <summary>
    /// Taps the feed card with the given title on the feed list and waits for
    /// the feed detail page's action button as its stable anchor. The card is
    /// activated by a real mouse click that can be swallowed while the add
    /// sheet is still closing or the card re-renders, and a card that sits
    /// below the visible list area reports no clickable point until it is
    /// scrolled into view — so the whole scroll-then-tap sequence is retried
    /// before the caller gives up.
    /// </summary>
    /// <param name="feedTitle">The displayed title of the feed card.</param>
    public void OpenFeedDetail(string feedTitle)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var target = WaitForFeedCard(feedTitle);
                ScrollCardIntoView(feedTitle);

                // The tap gesture lives on the card group; its ListViewItem
                // wrapper carries the same UIA name but only offers Select(),
                // which does not fire the gesture — so the group is preferred
                // for the click once it is materialized.
                var card = UiRetry.TryFindElementByName(
                               Window,
                               feedTitle,
                               ControlType.Group,
                               TimeSpan.FromSeconds(3))
                           ?? target;
                UiRetry.InvokeOrClick(card);
            }
            catch (FlaUI.Core.Exceptions.NoClickablePointException)
            {
                // The card was still sliding into the visible area; retry.
            }

            if (UiRetry.TryFindElementByName(
                    Window,
                    AppResources.ButtonFeedActions,
                    ControlType.Button,
                    TimeSpan.FromSeconds(6)) is not null)
            {
                return;
            }
        }

        // Same failure signature as a regular wait: reports the missing anchor
        // after the last retry.
        UiRetry.WaitForElementByName(Window, AppResources.ButtonFeedActions, ControlType.Button);
    }

    /// <summary>
    /// Finds a feed card even when the feed list has virtualized it away:
    /// off-screen cards do not exist in the UIA tree at all, so every card
    /// list is scrolled down step by step until the card materializes. The
    /// card can also sit above the current viewport — earlier searches and
    /// scrolls may have left the list scrolled down — so each pass resets the
    /// lists to their top first, and the whole scan repeats until the
    /// deadline so a card that appears late (e.g. a title updated by a
    /// background sync) is still caught.
    /// </summary>
    /// <param name="feedTitle">The displayed title of the feed card.</param>
    /// <returns>The found card element.</returns>
    public AutomationElement WaitForFeedCard(string feedTitle)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        while (true)
        {
            ScrollListToTop();

            for (var attempt = 0; attempt < 12; attempt++)
            {
                var card = TryFindCard(feedTitle);
                if (card is not null)
                {
                    return card;
                }

                ScrollListDownOnce();
            }

            if (DateTime.UtcNow >= deadline)
            {
                // Same failure signature as a regular wait: reports the
                // missing card after the last scan pass.
                return UiRetry.WaitForCard(Window, feedTitle);
            }
        }
    }

    // Finds the window's card lists: the first ControlType.List in tree order
    // can be the Shell tab strip (its entries are TabItems), so lists are
    // identified by their ListItem children; a list without items is only
    // used as fallback when no populated list is rendered. More than one
    // list can match — e.g. a leftover page still hosting a CollectionView —
    // so all candidates are returned for the scroll scan.
    private AutomationElement[] FindCardLists()
    {
        var lists = UiRetry.TryFindElements(
            Window,
            cf => cf.ByControlType(ControlType.List),
            TimeSpan.FromSeconds(2));
        var withItems = lists.Where(HasListItems).ToArray();
        return withItems.Length > 0 ? withItems : lists;
    }

    // Whether the list has at least one realized ListItem child; stale
    // proxies while pages switch simply count as empty.
    private static bool HasListItems(AutomationElement list)
    {
        try
        {
            return list.FindFirstDescendant(cf => cf.ByControlType(ControlType.ListItem)) is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // The card itself: the border group when already realized (preferred —
    // it is the clickable element), otherwise any element carrying the
    // title, e.g. the still child-less ListViewItem wrapper.
    private AutomationElement? TryFindCard(string title)
        => UiRetry.TryFindElementByName(Window, title, ControlType.Group, TimeSpan.FromMilliseconds(800))
           ?? UiRetry.TryFindElementByName(Window, title, timeout: TimeSpan.FromMilliseconds(400));

    // Resets every card list to its top so a downward scan covers all cards:
    // the scroll pattern's SetScrollPercent when offered — providers that
    // silently ignore it are additionally dragged up by the mouse wheel at
    // the list centre. A no-op when no list is rendered.
    private void ScrollListToTop()
    {
        foreach (var list in FindCardLists())
        {
            if (list.Patterns.Scroll.TryGetPattern(out var scroll))
            {
                try
                {
                    // -1 keeps the horizontal position untouched; 0 pins the
                    // vertical offset to the top of the list.
                    scroll.SetScrollPercent(-1, 0);
                }
                catch (Exception)
                {
                    // Providers that reject SetScrollPercent are covered by
                    // the mouse-wheel pass below.
                }
            }

            var bounds = list.BoundingRectangle;
            if (bounds.IsEmpty)
            {
                continue;
            }

            Mouse.MoveTo(bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));
            for (var i = 0; i < 10; i++)
            {
                Mouse.Scroll(5);
            }
        }
    }

    // Scrolls every card list of the window one viewport down.
    // A no-op when no list is rendered.
    private void ScrollListDownOnce()
    {
        foreach (var list in FindCardLists())
        {
            ScrollListDown(list);
        }
    }

    /// <summary>
    /// Opens the feed action sheet on the detail page and waits until the
    /// expected entry is rendered (options are list items inside a popup, not
    /// buttons). A tap on the actions button can be swallowed while the page
    /// is still settling, so the open is retried before the caller gives up.
    /// </summary>
    /// <param name="expectedEntryName">A localized action name that must appear in the sheet.</param>
    public void OpenFeedDetailActions(string expectedEntryName)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var actions = UiRetry.WaitForElementByName(Window, AppResources.ButtonFeedActions, ControlType.Button);
            UiRetry.InvokeOrClick(actions);
            if (UiRetry.TryFindElementInScope(
                    _fixture.App,
                    _fixture.Automation,
                    cf => cf.ByName(expectedEntryName),
                    TimeSpan.FromSeconds(10)) is not null)
            {
                return;
            }

            DismissPopups();
        }

        // Same failure signature as a regular wait: reports the missing entry
        // after the last retry.
        UiRetry.WaitForElementInScope(
            _fixture.App,
            _fixture.Automation,
            cf => cf.ByName(expectedEntryName),
            description: $"action sheet entry '{expectedEntryName}'");
    }

    /// <summary>
    /// Navigates back from the detail page to the feed list via the page's own
    /// back button and waits for the list anchor.
    /// </summary>
    public void NavigateBackToFeedList()
    {
        var back = UiRetry.WaitForElement(
            Window,
            cf => cf.ByName(AppResources.AccessibilityBack).And(cf.ByControlType(ControlType.Button)),
            description: "detail back button");
        UiRetry.InvokeOrClick(back);
        UiRetry.WaitForElementByName(Window, AppResources.ActionAddFeed, ControlType.Button);
    }

    // Best-effort scroll-into-view for a named card: the CollectionView list
    // item wrapper exposes the scroll-item pattern, so the nearest ancestor
    // offering it is asked to bring the card into the visible area. Elements
    // already visible simply have no such ancestor — then this is a no-op.
    private void ScrollCardIntoView(string title)
    {
        try
        {
            var element = UiRetry.TryFindElementByName(Window, title, timeout: TimeSpan.FromSeconds(5));
            while (element is not null)
            {
                if (element.Patterns.ScrollItem.TryGetPattern(out var scrollItem))
                {
                    scrollItem.ScrollIntoView();
                    return;
                }

                element = element.Parent;
            }
        }
        catch (Exception)
        {
            // Stale proxies or a tree rebuild while the list re-renders: the
            // tap retry handles the case where the scroll did not happen.
        }
    }

    /// <summary>
    /// Opens the add sheet by tapping the "+" button and waits for the URL entry.
    /// </summary>
    public void OpenAddSheet()
    {
        var addButton = UiRetry.WaitForElementByName(Window, AppResources.ActionAddFeed, ControlType.Button);
        UiRetry.InvokeOrClick(addButton);
        WaitForUrlEntry();
    }

    /// <summary>
    /// Finds the URL/search entry of the add sheet. MAUI does not propagate
    /// x:Name as automation id on Windows, so the entry is located by its
    /// accessible name (SemanticProperties.Description) or as the sheet's edit
    /// control.
    /// </summary>
    /// <returns>The URL entry element.</returns>
    public AutomationElement WaitForUrlEntry()
        => UiRetry.WaitForElement(
            Window,
            cf => cf.ByName(AppResources.PlaceholderFeedSearch).Or(cf.ByControlType(ControlType.Edit)),
            description: "URL entry");

    /// <summary>
    /// Finds the URL entry of the feed detail page's edit sheet by its
    /// accessible name (SemanticProperties.Description).
    /// </summary>
    /// <returns>The edit-sheet URL entry element.</returns>
    public AutomationElement WaitForEditUrlEntry()
        => UiRetry.WaitForElement(
            Window,
            cf => cf.ByName(AppResources.PlaceholderFeedEditUrl).And(cf.ByControlType(ControlType.Edit)),
            description: "edit sheet URL entry");

    // Returns whether a back navigation was triggered. Covers both the Shell
    // chrome back button (WinUI NavigationView) and the article detail's own
    // back button in the reader control bar — the two affordances that pop a
    // page pushed onto a tab's navigation stack.
    private bool TryPopPushedPage()
    {
        var back = UiRetry.TryFindElement(
            Window,
            cf => cf.ByAutomationId("NavigationViewBackButton")
                .Or(cf.ByName(AppResources.AccessibilityBack).And(cf.ByControlType(ControlType.Button))),
            TimeSpan.FromSeconds(1));
        if (back is null)
        {
            return false;
        }

        UiRetry.InvokeOrClick(back);
        return true;
    }
}
