# Interfaces

Bestehende Contracts in `src/Reporter.Core/Interfaces/`, die der Benachrichtigungs-Service nutzen bzw. deren Muster er folgen würde. **Es existiert weder `INotificationService` noch `ILocalNotificationService`** (geprüft: kein Vorkommen von `Notification` im Interfaces-Verzeichnis).

## `IFeedSyncService`
Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs` — implementiert von `FeedSyncService`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId`, `CancellationToken` | `Task<SyncResult>` | Synchronisiert einen Feed (Zeile 16) |
| `SyncAllAsync` | `CancellationToken` | `Task<SyncResult>` | Synchronisiert alle Feeds (Zeile 23) |

## `IFeedRepository`
Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs` — implementiert von `FeedRepository`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Feed>>` | Alle Feeds (Zeile 14) |
| `GetByIdAsync` | `Guid id` | `Task<Feed?>` | Feed per ID (Zeile 21) |
| `AddAsync` | `Feed feed` | `Task` | Feed anlegen (Zeile 28) |
| `UpdateAsync` | `Feed feed` | `Task` | Feed aktualisieren — `FeedRepository.UpdateAsync` überträgt nur die bekannten Felder (Zeile 35) |
| `DeleteAsync` | `Guid id` | `Task` | Feed löschen, Kaskade auf `items` (Zeile 42) |
| `GetAllWithDetailsAsync` | — | `Task<IReadOnlyList<FeedListItem>>` | Feeds mit Kategoriename und UnreadCount — Projektion müsste ein neues Feld mitliefern (Zeile 48) |
| `GetByUrlAsync` | `string url` | `Task<Feed?>` | Duplikatprüfung in `FeedsViewModel.SaveAsync` (Zeile 55) |

## `IItemRepository`
Datei: `src/Reporter.Core/Interfaces/IItemRepository.cs` — implementiert von `ItemRepository`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetByGuidOrHashAsync` | `Guid feedId`, `string guidOrHash` | `Task<Item?>` | **Dublettenerkennung** im Sync (`FeedSyncService.RunSyncAsync`, Zeile 134 dort) (Zeile 115) |
| `GetByFeedAsync` | `Guid feedId` | `Task<IReadOnlyList<Item>>` | Items eines Feeds — wird in `RunSyncAsync` für `existingCount`/`lastPublishedAt` genutzt (Zeile 94) |
| `AddAsync` | `Item item` | `Task` | Neuen Artikel speichern (Zeile 28) |
| Weitere (`GetAllAsync`, `GetByIdAsync`, `UpdateAsync`, `DeleteAsync`, `GetUnreadByDateAsync`, `GetUnreadCountAsync`, `MarkAllAsReadAsync`, `ToggleSavedForLaterAsync`, `MarkAsReadAsync`, `GetByCategoryAsync`, `GetSavedForLaterAsync`, `DeleteExpiredAsync`, `GetExpiredKeywordCandidatesAsync`, `DeleteRangeAsync`) | — | — | Für die Anforderung nicht direkt relevant (Zeilen 14-148) |

## `ISettingsRepository`
Datei: `src/Reporter.Core/Interfaces/ISettingsRepository.cs` — implementiert von `SettingsRepository`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync` | `CancellationToken` | `Task<Settings>` | Singleton-Einstellungen laden; legt Default-Record an, falls fehlend — liefert `NotificationsEnabled`/`QuietHoursStart`/`QuietHoursEnd` (Zeile 15) |
| `SaveAsync` | `Settings settings` | `Task` | Singleton-Einstellungen speichern (Zeile 22) |

## `IKeywordRepository`
Datei: `src/Reporter.Core/Interfaces/IKeywordRepository.cs` — implementiert von `KeywordRepository`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Keyword>>` | Alle Keyword-Filter — für die Keyword-Prüfung im Benachrichtigungs-Service (Zeile 14) |
| `GetByIdAsync` / `AddAsync` / `UpdateAsync` / `DeleteAsync` | — | — | Pflege der Keywords (Zeilen 21-42) |

## `IKeywordMatcher`
Datei: `src/Reporter.Core/Interfaces/IKeywordMatcher.cs` — implementiert von `KeywordMatcher`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `MatchesAny` | `string? title`, `string? contentHtml`, `IEnumerable<string> keywords` | `bool` | Case-insensitiver Teilwort-Match auf Titel und HTML-Inhalt; bereits vom `RetentionCleanupService` genutzt (Zeile 16) |

## `IAutoRefreshService`
Datei: `src/Reporter.Core/Interfaces/IAutoRefreshService.cs` — implementiert von `AutoRefreshService`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `StartAsync` | `CancellationToken` | `Task` | Startet den periodischen Sync-Timer (Zeile 15) |
| `ApplySettingsAsync` | `Settings settings` | `Task` | Wendet Einstellungen an / startet Loop neu (Zeile 23) |
| `StopAsync` | — | `Task` | Stoppt den Timer (Zeile 29) |

## `IAppThemeService`
Datei: `src/Reporter.Core/Interfaces/IAppThemeService.cs` — implementiert von `AppThemeService` (`src/Reporter/Services/`)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ApplyTheme` | `string? theme` | `void` | Muster für ein plattformneutrales Interface in `Reporter.Core` mit Implementierung im App-Projekt (Zeile 13) |
