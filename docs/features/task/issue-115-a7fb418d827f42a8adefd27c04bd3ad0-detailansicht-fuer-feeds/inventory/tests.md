<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Bestandsaufnahme und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-19, ca. 16:27–16:45 +02:00 (Europe/Berlin, CEST)
- Branch und Commit-ID: `task/issue-115-a7fb418d827f42a8adefd27c04bd3ad0-detailansicht-fuer-feeds` @ `875d265532162393301cb7e2ece86c45a793b202`
- Uncommittete Änderungen im getesteten Stand: keine Änderungen an getrackten Dateien; einzige untracked-Dateien sind die Feature-Dokumente unter `docs/features/task/issue-115-a7fb418d827f42a8adefd27c04bd3ad0-detailansicht-fuer-feeds/` (`requirement.md` und diese Inventory-Ausgaben). Kein Produktivcode, keine Tests und keine Testkonfiguration wurden vor dem Lauf verändert.
- Testumgebung und Runtime-/SDK-Versionen: Windows 11 (win-x64), .NET SDK 10.0.401, .NET Runtime 10.0.12, installierte Workloads `maui-windows` 10.0.20, `android`, `ios`, `maccatalyst` (Manifest 10.0.100); Node.js v24.15.0; interaktive Desktop-Session (Voraussetzung der FlaUI-E2E-Tests).
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - **Unit-/Integrationstests** `src/Reporter.Tests` (xUnit, `net10.0`): CI-Job `build-and-test` in `.github/workflows/staging-ci.yml` (Zeilen 120–126) führt `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"` aus.
  - **E2E-Tests** `src/Reporter.E2ETests` (xUnit + FlaUI.UIA3, `net10.0-windows10.0.19041.0`): `scripts/Run-E2ETests.ps1` — baut `src/Reporter` (Debug, win-x64) und führt `dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0` mit `REPORTER_APP_PATH` aus.
  - **Release-Skript-Tests** `scripts/*.test.mjs` (node:test): `package.json` → `npm test` = `node --test "scripts/*.test.mjs"`.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 – Unit/Integration | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --logger "trx;LogFileName=inventory-baseline-unittests.trx" --logger "console;verbosity=normal"` | Repository-Root | 0 | 622 | 0 | 0 | [Log](test-results/unittests-console.log), [TRX](test-results/inventory-baseline-unittests.trx) |
| 2 – Node-Skripte | `npm test` (`node --test "scripts/*.test.mjs"`) | Repository-Root | 0 | 36 | 0 | 0 | [Log](test-results/nodetests-console.log) |
| 3 – E2E (Ausgangslauf) | `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Run-E2ETests.ps1` (Build Debug/win-x64 + `dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0`) | Repository-Root | unbekannt (Skript lief über `tee`-Pipeline; `dotnet test` meldete „Fehler!") | 17 | 2 | 0 | [Log](test-results/e2etests-console.log) |
| 4 – E2E (Wiederholung, gefiltert) | `dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0 --filter "FullyQualifiedName~ArticleImageTests" --logger "trx;LogFileName=inventory-baseline-e2e-rerun.trx"` (Env: `REPORTER_APP_PATH=<Reporter.exe>`, `IncludeIosTarget=false`) | Repository-Root | 0 | 2 | 0 | 0 | [Log](test-results/e2etests-rerun-articleimage-console.log), [TRX](test-results/inventory-baseline-e2e-rerun.trx) |

### Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|------------------------|-------------------|----------------------------|-------------------|
| `Reporter.E2ETests.ArticleImageTests.ArticleImage_StoredLocally_AndShownOnCardAndDetail` | `src/Reporter.E2ETests/ArticleImageTests.cs:52` | `No feed row with URL 'http://127.0.0.1:65128/feeds/image-feed.xml' found in <Temp>\reporter-e2e-…\reporter.db — the direct add did not persist the feed.` — das UI-gesteuerte Direkt-Hinzufügen persistierte den Feed nicht rechtzeitig. In Lauf 4 (isolierter Wiederholungslauf) **bestanden** → instabiler/flaky Fehlschlag, kein dauerhafter Defekt nachgewiesen | Lauf 3, [Log](test-results/e2etests-console.log); Wiederholung [TRX](test-results/inventory-baseline-e2e-rerun.trx) |
| `Reporter.E2ETests.ArticleImageTests.BrokenImage_DoesNotFailSync_StoresNoImage` | `src/Reporter.E2ETests/ArticleImageTests.cs:138` (via `ReporterAppFixture.cs:94`, `E2EPageHelpers.cs:55`) | `System.InvalidOperationException : The Reporter.exe process is not running anymore.` — Folgefehler: der App-Prozess war nach dem vorherigen Fehlschlag nicht mehr verfügbar. In Lauf 4 ebenfalls **bestanden** → Folge-/Kaskadenfehler, instabil | Lauf 3, [Log](test-results/e2etests-console.log); Wiederholung [TRX](test-results/inventory-baseline-e2e-rerun.trx) |

Unit- und Node-Suiten zeigen keine Fehlschläge. Die beiden E2E-Fehler aus Lauf 3 gelten als nachgewiesen vorhanden, sind aber durch Lauf 4 als nicht persistent (flaky) eingestuft — sie verdecken keinen dauerhaften Defekt, belegen aber eine instabile `ArticleImageTests`-Sequenz.

### Testlücken und Ausführungsprobleme

- **Abweichung vom CI-Befehl (Lauf 1):** Ohne `--settings src/Reporter.Tests/coverlet.runsettings`/`--collect:"XPlat Code Coverage"` und mit Build statt `--no-build` ausgeführt — Ergebnis deckt die Testfälle ab, liefert aber keinen Coverage-Report.
- **Kein TRX für Lauf 3:** `Run-E2ETests.ps1` definiert keinen TRX-Logger; als Nachweis liegt nur das Konsolen-Log vor.
- **Exit-Code Lauf 3 unbekannt:** Das Skript lief in einer `tee`-Pipeline; die Zusammenfassung „Fehler: 2, erfolgreich: 17" ist dokumentiert, der numerische Exit-Code von `dotnet test` wurde nicht separat erfasst.
- **E2E-Voraussetzungen:** Die Suite benötigt Windows + MAUI-Workload + interaktive Desktop-Session (UIA3 sichtbares Fenster) sowie eine gebaute `Reporter.exe` — auf anderen Umgebungen (z. B. Linux-CI) nicht ausführbar. iOS-/Android-spezifische Tests existieren nicht.
- **Bereichs-Lücke:** Es existieren keine Tests für eine `FeedDetailPage`/`FeedDetailViewModel`/`feeddetail`-Route — das Feature ist noch nicht implementiert. Die bestehenden `SmokeTests.FeedActionSheet_*`-E2E-Tests prüfen genau das Aktionsblatt, das durch die Navigation ersetzt werden soll, und werden von der Umsetzung betroffen sein.

## Testklassen

### `FeedsViewModelTests` (`src/Reporter.Tests/FeedsViewModelTests.cs`, ~80 Tests)
Integrationstests gegen echte `FeedRepository`/`CategoryRepository` auf `TestDbContextFactory` (SQLite) plus Fakes (`FakeFeedSyncService`, `FakeFeedSearchService`, `FakeFeedIconService`, `FakeNetworkStatusService`, `FakeItemContentStore`). `CreateViewModel()` erzeugt das VM ohne `ILocalNotificationService`. Relevante Gruppen:

- **Refresh/Sync:** `RefreshCommand_InvokesSyncService_AndReloadsList`, `RefreshAllCommand_InvokesSyncService_AndReloadsList`, `RefreshCommand_WhenSyncReturnsError_SetsLocalizedSyncErrorMessage`, `RefreshAllCommand_WhenSyncThrows_SetsLocalizedSyncError`, `RefreshCommand_WhenOffline_SkipsSyncWithoutError`, `RefreshAllCommand_WhenOffline_SkipsSyncWithoutError`, `RefreshCommand_WhenIsSyncingPresetByBinding_StillSyncs`, `RefreshAllCommand_WhenIsSyncingPresetByBinding_StillSyncs`, `RefreshCommand_WhenOffline_ResetsPresetIsSyncing`, `RefreshAllCommand_WhenOffline_ResetsPresetIsSyncing`, `ConnectivityChanged_ClearsSyncErrorMessage`, `ConnectivityChanged_UpdatesIsOnline`, `ConnectivityChanged_UpdatesSearchCommandCanExecute`
- **Edit/Save/Delete:** `EditCommand_PopulatesFeedNotificationsEnabled`, `EditAsync_OpensSheetInEditMode`, `SaveCommand_ExistingFeed_PersistsNotificationsEnabled`, `SaveCommand_ResetsFeedNotificationsEnabled`, `SaveCommand_WithoutSelectedFeed_DoesNotAddFeed`, `SaveCommand_EditMode_WhenInvalidUrl_KeepsSheetOpenAndSetsError`, `SaveCommand_EditMode_WhenDuplicate_KeepsSheetOpenAndSetsError`, `SaveCommand_EditMode_PreservesCategoryId`, `DeleteCommand_ResetsFeedNotificationsEnabled`
- **Rename/Kategorie/Fehlerdetails (die zu verlagernden Aktionen):** `RenameFeedAsync_UpdatesTitle`, `RenameFeedAsync_EmptyTitle_SetsErrorAndKeepsTitle`, `RenameFeedAsync_NullFeed_DoesNothing`, `RenameFeedAsync_PreservesFaviconUrl`, `RenameFeedAsync_PreservesLastError`, `ChangeFeedCategoryAsync_SetsCategoryId`, `ChangeFeedCategoryAsync_EmptyGuid_ClearsCategory`, `ChangeFeedCategoryAsync_NullArguments_DoNothing`, `GetFeedErrorMessage_MapsKindToLocalizedText`, `GetFeedErrorMessage_FallsBackToUnknown`, `GetFeedErrorMessage_AppendsTechnicalMessage`, `MakeUniqueOptionLabels_*` (3 Tests)
- **Add-/Such-Flows:** `SearchCommand_*` (20+ Tests), `DirectAddCommand_*` (8 Tests), `SubscribeResultCommand_*` (3 Tests), `OpenAddFormCommand_*`/`CloseAddFormCommand_*` (4 Tests), `NewUrl_Changed_ClearsSearchState`

### `ItemRepositoryTests` (`src/Reporter.Tests/ItemRepositoryTests.cs`)
Echte `ItemRepository` auf `TestDbContextFactory` + `FakeItemContentStore`. Relevant:
- `GetByFeedAsync_ReturnsOnlyMatchingFeed`, `GetByFeedAsync_NonExistingFeed_ReturnsEmpty`, `GetByFeedAsync_HydratesContentFromStore` — die bestehende ungepagede Feed-Abfrage
- `GetSavedForLaterAsync_ReturnsOnlySaved`, `GetSavedForLaterAsync_Paged_ReturnsPage`, `GetSavedForLaterAsync_OrdersByPublishedAtDescending`, `GetSavedForLaterAsync_ProjectsReadingTimeText`, `GetSavedForLaterAsync_ProjectsLocalImageLoader` — Paged-/Projektionsmuster
- `GetUnreadByDateAsync_Paged_*`, `GetUnreadByDateAsync_ProjectsFeedFaviconUrl`, `GetUnreadByDateAsync_Ascending_*`, `GetUnreadCountAsync_ReturnsCorrectCount`, `MarkAsReadAsync_SetsIsRead`, `ToggleSavedForLaterAsync_*`, `MarkAllAsRead`-Nebenpfade, Content-Store-Tests (`AddAsync_ThenGetByIdAsync_ReturnsContentHtml`, `DeleteAsync_RemovesStoredContent`, …)

### `FeedRepositoryTests` (`src/Reporter.Tests/FeedRepositoryTests.cs`)
- `AddAsync_ThenGetByIdAsync_ReturnsFeed`, `GetByIdAsync_NonExisting_ReturnsNull`, `GetAllAsync_ReturnsFeedsOrderedByTitle`, `UpdateAsync_PersistsChanges`, `UpdateAsync_PersistsFaviconUrl`, `UpdateAsync_PersistsLastError`, `GetAllWithDetailsAsync_ProjectsNotificationsEnabled`, `GetAllWithDetailsAsync_ProjectsFaviconUrl`, `GetAllWithDetailsAsync_ProjectsLastError`, `DeleteAsync_RemovesFeed`, `DeleteAsync_CascadeDeletesSavedItems`, `DeleteAsync_RemovesItemContents`, `DeleteAsync_RemovesItemImages`

### `LaterViewModelTests` (`src/Reporter.Tests/LaterViewModelTests.cs`)
Paging-Referenztests auf echtem `ItemRepository`: `LoadCommand_PopulatesOnlySavedItems`, `LoadCommand_OrdersByPublishedAtDescending`, `LoadMoreCommand_AppendsNextPage`, `LoadMoreCommand_WhenNoMoreItems_DoesNothing`, `LoadCommand_WhenLoadMoreInFlight_ReloadsFirstPage` (über `BlockingItemRepository : DelegatingItemRepository` mit `GateNextPageRequest`/`ReleaseGate`), `LoadCommand_WhenRepositoryFails_SetsLocalizedErrorMessage`, `MarkReadCommand_*`, `ToggleSavedCommand_*`, `ConnectivityChanged_UpdatesIsOnline`

### `FeedListItemTests` / `ItemListItemTests`
- `FeedInitial_FeedWithFavicon_StillProvidesFallbackLetter`, `FeedInitial_BothListItems_DeriveSameLetter`, `FeedInitial_EmptyTitle_FallsBackToQuestionMark`
- `CopyWith_PreservesLocalImageLoader`, `HasLocalImage_ReflectsLocalImageLoader`

### E2E-Testklassen (`src/Reporter.E2ETests/`)
- `SmokeTests`: `AppStarts_FeedListRenders`, `AddButton_OpensSheet_FocusesUrlEntry`, `DirectAdd_FeedAppearsInListAndDatabase`, **`FeedActionSheet_Rename_UpdatesTitle`**, **`FeedActionSheet_ChangeCategory_IncludingNone`** (direkt vom Ersatz des Aktionsblatts betroffen), `Search_SubscribesResult_PersistsFeed`, `Search_SiteUrl_DiscoversFeedViaLinkTag`
- `ArticleImageTests`: `ArticleImage_StoredLocally_AndShownOnCardAndDetail`, `BrokenImage_DoesNotFailSync_StoresNoImage` (beide im Ausgangslauf flaky fehlgeschlagen)
- `ArticleLinkTests`: `ExternalLinkInArticle_OpensSystemBrowser`
- `DemoSeedTests`: `FirstStart_SeedsNewsCategoryAndDemoFeed`
- `E2EProcessGuardTests`: Prozess-/Cleanup-Tests der E2E-Infrastruktur

## Hilfsmethoden

### `TestDbContextFactory` / `TestDbContextFactoryBase` / `TestContentDbContextFactory`
- `CreateDbContext()` / `CreateDbContextAsync()` — SQLite-In-Memory-/Temp-DBs pro Test (`IDbContextFactory<ReporterDbContext>`/`ContentDbContext`); `Dispose` räumt Dateien auf.

### `TestDataSeeder`
- `SeedFeedAsync(TestDbContextFactory)` — Feed-Entity direkt in den Kontext; `SeedFeedAsync(IFeedRepository, url, notificationsEnabled)` — Feed über das Repository.

### `DelegatingItemRepository`
- `IItemRepository`-Dekorierer, der alle Member virtuell an ein inneres Repository delegiert — Basisklasse für Test-Doubles (z. B. `BlockingItemRepository` in `LaterViewModelTests`). Eine neue Interface-Methode (paged `GetByFeedAsync`) muss hier nachgezogen werden.

### Fakes (handgeschrieben)
- `FakeFeedSyncService` — zählt `SyncAllAsync`-Aufrufe, konfigurierbare Exception/`TaskCompletionSource`-Blockade; `SyncFeedAsync` liefert festes `SyncResult("OK", 0)` (kein Aufrufzähler).
- `FakeNetworkStatusService` — setzbares `IsOnline`, `RaiseConnectivityChanged()`.
- `FakeFeedSearchService`, `FakeFeedIconService`, `FakeItemContentStore`, `FakeLocalNotificationService`, weitere Fakes (`FakeAppThemeService`, `FakeAutoRefreshService`, `FakeBackgroundRefreshService`, …).

### Weitere Unit-Test-Helfer
- `TestWaitHelper`, `TestFeedXml`, `TestHttpResponses`, `FakeHttpMessageHandler`, `TestSettingsHelper`

### E2E-Infrastruktur
- `ReporterAppFixture` (App-Start, `GetMainWindow`), `E2ETestCollection` (xUnit-Collection), `E2EPageHelpers` (`SelectTab`, `Window`), `UiRetry`, `E2EProcessGuard`, `StubFeedServer` (lokaler HTTP-Feed-Stub), `FeedDbAssertions` (DB-Assertions auf `reporter.db` im Temp-Dir via `REPORTER_DB_PATH`)
