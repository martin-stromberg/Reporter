<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Unterstützung von Tags (Issue #128)

## Übersicht

Schlagworte aus Feed-Item-Erweiterungselementen mit lokalem Namen `tags` (beliebiger Namespace, insbesondere `<app:tags>` des WDR-Namespace `http://www.wdr.de/rss/1.0/modules/app/1.0/`) sowie aus standardisierten Kategorien (`SyndicationItem.Categories` — Atom-`<category term="…"/>` und RSS-`<category>…</category>`) werden beim Feed-Sync gelesen, dedupliziert, als kommaseparierte `tags`-Spalte an `items` persistiert, in der Artikeldetailansicht als abgeschwächte Textzeile im WebView-HTML angezeigt und in der Artikelsuche der Feed-Detailansicht neben dem Titel per Teilstring-Match durchsucht. Bereits gespeicherte Artikel ohne Tags erhalten die aktuellen Feed-Tags beim nächsten Sync nachgezogen (Backfill, nur fehlende Werte). Betroffen sind `Item` (Entity und Domain-Modell), `ReporterDbContext` inkl. neuer EF-Migration, `ItemRepository`, `FeedSyncService`, die neue HTML-Aufbau-Hilfsklasse `ArticleHtmlBuilder`, `ArticleDetailViewModel` sowie Unit- und E2E-Tests.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| `Item.Tags` (Domain-Modell `Reporter.Core.Models.Item`) | `IReadOnlyList<string>` (`init`, Standard leere Liste) | Strukturierter Typ im Domain-Modell; die serialisierte Form bleibt Persistenzdetail des Repository (Data-Mapper-Muster). Erleichtert HTML-Encoding je Einzelbegriff und Tag-Assertions in Tests. |
| `items.tags` (Entity `Reporter.Data.Entities.Item`) | Einzelne nullable `TEXT`-Spalte mit kommaseparierten Begriffen (`string?`) | Reicht für die geforderte `Contains`-Suche; eine eigene `item_tags`-Tabelle wäre nur für exakte Tag-Matches, Autovervollständigung oder Tag-Listen nötig — in der Anforderung nicht begründet. |
| Speicherort | Haupt-Datenbank `reporter.db` (`ReporterDbContext`), nicht `reporter-content.db` | Die Artikelsuche filtert DB-seitig über `context.Items`; ein datenbankübergreifender Join auf `ItemContent` ist nicht möglich. Tags sind kleine Metadaten und bleiben damit im Cloud-Backup. |
| Element-Abgleich beim Parsen | Alle `ElementExtensions` mit `OuterName == "tags"` (beliebiger `OuterNamespace`) **plus** `SyndicationItem.Categories` (`SyndicationCategory.Name`); Zusammenführung mit Dedup per `StringComparer.Ordinal`, Reihenfolge des ersten Vorkommens | Anwender-Entscheidung: lokaler Name `tags` in jedem Namespace deckt `app:tags` und gleichnamige Erweiterungen anderer Feeds ab; `SyndicationItem.Categories` liefert standardisierte Atom-`<category term>`- und RSS-`<category>`-Werte über eine einheitliche Property (`Name`) — `SyndicationFeed.Load` (Atom10-/Rss20-Formatter) füllt diese Collection bereits, standardisierte Kategorien tauchen nicht in `ElementExtensions` auf. |
| Tag-Backfill für Bestandsartikel | Ja — Artikel ohne gespeicherte Tags, die noch im Feed enthalten sind, erhalten die Feed-Tags beim nächsten Sync nachgezogen; nur fehlende Werte, kein Update geänderter Tags | Ohne Backfill bliebe das Feature für sämtliche bereits gespeicherten Artikel unsichtbar; das Muster existiert bereits (`backfillCandidates`/`imageBackfillCandidates` in `CollectNewItems`). |
| Schreibpfad des Backfills | Neue `IItemRepository`-Methode `UpdateTagsAsync` (Batch, ItemId → Tags) | `UpdateAsync` würde zusätzlich `ContentHtml` in den Content-Store zurückschreiben und alle Felder überschreiben; eine dedizierte Methode aktualisiert nur `tags` und hat keine Content-Store-Seiteneffekte. |
| Anzeige der Tags | Tag-Block im WebView-HTML in `ArticleDetailViewModel.RebuildHtml`, angehängt an `content` | Scrollt mit dem Artikeltext mit („am Ende nach dem Text"); `ArticleDetailPage.xaml` bleibt unverändert; Dark Mode wird über das vorhandene `prefers-color-scheme`-CSS abgedeckt statt über `AppThemeBinding`. |
| Darstellungsform | Schlichte Textzeile `<p class='tags'>`, kommasepariert, farblich abgeschwächt | Anwender-Entscheidung: „farblich vom Text etwas schwächer abgegrenzt" = schwächerer Fließtext, keine Chips/Badges. |
| **Testbarkeit des HTML-Aufbaus** | Neue `public static`-Hilfsklasse `ArticleHtmlBuilder` in `src/Reporter.Core/Services/` (Namespace `Reporter.Core.Services`), die aus bereits sanitisiertem `content`, `Item.Tags` und der Basis-Schriftgröße das vollständige HTML-Dokument (Doctype, `<head>` mit Inline-CSS inkl. `.tags`-Regel, `<body>`) zusammensetzt; `RebuildHtml` delegiert an sie | `ArticleDetailViewModel` liegt im MAUI-App-Projekt `src/Reporter` (TFM `net10.0-windows10.0.19041.0`, `UseMaui=true`) und kann von `src/Reporter.Tests` (`net10.0`, referenziert nur `Reporter.Core` + `Reporter.Data`) nicht referenziert werden; eine Verschiebung des ViewModels nach `Reporter.Core` scheidet aus, weil es MAUI-APIs nutzt (`Browser`, `Share`, `Shell`). Die Extraktion der reinen HTML-Aufbau-Logik folgt dem etablierten Muster (`ArticleHtmlSanitizer`, `ReadingTimeEstimator` — beides `public static`-Hilfsklassen in `Reporter.Core.Services`, getestet in `Reporter.Tests`) und liefert echten Unit-Testnachweis für Anhängen, HTML-Encoding und Weglassen ohne Tags. |
| Suchsemantik | Teilstring-`Contains` über der gespeicherten Tag-Zeichenkette | Anwender-Entscheidung: konsistent zur bestehenden Titelsuche (`i.Title.Contains(term)`); „bahn" trifft auch „Stadtbahnen". |
| Stichwort-Filter (`IKeywordFilter.MatchesAny`) | Unverändert — Tags sind keine Filtergrundlage | Anwender-Entscheidung: nicht gefordert; die Signatur von `IKeywordFilter`/`IKeywordMatcher` bleibt stabil. |

## Programmabläufe

### Feed-Sync mit Tag-Extraktion und Tag-Backfill

1. `RunSyncAsync` lädt den `SyndicationFeed` wie bisher über `XmlReader` → `PrepareFeedReader` → `SyndicationFeed.Load`; unbekannte Erweiterungselemente landen dabei automatisch in `SyndicationItem.ElementExtensions`, standardisierte Kategorien in `SyndicationItem.Categories` (Parse-Pfad unverändert).
2. `CollectNewItems` iteriert `context.FeedItems`. Pro `SyndicationItem` sammelt eine neue private Hilfsmethode `ExtractTags` Begriffe aus zwei Quellen:
   - **Erweiterungselemente:** alle `ElementExtensions` mit `OuterName == "tags"` (Abgleich nur auf den lokalen Namen, `OuterNamespace` beliebig), Elementtext per `GetObject<System.Xml.Linq.XElement>()` → `Value`, Split an `,`, Trimmen jedes Begriffs, Verwerfen leerer Einträge (Ausleseschema wie in `ItemImageService.ResolveMediaRssUrl`/`ResolveItunesImageUrl`).
   - **Kategorien:** alle `SyndicationCategory` in `item.Categories` → `category.Name`. Konkreter Zugriffspfad: `Atom10ItemFormatter` legt den Wert des `term`-Attributs in `SyndicationCategory.Name` ab, `Rss20ItemFormatter` den Elementinhalt von RSS-`<category>` (das `domain`-Attribut landet in `Scheme` und wird ignoriert). Beide Formatter werden von `SyndicationFeed.Load` intern verwendet; der `Atom03NormalizingXmlReader` bildet den Atom-0.3-Namespace auf Atom 1.0 ab, sodass Atom-0.3-Feeds denselben `Categories`-Pfad durchlaufen. `Name` kann `null` sein — ebenfalls trimmen und leere Einträge verwerfen, kein Komma-Split.
   - **Zusammenführen:** `tags`-Elemente zuerst, dann `Categories`, jeweils in Dokument-/Collection-Reihenfolge; Dubletten per `StringComparer.Ordinal` entfernen (erstes Vorkommen gewinnt). Ergebnis: `IReadOnlyList<string>` (leer, wenn keine Quelle Begriffe liefert). Ein `tags`-Element bzw. `<category>` auf Feed-Ebene liegt in `SyndicationFeed.ElementExtensions`/`SyndicationFeed.Categories` und wird nicht gelesen.
3. Für **neue** Items (`knownKeys.Add` erfolgreich, Keyword-Filter passiert) wird `Tags` beim Anlegen des `Item` befüllt.
4. Für **bekannte** Items (Dedup-Treffer) wird — analog `backfillCandidates`/`imageBackfillCandidates` — eine `tagBackfillCandidates`-Map aus `context.ExistingItems` gebildet (Einträge ohne gespeicherte Tags, gruppiert per `GuidOrHash`). Liefert `ExtractTags` Begriffe, entsteht ein `TagBackfill`-Eintrag (ItemId → Tags) im `CollectResult`.
5. `RunSyncAsync` ruft nach `AddRangeAsync` zusätzlich `_itemRepository.UpdateTagsAsync(collected.TagBackfill)` auf, sofern Einträge vorhanden sind. Content-Backfill (`SetRangeAsync`), Image-Downloads, Health-Update und Benachrichtigung bleiben unverändert.

Beteiligte Klassen/Komponenten: `FeedSyncService`, `CollectContext`, `CollectResult`, `Item`, `IItemRepository`, `ItemRepository`, `SyndicationItem.ElementExtensions`, `SyndicationItem.Categories`/`SyndicationCategory`, `Atom03NormalizingXmlReader` (unverändert).

### Tag-gefilterte Artikelsuche

1. `FeedDetailViewModel.SearchText` → `DebounceRestartList` → `RestartListDebouncedAsync` → `ResetAndLoadFirstPageAsync`/`LoadPageCoreAsync` → `_itemRepository.GetByFeedAsync(_feedId, _currentPage, PageSize, SearchTermOrNull)` bleibt vollständig unverändert.
2. In `ItemRepository.GetByFeedAsync(feedId, page, pageSize, searchTerm)` wird die `Where`-Bedingung von `i.Title.Contains(term)` auf `i.Title.Contains(term) || (i.Tags != null && i.Tags.Contains(term))` erweitert (Teilstring-Match, konsistent zum Titel). Sortierung, Paging und die `ItemListRow`-Projektion bleiben unverändert — Tags werden nicht in die Listenprojektion aufgenommen, da die Suche DB-seitig vor der Projektion filtert.

Beteiligte Klassen/Komponenten: `ItemRepository`, `IItemRepository`, `FeedDetailViewModel`.

### Tag-Anzeige in der Artikeldetailansicht

1. `LoadAsync` lädt das `Item` über `_itemRepository.GetByIdAsync` — `MapToModel` transportiert `Tags` mit.
2. `RebuildHtml` ruft weiterhin `ArticleHtmlSanitizer.Sanitize(...)` → `content` und bricht bei leerem `content` wie bisher ab (`HtmlSource = ""`). Die Dokument-Zusammensetzung (Doctype, `<head>` mit Inline-CSS, `<body>{content}…</body>`) wird an die neue statische Hilfsklasse `ArticleHtmlBuilder.Build(content, tags, baseFontSize)` in `Reporter.Core.Services` delegiert — die bisherige Inline-Template-Logik zieht dorthin um.
3. `ArticleHtmlBuilder.Build` hängt bei nicht-leeren `tags` an `content` einen Block `<p class='tags'>{begriffe}</p>` an (innerhalb von `<body>`, also am Ende nach dem Artikeltext); die Einzelbegriffe werden per `WebUtility.HtmlEncode` encodiert und mit `, ` verbunden (Injection-Schutz, da der Block nach der Sanitization eingefügt wird).
4. Das Inline-CSS erhält eine `.tags`-Regel mit abgeschwächter Farbe, die in beiden Schemes wirkt (`color-scheme: light dark` und `@media (prefers-color-scheme: dark)` sind bereits vorhanden; z. B. reduzierte `opacity` und/oder sekundäre Farbwerte je Scheme).
5. `Item.Tags` leer → kein `<p class='tags'>`-Element, keine sichtbare Ausgabe. Leeres `content` (kein Artikeltext): das Verhalten bleibt wie bisher (`HtmlSource = ""`) — ein Artikel ohne Content zeigt keine Tags, weil der Block an `content` hängt (Early-Return verbleibt in `RebuildHtml`).
6. `RebuildHtml` läuft auch nach `ToggleFontSizeAsync`, `OnConnectivityChanged` und `CreateItemCopy`-Pfaden (`MarkReadDelayedAsync`, `ToggleMarkReadAsync`, `ToggleSavedForLaterAsync`); `CreateItemCopy` reicht `Tags` durch, damit die Zeile erhalten bleibt.

Beteiligte Klassen/Komponenten: `ArticleDetailViewModel`, `ArticleHtmlBuilder` (neu), `ArticleHtmlSanitizer`, `Item`, `ArticleDetailPage` (unverändert, `WebView` zeigt `HtmlSource`).

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `AddItemTags` | EF-Core-Migration (`src/Reporter.Data/Migrations/`, generiert) | Fügt die Spalte `items.tags` (`TEXT`, nullable) hinzu; aktualisiert `ReporterDbContextModelSnapshot`. |
| `ArticleHtmlBuilder` | `public static`-Hilfsklasse (`src/Reporter.Core/Services/`) | Setzt aus sanitisiertem `content`, `tags` und der Basis-Schriftgröße das vollständige WebView-HTML-Dokument zusammen (Inline-CSS inkl. `.tags`-Regel; hängt bei nicht-leeren Tags den HTML-encodierten `<p class='tags'>`-Block nach dem Content an). Muster: `ArticleHtmlSanitizer`/`ReadingTimeEstimator` — reine Logik in `Reporter.Core`, damit aus `Reporter.Tests` (`net10.0`) testbar. |
| `ArticleHtmlBuilderTests` | Testklasse (`src/Reporter.Tests/`) | Prüft den Tag-Block im erzeugten Dokument (Anhängen nach dem Content, HTML-Encoding/Injection-Schutz, Weglassen ohne Tags). |
| `ArticleTagsTests` | E2E-Testklasse (`src/Reporter.E2ETests/`) | Nachweis der Tag-Zeile in der Artikeldetailansicht und der Tag-Persistenz in der App-Datenbank. |
| `tag-feed.xml` | E2E-Fixture-Datei (`src/Reporter.E2ETests/Fixtures/`) | RSS-2.0-Feed mit `xmlns:app`-Deklaration, `<app:tags>` und `<category>` pro `<item>` für den `StubFeedServer`. |

## Änderungen an bestehenden Klassen

### `Item` (`src/Reporter.Data/Entities/Item.cs`, EF-Core-Entity)

- **Neue Eigenschaften:** `Tags` (`string?`) — kommaseparierte Schlagworte, Spalte `tags`.

### `Item` (`src/Reporter.Core/Models/Item.cs`, Domain-Modell)

- **Neue Eigenschaften:** `Tags` (`IReadOnlyList<string>`, `init`, Standard `[]`) — normalisierte Schlagwortliste; leer, wenn der Artikel keine Tags hat.

### `ReporterDbContext` (`src/Reporter.Data/ReporterDbContext.cs`)

- **Geänderte Methoden:** `ConfigureItem` — Mapping `entity.Property(e => e.Tags).HasColumnName("tags")` (nullable, keine Längenbegrenzung analog `Message`-Spalten bzw. optional begrenzt).

### `ItemRepository` (`src/Reporter.Data/Repositories/ItemRepository.cs`)

- **Neue Methoden:**
  - `UpdateTagsAsync(IReadOnlyDictionary<Guid, IReadOnlyList<string>> tagsByItemId)` — schreibt für jede ItemId die serialisierte Tag-Zeichenkette in `items.tags` (nur Spalten-Update, kein Content-Store-Zugriff); Implementierung über `IDbContextFactory<ReporterDbContext>` (z. B. `ExecuteUpdateAsync` je Eintrag).
  - Private Hilfen zur Serialisierung/Deserialisierung (`string.Join(", ", tags)` bzw. `Split(',')` mit `TrimEntries`/`RemoveEmptyEntries`), gemeinsam von `MapToEntity`, `MapToModel` und `UpdateTagsAsync`/`UpdateAsync` genutzt.
- **Geänderte Methoden:**
  - `MapToEntity` — serialisiert `model.Tags` in `entity.Tags` (`null` bei leerer Liste).
  - `MapToModel` — deserialisiert `entity.Tags` in `model.Tags` (leere Liste bei `null`).
  - `UpdateAsync` — setzt zusätzlich `entity.Tags` aus dem serialisierten `item.Tags`, damit Updates die Spalte nicht verlieren.
  - `GetByFeedAsync(Guid, int, int, string?)` — `Where`-Bedingung um `i.Tags != null && i.Tags.Contains(term)` erweitern.

### `IItemRepository` (`src/Reporter.Core/Interfaces/IItemRepository.cs`)

- **Neue Methoden:** `UpdateTagsAsync(IReadOnlyDictionary<Guid, IReadOnlyList<string>> tagsByItemId)` — Batch-Update der Schlagworte für den Sync-Backfill.
- **Geänderte Methoden:** `GetByFeedAsync(feedId, page, pageSize, searchTerm)` — Signatur unverändert; XML-Doku des `searchTerm`-Parameters auf „Titel oder Schlagworte" präzisieren.

### `FeedSyncService` (`src/Reporter.Core/Services/FeedSyncService.cs`)

- **Neue Methoden:** `ExtractTags(SyndicationItem)` (`private static`) — liest alle `ElementExtensions` mit `OuterName == "tags"` (beliebiger Namespace) plus `item.Categories` (`SyndicationCategory.Name`), normalisiert (Split `,`/Trim/Leer-Einträge verwerfen) und dedupliziert per `StringComparer.Ordinal`; liefert `IReadOnlyList<string>`.
- **Geänderte Methoden:**
  - `CollectNewItems` — neues `Item.Tags` beim Anlegen befüllen; `tagBackfillCandidates`-Map aus `context.ExistingItems` ohne Tags aufbauen und bei Dedup-Treffern mit Feed-Tags `TagBackfill`-Einträge sammeln.
  - `RunSyncAsync` — `UpdateTagsAsync` für `collected.TagBackfill` aufrufen (nur bei Einträgen).
- **Geänderte Records:** `CollectResult` — neues Feld `TagBackfill` (`List<KeyValuePair<Guid, IReadOnlyList<string>>>` o. ä.).

### `ArticleDetailViewModel` (`src/Reporter/ViewModels/ArticleDetailViewModel.cs`)

- **Geänderte Methoden:**
  - `RebuildHtml` — delegiert die Dokument-Zusammensetzung an `ArticleHtmlBuilder.Build(content, Item.Tags, baseSize)`; die bisherige Inline-HTML-Template-Logik inkl. CSS zieht in `ArticleHtmlBuilder` um, dort kommen die `.tags`-CSS-Regel (abgeschwächte Farbe für Light/Dark) und der HTML-encodierte `<p class='tags'>`-Block hinzu. `Sanitize`-Aufruf und der Early-Return bei leerem `content` bleiben im ViewModel.
  - `CreateItemCopy` — `Tags = Item.Tags` durchreichen.

### `IItemRepository`-Test-Doubles (`src/Reporter.Tests/DelegatingItemRepository.cs`)

- **Neue Methoden:** `UpdateTagsAsync` — `virtual`-Delegat an `_inner` (Pflicht wegen Interface-Erweiterung).

### `TestFeedXml` (`src/Reporter.Tests/TestFeedXml.cs`)

- **Neue Methoden:**
  - `AtomWithTags(...)` — Atom-1.0-Builder mit `xmlns:app="http://www.wdr.de/rss/1.0/modules/app/1.0/"`, optionalem `tags`-Element (Text und Namespace parametrisierbar für den beliebiger-Namespace-Fall) und optionaler `<category term="…"/>`-Liste pro `<entry>` sowie optional `tags`/`category` auf Feed-Ebene.
  - `RssWithTags(...)` — RSS-2.0-Builder mit optionaler `<category>`-Liste und optionalem namensraum-behaftetem `tags`-Element pro `<item>` (deckt den RSS-`Categories`-Pfad ab).
- Signaturen der bestehenden Builder (`Rss`, `RssWithEnclosure`, `Atom`, `Atom03`) bleiben unverändert.

### `FeedDbAssertions` (`src/Reporter.E2ETests/FeedDbAssertions.cs`)

- **Neue Methoden:** `ItemTagsContainAsync(databasePath, title, tagFragment)` — pollt die `items.tags`-Spalte der Zeile mit dem gegebenen Titel (Bestehende `PollUntilExistsAsync`-Struktur mit eigener COUNT-Query analog `FeedKeywordExistsCoreAsync`).

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `AddItemTags` | `items.tags` (`TEXT`, nullable) | `migrationBuilder.AddColumn<string>("tags", "items", type: "TEXT", nullable: true)`; `Down` entfernt die Spalte. Scaffolding via `dotnet ef migrations add AddItemTags --project src/Reporter.Data --startup-project src/Reporter.Data --context ReporterDbContext` (Design-Time-Factory `ReporterDbContextFactory` ist vorhanden); `ReporterDbContextModelSnapshot` wird automatisch aktualisiert und gegen `ConfigureItem` gegengeprüft. Zur Laufzeit wendet `App.MigrateDatabaseAsync` die Migration via `Database.MigrateAsync()` an. |

## Validierungsregeln

Keine. Die Tag-Normalisierung (Splitten an `,`, Trimmen, Verwerfen leerer Einträge, Dedup) ist Parse-Logik in `FeedSyncService.ExtractTags`, keine Eingabevalidierung mit Fehlerfall — es gibt keine Benutzereingabe in diesem Feature.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **`DelegatingItemRepository` und weitere `IItemRepository`-Implementierungen:** Die Interface-Erweiterung um `UpdateTagsAsync` bricht den Build, bis alle Implementierungen (`ItemRepository`, `DelegatingItemRepository`) den Member besitzen. Beide liegen im Repo und werden im selben Schritt angepasst.
- **`ItemRepository.UpdateAsync`:** Kopiert Entity-Felder einzeln; ohne `entity.Tags`-Zuweisung gingen Tags bei Updates verloren — muss mitgeändert werden.
- **`ArticleDetailViewModel.CreateItemCopy`:** Wird `Tags` nicht durchgereicht, verschwindet die Tag-Zeile nach `MarkReadDelayedAsync`/`ToggleMarkReadAsync`/`ToggleSavedForLaterAsync` beim nächsten `RebuildHtml` — muss mitgeändert werden.
- **`ArticleHtmlBuilder`-Umzug des HTML-Templates:** `RebuildHtml` gibt bisher das komplette Dokument als Inline-Interpolated-String aus; der Umzug in `ArticleHtmlBuilder.Build` darf das erzeugte Markup (Doctype, `<head>`-Metas, CSP, bestehende CSS-Regeln, `<body>{content}</body>`) nicht verändern — die Unit-Tests und der E2E-Sichtbarkeitstest sichern die Gleichwertigkeit; nur die `.tags`-Regel und der optionale Tag-Block kommen hinzu.
- **Injection-Risiko am Tag-Block:** Der Block wird **nach** `ArticleHtmlSanitizer.Sanitize` angehängt und umgeht damit die Sanitization; `WebUtility.HtmlEncode` auf jeden Begriff ist zwingend (durch Unit-Test abgesichert). `SyndicationCategory.Name` aus `<category>`-Elementen ist ebenfalls ungeprüfter Feed-Content und unterliegt demselben Encoding.
- **Fehltreffer durch lokalem-Namen-Abgleich:** Der bewusst lockere `tags`-Abgleich (beliebiger Namespace) kann `tags`-Elemente fremder Erweiterungen aufnehmen. Der Inhalt ist unabhängig vom Namespace immer eine kommaseparierte Begriffsliste — akzeptiert, da Fehltreffer höchstens zusätzliche Schlagworte erzeugen und ebenfalls dem HTML-Encoding unterliegen.
- **Namensraum-behaftete `category`-Erweiterungen:** `category`-Elemente in fremden Namespaces (z. B. `itunes:category`) landen in `ElementExtensions`, nicht in `Categories`, und werden nicht gelesen — nur standardisierte Atom-/RSS-Kategorien sind gemeint. Namensgleichheit `tags` vs. `category` in Extensions ist kein Konflikt, da nur `OuterName == "tags"` abgeglichen wird.
- **Artikel ohne Content:** Ist `content` leer, bleibt `HtmlSource` leer — Tags werden dann nicht angezeigt (folgt aus dem Ansatz „Block hängt am `content`"); für den WDR-Fall mit `content`-Element unkritisch, wird als bewusste Einschränkung dokumentiert und in `docs/help/anwendung/artikeldetailansicht.md` erwähnt.
- **Backfill-Menge:** `tagBackfillCandidates` iteriert nur `ExistingItems` ohne Tags; Schreibzugriffe erfolgen gebündelt pro Sync — kein nennenswerter Performance-Effekt.
- **E2E-Suite läuft nicht in dieser Umgebung/CI:** Die geplanten FlaUI-Tests (`FeedDetail_Search_FiltersByTag`, `ArticleTagsTests`) können hier nicht ausgeführt werden; sie werden als Pflicht-Artefakte geplant, müssen aber vom Anwender lokal via `scripts/Run-E2ETests.ps1` verifiziert werden (bekannte Testlücke, analog zum Ausgangszustand).
- **`ItemListItem`/Listenprojektion:** Bewusst unverändert — die Artikelsuche filtert vor der Projektion DB-seitig; Listen zeigen weiterhin keine Tags.

## Umsetzungsreihenfolge

1. **Datenmodell erweitern**
   - Voraussetzungen: Keine.
   - Beschreibung: `Tags` (`string?`) in `Reporter.Data.Entities.Item`, `Tags` (`IReadOnlyList<string>`, `init`, Default `[]`) in `Reporter.Core.Models.Item` ergänzen; `ReporterDbContext.ConfigureItem` um das `tags`-Spalten-Mapping erweitern.

2. **EF-Migration `AddItemTags` scaffolden**
   - Voraussetzungen: Schritt 1 (Entity-Feld + Mapping); Design-Time-Factory `ReporterDbContextFactory` (vorhanden); **`dotnet-ef`-CLI installiert** — im Repo ist kein alternatives Migrations-Vorgehen etabliert (kein Skript in `scripts/`, kein `dotnet-tools.json`, CI erzeugt keine Migrationen); alle bisherigen Migrationen wurden per `dotnet ef` scaffoldet. Falls nicht vorhanden: `dotnet tool install --global dotnet-ef` (bzw. `dotnet tool update --global dotnet-ef`; Version 10.x passend zu `Microsoft.EntityFrameworkCore.Sqlite` 10.0.x).
   - Beschreibung: `dotnet ef migrations add AddItemTags --project src/Reporter.Data --startup-project src/Reporter.Data --context ReporterDbContext` ausführen; generierte `Up`/`Down` und den aktualisierten `ReporterDbContextModelSnapshot` gegen `ConfigureItem` prüfen.

3. **Repository- und Interface-Anpassungen**
   - Voraussetzungen: Schritt 1 (Domain-/Entity-Feld vorhanden).
   - Beschreibung: In `ItemRepository` `MapToEntity`/`MapToModel`/`UpdateAsync` um `Tags` erweitern, Serialisierungs-/Deserialisierungshilfen anlegen, `GetByFeedAsync`-Suchfilter erweitern und `UpdateTagsAsync` implementieren. In `IItemRepository` `UpdateTagsAsync` deklarieren und die `searchTerm`-XML-Doku präzisieren. `DelegatingItemRepository` um den neuen Member ergänzen.

4. **Feed-Sync: Tag-Extraktion (`tags`-Extensions + `Categories`) und Tag-Backfill**
   - Voraussetzungen: Schritt 3 (`UpdateTagsAsync` im Interface und Repository).
   - Beschreibung: `FeedSyncService.ExtractTags` implementieren (lokaler Name `tags` in beliebigem Namespace aus `ElementExtensions` + `SyndicationItem.Categories` via `SyndicationCategory.Name`, Normalisierung und Ordinal-Dedup); `CollectNewItems` um Tag-Befüllung neuer Items und `tagBackfillCandidates`/`TagBackfill` erweitern; `CollectResult` um das Backfill-Feld ergänzen; `RunSyncAsync` ruft `UpdateTagsAsync` auf.

5. **Detailansicht: `ArticleHtmlBuilder` anlegen und Tag-Block im WebView-HTML**
   - Voraussetzungen: Schritt 1 und 3 (`Item.Tags` wird über `GetByIdAsync` transportiert).
   - Beschreibung: Neue `public static`-Klasse `ArticleHtmlBuilder` in `src/Reporter.Core/Services/` mit `Build(content, tags, baseFontSize)` anlegen und die bisherige Inline-HTML-Template-Logik aus `ArticleDetailViewModel.RebuildHtml` dorthin umziehen (Markup unverändert übernehmen); `.tags`-CSS-Regel (abgeschwächt, Light/Dark) und den HTML-encodierten `<p class='tags'>`-Block ergänzen. `RebuildHtml` ruft `Build` nach `Sanitize` auf; `Sanitize`-Aufruf und Early-Return bei leerem `content` bleiben im ViewModel. `CreateItemCopy` reicht `Tags` durch. `ArticleDetailPage.xaml` bleibt unverändert.

6. **Unit-/Integrationstests**
   - Voraussetzungen: Schritte 3–5 (Implementierung komplett); Testinfrastruktur (`TestDbContextFactory`, `FakeItemContentStore`, `FakeHttpMessageHandler`, `FakeNetworkStatusService`, `TestDataSeeder`, `TestWaitHelper`) ist vorhanden.
   - Beschreibung: `TestFeedXml.AtomWithTags` und `TestFeedXml.RssWithTags` anlegen; neue Tests in `FeedSyncServiceTests` (Extraktion aus `tags`-Elementen beliebiger Namespaces, `category`-Übernahme für Atom und RSS, Dedup, Backfill), `ItemRepositoryTests`, `FeedDetailViewModelTests`, `ReporterDbContextTests_Schema`/`_Persistence` sowie die neue Klasse `ArticleHtmlBuilderTests` (gegen `Reporter.Core`, TFM-kompatibel) schreiben.

7. **E2E-Artefakte**
   - Voraussetzungen: Schritte 3–5 (Feature lauffähig); E2E-Infrastruktur (`StubFeedServer`, `E2EPageHelpers`, `UiRetry`, `FeedDbAssertions`) ist vorhanden.
   - Beschreibung: Fixture `tag-feed.xml` anlegen (`<app:tags>` und `<category>` pro `<item>`), `FeedDbAssertions.ItemTagsContainAsync` ergänzen, `FeedDetailTests.FeedDetail_Search_FiltersByTag` und die neue Klasse `ArticleTagsTests` schreiben. Ausführung erfolgt außerhalb dieser Umgebung via `scripts/Run-E2ETests.ps1`.

8. **Dokumentation und manuelle UI-Verifikation**
   - Voraussetzungen: Schritt 5 (sichtbare UI-Änderung); Schritt 7 (E2E-Nachweis auf 390 × 844 pt geplant).
   - Beschreibung: `docs/help/anwendung/artikeldetailansicht.md` (Tag-Zeile **inkl. der dokumentierten Einschränkung, dass Artikel ohne `content` keine Tags anzeigen**), `feeddetailansicht.md` und `datenmodell.md` aktualisieren. Mobile-UI-Design-Review nach `AGENTS.md`: Tag-Zeile gegen `design-draft/stitch_local_rss_feed_reader/artikel_lesemodus/screen.png` und `artikel_lesemodus_dark_mode/screen.png` vergleichen, Layout im Windows-Handyfenster 390 × 844 pt in Light und Dark prüfen (Screenshot), Ergebnis in `docs/help/anwendung/mobile-ui-design.md` dokumentieren.

9. **Statische Checks**
   - Voraussetzungen: Schritte 1–8.
   - Beschreibung: `.\scripts\Run-StaticChecks.ps1` ausführen; Format, Security und Static-Analysis-Build müssen ohne Befund durchlaufen (Exit-Code 0).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `AtomWithTags(...)` | `TestFeedXml` (Hilfsmethode) | Baut Atom-1.0-Feeds mit `xmlns:app`-Deklaration, optionalem `tags`-Element (Text + Namespace parametrisierbar) und `<category term="…"/>` pro `<entry>` sowie optional auf Feed-Ebene. |
| `RssWithTags(...)` | `TestFeedXml` (Hilfsmethode) | Baut RSS-2.0-Feeds mit optionaler `<category>`-Liste und optionalem namensraum-behaftetem `tags`-Element pro `<item>`. |
| `SyncFeedAsync_AppTags_StoredOnItem` | `FeedSyncServiceTests` | Item aus Atom-`<entry>` mit `<app:tags>SWB, Aufsichtsrat, Stadtbahnen</app:tags>` erhält die drei normalisierten Tags persistiert. |
| `SyncFeedAsync_NoTags_ItemHasNoTags` | `FeedSyncServiceTests` | Item ohne `tags`-Element und ohne `category` erhält keine Tags. |
| `SyncFeedAsync_Tags_TrimsAndDropsEmptyEntries` | `FeedSyncServiceTests` | `"SWB, , Stadtbahnen ,"` → `["SWB", "Stadtbahnen"]`; Whitespace-/Leereinträge werden verworfen. |
| `SyncFeedAsync_TagsInOtherNamespace_Stored` | `FeedSyncServiceTests` | Ein `tags`-Element in einem fremden Namespace (lokaler Name `tags`) wird ebenfalls übernommen (Abgleich nur auf `OuterName`). |
| `SyncFeedAsync_AtomCategory_StoredAsTag` | `FeedSyncServiceTests` | `<category term="Politik"/>` an einer Atom-`<entry>` landet als Tag `Politik` am Item (Zugriffspfad `SyndicationItem.Categories` → `Name`). |
| `SyncFeedAsync_RssCategory_StoredAsTag` | `FeedSyncServiceTests` | `<category>Politik</category>` an einem RSS-`<item>` landet als Tag `Politik` am Item (Elementinhalt → `Name`). |
| `SyncFeedAsync_TagsAndCategory_DuplicatesRemoved` | `FeedSyncServiceTests` | Ein Begriff, der sowohl in `<app:tags>` als auch als `<category>` vorkommt, wird nur einmal gespeichert; Reihenfolge des ersten Vorkommens bleibt erhalten. |
| `SyncFeedAsync_FeedLevelTags_NotStoredOnItem` | `FeedSyncServiceTests` | `tags`-Element/`category` auf Feed-Ebene landet nicht am Item. |
| `SyncFeedAsync_ExistingItemWithoutTags_BackfillsTags` | `FeedSyncServiceTests` | Zweiter Sync desselben Items mit Tags schreibt sie auf den bereits gespeicherten Artikel (Backfill). |
| `SyncFeedAsync_ExistingItemWithTags_KeepsTags` | `FeedSyncServiceTests` | Bestehende Tags werden beim Backfill nicht überschrieben. |
| `GetByFeedAsync_Paged_SearchTermMatchesTags` | `ItemRepositoryTests` | Suchbegriff, der nur in `tags` vorkommt, findet den Artikel; Artikel ohne Treffer bleiben ausgeschlossen. |
| `GetByFeedAsync_Paged_SearchTermMatchesTitleOrTags` | `ItemRepositoryTests` | Titel-Treffer funktionieren weiterhin neben Tag-Treffern. |
| `AddAsync_ThenGetByIdAsync_RoundtripsTags` | `ItemRepositoryTests` | `Tags` überstehen den Insert-/Lese-Roundtrip (Serialisierung korrekt). |
| `UpdateAsync_PersistsTags` | `ItemRepositoryTests` | `UpdateAsync` schreibt geänderte Tags und überschreibt sie nicht mit `null`. |
| `UpdateTagsAsync_UpdatesOnlyTags` | `ItemRepositoryTests` | Batch-Update ändert ausschließlich `tags`; Titel/Content bleiben unverändert. |
| `SearchText_TagMatch_FiltersItems` | `FeedDetailViewModelTests` | `SearchText` mit Tag-Begriff liefert den Artikel über die echte Repository-Kette. |
| `Build_WithTags_AppendsTagBlock` | `ArticleHtmlBuilderTests` (neu) | Das erzeugte Dokument enthält die `.tags`-CSS-Regel und einen `<p class='tags'>…`-Block, der **nach** dem Content innerhalb von `<body>` (vor `</body>`) steht — die Begriffe erscheinen kommasepariert. |
| `Build_Tags_AreHtmlEncoded` | `ArticleHtmlBuilderTests` (neu) | Ein Tag mit HTML-Payload (z. B. `<script>`/`onerror`) erscheint encodiert, nicht als Element/Attribut — Injection-Schutz für den nach `Sanitize` eingefügten Block. |
| `Build_WithoutTags_NoTagBlock` | `ArticleHtmlBuilderTests` (neu) | Leere `tags`-Liste: Das Dokument enthält kein `<p class='tags'>`-Element im Body. |
| `Item_TagsColumn_MappedToExpectedColumn` | `ReporterDbContextTests_Schema` | `context.Model.FindEntityType(typeof(Item))` mappt `Tags` auf die Spalte `tags` (Muster: `Feed_LastMessage_MappedToExpectedColumns`). |
| `Item_PersistRoundtrip_Tags` | `ReporterDbContextTests_Persistence` | `tags`-Wert übersteht Speichern/Lesen auf Entity-Ebene. |
| `FeedDetail_Search_FiltersByTag` | `FeedDetailTests` (E2E) | Suche im `FeedDetailSearchBar` nach einem Tag-Begriff zeigt den getaggten Artikel und blendet den nicht passenden aus. |
| `ArticleDetail_Tags_ShownBelowArticleText` | `ArticleTagsTests` (E2E, neu) | In der geöffneten Artikeldetailansicht ist die Tag-Zeile im WebView-UIA-Teilbaum sichtbar. |
| `ArticleDetail_Tags_PersistedInDatabase` | `ArticleTagsTests` (E2E, neu) | `FeedDbAssertions.ItemTagsContainAsync` weist die `tags`-Spalte am synchronisierten Artikel nach. |
| `ItemTagsContainAsync` | `FeedDbAssertions` (E2E-Hilfsmethode) | Pollt `items.tags` für den Artikel mit gegebenem Titel. |
| `tag-feed.xml` | E2E-Fixture | RSS-Feed mit `xmlns:app`, `<app:tags>` und `<category>` an mindestens einem `<item>`; ein weiteres Item ohne/anders getaggt für den Such-Filtertest. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `DelegatingItemRepository` | Neuer Interface-Member `UpdateTagsAsync` muss ergänzt werden (Build-Bruch sonst); Basis für `CountingItemRepository` u. a. |
| `ItemRepositoryTests.GetByFeedAsync_Paged_FiltersBySearchTerm` | Bleibt fachlich gültig (Titel-Treffer), wird ggf. um einen Tag-Treffer-Fall ergänzt — keine zwingende Änderung. |
| Bestehende `Item`-Konstruktionen in Tests | `Tags` ist optional (`init`, Default leer) — bestehende `Item`-Objektinitialisierer kompilieren unverändert. |
| Bestehende `ArticleDetailPage`-E2E-Tests (z. B. `ArticleImageTests`, `ArticleLinkTests`) | Bleiben fachlich gültig — `ArticleHtmlBuilder.Build` muss das bisherige Markup unverändert liefern; kein Test bricht, wenn die Umstellung verlustfrei ist. |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Artikel eines Feeds mit `tags`-Element und `<category>` wird synchronisiert und die Tag-Zeile erscheint am Ende der Artikeldetailansicht | `src/Reporter.E2ETests/ArticleTagsTests.cs` — `ArticleDetail_Tags_ShownBelowArticleText` | Tag-Anzeige in `ArticleDetailPage` | Der Fluss Feed abrufen → Sync → Detailansicht öffnen → gerendertes WebView-HTML prüfen lässt sich nur End-to-End nachweisen (WebView2-UIA-Teilbaum). |
| Pflicht | Tag-Persistenz nach dem Sync | `src/Reporter.E2ETests/ArticleTagsTests.cs` — `ArticleDetail_Tags_PersistedInDatabase` | `items.tags` ist am Artikel befüllt | Belegt die DB-Migration und den Sync-Schreibpfad gegen die reale App-Datenbank. |
| Pflicht | Suche in der Feed-Detailansicht findet Artikel über einen Tag-Begriff | `src/Reporter.E2ETests/FeedDetailTests.cs` — `FeedDetail_Search_FiltersByTag` | Tag-Treffer in `FeedDetailViewModel.SearchText` | Der Benutzerfluss „Suchfeld tippen → Liste filtert" ist nur über die UI vollständig nachweisbar (analog `FeedDetail_Search_FiltersItems`). |

Hinweis: Die E2E-Suite (FlaUI/UIA3) läuft nicht in dieser Umgebung und nicht in der CI; die Tests werden als Pflicht-Artefakte erstellt und vom Anwender lokal via `scripts/Run-E2ETests.ps1` verifiziert.

Welche bestehenden E2E-Tests müssen angepasst werden?

Keine — `FeedDetail_Search_FiltersItems` bleibt unverändert gültig (Titel-Suche funktioniert weiterhin); `StubFeedServer` benötigt keine Änderung, da `tag-feed.xml` über den bestehenden Fixture-Mechanismus (`/feeds/{name}.xml` → `Fixtures/{name}.xml`) ausgeliefert wird.

## Offene Punkte

Keine.
