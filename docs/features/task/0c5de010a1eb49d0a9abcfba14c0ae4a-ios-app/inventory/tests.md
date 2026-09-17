<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme — Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-17 18:18 +02:00 (`W. Europe Standard Time`, Sommerzeit/CEST)
- Branch und Commit-ID: `task/0c5de010a1eb49d0a9abcfba14c0ae4a-ios-app` @ `393f2570bd1fb9b03bc99d39a8c3bc4c276c6048`
- Uncommittete Änderungen im getesteten Stand: ausschließlich untracked Verzeichnis `docs/features/` (die übersetzte Anforderung `requirement.md` sowie die im Rahmen dieser Bestandsaufnahme angelegten `inventory`-Dateien und Testnachweise). Keine Änderungen an Produktivcode, Tests oder Konfiguration.
- Testumgebung und Runtime-/SDK-Versionen: Windows-Entwicklungsrechner (Git Bash), .NET SDK `10.0.401` (einzige installierte SDK-Version, `C:\Program Files\dotnet\sdk`), Node.js `v24.15.0`, npm `11.10.0`. Der `dotnet test`-Lauf kompilierte dabei alle Solution-Projekte inkl. der Targets `net10.0-ios` (`iossimulator-x64`) und `net10.0-windows10.0.19041.0` (`win-x64`) ohne Buildfehler.
- Ermittelte Testsuiten und Quellen der Testbefehle (aus `README.md`, Abschnitt „Tests", Zeilen 65–71):
  - `dotnet test Reporter.sln --filter "Category!=E2E"` — Unit-/Integrationstests (`src/Reporter.Tests`, xUnit)
  - `npm test` — `node --test "scripts/*.test.mjs"` (Release-Skript-Tests, siehe `package.json`)
  - `.\scripts\Run-E2ETests.ps1` — FlaUI-UIA3-End-to-End-Smoke-Tests der Windows-App (`src/Reporter.E2ETests`; nur Windows, interaktive Desktop-Session)
  - Ergänzendes Quality Gate (kein Testlauf): `.\scripts\Run-StaticChecks.ps1` (Format, Lizenzheader, Security-Scan, Release-Build mit `TreatWarningsAsErrors`) — laut `AGENTS.md` vor Abschluss einer Änderung auszuführen; wurde für diese Bestandsaufnahme nicht gestartet.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| dotnet-Unit/Integration | `dotnet test Reporter.sln --filter "Category!=E2E"` | Repository-Root | 0 | 514 | 0 | 0 | [dotnet-test-baseline.log](test-results/dotnet-test-baseline.log) |
| Release-Skript-Tests | `npm test` | Repository-Root | 0 | 36 | 0 | 0 | [npm-test-baseline.log](test-results/npm-test-baseline.log) |

Anmerkungen zum dotnet-Lauf:

- `Reporter.Tests.dll` (net10.0): `Fehler: 0, erfolgreich: 514, übersprungen: 0, gesamt: 514` — Bestanden.
- `Reporter.E2ETests.dll` (net10.0-windows10.0.19041.0): `Kein Test entspricht dem angegebenen Testfallfilter "Category!=E2E"` — alle Tests der E2E-Assembly tragen `[Trait("Category", "E2E")]` und wurden vom Filter ausgeschlossen (nicht ausgeführt, nicht als „übersprungen" gezählt).

### Nachgewiesene bestehende Testfehler

Es wurden keine Testfehler nachgewiesen — beide ausgeführten Läufe endeten fehlerfrei mit Exit-Code 0. Die E2E-Suite wurde nicht ausgeführt (siehe nächster Abschnitt); über ihren Zustand kann daher keine Aussage getroffen werden.

### Testlücken und Ausführungsprobleme

- **E2E-Suite (`src/Reporter.E2ETests`) nicht ausgeführt.** Die FlaUI-UIA3-Tests starten die echte `Reporter.exe` und benötigen laut `scripts/Run-E2ETests.ps1` (Zeilen 9–14) eine interaktive Desktop-Session mit sichtbarem Fenster; sie sind sämtlich mit `[Trait("Category", "E2E")]` markiert (u. a. `SmokeTests.cs` Zeilen 201/216/230/246/305/331/361, `DemoSeedTests.cs` Zeile 43) und damit bereits vom projektüblichen Baseline-Filter `Category!=E2E` ausgeschlossen. In dieser nicht-interaktiven Ausführungsumgebung sind sie nicht ausführbar; der Baseline-Lauf meldet für die Assembly keinen dem Filter entsprechenden Test. Umfang: alle Tests in `SmokeTests.cs` und `DemoSeedTests.cs`.
- **Keine Unit-Tests für die WebView-Navigationsentscheidung.** `ArticleDetailPage.OnWebViewNavigating` (`src/Reporter/Views/ArticleDetailPage.xaml.cs`, Zeilen 72–81) liegt im MAUI-Code-Behind und ist heute nicht unit-testbar — die bestehenden `WebViewNavigationGuardTests` decken nur die URL-Klassifikation ab, nicht die Cancel-/Alert-/Redirect-Entscheidung.
- **iOS-spezifische Pfade ohne Abdeckung.** Code unter `src/Reporter/Platforms/iOS/` (`AppDelegate`, `NotificationDelegate`) sowie die `#if IOS`-Blöcke in `src/Reporter/Services/` (`LocalNotificationService`, `BackgroundRefreshService`) werden unter `net10.0` nicht kompiliert und sind durch die vorhandene Suite nicht getestet. Ebenso ungetestet: `Info.plist`, `PrivacyInfo.xcprivacy` und die macOS-Remote-Schritte in `scripts/iOS-Deployment.ps1` (kein macOS-Host in dieser Umgebung).

## Testklassen

### `WebViewNavigationGuardTests`
Datei: `src/Reporter.Tests/WebViewNavigationGuardTests.cs`

- `IsExternalUrl_WebUrls_ReturnsTrue` (Theory: `http://…`, `https://…`, `HTTPS://…`) — http/https-URLs werden als extern klassifiziert (Zeilen 16–23).
- `IsExternalUrl_LocalOrOtherUrls_ReturnsFalse` (Theory: `null`, leer, Blank, `about:blank`, `data:`, `file:`, `ftp:`) — lokale/sonstige URLs gelten nicht als extern (Zeilen 30–41).

### `BaseViewModelConnectivityTests`
Datei: `src/Reporter.Tests/BaseViewModelConnectivityTests.cs`

- `InitConnectivity_SetsIsOnline_WithoutTracking` — Initialzustand `IsOnline` ohne Event-Abo.
- `TrackConnectivity_UpdatesIsOnline_AndInvokesHook` — `ConnectivityChanged` aktualisiert `IsOnline` und ruft `OnConnectivityChanged`.
- `UntrackConnectivity_StopsUpdates` — Abmeldung stoppt Updates.
- `RefreshConnectivityStatus_InvokesHookOnlyOnChange` — Hook nur bei tatsächlicher Änderung.
- `TrackConnectivity_CanBeReattached` — wiederholbares An-/Abmelden.
- Interne Hilfsklasse `TestViewModel` (Zeilen ~130–155) exponiert die `protected`-Methoden `InitConnectivity`/`TrackConnectivity`/`UntrackConnectivity`/`RefreshConnectivityStatus`.

### `DemoContentServiceTests`
Datei: `src/Reporter.Tests/DemoContentServiceTests.cs`

- `EnsureSeededAsync_FirstRun_CreatesNewsCategoryAndDemoFeed` — Seed legt Kategorie „News" und Demo-Feed an.
- `EnsureSeededAsync_FirstRun_SeedsExpectedFeedDefaults` — prüft Titel/URL/`NotificationsEnabled=false` des Seeds.
- `EnsureSeededAsync_NotFirstRun_CreatesNothing` — kein Seed bei vorhandener DB.
- `EnsureSeededAsync_SeedSuppressed_CreatesNothing` — `DemoSeedSuppressed` unterdrückt den Seed.
- `EnsureSeededAsync_ExistingNewsCategory_ReusesCategory` — Wiederverwendung einer vorhandenen Kategorie.
- `EnsureSeededAsync_CalledTwice_IsIdempotent` — Idempotenz.
- `EnsureSeededAsync_ExistingDemoFeed_SkipsSeed` — vorhandener Feed macht den Aufruf zum No-op.
- `EnsureSeededAsync_CancelledToken_ThrowsOperationCanceled` — Cancellation-Verhalten.
- `EnsureSeededAsync_FirstRun_LogsInfoEntry` — Lifecycle/`Info`-Eintrag ins Session-Log.

### `DebugLogServiceTests`
Datei: `src/Reporter.Tests/DebugLogServiceTests.cs`

- `BeginSessionAsync_ResetsPreviousEntries_KeepsErrors` — Session-Reset mit Erhalt der `Error`-Einträge.
- `BeginSessionAsync_LoadsEnabledState` (Theory) — lädt `Settings.DebugCollectionEnabled`.
- `BeginSessionAsync_Enabled_WritesStartEntry` — Starteintrag bei aktiver Sammlung.
- `LogAsync_Disabled_WritesNothing` / `LogAsync_Enabled_WritesEntry` — `IsEnabled`-Gate.
- `LogAsync_TrimsToMaxStoredEntries` — Kappung auf `MaxStoredEntries` (500).
- `LogAsync_RepositoryError_DoesNotThrow` — Fehlerisolierung des Loggers.
- `SetEnabled_EnableTransition_WritesLifecycleEntry` / `SetEnabled_DisableTransition_WritesNothing` — Übergangsverhalten.

### `DebugReportServiceTests`
Datei: `src/Reporter.Tests/DebugReportServiceTests.cs`

- `SendReportAsync_Unsupported_ReturnsFalse_AndDoesNotCompose` — kein Mail-Client → `false`.
- `SendReportAsync_ComposesWithRecipientAndLocalizedSubject` — Empfänger `DebugReportRecipient` + lokalisierter Betreff.
- `SendReportAsync_BodyContainsDeviceAppAndNetworkInfo` / `…BodyContainsSettings` / `…BodyContainsFeedHealth` / `…BodyContainsLatestSyncLogs` / `…BodyContainsSessionDebugLog` — Abschnitte des Report-Bodys (Datenfluss-Nachweis für die Datenschutz-Doku).
- `SendReportAsync_LimitsSyncLogsTo50` / `…LimitsDebugLogsTo200` — Mengenbegrenzungen.
- `SendReportAsync_EmptyLogs_StillComposes`, `…ComposeReturnsFalse_ReturnsFalse`, `…LogsReportEntry`, `…ComposeReturnsFalse_LogsErrorEntry`, `…Unsupported_LogsErrorEntry` — Erfolgs-/Fehlerpfade inkl. Report-Logeinträgen.
- `DebugReportRecipient_ReadsAssemblyMetadata` — Empfängeradresse aus Assembly-Metadaten (`Directory.Build.props`).

### `ServiceCollectionTests`
Datei: `src/Reporter.Tests/ServiceCollectionTests.cs`

- `AddReporterRepositories_ResolvesAllRepositories`, `AddReporterServices_ResolvesFeedSearchService`, `…ResolvesFeedSyncService`, `…ResolvesDebugServices`, `…ResolvesAutoRefreshService`, `…ResolvesScheduledSyncRunner`, `…ResolvesDemoContentService` — DI-Auflösung der registrierten Services/Repositories.

### `ArticleHtmlSanitizerTests`
Datei: `src/Reporter.Tests/ArticleHtmlSanitizerTests.cs`

- Tests der Sanitize-Pipeline (Skript-/Event-Handler-Entfernung; `forOffline: true` ersetzt `<a>`-Tags durch Text und entfernt `<img>`) — relevant für das Offline-Verhalten der `ArticleDetailPage`.

### `ReporterDbContextTests_Persistence` / `ReporterDbContextTests_Schema` / `ReporterDbContextFactoryTests`
Dateien: `src/Reporter.Tests/`

- Persistenz- und Schema-Tests des `ReporterDbContext` (SQLite, In-Memory-Datei via `TestDbContextFactory`) — relevant, weil die Datenbankdatei Gegenstand des iCloud-Backup-Ausschlusses ist.

## Hilfsmethoden

### `TestDbContextFactory`
Datei: `src/Reporter.Tests/TestDbContextFactory.cs`

- `TestDbContextFactory()` — erzeugt eine isolierte SQLite-Testdatenbank.
- `CreateDbContext()` — liefert einen `ReporterDbContext` gegen die Test-DB; implementiert `IDbContextFactory<ReporterDbContext>` und `IDisposable`.

### `TestDataSeeder`
Datei: `src/Reporter.Tests/TestDataSeeder.cs`

- `SeedFeedAsync(TestDbContextFactory)` / `SeedFeedAsync(IFeedRepository, url, notificationsEnabled)` — legt Test-Feeds an.

### `FakeNetworkStatusService`
Datei: `src/Reporter.Tests/FakeNetworkStatusService.cs`

- `IsOnline` (setzbar), `RaiseConnectivityChanged()` — simuliert Online-/Offline-Wechsel für Connectivity-Tests (u. a. Basis für das Offline-Verhalten der Detailansicht).

### Weitere Fakes/Helfer in `src/Reporter.Tests/`

- `FakeDebugLogService`, `FakeEmailService`, `FakeDeviceInfoProvider`, `FakeFeedSearchService`, `FakeFeedSyncService`, `FakeFeedIconService`, `FakeLocalNotificationService`, `FakeNotificationService`, `FakeAppThemeService`, `FakeAutoRefreshService`, `FakeBackgroundRefreshService` — No-op-/Stub-Implementierungen der Interfaces für ViewModel-/Service-Tests.
- `FakeHttpMessageHandler`, `TestFeedXml` — HTTP-/Feed-Fixtures für `FeedSyncService`-/`FeedSearchService`-Tests.
- `DelegatingItemRepository`, `TestSettingsHelper`, `TestWaitHelper` — Repository-Delegator, Settings-Fixture und Wartehelfer.
