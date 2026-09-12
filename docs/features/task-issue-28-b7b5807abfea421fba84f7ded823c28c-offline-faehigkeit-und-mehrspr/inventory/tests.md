# Tests — Bestandsaufnahme und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-12, ca. 07:47–07:52 MESZ (UTC+02:00)
- **Branch und Commit-ID:** `task/issue-28-b7b5807abfea421fba84f7ded823c28c-offline-faehigkeit-und-mehrspr`, Commit `2704b5addc904b5991311783ff75468d097d4ba2` (2026-09-12 07:37:54 +0200)
- **Uncommittete Änderungen im getesteten Stand:** Nur untracked Dateien unter `docs/features/task-issue-28-b7b5807abfea421fba84f7ded823c28c-offline-faehigkeit-und-mehrspr/` (`requirement.md`, `todo.md`, sowie die bei diesem Lauf neu angelegten `inventory/*`-Dateien). **Keine Änderungen an Code, Tests oder Testkonfiguration.** `git status --porcelain` meldete vor dem Lauf ausschließlich `?? docs/features/`.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows (Arbeitsplatz), .NET SDK `10.0.401` (`C:\Program Files\dotnet\sdk`), installierte Workloads `android`, `ios`, `maccatalyst`, `maui-windows` (je 10.0.400/VS 18.10.12201.205). Umgebungsvariable `IncludeIosTarget=false` gesetzt (spiegelt die CI-Jobs, die das iOS-Target auf Windows-Runnern deaktivieren).
- **Ermittelte Testsuiten und Quellen der Testbefehle:** Einzige Testsuite ist `src/Reporter.Tests/Reporter.Tests.csproj` (xUnit 2.9.3, `net10.0`, referenziert nur `Reporter.Core` und `Reporter.Data` — nicht das MAUI-Projekt). Testbefehl aus `.github/workflows/pr-staging-ci.yml` (Job `build-and-test`, Zeilen 73–84) und identisch in `staging-ci.yml`; `release.yml` Zeile 61 nutzt `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release`. `scripts/Run-StaticChecks.ps1` führt nur Restore/Format/Security/Static-Analysis-Build aus (keine Tests). `ci-instructions.md` ist eine generische Pipeline-Anleitung (Platzhalter `MyApp.sln`), nicht projektspezifisch.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Restore | `dotnet restore Reporter.sln -r win-x64` (Env: `IncludeIosTarget=false`) | Repo-Root | 0 | – | – | – | [01-restore.log](test-results/01-restore.log) |
| Build | `dotnet build Reporter.sln --configuration Release --no-restore` (Env: `IncludeIosTarget=false`) | Repo-Root | 0 (0 Warnungen, 0 Fehler) | – | – | – | [02-build-release.log](test-results/02-build-release.log) |
| Tests | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 189 | 0 | 0 | [03-dotnet-test.log](test-results/03-dotnet-test.log), [test-results.trx](test-results/test-results.trx), [coverage.cobertura.xml](test-results/coverage.cobertura.xml) |

### Nachgewiesene bestehende Testfehler

Keine. Der Ausgangslauf meldet `Gesamtzahl Tests: 189 / Bestanden: 189`, 0 fehlgeschlagen, 0 übersprungen (siehe [03-dotnet-test.log](test-results/03-dotnet-test.log) und TRX-Report).

### Testlücken und Ausführungsprobleme

- Alle 189 Tests wurden ausgeführt; keine Abbrüche, keine Infrastrukturfehler.
- **Das MAUI-Projekt `Reporter` wird von `Reporter.Tests` nicht referenziert** (`Reporter.Tests.csproj` referenziert nur `Reporter.Core` und `Reporter.Data`). Dadurch ohne jede Testabdeckung: `ArticleDetailViewModel` (liegt in `src/Reporter/ViewModels/` trotz Namespace `Reporter.Core.ViewModels`), alle `Views`/`Pages` (`ArticleDetailPage`, `UnreadPage`, `FeedsPage`, `SettingsPage`), `AppThemeService`, `LocalNotificationService`, `MauiProgram`, `App`, `AppShell`.
- Keine Tests für Netzwerk-/Offline-Verhalten — ein `INetworkStatusService` existiert noch nicht (kein `FakeNetworkStatusService` o. ä. vorhanden).
- Keine Tests für die ResX-Lokalisierung (Schlüsselgleichstand EN/DE prüft nur der Pre-Commit-Hook `.githooks/translation-check.py`, nicht die Testsuite).

## Testklassen (für die Anforderung relevant)

### `UnreadViewModelTests`
Datei: `src/Reporter.Tests/UnreadViewModelTests.cs` — nutzt `TestDbContextFactory` + echte `ItemRepository`/`CategoryRepository` und eine **klasseninterne** `private sealed class FakeFeedSyncService` (Zeilen 222–236; eigenständig, nicht die Datei `FakeFeedSyncService.cs`).

- `LoadCommand_PopulatesArticlesAndCategories` — lädt ungelesene Artikel und Kategoriefilter
- `SelectCategoryCommand_FiltersArticles` — Kategoriefilter reduziert die Liste
- `MarkReadCommand_RemovesArticle` — Artikel wird aus der Liste entfernt
- `MarkAllReadCommand_ClearsArticles` — Liste wird geleert
- `RefreshCommand_InvokesSyncService` — `RefreshCommand` ruft `SyncAllAsync` auf (relevanter Ankerpunkt für Offline-Verhalten)
- `ToggleSavedCommand_TogglesFlagInPlace` — In-place-Update des `IsSavedForLater`-Flags

### `FeedsViewModelTests`
Datei: `src/Reporter.Tests/FeedsViewModelTests.cs` — ebenfalls mit klasseninternem `private sealed class FakeFeedSyncService` (Zeilen 205–236) mit `LastFeedId`, `SyncAllCalled`, `NextResult`.

- `RefreshCommand_InvokesSyncService_AndReloadsList` — Einzel-Feed-Sync und Reload
- `RefreshAllCommand_InvokesSyncService_AndReloadsList` — `SyncAllAsync` und Reload
- `RefreshCommand_WhenSyncReturnsError_SetsErrorMessage` — `FeedHealth.Error` → `ErrorMessage` (Ankerpunkt für Offline-Meldung)
- `EditCommand_PopulatesFeedNotificationsEnabled` / `SaveCommand_NewFeed_PersistsNotificationsEnabledFalse` / `SaveCommand_ExistingFeed_PersistsNotificationsEnabled` / `SaveCommand_ResetsFeedNotificationsEnabled` / `DeleteCommand_ResetsFeedNotificationsEnabled` — Feed-Formular-Logik

### `FeedSyncServiceTests`
Datei: `src/Reporter.Tests/FeedSyncServiceTests.cs` — mit `private sealed class FakeHttpMessageHandler : HttpMessageHandler` (Zeile 428) für simulierte HTTP-Antworten; nutzt `FakeNotificationService`.

- `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk` — Erfolgspfad
- `SyncFeedAsync_Duplicates_SkipsExistingItems` — Dedupe über `GuidOrHash`
- `SyncFeedAsync_Unreachable_KeepsItemsAndLogsError` — **bereits vorhandener Offline-/Fehler-Test:** unerreichbarer Feed → bestehende Items bleiben, `SyncLog`/`HealthStatus` = Error (Zeilen 180 ff.)
- `SyncFeedAsync_InvalidXml_SetsError` / `SyncFeedAsync_FewerItems_SetsWarning` / `SyncFeedAsync_NoNewItemsForThirtyDays_SetsWarning` — Statuslogik
- `SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth` — Mehrfach-Feeds
- `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems` / `_SummaryMode_SendsSingleSummaryNotification` / `_NoNewItems_DoesNotNotify` / `_FeedDisabled_NoNotifications` / `_NotificationThrows_SyncStillSucceeds` — Benachrichtigungspfad

### `AutoRefreshServiceTests`
Datei: `src/Reporter.Tests/AutoRefreshServiceTests.cs` — nutzt `Microsoft.Extensions.TimeProvider.Testing.FakeTimeProvider` und `FakeFeedSyncService` (Shared-Fake mit `SyncAllException`/`SyncAllBlocker`/`SyncAllCallCount`).

- `StartAsync_InvokesSyncAfterInterval` — Timer tickt → `SyncAllAsync`
- `ApplySettings_Disabled_Stops` / `ApplySettings_ChangesInterval` / `StartStop_RepeatedCycles_RestartsCleanly` / `ApplySettings_Concurrent_LeavesSingleActiveLoop` — Loop-Lebenszyklus
- `SyncThrows_LoopContinues` — Exception im Sync beendet den Loop nicht
- `CoalescedTicks_DoNotStartParallelSync` — keine parallelen Syncs
- `InvalidInterval_Clamped` (Theory) — Intervall-Clamping 1–1440 min

### `SettingsViewModelTests_*`
Dateien: `SettingsViewModelTests_Load.cs`, `SettingsViewModelTests_Persist.cs`, `SettingsViewModelTests_Keywords.cs`, `SettingsViewModelTests_E2E.cs` — nutzen `FakeAppThemeService`, `FakeAutoRefreshService`, `FakeLocalNotificationService`, `TestWaitHelper` sowie klasseninterne `RecordingSettingsRepository`/`GatedSettingsRepository` (`SettingsViewModelTests_Persist.cs` Zeilen 540, 563).

- `ThemeOptions_ExposePersistedValues` (`_Load`, Z. 240) und `ThemeChange_AppliesTheme` (`_Persist`, Z. 310) — Muster, das ein Sprach-Picker-Test nachbilden würde
- `AutoRefreshChange_AppliesSettings` (`_Persist`, Z. 325) — Service-Reapply beim Persistieren
- `E2E_ChangeSettings_PersistRoundtrip` / `E2E_NotificationSummary_PersistRoundtrip` (`_E2E`) — Persist-Roundtrips über echtes `SettingsRepository`
- u. a. Ruhezeiten-, Benachrichtigungs-Berechtigungs- und Keyword-Tests

### `SettingsRepositoryTests` / `SettingsValuesTests_AutoMarkRead` / `ServiceCollectionTests`

- `SettingsRepositoryTests` (`SettingsRepositoryTests.cs`): `GetAsync_ReturnsSeededSettings`, `GetAsync_CreatesDefaultRecordWhenMissing`, `SaveAsync_OverwritesSameRecord`, `SaveAsync_IgnoresDifferentIdAndUpdatesDefault`, `GetAsync_AlwaysReturnsSingleRecord`, `SaveAsync_PersistsNewFields`, `SaveAsync_PersistsNotificationSummaryEnabled` — Singleton-Semantik der `settings`-Tabelle
- `SettingsValuesTests_AutoMarkRead` (`SettingsValuesTests_AutoMarkRead.cs`): `IsAutoMarkReadEnabled_EvaluatesMode` (Theory) — Konstanten-Auswertung
- `ServiceCollectionTests` (`ServiceCollectionTests.cs`): `AddReporterRepositories_ResolvesAllRepositories` — prüft nur Repository-Registrierungen, nicht `MauiProgram`

### Nicht direkt betroffene Testklassen (Vollständigkeit)

`CategoriesViewModelTests`, `LaterViewModelTests`, `CategoryRepositoryTests`, `FeedRepositoryTests`, `ItemRepositoryTests`, `KeywordRepositoryTests`, `KeywordMatcherTests`, `NotificationServiceTests`, `RetentionCleanupServiceTests`, `SyncLogRepositoryTests`, `ReporterDbContextTests_Schema`, `ReporterDbContextTests_Persistence`, `ReporterDbContextFactoryTests` — laufen alle im Ausgangslauf erfolgreich.

## Hilfsmethoden

### `TestDbContextFactory`
`src/Reporter.Tests/TestDbContextFactory.cs` — `IDbContextFactory<ReporterDbContext>` über Shared-In-Memory-SQLite (`Mode=Memory;Cache=Shared;Default Timeout=30`), Keep-Alive-Connection, `EnsureCreated` im Konstruktor.

### `TestDataSeeder`
`src/Reporter.Tests/TestDataSeeder.cs` — `SeedFeedAsync(TestDbContextFactory)`: legt einen Default-Feed an, gibt die `feedId` zurück.

### `TestSettingsHelper`
`src/Reporter.Tests/TestSettingsHelper.cs` — `SaveAsync(ISettingsRepository, notificationsEnabled?, notificationSummaryEnabled?, quietHoursStart?, quietHoursEnd?)`: liest den Singleton-Datensatz und speichert ihn mit Overrides zurück (Voll-Feld-Kopie; müsste bei einem neuen `Settings.Language`-Feld erweitert werden).

### `TestWaitHelper`
`src/Reporter.Tests/TestWaitHelper.cs` — `WaitUntilAsync(Func<bool>|Func<Task<bool>>, timeoutMilliseconds = 5000)`: Polling-Helper für Fire-and-Forget-Persistierungen; schlägt per `Assert.True` fehl, wenn die Bedingung nicht eintritt.

### Fakes (handgeschrieben, Dateiebene `src/Reporter.Tests/`)

- `FakeFeedSyncService` (`FakeFeedSyncService.cs`) — `SyncAllException`, `SyncAllBlocker` (`TaskCompletionSource<bool>`), `SyncAllCallCount`; `SyncFeedAsync`/`SyncAllAsync` liefern `SyncResult("OK", 0)`
- `FakeAutoRefreshService` (`FakeAutoRefreshService.cs`) — `AppliedSettings`, `StartCallCount`, `StopCallCount`
- `FakeAppThemeService` (`FakeAppThemeService.cs`) — `AppliedThemes`
- `FakeLocalNotificationService` (`FakeLocalNotificationService.cs`) — `ShownNotifications`, `AuthorizationResult`, `AuthorizationStatus`, `IsSupported`, `RequestAuthorizationCallCount`; Record `ShownNotification`
- `FakeNotificationService` (`FakeNotificationService.cs`) — `Calls`, `Exception`; Record `NotificationCall`

Klasseninterne (private) Fakes: `FakeFeedSyncService` in `UnreadViewModelTests` und `FeedsViewModelTests`, `FakeHttpMessageHandler` in `FeedSyncServiceTests`, `RecordingSettingsRepository`/`GatedSettingsRepository` in `SettingsViewModelTests_Persist`.
