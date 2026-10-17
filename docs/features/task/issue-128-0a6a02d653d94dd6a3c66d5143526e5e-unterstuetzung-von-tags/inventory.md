<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Unterstützung von Tags (Issue #128)

Analysiert wurde der Bestand rund um Feed-Sync (`FeedSyncService`), Item-Persistenz (`Item`/`ItemRepository`/`ReporterDbContext`), Artikeldetail-Anzeige (`ArticleDetailViewModel`/`ArticleDetailPage`) und Feed-Artikelsuche (`FeedDetailViewModel.SearchText` → `ItemRepository.GetByFeedAsync`), bezogen auf die Anforderung, `<app:tags>`-Schlagworte (WDR-Namespace `http://www.wdr.de/rss/1.0/modules/app/1.0/`) zu persistieren, abgeschwächt anzuzeigen und in die Suche einzubeziehen.

## Zusammenfassung

- **Kein Tag-Bestand:** Weder `Reporter.Data.Entities.Item` noch `Reporter.Core.Models.Item` oder `ItemListItem` besitzen eine `Tags`-Eigenschaft; die Tabelle `items` hat keine `tags`-Spalte und keine Migration dafür. `FeedSyncService` liest derzeit keine `SyndicationItem.ElementExtensions`.
- **Vorhandene Ankerpunkte:** `ItemImageService` zeigt bereits das ElementExtensions-Auslesemuster (`OuterNamespace`/`OuterName` + `GetReader()`); `FeedSyncService.CollectNewItems` legt neue `Item`-Objekte an und bietet mit `backfillCandidates`/`imageBackfillCandidates` ein etabliertes Backfill-Muster; `ItemRepository.GetByFeedAsync(feedId, page, pageSize, searchTerm)` filtert derzeit ausschließlich `i.Title.Contains(term)`; `ArticleDetailViewModel.RebuildHtml` erzeugt das komplette WebView-HTML inkl. Light-/Dark-CSS — der Tag-Block würde dort ans `content` angehängt.
- **Offene Designpunkte aus der Anforderung betreffen realen Bestand:** Bestandsartikel werden per `GuidOrHash`-Dedup übersprungen (Backfill-Frage), `CreateItemCopy` in `ArticleDetailViewModel` und `UpdateAsync` in `ItemRepository` kopieren Felder einzeln und müssten `Tags` explizit durchreichen, und die `ItemListRow`-Projektion enthält nur Darstellungsfelder (Tags sind dort nicht nötig, solange die Suche DB-seitig bleibt).
- **Test-Ausgangszustand:** Unit-Test-Suite `Reporter.Tests` vollständig grün — **731/731 bestanden, 0 fehlgeschlagen, 0 übersprungen** (Solution-Build Release/win-x64 vorher mit 0 Warnungen/Fehlern). Testlücke: `Reporter.E2ETests` (FlaUI) wurde nicht ausgeführt — benötigt interaktive Desktop-Session und ist nicht Teil der CI. Details und Nachweise: [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md) — `Item`-Entity, `Item`-Domain-Modell, `ItemListItem`, `ItemContent`, `ItemContentEntry`, EF-Mapping und Migrationsstand
- [Logik](inventory/logic.md) — `FeedSyncService`, `ItemRepository`, `ArticleDetailViewModel`, `FeedDetailViewModel`, `ArticleHtmlSanitizer`, `KeywordFilter`, `ItemImageService`
- [Interfaces](inventory/interfaces.md) — `IItemRepository`, `IKeywordFilter`, `IItemContentStore`
- [Tests](inventory/tests.md) — Test-Ausgangszustand, Nachweise, relevante Testklassen und Hilfsmethoden
