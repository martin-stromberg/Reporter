<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedsViewModel.Search.cs (FeedsViewModel)

- **Doppelter Code / fehlende Kapselung** — Der Stale-Results-Check `!string.Equals(NewUrl.Trim(), input, StringComparison.Ordinal)` steht identisch zweimal in `SearchAsync` (Z. 131–136 im Try-Zweig, Z. 153–156 im Catch-Zweig). Zusammen mit der Fehlerbehandlung wächst die Methode auf ~75 Zeilen an.

  Empfehlung: Die Prüfung in eine kleine Hilfsmethode auslagern (z. B. `private bool IsStaleInput(string input) => !string.Equals(NewUrl.Trim(), input, StringComparison.Ordinal);`) und an beiden Stellen aufrufen; der erklärende Kommentar aus Z. 133–135 wandert dorthin.

- **Fachliche Konsistenz** — `OfferDirectAddAsync` (Z. 228–232) füllt `NewTitle` beim bestätigten Direkt-Hinzufügen mit `hostUri.Host` (z. B. `example.com`). Die in diesem Branch neu eingeführte Platzhalter-Erkennung in `FeedSyncService` (`isPlaceholderTitle`, Z. ~187–189) löst den Dokumenttitel aber nur auf, wenn der Titel leer ist oder exakt der Feed-URL entspricht. Ein direkt hinzugefügter Feed behält damit dauerhaft den bloßen Hostnamen als Titel, während ein über `SubscribeResultAsync` abonnierter titelloser Feed (Titel = FeedUrl) beim ersten Sync den echten Feed-Titel erhält — zwei automatisch erzeugte Titel werden unterschiedlich behandelt, und ausgerechnet der Direkt-Hinzufügen-Pfad, in dem kein Titel ermittelt werden konnte, profitiert nicht von der Titel-Auflösung.

  Empfehlung: Eine der beiden Varianten vereinheitlichen — entweder in `OfferDirectAddAsync` ebenfalls die URL als Titel-Platzhalter speichern (analog `SubscribeResultAsync`), damit der Sync den Dokumenttitel nachzieht, oder die Platzhalter-Erkennung in `FeedSyncService` um den Host-Fall erweitern (Titel == Host der Feed-URL).

## Geprüfte Dateien

- `src/Reporter.Core/Interfaces/IFeedSearchService.cs`
- `src/Reporter.Core/Models/FeedSearchMatchKind.cs`
- `src/Reporter.Core/Models/FeedSearchResult.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Services/FeedSearchService.cs`
- `src/Reporter.Core/Services/FeedSearchUnavailableException.cs`
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter.Tests/FakeFeedSearchService.cs`
- `src/Reporter.Tests/FeedSearchServiceTests.cs`
- `src/Reporter.Tests/FeedSyncServiceTests.cs`
- `src/Reporter.Tests/FeedsViewModelTests.cs`
- `src/Reporter.Tests/ServiceCollectionTests.cs`

Zusätzlich grob geprüft (Verifikationshilfsmittel, kein Produktivcode — keine Befunde):
- `test-results/issue-59/uia.ps1`
- `test-results/issue-59/stubserver.py`
- `test-results/issue-59/probe.fsx`

## Verifizierte Fixes aus Iteration 2

- God-Klasse: Such-Verantwortlichkeit in `FeedsViewModel.Search.cs` ausgelagert.
- Stale-Results-Race: `NewUrl.Trim()`-Vergleich nach dem `await` in Try- und Catch-Pfad.
- `FeedHealth.Ok`-Konstante statt `"OK"`-Literal in `SaveAsync` (Z. 294).
- Doppelter Offline-Hinweis: `!IsOnline`-Zweig in `SearchAsync` ist jetzt schlichtes `return`; das Label bleibt einziger Mechanismus.
- Redirect-URL: `FeedUrl` nutzt jetzt `response.RequestMessage?.RequestUri?.AbsoluteUri` in `DiscoverFeedsAsync` (Z. 179) und `ProbeStandardPathsAsync` (Z. 233).

UI-Verdrahtung `FeedsPage.xaml`: Alle Aktionen besitzen Handler — `SearchCommand`, `SaveCommand` (jeweils Button + ReturnCommand), `SubscribeResultCommand` via `OnSearchResultTapped`, `CloseSearchResultsCommand`, `RefreshAllCommand` via `RefreshView`, `OnFeedTapped` → Refresh/Edit/Delete. Keine toten Aktionen.
