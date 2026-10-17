<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-16, ca. 21:44–21:55 Uhr +0200 (CEST)
- Branch und Commit-ID: `task/issue-128-0a6a02d653d94dd6a3c66d5143526e5e-unterstuetzung-von-tags` @ `25271bef9b8e2ed51d865dcf3986fe887817945e` („feat: Editorial-Redesign von Startseite und Später-Seite (#157)")
- Uncommittete Änderungen im getesteten Stand: nur untracked `docs/features/task/issue-128-…-unterstuetzung-von-tags/` (requirement.md und die hier entstehende Inventur) — **keine** Änderungen an `src/`; der getestete Produktivcode entspricht exakt dem genannten Commit
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK `10.0.402`; installierte Workloads `maui-windows 10.0.20`, `android 36.1.69`, `ios 26.5.10318`, `maccatalyst 26.5.10318`; Umgebungsvariable `IncludeIosTarget=false` (wie CI)
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `src/Reporter.Tests` (xUnit, `net10.0`, EF-Core-SQLite-In-Memory) — CI-Befehl aus `.github/workflows/staging-ci.yml`/`pr-staging-ci.yml` („build & test"): `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build` (CI zusätzlich mit `--settings coverlet.runsettings`, Coverage-Collection und TRX-Logger)
  - `src/Reporter.E2ETests` (xUnit + FlaUI.UIA3, `net10.0-windows10.0.19041.0`) — nur via `scripts/Run-E2ETests.ps1` dokumentiert; **nicht** Teil der CI-Workflows
  - Voll-Build: `dotnet restore Reporter.sln -r win-x64`, `dotnet build Reporter.sln --configuration Release --no-restore` (CI-Job „build & test")

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Restore | `dotnet restore Reporter.sln -r win-x64` (mit `IncludeIosTarget=false`) | Repo-Root | 0 | — | — | — | [restore.log](test-results/restore.log) |
| Build | `dotnet build Reporter.sln --configuration Release --no-restore` (mit `IncludeIosTarget=false`) | Repo-Root | 0 (0 Warnungen, 0 Fehler) | — | — | — | [build-solution.log](test-results/build-solution.log) |
| Unit-Tests | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=test-results-baseline.trx" --logger "console;verbosity=normal"` | Repo-Root (Logs in `inventory/test-results/` geschrieben) | 0 | 731 | 0 | 0 | [unit-tests-baseline.log](test-results/unit-tests-baseline.log), [test-results-baseline.trx](test-results/test-results-baseline.trx) |

### Nachgewiesene bestehende Testfehler

Keine. Der Baseline-Lauf meldet 731 von 731 ausgeführten Tests als `Passed` (TRX: `total="731" executed="731" passed="731" failed="0"`, kein `notExecuted`/`notRunnable`).

### Testlücken und Ausführungsprobleme

- **`src/Reporter.E2ETests` nicht ausgeführt.** Grund: Die FlaUI-UIA3-Suite benötigt eine interaktive Desktop-Session mit sichtbarem Fenster (`ReporterAppFixture` startet `Reporter.exe` und attached per UIA3) sowie einen vorherigen Debug-Build der App (`scripts/Run-E2ETests.ps1`, `REPORTER_APP_PATH`). Die Suite ist nicht Teil der CI-Workflows (kein Workflow ruft sie auf). Umfang: alle E2E-Tests in `ArticleImageTests`, `ArticleLinkTests`, `DemoSeedTests`, `FeedDetailTests`, `SmokeTests` (≈ 23 Tests). Für die Anforderung relevant: `FeedDetailTests.FeedDetail_Search_FiltersItems` (Zeile 261) testet die Artikelsuche; ein Tag-Treffer-E2E-Test ist laut Anforderung zu ergänzen und müsste in dieser Umgebung verifiziert oder als Testlücke offengelegt werden.
- **Keine Coverage-Collection im Baseline-Lauf** (`--settings coverlet.runsettings`/`--collect` weggelassen; nur Konsolen- und TRX-Logger). Kein Einfluss auf Pass/Fail.
- Ansonsten keine Ausführungsprobleme: Restore, Solution-Build (inkl. MAUI-Projekt `Reporter`, `win-x64`) und Unit-Tests liefen durch.

## Testklassen (für die Anforderung relevant)

### `FeedSyncServiceTests`
Datei: `src/Reporter.Tests/FeedSyncServiceTests.cs` (Integrationstests mit echtem `ItemRepository`/`FeedRepository` auf `TestDbContextFactory` + `FakeHttpMessageHandler`; Feed-XML via `TestFeedXml`)

- `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk` — Baseline-Sync erzeugt Items
- `SyncFeedAsync_Duplicates_SkipsExistingItems` / `SyncFeedAsync_DuplicatesWithinSameDocument_InsertsOnce` / `SyncFeedAsync_MixedNewAndExisting_BatchInsertsOnlyNew` — `GuidOrHash`-Dedup (relevant für die Backfill-Frage)
- `SyncFeedAsync_KeywordTitleMatch_NotSaved` / `…_KeywordContentHtmlMatch_NotSaved` / `…_KeywordNoMatch_SavesNormally` / `…_AtomFeed_NotSaved` u. a. — Stichwort-Filter im `CollectNewItems`-Pfad
- `SyncFeedAsync_Atom03_CreatesItems_AndSetsHealthOk` / `…_MapsIssuedToPublishedAt` — Atom-0.3-Pfad
- Weitere: Health-/Warning-/LastMessage-Tests, Favicon-Backfill, Notification-Tests, Concurrency (`FeedSyncServiceTests_Concurrency`), DebugLog (`FeedSyncServiceTests_DebugLog`), Titel-Normalisierung (`FeedSyncServiceTests_TitleNormalization`)
- Kein Test liest bisher `ElementExtensions`/`app:tags` — Tag-Tests sind neu anzulegen (Atom-Builder `TestFeedXml.Atom` hat derzeit keine Erweiterungselement-Unterstützung)

### `ItemRepositoryTests`
Datei: `src/Reporter.Tests/ItemRepositoryTests.cs`

- `GetByFeedAsync_Paged_FiltersBySearchTerm` (Zeile 1449) — **bestehender Suchtest**: filtert per `searchTerm` nur über den Titel; Leer-String = kein Filter
- `AddAsync_ThenGetByIdAsync_ReturnsItem`, `AddRangeAsync_InsertsAllItems`, `UpdateAsync_PersistsChanges` — Persistenz-Roundtrips, an die ein `Tags`-Feld anschließen würde
- `GetByFeedAsync_ReturnsOnlyMatchingFeed`, `GetByFeedAsync_NonExistingFeed_ReturnsEmpty` — Feed-Filter
- Paging-/Projektions-, Saved-, Delete- und Keyword-Kandidaten-Tests

### `FeedDetailViewModelTests`
Datei: `src/Reporter.Tests/FeedDetailViewModelTests.cs` (echtes `ItemRepository` auf In-Memory-SQLite, `FakeFeedSyncService`, `TestWaitHelper` für Debounce-Warten)

- `SearchText_Changed_ResetsPagingAndFilters` (Zeile 269) — Suche setzt Paging zurück und filtert (derzeit implizit Titel-only)
- `SearchText_RapidChanges_DebouncesListRestart` (Zeile 301) — 300-ms-Debounce, genau ein Repository-Aufruf
- `LoadAsync_PopulatesFeedAndItems`, `LoadMoreCommand_AppendsNextPage` u. a. — Listenladen

### `ReporterDbContextTests_Schema` / `ReporterDbContextTests_Persistence`
Dateien: `src/Reporter.Tests/ReporterDbContextTests_Schema.cs`, `src/Reporter.Tests/ReporterDbContextTests_Persistence.cs`

- `EnsureCreatedAsync_CreatesQueryableTables` — alle DbSets abfragbar
- `DebugLogEntries_MappedToExpectedTable`, `Feed_LastMessage_MappedToExpectedColumns` — Muster für Spalten-Mapping-Assertions über `context.Model.FindEntityType` (Referenz für einen `tags`-Spalten-Test)
- `Items_WithInclude_ReturnsFeedAndCategory`, `Feed_PersistRoundtrip_LastMessage` — Persistenz-Roundtrips

### `ArticleHtmlSanitizerTests`
Datei: `src/Reporter.Tests/ArticleHtmlSanitizerTests.cs`

- Sanitizer-Pipeline-Tests (`Sanitize_RemovesDangerousElementsIncludingClosingTags` u. a.) — Tag-Block-Anforderung betrifft aber `ArticleDetailViewModel.RebuildHtml`, nicht den Sanitizer selbst

### `ItemImageServiceTests`
Datei: `src/Reporter.Tests/ItemImageServiceTests.cs`

- `ResolveImageUrl_MediaRss_Used`, `ResolveImageUrl_MediaRssContent_Used` u. a. — **Referenz für ElementExtensions-Tests**: Hilfsmethode `ParseItem(string feedXml)` (Zeile 28) parst ein Inline-Feed-XML zu `SyndicationItem`; MediaRSS-XML mit Namespace-Deklaration wird inline im Test aufgebaut

### `WebViewNavigationGuardTests`, `BaseViewModelConnectivityTests`
Decken Teilbereiche der Detailseite ab; für Tags nicht direkt relevant.

### E2E: `FeedDetailTests`
Datei: `src/Reporter.E2ETests/FeedDetailTests.cs`

- `FeedDetail_Search_FiltersItems` (Zeile 261) — Suchfeld `FeedDetailSearchBar` filtert die Artikelliste (derzeit Titel-only-Verhalten)
- `FeedDetail_Navigation_ShowsFeedItems`, `FeedDetail_InfiniteScroll_LoadsNextPage` u. a. — Listenverhalten
- Kein E2E-Test zur Artikeldetail-Tag-Anzeige vorhanden

## Hilfsmethoden / Test-Doubles

### `TestFeedXml`
Datei: `src/Reporter.Tests/TestFeedXml.cs`

- `Rss(...)` — RSS-2.0-Builder (Titel/Link/Guid/PubDate/Description)
- `RssWithEnclosure(...)` — RSS mit `<enclosure>`
- `Atom(...)` — Atom-1.0-Builder (`<feed xmlns="http://www.w3.org/2005/Atom">`), keine Erweiterungselement-Unterstützung
- `Atom03(...)` — Atom-0.3-Builder inkl. `xmlns:dc`

### `TestDbContextFactory` / `TestContentDbContextFactory` / `TestDbContextFactoryBase`
Dateien: `src/Reporter.Tests/TestDbContextFactory*.cs`

- `TestDbContextFactory` — `IDbContextFactory<ReporterDbContext>` auf In-Memory-SQLite (`EnsureCreated`, Datenbankname `reporter-tests`)
- `TestContentDbContextFactory` — analog für `ContentDbContext`

### `TestDataSeeder`
Datei: `src/Reporter.Tests/TestDataSeeder.cs`

- `SeedFeedAsync(...)` — legt Test-Feeds an (u. a. in `ItemRepositoryTests`, `FeedSyncServiceTests` genutzt)

### `FakeItemContentStore`
Datei: `src/Reporter.Tests/FakeItemContentStore.cs`

- In-Memory-Implementierung von `IItemContentStore` für Repository-/Sync-Tests

### `DelegatingItemRepository`
Datei: `src/Reporter.Tests/DelegatingItemRepository.cs`

- `virtual`-Decorator über `IItemRepository`; Basis für Test-Doubles wie `CountingItemRepository` (in `FeedDetailViewModelTests`) — bei Signaturänderungen betroffen, bei reiner Property-Erweiterung nicht

### Weitere Fakes
`FakeHttpMessageHandler`, `FakeNotificationService`, `FakeNetworkStatusService`, `FakeFeedSyncService`, `FakeFeedIconService`, `FakeItemImageService`, `FakeLocalNotificationService`, `FakeKeywordMatcher`-Nutzung via `KeywordFilter`, `TestWaitHelper` (`WaitUntilAsync`), `TestHttpResponses`.
