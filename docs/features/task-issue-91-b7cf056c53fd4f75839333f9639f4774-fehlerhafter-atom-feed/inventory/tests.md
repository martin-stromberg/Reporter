<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-15, ca. 22:26–22:30 Uhr +02:00 (MESZ)
- **Branch und Commit-ID:** `task/issue-91-b7cf056c53fd4f75839333f9639f4774-fehlerhafter-atom-feed` @ `30b128a89fc35918e2b311cd4d9a1094921c7ec4` („E2E-Tests einführen: FlaUI-Smoke-Suite + Compiled Bindings (#92)", 2026-09-15 22:17:52 +0200)
- **Uncommittete Änderungen im getesteten Stand:** Nur das untracked Feature-Verzeichnis `docs/features/task-issue-91-b7cf056c53fd4f75839333f9639f4774-fehlerhafter-atom-feed/` (`requirement.md`, `todo.md`, `inventory/` inkl. Testnachweise). **Keine Änderungen an getracktem Produktivcode, Tests oder Konfiguration.**
- **Testumgebung und Runtime-/SDK-Versionen:** Windows 11 (Host `DESKTOP-CM8OBSG`), .NET SDK 10.0.401, Runtime .NET 10.0.12; installierte Workloads `maui-windows` 10.0.20/10.0.100, `android`, `ios`, `maccatalyst`; xunit 2.9.3, xunit.runner.visualstudio 3.1.4, Microsoft.NET.Test.Sdk 17.14.1, coverlet.collector 6.0.4, FlaUI.UIA3 5.0.0. Umgebungsvariablen wie in CI: `IncludeIosTarget=false`, `IncludeAndroidTarget=false`.
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `src/Reporter.Tests/Reporter.Tests.csproj` (net10.0, xunit) — Unit-/Integrationstests. Befehl aus CI-Job `build & test` in `.github/workflows/pr-staging-ci.yml` (Zeilen 123–129).
  - `src/Reporter.E2ETests/Reporter.E2ETests.csproj` (net10.0-windows10.0.19041.0, xunit + FlaUI UIA3) — UI-Smoke-Tests gegen die echte App. Vorgehen aus `scripts/Run-E2ETests.ps1`: App zunächst als Debug/win-x64 bauen, dann `dotnet test`; erfordert interaktive Windows-Desktop-Session (war vorhanden).

### Vorbereitende Befehle (keine Testläufe)

| Befehl | Zweck | Exit-Code | Nachweis |
|--------|-------|-----------|----------|
| `dotnet restore Reporter.sln -r win-x64` | Restore aller Projekte | 0 | Konsolenausgabe |
| `dotnet build src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-restore` | Release-Build des Testprojekts (Reporter.Core/Reporter.Data transitiv) für `--no-build`-Testlauf; 0 Warnungen/0 Fehler | 0 | Konsolenausgabe |
| `dotnet build src/Reporter/Reporter.csproj -c Debug -f net10.0-windows10.0.19041.0 -r win-x64` | Debug-Build der MAUI-App als E2E-Voraussetzung (`Reporter.exe`); 0 Warnungen/0 Fehler | 0 | [e2e-app-build.log](test-results/e2e-app-build.log) |

*Abweichung zur CI:* Statt `dotnet build Reporter.sln --configuration Release` wurde der Release-Build nur für `Reporter.Tests` ausgeführt (das Testprojekt referenziert die MAUI-App nicht; die getesteten Assemblies sind identisch). Der E2E-App-Build folgt exakt `Run-E2ETests.ps1`.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit/Integration (Release) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 492 | 0 | 0 | [unit-tests.trx](test-results/unit-tests.trx), [unit-tests-console.log](test-results/unit-tests-console.log), [coverage.cobertura.xml](test-results/coverage.cobertura.xml) |
| E2E Smoke (Debug) | `dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0 --logger "trx;LogFileName=e2e-results.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 7 | 0 | 0 | [e2e-tests.trx](test-results/e2e-tests.trx), [e2e-tests-console.log](test-results/e2e-tests-console.log) |

### Nachgewiesene bestehende Testfehler

Keine — beide Suiten liefen vollständig und ohne Fehlschlag durch (492/492 bzw. 7/7). Es wurden keine Tests übersprungen, deaktiviert oder abgebrochen.

### Testlücken und Ausführungsprobleme

- Keine Ausführungsprobleme; beide Suiten vollständig gelaufen.
- **Inhaltliche Testlücke bezogen auf die Anforderung:** Es existiert kein Test für Atom 0.3 (`http://purl.org/atom/ns#`). `TestFeedXml` kann nur RSS 2.0 und Atom 1.0 erzeugen. Der bestehende Parse-Fehlerpfad ist durch `SyncFeedAsync_InvalidXml_SetsError` und `SyncFeedAsync_InvalidXml_ClassifiedAsParse` abgedeckt.
- E2E-Suite: läuft nur in interaktiver Windows-Desktop-Session (UIA3) — Voraussetzung war hier gegeben; in headless Umgebungen nicht ausführbar (Fixture meldet dann „requires an interactive Windows desktop session").

## Testklassen

### `FeedSyncServiceTests`
Datei: `src/Reporter.Tests/FeedSyncServiceTests.cs` — 38 Facts, Integrationstests gegen In-Memory-SQLite (`TestDbContextFactory`) mit `FakeHttpMessageHandler`.

- `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk` — RSS-Feed erzeugt Items, `FeedHealth.Ok`, SyncLog abgeschlossen
- `SyncFeedAsync_Duplicates_SkipsExistingItems` — Dedup über `GuidOrHash` bei Folge-Sync
- `SyncFeedAsync_DuplicatesWithinSameDocument_InsertsOnce` — Dedup innerhalb eines Dokuments
- `SyncFeedAsync_MixedNewAndExisting_BatchInsertsOnlyNew` — Batch-Insert nur neuer Items
- `SyncFeedAsync_Unreachable_KeepsItemsAndLogsError` — Netzwerkfehler → `Error`, Bestand bleibt
- `SyncFeedAsync_InvalidXml_SetsError` — unparsebares Dokument → `Error` + SyncLog
- `SyncFeedAsync_FewerItems_SetsWarning` — > 50 % Itemschwund → `Warning`
- `SyncFeedAsync_NoNewItemsForThirtyDays_SetsWarning` — 30-Tage-Regel → `Warning`
- `SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth` — Aggregation über Feeds
- `SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog` — Offline: Fehler ohne SyncLog/Health-Änderung
- `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange` — Offline: kein HTTP-Aufruf
- `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems` — Notification je neuem Item über echte `NotificationService`-Kette
- `SyncFeedAsync_SummaryMode_SendsSingleSummaryNotification` — Summary-Modus: eine Notification
- `SyncFeedAsync_NoNewItems_DoesNotNotify` — kein erneutes Benachrichtigen
- `SyncFeedAsync_FeedDisabled_NoNotifications` — per-Feed-Schalter
- `SyncFeedAsync_NotificationThrows_SyncStillSucceeds` — Notification-Fehler verfälscht Sync nicht
- `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument` — URL-Platzhalter → Dokumenttitel
- `SyncFeedAsync_WhenTitleIsHostPlaceholder_UpdatesTitleFromFeedDocument` — Host-Platzhalter → Dokumenttitel
- `SyncFeedAsync_WhenTitleIsFileNamePlaceholder_UpdatesTitleFromFeedDocument` — Dateiname-Platzhalter → Dokumenttitel
- `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle` — gesetzter Titel bleibt
- `SyncFeedAsync_KeywordTitleMatch_NotSaved` — Keyword-Treffer im Titel wird verworfen (RSS)
- `SyncFeedAsync_KeywordContentHtmlMatch_NotSaved` — Keyword-Treffer im Content (RSS)
- `SyncFeedAsync_KeywordNoMatch_SavesNormally` — Nicht-Treffer normal gespeichert
- `SyncFeedAsync_EmptyKeywords_SavesAll` — leere Keyword-Liste unverändert
- `SyncFeedAsync_KeywordFiltered_NotNotified` — gefiltertes Item löst keine Notification aus
- `SyncFeedAsync_KeywordFiltered_ResyncStaysFiltered` — Filter bleibt bei Re-Sync
- `SyncFeedAsync_KeywordFiltered_LogCountsFiltered` — `filteredCount` in `SyncLog`/`SyncResult`
- `SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved` — **einziger Atom-Test, deckt Atom 1.0 (`http://www.w3.org/2005/Atom`) über `TestFeedXml.Atom` ab**
- `SyncFeedAsync_MissingFavicon_BackfillsFromFeedAuthority` — Favicon-Backfill, `alternate`-Fallback auf Feed-Authority
- `SyncFeedAsync_ExistingFavicon_SkipsLookup` — kein Lookup bei vorhandenem Favicon
- `SyncFeedAsync_FaviconLookupFails_SyncStillSucceeds` — Lookup-Fehler isoliert
- `SyncFeedAsync_MissingFavicon_ForwardsCancellationTokenToIconLookup` — CancellationToken-Weitergabe
- `SyncFeedAsync_Failure_PersistsErrorKindAndMessage` — `LastErrorKind`/`LastErrorMessage` persistiert
- `SyncFeedAsync_Success_ClearsLastError` — Fehlerfelder nach Erfolg zurückgesetzt
- `SyncFeedAsync_HttpFeedNetworkFailure_ClassifiedAsInsecureHttpBlocked` — `http`-URL ohne Antwort
- `SyncFeedAsync_HttpStatusError_ClassifiedAsHttpStatus` — Nicht-Erfolgs-Statuscode
- `SyncFeedAsync_InvalidXml_ClassifiedAsParse` — **`XmlException` → `FeedSyncErrorKind.Parse` (der Pfad, in dem Atom 0.3 aktuell landet)**
- `SyncFeedAsync_HttpsFeedNetworkFailure_ClassifiedAsNetwork` — `https`-URL ohne Antwort

### `FeedSyncServiceTests_DebugLog`
Datei: `src/Reporter.Tests/FeedSyncServiceTests_DebugLog.cs` — 2 Facts.

- `SyncFeedAsync_Failure_LogsErrorEntry` — Sync-Fehler schreibt `DebugLogCategory.Sync`/`DebugLogLevel.Error`-Eintrag
- `SyncFeedAsync_Failure_WithoutDebugLogService_StillReportsError` — optionaler Dienst darf `null` sein

### `FeedSyncErrorKindTests`
Datei: `src/Reporter.Tests/FeedSyncErrorKindTests.cs` — 6 Facts.

- `Classify_HttpRequestExceptionOnHttpUrl_ReturnsInsecureHttpBlocked`
- `Classify_HttpRequestExceptionWithStatusCode_ReturnsHttpStatus`
- `Classify_HttpRequestExceptionWithStatusCodeOnHttpUrl_ReturnsHttpStatus`
- `Classify_HttpRequestExceptionWithoutStatusCode_ReturnsNetwork`
- `Classify_XmlException_ReturnsParse` — **`XmlException` → `Parse`**
- `Classify_OtherException_ReturnsUnknown`

### `ServiceCollectionTests`
Datei: `src/Reporter.Tests/ServiceCollectionTests.cs` — relevant: `AddReporterServices_ResolvesFeedSyncService` spiegelt die `MauiProgram`-DI-Registrierung des `FeedSyncService` mit vollem Konstruktor.

### `SmokeTests` (E2E)
Datei: `src/Reporter.E2ETests/SmokeTests.cs` — 7 Facts, FlaUI-UIA3 gegen `Reporter.exe` + `StubFeedServer` (RSS-2.0-Fixture `stub-feed.xml`).

- `AppStarts_FeedListRenders`, `AddButton_OpensSheet_FocusesUrlEntry`, `DirectAdd_FeedAppearsInListAndDatabase`, `FeedActionSheet_Rename_UpdatesTitle`, `FeedActionSheet_ChangeCategory_IncludingNone`, `Search_SubscribesResult_PersistsFeed`, `Search_SiteUrl_DiscoversFeedViaLinkTag`

## Hilfsmethoden

### `TestFeedXml`
Datei: `src/Reporter.Tests/TestFeedXml.cs`

- `Rss(items, channelTitle)` — baut RSS-2.0-Dokument (`<rss version="2.0">`, Items mit `title`/`link`/`guid`/`pubDate` (RFC-„R"-Format)/`description`)
- `Atom(entries, feedTitle)` — baut Atom-1.0-Dokument (`<feed xmlns="http://www.w3.org/2005/Atom">`, Entries mit `title`/`link href`/`id`/`updated` (ISO-„o")/`content type="html"`)
- **Kein `Atom03`-Helfer vorhanden** — Atom-0.3-Elemente (`feed` in `http://purl.org/atom/ns#`, `issued`/`modified`, `tagline`, `entry`) werden nirgends erzeugt.

### `TestDataSeeder`
Datei: `src/Reporter.Tests/TestDataSeeder.cs`

- `SeedFeedAsync(TestDbContextFactory)` — legt Feed-Entity direkt über DbContext an
- `SeedFeedAsync(IFeedRepository, url, notificationsEnabled)` — legt `Feed`-Domänenmodell über Repository an

### `FakeHttpMessageHandler`
Datei: `src/Reporter.Tests/FakeHttpMessageHandler.cs`

- Konfigurierbarer `HttpMessageHandler`; `SendAsync` delegiert an übergebene Response-Factory (sync oder `Task`, auch faulted für Fehlerfälle)

### `TestDbContextFactory`
Datei: `src/Reporter.Tests/TestDbContextFactory.cs`

- `IDbContextFactory<ReporterDbContext>` auf In-Memory-SQLite; legt Schema im Konstruktor an

### Fakes (Auswahl, sync-relevant)
- `FakeNotificationService` (`Calls`, `Exception`) — `INotificationService`
- `FakeLocalNotificationService` (`ShownNotifications`) — für echte `NotificationService`-Kette
- `FakeNetworkStatusService` (`IsOnline`) — `INetworkStatusService`
- `FakeFeedIconService` (`NextResult`, `NextException`, `RequestedSiteUrls`, `ReceivedCancellationTokens`) — `IFeedIconService`, nutzt intern `FeedSiteResolver.ResolveSiteUrl`
- `FakeDebugLogService` (`IsEnabled`, `LoggedEntries`) — `IDebugLogService`
- `TestSettingsHelper.SaveAsync` — Settings für Notification-Tests
