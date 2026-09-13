<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Interfaces

## `IItemRepository`
Datei: `src\Reporter.Core\Interfaces\IItemRepository.cs` — Implementierung: `Reporter.Data.Repositories.ItemRepository` (`src\Reporter.Data\Repositories\ItemRepository.cs`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Item>>` | Alle Items, `PublishedAt` desc (`AsNoTracking`) |
| `GetByIdAsync` | `Guid id` | `Task<Item?>` | Einzelnes Item |
| `AddAsync` | `Item item` | `Task` | **Einzelnes Insert** — eigener `DbContext` + `SaveChanges` pro Aufruf; kein `AddRangeAsync`/Batch-Member vorhanden |
| `UpdateAsync` | `Item item` | `Task` | Update via `FindAsync` |
| `DeleteAsync` | `Guid id` | `Task` | Einzellöschung (ohne Saved-for-later-Invariante) |
| `GetUnreadByDateAsync` | — | `Task<IReadOnlyList<Item>>` | Alle ungelesenen Items |
| `GetUnreadByDateAsync` | `int page, int pageSize, Guid? categoryId = null` | `Task<IReadOnlyList<ItemListItem>>` | **Bereits gepaged** (`Skip`/`Take`, `ThenBy(Id)`-Stabilisierung, Projektion inkl. `FeedTitle`/`CategoryName`/`ImageUrl`/`Summary`) |
| `GetUnreadCountAsync` | `Guid? categoryId = null` | `Task<int>` | Unread-Count je Kategorie/gesamt |
| `MarkAllAsReadAsync` | `Guid? categoryId = null` | `Task` | `ExecuteUpdateAsync` (Bulk-Update) |
| `ToggleSavedForLaterAsync` | `Guid id` | `Task` | Toggle `IsSavedForLater` |
| `MarkAsReadAsync` | `Guid id` | `Task` | `IsRead = true` + `ReadAt` |
| `GetByFeedAsync` | `Guid feedId` | `Task<IReadOnlyList<Item>>` | Items eines Feeds (Basis für Sync-Dedup, aktuell nur für Count/Datum genutzt) |
| `GetByCategoryAsync` | `Guid categoryId` | `Task<IReadOnlyList<Item>>` | Items aller Feeds einer Kategorie |
| `GetSavedForLaterAsync` | — | `Task<IReadOnlyList<ItemListItem>>` | **Ohne Paging** — komplette Liste mit Projektion wie bei `GetUnreadByDateAsync` |
| `GetByGuidOrHashAsync` | `Guid feedId, string guidOrHash` | `Task<Item?>` | Dedup-Lookup (N+1 im Sync) |
| `DeleteExpiredAsync` | `DateTime cutoff, CancellationToken` | `Task<int>` | Retention-`ExecuteDeleteAsync` (nur gelesene, nicht gespeicherte Items) |
| `GetExpiredKeywordCandidatesAsync` | `DateTime cutoff, CancellationToken` | `Task<IReadOnlyList<Item>>` | Keyword-Retention-Kandidaten |
| `DeleteRangeAsync` | `IReadOnlyList<Guid> ids, CancellationToken` | `Task<int>` | `ExecuteDeleteAsync` über ID-Liste |

## `IFeedSyncService`
Datei: `src\Reporter.Core\Interfaces\IFeedSyncService.cs` — Implementierung: `FeedSyncService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId, CancellationToken = default` | `Task<SyncResult>` | Einzelnen Feed synchronisieren |
| `SyncAllAsync` | `CancellationToken = default` | `Task<SyncResult>` | Alle Feeds synchronisieren |

## `IAutoRefreshService`
Datei: `src\Reporter.Core\Interfaces\IAutoRefreshService.cs` — Implementierung: `AutoRefreshService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `StartAsync` | `CancellationToken = default` | `Task` | Settings laden, Timer ggf. starten |
| `ApplySettingsAsync` | `Settings settings` | `Task` | Timer neu konfigurieren/stoppen |
| `StopAsync` | — | `Task` | Timer stoppen |

## `IAppThemeService`
Datei: `src\Reporter.Core\Interfaces\IAppThemeService.cs` — Implementierung: `Reporter.Services.AppThemeService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ApplyTheme` | `string? theme` | `void` | `Settings.Theme`-Wert (`system`/`light`/`dark`) auf `Application.UserAppTheme` anwenden |

## `INetworkStatusService`
Datei: `src\Reporter.Core\Interfaces\INetworkStatusService.cs` — Implementierung: `Reporter.Services.NetworkStatusService`.

| Member | Typ | Zweck |
|--------|-----|-------|
| `ConnectivityChanged` | `event EventHandler?` | Online/Offline-Wechsel (implementierungsseitig auf UI-Thread ausgelöst); wird von `BaseViewModel.TrackConnectivity` abonniert |
| `IsOnline` | `bool` | Aktueller Online-Status |

## Weitere relevante Contracts (Peripherie)

- `ICategoryRepository` (`src\Reporter.Core\Interfaces\ICategoryRepository.cs`) — liefert u. a. `GetAllAsync`, `GetAllWithFeedCountAsync` (für `UnreadViewModel`-Chips bzw. `CategoriesPage`).
- `IFeedRepository` — `GetAllAsync`/`GetByIdAsync`/`UpdateAsync` u. a. (Sync + FeedsPage).
- `ISettingsRepository` — `GetAsync`/`SaveAsync` (Theme, AutoRefresh, Sprache).
- `ISyncLogRepository` — `AddAsync`/`UpdateAsync` (Sync-Protokoll).
- `INotificationService`, `ILocalNotificationService` — Benachrichtigungen nach Sync bzw. Permission-Status.
- `IFeedSearchService` — Feed-Suche (`FeedsViewModel.Search`).
