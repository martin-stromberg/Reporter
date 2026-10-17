<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

- [x] `Item.Tags` (`string?`) in `src/Reporter.Data/Entities/Item.cs:59` — vorhanden
- [x] `Item.Tags` (`IReadOnlyList<string>`, `init`, Default `[]`) in `src/Reporter.Core/Models/Item.cs:63` — vorhanden
- [x] `ReporterDbContext.ConfigureItem`: `entity.Property(e => e.Tags).HasColumnName("tags")` in `src/Reporter.Data/ReporterDbContext.cs:117` — vorhanden (nullable, keine Längenbegrenzung)
- [x] EF-Migration `AddItemTags` (`src/Reporter.Data/Migrations/20261017045445_AddItemTags.cs` + `.Designer.cs`) — `AddColumn<string>("tags", "items", TEXT, nullable)`, `Down` entfernt die Spalte; `ReporterDbContextModelSnapshot.cs:183-185` aktualisiert
- [x] `IItemRepository.UpdateTagsAsync(IReadOnlyDictionary<Guid, IReadOnlyList<string>>)` in `src/Reporter.Core/Interfaces/IItemRepository.cs:152` — deklariert
- [x] `IItemRepository.GetByFeedAsync`: `searchTerm`-XML-Doku auf „item title or its tags" präzisiert (`IItemRepository.cs:106`)
- [x] `ItemRepository.UpdateTagsAsync` in `src/Reporter.Data/Repositories/ItemRepository.cs:327-342` — `ExecuteUpdateAsync` je Eintrag, nur `tags`-Spalte, kein Content-Store-Zugriff, Early-Return bei leerem Dictionary
- [x] `ItemRepository.MapToEntity` — `Tags = SerializeTags(model.Tags)` (`ItemRepository.cs:529`)
- [x] `ItemRepository.MapToModel` — `Tags = DeserializeTags(entity.Tags)` (`ItemRepository.cs:512`)
- [x] `ItemRepository.UpdateAsync` — `entity.Tags = SerializeTags(item.Tags)` (`ItemRepository.cs:92`); verdeckte Kopierstelle geschlossen, Tags gehen bei Updates nicht verloren
- [x] `ItemRepository.GetByFeedAsync(feedId, page, pageSize, searchTerm)` — `Where` um `i.Tags != null && i.Tags.Contains(term)` erweitert (`ItemRepository.cs:260`); Projektion/Paging unverändert
- [x] Private Serialisierungs-/Deserialisierungshilfen `SerializeTags`/`DeserializeTags` (`ItemRepository.cs:536-546`) — `string.Join(", ", …)` bzw. `Split(',')` mit `TrimEntries | RemoveEmptyEntries`, leere Liste ↔ `null`; von `MapToEntity`, `MapToModel`, `UpdateAsync` und `UpdateTagsAsync` gemeinsam genutzt
- [x] `FeedSyncService.ExtractTags(SyndicationItem)` (`private static`) in `src/Reporter.Core/Services/FeedSyncService.cs:547-585` — `ElementExtensions` mit `OuterName == "tags"` (Ordinal, beliebiger `OuterNamespace`), `GetObject<XElement>()?.Value`, Split `,`, Trim, Leereinträge verworfen; plus `item.Categories` → `SyndicationCategory.Name` (trimmen, `null`/leer verwerfen, kein Komma-Split); Zusammenführen `tags`-Elemente zuerst, dann `Categories`, Dedup per `StringComparer.Ordinal` (erstes Vorkommen gewinnt); liefert `IReadOnlyList<string>`
- [x] `FeedSyncService.CollectNewItems` — `Tags = ExtractTags(feedItem)` beim Anlegen neuer Items (`FeedSyncService.cs:369`); `tagBackfillCandidates` aus `ExistingItems` mit `i.Tags.Count == 0`, gruppiert per `GuidOrHash` (`FeedSyncService.cs:311-314`); bei Dedup-Treffern `tagBackfill[tagItemId] = backfillTags` nur bei `Count > 0` (`FeedSyncService.cs:340-344`) — nur fehlende Werte, geänderte Tags werden nie überschrieben
- [x] `CollectResult`-Record um `TagBackfill` (`Dictionary<Guid, IReadOnlyList<string>>`) erweitert (`FeedSyncService.cs:644`)
- [x] `FeedSyncService.RunSyncAsync` — `UpdateTagsAsync(collected.TagBackfill)` nur bei `Count > 0` (`FeedSyncService.cs:234-237`), nach `AddRangeAsync`/`SetRangeAsync`
- [x] `ArticleHtmlBuilder` (`public static`, `src/Reporter.Core/Services/ArticleHtmlBuilder.cs`, Namespace `Reporter.Core.Services`) — `Build(content, tags, baseFontSize)` setzt das vollständige Dokument (Doctype, `<head>` mit CSP/`color-scheme`/Inline-CSS, `<body>`); `.tags`-Regel in beiden Schemes (Light `#64748b`, Dark `#94a3b8` in `@media (prefers-color-scheme: dark)`); `<p class='tags'>`-Block nur bei nicht-leeren Tags, Begriffe per `WebUtility.HtmlEncode` encodiert, mit `, ` verbunden, nach `content` innerhalb `<body>`
- [x] `ArticleDetailViewModel.RebuildHtml` — delegiert an `ArticleHtmlBuilder.Build(content, Item?.Tags ?? Array.Empty<string>(), baseSize)` (`ArticleDetailViewModel.cs:363`); `Sanitize`-Aufruf und Early-Return bei leerem `content` (`HtmlSource = ""`) verbleiben im ViewModel (`:355-360`); kein Inline-HTML-Template mehr im ViewModel
- [x] `ArticleDetailViewModel.CreateItemCopy` — `Tags = Item.Tags` durchgereicht (`ArticleDetailViewModel.cs:545`); `ArticleDetailPage.xaml` unverändert
- [x] `DelegatingItemRepository.UpdateTagsAsync` als `virtual`-Delegat (`src/Reporter.Tests/DelegatingItemRepository.cs:85-86`)
- [x] `TestFeedXml.AtomWithTags(...)` (`src/Reporter.Tests/TestFeedXml.cs:129-182`) — `xmlns:app` (WDR-Namespace), `tags`-Text + Namespace parametrisierbar, `<category term="…"/>`-Liste pro `<entry>`, optionale `feedTags`/`feedCategories` auf Feed-Ebene
- [x] `TestFeedXml.RssWithTags(...)` (`TestFeedXml.cs:194-234`) — `<category>`-Liste und namensraum-behaftetes `tags`-Element pro `<item>`; Signaturen der Bestands-Builder unverändert
- [x] `FeedSyncServiceTests_Tags` (`src/Reporter.Tests/FeedSyncServiceTests_Tags.cs`) — alle 10 geplanten Tests vorhanden: `SyncFeedAsync_AppTags_StoredOnItem`, `SyncFeedAsync_NoTags_ItemHasNoTags`, `SyncFeedAsync_Tags_TrimsAndDropsEmptyEntries`, `SyncFeedAsync_TagsInOtherNamespace_Stored`, `SyncFeedAsync_AtomCategory_StoredAsTag`, `SyncFeedAsync_RssCategory_StoredAsTag`, `SyncFeedAsync_TagsAndCategory_DuplicatesRemoved`, `SyncFeedAsync_FeedLevelTags_NotStoredOnItem`, `SyncFeedAsync_ExistingItemWithoutTags_BackfillsTags`, `SyncFeedAsync_ExistingItemWithTags_KeepsTags`
- [x] `ItemRepositoryTests` — alle 5 geplanten Tests vorhanden: `GetByFeedAsync_Paged_SearchTermMatchesTags` (:2037), `GetByFeedAsync_Paged_SearchTermMatchesTitleOrTags` (:2088), `AddAsync_ThenGetByIdAsync_RoundtripsTags` (:2139), `UpdateAsync_PersistsTags` (:2166), `UpdateTagsAsync_UpdatesOnlyTags` (:2203)
- [x] `FeedDetailViewModelTests.SearchText_TagMatch_FiltersItems` (`FeedDetailViewModelTests.cs:330`) — echte Repository-Kette
- [x] `ArticleHtmlBuilderTests` (`src/Reporter.Tests/ArticleHtmlBuilderTests.cs`) — `Build_WithTags_AppendsTagBlock`, `Build_Tags_AreHtmlEncoded`, `Build_WithoutTags_NoTagBlock`
- [x] `ReporterDbContextTests_Schema.Item_TagsColumn_MappedToExpectedColumn` (`:113`) und `ReporterDbContextTests_Persistence.Item_PersistRoundtrip_Tags` (`:214`)
- [x] E2E-Fixture `src/Reporter.E2ETests/Fixtures/tag-feed.xml` — RSS 2.0 mit `xmlns:app`, `<app:tags>` + `<category>` am getaggten `<item>`, zweites Item ohne Tags
- [x] `FeedDbAssertions.ItemTagsContainAsync(databasePath, title, tagFragment)` (`FeedDbAssertions.cs:132-166`) — pollend, eigene COUNT-Query analog `FeedKeywordExistsCoreAsync`
- [x] `FeedDetailTests.FeedDetail_Search_FiltersByTag` (`FeedDetailTests.cs:290-310`)
- [x] `ArticleTagsTests` (`src/Reporter.E2ETests/ArticleTagsTests.cs`) — `ArticleDetail_Tags_ShownBelowArticleText` (WebView-UIA-Teilbaum) und `ArticleDetail_Tags_PersistedInDatabase`
- [x] Dokumentation: `artikeldetailansicht.md` (Abschnitt „Schlagworte" inkl. Einschränkung „Artikel ohne `content` zeigen keine Schlagworte"), `feeddetailansicht.md` (Suche durchsucht Titel und Schlagworte), `datenmodell.md` (`Item.Tags`-Zeile + `AddItemTags`-Migrationsabsatz)
- [x] `mobile-ui-design.md`: Abschnitt „Artikel-Schlagworte (issue-128)" dokumentiert Design-Draft-Vergleich (beide `artikel_lesemodus`-Screens), Mobile-UI-Regeln und automatisierte Nachweise
- [x] `.\scripts\Run-StaticChecks.ps1` im Review ausgeführt: Format, Lizenzheader, Security, Static-Analysis-Build — alle ohne Befund, Exit-Code 0

## Offene Aufgaben

- [ ] `UI-Verifikation` (Tasks #32) — teilweise umgesetzt: Der dokumentierte Eintrag in `docs/help/anwendung/mobile-ui-design.md` existiert inkl. Design-Draft-Vergleich und Nachweis-Verweisen; die manuelle Laufzeit-Verifikation (Screenshot Light/Dark im Windows-Handyfenster 390 × 844 pt) steht jedoch aus und ist im Eintrag selbst als „Offen" markiert. Plan-konform bewusst auf die lokale Ausführung via `scripts/Run-E2ETests.ps1` verschoben (die FlaUI-Suite läuft nicht in dieser Umgebung/CI).

## Hinweise

- Die FlaUI-E2E-Artefakte (`ArticleTagsTests`, `FeedDetail_Search_FiltersByTag`, `tag-feed.xml`, `ItemTagsContainAsync`) sind vollständig erstellt, aber hier nicht ausführbar — Ausführung lokal via `scripts/Run-E2ETests.ps1` erforderlich (vom Plan ausdrücklich als bekannte Testlücke einkalkuliert).
- `UpdateTagsAsync` im Repository schreibt die übergebenen Werte ungeprüft; das „nur fehlende Werte"-Kriterium wird eine Ebene höher in `CollectNewItems` durch die Kandidatenfilterung `i.Tags.Count == 0` garantiert — entspricht dem Plan.
- `IKeywordFilter.MatchesAny` wurde plan- und entscheidungsgemäß nicht angefasst (Tags sind keine Filtergrundlage).
