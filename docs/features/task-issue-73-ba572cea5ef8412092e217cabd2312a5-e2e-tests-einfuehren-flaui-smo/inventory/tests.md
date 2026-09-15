<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ausgangszustand und bestehende Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-15, Testlauf 16:49:09–16:49:15 +02:00 (Mitteleuropa, Sommerzeit)
- Branch und Commit-ID: `task/issue-73-ba572cea5ef8412092e217cabd2312a5-e2e-tests-einfuehren-flaui-smo`, Commit `60e5c46e4e2e5957938e6adff9e50b30ae665e8d` („Systembenachrichtigungen nur bei Hintergrundabruf (#89)", 2026-09-15 16:34:17 +0200)
- Uncommittete Änderungen im getesteten Stand: keine Code-Änderungen; nur das neue, ungetrackte Verzeichnis `docs/features/task-issue-73-ba572cea5ef8412092e217cabd2312a5-e2e-tests-einfuehren-flaui-smo/` (`requirement.md`, `todo.md`, später `inventory/`). Der getestete Quellstand entspricht dem Commit.
- Testumgebung und Runtime-/SDK-Versionen: Windows-Desktop (DESKTOP-CM8OBSG), .NET SDK 10.0.401; installierte Workloads: `maui-windows` 10.0.20, `android` 36.1.69, `ios`/`maccatalyst` 26.5.10318; xunit 2.9.3, xunit.runner.visualstudio 3.1.4, Microsoft.NET.Test.Sdk 17.14.1, coverlet.collector 6.0.4.
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - **`src/Reporter.Tests/Reporter.Tests.csproj`** (net10.0, xunit) — einzige vorhandene automatisierte Testsuite. CI-Befehl aus `.github/workflows/staging-ci.yml` und `pr-staging-ci.yml` (Job `build-and-test`): `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx"`, davor `dotnet restore Reporter.sln -r win-x64` und `dotnet build Reporter.sln --configuration Release --no-restore` mit `IncludeIosTarget=false`, `IncludeAndroidTarget=false`. Coverage-Schwelle 70 % Zeilen (ReportGenerator).
  - **Statische Prüfungen** (CI-Job `static-checks`, lokal `scripts/Run-StaticChecks.ps1`): `dotnet format --verify-no-changes --severity error`, `node scripts/add-license-headers.mjs --check`, `dotnet list package --vulnerable --include-transitive`, `dotnet build -p:TreatWarningsAsErrors=true` (Release).
  - **E2E-/UI-Suite:** existiert nicht. Vorarbeiten nur als manuelle PoC-Skripte unter `test-results/issue-59/` (`uia.ps1`, `stubserver.py`, `probe.fsx`).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 – Unit-Tests (Baseline) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results-baseline.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 491 | 0 | 0 | [Konsolen-Log](test-results/dotnet-test-baseline.log), [TRX](test-results/test-results-baseline.trx), [Coverage](test-results/coverage.cobertura.xml) |
| 2 – App-Build (Windows-TFM) | `dotnet build src/Reporter/Reporter.csproj --configuration Release --no-restore -f net10.0-windows10.0.19041.0` (nach `dotnet restore Reporter.sln -r win-x64`, Env `IncludeIosTarget=false`, `IncludeAndroidTarget=false`) | Repo-Root | 0 | – (kein Testlauf; 0 Warnungen, 0 Fehler) | – | – | Ausgabe `src/Reporter/bin/Release/net10.0-windows10.0.19041.0/win-x64/Reporter.exe` erzeugt |

Hinweis zu Lauf 1: Anders als die CI wurde ohne `--no-build` ausgeführt (kein
vorbereiteter Solution-Release-Build nötig, da `Reporter.Tests` nur `Reporter.Core`
und `Reporter.Data` referenziert — beide net10.0 ohne MAUI-Abhängigkeit). Die TRX
des Laufs liegt zusätzlich unter `src/Reporter.Tests/TestResults/test-results-baseline.trx`.

Hinweis zu Lauf 2: Build-Verifikation, kein Test. Belegt, dass das Windows-TFM lokal
baubar ist und die unverpackte `Reporter.exe` entsteht (`WindowsPackageType=None`).
XAML-Source-Generierung (`MauiXamlInflator=SourceGen`) aktiv.

### Nachgewiesene bestehende Testfehler

Keine. Der Baseline-Lauf meldet 491/491 erfolgreiche Tests, 0 fehlgeschlagene,
0 übersprungene. (Das Issue nennt „316 Tests" — der aktuelle Stand umfasst bereits
491 ausgeführte Testfälle; `[Theory]`-Fälle zählen einzeln.)

### Testlücken und Ausführungsprobleme

- **Keine E2E-/UI-Tests vorhanden.** Es gibt kein UI-Testprojekt, keinen FlaUI-/UIA-
  Testcode und kein Test-Webserver-Fixture. Die gesamte Ebene „unterhalb der
  ViewModels" (XAML-Bindings, DataTrigger, Code-Behind-Verdrahtung, Dialoge, Layout)
  ist ungetestet.
- **`Reporter`-App-Projekt ist nicht testbar als Unit-Testziel**: das MAUI-Projekt
  (`net10.0-windows10.0.19041.0`, u. a. iOS/Android-TFMs) ist keine Test-Suite; die
  App kann gebaut (`Reporter.exe`) und interaktiv gestartet werden, aber nicht per
  `dotnet test` ausgeführt werden. Ein automatisierter Lauf erfordert eine
  interaktive Windows-Desktop-Session (UIA) — in dieser Umgebung nicht als
  Testlauf dokumentierbar.
- **Manuelle Verifikation** liegt nur als Screenshots/Skripte unter
  `test-results/issue-59/` vor (PoC, keine reproduzierbaren Tests).
- iOS-/Android-TFMs wurden nicht gebaut (`IncludeIosTarget=false`,
  `IncludeAndroidTarget=false` wie in CI).

## Testklassen (anforderungsrelevant)

Alle Klassen liegen in `src/Reporter.Tests/` (namespace `Reporter.Tests`, xunit).

### `FeedsViewModelTests` (73 Testmethoden, `IDisposable`)

Integrationstests des `FeedsViewModel` gegen echte Repositories auf In-Memory-SQLite
(`TestDbContextFactory`) plus Fakes (`FakeFeedSyncService`, `FakeFeedSearchService`,
`FakeFeedIconService`, `FakeNetworkStatusService`). Für die Anforderung relevante Gruppen:

- Suche: `SearchCommand_PopulatesSearchResults`, `SearchCommand_DomainInput_NormalizesToHttpsUrl`,
  `SearchCommand_FreeText_DoesNotCallService_ShowsEmptyResults`, `SearchCommand_WhenEmpty_DoesNotCallService`,
  `SearchCommand_WhenOffline_SkipsSearchWithoutError`, `SearchCommand_InEditMode_DoesNotDiscardEdit`,
  `SearchCommand_WhenServiceUnavailable_SetsSearchErrorMessage`,
  `SearchCommand_WhenServiceUnavailableAndDomainInput_SetsRetryHint`,
  `SearchCommand_WhenConfirmDirectAddThrows_InvokesDialogOnlyOnce`,
  `SearchCommand_NoResultsAndValidUrl_Confirmed_AddsFeedWithFileNameTitle`,
  `SearchCommand_WhenUnavailableAndConfirmedDuplicate_KeepsSheetOpenAndSetsError`,
  `SearchCommand_NoResultsAndConfirmedDuplicate_ClosesResultsView`,
  `SearchCommand_WhenResultsShown_ClosesAddForm`,
  `SearchCommand_WhenUnavailable_KeepsSheetOpenAndSetsSearchError`,
  `SearchCommand_NoResultsAndValidUrl_Declined_KeepsFormState`,
  `SearchCommand_NoResultsAndDomainInput_DoesNotInvokeConfirm`,
  `SearchCommand_WhenUrlChangesDuringSearch_DiscardsStaleResults`,
  `SearchCommand_WhenUrlChangesDuringFailedSearch_DiscardsStaleError`,
  `CloseSearchResultsCommand_ExitsResultsView`, `NewUrl_Changed_ClearsSearchState`,
  `ConnectivityChanged_UpdatesSearchCommandCanExecute`
- Direkt-Add/Abonnieren: `DirectAddCommand_WhenOffline_PersistsFeedWithDefaults`,
  `DirectAddCommand_DomainInput_NormalizesToHttpsUrl`,
  `DirectAddCommand_WhenUrlInvalid_SetsErrorAndKeepsSheetOpen`,
  `DirectAddCommand_WhenDuplicate_SetsErrorAndKeepsSheetOpen`,
  `DirectAddCommand_WhenDuplicate_ClearsStaleSearchError`,
  `DirectAddCommand_InEditMode_DoesNotAddFeed`, `DirectAddCommand_StoresFaviconUrl`,
  `DirectAddCommand_IconLookupFails_FeedStillAdded`, `DirectAddCommand_Offline_SkipsIconLookup`,
  `SubscribeResultCommand_PersistsFeedFromResult`,
  `SubscribeResultCommand_WhenTitleEmpty_StoresFileNameAsPlaceholder`,
  `SubscribeResultCommand_WhenDuplicate_SetsErrorAndDoesNotAdd`,
  `SubscribeResultCommand_UsesSiteUrlForFaviconLookup`
- Sheet/Edit: `OpenAddFormCommand_ShowsForm`, `OpenAddFormCommand_AfterEdit_ClearsStaleEditState`,
  `CloseAddFormCommand_ResetsFormAndHidesForm`,
  `CloseAddFormCommand_WhenSheetShowsErrors_ClearsErrorChannels`,
  `EditAsync_OpensSheetInEditMode`, `EditCommand_PopulatesFeedNotificationsEnabled`,
  `SaveCommand_ExistingFeed_PersistsNotificationsEnabled`, `SaveCommand_ResetsFeedNotificationsEnabled`,
  `SaveCommand_WithoutSelectedFeed_DoesNotAddFeed`, `SaveCommand_EditMode_*` (3 Tests),
  `DeleteCommand_ResetsFeedNotificationsEnabled`
- Umbenennen/Kategorie (Methoden, die das Code-Behind nach Dialogen aufruft):
  `RenameFeedAsync_UpdatesTitle`, `RenameFeedAsync_EmptyTitle_SetsErrorAndKeepsTitle`,
  `RenameFeedAsync_NullFeed_DoesNothing`, `RenameFeedAsync_PreservesFaviconUrl`,
  `RenameFeedAsync_PreservesLastError`, `ChangeFeedCategoryAsync_SetsCategoryId`,
  `ChangeFeedCategoryAsync_EmptyGuid_ClearsCategory` (Pseudo-Eintrag „Keine Kategorie"),
  `ChangeFeedCategoryAsync_NullArguments_DoNothing`,
  `MakeUniqueOptionLabels_*` (3 Tests), `GetFeedErrorMessage_*` (3 Tests)
- Sync/Connectivity: `RefreshCommand_*`/`RefreshAllCommand_*` (8 Tests),
  `ConnectivityChanged_ClearsSyncErrorMessage`, `ConnectivityChanged_UpdatesIsOnline`

Nicht abgedeckt: `OnFeedTapped`-Dialoge, `DisplayPromptAsync`/`DisplayActionSheetAsync`,
Fokus-Logik, `OnBackButtonPressed` — alles Code-Behind, ohne Tests.

### `CategoriesViewModelTests` (6 Testmethoden)

`LoadCommand`, `SaveCommand` (leer/Duplikat/Anlegen/Bearbeiten), `DeleteCommand`
gegen In-Memory-SQLite. Nicht abgedeckt: `OnCategoryTapped`-ActionSheet/-Alert
(Code-Behind).

### `FeedSearchServiceTests` (17 Testmethoden)

Unit-Tests des `FeedSearchService` mit privatem `StubHttpMessageHandler`
(URL-Regeln `On`/`OnExact`/`OnExactRedirect`, `Fallback`-Statuscode, `Delay`,
`RequestedUrls`): Directory-Mapping inkl. `bozo`-Filter, Sortierung,
`ExactUrl`-Rang, Autodiscovery (`<link>`-Tags, Well-Known-Pfade, Redirects),
Dedup, Fehlerfälle (`FeedSearchUnavailableException` nur bei Totalausfall),
Timeout (2 s Budget), Caller-Cancellation. Alle Tests instanziieren
`new FeedSearchService(new HttpClient(handler))` — ein optionaler
Endpoint-Konstruktorparameter bleibt kompatibel; ein Test für den
Endpoint-Override existiert noch nicht.

### `FeedTitleFallbackTests` (8 Testmethoden)

Fallback-Titel aus Pfadsegment/Host/URL, `IsFileNamePlaceholderTitle` — relevant
für IP-/Port-Feed-URLs des Test-Webservers.

### `ServiceCollectionTests` (6 Testmethoden)

Spiegelt die `MauiProgram`-DI-Registrierungen nach (u. a.
`AddReporterServices_ResolvesFeedSearchService` mit
`AddSingleton<IFeedSearchService, FeedSearchService>()`); bei Änderung der
Registrierung ggf. anzupassen.

### Übrige Suiten (Kontext)

Weitere Testklassen decken `FeedSyncService` (38+2), Repositories (Feed/Item/
Keyword/Category/Settings/SyncLog/DebugLog), `ReporterDbContext` (Schema/
Persistenz/Factory), `SettingsViewModel` (16+26+9+5+8+1), `UnreadViewModel` (19),
`LaterViewModel` (11), `FeedIconService` (11), `AutoRefreshService` (18+2),
`ScheduledSyncRunner` (4), `RetentionCleanupService` (7), `DebugReport*` (15+3),
`Keyword*` (7+3), `ArticleHtmlSanitizer` (7), `ReadingTimeEstimator` (7),
`WebViewNavigationGuard` (2), `AppCulture` (3), `FeedListItem` (3),
`FeedSyncErrorKind` (6), `BaseViewModelConnectivity` (5), `NotificationService` (12).

## Hilfsmethoden / Test-Doubles

### `TestDbContextFactory`

`IDbContextFactory<ReporterDbContext>` auf shared In-Memory-SQLite
(`Mode=Memory;Cache=Shared;Default Timeout=30`, Keep-Alive-Connection,
`EnsureCreated` im Ctor); `IDisposable`.

### `TestDataSeeder`

Statische Seeding-Helper: `SeedFeedAsync(TestDbContextFactory)` /
`SeedFeedAsync(IFeedRepository, url, notificationsEnabled)` u. a.

### `TestFeedXml`

Baut RSS-2.0-/Atom-1.0-Dokumente (`Rss(items, channelTitle)` u. a.) — Vorlage für
Fixture-Dateien des E2E-Webservers.

### `TestWaitHelper`

`WaitUntilAsync`-Polling (Timeout 5 s) für fire-and-forget-Persistpfade.

### `FakeHttpMessageHandler`

`HttpMessageHandler` mit `Func<HttpRequestMessage, Task<HttpResponseMessage>>`-Factory.

### `FakeFeedSearchService`

`IFeedSearchService`-Fake: `NextResults`, `NextException`, `PendingResult`-Gate,
`LastQuery`, `CallCount`.

### Weitere Fakes

`FakeFeedSyncService`, `FakeFeedIconService`, `FakeNetworkStatusService`,
`FakeLocalNotificationService`, `FakeNotificationService`, `FakeAppThemeService`,
`FakeAutoRefreshService`, `FakeBackgroundRefreshService`, `FakeDebugLogService`,
`FakeDeviceInfoProvider`, `FakeEmailService`, `DelegatingItemRepository`.

## Vorarbeiten unter `test-results/issue-59/` (kein Testcode, aber fachlich relevant)

- `uia.ps1` — PowerShell-PoC für UIA-Steuerung der laufenden `Reporter.exe`
  (`Get-Process Reporter`): Aktionen `shot` (Screenshot), `rect`, `click`,
  `type`, `setvalue` (ValuePattern auf Edit-Controls), `sysback` (XButton1 =
  System-Zurück), `list` (UIA-Baum mit Name/BoundingRectangle), `invoke`
  (InvokePattern bzw. Klick auf Elementzentrum), `pick` (ComboBox Expand +
  SelectionItemPattern). Demonstriert die UIA-Erreichbarkeit der App inkl.
  benannter Controls.
- `stubserver.py` — zwei `http.server`-Stubs: Port 8099 liefert HTML mit
  `<link rel="alternate" type="application/rss+xml" href="/feed.xml">` plus
  RSS auf allen Well-Known-Pfaden (`/feed`, `/rss`, `/rss.xml`, `/atom.xml`,
  `/feed.xml`, `/index.xml`); Port 8100 liefert leere HTML-Seite und 404 auf
  allen Feed-Pfaden (Direct-Add-Fallback). **Kein** feedsearch.dev-JSON-Stub —
  die Directory-API ist nicht abgedeckt (dafür wäre der
  `DirectoryEndpoint`-Override nötig).
- `probe.fsx` — F#-Skript, das `FeedSearchService` direkt gegen eine URL fährt
  (Search-Probe zum Debuggen; vom Issue fälschlich als „SQLite-Verifikation"
  bezeichnet). Der `#r`-Pfad zeigt auf ein **anderes Arbeitsverzeichnis**
  (`efbace56-…`), ist hier nicht lauffähig.
- Screenshots `manual-*.png` (5 manuelle Verifikationsrunden, Dark/Light).
