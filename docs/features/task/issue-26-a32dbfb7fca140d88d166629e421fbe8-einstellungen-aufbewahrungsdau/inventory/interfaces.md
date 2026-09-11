# Interfaces — Bestandsaufnahme

Alle Interfaces liegen in `src/Reporter.Core/Interfaces/` und folgen der Konvention `I<Name>`.

## `ISettingsRepository`

Datei: `src/Reporter.Core/Interfaces/ISettingsRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync` | `CancellationToken cancellationToken = default` | `Task<Settings>` | Singleton-Settings lesen; legt Default an, falls nicht vorhanden |
| `SaveAsync` | `Settings settings` | `Task` | Speichert immer auf den Singleton-Datensatz |

Implementiert von `SettingsRepository`; konsumiert von `RetentionCleanupService`, `SettingsViewModel`, `ArticleDetailViewModel`.

## `IKeywordRepository`

Datei: `src/Reporter.Core/Interfaces/IKeywordRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | – | `Task<IReadOnlyList<Keyword>>` | Alle Keywords (sortiert nach `KeywordText`) |
| `GetByIdAsync` | `Guid id` | `Task<Keyword?>` | Keyword per ID oder `null` |
| `AddAsync` | `Keyword keyword` | `Task` | Keyword hinzufügen |
| `UpdateAsync` | `Keyword keyword` | `Task` | Keyword-Text aktualisieren |
| `DeleteAsync` | `Guid id` | `Task` | Keyword per ID löschen |

Implementiert von `KeywordRepository`. Keine `CancellationToken`-Parameter. Wird aktuell von **keiner** anderen Komponente konsumiert (nur DI-Registrierung + Tests).

## `IItemRepository`

Datei: `src/Reporter.Core/Interfaces/IItemRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | – | `Task<IReadOnlyList<Item>>` | Alle Items, `PublishedAt` absteigend |
| `GetByIdAsync` | `Guid id` | `Task<Item?>` | Item per ID oder `null` |
| `AddAsync` | `Item item` | `Task` | Item hinzufügen |
| `UpdateAsync` | `Item item` | `Task` | Item aktualisieren |
| `DeleteAsync` | `Guid id` | `Task` | Einzel-Delete ohne Invarianten; explizit nicht für automatisches Cleanup (Zeilen 37–44) |
| `GetUnreadByDateAsync` | – | `Task<IReadOnlyList<Item>>` | Alle ungelesenen Items |
| `GetUnreadByDateAsync` | `int page, int pageSize, Guid? categoryId = null` | `Task<IReadOnlyList<ItemListItem>>` | Paged Ungelesen-Liste inkl. Kategoriefilter |
| `GetUnreadCountAsync` | `Guid? categoryId = null` | `Task<int>` | Anzahl ungelesener Items |
| `MarkAllAsReadAsync` | `Guid? categoryId = null` | `Task` | Alle ungelesenen Items als gelesen markieren |
| `ToggleSavedForLaterAsync` | `Guid id` | `Task` | `IsSavedForLater` invertieren |
| `MarkAsReadAsync` | `Guid id` | `Task` | Item als gelesen markieren (`ReadAt = UtcNow`) |
| `GetByFeedAsync` | `Guid feedId` | `Task<IReadOnlyList<Item>>` | Items eines Feeds |
| `GetByCategoryAsync` | `Guid categoryId` | `Task<IReadOnlyList<Item>>` | Items einer Kategorie |
| `GetSavedForLaterAsync` | – | `Task<IReadOnlyList<ItemListItem>>` | Gespeicherte Items |
| `GetByGuidOrHashAsync` | `Guid feedId, string guidOrHash` | `Task<Item?>` | Item per Original-GUID/Hash |
| `DeleteExpiredAsync` | `DateTime cutoff, CancellationToken cancellationToken = default` | `Task<int>` | Löscht nur `IsRead && !IsSavedForLater` und `(ReadAt ?? PublishedAt) < cutoff`; Items ohne beide Zeitstempel bleiben erhalten (Zeilen 117–127) |

## `IRetentionCleanupService`

Datei: `src/Reporter.Core/Interfaces/IRetentionCleanupService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CleanupAsync` | `CancellationToken cancellationToken = default` | `Task<int>` | Löscht gelesene Items älter als `RetentionDays`; gespeicherte Items nie. Rückgabe: Anzahl gelöschter Items |

Implementiert von `RetentionCleanupService`; aufgerufen in `App.OnStart`.

## `IFeedSyncService`

Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId, CancellationToken cancellationToken = default` | `Task<SyncResult>` | Einzelnen Feed synchronisieren |
| `SyncAllAsync` | `CancellationToken cancellationToken = default` | `Task<SyncResult>` | Alle Feeds synchronisieren — vorgesehener Aufrufpunkt der geplanten Hintergrund-Aktualisierung |

Implementiert von `FeedSyncService`; aufgerufen von `UnreadViewModel.RefreshCommand` und `FeedsViewModel.RefreshAllCommand`.
