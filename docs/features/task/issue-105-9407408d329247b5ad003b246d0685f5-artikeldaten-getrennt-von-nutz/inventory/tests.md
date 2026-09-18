<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Bestandsaufnahme

Bezug: Anforderung „Artikeldaten getrennt von Nutzerdaten speichern (iCloud-Backup)" (`requirement.md`).

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-18, ca. 03:45–03:50 MESZ (UTC+02:00, Windows-Zeitzone „W. Europe Standard Time")
- Branch und Commit-ID: `task/issue-105-9407408d329247b5ad003b246d0685f5-artikeldaten-getrennt-von-nutz` @ `3ff0bb402ff38e4ca58b535d8a2d1a3170df9a3f`
- Uncommittete Änderungen im getesteten Stand: nur untracked Dateien unter `docs/features/task/issue-105-.../` (`requirement.md`, `todo.md` und die hier erzeugten Inventory-Dateien/Logs) — keine Code- oder Teständerungen. `git status` meldet „nothing added to commit but untracked files present".
- Testumgebung und Runtime-/SDK-Versionen:
  - Windows (Git Bash), .NET SDK 10.0.401 (`C:\Program Files\dotnet\sdk`)
  - Installierte Workloads: `maui-windows` 10.0.20/10.0.100, `ios` 26.5.10318/10.0.100, `android` 36.1.69/10.0.100, `maccatalyst` 26.5.10318/10.0.100
  - Node.js v24.15.0, npm 11.10.0
  - `IncludeIosTarget`/`IncludeAndroidTarget` nicht gesetzt → `src/Reporter/Reporter.csproj`-Defaults: `IncludeIosTarget=true`, `IncludeAndroidTarget=false` → die App wurde für `net10.0-windows10.0.19041.0` und `net10.0-ios` gebaut.
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `src/Reporter.Tests` (`net10.0`, xunit) — projektüblicher Befehl laut `issue.md`: `dotnet test Reporter.sln --filter "Category!=E2E"`; CI (`staging-ci.yml`/`pr-staging-ci.yml`, Job `build-and-test`) nutzt `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx"`.
  - `src/Reporter.E2ETests` (`net10.0-windows10.0.19041.0`, FlaUI/UIA3) — alle 9 Tests tragen `[Trait("Category", "E2E")]`; projektüblicher Lauf über `.\scripts\Run-E2ETests.ps1` (Debug-Build der App + `dotnet test src/Reporter.E2ETests`).
  - Node-Skripttests — `npm test` (`package.json`: `node --test "scripts/*.test.mjs"`).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet test Reporter.sln --filter "Category!=E2E"` | Repo-Root (`D:\Repositories\softwareschmiede\9407408d-3292-47b5-ad00-3b246d0685f5`) | 0 | 531 (`Reporter.Tests.dll`, net10.0) | 0 | 0 | [Log](test-results/2026-09-18_dotnet-test-sln_category-ne-e2e.log) |
| 2 | `npm test` | Repo-Root | 0 | 36 | 0 | 0 | [Log](test-results/2026-09-18_npm-test.log) |
| 3 | `.\scripts\Run-E2ETests.ps1` | – | nicht ausgeführt | – | – | – | siehe Testlücken |

Anmerkungen zu Lauf 1:
- Debug-Konfiguration (Standard von `dotnet test` ohne `-c`), keine Coverage-/TRX-Logger — abweichend von der CI-Befehlszeile, aber identisch zum projektüblichen Befehl aus `issue.md`.
- `Reporter.E2ETests.dll` wurde gebaut und vom Testhost geladen; der Filter `Category!=E2E` schloss alle 9 E2E-Tests aus („Kein Test entspricht dem angegebenen Testfallfilter") — es wurden **0** E2E-Tests ausgeführt.
- Build der Lösung erfolgreich: `Reporter.Core`/`Reporter.Data`/`Reporter.Tests` (net10.0), `Reporter.E2ETests` (net10.0-windows10.0.19041.0), `Reporter` für `net10.0-ios` (iossimulator-x64) und `net10.0-windows10.0.19041.0` (win-x64).
- Keine maschinenlesbaren Reports (TRX/Coverage) erzeugt — die Zähler stammen aus der Konsolenausgabe des Testlaufs.
- Keine Zugangsdaten/Geheimnisse in den gespeicherten Logs enthalten.

### Nachgewiesene bestehende Testfehler

Es wurden keine Testfehler nachgewiesen: Lauf 1 (`dotnet test`): 531/531 erfolgreich, 0 fehlgeschlagen; Lauf 2 (`npm test`): 36/36 erfolgreich, 0 fehlgeschlagen.

### Testlücken und Ausführungsprobleme

1. **E2E-Suite nicht ausgeführt** (`src/Reporter.E2ETests`, 9 Tests mit `Trait("Category", "E2E")`: 7 × `SmokeTests`, 1 × `ArticleLinkTests`, 1 × `DemoSeedTests`). Grund: per Aufgabenanweisung nicht Teil der Bestandsaufnahme; das Skript `scripts/Run-E2ETests.ps1` benötigt zudem eine interaktive Windows-Desktop-Session (UIA3-Fensterattach). Im `dotnet-test`-Lauf wurden sie durch den Filter `Category!=E2E` ausgeschlossen — ein übersprungener/nicht ausgeführter Test ist weder Erfolg noch Beleg für einen Fehler.
2. **Kein Migrations-Pfad getestet:** Alle datenbanknahen Tests nutzen `TestDbContextFactory` mit `context.Database.EnsureCreated()` auf einer Shared-In-Memory-SQLite-DB — `EnsureCreated` erzeugt das Schema direkt aus dem Modell und umgeht die EF-Core-Migrationshistorie. `Migrate()`/`MigrateAsync()` (Update-Pfad Bestands-DB → neues Schema, `__EFMigrationsHistory`) wird von keinem Test ausgeübt; es existiert kein Test, der eine vorhandene DB-Datei mit altem Schema öffnet und migriert.
3. **`BackupExclusionService` faktisch nicht testbar auf Windows:** Die eigentliche Wirkung (`NSUrl.IsExcludedFromBackupKey`) steckt hinter `#if IOS`; auf Nicht-iOS-Targets ist der Service ein No-op. `ServiceCollectionTests.AddReporterServices_ResolvesBackupExclusion` prüft nur DI-Auflösung und dass `FakeBackupExclusionService` Aufrufe protokolliert.
4. **Ausschluss-Pfade nicht assertions-seitig abgedeckt:** `App.ExcludeDatabaseFilesFromBackup` (private statische Methode) schließt heute `reporter.db` + `-wal` + `-shm` aus — es gibt keinen Test, der die Menge der ausgeschlossenen Pfade verifiziert.
5. **`REPORTER_DB_PATH`-Verhalten ohne Unit-Test:** `MauiProgram.CreateMauiApp` ist in der Unit-Test-Suite nicht instanziierbar (MAUI-App); das Override-Verhalten wird nur durch die E2E-Fixture indirekt abgedeckt (dokumentiert in `docs/help/tests/`).
6. **Keine Migrationstests für den Update-Pfad der Anforderung:** Es existiert kein Test, der eine Bestands-`reporter.db` mit `content_html` öffnet und eine Aufteilung/Überführung prüft — erwartbar, da der Content-Speicher noch nicht existiert.
7. **`settings`-Seed prüft `EnsureCreated`-Pfad:** `ReporterDbContextTests_Schema.EnsureCreatedAsync_CreatesQueryableTables` erwartet 1 Settings-Zeile (`HasData`) — funktioniert nur im EnsureCreated-Pfad; unter Migrationen stellt `HasData` die Zeile in der Migration bereit.

## Testklassen

Relevante Testklassen für die betroffenen Bereiche (alle in `src/Reporter.Tests`, jeweils gegen `TestDbContextFactory` + echte Repositories oder Fakes):

### `ItemRepositoryTests`

Datei: `src/Reporter.Tests/ItemRepositoryTests.cs` — prüft `ItemRepository` gegen In-Memory-SQLite (inkl. `ContentHtml`-Roundtrip über `MapToModel`/`MapToEntity` und die Listenprojektion).

- `AddAsync_ThenGetByIdAsync_ReturnsItem` — Insert + Rücklesen eines Items.
- `GetAllAsync_ReturnsItemsOrderedByPublishedAtDescending` — Sortierung.
- `UpdateAsync_PersistsChanges` — Update aller Felder (inkl. `ContentHtml`).
- `DeleteAsync_RemovesItem` — Einzel-Löschung.
- `GetUnreadByDateAsync_ReturnsUnreadSortedByPublishedAtDescending` / `_AllRead_ReturnsEmpty` — Ungelesen-Filter.
- `GetUnreadByDateAsync_Paged_ReturnsPage` / `_Ascending_OrdersByPublishedAtAscending` / `_Ascending_NullPublishedAt_SortsFirst` — Paging/Sortierung.
- `GetUnreadByDateAsync_Paged_ProjectsReadingTimeText` / `GetUnreadByDateAsync_OneMinuteReadingTime_ProjectsEmpty` / `GetUnreadByDateAsync_ProjectsFeedFaviconUrl` — Projektion aus `ContentHtml` (Lesezeit, Favicon).
- `GetByFeedAsync_ReturnsOnlyMatchingFeed` / `_NonExistingFeed_ReturnsEmpty` — Feed-Filter.
- `GetByCategoryAsync_ReturnsOnlyMatchingCategory` / `_NoFeedsInCategory_ReturnsEmpty` — Kategorie-Filter.
- `GetSavedForLaterAsync_ReturnsOnlySaved` / `_Paged_ReturnsPage` / `_OrdersByPublishedAtDescending` / `_ProjectsReadingTimeText` — Merkliste inkl. Content-Ableitungen.
- `AddRangeAsync_InsertsAllItems` / `_EmptyList_DoesNothing` — Batch-Insert.
- `GetUnreadCountAsync_ReturnsCorrectCount` — Zähler.
- `MarkAsReadAsync_SetsIsRead` / `MarkAllAsReadAsync` (implizit über weitere Tests) — Status-Updates.
- `ToggleSavedForLaterAsync_TogglesState` / `_TogglesBackToFalse` — Merkliste-Toggle.
- `DeleteExpiredAsync_RemovesExpiredReadItems` / `_KeepsSavedForLaterItems` / `_KeepsUnreadItems` / `_KeepsNonExpiredItems` / `_UsesReadAtOverPublishedAt` / `_CancelledToken_ThrowsOperationCanceled` — Retention-Regeln (`ExecuteDelete`).
- `GetExpiredKeywordCandidatesAsync_UsesPublishedAtOverReadAt` / `_KeepsUnreadAndSaved` / `_FallsBackToReadAt` — Keyword-Kandidaten (liefern `ContentHtml` ans Matching).
- `DeleteRangeAsync_DeletesOnlyGivenIds` / `_EmptyList_ReturnsZero` — Batch-Löschung.

### `FeedSyncServiceTests`

Datei: `src/Reporter.Tests/FeedSyncServiceTests.cs` — prüft `FeedSyncService` mit `FakeHttpMessageHandler`, echten Repositories und `TestFeedXml`-Dokumenten.

- `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk` — Happy Path: Items werden mit Content gespeichert.
- `SyncFeedAsync_Duplicates_SkipsExistingItems` / `_DuplicatesWithinSameDocument_InsertsOnce` / `_MixedNewAndExisting_BatchInsertsOnlyNew` — `GuidOrHash`-Deduplizierung (bekannte Items werden komplett übersprungen — relevant für den Restore-/Backfill-Fall).
- `SyncFeedAsync_Unreachable_KeepsItemsAndLogsError` / `_InvalidXml_SetsError` / `_FewerItems_SetsWarning` / `_NoNewItemsForThirtyDays_SetsWarning` — Fehler-/Warning-Pfade.
- `SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth` / `_WhenOffline_ReturnsErrorWithoutSyncLog` / `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange` — Multi-Feed und Offline.
- `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems` / `_SummaryMode_SendsSingleSummaryNotification` / `_NoNewItems_DoesNotNotify` / `_FeedDisabled_NoNotifications` / `_NotificationThrows_SyncStillSucceeds` — Benachrichtigungsintegration.
- `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument` / `_WhenTitleIsHostPlaceholder_...` / `_WhenTitleIsFileNamePlaceholder_...` / `_WhenTitleIsSet_DoesNotOverwriteTitle` — Titelauflösung.
- `SyncFeedAsync_KeywordTitleMatch_NotSaved` / `_KeywordContentHtmlMatch_NotSaved` / `_KeywordNoMatch_SavesNormally` / `_EmptyKeywords_SavesAll` / `_KeywordFiltered_NotNotified` / `_KeywordFiltered_ResyncStaysFiltered` / `_KeywordFiltered_LogCountsFiltered` / `_KeywordTitleMatch_AtomFeed_NotSaved` — Keyword-Filterung auf `ContentHtml` vor dem Speichern.
- `SyncFeedAsync_MissingFavicon_BackfillsFromFeedAuthority` / `_ExistingFavicon_SkipsLookup` / `_FaviconLookupFails_SyncStillSucceeds` / `_MissingFavicon_ForwardsCancellationTokenToIconLookup` — Favicon-Backfill.
- `SyncFeedAsync_Failure_PersistsErrorKindAndMessage` / `_Success_ClearsLastError` / `_HttpFeedNetworkFailure_ClassifiedAsInsecureHttpBlocked` / `_HttpStatusError_ClassifiedAsHttpStatus` / `_InvalidXml_ClassifiedAsParse` / `_HttpsFeedNetworkFailure_ClassifiedAsNetwork` — `FeedSyncErrorKind`-Persistierung.
- `SyncFeedAsync_Atom03_CreatesItems_AndSetsHealthOk` / `_Atom03_MapsIssuedToPublishedAt` / `_Atom03_PlaceholderTitle_UpdatesTitleFromFeedDocument` — Atom-0.3-Normalisierung.

### `FeedSyncServiceTests_DebugLog`

Datei: `src/Reporter.Tests/FeedSyncServiceTests_DebugLog.cs`

- `SyncFeedAsync_Failure_LogsErrorEntry` — Sync-Fehler landet im `IDebugLogService`.
- `SyncFeedAsync_Failure_WithoutDebugLogService_StillReportsError` — optionaler Service darf fehlen.

### `RetentionCleanupServiceTests`

Datei: `src/Reporter.Tests/RetentionCleanupServiceTests.cs`

- `CleanupAsync_DeletesExpiredButKeepsSaved` — Retention-Löschung.
- `CleanupAsync_ZeroOrNegativeRetentionDays_Skips` (Theory) — `RetentionDays <= 0` → kein Cleanup.
- `CleanupAsync_RespectsConfiguredRetentionDays` — Cutoff-Berechnung.
- `CleanupAsync_CancelledToken_ThrowsOperationCanceled` — Abbruch.
- `CleanupAsync_DeletesKeywordMatchedExpired` — Keyword-Match auf `ContentHtml` löscht Kandidaten.
- `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved` — Invarianten.
- `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt` — Zeitstempel-Priorität.

### `ServiceCollectionTests`

Datei: `src/Reporter.Tests/ServiceCollectionTests.cs` — DI-Registrierungen (spiegelt `MauiProgram` nach).

- `AddReporterRepositories_ResolvesAllRepositories` — Repositories + `IDbContextFactory<ReporterDbContext>` auflösbar.
- `AddReporterServices_ResolvesFeedSearchService` / `_ResolvesFeedSyncService` / `_ResolvesDebugServices` / `_ResolvesAutoRefreshService` / `_ResolvesScheduledSyncRunner` / `_ResolvesDemoContentService` — Service-Auflösungen.
- `AddReporterServices_ResolvesBackupExclusion` — `DatabasePath`-Singleton + `FakeBackupExclusionService`: prüft, dass `ExcludeFromBackup(databasePath.FilePath)` den Pfad protokolliert (nur Fake-Ebene, keine Pfadliste der App).

### `NotificationServiceTests`

Datei: `src/Reporter.Tests/NotificationServiceTests.cs` — Benachrichtigungsregeln; Keyword-Match nutzt `i.ContentHtml` der übergebenen Items.

- `NotifyNewItemsAsync_FeedDisabled_SendsNothing` / `_GlobalDisabled_SendsNothing` — Schalter.
- `NotifyNewItemsAsync_WithinQuietHours_SendsNothing` / `_OutsideQuietHours_Sends` / `_QuietHoursStartEqualsEnd_Sends` / `_OnlyOneQuietHoursBound_Sends` (Theories) — Ruhezeiten.
- `NotifyNewItemsAsync_KeywordMatch_SkipsItem` — Keyword-Filterung auf Content.
- `NotifyNewItemsAsync_SendsPerItem_WithItemIdAsIdentifier` / `_SendsItemLink_InUserInfo` — Pro-Item-Benachrichtigung.
- `NotifyNewItemsAsync_SummaryEnabled_SendsSingleSummary` / `_SameItems_SameIdentifier` / `_KeywordFiltered_ExcludedFromSummary` — Sammel-Benachrichtigung.

### `KeywordMatcherTests`

Datei: `src/Reporter.Tests/KeywordMatcherTests.cs` — reiner In-Memory-Matcher.

- `MatchesAny_TitleCaseInsensitive` / `_ContentHtml` / `_Substring` / `_NoMatch` / `_NullTitleAndContent_ReturnsFalse` / `_EmptyKeywords_ReturnsFalse` / `_BlankKeywords_Skipped`.

### `KeywordFilterTests_E2E`

Datei: `src/Reporter.Tests/KeywordFilterTests_E2E.cs` — „E2E" im Sinne von Ende-zu-Ende über die echte Service-Kette (kein `Category=E2E`-Trait; lief im Unit-Testlauf mit). Sync → Filter → Liste/Benachrichtigung/SyncLog.

- `E2E_KeywordFilter_MatchedItemNotInUnreadList` / `_MatchedItemNoNotification` / `_SyncLogReportsFiltered`.

### `FeedRepositoryTests`

Datei: `src/Reporter.Tests/FeedRepositoryTests.cs` — u. a. relevant für die Feed-Löschkaskade.

- `DeleteAsync_RemovesFeed` — Feed-Löschung.
- `DeleteAsync_CascadeDeletesSavedItems` — DB-Kaskade `feeds → items` (löscht auch gemerkte Items).
- `AddAsync_ThenGetByIdAsync_ReturnsFeed`, `GetAllAsync_ReturnsFeedsOrderedByTitle`, `UpdateAsync_PersistsChanges`, `GetByIdAsync_NonExisting_ReturnsNull`, `GetAllWithDetailsAsync_Projects*` (NotificationsEnabled/FaviconUrl/LastError), `UpdateAsync_PersistsFaviconUrl`, `UpdateAsync_PersistsLastError`.

### `ReporterDbContextTests_Schema` / `ReporterDbContextTests_Persistence` / `ReporterDbContextFactoryTests`

Dateien: `src/Reporter.Tests/ReporterDbContextTests_Schema.cs`, `src/Reporter.Tests/ReporterDbContextTests_Persistence.cs`, `src/Reporter.Tests/ReporterDbContextFactoryTests.cs`

- `EnsureCreatedAsync_CreatesQueryableTables` — Tabellen/DbSets abfragbar, Settings-Seed (1 Zeile).
- `DebugLogEntries_MappedToExpectedTable` — Tabellen-/Spaltenmapping der Debug-Log-Entity.
- `SaveChangesAsync_PersistsFeedWithCategory`, `Items_WithInclude_ReturnsFeedAndCategory`, `Item_PersistRoundtrip`, `DebugLogEntry_PersistRoundtrip`, `Feed_PersistRoundtrip_LastError` — Entity-Persistenz (inkl. Item-`ContentHtml`-Roundtrip).
- `CreateDbContext_ReturnsContextWithSqliteProvider` — Design-Time-Factory.

### `DemoContentServiceTests`

Datei: `src/Reporter.Tests/DemoContentServiceTests.cs` — Erststart-Seed (`FirstRunState`/`REPORTER_DISABLE_DEMO_SEED` greifen in die Startsequenz ein, die um den Migrations-Schritt erweitert werden soll).

- `EnsureSeededAsync_FirstRun_CreatesNewsCategoryAndDemoFeed` / `_SeedsExpectedFeedDefaults` / `_NotFirstRun_CreatesNothing` / `_SeedSuppressed_CreatesNothing` / `_ExistingNewsCategory_ReusesCategory` / `_CalledTwice_IsIdempotent` / `_ExistingDemoFeed_SkipsSeed` / `_CancelledToken_ThrowsOperationCanceled` / `_FirstRun_LogsInfoEntry`.

### `SettingsViewModelTests_E2E` / `DebugReportTests_E2E`

Dateien: `src/Reporter.Tests/SettingsViewModelTests_E2E.cs`, `src/Reporter.Tests/DebugReportTests_E2E.cs` — Persistenz-Roundtrips über die echte Kette bis SQLite (kein `Category=E2E`-Trait, liefen im Unit-Testlauf).

### `UnreadViewModelTests` / `LaterViewModelTests`

Dateien: `src/Reporter.Tests/UnreadViewModelTests.cs`, `src/Reporter.Tests/LaterViewModelTests.cs` — konsumieren `ItemListItem`-Projektionen (`Articles`, `SavedItems`); indirekt abhängig von den `ContentHtml`-Ableitungen.

### Weitere indirekt relevante Testklassen

`FeedListItemTests`, `FeedSyncErrorKindTests`, `AutoRefreshServiceTests`(+`_DebugLog`), `ScheduledSyncRunnerTests`, `SyncLogRepositoryTests`, `SettingsRepositoryTests`, `CategoryRepositoryTests`, `KeywordRepositoryTests`, `DebugLogRepositoryTests`, `DebugLogServiceTests`, `ReadingTimeEstimatorTests`, `ArticleHtmlSanitizerTests`, `FeedIconServiceTests`, `FeedSearchServiceTests`, `FeedTitleFallbackTests`, `Atom03NormalizingXmlReaderTests`, `BaseViewModelConnectivityTests`, `CategoriesViewModelTests`, `FeedsViewModelTests`, `SettingsViewModelTests_*`, `SettingsValuesTests_AutoMarkRead`, `WebViewNavigationGuardTests`, `AppCultureTests`, `DebugReportServiceTests`.

## Hilfsmethoden

### `TestDbContextFactory`

Datei: `src/Reporter.Tests/TestDbContextFactory.cs`

- `TestDbContextFactory()` — öffnet eine Shared-In-Memory-SQLite-Verbindung (`Mode=Memory;Cache=Shared`, Keep-Alive-Connection) und erzeugt das Schema via `context.Database.EnsureCreated()` — **keine** Migrations.
- `CreateDbContext()` — neuer `ReporterDbContext` je Aufruf.
- `Dispose()` — schließt die Keep-Alive-Verbindung.

### `TestDataSeeder`

Datei: `src/Reporter.Tests/TestDataSeeder.cs`

- `SeedFeedAsync(TestDbContextFactory)` — legt einen `Entities.Feed` direkt im Kontext an.
- `SeedFeedAsync(IFeedRepository, url, notificationsEnabled)` — legt einen `Core.Models.Feed` über das Repository an.

### `TestFeedXml`

Datei: `src/Reporter.Tests/TestFeedXml.cs`

- `Rss(items, channelTitle)` — erzeugt RSS-2.0-Dokumente mit Titel/Link/Guid/PubDate/Description (→ `ContentHtml`-Quelle im Sync).
- Weitere Builder für Atom-/Atom-0.3-Dokumente.

### `FakeBackupExclusionService`

Datei: `src/Reporter.Tests/FakeBackupExclusionService.cs`

- `ExcludedPaths` — Liste der protokollierten `ExcludeFromBackup`-Aufrufe.

### `DelegatingItemRepository`

Datei: `src/Reporter.Tests/DelegatingItemRepository.cs`

- Dekorator über `IItemRepository` — Testdoubles überschreiben nur benötigte Member.

### `FakeHttpMessageHandler`

Datei: `src/Reporter.Tests/FakeHttpMessageHandler.cs`

- Simuliert HTTP-Antworten/-Fehler für `FeedSyncService`/`FeedSearchService`-Tests.

### Weitere Fakes (Auswahl)

`FakeNotificationService`, `FakeLocalNotificationService`, `FakeNetworkStatusService` (schaltbares `IsOnline`), `FakeFeedSyncService`, `FakeFeedIconService`, `FakeDebugLogService` (protokolliert Einträge), `FakeAutoRefreshService`, `FakeBackgroundRefreshService`, `FakeAppThemeService`, `FakeDeviceInfoProvider`, `FakeEmailService`, `FakeFeedSearchService` — jeweils `src/Reporter.Tests/Fake*.cs`.

### `TestWaitHelper` / `TestSettingsHelper`

Dateien: `src/Reporter.Tests/TestWaitHelper.cs`, `src/Reporter.Tests/TestSettingsHelper.cs` — Warte-/Polling-Helfer und Settings-Erzeugung für Tests.

## E2E-Infrastruktur (`src/Reporter.E2ETests`, nicht ausgeführt)

| Artefakt | Datei | Zweck |
|----------|-------|-------|
| `ReporterAppFixture` | `src/Reporter.E2ETests/ReporterAppFixture.cs` | Collection-Fixture: startet `StubFeedServer`, legt `%TEMP%/reporter-e2e-{guid}/reporter.db` an, startet `Reporter.exe` mit `REPORTER_FEEDSEARCH_ENDPOINT`, `REPORTER_DB_PATH`, `REPORTER_DISABLE_DEMO_SEED=1`; attacht FlaUI (UIA3), killt Prozess + löscht Temp-Dir am Ende. `REPORTER_APP_PATH` löst die Exe auf. |
| `E2ETestCollection` | `src/Reporter.E2ETests/E2ETestCollection.cs` | xunit-Collection „E2E" — serielle Ausführung gegen eine App-Instanz. |
| `FeedDbAssertions` | `src/Reporter.E2ETests/FeedDbAssertions.cs` | Read-only SQLite-Checks (`Mode=ReadOnly`) gegen `feeds`/`categories`/`items` der isolierten `reporter.db` mit 15-s-Polling (`FeedExistsAsync`, `CategoryExistsAsync`, `ItemExistsAsync`). |
| `StubFeedServer` | `src/Reporter.E2ETests/StubFeedServer.cs` | Kestrel-Stub (`/directory`, `/feeds/...`, Fixture-Dateien aus `Fixtures/`), `DirectoryUrl`, `BaseUrl`, `ExternalLinkHitCount`. |
| `E2EPageHelpers` / `UiRetry` | `src/Reporter.E2ETests/E2EPageHelpers.cs`, `src/Reporter.E2ETests/UiRetry.cs` | UIA-Helfer (Tab-Auswahl, Karten-Wartezyklen). |
| Fixtures | `src/Reporter.E2ETests/Fixtures/` | `stub-feed.xml`, `link-feed.xml`, `site-feed.xml`, `site.html`, `empty.html`. |

Hinweis aus `issue.md`: Bekanntes Folgeproblem `issue-e2e-prozessleck.md` (hängender `Reporter.exe` nach E2E-Läufen) — unabhängig von dieser Anforderung.
