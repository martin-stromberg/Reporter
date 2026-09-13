<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

Basisbranch: `origin/staging` (Merge-Base via `git merge-base HEAD origin/staging`). Geprüft wurden primär die uncommitted Lauf-2-/Iteration-2-Änderungen (`DirectAddCommand`/`DirectAddAsync`/`TryAddFeedDirectlyAsync`, Edit-only-`SaveAsync`, `OpenAddForm`-Reset, `PropertyChanged`-Lebenszyklus, ActionSheet-Cancel-Filter, `IsEditMode`-DataTrigger) sowie das Zusammenspiel mit dem committed Lauf-1-/Lauf-2-Stand (`be7e7c9`/`27f4795`/`813c271`). Build `src/Reporter.Tests` läuft fehlerfrei (0 Warnungen). Verdrahtungs-Check `FeedsPage.xaml`/`.xaml.cs`: Alle Commands (`OpenAddFormCommand`, `CloseAddFormCommand`, `SearchCommand`, `DirectAddCommand`, `SaveCommand`, `RefreshAllCommand`, `CloseSearchResultsCommand`, `Entry.ReturnCommand`), beide `TapGestureRecognizer` (`OnFeedTapped`, `OnSearchResultTapped`), der Backdrop-`TapGestureRecognizer` sowie alle `DisplayActionSheetAsync`/`DisplayPromptAsync`/`DisplayAlertAsync`-Aufrufe besitzen zugehörige Handler — keine toten/unverdrahteten Aktionen. Alle Befunde der Voriteration (Event-Leak, God-Methode `OnFeedTapped`, Cancel-Kollision, `ShowSearchResults`-Inkonsistenz, Add-Pfad in `SaveAsync`, `OpenAddForm`-Stale-State, veralteter Kommentar, toter Ressourcenschlüssel) sind behoben.

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedsViewModel.Search.cs (FeedsViewModel)

- **Doppelter Code** — `TryAddFeedDirectlyAsync` (Z. 299–309) und `SubscribeResultAsync` (Z. 335–345) enthalten nahezu identische `new Feed { ... }`-Initializer mit denselben festen Defaults (`CategoryId = null`, `LastCheckedAt = null`, `HealthStatus = FeedHealth.Ok`, `HealthLastChange = null`, `NotificationsEnabled = true`); nur `Url` und `Title` unterscheiden sich. Ebenso wiederholt sich die Abschluss-Sequenz `SearchResults.Clear(); ShowSearchResults = false; ResetForm(); await LoadAsync();` in `OfferDirectAddAsync` (Z. 264–267) und `SubscribeResultAsync` (Z. 347–350) sowie `ResetForm(); await LoadAsync();` in `DirectAddAsync` (Z. 281–282).

  Empfehlung: `PersistNewFeedAsync(string url, string title)` (inkl. Duplikat-Prüfung, die ohnehin identisch ist) und eine gemeinsame `FinishAddFlowAsync()`-Methode auslagern.

- **Fehlerbehandlung/Inkonsistenz (gering)** — `DirectAddAsync` (Z. 270–284): Schlägt `TryAddFeedDirectlyAsync` fehl (Dublette), bleibt eine zuvor gesetzte `SearchErrorMessage` stehen; das Sheet zeigt dann `SearchErrorMessage` und `ErrorMessage` gleichzeitig (`FeedsPage.xaml` Z. 256–263). `OfferDirectAddAsync` löscht `SearchErrorMessage` im analogen Fehlerpfad explizit (Z. 257) mit der Begründung, der Dubletten-Fehler solle die einzige sichtbare Meldung sein — der `DirectAddCommand`-Pfad ist damit asymmetrisch.

  Empfehlung: `SearchErrorMessage = string.Empty` in `DirectAddAsync` oder zentral in `TryAddFeedDirectlyAsync` setzen.

- **Fehlende Vorbedingung (gering)** — `DirectAddCommand` (CanExecute `!IsSearching`, Z. 67 in `FeedsViewModel.cs`) und `DirectAddAsync` prüfen `IsEditMode` nicht, während `SearchCommand`/`SearchAsync` den Modus doppelt absichern (CanExecute `!IsEditMode` + Methoden-Guard Z. 120–123). Aktuell über die UI nicht erreichbar (Button per `DataTrigger` ausgeblendet), aber die Asymmetrie ist fragil: Würde das Command im Edit-Modus ausgelöst, legte es während eines laufenden Edits einen neuen Feed mit der Edit-URL und Fallback-Titel an.

  Empfehlung: `!IsEditMode` in den CanExecute-Ausdruck aufnehmen und/oder denselben Guard wie in `SearchAsync` ergänzen.

- **God-Methode (gering)** — `SearchAsync` (Z. 109–186, ~78 Zeilen) orchestriert vier Phasen (Eingabe-Guard, URL-Auflösung, Suche inkl. Erfolgs-/Fehlerbehandlung, Direct-Add-Angebot). Lesbar durch Kommentare strukturiert, überschreitet aber die ~50-Zeilen-Grenze; die Fehlerbehandlung im `catch`-Block (Stale-Check, Meldungsauswahl, `offerDirectAdd`-Setzung) ist ein eigenständiger Teilschritt.

  Empfehlung: Den `catch`-Block in eine Methode wie `HandleSearchFailure(Exception ex, string input, bool isDirectUrl)` auslagern.

### FeedsViewModel.cs (FeedsViewModel)

- **Doppelter Code** — `OpenAddForm` (Z. 540–544) wiederholt den Reset-Block aus `ResetForm` (Z. 384–388: `SelectedFeed`, `NewUrl`, `NewTitle`, `FeedNotificationsEnabled`, `SelectedCategory` zurücksetzen) fast identisch. Außerdem bauen `RenameFeedAsync` (Z. 494–504) und `ChangeFeedCategoryAsync` (Z. 522–532) jeweils ein vollständiges `Feed`-Objekt aus denselben acht `FeedListItem`-Feldern — identisch bis auf `Title` bzw. `CategoryId` (gleiches Muster auch in `SaveAsync` Z. 332–342 und `FeedSyncService.UpdateFeedHealthAsync`).

  Empfehlung: `OpenAddForm` auf `ResetForm(); ShowAddForm = true;` plus Error-Clearing umstellen (oder gemeinsame private Reset-Routine extrahieren); für die Update-Pfade einen `FeedListItem`→`Feed`-Mapping-Helper (z. B. `ToFeed(feed, title: …, categoryId: …)`) einführen.

- **Veraltete Dokumentation** — `SaveCommand`-XML-Doc (Z. 80–81): „saves a new or existing feed" — der Add-Pfad wurde entfernt, `SaveAsync` ist edit-only (Guard Z. 301–304).

  Empfehlung: Kommentar auf „saves the currently edited feed" o. ä. umstellen.

### FeedsPage.xaml

- **Inkonsistente Touch-Target-Höhe** — Die Buttons „Suchen" (Z. 282), „URL direkt hinzufügen" (Z. 290) und „Speichern" (Z. 340) haben kein `MinimumHeightRequest="44"`, während alle anderen Buttons derselben Datei (Z. 17–19, 123–127, 251–254) eines setzen. Verstößt gegen die Projektregel „touch targets ≥ 44 × 44 pt" (`AGENTS.md`) und die Datei-interne Konvention.

  Empfehlung: `MinimumHeightRequest="44"` an den drei Buttons ergänzen.

- **Doppelte Anzeige der Feed-URL (gering)** — In der Trefferkarte zeigt die Headline-`Label` (Z. 80–89) bei leerem `Title` `FeedUrl` und die Meta-`Label` darunter (Z. 109–112) erneut `FeedUrl` — die URL erscheint dann zweimal direkt untereinander.

  Empfehlung: Die untere URL-`Label` per `DataTrigger` ausblenden, wenn `Title` leer ist (invers zum `StringNotEmptyToBoolConverter`-Trigger der Headline).

### FeedsPage.xaml.cs (FeedsPage)

- **Fehlerbehandlung/Mehrdeutigkeit (gering)** — `ChangeCategoryAsync` (Z. 136): `FirstOrDefault(c => c.Name == selectedName)` löst die Auswahl rein über den Namen auf. Existieren zwei Kategorien mit identischem Namen, wird stillschweigend die erste zugewiesen — die Auswahl ist nicht eindeutig. (Die Cancel-Kollision selbst ist korrekt gefiltert, Z. 127–129.)

  Empfehlung: Hinweis im Kommentar festhalten oder die Auswahl positionsbasiert auflösen (Index der gewählten Option in der gefilterten Liste), falls die Plattform den Index liefert; alternativ Dubletten-Namen beim Anlegen von Kategorien verhindern.

## Geprüfte Dateien

Produktivcode (vollständig gelesen):
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
- `src/Reporter.Core/Services/FeedTitleFallback.cs` (neu)
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/Services/FeedSearchService.cs`
- `src/Reporter.Core/Services/FeedSearchUnavailableException.cs`
- `src/Reporter.Core/Interfaces/IFeedSearchService.cs`
- `src/Reporter.Core/Models/FeedSearchResult.cs`
- `src/Reporter.Core/Models/FeedSearchMatchKind.cs`
- `src/Reporter.Core/Models/Feed.cs` / `FeedListItem.cs` (Kontext)
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` / `AppResources.Designer.cs` (Diff; Schlüssel-Parität EN/DE/Designer verifiziert, `PlaceholderFeedTitle` sauber entfernt)
- `src/Reporter/MauiProgram.cs` (Diff: `IFeedSearchService`-Registrierung)

Tests:
- `src/Reporter.Tests/FeedsViewModelTests.cs` (vollständig)
- `src/Reporter.Tests/FeedSyncServiceTests.cs` (vollständig)
- `src/Reporter.Tests/FeedTitleFallbackTests.cs` (neu)
- `src/Reporter.Tests/FeedSearchServiceTests.cs` (vollständig)
- `src/Reporter.Tests/FakeFeedSearchService.cs`
- `src/Reporter.Tests/ServiceCollectionTests.cs` (Diff)

Sonstiges (nur grob geprüft / Doku):
- `test-results/issue-59/uia.ps1`, `probe.fsx`, `stubserver.py` (Hilfsskripte, kein Produktivcode)
- `scripts/add-license-headers.mjs` (Diff: `SKIP_DIRS` um `.vs`/`TestResults` erweitert — unproblematisch)
- `docs/...`, `test-results.md`, `README.md`, `changes.log` (Dokumentation)
