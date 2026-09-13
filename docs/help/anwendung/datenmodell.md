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

### `SyncLog`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Sync-Logs. |
| `FeedId` | `Guid?` | Optionale Feed-Kennung. |
| `StartedAt` | `DateTime?` | Startzeitpunkt der Synchronisation. |
| `FinishedAt` | `DateTime?` | Endzeitpunkt der Synchronisation. |
| `Status` | `string?` | Status der Synchronisation. |
| `Message` | `string?` | Nachricht oder Fehlerdetails. |

## Beziehungen

- Ein `Feed` gehört optional zu einer `Category` (`CategoryId`).
- Ein `Item` gehört immer zu einem `Feed` (`FeedId`).
- Ein `SyncLog` gehört optional zu einem `Feed` (`FeedId`).

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
