using Reporter.Core.Interfaces;
using Reporter.Core.Models;

namespace Reporter.Tests;

/// <summary>
/// A hand-written <see cref="ILocalNotificationService"/> fake that records shown notifications.
/// </summary>
public sealed class FakeLocalNotificationService : ILocalNotificationService
{
    /// <summary>
    /// Gets the recorded <see cref="ShowAsync"/> calls in call order.
    /// </summary>
    /// <value>The recorded <see cref="ShowAsync"/> calls in call order.</value>
    public List<ShownNotification> ShownNotifications { get; } = new();

    /// <summary>
    /// Gets or sets the value returned by <see cref="RequestAuthorizationAsync"/>.
    /// </summary>
    /// <value>The value returned by <see cref="RequestAuthorizationAsync"/>.</value>
    public bool AuthorizationResult { get; set; } = true;

    /// <summary>
    /// Gets or sets the value returned by <see cref="GetAuthorizationStatusAsync"/>.
    /// </summary>
    /// <value>The value returned by <see cref="GetAuthorizationStatusAsync"/>.</value>
    public NotificationAuthorizationStatus AuthorizationStatus { get; set; } = NotificationAuthorizationStatus.Authorized;

    /// <summary>
    /// Gets or sets a value indicating whether the fake reports platform support for local notifications.
    /// </summary>
    /// <value>The value returned by <see cref="IsSupported"/>.</value>
    public bool IsSupported { get; set; } = true;

    /// <summary>
    /// Gets the number of recorded <see cref="RequestAuthorizationAsync"/> calls.
    /// </summary>
    /// <value>The number of recorded <see cref="RequestAuthorizationAsync"/> calls.</value>
    public int RequestAuthorizationCallCount { get; private set; }

    /// <inheritdoc />
    public Task<bool> RequestAuthorizationAsync(CancellationToken cancellationToken = default)
    {
        RequestAuthorizationCallCount++;
        return Task.FromResult(AuthorizationResult);
    }

    /// <inheritdoc />
    public Task<NotificationAuthorizationStatus> GetAuthorizationStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AuthorizationStatus);
    }

    /// <inheritdoc />
    public Task ShowAsync(
        string title,
        string body,
        string identifier,
        IReadOnlyDictionary<string, string>? userInfo = null,
        CancellationToken cancellationToken = default)
    {
        ShownNotifications.Add(new ShownNotification(title, body, identifier, userInfo));
        return Task.CompletedTask;
    }
}

/// <summary>
/// A recorded <see cref="ILocalNotificationService.ShowAsync"/> call.
/// </summary>
/// <param name="Title">The notification title.</param>
/// <param name="Body">The notification body.</param>
/// <param name="Identifier">The notification identifier.</param>
/// <param name="UserInfo">The optional user-info payload, or <see langword="null"/>.</param>
/// <returns>The recorded notification call.</returns>
public sealed record ShownNotification(
    string Title,
    string Body,
    string Identifier,
    IReadOnlyDictionary<string, string>? UserInfo);
