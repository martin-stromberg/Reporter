<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Bestandsaufnahme und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-13, ca. 08:16–08:18 Uhr MESZ (UTC+02:00)
- **Branch und Commit-ID:** `task/issue-59-efbace56737047f4b10610b83d0e137c-erweiterung-der-hinzufuegenfun`, Commit `27f4795f8b51f769fda7c82e2116879d08665bf9` („fix: Nacharbeiten Issue #59 — Stale-Input-Check extrahiert, Host-Platzhalter-Titel beim Sync auflösen")
- **Uncommittete Änderungen im getesteten Stand:** nur untracked `docs/features/task-issue-59-efbace56737047f4b10610b83d0e137c-erweiterung-der-hinzufuegenfun/` (requirement.md, Tasks-Doku, diese Bestandsaufnahme). Keine Änderungen an Produktivcode, Tests oder Testkonfiguration.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows, .NET SDK `10.0.401` (Testprojekt `net10.0`, xunit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, EF Core Sqlite In-Memory), Node.js `v24.15.0`, npm `11.10.0`.
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `dotnet test` (xunit, `src/Reporter.Tests/Reporter.Tests.csproj`) — Quellen: `README.md` Zeile 154 (`dotnet test Reporter.sln`), `CONTRIBUTING.md` Zeile 22, CI `.github/workflows/pr-staging-ci.yml` Zeile 125 (`dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings`). Ausgeführt wurde das projektspezifisch vorgegebene `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` (gleiche Suite, ohne Coverage-Settings).
  - `npm test` (`node --test "scripts/*.test.mjs"`, Release-Tooling-Skripte) — Quellen: `package.json` `scripts.test`, `README.md` Zeilen 155/159, `CONTRIBUTING.md` Zeile 22.
  - Hinweis zur Abdeckung: Der `dotnet test`-Aufruf auf das Testprojekt baut nur `Reporter.Core`, `Reporter.Data` und `Reporter.Tests` — das MAUI-App-Projekt `src/Reporter` (inkl. `FeedsPage.xaml`) wird dabei nicht kompiliert; UI-Code ist durch die Suite nicht abgedeckt.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` | Repo-Root | 0 | 280 | 0 | 0 | [dotnet-test.log](test-results/dotnet-test.log) |
| 2 | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release --no-build --logger "trx;LogFileName=dotnet-test-baseline.trx" --results-directory docs/features/.../inventory/test-results` | Repo-Root | 0 | 280 | 0 | 0 | [dotnet-test-trx.log](test-results/dotnet-test-trx.log), [dotnet-test-baseline.trx](test-results/dotnet-test-baseline.trx) (maschinenlesbarer Report) |
| 3 | `npm test` (`node --test "scripts/*.test.mjs"`) | Repo-Root | 0 | 36 | 0 | 0 | [npm-test.log](test-results/npm-test.log) |

Lauf 2 ist ein Wiederholungslauf von Lauf 1 auf dem unveränderten Stand (gleiche Binaries via `--no-build`), ergänzt um den TRX-Report als maschinenlesbaren Nachweis.

### Nachgewiesene bestehende Testfehler

Keine — alle 280 .NET-Tests und alle 36 node:test-Tests sind im Ausgangslauf erfolgreich.

### Testlücken und Ausführungsprobleme

- Die UI (`FeedsPage.xaml`, `FeedsPage.xaml.cs` — `OnFeedTapped`, `OnSearchResultTapped`, `ConfirmDirectAddAsync`) wird von keiner Testsuite kompiliert oder ausgeführt; ActionSheet-/Dialog-Verhalten ist ungetestet (plattformabhängiges Code-Behind ohne UI-Test-Projekt).
- `coverlet.runsettings`/Coverage wurde lokal nicht erhoben (nur CI).
- Keine übersprungenen, deaktivierten oder abgebrochenen Tests festgestellt (0 skipped in beiden Suiten).

## Testklassen

### `FeedsViewModelTests`
Datei: `src/Reporter.Tests/FeedsViewModelTests.cs` — Integrations-Tests gegen SQLite-In-Memory (`TestDbContextFactory`) mit `FeedRepository`/`CategoryRepository` echt, Sync/Suche/Netzwerk als Fakes.

- `RefreshCommand_InvokesSyncService_AndReloadsList` — Einzel-Sync ruft `SyncFeedAsync` mit der Feed-ID, Liste neu geladen.
- `RefreshAllCommand_InvokesSyncService_AndReloadsList` — `SyncAllAsync` wird aufgerufen.
- `RefreshCommand_WhenSyncReturnsError_SetsLocalizedSyncErrorMessage` — Fehler → `SyncErrorMessage = SyncStatusError`, Formular-Fehlerkanal unberührt.
- `RefreshAllCommand_WhenOffline_SkipsSyncWithoutError` — Offline: kein Sync, keine Fehlermeldung.
- `RefreshAllCommand_WhenIsSyncingPresetByBinding_StillSyncs` / `RefreshCommand_WhenIsSyncingPresetByBinding_StillSyncs` — Reentrancy-Guard unabhängig vom gebundenen `IsSyncing`.
- `RefreshAllCommand_WhenOffline_ResetsPresetIsSyncing` / `RefreshCommand_WhenOffline_ResetsPresetIsSyncing` — Offline-Pfad setzt vorbelegtes `IsSyncing` zurück.
- `RefreshCommand_WhenOffline_SkipsSyncWithoutError` — Einzel-Sync offline übersprungen.
- `RefreshAllCommand_WhenSyncThrows_SetsLocalizedSyncError` — Exception → generische lokalisierte Meldung.
- `ConnectivityChanged_ClearsSyncErrorMessage` — Konnektivitätswechsel leert Sync-Fehler.
- `ConnectivityChanged_UpdatesIsOnline` — `IsOnline` folgt dem Event in beide Richtungen.
- `EditCommand_PopulatesFeedNotificationsEnabled` — Edit lädt das Notification-Flag ins Formular.
- `SaveCommand_NewFeed_PersistsNotificationsEnabledFalse` / `SaveCommand_ExistingFeed_PersistsNotificationsEnabled` / `SaveCommand_ResetsFeedNotificationsEnabled` — `FeedNotificationsEnabled` wird persistiert bzw. nach Speichern/Löschen auf `true` zurückgesetzt.
- `SaveCommand_NewFeed_PersistsHealthStatusOk` — Neuer Feed erhält `FeedHealth.Ok`.
- `DeleteCommand_ResetsFeedNotificationsEnabled` — Löschen des Edit-Feeds setzt das Formular zurück.
- `SearchCommand_PopulatesSearchResults` — Query getrimmt an Service, Trefferliste + `ShowSearchResults`.
- `SearchCommand_DomainInput_NormalizesToHttpsUrl` — `example.com` → `https://example.com`.
- `SearchCommand_FreeText_DoesNotCallService_ShowsEmptyResults` — Freitext ohne Service-Call, keine Direkt-Hinzufügen-Frage.
- `SearchCommand_WhenEmpty_DoesNotCallService` — Leereingabe bricht ab.
- `SearchCommand_WhenOffline_SkipsSearchWithoutError` — Offline: `CanExecute=false`, kein Service-Call.
- `SearchCommand_WhenServiceUnavailable_SetsSearchErrorMessage` / `..._AndDomainInput_SetsRetryHint` — `FeedSearchUnavailable` vs. `FeedSearchUnavailableRetry`.
- `SearchCommand_WhenConfirmDirectAddThrows_InvokesDialogOnlyOnce` — Dialogfehler wird geschluckt, genau ein Aufruf.
- `SearchCommand_NoResultsAndValidUrl_Confirmed_PrefillsTitleFromHost` — bestätigtes Direkt-Hinzufügen belegt **`NewTitle = Host`** vor (aktueller Fallback; wird durch Dateinamen-Regel ersetzt).
- `SearchCommand_NoResultsAndValidUrl_Declined_KeepsFormState` — Ablehnung lässt Formular unverändert.
- `SearchCommand_NoResultsAndDomainInput_DoesNotInvokeConfirm` — Domain ohne Treffer ohne Direkt-Hinzufügen-Dialog.
- `CloseSearchResultsCommand_ExitsResultsView` — Trefferansicht verlassen.
- `NewUrl_Changed_ClearsSearchState` — URL-Änderung leert Treffer/Fehler.
- `SearchCommand_WhenUrlChangesDuringSearch_DiscardsStaleResults` / `..._DuringFailedSearch_DiscardsStaleError` — Stale-Input-Verwerfung.
- `SubscribeResultCommand_PersistsFeedFromResult` — persistiert Titel aus Treffer, `SelectedCategory`→`CategoryId`, `FeedNotificationsEnabled`, `HealthStatus.Ok`; Form-Reset.
- `SubscribeResultCommand_WhenTitleEmpty_StoresFeedUrlAsPlaceholder` — **aktueller Fallback `Title = FeedUrl`** (volle URL); soll Dateiname werden.
- `SubscribeResultCommand_WhenDuplicate_SetsErrorAndDoesNotAdd` — Dublettenprüfung.
- `ConnectivityChanged_UpdatesSearchCommandCanExecute` — `SearchCommand.CanExecute` + Fehlerkanal-Reset.

Private nested Klasse `FakeFeedSyncService` (eigene, separate Implementierung mit `LastFeedId`, `SyncAllCalled`, `NextResult`, `NextException`) — nicht zu verwechseln mit der gleichnamigen Datei `FakeFeedSyncService.cs` (siehe Hilfsmethoden).

### `FeedSyncServiceTests`
Datei: `src/Reporter.Tests/FeedSyncServiceTests.cs` — Integrations-Tests mit `FakeHttpMessageHandler`.

- `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk` / `..._Duplicates_SkipsExistingItems` — Item-Anlage, Dedupe via `GuidOrHash`.
- `SyncFeedAsync_Unreachable_KeepsItemsAndLogsError` / `..._InvalidXml_SetsError` — Fehlerpfade mit `SyncLog`.
- `SyncFeedAsync_FewerItems_SetsWarning` / `..._NoNewItemsForThirtyDays_SetsWarning` — `DetermineStatus`-Warnungen.
- `SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth` — Mehrfach-Sync.
- `SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog` / `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange` — Offline-Verhalten.
- `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems` / `..._SummaryMode_SendsSingleSummaryNotification` / `..._NoNewItems_DoesNotNotify` / `..._FeedDisabled_NoNotifications` / `..._NotificationThrows_SyncStillSucceeds` — Benachrichtigungspfad.
- `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument` — `Title == Url` wird durch `SyndicationFeed.Title` ersetzt.
- `SyncFeedAsync_WhenTitleIsHostPlaceholder_UpdatesTitleFromFeedDocument` — `Title == Host` (Direkt-Hinzufügen-Fallback) wird ersetzt.
- `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle` — manueller Titel bleibt erhalten.
- **Nicht vorhanden:** Test für Dateinamen-Platzhalter (`Title == letztes Pfadsegment`, z. B. `heise-atom.xml`) — neue Regel aus der Anforderung.

### `FeedSearchServiceTests`
Datei: `src/Reporter.Tests/FeedSearchServiceTests.cs` — Unit-Tests mit privatem `StubHttpMessageHandler`.

- Mapping/Sortierung: `SearchAsync_MapsDirectoryEntries_ToFeedSearchResults`, `..._SortsByMatchKindThenScoreThenFeedUrl`, `..._WhenResultFeedUrlEqualsQuery_RanksExactUrlFirst`.
- Autodiscovery: `..._DirectoryEmpty_FallsBackToAutodiscovery_LinkTags`, `..._ProbesStandardPaths`, `..._WhenQueryIsFeedDocument_ReturnsExactUrlResult`, `..._AfterRedirect_ReturnsFinalUrl`, `..._WhenStandardPathRespondsAfterRedirect_ReturnsFinalUrl`.
- Fehlerfälle: `..._DirectoryError_ButSiteReachable_ReturnsDiscoveredResults`, `..._BothSourcesFail_ThrowsFeedSearchUnavailableException`, `..._Timeout_ThrowsFeedSearchUnavailableException`, `..._WhenCallerCancels_ThrowsOperationCanceledException`, `..._MalformedDirectoryJson_FallsBackToAutodiscovery`.
- Dedupe/Fast-Path: `..._DirectoryDuplicates_DedupesByFeedUrl`, `..._DiscoveryDuplicates_DedupesByFeedUrl`, `..._DirectoryResultsPresent_SkipsAutodiscovery`, `..._EverythingEmpty_ReturnsEmptyList`.

### Weitere projektweite Testklassen (nicht direkt betroffen, Teil der 280er-Suite)

`ArticleHtmlSanitizerTests`, `AutoRefreshServiceTests`, `BaseViewModelConnectivityTests`, `CategoriesViewModelTests`, `CategoryRepositoryTests`, `FeedRepositoryTests`, `ItemRepositoryTests`, `KeywordMatcherTests`, `KeywordRepositoryTests`, `LaterViewModelTests`, `NotificationServiceTests`, `ReporterDbContextFactoryTests`, `ReporterDbContextTests_Persistence`, `ReporterDbContextTests_Schema`, `RetentionCleanupServiceTests`, `ServiceCollectionTests`, `SettingsRepositoryTests`, `SettingsValuesTests_AutoMarkRead`, `SettingsViewModelTests_E2E`, `SettingsViewModelTests_Keywords`, `SettingsViewModelTests_Load`, `SettingsViewModelTests_Persist`, `SyncLogRepositoryTests`, `UnreadViewModelTests`, `WebViewNavigationGuardTests`.

## Hilfsmethoden / Test-Doubles

### `TestDbContextFactory`
Datei: `src/Reporter.Tests/TestDbContextFactory.cs`
- `CreateDbContext` — `ReporterDbContext` auf shared-in-memory SQLite (`Mode=Memory;Cache=Shared`), Schema via `EnsureCreated`; `Dispose` schließt die Keep-Alive-Connection.

### `FakeFeedSearchService`
Datei: `src/Reporter.Tests/FakeFeedSearchService.cs`
- `NextResults`, `NextException`, `PendingResult` (TaskCompletionSource-Gate für In-Flight-Suchen), `LastQuery`, `CallCount`.

### `FakeFeedSyncService` (Datei)
Datei: `src/Reporter.Tests/FakeFeedSyncService.cs`
- `SyncAllException`, `SyncAllBlocker`, `SyncAllCallCount`; `SyncFeedAsync` gibt `SyncResult("OK", 0)` zurück. (Nicht von `FeedsViewModelTests` verwendet — dort existiert eine eigene nested Variante.)

### `FakeNetworkStatusService`
Datei: `src/Reporter.Tests/FakeNetworkStatusService.cs`
- `IsOnline` (settable), `RaiseConnectivityChanged` zum Auslösen des Events.

### `FakeLocalNotificationService`
Datei: `src/Reporter.Tests/FakeLocalNotificationService.cs`
- `IsSupported`, `AuthorizationResult`, `AuthorizationStatus`, `ShownNotifications`, `RequestAuthorizationCallCount`; Record `ShownNotification`.

### `FakeNotificationService`
Datei: `src/Reporter.Tests/FakeNotificationService.cs` — `INotificationService`-Double für die Sync-Tests.

### Sonstige
- `FeedsViewModelTests.SeedFeedAsync(title, url, notificationsEnabled)` — legt einen Feed direkt über `FeedRepository.AddAsync` an.
- `FeedSyncServiceTests.RssXml(items, channelTitle)` — baut RSS-2.0-Testdokumente; `CreateService`/`CreateFailingService`/`CreateNotificationService` — Service-Factorys; nested `FakeHttpMessageHandler`.
- `FeedSearchServiceTests.StubHttpMessageHandler` — `On`/`OnExact`/`OnExactRedirect`-Routen pro URL-Substring/Exaktmatch.
- `TestWaitHelper`, `TestDataSeeder`, `TestSettingsHelper` — allgemeine Helfer für andere Suites.
