<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces — Bestandsaufnahme

## `ISyncLogRepository`

Datei: `src/Reporter.Core/Interfaces/ISyncLogRepository.cs` — implementiert von `SyncLogRepository` (`src/Reporter.Data/Repositories/SyncLogRepository.cs`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync()` | — | `Task<IReadOnlyList<SyncLog>>` | Alle Einträge, absteigend nach `StartedAt` |
| `GetLatestAsync(int)` | `maxEntries` | `Task<IReadOnlyList<SyncLog>>` | Neueste N Einträge, absteigend nach `StartedAt` — **nicht** Feed-bezogen |
| `GetByIdAsync(Guid)` | `id` | `Task<SyncLog?>` | Einzelner Eintrag |
| `AddAsync(SyncLog)` | `syncLog` | `Task` | Einfügen |
| `UpdateAsync(SyncLog)` | `syncLog` | `Task` | Aktualisieren (u. a. `Message`, `Status`, `FinishedAt`) |
| `DeleteAsync(Guid)` | `id` | `Task` | Löschen |

**Lücke für die Anforderung:** keine Methode zur Abfrage des letzten Eintrags je Feed (Variante B).

## `IFeedRepository`

Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs` — implementiert von `FeedRepository` (`src/Reporter.Data/Repositories/FeedRepository.cs`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync()` | — | `Task<IReadOnlyList<Feed>>` | Alle Feeds, nach `Title` sortiert |
| `GetByIdAsync(Guid)` | `id` | `Task<Feed?>` | Feed per ID |
| `AddAsync(Feed)` | `feed` | `Task` | Einfügen via `MapToEntity` |
| `UpdateAsync(Feed)` | `feed` | `Task` | Überschreibt alle gemappten Felder inkl. `Url`, `HealthStatus`, `FaviconUrl` (`FeedRepository.cs:56-74`) |
| `DeleteAsync(Guid)` | `id` | `Task` | Löschen |
| `GetAllWithDetailsAsync()` | — | `Task<IReadOnlyList<FeedListItem>>` | Projektion mit `CategoryName`, `UnreadCount` (`FeedRepository.cs:91-116`) |
| `GetByUrlAsync(string)` | `url` | `Task<Feed?>` | Duplikatprüfung; exakter String-Vergleich (`f.Url == url`) → **scheme-sensitiv**, `http`- vs. `https`-Variante wird nicht als Duplikat erkannt |

## `IFeedSyncService`

Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs` — implementiert von `FeedSyncService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync(Guid, CancellationToken)` | `feedId`, `cancellationToken` | `Task<SyncResult>` | Einzel-Feed-Sync; `SyncResult.Message` enthält die technische Fehlermeldung |
| `SyncAllAsync(CancellationToken)` | `cancellationToken` | `Task<SyncResult>` | Alle Feeds; aggregierte `Message` |

## `IFeedSearchService`

Datei: `src/Reporter.Core/Interfaces/IFeedSearchService.cs` — implementiert von `FeedSearchService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchAsync(string, CancellationToken)` | `query`, `cancellationToken` | `Task<IReadOnlyList<FeedSearchResult>>` | Verzeichnis- + Autodiscovery-Suche; wirft `FeedSearchUnavailableException` |

## `IFeedIconService`

Datei: `src/Reporter.Core/Interfaces/IFeedIconService.cs` — implementiert von `FeedIconService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `FindFaviconUrlAsync(string, CancellationToken)` | `siteUrl`, `cancellationToken` | `Task<string?>` | Favicon-URL einer Website ermitteln/verifizieren |
| `TryFindFaviconUrlAsync(string, string?, CancellationToken)` | `feedUrl`, `siteUrl`, `cancellationToken` | `Task<string?>` | Isolierte Variante — Fehler → `null` statt Exception |
