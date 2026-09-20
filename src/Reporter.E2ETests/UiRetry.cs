// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace Reporter.E2ETests;

/// <summary>
/// Polling and interaction helpers for the FlaUI-based smoke tests. UIA access is
/// timing- and focus-sensitive, so every lookup retries until a generous timeout
/// instead of failing on the first miss.
/// </summary>
public static class UiRetry
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Polls <paramref name="root"/> for the first descendant matching
    /// <paramref name="condition"/> and throws when it does not appear in time.
    /// </summary>
    /// <param name="root">The element to search below.</param>
    /// <param name="condition">The condition factory expression.</param>
    /// <param name="timeout">An optional timeout overriding the 20 s default.</param>
    /// <param name="description">An optional description used in the timeout message.</param>
    /// <returns>The found element.</returns>
    public static AutomationElement WaitForElement(
        AutomationElement root,
        Func<ConditionFactory, ConditionBase> condition,
        TimeSpan? timeout = null,
        string? description = null)
    {
        return TryFindElement(root, condition, timeout)
            ?? throw new TimeoutException($"Element not found within {timeout ?? DefaultTimeout}: {description ?? "(unnamed)"}");
    }

    /// <summary>
    /// Polls <paramref name="root"/> for the first descendant matching
    /// <paramref name="condition"/> and returns <see langword="null"/> on timeout.
    /// </summary>
    /// <param name="root">The element to search below.</param>
    /// <param name="condition">The condition factory expression.</param>
    /// <param name="timeout">An optional timeout overriding the 20 s default.</param>
    /// <returns>The found element or <see langword="null"/>.</returns>
    public static AutomationElement? TryFindElement(
        AutomationElement root,
        Func<ConditionFactory, ConditionBase> condition,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (true)
        {
            try
            {
                var element = root.FindFirstDescendant(condition);
                if (element is not null)
                {
                    return element;
                }
            }
            catch (Exception)
            {
                // The UIA tree can be transiently unavailable while the app renders.
            }

            if (DateTime.UtcNow >= deadline)
            {
                return null;
            }

            Thread.Sleep(PollInterval);
        }
    }

    /// <summary>
    /// Polls <paramref name="root"/> for all descendants matching
    /// <paramref name="condition"/> and returns them once at least one exists,
    /// or an empty array on timeout.
    /// </summary>
    /// <param name="root">The element to search below.</param>
    /// <param name="condition">The condition factory expression.</param>
    /// <param name="timeout">An optional timeout overriding the 20 s default.</param>
    /// <returns>The found elements or an empty array.</returns>
    public static AutomationElement[] TryFindElements(
        AutomationElement root,
        Func<ConditionFactory, ConditionBase> condition,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (true)
        {
            try
            {
                var elements = root.FindAllDescendants(condition);
                if (elements.Length > 0)
                {
                    return elements;
                }
            }
            catch (Exception)
            {
                // The UIA tree can be transiently unavailable while the app renders.
            }

            if (DateTime.UtcNow >= deadline)
            {
                return [];
            }

            Thread.Sleep(PollInterval);
        }
    }

    /// <summary>
    /// Waits for a descendant of <paramref name="root"/> with the given UIA name,
    /// optionally restricted to a control type.
    /// </summary>
    /// <param name="root">The element to search below.</param>
    /// <param name="name">The UIA name (for MAUI controls: text or <c>SemanticProperties.Description</c>).</param>
    /// <param name="controlType">An optional control type filter.</param>
    /// <param name="timeout">An optional timeout overriding the 20 s default.</param>
    /// <returns>The found element.</returns>
    public static AutomationElement WaitForElementByName(
        AutomationElement root,
        string name,
        ControlType? controlType = null,
        TimeSpan? timeout = null)
    {
        return WaitForElement(
            root,
            cf => controlType is null
                ? cf.ByName(name)
                : cf.ByName(name).And(cf.ByControlType(controlType.Value)),
            timeout,
            $"name '{name}'");
    }

    /// <summary>
    /// Like <see cref="WaitForElementByName"/> but returns <see langword="null"/> instead
    /// of throwing when the element does not appear in time.
    /// </summary>
    /// <param name="root">The element to search below.</param>
    /// <param name="name">The UIA name to look for.</param>
    /// <param name="controlType">An optional control type filter.</param>
    /// <param name="timeout">An optional timeout overriding the 20 s default.</param>
    /// <returns>The found element or <see langword="null"/>.</returns>
    public static AutomationElement? TryFindElementByName(
        AutomationElement root,
        string name,
        ControlType? controlType = null,
        TimeSpan? timeout = null)
    {
        return TryFindElement(
            root,
            cf => controlType is null
                ? cf.ByName(name)
                : cf.ByName(name).And(cf.ByControlType(controlType.Value)),
            timeout);
    }

    /// <summary>
    /// Waits for an element anywhere in the app's UI scope: the main window is
    /// re-resolved on every poll (stale proxies after dialogs), then the process'
    /// other top-level windows and popups are searched (MAUI action sheets and
    /// dialogs may render in separate UIA roots).
    /// </summary>
    /// <param name="app">The running application.</param>
    /// <param name="automation">The automation instance.</param>
    /// <param name="condition">The condition factory expression.</param>
    /// <param name="timeout">An optional timeout overriding the 20 s default.</param>
    /// <param name="description">An optional description used in the timeout message.</param>
    /// <returns>The found element.</returns>
    public static AutomationElement WaitForElementInScope(
        Application app,
        AutomationBase automation,
        Func<ConditionFactory, ConditionBase> condition,
        TimeSpan? timeout = null,
        string? description = null)
    {
        return TryFindElementInScope(app, automation, condition, timeout)
            ?? throw new TimeoutException($"Element not found within {timeout ?? DefaultTimeout}: {description ?? "(unnamed)"}");
    }

    /// <summary>
    /// Like <see cref="WaitForElementInScope"/> but returns <see langword="null"/> on timeout.
    /// </summary>
    /// <param name="app">The running application.</param>
    /// <param name="automation">The automation instance.</param>
    /// <param name="condition">The condition factory expression.</param>
    /// <param name="timeout">An optional timeout overriding the 20 s default.</param>
    /// <returns>The found element or <see langword="null"/>.</returns>
    public static AutomationElement? TryFindElementInScope(
        Application app,
        AutomationBase automation,
        Func<ConditionFactory, ConditionBase> condition,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (true)
        {
            try
            {
                var element = FindInScope(app, automation, condition);
                if (element is not null)
                {
                    return element;
                }
            }
            catch (Exception)
            {
                // Transient UIA tree unavailability while dialogs open or close.
            }

            if (DateTime.UtcNow >= deadline)
            {
                return null;
            }

            Thread.Sleep(PollInterval);
        }
    }

    /// <summary>
    /// Selects a Shell tab by its localized title and returns whether a
    /// selectable candidate was found in time. Prefers the selection-item
    /// pattern, falls back to invoking/clicking a non-text candidate and opens
    /// the NavigationView overflow ("Mehr"/"More") when the tab is not yet in
    /// the UIA tree — on the narrow window only the first tabs render in the
    /// Shell tab strip. Callers assert on the result and wait for an anchor of
    /// the target page themselves.
    /// </summary>
    /// <param name="window">The main window to search.</param>
    /// <param name="tabTitle">The localized tab title.</param>
    /// <returns>Whether the tab was selected.</returns>
    public static bool SelectTab(Window window, string tabTitle)
    {
        return WaitFor(() =>
        {
            var candidates = window.FindAllDescendants(cf => cf.ByName(tabTitle));
            foreach (var candidate in candidates)
            {
                if (candidate.Patterns.SelectionItem.TryGetPattern(out var selection))
                {
                    selection.Select();
                    return true;
                }
            }

            var clickable = candidates.FirstOrDefault(e => e.ControlType != ControlType.Text);
            if (clickable is not null)
            {
                InvokeOrClick(clickable);
                return true;
            }

            var overflow = window.FindFirstDescendant(cf => cf.ByAutomationId("TopNavOverflowButton"));
            if (overflow is not null)
            {
                InvokeOrClick(overflow);
            }

            return false;
        });
    }

    /// <summary>
    /// Waits for a feed or category card by its title. Cards are MAUI borders
    /// exposed as a Group; the CollectionView ListItem wrapper carries the same
    /// name, so the group is preferred — a Select() on the list item would not
    /// fire the tap gesture. When no group appears within
    /// <paramref name="timeout"/>, any element with the title is accepted as a
    /// fallback (with the regular 20 s default timeout).
    /// </summary>
    /// <param name="root">The element to search below.</param>
    /// <param name="title">The card title (UIA name).</param>
    /// <param name="timeout">An optional timeout for the group lookup overriding the 20 s default.</param>
    /// <returns>The found card element.</returns>
    public static AutomationElement WaitForCard(
        AutomationElement root,
        string title,
        TimeSpan? timeout = null)
    {
        return TryFindElementByName(root, title, ControlType.Group, timeout)
            ?? WaitForElementByName(root, title);
    }

    /// <summary>
    /// Polls <paramref name="condition"/> until it returns <see langword="true"/> or
    /// the timeout elapses.
    /// </summary>
    /// <param name="condition">The condition to poll.</param>
    /// <param name="timeout">An optional timeout overriding the 20 s default.</param>
    /// <returns>Whether the condition became true in time.</returns>
    public static bool WaitFor(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (condition())
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // Treat exceptions like a not-yet-satisfied condition.
            }

            Thread.Sleep(PollInterval);
        }

        return false;
    }

    /// <summary>
    /// Activates an element: via the invoke pattern when supported, then via the
    /// selection-item pattern (action sheet options are list items), otherwise via
    /// a real mouse click on the element centre (needed for MAUI tap gestures).
    /// </summary>
    /// <param name="element">The element to activate.</param>
    public static void InvokeOrClick(AutomationElement element)
    {
        if (element.Patterns.Invoke.TryGetPattern(out var invoke))
        {
            invoke.Invoke();
            return;
        }

        if (element.Patterns.SelectionItem.TryGetPattern(out var selectionItem))
        {
            selectionItem.Select();
            return;
        }

        try
        {
            element.Focus();
        }
        catch (Exception)
        {
            // Non-focusable elements (e.g. plain groups) are clicked anyway.
        }

        BringToFront(element);
        element.Click(moveMouse: true);
    }

    /// <summary>
    /// Enters text into an element: via the value pattern when supported, otherwise
    /// by focusing the element and typing the keys.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <param name="text">The text to enter.</param>
    public static void SetText(AutomationElement element, string text)
    {
        if (element.Patterns.Value.TryGetPattern(out var value))
        {
            value.SetValue(text);
            return;
        }

        element.Focus();
        // Without the value pattern plain typing would append to a prefilled
        // entry (e.g. the edit sheet's URL), so the field is cleared first.
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(VirtualKeyShort.DELETE);
        Keyboard.Type(text);
    }

    // Raises the window hosting the element before a real mouse click: another
    // window (e.g. a browser opened by a previous test) can cover the app and
    // swallow the click aimed at the element's screen coordinates.
    private static void BringToFront(AutomationElement element)
    {
        try
        {
            var current = element;
            while (current.ControlType != ControlType.Window && current.Parent is { } parent)
            {
                current = parent;
            }

            current.AsWindow()?.SetForeground();
            Thread.Sleep(150);
        }
        catch (Exception)
        {
            // Best effort — the click is attempted either way.
        }
    }

    private static AutomationElement? FindInScope(
        Application app,
        AutomationBase automation,
        Func<ConditionFactory, ConditionBase> condition)
    {
        // The desktop children of the process include the main window as well as
        // popups (e.g. WinUI PopupRoot) that are not top-level windows.
        var desktop = automation.GetDesktop();
        foreach (var topLevel in desktop.FindAllChildren(cf => cf.ByProcessId(app.ProcessId)))
        {
            var element = topLevel.FindFirstDescendant(condition);
            if (element is not null)
            {
                return element;
            }
        }

        return null;
    }
}
