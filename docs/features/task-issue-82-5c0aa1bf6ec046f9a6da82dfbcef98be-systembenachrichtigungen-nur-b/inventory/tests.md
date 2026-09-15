<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-15 ~07:26 (+02:00, lokale Windows-Zeit)
- Branch und Commit-ID: `task/issue-82-5c0aa1bf6ec046f9a6da82dfbcef98be-systembenachrichtigungen-nur-b` @ `ccc6fce589bfa8a2e43bb3d81af07dc8783f85fb`
- Uncommittete Änderungen im getesteten Stand: keine Codeänderungen; nur das untracked Verzeichnis `docs/features/task-issue-82-5c0aa1bf6ec046f9a6da82dfbcef98be-systembenachrichtigungen-nur-b/` (Anforderungsdokument `requirement.md`)
- Testumgebung und Runtime-/SDK-Versionen: Windows; .NET SDK 10.0.401 (Test-Runtime .NET 10.0.12, xUnit.net VSTest Adapter 3.1.4); Node.js v24.15.0
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - **xUnit-Suite** `src/Reporter.Tests/Reporter.Tests.csproj` (net10.0; referenziert `Reporter.Core` + `Reporter.Data`, kein MAUI): Befehl aus `.github/workflows/staging-ci.yml` (`build-and-test`-Job) — `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;..." --logger "console;verbosity=normal"`
  - **Node-Skripttests** `scripts/*.test.mjs`: `npm test` = `node --test "scripts/*.test.mjs"` (`package.json`); Release-Tooling-Tests, fachlich nicht betroffen, aber Teil des Projektbestands
  - **Statische Checks** (kein Testlauf, aber CI-relevant): `scripts/Run-StaticChecks.ps1` (`dotnet format --verify-no-changes`, Lizenz-Header-Check, `dotnet build -p:TreatWarningsAsErrors=true`) — nicht ausgeführt, kein Testnachweis
  - Die MAUI-App (`src/Reporter/Reporter.csproj`, Targets `net10.0-windows10.0.19041.0` + `net10.0-ios`) wurde nicht gebaut/getestet; iOS-Plattformcode (`AppDelegate`, `NotificationDelegate`, `Info.plist`) ist per `dotnet test` nicht erreichbar.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| dotnet-baseline | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results-inventory-baseline.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 481 | 0 | 0 | [Log](test-results/dotnet-test-baseline.log), [TRX](test-results/test-results-inventory-baseline.trx), [Coverage](test-results/coverage-inventory-baseline.cobertura.xml) |
| npm-baseline | `npm test` (= `node --test "scripts/*.test.mjs"`) | Repo-Root | 0 | 36 | 0 | 0 | [Log](test-results/npm-test-baseline.log) |

### Nachgewiesene bestehende Testfehler

Keine — im Ausgangslauf sind alle 481 .NET-Tests und alle 36 Node-Tests bestanden. Es wurden keine fehlgeschlagenen Tests nachgewiesen.

### Testlücken und Ausführungsprobleme

- Plattformcode (`src/Reporter/Platforms/**`, `LocalNotificationService`, `App.xaml.cs`, `MauiProgram.cs`, XAML-Views) ist durch die `Reporter.Tests`-Suite nicht abgedeckt — `Reporter.Tests` referenziert nur `Reporter.Core` und `Reporter.Data`.
- Der `dotnet test`-Lauf baut/testet nur die Testprojekt-Kette; ein `dotnet build Reporter.sln` inklusive iOS-Target wurde nicht ausgeführt (CI baut mit `IncludeIosTarget=false` auf Windows).
- Keine übersprungenen, deaktivierten oder abgebrochenen Tests im Lauf festgestellt.

## Testklassen

### `NotificationServiceTests`
Datei: `src/Reporter.Tests/NotificationServiceTests.cs` — Testet die `NotificationService`-Entscheidungskette gegen `FakeLocalNotificationService` + reale Repositories (`TestDbContextFactory`).

- `NotifyNewItemsAsync_FeedDisabled_SendsNothing` — `Feed.NotificationsEnabled = false` unterdrückt.
- `NotifyNewItemsAsync_GlobalDisabled_SendsNothing` — `Settings.NotificationsEnabled = false` unterdrückt.
- `NotifyNewItemsAsync_WithinQuietHours_SendsNothing` (Theory 23/6 Uhr) — Ruhezeit unterdrückt.
- `NotifyNewItemsAsync_OutsideQuietHours_Sends` (Theory 12/21 Uhr) — außerhalb der Ruhezeit wird gesendet.
- `NotifyNewItemsAsync_QuietHoursStartEqualsEnd_Sends` — leeres Intervall unterdrückt nicht.
- `NotifyNewItemsAsync_OnlyOneQuietHoursBound_Sends` — einseitige Grenze deaktiviert Ruhezeit.
- `NotifyNewItemsAsync_KeywordMatch_SkipsItem` — Keyword-Treffer (Titel oder ContentHtml) werden übersprungen.
- `NotifyNewItemsAsync_SendsPerItem_WithItemIdAsIdentifier` — Einzelmodus: eine Mitteilung pro Item, Identifier = `item.Id`, `userInfo["itemId"]`.
- `NotifyNewItemsAsync_SendsItemLink_InUserInfo` — `userInfo` enthält `itemId` und `link`.
- `NotifyNewItemsAsync_SummaryEnabled_SendsSingleSummary` — Summary-Modus: eine Mitteilung, Identifier `"{feedId}-…"`, `userInfo["feedId"]`.
- `NotifyNewItemsAsync_SummaryEnabled_SameItems_SameIdentifier` — stabiler Summary-Identifier.
- `NotifyNewItemsAsync_SummaryEnabled_KeywordFiltered_ExcludedFromSummary` — Keyword-Treffer fehlen in Zählung/Identifier.

### `FeedSyncServiceTests`
Datei: `src/Reporter.Tests/FeedSyncServiceTests.cs` — Integrationstests mit `FakeHttpMessageHandler`/`TestFeedXml`; Benachrichtigungsbezug:

- `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems` — neue Items lösen pro Item eine Mitteilung aus (echte `NotificationService`-Kette).
- `SyncFeedAsync_SummaryMode_SendsSingleSummaryNotification` — Summary-Modus im Sync-Pfad.
- `SyncFeedAsync_NoNewItems_DoesNotNotify` — Zweit-Sync ohne neue Items benachrichtigt nicht.
- `SyncFeedAsync_FeedDisabled_NoNotifications` — Feed-Schalter unterdrückt.
- `SyncFeedAsync_NotificationThrows_SyncStillSucceeds` — `FakeNotificationService` mit Exception; Sync-Ergebnis bleibt `Ok`, Aufruf wird aufgezeichnet.
- `SyncFeedAsync_KeywordFiltered_NotNotified` — gefilterte Items erzeugen keine Mitteilung.
- Übrige Tests: Dedup, Health (`Warning`/`Error`), Offline-Guards, Keyword-Filter, Titel-Auflösung, Favicon-Backfill, `FeedSyncErrorKind`-Persistenz.

### `FeedSyncServiceTests_DebugLog`
Datei: `src/Reporter.Tests/FeedSyncServiceTests_DebugLog.cs`

- `SyncFeedAsync_Failure_LogsErrorEntry` — Sync-Fehler erzeugt `DebugLogCategory.Sync`-Eintrag (Error).
- `SyncFeedAsync_Failure_WithoutDebugLogService_StillReportsError` — optionale `IDebugLogService`-Abhängigkeit (`null` möglich).

### `AutoRefreshServiceTests`
Datei: `src/Reporter.Tests/AutoRefreshServiceTests.cs` — nutzt `FakeFeedSyncService`, `FakeNetworkStatusService`, `FakeTimeProvider`, `TestWaitHelper`.

- `StartAsync_InvokesSyncAfterInterval` — Timer-Tick ruft `SyncAllAsync`.
- `ApplySettings_Disabled_Stops` / `ApplySettings_ChangesInterval` — Timer-Steuerung über Settings.
- `SyncThrows_LoopContinues` — Fehler pro Tick stoppen den Loop nicht.
- `CoalescedTicks_DoNotStartParallelSync` — sequenzieller Sync, keine Überlappung.
- `Tick_WhenOffline_SkipsSyncAll` / `Tick_WhenBackOnline_ResumesSync` — Offline-Guard.
- `InvalidInterval_Clamped` (Theory 0→1, 99999→1440) — Intervall-Clamping.
- `StartStop_RepeatedCycles_RestartsCleanly` — wiederholte Start/Stop-Zyklen.
- `StartAsync_StartupRefreshEnabled_SyncsImmediately` / `StartAsync_StartupRefreshDisabled_DoesNotSyncImmediately` / `StartAsync_Offline_SkipsStartupSync` / `StartAsync_StartupSyncThrows_StartStillCompletes` — Start-Abruf-Semantik.
- `ApplySettings_Concurrent_LeavesSingleActiveLoop` — Nebenläufigkeits-Guard.

### `AutoRefreshServiceTests_DebugLog`
Datei: `src/Reporter.Tests/AutoRefreshServiceTests_DebugLog.cs` — Debug-Log-Einträge für Timer-/Start-Sync-Fehler.

### `FeedsViewModelTests`
Datei: `src/Reporter.Tests/FeedsViewModelTests.cs` — u. a. Refresh-Befehle und `FeedNotificationsEnabled`-UI-Enablement (`NotificationsSupported` über `ILocalNotificationService.IsSupported`); enthält eine lokale `IFeedSyncService`-Fake-Klasse (ca. Zeilen 1761–1791) mit `SyncFeedAsync`/`SyncAllAsync`-Aufzeichnung.

### `UnreadViewModelTests`
Datei: `src/Reporter.Tests/UnreadViewModelTests.cs` — `RefreshCommand_InvokesSyncService`, `RefreshCommand_WhenSyncThrows_SetsLocalizedSyncError`, `RefreshCommand_WhenBackOnline_InvokesSyncService`.

### `SettingsViewModelTests_Persist` / `SettingsViewModelTests_Load`
Dateien: `src/Reporter.Tests/SettingsViewModelTests_Persist.cs`, `SettingsViewModelTests_Load.cs` — u. a. `NotificationsEnabled_TurnedOn_UnsupportedPlatform_SkipsRequest`, `NotificationsEnabled_TurnedOff_ClearsNotificationPermissionDenied`/`…NotDetermined`, Berechtigungsstatus-Flags (`Denied`/`NotDetermined`), `AutoRefresh`-Persistierung → `ApplySettingsAsync` (via `FakeAutoRefreshService`).

### `ServiceCollectionTests`
Datei: `src/Reporter.Tests/ServiceCollectionTests.cs`

- `AddReporterRepositories_ResolvesAllRepositories` — Repository-Registrierungen.
- `AddReporterServices_ResolvesFeedSearchService` — `IFeedSearchService`.
- `AddReporterServices_ResolvesFeedSyncService` — `IFeedSyncService` inkl. `INotificationService`/`ILocalNotificationService`-Fake-Registrierung.
- `AddReporterServices_ResolvesDebugServices` — Debug-Services.

## Hilfsmethoden

### `FakeLocalNotificationService`
Datei: `src/Reporter.Tests/FakeLocalNotificationService.cs` — `ILocalNotificationService`-Fake; zeichnet `ShowAsync`-Aufrufe als `ShownNotification`-Records (Title/Body/Identifier/UserInfo) auf, konfigurierbare `AuthorizationResult`/`AuthorizationStatus`/`IsSupported`, `RequestAuthorizationCallCount`.

### `FakeNotificationService`
Datei: `src/Reporter.Tests/FakeNotificationService.cs` — `INotificationService`-Fake; `Calls` (Record `NotificationCall(Feed, Items)`), optional `Exception`.

### `FakeFeedSyncService`
Datei: `src/Reporter.Tests/FakeFeedSyncService.cs` — `IFeedSyncService`-Fake; `SyncAllCallCount`, `SyncAllException`, `SyncAllBlocker` (TaskCompletionSource für Overlap-Tests).

### `FakeAutoRefreshService`
Datei: `src/Reporter.Tests/FakeAutoRefreshService.cs` — `IAutoRefreshService`-Fake; `AppliedSettings`, `StartCallCount`, `StopCallCount`.

### `FakeNetworkStatusService` / `FakeDebugLogService` / `FakeHttpMessageHandler` / `FakeFeedIconService`
Dateien: `src/Reporter.Tests/FakeNetworkStatusService.cs`, `FakeDebugLogService.cs`, `FakeHttpMessageHandler.cs`, `FakeFeedIconService.cs` — schaltbarer Online-Status, Log-Aufzeichnung, konfigurierbarer HTTP-Handler bzw. Favicon-Ergebnisse.

### `TestDbContextFactory` / `TestDataSeeder` / `TestSettingsHelper` / `TestWaitHelper` / `TestFeedXml`
Dateien: `src/Reporter.Tests/TestDbContextFactory.cs`, `TestDataSeeder.cs`, `TestSettingsHelper.cs`, `TestWaitHelper.cs`, `TestFeedXml.cs` — In-Memory-SQLite-Kontext; Feed-Seeding (`SeedFeedAsync`, `notificationsEnabled`-Parameter); Settings-Overrides inkl. `notificationsEnabled`, `notificationSummaryEnabled`, `quietHours*`, `refreshOnStartupEnabled`; Polling-Helper für async Seiteneffekte; RSS/Atom-XML-Generatoren.
