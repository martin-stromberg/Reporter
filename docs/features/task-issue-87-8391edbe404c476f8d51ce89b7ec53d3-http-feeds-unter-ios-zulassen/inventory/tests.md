<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-14, ca. 19:52–19:53 MESZ (UTC+2, lokale Rechnerzeit)
- **Branch und Commit-ID:** `task/issue-87-8391edbe404c476f8d51ce89b7ec53d3-http-feeds-unter-ios-zulassen` @ `652b37d77f36143f5d6c3b1752a44060f6ab371f` („Debuginformationen sammeln und per E-Mail versenden (#86)", 2026-09-14 19:30:58 +0200)
- **Uncommittete Änderungen im getesteten Stand:** nur untracked Verzeichnis `docs/features/task-issue-87-8391edbe404c476f8d51ce89b7ec53d3-http-feeds-unter-ios-zulassen/` (`requirement.md` und die hier erzeugten Inventory-Artefakte). Keine Änderungen an getracktem Produktiv- oder Testcode.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows (MINGW64_NT-10.0-26200, x86_64), .NET SDK `10.0.401`, Laufzeit `Microsoft.NETCore.App 10.0.12`; Testprojekt `net10.0`, xunit `2.9.3`, xunit.runner.visualstudio `3.1.4`, Microsoft.NET.Test.Sdk `17.14.1`, coverlet.collector `6.0.4`, Microsoft.EntityFrameworkCore.Sqlite `10.0.12`. Umgebungsvariablen wie in CI: `IncludeIosTarget=false`, `IncludeAndroidTarget=false`.
- **Ermittelte Testsuiten und Quellen der Testbefehle:** einzige Suite `src/Reporter.Tests/Reporter.Tests.csproj` (xunit; referenziert `Reporter.Core` und `Reporter.Data`, **nicht** die MAUI-App `Reporter`). Befehlsquellen: `.github/workflows/staging-ci.yml` (:120-126), `.github/workflows/pr-staging-ci.yml` (:125-126), `.github/workflows/release.yml` (:78); Coverage-Config `src/Reporter.Tests/coverlet.runsettings` (schließt `Reporter.Data.Migrations.*` aus). Das MAUI-Projekt wird in CI separat über `dotnet build Reporter.sln` gebaut; für den Testbefehl ist das nicht erforderlich.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"` (ohne `--no-build`, impliziter Release-Build der drei referenzierten Projekte) | Repo-Root `D:\Repositories\softwareschmiede\8391edbe-404c-476f-8d51-ce89b7ec53d3` | 0 | 462 | 0 | 0 | [TRX](test-results/test-results-baseline.trx), [Konsolenlog](test-results/dotnet-test-baseline-console.log), [Coverage](test-results/coverage-baseline.cobertura.xml) |

TRX-Zählerstand: `total=462, executed=462, passed=462, failed=0, notExecuted=0`. Gesamtdauer ~5,2 s.

### Nachgewiesene bestehende Testfehler

Keine — der Ausgangslauf ist vollständig grün; es wurden keine Testfehler nachgewiesen.

### Testlücken und Ausführungsprobleme

- **MAUI-App `src/Reporter` wurde in diesem Lauf nicht gebaut** (das Testprojekt referenziert nur `Reporter.Core`/`Reporter.Data`); Aussagen zur Übersetzbarkeit der App erfordern `dotnet build Reporter.sln` bzw. `.\scripts\Run-StaticChecks.ps1`.
- **Keine Unit-Abdeckung für iOS/ATS-Verhalten:** Alle Tests laufen auf `net10.0` unter Windows; ATS-Blockierung, `NSUrlSessionHandler` und `Info.plist`-Wirkung sind nicht testbar — Verifikation nur manuell auf iOS/MacCatalyst (siehe `scripts/iOS-Deployment.ps1`).
- **Keine UI-Tests:** `FeedsPage`-Interaktionen (Action Sheet, `DisplayAlert`) sind nur manuell verifizierbar; `docs/help/anwendung/mobile-ui-design.md` und `test-results.md` sind die dokumentierten Ablageorte für Screenshots.
- Coverage wurde erhoben ([coverage-baseline.cobertura.xml](test-results/coverage-baseline.cobertura.xml)); der CI-Schwellenwert (70 % Zeilenabdeckung) wurde lokal nicht ausgewertet.

## Testklassen (anforderungsrelevant)

### `FeedSyncServiceTests` (`src/Reporter.Tests/FeedSyncServiceTests.cs`, 32 Tests)

Integrationstests gegen SQLite-In-Memory (`TestDbContextFactory`) mit `FakeHttpMessageHandler`. Setup-Hilfen: `CreateService(content, statusCode, …)`, `CreateFailingService(exception, …)`, `CreateNotificationService(...)`.

- `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk` — RSS-Sync legt Items an, Health = OK
- `SyncFeedAsync_Duplicates_SkipsExistingItems` / `_DuplicatesWithinSameDocument_InsertsOnce` / `_MixedNewAndExisting_BatchInsertsOnlyNew` — Dedupe-Verhalten
- `SyncFeedAsync_Unreachable_KeepsItemsAndLogsError` — HTTP-Fehlschlag: Items bleiben, Fehler wird geloggt (relevante Stelle für Fehlerpersistenz)
- `SyncFeedAsync_InvalidXml_SetsError` — Parsefehler → Error
- `SyncFeedAsync_FewerItems_SetsWarning` / `_NoNewItemsForThirtyDays_SetsWarning` — Warning-Heuristiken
- `SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth` / `_WhenOffline_ReturnsErrorWithoutSyncLog` — Mehr-Feed-Sync, Offline-Pfad
- `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange` — Offline: kein Health-Change
- `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems` / `_SummaryMode_SendsSingleSummaryNotification` / `_NoNewItems_DoesNotNotify` / `_FeedDisabled_NoNotifications` / `_NotificationThrows_SyncStillSucceeds` — Benachrichtigungspfad
- `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument` / `_WhenTitleIsHostPlaceholder_...` / `_WhenTitleIsFileNamePlaceholder_...` / `_WhenTitleIsSet_DoesNotOverwriteTitle` — Titel-Auflösung
- `SyncFeedAsync_KeywordTitleMatch_NotSaved` / `_KeywordContentHtmlMatch_NotSaved` / `_KeywordNoMatch_SavesNormally` / `_EmptyKeywords_SavesAll` / `_KeywordFiltered_NotNotified` / `_KeywordFiltered_ResyncStaysFiltered` / `_KeywordFiltered_LogCountsFiltered` / `_KeywordTitleMatch_AtomFeed_NotSaved` — Keyword-Filter
- `SyncFeedAsync_MissingFavicon_BackfillsFromFeedAuthority` / `_ExistingFavicon_SkipsLookup` / `_FaviconLookupFails_SyncStillSucceeds` / `_MissingFavicon_ForwardsCancellationTokenToIconLookup` — Favicon-Backfill (nutzt Feed-URL-Authority via `FeedSiteResolver`)

**Nicht abgedeckt:** Scheme-Upgrade `http → https`, kein Retry-Verhalten, keine Fehlermeldungs-Persistenz am `Feed` (Variante A), kein ATS-Fehlerbild.

### `FeedSyncServiceTests_DebugLog` (`src/Reporter.Tests/FeedSyncServiceTests_DebugLog.cs`, 2 Tests)

- `SyncFeedAsync_Failure_LogsErrorEntry` — Fehlschlag schreibt `IDebugLogService`-Fehlereintrag (`ex.ToString()`)
- `SyncFeedAsync_Failure_WithoutDebugLogService_StillReportsError` — optionaler Debug-Dienst

### `FeedsViewModelTests` (`src/Reporter.Tests/FeedsViewModelTests.cs`, 69 Tests)

Integrationstests mit echten Repositories (`TestDbContextFactory`) plus `FakeFeedSearchService`, `FakeFeedIconService`, `FakeNetworkStatusService` und einem verschachtelten `FakeFeedSyncService` (`:1644`). Setup-Hilfen: `CreateViewModel()`, `SeedFeedAsync(title, url, notificationsEnabled)`.

Relevante Gruppen:

- Sync-/Fehleranzeige: `RefreshCommand_InvokesSyncService_AndReloadsList`, `RefreshAllCommand_InvokesSyncService_AndReloadsList`, `RefreshCommand_WhenSyncReturnsError_SetsLocalizedSyncErrorMessage` (prüft `SyncErrorMessage == AppResources.SyncStatusError`), `RefreshAllCommand_WhenSyncThrows_SetsLocalizedSyncError`, Offline-Fälle (`_WhenOffline_SkipsSyncWithoutError`, `_ResetsPresetIsSyncing`), `ConnectivityChanged_ClearsSyncErrorMessage`, `ConnectivityChanged_UpdatesIsOnline`, `ConnectivityChanged_UpdatesSearchCommandCanExecute`
- Add-Pfade/URL-Handling: `DirectAddCommand_WhenOffline_PersistsFeedWithDefaults`, `DirectAddCommand_DomainInput_NormalizesToHttpsUrl` (einziger HTTPS-Upgrade-Test), `DirectAddCommand_WhenUrlInvalid_SetsErrorAndKeepsSheetOpen`, `DirectAddCommand_WhenDuplicate_*` (Duplikatprüfung scheme-sensitiv, exakter Vergleich), `DirectAddCommand_InEditMode_DoesNotAddFeed`, `DirectAddCommand_StoresFaviconUrl`, `DirectAddCommand_IconLookupFails_FeedStillAdded`, `DirectAddCommand_Offline_SkipsIconLookup`
- Suche: `SearchCommand_PopulatesSearchResults`, `_DomainInput_NormalizesToHttpsUrl`, `_FreeText_...`, `_WhenEmpty_...`, `_WhenOffline_...`, `_InEditMode_...`, `_WhenServiceUnavailable_*`, `_NoResultsAndValidUrl_Confirmed_AddsFeedWithFileNameTitle`, `_NoResultsAndValidUrl_Declined_KeepsFormState`, `_WhenUrlChangesDuring*_DiscardsStale*`, `SearchCommand_WhenConfirmDirectAddThrows_InvokesDialogOnlyOnce` (Callback-Muster `ConfirmDirectAddAsync`)
- Subscribe: `SubscribeResultCommand_PersistsFeedFromResult`, `_WhenTitleEmpty_StoresFileNameAsPlaceholder`, `_WhenDuplicate_SetsErrorAndDoesNotAdd`, `_UsesSiteUrlForFaviconLookup`
- Edit-/Save-Pfad: `SaveCommand_ExistingFeed_PersistsNotificationsEnabled`, `SaveCommand_EditMode_WhenInvalidUrl_KeepsSheetOpenAndSetsError`, `_WhenDuplicate_...`, `_PreservesCategoryId`, `EditAsync_OpensSheetInEditMode`, `EditCommand_PopulatesFeedNotificationsEnabled`, Sheet-Zustands-Tests
- Rename/Category: `RenameFeedAsync_*` (inkl. `_PreservesFaviconUrl`), `ChangeFeedCategoryAsync_*`, `DeleteCommand_ResetsFeedNotificationsEnabled`
- `MakeUniqueOptionLabels_*` — Action-Sheet-Label-Hilfe

**Nicht abgedeckt:** Fehlerdetails-Command/-Property, Anzeige der letzten `SyncLog`-/`Feed`-Fehlermeldung, HTTPS-Upgrade expliziter `http://`-Eingaben.

### `SyncLogRepositoryTests` (`src/Reporter.Tests/SyncLogRepositoryTests.cs`, 6 Tests)

`AddAsync_ThenGetByIdAsync_ReturnsSyncLog`, `GetAllAsync_ReturnsSyncLogsOrderedByStartedAtDescending`, `GetLatestAsync_ReturnsNewestSyncLogsLimited`, `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesSyncLog`, `GetByIdAsync_NonExisting_ReturnsNull`. **Keine** Feed-bezogene Abfrage getestet (existiert nicht).

### `FeedRepositoryTests` (`src/Reporter.Tests/FeedRepositoryTests.cs`, 9 Tests)

CRUD + Sortierung, `GetAllWithDetailsAsync_ProjectsNotificationsEnabled`/`_ProjectsFaviconUrl` (Projektions-Roundtrips — Muster für ein neues `LastErrorMessage`-Feld), `UpdateAsync_PersistsFaviconUrl`, `DeleteAsync_CascadeDeletesSavedItems`.

### `ReporterDbContextTests_Schema` / `ReporterDbContextTests_Persistence` (2 + 4 Tests)

`EnsureCreatedAsync_CreatesQueryableTables`, `DebugLogEntries_MappedToExpectedTable`; `SaveChangesAsync_PersistsFeedWithCategory`, `Items_WithInclude_ReturnsFeedAndCategory`, `Settings_DebugCollectionEnabled_PersistRoundtrip`, `DebugLogEntry_PersistRoundtrip`. Testen `EnsureCreated`, nicht die Migrationen.

### `FeedSearchServiceTests` (`src/Reporter.Tests/FeedSearchServiceTests.cs`, 17 Tests)

Verzeichnis-Mapping, Sortierung/Dedupe (`FeedUrl`, OrdinalIgnoreCase), Autodiscovery-Fallbacks (`_FallsBackToAutodiscovery_LinkTags`, `_ProbesStandardPaths`), Redirect-Final-URLs, Fehler-/Timeout-Pfade (`FeedSearchUnavailableException`), Cancellation. Verschachtelter `StubHttpMessageHandler` (`:400`) mit Routing-Hilfen `On`/`OnExact`/`OnExactRedirect`. **Keine** Tests zu `http`-Ergebnis-Behandlung/Upgrade.

### `FeedIconServiceTests` (`src/Reporter.Tests/FeedIconServiceTests.cs`, 11 Tests)

Link-Tag-Parsing, `/favicon.ico`-Fallback, isolierte Fehler (`_ReturnsNullOnFailure`, `_SiteRequestFails_StillTriesFallback`, `_InvalidSiteUrl_ReturnsNull`), `TryFindFaviconUrlAsync`-Auflösung inkl. Feed-Authority-Fallback und Cancellation.

### `FeedListItemTests` (`src/Reporter.Tests/FeedListItemTests.cs`, 3 Tests)

`FeedInitial`-Fallback-Verhalten.

### `ServiceCollectionTests` (`src/Reporter.Tests/ServiceCollectionTests.cs`, 4 Tests)

DI-Resolution der Repositories und Services (`AddReporterRepositories_ResolvesAllRepositories` u. a.) — relevant, falls neue Registrierungen/Abhängigkeiten hinzukommen.

### `FeedTitleFallbackTests` (`src/Reporter.Tests/FeedTitleFallbackTests.cs`, 8 Tests)

Fallback-Titel-Ableitung aus Feed-URLs.

## Hilfsmethoden

### `FakeHttpMessageHandler` (`src/Reporter.Tests/FakeHttpMessageHandler.cs`)

- `FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>)` / `(Func<HttpRequestMessage, Task<HttpResponseMessage>>)` — beliebige Antworten bzw. faulted Tasks (z. B. `HttpRequestException` für ATS-artige Fehlschläge); `SendAsync` delegiert an die Factory.

### `TestDbContextFactory` (`src/Reporter.Tests/TestDbContextFactory.cs`)

- Konstruktor: SQLite-In-Memory (`Mode=Memory;Cache=Shared`), `EnsureCreated`; `CreateDbContext()`; `Dispose`.

### `TestDataSeeder` (`src/Reporter.Tests/TestDataSeeder.cs`)

- `SeedFeedAsync(TestDbContextFactory)` — Entity-Direktseed (`https://example.com/feed`)
- `SeedFeedAsync(IFeedRepository, url, notificationsEnabled)` — Seed über Repository

### `TestFeedXml` (`src/Reporter.Tests/TestFeedXml.cs`)

- `Rss(items, channelTitle)` / `Atom(entries, feedTitle)` — Feed-XML-Builder

### Weitere Fakes

- `FakeFeedSyncService` (Datei + verschachtelte Variante in `FeedsViewModelTests`/`UnreadViewModelTests`): `NextResult`, `NextException`, `LastFeedId`, `SyncAllCalled`, `SyncAllBlocker`, `SyncAllException`
- `FakeFeedSearchService`: `NextResults`, `NextException`, `PendingResult`, `LastQuery`, `CallCount`
- `FakeFeedIconService`: `NextResult`, `NextException`, `RequestedSiteUrls`, `ReceivedCancellationTokens` (delegiert an echten `FeedSiteResolver`)
- `FakeNetworkStatusService`, `FakeNotificationService`, `FakeLocalNotificationService`, `FakeDebugLogService`, `DelegatingItemRepository`
