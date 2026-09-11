# Tests

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt:** Lauf 1: 2026-09-11, ca. 19:01–19:03 Uhr MESZ (+02:00); Lauf 2: unmittelbar danach, ca. 19:04 Uhr MESZ.
- **Branch und Commit-ID:** `task/issue-27-701ef16d7c03422499bad991054d7310-lokale-ios-benachrichtigungen`, Commit `84d6aa0834efb2bf8ddb38145266d790bc6122e4` („Einstellungen, Aufbewahrungsdauer, Keyword-Filter und Löschlogik (#60)").
- **Uncommittete Änderungen im getesteten Stand:** Nur untracked Dateien unter `docs/features/` (übersetzte Anforderung `requirement.md`, `todo.md` sowie die bei der Bestandsaufnahme entstehenden Inventory-Dateien). **Keine Änderungen an Produktivcode, Tests oder Testkonfiguration.**
- **Testumgebung und Runtime-/SDK-Versionen:** Windows (Git-Bash-Shell), .NET SDK `10.0.401` (einzige installierte SDK-Version). Testprojekt `src/Reporter.Tests/Reporter.Tests.csproj`: `net10.0`, xUnit `2.9.3` + `xunit.runner.visualstudio 3.1.4`, `Microsoft.EntityFrameworkCore.Sqlite 10.0.12`, `Microsoft.Extensions.TimeProvider.Testing 10.1.0`, `coverlet.collector 6.0.4`, `Microsoft.NET.Test.Sdk 17.14.1`. Testdatenbanken laufen über In-Memory-SQLite (`TestDbContextFactory`).
- **Ermittelte Testsuiten und Quellen der Testbefehle:** Es existiert genau eine Testsuite (`Reporter.Tests`). Der Testbefehl stammt aus dem CI-Workflow `.github/workflows/pr-staging-ci.yml`, Job `build-and-test`, Schritt „Test" (Zeilen 78-84). `scripts/Run-StaticChecks.ps1` deckt nur statische Prüfungen ab (Format, Security-Scan, Release-Build mit `TreatWarningsAsErrors`), keine Tests. Die MAUI-App `src/Reporter` ist nicht Teil der Testausführung; Plattformcode (`UNUserNotificationCenter` etc.) ist nicht unit-testbar.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"` (mit Release-Build, ohne `--no-build`) | Repository-Root | 1 | 145 | 2 | 0 | [TRX](test-results/run1/test-results.trx), [Konsole](test-results/run1/console-output.txt), [Coverage](test-results/run1/coverage.cobertura.xml) |
| 2 (Wiederholung) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results-run2.trx" --logger "console;verbosity=normal"` | Repository-Root | 0 | 147 | 0 | 0 | [TRX](test-results/run2/test-results-run2.trx), [Konsole](test-results/run2/console-output.txt) |

### Nachgewiesene bestehende Testfehler

Beide Fehlschläge traten **nur in Lauf 1** auf und waren in Lauf 2 erfolgreich — nicht-deterministisches (flaky) Verhalten. Das Fehlerbild (`SQLite Error 5: 'unable to delete/modify user-function due to active statements'`) entsteht, wenn `TestWaitHelper.WaitUntilAsync` parallel zum noch laufenden Debounced-Persist einen neuen `DbContext` auf derselben geteilten In-Memory-SQLite-Verbindung erzeugt (`SqliteRelationalConnection.InitializeDbConnection` → `CreateFunction` kollidiert mit aktiven Statements). Es ist ein Testinfrastruktur-/Timing-Problem, kein Produktivcode-Fehler.

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|-----------------------|------------------|---------------------------|-------------------|
| `Reporter.Tests.SettingsViewModelTests_Persist.RetentionDays_ChangeWithoutDragCompleted_PersistsAfterDebounce` | `src/Reporter.Tests/SettingsViewModelTests_Persist.cs:91` (Assertion über `TestWaitHelper.WaitUntilAsync`, Zeile 102) | `Microsoft.Data.Sqlite.SqliteException : SQLite Error 5: 'unable to delete/modify user-function due to active statements'` — Stacktrace durch `SettingsRepository.GetAsync` (`SettingsRepository.cs:28/39`) | Lauf 1, [TRX](test-results/run1/test-results.trx) / [Konsole](test-results/run1/console-output.txt) |
| `Reporter.Tests.SettingsViewModelTests_Persist.Persist_QueuedBehindRunningSave_AppliesThemeOnce` | `src/Reporter.Tests/SettingsViewModelTests_Persist.cs:375` (Assertion über `TestWaitHelper.WaitUntilAsync`, Zeile 386) | Identisches Fehlerbild wie oben | Lauf 1, [TRX](test-results/run1/test-results.trx) / [Konsole](test-results/run1/console-output.txt) |

**Konsequenz für spätere Arbeiten:** Nur diese beiden Testfälle mit diesem Fehlerbild gelten als nachgewiesen „preexisting" (und flaky). Alle anderen Tests waren in beiden Läufen erfolgreich; neue oder abweichende Fehlschläge sind zu untersuchen.

### Testlücken und Ausführungsprobleme

- **Keine Tests für Benachrichtigungen vorhanden:** Es gibt keine Testklasse für einen `INotificationService`/`ILocalNotificationService` (Interfaces existieren nicht) und keine Tests für ein Pro-Feed-`NotificationsEnabled` (Feld existiert nicht).
- **Plattformcode nicht unit-testbar:** `UNUserNotificationCenter` (iOS) kann nicht in `Reporter.Tests` geprüft werden; manuelle Verifikation ist per `scripts/iOS-Deployment.ps1` (Aktion `simulator`) auf macOS vorgesehen — in dieser Windows-Umgebung nicht ausführbar.
- **Keine UI-Tests:** Es existiert keine automatisierte UI-Testsuite; die Mobile-UI-Verifikation erfolgt laut `AGENTS.md` manuell per Screenshot.
- **Keine Migrations-Tests:** Tests nutzen `EnsureCreated`/`EnsureCreatedAsync` statt `Migrate` — das tatsächliche Migrations-SQL (z. B. einer künftigen `AddFeedNotificationsEnabled`-Migration) wird durch die Suite nicht geprüft.
- Übersprungene/deaktivierte Tests: keine (TRX: `total=147, executed=147`).

## Testklassen

### `FeedSyncServiceTests` — `src/Reporter.Tests/FeedSyncServiceTests.cs`
Integrationstests mit `TestDbContextFactory` + privatem `FakeHttpMessageHandler` (RSS-XML über `StringContent`).
- `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk` — legt Items an, Status `Ok`, `NewItems = 2`
- `SyncFeedAsync_Duplicates_SkipsExistingItems` — Dedup über `GuidOrHash`, `NewItems = 0`
- `SyncFeedAsync_Unreachable_KeepsItemsAndLogsError` — Fehler → `FeedHealth.Error`, Items bleiben
- `SyncFeedAsync_InvalidXml_SetsError` — Parse-Fehler → `Error`
- `SyncFeedAsync_FewerItems_SetsWarning` — deutlich weniger Einträge → `Warning`
- `SyncFeedAsync_NoNewItemsForThirtyDays_SetsWarning` — Staleness → `Warning`
- `SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth` — Aggregation über zwei Feeds
- Hilfsmethoden (privat): `SeedFeedAsync`, `CreateService`, `CreateFailingService`, `RssXml`

### `FeedsViewModelTests` — `src/Reporter.Tests/FeedsViewModelTests.cs`
- `RefreshCommand_InvokesSyncService_AndReloadsList` — manueller Einzel-Sync
- `RefreshAllCommand_InvokesSyncService_AndReloadsList` — manueller Gesamt-Sync
- `RefreshCommand_WhenSyncReturnsError_SetsErrorMessage` — Fehleranzeige
- Enthält ein **eigenes privates `FakeFeedSyncService`** (Zeilen 110-141; zusätzlich zum globalen Fake in `FakeFeedSyncService.cs`)

### `FeedRepositoryTests` — `src/Reporter.Tests/FeedRepositoryTests.cs`
- `AddAsync_ThenGetByIdAsync_ReturnsFeed`, `GetAllAsync_ReturnsFeedsOrderedByTitle`, `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesFeed`, `GetByIdAsync_NonExisting_ReturnsNull`, `DeleteAsync_CascadeDeletesSavedItems`
- Kein Test für ein `NotificationsEnabled`-Feld (existiert noch nicht)

### `KeywordMatcherTests` — `src/Reporter.Tests/KeywordMatcherTests.cs`
- `MatchesAny_TitleCaseInsensitive`, `MatchesAny_ContentHtml`, `MatchesAny_Substring`, `MatchesAny_NoMatch`, `MatchesAny_NullTitleAndContent_ReturnsFalse`, `MatchesAny_EmptyKeywords_ReturnsFalse`, `MatchesAny_BlankKeywords_Skipped` — decken exakt die Match-Semantik ab, die der Benachrichtigungs-Keyword-Filter wiederverwendet

### `SettingsViewModelTests_Persist` — `src/Reporter.Tests/SettingsViewModelTests_Persist.cs`
- `PropertyChange_PersistsImmediately` — inkl. `NotificationsEnabled = false` (Zeilen 49-65)
- `RetentionDays_OutOfRange_Clamped` (Theory), `RetentionDays_ChangeWithoutDragCompleted_PersistsAfterDebounce` ⚠️ flaky (s. o.), `RetentionDays_RapidChanges_PersistOnlyLastValue`, `RetentionDays_DragCompleted_PersistsImmediatelyAndCancelsDebounce`
- `ThemeChange_AppliesTheme`, `AutoRefreshChange_AppliesSettings`, `Persist_QueuedBehindRunningSave_AppliesThemeOnce` ⚠️ flaky (s. o.)
- `QuietHoursEnabled_TurnedOff_PersistsNull`, `QuietHoursEnabled_ToggledOffAndOn_RestoresSessionValues`, `QuietHoursEnabled_TurnedOff_ReloadKeepsSessionValues`, `QuietHoursEnabled_TurnedOn_AppliesDefaults` (Defaults 22:00/07:00), `QuietHoursEnabled_TurnedOn_KeepsExistingValues`
- Verwendet `FakeTimeProvider` (`Microsoft.Extensions.Time.Testing`) und eigene private `ISettingsRepository`-Wrapper zur Serialisierungssteuerung

### `SettingsViewModelTests_Load` — `src/Reporter.Tests/SettingsViewModelTests_Load.cs`
- `Load_PopulatesAllOptions` — inkl. `NotificationsEnabled = false`, Ruhezeit 22:00–07:00 (Zeilen 45-83)
- `Load_InvalidPersistedValues_UsesFallbacks`, `Load_WithoutQuietHours_QuietHoursDisabled`, `ThemeOptions_ExposePersistedValues`

### `SettingsViewModelTests_E2E` — `src/Reporter.Tests/SettingsViewModelTests_E2E.cs`
- `E2E_ChangeSettings_PersistRoundtrip` — persistiert u. a. `NotificationsEnabled` + Ruhezeit 22:00–07:00 und lädt neu
- `E2E_KeywordAddRemove_Persists`, `E2E_KeywordEmpty_Rejected`, `E2E_KeywordDuplicate_Rejected`

### `SettingsViewModelTests_Keywords` — `src/Reporter.Tests/SettingsViewModelTests_Keywords.cs`
Keyword-Pflege im ViewModel (Hinzufügen/Entfernen/Duplikate).

### `SettingsRepositoryTests` — `src/Reporter.Tests/SettingsRepositoryTests.cs`
- `GetAsync_ReturnsSeededSettings`, `GetAsync_CreatesDefaultRecordWhenMissing`, `GetAsync_AlwaysReturnsSingleRecord`
- `SaveAsync_OverwritesSameRecord` — persistiert und prüft u. a. `NotificationsEnabled = false` (Zeilen 69-92)
- `SaveAsync_IgnoresDifferentIdAndUpdatesDefault` — persistiert und prüft u. a. `NotificationsEnabled = true` (Zeilen 94-118)
- `SaveAsync_PersistsNewFields` — prüft `AutoRefreshEnabled`/`RefreshIntervalMinutes`/`Theme` (setzt `NotificationsEnabled`, assertet es aber nicht) (Zeilen 136 ff.)

### `AutoRefreshServiceTests` — `src/Reporter.Tests/AutoRefreshServiceTests.cs`
- `StartAsync_InvokesSyncAfterInterval`, `ApplySettings_Disabled_Stops`, `ApplySettings_ChangesInterval`, `SyncThrows_LoopContinues`, `CoalescedTicks_DoNotStartParallelSync`, `InvalidInterval_Clamped` (Theory), `StartStop_RepeatedCycles_RestartsCleanly`, `ApplySettings_Concurrent_LeavesSingleActiveLoop`
- Muster: `FakeTimeProvider` steuert den `PeriodicTimer`; `FakeFeedSyncService` zählt/blockiert `SyncAllAsync`-Aufrufe

### `RetentionCleanupServiceTests` — `src/Reporter.Tests/RetentionCleanupServiceTests.cs`
- `CleanupAsync_DeletesExpiredButKeepsSaved`, `CleanupAsync_ZeroOrNegativeRetentionDays_Skips` (Theory), `CleanupAsync_RespectsConfiguredRetentionDays`, `CleanupAsync_CancelledToken_ThrowsOperationCanceled`, `CleanupAsync_DeletesKeywordMatchedExpired`, `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved`, `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt`

### Weitere Testklassen (nicht direkt betroffen)
`CategoriesViewModelTests`, `CategoryRepositoryTests`, `ItemRepositoryTests`, `KeywordRepositoryTests`, `LaterViewModelTests`, `ReporterDbContextFactoryTests`, `ReporterDbContextTests_Persistence`, `ReporterDbContextTests_Schema` (`EnsureCreatedAsync_CreatesQueryableTables` — erwartet u. a. 1 seedeten Settings-Datensatz), `ServiceCollectionTests` (DI-Auflösung der Repositories), `SettingsValuesTests_AutoMarkRead`, `SyncLogRepositoryTests`, `UnreadViewModelTests`.

## Hilfsmethoden

### `TestDbContextFactory` — `src/Reporter.Tests/TestDbContextFactory.cs`
- Konstruktor: öffnet geteilte In-Memory-`SqliteConnection` (`DataSource=:memory:`), `EnsureCreated` (Zeilen 19-30)
- `CreateDbContext()` — neuer `ReporterDbContext` auf derselben Verbindung (Zeilen 36-39)
- `Dispose()` — schließt die Verbindung

### `TestDataSeeder` — `src/Reporter.Tests/TestDataSeeder.cs`
- `SeedFeedAsync(TestDbContextFactory)` — legt `Entities.Feed` direkt an, gibt `Guid` zurück (Zeilen 15-27)

### `TestWaitHelper` — `src/Reporter.Tests/TestWaitHelper.cs`
- `WaitUntilAsync(Func<bool>, int timeoutMilliseconds = 5000)` / `WaitUntilAsync(Func<Task<bool>>, ...)` — Polling für fire-and-forget Persist-Operationen (Zeilen 14-39)

### `FakeFeedSyncService` — `src/Reporter.Tests/FakeFeedSyncService.cs`
- `SyncAllException`, `SyncAllBlocker`, `SyncAllCallCount`; `SyncFeedAsync` liefert festes `SyncResult("OK", 0)` (Zeilen 10-52). **Achtung:** `FeedsViewModelTests` nutzt ein eigenes privates Fake, `AutoRefreshServiceTests` dieses globale.

### `FakeAutoRefreshService` — `src/Reporter.Tests/FakeAutoRefreshService.cs`
- `AppliedSettings`, `StartCallCount`, `StopCallCount` (Zeilen 9-47)

### `FakeAppThemeService` — `src/Reporter.Tests/FakeAppThemeService.cs`
- `AppliedThemes` — Muster für ein künftiges `FakeLocalNotificationService` (Zeilen 8-21)

### `FakeHttpMessageHandler` (privat) — in `FeedSyncServiceTests.cs:293-311`
- `HttpMessageHandler` mit konfigurierbarer Response-Factory für Feed-Abrufe
