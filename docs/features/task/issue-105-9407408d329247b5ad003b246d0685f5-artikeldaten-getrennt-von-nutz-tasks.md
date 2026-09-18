<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Artikeldaten getrennt von Nutzerdaten speichern (iCloud-Backup) — Issue #105

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `ItemContent`-Entity anlegen (`src/Reporter.Data/Entities/ItemContent.cs`: `ItemId` PK, `ContentHtml`) | Offen | — |
| 2 | Datenmodell | `ContentDbContext` anlegen (`src/Reporter.Data/ContentDbContext.cs`, `DbSet<ItemContent>`, Tabelle `item_contents`) | Offen | — |
| 3 | Datenmodell | `ContentDbContextFactory` (Design-Time) anlegen (`src/Reporter.Data/ContentDbContextFactory.cs`) | Offen | — |
| 4 | Datenmodell | `ContentDatabasePath`-Träger anlegen (`src/Reporter.Core/Models/ContentDatabasePath.cs`) | Offen | — |
| 5 | Datenmodell | `ItemContentEntry`-Record anlegen (`src/Reporter.Core/Models/ItemContentEntry.cs`) | Offen | — |
| 6 | Datenmodell | `ContentHtml` aus `Reporter.Data.Entities.Item` entfernen | Offen | — |
| 7 | Datenmodell | `content_html`-Mapping in `ReporterDbContext.ConfigureItem` entfernen | Offen | — |
| 8 | Interfaces | `IItemContentStore`-Interface anlegen (`src/Reporter.Core/Interfaces/IItemContentStore.cs`) | Offen | — |
| 9 | Interfaces | `IBackupExclusionService` um `IncludeInBackup(string filePath)` erweitern | Offen | — |
| 10 | Interfaces | `IItemRepository` um `GetAllIdsAsync(CancellationToken)` erweitern | Offen | — |
| 11 | Interfaces | `IContentMigrationService`-Interface anlegen (`src/Reporter.Core/Interfaces/IContentMigrationService.cs`) | Offen | — |
| 12 | Logik | `ItemContentRepository` implementieren (`src/Reporter.Data/Repositories/ItemContentRepository.cs`, Upsert-Semantik) | Offen | — |
| 13 | Logik | `ItemContentMigrationService` implementieren (`src/Reporter.Data/ItemContentMigrationService.cs`, `PRAGMA table_info`-Idempotenz, seitenweise Kopie) | Offen | — |
| 14 | Logik | `BackupExclusionPlan`-Helper anlegen (`src/Reporter.Core/Services/BackupExclusionPlan.cs`, Excluded-/Included-Pfadmengen) | Offen | — |
| 15 | Logik | `BackupExclusionService.IncludeInBackup` implementieren (`NSNumber.FromBoolean(false)` unter `#if IOS`) | Offen | — |
| 16 | Logik | `ItemRepository`: Konstruktor um `IItemContentStore` erweitern; Schreibpfade `AddAsync`/`AddRangeAsync`/`UpdateAsync` auf Store umstellen | Offen | — |
| 17 | Logik | `ItemRepository`: Lesepfade `GetByIdAsync`/`GetAllAsync`/`GetUnreadByDateAsync()`/`GetByFeedAsync`/`GetByCategoryAsync`/`GetExpiredKeywordCandidatesAsync` auf Store umstellen (`MapToModel` mit Content-Parameter) | Offen | — |
| 18 | Logik | `ItemRepository`: Listenprojektion umstellen (`SelectListItemRows`/`ItemListRow` ohne `ContentHtml`, `GetRangeAsync` pro Seite, `MapToListItem` mit Content-Parameter) | Offen | — |
| 19 | Logik | `ItemRepository`: Löschpfade `DeleteAsync`/`DeleteExpiredAsync`/`DeleteRangeAsync` um Content-Löschung erweitern; `GetAllIdsAsync` implementieren | Offen | — |
| 20 | Logik | `FeedRepository.DeleteAsync`: Item-Ids vor Kaskade selektieren, Content via `IItemContentStore.DeleteRangeAsync` mitlöschen (Konstruktor erweitern) | Offen | — |
| 21 | Logik | `FeedSyncService`: `IItemContentStore` injizieren; `CollectNewItems` sammelt Content-Backfill für bekannte Items ohne Inhalt; `RunSyncAsync` schreibt via `SetRangeAsync` | Offen | — |
| 22 | Logik | `RetentionCleanupService`: `IItemContentStore` injizieren; Waisen-Sweep in `CleanupAsync` (`GetItemIdsAsync` ∖ `GetAllIdsAsync` → `DeleteRangeAsync`) | Offen | — |
| 23 | Migrationen | EF-Migration `InitialContentCreate` für `ContentDbContext` scaffolden (OutputDir `Migrations/Content`) | Offen | — |
| 24 | Migrationen | EF-Migration `DropItemContentHtml` für `ReporterDbContext` scaffolden (`items.content_html` entfernen) | Offen | — |
| 25 | Konfiguration/Start | `MauiProgram`: `contentDatabasePath` aus `Path.GetFullPath(databasePath)`-Verzeichnis ableiten (`reporter-content.db`) | Offen | — |
| 26 | Konfiguration/Start | `MauiProgram`: `AddDbContextFactory<ContentDbContext>`, `ContentDatabasePath`, `IItemContentStore`, `IContentMigrationService` registrieren | Offen | — |
| 27 | Konfiguration/Start | `MauiProgram`: `MigrateContentStoreAndLegacyData` (Content-`Migrate()` + `MigrateLegacyContentAsync`) try/catch-isoliert vor `ApplyPersistedLanguage` einfügen | Offen | — |
| 28 | Konfiguration/Start | `App`: `RunStartupStepAsync`-Schritt `MigrateContentAsync` nach `MigrateDatabaseAsync` einfügen | Offen | — |
| 29 | Konfiguration/Start | `App.ExcludeDatabaseFilesFromBackup`: `IncludeInBackup` auf `reporter.db`+Sidecars, `ExcludeFromBackup` auf Content-DB+Sidecars via `BackupExclusionPlan` | Offen | — |
| 30 | Tests | `TestContentDbContextFactory`-Hilfsklasse anlegen (`src/Reporter.Tests/TestContentDbContextFactory.cs`) | Offen | — |
| 31 | Tests | `FakeItemContentStore` (In-Memory) anlegen (`src/Reporter.Tests/FakeItemContentStore.cs`) | Offen | — |
| 32 | Tests | `FakeBackupExclusionService` um `IncludeInBackup`/`IncludedPaths` erweitern | Offen | — |
| 33 | Tests | `DelegatingItemRepository` um `GetAllIdsAsync`-Delegation erweitern | Offen | — |
| 34 | Tests | Konstruktorverdrahtung in bestehenden Tests anpassen (`ItemRepositoryTests`, `FeedSyncServiceTests`( +`_DebugLog`), `RetentionCleanupServiceTests`, `FeedRepositoryTests`, `ServiceCollectionTests.AddTestRepositories` u. a.) | Offen | — |
| 35 | Tests | `ReporterDbContextTests_Persistence.Item_PersistRoundtrip` und sonstige Entity-Seeds ohne `content_html` anpassen | Offen | — |
| 36 | Tests | `ItemContentRepositoryTests` anlegen: CRUD-/Batch-Roundtrips und Upsert-Semantik | Offen | — |
| 37 | Tests | `ItemContentMigrationServiceTests` anlegen: Legacy-Kopie, Idempotenz, No-op ohne `content_html`-Spalte | Offen | — |
| 38 | Tests | `ItemRepositoryTests` erweitern: Content-Roundtrip über Store, Content-Mitlöschung, Listenprojektion (`ImageUrl`/`Summary`/`ReadingTimeText`) | Offen | — |
| 39 | Tests | `FeedSyncServiceTests` erweitern: Backfill bei fehlendem Content, kein Überschreiben vorhandenen Contents | Offen | — |
| 40 | Tests | `RetentionCleanupServiceTests` erweitern: Waisen-Sweep entfernt Content ohne Item | Offen | — |
| 41 | Tests | `FeedRepositoryTests` erweitern: Feed-Löschung entfernt Item-Contents | Offen | — |
| 42 | Tests | `BackupExclusionPlanTests` anlegen: Excluded-/Included-Pfadmengen | Offen | — |
| 43 | Tests | `ServiceCollectionTests` erweitern: `IItemContentStore`/`IDbContextFactory<ContentDbContext>`/`ContentDatabasePath`/`IContentMigrationService` auflösbar; Pfadmengen via `FakeBackupExclusionService` | Offen | — |
| 44 | Tests | `ContentDbContextTests` anlegen: Tabellenmapping + `ItemContent`-Persistenz-Roundtrip | Offen | — |
| 45 | Dokumentation | `docs/privacy-policy.md`: Abschnitt „Lokale Datenspeicherung" auf Zwei-Speicher-Modell anpassen (deutsch + englisch) | Offen | — |
| 46 | Dokumentation | `docs/app-store-review.md`: Backup-Verhalten neu beschreiben (nur Content ausgeschlossen, Nutzerdaten gesichert) | Offen | — |
| 47 | Dokumentation | `docs/help/anwendung/architektur.md` + `datenmodell.md`: zweiter Speicher, `ContentDbContext`, `ContentDatabasePath`, `IncludeInBackup` dokumentieren | Offen | — |
| 48 | Dokumentation | `docs/help/anwendung/aufbewahrung.md` + `offline.md`: Content-Cleanup/Waisen-Sweep und Verhalten bei fehlendem Content (Backfill beim Sync) dokumentieren | Offen | — |
| 49 | Dokumentation | `docs/help/tests/*`: `REPORTER_DB_PATH`-Ableitung des Content-Pfads dokumentieren (`api.md`, `ablauf-technisch.md`, `architektur.md` ggf.) | Offen | — |
| 50 | Qualitätssicherung | `dotnet test Reporter.sln --filter "Category!=E2E"` und `.\scripts\Run-StaticChecks.ps1` ohne Befund ausführen | Offen | — |
