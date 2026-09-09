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

### `Item`

| Eigenschaft | Typ | Beschreibung |
|-------------|-----|--------------|
| `Id` | `Guid` | Eindeutige Kennung des Artikels. |
| `FeedId` | `Guid` | Kennung des zugehörigen Feeds. |
| `Title` | `string` | Titel des Artikels. |
| `Link` | `string?` | Link zum Originalartikel. |
| `PublishedAt` | `DateTime?` | Veröffentlichungszeitpunkt. |
| `GuidOrHash` | `string?` | Original-GUID oder Hash. |
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
