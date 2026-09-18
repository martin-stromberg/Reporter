<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces — Bestandsaufnahme

Bezug: Anforderung „Artikeldaten getrennt von Nutzerdaten speichern (iCloud-Backup)" (`requirement.md`). Alle Interfaces liegen in `src/Reporter.Core/Interfaces/`. Ein Interface für einen separaten Content-Speicher (z. B. `IContentStore`/`IItemContentRepository`) existiert noch nicht.

## `IItemRepository`

Datei: `src/Reporter.Core/Interfaces/IItemRepository.cs`

Implementiert von `ItemRepository` (`src/Reporter.Data/Repositories/ItemRepository.cs`). `Item` ist dabei `Reporter.Core.Models.Item` inklusive `ContentHtml` — der Contract transportiert Content heute implizit über das Domänenmodell.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | – | `Task<IReadOnlyList<Item>>` | Alle Items inkl. `ContentHtml`. |
| `GetByIdAsync` | `Guid id` | `Task<Item?>` | Ein Item inkl. `ContentHtml`; u. a. `ArticleDetailViewModel`. |
| `AddAsync` | `Item item` | `Task` | Insert inkl. `ContentHtml`. |
| `UpdateAsync` | `Item item` | `Task` | Update inkl. `ContentHtml`. |
| `DeleteAsync` | `Guid id` | `Task` | Einzel-Löschung (ohne saved-for-later-Invariante). |
| `GetUnreadByDateAsync` | – | `Task<IReadOnlyList<Item>>` | Ungelesene Items inkl. `ContentHtml`. |
| `GetUnreadByDateAsync` | `int page, int pageSize, Guid? categoryId = null, bool ascending = false` | `Task<IReadOnlyList<ItemListItem>>` | Paged `ItemListItem`; `ImageUrl`/`Summary`/`ReadingTimeText` werden aus `ContentHtml` abgeleitet. |
| `GetUnreadCountAsync` | `Guid? categoryId = null` | `Task<int>` | Anzahl ungelesener Items. |
| `MarkAllAsReadAsync` | `Guid? categoryId = null` | `Task` | Massen-Update `IsRead`/`ReadAt`. |
| `ToggleSavedForLaterAsync` | `Guid id` | `Task` | Merkliste umschalten. |
| `MarkAsReadAsync` | `Guid id` | `Task` | `IsRead`/`ReadAt` setzen. |
| `GetByFeedAsync` | `Guid feedId` | `Task<IReadOnlyList<Item>>` | Items eines Feeds inkl. `ContentHtml`; `FeedSyncService` nutzt es für die `GuidOrHash`-Deduplizierung. |
| `GetByCategoryAsync` | `Guid categoryId` | `Task<IReadOnlyList<Item>>` | Items einer Kategorie inkl. `ContentHtml`. |
| `GetSavedForLaterAsync` | `int page, int pageSize` | `Task<IReadOnlyList<ItemListItem>>` | Paged Merkliste mit Content-Ableitungen. |
| `AddRangeAsync` | `IReadOnlyList<Item> items` | `Task` | Batch-Insert inkl. `ContentHtml`; vom `FeedSyncService` genutzt. |
| `DeleteExpiredAsync` | `DateTime cutoff, CancellationToken` | `Task<int>` | Retention-Löschung (`ExecuteDelete`); vom `RetentionCleanupService` genutzt. |
| `GetExpiredKeywordCandidatesAsync` | `DateTime cutoff, CancellationToken` | `Task<IReadOnlyList<Item>>` | Keyword-Löschkandidaten inkl. `ContentHtml`. |
| `DeleteRangeAsync` | `IReadOnlyList<Guid> ids, CancellationToken` | `Task<int>` | Batch-Löschung (`ExecuteDelete`). |

## `IBackupExclusionService`

Datei: `src/Reporter.Core/Interfaces/IBackupExclusionService.cs`

Implementiert von `BackupExclusionService` (`src/Reporter/Services/BackupExclusionService.cs`); in Tests durch `FakeBackupExclusionService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ExcludeFromBackup` | `string filePath` | `void` | Markiert eine Datei als vom Cloud-Backup ausgeschlossen (`NSUrl.IsExcludedFromBackupKey`, iOS-only; sonst No-op). Fehlende Dateien sind toleriert. |

Der Contract kennt nur die Ausschluss-Richtung — ein Wieder-Einschließen (`NSNumber.FromBoolean(false)`) ist nicht definiert.

## `IKeywordFilter`

Datei: `src/Reporter.Core/Interfaces/IKeywordFilter.cs`

Implementiert von `KeywordFilter`. Aufgerufen von `FeedSyncService`, `RetentionCleanupService`, `NotificationService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetKeywordTextsAsync` | – | `Task<IReadOnlyList<string>>` | Konfigurierte Keyword-Texte aus `IKeywordRepository`. |
| `MatchesAny` | `string? title, string? contentHtml, IReadOnlyList<string> keywordTexts` | `bool` | Case-insensitiver Substring-Match auf Titel/Content — benötigt `ContentHtml` zur Laufzeit im Arbeitsspeicher. |

## `IKeywordMatcher`

Datei: `src/Reporter.Core/Interfaces/IKeywordMatcher.cs`

Implementiert von `KeywordMatcher`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `MatchesAny` | `string? title, string? contentHtml, IEnumerable<string> keywords` | `bool` | Reiner In-Memory-Matcher (ohne Storage). |

## `IFeedSyncService`

Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs`

Implementiert von `FeedSyncService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId, CancellationToken` | `Task<SyncResult>` | Sync eines Feeds; schreibt Items inkl. `ContentHtml`. |
| `SyncAllAsync` | `CancellationToken` | `Task<SyncResult>` | Sync aller Feeds (seriell via `_syncAllLock`). |

## `IRetentionCleanupService`

Datei: `src/Reporter.Core/Interfaces/IRetentionCleanupService.cs`

Implementiert von `RetentionCleanupService`; aufgerufen aus `App.CleanupRetainedDataAsync`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CleanupAsync` | `CancellationToken` | `Task<int>` | Retention- + Keyword-basierte Löschung; benötigt `ContentHtml` der Kandidaten für `MatchesAny`. |

## `INotificationService`

Datei: `src/Reporter.Core/Interfaces/INotificationService.cs`

Implementiert von `NotificationService`; aufgerufen aus `FeedSyncService.RunSyncAsync`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `NotifyNewItemsAsync` | `Feed feed, IReadOnlyList<Item> newItems, CancellationToken` | `Task` | Benachrichtigungen für neue Items; matched `i.ContentHtml` aus dem Arbeitsspeicher. |

## `IFeedRepository` (Kaskaden-Kontext)

Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs`

Implementiert von `FeedRepository`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | – | `Task<IReadOnlyList<Feed>>` | Alle Feeds. |
| `GetByIdAsync` | `Guid id` | `Task<Feed?>` | Feed per Id. |
| `AddAsync` | `Feed feed` | `Task` | Feed anlegen. |
| `UpdateAsync` | `Feed feed` | `Task` | Feed aktualisieren. |
| `DeleteAsync` | `Guid id` | `Task` | Feed löschen — Items laufen über die DB-Kaskade `OnDelete(Cascade)` auf `items.feed_id` mit. |
| `GetAllWithDetailsAsync` | – | `Task<IReadOnlyList<FeedListItem>>` | Feedliste mit Item-/Unread-Zählern. |
| `GetByUrlAsync` | `string url` | `Task<Feed?>` | Feed per URL. |

## `IDbContextFactory<ReporterDbContext>`

Microsoft.EntityFrameworkCore-Contract (kein projekteigenes Interface). Registriert in `MauiProgram` (Zeile 73) als einzige Factory; alle Repositories (`ItemRepository`, `FeedRepository`, `CategoryRepository`, `KeywordRepository`, `SettingsRepository`, `SyncLogRepository`, `DebugLogRepository`) hängen daran. In Tests über `TestDbContextFactory` (`src/Reporter.Tests/TestDbContextFactory.cs`) ersetzt.
