<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Keine Befunde

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Reporter.Core/Interfaces/IItemRepository.cs`
- `src/Reporter.Core/Models/Item.cs`
- `src/Reporter.Core/Services/ArticleHtmlBuilder.cs`
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Data/Entities/Item.cs`
- `src/Reporter.Data/Migrations/20261017045445_AddItemTags.cs`
- `src/Reporter.Data/Migrations/20261017045445_AddItemTags.Designer.cs`
- `src/Reporter.Data/Migrations/ReporterDbContextModelSnapshot.cs`
- `src/Reporter.Data/ReporterDbContext.cs`
- `src/Reporter.Data/Repositories/ItemRepository.cs`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter.E2ETests/ArticleTagsTests.cs`
- `src/Reporter.E2ETests/FeedDbAssertions.cs`
- `src/Reporter.E2ETests/FeedDetailTests.cs`
- `src/Reporter.E2ETests/Fixtures/tag-feed.xml`
- `src/Reporter.Tests/ArticleHtmlBuilderTests.cs`
- `src/Reporter.Tests/DelegatingItemRepository.cs`
- `src/Reporter.Tests/FeedDetailViewModelTests.cs`
- `src/Reporter.Tests/FeedSyncServiceTests_Tags.cs`
- `src/Reporter.Tests/ItemRepositoryTests.cs`
- `src/Reporter.Tests/ReporterDbContextTests_Persistence.cs`
- `src/Reporter.Tests/ReporterDbContextTests_Schema.cs`
- `src/Reporter.Tests/TestFeedXml.cs`
- `docs/help/anwendung/artikeldetailansicht.md` (nur Code-korrelierende Aussagen geprüft — konsistent)
- `docs/help/anwendung/datenmodell.md` (nur Code-korrelierende Aussagen geprüft — konsistent, dokumentiert U+001F korrekt)
- `docs/help/anwendung/feeddetailansicht.md` (nur Code-korrelierende Aussagen geprüft — konsistent)
- `docs/help/anwendung/mobile-ui-design.md` (nur Code-korrelierende Aussagen geprüft — konsistent)

## Anmerkungen zur Nachprüfung des Runde-2-Fixes

- **Fix (Stale XML-Doku `Tags`-Eigenschaft):** Korrekt umgesetzt. `src/Reporter.Data/Entities/Item.cs:55-58` beschreibt den Spalteninhalt jetzt als „tag terms of the item joined with the ASCII unit separator (U+001F), or `null` when the item has no tags" — deckt sich exakt mit `ItemRepository.TagSeparator = '\x1F'` (`ItemRepository.cs:541`) und der `null`-Semantik von `SerializeTags` (leere Liste → `null`, `ItemRepository.cs:543-546`).
- **Grep-Verifikation:** Die vier verbliebenen „comma-separated"-Treffer im `src`-Baum sind korrekt und beziehen sich nicht auf das Speicherformat: `FeedSyncService.cs:543` und `FeedSyncServiceTests_Tags.cs:84` beschreiben das Feed-Eingabeformat (`<app:tags>` trägt eine Komma-Liste), `ArticleHtmlBuilder.cs:19` und `ArticleHtmlBuilderTests.cs:15` beschreiben das Anzeigeformat (termweise HTML-enkodiert, mit `", "` gerendert). Kein stale Treffer mehr.

## Erneute Sichtprüfung der Feature-Dateien (Iteration 3)

- `ItemRepository.cs`: `SerializeTags`/`DeserializeTags` (Z. 541-553) bilden eine für domänenseitige Eingaben injektive Roundtrip-Abbildung; `UpdateTagsAsync` (Z. 327-342) mit Empty-Guard und dokumentiertem Update-pro-Eintrag-Verhalten konsistent zur Interface-Doku (`IItemRepository.cs:144-151`); `GetByFeedAsync`-Suche (Z. 260) prüft Titel und `Tags.Contains` — Trennzeichen nicht per Tastatur erreichbar, Substring-Semantik bleibt korrekt; alle `Tags`-Kopierstellen (`MapToEntity` Z. 529, `MapToModel` Z. 512, `UpdateAsync` Z. 92) laufen über das zentrale Serialisierungspaar.
- `FeedSyncService.cs`: `ExtractTags` (Z. 547-585) merged `<*:tags>`-Extensions (Komma-Liste, Trim, leere Einträge verworfen) und `item.Categories` in Dokumentreihenfolge mit ordinaler Dedup; Tag-Backfill (Z. 311-314, 340-344) greift nur bei `Tags.Count == 0`, überschreibt nie und schreibt nur nicht-leere Treffer via `UpdateTagsAsync` (Z. 234-237); neue Items erhalten `Tags = ExtractTags(feedItem)` (Z. 369).
- `ArticleHtmlBuilder.cs`: Tag-Block wird nach dem Sanitizer angehängt, jeder Term einzeln `WebUtility.HtmlEncode`-enkodiert; CSP verbietet Skripte.
- Konsistenz quer über den Branch: `Models/Item.cs:63` (`IReadOnlyList<string> Tags = []`), Entity `Tags` nullable string, Migration `AddItemTags` (`tags` TEXT nullable), `ReporterDbContext.cs:117` Mapping, `DelegatingItemRepository.cs:85-86` delegiert `UpdateTagsAsync`, `ArticleDetailViewModel.cs:363` übergibt `Item?.Tags ?? Array.Empty<string>()`. Kein toter Code, keine neuen Befunde.
