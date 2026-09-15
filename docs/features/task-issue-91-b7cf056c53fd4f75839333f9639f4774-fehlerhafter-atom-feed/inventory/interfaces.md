<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces — Bestandsaufnahme

Aufgeführt sind `IFeedSyncService` vollständig sowie die vom Sync-Pfad tatsächlich genutzten Member der übrigen Contracts (Interfaces besitzen teils weitere Member, die hier nicht relevant sind).

## `IFeedSyncService`
Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId`, `CancellationToken` | `Task<SyncResult>` | Einzelnen Feed synchronisieren |
| `SyncAllAsync` | `CancellationToken` | `Task<SyncResult>` | Alle konfigurierten Feeds synchronisieren (aggregiert) |

Implementiert von `FeedSyncService`; aufgerufen von `FeedsViewModel`, `UnreadViewModel`, `AutoRefreshService`, `ScheduledSyncRunner`.

## `IFeedRepository`
Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs` — vom Sync genutzte Member:

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetByIdAsync` | `Guid id` | `Task<Feed?>` | Feed zum Sync laden |
| `GetAllAsync` | — | `Task<IReadOnlyList<Feed>>` | Alle Feeds für `SyncAllAsync` |
| `UpdateAsync` | `Feed feed` | `Task` | Health/Titel/Favicon/Fehlerfelder persistieren (`UpdateFeedHealthAsync`) |

## `IItemRepository`
Datei: `src/Reporter.Core/Interfaces/IItemRepository.cs` — vom Sync genutzte Member:

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetByFeedAsync` | `Guid feedId` | `Task<IReadOnlyList<Item>>` | Bestandsitems für Dedup und `lastPublishedAt` laden |
| `AddRangeAsync` | `IReadOnlyList<Item>` | `Task` | Neue Items batchweise speichern |

## `ISyncLogRepository`
Datei: `src/Reporter.Core/Interfaces/ISyncLogRepository.cs` — vom Sync genutzte Member:

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `AddAsync` | `SyncLog` | `Task` | Sync-Log beim Start anlegen |
| `UpdateAsync` | `SyncLog` | `Task` | Sync-Log mit Ergebnis abschließen |

## `INotificationService`
Datei: `src/Reporter.Core/Interfaces/INotificationService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `NotifyNewItemsAsync` | `Feed feed`, `IReadOnlyList<Item> newItems`, `CancellationToken` | `Task` | Benachrichtigungsentscheidung für neu gespeicherte Items; Aufruf im Sync fehlerisoliert |

## `INetworkStatusService`
Datei: `src/Reporter.Core/Interfaces/INetworkStatusService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsOnline` (Property) | — | `bool` | Offline-Vorprüfung in `SyncFeedAsync`/`SyncAllAsync` |
| `ConnectivityChanged` (Event) | — | `EventHandler?` | Vom Sync nicht abonniert (nur ViewModels/Services) |

## `IKeywordFilter`
Datei: `src/Reporter.Core/Interfaces/IKeywordFilter.cs` — vom Sync genutzte Member:

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetKeywordTextsAsync` | — | `Task<IReadOnlyList<string>>` | Keyword-Liste vor der Item-Schleife laden |
| `MatchesAny` | `title`, `contentHtml`, `keywordTexts` | `bool` | Trefferprüfung pro Item in `CollectNewItems` (verworfene Items werden nicht gespeichert) |

## `IFeedIconService`
Datei: `src/Reporter.Core/Interfaces/IFeedIconService.cs` — vom Sync genutztes Member:

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `TryFindFaviconUrlAsync` | `string feedUrl`, `string? siteUrl`, `CancellationToken` | `Task<string?>` | Favicon-Lookup; fehlerisoliert (`null` statt Exception); `siteUrl` stammt aus dem `alternate`-Link des `SyndicationFeed` |

## `IDebugLogService`
Datei: `src/Reporter.Core/Interfaces/IDebugLogService.cs` — vom Sync genutztes Member (optionaler Konstruktorparameter):

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `LogAsync` | `category`, `message`, `details?`, `level`, `CancellationToken` | `Task` | Fehlerprotokoll (`Sync`/`Error` im Sync-`catch`, `Sync`/`Warning` im Notification-`catch`) |
