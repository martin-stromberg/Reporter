<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces — Bestandsaufnahme

Verträge im betroffenen Bereich (`src/Reporter.Core/Interfaces/`).

## `IFeedSyncService`

Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId`, `CancellationToken = default` | `Task<SyncResult>` | Synchronisiert einen Feed. |
| `SyncAllAsync` | `CancellationToken = default` | `Task<SyncResult>` | Synchronisiert alle konfigurierten Feeds, aggregiert Ergebnisse. |

Implementiert von `FeedSyncService`; konsumiert von `FeedsViewModel`, `UnreadViewModel`, `AutoRefreshService`. Kein keywordbezogener Member.

## `IKeywordMatcher`

Datei: `src/Reporter.Core/Interfaces/IKeywordMatcher.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `MatchesAny` | `string? title`, `string? contentHtml`, `IEnumerable<string> keywords` | `bool` | Teilwort-Match (case-insensitiv) auf Titel/HTML-Inhalt. |

Implementiert von `KeywordMatcher`; verwendet von `NotificationService` und `RetentionCleanupService`.

## `IKeywordRepository`

Datei: `src/Reporter.Core/Interfaces/IKeywordRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Keyword>>` | Alle Schlagworte (sortiert nach `KeywordText`). |
| `GetByIdAsync` | `Guid id` | `Task<Keyword?>` | Einzelnes Schlagwort. |
| `AddAsync` | `Keyword keyword` | `Task` | Schlagwort anlegen. |
| `UpdateAsync` | `Keyword keyword` | `Task` | Schlagwort aktualisieren. |
| `DeleteAsync` | `Guid id` | `Task` | Schlagwort löschen. |

Implementiert von `KeywordRepository`; verwendet von `SettingsViewModel`, `NotificationService`, `RetentionCleanupService`. Kein `CancellationToken`-Parameter.

## `IItemRepository`

Datei: `src/Reporter.Core/Interfaces/IItemRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Item>>` | Alle Items, nach `PublishedAt` absteigend. |
| `GetByIdAsync` | `Guid id` | `Task<Item?>` | Einzelnes Item. |
| `AddAsync` | `Item item` | `Task` | Item anlegen. |
| `UpdateAsync` | `Item item` | `Task` | Item aktualisieren. |
| `DeleteAsync` | `Guid id` | `Task` | Explizite Einzellöschung (keine Saved-Invariante; nicht vom Auto-Cleanup genutzt). |
| `GetUnreadByDateAsync` | — | `Task<IReadOnlyList<Item>>` | Alle ungelesenen Items, `PublishedAt` absteigend — **kein Keyword-Filter**. |
| `GetUnreadByDateAsync` | `int page`, `int pageSize`, `Guid? categoryId = null` | `Task<IReadOnlyList<ItemListItem>>` | Paged Ungelesen-Liste inkl. Feed-/Kategorie-Projektion — **kein Keyword-Filter**. |
| `GetUnreadCountAsync` | `Guid? categoryId = null` | `Task<int>` | Anzahl ungelesener Items (Badge/„Alle gelesen"). |
| `MarkAllAsReadAsync` | `Guid? categoryId = null` | `Task` | Alle ungelesenen Items als gelesen markieren. |
| `ToggleSavedForLaterAsync` | `Guid id` | `Task` | Merken-Flag umschalten. |
| `MarkAsReadAsync` | `Guid id` | `Task` | Item als gelesen markieren (`ReadAt = UtcNow`). |
| `GetByFeedAsync` | `Guid feedId` | `Task<IReadOnlyList<Item>>` | Items eines Feeds — Dedup-Grundlage in `RunSyncAsync`. |
| `GetByCategoryAsync` | `Guid categoryId` | `Task<IReadOnlyList<Item>>` | Items aller Feeds einer Kategorie — **kein Keyword-Filter**. |
| `GetSavedForLaterAsync` | `int page`, `int pageSize` | `Task<IReadOnlyList<ItemListItem>>` | Paged „Später lesen"-Liste — **kein Keyword-Filter**. |
| `AddRangeAsync` | `IReadOnlyList<Item> items` | `Task` | Batch-Insert neuer Items (Sync). |
| `DeleteExpiredAsync` | `DateTime cutoff`, `CancellationToken = default` | `Task<int>` | Löscht `IsRead && !IsSavedForLater && (ReadAt ?? PublishedAt) < cutoff`. |
| `GetExpiredKeywordCandidatesAsync` | `DateTime cutoff`, `CancellationToken = default` | `Task<IReadOnlyList<Item>>` | Keyword-Löschkandidaten: `IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff`. |
| `DeleteRangeAsync` | `IReadOnlyList<Guid> ids`, `CancellationToken = default` | `Task<int>` | Löscht Items per ID-Liste. |

Implementiert von `ItemRepository`. Es existiert **kein** Member, der Keyword-Treffer ohne `IsRead`-/Frist-Bedingung liefert (relevant für die offene Frage zu Bestandsdaten).

## `INotificationService`

Datei: `src/Reporter.Core/Interfaces/INotificationService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `NotifyNewItemsAsync` | `Feed feed`, `IReadOnlyList<Item> newItems`, `CancellationToken = default` | `Task` | Wertet Benachrichtigungsregeln inkl. Keyword-Filter für neu gespeicherte Items aus. |

Implementiert von `NotificationService`; aufgerufen von `FeedSyncService.RunSyncAsync`.

## `IRetentionCleanupService`

Datei: `src/Reporter.Core/Interfaces/IRetentionCleanupService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CleanupAsync` | `CancellationToken = default` | `Task<int>` | Löscht abgelaufene gelesene Items inkl. Keyword-Regel; Rückgabe = Anzahl gelöschter Items. |

Implementiert von `RetentionCleanupService`; aufgerufen von `App.OnStart`.
