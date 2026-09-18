<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Artikeldaten getrennt von Nutzerdaten speichern (iCloud-Backup) — Issue #105

## Übersicht

Die SQLite-Datenbank `reporter.db` wird in zwei Speicher aufgeteilt: Die Nutzerdaten (`feeds`, `categories`, `keywords`, `settings`, `sync_logs`, `debug_log_entries` sowie `items` ohne `content_html`) verbleiben in `reporter.db` und werden künftig ins iCloud-Backup eingeschlossen; der re-downloadbare Artikelinhalt (`Item.ContentHtml`) wandert in eine zweite SQLite-Datei `reporter-content.db` mit eigenem `ContentDbContext`, die weiterhin vom Backup ausgeschlossen bleibt. Betroffen sind das Datenmodell (neue `ItemContent`-Entity, Entfernen von `items.content_html`), `ItemRepository`/`FeedRepository` (zweistufige Lese-/Schreib-/Löschpfade), `FeedSyncService` (Content-Backfill nach Restore), `RetentionCleanupService` (Content-Waisen), `BackupExclusionService` (Umkehrpfad Re-Include), `MauiProgram`/`App` (Pfad-Träger, Startsequenz, Ausschluss-Menge) sowie Tests und Dokumentation. Es gibt keine UI-Änderungen.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Content-Speicher-Technologie | Zweite SQLite-Datei `reporter-content.db` im selben Verzeichnis wie `reporter.db`, eigener `ContentDbContext` + `IDbContextFactory<ContentDbContext>` + eigene Migrationshistorie | Konsistent zum bestehenden EF-Core-Stack (`IDbContextFactory`, `MigrateAsync`) und zum `BackupExclusionService` (`IsExcludedFromBackupKey` auf Datei + Sidecars); `NSCachesDirectory` würde Re-Fetch-Toleranz für alle Inhalte erfordern, ein Dateiverzeichnis brächte einen zweiten Speichermechanismus ohne Bestandsnutzen. |
| Aufteilung der `Item`-Daten | Nur `ContentHtml` wandert in den Content-Speicher; `Title`, `Link`, `PublishedAt`, `GuidOrHash`, `FeedId`, `IsRead`, `IsSavedForLater`, `ReadAt` bleiben in `reporter.db` | Identität (`GuidOrHash`+`FeedId` Unique-Index), Sortierung, Nutzerdaten und Retention-Kriterien sind klein und für Wiederzuordnung/Listen/Backup unverzichtbar; nur `content_html` ist re-downloadbare Massendaten. |
| Content-Zugriff | Neues Interface `IItemContentStore` (`Reporter.Core.Interfaces`, Repository-Muster) mit EF-Implementierung `ItemContentRepository` (`Reporter.Data`); `IItemRepository`-Contract bleibt unverändert, `ItemRepository` orchestriert beide Speicher intern | Kapselt die Zwei-Speicher-Logik an einer Stelle; Aufrufer (ViewModels, Services) bleiben unverändert; der Store ist separat testbar und auch für `FeedSyncService`/`RetentionCleanupService`/`FeedRepository` direkt injizierbar. |
| Listenprojektion (`ImageUrl`/`Summary`/`ReadingTimeText`) | Batch-Lookup: `SelectListItemRows` projiziert ohne `content_html`; die Seiten-Methoden laden `ContentHtml` für die Item-Ids der aktuellen Seite via `IItemContentStore.GetRangeAsync` nach | Single Source of Truth, keine redundanten abgeleiteten Felder mit Abgleichproblem; Seitengrößen sind klein, ein zweiter round-trip pro Seite ist vertretbar. |
| Migration des Altbestands | Bestehende `content_html`-Werte werden in `reporter-content.db` kopiert (nicht verworfen); die Kopie läuft über `IContentMigrationService` in `MauiProgram` **vor** `ApplyPersistedLanguage`, fehlerisoliert per try/catch | `ApplyPersistedLanguage` ruft `ReporterDbContext.Migrate()` bereits vor `App.OnStart` auf — die EF-Migration, die `content_html` entfernt, würde die Daten vor einer Kopie in `OnStart` zerstören. Kopieren statt Verwerfen erhält Offline-Lesbarkeit für Items, die nicht mehr im Feed-XML stehen. Idempotent über `PRAGMA table_info(items)` (Spalte vorhanden?) + `INSERT OR IGNORE`. |
| Umkehrpfad Backup-Flag | `IBackupExclusionService` wird um `IncludeInBackup(string filePath)` ergänzt (`NSNumber.FromBoolean(false)`, `#if IOS`, `File.Exists`-Guard analog) | Symmetrischer, kleiner Contract statt generischem `SetExcluded(path, bool)`; nur so kann das auf Bestandsgeräten gesetzte `IsExcludedFromBackupKey` auf `reporter.db` wieder entfernt werden. |
| Content-Backfill nach Restore | Backfill beim Sync: `FeedSyncService.CollectNewItems` sammelt für bereits bekannte `GuidOrHash` mit fehlendem `ContentHtml` (`existingItems` mit `ContentHtml == null`) den frisch gelesenen Inhalt und `RunSyncAsync` schreibt ihn via `IItemContentStore.SetRangeAsync` | Das Feed-XML trägt den Inhalt bereits — kein zusätzlicher HTTP-Pfad nötig; Lazy-Load beim Öffnen der Detailansicht hätte einen neuen Fetch-Mechanismus pro Artikel erfordert und liefe dem Zweck (offline lesbar) zuwider. Keyword-Filterung entfällt beim Backfill, da das Item beim ursprünglichen Speichern den Filter bereits passiert hat. |
| Content-Löschung | Explizites Mitlöschen in allen Löschpfaden (`ItemRepository.DeleteAsync`/`DeleteExpiredAsync`/`DeleteRangeAsync`, `FeedRepository.DeleteAsync` für die Feed-Kaskade) plus Waisen-Sweep in `RetentionCleanupService.CleanupAsync` (`IContentStore.GetItemIdsAsync` ∖ `IItemRepository.GetAllIdsAsync`) als Sicherheitsnetz | Datenbankübergreifende Kaskaden/Transaktionen existieren nicht; explizite Löschung deckt alle Pfade ab, der Sweep räumt Reste aus Teilfehlern oder der `ExecuteDelete`-Kaskade auf. |
| Pfadmengen für Backup-Ausschluss | Statischer Helper `BackupExclusionPlan` in `Reporter.Core.Services`: liefert `ExcludedPaths` (Content-DB + `-wal`/`-shm`) und `IncludedPaths` (`reporter.db` + `-wal`/`-shm`); `App.ExcludeDatabaseFilesFromBackup` wendet die Mengen an | `Reporter.Tests` referenziert nur `Reporter.Core`/`Reporter.Data`, nicht das App-Projekt — die Pfadmengen-Logik muss in `Core` liegen, damit sie per `FakeBackupExclusionService` prüfbar ist. |
| E2E-Hermetik des Content-Pfads | `contentDatabasePath` wird aus dem Verzeichnis des aufgelösten `databasePath` abgeleitet (`Path.GetFullPath(databasePath)` → Verzeichnis → `reporter-content.db`); kein neues Env-Override | `REPORTER_DB_PATH` zeigt in der E2E-Fixture auf ein Temp-Verzeichnis — die Content-DB landet automatisch im selben, im Teardown gelöschten Verzeichnis. Ein `REPORTER_CONTENT_DB_PATH`-Override wäre redundante Konfiguration. |
| Verbleib `SyncLog`/`DebugLogEntry` | Beide verbleiben in `reporter.db` | Klein bzw. sitzungsbezogen, keine Massendaten; ein Umzug brächte keinen Backup-Größenvorteil, aber zweite Schreibpfade für die Logger. |

## Programmabläufe

### App-Start nach Update (Bestandsmigration + Backup-Ausschluss)

1. `MauiProgram.CreateMauiApp` löst `databasePath` wie bisher auf (`REPORTER_DB_PATH`-Override) und leitet `contentDatabasePath` aus `Path.GetFullPath(databasePath)` → Verzeichnis + `reporter-content.db` ab; `isFirstRun` bleibt an `reporter.db` gebunden.
2. DI-Registrierung: `AddDbContextFactory<ContentDbContext>` (`UseSqlite` auf den Content-Pfad), `ContentDatabasePath`-Singleton, `IItemContentStore → ItemContentRepository`, `IContentMigrationService → ItemContentMigrationService`.
3. Nach `builder.Build()`, vor `ApplyPersistedLanguage(app)`: neuer, per try/catch fehlerisolierter Schritt `MigrateContentStoreAndLegacyData(app)` — `ContentDbContext.Database.Migrate()` erzeugt das `item_contents`-Schema, danach `IContentMigrationService.MigrateLegacyContentAsync()`.
4. `ItemContentMigrationService.MigrateLegacyContentAsync` prüft per Raw-SQL (`PRAGMA table_info('items')` über die `ReporterDbContext`-Verbindung), ob `items.content_html` existiert; falls nein → No-op (idempotent). Falls ja: seitenweise `SELECT id, content_html FROM items WHERE content_html IS NOT NULL` lesen und via `IItemContentStore.SetRangeAsync` (`INSERT OR REPLACE`/`INSERT OR IGNORE`-Semantik) in `item_contents` schreiben.
5. `ApplyPersistedLanguage` migriert `reporter.db` — die neue EF-Migration `DropItemContentHtml` entfernt `items.content_html`.
6. `App.OnStart`: `MigrateDatabaseAsync` (idempotent, Sicherheitsnetz falls Schritt 3–5 teilweise fehlschlugen), dann neuer `RunStartupStepAsync`-Schritt `MigrateContentAsync(scope)` (erneuter, idempotenter Aufruf von `IContentMigrationService.MigrateLegacyContentAsync` — fängt den Fall ab, dass Schritt 3 scheiterte, aber `reporter.db` noch nicht migriert ist), danach `ExcludeDatabaseFilesFromBackup` (sync, `RunStartupStep`).
7. `ExcludeDatabaseFilesFromBackup` nutzt `BackupExclusionPlan`: `IncludeInBackup` auf `reporter.db` + `-wal` + `-shm` (entfernt das Bestands-Flag; bei Neuinstallation harmlose No-op), `ExcludeFromBackup` auf `reporter-content.db` + `-wal` + `-shm`.
8. Restliche Startsequenz unverändert (`SeedDemoContentAsync` → `CleanupRetainedDataAsync` → …); der Cleanup enthält nun den Waisen-Sweep.

Beteiligte Klassen/Komponenten: `MauiProgram`, `App`, `IContentMigrationService`, `ItemContentMigrationService`, `ContentDbContext`, `ReporterDbContext`, `IItemContentStore`, `BackupExclusionPlan`, `IBackupExclusionService`, `ContentDatabasePath`, `DatabasePath`

### Feed-Sync mit Content-Backfill

1. `FeedSyncService.RunSyncAsync` lädt `existingItems` via `IItemRepository.GetByFeedAsync` — die Modelle enthalten `ContentHtml` bereits aus dem Content-Speicher.
2. `CollectNewItems` baut neben dem `knownKeys`-`HashSet` zusätzlich ein Dictionary `guidOrHash → itemId` der `existingItems` mit `ContentHtml == null` (Backfill-Kandidaten).
3. Bekannte Items (`!knownKeys.Add`) werden weiterhin übersprungen — steht ihr `guidOrHash` im Backfill-Dictionary und liefert `GetContentHtml(feedItem)` einen Inhalt, wird ein `ItemContentEntry(itemId, contentHtml)` gesammelt und der Schlüssel aus dem Dictionary entfernt (Dedup innerhalb des Dokuments).
4. Neue Items werden wie bisher gegen `IKeywordFilter.MatchesAny` geprüft und via `AddRangeAsync` gespeichert (das Repository schreibt Content intern in den Store).
5. `RunSyncAsync` ruft bei nicht-leerer Backfill-Liste `IItemContentStore.SetRangeAsync` auf; Fehler im Backfill beeinflussen das Sync-Ergebnis nicht zusätzlich (Teil des bestehenden try/catch in `SyncFeedAsync`).

Beteiligte Klassen/Komponenten: `FeedSyncService`, `IItemRepository`, `IItemContentStore`, `ItemContentEntry`, `INotificationService`, `IKeywordFilter`

### Artikellisten laden (Unread/Saved)

1. `GetUnreadByDateAsync(page, pageSize, categoryId, ascending)`/`GetSavedForLaterAsync(page, pageSize)` führen die bestehende Filter-/Sortier-/Paging-Query aus; `SelectListItemRows` projiziert ohne `ContentHtml`.
2. Für die Ids der Seitenzeilen ruft das Repository `IItemContentStore.GetRangeAsync(ids)` auf (ein Batch-Select auf `item_contents`).
3. `MapToListItem(row, contentHtml)` leitet `ImageUrl`, `Summary`, `ReadingTimeText` aus dem nachgeladenen Content ab; fehlender Content → `null`-Ableitungen (Karten bleiben wie heute bedienbar).

Beteiligte Klassen/Komponenten: `ItemRepository`, `IItemContentStore`, `ItemListRow`, `ItemListItem`, `ReadingTimeEstimator`

### Artikeldetail laden / Item lesen

1. `GetByIdAsync` lädt die `items`-Zeile und holt `ContentHtml` via `IItemContentStore.GetAsync(id)`; `MapToModel` erhält den Content als Parameter.
2. `GetAllAsync`, `GetUnreadByDateAsync()`, `GetByFeedAsync`, `GetByCategoryAsync`, `GetExpiredKeywordCandidatesAsync` laden Content pro Treffermenge via `GetRangeAsync`.
3. `ArticleDetailViewModel.RebuildHtml` bleibt unverändert — der defensive Leer-Pfad (`HtmlSource = string.Empty`) deckt nach einem Restore fehlenden Content ab, bis der Sync-Backfill ihn nachlädt.

Beteiligte Klassen/Komponenten: `ItemRepository`, `IItemContentStore`, `ArticleDetailViewModel`

### Item-Löschungen (Retention, Keyword, manuell, Feed-Kaskade)

1. `DeleteAsync(id)`: `items`-Zeile entfernen, dann `IItemContentStore.DeleteAsync(id)`.
2. `DeleteExpiredAsync(cutoff)`/`DeleteRangeAsync(ids)`: zuerst die betroffenen Item-Ids per Select ermitteln, `ExecuteDelete` auf `items`, danach `IItemContentStore.DeleteRangeAsync(ids)`; Rückgabewert bleibt die Anzahl gelöschter Items.
3. `FeedRepository.DeleteAsync(id)`: vor dem Entfernen des Feeds die Item-Ids des Feeds selektieren (`context.Items.Where(i => i.FeedId == id).Select(i => i.Id)`), Feed entfernen (DB-Kaskade löscht die `items`-Zeilen), danach `IItemContentStore.DeleteRangeAsync(itemIds)`.
4. `RetentionCleanupService.CleanupAsync` führt nach den bisherigen Schritten den Waisen-Sweep aus: `GetItemIdsAsync` (Content-Speicher) ∖ `GetAllIdsAsync` (`items`) → `IItemContentStore.DeleteRangeAsync(orphans)`; der Rückgabewert zählt weiterhin gelöschte Items.

Beteiligte Klassen/Komponenten: `ItemRepository`, `FeedRepository`, `RetentionCleanupService`, `IItemContentStore`, `IItemRepository`

### Item schreiben (Add/Update)

1. `AddAsync`/`AddRangeAsync`: `MapToEntity` erzeugt Entities ohne `ContentHtml`; nach dem `items`-Insert schreibt das Repository den Content via `SetAsync`/`SetRangeAsync` (nur nicht-leere Inhalte werden als Zeile angelegt; `Set*` mit `null` entfernt eine etwaige Zeile — Upsert-Semantik).
2. `UpdateAsync`: skalare Felder wie bisher, die Zeile `entity.ContentHtml = …` entfällt; danach `IItemContentStore.SetAsync(item.Id, item.ContentHtml)`.

Beteiligte Klassen/Komponenten: `ItemRepository`, `IItemContentStore`, `MapToEntity`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `ItemContent` (`src/Reporter.Data/Entities/ItemContent.cs`) | Datenmodellklasse (Entity) | `ItemId` (Guid, PK, `item_id`) + `ContentHtml` (string?, `content_html`) — Tabelle `item_contents` in `reporter-content.db`; kein FK zu `reporter.db` (datenbankübergreifend nicht möglich). |
| `ContentDbContext` (`src/Reporter.Data/ContentDbContext.cs`) | `DbContext` | Eigener Kontext für `reporter-content.db` mit `DbSet<ItemContent>` und eigener Migrationshistorie (`Migrations/Content/`). |
| `ContentDbContextFactory` (`src/Reporter.Data/ContentDbContextFactory.cs`) | `IDesignTimeDbContextFactory<ContentDbContext>` | Design-Time-Factory für EF-Tooling (`Data Source=reporter-content.db`), analog `ReporterDbContextFactory`. |
| `ContentDatabasePath` (`src/Reporter.Core/Models/ContentDatabasePath.cs`) | Träger-Klasse | DI-Träger für den effektiven Content-DB-Pfad, analog `DatabasePath`. |
| `ItemContentEntry` (`src/Reporter.Core/Models/ItemContentEntry.cs`) | Record | Transportwert `(Guid ItemId, string? ContentHtml)` für Batch-Schreiboperationen (`SetRangeAsync`). |
| `IItemContentStore` (`src/Reporter.Core/Interfaces/IItemContentStore.cs`) | Interface | Contract des Content-Speichers: `GetAsync`, `GetRangeAsync`, `SetAsync`, `SetRangeAsync`, `DeleteAsync`, `DeleteRangeAsync`, `GetItemIdsAsync`. |
| `ItemContentRepository` (`src/Reporter.Data/Repositories/ItemContentRepository.cs`) | Klasse | EF-Core-Implementierung von `IItemContentStore` über `IDbContextFactory<ContentDbContext>`; Upsert-Semantik (nur nicht-leere Inhalte erhalten eine Zeile). |
| `IContentMigrationService` (`src/Reporter.Core/Interfaces/IContentMigrationService.cs`) | Interface | `MigrateLegacyContentAsync(CancellationToken)` — Bestandsüberführung `items.content_html` → `item_contents`. |
| `ItemContentMigrationService` (`src/Reporter.Data/ItemContentMigrationService.cs`) | Klasse | Implementierung über beide `IDbContextFactory`s; spaltenbasierte Idempotenz (`PRAGMA table_info`), seitenweises Kopieren. |
| `BackupExclusionPlan` (`src/Reporter.Core/Services/BackupExclusionPlan.cs`) | Statische Hilfsklasse | Liefert die Pfadmengen für `ExcludeFromBackup` (Content-DB + Sidecars) und `IncludeInBackup` (`reporter.db` + Sidecars) aus `DatabasePath`/`ContentDatabasePath`. |
| `TestContentDbContextFactory` (`src/Reporter.Tests/TestContentDbContextFactory.cs`) | Test-Hilfsklasse | Shared-In-Memory-SQLite-Factory für `ContentDbContext`, analog `TestDbContextFactory`. |
| `FakeItemContentStore` (`src/Reporter.Tests/FakeItemContentStore.cs`) | Test-Fake | In-Memory-`IItemContentStore` (Dictionary) für Core-Service-Tests. |

## Änderungen an bestehenden Klassen

### `Item` (Entity, `src/Reporter.Data/Entities/Item.cs`)

- **Entfernte Eigenschaften:** `ContentHtml` — Inhalt wandert in `item_contents`/`reporter-content.db`.

### `Item` (Domänenmodell, `src/Reporter.Core/Models/Item.cs`)

- Keine Änderung — `ContentHtml` bleibt im Domänenmodell und wird aus dem Content-Speicher befüllt.

### `ReporterDbContext` (`src/Reporter.Data/ReporterDbContext.cs`)

- **Geänderte Methoden:** `ConfigureItem` — Mapping `entity.Property(e => e.ContentHtml).HasColumnName("content_html")` entfällt.

### `IItemRepository` (`src/Reporter.Core/Interfaces/IItemRepository.cs`)

- **Neue Methoden:** `GetAllIdsAsync(CancellationToken)` → `Task<IReadOnlyList<Guid>>` — alle Item-Ids für den Waisen-Sweep.
- Alle übrigen Signaturen bleiben unverändert (`Item` transportiert `ContentHtml` weiterhin implizit).

### `ItemRepository` (`src/Reporter.Data/Repositories/ItemRepository.cs`)

- **Konstruktor:** zusätzlicher Parameter `IItemContentStore contentStore`.
- **Geänderte Methoden:**
  - `GetByIdAsync` — Content via `contentStore.GetAsync`; `MapToModel(entity, contentHtml)`.
  - `GetAllAsync`, `GetUnreadByDateAsync()`, `GetByFeedAsync`, `GetByCategoryAsync`, `GetExpiredKeywordCandidatesAsync` — Batch-Content via `GetRangeAsync`.
  - `GetUnreadByDateAsync(paged)`, `GetSavedForLaterAsync` — `SelectListItemRows` ohne `ContentHtml`; anschließend `GetRangeAsync` für die Seiten-Ids; `MapToListItem(row, contentHtml)`.
  - `AddAsync`, `AddRangeAsync` — `MapToEntity` ohne `ContentHtml`; Content-Schreiben via `SetAsync`/`SetRangeAsync` (nur nicht-leere Inhalte).
  - `UpdateAsync` — `entity.ContentHtml = …` entfällt; `contentStore.SetAsync(item.Id, item.ContentHtml)`.
  - `DeleteAsync` — zusätzlich `contentStore.DeleteAsync(id)`.
  - `DeleteExpiredAsync`, `DeleteRangeAsync` — betroffene Ids vorher selektieren; nach dem `ExecuteDelete` `contentStore.DeleteRangeAsync(ids)`.
- **Neue Methoden:** `GetAllIdsAsync` — alle `items.id`.
- **Geänderte Member:** `ItemListRow` — Eigenschaft `ContentHtml` entfällt; `MapToModel`/`MapToEntity` ohne ContentHtml-Kopie (Content als Parameter/`SetAsync`); `MapToListItem` mit Content-Parameter.

### `FeedRepository` (`src/Reporter.Data/Repositories/FeedRepository.cs`)

- **Konstruktor:** zusätzlicher Parameter `IItemContentStore contentStore`.
- **Geänderte Methoden:** `DeleteAsync` — Item-Ids des Feeds vor dem Entfernen selektieren; nach `SaveChangesAsync` (DB-Kaskade) `contentStore.DeleteRangeAsync(itemIds)`.

### `FeedSyncService` (`src/Reporter.Core/Services/FeedSyncService.cs`)

- **Konstruktor:** zusätzlicher Parameter `IItemContentStore contentStore`.
- **Geänderte Methoden:**
  - `CollectNewItems` — Rückgabewert erweitert um `List<ItemContentEntry> ContentBackfill`; Backfill-Kandidaten aus `existingItems` mit `ContentHtml == null`; bekannte Items mit frischem Inhalt werden gesammelt statt nur übersprungen.
  - `RunSyncAsync` — nach `AddRangeAsync` `contentStore.SetRangeAsync(contentBackfill)` bei nicht-leerer Liste.

### `RetentionCleanupService` (`src/Reporter.Core/Services/RetentionCleanupService.cs`)

- **Konstruktor:** zusätzlicher Parameter `IItemContentStore contentStore`.
- **Geänderte Methoden:** `CleanupAsync` — nach den bisherigen Löschungen Waisen-Sweep (`GetItemIdsAsync` ∖ `GetAllIdsAsync` → `DeleteRangeAsync`); Rückgabewert bleibt die Item-Anzahl.

### `NotificationService` (`src/Reporter.Core/Services/NotificationService.cs`)

- Keine Änderung — die `Item`-Objekte liegen beim Aufruf im Arbeitsspeicher inkl. `ContentHtml` vor; `IKeywordFilter.MatchesAny`-Signatur bleibt unverändert.

### `IBackupExclusionService` (`src/Reporter.Core/Interfaces/IBackupExclusionService.cs`)

- **Neue Methoden:** `IncludeInBackup(string filePath)` — hebt einen gesetzten Backup-Ausschluss wieder auf; fehlende Dateien bleiben No-op.

### `BackupExclusionService` (`src/Reporter/Services/BackupExclusionService.cs`)

- **Neue Methoden:** `IncludeInBackup` — `File.Exists`-Guard, unter `#if IOS` `NSUrl.SetResource(NSUrl.IsExcludedFromBackupKey, NSNumber.FromBoolean(false))`; sonst No-op wie `ExcludeFromBackup`.

### `App` (`src/Reporter/App.xaml.cs`)

- **Geänderte Methoden:**
  - `OnStart` — neuer `RunStartupStepAsync`-Schritt `MigrateContentAsync(scope)` (idempotenter Aufruf von `IContentMigrationService.MigrateLegacyContentAsync`) nach `MigrateDatabaseAsync` und vor `ExcludeDatabaseFilesFromBackup`.
  - `ExcludeDatabaseFilesFromBackup` — nutzt `BackupExclusionPlan` mit `DatabasePath` + `ContentDatabasePath`: `IncludeInBackup` auf `reporter.db` + `-wal` + `-shm`, `ExcludeFromBackup` auf `reporter-content.db` + `-wal` + `-shm`.
- **Neue Methoden:** `MigrateContentAsync(IServiceScope)` — Start-Schritt-Wrapper um `IContentMigrationService`.

### `MauiProgram` (`src/Reporter/MauiProgram.cs`)

- **Geänderte Methoden:**
  - `CreateMauiApp` — `contentDatabasePath` aus dem Verzeichnis von `Path.GetFullPath(databasePath)` ableiten (`reporter-content.db`); Registrierungen `AddDbContextFactory<ContentDbContext>`, `new ContentDatabasePath(contentDatabasePath)`, `IItemContentStore → ItemContentRepository`, `IContentMigrationService → ItemContentMigrationService`; nach `builder.Build()` und vor `ApplyPersistedLanguage` neuer try/catch-isolierter Schritt `MigrateContentStoreAndLegacyData(app)` (Content-Schema-Migration + Legacy-Kopie).
- **Neue Methoden:** `MigrateContentStoreAndLegacyData(MauiApp)` — `ContentDbContext.Database.Migrate()` + `IContentMigrationService.MigrateLegacyContentAsync()`; Fehler werden per `Debug.WriteLine` geloggt und blockieren den Start nicht.

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `InitialContentCreate` (`ContentDbContext`, `Migrations/Content/`) | `item_contents` (`item_id` PK, `content_html`) | Erzeugt die Content-Tabelle in `reporter-content.db`; eigene Migrationshistorie/`ContentDbContextModelSnapshot`. |
| `DropItemContentHtml` (`ReporterDbContext`, `Migrations/`) | `items.content_html` | Entfernt die Content-Spalte aus `reporter.db`; läuft nach der datengetriebenen Legacy-Kopie (siehe Programmablauf „App-Start nach Update"). |

Die Überführung der Bestandsdaten (`content_html` → `item_contents`) ist **keine** EF-Migration, sondern der Laufzeit-Schritt `IContentMigrationService.MigrateLegacyContentAsync` (Pfad- und Reihenfolgeabhängig, seitenweise, idempotent).

## Validierungsregeln

Keine — es gibt keine neuen oder geänderten Benutzereingaben. Die Pfadableitung ist defensiv (`Path.GetFullPath` liefert immer ein absolutes Verzeichnis).

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `contentDatabasePath` / `ContentDatabasePath` | DI-Träger (Singleton) | `{Verzeichnis von reporter.db}/reporter-content.db` | Effektiver Pfad des Content-Speichers; aus `REPORTER_DB_PATH` abgeleitet, dadurch E2E-hermetisch ohne neues Override. |
| `IDbContextFactory<ContentDbContext>` | DI-Registrierung | `UseSqlite("Data Source={contentDatabasePath}")` | Factory für den zweiten Kontext. |

Kein neuer benutzerseitiger Konfigurationseintrag, kein neues `Settings`-Feld, kein neuer Env-Override.

## Seiteneffekte und Risiken

- **Fehlschlag der Legacy-Kopie:** Scheitert `MigrateLegacyContentAsync` (Schritt 3 im Startablauf), entfernt die nachfolgende `reporter.db`-Migration `content_html` dennoch — der Altinhalt ist dann nur noch über den Sync-Backfill wiederherstellbar und für Items, die nicht mehr im Feed-XML stehen, verloren. Bewusst akzeptiert (re-downloadbare Daten; Start darf nicht blockieren); der Inhalt von In-Feed-Items heilt sich beim nächsten Sync.
- **Fehlende datenbankübergreifende Transaktion:** Schreib-/Löschpaare (`items` + `item_contents`) können bei Abbruch auseinanderlaufen — Items ohne Content heilt der Sync-Backfill, Content-Waisen der Sweep im `RetentionCleanupService` beim nächsten Start.
- **Zusätzliche DB-Roundtrips:** Listen- und Feed-Abfragen führen einen Batch-Select auf `item_contents` zusätzlich aus (`GetRangeAsync`); Datenmenge wie bisher, aber eine zweite Datei/ein zweiter Context — Seitengrößen begrenzen die `IN`-Mengen.
- **`GetByFeedAsync` lädt pro Sync den gesamten Content eines Feeds** (für die Backfill-Erkennung nötig) — entspricht der bisherigen Datenmenge (`content_html`-Spalte), nur aus zweiter Datei.
- **`IItemRepository.UpdateAsync`-Aufrufer:** `UpdateAsync` transportiert `ContentHtml` weiterhin; aktuell einziger Produktivpfad über `ArticleDetailViewModel.CreateItemCopy` — dort wird `Item.ContentHtml` aus dem geladenen Item kopiert, also konsistent. Aufrufer, die `UpdateAsync` mit `ContentHtml == null` aufrufen, löschen den gespeicherten Inhalt (`SetAsync`-Semantik) — Verhalten wie bisher (`entity.ContentHtml = null`).
- **Testkonstruktionen:** Alle Tests, die `ItemRepository`, `FeedRepository`, `FeedSyncService` oder `RetentionCleanupService` direkt instanziieren, benötigen den neuen `IItemContentStore`-Parameter (über `ItemContentRepository` + `TestContentDbContextFactory` oder `FakeItemContentStore`).
- **Entity-Breaking-Change:** Tests/Seeder, die `Entities.Item.ContentHtml` setzen (z. B. `ReporterDbContextTests_Persistence.Item_PersistRoundtrip`, Seeder), brechen kompilierend und müssen angepasst werden.
- **iOS-Verhalten nicht unter Windows testbar:** `IncludeInBackup`/`ExcludeFromBackup` wirken nur unter `#if IOS`; die Pfadmengen-Logik (`BackupExclusionPlan`) ist die auf Windows testbare Oberfläche — manuelle Verifikation auf iOS-Gerät/Simulator empfohlen.
- **`ReporterAppFixture` (E2E):** Keine Anpassung nötig — die Content-DB landet via Pfadableitung im selben Temp-Verzeichnis und wird mitgelöscht; `FeedDbAssertions` bleibt unverändert nutzbar.

## Umsetzungsreihenfolge

1. **Datenmodell Content-Speicher: `ItemContent`, `ContentDbContext`, `ContentDbContextFactory`, `ContentDatabasePath`, `ItemContentEntry` anlegen**
   - Voraussetzungen: Keine (neue Dateien, bestehender EF-Core-Stack).
   - Beschreibung: Neue Entity (`item_id` PK, `content_html`), zweiter DbContext mit `DbSet<ItemContent>` + Tabellenkonfiguration, Design-Time-Factory, Pfad-Träger analog `DatabasePath`, Transport-Record.

2. **`IItemContentStore`-Interface und `ItemContentRepository` implementieren**
   - Voraussetzungen: Schritt 1 (`ContentDbContext`, `ItemContent`, `ItemContentEntry`).
   - Beschreibung: Interface in `Reporter.Core.Interfaces` (`GetAsync`, `GetRangeAsync`, `SetAsync`, `SetRangeAsync`, `DeleteAsync`, `DeleteRangeAsync`, `GetItemIdsAsync`); EF-Implementierung in `Reporter.Data.Repositories` mit Upsert-Semantik (nur nicht-leere Inhalte erhalten eine Zeile; `Set*` mit `null` entfernt sie).

3. **`IBackupExclusionService.IncludeInBackup` + `BackupExclusionService`-Implementierung + `BackupExclusionPlan`**
   - Voraussetzungen: `ContentDatabasePath` aus Schritt 1.
   - Beschreibung: Interface-Methode ergänzen; iOS-Pfad mit `NSNumber.FromBoolean(false)`; statischen Helper für die Pfadmengen in `Reporter.Core.Services` anlegen (testbar ohne App-Projekt).

4. **`Item.ContentHtml` (Entity) entfernen und `ItemRepository` auf zwei Speicher umstellen**
   - Voraussetzungen: Schritt 2 (`IItemContentStore`).
   - Beschreibung: `ContentHtml` aus Entity und `ConfigureItem` entfernen; Konstruktorparameter `IItemContentStore`; alle Lese-/Schreib-/Löschpfade sowie `SelectListItemRows`/`MapToListItem`/`MapToModel`/`MapToEntity` umstellen; `IItemRepository.GetAllIdsAsync` ergänzen und implementieren.

5. **`FeedRepository.DeleteAsync`: Content der kaskadierten Items mitlöschen**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: `IItemContentStore` injizieren; Item-Ids vor der Feed-Löschung selektieren; nach `SaveChangesAsync` `DeleteRangeAsync` aufrufen.

6. **`FeedSyncService`: Content-Backfill für bekannte Items ohne Inhalt**
   - Voraussetzungen: Schritte 2 und 4 (`GetByFeedAsync` liefert `ContentHtml` aus dem Store).
   - Beschreibung: `IItemContentStore` injizieren; `CollectNewItems` sammelt `ItemContentEntry`s für bekannte `GuidOrHash` mit fehlendem Content; `RunSyncAsync` schreibt sie via `SetRangeAsync`.

7. **`RetentionCleanupService`: Content-Waisen-Sweep**
   - Voraussetzungen: Schritte 2 und 4 (`GetAllIdsAsync`).
   - Beschreibung: `IItemContentStore` injizieren; nach den bisherigen Löschungen `GetItemIdsAsync` ∖ `GetAllIdsAsync` ermitteln und `DeleteRangeAsync` aufrufen.

8. **`IContentMigrationService` + `ItemContentMigrationService` implementieren**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: Interface in `Reporter.Core.Interfaces`; Implementierung in `Reporter.Data` über beide `IDbContextFactory`s: Spaltenprüfung via `PRAGMA table_info('items')` (Idempotenz), seitenweises Kopieren `items.content_html` → `item_contents`.

9. **EF-Migrationen erstellen**
   - Voraussetzungen: Schritte 1 (`ContentDbContext`) und 4 (Entity ohne `ContentHtml`); `dotnet ef`-Tooling wie bei bestehenden Migrationen.
   - Beschreibung: `InitialContentCreate` für `ContentDbContext` (OutputDir `Migrations/Content`); `DropItemContentHtml` für `ReporterDbContext` (`items.content_html` entfernen).

10. **`MauiProgram`: Content-Pfad ableiten, DI-Registrierungen, Start-Schritt vor `ApplyPersistedLanguage`**
    - Voraussetzungen: Schritte 1, 2, 8.
    - Beschreibung: `contentDatabasePath`-Ableitung aus `Path.GetFullPath(databasePath)`-Verzeichnis; `AddDbContextFactory<ContentDbContext>`, `ContentDatabasePath`, `IItemContentStore`, `IContentMigrationService` registrieren; `MigrateContentStoreAndLegacyData` (Content-`Migrate()` + Legacy-Kopie) try/catch-isoliert vor `ApplyPersistedLanguage` einfügen.

11. **`App`: Start-Schritt für Content-Migration + Backup-Ausschluss umstellen**
    - Voraussetzungen: Schritte 3, 8, 10 (`ContentDatabasePath` in DI).
    - Beschreibung: `RunStartupStepAsync`-Schritt `MigrateContentAsync` nach `MigrateDatabaseAsync`; `ExcludeDatabaseFilesFromBackup` auf `BackupExclusionPlan`-Mengen umstellen (`IncludeInBackup` für `reporter.db`-Dateien, `ExcludeFromBackup` für Content-Dateien).

12. **Test-Hilfsmittel anlegen**
    - Voraussetzungen: Schritte 1–3.
    - Beschreibung: `TestContentDbContextFactory`, `FakeItemContentStore` (In-Memory), `FakeBackupExclusionService.IncludedPaths`/`IncludeInBackup`, `DelegatingItemRepository.GetAllIdsAsync` ergänzen.

13. **Bestehende Tests anpassen**
    - Voraussetzungen: Schritte 4–7, 12.
    - Beschreibung: Konstruktoraufrufe von `ItemRepository`/`FeedRepository`/`FeedSyncService`/`RetentionCleanupService` verdrahten; `ServiceCollectionTests.AddTestRepositories` um Content-Store/-Factory erweitern; Entity-Seeds ohne `content_html` anpassen.

14. **Neue Tests schreiben**
    - Voraussetzungen: Schritte 4–8, 12.
    - Beschreibung: `ItemContentRepositoryTests`, `ItemContentMigrationServiceTests`, `BackupExclusionPlanTests`, neue Assertions in `ItemRepositoryTests`/`FeedSyncServiceTests`/`RetentionCleanupServiceTests`/`FeedRepositoryTests`/`ServiceCollectionTests`/`ContentDbContext`-Schema-Test (siehe Tests-Abschnitt).

15. **Dokumentation aktualisieren**
    - Voraussetzungen: Schritte 9–11 (endgültiges Verhalten steht).
    - Beschreibung: `docs/privacy-policy.md`, `docs/app-store-review.md`, `docs/help/anwendung/{architektur,datenmodell,aufbewahrung,offline}.md`, `docs/help/tests/*` (Pfadableitung/`REPORTER_DB_PATH`-Verhalten).

16. **Abschlussprüfung: Testsuite + statische Checks**
    - Voraussetzungen: Schritte 1–15.
    - Beschreibung: `dotnet test Reporter.sln --filter "Category!=E2E"` und `.\scripts\Run-StaticChecks.ps1` müssen ohne Befund durchlaufen (projektweite Regel).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `TestContentDbContextFactory` | — (Hilfsklasse) | Shared-In-Memory-SQLite-Factory für `ContentDbContext` (`EnsureCreated`), analog `TestDbContextFactory`. |
| `FakeItemContentStore` | — (Hilfsklasse) | In-Memory-`IItemContentStore` für `FeedSyncService`-/`RetentionCleanupService`-/`FeedRepository`-Tests. |
| `GetAsync`/`SetAsync`/`SetRangeAsync`/`DeleteAsync`/`DeleteRangeAsync`/`GetItemIdsAsync`-Roundtrips | `ItemContentRepositoryTests` | CRUD und Batch-Verhalten des Content-Stores gegen In-Memory-SQLite; Upsert-Semantik (null → Zeile entfernt). |
| `MigrateLegacyContentAsync_CopiesContentHtml` / `_Idempotent` / `_NoColumn_NoOp` | `ItemContentMigrationServiceTests` | Bestands-`reporter.db` (via Raw-SQL mit `content_html` angelegt) → Werte landen in `item_contents`; zweiter Lauf ohne Effekt; fehlende Spalte → No-op. |
| `AddAsync/GetByIdAsync`-Content-Roundtrip, `DeleteAsync`/`DeleteExpiredAsync`/`DeleteRangeAsync` entfernen Content, Listenprojektion ohne `content_html`-Spalte | `ItemRepositoryTests` (Erweiterung) | Content kommt aus dem Store; Löschungen ziehen Content-Löschung nach; `ImageUrl`/`Summary`/`ReadingTimeText` werden weiterhin projiziert. |
| `SyncFeedAsync_BackfillsMissingContent` / `_KeepsExistingContent` | `FeedSyncServiceTests` (Erweiterung) | Bekanntes Item ohne Content erhält Feed-Inhalt nachgeladen; vorhandener Content wird nicht überschrieben; Dedup-Verhalten unverändert. |
| `CleanupAsync_RemovesOrphanedContent` | `RetentionCleanupServiceTests` (Erweiterung) | `item_contents`-Zeilen ohne `items`-Zeile werden entfernt; Rückgabewert bleibt Item-Anzahl. |
| `DeleteAsync_RemovesItemContents` | `FeedRepositoryTests` (Erweiterung) | Feed-Löschung (Kaskade) entfernt die Content-Zeilen der Feed-Items. |
| `ExcludedPaths`/`IncludedPaths` | `BackupExclusionPlanTests` | Ausschluss-Menge = Content-DB + `-wal`/`-shm`; Re-Include-Menge = `reporter.db` + `-wal`/`-shm`. |
| `AddReporterServices_ResolvesContentStore` / `_ResolvesContentDatabasePath` / `_ResolvesContentMigrationService` | `ServiceCollectionTests` (Erweiterung) | `IItemContentStore`, `IDbContextFactory<ContentDbContext>`, `ContentDatabasePath`, `IContentMigrationService` auflösbar; `BackupExclusionPlan`-Mengen via `FakeBackupExclusionService` anwendbar. |
| `ItemContents_MappedToExpectedTable` / `ItemContent_PersistRoundtrip` | `ContentDbContextTests` (neu) | Tabellen-/Spaltenmapping und Persistenz der neuen Entity. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `ItemRepositoryTests` (alle Tests) | Neuer Konstruktorparameter `IItemContentStore` (echter `ItemContentRepository` + `TestContentDbContextFactory`); Entity-Seeds ohne `ContentHtml` — Content wird über den Store bzw. das Domänenmodell gesetzt. |
| `FeedSyncServiceTests`, `FeedSyncServiceTests_DebugLog` | Neuer Konstruktorparameter `IItemContentStore` (`FakeItemContentStore` oder echte Implementierung). |
| `RetentionCleanupServiceTests` | Neuer Konstruktorparameter `IItemContentStore`. |
| `FeedRepositoryTests` | Neuer Konstruktorparameter `IItemContentStore`. |
| `ServiceCollectionTests` | `AddTestRepositories` muss `IDbContextFactory<ContentDbContext>` + `IItemContentStore` registrieren; `AddReporterServices_ResolvesFeedSyncService` braucht den Store für die `FeedSyncService`-Auflösung. |
| `ReporterDbContextTests_Persistence.Item_PersistRoundtrip` | `Entities.Item.ContentHtml` existiert nicht mehr — Seed/Assertion auf Domänen-/Store-Ebene verlagern. |
| `KeywordFilterTests_E2E`, `SettingsViewModelTests_E2E`, `DebugReportTests_E2E`, `UnreadViewModelTests`, `LaterViewModelTests` und weitere Klassen, die `ItemRepository`/`FeedRepository`/`FeedSyncService`/`RetentionCleanupService` direkt bauen | Konstruktorverdrahtung (Inventar: „echte Repositories" bzw. `DelegatingItemRepository`). |
| `DelegatingItemRepository` | Neues Interface-Member `GetAllIdsAsync` delegieren. |
| `FakeBackupExclusionService` | Interface-Member `IncludeInBackup` implementieren (`IncludedPaths`-Liste). |

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich. Begründung: Die Anforderung verändert ausschließlich Speicher-Infrastruktur — kein einziger über UI oder Nutzeraktion erreichbarer Benutzerfluss entsteht oder ändert sich (keine neuen Seiten, keine geänderten Interaktionen, keine neuen Eingaben). Die sichtbaren Effekte (Artikel erscheinen in Listen, Detailansicht rendert Inhalt, Sync spielt Items ein) werden bereits durch die vorhandenen `SmokeTests`/`ArticleLinkTests` transitiv über den neuen Zwei-Speicher-Pfad ausgeübt: Ein Bruch in der Content-Verkabelung (z. B. Sync schreibt, Liste liest nicht) würde dort als fehlender Artikel sichtbar. Der für iOS spezifische Kern der Änderung (`IsExcludedFromBackupKey` setzen/entfernen) ist hinter `#if IOS` gekapselt und auf dem Windows-E2E-Host ohnehin nicht ausübbar — ein E2E-Test könnte ihn nicht belegen. Die Hermetik der Suite bleibt ohne Änderung erhalten: `reporter-content.db` wird aus dem `REPORTER_DB_PATH`-Verzeichnis abgeleitet und landet damit im Temp-Verzeichnis der `ReporterAppFixture`.

Angepasste bestehende E2E-Tests: Keine.

## Offene Punkte

Keine. Die sieben offenen Fragen der Anforderung wurden durch die Designentscheidungen beantwortet: Content-Speicher-Technologie (zweite SQLite-Datei), Altbestand-Migration (kopieren statt verwerfen), Content-Backfill (beim Sync), Backup-Flag-Umkehr (`IncludeInBackup`), `SyncLog`/`DebugLogEntry` (verbleiben in `reporter.db`), Content-Zugriff für Listen/Matching (Cross-Store-Batch je Aufruf) und E2E-Hermetik (Pfadableitung ohne neues Override).
