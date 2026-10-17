<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### ItemRepository.cs (ItemRepository)

- **Datenstrukturen / Primitive Obsession** — `SerializeTags`/`DeserializeTags` (Zeilen 536–546) bilden keinen verlustfreien Roundtrip: Ein einzelnes Schlagwort, das selbst ein Komma enthält (zulässig als Atom `<category term="A, B">` oder RSS `<category>` — `ExtractTags` übernimmt Kategorienamen unverändert), wird per `string.Join(", ", …)` gespeichert und beim `Split(',')` in zwei Tags zerlegt. Umgekehrt serialisieren `["a", "b,c"]` und `["a", "b", "c"]` zur identischen Spalte — die Abbildung ist nicht injektiv.

  Empfehlung: Entweder ein kollisionsfreies Format speichern (z. B. JSON-Array oder Komma-Escaping vor dem Join) oder Kommas bereits in `FeedSyncService.ExtractTags` verwerfen/ersetzen und das dokumentieren. Ein Roundtrip-Test mit einem kommahaltigen Tag (in `ItemRepositoryTests`) sollte die gewählte Semantik festnageln.

- **Struktur und Verantwortlichkeiten / Dokumentationsabweichung** — `UpdateTagsAsync` (Zeilen 327–342): Der Kommentar im Interface (`IItemRepository.cs`, Zeile 146) verspricht „in a single batch", die Implementierung führt jedoch pro Eintrag ein eigenes `ExecuteUpdateAsync` aus — beim ersten Sync nach dem Upgrade auf den Tag-Support sind das potenziell hunderte sequentielle UPDATE-Statements auf demselben Kontext. Nebenläufigkeit ist unproblematisch (sequentieller Loop, pro Feed serialisiert über `_feedSyncLocks`), aber die Aussage stimmt nicht mit dem Verhalten überein.

  Empfehlung: Entweder nach serialisiertem Wert gruppieren und pro Tag-Set nur ein `ExecuteUpdateAsync` mit `Where(i => groupIds.Contains(i.Id))` absetzen, oder die Interface-Dokumentation an die Loop-Implementierung anpassen.

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
- `docs/help/anwendung/datenmodell.md` (nur Code-korrelierende Aussagen geprüft — konsistent)
- `docs/help/anwendung/feeddetailansicht.md` (nur Code-korrelierende Aussagen geprüft — konsistent)
- `docs/help/anwendung/mobile-ui-design.md` (nur Code-korrelierende Aussagen geprüft — konsistent)
