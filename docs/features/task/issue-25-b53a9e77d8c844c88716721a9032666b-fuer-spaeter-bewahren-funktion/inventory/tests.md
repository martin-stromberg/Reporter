# Tests – Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-11, Läufe zwischen ca. 08:18 und 08:25 MESZ (UTC+02:00)
- **Branch und Commit-ID:** `task/issue-25-b53a9e77d8c844c88716721a9032666b-fuer-spaeter-bewahren-funktion` @ `ee72f1baeb7cd8a576c34e8b5f51e25fac182352` („CI/CD-Pipeline pausieren (#56)", 2026-09-11 08:07:53 +0200)
- **Uncommittete Änderungen im getesteten Stand:** keine geänderten oder gestagten Dateien; nur untracked Doku-Artefakte des Feature-Ordners (`requirement.md`, `todo.md` sowie die hier erzeugten Inventory-Dateien). Kein Produktivcode, keine Tests, keine Konfiguration verändert.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows, .NET SDK 10.0.401 (`C:\Program Files\dotnet\sdk`), installierte Workloads `maui-windows` 10.0.20, `ios`, `android`, `maccatalyst`; Test-Runtime .NET 10.0.12; Testprojekt `net10.0`, xunit 2.9.3, xunit.runner.visualstudio 3.1.4, coverlet.collector 6.0.4.
- **Ermittelte Testsuiten und Quellen der Testbefehle:** Einzige Testsuite ist `src/Reporter.Tests/Reporter.Tests.csproj` (xUnit, SQLite-In-Memory via `TestDbContextFactory`). Befehle aus `.github/workflows/pr-staging-ci.yml` (Job `build & test`) bzw. `README.md` („Tests": `dotnet test Reporter.sln`). Die CI setzt `IncludeIosTarget=false` und nutzt `coverlet.runsettings` (schließt `Reporter.Data.Migrations.*` aus) plus Coverage-Threshold 70 %. Hinweis: Die Workflow-Trigger sind aktuell auskommentiert (Pipeline pausiert, Commit `ee72f1b`); die Befehle wurden dennoch 1:1 wie in der CI-Definition ausgeführt.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 – Restore | `IncludeIosTarget=false dotnet restore Reporter.sln -r win-x64` | Repo-Root | 0 | — | — | — | [01-restore.log](test-results/01-restore.log) |
| 2 – Build | `IncludeIosTarget=false dotnet build Reporter.sln --configuration Release --no-restore` | Repo-Root | 0 (0 Warnungen, 0 Fehler) | — | — | — | [02-build-release.log](test-results/02-build-release.log) |
| 3 – Tests | `IncludeIosTarget=false dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 70 | 0 | 0 | [03-dotnet-test.log](test-results/03-dotnet-test.log), [test-results.trx](test-results/test-results.trx), [coverage.cobertura.xml](test-results/coverage.cobertura.xml) |

Gesamtdauer Testlauf: 3,8 s. Zeilenabdeckung laut Cobertura-Report: 80,39 % (1050/1306 Zeilen), Branch-Abdeckung 68,53 % — oberhalb des CI-Schwellwerts von 70 %.

### Nachgewiesene bestehende Testfehler

Keine — alle 70 Tests bestanden im Ausgangslauf. Es existieren keine nachgewiesenen „preexisting failures".

### Testlücken und Ausführungsprobleme

- **Keine `LaterViewModelTests`:** `LaterViewModel` (`LoadCommand`, `ToggleSavedCommand`, `MarkReadCommand`, `SavedItems`) ist derzeit völlig ungetestet.
- **`ToggleSavedCommand` von `UnreadViewModel` ungetestet:** `UnreadViewModelTests` deckt Load/Select/MarkRead/MarkAllRead/Refresh ab, nicht aber den Bewahren-Toggle.
- **`ArticleDetailViewModel` nicht testbar:** Die Datei liegt in `src/Reporter/` (MAUI-Assembly `net10.0-windows…`/`net10.0-ios`); `Reporter.Tests` referenziert nur `Reporter.Core` und `Reporter.Data`. `ToggleSavedForLaterCommand`/`BookmarkButtonLabel` sind daher ohne strukturelle Änderung nicht unit-testbar.
- **Kein Retention-/Lösch-Test möglich:** Es existiert keine automatische Löschlogik (siehe `../inventory.md`, `logic.md`); ein Test „bewahrte Artikel werden nicht gelöscht" hat aktuell kein zu prüfendes Verhalten.
- **Cascade-Verhalten ungetestet:** Es gibt keinen Test, der das Löschen eines Feeds mit bewahrten Items (`DeleteBehavior.Cascade`) dokumentiert.
- Keine abgebrochenen, deaktivierten oder nicht ausführbaren Tests; keine Build-/Infrastrukturfehler im Ausgangslauf.

## Testklassen

Projekt: `src/Reporter.Tests/` — alle Klassen nutzen `TestDbContextFactory` (In-Memory-SQLite, `EnsureCreated`), sofern nicht anders angegeben.

### `ItemRepositoryTests` (`ItemRepositoryTests.cs`) — **feature-relevant**
- `AddAsync_ThenGetByIdAsync_ReturnsItem` — Add/GetRoundtrip
- `GetAllAsync_ReturnsItemsOrderedByPublishedAtDescending` — Sortierung
- `UpdateAsync_PersistsChanges` — persistiert u. a. `IsSavedForLater = true`
- `DeleteAsync_RemovesItem` — Löschung ohne `IsSavedForLater`-Bezug
- `GetUnreadByDateAsync_ReturnsUnreadSortedByPublishedAtDescending` — Filter `!IsRead` + Sortierung
- `GetByFeedAsync_ReturnsOnlyMatchingFeed` — Feed-Filter
- `GetByCategoryAsync_ReturnsOnlyMatchingCategory` — Kategorie-Filter
- **`GetSavedForLaterAsync_ReturnsOnlySaved`** — nur `IsSavedForLater == true` kommen zurück (kein Sortierungs-Assert)
- `GetByCategoryAsync_NoFeedsInCategory_ReturnsEmpty` — Leermenge
- `GetByFeedAsync_NonExistingFeed_ReturnsEmpty` — Leermenge
- `GetUnreadByDateAsync_AllRead_ReturnsEmpty` — Leermenge
- `GetUnreadByDateAsync_Paged_ReturnsPage` — Paging
- `GetUnreadCountAsync_ReturnsCorrectCount` — Zähler
- `MarkAsReadAsync_SetsIsRead` — Gelesen-Markierung
- **`ToggleSavedForLaterAsync_TogglesState`** — Toggle `false → true` (kein Rück-Toggle, kein Nicht-Gefunden-Fall)

### `UnreadViewModelTests` (`UnreadViewModelTests.cs`) — **feature-relevant (UI-nahe Logik)**
- `LoadCommand_PopulatesArticlesAndCategories`
- `SelectCategoryCommand_FiltersArticles`
- `MarkReadCommand_RemovesArticle`
- `MarkAllReadCommand_ClearsArticles`
- `RefreshCommand_InvokesSyncService`
- *(kein Test für `ToggleSavedCommand`)*

### `FeedSyncServiceTests` (`FeedSyncServiceTests.cs`)
- `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk`
- `SyncFeedAsync_Duplicates_SkipsExistingItems`
- `SyncFeedAsync_Unreachable_KeepsItemsAndLogsError` — belegt: Items werden bei Fehlern **nicht** gelöscht
- `SyncFeedAsync_InvalidXml_SetsError` — belegt ebenfalls Nicht-Löschen
- `SyncFeedAsync_FewerItems_SetsWarning`
- `SyncFeedAsync_NoNewItemsForThirtyDays_SetsWarning`
- `SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth`

### `SettingsRepositoryTests` (`SettingsRepositoryTests.cs`)
- `GetAsync_ReturnsSeededSettings` — prüft u. a. `RetentionDays > 0`
- `GetAsync_CreatesDefaultRecordWhenMissing`
- `SaveAsync_OverwritesSameRecord` — inkl. `RetentionDays`-Änderung
- `SaveAsync_IgnoresDifferentIdAndUpdatesDefault`
- `GetAsync_AlwaysReturnsSingleRecord`

### `FeedsViewModelTests` (`FeedsViewModelTests.cs`)
- `RefreshCommand_InvokesSyncService_AndReloadsList`
- `RefreshAllCommand_InvokesSyncService_AndReloadsList`
- `RefreshCommand_WhenSyncReturnsError_SetsErrorMessage`
- *(kein Test für `DeleteCommand` / Feed-Cascade)*

### `CategoriesViewModelTests` (`CategoriesViewModelTests.cs`)
- `LoadCommand_PopulatesCategoriesWithFeedCounts`, `SaveCommand_*` (4×), `DeleteCommand_RemovesCategoryAndReloadsList`

### `FeedRepositoryTests` (`FeedRepositoryTests.cs`)
- `AddAsync_ThenGetByIdAsync_ReturnsFeed`, `GetAllAsync_ReturnsFeedsOrderedByTitle`, `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesFeed`, `GetByIdAsync_NonExisting_ReturnsNull`

### `CategoryRepositoryTests` (`CategoryRepositoryTests.cs`)
- `AddAsync_ThenGetByIdAsync_ReturnsCategory`, `GetAllAsync_ReturnsCategoriesOrderedByName`, `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesCategory`, `GetByIdAsync_NonExisting_ReturnsNull`, `DeleteAsync_NonExisting_DoesNotThrow`, `GetAllWithFeedCountAsync_ReturnsCountOfFeedsPerCategory`, `UpdateAsync_DuplicateName_ThrowsDbUpdateException`, `DeleteAsync_WithAssignedFeeds_SetsCategoryIdToNull` (SetNull-Verhalten)

### `KeywordRepositoryTests` (`KeywordRepositoryTests.cs`)
- `AddAsync_ThenGetByIdAsync_ReturnsKeyword`, `GetAllAsync_ReturnsKeywordsOrderedByText`, `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesKeyword`, `GetByIdAsync_NonExisting_ReturnsNull`

### `SyncLogRepositoryTests` (`SyncLogRepositoryTests.cs`)
- `AddAsync_ThenGetByIdAsync_ReturnsSyncLog`, `GetAllAsync_ReturnsSyncLogsOrderedByStartedAtDescending`, `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesSyncLog`, `GetByIdAsync_NonExisting_ReturnsNull`

### `ReporterDbContextTests_Persistence` / `ReporterDbContextTests_Schema` / `ReporterDbContextFactoryTests` / `ServiceCollectionTests`
- `SaveChangesAsync_PersistsFeedWithCategory`, `Items_WithInclude_ReturnsFeedAndCategory` (eigene In-Memory-Contexts), `EnsureCreatedAsync_CreatesQueryableTables`, `CreateDbContext_ReturnsContextWithSqliteProvider`, `AddReporterRepositories_ResolvesAllRepositories`

## Hilfsmethoden

### `TestDbContextFactory` (`TestDbContextFactory.cs`)
- `TestDbContextFactory()` — öffnet `SqliteConnection("DataSource=:memory:")` und erstellt Schema via `EnsureCreated`
- `CreateDbContext()` — neuer `ReporterDbContext` auf derselben In-Memory-Verbindung (für `IDbContextFactory<ReporterDbContext>`-basierte Repositories)
- `Dispose()` — schließt die Verbindung

### Seeding-Hilfen (privat, je Testklasse)
- `ItemRepositoryTests.SeedFeedAsync()` — legt `Entities.Feed` direkt über den Context an
- `UnreadViewModelTests.SeedFeedAndCategoryAsync()` — Kategorie + Feed mit `CategoryId`
- `FeedSyncServiceTests.SeedFeedAsync(url)` — Feed über `FeedRepository.AddAsync`

### Fakes
- `UnreadViewModelTests.FakeFeedSyncService` — `IFeedSyncService`-Stub, merkt `SyncAllCalled`, liefert `SyncResult(FeedHealth.Ok, 0)`
- `FeedSyncServiceTests.FakeHttpMessageHandler` — `HttpMessageHandler` mit Response-Factory für Feed-XML/Fehlerfälle
- `FeedSyncServiceTests.CreateService(content, statusCode)` / `CreateFailingService(exception)` — bauen `FeedSyncService` gegen Fake-`HttpClient`
- `FeedSyncServiceTests.RssXml(items)` — erzeugt RSS-2.0-Testdokumente
- `FeedsViewModelTests.FakeFeedSyncService` — `IFeedSyncService`-Fake mit `LastFeedId`, `SyncAllCalled` und setzbarem `NextResult` (Default `SyncResult(FeedHealth.Ok, 0)`)
- `FeedsViewModelTests.SeedFeedAsync(title, url)` — Feed über `FeedRepository.AddAsync`; `CreateViewModel()` baut `FeedsViewModel` gegen echte Repositories + Fake-Sync-Service
