<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Anwendung — Datenmodell

## Entitäten

### `Category`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung der Kategorie. |
| `Name` | `string` | Name der Kategorie. |

### `Feed`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Feeds. |
| `Url` | `string` | URL des Feeds. |
| `Title` | `string` | Titel des Feeds. |
| `CategoryId` | `Guid?` | Optionale Kategoriekennung. |
| `LastCheckedAt` | `DateTime?` | Zeitpunkt der letzten Abfrage. |
| `HealthStatus` | `string?` | Aktueller Gesundheitsstatus. |
| `HealthLastChange` | `DateTime?` | Zeitpunkt der letzten Statusänderung. |
| `NotificationsEnabled` | `bool` | Pro-Feed-Schalter für Benachrichtigungen (Standard `true`). |
| `FaviconUrl` | `string?` | URL des Favicons der Feed-Website (optional). |

### `Item`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Artikels. |
| `FeedId` | `Guid` | Kennung des zugehörigen Feeds. |
| `Title` | `string` | Titel des Artikels. |
| `Link` | `string?` | Link zum Originalartikel. |
| `PublishedAt` | `DateTime?` | Veröffentlichungszeitpunkt. |
| `GuidOrHash` | `string` | Original-GUID oder Hash. |
| `IsRead` | `bool` | Gibt an, ob der Artikel gelesen wurde. |
| `IsSavedForLater` | `bool` | Gibt an, ob der Artikel für später bewahrt wurde. |
| `ReadAt` | `DateTime?` | Zeitpunkt, an dem der Artikel gelesen wurde. |
| `ContentHtml` | `string?` | HTML-Inhalt für den Offline-Lesemodus. |

### `Keyword`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Keywords. |
| `KeywordText` | `string` | Der Keyword-Text. |

### `Settings`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung (Singleton). |
| `RetentionDays` | `int` | Aufbewahrungsdauer in Tagen. |
| `AutoMarkReadMode` | `string?` | Modus für automatisches Als-gelesen-markieren. |
| `AutoMarkReadDelaySeconds` | `int` | Verzögerung in Sekunden. |
| `NotificationsEnabled` | `bool` | Gibt an, ob Benachrichtigungen aktiv sind. |
| `QuietHoursStart` | `TimeSpan?` | Beginn der Ruhezeit. |
| `QuietHoursEnd` | `TimeSpan?` | Ende der Ruhezeit. |
| `AutoRefreshEnabled` | `bool` | Gibt an, ob die automatische Hintergrund-Aktualisierung aktiv ist (Standard `true`). |
| `RefreshIntervalMinutes` | `int` | Abruf-Intervall in Minuten (Standard `30`; UI-Auswahl 15/30/60/240). |
| `Theme` | `string?` | Erscheinungsbild (`"system"`/`"light"`/`"dark"`, Standard `"system"`). |
| `Language` | `string?` | Sprachauswahl (`"system"`/`"de"`/`"en"`, Standard `"system"`). |
| `NotificationSummaryEnabled` | `bool` | Benachrichtigungsmodus: `false` = eine Benachrichtigung pro Artikel (Standard), `true` = Sammel-Benachrichtigung pro Feed. |
| `RefreshOnStartupEnabled` | `bool` | Gibt an, ob die Feeds beim Start der App einmalig abgerufen werden (Standard `true`). |
| `UnreadSortOrder` | `string?` | Sortierrichtung der Ungelesen-Liste (`"desc"` = neueste zuerst, Standard; `"asc"` = älteste zuerst). |
| `DebugCollectionEnabled` | `bool` | Opt-in-Schalter für die Sammlung von Debuginformationen / das Session-Debug-Log (Standard `false`). |

### `SyncLog`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Sync-Logs. |
| `FeedId` | `Guid?` | Optionale Feed-Kennung. |
| `StartedAt` | `DateTime?` | Startzeitpunkt der Synchronisation. |
| `FinishedAt` | `DateTime?` | Endzeitpunkt der Synchronisation. |
| `Status` | `string?` | Status der Synchronisation. |
| `Message` | `string?` | Nachricht oder Fehlerdetails. |

### `DebugLogEntry`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Eintrags. |
| `Timestamp` | `DateTime` | UTC-Zeitpunkt des Eintrags (indiziert). |
| `Level` | `string?` | Schweregrad (`DebugLogLevel`: `Info`/`Warning`/`Error`). |
| `Category` | `string?` | Kategorie (`DebugLogCategory`: `Lifecycle`/`Sync`/`Exception`/`Settings`/`Report`). |
| `Message` | `string?` | Logmeldung. |
| `Details` | `string?` | Optionale Details, z. B. `exception.ToString()`. |

## Beziehungen

- Ein `Feed` gehört optional zu einer `Category` (`CategoryId`).
- Ein `Item` gehört immer zu einem `Feed` (`FeedId`).
- Ein `SyncLog` gehört optional zu einem `Feed` (`FeedId`).
- `DebugLogEntry` hat keine Beziehungen — die Einträge sind sitzungsbezogen und werden beim App-Start bis auf `Error`-Einträge zurückgesetzt.

## Datenzugriff

Die App verwendet eine saubere Schichtung:

- `Reporter.Core.Models` enthält die Domänenmodelle.
- `Reporter.Core.Interfaces` definiert Repository-Schnittstellen für alle Entitäten.
- `Reporter.Data.Repositories` implementiert die Schnittstellen mit Entity Framework Core und SQLite.
- `Reporter.Data.Repositories` injiziert `IDbContextFactory<ReporterDbContext>`, um pro Operation einen neuen `DbContext` zu erzeugen.
- `Settings` wird als Singleton verwaltet; es existiert immer genau ein Datensatz.
- `IItemRepository` bietet zusätzliche Queries für ungelesene Artikel, Artikel pro Feed/Kategorie und gespeicherte Artikel (paged); die Dublettenerkennung pro Feed läuft im Sync über ein In-Memory-`HashSet` auf `GuidOrHash` mit Batch-Insert via `AddRangeAsync`.
- `IItemRepository.DeleteExpiredAsync` entfernt abgelaufene Artikel für die automatische Aufbewahrungsfrist (`IsRead && !IsSavedForLater && (ReadAt ?? PublishedAt) < cutoff`); `GetExpiredKeywordCandidatesAsync` liefert die Kandidaten der Keyword-Löschregel (`IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff`), `DeleteRangeAsync` löscht Treffer per IDs; Details siehe [Aufbewahrung und automatisches Aufräumen](aufbewahrung.md).
- Die `settings`-Spalten `auto_refresh_enabled`, `refresh_interval_minutes` und `theme` wurden per Migration `AddSettingsAutoRefreshAndTheme` ergänzt; die Spalte `language` (Standard `"system"`, inkl. `UpdateData` des Singletons) per Migration `AddSettingsLanguage`.
- Für die lokalen Benachrichtigungen wurden per Migration `AddFeedNotificationsEnabled` die Spalte `feeds.notifications_enabled` (Default `true`) und per `AddSettingsNotificationSummary` die Spalte `settings.notification_summary_enabled` (Default `false`, inkl. `UpdateData` des Singletons) ergänzt — Details siehe [Benachrichtigungen](../benachrichtigungen/index.md).
- Für die Feed-Symbole wurde per Migration `AddFeedFaviconUrl` die Spalte `feeds.favicon_url` ergänzt; für Start-Abruf und Ungelesen-Sortierung per `AddSettingsStartupRefreshAndSortOrder` die Spalten `settings.refresh_on_startup_enabled` (Default `true`, inkl. `UpdateData` des Singletons) und `settings.unread_sort_order` (Default `"desc"`, inkl. `UpdateData` des Singletons).
- Für die Diagnose-Funktion wurden per Migration `AddSettingsDebugCollection` die Spalte `settings.debug_collection_enabled` (Default `false`) und per `AddDebugLogEntries` die Tabelle `debug_log_entries` (`id`, `timestamp` mit Index `IX_debug_log_entries_timestamp`, `level` max. 20, `category` max. 50, `message`, `details`) ergänzt — Details zum Session-Log siehe [Einstellungen](../einstellungen/index.md).
