<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Enums und Statuskonstanten — Bestandsaufnahme

## `FeedSearchMatchKind` (Enum)
Datei: `src/Reporter.Core/Models/FeedSearchMatchKind.cs`

Deklarationsreihenfolge = Sortierreihenfolge der Treffer (Exakt-URL zuerst, dann Verzeichnis, dann Autodiscovery).

| Wert | Bedeutung |
|------|-----------|
| `ExactUrl` | Feed-URL entspricht exakt der Eingabe, oder die eingegebene URL ist selbst ein Feed-Dokument |
| `Directory` | Treffer aus dem feedsearch.dev-Verzeichnis |
| `Discovered` | Per Link-Tags oder Standardpfaden auf der Website gefunden |

## `FeedHealth` (statische Klasse mit Konstanten, kein Enum)
Datei: `src/Reporter.Core/Services/FeedHealth.cs`

| Wert | Bedeutung |
|------|-----------|
| `Ok` = `"OK"` | Feed fehlerfrei |
| `Warning` = `"Warning"` | Warnung (Item-Rückgang >50 % oder keine neuen Artikel seit >30 Tagen) |
| `Error` = `"Error"` | Synchronisation fehlgeschlagen |

Hilfsmethode: `Changed(string? current, string? next)` — Vergleich für `HealthLastChange`. Die String-Werte werden in `FeedsPage.xaml` per `DataTrigger` auf `FeedListItem.HealthStatus` gematcht.

## `NotificationAuthorizationStatus` (Enum)
Datei: `src/Reporter.Core/Models/NotificationAuthorizationStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unsupported` | Plattform unterstützt keine lokalen Benachrichtigungen |
| `NotDetermined` | Nutzer wurde noch nicht gefragt |
| `Denied` | In den Systemeinstellungen verweigert |
| `Authorized` | Erlaubt (inkl. provisional/ephemeral) |

Relevanz: `ILocalNotificationService.IsSupported`/`GetAuthorizationStatusAsync` steuern über `FeedsViewModel.NotificationsSupported` den Formular-Switch `FeedNotificationsEnabled` und den iOS-Hinweis.
