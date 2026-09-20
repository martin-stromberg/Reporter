<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Tests

Bestehende Tests und Hilfsmethoden für den Bereich „Stichwort-Filter" sowie der projektweite Test-Ausgangszustand vor der Umsetzung der Anforderung (Issue #116).

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-20, Läufe zwischen 10:47 und ca. 11:10 **+02:00** (Mitteleuropäische Sommerzeit)
- Branch und Commit-ID: `task/issue-116-1f7a5a021da5422d82b9097b8688d0b1-stichwort-filter-pro-feed`, Commit `1bd324aa2dd024c601ff6c975942eaf86a7d3e28`
- Uncommittete Änderungen im getesteten Stand: nur neue, nicht committete Dateien unter `docs/features/task/issue-116-1f7a5a021da5422d82b9097b8688d0b1-stichwort-filter-pro-feed/` (`requirement.md` sowie die hier erzeugten Inventory-Dateien). Keine Änderungen an Produktivcode, Tests oder Testkonfiguration.
- Testumgebung und Runtime-/SDK-Versionen:
  - Windows 11 (Host `DESKTOP-CM8OBSG`), interaktive Konsolensession (SESSIONNAME=Console, Session 1 aktiv)
  - .NET SDK 10.0.401 (einzige installierte SDK-Version), Laufzeit .NET 10.0.12 (xUnit.net VSTest Adapter v3.1.4)
  - Installierte Workloads: `maui-windows` 10.0.20/10.0.100, `android` 36.1.69, `ios` 26.5.10318, `maccatalyst` 26.5.10318
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `src/Reporter.Tests/Reporter.Tests.csproj` (xunit 2.9.3, `net10.0`, coverlet.collector) — Unit-/Integrationstests. Testbefehl aus CI-Workflow `.github/workflows/pr-staging-ci.yml` (Job `build-and-test`, Schritt „Test", Z. 125–128); lokal ohne `--no-build` ausgeführt (impliziter Release-Build).
  - `src/Reporter.E2ETests/Reporter.E2ETests.csproj` (xunit, FlaUI.UIA3, `net10.0-windows10.0.19041.0`) — UI-End-to-End-Tests gegen die laufende Windows-App. Testbefehl aus Projektskript `scripts/Run-E2ETests.ps1` (baut `src/Reporter/Reporter.csproj` Debug/win-x64, setzt `REPORTER_APP_PATH`, führt `dotnet test` aus). Die E2E-Suite ist **nicht** Teil der CI-Workflows (kein Workflow referenziert `Reporter.E2ETests`).
  - `Reporter.sln` enthält beide Testprojekte sowie `Reporter`, `Reporter.Core`, `Reporter.Data`.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit/Integration (Release, mit Coverage) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 641 | 0 | 0 | [Log](test-results/unit-tests-release.log), [TRX](test-results/unit-tests-release.trx) |
| E2E (Debug, FlaUI/Windows) | `powershell -ExecutionPolicy Bypass -File scripts\Run-E2ETests.ps1` (intern: `dotnet build src/Reporter/Reporter.csproj -c Debug -f net10.0-windows10.0.19041.0 -r win-x64`, dann `dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f net10.0-windows10.0.19041.0` mit `REPORTER_APP_PATH` auf die gebaute `Reporter.exe`) | Repo-Root | 1 | 24 | 2 | 0 | [Log](test-results/e2e-tests-debug.log) |

Hinweis zum Unit-Lauf: Der CI-Befehl verwendet `--no-build` nach einem separaten Build-Schritt; hier wurde derselbe Aufruf ohne `--no-build` ausgeführt (impliziter Build vor dem Test). Die Coverage-Ergebnisdatei liegt unter `src/Reporter.Tests/TestResults/<run>/coverage.cobertura.xml` (nicht in `test-results/` gespiegelt, im Log referenziert).

### Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|------------------------|-------------------|----------------------------|-------------------|
| `Reporter.E2ETests.ArticleImageTests.ArticleImage_StoredLocally_AndShownOnCardAndDetail` | `src/Reporter.E2ETests/ArticleImageTests.cs` (Assert in Z. 56) | `No feed row with URL 'http://127.0.0.1:50353/feeds/image-feed.xml' found in C:\Users\Martin\AppData\Local\Temp\reporter-e2e-d6600f7878ee4908a97e363388320dd9\reporter.db — the direct add did not persist the feed.` (Dauer 16 s) | E2E-Lauf, [Log](test-results/e2e-tests-debug.log) |
| `Reporter.E2ETests.ArticleImageTests.BrokenImage_DoesNotFailSync_StoresNoImage` | `src/Reporter.E2ETests/ArticleImageTests.cs` (Z. 168) via `E2EPageHelpers.cs:224` → `ReporterAppFixture.cs:94` | `System.InvalidOperationException : The Reporter.exe process is not running anymore.` (< 1 ms — Folgefehler: Der App-Prozess war nach dem ersten Fehlschlag nicht mehr aktiv) | E2E-Lauf, [Log](test-results/e2e-tests-debug.log) |

Beide Fehlschläge liegen in `ArticleImageTests` (Artikelbild-Download), nicht im Keyword-/Feed-Bereich. Sie sind als Ausgangszustand dokumentiert; ob es sich um ein instabiles UI-Test- oder ein Produktivproblem handelt, wurde für die Bestandsaufnahme nicht weiter untersucht.

### Testlücken und Ausführungsprobleme

- Alle 641 Unit-Tests liefen; keine übersprungenen oder nicht ausgeführten Unit-Tests.
- Die E2E-Suite (26 Tests) lief vollständig; 2 Fehlschläge siehe oben. Der zweite Fehlschlag ist ein Folgefehler des ersten (App-Prozess beendet); die übrigen 24 Tests liefen normal.
- Keine Build-, Setup- oder Infrastrukturfehler aufgetreten (App-Build Debug/win-x64 erfolgreich, 0 Warnungen/0 Fehler).

## Testklassen (keyword-/feed-bezogen)

### `KeywordRepositoryTests`
Datei: `src/Reporter.Tests/KeywordRepositoryTests.cs` — In-Memory-SQLite via `TestDbContextFactory`.

- `AddAsync_ThenGetByIdAsync_ReturnsKeyword` — Anlegen und Auslesen eines Stichworts.
- `GetAllAsync_ReturnsKeywordsOrderedByText` — Sortierung der Gesamtliste.
- `UpdateAsync_PersistsChanges` — Update von `KeywordText`.
- `DeleteAsync_RemovesKeyword` — Löschen.
- `GetByIdAsync_NonExisting_ReturnsNull` — Negativfall.

### `KeywordMatcherTests`
Datei: `src/Reporter.Tests/KeywordMatcherTests.cs`

- `MatchesAny_TitleCaseInsensitive`, `MatchesAny_ContentHtml`, `MatchesAny_Substring`, `MatchesAny_NoMatch`, `MatchesAny_NullTitleAndContent_ReturnsFalse`, `MatchesAny_EmptyKeywords_ReturnsFalse`, `MatchesAny_BlankKeywords_Skipped` — Match-Semantik (OrdinalIgnoreCase, Teilwort, Titel+Content).

### `KeywordFilterTests_E2E`
Datei: `src/Reporter.Tests/KeywordFilterTests_E2E.cs` — Voller Fluss Settings → Sync → Unread/Notification mit echten Repositories auf In-Memory-SQLite.

- `E2E_KeywordFilter_MatchedItemNotInUnreadList` — Gefilterter Artikel landet nicht in der Unread-Liste.
- `E2E_KeywordFilter_MatchedItemNoNotification` — Gefilterter Artikel erzeugt keine Benachrichtigung.
- `E2E_KeywordFilter_SyncLogReportsFiltered` — Sync-Log meldet „, 1 filtered".

### `FeedSyncServiceTests` (keyword-bezogene Auswahl)
Datei: `src/Reporter.Tests/FeedSyncServiceTests.cs`

- `SyncFeedAsync_KeywordTitleMatch_NotSaved`, `SyncFeedAsync_KeywordContentHtmlMatch_NotSaved`, `SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved` — Treffer werden nicht gespeichert (RSS und Atom).
- `SyncFeedAsync_KeywordNoMatch_SavesNormally`, `SyncFeedAsync_EmptyKeywords_SavesAll` — Kein Treffer / leere Liste.
- `SyncFeedAsync_KeywordFiltered_NotNotified` — Kein Notification-Aufruf bei ausschließlich gefilterten Items.
- `SyncFeedAsync_KeywordFiltered_ResyncStaysFiltered` — Filterung bleibt beim Re-Sync wirksam.
- `SyncFeedAsync_KeywordFiltered_LogCountsFiltered` — `filtered`-Zähler im Sync-Log.

### `NotificationServiceTests` (keyword-bezogene Auswahl)
Datei: `src/Reporter.Tests/NotificationServiceTests.cs`

- `NotifyNewItemsAsync_KeywordMatch_SkipsItem(contentHtml)` (Theory: `null`, `"<p>Read more about sport</p>"`) — Treffer in Titel oder Content wird nicht benachrichtigt.
- `NotifyNewItemsAsync_SummaryEnabled_KeywordFiltered_ExcludedFromSummary` — Gefilterte Items fehlen in der Summary-Benachrichtigung.

### `RetentionCleanupServiceTests` (keyword-bezogene Auswahl)
Datei: `src/Reporter.Tests/RetentionCleanupServiceTests.cs`

- `CleanupAsync_DeletesKeywordMatchedExpired` — Abgelaufene gelesene Items mit Keyword-Treffer werden gelöscht.
- `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved` — Ungelesene/gespeicherte Treffer bleiben erhalten.
- `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt` — Cutoff-Basis `PublishedAt` vor `ReadAt`.

### `SettingsViewModelTests_Keywords`
Datei: `src/Reporter.Tests/SettingsViewModelTests_Keywords.cs`

- `AddKeyword_Valid_AddsToRepository`, `AddKeyword_DuplicateCaseInsensitive_ShowsError`, `AddKeyword_Empty_ShowsError`, `AddKeyword_TooLong_ShowsError`, `RemoveKeyword_DeletesFromRepository`.

### `FeedRepositoryTests` (delete-bezogene Auswahl)
Datei: `src/Reporter.Tests/FeedRepositoryTests.cs`

- `DeleteAsync_RemovesFeed`, `DeleteAsync_CascadeDeletesSavedItems`, `DeleteAsync_RemovesItemContents`, `DeleteAsync_RemovesItemImages` — Feed-Löschung inkl. Kaskade/Content-Bereinigung.

### `FeedDetailViewModelTests` (edit-sheet-bezogene Auswahl)
Datei: `src/Reporter.Tests/FeedDetailViewModelTests.cs`

- `EditCommand_PrefillsForm`, `SaveEditCommand_PersistsUrlAndNotifications`, `SaveEditCommand_InvalidUrl_SetsError`, `SaveEditCommand_DuplicateUrl_SetsError`, `SaveEditCommand_PreservesCategoryId`, `SaveEditCommand_WithoutFeed_DoesNothing`, `SaveEditCommand_ResetsEditState`, `SaveEditCommand_WhenRepositoryThrows_SetsErrorAndKeepsSheetOpen`, `DeleteFeedAsync_RemovesFeed`, `DeleteFeedAsync_WithoutFeed_DoesNothing`.

### `ServiceCollectionTests`
Datei: `src/Reporter.Tests/ServiceCollectionTests.cs`

- `AddReporterRepositories_ResolvesAllRepositories` — löst u. a. `IKeywordRepository` auf.
- `AddReporterServices_ResolvesFeedSyncService` — registriert `IKeywordMatcher`/`IKeywordFilter`/`INotificationService` wie `MauiProgram` und löst `IFeedSyncService` auf.

### `ReporterDbContextTests_Schema`
Datei: `src/Reporter.Tests/ReporterDbContextTests_Schema.cs`

- `EnsureCreatedAsync_CreatesQueryableTables` — prüft u. a. Abfragbarkeit der `keywords`-Tabelle (`context.Keywords.CountAsync()`).
- `DebugLogEntries_MappedToExpectedTable` — Tabellen-Mapping.

### `FeedDetailTests` (E2E, FlaUI)
Datei: `src/Reporter.E2ETests/FeedDetailTests.cs` — Collection-Fixture `ReporterAppFixture` (startet die echte `Reporter.exe`).

- `FeedDetail_Edit_PersistsChanges` — UI-Fluss des Bearbeiten-Sheets (URL + Notifications-Toggle).
- Weitere: `FeedDetail_Navigation_ShowsFeedItems`, `FeedDetail_Search_FiltersItems`, `FeedDetail_Rename_UpdatesTitle`, `FeedDetail_ChangeCategory_IncludingNone`, `FeedDetail_Delete_ReturnsToList`, `FeedDetail_RefreshAction_SyncsFeed`, `FeedDetail_InfiniteScroll_LoadsNextPage`, `FeedDetail_ErrorDetails_OnlyForErrorFeed`.
- **Kein E2E-Test für Stichwort-Pflege vorhanden** (weder global noch feed-spezifisch).

## Hilfsmethoden / Testinfrastruktur

### `TestDbContextFactoryBase<TContext>` / `TestDbContextFactory` / `TestContentDbContextFactory`
Dateien: `src/Reporter.Tests/TestDbContextFactoryBase.cs`, `TestDbContextFactory.cs`, `TestContentDbContextFactory.cs`
- Shared-In-Memory-SQLite (`Mode=Memory;Cache=Shared`, Keep-Alive-Connection, `Default Timeout=30`); Schema via `EnsureCreated`/`Migrate`; `IDbContextFactory<TContext>`-Implementierung für die Repositories.

### `TestDataSeeder`
Datei: `src/Reporter.Tests/TestDataSeeder.cs`
- `SeedFeedAsync(TestDbContextFactory)` / `SeedFeedAsync(IFeedRepository, url, notificationsEnabled)` — legt Testfeeds an (Standard-URL `https://example.com/rss`).

### `TestFeedXml`
Datei: `src/Reporter.Tests/TestFeedXml.cs`
- `Rss(items, channelTitle)` — erzeugt RSS-2.0-XML aus Tupeln `(Title, Link, Guid, PubDate, Description)` für `FakeHttpMessageHandler`.

### Fakes
- `FakeHttpMessageHandler` — stubbt `HttpClient`-Feedabrufe.
- `FakeLocalNotificationService` — sammelt `ShownNotifications` (`Identifier`, `Title`, `Body`, `UserInfo`), steuerbar via `AuthorizationResult`/`AuthorizationStatus`/`IsSupported`.
- `FakeNetworkStatusService`, `FakeFeedIconService`, `FakeItemImageService`, `FakeAutoRefreshService`, `FakeAppThemeService`, `FakeNotificationService`, `FakeDebugLogService`, `FakeEmailService`, `FakeDeviceInfoProvider`, `FakeItemContentStore`, `FakeFeedSearchService`, `FakeBackgroundRefreshService`, `FakeBackupExclusionService`.
- `DelegatingFeedRepository`, `DelegatingItemRepository` — Basisklassen für Test-Doubles, die nur einzelne Member überschreiben. **Es existiert kein `DelegatingKeywordRepository`.**
- `ThrowingFeedRepository`, `ThrowingCategoryRepository` — Fehler-Injektion.
- `TestSettingsHelper`, `TestWaitHelper`, `TestHttpResponses` — Settings-Defaults, UI-ähnliche Wartehelfer, vorgefertigte HTTP-Antworten.

### E2E-Infrastruktur (`src/Reporter.E2ETests/`)
- `ReporterAppFixture` / `E2ETestCollection` — startet die App einmal pro Collection, liefert UIA3-Hauptfenster.
- `E2EPageHelpers` — UI-Treiber (`SelectTab`, `OpenFeedDetail`, `OpenFeedDetailActions`, Such-/Listen-Helfer).
- `StubFeedServer` — lokaler HTTP-Server für Feed-Fixtures (`Fixtures/`).
- `FeedDbAssertions` — Assertions gegen die SQLite-DB der App (`reporter.db` im Temp-Profil).
- `UiRetry`, `E2EProcessGuard` — Retry-Logik und Prozess-Guard für UI-Tests.
