<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### `docs/help/anwendung/artikeldetailansicht.md` (Anwenderdoku, Abschnitt „Schlagworte")

- **Erreichbarkeit/Beschriftung** — Der Abschnitt erklärt der Endanwenderin die Herkunft der Schlagworte über XML-Elementnamen: „…als kommaseparierte Liste im Erweiterungselement `tags` (z. B. `<app:tags>`) oder als standardisierte Kategorien (`<category>`)…" (Zeile 22). Eine nicht-technische Leserin kennt Feed-Erweiterungselemente und deren Namen nicht; die Elementnamen sind für sie ohne Aussagewert, weil sie auf das Verhalten ohnehin keinen Einfluss nehmen kann. Der Satz bleibt zwar insgesamt verständlich, verlangt aber zum vollständigen Verständnis Vorwissen über das Feed-XML-Format.

  Empfehlung: Neutral formulieren ohne Elementnamen, z. B. „Liefert der Feed Schlagworte zu einem Artikel mit (z. B. als Stichwortliste oder Kategorien), erscheinen sie als schlichte Textzeile am Ende des Artikeltexts im Lesefenster." — die Beobachtung („Schlagworte erscheinen am Artikelende, abgeschwächt, nur wenn vorhanden") bleibt identisch.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- **Schlagworte in der Artikeldetailansicht sehen** (keine aktive Handlung nötig; die Zeile erscheint automatisch beim Öffnen eines getaggten Artikels): Die Schlagworte werden als kommaseparierte Textzeile (`<p class='tags'>` in `ArticleHtmlBuilder.Build`, src/Reporter.Core/Services/ArticleHtmlBuilder.cs:29-31) direkt hinter dem Artikeltext im WebView gerendert, scrollen mit und sind in Light (`#64748b`) wie Dark (`#94a3b8` via `prefers-color-scheme`, Zeilen 49-56, 62) deutlich schwächer als der Fließtext (`#0f172a`/`#f1f5f9`) abgegrenzt — genau die geforderte Darstellungsform („schlichte, schwächer eingefärbte Textzeile", keine Chips). Ohne Schlagworte entfällt der Block restlos (Zeile 29-30). Die Zeile enthält keinen Beschriftungs-Präfix — das entspricht der Anforderung wörtlich und folgt dem üblichen Schlagwort-Muster von News-Seiten. → unauffällig
- **Artikelsuche in der Feeddetailansicht über ein Schlagwort auslösen**: Das vorhandene Suchfeld „Artikel in diesem Feed suchen…" (`PlaceholderFeedDetailSearch`, src/Reporter/Views/FeedDetailPage.xaml:229-231) filtert jetzt Titel **oder** Schlagworte (`ItemRepository.GetByFeedAsync`, `i.Title.Contains(term) || i.Tags.Contains(term)`). Teilstring-Treffer sind identisch zur bisherigen Titelsuche — konsistent und ohne neues Bedienmuster; die Anwenderdoku (feeddetailansicht.md Zeile 32-35) benennt den durchsuchten Umfang korrekt („Titel und Schlagworte… nicht der Text des Artikels"). Kein Eingeben interner Kennungen, kein neues Feld. → unauffällig
- **Schlagworte beim Synchronisieren automatisch übernehmen** (keine Nutzerinteraktion): `FeedSyncService.ExtractTags` liest `tags`-Erweiterungselemente und `category`-Elemente; ein Backfill (`UpdateTagsAsync`) zieht fehlende Schlagworte für Bestandsartikel nach — aus Anwendersicht erscheinen die Zeilen einfach beim nächsten Abruf. → unauffällig
- **Anwenderdoku auf technische Interna**: `feeddetailansicht.md` ist sauber auf Anwenderebene formuliert; `datenmodell.md` ist als technische Referenzseite konzipiert und führt `Tags`/`items.tags` einheitlich zu den übrigen Entitäten — dort zulässig. `artikeldetailansicht.md` enthält dagegen XML-Elementnamen — siehe Befund oben. → Befund vorhanden

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:

- `src/Reporter/ViewModels/ArticleDetailViewModel.cs` (`RebuildHtml`, Zeilen 353-364)
- `src/Reporter.Core/Services/ArticleHtmlBuilder.cs` (komplett — erzeugt das sichtbare WebView-HTML inkl. Tag-Block und Light/Dark-CSS)
- `src/Reporter.Core/Services/FeedSyncService.cs` (`ExtractTags`, Tag-Backfill — betrifft das sichtbare Ergebnis)
- `src/Reporter.Data/Repositories/ItemRepository.cs` (`GetByFeedAsync`-Suchfilter, Tag-(De)Serialisierung)
- `src/Reporter.E2ETests/ArticleTagsTests.cs` und `src/Reporter.E2ETests/Fixtures/tag-feed.xml` (Sichtbarkeits-/Persistenznachweis der Tag-Zeile)
- `src/Reporter.E2ETests/FeedDetailTests.cs` (`FeedDetail_Search_FiltersByTag` — Suchverhalten)
- `docs/help/anwendung/artikeldetailansicht.md`
- `docs/help/anwendung/feeddetailansicht.md`
- `docs/help/anwendung/mobile-ui-design.md` (Abschnitt „Artikel-Schlagworte (issue-128)")
- `docs/help/anwendung/datenmodell.md`
