<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Enums und Konstanten

Das Projekt verwendet keine echten `enum`-Typen im relevanten Bereich, sondern statische Konstantenklassen, die als Strings persistiert werden (`feeds.health_status`, `debug_log_entries.category`/`level` u. a.).

## `FeedHealth`

Datei: `src/Reporter.Core/Services/FeedHealth.cs`

| Wert | Bedeutung |
|------|-----------|
| `Ok` (`"OK"`) | Feed ist gesund — Default für neu angelegte Feeds (`FeedsViewModel.Search.cs:323`) |
| `Warning` (`"Warning"`) | Feed mit Warnung (z. B. deutlich weniger Artikel oder seit > 30 Tagen keine neuen) |
| `Error` (`"Error"`) | Sync-Fehler |

Hilfsmethode: `Changed(current, next)` — String-Vergleich für Statuswechsel.

## `FeedSyncErrorKind`

Datei: `src/Reporter.Core/Services/FeedSyncErrorKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `InsecureHttpBlocked` | HTTP-URL auf iOS blockiert (ATS) |
| `HttpStatus` | `HttpRequestException` mit Statuscode |
| `Network` | `HttpRequestException` ohne Statuscode |
| `Parse` | `XmlException` beim Feed-Parsing |
| `Unknown` | Sonstige Ausnahme |

Hilfsmethode: `Classify(Exception, string feedUrl)`.

## `DebugLogCategory`

Datei: `src/Reporter.Core/Services/DebugLogCategory.cs`

| Wert | Bedeutung |
|------|-----------|
| `Lifecycle` | App-Lifecycle (Start, Suspend, Resume) — Kategorie für die `App.OnStart`-Fehlerblöcke; passende Kategorie für Seed-Fehler |
| `Sync` | Feed-Sync- und Auto-Refresh-Fehler |
| `Exception` | Unbehandelte/unobserved Exceptions |
| `Notification` | Zustellung lokaler Benachrichtigungen |
| `Settings` | Settings-Ereignisse |
| `Report` | Debugbericht-Ereignisse |

## `DebugLogLevel`

Datei: `src/Reporter.Core/Services/DebugLogLevel.cs`

| Wert | Bedeutung |
|------|-----------|
| `Info` | Informationseintrag |
| `Warning` | Warnung |
| `Error` | Fehler (überlebt den Session-Reset von `BeginSessionAsync`) |

## `SettingsValues` (Auswahl)

Datei: `src/Reporter.Core/Models/SettingsValues.cs`

| Wert | Bedeutung |
|------|-----------|
| `AutoMarkReadOnOpen` / `AutoMarkReadOnScroll` / `AutoMarkReadOff` | Auto-Als-gelesen-Modi |
| `ThemeSystem` / `ThemeLight` / `ThemeDark` | Theme-Werte |
| `LanguageSystem` / `LanguageGerman` / `LanguageEnglish` | Sprachauswahl |
| `MinRefreshIntervalMinutes` = 1 / `MaxRefreshIntervalMinutes` = 1440 | Intervall-Grenzen |
| `SortOrderDescending` / `SortOrderAscending` | Sortierung der Ungelesen-Liste |

Hilfsmethoden: `IsAutoMarkReadEnabled`, `ClampRefreshIntervalMinutes`.

## `NotificationAuthorizationStatus` (echtes Enum)

Datei: `src/Reporter.Core/Models/NotificationAuthorizationStatus.cs` — Werte `NotDetermined`, `Denied`, `Authorized`, `Unsupported` (iOS-Mapping in `LocalNotificationService`). Nur indirekt relevant (Benachrichtigungs-Nebenwirkung des Demo-Feeds).
