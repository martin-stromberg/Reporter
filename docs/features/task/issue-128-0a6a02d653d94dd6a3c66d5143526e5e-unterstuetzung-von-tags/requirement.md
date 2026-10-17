<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: Unterstützung von Tags (Issue #128)

## Fachliche Zusammenfassung

Beim Synchronisieren von Feeds (`FeedSyncService`) sollen Schlagworte (Tags) aus dem Feed-Item-Erweiterungselement `<app:tags>` ausgelesen und am Artikel (`Item`) persistiert werden. Der konkret genannte Feed (`https://www1.wdr.de/uebersicht-100.feed`, Atom 1.0) liefert das Element pro `<entry>` im Namespace `xmlns:app="http://www.wdr.de/rss/1.0/modules/app/1.0/"` mit kommaseparierten Begriffen, z. B. `<app:tags>SWB, Aufsichtsrat, Stadtbahnen</app:tags>`; ein Feed-ebene-Element `<app:tags>` existiert ebenfalls, ist aber nicht gemeint — die Anforderung bezieht sich auf die Beiträge. In der Artikeldetailansicht (`ArticleDetailPage`) werden die erfassten Schlagworte am Ende nach dem Artikeltext angezeigt und farblich schwächer vom Text abgegrenzt; ohne Schlagworte entfällt die Ausgabe vollständig. In der Artikelsuche der Feed-Detailansicht (`FeedDetailViewModel.SearchText` → `ItemRepository.GetByFeedAsync(feedId, page, pageSize, searchTerm)`) werden die Schlagworte zusätzlich zum Titel berücksichtigt.

## Betroffene Klassen und Komponenten

### Datenmodell

- `Reporter.Data.Entities.Item`: neue Eigenschaft `Tags` (`string?`), Spalte `tags` in Tabelle `items`.
- `Reporter.Core.Models.Item`: neue Eigenschaft `Tags` (init-only; Datentyp als Annahme: `IReadOnlyList<string>` oder delimitierter `string?` — abhängig vom gewählten Speicherformat).
- `ReporterDbContext.ConfigureItem` (`src/Reporter.Data/ReporterDbContext.cs`): Mapping der `tags`-Spalte (nullable).
- Neue EF-Migration (z. B. `AddItemTags`) unter `src/Reporter.Data/Migrations/` inkl. `ReporterDbContextModelSnapshot`.
- **Annahme:** Speicherung in der Haupt-Datenbank (`reporter.db`, `ReporterDbContext`), nicht in der Content-Datenbank (`reporter-content.db`, `ContentDbContext`/`ItemContent`). Begründung: Die Artikelsuche filtert DB-seitig über `context.Items`; ein datenbankübergreifender Join auf `ItemContent` ist nicht möglich. Tags sind zudem kleine Metadaten, keine Bulk-Daten — die Backup-Trennung (Items eingeschlossen, Content ausgeschlossen) bleibt sinnvoll.
- `ItemContentEntry`/`IItemContentStore`: voraussichtlich **nicht** betroffen (siehe Annahme oben).
- `ItemListItem`: voraussichtlich **nicht** betroffen — die Tags werden nur in der Detailansicht angezeigt; die Listendarstellung bleibt unverändert. Falls sie später in Listen gebraucht werden, wären `ItemListItem` und `ItemRepository.SelectListItemRows`/`ItemListRow` zu erweitern.

### Logik / Services

- `FeedSyncService.CollectNewItems` (`src/Reporter.Core/Services/FeedSyncService.cs`): Extraktion der Tags pro `SyndicationItem` aus `item.ElementExtensions` (Element mit `OuterName == "tags"` im Namespace `http://www.wdr.de/rss/1.0/modules/app/1.0/`; Wert per `GetObject<System.Xml.Linq.XElement>()` lesen), Split an `,`, Trimmen der Einzelbegriffe, Verwerfen leerer Einträge; Befüllung des neuen `Item`-Felds beim Anlegen.
- `ItemRepository` (`src/Reporter.Data/Repositories/ItemRepository.cs`):
  - `MapToEntity`/`MapToModel` um `Tags` erweitern.
  - `GetByFeedAsync(feedId, page, pageSize, searchTerm)`: Suchfilter von `i.Title.Contains(term)` auf Titel **oder** Tags erweitern (null-sicher, z. B. `i.Title.Contains(term) || (i.Tags != null && i.Tags.Contains(term))`).
- `IItemRepository` (`src/Reporter.Core/Interfaces/IItemRepository.cs`): Signatur unverändert; XML-Dokumentation des `searchTerm`-Parameters auf „Titel oder Schlagworte" präzisieren.

### UI / ViewModel

- `ArticleDetailViewModel` (`src/Reporter/ViewModels/ArticleDetailViewModel.cs`), Methode `RebuildHtml`: Schlagwort-Block ans Ende des generierten HTML anhängen (z. B. `<p class='tags'>…</p>`) mit abgeschwächter Farbe für Light- und Dark-Scheme (`prefers-color-scheme`), sodass die Tags mit dem Artikeltext mitscrollen. Die Tag-Texte müssen beim Einfügen HTML-encodiert werden (`WebUtility.HtmlEncode`), da sie ungeprüfter Feed-Content sind und der Block **nach** dem `ArticleHtmlSanitizer.Sanitize`-Aufruf angehängt wird.
- Alternative (Annahme, zu entscheiden): separates `Label` in `ArticleDetailPage.xaml` unterhalb des `WebView` mit `MetaStyle`/sekundärer Textfarbe via `AppThemeBinding` — verwirft jedoch die Mitscroll-Eigenschaft, da das `WebView` den restlichen Raum füllt. Die Formulierung „am Ende nach dem Text" spricht für die Variante im WebView-HTML.

### Tests

- `src/Reporter.Tests/FeedSyncServiceTests.cs`: Sync speichert Tags aus `<app:tags>`; Items ohne Element erhalten keine Tags; leere/Whitespace-Einträge werden verworfen; Feed-ebenen-`<app:tags>` wird nicht übernommen.
- `src/Reporter.Tests/TestFeedXml.cs`: Hilfsbuilder ggf. um `app:tags`-Ausgabe erweitern (Namespace-Deklaration beachten).
- `src/Reporter.Tests/ItemRepositoryTests.cs`: `GetByFeedAsync`-Suche trifft Tag-Begriffe (und weiterhin den Titel); Artikel ohne Tags bleiben auffindbar/nicht betroffen.
- `src/Reporter.Tests/FeedDetailViewModelTests.cs`: `SearchText` liefert per Tag gefilterte Artikel.
- `src/Reporter.Tests/ArticleHtmlSanitizerTests.cs` bzw. ViewModel-Test für `ArticleDetailViewModel`: Tags werden ans Ende angehängt und HTML-encodiert (Injection-Schutz); ohne Tags kein Block.
- `src/Reporter.Tests/ReporterDbContextTests_Schema.cs`/`ReporterDbContextTests_Persistence.cs`: neue Spalte prüfen/persistieren.
- `src/Reporter.E2ETests`: Sichtbarkeit der Tag-Zeile in der Detailansicht sowie Tag-Treffer in der Feed-Artikelsuche (analog zu `FeedDetailTests`).
- Test-Doubles: `DelegatingItemRepository`, `FakeItemContentStore` u. a. sind betroffen, falls Signaturen angepasst werden (bei reiner Property-Erweiterung nicht).

### Dokumentation (Folgeschritte)

- `docs/help/anwendung/artikeldetailansicht.md`, `docs/help/anwendung/feeddetailansicht.md`, `docs/help/anwendung/datenmodell.md` sowie `docs/help/anwendung/mobile-ui-design.md` (UI-Verifikation des Tag-Blocks, 390 × 844 pt, Light/Dark).

## Implementierungsansatz

1. **Parsen:** In `CollectNewItems` wird beim Iterieren über `context.FeedItems` pro `SyndicationItem` das `ElementExtensions`-Element `tags` im WDR-Namespace gelesen. Da `SyndicationFeed.Load` unbekannte Elemente als `SyndicationElementExtension` in `ElementExtensions` ablegt, ist kein eigener `XmlReader`-Hook nötig; der bestehende Parse-Pfad (`PrepareFeedReader`, `Atom03NormalizingXmlReader` für Atom 0.3) bleibt unverändert. Ergebnis: normalisierte `IReadOnlyList<string>` (getrimmt, leere Einträge entfernt) oder `null` bei fehlendem Element.
2. **Persistieren:** `Item.Tags` (Domain) ↔ `ItemEntity.Tags` (`tags`-Spalte). Speicherformat als einzelne Textspalte (z. B. kommasepariert) ist die einfachste Variante und reicht für die `Contains`-Suche; eine eigene `item_tags`-Tabelle wäre nur nötig, falls künftig exakte Tag-Matches, Tag-Autovervollständigung oder Tag-Listen gefordert werden — in der Anforderung nicht begründet (Annahme).
3. **Suchen:** Der vorhandene Debounce-/Paging-Pfad bleibt unverändert; nur die `Where`-Bedingung in `GetByFeedAsync` wird um den Tag-Abgleich ergänzt.
4. **Anzeigen:** `ArticleDetailViewModel.RebuildHtml` hängt bei vorhandenen Tags einen HTML-Block mit eigener CSS-Klasse ans `content` an; das CSS definiert für Light und Dark jeweils eine schwächere Farbe (z. B. sekundäre Textfarbe/`opacity`). Keine Tags → kein Block, keine sichtbare Ausgabe.

## Konfiguration

Keine Konfiguration erforderlich. Die Anforderung verlangt kein konfigurierbares Verhalten; das Feature ist beim Sync und in der Anzeige/Suche immer aktiv.

## Offene Fragen

1. **Backfill für Bestandsartikel:** Bereits gespeicherte Artikel werden beim Sync per `GuidOrHash`-Dedup übersprungen und erhalten daher keine Tags. Sollen Tags analog zum Content-/Image-Backfill (`backfillCandidates`/`imageBackfillCandidates` in `CollectNewItems`) für bereits gespeicherte Artikel nachgezogen werden, solange sie noch im Feed enthalten sind?
2. **Element-Abgleich:** Soll ausschließlich `<app:tags>` im WDR-Namespace `http://www.wdr.de/rss/1.0/modules/app/1.0/` gelesen werden, oder jedes Erweiterungselement mit lokalem Namen `tags` (unabhängig vom Namespace), oder zusätzlich standardisierte Kategorien (Atom `<category>`, RSS `<category>`)? Die Anforderung nennt nur `app:tags`; eine Generalisierung ist plausibel, aber nicht gefordert.
3. **Suchsemantik:** Teilstring-Treffer über die gespeicherte Tag-Zeichenkette (konsistent zur bisherigen Titelsuche per `Contains`; „bahn" trifft dann auch „Stadtbahnen") oder nur exakte Einzel-Tag-Treffer?
4. **Darstellungsform:** Schlagworte als schlichte, schwächer eingefärbte Textzeile (kommasepariert, wie im Feed geliefert) oder als Chip-/Badge-Elemente? „Farblich vom Text etwas schwächer abgegrenzt" deutet auf schwächeren Fließtext, nicht auf hervorgehobene Chips.
5. **Wechselwirkung mit Stichwort-Filter:** Sollen Tags beim Feed-Stichwortfilter (`IKeywordFilter.MatchesAny`, derzeit Titel + Inhalt) ebenfalls als Filtergrundlage dienen? Von der Anforderung nicht gefordert — Annahme: nein.
