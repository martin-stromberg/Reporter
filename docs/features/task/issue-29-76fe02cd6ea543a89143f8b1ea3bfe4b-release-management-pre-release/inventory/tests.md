# Detail: Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-12, Testlauf ca. 16:19–16:26 Uhr +02:00 (`W. Europe Standard Time`, Sommerzeit/CEST)
- **Branch und Commit-ID:** `task/issue-29-76fe02cd6ea543a89143f8b1ea3bfe4b-release-management-pre-release` @ `226c974a9b52c67050fc7f57637152074d26a28e` („Offline-Fähigkeit und Mehrsprachigkeit (EN/DE) (#63)") — identisch mit `origin/staging`
- **Uncommittete Änderungen im getesteten Stand:** keine Änderungen an tracked Files; untracked ist `docs/features/` (die Anforderungs-/Inventory-Dokumente dieses Issues). Der Testlauf erzeugte zusätzlich `src/Reporter.Tests/TestResults/test-results-baseline.trx` (Build-/Test-Artefakt). Produktivcode und Testcode sind unverändert.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows (lokale Entwicklermaschine), .NET SDK `10.0.401` (einzige installierte SDK-Version), .NET Runtime `10.0.12`, xUnit VSTest Adapter `3.1.4`, Node.js `v24.15.0`. Installierte Workloads: `android 36.1.69`, `ios 26.5.10318`, `maccatalyst 26.5.10318`, `maui-windows 10.0.20` (SDK 10.0.400).
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - Einzige Testsuite der Solution: `src/Reporter.Tests/Reporter.Tests.csproj` (`net10.0`, xUnit 2.9.3, `Microsoft.NET.Test.Sdk` 17.14.1, `coverlet.collector` 6.0.4, EF Core Sqlite, `Microsoft.Extensions.TimeProvider.Testing`). Referenziert nur `Reporter.Core` und `Reporter.Data` — kein MAUI nötig.
  - Befehlsquellen: `README.md` (Z. 148: `dotnet test Reporter.sln`), CI-Workflows `staging-ci.yml`/`pr-staging-ci.yml` (`dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release --no-build` + `coverlet.runsettings` + Coverage), `release.yml` Release-Gate (`dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release`).
  - Für den Ausgangslauf wurde die Release-Gate-Variante gewählt (direkter `dotnet test` auf das Testprojekt in Release-Konfiguration, impliziter Restore/Build), ergänzt um TRX- und Konsolen-Logger.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 (Baseline) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --logger "trx;LogFileName=test-results-baseline.trx" --logger "console;verbosity=normal"` | Repo-Root `D:\Repositories\softwareschmiede\76fe02cd-6ea5-43a8-9143-f8b1ea3bfe4b` | 0 | 239 | 0 | 0 | [Konsolen-Log](test-results/dotnet-test-baseline.log), [TRX-Report](test-results/test-results-baseline.trx) |

Gesamtzeit des Testlaufs: ~3,4 s (Testausführung). Es wurden keine Coverage-Daten erhoben (`--collect:"XPlat Code Coverage"` nicht Teil des Baseline-Befehls) und kein ReportGenerator-Lauf durchgeführt.

### Nachgewiesene bestehende Testfehler

Keine. Im Baseline-Lauf sind alle 239 Tests erfolgreich; es wurden keine Testfehler nachgewiesen.

### Testlücken und Ausführungsprobleme

- **Keine Tests für `scripts/resolve-release-version.mjs`:** Die Vorlage (ci-instructions.md Abschnitt 8.2) verlangt eine Unit-Testsuite inkl. Regressionstest für den Prerelease-Guard (Abschnitt 11.1). Es existiert weder eine Testdatei (kein `*.test.*` im Repo) noch ein Node-Test-Runner oder `test`-Script in `package.json`. Zusätzlich erschwert der unbedingte Top-Level-Aufruf `resolveReleaseVersion()` (Zeile 164) den Import der exportierten Funktionen in Tests.
- **MAUI-App-Projekt nicht gebaut/getestet:** `src/Reporter/Reporter.csproj` wurde im Baseline-Lauf nicht kompiliert (das Testprojekt referenziert es nicht). Das ist keine Testlücke im engeren Sinn — es gibt ohnehin keine Tests, die das App-Projekt laden. Die Solution-weite Variante `dotnet test Reporter.sln` (README) würde den App-Build erfordern; lokal wäre dieser auf Windows prinzipiell möglich (Workloads installiert), wurde aber nicht ausgeführt, da das Release-Gate in `release.yml` ebenfalls nur `Reporter.Tests` ausführt. Kein Infrastrukturproblem festgestellt.
- **Coverage-Schwelle nicht geprüft:** Der CI-Job `build-and-test` erzwingt 70 % Line Coverage; im lokalen Baseline-Lauf wurde keine Coverage gemessen. Aussage zur aktuellen Coverage: unbekannt.
- **Keine E2E-/UI-Tests:** Es existiert keine separate E2E-Suite (kein Playwright o. ä.); `test-results.md` und `test-results/` dokumentieren manuelle UI-Verifikationen früherer Issues.

## Testklassen

Alle Testklassen liegen in `src/Reporter.Tests/` (Namespace `Reporter.Tests`, xUnit, handgeschriebene Fakes statt Mocking-Framework, EF Core SQLite In-Memory via `TestDbContextFactory`).

### `ArticleHtmlSanitizerTests` (7 Tests)
`Sanitize_RemovesScriptsAndEventHandlers`, `Sanitize_Online_PreservesAnchorsAndImages`, `Sanitize_Offline_NeutralizesAnchorsButKeepsText`, `Sanitize_Offline_RemovesImages`, `Sanitize_RemovesDangerousElementsIncludingClosingTags`, `Sanitize_RemovesOrphanedDangerousClosingTags`, `Sanitize_NullOrWhitespace_ReturnsInput` — HTML-Sanitizer für Online-/Offline-Darstellung.

### `AutoRefreshServiceTests` (10 Tests, via `FakeTimeProvider`)
`StartAsync_InvokesSyncAfterInterval`, `ApplySettings_Disabled_Stops`, `ApplySettings_ChangesInterval`, `SyncThrows_LoopContinues`, `CoalescedTicks_DoNotStartParallelSync`, `Tick_WhenOffline_SkipsSyncAll`, `Tick_WhenBackOnline_ResumesSync`, `InvalidInterval_Clamped` (parametrisiert), `StartStop_RepeatedCycles_RestartsCleanly`, `ApplySettings_Concurrent_LeavesSingleActiveLoop` — Timer-Loop, Intervall-Clamping, Offline-Verhalten, Nebenläufigkeit.

### `BaseViewModelConnectivityTests` (5 Tests)
`InitConnectivity_SetsIsOnline_WithoutTracking`, `TrackConnectivity_UpdatesIsOnline_AndInvokesHook`, `UntrackConnectivity_StopsUpdates`, `RefreshConnectivityStatus_InvokesHookOnlyOnChange`, `TrackConnectivity_CanBeReattached` — Connectivity-Tracking der ViewModel-Basisklasse.

### `CategoriesViewModelTests` (6 Tests)
`LoadCommand_PopulatesCategoriesWithFeedCounts`, `SaveCommand_WithEmptyName_SetsValidationErrorAndDoesNotAdd`, `SaveCommand_WithDuplicateName_SetsValidationError`, `SaveCommand_AddsNewCategory`, `SaveCommand_UpdatesExistingCategory`, `DeleteCommand_RemovesCategoryAndReloadsList`.

### `CategoryRepositoryTests` (9 Tests)
CRUD, Sortierung nach Name, `GetAllWithFeedCountAsync`, Duplikat-`DbUpdateException`, `DeleteAsync` setzt `CategoryId` zugeordneter Feeds auf `null`.

### `FeedRepositoryTests` (7 Tests)
CRUD, Sortierung nach Titel, `GetAllWithDetailsAsync` (projiziert `NotificationsEnabled`), kaskadierendes Löschen gespeicherter Items.

### `FeedSyncServiceTests` (14 Tests)
`SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk`, `..._Duplicates_SkipsExistingItems`, `..._Unreachable_KeepsItemsAndLogsError`, `..._InvalidXml_SetsError`, `..._FewerItems_SetsWarning`, `..._NoNewItemsForThirtyDays_SetsWarning`, `SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth`, `SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog`, `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange`, `..._NewItems_NotifiesWithFeedAndItems`, `..._SummaryMode_SendsSingleSummaryNotification`, `..._NoNewItems_DoesNotNotify`, `..._FeedDisabled_NoNotifications`, `..._NotificationThrows_SyncStillSucceeds` — Service-Ebene-Integration (RSS-Parsing, Health-Status, Benachrichtigungsanbindung, Offline).

### `FeedsViewModelTests` (17 Tests)
Refresh-/RefreshAll-Commands inkl. Offline- und Fehlerfälle, `ConnectivityChanged`-Reaktionen, `NotificationsEnabled`-Persistenz über `EditCommand`/`SaveCommand`/`DeleteCommand`.

### `ItemRepositoryTests` (28 Tests)
CRUD, `GetUnreadByDateAsync` (Sortierung, Paging, All-Read), `GetByFeedAsync`/`GetByCategoryAsync`, `GetSavedForLaterAsync`, `MarkAsReadAsync`, `ToggleSavedForLaterAsync`, `DeleteExpiredAsync` (Aufbewahrungsfrist, `ReadAt` vs. `PublishedAt`, Saved/Unread-Schutz, Cancellation), `GetExpiredKeywordCandidatesAsync`, `DeleteRangeAsync`.

### `KeywordMatcherTests` (7 Tests)
Case-insensitive Titel-/Content-Treffer, Substring, Null/Leer-Eingaben, Blank-Keywords.

### `KeywordRepositoryTests` (5 Tests)
CRUD, Sortierung nach Text, `GetByIdAsync`-Nullfall.

### `LaterViewModelTests` (6 Tests)
`LoadCommand` (nur gespeicherte Items, Sortierung), `ConnectivityChanged`, `ToggleSavedCommand` (Entfernen aus Liste, Null-Fall), `MarkReadCommand`.

### `NotificationServiceTests` (12 Tests, via `FakeTimeProvider` + Fakes)
Ruhezeiten inkl. Mitternachts-Wrap-around und Teil-Bounds, globale/pro-Feed-Deaktivierung, Keyword-Filter, Einzel- vs. Sammel-Modus, Dedup-Identifier, `ItemLink` im UserInfo.

### `ReporterDbContextFactoryTests` (1 Test)
`CreateDbContext_ReturnsContextWithSqliteProvider`.

### `ReporterDbContextTests_Persistence` (2 Tests)
`SaveChangesAsync_PersistsFeedWithCategory`, `Items_WithInclude_ReturnsFeedAndCategory`.

### `ReporterDbContextTests_Schema` (1 Test)
`EnsureCreatedAsync_CreatesQueryableTables`.

### `RetentionCleanupServiceTests` (7 Tests)
`CleanupAsync` (Aufbewahrungsfrist, Saved/Unread-Schutz, Keyword-Match, `PublishedAt`-vs-`ReadAt`-Priorität, Cancellation, Deaktivierung bei ≤ 0 Tagen).

### `ServiceCollectionTests` (1 Test)
`AddReporterRepositories_ResolvesAllRepositories` — DI-Registrierung.

### `SettingsRepositoryTests` (7 Tests)
`GetAsync` (Seed/Default-Singleton), `SaveAsync` (Overwrite, ID-Ignorierung, neue Felder, `NotificationSummaryEnabled`).

### `SettingsValuesTests_AutoMarkRead` (1 Test, parametrisiert)
`IsAutoMarkReadEnabled_EvaluatesMode` (`on_open`, `null` → `true`).

### `SettingsViewModelTests_E2E` (5 Tests)
`E2E_ChangeSettings_PersistRoundtrip`, `E2E_NotificationSummary_PersistRoundtrip`, `E2E_KeywordAddRemove_Persists`, `E2E_KeywordEmpty_Rejected`, `E2E_KeywordDuplicate_Rejected` — Persistenz-Roundtrips über ViewModel + Repository + DB.

### `SettingsViewModelTests_Keywords` (5 Tests)
`AddKeyword_Valid_AddsToRepository`, `AddKeyword_DuplicateCaseInsensitive_ShowsError`, `AddKeyword_Empty_ShowsError`, `AddKeyword_TooLong_ShowsError`, `RemoveKeyword_DeletesFromRepository`.

### `SettingsViewModelTests_Load` (10 Tests)
`Load` (Optionen, Fallbacks bei ungültigen Persistenzwerten, `NotificationSummaryEnabled`, Permission-Status `Denied`/`Granted`/`NotDetermined`, Plattform-Support, Quiet-Hours-Default) und `ThemeOptions_ExposePersistedValues`.

### `SettingsViewModelTests_Persist` (20 Tests)
Sofort-Persistenz, RetentionDays (Clamping, Debounce, `DragCompleted`), `NotificationSummaryEnabled`, `NotificationsEnabled` (Authorization-Request, Denied-Event, Unsupported-Platform, Permission-Hints), `ThemeChange_AppliesTheme`, `AutoRefreshChange_AppliesSettings`, QuietHours (Persist-Null, Session-Restore, Defaults, Reload), `Persist_QueuedBehindRunningSave_AppliesThemeOnce`.

### `SyncLogRepositoryTests` (5 Tests)
CRUD, Sortierung nach `StartedAt` absteigend, `GetByIdAsync`-Nullfall.

### `UnreadViewModelTests` (15 Tests)
`LoadCommand` (Artikel + Kategorien), `SelectCategoryCommand`-Filter, `MarkReadCommand`/`MarkAllReadCommand`, `RefreshCommand` (Sync-Aufruf, Offline, Fehler, `IsSyncing`-Preset, Load-Fehler nach erfolgreichem Sync), `ConnectivityChanged` (Fehlermeldung, `IsOnline`), `ToggleSavedCommand`.

### `WebViewNavigationGuardTests` (2 Tests, parametrisiert)
`IsExternalUrl_WebUrls_ReturnsTrue`, `IsExternalUrl_LocalOrOtherUrls_ReturnsFalse` (`file:///`, `about:blank`, `null`).

## Hilfsmethoden

### `TestDbContextFactory` (`TestDbContextFactory.cs`)
`IDbContextFactory<ReporterDbContext>` auf SQLite-In-Memory-Basis (`Mode=Memory;Cache=Shared`, GUID-isolierte DB pro Factory, Keep-Alive-Connection, `DefaultTimeout=30` gegen `SQLITE_BUSY`). Erstellt das Schema im Konstruktor; `IDisposable`.

### `TestDataSeeder` (`TestDataSeeder.cs`)
`SeedFeedAsync(TestDbContextFactory)` — legt einen Feed mit Default-Werten an und liefert die Feed-ID.

### `TestSettingsHelper` (`TestSettingsHelper.cs`)
`SaveAsync(ISettingsRepository, ...)` — lädt das Singleton-Settings-Record und speichert es mit optionalen Overrides (u. a. `notificationsEnabled`, `notificationSummaryEnabled`, `quietHoursStart`/`quietHoursEnd`); `null` = bestehender Wert bleibt.

### `TestWaitHelper` (`TestWaitHelper.cs`)
`WaitUntilAsync(Func<bool> | Func<Task<bool>>, timeoutMilliseconds = 5000)` — Polling-Helfer für fire-and-forget Persistenz der ViewModels.

### Fake-Implementierungen (ohne eigene Tests)
`FakeAppThemeService`, `FakeAutoRefreshService`, `FakeFeedSyncService`, `FakeLocalNotificationService`, `FakeNetworkStatusService`, `FakeNotificationService` — kapseln plattformabhängige bzw. zeitgesteuerte Dienste für ViewModel-/Service-Tests.

### `coverlet.runsettings`
Schließt `[*]Reporter.Data.Migrations.*` aus der Coverage aus — relevant für das 70-%-Gate in den CI-Jobs.
