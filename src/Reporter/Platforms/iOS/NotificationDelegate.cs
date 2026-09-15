// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

using Foundation;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using UserNotifications;

namespace Reporter;

/// <summary>
/// Suppresses the foreground presentation of local notifications — notifications
/// are only surfaced when iOS delivers them while the app is not in the foreground
/// (the OS background refresh) — and handles notification taps (in-app navigation
/// to the article or the unread list).
/// </summary>
public class NotificationDelegate : UNUserNotificationCenterDelegate
{
    /// <inheritdoc />
    public override void WillPresentNotification(
        UNUserNotificationCenter center,
        UNNotification notification,
        Action<UNNotificationPresentationOptions> completionHandler)
    {
        // iOS ruft diese Methode nur bei Vordergrund-App auf — die Mitteilung
        // wird komplett unterdrueckt (kein Banner, kein Sound, kein Eintrag im
        // Mitteilungszentrum); Hintergrund-Zustellungen zeigt iOS automatisch an.
        completionHandler(UNNotificationPresentationOptions.None);
    }

    /// <inheritdoc />
    public override async void DidReceiveNotificationResponse(
        UNUserNotificationCenter center,
        UNNotificationResponse response,
        Action completionHandler)
    {
        // Nur das Antippen der Mitteilung selbst (Default-Aktion) darf eine
        // Navigation ausloesen; das Wegwischen (Dismiss) oder Aktions-Buttons nicht.
        if (!response.IsDefaultAction)
        {
            completionHandler();
            return;
        }

        try
        {
            var userInfo = response.Notification.Request.Content.UserInfo;
            var itemId = (userInfo?.ValueForKey(new NSString("itemId")) as NSString)?.ToString();
            var feedId = (userInfo?.ValueForKey(new NSString("feedId")) as NSString)?.ToString();
            var link = (userInfo?.ValueForKey(new NSString("link")) as NSString)?.ToString();

            var route = !string.IsNullOrEmpty(itemId)
                ? $"articledetail?itemId={itemId}"
                : !string.IsNullOrEmpty(feedId) ? "//unread" : null;

            var navigatedInApp = false;
            if (route is not null)
            {
                navigatedInApp = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Shell.Current is null)
                    {
                        return false;
                    }

                    await Shell.Current.GoToAsync(route);
                    return true;
                }).ConfigureAwait(false);
            }

            // Fallback fuer den Kaltstart, wenn die Shell noch nicht bereit ist.
            if (!navigatedInApp && !string.IsNullOrEmpty(link))
            {
                await MainThread.InvokeOnMainThreadAsync(() => Launcher.Default.OpenAsync(link)).ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            // Ein Antippen darf die App niemals abstuerzen lassen; iOS hat sie bereits in den Vordergrund geholt.
        }
        finally
        {
            completionHandler();
        }
    }
}
