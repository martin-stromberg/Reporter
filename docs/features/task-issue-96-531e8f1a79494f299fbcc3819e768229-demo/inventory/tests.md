<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-16, ca. 20:05–20:08 Uhr +02:00 (Europe/Berlin)
- **Branch und Commit-ID:** `task/issue-96-531e8f1a79494f299fbcc3819e768229-demo`, Commit `b231a7ccd35d86014ef5d05905678238ee74906d` (2026-09-16 19:50:52 +0200)
- **Uncommittete Änderungen im getesteten Stand:** Keine Änderungen an getrackten Dateien. Untracked: `docs/features/task-issue-96-531e8f1a79494f299fbcc3819e768229-demo/` mit `requirement.md` und `todo.md`; während des Laufs wurden die Nachweisdateien unter `inventory/test-results/` angelegt. Produktivcode, Tests und Testkonfiguration sind unverändert.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows (Host `DESKTOP-CM8OBSG`), .NET SDK 10.0.401, Laufzeit .NET 10.0.12, xunit 2.9.3 mit xunit.runner.visualstudio 3.1.4, Node.js v24.15.0. Installierte Workloads: `maui-windows` 10.0.20, `android`, `ios`, `maccatalyst` (SDK 10.0.400).
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `src/Reporter.Tests` (xunit, `net10.0`) — Unit-/Integrationstests; CI-Befehl aus `.github/workflows/pr-staging-ci.yml` (Job `build-and-test`, Schritt „Test"): `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage"`.
  - `src/Reporter.E2ETests` (xunit + FlaUI.UIA3, `net10.0-windows10.0.19041.0`) — FlaUI-Smoke-Tests gegen die reale `Reporter.exe`; projektüblicher Befehl `.\scripts\Run-E2ETests.ps1` (baut Debug `win-x64`, setzt `REPORTER_APP_PATH`).
  - `npm test` (`node --test "scripts/*.test.mjs"`) — Tests der Release-/Update-Skripte (`create-update-manifest`, `release-assets`, `resolve-release-version`), Quelle `package.json`.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit-Tests (Release) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results-inventory.trx" --logger "console;verbosity=normal"` | Repository-Root | 0 | 504 | 0 | 0 | [Log](test-results/dotnet-test-reporter-tests.log), [TRX](test-results/test-results-inventory.trx), [Coverage](test-results/coverage.cobertura.xml) |
| Skript-Tests (Node) | `npm test` (`node --test "scripts/*.test.mjs"`) | Repository-Root | 0 | 36 | 0 | 0 | [Log](test-results/npm-test.log) |

### Nachgewiesene bestehende Testfehler

Keine. Beide ausgeführten Läufe waren vollständig erfolgreich; es existiert kein nachgewiesener vorab fehlschlagender Test.

### Testlücken und Ausführungsprobleme

- **`src/Reporter.E2ETests` (7 Smoke-Tests in `SmokeTests.cs`) wurde nicht ausgeführt.** Grund: Die Suite benötigt einen vorab gebauten `Reporter.exe` (Debug, `net10.0-windows10.0.19041.0`, `win-x64` — im Arbeitsstand nicht vorhanden) und eine interaktive Windows-Desktop-Session, in der die UIA3-Automatisierung Fokus und Eingaben übernimmt (`ReporterAppFixture.cs:104-107` wartet bis zu 2 Minuten auf das Hauptfenster). Der Lauf wurde für diese Bestandsaufnahme nicht angestoßen; über den Ausgangszustand der E2E-Tests liegt daher **kein** Nachweis vor — sie gelten als unbekannt, nicht als fehlerfrei.
- Build-, Setup- oder Infrastrukturfehler traten in den ausgeführten Läufen nicht auf.
- **Relevanz für die Anforderung:** Die E2E-Fixture startet die App mit frischer Temp-DB (`REPORTER_DB_PATH`, `ReporterAppFixture.cs:52-59`) — ein künftiger Demo-Seed würde in jedem E2E-Lauf greifen und der Start-Abruf würde echten Netzverkehr zu `apple.com` auslösen (nur der Feed-Search-Endpunkt ist via `REPORTER_FEEDSEARCH_ENDPOINT` gestubbt; der eigentliche Feed-Download läuft über den realen `HttpClient`, `FeedSyncService.cs:168`). Die Smoke-Tests tolerieren eine bestehende Feed-Karte (`AppStarts_FeedListRenders` akzeptiert Karten *oder* den Leerzustand), wären aber bei Kategorie-Assertions (z. B. `FeedActionSheet_ChangeCategory_IncludingNone`) mit einer zusätzlichen Karte „News" konfrontiert.

## Testklassen

Bestehende Testsuiten mit Bezug zur Anforderung (Auswahl; alle liegen unter `src/Reporter.Tests/`, E2E unter `src/Reporter.E2ETests/`):

### `CategoryRepositoryTests`
- `AddAsync_ThenGetByIdAsync_ReturnsCategory` — Anlegen + Lesen.
- `GetAllAsync_ReturnsCategoriesOrderedByName` — Sortierung.
- `UpdateAsync_PersistsChanges` / `DeleteAsync_RemovesCategory` — Update/Delete.
- `GetByIdAsync_NonExisting_ReturnsNull` / `DeleteAsync_NonExisting_DoesNotThrow` — Negativpfade.
- `GetAllWithFeedCountAsync_ReturnsCountOfFeedsPerCategory` — Feed-Count-Projektion.
- `UpdateAsync_DuplicateName_ThrowsDbUpdateException` — **Unique-Index `categories.name` schlägt zu** (relevant für Kategorie-Kollision „News").
- `DeleteAsync_WithAssignedFeeds_SetsCategoryIdToNull` — `DeleteBehavior.SetNull` auf `feeds.category_id` (Entfernbarkeit der Demo-Kategorie).

### `FeedRepositoryTests`
- `AddAsync_ThenGetByIdAsync_ReturnsFeed`, `GetAllAsync_ReturnsFeedsOrderedByTitle`, `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesFeed`, `GetByIdAsync_NonExisting_ReturnsNull` — CRUD-Grundlagen.
- `GetAllWithDetailsAsync_ProjectsNotificationsEnabled` / `..._ProjectsFaviconUrl` / `..._ProjectsLastError` — Anzeige-Projektion (Feed-Karte).
- `UpdateAsync_PersistsFaviconUrl` / `..._PersistsLastError` — neue Spalten.
- `DeleteAsync_CascadeDeletesSavedItems` — Kaskade `feeds` → `items`.

### `CategoriesViewModelTests`
- `LoadCommand_PopulatesCategoriesWithFeedCounts`, `SaveCommand_WithEmptyName_SetsValidationErrorAndDoesNotAdd`, `SaveCommand_WithDuplicateName_SetsValidationError` (OrdinalIgnoreCase-Duplikatprüfung), `SaveCommand_AddsNewCategory`, `SaveCommand_UpdatesExistingCategory`, `DeleteCommand_RemovesCategoryAndReloadsList`.

### `FeedsViewModelTests` (~50 Tests, Datei `FeedsViewModelTests.cs`)
- Private Hilfsmethode `SeedFeedAsync(title, url, notificationsEnabled)` (Zeile 52–63) legt Feeds über `FeedRepository` an — Vorbild für Seed-Tests.
- U. a. Dublettenprüfung (`AddAsync`/Direct-Add auf bestehende URL → `ErrorFeedDuplicate`, Zeilen 411/434/797/823/1106), Edit/Save, Rename, `ChangeFeedCategory` inkl. `Guid.Empty`-Pseudo-Eintrag, Benachrichtigungsschalter, Sync-Fehlerkanal.

### `AutoRefreshServiceTests` (+ `AutoRefreshServiceTests_DebugLog`)
- `StartAsync_StartupRefreshEnabled_SyncsImmediately` — **Start-Abruf synchronisiert sofort** (der Mechanismus, der den Demo-Feed beim ersten Start abholt).
- `StartAsync_StartupRefreshDisabled_DoesNotSyncImmediately`, `StartAsync_Offline_SkipsStartupSync`, `StartAsync_StartupSyncThrows_StartStillCompletes` — Gating und Fehlerisolierung des Start-Abrufs.
- Timer-Loop-, Clamp-, Offline-/Online- und Concurrency-Tests.

### `ServiceCollectionTests`
- `AddReporterRepositories_ResolvesAllRepositories` — alle Repositories auflösbar.
- `AddReporterServices_ResolvesFeedSearchService` / `..._FeedSyncService` / `..._DebugServices` / `..._AutoRefreshService` / `..._ScheduledSyncRunner` — DI-Auflösung spiegelt `MauiProgram`-Registrierung; ein neuer Seed-Service würde hier ergänzt (Fakes nach Muster `FakeAutoRefreshService`).

### `SettingsRepositoryTests`
- `GetAsync_ReturnsSeededSettings` — belegt den **`HasData`-Seed der `settings`-Zeile** (der einzige bestehende Seed; Referenz dafür, dass deklaratives Seeden auch in `EnsureCreated`-Test-DBs wirkt).
- `GetAsync_CreatesDefaultRecordWhenMissing`, `SaveAsync_*` (Persistenz der einzelnen Felder), `GetAsync_AlwaysReturnsSingleRecord`.

### `ReporterDbContextTests_Schema` / `ReporterDbContextTests_Persistence`
- `EnsureCreatedAsync_CreatesQueryableTables`, `DebugLogEntries_MappedToExpectedTable` — Schema-Mapping.
- `SaveChangesAsync_PersistsFeedWithCategory` — Feed↔Kategorie-Roundtrip; `Items_WithInclude_ReturnsFeedAndCategory`, `Settings_DebugCollectionEnabled_PersistRoundtrip`, `DebugLogEntry_PersistRoundtrip`, `Feed_PersistRoundtrip_LastError`.

### `SmokeTests` (E2E, `src/Reporter.E2ETests/`, Collection `E2E`, seriell)
- `AppStarts_FeedListRenders` — Feeds-Tab rendert Karten oder Leerzustand (ein Demo-Seed würde hier eine Karte erzeugen).
- `AddButton_OpensSheet_FocusesUrlEntry`, `DirectAdd_FeedAppearsInListAndDatabase` (inkl. `FeedDbAssertions.FeedExistsAsync` gegen die isolierte DB), `FeedActionSheet_Rename_UpdatesTitle`, `FeedActionSheet_ChangeCategory_IncludingNone`, `Search_SubscribesResult_PersistsFeed`, `Search_SiteUrl_DiscoversFeedViaLinkTag`.

## Hilfsmethoden

### `TestDbContextFactory` (`src/Reporter.Tests/TestDbContextFactory.cs`)
- Konstruktor öffnet Shared-In-Memory-SQLite (`Mode=Memory;Cache=Shared;Default Timeout=30`) mit Keep-Alive-Connection und ruft **`EnsureCreated()`** (Zeile 40) — keine Migrationen; ein `HasData`-Seed auf `feeds`/`categories` würde alle Repository-Tests vorbelegen.
- `CreateDbContext()` — neuer `ReporterDbContext` pro Aufruf; `Dispose()` schließt die DB.

### `TestDataSeeder` (`src/Reporter.Tests/TestDataSeeder.cs`)
- `SeedFeedAsync(TestDbContextFactory)` — legt Entity direkt über den Context an (`https://example.com/feed`, „Example Feed").
- `SeedFeedAsync(IFeedRepository, url, notificationsEnabled)` — legt `Feed`-Modell über das Repository an („Test Feed").

### `TestSettingsHelper` (`src/Reporter.Tests/TestSettingsHelper.cs`)
- `SaveAsync(ISettingsRepository, …)` — Settings laden und mit Overrides zurückschreiben (u. a. `refreshOnStartupEnabled`, `notificationsEnabled`, `notificationSummaryEnabled`).

### `TestWaitHelper` (`src/Reporter.Tests/TestWaitHelper.cs`)
- `WaitUntilAsync` — Polling-Helfer für asynchrone Nebenwirkungen (z. B. fire-and-forget `SyncAllAsync`).

### Fake-Implementierungen (`src/Reporter.Tests/`)
- `FakeAutoRefreshService` — zählt `StartAsync`/`StopAsync`, sammelt `AppliedSettings` (Muster für ein ggf. benötigtes `IDemoContentService`-Fake).
- `FakeFeedSyncService`, `FakeFeedSearchService`, `FakeFeedIconService`, `FakeNetworkStatusService` (`IsOnline` steuerbar), `FakeNotificationService`, `FakeLocalNotificationService`, `FakeBackgroundRefreshService`, `FakeDebugLogService`, `FakeEmailService`, `FakeDeviceInfoProvider`, `DelegatingItemRepository`, `FakeHttpMessageHandler`.

### E2E-Hilfen (`src/Reporter.E2ETests/`)
- `ReporterAppFixture` — startet `StubFeedServer`, legt Temp-DB an, startet `Reporter.exe` mit `REPORTER_FEEDSEARCH_ENDPOINT` + `REPORTER_DB_PATH`, attach via FlaUI/UIA3, gibt Prozess-Exit aus.
- `StubFeedServer` — in-prozessiger HTTP-Stub für Feed-Search-Verzeichnis, Stub-Feeds und HTML-Discovery (`Fixtures/*.xml|*.html`).
- `FeedDbAssertions` — `FeedExistsAsync(databasePath, url)`: Read-only-Polling auf die `feeds`-Tabelle der isolierten DB.
- `UiRetry` — Retry-/Wait-Helfer für UIA-Elemente.
