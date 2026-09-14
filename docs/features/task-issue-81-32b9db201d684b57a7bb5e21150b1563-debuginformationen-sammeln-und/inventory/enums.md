<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Enums

## `NotificationAuthorizationStatus`
Datei: `src/Reporter.Core/Models/NotificationAuthorizationStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unsupported` | Plattform unterstützt keine lokalen Benachrichtigungen |
| `NotDetermined` | Nutzer wurde noch nicht nach Autorisierung gefragt |
| `Denied` | Nutzer hat die Autorisierung in den Systemeinstellungen verweigert |
| `Authorized` | Benachrichtigungen sind erlaubt (inkl. provisional/ephemeral) |

Rückgabetyp von `ILocalNotificationService.GetAuthorizationStatusAsync`; wird von `SettingsViewModel.ApplyAuthorizationStatus` auf `NotificationPermissionDenied`/`NotificationPermissionNotDetermined` gemappt. Referenzmuster für einen optionalen Plattformfähigkeits-Status (analog zu `IEmailService.IsSupported`).

Hinweis: `FeedHealth` (Konstanten `OK`/`Warning`/`Error`) ist eine `static class`, kein Enum — siehe [models.md](models.md).
