<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-13, ca. 14:15–14:20 MESZ (UTC+02:00)
- **Branch und Commit-ID:** `task/issue-30-ee2cf2e629e44c2ba7e2c3d7d5e7a490-ui-polish-accessibility-und-de`, Commit `3f70a3a15303c195aa4e128e3db0a40149f7255d` („Manueller Sprachwechsel (EN/DE) in den Einstellungen (#76)")
- **Uncommittete Änderungen im getesteten Stand:** Keine Änderungen an tracked Dateien (`git status --porcelain` zeigt nur untracked `docs/features/task-issue-30-…/requirement.md` und `…/todo.md` — reine Feature-Dokumentation, kein Code). Die während der Bestandsaufnahme neu erzeugten `inventory/`-Dateien sind ebenfalls untracked und für den Code ohne Wirkung.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows (Git-Bash), .NET SDK `10.0.401`, Laufzeit .NET `10.0.12`, xUnit `2.9.3` mit `xunit.runner.visualstudio` `3.1.4`, `Microsoft.NET.Test.Sdk` `17.14.1`, `Microsoft.EntityFrameworkCore.Sqlite` `10.0.12` (In-Memory-SQLite via `TestDbContextFactory`), `Microsoft.Extensions.TimeProvider.Testing` `10.1.0`, `coverlet.collector` `6.0.4`.
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - Einzige automatisierte Testsuite: `src/Reporter.Tests/Reporter.Tests.csproj` (`net10.0`, xUnit) — Unit-/Integrationstests für `Reporter.Core` und `Reporter.Data`.
  - CI-Testbefehle: `.github/workflows/staging-ci.yml` und `pr-staging-ci.yml` (Job `build-and-test`): `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx"`; `.github/workflows/release.yml` (Release-Gate): `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release`.
  - `scripts/Run-StaticChecks.ps1` (Format/Lizenzheader/Security/Release-Build `TreatWarningsAsErrors`) ist eine statische Prüfung, keine Testsuite — wurde für die Bestandsaufnahme nicht ausgeführt.
  - Es gibt **keine automatisierten UI-Tests**; UI-Verifikation erfolgt manuell gemäß `AGENTS.md` (390 × 844 pt Windows-Fenster, Screenshots in `docs/help/anwendung/mobile-ui-design.md`/`test-results.md`; iOS über `scripts/iOS-Deployment.ps1`).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline (2026-09-13, MESZ) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --logger "trx;LogFileName=inventory-baseline.trx" --logger "console;verbosity=normal"` | Repo-Root `D:\Repositories\softwareschmiede\ee2cf2e6-29e4-4c2b-a7e2-c3d7d5e7a490` | 0 | 331 | 0 | 0 | [Console-Log](test-results/dotnet-test-baseline-console.log), [TRX-Report](test-results/inventory-baseline.trx) |

Hinweis: Der Baseline-Lauf baute die Projekte implizit (kein `--no-build`); die Coverage-Settings (`coverlet.runsettings`) wurden lokal nicht verwendet — das ändert nichts an der Testmenge/-auswertung, liefert aber keinen Coverage-Report.

### Nachgewiesene bestehende Testfehler

**Keine.** Im Baseline-Lauf sind alle 331 entdeckten Tests erfolgreich durchgelaufen (`Gesamtzahl Tests: 331, Bestanden: 331, Gesamtzeit ≈ 4 s`).

### Testlücken und Ausführungsprobleme

- Keine fehlgeschlagenen, übersprungenen oder abgebrochenen Tests; keine Build-/Setup-/Infrastrukturfehler im Baseline-Lauf.
- Keine automatisierte Abdeckung für die MAUI-UI-Schicht (`src/Reporter`): XAML-Views, Styles, `SemanticProperties`, Kontraste und App-Icon werden ausschließlich manuell verifiziert (s. `AGENTS.md`, „Mobile UI Design Review"). Für die in der Anforderung genannten Accessibility-/Design-Punkte existiert kein Testfundament.
- `LaterViewModel`: keine Paging-Tests (es gibt kein Paging — s. Testklassen unten). `FeedSyncService`: kein Test für Batch-Insert/In-Memory-Dedup (derzeit N+1-Pfad). `ItemRepository`: kein paged `GetSavedForLaterAsync` (Member existiert nicht).
- iOS-Verifikation (`scripts/iOS-Deployment.ps1`) erfordert macOS und wurde nicht ausgeführt.

## Testklassen

### `UnreadViewModelTests` (`src/Reporter.Tests/UnreadViewModelTests.cs`)

- `LoadCommand_PopulatesArticlesAndCategories` — lädt Artikel + Kategoriefilter inkl. „Alle"-Eintrag
- `SelectCategoryCommand_FiltersArticles` — Kategoriefilter-Auswahl filtert Liste
- `MarkReadCommand_RemovesArticle` — Artikel wird entfernt, Count aktualisiert
- `MarkAllReadCommand_ClearsArticles` — leert Liste und lädt neu
- `RefreshCommand_InvokesSyncService` — ruft `SyncAllAsync`
- `RefreshCommand_WhenOffline_SkipsSyncWithoutError` — kein Sync offline
- `RefreshCommand_WhenSyncThrows_SetsLocalizedSyncError` — `SyncStatusError`
- `RefreshCommand_WhenOffline_ResetsPresetIsSyncing` — `IsSyncing`-Reset
- `RefreshCommand_WhenIsSyncingPresetByBinding_StillSyncs`
- `RefreshCommand_WhenLoadFailsAfterSync_KeepsLoadErrorMessage` — nutzt `FailingItemRepository`
- `ConnectivityChanged_ClearsSyncErrorMessage` / `ConnectivityChanged_KeepsLoadErrorMessage` / `ConnectivityChanged_UpdatesIsOnline`
- `RefreshCommand_WhenBackOnline_InvokesSyncService`
- `ToggleSavedCommand_TogglesFlagInPlace` — In-Place-Update der Karte

### `LaterViewModelTests` (`src/Reporter.Tests/LaterViewModelTests.cs`)

- `LoadCommand_PopulatesOnlySavedItems` / `LoadCommand_OrdersByPublishedAtDescending`
- `ConnectivityChanged_UpdatesIsOnline`
- `ToggleSavedCommand_RemovesItemFromSavedItems` / `ToggleSavedCommand_NullItem_DoesNothing`
- `MarkReadCommand_SetsReadAndKeepsItemInList`

### `FeedSyncServiceTests` (`src/Reporter.Tests/FeedSyncServiceTests.cs`)

- `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk`
- `SyncFeedAsync_Duplicates_SkipsExistingItems` — Dedup über `GetByGuidOrHashAsync`-Pfad
- `SyncFeedAsync_Unreachable_KeepsItemsAndLogsError` / `SyncFeedAsync_InvalidXml_SetsError` — Fehlerisolation + SyncLog
- `SyncFeedAsync_FewerItems_SetsWarning` / `SyncFeedAsync_NoNewItemsForThirtyDays_SetsWarning` — `DetermineStatus`
- `SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth` — Fehlerisolation je Feed über Einzelsyncs
- `SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog` / `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange`
- `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems` / `…_SummaryMode_SendsSingleSummaryNotification` / `…_NoNewItems_DoesNotNotify` / `…_FeedDisabled_NoNotifications` / `…_NotificationThrows_SyncStillSucceeds`
- `SyncFeedAsync_WhenTitleIsPlaceholder/HostPlaceholder/FileNamePlaceholder_UpdatesTitleFromFeedDocument`, `…_WhenTitleIsSet_DoesNotOverwriteTitle`

### `ItemRepositoryTests` (`src/Reporter.Tests/ItemRepositoryTests.cs`)

- CRUD: `AddAsync_ThenGetByIdAsync_ReturnsItem`, `GetAllAsync_ReturnsItemsOrderedByPublishedAtDescending`, `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesItem`
- Ungelesen/Paging: `GetUnreadByDateAsync_ReturnsUnreadSortedByPublishedAtDescending`, `GetUnreadByDateAsync_AllRead_ReturnsEmpty`, `GetUnreadByDateAsync_Paged_ReturnsPage`, `GetUnreadCountAsync_ReturnsCorrectCount`
- Filter: `GetByFeedAsync_ReturnsOnlyMatchingFeed`/`…_NonExistingFeed_ReturnsEmpty`, `GetByCategoryAsync_ReturnsOnlyMatchingCategory`/`…_NoFeedsInCategory_ReturnsEmpty`
- Saved: `GetSavedForLaterAsync_ReturnsOnlySaved`, `GetSavedForLaterAsync_OrdersByPublishedAtDescending`, `ToggleSavedForLaterAsync_TogglesState`/`…_TogglesBackToFalse`, `MarkAsReadAsync_SetsIsRead`
- Retention: `DeleteExpiredAsync_*` (5 Fälle), `GetExpiredKeywordCandidatesAsync_*` (3 Fälle), `DeleteRangeAsync_*` (2 Fälle)

### `FeedsViewModelTests` (`src/Reporter.Tests/FeedsViewModelTests.cs`)

Umfangreiche Suite (~55 Fälle): Sync-/Refresh-Pfade inkl. lokalisierter `SyncErrorMessage` und Offline-Verhalten, Add/Edit/Delete/Save, `RenameFeedAsync`/`ChangeFeedCategoryAsync`-Nullfälle, Feed-Suche (`SearchCommand`-Varianten inkl. Service-Unavailable, Stale-Results, Confirm-Direct-Add-Dialog), `SubscribeResultCommand`, `OpenAddFormCommand`.

### `CategoriesViewModelTests` (`src/Reporter.Tests/CategoriesViewModelTests.cs`)

- `LoadCommand_PopulatesCategoriesWithFeedCounts`, `SaveCommand_WithEmptyName/WithDuplicateName/AddsNewCategory/UpdatesExistingCategory`, `DeleteCommand_RemovesCategoryAndReloadsList`

### `AutoRefreshServiceTests` (`src/Reporter.Tests/AutoRefreshServiceTests.cs`)

- `StartAsync_InvokesSyncAfterInterval`, `ApplySettings_Disabled_Stops`, `ApplySettings_ChangesInterval`, `SyncThrows_LoopContinues`, `CoalescedTicks_DoNotStartParallelSync`, `Tick_WhenOffline_SkipsSyncAll`, `Tick_WhenBackOnline_ResumesSync`, `InvalidInterval_Clamped` (Theory), `StartStop_RepeatedCycles_RestartsCleanly`, `ApplySettings_Concurrent_LeavesSingleActiveLoop`

### Weitere vorhandene Testklassen (nicht primär betroffen)

`AppCultureTests`, `ArticleHtmlSanitizerTests`, `BaseViewModelConnectivityTests`, `CategoryRepositoryTests`, `FeedRepositoryTests`, `FeedSearchServiceTests`, `FeedTitleFallbackTests`, `KeywordMatcherTests`, `KeywordRepositoryTests`, `NotificationServiceTests`, `ReporterDbContextFactoryTests`, `ReporterDbContextTests_Persistence`, `ReporterDbContextTests_Schema`, `RetentionCleanupServiceTests`, `ServiceCollectionTests`, `SettingsRepositoryTests`, `SettingsValuesTests_AutoMarkRead`, `SettingsViewModelTests_E2E`/`_Keywords`/`_Load`/`_Persist`, `SyncLogRepositoryTests`, `WebViewNavigationGuardTests`.

## Hilfsmethoden

### `TestDbContextFactory` (`src/Reporter.Tests/TestDbContextFactory.cs`)
- `CreateDbContext` — In-Memory-SQLite (`Mode=Memory;Cache=Shared`, Keep-Alive-Connection, `EnsureCreated`) als `IDbContextFactory<ReporterDbContext>`; `IDisposable`.

### `TestDataSeeder` (`src/Reporter.Tests/TestDataSeeder.cs`)
- `SeedFeedAsync(factory)` — legt einen Beispiel-Feed an, gibt `feedId` zurück.

### `TestSettingsHelper` (`src/Reporter.Tests/TestSettingsHelper.cs`)
- `SaveAsync(repository, notificationsEnabled, notificationSummaryEnabled, quietHoursStart, quietHoursEnd, language)` — Settings-Update mit Overrides.

### `TestWaitHelper` (`src/Reporter.Tests/TestWaitHelper.cs`)
- `WaitUntilAsync(condition, timeoutMilliseconds = 5000)` — Polling für Fire-and-forget-VM-Persistenz.

### Fakes (`src/Reporter.Tests/`)
- `FakeFeedSyncService` — `SyncAllCallCount`, `SyncAllException`, `SyncAllBlocker` (`TaskCompletionSource`)
- `FakeNetworkStatusService` — `ConnectivityChanged` steuerbar
- `FakeAppThemeService`, `FakeAutoRefreshService`, `FakeFeedSearchService`, `FakeLocalNotificationService`, `FakeNotificationService`
- `FailingItemRepository` (nested in `UnreadViewModelTests`) — delegiert an `ItemRepository`, lässt `GetUnreadByDateAsync(paged)` fehlschlagen
