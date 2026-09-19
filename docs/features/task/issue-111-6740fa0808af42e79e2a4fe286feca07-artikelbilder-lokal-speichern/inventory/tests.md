<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests

Übersicht bestehender Tests und Hilfsmethoden für den Bereich „Artikelbilder lokal speichern" sowie der projektweite Test-Ausgangszustand vor der Umsetzung.

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-19, ca. 08:21–08:25 MESZ (UTC+02:00; UTC 06:21–06:25)
- **Branch und Commit-ID:** `task/issue-111-6740fa0808af42e79e2a4fe286feca07-artikelbilder-lokal-speichern`, Commit `d174231fc7cd7174427935edac715557f6a2e510`
- **Uncommittete Änderungen im getesteten Stand:** Keine Änderungen an `src/`. Einzig untracked ist das Verzeichnis `docs/features/task/issue-111-6740fa0808af42e79e2a4fe286feca07-artikelbilder-lokal-speichern/` (Anforderungs- und Bestandsaufnahme-Dokumente, kein Code). `git stash list` ist leer.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows, Git Bash; .NET SDK 10.0.401; Workloads `maui-windows` 10.0.20 (sowie android/ios/maccatalyst installiert). Testprojekte: xunit 2.9.3, xunit.runner.visualstudio 3.1.4, Microsoft.NET.Test.Sdk 17.14.1, Microsoft.EntityFrameworkCore.Sqlite 10.0.12, coverlet.collector 6.0.4; E2E: FlaUI.UIA3 5.0.0.
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `src/Reporter.Tests/Reporter.Tests.csproj` (net10.0, xunit) — CI-Testbefehl aus `.github/workflows/pr-staging-ci.yml` bzw. `staging-ci.yml`: `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build` nach `dotnet restore Reporter.sln -r win-x64` + `dotnet build Reporter.sln --configuration Release --no-restore`.
  - `src/Reporter.E2ETests/Reporter.E2ETests.csproj` (net10.0-windows10.0.19041.0, xunit + FlaUI) — nicht Teil der CI-Workflows; lokaler Ablauf über `scripts/Run-E2ETests.ps1` (baut die App Debug/win-x64 und setzt `REPORTER_APP_PATH`). Voraussetzung: interaktive Desktop-Session (UIA3).

### Vorbereitende Builds (keine Testläufe)

| Befehl | Arbeitsverzeichnis | Exit-Code | Ergebnis |
|--------|--------------------|-----------|----------|
| `dotnet build src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` | Repo-Root | 0 | 0 Warnungen, 0 Fehler |
| `dotnet build src/Reporter/Reporter.csproj -c Debug -f net10.0-windows10.0.19041.0 -r win-x64` (mit `IncludeIosTarget=false`) | Repo-Root | 0 | 0 Warnungen, 0 Fehler; erzeugt `src/Reporter/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Reporter.exe` |
| `dotnet build src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0` | Repo-Root | 0 | 0 Warnungen, 0 Fehler |

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit-Tests (CI-Suite) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --logger "trx;LogFileName=test-results-baseline.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 566 | 0 | 0 | [TRX](test-results/reporter-tests-baseline.trx), [Konsole](test-results/reporter-tests-baseline-console.log) |
| E2E-Tests (FlaUI) | `dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0 --no-build --logger "trx;LogFileName=e2e-results-baseline.trx" --logger "console;verbosity=normal"` (mit `REPORTER_APP_PATH=…/Reporter.exe`) | Repo-Root | 0 | 17 | 0 | 0 | [TRX](test-results/reporter-e2etests-baseline.trx), [Konsole](test-results/reporter-e2etests-baseline-console.log) |

### Nachgewiesene bestehende Testfehler

Keine — beide Suiten liefen im Ausgangslauf vollständig und fehlerfrei durch (566 + 17 Tests, 0 Fehlschläge, 0 Übersprungene). Ein „preexisting failure" kann für spätere Änderungen aus diesem Lauf **nicht** abgeleitet werden; jeder neue Fehlschlag ist zu untersuchen.

### Testlücken und Ausführungsprobleme

- Keine nicht ausgeführten oder übersprungenen Tests in beiden Läufen (TRX: `executed` = `total`, `failed` = 0).
- Die E2E-Suite erfordert eine interaktive Windows-Desktop-Session (FlaUI/UIA3 startet die App sichtbar) und ist bewusst nicht Teil der CI-Workflows; sie lief in dieser Umgebung erfolgreich. In Umgebungen ohne Desktop-Session wäre sie nicht ausführbar.
- Anmerkung zum E2E-Log: Der Lauf meldet harmlose `[E2E]`-Hinweise des `E2EProcessGuard` (u. a. „Reporter.exe exited … with code -1" im Cleanup-Pfad) — kein Testfehler, alle 17 Tests bestanden.
- Keine Build-, Setup- oder Infrastrukturfehler aufgetreten.

## Testklassen

### `FeedSyncServiceTests` (`src/Reporter.Tests/FeedSyncServiceTests.cs`)

Integrationstests gegen In-Memory-SQLite (`TestDbContextFactory`), `FakeItemContentStore`, echte Repositories, `FakeHttpMessageHandler` für Feed-Abrufe, `FakeFeedIconService`.

- `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk` — gültiges RSS erzeugt Items + Health Ok
- `SyncFeedAsync_Duplicates_SkipsExistingItems` — Duplikate über `GuidOrHash` übersprungen
- `SyncFeedAsync_DuplicatesWithinSameDocument_InsertsOnce` — Duplikate im selben Dokument nur einmal
- `SyncFeedAsync_MixedNewAndExisting_BatchInsertsOnlyNew` — nur neue Items werden eingefügt
- `SyncFeedAsync_Unreachable_KeepsItemsAndLogsError` — Fehlerfall behält Items, loggt Error
- `SyncFeedAsync_InvalidXml_SetsError` — Parse-Fehler → Health Error
- `SyncFeedAsync_FewerItems_SetsWarning` — drastisch weniger Items → Warning
- `SyncFeedAsync_NoNewItemsForThirtyDays_SetsWarning` — inaktiver Feed → Warning
- `SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth` — Gesamt-Sync über zwei Feeds
- `SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog` — Offline → Fehler ohne SyncLog
- `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange` — Offline → Fehler ohne Health-Änderung
- `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems` — Notification mit Feed + Items
- `SyncFeedAsync_SummaryMode_SendsSingleSummaryNotification` — Summary-Modus
- `SyncFeedAsync_NoNewItems_DoesNotNotify` — keine Notification ohne neue Items
- `SyncFeedAsync_FeedDisabled_NoNotifications` — deaktivierter Feed → keine Notification
- `SyncFeedAsync_NotificationThrows_SyncStillSucceeds` — Notification-Fehler isoliert (Muster für Bild-Download-Isolation)
- `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument` — Titel-Auflösung
- `SyncFeedAsync_WhenTitleIsHostPlaceholder_UpdatesTitleFromFeedDocument` — Host-Platzhalter
- `SyncFeedAsync_WhenTitleIsFileNamePlaceholder_UpdatesTitleFromFeedDocument` — Dateinamen-Platzhalter
- `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle` — vorhandener Titel bleibt
- `SyncFeedAsync_KeywordTitleMatch_NotSaved` — Keyword-Filter Titel
- `SyncFeedAsync_KeywordContentHtmlMatch_NotSaved` — Keyword-Filter Content
- `SyncFeedAsync_KeywordNoMatch_SavesNormally` — kein Match → speichern
- `SyncFeedAsync_EmptyKeywords_SavesAll` — leere Keywords → alles speichern
- `SyncFeedAsync_KeywordFiltered_NotNotified` — gefilterte Items nicht benachrichtigt
- `SyncFeedAsync_KeywordFiltered_ResyncStaysFiltered` — Filter stabil über Re-Sync
- `SyncFeedAsync_KeywordFiltered_LogCountsFiltered` — Filterzähler im Log
- `SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved` — Filter bei Atom
- `SyncFeedAsync_MissingFavicon_BackfillsFromFeedAuthority` — Favicon-Backfill
- `SyncFeedAsync_ExistingFavicon_SkipsLookup` — vorhandenes Favicon → kein Lookup
- `SyncFeedAsync_FaviconLookupFails_SyncStillSucceeds` — Favicon-Fehler isoliert (Referenzmuster für Bild-Download)
- `SyncFeedAsync_MissingFavicon_ForwardsCancellationTokenToIconLookup` — Token-Weitergabe
- `SyncFeedAsync_Failure_PersistsErrorKindAndMessage` — `LastErrorKind`/`LastErrorMessage`
- `SyncFeedAsync_Success_ClearsLastError` — Fehler zurückgesetzt
- `SyncFeedAsync_HttpFeedNetworkFailure_ClassifiedAsInsecureHttpBlocked` — Fehlerklassifizierung
- `SyncFeedAsync_HttpStatusError_ClassifiedAsHttpStatus` — Fehlerklassifizierung
- `SyncFeedAsync_InvalidXml_ClassifiedAsParse` — Fehlerklassifizierung
- `SyncFeedAsync_HttpsFeedNetworkFailure_ClassifiedAsNetwork` — Fehlerklassifizierung
- `SyncFeedAsync_Atom03_CreatesItems_AndSetsHealthOk` — Atom 0.3
- `SyncFeedAsync_Atom03_MapsIssuedToPublishedAt` — Atom 0.3 Datums-Mapping
- `SyncFeedAsync_Atom03_PlaceholderTitle_UpdatesTitleFromFeedDocument` — Atom 0.3 Titel
- `SyncFeedAsync_BackfillsMissingContent` — `contentBackfill` lädt fehlenden Inhalt nach
- `SyncFeedAsync_KeepsExistingContent` — vorhandener Inhalt wird nicht überschrieben

### `FeedSyncServiceTests_DebugLog` (`src/Reporter.Tests/FeedSyncServiceTests_DebugLog.cs`)

- `SyncFeedAsync_Failure_LogsErrorEntry` — Sync-Fehler landet im `IDebugLogService`
- `SyncFeedAsync_Failure_WithoutDebugLogService_StillReportsError` — optionaler Debug-Log-Dienst darf fehlen

### `ItemContentRepositoryTests` (`src/Reporter.Tests/ItemContentRepositoryTests.cs`)

Tests gegen `ItemContentRepository` auf `TestContentDbContextFactory` (In-Memory-SQLite mit echtem Migrations-Schema).

- `SetAsync_ThenGetAsync_ReturnsContent` — Roundtrip
- `GetAsync_UnknownItem_ReturnsNull` — unbekannte ID → `null`
- `SetAsync_ExistingItem_OverwritesContent` — Überschreiben
- `SetAsync_NullContent_RemovesEntry` — `null` löscht
- `GetRangeAsync_ReturnsOnlyStoredContents` — Batch-Lesen
- `SetRangeAsync_UpsertsAndRemovesEntries` — Batch-Upsert + Entfernen
- `SetRangeAsync_DuplicateItemId_LastEntryWins` — Duplikat-Reduktion
- `SetRangeAsync_DuplicateItemIdWithNull_RemovesEntry` — `null` im Batch
- `DeleteAsync_RemovesContent` — Einzel-Löschung
- `DeleteRangeAsync_RemovesOnlyGivenIds` — Sammel-Löschung
- `GetItemIdsAsync_ReturnsAllStoredIds` — ID-Liste für Waisen-Sweep

### `ItemRepositoryTests` (`src/Reporter.Tests/ItemRepositoryTests.cs`)

Content-Store-relevante Tests (echte `ItemContentRepository` auf `TestContentDbContextFactory`):

- `AddAsync_ThenGetByIdAsync_ReturnsContentHtml` — Inhalt wird gespeichert/hydratisiert
- `AddAsync_WithoutContent_StoresNoContentEntry` — kein Eintrag ohne Inhalt
- `GetByFeedAsync_HydratesContentFromStore` — Hydratisierung aus dem Store
- `UpdateAsync_UpdatesStoredContent` — Inhalt überschreiben
- `UpdateAsync_NullContent_RemovesStoredContent` — `null` entfernt Inhalt
- `DeleteAsync_RemovesStoredContent` — Mitlöschung bei Einzel-Löschung
- `DeleteExpiredAsync_RemovesStoredContent` — Mitlöschung bei Retention
- `DeleteRangeAsync_RemovesStoredContent` — Mitlöschung bei Sammel-Löschung
- `GetUnreadByDateAsync_ProjectsFeedFaviconUrl` — `FeedFaviconUrl`-Projektion
- `GetUnreadByDateAsync_Paged_ProjectsReadingTimeText` / `GetSavedForLaterAsync_ProjectsReadingTimeText` / `GetUnreadByDateAsync_OneMinuteReadingTime_ProjectsEmpty` — `ItemListItem`-Projektionen (gleicher Pfad wie `ImageUrl`-Extraktion)
- Übrige Tests (ca. 30): CRUD, Paging, Sortierung, Counts, Gelesen/Gemerkt-Flags, `DeleteExpiredAsync`-/`GetExpiredKeywordCandidatesAsync`-Regeln, `GetAllIdsAsync` — ohne Content-Store-Bezug

### `FeedRepositoryTests` (`src/Reporter.Tests/FeedRepositoryTests.cs`)

- `DeleteAsync_RemovesFeed` — Feed-Löschung
- `DeleteAsync_CascadeDeletesSavedItems` — DB-Kaskade entfernt Items (auch gemerkte)
- `DeleteAsync_RemovesItemContents` — Feed-Kaskade löscht `item_contents` mit
- `UpdateAsync_PersistsFaviconUrl` / `GetAllWithDetailsAsync_ProjectsFaviconUrl` — `FaviconUrl`-Persistenz/Projektion
- `UpdateAsync_PersistsLastError` / `GetAllWithDetailsAsync_ProjectsLastError` — Fehlerfelder
- Übrige: `AddAsync_ThenGetByIdAsync_ReturnsFeed`, `GetAllAsync_ReturnsFeedsOrderedByTitle`, `UpdateAsync_PersistsChanges`, `GetByIdAsync_NonExisting_ReturnsNull`, `GetAllWithDetailsAsync_ProjectsNotificationsEnabled`

### `RetentionCleanupServiceTests` (`src/Reporter.Tests/RetentionCleanupServiceTests.cs`)

- `CleanupAsync_DeletesExpiredButKeepsSaved` — Retention-Grundregel
- `CleanupAsync_ZeroOrNegativeRetentionDays_Skips` — deaktivierte Retention
- `CleanupAsync_RespectsConfiguredRetentionDays` — Frist wirkt
- `CleanupAsync_CancelledToken_ThrowsOperationCanceled` — Abbruch
- `CleanupAsync_DeletesKeywordMatchedExpired` — Keyword-Löschkandidaten
- `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved` — Schutzregeln
- `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt` — Zeitstempel-Regel
- `CleanupAsync_RemovesOrphanedContent` — Waisen-Sweep entfernt verwaiste `item_contents`
- `CleanupAsync_ZeroOrNegativeRetentionDays_StillRemovesOrphanedContent` — Sweep läuft auch ohne Retention

### `ArticleHtmlSanitizerTests` (`src/Reporter.Tests/ArticleHtmlSanitizerTests.cs`)

- `Sanitize_RemovesScriptsAndEventHandlers` — gefährliche Tags/Handler (beide Modi)
- `Sanitize_Online_PreservesAnchorsAndImages` — online bleiben `<a>`/`<img>`
- `Sanitize_Offline_NeutralizesAnchorsButKeepsText` — offline `<a>` → Text
- `Sanitize_Offline_RemovesImages` — offline werden alle `<img>` entfernt (relevante Ausgangslage für die Anforderung)
- `Sanitize_RemovesDangerousElementsIncludingClosingTags` — audio/video/applet
- `Sanitize_RemovesOrphanedDangerousClosingTags` — verwaiste Endtags
- `Sanitize_NullOrWhitespace_ReturnsInput` — `null`/leer unverändert

### `FeedIconServiceTests` (`src/Reporter.Tests/FeedIconServiceTests.cs`)

Referenzsuite für HTTP-Download-Tests mit `FakeHttpMessageHandler`:

- `FindFaviconUrlAsync_ParsesLinkTag` / `..._ResolvesRelativeUrl` / `..._FallsBackToFaviconIco` / `..._BrokenLinkTagCandidate_TriesNext` / `..._ReturnsNullOnFailure` / `..._SiteRequestFails_StillTriesFallback` / `..._InvalidSiteUrl_ReturnsNull` — Kandidaten-Logik + Fehlerisolation
- `TryFindFaviconUrlAsync_WithSiteUrl_LooksUpSiteUrl` / `..._WithoutSiteUrl_FallsBackToFeedAuthority` / `..._UnresolvableUrls_ReturnsNull` / `..._WhenCancelled_ReturnsNull` — `Try…`-Wrapper

### `ItemContentMigrationServiceTests` (`src/Reporter.Tests/ItemContentMigrationServiceTests.cs`)

- `MigrateLegacyContentAsync_CopiesContentHtml` — Legacy-Spalte → Store
- `MigrateLegacyContentAsync_Idempotent` — wiederholter Lauf no-op
- `MigrateLegacyContentAsync_MoreThanOnePage_CopiesAll` — Paging > 500
- `MigrateLegacyContentAsync_NoColumn_NoOp` — ohne Spalte no-op

### `ContentDbContextTests` (`src/Reporter.Tests/ContentDbContextTests.cs`)

- `ItemContents_MappedToExpectedTable` — Schema-Mapping `item_contents`
- `ItemContent_PersistRoundtrip` — Persistenz der Entity

### `ServiceCollectionTests` (`src/Reporter.Tests/ServiceCollectionTests.cs`)

- `AddReporterRepositories_ResolvesAllRepositories` — u. a. `IItemContentStore`, `IContentMigrationService` auflösbar
- `AddReporterServices_ResolvesFeedSyncService` — `FeedSyncService` inkl. `IFeedIconService` auflösbar
- `AddReporterServices_ResolvesBackupExclusion` — `DatabasePath`/`ContentDatabasePath`/`IBackupExclusionService`
- Übrige: `..._ResolvesFeedSearchService`, `..._ResolvesDebugServices`, `..._ResolvesAutoRefreshService`, `..._ResolvesScheduledSyncRunner`, `..._ResolvesDemoContentService`

### E2E-Tests (`src/Reporter.E2ETests/`, FlaUI, nicht in CI)

- `SmokeTests` (9 Tests): App-Start, Feed-Liste, Direkt-Add, Umbenennen, Kategorie-Wechsel, Suche, Sheet-Fokus — UI-Smoke
- `DemoSeedTests` (1): `FirstStart_SeedsNewsCategoryAndDemoFeed`
- `ArticleLinkTests` (1): `ExternalLinkInArticle_OpensSystemBrowser` — betrifft die Artikeldetailansicht
- `E2EProcessGuardTests` (6): Prozess-/Dateisystem-Aufräumhelfer der E2E-Infrastruktur

## Hilfsmethoden

### `FakeItemContentStore` (`src/Reporter.Tests/FakeItemContentStore.cs`)

- `GetAsync`/`GetRangeAsync`/`SetAsync`/`SetRangeAsync`/`DeleteAsync`/`DeleteRangeAsync`/`GetItemIdsAsync` — In-Memory-`IItemContentStore` mit identischer Upsert-Semantik (`null`/leer löscht); müsste für Bild-Zugriff erweitert werden

### `FakeHttpMessageHandler` (`src/Reporter.Tests/FakeHttpMessageHandler.cs`)

- `SendAsync` — konfigurierbare `HttpResponseMessage`-Fabrik (sync oder `Task`, auch faulted); für Bild-Download-Tests wiederverwendbar (bereits in `FeedIconServiceTests` und `FeedSyncServiceTests` im Einsatz)

### `FakeFeedIconService` (`src/Reporter.Tests/FakeFeedIconService.cs`)

- `NextResult`/`NextException`/`RequestedSiteUrls`/`ReceivedCancellationTokens` — konfigurierbares `IFeedIconService`-Fake mit Aufrufprotokoll

### `DelegatingItemRepository` (`src/Reporter.Tests/DelegatingItemRepository.cs`)

- Alle `IItemRepository`-Member als `virtual` an `_inner` delegierend — Basis für Test-Doubles, die einzelne Member überschreiben

### `TestDbContextFactory` (`src/Reporter.Tests/TestDbContextFactory.cs`)

- In-Memory-SQLite-`IDbContextFactory<ReporterDbContext>`; Schema via `EnsureCreated`

### `TestContentDbContextFactory` (`src/Reporter.Tests/TestContentDbContextFactory.cs`)

- In-Memory-SQLite-`IDbContextFactory<ContentDbContext>`; Schema via `Migrate()` (echte Migrationen inkl. History-Tabelle)

### `TestDataSeeder` (`src/Reporter.Tests/TestDataSeeder.cs`)

- `SeedFeedAsync(TestDbContextFactory)` — Feed direkt im Kontext anlegen
- `SeedFeedAsync(IFeedRepository, url, notificationsEnabled)` — Feed über Repository anlegen

### Weitere Fakes (Auswahl)

`FakeNotificationService`, `FakeLocalNotificationService`, `FakeNetworkStatusService` (konfigurierbarer `IsOnline`), `FakeFeedSyncService`, `FakeDebugLogService`, `FakeBackupExclusionService`, `FakeAutoRefreshService`, `FakeBackgroundRefreshService`, `FakeAppThemeService`, `FakeDeviceInfoProvider`, `FakeEmailService`, `FakeFeedSearchService` — jeweils in `src/Reporter.Tests/`.
