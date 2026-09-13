<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

Basisbranch: `origin/staging` (Merge-Base via `git merge-base HEAD origin/staging`). Geprüft wurden primär die uncommitted Lauf-2-/Iteration-3-Änderungen (`TryPersistNewFeedAsync`/`FinishAddFlowAsync`/`HandleSearchFailure`, `IsEditMode`-Guard für `DirectAddCommand`, `ToFeed`-Helper, `ResetForm`-Wiederverwendung in `OpenAddForm`, positionsbasierte ActionSheet-Auflösung mit Zählsuffix, Meta-URL-`DataTrigger`, `MinimumHeightRequest="44"`, Offline-Hinweis-`IsEditMode`-Trigger) sowie das Zusammenspiel mit dem committed Stand (`be7e7c9`/`27f4795`/`813c271`). Build `src/Reporter.Tests` läuft fehlerfrei (0 Warnungen, 0 Fehler), `dotnet test`: 311/311 Tests grün. Verdrahtungs-Check `FeedsPage.xaml`/`.xaml.cs`: Alle Commands (`OpenAddFormCommand`, `CloseAddFormCommand`, `SearchCommand`, `DirectAddCommand`, `SaveCommand`, `RefreshAllCommand`, `CloseSearchResultsCommand`, `Entry.ReturnCommand`), beide `TapGestureRecognizer` (`OnFeedTapped`, `OnSearchResultTapped`) und der Backdrop-`TapGestureRecognizer` besitzen zugehörige Handler — keine toten/unverdrahteten Aktionen. Alle Befunde aus Iteration 2 sind behoben. Neu referenzierte `AppResources`-Schlüssel existieren in `resx` und `de.resx`; der entfernte Schlüssel `PlaceholderFeedTitle` ist aus beiden resx-Dateien getilgt.

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedsViewModel.cs (FeedsViewModel)

- **Fehlerbehandlung/Stale State (gering)** — `ResetForm` (Z. 383–392) leert `ErrorMessage` und `SearchErrorMessage` nicht. `CloseAddFormCommand` ist direkt auf `ResetForm` verdrahtet (Z. 71) und wird auch über `OnBackButtonPressed` und den Backdrop-Tap ausgelöst: Ein Validierungs-, Dubletten- oder Such-Fehler, der im Sheet angezeigt wurde (Sheet-Labels `FeedsPage.xaml` Z. 262–269), bleibt nach dem Schließen des Sheets im Seitenkopf über der Feed-Liste sichtbar (Header-Labels `FeedsPage.xaml` Z. 20–23 und 48–51 binden dieselben Kanäle). `OpenAddForm` (Z. 537–545) leert beide Kanäle dagegen explizit — die Schließ-Richtung ist asymmetrisch.

  Empfehlung: `ErrorMessage = string.Empty` und `SearchErrorMessage = string.Empty` in `ResetForm` (oder einem dedizierten Close-Pfad) setzen; `RenameFeedAsync`-Fehler bleiben davon unberührt, da sie erst nach dem Schließen gesetzt werden.

- **Konsistenz/fragile Indirektion (gering)** — `SaveAsync` (Z. 331) leitet `categoryId` aus `SelectedCategory` ab, obwohl der Kategorie-`Picker` mit dem Umbau entfernt wurde und `SelectedCategory` an kein UI-Element mehr gebunden ist. Alle anderen erhaltenen Felder (`LastCheckedAt`, `HealthStatus`, `HealthLastChange`) kommen direkt aus `SelectedFeed`. `SelectedCategory` ist damit reiner Preserve-State aus `EditAsync` und kann durch `LoadAsync` (Z. 292–295: Reset auf den Pseudo-Eintrag bei fehlender Kategorie) driften — ein `LoadAsync` zwischen `EditAsync` und `SaveAsync` würde die Kategorie-Zuordnung still verwerfen. Zusätzlich dupliziert der Inline-`new Feed`-Block in `SaveAsync` (Z. 333–343) das inzwischen zentrale `ToFeed`-Mapping (Z. 522–535).

  Empfehlung: `categoryId` direkt aus `SelectedFeed.CategoryId` lesen (konsistent zu den übrigen Feldern) und `ToFeed` um einen `url`-Parameter erweitern, sodass auch `SaveAsync` den Helper nutzt. `SelectedCategory` ist danach nur noch für `EditAsync`→`SaveAsync` relevant bzw. kann ganz entfallen.

### FeedsPage.xaml.cs (FeedsPage)

- **Fehlerbehandlung/Mehrdeutigkeit (gering)** — `ChangeCategoryAsync` (Z. 135–156): Die Zählsuffix-Disambiguierung garantiert keine eindeutigen Optionen. Heißt eine Kategorie wörtlich wie ein erzeugtes Suffix (z. B. Kategorien `News`, `News`, `News (2)`), entstehen die Optionen `News`, `News (2)`, `News (2)` — `options.IndexOf(selectedName)` (Z. 152) löst den Klick auf den dritten Eintrag dann fälschlich auf Index 1 auf, die Zuordnung geht stillschweigend an die falsche Kategorie. Der Kommentar (Z. 132–134) behauptet „keeps every option unique", was nicht zutrifft.

  Empfehlung: Beim Erzeugen gegen die bereits vergebenen Labels prüfen (z. B. `HashSet<string>`/`options.Contains`) und den Zähler bei Kollision weiter erhöhen, oder den Kommentar auf die tatsächlich abgedeckte Teilmenge korrigieren.

## Geprüfte Dateien

Produktivcode (vollständig gelesen):
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
- `src/Reporter.Core/Services/FeedTitleFallback.cs` (neu)
- `src/Reporter.Core/Services/FeedSyncService.cs` (Diff)
- `src/Reporter.Core/Models/FeedListItem.cs`, `src/Reporter.Core/Models/Feed.cs` (Kontext)
- `src/Reporter.Core/ViewModels/BaseViewModel.cs` (Kontext)
- `src/Reporter/MauiProgram.cs` (DI-Registrierung, Kontext)
- `src/Reporter.Core/Resources/Strings/AppResources.resx`, `AppResources.de.resx`, `AppResources.Designer.cs` (Diff/Referenz-Check)

Tests (vollständig gelesen):
- `src/Reporter.Tests/FeedsViewModelTests.cs` (Diff)
- `src/Reporter.Tests/FeedSyncServiceTests.cs` (Diff)
- `src/Reporter.Tests/FeedTitleFallbackTests.cs` (neu)
- `src/Reporter.Tests/FakeFeedSearchService.cs` (Kontext)

Sonstige (grob geprüft / Doku):
- `scripts/add-license-headers.mjs`
- `test-results/issue-59/uia.ps1`
- `docs/help/anwendung/mobile-ui-design.md`, `test-results.md`, `docs/features/...` (Tasks/Todo/Review-Dokumente)
