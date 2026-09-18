<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Enums und Konstanten — Bestandsaufnahme

Bezug: Anforderung „Artikeldaten getrennt von Nutzerdaten speichern (iCloud-Backup)" (`requirement.md`). Es existieren keine Enums oder Konstanten, die Speicherbereiche oder Backup-Status beschreiben. Folgende Konstanten-Typen sind den betroffenen Bereichen zugeordnet (Statuswerte in `reporter.db`-Tabellen bzw. Einstellungen, die im Nutzerdaten-Speicher verbleiben):

## `FeedHealth`

Datei: `src/Reporter.Core/Services/FeedHealth.cs`

`static class` mit String-Konstanten; Werte landen in `feeds.health_status` und `sync_logs.status`.

| Wert | Bedeutung |
|------|-----------|
| `Ok = "OK"` | Sync erfolgreich. |
| `Warning = "Warning"` | Sync mit Auffälligkeit (z. B. deutlich weniger Items, keine neuen Items seit 30 Tagen). |
| `Error = "Error"` | Sync fehlgeschlagen / offline. |
| `Changed(string? current, string? next)` | Hilfsmethode: Status-Wechsel-Erkennung (steuert `HealthLastChange`). |

## `FeedSyncErrorKind`

Datei: `src/Reporter.Core/Services/FeedSyncErrorKind.cs`

`static class` mit String-Konstanten; Werte landen in `feeds.last_error_kind`.

| Wert | Bedeutung |
|------|-----------|
| `InsecureHttpBlocked = "InsecureHttpBlocked"` | `HttpRequestException` gegen `http://`-Feed (ATS-Block). |
| `HttpStatus = "HttpStatus"` | HTTP-Fehlerstatus vorhanden. |
| `Network = "Network"` | Netzwerkfehler ohne Statuscode (https). |
| `Parse = "Parse"` | `XmlException` beim Feed-Parsen. |
| `Unknown = "Unknown"` | Sonstige Ausnahme. |
| `Classify(Exception ex, string feedUrl)` | Hilfsmethode: klassifiziert die Sync-Ausnahme. |

## `SettingsValues`

Datei: `src/Reporter.Core/Models/SettingsValues.cs`

`static class` mit Konstanten für persistierte Einstellungswerte (`settings`-Tabelle — verbleibt im Nutzerdaten-Speicher).

| Wert | Bedeutung |
|------|-----------|
| `AutoMarkReadOnOpen = "on_open"` | Auto-gelesen-Modus beim Öffnen. |
| `AutoMarkReadOnScroll = "on_scroll"` | Auto-gelesen-Modus beim Scrollen (Default). |
| `AutoMarkReadOff = "off"` | Auto-gelesen deaktiviert. |
| `ThemeSystem = "system"` / `ThemeLight = "light"` / `ThemeDark = "dark"` | Theme-Auswahl. |
| `LanguageSystem = "system"` / `LanguageGerman = "de"` / `LanguageEnglish = "en"` | Sprachwahl. |
| `MinRefreshIntervalMinutes = 1` / `MaxRefreshIntervalMinutes = 1440` | Grenzen des Refresh-Intervalls. |
| `SortOrderDescending = "desc"` / `SortOrderAscending = "asc"` | Sortierung der Ungelesen-Liste. |

## `DebugLogCategory` / `DebugLogLevel`

Dateien: `src/Reporter.Core/Services/DebugLogCategory.cs`, `src/Reporter.Core/Services/DebugLogLevel.cs`

`static class`es mit String-Konstanten für `debug_log_entries` (sitzungsbezogenes Log in `reporter.db`).

| Konstante | Werte |
|-----------|-------|
| `DebugLogCategory` | `Lifecycle`, `Sync`, `Exception`, `Notification`, `Settings`, `Report` |
| `DebugLogLevel` | `Info`, `Warning`, `Error` |

Relevant: Start-Schritte der App (`App.OnStart`) und Sync-/Benachrichtigungsfehler werden über `IDebugLogService` mit diesen Kategorien protokolliert — ein neuer Migrations-Schritt beim Update würde denselben Mechanismus nutzen.
