<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

- [x] `FeedSyncWarningKind` (statische Konstantenklasse, `src/Reporter.Core/Services/FeedSyncWarningKind.cs`) — angelegt mit `FewerItems = "FewerItems"` und `NoRecentItems = "NoRecentItems"`
- [x] `HealthDecision` (privates `sealed record` mit `Status`, `Kind`, `Message` in `FeedSyncService.cs`, Zeile ~564) — angelegt
- [x] `RenameFeedLastErrorToLastMessage` (EF-Migration `src/Reporter.Data/Migrations/20261002192257_RenameFeedLastErrorToLastMessage.cs`) — angelegt; `Up`/`Down` enthalten ausschließlich `RenameColumn` (kein `DropColumn`/`AddColumn`); `ReporterDbContextModelSnapshot` aktualisiert (`last_message_kind` max. 50, `last_message`)
- [x] `stale-feed.xml` (E2E-Fixture `src/Reporter.E2ETests/Fixtures/stale-feed.xml`) — angelegt; zwei Items mit `pubDate` Jan 2024 und stabilen `<guid>`-Elementen; Auslieferung über Named-Fixture-Mechanismus des `StubFeedServer` (`/feeds/{name}.xml`), `PreserveNewest` per `Fixtures\**\*`-Wildcard in `Reporter.E2ETests.csproj`
- [x] Felder `LastMessageKind`/`LastMessage` in `Feed` (Entity, `src/Reporter.Data/Entities/Feed.cs`) — umbenannt, XML-Docs auf Fehler-oder-Warnung-Semantik angepasst
- [x] Felder `LastMessageKind`/`LastMessage` in `Feed` (Domänenmodell, `src/Reporter.Core/Models/Feed.cs`) — umbenannt
- [x] Felder `LastMessageKind`/`LastMessage` in `FeedListItem` (`src/Reporter.Core/Models/FeedListItem.cs`) — umbenannt
- [x] Member `MessageKind`/`Message` im Record `FeedHealthUpdate` (`src/Reporter.Core/Services/FeedHealthUpdate.cs`) — umbenannt, Defaults `null` erhalten
- [x] `ReporterDbContext.ConfigureFeed` — Mapping auf `last_message_kind` (max. 50) und `last_message` umgestellt
- [x] `FeedRepository` — `UpdateAsync`, `MapToModel`, `MapToEntity` und `GetAllWithDetailsAsync`-Projektion führen `LastMessageKind`/`LastMessage`
- [x] `FeedSyncService.DetermineStatus` — Rückgabetyp `HealthDecision`; `FewerItems`-Zweig mit Kennzahl-Meldung (`fetchedCount`/`existingCount`), `NoRecentItems`-Zweig mit `lastPublishedAt`-Datum, `Ok`-Zweig mit `null`-Werten; Schwellen unverändert
- [x] `FeedSyncService.RunSyncAsync` — nutzt `decision.Status` für `SyncLog`-Meldung, `UpdateFeedHealthAsync` und `SyncResult`; `FeedHealthUpdate(resolvedTitle, faviconUrl, MessageKind: decision.Kind, Message: decision.Message)` — `Ok` leert die Felder
- [x] `FeedSyncService.UpdateFeedHealthAsync` — schreibt `LastMessageKind = update.MessageKind` / `LastMessage = update.Message`
- [x] `FeedSyncService.SyncFeedAsync` (`catch`-Pfad) — `new FeedHealthUpdate(MessageKind: errorKind, Message: message)`; Warnungskind wird vom Fehlerkind überschrieben („letzter Status gewinnt")
- [x] `DemoContentService` — Seed-Feed auf `LastMessageKind = null` / `LastMessage = null` umgestellt
- [x] `AppResources.resx` (EN) — `ButtonShowMessage` = „Show message", `FeedMessageDetailsTitle` = „Sync message", neue Schlüssel `FeedWarningKindFewerItems`/`FeedWarningKindNoRecentItems`/`FeedWarningKindUnknown` mit den geplanten Texten; `FeedErrorKind*` unverändert
- [x] `AppResources.de.resx` (DE) — „Meldung anzeigen", „Synchronisierungsmeldung" und die drei `FeedWarningKind*`-Texte
- [x] `AppResources.Designer.cs` — Properties `ButtonShowMessage`/`FeedMessageDetailsTitle` umbenannt, `FeedWarningKindFewerItems`/`FeedWarningKindNoRecentItems`/`FeedWarningKindUnknown` ergänzt
- [x] `FeedDetailViewModel.GetFeedMessage` — flaches `switch` auf `feed.LastMessageKind` (`FeedSyncErrorKind.*` → `FeedErrorKind*`, `FeedSyncWarningKind.*` → `FeedWarningKind*`), `default` → `FeedWarningKindUnknown` bei `HealthStatus == Warning` sonst `FeedErrorKindUnknown`, `feed.LastMessage` als `"\n\n"`-Absatz
- [x] `FeedDetailViewModel.ToFeed` — übernimmt `LastMessageKind`/`LastMessage` (Preserve bei Teil-Updates)
- [x] `FeedDetailPage.OnFeedActionsClicked` — Eintrag `AppResources.ButtonShowMessage` bei `feed.HealthStatus is FeedHealth.Error or FeedHealth.Warning` vor `ButtonDelete`; Dispatch-Zweig entsprechend
- [x] `FeedDetailPage.ShowFeedMessageAsync` — `DisplayAlertAsync` mit `FeedMessageDetailsTitle`, `GetFeedMessage(feed)`, `ButtonOk`
- [x] `FeedSyncServiceTests` — neue Tests `SyncFeedAsync_FewerItems_PersistsWarningMessage`, `SyncFeedAsync_NoNewItemsForThirtyDays_PersistsWarningMessage`, `SyncFeedAsync_SuccessAfterWarning_ClearsLastMessage`, `SyncFeedAsync_ErrorAfterWarning_OverwritesLastMessage`; alle `LastError*`-Assertions auf `LastMessage*` umgestellt (`SyncFeedAsync_Success_ClearsLastMessage`, `*_ClassifiedAs*`-Tests)
- [x] `FeedRepositoryTests` — `UpdateAsync_PersistsLastMessage`, `GetAllWithDetailsAsync_ProjectsLastMessage` umbenannt und umgestellt
- [x] `ReporterDbContextTests_Persistence.Feed_PersistRoundtrip_LastMessage` — umbenannt/umgestellt; `ReporterDbContextTests_Schema.Feed_LastMessage_MappedToExpectedColumns` neu (Spaltennamen + MaxLength 50)
- [x] `FeedDetailViewModelTests` — `GetFeedMessage_*`-Umbenennungen, `RenameFeedAsync_PreservesLastMessage`, neu `GetFeedMessage_MapsWarningKindToLocalizedText` (beide Kinds) und `GetFeedMessage_WarningFallsBackToUnknown`
- [x] `DemoContentServiceTests.EnsureSeededAsync_FirstRun_SeedsExpectedFeedDefaults` — `LastMessage* == null`-Assertions
- [x] `FeedDetailTests` (E2E) — `FeedDetail_Message_OnlyForErrorFeed` auf `ButtonShowMessage` umgestellt; `FeedDetail_Message_OnlyForWarningFeed` neu implementiert (stale-feed-Fixture → 2. Sync → `HealthStatusWarningLabel` → `ButtonShowMessage` → Alert mit `FeedWarningKindNoRecentItems` via `ScopedTextContains`, gesunder Feed ohne Eintrag)
- [x] Dokumentation — alle 14 geplanten Hilfedokumente aktualisiert (`synchronisation.md`, `feeddetailansicht.md`, `feed-suche.md`, `beschreibung.md`, `ablauf-anwender.md`, `fehlerbehebung-anwender.md`, `troubleshooting.md` inkl. `feeds.last_message_kind`, `feeddetailansicht-technisch.md`, `feed-suche-technisch.md` Abschnitt 9 komplett überarbeitet, `architektur.md`, `datenmodell.md` inkl. `RenameFeedLastErrorToLastMessage` und beider Kind-Wertemengen, `tests/ablauf-technisch.md`, `tests/architektur.md`, `tests/beschreibung.md`); historische issue-87-Notizen in `feed-suche-technisch.md`/`mobile-ui-design.md` als Zeitdokumente belassen
- [x] Mobile-UI-Verifikation — eigener Abschnitt „Sync-Meldung bei Fehler und Warnung (issue-126)" in `docs/help/anwendung/mobile-ui-design.md` (Draft-Vergleich `feeds_health_status`, 390 × 844 pt, Aktionsblatt/Alert-Muster, 44 × 44-pt-Touch-Target)
- [x] `FeedSyncErrorKind`/`FeedSyncErrorKindTests` — unverändert, wie geplant
- [x] `DebugReportService` — unverändert, wie geplant
- [x] `IFeedRepository` — unverändert, wie geplant

## Hinweise

- **Testlauf-Nachweise:** Die in `inventory/test-results/` abgelegten Protokolle (`unittests-release-console.log` mit 668 Tests und dem alten Testnamen `GetFeedErrorMessage_AppendsTechnicalMessage`, `e2etests-console.log` mit 29/31) sind die Baseline-Läufe aus der Bestandsaufnahme, nicht der Post-Implementierungs-Lauf. Ein Post-Implementierungs-Build ist jedoch belegt: `stale-feed.xml` angelegt 21:24, Release-Build von `Reporter.Core`/`Reporter.Tests` 21:24 (konsistent mit `Run-StaticChecks.ps1`), Debug-Build von `Reporter.exe`/`Reporter.E2ETests.dll` 21:28 (konsistent mit dem Build-Schritt von `Run-E2ETests.ps1`). Die mobile-ui-design.md-Verifikation dokumentiert den E2E-Lauf explizit. Vor dem Abschluss-Commit empfiehlt sich eine kurze Gegenprobe, ob der E2E-Lauf mit dem neuen Test (32 Tests erwartet: 30 bestanden + 2 bekannte Bestandsfehler `DemoSeedTests`/`ArticleImageTests`) tatsächlich durchlief — kein persistiertes Ergebnisprotokoll vorhanden.
- **Testname bewusst belassen:** `SyncFeedAsync_Failure_PersistsErrorKindAndMessage` heißt weiterhin so — fachlich weiterhin korrekt (der Test persistiert einen *Fehler*-Kind in den Meldungsfeldern); die Assertions sind umgestellt. Plan verlangte nur „sinngemäße" Anpassung.
- **Grep-Verifikation:** Keine verbliebenen Referenzen auf `LastError*`/`last_error_*`/`ButtonShowErrorDetails`/`FeedErrorDetailsTitle`/`GetFeedErrorMessage`/`ShowFeedErrorDetailsAsync`/`FeedDetail_ErrorDetails` in `src/` oder `docs/` außer den beabsichtigten historischen Nennungen (`AddFeedLastError`-Migrationshistorie in `datenmodell.md`/`feed-suche-technisch.md`, issue-87-Verifikationsnotizen in `mobile-ui-design.md`).
