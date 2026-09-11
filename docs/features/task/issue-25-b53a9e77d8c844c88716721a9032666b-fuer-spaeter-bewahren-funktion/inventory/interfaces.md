# Interfaces – Bestandsaufnahme

## `IItemRepository`
Datei: `src/Reporter.Core/Interfaces/IItemRepository.cs` — implementiert von `Reporter.Data.Repositories.ItemRepository`; konsumiert von `LaterViewModel`, `UnreadViewModel`, `ArticleDetailViewModel`, `FeedSyncService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync()` | — | `Task<IReadOnlyList<Item>>` | Alle Items |
| `GetByIdAsync(Guid id)` | `id` | `Task<Item?>` | Einzelnes Item |
| `AddAsync(Item item)` | `item` | `Task` | Item anlegen |
| `UpdateAsync(Item item)` | `item` | `Task` | Item aktualisieren (überträgt `IsSavedForLater`) |
| `DeleteAsync(Guid id)` | `id` | `Task` | Item löschen — **kein `IsSavedForLater`-Schutz im Contract oder in der Implementierung** |
| `GetUnreadByDateAsync()` | — | `Task<IReadOnlyList<Item>>` | Ungelesene Items, `PublishedAt` absteigend |
| `GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null)` | `page`, `pageSize`, `categoryId` | `Task<IReadOnlyList<ItemListItem>>` | Paged Unread-Liste |
| `GetUnreadCountAsync(Guid? categoryId = null)` | `categoryId` | `Task<int>` | Ungelesenen-Zähler |
| `MarkAllAsReadAsync(Guid? categoryId = null)` | `categoryId` | `Task` | Alle ungelesenen Items als gelesen markieren |
| `ToggleSavedForLaterAsync(Guid id)` | `id` | `Task` | **Bewahrungsstatus umschalten (Feature-Methode)** |
| `MarkAsReadAsync(Guid id)` | `id` | `Task` | Item als gelesen markieren |
| `GetByFeedAsync(Guid feedId)` | `feedId` | `Task<IReadOnlyList<Item>>` | Items eines Feeds |
| `GetByCategoryAsync(Guid categoryId)` | `categoryId` | `Task<IReadOnlyList<Item>>` | Items einer Kategorie |
| `GetSavedForLaterAsync()` | — | `Task<IReadOnlyList<ItemListItem>>` | **Bewahrte Artikel (Feature-Methode)** — Contract nennt keine Sortierung; Implementierung sortiert `PublishedAt` absteigend |
| `GetByGuidOrHashAsync(Guid feedId, string guidOrHash)` | `feedId`, `guidOrHash` | `Task<Item?>` | Duplikatprüfung beim Sync |

Nicht vorhanden: eine Retention-/Cleanup-Methode (z. B. `DeleteExpiredAsync`) — die Anforderung, bewahrte Artikel nie automatisch zu löschen, kann derzeit an keiner Methode verletzt werden.

## `ISettingsRepository`
Datei: `src/Reporter.Core/Interfaces/ISettingsRepository.cs` — implementiert von `SettingsRepository`; konsumiert von `SettingsViewModel`, `ArticleDetailViewModel`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync()` | — | `Task<Settings>` | Singleton-Einstellungen laden (legt Default-Datensatz an, falls fehlend) |
| `SaveAsync(Settings settings)` | `settings` | `Task` | Einstellungen speichern (inkl. `RetentionDays`) |

## `IFeedRepository` (Cascade-Kontext)
Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs` — implementiert von `FeedRepository`; konsumiert u. a. von `FeedsViewModel` (`DeleteCommand`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `DeleteAsync(Guid id)` | `id` | `Task` | Feed löschen — entfernt über `DeleteBehavior.Cascade` (`ReporterDbContext.cs:107`) auch alle Items, einschließlich bewahrter |

Übrige Member (`GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `GetAllWithDetailsAsync`, `GetByUrlAsync`) sind für die Anforderung nicht direkt relevant.

## `IFeedSyncService`
Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs` — implementiert von `FeedSyncService`; konsumiert von `UnreadViewModel` (`RefreshCommand`), `FeedsViewModel` (`RefreshCommand`, `RefreshAllCommand`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync(Guid feedId, CancellationToken cancellationToken = default)` | `feedId`, `cancellationToken` | `Task<SyncResult>` | Einzelnen Feed synchronisieren — fügt nur hinzu, löscht nichts |
| `SyncAllAsync(CancellationToken cancellationToken = default)` | `cancellationToken` | `Task<SyncResult>` | Alle Feeds synchronisieren — fügt nur hinzu, löscht nichts |

`SyncResult` (`src/Reporter.Core/Services/SyncResult.cs`): `record SyncResult(string Status, int NewItems, string? Message = null)`.
