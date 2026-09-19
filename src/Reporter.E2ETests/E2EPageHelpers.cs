// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Reporter.Core.Resources.Strings;

namespace Reporter.E2ETests;

/// <summary>
/// Shared page-level helpers for the E2E tests on top of <see cref="UiRetry"/>:
/// tab selection with page-anchor waits and the add-sheet flow used by both
/// <see cref="SmokeTests"/> and <see cref="ArticleLinkTests"/>.
/// </summary>
public sealed class E2EPageHelpers
{
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
