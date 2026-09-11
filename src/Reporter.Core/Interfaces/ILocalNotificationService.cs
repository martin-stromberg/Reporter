namespace Reporter.Core.Interfaces;

/// <summary>
/// Abstracts the platform facility for local notifications (for example the iOS
/// UserNotifications framework). Implementations are no-ops on platforms that
/// do not support local notifications.
/// </summary>
public interface ILocalNotificationService
{
    /// <summary>
    /// Gets a value indicating whether the current platform supports local notifications.
    /// </summary>
    /// <value><c>true</c> when the platform can display local notifications; otherwise <c>false</c>.</value>
    bool IsSupported { get; }

    /// <summary>
    /// Requests the user's authorization to display local notifications.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> when notifications are authorized.</returns>
    Task<bool> RequestAuthorizationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the current system authorization status without prompting the user.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> when notifications are currently authorized.</returns>
    Task<bool> IsAuthorizedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a local notification immediately. A pending or delivered notification with the
    /// same <paramref name="identifier"/> is replaced instead of duplicated.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="body">The notification body.</param>
    /// <param name="identifier">The stable notification identifier.</param>
    /// <param name="userInfo">Optional string key/value pairs forwarded to the platform payload for tap handling (deep linking).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ShowAsync(
        string title,
        string body,
        string identifier,
        IReadOnlyDictionary<string, string>? userInfo = null,
        CancellationToken cancellationToken = default);
}
