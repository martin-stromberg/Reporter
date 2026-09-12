<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-12, ca. 21:34–21:40 +02:00 (W. Europe Standard Time / CEST)
- Branch und Commit-ID: `task/issue-59-efbace56737047f4b10610b83d0e137c-erweiterung-der-hinzufuegenfun` @ `b3aee195f9540e623c597e8065f349d35e8d1999`
- Uncommittete Änderungen im getesteten Stand: nur untracked `docs/features/` (die `requirement.md` dieses Features; die Inventar-Dokumente wurden nach dem Testlauf ergänzt). Vom Testlauf erzeugte Artefakte unter `src/Reporter.Tests/TestResults/` sind per `.gitignore` ignoriert und gehören nicht zum getesteten Stand.
- Testumgebung und Runtime-/SDK-Versionen: Windows 11 (OS 10.0.26200), .NET SDK 10.0.401 (MSBuild 18.9.11), Testruntime .NET 10.0.12, installierte Workloads `android`, `ios`, `maccatalyst`, `maui-windows` (10.0.20); Node.js v24.15.0, npm 11.10.0.
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - .NET/xUnit-Suite `src/Reporter.Tests/Reporter.Tests.csproj` — Befehl aus `.github/workflows/pr-staging-ci.yml` (Job `build-and-test`, Schritt „Test“). CI setzt `IncludeIosTarget=false`/`IncludeAndroidTarget=false` und baut vorher die ganze Solution (`dotnet build Reporter.sln -c Release`); für die Bestandsaufnahme wurde das Testprojekt direkt gebaut (referenziert nur `Reporter.Core`/`Reporter.Data`, kein MAUI-Workload nötig — siehe „Testlücken“).
  - Node-Suite `scripts/*.test.mjs` — `npm test` aus `package.json` (`node --test`), testet die Release-/Manifest-Skripte.
  - Es gibt keine UI-/E2E-Testsuite und keinen Windows-Desktop-/Emulator-Testlauf im Projekt.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results-inventory.trx" --logger "console;verbosity=normal"` (Env: `IncludeIosTarget=false`, `IncludeAndroidTarget=false`) | Repo-Root | 0 | 239 | 0 | 0 | [dotnet-test-release.log](test-results/dotnet-test-release.log), [test-results-inventory.trx](test-results/test-results-inventory.trx), [coverage.cobertura.xml](test-results/coverage.cobertura.xml) |
| 2 | `npm test` (`node --test "scripts/*.test.mjs"`) | Repo-Root | 0 | 36 | 0 | 0 | [npm-test.log](test-results/npm-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — beide Suiten liefen vollständig ohne Fehlschlag (239/239 bzw. 36/36 bestanden).

### Testlücken und Ausführungsprobleme

- Der CI-Schritt „Build“ (`dotnet build Reporter.sln --configuration Release`, inkl. MAUI-App `src/Reporter`) wurde für die Bestandsaufnahme nicht separat ausgeführt; `dotnet test` auf das Testprojekt hat `Reporter.Core` und `Reporter.Data` im Release-Build fehlerfrei kompiliert. Der MAUI-App-Build ist damit nicht Teil dieses Nachweises (Workloads sind installiert: `maui-windows` 10.0.20 u. a.).
- Die Coverage-Sammlung lief mit (`coverage.cobertura.xml` erzeugt); die CI-Coverage-Schwelle (70 % via ReportGenerator) wurde nicht ausgewertet — kein Test, sondern ein Quality-Gate.
- Es existieren keine UI-/E2E-Tests (`SettingsViewModelTests_E2E` ist trotz Namens ein reiner xUnit-Integrationstest ohne UI-Runner). Ein Emulator-/Desktop-Testlauf ist nicht vorgesehen und wurde nicht ausgeführt.
- Übersprungene Tests: keine (beide Läufe melden 0 Skips).

## Testklassen (für die Anforderung relevante)

### `FeedsViewModelTests`
Datei: `src/Reporter.Tests/FeedsViewModelTests.cs` — Integrationstests gegen SQLite-In-Memory (`TestDbContextFactory` + echte `FeedRepository`/`CategoryRepository`), `FakeFeedSyncService` (privat, verschachtelt) und `FakeNetworkStatusService`. `CreateViewModel()` erzeugt das VM ohne `ILocalNotificationService` (optionaler Parameter).

- `RefreshCommand_InvokesSyncService_AndReloadsList` — Einzel-Sync wird aufgerufen, Liste neu geladen
- `RefreshAllCommand_InvokesSyncService_AndReloadsList` — SyncAll wird aufgerufen
- `RefreshCommand_WhenSyncReturnsError_SetsLocalizedSyncErrorMessage` — `SyncErrorMessage = AppResources.SyncStatusError`, Formular-Fehlerkanal bleibt leer
- `RefreshAllCommand_WhenOffline_SkipsSyncWithoutError` — offline kein Sync, kein Fehler (Banner reicht)
- `RefreshAllCommand_WhenIsSyncingPresetByBinding_StillSyncs` — Reentrancy-Guard unabhängig vom gebundenen `IsSyncing`
- `RefreshCommand_WhenIsSyncingPresetByBinding_StillSyncs` — dto. für Einzel-Sync
- `RefreshAllCommand_WhenOffline_ResetsPresetIsSyncing` / `RefreshCommand_WhenOffline_ResetsPresetIsSyncing` — Offline-Pfad setzt `IsSyncing` zurück
- `RefreshCommand_WhenOffline_SkipsSyncWithoutError` — Einzel-Sync offline übersprungen
- `RefreshAllCommand_WhenSyncThrows_SetsLocalizedSyncError` — Exception → generische lokalisierte Meldung
- `ConnectivityChanged_ClearsSyncErrorMessage` — Connectivity-Event leert Sync-Fehler
- `ConnectivityChanged_UpdatesIsOnline` — `IsOnline` folgt dem Event (offline→online)
- `EditCommand_PopulatesFeedNotificationsEnabled` — Edit lädt Notification-Flag
- `SaveCommand_NewFeed_PersistsNotificationsEnabledFalse` — `AddAsync`-Pfad persistiert Flag
- `SaveCommand_ExistingFeed_PersistsNotificationsEnabled` — `UpdateAsync`-Pfad persistiert Flag
- `SaveCommand_ResetsFeedNotificationsEnabled` — `ResetForm` nach Speichern
- `DeleteCommand_ResetsFeedNotificationsEnabled` — `ResetForm` beim Löschen des editierten Feeds

Hinweis: Es existieren keine Tests für die URL-Validierung (`ErrorFeedUrlInvalid`), Titel-Pflicht oder Dublettenprüfung in `SaveAsync` — diese Pfade sind aktuell ungetestet.

### `FeedRepositoryTests`
Datei: `src/Reporter.Tests/FeedRepositoryTests.cs`

- `AddAsync_ThenGetByIdAsync_ReturnsFeed`, `GetAllAsync_ReturnsFeedsOrderedByTitle`, `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesFeed`, `GetByIdAsync_NonExisting_ReturnsNull`, `GetAllWithDetailsAsync_ProjectsNotificationsEnabled`, `DeleteAsync_CascadeDeletesSavedItems`
- Hinweis: kein Test für `GetByUrlAsync` vorhanden.

### `FeedSyncServiceTests`
Datei: `src/Reporter.Tests/FeedSyncServiceTests.cs` — deckt Sync-Erfolg/Fehler/Offline-Verhalten des Sync-Service ab (u. a. `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange`, `SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog`, `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk`, `SyncFeedAsync_Unreachable_KeepsItemsAndLogsError`).

### `ServiceCollectionTests`
Datei: `src/Reporter.Tests/ServiceCollectionTests.cs`

- `AddReporterRepositories_ResolvesAllRepositories` — löst die sechs Repository-Interfaces aus einer manuell aufgebauten `ServiceCollection` auf (spiegelt die `MauiProgram`-Registrierungen, ohne MAUI). Referenzpunkt für die geforderte `IFeedSearchService`-Registrierungsprüfung.

### `BaseViewModelConnectivityTests`
Datei: `src/Reporter.Tests/BaseViewModelConnectivityTests.cs` — `InitConnectivity`/`TrackConnectivity`/`UntrackConnectivity`/`RefreshConnectivityStatus`/`OnConnectivityChanged`-Hook.

## Hilfsmethoden / Test-Doubles

### `TestDbContextFactory`
- `TestDbContextFactory()` — SQLite-In-Memory (`Mode=Memory;Cache=Shared`), `EnsureCreated`; `CreateDbContext()` liefert eigenen Context; `IDisposable`

### `TestDataSeeder`
- `SeedFeedAsync(TestDbContextFactory)` — legt einen Standard-Feed an

### `TestWaitHelper`
- `WaitUntilAsync(Func<bool>, int)` / `WaitUntilAsync(Func<Task<bool>>, int)` — Polling bis Bedingung/Timeout (für async Service-Tests)

### `TestSettingsHelper`
- `SaveAsync(...)` — schreibt `Settings`-Datensatz für Settings-Tests

### `FakeNetworkStatusService`
- `IsOnline` (settable), `RaiseConnectivityChanged()` — steuert Online-Status und feuert `ConnectivityChanged` synchron

### `FakeFeedSyncService` (Datei `FakeFeedSyncService.cs`)
- `SyncAllException`, `SyncAllBlocker`, `SyncAllCallCount` — für AutoRefresh-/Nebenläufigkeits-Tests; `SyncFeedAsync` liefert fest `SyncResult("OK", 0)`
- Hinweis: `FeedsViewModelTests` enthält eine eigene privat verschachtelte `FakeFeedSyncService`-Klasse (`LastFeedId`, `SyncAllCalled`, `NextResult`, `NextException`) — zwei verschiedene Fakes mit gleichem Namen existieren.

### Weitere Fakes (nicht feed-spezifisch)
- `FakeNotificationService` (`Calls`-Liste `NotificationCall`), `FakeLocalNotificationService` (`ShownNotifications`, `IsSupported`, Authorization-Stubs), `FakeAppThemeService` (`AppliedThemes`), `FakeAutoRefreshService` (`AppliedSettings`, Start/Stop-Stubs)

### Nicht vorhanden
- `RssAtlasFeedSearchServiceTests` (Mapping/Sortierung/Fehlerfälle/Timeout mit gemocktem `HttpMessageHandler`) — von der Anforderung gefordert.
- `FakeFeedSearchService` als Test-Double — von der Anforderung gefordert.
