<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Atom-0.3-Unterstützung für den Feed-Sync (Issue #91)

## Übersicht

Der Sync-Pfad in `FeedSyncService.RunSyncAsync` wird so erweitert, dass Atom-0.3-Dokumente (Namespace `http://purl.org/atom/ns#`) zusätzlich zu RSS 2.0 und Atom 1.0 akzeptiert werden. Umgesetzt wird dies über einen normalisierenden `XmlReader`-Wrapper, der Atom-0.3-Elemente on-the-fly auf Atom 1.0 abbildet, sodass `SyndicationFeed.Load` die einzige Parse-Stelle bleibt. Betroffen ist ausschließlich `Reporter.Core` (Parse-Stelle plus neue Reader-Klasse); Datenmodell, Interfaces, DI, UI und die gesamte nachgelagerte Pipeline (`CollectNewItems`, `DetermineStatus`, Health, Benachrichtigung) bleiben unverändert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Umsetzungsvariante | **Variante B:** normalisierender `XmlReader`-Wrapper (`Atom03NormalizingXmlReader`, Adapter-/Decorator-Muster auf `XmlReader`-Ebene) statt eigenem `Atom03FeedFormatter` | `SyndicationFeed.Load` bleibt die zentrale und einzige Parse-Stelle; Datums-, Link- und Text-Konstrukt-Parsing (inkl. `PublishDate`/`LastUpdatedTime`, `Links`, `TextSyndicationContent`) liefert das Framework. Ein eigener `SyndicationFeedFormatter` müsste die komplette Feed-Parsing-Logik in Eigenregie nachbauen — mehr Code, mehr Fehlerquellen, gleiches Ergebnis. |
| Format-Erkennung | Root-Peek: `XmlReader.MoveToContent()` auf dem Response-Stream, dann Prüfung `LocalName == "feed" && NamespaceURI == "http://purl.org/atom/ns#"`; bei Treffer wird der bereits positionierte Reader in den Wrapper gepackt | Der `HttpClient`-Stream ist nicht seekbar — `MoveToContent` liest nur Präambel/Whitespace/Kommentare und verletzt die Seekbarkeit nicht; ein `MemoryStream`-Puffer entfällt. `SyndicationFeed.Load` ruft intern selbst `MoveToContent`, ein vorpositionierter Reader ist unproblematisch. |
| Erkennungskriterium `version`-Attribut | Wird **nicht** geprüft — allein Namespace + Root-Name entscheiden | Robuster gegenüber fehlendem oder abweichendem `version`-Attribut; der Namespace ist das verbindliche Formatmerkmal. |
| Scope der Atom-0.3-Abdeckung | Teilmenge anhand des realen Feeds plus robuste Defaults: `feed`/`entry`, `title`, `link` (`href`/`rel`/`type`), `id`, `tagline`, `copyright`, `issued`, `modified`, `summary`, `content` sowie `author`/`generator` soweit namensgleich | Die Anforderung verlangt die Synchronisation des Sportschau-Feeds „wie bei RSS/Atom 1.0" — nicht volle Atom-0.3-Konformanz. `dc:`-Elemente und unbekannte Atom-0.3-Elemente landen als `ElementExtensions` und werden ignoriert. `content mode="base64"` bleibt Framework-Verhalten (Base64-Dekodierung bei MIME-`type`); `mode="xml"` ohne `div`-Wrapper kann weiterhin in den bestehenden `Parse`-Fehlerpfad laufen — bewusst akzeptiert. |
| Datumsabbildung | `issued` → `published` (`SyndicationItem.PublishDate`), `modified` → `updated` (`LastUpdatedTime`); `created` wird **nicht** abgebildet | `issued` ist in Atom 0.3 das Publikationsdatum und entspricht `published` in Atom 1.0. Ein `created`→`published`-Fallback wäre kontraproduktiv: In der Atom-0.3-Reihenfolge folgt `created` auf `issued`, und bei Mehrfachvorkommen gewinnt der letzte Wert — `created` würde das fachlich korrekte `issued` überschreiben. Feeds ohne `issued` erhalten `PublishedAt = null` (bestehendes `DateTimeOffset.MinValue`→`null`-Verhalten); der SHA256-Fallback in `NormalizeGuidOrHash` funktioniert auch mit `null`. |
| `type`-Attributwert-Übersetzung | Auf den Atom-0.3-Text-/Content-Konstrukten `title`, `tagline`, `copyright`, `summary`, `content` werden `type`-Werte übersetzt (`text/plain`→`text`, `text/html`→`html`, `application/xhtml+xml`→`xhtml`); bei `mode="base64"` keine Übersetzung; `link`-Elemente sind explizit ausgenommen | Atom 0.3 nutzt MIME-Typen, Atom 1.0 die Kürzel `text`/`html`/`xhtml`. Ohne Übersetzung interpretiert der `Atom10FeedFormatter` `text/html` als generischen MIME-Typ und erzeugt kein lesbares `TextSyndicationContent` — `GetContentHtml` läge leer oder der Parse würde fehlschlagen. `link@type` ist in beiden Formaten ein MIME-Typ und darf nicht angetastet werden (Favicon-Lookup via `alternate`-Link). |
| Sichtbarkeit der neuen Klasse | `public` in `Reporter.Core.Services` | Es existiert kein `InternalsVisibleTo` für `Reporter.Tests`; direkte Unit-Tests der Reader-Klasse erfordern `public`. XML-Dokumentationskommentare sind Pflicht (`CS1591` als `WarningsAsErrors`). |

## Programmabläufe

### Feed-Abruf mit Format-Erkennung (geändert)

1. `RunSyncAsync` lädt Bestandsitems und `lastPublishedAt` (unverändert).
2. `HttpClient.GetStreamAsync(feed.Url)` liefert den Response-Stream (unverändert).
3. `XmlReader.Create(stream, settings)` mit `DtdProcessing.Ignore` (unverändert).
4. **Neu:** `reader.MoveToContent()` positioniert auf das Root-Element. Anschließend Prüfung `LocalName == "feed" && NamespaceURI == Atom-0.3-Namespace` (Konstante auf `Atom03NormalizingXmlReader`).
5. **Neu:** Bei Treffer wird der Reader in `Atom03NormalizingXmlReader` gewrappt; sonst bleibt der Original-Reader. `SyndicationFeed.Load` wird mit dem effektiven Reader per `Task.Run` aufgerufen — für Atom 0.3 sieht der `Atom10FeedFormatter` ein normalisiertes Atom-1.0-Dokument.
6. Rest der Pipeline unverändert: `syndicationFeed.Items` → `CollectNewItems` (Dedup, Keyword-Filter, `Item`-Mapping) → `AddRangeAsync` → `DetermineStatus` → `ResolveFeedTitle` → `TryFindFaviconUrlAsync` → `UpdateFeedHealthAsync`/`UpdateLogAsync` → `NotifyNewItemsAsync` (fehlerisoliert).
7. Fehlerpfad unverändert: `XmlException` (unparsebar, `mode="xml"` ohne `div`-Wrapper, EOF ohne Root-Element o. ä.) läuft in den `catch` von `SyncFeedAsync` → `FeedSyncErrorKind.Parse` → `FeedHealth.Error` + `SyncLog` + `IDebugLogService`.

Beteiligte Klassen/Komponenten: `FeedSyncService`, `Atom03NormalizingXmlReader` (neu), `SyndicationFeed`/`SyndicationFeed.Load`, `XmlReader`

### Atom-0.3-Normalisierung (neu, innerhalb `Atom03NormalizingXmlReader`)

Der Wrapper delegiert sämtliche `XmlReader`-Member an den inneren Reader und übersetzt nur die folgenden Aspekte:

1. **`NamespaceURI`:** `http://purl.org/atom/ns#` → `http://www.w3.org/2005/Atom` (auf Element- und ggf. Attributknoten; `xmlns`-, `xml:`- und `dc:`-Namespaces bleiben unberührt).
2. **`LocalName` auf Element-/EndElement-Knoten im Atom-0.3-Namespace:** `tagline`→`subtitle`, `issued`→`published`, `modified`→`updated`, `copyright`→`rights`. Alle übrigen Namen (`feed`, `entry`, `title`, `link`, `id`, `summary`, `content`, `author`, `generator`, `created`, `info`, …) werden unverändert durchgereicht. `Name` kombiniert konsistent `Prefix` + übersetzten `LocalName`.
3. **`type`-Attributwerte:** Ist der Reader auf einem `type`-Attribut positioniert (`GetAttribute`, Indexer, `MoveToAttribute` + `Value`) und gehört das umschließende Element zu den Text-/Content-Konstrukten (`title`, `tagline`, `copyright`, `summary`, `content`), wird der Wert übersetzt (`text/plain`→`text`, `text/html`→`html`, `application/xhtml+xml`→`xhtml`). Dazu merkt sich der Wrapper den lokalen Namen des zuletzt gelesenen Elements. Bei `mode="base64"` auf dem Element findet keine Übersetzung statt. `link@type` und alle anderen Attribute bleiben unberührt.
4. **`mode`-Attribut und alle übrigen Attribute** werden unverändert durchgereicht (der Atom-1.0-Formatter ignoriert unbekannte Attribute bzw. legt sie als Extensions ab).
5. **Nicht abgebildet (bewusst):** `created` (kein `published`-Fallback, siehe Designentscheidung), `author/url` (Autoren-URI — wird von der Pipeline nicht ausgewertet), `dc:`-Erweiterungen.

Beteiligte Klassen/Komponenten: `Atom03NormalizingXmlReader`, `XmlReader`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `Atom03NormalizingXmlReader` (`src/Reporter.Core/Services/`) | Klasse (`XmlReader`-Ableitung, `public`) | Delegierender Reader, der ein Atom-0.3-Dokument on-the-fly als Atom 1.0 präsentiert: Namespace-Umbiegung `http://purl.org/atom/ns#` → `http://www.w3.org/2005/Atom`, Element-Umbenennungen (`tagline`→`subtitle`, `issued`→`published`, `modified`→`updated`, `copyright`→`rights`) sowie `type`-Wert-Übersetzung auf Text-/Content-Konstrukten. Hält die öffentliche Konstante des Atom-0.3-Namespace für die Erkennung. |

## Änderungen an bestehenden Klassen

### `FeedSyncService` (Service, `src/Reporter.Core/Services/FeedSyncService.cs`)

- **Geänderte Methoden:** `RunSyncAsync` — Die Parse-Stelle (aktuell Zeilen 156–159) wird um die Format-Erkennung erweitert: nach `XmlReader.Create` ein `MoveToContent`-Peek auf das Root-Element; bei Root `feed` im Atom-0.3-Namespace wird der Reader in `Atom03NormalizingXmlReader` gewrappt, sonst unverändert weitergereicht. `SyndicationFeed.Load` wird weiterhin als einzige Parse-Stelle aufgerufen (im bestehenden `Task.Run`). Die Erkennung darf in eine kleine `private static` Hilfsmethode ausgelagert werden, wenn es der Lesbarkeit dient. Konstruktor, `IFeedSyncService`-Signatur und alle anderen Methoden bleiben unverändert — keine neuen Abhängigkeiten, keine DI-Änderung.

### `TestFeedXml` (Test-Hilfsklasse, `src/Reporter.Tests/TestFeedXml.cs`)

- **Neue Methoden:** `Atom03(entries, feedTitle)` — baut analog zu `Rss`/`Atom` ein Atom-0.3-Dokument: `<?xml …?>`, `<feed version="0.3" xmlns="http://purl.org/atom/ns#" xmlns:dc="http://purl.org/dc/elements/1.1/">` mit `<title>` und feed-seitigem `<link rel="alternate" href="…">`; pro Entry `<entry>` mit `<title>`, `<link href="…">`, `<id>`, optional `<issued>`/`<modified>` (ISO-„o"-Format) und optionalem `<content type="text/html" mode="escaped">`. Signatur mit Tupel-Parametern im Stil der bestehenden Methoden, u. a. `Issued`- und `Modified`-Felder, damit die Datumsabbildung prüfbar ist.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — es kommen keine neuen oder geänderten Eingaben hinzu. Nicht lesbare Dokumente laufen weiterhin unverändert in den bestehenden `Parse`-Fehlerpfad.

## Konfigurationsänderungen

Keine — die Atom-0.3-Unterstützung gilt implizit für alle Feeds; kein Schalter, keine `Settings`-Erweiterung, keine `appsettings`-Einträge.

## Seiteneffekte und Risiken

- **Parse-Fehlerpfad bei Nicht-Feed-Dokumenten:** Der neue `MoveToContent`-Peek wirft `XmlException` bei ungültigem XML bereits vor `SyndicationFeed.Load` — landet im selben `catch` und wird identisch als `FeedSyncErrorKind.Parse` klassifiziert. Bei EOF ohne Root-Element bleibt der Reader unverändert, `Load` wirft wie bisher. Die bestehenden Tests `SyncFeedAsync_InvalidXml_SetsError`/`_ClassifiedAsParse` verifizieren dies.
- **RSS-2.0-/Atom-1.0-Pfad:** Der Peek konsumiert nur Präambel/Whitespace; `SyndicationFeed.Load` ruft intern selbst `MoveToContent` — kein Verhaltensbruch. Abgesichert durch die bestehenden RSS-/Atom-1.0-Tests (u. a. `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk`, `SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved`).
- **Kontextsensitive `type`-Übersetzung:** Eine versehentliche Übersetzung von `link@type` würde `alternate`-Links/Favicon-Lookup beeinflussen — daher strikte Bindung an die Text-/Content-Konstrukte und Absicherung durch einen Reader-Unit-Test (link-`type` bleibt unverändert).
- **Delegationsvollständigkeit:** `XmlReader` hat rund 30 abstrakte Member; der Wrapper muss alle delegieren, damit Aufrufe wie `ReadSubtree`/`ReadInnerXml` (u. a. bei `type="xhtml"`-Content) korrekt durch die Übersetzung laufen. Risiko rein implementierungsseitig, durch die Unit-Tests abgedeckt.
- **Speicher/Laufzeit:** Kein Puffern des Dokuments — der Wrapper arbeitet streamend; Overhead vernachlässigbar.
- **Feeds mit `version`-Abweichungen oder ohne `issued`:** Werden weiterhin verarbeitet (`version` ignoriert) bzw. erhalten `PublishedAt = null` — entspricht dem bisherigen Verhalten bei fehlenden Datumselementen.

## Umsetzungsreihenfolge

1. **`Atom03NormalizingXmlReader` anlegen** (`src/Reporter.Core/Services/Atom03NormalizingXmlReader.cs`)
   - Voraussetzungen: `System.ServiceModel.Syndication` 10.0.11 und `System.Xml` sind bereits im `Reporter.Core`-Projekt referenziert; keine neuen NuGet-Pakete nötig. XML-Doc-Kommentare Pflicht (`CS1591` als `WarningsAsErrors`).
   - Beschreibung: `XmlReader`-Ableitung mit Delegationskonstruktor `(XmlReader inner)`; Namespace- und `LocalName`-Übersetzung gemäß Mapping-Tabelle; kontextsensitive `type`-Wert-Übersetzung (Element-Merker); öffentliche Konstante `NamespaceUri` (`http://purl.org/atom/ns#`).

2. **Parse-Stelle in `FeedSyncService.RunSyncAsync` erweitern**
   - Voraussetzungen: `Atom03NormalizingXmlReader` (Schritt 1).
   - Beschreibung: `MoveToContent`-Peek + Root-Prüfung (`feed` im Atom-0.3-Namespace) + bedingtes Wrappen vor `SyndicationFeed.Load`; Erkennung ggf. als `private static` Hilfsmethode.

3. **`TestFeedXml.Atom03`-Hilfsmethode ergänzen** (`src/Reporter.Tests/TestFeedXml.cs`)
   - Voraussetzungen: Keine (unabhängige Testinfrastruktur).
   - Beschreibung: Atom-0.3-Dokument-Builder im Stil von `Rss`/`Atom` mit `issued`/`modified`-Parametern und `content mode="escaped"`.

4. **Unit-Tests für `Atom03NormalizingXmlReader` schreiben** (`src/Reporter.Tests/Atom03NormalizingXmlReaderTests.cs`, neu)
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Facts für Namespace-Umbiegung, Element-Renames, `type`-Übersetzung inkl. `link`-Ausschluss, Passthrough fremder Namespaces sowie ein Roundtrip-Test `SyndicationFeed.Load` auf gewrapptem Atom-0.3-Dokument.

5. **Integrationstests in `FeedSyncServiceTests` ergänzen**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: Neue Facts für Atom-0.3-Happy-Path (`FeedHealth.Ok`, Items mit `Title`/`Link`/`PublishedAt`/`GuidOrHash`, `SyncLog` abgeschlossen), Datumsabbildung `issued`→`PublishedAt` und Platzhalter-Titel-Auflösung über Atom-0.3-Dokument.

6. **Regression: `Reporter.Tests`-Suite ausführen**
   - Voraussetzungen: Schritte 1–5.
   - Beschreibung: Gesamte Suite muss unverändert grün bleiben — insbesondere die RSS-, Atom-1.0- und Parse-Fehlerpfad-Tests.

7. **Dokumentation aktualisieren**
   - Voraussetzungen: Schritt 2 (Verhalten final).
   - Beschreibung: `docs/help/einstellungen/ablauf-technisch.md` (Abschnitt 4, Ingest-Pfad um `SyndicationFeed.Load`) um Format-Erkennung und Atom-0.3-Normalisierung ergänzen; `docs/help/anwendung/synchronisation.md` unterstützte Formate präzisieren („RSS-/Atom-Daten" → RSS 2.0, Atom 1.0, Atom 0.3); `docs/help/anwendung/architektur.md` `FeedSyncService`-Beschreibung um die Normalisierung ergänzen.

8. **Statische Prüfungen ausführen**
   - Voraussetzungen: Schritte 1–7.
   - Beschreibung: `scripts/Run-StaticChecks.ps1` muss mit Exit-Code 0 und ohne Findings durchlaufen (Projektregel).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `Atom03(entries, feedTitle)` | `TestFeedXml` (Hilfsmethode) | Baut Atom-0.3-Testdokumente (`feed version="0.3"` im `http://purl.org/atom/ns#`-Namespace, `dc`-Namespace, `issued`/`modified`, `content mode="escaped"`). |
| `Wrap_Atom03Root_TranslatesNamespace` | `Atom03NormalizingXmlReaderTests` | Elemente im Atom-0.3-Namespace melden `http://www.w3.org/2005/Atom` als `NamespaceURI`. |
| `Wrap_Atom03_RenamesElements` | `Atom03NormalizingXmlReaderTests` | `tagline`→`subtitle`, `issued`→`published`, `modified`→`updated`, `copyright`→`rights` (Element- und EndElement-Knoten). |
| `Wrap_ContentType_TranslatesMimeToAtom10` | `Atom03NormalizingXmlReaderTests` | `type="text/html"` auf `content`/`summary` wird zu `html`; `mode` bleibt unverändert. |
| `Wrap_LinkType_NotTranslated` | `Atom03NormalizingXmlReaderTests` | `link type="text/html"` bleibt unverändert (Kontextsensitivität). |
| `Wrap_OtherNamespaces_PassThrough` | `Atom03NormalizingXmlReaderTests` | `dc:`- und sonstige Elemente/Attribute werden unverändert durchgereicht. |
| `Load_WrappedAtom03_ProducesFeedWithItemsAndDates` | `Atom03NormalizingXmlReaderTests` | `SyndicationFeed.Load` auf gewrapptem Dokument liefert `SyndicationFeed` mit Items, `PublishDate` aus `issued`, `LastUpdatedTime` aus `modified`, `Title`, `Links`, `TextSyndicationContent`. |
| `SyncFeedAsync_Atom03_CreatesItems_AndSetsHealthOk` | `FeedSyncServiceTests` | Atom-0.3-Dokument → `FeedHealth.Ok`, Items persistiert mit `Title`/`Link`/`PublishedAt`/`GuidOrHash`, `SyncLog` abgeschlossen. |
| `SyncFeedAsync_Atom03_MapsIssuedToPublishedAt` | `FeedSyncServiceTests` | `Item.PublishedAt` entspricht dem `issued`-Datum des Entrys (kein `null`). |
| `SyncFeedAsync_Atom03_PlaceholderTitle_UpdatesTitleFromFeedDocument` | `FeedSyncServiceTests` | Platzhalter-`Feed.Title` wird durch den `title` des Atom-0.3-Dokuments ersetzt (`ResolveFeedTitle` greift über die Normalisierung). |

### Betroffene bestehende Tests

Keine — alle bestehenden Tests bleiben unverändert und dienen als Regressionssicherung; explizit zu beobachten sind `SyncFeedAsync_ValidRss_CreatesItems_AndSetsHealthOk` (RSS-Pfad), `SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved` (Atom-1.0-Pfad) sowie `SyncFeedAsync_InvalidXml_SetsError`/`SyncFeedAsync_InvalidXml_ClassifiedAsParse` (Parse-Fehlerpfad mit `MoveToContent`-Peek).

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich. Begründung: Die Anforderung ändert **keinen Benutzerfluss** — keine UI-, Interface- oder Datenmodelländerung; der Anwender legt Feeds an und aktualisiert sie exakt wie bisher (dieser Fluss ist bereits durch die E2E-Smoke-Tests `DirectAdd_FeedAppearsInListAndDatabase` und `Search_SubscribesResult_PersistsFeed` abgedeckt). Der einzige Verhaltensunterschied ist das akzeptierte Server-Payload-Format (Atom 0.3 zusätzlich), das über denselben unveränderten UI-Pfad eingeht — über die UI ist dafür kein neues oder abweichendes Nutzerszenario erreichbar. Der Funktionsnachweis erfolgt über die geplanten Integrationstests, die den vollständigen Sync-Pfad (HTTP-Fake → Format-Erkennung → Parse → Dedup → Persistenz → Health/`SyncLog`) gegen eine echte In-Memory-SQLite-DB prüfen.

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| — | Keine bestehenden E2E-Tests betroffen. |

## Offene Punkte

Keine. Die in der Anforderung genannten offenen Fragen (Umsetzungsvariante, Scope der Atom-0.3-Abdeckung, Datumsabbildung, `version`-Attribut bei der Erkennung) sind interne Designentscheidungen und wurden im Abschnitt „Designentscheidungen" festgelegt.
