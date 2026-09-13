<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-13, Läufe ca. 20:04–20:06 MESZ (UTC+02:00)
- Branch und Commit-ID: `task/issue-79-265f19b9f96d4108ba3e79cd3b10bed5-bug-schlagwortfilter-funktioni` @ `b0738a06b4a39604b60091bfaa3ebc74a95bc456` (2026-09-13 19:49:17 +0200)
- Uncommittete Änderungen im getesteten Stand: keine geänderten getrackten Dateien; einzig untracked ist das Feature-Verzeichnis `docs/features/task-issue-79-265f19b9-f96d-4108-ba3e-79cd3b10bed5-bug-schlagwortfilter-funktioni/` (`requirement.md` und die im Lauf angelegten Inventory-Nachweise). Produktiv- und Testcode entsprechen exakt dem Commit.
- Testumgebung und Runtime-/SDK-Versionen: Windows (Git Bash), .NET SDK `10.0.401` (`C:\Program Files\dotnet\sdk`), Node.js `v24.15.0`, npm `11.10.0`. Testprojekt-Target: `net10.0` (kein MAUI-Workload nötig); xUnit 2.9.3, EF Core SQLite In-Memory.
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `dotnet test src/Reporter.Tests/Reporter.Tests.csproj` — xUnit-Suite `Reporter.Tests` (README „Tests", CI `pr-staging-ci.yml` Job `build & test` führt `dotnet test ... --configuration Release --no-build` nach Release-Build aus).
  - `npm test` — `node --test "scripts/*.test.mjs"`, Release-Tooling-Skripte (`package.json`); fachlich nicht betroffen, der Vollständigkeit halber mitgelaufen.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| dotnet-baseline-1 | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --logger "console;verbosity=normal" --logger "trx;LogFileName=baseline-run-1.trx" --results-directory docs/features/<feature>/inventory/test-results` (Debug-Build implizit, kein Filter) | Repo-Root | 0 | 350 | 0 | 0 | [Log](test-results/dotnet-test-baseline-run-1.log), [TRX](test-results/baseline-run-1.trx) |
| npm-baseline-1 | `npm test` | Repo-Root | 0 | 36 | 0 | 0 | [Log](test-results/npm-test-baseline-run-1.log) |

### Nachgewiesene bestehende Testfehler

Keine — beide Suiten liefen im Ausgangslauf vollständig grün (350/350 bzw. 36/36, 0 fehlgeschlagen, 0 übersprungen). Es gibt keine nachgewiesenen Preexisting-Fehler.

### Testlücken und Ausführungsprobleme

- Keine Ausführungsprobleme; beide Läufe vollständig.
- **Fachliche Testlücke (der eigentliche Bug):** Es existiert kein Test, der ein Keyword-Match beim Feed-Sync-Ingest prüft. `FeedSyncServiceTests` enthält keinen Keyword-Testfall; `CreateService`/`CreateFailingService` übergeben dem `FeedSyncService` weder `IKeywordRepository` noch `IKeywordMatcher` (der Konstruktor kennt diese Abhängigkeiten nicht). Das belegt den Ist-Zustand: Treffer-Artikel werden ungeprüft gespeichert und gelistet.
- `ServiceCollectionTests` löst nur Repositories und `FeedSearchService` auf — `IFeedSyncService`/`FeedSyncService` wird dort nicht aufgelöst, ein erweiterter Konstruktor wäre also aktuell ungeprüft.

## Testklassen

### `FeedSyncServiceTests`

Datei: `src/Reporter.Tests/FeedSyncServiceTests.cs` — Integrationstests gegen In-Memory-SQLite mit `FakeHttpMessageHandler`.

- `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk` — RSS-Items werden gespeichert, Health `Ok`, `SyncLog` geschrieben.
- `SyncFeedAsync_Duplicates_SkipsExistingItems` — erneuter Sync legt keine Duplikate an (`GuidOrHash`-Dedup).
- `SyncFeedAsync_DuplicatesWithinSameDocument_InsertsOnce` — Dublette im selben Feed-Dokument wird einmal eingefügt.
- `SyncFeedAsync_MixedNewAndExisting_BatchInsertsOnlyNew` — Batch-Insert nur der neuen Items.
- `SyncFeedAsync_Unreachable_KeepsItemsAndLogsError` — HTTP-Fehler → `Error`, Bestand bleibt.
- `SyncFeedAsync_InvalidXml_SetsError` — Parse-Fehler → `Error` + Log.
- `SyncFeedAsync_FewerItems_SetsWarning` — < 50 % Abrufmenge → `Warning`.
- `SyncFeedAsync_NoNewItemsForThirtyDays_SetsWarning` — > 30 Tage keine neuen Items → `Warning`.
- `SyncAllAsync_WithTwoFeeds_CreatesItemsForBoth` — Aggregat über zwei Feeds.
- `SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog` — Offline-Frühabbruch ohne Log/Health-Änderung.
- `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange` — Offline-Frühabbruch ohne HTTP-Aufruf.
- `SyncFeedAsync_NewItems_NotifiesWithFeedAndItems` — neue Items lösen Einzel-Benachrichtigungen über den echten `NotificationService` aus.
- `SyncFeedAsync_SummaryMode_SendsSingleSummaryNotification` — Summary-Modus sendet eine Sammel-Benachrichtigung.
- `SyncFeedAsync_NoNewItems_DoesNotNotify` — zweiter Sync ohne neue Items benachrichtigt nicht erneut.
- `SyncFeedAsync_FeedDisabled_NoNotifications` — `feed.NotificationsEnabled = false` → keine Benachrichtigung.
- `SyncFeedAsync_NotificationThrows_SyncStillSucceeds` — Fehler im Benachrichtigungspfad verfälscht das Sync-Ergebnis nicht.
- `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument` — URL-als-Titel wird durch Dokumenttitel ersetzt.
- `SyncFeedAsync_WhenTitleIsHostPlaceholder_UpdatesTitleFromFeedDocument` — Host-Platzhaltertitel wird ersetzt.
- `SyncFeedAsync_WhenTitleIsFileNamePlaceholder_UpdatesTitleFromFeedDocument` — Dateinamen-Platzhaltertitel wird ersetzt.
- `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle` — expliziter Titel bleibt erhalten.

Kein Keyword-/Filter-Testfall vorhanden. Im Test-Setup bereits vorhanden: `_keywordRepository` (Feld, Zeile 23/35) und `new KeywordMatcher()` in `CreateNotificationService` (Zeile 86) — beide werden aktuell nur für den `NotificationService` verdrahtet.

### `RetentionCleanupServiceTests`

Datei: `src/Reporter.Tests/RetentionCleanupServiceTests.cs`

- `CleanupAsync_DeletesExpiredButKeepsSaved` — allgemeine Fristregel: löscht abgelaufene gelesene Items, behält gemerkte und ungelesene.
- `CleanupAsync_ZeroOrNegativeRetentionDays_Skips` (Theory: `0`, `-5`) — deaktivierte Frist → keine Löschung.
- `CleanupAsync_RespectsConfiguredRetentionDays` — konfigurierte Frist als Stichtag.
- `CleanupAsync_CancelledToken_ThrowsOperationCanceled` — Cancellation propagiert.
- `CleanupAsync_DeletesKeywordMatchedExpired` — gelesener, fristablaufener Keyword-Treffer wird gelöscht, Nicht-Treffer bleibt.
- `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved` — ungelesene und gemerkte Treffer werden nie gelöscht (dokumentiert die Lösch-Invariante).
- `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt` — Fristbasis der Keyword-Regel ist `PublishedAt` (Fallback `ReadAt`), nicht `ReadAt`.

### `KeywordMatcherTests`

Datei: `src/Reporter.Tests/KeywordMatcherTests.cs`

- `MatchesAny_TitleCaseInsensitive` — Match im Titel, case-insensitiv.
- `MatchesAny_ContentHtml` — Match im HTML-Inhalt.
- `MatchesAny_Substring` — Teilwort innerhalb von Wörtern.
- `MatchesAny_NoMatch` — kein Treffer.
- `MatchesAny_NullTitleAndContent_ReturnsFalse` — `null`-Felder → `false`.
- `MatchesAny_EmptyKeywords_ReturnsFalse` — leere Keyword-Liste → `false`.
- `MatchesAny_BlankKeywords_Skipped` — Whitespace/leere Keywords werden übersprungen.

### `NotificationServiceTests`

Datei: `src/Reporter.Tests/NotificationServiceTests.cs` — Entscheidungskette mit `FakeLocalNotificationService` und `FakeTimeProvider`.

- `NotifyNewItemsAsync_FeedDisabled_SendsNothing` — Feed-Schalter aus.
- `NotifyNewItemsAsync_GlobalDisabled_SendsNothing` — globaler Schalter aus.
- `NotifyNewItemsAsync_WithinQuietHours_SendsNothing` (Theory: `23`, `6`) — Ruhezeit inkl. Wrap-around.
- `NotifyNewItemsAsync_OutsideQuietHours_Sends` (Theory: `12`, `21`).
- `NotifyNewItemsAsync_QuietHoursStartEqualsEnd_Sends` — leeres Ruhezeiten-Intervall.
- `NotifyNewItemsAsync_OnlyOneQuietHoursBound_Sends` (Theory) — einseitige Grenze deaktiviert Ruhezeit.
- `NotifyNewItemsAsync_KeywordMatch_SkipsItem` (Theory: `contentHtml` `null` / mit Treffer) — Keyword-Treffer wird von der Benachrichtigung ausgenommen (Titel- und Content-Match).
- `NotifyNewItemsAsync_SendsPerItem_WithItemIdAsIdentifier` — Einzel-Modus mit Item-ID als Identifier.
- `NotifyNewItemsAsync_SendsItemLink_InUserInfo` — `link` im `userInfo`.
- `NotifyNewItemsAsync_SummaryEnabled_SendsSingleSummary` — Summary-Modus mit Zähler und `feedId`-Präfix.
- `NotifyNewItemsAsync_SummaryEnabled_SameItems_SameIdentifier` — stabiler Summary-Identifier.
- `NotifyNewItemsAsync_SummaryEnabled_KeywordFiltered_ExcludedFromSummary` — Keyword-Treffer fließt nicht in Zähler/Identifier ein.

### `ItemRepositoryTests` (relevante Auswahl)

Datei: `src/Reporter.Tests/ItemRepositoryTests.cs`

- `GetUnreadByDateAsync_ReturnsUnreadSortedByPublishedAtDescending`, `GetUnreadByDateAsync_AllRead_ReturnsEmpty`, `GetUnreadByDateAsync_Paged_ReturnsPage`, `GetUnreadByDateAsync_Paged_ProjectsReadingTimeText` — Ungelesen-Abfragen (ungefiltert).
- `GetUnreadCountAsync_ReturnsCorrectCount` — Zähler.
- `AddRangeAsync_InsertsAllItems`, `AddRangeAsync_EmptyList_DoesNothing` — Batch-Insert.
- `GetExpiredKeywordCandidatesAsync_UsesPublishedAtOverReadAt`, `GetExpiredKeywordCandidatesAsync_KeepsUnreadAndSaved`, `GetExpiredKeywordCandidatesAsync_FallsBackToReadAt` — Kandidatenmenge der Keyword-Löschregel.
- `DeleteRangeAsync_DeletesOnlyGivenIds`, `DeleteRangeAsync_EmptyList_ReturnsZero` — ID-basierte Löschung.
- `DeleteExpiredAsync_*` — allgemeine Fristregel inkl. `ReadAt`-Priorität und Cancellation.

### `KeywordRepositoryTests`

Datei: `src/Reporter.Tests/KeywordRepositoryTests.cs`

- `AddAsync_ThenGetByIdAsync_ReturnsKeyword`, `GetAllAsync_ReturnsKeywordsOrderedByText`, `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesKeyword`, `GetByIdAsync_NonExisting_ReturnsNull`.

### `SettingsViewModelTests_Keywords`

Datei: `src/Reporter.Tests/SettingsViewModelTests_Keywords.cs`

- `AddKeyword_Valid_AddsToRepository` — Hinzufügen persistiert und füllt `Keywords`.
- `AddKeyword_DuplicateCaseInsensitive_ShowsError` — `OrdinalIgnoreCase`-Dublette → `ErrorKeywordDuplicate`.
- `AddKeyword_Empty_ShowsError` — leeres Feld → `ErrorKeywordEmpty`.
- `AddKeyword_TooLong_ShowsError` — > 500 Zeichen → `ErrorKeywordTooLong`.
- `RemoveKeyword_DeletesFromRepository` — Entfernen löscht Datensatz und Chip.

### `ServiceCollectionTests`

Datei: `src/Reporter.Tests/ServiceCollectionTests.cs`

- `AddReporterRepositories_ResolvesAllRepositories` — löst alle 6 Repository-Interfaces gegen In-Memory-SQLite auf.
- `AddReporterServices_ResolvesFeedSearchService` — löst `IFeedSearchService` mit `HttpClient` auf. `IFeedSyncService` wird nicht aufgelöst.

## Hilfsmethoden

### `TestDbContextFactory`

Datei: `src/Reporter.Tests/TestDbContextFactory.cs`

- Konstruktor — legt shared In-Memory-SQLite-DB an (`Mode=Memory;Cache=Shared;Default Timeout=30`), `EnsureCreated`.
- `CreateDbContext()` — neuer `ReporterDbContext` mit eigener Verbindung auf dieselbe DB.
- `Dispose()` — schließt Keep-Alive-Verbindung.

### `TestDataSeeder`

Datei: `src/Reporter.Tests/TestDataSeeder.cs`

- `SeedFeedAsync(TestDbContextFactory)` — legt einen Standard-Feed (`https://example.com/feed`) an, liefert `feedId`.

### `TestSettingsHelper`

Datei: `src/Reporter.Tests/TestSettingsHelper.cs`

- `SaveAsync(ISettingsRepository, notificationsEnabled?, notificationSummaryEnabled?, quietHoursStart?, quietHoursEnd?, language?)` — lädt Singleton-Settings und speichert mit Overrides zurück.

### `FakeNotificationService`

Datei: `src/Reporter.Tests/FakeNotificationService.cs`

- `Calls` — aufgezeichnete `NotifyNewItemsAsync`-Aufrufe (`NotificationCall(Feed, Items)`).
- `Exception` — optionale Ausnahme für Fehlerpfad-Tests.

### `FakeLocalNotificationService`

Datei: `src/Reporter.Tests/FakeLocalNotificationService.cs`

- `ShownNotifications` — aufgezeichnete `ShowAsync`-Aufrufe (`ShownNotification(Title, Body, Identifier, UserInfo)`).
- `AuthorizationResult`, `AuthorizationStatus`, `IsSupported`, `RequestAuthorizationCallCount` — steuerbare Berechtigungs-/Support-Antworten.

### `FakeNetworkStatusService`

Datei: `src/Reporter.Tests/FakeNetworkStatusService.cs`

- `IsOnline` (setzbar, Default `true`), `RaiseConnectivityChanged()` — Online/Offline-Simulation inkl. Event.

### `FakeFeedSyncService`

Datei: `src/Reporter.Tests/FakeFeedSyncService.cs`

- `SyncAllException`, `SyncAllBlocker`, `SyncAllCallCount` — `IFeedSyncService`-Fake für ViewModel-/AutoRefresh-Tests (liefert `SyncResult("OK", 0)`).

### `DelegatingItemRepository`

Datei: `src/Reporter.Tests/DelegatingItemRepository.cs`

- `IItemRepository`-Dekorierer: alle Member `virtual` an `_inner` delegiert; Basisklasse für Test-Doubles, die nur einzelne Member ändern (inkl. `GetExpiredKeywordCandidatesAsync`, `DeleteRangeAsync`).

### `FeedSyncServiceTests`-interne Helfer

- `SeedFeedAsync(url?, notificationsEnabled?)` — Feed über `FeedRepository` anlegen.
- `CreateService(content, statusCode?, notificationService?, networkStatusService?)` — `FeedSyncService` mit `FakeHttpMessageHandler` (RSS-Content oder Fehlerstatus); übergibt aktuell **keine** Keyword-Abhängigkeiten.
- `CreateFailingService(exception, networkStatusService?)` — Handler wirft beim Senden.
- `CreateNotificationService(localNotificationService)` — echter `NotificationService` mit `SettingsRepository`, `_keywordRepository`, `new KeywordMatcher()`.
- `RssXml(items, channelTitle?)` — generiert RSS-2.0-Dokument aus `(Title, Link, Guid, PubDate, Description)`-Tupeln (kein Atom-Generator vorhanden).
- `FakeHttpMessageHandler` (private Klasse) — `HttpMessageHandler` mit Response-Factory.
