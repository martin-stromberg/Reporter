<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-14, Testlauf ca. 06:59–07:00 MESZ (UTC+02:00, „W. Europe Standard Time")
- Branch und Commit-ID: `task/issue-81-32b9db201d684b57a7bb5e21150b1563-debuginformationen-sammeln-und` @ `e7830d06e3b5e2c85f617024b45e10bbc8d9d6c3`
- Uncommittete Änderungen im getesteten Stand: ausschließlich untracked `docs/features/task-issue-81-…/` (Anforderungs-/Bestandsaufnahme-Dokumente) — kein Produktivcode, keine Tests und keine Konfiguration verändert (`git status --short` zeigte nur `?? docs/features/task-issue-81-…/`).
- Testumgebung und Runtime-/SDK-Versionen: Windows, .NET SDK 10.0.401, Test-Runtime .NET 10.0.12, xunit 2.9.3 mit xUnit.net VSTest Adapter 3.1.4, coverlet.collector 6.0.4 (Coverage über `src/Reporter.Tests/coverlet.runsettings`, Migrations ausgeschlossen).
- Ermittelte Testsuiten und Quellen der Testbefehle: Einzige Testsuite ist `src/Reporter.Tests/Reporter.Tests.csproj` (net10.0; referenziert `Reporter.Core` + `Reporter.Data`, nicht das MAUI-Projekt `Reporter`). Befehlsquelle: CI-Job `build-and-test` in `.github/workflows/pr-staging-ci.yml` (Zeilen 123–129). Ergänzende Quality-Gates ohne Testcharakter: `static-checks`-Job ebendort bzw. `scripts/Run-StaticChecks.ps1` (Format, Lizenzheader, Security-Scan, Release-Build mit `TreatWarningsAsErrors`) — nicht Teil dieses Testlaufs. Abweichung zum CI-Befehl: `--no-build` entfällt, da kein vorheriger Build existierte (Restore+Build waren im Lauf enthalten); `--configuration Release` und Coverlet-Settings wie in CI.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline (2026-09-14, MESZ) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results-baseline.trx" --logger "console;verbosity=normal" --results-directory docs/features/task-issue-81-32b9db201d684b57a7bb5e21150b1563-debuginformationen-sammeln-und/inventory/test-results` | Repo-Root | 0 | 409 | 0 | 0 | [TRX](test-results/test-results-baseline.trx), [Konsolen-Log](test-results/dotnet-test-baseline.log), [Coverage](test-results/7673af46-3154-4064-a769-dc4450673d0d/coverage.cobertura.xml) |

### Nachgewiesene bestehende Testfehler

Keine — alle 409 entdeckten Tests waren erfolgreich; es gibt keinen nachgewiesenen Fehlschlag im Ausgangslauf.

### Testlücken und Ausführungsprobleme

- Das MAUI-App-Projekt `src/Reporter/Reporter.csproj` (net10.0-windows/ios) wird von `dotnet test` nicht gebaut und enthält keinerlei ausführbaren Testcode — Views, `MauiProgram` und die Plattform-Services (`LocalNotificationService`, `NetworkStatusService`, `AppThemeService`) haben keine automatisierte Abdeckung. UI-Verifikation ist manuell (vgl. `AGENTS.md`, `test-results.md`).
- Es existieren keine automatisierten UI-Tests; der E-Mail-Compose-Pfad (`Email.ComposeAsync`) ist prinzipiell nur manuell auf einem Gerät mit eingerichtetem Mail-Client verifizierbar.
- Für die Anforderung fehlen noch: `FakeEmailService`, `FakeDeviceInfoProvider` und `DebugReportServiceTests`; die `ServiceCollectionTests` decken bislang nur Repositories und `FeedSearchService`/`FeedSyncService`-Auflösung ab (Gateways werden über Fakes registriert).
- Keine übersprungenen, deaktivierten oder abgebrochenen Tests; keine Build-/Setup-/Infrastrukturfehler im Baseline-Lauf.

## Testklassen

### `SettingsViewModelTests_Load`
Datei: `src/Reporter.Tests/SettingsViewModelTests_Load.cs` — nutzt `TestDbContextFactory`, echte `SettingsRepository`/`KeywordRepository`, `FakeAutoRefreshService`, `FakeAppThemeService`, optional `FakeLocalNotificationService`.

- `Load_PopulatesAllOptions` — vollständiges Mapping persistenter Settings auf VM-Properties
- `Load_InvalidPersistedValues_UsesFallbacks` — ungültige Werte → Defaults
- `Load_PopulatesNotificationSummaryEnabled` — Summary-Flag wird geladen
- `Load_PermissionDenied_SetsNotificationPermissionDenied` / `Load_PermissionGranted_ClearsNotificationPermissionDenied` / `Load_PermissionNotDetermined_SetsNotificationPermissionNotDetermined` — Autorisierungsstatus-Mapping (Muster für `DebugEmailSupported`-Fehlerpfad)
- `Load_NotificationsSupported_ReflectsPlatformSupport` — `IsSupported`-Gateway-Prop auf VM-Property (Muster für Mail-Client-Verfügbarkeit)
- `Load_NotificationsDisabled_HidesNotificationPermissionDenied`, `Load_WithoutQuietHours_QuietHoursDisabled`
- `ThemeOptions_ExposePersistedValues`, `LanguageOptions_ExposePersistedValues`, `Load_InvalidLanguage_UsesSystemFallback`, `Load_PopulatesSelectedLanguage`, `Load_PopulatesRefreshOnStartup`, `Load_PopulatesSelectedSortOrder`, `Load_ResetsRestartHint`

### `SettingsViewModelTests_Persist`
Datei: `src/Reporter.Tests/SettingsViewModelTests_Persist.cs`

- `PropertyChange_PersistsImmediately` — `PersistOnChange`-Muster (Setter → `SaveAsync`)
- `RetentionDays_OutOfRange_Clamped`, `RetentionDays_ChangeWithoutDragCompleted_PersistsAfterDebounce`, `RetentionDays_RapidChanges_PersistOnlyLastValue`, `RetentionDays_DragCompleted_PersistsImmediatelyAndCancelsDebounce` — Debounce-Persist
- `NotificationSummaryEnabled_Change_Persists` — bool-Toggle-Persist (direktes Muster für `DebugCollectionEnabled`)
- `NotificationsEnabled_TurnedOn_RequestsAuthorization`, `_Denied_RaisesNotificationAuthorizationDenied`, `_UnsupportedPlatform_SkipsRequest`, `_TurnedOff_ClearsNotificationPermissionDenied`/`…NotDetermined`, `RequestNotificationPermission_NotDetermined_RequestsAndClearsHint` — Plattform-Gateway-Interaktion inkl. Event und `IsSupported == false`-Pfad
- `ThemeChange_AppliesTheme`, `SelectedLanguage_Change_Persists`, `LanguageChange_SetsRestartHint`, `LanguageReverted_ClearsRestartHint`, `OtherChange_DoesNotLoseLanguage`, `RefreshOnStartup_Change_Persists`, `SelectedSortOrder_Change_Persists`, `AutoRefreshChange_AppliesSettings`
- `QuietHoursEnabled_*` (5 Tests) — Toggle-Persist mit null-Werten
- `Persist_QueuedBehindRunningSave_AppliesThemeOnce` — Serialisierung via `_persistLock`

### `SettingsViewModelTests_E2E`
Datei: `src/Reporter.Tests/SettingsViewModelTests_E2E.cs` — VM ↔ echtes Repository ↔ In-Memory-SQLite Roundtrips.

- `E2E_ChangeSettings_PersistRoundtrip`, `E2E_NotificationSummary_PersistRoundtrip`, `E2E_ChangeLanguage_PersistRoundtrip`, `E2E_RefreshOnStartup_PersistRoundtrip`, `E2E_SortOrder_PersistRoundtrip` (Muster für einen `E2E_DebugCollection_PersistRoundtrip`)
- `E2E_KeywordAddRemove_Persists`, `E2E_KeywordEmpty_Rejected`, `E2E_KeywordDuplicate_Rejected`

### `SettingsViewModelTests_Keywords`
Datei: `src/Reporter.Tests/SettingsViewModelTests_Keywords.cs`

- `AddKeyword_Valid_AddsToRepository`, `AddKeyword_DuplicateCaseInsensitive_ShowsError`, `AddKeyword_Empty_ShowsError`, `AddKeyword_TooLong_ShowsError`, `RemoveKeyword_DeletesFromRepository`

### `SettingsRepositoryTests`
Datei: `src/Reporter.Tests/SettingsRepositoryTests.cs`

- `GetAsync_ReturnsSeededSettings`, `GetAsync_CreatesDefaultRecordWhenMissing`, `GetAsync_AlwaysReturnsSingleRecord`
- `SaveAsync_OverwritesSameRecord`, `SaveAsync_IgnoresDifferentIdAndUpdatesDefault`, `SaveAsync_PersistsNewFields`, `SaveAsync_PersistsStartupRefreshAndSortOrder`, `SaveAsync_PersistsLanguage`, `SaveAsync_PersistsNotificationSummaryEnabled` (Muster für `SaveAsync_PersistsDebugCollectionEnabled`)

### `SyncLogRepositoryTests`
Datei: `src/Reporter.Tests/SyncLogRepositoryTests.cs`

- `AddAsync_ThenGetByIdAsync_ReturnsSyncLog`
- `GetAllAsync_ReturnsSyncLogsOrderedByStartedAtDescending` — belegt die Sortierreihenfolge der Report-Datenquelle
- `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesSyncLog`, `GetByIdAsync_NonExisting_ReturnsNull`

### `ReporterDbContextTests_Schema` / `ReporterDbContextTests_Persistence`
Dateien: `src/Reporter.Tests/ReporterDbContextTests_Schema.cs`, `src/Reporter.Tests/ReporterDbContextTests_Persistence.cs`

- `EnsureCreatedAsync_CreatesQueryableTables` — Schema-Smoke-Test (Tabellen abfragbar)
- `SaveChangesAsync_PersistsFeedWithCategory`, `Items_WithInclude_ReturnsFeedAndCategory`

### `ServiceCollectionTests`
Datei: `src/Reporter.Tests/ServiceCollectionTests.cs`

- `AddReporterRepositories_ResolvesAllRepositories` — alle `I*Repository` auflösbar
- `AddReporterServices_ResolvesFeedSearchService`, `AddReporterServices_ResolvesFeedSyncService` — spiegeln die `MauiProgram`-Registrierungen inkl. Gateway-Fakes (Muster für `IEmailService`/`IDeviceInfoProvider`/`DebugReportService`-Registrierung)

### `NotificationServiceTests`
Datei: `src/Reporter.Tests/NotificationServiceTests.cs` — Referenz für die Teststruktur eines Orchestrierungs-Service mit Gateway-Fake (`FakeLocalNotificationService`) und echtem `SettingsRepository`.

- `NotifyNewItemsAsync_FeedDisabled_SendsNothing`, `_GlobalDisabled_SendsNothing`, `_WithinQuietHours_SendsNothing`, `_OutsideQuietHours_Sends`, `_QuietHoursStartEqualsEnd_Sends`, `_OnlyOneQuietHoursBound_Sends`, `_KeywordMatch_SkipsItem`, `_SendsPerItem_WithItemIdAsIdentifier`, `_SendsItemLink_InUserInfo`, `_SummaryEnabled_SendsSingleSummary`, `_SummaryEnabled_SameItems_SameIdentifier`, `_SummaryEnabled_KeywordFiltered_ExcludedFromSummary`

### Weitere Testklassen im Projekt (nicht feature-relevant, im Baseline-Lauf enthalten)

`AppCultureTests`, `ArticleHtmlSanitizerTests`, `AutoRefreshServiceTests`, `BaseViewModelConnectivityTests`, `CategoriesViewModelTests`, `CategoryRepositoryTests`, `FeedIconServiceTests`, `FeedListItemTests`, `FeedRepositoryTests`, `FeedSearchServiceTests`, `FeedsViewModelTests`, `FeedSyncServiceTests`, `FeedTitleFallbackTests`, `ItemRepositoryTests`, `KeywordFilterTests_E2E`, `KeywordMatcherTests`, `KeywordRepositoryTests`, `LaterViewModelTests`, `ReadingTimeEstimatorTests`, `ReporterDbContextFactoryTests`, `RetentionCleanupServiceTests`, `SettingsValuesTests_AutoMarkRead`, `UnreadViewModelTests`, `WebViewNavigationGuardTests`.

## Hilfsmethoden

### `TestDbContextFactory` (`src/Reporter.Tests/TestDbContextFactory.cs`)
- Konstruktor — baut shared In-Memory-SQLite (`Mode=Memory;Cache=Shared`) mit Keep-Alive-Connection und `EnsureCreated()`-Schema
- `CreateDbContext()` — neuer `ReporterDbContext` pro Aufruf
- `Dispose()` — schließt Keep-Alive-Connection

### `TestSettingsHelper` (`src/Reporter.Tests/TestSettingsHelper.cs`)
- `SaveAsync(repository, notificationsEnabled, notificationSummaryEnabled, quietHoursStart, quietHoursEnd, language, refreshOnStartupEnabled, unreadSortOrder)` — lädt den Singleton und schreibt ihn mit selektiven Overrides zurück (bei neuem `required`-Feld `DebugCollectionEnabled` ist dieser Helper zu erweitern)

### `TestDataSeeder` (`src/Reporter.Tests/TestDataSeeder.cs`)
- `SeedFeedAsync(TestDbContextFactory)` — Feed-Entity direkt in den Context
- `SeedFeedAsync(IFeedRepository, url, notificationsEnabled)` — Feed über das Repository

### `TestWaitHelper` (`src/Reporter.Tests/TestWaitHelper.cs`)
- `WaitUntilAsync(Func<bool>/Func<Task<bool>>, timeoutMilliseconds)` — Polling für Fire-and-forget-Persist des VMs

### Fakes (`src/Reporter.Tests/`)
- `FakeLocalNotificationService` — `IsSupported`, `AuthorizationResult`, `AuthorizationStatus`, `RequestAuthorizationCallCount`, `ShownNotifications` (record `ShownNotification`); Vorlage für `FakeEmailService`
- `FakeNetworkStatusService` — `IsOnline` settable + `RaiseConnectivityChanged()`; Vorlage für `FakeDeviceInfoProvider`
- `FakeAppThemeService`, `FakeAutoRefreshService`, `FakeNotificationService`, `FakeFeedSyncService`, `FakeFeedIconService`, `FakeFeedSearchService`, `FakeHttpMessageHandler`, `DelegatingItemRepository` — weitere handgeschriebene Test-Doubles
