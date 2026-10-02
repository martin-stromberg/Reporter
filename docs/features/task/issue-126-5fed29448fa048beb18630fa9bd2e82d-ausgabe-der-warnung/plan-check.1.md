<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

Geprüft: `requirement.md`, `inventory.md` inkl. Detaildokumente (`models.md`, `logic.md`, `enums.md`, `interfaces.md`, `tests.md`) gegen `plan.md`. Alle Referenz-Behauptungen des Plans wurden per grep im Repo verifiziert.

## Ergebnis

**Status:** Plan lückenhaft

Der Funktions- und Testteil des Plans ist vollständig und präzise: Die datenerhaltende Umbenennung (`RenameColumn`), die komplette Referenzabdeckung in Produktiv- und Testcode, die bewusste `DebugReportService`-Entscheidung und die konkreten, gegen die E2E-Infrastruktur verifizierten E2E-Tests sind alle sauber geplant. Einzige Lücke: Die Umbenennung `last_error_*`/`LastError*`/`ButtonShowErrorDetails`/`FeedErrorDetailsTitle`/`GetFeedErrorMessage`/`ShowFeedErrorDetailsAsync` invalidiert vier weitere Hilfedokumente, die der Dokumentationsschritt (Schritt 11) nicht nennt — darunter `feed-suche-technisch.md`, dessen Abschnitt 9 genau diesen Mechanismus dokumentiert.

## Abgleich Akzeptanzkriterien

Die `requirement.md` enthält keine nummerierte AC-Liste; abgeleitet aus der fachlichen Anforderung und den betroffenen Komponenten:

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Warnungsgrund bei `FeedHealth.Warning` einsehbar (beide Auslöser: <50 %-Artikeleinbruch, >30 Tage keine neuen Artikel) | `FeedSyncWarningKind` (`FewerItems`/`NoRecentItems`), `HealthDecision`-Record aus `DetermineStatus`, Persistenz über `LastMessageKind`/`LastMessage`, Anzeige via `ButtonShowMessage` → `ShowFeedMessageAsync` → `DisplayAlertAsync` (Plan: Übersicht, Designentscheidungen, Programmabläufe, Schritte 1/4/7/8) | Unit: `SyncFeedAsync_FewerItems_PersistsWarningMessage`, `SyncFeedAsync_NoNewItemsForThirtyDays_PersistsWarningMessage`; E2E: `FeedDetail_Message_OnlyForWarningFeed` | Abgedeckt |
| Datenerhaltende Migration `last_error_*` → `last_message_*` (Prüfschwerpunkt) | Migration `RenameFeedLastErrorToLastMessage` mit explizitem `RenameColumn`-`Up`/`Down`; Verifikationspflicht im Plan: bei `DropColumn`+`AddColumn` manuell umschreiben; `ReporterDbContextModelSnapshot` wird aktualisiert (Schritt 3, Abschnitt „Datenbankmigrationen") | `Feed_LastMessage_MappedToExpectedColumns` (`ReporterDbContextTests_Schema`), `Feed_PersistRoundtrip_LastMessage` | Abgedeckt — RenameColumn-Verifikation ist explizit geplant; kein automatisierter Alt-Schema-Migrationstest vorhanden (siehe Hinweise) |
| Fehlerdetails-Funktion bleibt nach Verallgemeinerung funktionsfähig (Regression) | Gemeinsame Felder, Fehler-`catch`-Pfad nutzt `MessageKind:`/`Message:`, `GetFeedMessage` mappt `FeedSyncErrorKind.*` weiterhin auf `FeedErrorKind*` (Designentscheidungen, Programmablauf Schritt 6) | Angepasste Tests `SyncFeedAsync_Failure_PersistsErrorKindAndMessage`, `*_ClassifiedAs*`, `GetFeedMessage_*`; E2E `FeedDetail_Message_OnlyForErrorFeed` (umbenannt, gleiche Assertions) | Abgedeckt |
| UI-Einstieg über Aktionsblatt der `FeedDetailPage` bei `Error` **und** `Warning`; kein Eintrag bei `Ok` | `OnFeedActionsClicked`: `HealthStatus is FeedHealth.Error or FeedHealth.Warning` (Schritt 8, Programmablauf) | E2E `FeedDetail_Message_OnlyForWarningFeed` inkl. Negativnachweis (gesunder Feed ohne Eintrag); E2E `FeedDetail_Message_OnlyForErrorFeed` inkl. Negativnachweis | Abgedeckt |
| Fallback für Feeds mit `Warning`-Status ohne gespeicherten Kind (Altbestand, `last_message_kind = NULL`) | Severity-abhängiger `default`-Zweig in `GetFeedMessage` → `FeedWarningKindUnknown` bei `Warning` (Designentscheidung „Kind→Text-Mapping") | `GetFeedMessage_WarningFallsBackToUnknown` | Abgedeckt |
| Reset-/Überschreib-Semantik: `Ok` leert, letzter Status (`Error`/`Warning`) gewinnt im gemeinsamen Feld | Programmablauf Schritte 4–6; Risikoabschnitt „Reset-Semantik vereinfacht sich" | `SyncFeedAsync_SuccessAfterWarning_ClearsLastMessage`, `SyncFeedAsync_ErrorAfterWarning_OverwritesLastMessage`, angepasstes `SyncFeedAsync_Success_ClearsLastError` | Abgedeckt |
| Teil-Updates (Rename/Kategorie/Edit) verwerfen die gespeicherte Meldung nicht | `FeedDetailViewModel.ToFeed` führt `LastMessageKind`/`LastMessage` mit (Schritt 7) | `RenameFeedAsync_PreservesLastMessage` (umbenannt) | Abgedeckt |
| Lokalisierte Texte EN + DE inkl. `AppResources.Designer.cs` | Schlüsselumbenennung `ButtonShowErrorDetails`→`ButtonShowMessage`, `FeedErrorDetailsTitle`→`FeedMessageDetailsTitle` + drei neue `FeedWarningKind*`-Schlüssel mit konkreten Formulierungen; Designer manuell nach `FeedErrorKind*`-Muster (Schritt 6) | Indirekt über `GetFeedMessage_*`-Tests und E2E-Alert-Assertions | Abgedeckt |
| Technische Kennzahlen (gelieferte/gespeicherte Artikelzahl, Datum des jüngsten Artikels) als Detail | `HealthDecision.Message` trägt englische Rohmeldung; `GetFeedMessage` hängt `LastMessage` als zweiten Absatz an (Designentscheidung „Technische Detailmeldung") | `GetFeedMessage_AppendsTechnicalMessage` (umbenannt); E2E prüft technischen Absatz | Abgedeckt |
| Dokumentation nach Implementierung | Schritt 11 nennt `synchronisation.md`, `feeddetailansicht.md`, `feeddetailansicht-technisch.md`, `datenmodell.md`, `mobile-ui-design.md`/`test-results.md` | — | **Lücke** — vier weitere Hilfedateien mit Referenzen auf die umbenannten Bezeichner fehlen (siehe unten) |
| Mobile-UI-Regeln (kein neuer Screen, ActionSheet/Alert-Muster, 390 × 844-Verifikation, Doku der Verifikation) | Schritt 8: Vergleich mit `design-draft/stitch_local_rss_feed_reader/feeds_health_status/screen.png` (existiert), Handysize-Verifikation, Dokumentationspflicht | — | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine — die Testplanung ist vollständig:

- Neue Unit-/Integrationstests decken beide Warnungsauslöser, das Leeren bei `Ok`, das Überschreiben bei `Error`, das Schema-Mapping, die ViewModel-Textbildung (beide Kinds + severity-abhängiger Fallback) und den `ToFeed`-Preserve ab.
- Alle grep-verifizierten betroffenen bestehenden Tests sind benannt: `FeedSyncServiceTests` (9 `LastError*`-Assertions, Zeilen 1071–1177), `FeedRepositoryTests` (10 Stellen, Zeilen 209–245), `ReporterDbContextTests_Persistence` (Zeilen 196–205), `FeedDetailViewModelTests` (15 Stellen, Zeilen 574–737), `DemoContentServiceTests` (Zeilen 85–86), E2E `FeedDetailTests` (Zeilen 429–450). `FeedSyncErrorKindTests` bleibt korrekt unverändert (Konstantenklasse ohne `Classify`-Logik → keine `FeedSyncWarningKindTests` nötig).
- Notwendige Fixtures/Hilfsmethoden sind benannt und verifiziert vorhanden: `stale-feed.xml` (Named-Fixture-Mechanismus des `StubFeedServer` bestätigt — `Fixtures/{name}.xml` schlägt `stub-feed.xml`, `StubFeedServer.cs:89-100`; csproj kopiert `Fixtures\**\*` mit `PreserveNewest`, `Reporter.E2ETests.csproj:31-33`), `SyncAllViaUnreadTab` (`FeedDetailTests.cs:64`), `ScopedTextContains` (`FeedDetailTests.cs:150`), `OpenFeedDetailActions` (`E2EPageHelpers.cs:426`).

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Warnungsgrund aus der UI einsehbar: Feed anlegen → 1. Sync (Ok) → Detailseite → Aktionsblatt „Aktualisieren" → 2. Sync (Warning) → Badge `HealthStatusWarningLabel` → Eintrag `ButtonShowMessage` → Alert mit `FeedWarningKindNoRecentItems`-Text + technischem Absatz | `FeedDetail_Message_OnlyForWarningFeed` (`FeedDetailTests.cs` + `Fixtures/stale-feed.xml`) — konkret, ausführbar, nutzt ausschließlich verifiziert vorhandene Helfer; Negativnachweis (gesunder Feed ohne Eintrag) enthalten | Abgedeckt |
| Fehlerfall weiterhin über den gemeinsamen „Meldung"-Eintrag erreichbar | `FeedDetail_Message_OnlyForErrorFeed` (umbenannt aus `FeedDetail_ErrorDetails_OnlyForErrorFeed`, gleiches Szenario, `FeedErrorKindHttpStatus`-Assertion) | Abgedeckt |
| Sichtbarkeitsregel `Error or Warning` / Absenz bei `Ok` | In beiden Pflicht-E2E-Tests als Negativnachweis enthalten | Abgedeckt |
| Zweiter Warnungsauslöser (`FewerItems`) über UI | Optionaler E2E-Test mit Begründung (identischer UI-Weg; Kind per `FeedSyncServiceTests`/`FeedDetailViewModelTests` abgesichert) | Abgedeckt — Option-Status vertretbar, da der UI-Fluss durch den Pflichttest belegt ist |

## Fehlende oder unvollständige Planbestandteile

- [ ] **Dokumentationsschritt (Schritt 11) deckt nicht alle durch die Umbenennung betroffenen Hilfedateien ab.** Der Plan benennt `synchronisation.md`, `feeddetailansicht.md`, `feeddetailansicht-technisch.md` und `datenmodell.md`, überlässt aber vier Dateien mit veralteten Bezeichnern:
  - `docs/help/anwendung/feed-suche-technisch.md` — 12 Referenzen, u. a. der komplette Abschnitt 9 „Fehlerdetails anzeigen (`FeedDetailPage` → `ShowFeedErrorDetailsAsync`)" mit `FeedHealthUpdate(ErrorKind:…)`, `feeds.last_error_kind`/`last_error_message`, `Feed.LastErrorKind`/`LastErrorMessage`, `GetFeedErrorMessage`, `ButtonShowErrorDetails`, `FeedErrorDetailsTitle` (Zeilen 27, 29, 97, 106, 107, 111–113, 167, 168, 176).
  - `docs/help/anwendung/architektur.md` — `GetFeedErrorMessage`, `Feed.LastErrorKind`/`LastErrorMessage`, `FeedListItem`-Felder (Zeilen 64, 66, 94).
  - `docs/help/einstellungen/troubleshooting.md` — Dialog „Fehlerdetails anzeigen" und Spalte `feeds.last_error_kind` (Zeile 42).
  - `docs/help/tests/ablauf-technisch.md` — nennt den umbenannten E2E-Test `FeedDetail_ErrorDetails_OnlyForErrorFeed` und `ButtonShowErrorDetails` (Zeile 60).
  Der Risikoabschnitt „Umbenennungs-Breite" zählt nur Quell-/Testdateien und den Snapshot — die Dokumentations-Kohärenz nach der Umbenennung (die Anforderung führt Dokumentation als betroffene Komponente) ist im Plan nicht abgedeckt. `docs/help/anwendung/mobile-ui-design.md` Zeilen 169–171 ist eine historische Verifikations-Notiz und kann als Zeitdokument stehen bleiben — eine kurze Entscheidung dazu gehört ebenfalls in den Plan.

## Hinweise

- **Migrations-Verifikation:** Der Plan verlangt zutreffend die manuelle Prüfung des generierten Codes auf `RenameColumn`. Ein automatisierter Migrationstest gegen eine Alt-Schema-DB ist nicht geplant und im Projekt ohne Muster (`TestDbContextFactory` nutzt `EnsureCreated` ohne Migrationen) — als manueller Nachweis im Plan hinreichend, die explizite Verifikationspflicht ist der richtige Ausgleich.
- **`FeedHealthUpdate`-Kompatibilität:** Verifiziert — der Record hat exakt zwei Erzeuger (`FeedSyncService.cs:105` benannt, `FeedSyncService.cs:229` positional); die Umbenennung der Member zu `MessageKind`/`Message` bricht den positionalen Aufruf nicht.
- **Referenzabdeckung vollständig verifiziert:** grep über `src/` bestätigt, dass alle `LastError*`/`last_error_*`/`GetFeedErrorMessage`/`ShowFeedErrorDetailsAsync`/`ButtonShowErrorDetails`/`FeedErrorDetailsTitle`-Vorkommen in den neun Quelldateien, fünf Unit-Testdateien, der E2E-Datei und den drei `AppResources`-Dateien liegen — alle im Plan benannt. Historische Migrationsdateien (`AddFeedLastError` samt Designer-Snapshots) bleiben korrekt unverändert.
- **`DebugReportService`:** Bewusste, begründete Nicht-Änderung — die Anforderung verlangt nur UI-Einsicht, und die Felder standen bisher nicht im Bericht. Abgedeckt.
- **E2E-Fixture `stale-feed.xml`:** Ausführbarkeit verifiziert — fester `pubDate` >30 Tage + stabile `<guid>` erzeugen beim ersten Sync `newItems > 0` → `Ok` (kein Auslöser, `existingCount == 0`), beim zweiten `newItems == 0` + altem `lastPublishedAt` → `Warning`; `fetchedCount == existingCount` verhindert Fehlauslösung des `FewerItems`-Zweigs.
- **`FeedSyncWarningKind`** als reine Konstantenklasse benötigt keine eigene Testklasse — `FeedSyncErrorKindTests` testet nur `Classify`, das kein Pendant hat.
