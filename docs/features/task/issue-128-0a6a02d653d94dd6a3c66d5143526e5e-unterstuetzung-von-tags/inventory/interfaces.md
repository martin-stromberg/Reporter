<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces — Bestandsaufnahme

## `IItemRepository`

Datei: `src/Reporter.Core/Interfaces/IItemRepository.cs`

Contract für den Item-Zugriff; implementiert von `ItemRepository`, im Test umhüllt von `DelegatingItemRepository`. Für die Anforderung relevante Member:

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetByIdAsync` | `Guid id` | `Task<Item?>` | Vollständiges `Item` inkl. Content/Image — der Pfad, über den `ArticleDetailViewModel` das Item lädt (Tags müssten hier transportiert werden) |
| `GetByFeedAsync` | `Guid feedId` | `Task<IReadOnlyList<Item>>` | Alle Items eines Feeds (Sync-Dedup-Basis) |
| `GetByFeedAsync` | `Guid feedId, int page, int pageSize, string? searchTerm = null` | `Task<IReadOnlyList<ItemListItem>>` | Paged Feed-Artikelliste; XML-Doku beschreibt `searchTerm` derzeit als „matched against the item title" (Zeile 106) |
| `AddAsync` | `Item item` | `Task` | Einzel-Insert |
| `AddRangeAsync` | `IReadOnlyList<Item> items` | `Task` | Batch-Insert — wird von `FeedSyncService` für neue Items genutzt |
| `UpdateAsync` | `Item item` | `Task` | Update inkl. Content |
| `GetAllAsync` / `GetUnreadByDateAsync` (2×) / `GetUnreadCountAsync` / `GetByCategoryAsync` / `GetSavedForLaterAsync` / `GetSavedCountAsync` | — | div. | Übrige Lesezugriffe |
| `MarkAllAsReadAsync` / `MarkAsReadAsync` / `ToggleSavedForLaterAsync` / `DeleteAsync` / `DeleteExpiredAsync` / `DeleteRangeAsync` / `GetExpiredKeywordCandidatesAsync` / `GetAllIdsAsync` | — | div. | Status- und Löschpfade |

Die Signatur von `GetByFeedAsync(feedId, page, pageSize, searchTerm)` bleibt für die Anforderung unverändert — nur die Suchsemantik und die XML-Doku sind betroffen.

## `IKeywordFilter`

Datei: `src/Reporter.Core/Interfaces/IKeywordFilter.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetKeywordTextsAsync` | `Guid? feedId` | `Task<IReadOnlyList<string>>` | Effektive Stichwortliste (global bzw. global ∪ feed) |
| `HasKeywordsAsync` | — | `Task<bool>` | Ob irgendwelche Stichworte existieren |
| `MatchesAny` | `string? title, string? contentHtml, IReadOnlyList<string> keywordTexts` | `bool` | Case-insensitive Teilstring-Prüfung gegen Titel und Inhalt — Tags sind nicht Teil der Signatur (von der Anforderung nicht gefordert) |

Implementiert von `KeywordFilter`; Matcher-Logik in `IKeywordMatcher` (`src/Reporter.Core/Interfaces/IKeywordMatcher.cs`).

## `IItemContentStore`

Datei: `src/Reporter.Core/Interfaces/IItemContentStore.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync` | `Guid itemId, CancellationToken` | `Task<string?>` | Gespeichertes ContentHtml |
| `GetRangeAsync` | `IReadOnlyList<Guid>, CancellationToken` | `Task<IReadOnlyDictionary<Guid, string>>` | Batch-Content-Abruf |
| `GetImageAsync` | `Guid itemId, CancellationToken` | `Task<ItemImage?>` | Gespeichertes Artikelbild |
| `GetImageIdsAsync` | `IReadOnlyList<Guid>, CancellationToken` | `Task<IReadOnlySet<Guid>>` | IDs mit Bild ohne Blob-Transfer (Basis der Image-Backfill-Kandidaten im Sync) |
| `SetAsync` | `Guid itemId, string? contentHtml, ItemImage? image = null, CancellationToken` | `Task` | Feldweiser Upsert |
| `SetRangeAsync` | `IReadOnlyList<ItemContentEntry>, CancellationToken` | `Task` | Batch-Upsert (Content-/Image-Backfill) |
| `DeleteAsync` / `DeleteRangeAsync` | `Guid` bzw. `IReadOnlyList<Guid>, CancellationToken` | `Task` | Content-Löschung |
| `GetItemIdsAsync` | `CancellationToken` | `Task<IReadOnlyList<Guid>>` | Alle IDs mit Inhalt oder Bild |

Voraussichtlich **nicht** betroffen: Die Anforderung sieht die `tags`-Spalte in der Haupt-DB vor (DB-seitige Suche über `context.Items`), nicht im Content-Store.
