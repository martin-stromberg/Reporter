using Reporter.Core.Interfaces;
#if IOS
using Foundation;
using UserNotifications;
#endif

namespace Reporter.Services;

/// <summary>
/// Shows local notifications via the iOS UserNotifications framework.
/// On other platforms the service is a no-op.
/// </summary>
public class LocalNotificationService : ILocalNotificationService
{
#if IOS
    private const UNAuthorizationOptions RequestedAuthorizationOptions =
        UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;
#endif

    /// <inheritdoc />
    public bool IsSupported =>
#if IOS
        true;
#else
        false;
#endif

    /// <inheritdoc />
    public async Task<bool> RequestAuthorizationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if IOS
        return await EnsureAuthorizedAsync(cancellationToken).ConfigureAwait(false);
#else
        return await Task.FromResult(false);
#endif
    }

    /// <inheritdoc />
    public async Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if IOS
        var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync()
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        return IsAuthorized(settings.AuthorizationStatus);
#else
        return await Task.FromResult(false);
#endif
    }

    /// <inheritdoc />
    public async Task ShowAsync(
        string title,
        string body,
        string identifier,
        IReadOnlyDictionary<string, string>? userInfo = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if IOS
        if (!await EnsureAuthorizedAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var content = new UNMutableNotificationContent
        {
            Title = title,
            Body = body,
            Sound = UNNotificationSound.Default,
            UserInfo = BuildUserInfo(userInfo),
        };

        var request = UNNotificationRequest.FromIdentifier(identifier, content, null);
        await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
#else
        await Task.CompletedTask;
#endif
    }

#if IOS
    private static async Task<bool> EnsureAuthorizedAsync(CancellationToken cancellationToken)
    {
        var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync()
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        if (settings.AuthorizationStatus == UNAuthorizationStatus.NotDetermined)
        {
            var (granted, _) = await UNUserNotificationCenter.Current
                .RequestAuthorizationAsync(RequestedAuthorizationOptions)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            return granted;
        }

        return IsAuthorized(settings.AuthorizationStatus);
    }

    private static NSDictionary BuildUserInfo(IReadOnlyDictionary<string, string>? userInfo)
    {
        // Der Notification-Identifier ist bereits als UNNotificationRequest-Identifier
        // gesetzt und muss nicht zusaetzlich im UserInfo-Payload uebertragen werden.
        if (userInfo is null || userInfo.Count == 0)
        {
            return new NSDictionary();
        }

        var keys = new List<NSString>(userInfo.Count);
        var values = new List<NSString>(userInfo.Count);
        foreach (var pair in userInfo)
        {
            keys.Add(new NSString(pair.Key));
            values.Add(new NSString(pair.Value));
        }

        return NSDictionary.FromObjectsAndKeys(values.ToArray(), keys.ToArray());
    }

    private static bool IsAuthorized(UNAuthorizationStatus status)
    {
        return status is UNAuthorizationStatus.Authorized
            or UNAuthorizationStatus.Provisional
            or UNAuthorizationStatus.Ephemeral;
    }
#endif
}
