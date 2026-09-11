using Foundation;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using UserNotifications;

namespace Reporter;

/// <summary>
/// Presents local notifications as banners while the app is in the foreground
/// and handles notification taps (in-app navigation to the article or the unread list).
/// </summary>
public class NotificationDelegate : UNUserNotificationCenterDelegate
{
    /// <inheritdoc />
    public override void WillPresentNotification(
        UNUserNotificationCenter center,
        UNNotification notification,
        Action<UNNotificationPresentationOptions> completionHandler)
    {
        completionHandler(UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.List | UNNotificationPresentationOptions.Sound);
    }

    /// <inheritdoc />
    public override async void DidReceiveNotificationResponse(
        UNUserNotificationCenter center,
        UNNotificationResponse response,
        Action completionHandler)
    {
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
                await Launcher.Default.OpenAsync(link);
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
