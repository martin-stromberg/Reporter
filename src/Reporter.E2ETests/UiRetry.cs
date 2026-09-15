// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;

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
        Keyboard.Type(text);
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
