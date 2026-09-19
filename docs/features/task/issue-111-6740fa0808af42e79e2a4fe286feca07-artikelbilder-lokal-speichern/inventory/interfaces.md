<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces

Contracts, die für die Anforderung „Artikelbilder lokal speichern" relevant sind.

## `IItemContentStore`

Datei: `src/Reporter.Core/Interfaces/IItemContentStore.cs`

Implementiert von `ItemContentRepository` (EF/`reporter-content.db`) und `FakeItemContentStore` (Tests). Upsert-Semantik: `null`/leerer Inhalt entfernt den Eintrag. Enthält aktuell **keinen** Bild-Zugriff.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync` | `Guid itemId`, `CancellationToken` | `Task<string?>` | Gespeicherten HTML-Inhalt eines Items lesen |
| `GetRangeAsync` | `IReadOnlyList<Guid> itemIds`, `CancellationToken` | `Task<IReadOnlyDictionary<Guid, string>>` | Inhalte mehrerer Items als Lookup lesen |
| `SetAsync` | `Guid itemId`, `string? contentHtml`, `CancellationToken` | `Task` | Inhalt speichern/überschreiben; `null`/leer löscht |
| `SetRangeAsync` | `IReadOnlyList<ItemContentEntry> entries`, `CancellationToken` | `Task` | Batch-Upsert über `ItemContentEntry` |
| `DeleteAsync` | `Guid itemId`, `CancellationToken` | `Task` | Eintrag löschen |
| `DeleteRangeAsync` | `IReadOnlyList<Guid> itemIds`, `CancellationToken` | `Task` | Einträge löschen |
| `GetItemIdsAsync` | `CancellationToken` | `Task<IReadOnlyList<Guid>>` | Alle Item-IDs mit gespeichertem Inhalt (Waisen-Sweep) |

Aufrufer: `FeedSyncService` (Backfill), `ItemRepository` (Hydratisierung + Mitlöschung), `FeedRepository` (Feed-Kaskade), `RetentionCleanupService` (Waisen-Sweep), `ItemContentMigrationService` (Legacy-Migration).

## `IItemRepository`

Datei: `src/Reporter.Core/Interfaces/IItemRepository.cs`

Implementiert von `ItemRepository`; Test-Dekorator `DelegatingItemRepository`. Methoden: `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `GetUnreadByDateAsync()` und paged `(int page, int pageSize, Guid? categoryId = null, bool ascending = false)`, `GetUnreadCountAsync`, `MarkAllAsReadAsync`, `ToggleSavedForLaterAsync`, `MarkAsReadAsync`, `GetByFeedAsync`, `GetByCategoryAsync`, `GetSavedForLaterAsync`, `AddRangeAsync`, `DeleteExpiredAsync`, `GetExpiredKeywordCandidatesAsync`, `DeleteRangeAsync`, `GetAllIdsAsync` — Signaturen siehe Quelldatei.

## `IFeedRepository`

Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Feed>>` | Alle Feeds |
| `GetByIdAsync` | `Guid id` | `Task<Feed?>` | Feed per ID |
| `AddAsync` | `Feed feed` | `Task` | Feed anlegen |
| `UpdateAsync` | `Feed feed` | `Task` | Feed aktualisieren |
| `DeleteAsync` | `Guid id` | `Task` | Feed löschen inkl. Item-/Content-Kaskade |
| `GetAllWithDetailsAsync` | — | `Task<IReadOnlyList<FeedListItem>>` | Feeds mit Anzeigedetails |
| `GetByUrlAsync` | `string url` | `Task<Feed?>` | Feed per URL |

## `IFeedSyncService`

Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId`, `CancellationToken` | `Task<SyncResult>` | Einzelnen Feed synchronisieren |
| `SyncAllAsync` | `CancellationToken` | `Task<SyncResult>` | Alle Feeds synchronisieren |

## `IRetentionCleanupService`

Datei: `src/Reporter.Core/Interfaces/IRetentionCleanupService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CleanupAsync` | `CancellationToken` | `Task<int>` | Abgelaufene gelesene Items löschen (inkl. Waisen-Sweep); Rückgabe = Anzahl gelöschter Items |

## `IContentMigrationService`

Datei: `src/Reporter.Core/Interfaces/IContentMigrationService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `MigrateLegacyContentAsync` | `CancellationToken` | `Task` | Legacy `items.content_html`-Werte idempotent in den Content-Store kopieren |

## `IFeedIconService`

Datei: `src/Reporter.Core/Interfaces/IFeedIconService.cs`

Referenz-Contract für einen isolierten Download-Service (mögliches Vorbild für einen Bild-Download-Service).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `FindFaviconUrlAsync` | `string siteUrl`, `CancellationToken` | `Task<string?>` | Verifizierte Favicon-URL oder `null` |
| `TryFindFaviconUrlAsync` | `string feedUrl`, `string? siteUrl`, `CancellationToken` | `Task<string?>` | Fehlerisolierter Lookup — wirft nie, liefert `null` |

## `INetworkStatusService`

Datei: `src/Reporter.Core/Interfaces/INetworkStatusService.cs`

| Member | Typ | Zweck |
|--------|-----|-------|
| `ConnectivityChanged` | `event EventHandler?` | Netzwerkwechsel (UI-Thread) |
| `IsOnline` | `bool` (get) | Aktueller Online-Status |

## `IDebugLogService`

Datei: `src/Reporter.Core/Interfaces/IDebugLogService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `BeginSessionAsync` | `CancellationToken` | `Task` | Debug-Sitzung starten |
| `SetEnabled` | `bool enabled` | `void` | Sammlung ein-/ausschalten |
| `LogAsync` | `string category`, `string message`, `string? details`, `string level`, `CancellationToken` | `Task` | Eintrag schreiben; no-op wenn deaktiviert, wirft nie |

Optionale Abhängigkeit im `FeedSyncService` (Muster für Bild-Download-Fehler-Protokollierung, Kategorien in `DebugLogCategory`, u. a. `Sync`).
