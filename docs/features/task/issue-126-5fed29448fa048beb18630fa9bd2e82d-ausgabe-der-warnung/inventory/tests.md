<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-02, Läufe ca. 19:44–20:00 +02:00 (MESZ).
- Branch und Commit-ID: `task/issue-126-5fed29448fa048beb18630fa9bd2e82d-ausgabe-der-warnung` @ `16e5ea46d6e732ed8fbda4b49bccaa14edbdc78a` (2026-09-20 15:04:59 +0200, „Backmerge into Staging (#124) (#125)").
- Uncommittete Änderungen im getesteten Stand: nur das neue, untracked Doku-Verzeichnis `docs/features/task/issue-126-5fed29448fa048beb18630fa9bd2e82d-ausgabe-der-warnung/` (`requirement.md` sowie diese Bestandsaufnahme samt Testergebnissen). Keine Änderungen an Produktiv- oder Testcode.
- Testumgebung und Runtime-/SDK-Versionen: Windows (interaktive Session, `Environment.UserInteractive = true`), .NET SDK 10.0.401; Workloads `maui-windows` 10.0.20, `android`, `ios`, `maccatalyst`. Testframework xUnit 2.9.3 mit `Microsoft.NET.Test.Sdk` 17.14.1, `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12, `coverlet.collector` 6.0.4 (siehe `src/Reporter.Tests/Reporter.Tests.csproj`).
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `src/Reporter.Tests` (Unit-/Integrationstests, `net10.0`) — Befehl aus CI `pr-staging-ci.yml`/`staging-ci.yml` („build & test"-Job): `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx"`.
  - `src/Reporter.E2ETests` (FlaUI/UIA3-Smoke-Tests, `net10.0-windows10.0.19041.0`) — projekteigenes Skript `scripts/Run-E2ETests.ps1` (baut `src/Reporter` Debug/`win-x64`, setzt `REPORTER_APP_PATH`, dann `dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0`). Läuft nicht in CI; benötigt interaktive Desktop-Session.
  - `scripts/Run-StaticChecks.ps1` bildet den CI-Job „static checks" lokal ab (Restore, `dotnet format`, Lizenzheader, Security-Scan, Release-Build mit `TreatWarningsAsErrors`); ist kein Testlauf und wurde für die Bestandsaufnahme nicht ausgeführt.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit (Release) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results-baseline.trx" --logger "console;verbosity=normal"` (Env: `IncludeIosTarget=false`, `IncludeAndroidTarget=false`; `--no-build` entfiel, da kein vorheriger Build existierte) | Repo-Root | 0 | 668 | 0 | 0 | [unittests-release-console.log](test-results/unittests-release-console.log), [test-results-baseline.trx](test-results/test-results-baseline.trx), [coverage.cobertura.xml](test-results/coverage.cobertura.xml) |
| E2E (Debug) | `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Run-E2ETests.ps1` (baut `src/Reporter/Reporter.csproj -c Debug -f net10.0-windows10.0.19041.0 -r win-x64`, dann `dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0`) | Repo-Root | 1 | 29 | 2 | 0 | [e2etests-console.log](test-results/e2etests-console.log) |

### Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|-----------------------|------------------|---------------------------|-------------------|
| `Reporter.E2ETests.DemoSeedTests.FirstStart_SeedsNewsCategoryAndDemoFeed` | `src/Reporter.E2ETests/DemoSeedTests.cs` | `Microsoft.Data.Sqlite.SqliteException : SQLite Error 1: 'no such table: feeds'` in `FeedDbAssertions.ExistsCoreAsync` (`FeedDbAssertions.cs:238-239`) via `PollUntilExistsAsync` (`FeedDbAssertions.cs:206`), ausgelöst aus `DemoSeedTests.cs:66`. Die zweite, vom Test selbst gestartete App-Instanz hat in ihrer isolierten Temp-DB innerhalb des Poll-Zeitfensters keine `feeds`-Tabelle erzeugt (umgebungs-/timingabhängige E2E-Bedingung). | E2E-Lauf — [e2etests-console.log](test-results/e2etests-console.log) |
| `Reporter.E2ETests.ArticleImageTests.BrokenImage_DoesNotFailSync_StoresNoImage` | `src/Reporter.E2ETests/ArticleImageTests.cs` | Assert-Fehler in `ArticleImageTests.cs:169`: `No feed row with URL 'http://127.0.0.1:60193/feeds/broken-image-feed.xml' found in …\reporter.db — the direct add did not persist the feed.` Der über die UI ausgelöste Direct-Add hat die Feed-Zeile innerhalb des Poll-Zeitfensters nicht persistiert (umgebungs-/timingabhängige E2E-Bedingung). | E2E-Lauf — [e2etests-console.log](test-results/e2etests-console.log) |

Keine Fehlschläge in `Reporter.Tests` (668/668 bestanden). Der für die Anforderung einschlägige E2E-Test `FeedDetailTests.FeedDetail_ErrorDetails_OnlyForErrorFeed` (Fehlerdetails-Eintrag nur bei `FeedHealth.Error`) ist im Ausgangslauf bestanden.

### Testlücken und Ausführungsprobleme

- Der CI-Coverage-Schwellenwert (70 % Zeilenabdeckung, `pr-staging-ci.yml`) wurde nicht lokal erzwungen; die Coverage wurde gesammelt ([coverage.cobertura.xml](test-results/coverage.cobertura.xml)), aber `reportgenerator`-Report und Schwellenprüfung wurden nicht ausgeführt.
- Die E2E-Suite ist nicht Teil der CI und hängt von UIA3 auf einem sichtbaren Desktop ab; beide Fehlschläge sind DB-Polling-/Timing-Phänomene gegen eine reale App-Instanz. Ob sie reproduzierbar oder sporadisch sind, ist mit einem einzelnen Lauf nicht entscheidbar — als gesichert gilt nur der dokumentierte Fehlschlag in diesem Ausgangslauf.
- Keine übersprungenen oder deaktivierten Tests festgestellt (Unit: 0 übersprungen; E2E: 0 übersprungen). Keine Build-, Setup- oder Infrastrukturfehler — beide Builds (Release `Reporter.Tests`, Debug `Reporter`/`Reporter.E2ETests`) waren erfolgreich.

## Testklassen

### `FeedSyncServiceTests` (`src/Reporter.Tests/FeedSyncServiceTests.cs`)

Integrationstests über echte Repositories + `FakeHttpMessageHandler`. Für die Anforderung relevant:

- `SyncFeedAsync_FewerItems_SetsWarning` — Warnungsauslöser 1 (fetched < 50 % von existing): Status `Warning` am `SyncResult` und am gespeicherten Feed. Prüft keinen Warnungsgrund (existiert nicht).
- `SyncFeedAsync_NoNewItemsForThirtyDays_SetsWarning` — Warnungsauslöser 2 (keine neuen Artikel, jüngster Artikel >30 Tage): Status `Warning` im `SyncResult`.
- `SyncFeedAsync_Failure_PersistsErrorKindAndMessage` — Fehlerfall schreibt `LastErrorKind`/`LastErrorMessage` am Feed.
- `SyncFeedAsync_Success_ClearsLastError` — Erfolgreicher Sync leert die Fehlerfelder.
- `SyncFeedAsync_HttpFeedNetworkFailure_ClassifiedAsInsecureHttpBlocked`, `SyncFeedAsync_HttpStatusError_ClassifiedAsHttpStatus`, `SyncFeedAsync_InvalidXml_ClassifiedAsParse`, `SyncFeedAsync_HttpsFeedNetworkFailure_ClassifiedAsNetwork` — Klassifikation → `LastErrorKind`.
- `SyncFeedAsync_KeywordFiltered_LogCountsFiltered` — Nachrichtenformat `"... N filtered"` in `SyncResult.Message` und `SyncLog.Message`.
- Übrige Tests (Auswahl): Item-Anlage/Dedup (`SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk`, `SyncFeedAsync_Duplicates_SkipsExistingItems`, `SyncFeedAsync_MixedNewAndExisting_BatchInsertsOnlyNew`, `SyncFeedAsync_DuplicatesWithinSameDocument_InsertsOnce`), Offline (`SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog`, `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange`), Benachrichtigungen (`SyncFeedAsync_NewItems_NotifiesWithFeedAndItems`, `SyncFeedAsync_SummaryMode_SendsSingleSummaryNotification`, `SyncFeedAsync_NoNewItems_DoesNotNotify`, `SyncFeedAsync_FeedDisabled_NoNotifications`, `SyncFeedAsync_NotificationThrows_SyncStillSucceeds`), Titel-Auflösung (`SyncFeedAsync_WhenTitleIsPlaceholder_*`, `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle`), Keyword-Filter, Favicon-Backfill, Atom-0.3, Content-/Bild-Backfill und Bild-Download-Fehler.

### `FeedSyncServiceTests_DebugLog` (`src/Reporter.Tests/FeedSyncServiceTests_DebugLog.cs`)

- `SyncFeedAsync_Failure_LogsErrorEntry` — Fehlerfall schreibt einen `DebugLogCategory.Sync`-Eintrag mit `DebugLogLevel.Error` und Exception-Details.
- `SyncFeedAsync_Failure_WithoutDebugLogService_StillReportsError` — optionaler `IDebugLogService`.

### `FeedRepositoryTests` (`src/Reporter.Tests/FeedRepositoryTests.cs`)

- `UpdateAsync_PersistsLastError` — `LastErrorKind`/`LastErrorMessage` überleben `UpdateAsync`.
- `GetAllWithDetailsAsync_ProjectsLastError` — Projektion der Fehlerfelder in `FeedListItem`.
- Übrige Tests: CRUD, `GetAllAsync`-Sortierung, Projektion von `NotificationsEnabled`/`FaviconUrl`/`FeedInitial`, Kaskaden-Löschen (Items, Contents, Images, Feed-Keywords).

### `ReporterDbContextTests_Persistence` (`src/Reporter.Tests/ReporterDbContextTests_Persistence.cs`)

- `Feed_PersistRoundtrip_LastError` — `LastErrorKind`/`LastErrorMessage` überstehen einen SQLite-Roundtrip.
- Übrige Tests: `SaveChangesAsync_PersistsFeedWithCategory`, `Items_WithInclude_ReturnsFeedAndCategory`, `Settings_DebugCollectionEnabled_PersistRoundtrip`, `DebugLogEntry_PersistRoundtrip`.

### `ReporterDbContextTests_Schema` (`src/Reporter.Tests/ReporterDbContextTests_Schema.cs`)

- `EnsureCreatedAsync_CreatesQueryableTables` — `EnsureCreated` legt alle Tabellen an.
- `DebugLogEntries_MappedToExpectedTable` — Mapping-Prüfung (Tabellen-/Spaltennamen, Index, Default) als Muster für Schema-Tests neuer Feed-Spalten.

### `FeedDetailViewModelTests` (`src/Reporter.Tests/FeedDetailViewModelTests.cs`)

Für die Anforderung relevant:

- `GetFeedErrorMessage_MapsKindToLocalizedText` — `LastErrorKind` → `FeedErrorKind*`-Ressource.
- `GetFeedErrorMessage_FallsBackToUnknown` — unbekannter/`null`-Kind → `FeedErrorKindUnknown`.
- `GetFeedErrorMessage_AppendsTechnicalMessage` — `LastErrorMessage` wird als zweiter Absatz angehängt.
- `RenameFeedAsync_PreservesLastError` — Umbenennen über `ToFeed` erhält `LastErrorKind`/`LastErrorMessage` (auch `RenameFeedAsync_PreservesFaviconUrl`, `SaveEditCommand_PreservesCategoryId` prüfen das Überleben unberührter Felder — neue Warnungsfelder müssten denselben Schutz erhalten).
- `RefreshCommand_*` — Sync-Auslösung, Offline-Verhalten, `SyncStatusError` bei `FeedHealth.Error`/Exception (`RefreshCommand_WhenSyncReturnsError_SetsLocalizedSyncErrorMessage`, `RefreshCommand_WhenSyncThrows_SetsLocalizedSyncErrorMessage` u. a.).
- `MakeUniqueOptionLabels_*` — Label-Kollisionen im Kategorie-Picker.
- Übrige Tests: Laden/Paging/Suche, Keyword-Verwaltung im Edit-Sheet, Löschen, Connectivity.

### `FeedSyncErrorKindTests` (`src/Reporter.Tests/FeedSyncErrorKindTests.cs`)

- `Classify_HttpRequestExceptionOnHttpUrl_ReturnsInsecureHttpBlocked`
- `Classify_HttpRequestExceptionWithStatusCode_ReturnsHttpStatus`
- `Classify_HttpRequestExceptionWithStatusCodeOnHttpUrl_ReturnsHttpStatus`
- `Classify_HttpRequestExceptionWithoutStatusCode_ReturnsNetwork`
- `Classify_XmlException_ReturnsParse`
- `Classify_OtherException_ReturnsUnknown`

Muster für eine eventuelle `FeedSyncWarningKindTests`.

### `FeedListItemTests` (`src/Reporter.Tests/FeedListItemTests.cs`)

- `FeedInitial_FeedWithFavicon_StillProvidesFallbackLetter`, `FeedInitial_BothListItems_DeriveSameLetter`, `FeedInitial_EmptyTitle_FallsBackToQuestionMark` — nur Avatar-Initialen; keine Health-/Fehler-Assertions.

### `DebugReportServiceTests` (`src/Reporter.Tests/DebugReportServiceTests.cs`)

- `SendReportAsync_BodyContainsFeedHealth` — Abschnitt „Feed health" enthält Titel/Status eines Feeds (aktuell ohne Fehler-/Warnungsfelder).
- Übrige Tests: Body-Abschnitte (Device/App/Network/Settings/Sync-Log/Session-Log), Eintrags-Limits (50/200), Empfänger/Subject, Fehlpfade.

### `DemoContentServiceTests` (`src/Reporter.Tests/DemoContentServiceTests.cs`)

- `EnsureSeededAsync_FirstRun_SeedsExpectedFeedDefaults` — assertet u. a. `LastErrorKind == null`, `LastErrorMessage == null` auf dem Demo-Feed (müsste neue Warnungsfelder ebenfalls als `null` erwarten können).
- Weitere: Kein Re-Seed, unterdrückter Seed, Kategorie-Anlage.

### E2E-Tests (`src/Reporter.E2ETests/`)

- `FeedDetailTests.FeedDetail_ErrorDetails_OnlyForErrorFeed` (`FeedDetailTests.cs:407-451`) — einziger Test des Fehlerdetails-Interaktionswegs: `ButtonShowErrorDetails` erscheint im Aktionsblatt nach einem fehlgeschlagenen Refresh (404-Feed), der Alert zeigt `FeedErrorKindHttpStatus`; bei gesundem Feed fehlt der Eintrag. **Im Ausgangslauf bestanden.** Ein Warnungs-Pendant existiert nicht.
- Übrige E2E-Tests: `FeedDetailTests` (13 weitere `[Fact]`s: Suche, Paging/Scrollen, Umbenennen, Kategorie, Edit-Sheet/Keywords, Löschen), `SmokeTests` (5), `ArticleImageTests` (2), `ArticleLinkTests` (1), `DemoSeedTests` (1), `E2EProcessGuardTests` (8, ohne App-Fixture).

## Hilfsmethoden

### `TestDbContextFactory` / `TestDbContextFactoryBase<TContext>` (`src/Reporter.Tests/`)

- Konstruktor / `CreateDbContext` / `CreateDbContextAsync` — In-Memory-SQLite (`EnsureCreated`, Schema ohne Migrationen) pro Testklasse; `IDbContextFactory<T>`-Implementierung für die echten Repositories.

### `TestDataSeeder` (`src/Reporter.Tests/TestDataSeeder.cs`)

- `SeedFeedAsync(TestDbContextFactory, url)` — direkter Entity-Insert (ohne Defaults wie `NotificationsEnabled`).
- `SeedFeedAsync(IFeedRepository, url, notificationsEnabled)` — Feed über das Repository (Titel „Test Feed").

### `TestFeedXml` (`src/Reporter.Tests/TestFeedXml.cs`)

- `Rss(items, channelTitle)` / `RssWithEnclosure(items, channelTitle)` / `Atom(entries, feedTitle)` / `Atom03(entries, feedTitle)` — Feed-Dokumente als String für `FakeHttpMessageHandler`.

### Fake-/Delegating-Klassen (`src/Reporter.Tests/`)

- `FakeHttpMessageHandler` — `Func<HttpRequestMessage, HttpResponseMessage>`-Responder (auch `Task.FromException` für Fehlerpfade).
- `FakeNotificationService`, `FakeNetworkStatusService` (setzbares `IsOnline`), `FakeFeedIconService` (`NextResult`/`NextException`/Request-Protokoll), `FakeItemImageService`, `FakeItemContentStore`, `FakeDebugLogService` (`IsEnabled`, `LoggedEntries`), `FakeFeedSyncService` (`SyncFeedResult`/`SyncFeedException`/Zähler), `FakeLocalNotificationService` (`ShownNotifications`).
- `DelegatingFeedRepository`, `DelegatingItemRepository`, `DelegatingKeywordRepository` — virtuelle Durchreicher für partielle Test-Doubles.
- `ThrowingFeedRepository`, `ThrowingCategoryRepository` — werfen bei jedem Aufruf.
- Private Doubles in `FeedDetailViewModelTests`: `FailingItemRepository`, `FailingReloadFeedRepository` (`FailGetAllWithDetails`), `FailingSaveFeedRepository`, `CountingItemRepository` (`GetByFeedCallCount`).

### Sonstige Testhelfer (`src/Reporter.Tests/`)

- `TestWaitHelper.WaitUntilAsync` — Polling auf ViewModel-Zustand (Debounce-Tests).
- `TestHttpResponses.Png(byte[])` — PNG-Response für Bild-Download-Tests.
- `TestSettingsHelper.SaveAsync` — Settings-Vorgaben (z. B. `notificationSummaryEnabled`).

### E2E-Helfer (`src/Reporter.E2ETests/`)

- `ReporterAppFixture` — startet `StubFeedServer` + `Reporter.exe` (Env `REPORTER_FEEDSEARCH_ENDPOINT`, `REPORTER_DB_PATH`, `REPORTER_DISABLE_DEMO_SEED=1`), FlaUI-Attach, Teardown; `ResolveAppPath` (`REPORTER_APP_PATH` oder Debug-Build-Konvention).
- `E2EPageHelpers` — u. a. `OpenFeedDetailActions(expectedEntryName)` (öffnet das Aktionsblatt der Detailseite), `WaitForElementInScopeByName`, `TryFindElementInScopeByName`, `WaitForCard`, `AddFeedViaUi`, `SelectTab`, `OpenFeedDetail`, `NavigateBackToFeedList`.
- `FeedDbAssertions` — `FeedExistsAsync`, `ItemExistsAsync`, `ItemImageExistsAsync`, `KeywordExistsForFeedAsync` u. a. (Polling gegen die SQLite-DB der laufenden App).
- `StubFeedServer`, `UiRetry`, `E2EProcessGuard`, `E2ETestCollection` (serielle Collection „E2E").
