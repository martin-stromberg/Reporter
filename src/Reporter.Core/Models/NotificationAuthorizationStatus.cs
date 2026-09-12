// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

namespace Reporter.Core.Models;

/// <summary>
/// Describes the system authorization status for local notifications.
/// </summary>
public enum NotificationAuthorizationStatus
{
    /// <summary>
    /// The current platform does not support local notifications.
    /// </summary>
    Unsupported,

    /// <summary>
    /// The user has not yet been asked for notification authorization.
    /// </summary>
    NotDetermined,

    /// <summary>
    /// The user has denied notification authorization in the system settings.
    /// </summary>
    Denied,

    /// <summary>
    /// Notifications are authorized (including provisional or ephemeral grants).
    /// </summary>
    Authorized,
}
