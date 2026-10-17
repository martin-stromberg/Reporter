<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

- [x] `Item.Tags` (`string?`) in `src/Reporter.Data/Entities/Item.cs:59` — vorhanden
- [x] `Item.Tags` (`IReadOnlyList<string>`, `init`, Default `[]`) in `src/Reporter.Core/Models/Item.cs:63` — vorhanden
- [x] `ReporterDbContext.ConfigureItem`: `entity.Property(e => e.Tags).HasColumnName("tags")` in `src/Reporter.Data/ReporterDbContext.cs:117` — vorhanden
- [x] EF-Migration `AddItemTags` (`src/Reporter.Data/Migrations/20261017045445_AddItemTags.cs` + `.Designer.cs`) — vorhanden; `ReporterDbContextModelSnapshot` aktualisiert
- [x] `IItemRepository.UpdateTagsAsync(IReadOnlyDictionary<Guid, IReadOnlyList<string>>)` in `src/Reporter.Core/Interfaces/IItemRepository.cs:154` — deklariert; `searchTerm`-XML-Doku präzisiert (`IItemRepository.cs:106`)
- [x] `ItemRepository.UpdateTagsAsync` (`ItemRepository.cs:327-342`) — `ExecuteUpdateAsync` je Eintrag, nur `tags`-Spalte, kein Content-Store-Zugriff
- [x] `ItemRepository.MapToEntity`/`MapToModel`/`UpdateAsync` — `Tags`-Serialisierung/-Deserialisierung über gemeinsame Hilfen `SerializeTags`/`DeserializeTags` (`ItemRepository.cs:92/512/529/543/548`)
- [x] `ItemRepository.GetByFeedAsync(feedId, page, pageSize, searchTerm)` — `Where` um `i.Tags != null && i.Tags.Contains(term)` erweitert (`ItemRepository.cs:260`)
- [x] `FeedSyncService.ExtractTags(SyndicationItem)` (`private static`, `FeedSyncService.cs:547`) — `ElementExtensions` mit `OuterName == "tags"` (beliebiger Namespace) plus `item.Categories` → `Name`; Normalisierung und Ordinal-Dedup
- [x] `FeedSyncService.CollectNewItems` — `Tags = ExtractTags(feedItem)` beim Anlegen (`FeedSyncService.cs:369`); `tagBackfillCandidates` aus `ExistingItems` ohne Tags, `CollectResult.TagBackfill` (`FeedSyncService.cs:283/311/340-343/644`)
- [x] `FeedSyncService.RunSyncAsync` — `UpdateTagsAsync(collected.TagBackfill)` nur bei `Count > 0` (`FeedSyncService.cs:234-236`)
- [x] `ArticleHtmlBuilder` (`public static`, `src/Reporter.Core/Services/ArticleHtmlBuilder.cs`) — `Build(content, tags, baseFontSize)` setzt das vollständige Dokument; `.tags`-CSS-Regel für Light (`#64748b`) und Dark (`#94a3b8`); HTML-encodierter `<p class='tags'>`-Block nach dem Content (`ArticleHtmlBuilder.cs:25/31/55/62`)
- [x] `ArticleDetailViewModel.RebuildHtml` — delegiert an `ArticleHtmlBuilder.Build` (`ArticleDetailViewModel.cs:363`); `Sanitize`-Aufruf und Early-Return bei leerem `content` verbleiben im ViewModel
- [x] `ArticleDetailViewModel.CreateItemCopy` — `Tags = Item.Tags` durchgereicht (`ArticleDetailViewModel.cs:545`)
- [x] `DelegatingItemRepository.UpdateTagsAsync` als `virtual`-Delegat (`src/Reporter.Tests/DelegatingItemRepository.cs:85-86`)
- [x] `TestFeedXml.AtomWithTags(...)` (`src/Reporter.Tests/TestFeedXml.cs:129`) und `TestFeedXml.RssWithTags(...)` (`:194`)
- [x] `FeedSyncServiceTests_Tags` (`src/Reporter.Tests/FeedSyncServiceTests_Tags.cs`) — alle 10 geplanten Tests vorhanden
- [x] `ItemRepositoryTests` — alle geplanten Tests vorhanden (`GetByFeedAsync_Paged_SearchTermMatchesTags` :2037, `GetByFeedAsync_Paged_SearchTermMatchesTitleOrTags` :2088, `AddAsync_ThenGetByIdAsync_RoundtripsTags` :2139, `AddAsync_ThenGetByIdAsync_RoundtripsTagContainingComma` :2166, `UpdateAsync_PersistsTags` :2193, `UpdateTagsAsync_UpdatesOnlyTags` :2230)
- [x] `FeedDetailViewModelTests.SearchText_TagMatch_FiltersItems` (`FeedDetailViewModelTests.cs:330`)
- [x] `ArticleHtmlBuilderTests` (`src/Reporter.Tests/ArticleHtmlBuilderTests.cs`) — `Build_WithTags_AppendsTagBlock`, `Build_Tags_AreHtmlEncoded`, `Build_WithoutTags_NoTagBlock`
- [x] `ReporterDbContextTests_Schema.Item_TagsColumn_MappedToExpectedColumn` (`:113`) und `ReporterDbContextTests_Persistence.Item_PersistRoundtrip_Tags` (`:214`)
- [x] E2E-Fixture `src/Reporter.E2ETests/Fixtures/tag-feed.xml` — RSS 2.0 mit `xmlns:app`, `<app:tags>` + `<category>` am getaggten `<item>`, zweites Item ohne Tags
- [x] `FeedDbAssertions.ItemTagsContainAsync(databasePath, title, tagFragment)` (`FeedDbAssertions.cs:132`)
- [x] `FeedDetailTests.FeedDetail_Search_FiltersByTag` (`FeedDetailTests.cs:290`)
- [x] `ArticleTagsTests` (`src/Reporter.E2ETests/ArticleTagsTests.cs`) — `ArticleDetail_Tags_ShownBelowArticleText` (`:71`) und `ArticleDetail_Tags_PersistedInDatabase` (`:128`)
- [x] Dokumentation: `artikeldetailansicht.md` (Abschnitt „Schlagworte" inkl. Einschränkung „Artikel ohne Inhalt zeigen keine Schlagworte"), `feeddetailansicht.md` (Suche durchsucht Titel und Schlagworte), `datenmodell.md` (`Item.Tags`-Zeile + `AddItemTags`-Migrationsabsatz, Format `U+001F`-getrennt)
- [x] `mobile-ui-design.md`: Abschnitt „Artikel-Schlagworte (issue-128)" (`mobile-ui-design.md:212-216`) — Design-Draft-Vergleich (beide `artikel_lesemodus`-Screens), Mobile-UI-Regeln und Laufzeit-Verifikation des FlaUI-Laufs vom 2026-10-16 dokumentiert
- [x] `.\scripts\Run-StaticChecks.ps1` — Format, Security und Static-Analysis-Build ohne Befund (Exit-Code 0)
- [x] FlaUI-E2E-Suite tatsächlich ausgeführt (2026-10-16 via `scripts/Run-E2ETests.ps1`): alle drei Pflicht-Tag-Szenarien bestanden — `ArticleTagsTests.ArticleDetail_Tags_ShownBelowArticleText`, `ArticleDetail_Tags_PersistedInDatabase`, `FeedDetailTests.FeedDetail_Search_FiltersByTag`; Gesamtlauf 35/36, einziger Fehlschlag vorbestehend und feature-fremd (`FeedDetail_MetaInfo_WrapsInsteadOfOverlappingActionsButton`, siehe `test-results.md`)

## Hinweise

- **Delta seit `review.3.md`:** Die einzige offene Aufgabe (Task #32, manuelle UI-Verifikation) ist geschlossen. Die AGENTS.md-Regel verlangt „Document the manual UI verification **or** add an automated UI test" — der automatisierte UI-Test existiert (`ArticleTagsTests`), lief auf dem app-seitig erzwungenen 390 × 844-pt-Fenster und ist grün. Die Dokumentation in `mobile-ui-design.md:212-216` und der Eintrag in der Tasks-Datei (Task #32, Testnachweis) entsprechen dem tatsächlichen Lauf; ein manueller Screenshot entfällt plan- und regelkonform.
- **Einordnung des einzigen E2E-Fehlschlags:** `FeedDetail_MetaInfo_WrapsInsteadOfOverlappingActionsButton` gehört zu Issue #127, ist auf `origin/staging` unverändert enthalten und schlägt per Codeanalyse deterministisch fehl (Pre-Sync-Kartentitel statt Kanaltitel). Der Branch berührt weder Feed-Benennung, Sync-Ablauf noch Navigation — feature-fremd, kein Blocker für dieses Feature; Beseitigung obliegt dem Anwender/Folge-Issue.
- **Tag-Serialisierung `U+001F` statt Komma** (Implementierungsverfeinerung, bereits in `review.3.md` bewertet): zulässig, da alle Plan-Invarianten erhalten bleiben und die kommaseparierte Anzeige in `ArticleHtmlBuilder` besteht; abgesichert durch `AddAsync_ThenGetByIdAsync_RoundtripsTagContainingComma`; `datenmodell.md` beschreibt das Format korrekt.
- **Tasks-Datei:** Alle 33 Tasks stehen auf `Erledigt` mit belegten Testnachweisen — keine Änderung an `issue-128-…-tasks.md` erforderlich, der Stand wurde stichprobenartig re-verifiziert.
- **Testnachweise:** `test-results.md` ist aktuell — 753/753 Unit-/Integrationstests bestanden, E2E 35/36 mit dokumentiertem feature-fremdem Einzelfehler.
