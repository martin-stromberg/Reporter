<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Enums

Ein Enum bzw. Rangfolge-Typ für die in der Anforderung geforderte Trefferart (exakter Domain-/URL-/Titel-/Keyword-Treffer) existiert nicht. Im betroffenen Bereich gibt es genau einen echten Enum sowie eine statische Klasse mit String-Konstanten, die faktisch als Status-Enum dient.

## `NotificationAuthorizationStatus`
Datei: `src/Reporter.Core/Models/NotificationAuthorizationStatus.cs`

Einziger Enum im `Reporter.Core.Models`-Namespace; wird von `ILocalNotificationService.GetAuthorizationStatusAsync` verwendet. `FeedsViewModel` nutzt den Enum nicht direkt, nur `ILocalNotificationService.IsSupported` (Eigenschaft `NotificationsSupported`).

| Wert | Bedeutung |
|------|-----------|
| `Unsupported` | Plattform unterstützt keine lokalen Benachrichtigungen |
| `NotDetermined` | Nutzer wurde noch nicht gefragt |
| `Denied` | In den Systemeinstellungen verweigert |
| `Authorized` | Autorisiert (inkl. provisorischer Grants) |

## `FeedHealth` (statische Klasse, de-facto Status-Enum)
Datei: `src/Reporter.Core/Services/FeedHealth.cs`

Kein `enum`, sondern String-Konstanten — relevant, weil `Feed.HealthStatus`/`FeedListItem.HealthStatus` und die XAML-DataTrigger in `FeedsPage.xaml` auf diesen Strings basieren:

| Wert | Bedeutung |
|------|-----------|
| `Ok` = `"OK"` | Feed fehlerfrei |
| `Warning` = `"Warning"` | Warnung (z. B. weniger abgerufene Items oder >30 Tage ohne neue) |
| `Error` = `"Error"` | Fehler beim Sync |

Hilfsmethode: `Changed(string? current, string? next)` — einfacher Ungleich-Vergleich.
