<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces — Bestandsaufnahme

Contracts der betroffenen Services und Repositories. Alle liegen in `src/Reporter.Core/Interfaces/` und haben damit keine MAUI-Abhängigkeit (testbar in `Reporter.Tests`).

## `IItemRepository`
Datei: `src/Reporter.Core/Interfaces/IItemRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Item>>` | Alle Items, absteigend nach `PublishedAt` |
| `GetByIdAsync` | `Guid id` | `Task<Item?>` | Einzelnes Item inkl. Content/Bild-Hydration |
| `AddAsync` | `Item item` | `Task` | Item + Content/Bild speichern |
| `UpdateAsync` | `Item item` | `Task` | Item + Content aktualisieren |
| `DeleteAsync` | `Guid id` | `Task` | Einzelnes Item + Content löschen |
| `GetUnreadByDateAsync` | — | `Task<IReadOnlyList<Item>>` | Alle ungelesenen Items |
| `GetUnreadByDateAsync` | `int page, int pageSize, Guid? categoryId = null, bool ascending = false` | `Task<IReadOnlyList<ItemListItem>>` | Paged-Variante des Unread-Dashboards — Muster für die neue Feed-Abfrage |
| `GetUnreadCountAsync` | `Guid? categoryId = null` | `Task<int>` | Anzahl ungelesener Items |
| `MarkAllAsReadAsync` | `Guid? categoryId = null` | `Task` | Alle ungelesenen Items als gelesen markieren |
| `ToggleSavedForLaterAsync` | `Guid id` | `Task` | „Später lesen" umschalten |
| `MarkAsReadAsync` | `Guid id` | `Task` | Als gelesen markieren (`ReadAt = UtcNow`) |
| `GetByFeedAsync` | `Guid feedId` | `Task<IReadOnlyList<Item>>` | **Bestehend:** alle Items eines Feeds, ungepaged, ohne Suchfilter, liefert `Item` statt `ItemListItem` — die Anforderung sieht eine neue Überladung `GetByFeedAsync(Guid feedId, int page, int pageSize, string? searchTerm = null)` mit `IReadOnlyList<ItemListItem>` vor |
| `GetByCategoryAsync` | `Guid categoryId` | `Task<IReadOnlyList<Item>>` | Items aller Feeds einer Kategorie |
| `GetSavedForLaterAsync` | `int page, int pageSize` | `Task<IReadOnlyList<ItemListItem>>` | Paged-Variante der „Später lesen"-Liste |
| `AddRangeAsync` | `IReadOnlyList<Item> items` | `Task` | Batch-Insert |
| `DeleteExpiredAsync` | `DateTime cutoff, CancellationToken = default` | `Task<int>` | Retention-Bereinigung |
| `GetExpiredKeywordCandidatesAsync` | `DateTime cutoff, CancellationToken = default` | `Task<IReadOnlyList<Item>>` | Keyword-Filter-Kandidaten |
| `DeleteRangeAsync` | `IReadOnlyList<Guid> ids, CancellationToken = default` | `Task<int>` | Batch-Delete |
| `GetAllIdsAsync` | `CancellationToken = default` | `Task<IReadOnlyList<Guid>>` | Alle Item-IDs |

## `IFeedRepository`
Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Feed>>` | Alle Feeds |
| `GetByIdAsync` | `Guid id` | `Task<Feed?>` | Feed-Stammdaten — für den `feedId`-Parameter der Detailansicht nutzbar |
| `AddAsync` | `Feed feed` | `Task` | Feed anlegen |
| `UpdateAsync` | `Feed feed` | `Task` | Feed aktualisieren (Rename/Kategorie/Bearbeiten) |
| `DeleteAsync` | `Guid id` | `Task` | Feed löschen (Items/Content kaskadierend) |
| `GetAllWithDetailsAsync` | — | `Task<IReadOnlyList<FeedListItem>>` | Feeds inkl. Kategoriename und `UnreadCount` |
| `GetByUrlAsync` | `string url` | `Task<Feed?>` | Duplikatprüfung |

## `IFeedSyncService`
Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId, CancellationToken = default` | `Task<SyncResult>` | Einzel-Feed-Sync — Grundlage der „Aktualisieren"-Aktion |
| `SyncAllAsync` | `CancellationToken = default` | `Task<SyncResult>` | Sync aller Feeds |

## `ICategoryRepository`
Datei: `src/Reporter.Core/Interfaces/ICategoryRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Category>>` | Kategorieliste für die „Kategorie ändern"-Aktion |
| `GetAllWithFeedCountAsync` | — | `Task<IReadOnlyList<CategoryWithCount>>` | Kategorien mit Feed-Anzahl |
| `GetByIdAsync` | `Guid id` | `Task<Category?>` | Einzelne Kategorie |
| `AddAsync` | `Category category` | `Task` | Anlegen |
| `UpdateAsync` | `Category category` | `Task` | Aktualisieren |
| `DeleteAsync` | `Guid id` | `Task` | Löschen |

## `INetworkStatusService`
Datei: `src/Reporter.Core/Interfaces/INetworkStatusService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsOnline` | — | `bool` (Property) | Aktueller Connectivity-Status |
| `ConnectivityChanged` | — | `event EventHandler?` | Event, von `BaseViewModel.TrackConnectivity` abonniert |

## `ILocalNotificationService`
Datei: `src/Reporter.Core/Interfaces/ILocalNotificationService.cs`

Optionale `FeedsViewModel`-Abhängigkeit; `IsSupported` steuert die Sichtbarkeit des Notification-Schalters im Edit-Sheet. Relevant, falls das Bearbeiten-UI in die Detailansicht wandert.

## `IFeedSearchService` / `IFeedIconService`
Dateien: `src/Reporter.Core/Interfaces/IFeedSearchService.cs`, `src/Reporter.Core/Interfaces/IFeedIconService.cs`

`FeedsViewModel`-Abhängigkeiten für die Add-Flows (`SearchAsync`, `TryFindFaviconUrlAsync`). Für die Detailansicht nur relevant, wenn das Feed-Bearbeiten (URL ändern) dorthin verlagert wird.

## `IItemContentStore`
Datei: `src/Reporter.Core/Interfaces/IItemContentStore.cs`

Abhängigkeit des `ItemRepository`: `GetRangeAsync` für Content-Hydration und `GetImageIdsAsync` für den `LocalImageLoader` der `ItemListItem`-Projektion — die neue paged-Abfrage wird beide Pfade ebenfalls benötigen.
