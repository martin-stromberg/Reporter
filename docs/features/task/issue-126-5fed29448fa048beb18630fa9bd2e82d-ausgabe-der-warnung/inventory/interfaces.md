<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces — Bestandsaufnahme

Für die Anforderung relevante Contracts. `IFeedRepository` trägt die Persistenz der Feed-Felder; `ISyncLogRepository`/`IFeedSyncService` sind Kontext. Keine der Signaturen enthält Warnungs-spezifische Member — alle Änderungen würden über die transportierten Modelle laufen, nicht über neue Interface-Methoden.

## `IFeedRepository`

Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Feed>>` | Alle Feeds. |
| `GetByIdAsync` | `Guid id` | `Task<Feed?>` | Feed per ID. |
| `AddAsync` | `Feed feed` | `Task` | Feed anlegen. |
| `UpdateAsync` | `Feed feed` | `Task` | Feed aktualisieren — Schreibpfad für `HealthStatus`/`LastError*`. |
| `DeleteAsync` | `Guid id` | `Task` | Feed löschen. |
| `GetAllWithDetailsAsync` | — | `Task<IReadOnlyList<FeedListItem>>` | Projektion inkl. `LastErrorKind`/`LastErrorMessage` — Liesepfad der `FeedDetailViewModel`. |
| `GetByUrlAsync` | `string url` | `Task<Feed?>` | Feed per URL (Dublettenprüfung). |

Implementiert von `FeedRepository` (`src/Reporter.Data/Repositories/FeedRepository.cs`); Test-Dekorator `DelegatingFeedRepository` (`src/Reporter.Tests/DelegatingFeedRepository.cs`).

## `IFeedSyncService`

Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId`, `CancellationToken` | `Task<SyncResult>` | Einzel-Feed-Sync. |
| `SyncAllAsync` | `CancellationToken` | `Task<SyncResult>` | Sync aller Feeds, aggregierter Status. |

Implementiert von `FeedSyncService`; Test-Double `FakeFeedSyncService` (`src/Reporter.Tests/FakeFeedSyncService.cs`). `SyncResult` trägt keinen Warnungsgrund.

## `ISyncLogRepository`

Datei: `src/Reporter.Core/Interfaces/ISyncLogRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<SyncLog>>` | Alle Log-Einträge (neueste zuerst). |
| `GetLatestAsync` | `int maxEntries` | `Task<IReadOnlyList<SyncLog>>` | Neueste N Einträge — Basis für den Debug-Bericht; **keine** feed-gefilterte Abfrage vorhanden. |
| `GetByIdAsync` | `Guid id` | `Task<SyncLog?>` | Eintrag per ID. |
| `AddAsync` | `SyncLog syncLog` | `Task` | Eintrag anlegen. |
| `UpdateAsync` | `SyncLog syncLog` | `Task` | Eintrag aktualisieren. |
| `DeleteAsync` | `Guid id` | `Task` | Eintrag löschen. |

Implementiert von `SyncLogRepository` (`src/Reporter.Data/Repositories/SyncLogRepository.cs`). Relevant für die offene Frage „Warnungsgrund aus `SyncLog` lesen": es gibt keinen `GetLatestForFeedAsync`-ähnlichen Member — ein solcher müsste neu hinzukommen.
