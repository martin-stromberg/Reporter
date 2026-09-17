// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Services;

/// <summary>
/// The action the article WebView should take for a navigation request,
/// as decided by <see cref="WebViewNavigationGuard.DecideAction"/>.
/// </summary>
public enum WebViewNavigationAction
{
    /// <summary>
    /// The navigation is a local content load and proceeds inside the WebView.
    /// </summary>
    Proceed,

    /// <summary>
    /// The navigation targets an external web resource while online: it is
    /// cancelled inside the WebView and the URL opens in the system browser.
    /// </summary>
    CancelAndOpenExternally,

    /// <summary>
    /// The navigation targets an external web resource while offline: it is
    /// cancelled inside the WebView and the offline hint is shown.
    /// </summary>
    CancelAndShowOfflineHint,
}
