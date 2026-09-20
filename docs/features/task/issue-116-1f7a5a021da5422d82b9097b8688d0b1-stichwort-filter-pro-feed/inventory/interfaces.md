<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Interfaces

Betroffene Contracts der Anforderung „Stichwort-Filter pro Feed" (Issue #116). Alle Dateien unter `src/Reporter.Core/Interfaces/`.

## `IKeywordFilter`
Datei: `src/Reporter.Core/Interfaces/IKeywordFilter.cs` — Implementierung: `KeywordFilter`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetKeywordTextsAsync()` | — | `Task<IReadOnlyList<string>>` | Liefert die konfigurierte Stichwortliste — **global, ohne Feed-Parameter**. |
| `MatchesAny(title, contentHtml, keywordTexts)` | `string? title`, `string? contentHtml`, `IReadOnlyList<string> keywordTexts` | `bool` | Prüft Titel/HTML-Inhalt gegen die übergebene Liste (case-insensitive Substrings). |

## `IKeywordMatcher`
Datei: `src/Reporter.Core/Interfaces/IKeywordMatcher.cs` — Implementierung: `KeywordMatcher`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `MatchesAny(title, contentHtml, keywords)` | `string? title`, `string? contentHtml`, `IEnumerable<string> keywords` | `bool` | Teilwort-Match `OrdinalIgnoreCase` auf Titel und HTML-Inhalt; Match-Semantik fest verdrahtet. |

## `IKeywordRepository`
Datei: `src/Reporter.Core/Interfaces/IKeywordRepository.cs` — Implementierung: `KeywordRepository`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync()` | — | `Task<IReadOnlyList<Keyword>>` | Alle Stichworte; keine Trennung global/feed-spezifisch. |
| `GetByIdAsync(id)` | `Guid id` | `Task<Keyword?>` | Einzelnes Stichwort. |
| `AddAsync(keyword)` | `Keyword keyword` | `Task` | Anlegen. |
| `UpdateAsync(keyword)` | `Keyword keyword` | `Task` | Aktualisieren (nur `KeywordText`). |
| `DeleteAsync(id)` | `Guid id` | `Task` | Löschen. |

## `IFeedRepository`
Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs` — Implementierung: `FeedRepository`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync()` | — | `Task<IReadOnlyList<Feed>>` | Alle Feeds. |
| `GetByIdAsync(id)` | `Guid id` | `Task<Feed?>` | Einzelfeed. |
| `AddAsync(feed)` | `Feed feed` | `Task` | Anlegen. |
| `UpdateAsync(feed)` | `Feed feed` | `Task` | Feldweises Update. |
| `DeleteAsync(id)` | `Guid id` | `Task` | Löscht Feed; Items per Kaskade, Contents explizit. |
| `GetAllWithDetailsAsync()` | — | `Task<IReadOnlyList<FeedListItem>>` | Listen-Projektion mit `UnreadCount`, `CategoryName`, `NotificationsEnabled`. |
| `GetByUrlAsync(url)` | `string url` | `Task<Feed?>` | URL-Lookup (Duplikatprüfung). |

## `INotificationService`
Datei: `src/Reporter.Core/Interfaces/INotificationService.cs` — Implementierung: `NotificationService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `NotifyNewItemsAsync(feed, newItems, cancellationToken)` | `Feed feed`, `IReadOnlyList<Item> newItems`, `CancellationToken` | `Task` | Benachrichtigt über neue Items; wendet intern den (globalen) Keyword-Filter an. Der `Feed`-Parameter ist bereits Teil der Signatur. |

## `IRetentionCleanupService`
Datei: `src/Reporter.Core/Interfaces/IRetentionCleanupService.cs` — Implementierung: `RetentionCleanupService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CleanupAsync(cancellationToken)` | `CancellationToken` | `Task<int>` | Retention-Bereinigung inkl. globaler Keyword-Löschregel; Rückgabe = Anzahl gelöschter Items. |

## `IItemRepository` (relevanter Ausschnitt)
Datei: `src/Reporter.Core/Interfaces/IItemRepository.cs` — Implementierung: `ItemRepository`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetExpiredKeywordCandidatesAsync(cutoff, cancellationToken)` | `DateTime cutoff`, `CancellationToken` | `Task<IReadOnlyList<Item>>` | Löschkandidaten für die Keyword-Löschregel; feed-übergreifend, Items tragen `FeedId`. |
| `GetByFeedAsync(feedId)` | `Guid feedId` | `Task<IReadOnlyList<Item>>` | Bestandsitems eines Feeds (Dedup/Backfill im Sync). |

## `IFeedSyncService`
Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs` — Implementierung: `FeedSyncService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync(feedId, cancellationToken)` | `Guid feedId`, `CancellationToken` | `Task<SyncResult>` | Synchronisiert einen Feed (hier greift der Keyword-Filter). |
| `SyncAllAsync(cancellationToken)` | `CancellationToken` | `Task<SyncResult>` | Synchronisiert alle Feeds. |
