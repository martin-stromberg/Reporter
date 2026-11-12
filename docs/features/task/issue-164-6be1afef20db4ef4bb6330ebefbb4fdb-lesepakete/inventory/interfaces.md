<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces — Bestandsaufnahme Lesepakete

## `IItemRepository`

Datei: `src/Reporter.Core/Interfaces/IItemRepository.cs` (194 Zeilen, 21 Member)

Für die Anforderung relevante Member:

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetUnreadByDateAsync()` | — | `Task<IReadOnlyList<Item>>` | Alle ungelesenen Artikel, absteigend nach `PublishedAt` (ohne Paging/Filter). |
| `GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null, bool ascending = false)` | Seite, Seitengröße, Kategoriefilter, Sortierrichtung | `Task<IReadOnlyList<ItemListItem>>` | Seitenweise Ungelesen-Abfrage — der einzige Lade-Pfad des `UnreadViewModel`. **Kein Datumsparameter.** |
| `GetUnreadCountAsync(Guid? categoryId = null)` | Kategoriefilter | `Task<int>` | Gesamtzahl ungelesener Artikel (treibt `UnreadCount`, Chip-Counts, `UnreadBadgeText`). |
| `MarkAllAsReadAsync(Guid? categoryId = null)` | Kategoriefilter | `Task` | Markiert **alle** ungelesenen Artikel (optional kategoriefiltert) — keine Tagesgrenze. |
| `MarkAsReadAsync(Guid id)` | Artikel-ID | `Task` | Markiert einen einzelnen Artikel als gelesen. |
| `ToggleSavedForLaterAsync(Guid id)` | Artikel-ID | `Task` | Toggle des Gespeichert-Flags. |

Übrige Member (nicht direkt betroffen): `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `GetByFeedAsync` (2×), `GetByCategoryAsync`, `GetSavedForLaterAsync`, `GetSavedCountAsync`, `AddRangeAsync`, `UpdateTagsAsync`, `DeleteExpiredAsync`, `GetExpiredKeywordCandidatesAsync`, `DeleteRangeAsync`, `GetAllIdsAsync`.

**Befund:** Es existiert kein tag-begrenzter Member (weder Datumsfilter beim Lesen noch beim Massenmarkieren, keine Abfrage des ältesten ungelesenen Datums). Implementierungen des Interfaces: `ItemRepository` (Produktivcode) und `DelegatingItemRepository` (Test-Dekorator, alle Member `virtual`) — ein neuer Interface-Member muss in beiden ergänzt werden.

## `ISettingsRepository`

Datei: `src/Reporter.Core/Interfaces/ISettingsRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync(CancellationToken cancellationToken = default)` | Abbruch-Token | `Task<Settings>` | Liefert u. a. `UnreadSortOrder` und `UnreadListLayout` — in `LoadPageAsync`/`LoadAsync` gelesen. |
| `SaveAsync(Settings settings)` | Settings | `Task` | Persistiert (genutzt für Layout-Umschaltung). |

## `ICategoryRepository`

Datei: `src/Reporter.Core/Interfaces/ICategoryRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync()` | — | `Task<IReadOnlyList<Category>>` | Befüllt die Filter-Chips in `LoadAsync`. |
| `GetAllWithFeedCountAsync()` | — | `Task<IReadOnlyList<CategoryWithCount>>` | Nicht vom `UnreadViewModel` genutzt. |
| `GetByIdAsync(Guid id)` | Kategorie-ID | `Task<Category?>` | Nicht vom `UnreadViewModel` genutzt. |
| `AddAsync` / `UpdateAsync` / `DeleteAsync` | Kategorie / ID | `Task` | CRUD; nicht vom `UnreadViewModel` genutzt. |

## `IFeedSyncService`

Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync(Guid feedId, CancellationToken)` | Feed-ID, Token | `Task<SyncResult>` | Einzelfeed-Sync (nicht von `UnreadViewModel`). |
| `SyncAllAsync(CancellationToken)` | Token | `Task<SyncResult>` | Wird in `RefreshAsync` aufgerufen; `SyncResult.Status == FeedHealth.Error` steuert die Fehleranzeige. |

## `INetworkStatusService`

Datei: `src/Reporter.Core/Interfaces/INetworkStatusService.cs`

| Member | Typ | Zweck |
|--------|-----|-------|
| `IsOnline` | `bool` (get) | Offline-Guard in `RefreshAsync`; Konnektivitätsänderungen via `BaseViewModel.TrackConnectivity`. |

## `IItemContentStore` (Randbezug)

Vom `ItemRepository` für Inhalt/Bilder der Listenelemente verwendet (`GetRangeAsync`, `GetImageIdsAsync`); für die Lesepaket-Logik selbst nicht relevant, bei neuen Repository-Abfragen aber mitzudenken (gleiche Hydration wie `GetUnreadByDateAsync`).
