<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Atom-0.3-Unterstützung für den Feed-Sync (Issue #91)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `Atom03NormalizingXmlReader`-Klasse in `src/Reporter.Core/Services/` anlegen: `XmlReader`-Ableitung (`public`, XML-Docs), Delegationskonstruktor, Namespace-Umbiegung `http://purl.org/atom/ns#` → `http://www.w3.org/2005/Atom`, öffentliche Konstante `NamespaceUri` | Erledigt | `Atom03NormalizingXmlReaderTests.Wrap_Atom03Root_TranslatesNamespace` |
| 2 | Logik | Element-Umbenennungen im `Atom03NormalizingXmlReader` implementieren (`tagline`→`subtitle`, `issued`→`published`, `modified`→`updated`, `copyright`→`rights`; `created` bewusst nicht abgebildet) | Erledigt | `Atom03NormalizingXmlReaderTests.Wrap_Atom03_RenamesElements` |
| 3 | Logik | Kontextsensitive `type`-Wert-Übersetzung im `Atom03NormalizingXmlReader` implementieren (`text/plain`→`text`, `text/html`→`html`, `application/xhtml+xml`→`xhtml`; nur auf `title`/`tagline`/`copyright`/`summary`/`content`; `link` und `mode="base64"` ausgenommen) | Erledigt | `Atom03NormalizingXmlReaderTests.Wrap_ContentType_TranslatesMimeToAtom10`, `Wrap_ContentType_TranslatesOnAttributePosition`, `Wrap_LinkType_NotTranslated` |
| 4 | Logik | Format-Erkennung in `FeedSyncService.RunSyncAsync` einbauen: `MoveToContent`-Peek + Root-Prüfung (`feed` im Atom-0.3-Namespace) + bedingtes Wrappen vor `SyndicationFeed.Load` | Erledigt | `FeedSyncServiceTests.SyncFeedAsync_Atom03_CreatesItems_AndSetsHealthOk` (+ RSS-/Atom-1.0-Regressionstests) |
| 5 | Tests | `TestFeedXml.Atom03(entries, feedTitle)`-Hilfsmethode ergänzen (Atom-0.3-Dokument mit `version="0.3"`, `dc`-Namespace, `issued`/`modified`, `content mode="escaped"`) | Erledigt | von allen Atom-0.3-Tests genutzt |
| 6 | Tests | `Atom03NormalizingXmlReaderTests` anlegen: Namespace-Umbiegung, Element-Renames, `type`-Übersetzung inkl. `link`-Ausschluss, Passthrough fremder Namespaces | Erledigt | 7 Facts, alle grün |
| 7 | Tests | Roundtrip-Test `Load_WrappedAtom03_ProducesFeedWithItemsAndDates` in `Atom03NormalizingXmlReaderTests` | Erledigt | grün |
| 8 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_Atom03_CreatesItems_AndSetsHealthOk` hinzufügen (`FeedHealth.Ok`, Items mit `Title`/`Link`/`PublishedAt`/`GuidOrHash`, `SyncLog` abgeschlossen) | Erledigt | grün |
| 9 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_Atom03_MapsIssuedToPublishedAt` hinzufügen | Erledigt | grün |
| 10 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_Atom03_PlaceholderTitle_UpdatesTitleFromFeedDocument` hinzufügen | Erledigt | grün |
| 11 | Tests | Regression: gesamte `Reporter.Tests`-Suite ausführen (RSS-, Atom-1.0- und Parse-Fehlerpfad unverändert grün) | Erledigt | 502/502 bestanden (Release); Review: alle 10 Atom-0.3-Tests grün (Debug) |
| 12 | Dokumentation | `docs/help/einstellungen/ablauf-technisch.md`: Format-Erkennung und Atom-0.3-Normalisierung im Ingest-Pfad (Abschnitt 4) ergänzen | Erledigt | — |
| 13 | Dokumentation | `docs/help/anwendung/synchronisation.md`: unterstützte Feed-Formate präzisieren (RSS 2.0, Atom 1.0, Atom 0.3) | Erledigt | — |
| 14 | Dokumentation | `docs/help/anwendung/architektur.md`: `FeedSyncService`-Beschreibung um Atom-0.3-Normalisierung ergänzen | Erledigt | — |
| 15 | Qualität | `scripts/Run-StaticChecks.ps1` ausführen — Exit-Code 0 ohne Findings | Erledigt | Format, Lizenzheader, Security, Static-Analysis-Build: alle bestanden, 0 Warnungen/Fehler |
