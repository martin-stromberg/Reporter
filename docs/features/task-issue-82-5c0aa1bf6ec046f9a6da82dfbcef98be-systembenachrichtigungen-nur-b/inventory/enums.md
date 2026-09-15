<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Enums und Konstanten

## `NotificationAuthorizationStatus` (enum)
Datei: `src/Reporter.Core/Models/NotificationAuthorizationStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Unsupported` | Plattform unterstützt keine lokalen Benachrichtigungen. |
| `NotDetermined` | Nutzer wurde noch nicht nach der Berechtigung gefragt. |
| `Denied` | Berechtigung wurde in den Systemeinstellungen verweigert. |
| `Authorized` | Benachrichtigungen erlaubt (inkl. `Provisional`/`Ephemeral`). |

Verwendet von `ILocalNotificationService.GetAuthorizationStatusAsync` und `SettingsViewModel.ApplyAuthorizationStatus`.

## `FeedHealth` (statische Konstantenklasse)
Datei: `src/Reporter.Core/Services/FeedHealth.cs`

| Wert | Bedeutung |
|------|-----------|
| `Ok` (`"OK"`) | Feed gesund. |
| `Warning` (`"Warning"`) | Warnung (z. B. deutlich weniger Items oder > 30 Tage ohne Neuzugänge). |
| `Error` (`"Error"`) | Sync-Fehler. |

Hilfsmethode `Changed(current, next)` für Statusübergänge.

## `FeedSyncErrorKind` (statische Konstantenklasse)
Datei: `src/Reporter.Core/Services/FeedSyncErrorKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `InsecureHttpBlocked` | `http`-Feed ohne HTTP-Antwort (blockierter Klartext). |
| `HttpStatus` | Server antwortete mit Nicht-Erfolgs-Statuscode. |
| `Network` | Netzwerkfehler ohne HTTP-Antwort. |
| `Parse` | Feed-Dokument nicht parsebar (`XmlException`). |
| `Unknown` | Sonstiger Fehler. |

`Classify(ex, feedUrl)` bildet Exceptions auf diese Kategorien ab (persistiert in `Feed.LastErrorKind`).

## `DebugLogCategory` (statische Konstantenklasse)
Datei: `src/Reporter.Core/Services/DebugLogCategory.cs`

| Wert | Bedeutung |
|------|-----------|
| `Lifecycle` (`"Lifecycle"`) | App-Lebenszyklus (Start, Suspend, Resume). |
| `Sync` (`"Sync"`) | Feed-Sync- und Auto-Refresh-Fehler; auch Benachrichtigungsfehler in `FeedSyncService`. |
| `Exception` (`"Exception"`) | Unbehandelte/unbeobachtete Exceptions. |
| `Settings` (`"Settings"`) | Einstellungsbezogene Ereignisse. |
| `Report` (`"Report"`) | Debugbericht-Ereignisse. |

## `DebugLogLevel` (statische Konstantenklasse)
Datei: `src/Reporter.Core/Services/DebugLogLevel.cs` — Level-Konstanten (`Info`, `Warning`, `Error`), im Benachrichtigungsfehlerpfad wird `Warning` verwendet.

## Hinweis zu `SettingsValues`
Datei: `src/Reporter.Core/Models/SettingsValues.cs` — Konstanten für `AutoMarkReadMode`, `Theme`, `Language`, `UnreadSortOrder` sowie `IsAutoMarkReadEnabled`. Kein benachrichtigungsspezifischer Wert vorhanden.
