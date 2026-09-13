<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Bestandsaufnahme (Issue #77)

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-13, ca. 22:08–22:15 Uhr +0200 (MESZ)
- Branch und Commit-ID: `task/issue-77-b1023d3d5f804e239e02f48af24b0ac3-verbesserungen-der-app`, Commit `d9100c625fc2502e446963e502c896c7c7a7e273`
- Uncommittete Änderungen im getesteten Stand: keine geänderten oder gestagten Dateien; einzig das Feature-Dokumentationsverzeichnis `docs/features/task-issue-77-b1023d3d5f804e239e02f48af24b0ac3-verbesserungen-der-app/` war untracked (`requirement.md`, `todo.md`, zuzüglich der bei diesem Lauf angelegten `inventory/`-Dateien). Der getestete Quellcode entspricht vollständig dem Commit-Stand.
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK `10.0.401`; installierte Workloads: `maui-windows 10.0.20`, `ios 26.5.10318`, `android 36.1.69`, `maccatalyst 26.5.10318`. Testprojekt: xUnit (`xunit 2.9.3`, `Microsoft.NET.Test.Sdk 17.14.1`, `coverlet.collector 6.0.4`, `Microsoft.EntityFrameworkCore.Sqlite 10.0.12`, `Microsoft.Extensions.TimeProvider.Testing 10.1.0`), Zielframework `net10.0`.
- Ermittelte Testsuiten und Quellen der Testbefehle: einzige Testsuite ist `src/Reporter.Tests/Reporter.Tests.csproj` (xUnit). Befehle aus `.github/workflows/pr-staging-ci.yml` (Jobs `static-checks`/`build-and-test`) und `scripts/Run-StaticChecks.ps1` übernommen: `IncludeIosTarget=false`, `IncludeAndroidTarget=false`, Windows-Runtime `win-x64`, Release-Konfiguration. Die MAUI-App `src/Reporter` wird in diesem Lauf nur für `net10.0-windows10.0.19041.0` gebaut (iOS-Target ist über die Env-Variablen ausgeschlossen — projektübliche Konvention auf Windows).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Restore | `dotnet restore Reporter.sln -r win-x64` (Env: `IncludeIosTarget=false`, `IncludeAndroidTarget=false`) | Repo-Root | 0 | – | – | – | [restore.log](test-results/restore.log) |
| Build | `dotnet build Reporter.sln --configuration Release --no-restore` | Repo-Root | 0 (0 Warnungen, 0 Fehler, ~8,5 s) | – | – | – | [build.log](test-results/build.log) |
| Tests | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results-inventory.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 362 | 0 | 0 | [test-run.log](test-results/test-run.log), [test-results-inventory.trx](test-results/test-results-inventory.trx), [coverage.cobertura.xml](test-results/coverage.cobertura.xml) |

Gesamtzahl laut Konsolenausgabe und TRX-`Counters`: `total=362, executed=362, passed=362, failed=0, notExecuted=0`. Laufzeit ~5,3 s.

### Nachgewiesene bestehende Testfehler

Keine — alle 362 Tests des Ausgangslaufs sind bestanden; es wurden keine fehlgeschlagenen, übersprungenen oder nicht ausgeführten Tests nachgewiesen.

### Testlücken und Ausführungsprobleme

- Keine Infrastruktur- oder Setup-Fehler; Restore, Build und Testlauf liefen vollständig durch.
- Der iOS-Target der App (`net10.0-ios`) wurde projektkonform nicht gebaut/getestet (`IncludeIosTarget=false`); plattformspezifischer Code unter `Platforms/iOS/` (u. a. `NotificationDelegate`, R8) ist damit im Ausgangslauf nicht kompiliert.
- `scripts/Run-StaticChecks.ps1` (Format, Security, Static Analysis) wurde **nicht** ausgeführt — laut Aufgabenstellung nicht Teil der Bestandsaufnahme.
- Es existieren keine UI-/Plattform-Tests; Verifikation von Views erfolgt manuell (390 × 844-pt-Windows-Fenster bzw. iOS-Deployment-Skript).

## Testklassen

### `ReadingTimeEstimatorTests` (`src\Reporter.Tests\ReadingTimeEstimatorTests.cs`) — R1
- `EstimateText_NullContent_ReturnsEmpty`, `EstimateText_EmptyContent_ReturnsEmpty` — leere Eingaben → `string.Empty`
- `EstimateText_ShortContent_ReturnsOneMinute` — kurze Inhalte → „1 Min." (untere Klemme, R1-Ausgangspunkt)
- `EstimateText_HtmlContent_StripsTags`, `EstimateText_LongContent_ReturnsRoundedMinutes` — Tag-Stripping und Rundung

### `ItemRepositoryTests` (`src\Reporter.Tests\ItemRepositoryTests.cs`) — R1, R2, R4
- `GetUnreadByDateAsync_ReturnsUnreadSortedByPublishedAtDescending`, `GetUnreadByDateAsync_Paged_ReturnsPage`, `GetSavedForLaterAsync_OrdersByPublishedAtDescending`, `GetSavedForLaterAsync_Paged_ReturnsPage` — feste absteigende Sortierung (R4-Regressionstests)
- `GetUnreadByDateAsync_Paged_ProjectsReadingTimeText`, `GetSavedForLaterAsync_ProjectsReadingTimeText` — Projektion der Lesezeit (R1-Anker)
- CRUD-, Zähl-, `MarkAsRead`-/`ToggleSaved`-, `DeleteExpired`-, `GetExpiredKeywordCandidates`-, `DeleteRange`-Tests; Kategoriefilter über `GetUnreadCountAsync`/`GetByCategoryAsync`

### `UnreadViewModelTests` (`src\Reporter.Tests\UnreadViewModelTests.cs`) — R4
- `LoadCommand_PopulatesArticlesAndCategories`, `SelectCategoryCommand_*`, `MarkReadCommand_*`, `MarkAllReadCommand_ClearsArticles`, `RefreshCommand_*` (inkl. Offline-Verhalten und `IsSyncing`-Presets), `ConnectivityChanged_*`, `ToggleSavedCommand_TogglesFlagInPlace`
- Konstruktor: `UnreadViewModel(IItemRepository, ICategoryRepository, IFeedSyncService, INetworkStatusService)` — R4-Signaturbruch (neue `ISettingsRepository`-Abhängigkeit) muss hier nachgezogen werden.

### `LaterViewModelTests` (`src\Reporter.Tests\LaterViewModelTests.cs`)
- `LoadCommand_PopulatesOnlySavedItems`, `LoadCommand_OrdersByPublishedAtDescending`, `LoadMoreCommand_*`, `MarkReadCommand_*`, `ToggleSavedCommand_*`, `ConnectivityChanged_*`, Lade-Fehlerfälle

### `SettingsViewModelTests_Load` / `_Persist` / `_Keywords` / `_E2E` (`src\Reporter.Tests\SettingsViewModelTests_*.cs`) — R3, R4, R5, R7
- `_Load`: `Load_PopulatesAllOptions`, `Load_InvalidPersistedValues_UsesFallbacks`, `Load_PopulatesSelectedLanguage`, `Load_InvalidLanguage_UsesSystemFallback`, `Load_PopulatesNotificationSummaryEnabled`, Permission-Flags (`Denied`/`NotDetermined`/`Supported`), `Load_WithoutQuietHours_QuietHoursDisabled`, `ThemeOptions_ExposePersistedValues`, `LanguageOptions_ExposePersistedValues`
- `_Persist`: `PropertyChange_PersistsImmediately`, `RetentionDays_*` (Clamp, Debounce, DragCompleted), `NotificationSummaryEnabled_Change_Persists`, `NotificationsEnabled_*` (Authorization-Anfrage, Denied-Event, Hint-Rücksetzung), `RequestNotificationPermission_NotDetermined_*`, `ThemeChange_AppliesTheme`, `SelectedLanguage_Change_Persists`, `OtherChange_DoesNotLoseLanguage`, `AutoRefreshChange_AppliesSettings`, `QuietHoursEnabled_*`, `Persist_QueuedBehindRunningSave_AppliesThemeOnce`
- `_Keywords`: `AddKeyword_*`, `RemoveKeyword_DeletesFromRepository`
- `_E2E`: `E2E_ChangeSettings_PersistRoundtrip`, `E2E_NotificationSummary_PersistRoundtrip`, `E2E_ChangeLanguage_PersistRoundtrip`, `E2E_KeywordAddRemove_Persists`, `E2E_KeywordEmpty_Rejected`, `E2E_KeywordDuplicate_Rejected`

### `FeedsViewModelTests` (`src\Reporter.Tests\FeedsViewModelTests.cs`) — R2
- Anlage-Wege: `DirectAddCommand_*` (offline, Domain-Normalisierung, invalide/duplikate URLs, Edit-Mode-Guard), `SearchCommand_*` (Ergebnisse, Fehlerkanal, `ConfirmDirectAdd`-Fluss, Stale-Input), `SubscribeResultCommand_*` (`PersistsFeedFromResult`, Titel-Fallback, Duplikat)
- `EditCommand_*`, `SaveCommand_*`, `DeleteCommand_*`, `Refresh(All)Command_*`, `ConnectivityChanged_*`, Sheet-Open/Close, `RenameFeedAsync_*`, `ChangeFeedCategoryAsync_*`, `MakeUniqueOptionLabels_*`

### `FeedSyncServiceTests` (`src\Reporter.Tests\FeedSyncServiceTests.cs`) — R2, R8
- `SyncFeedAsync_*` (Items anlegen, Dedup, Fehlerfälle, Health-Warnings, Titel-Auflösung aus Feed-Dokument, Keyword-Filter)
- Benachrichtigungen: `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems`, `SyncFeedAsync_SummaryMode_SendsSingleSummaryNotification`, `SyncFeedAsync_NoNewItems_DoesNotNotify`, `SyncFeedAsync_FeedDisabled_NoNotifications`, `SyncFeedAsync_NotificationThrows_SyncStillSucceeds`, `SyncFeedAsync_KeywordFiltered_NotNotified`
- `SyncAllAsync_WithTwoFeeds_*`, `SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog`

### `NotificationServiceTests` (`src\Reporter.Tests\NotificationServiceTests.cs`) — R8
- `NotifyNewItemsAsync_FeedDisabled_SendsNothing`, `_GlobalDisabled_SendsNothing`, `_WithinQuietHours_SendsNothing`, `_OutsideQuietHours_Sends`, `_QuietHoursStartEqualsEnd_Sends`, `_OnlyOneQuietHoursBound_Sends`, `_KeywordMatch_SkipsItem`, `_SendsPerItem_WithItemIdAsIdentifier`, `_SendsItemLink_InUserInfo`, `_SummaryEnabled_*`
- **Kein Test zum App-Zustand (Vordergrund/Hintergrund)** — dieser Aspekt ist im Service aktuell nicht modelliert.

### `AutoRefreshServiceTests` (`src\Reporter.Tests\AutoRefreshServiceTests.cs`) — R3
- `StartAsync_InvokesSyncAfterInterval` (kein Sofort-Sync: erster Tick erst nach Intervall), `ApplySettings_Disabled_Stops`, `ApplySettings_ChangesInterval`, `SyncThrows_LoopContinues`, `CoalescedTicks_DoNotStartParallelSync`, `Tick_WhenOffline_SkipsSyncAll`, `Tick_WhenBackOnline_ResumesSync`, `InvalidInterval_Clamped` (Theory), `StartStop_RepeatedCycles_RestartsCleanly`, `ApplySettings_Concurrent_LeavesSingleActiveLoop`

### `FeedSearchServiceTests` (`src\Reporter.Tests\FeedSearchServiceTests.cs`) — R2
- Directory-Mapping/Sortierung/Dedup, Autodiscovery via Link-Tags und Standardpfade, Redirects, `FeedSearchUnavailableException` bei beidseitigem Ausfall, Timeout, Caller-Cancellation; interner `FakeHttpMessageHandler`-Router (`On`/`OnExact`/`OnExactRedirect`) — direkt wiederverwendbar für `FeedIconService`-Tests.

### `FeedRepositoryTests` / `SettingsRepositoryTests` / `SyncLogRepositoryTests` (`src\Reporter.Tests\*RepositoryTests.cs`) — R2, R3, R4, R7
- Feed: CRUD, `GetAllAsync` sortiert nach Titel, `GetAllWithDetailsAsync_ProjectsNotificationsEnabled`, Kaskaden-Löschung
- Settings: `GetAsync_ReturnsSeededSettings`, `GetAsync_CreatesDefaultRecordWhenMissing`, `SaveAsync_*` (Singleton-Verhalten, neue Felder, Language, NotificationSummary)
- SyncLog: CRUD, `GetAllAsync` nach `StartedAt` absteigend

### `ReporterDbContextTests_Schema` / `_Persistence` (`src\Reporter.Tests\ReporterDbContextTests_*.cs`)
- `EnsureCreatedAsync_CreatesQueryableTables`, `SaveChangesAsync_PersistsFeedWithCategory`, `Items_WithInclude_ReturnsFeedAndCategory` — **Hinweis:** `EnsureCreated`-basiert; Schema-Tests prüfen keine Migrations-SQL.

### `AppCultureTests` / `SettingsValuesTests_AutoMarkRead` / `FeedTitleFallbackTests` — R4/R5-Kontext
- `ResolveCulture_ReturnsExpected` (Theory), `Apply_*`; `IsAutoMarkReadEnabled_EvaluatesMode` (Theory); `GetFallbackTitle_*`, `IsFileNamePlaceholderTitle_*`

### Übrige Suites (nicht direkt betroffen)
`CategoriesViewModelTests`, `KeywordRepositoryTests`, `KeywordMatcherTests`, `KeywordFilterTests_E2E` (3 E2E-Tests: Item nicht in Ungelesen-Liste, keine Benachrichtigung, SyncLog-Meldung), `RetentionCleanupServiceTests`, `BaseViewModelConnectivityTests`, `WebViewNavigationGuardTests`, `ArticleHtmlSanitizerTests`, `ServiceCollectionTests` (DI-Registrierung der Repositories/Services via `AddDbContextFactory`/`ServiceCollection`), `FeedTitleFallbackTests`.

## Hilfsmethoden / Test-Doubles

### `DelegatingItemRepository` (`src\Reporter.Tests\DelegatingItemRepository.cs`) — R4-relevant
- Implementiert alle `IItemRepository`-Member `virtual` und delegiert an ein Inner-Repository; Test-Doubles überschreiben nur benötigte Member. **Bei Signaturänderung von `GetUnreadByDateAsync` (Sortierparameter) muss diese Klasse angepasst werden.**

### `TestDbContextFactory` (`src\Reporter.Tests\TestDbContextFactory.cs`)
- In-Memory-SQLite-`IDbContextFactory<ReporterDbContext>` (`EnsureCreated`), `IDisposable`.

### `TestDataSeeder` (`src\Reporter.Tests\TestDataSeeder.cs`)
- `SeedFeedAsync(TestDbContextFactory)` / `SeedFeedAsync(IFeedRepository, url, notificationsEnabled)` — legt Feeds für Repository-/ViewModel-Tests an.

### `TestSettingsHelper` (`src\Reporter.Tests\TestSettingsHelper.cs`)
- `SaveAsync(...)` — schreibt Settings-Datensätze mit auswählbaren Feldern für Tests.

### `TestFeedXml` / `FakeHttpMessageHandler` (`src\Reporter.Tests\TestFeedXml.cs`, `FakeHttpMessageHandler.cs`)
- RSS/Atom-Testdokumente bzw. URL-Routing-Mock für `FeedSyncService`-/`FeedSearchService`-Tests (`On`, `OnExact`, `OnExactRedirect`) — Grundlage für künftige Favicon-Service-Tests (R2).

### Fakes (`src\Reporter.Tests\Fake*.cs`)
- `FakeFeedSearchService` (`PendingResult`-`TaskCompletionSource` für Stale-Search-Szenarien), `FakeFeedSyncService` (`SyncAllBlocker`), `FakeAutoRefreshService`, `FakeAppThemeService` (zeichnet `ApplyTheme`-Aufrufe auf), `FakeNetworkStatusService` (`RaiseConnectivityChanged`), `FakeNotificationService`, `FakeLocalNotificationService` (Authorization-Status, `ShowAsync`-Aufrufliste).

### `TestWaitHelper` (`src\Reporter.Tests\TestWaitHelper.cs`)
- `WaitUntilAsync(Func<bool>|Func<Task<bool>>, timeout)` — Polling-Helfer für Timer-/Async-Tests (AutoRefreshService).
