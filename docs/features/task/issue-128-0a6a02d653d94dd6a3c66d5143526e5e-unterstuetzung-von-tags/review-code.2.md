<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### Item.cs (Reporter.Data.Entities.Item)

- **Stale XML-Dokumentation / Dokumentationsabweichung** — Zeile 56: Der Kommentar der `Tags`-Eigenschaft beschreibt den Spalteninhalt noch als „comma-separated tag terms". Seit der Umstellung des Serialisierungsformats auf den Unit-Separator (U+001F) in `ItemRepository.cs` (`TagSeparator = '\x1F'`, Zeile 541) stimmt das nicht mehr — ein kommahaltiger Begriff bleibt gerade deshalb verlustfrei, weil kein Komma als Trenner dient. Einzige verbliebene Stelle im src-Baum mit der veralteten Formatbeschreibung.

  Empfehlung: Kommentar an das tatsächliche Format anpassen, z. B. „Gets or sets the tag terms of the item separated by the unit separator (U+001F), or `null` when the item has no tags." Optional: den Verweis auf die Formatbegründung in `ItemRepository.SerializeTags` knapp halten (die Spalte bleibt für die Entität opaker Text).

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

## Anmerkungen zur Nachprüfung der Runde-1-Fixes

- **Fix 1 (Komma-Kollision):** Korrekt umgesetzt. `SerializeTags`/`DeserializeTags` (`ItemRepository.cs:541-553`) bilden für domänenseitig mögliche Eingaben eine injektive Abbildung: leere Liste → `null`, `null` → `[]`, kommahaltige Begriffe bleiben ein einzelnes Tag (durch `AddAsync_ThenGetByIdAsync_RoundtripsTagContainingComma` festgenagelt). U+001F ist in XML 1.0 kein zulässiges Zeichen — feed-seitig kann kein Tag das Trennzeichen enthalten, defensive Filterung ist daher nicht nötig und der Code-Kommentar begründet die Invariante nachvollziehbar.
- **Fix 2 (`UpdateTagsAsync`-Doku):** Korrekt umgesetzt — `IItemRepository.cs:144-151` beschreibt jetzt „one UPDATE statement per entry" und begründet, warum EF Core nicht zu einem einzelnen `ExecuteUpdate` falten kann; deckt sich mit der Schleifen-Implementierung (`ItemRepository.cs:327-342`).
- **Weitere Verifikation:** Die `Contains`-Suche (`ItemRepository.cs:260`) kann das Trennzeichen nicht per Tastatureingabe überspringen; alle `Tags`-Kopierstellen (`MapToEntity`, `MapToModel`, `UpdateAsync`, `ArticleDetailViewModel.CreateItemCopy`) sind konsistent; kein toter Code; `IItemRepository` hat nur zwei Implementierungen (`ItemRepository`, `DelegatingItemRepository`), beide vollständig; Migration, Designer und ModelSnapshot sind konsistent (`tags` TEXT nullable).
