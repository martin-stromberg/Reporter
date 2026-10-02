<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Ausgabe der Warnung (Issue #126)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | Statische Konstantenklasse `FeedSyncWarningKind` mit `FewerItems` und `NoRecentItems` in `src/Reporter.Core/Services/` anlegen | Offen | — |
| 2 | Datenmodell | `Feed`-Entity (`src/Reporter.Data/Entities/Feed.cs`): `LastErrorKind`/`LastErrorMessage` → `LastMessageKind`/`LastMessage` umbenennen | Offen | — |
| 3 | Datenmodell | `Feed`-Domänenmodell (`src/Reporter.Core/Models/Feed.cs`): `LastErrorKind`/`LastErrorMessage` → `LastMessageKind`/`LastMessage` umbenennen | Offen | — |
| 4 | Datenmodell | `FeedListItem`: `LastErrorKind`/`LastErrorMessage` → `LastMessageKind`/`LastMessage` umbenennen | Offen | — |
| 5 | Datenmodell | Record `FeedHealthUpdate`: Member `ErrorKind`/`ErrorMessage` → `MessageKind`/`Message` umbenennen | Offen | — |
| 6 | Datenmodell | `ReporterDbContext.ConfigureFeed` auf Spalten-Mapping `last_message_kind` (TEXT, max. 50) und `last_message` (TEXT) umstellen | Offen | — |
| 7 | Migrationen | EF-Migration `RenameFeedLastErrorToLastMessage` scaffolden (`dotnet ef migrations add RenameFeedLastErrorToLastMessage --project src/Reporter.Data`); generiertes `Up`/`Down` muss `RenameColumn` enthalten (kein `DropColumn`/`AddColumn`); `ReporterDbContextModelSnapshot` aktualisiert | Offen | — |
| 8 | Persistenz | `FeedRepository.UpdateAsync`, `MapToModel`, `MapToEntity` und `GetAllWithDetailsAsync`-Projektion auf `LastMessageKind`/`LastMessage` umstellen | Offen | — |
| 9 | Logik | Privates `sealed record HealthDecision` (Status, Kind, Message) in `FeedSyncService` anlegen | Offen | — |
| 10 | Logik | `FeedSyncService.DetermineStatus` auf `HealthDecision`-Rückgabe umstellen — Warnungszweige liefern `FeedSyncWarningKind`-Wert + technische Meldung (Kennzahlen), `Ok`-Zweig `null` | Offen | — |
| 11 | Logik | `FeedSyncService.RunSyncAsync` reicht `Kind`/`Message` aus der Decision in `FeedHealthUpdate` durch (`MessageKind:`/`Message:`) | Offen | — |
| 12 | Logik | `FeedSyncService.UpdateFeedHealthAsync` schreibt `LastMessageKind`/`LastMessage`; `catch`-Pfad in `SyncFeedAsync` nutzt `MessageKind:`/`Message:`-Parameter | Offen | — |
| 13 | Seed | `DemoContentService` auf `LastMessageKind = null` / `LastMessage = null` umstellen | Offen | — |
| 14 | Ressourcen | `AppResources.resx` (EN): `ButtonShowErrorDetails` → `ButtonShowMessage` („Show message") und `FeedErrorDetailsTitle` → `FeedMessageDetailsTitle` („Sync message") umbenennen; `FeedWarningKindFewerItems`, `FeedWarningKindNoRecentItems`, `FeedWarningKindUnknown` neu | Offen | — |
| 15 | Ressourcen | `AppResources.de.resx` (DE): dieselben Umbenennungen („Meldung anzeigen", „Synchronisierungsmeldung") und drei neuen `FeedWarningKind*`-Schlüssel eintragen | Offen | — |
| 16 | Ressourcen | `AppResources.Designer.cs`: zwei Properties umbenennen, drei neue ergänzen (Muster `FeedErrorKind*`) | Offen | — |
| 17 | UI | `FeedDetailViewModel.GetFeedErrorMessage` → `GetFeedMessage(FeedListItem)`: flaches Kind-Mapping (`FeedErrorKind*` + `FeedWarningKind*`), severity-abhängiger Fallback, `LastMessage` als zweiter Absatz | Offen | — |
| 18 | UI | `FeedDetailViewModel.ToFeed` auf `LastMessageKind`/`LastMessage` umstellen (Preserve bei Rename/Kategorie/Edit) | Offen | — |
| 19 | UI | `FeedDetailPage.OnFeedActionsClicked`: `ButtonShowMessage`-Eintrag bei `FeedHealth.Error or FeedHealth.Warning` aufnehmen und dispatchen | Offen | — |
| 20 | UI | `FeedDetailPage.ShowFeedErrorDetailsAsync` → `ShowFeedMessageAsync` (`DisplayAlertAsync` mit `FeedMessageDetailsTitle`) | Offen | — |
| 21 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_FewerItems_PersistsWarningMessage` und `SyncFeedAsync_NoNewItemsForThirtyDays_PersistsWarningMessage` neu | Offen | — |
| 22 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_SuccessAfterWarning_ClearsLastMessage` und `SyncFeedAsync_ErrorAfterWarning_OverwritesLastMessage` neu | Offen | — |
| 23 | Tests | `FeedSyncServiceTests`: bestehende `LastError*`-Assertions und Testnamen auf `LastMessage*` umstellen | Offen | — |
| 24 | Tests | `FeedRepositoryTests`: `UpdateAsync_PersistsLastError` → `UpdateAsync_PersistsLastMessage`, `GetAllWithDetailsAsync_ProjectsLastError` → `..._ProjectsLastMessage` | Offen | — |
| 25 | Tests | `ReporterDbContextTests_Persistence.Feed_PersistRoundtrip_LastError` → `..._LastMessage`; `ReporterDbContextTests_Schema.Feed_LastMessage_MappedToExpectedColumns` neu | Offen | — |
| 26 | Tests | `FeedDetailViewModelTests`: `GetFeedErrorMessage_*` → `GetFeedMessage_*`, `RenameFeedAsync_PreservesLastError` → `..._PreservesLastMessage`; neu `GetFeedMessage_MapsWarningKindToLocalizedText` und `GetFeedMessage_WarningFallsBackToUnknown` | Offen | — |
| 27 | Tests | `DemoContentServiceTests.EnsureSeededAsync_FirstRun_SeedsExpectedFeedDefaults` auf `LastMessage* == null`-Assertions umstellen | Offen | — |
| 28 | E2E-Tests | Fixture `src/Reporter.E2ETests/Fixtures/stale-feed.xml` anlegen (Items mit pubDate >30 Tage alt, stabile `<guid>`-Elemente) | Offen | — |
| 29 | E2E-Tests | `FeedDetailTests.FeedDetail_ErrorDetails_OnlyForErrorFeed` → `FeedDetail_Message_OnlyForErrorFeed` auf `ButtonShowMessage` umstellen | Offen | — |
| 30 | E2E-Tests | `FeedDetailTests.FeedDetail_Message_OnlyForWarningFeed` implementieren und via `scripts/Run-E2ETests.ps1` laufen lassen | Offen | — |
| 31 | Dokumentation | Anwenderdoku aktualisieren: `anwendung/synchronisation.md` (Warnungsgrund einsehbar; Eintrag „Meldung anzeigen" bei Fehler/Warnung, Dialog „Synchronisierungsmeldung"), `anwendung/feeddetailansicht.md`, `anwendung/feed-suche.md`, `anwendung/beschreibung.md`, `anwendung/ablauf-anwender.md`, `einstellungen/fehlerbehebung-anwender.md`, `einstellungen/troubleshooting.md` (inkl. `feeds.last_message_kind`) | Offen | — |
| 32 | Dokumentation | Technische Doku aktualisieren: `anwendung/feeddetailansicht-technisch.md`, `anwendung/feed-suche-technisch.md` (Abschnitt 9 komplett + Referenzen/Regeln/Ressourcen/Testnamen), `anwendung/architektur.md`, `anwendung/datenmodell.md` (`LastMessage*`/`last_message_*` + Migration `RenameFeedLastErrorToLastMessage`) | Offen | — |
| 33 | Dokumentation | Testdoku aktualisieren: `tests/ablauf-technisch.md` (Testname `FeedDetail_Message_OnlyForErrorFeed`, `ButtonShowMessage`, neuer Warnungs-Test), `tests/architektur.md`, `tests/beschreibung.md` | Offen | — |
| 34 | Dokumentation | Mobile-UI-Verifikation dokumentieren (390 × 844 pt Handysize, Vergleich mit `design-draft/feeds_health_status`, Aktionsblatt/Dialog-Muster) als eigener Abschnitt in `mobile-ui-design.md` — historische issue-87/issue-115-Notizen bleiben unverändert — oder `test-results.md` | Offen | — |
| 35 | Abschluss | `scripts/Run-StaticChecks.ps1` (Exit 0, keine Befunde) und `scripts/Run-E2ETests.ps1` ausführen; bekannte Bestandsfehler einordnen | Offen | — |
