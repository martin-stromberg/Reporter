<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

Basisbranch: `origin/staging` (Merge-Base `b3aee19`). Geprüft wurden primär die uncommitted Lauf-2-Änderungen (Bottom-Sheet, Umbenennen/Kategorie-Dialoge, `FeedTitleFallback`) sowie das Zusammenspiel mit dem committed Lauf-1-Code (Feed-Suche, `be7e7c9`/`27f4795`). Verdrahtungs-Check `FeedsPage.xaml`/`.xaml.cs`: Alle Commands (`OpenAddFormCommand`, `CloseAddFormCommand`, `SearchCommand`, `SaveCommand`, `RefreshAllCommand`, `CloseSearchResultsCommand`), beide `TapGestureRecognizer` (`OnFeedTapped`, `OnSearchResultTapped`), `Entry.ReturnCommand` sowie alle `DisplayActionSheet`/`DisplayPromptAsync`/`DisplayAlertAsync`-Aufrufe besitzen zugehörige Handler — keine toten/unverdrahteten Aktionen.

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedsPage.xaml.cs (FeedsPage)

- **Kopplung/Lebenszyklus — Event-Subscription ohne Abmeldung** — Konstruktor, Z. 24–30: `viewModel.PropertyChanged += (s, e) => …` abonniert ein Ereignis des als **Singleton** registrierten `FeedsViewModel` (`MauiProgram.cs` Z. 63) aus einer als **Transient** registrierten Page (`MauiProgram.cs` Z. 70) und wird nirgends abgemeldet. Das Lambda captured `this`/`NewUrlEntry`: Bei jeder erneuten Erstellung der Page akkumuliert ein weiterer Handler am Singleton, der die alte Page-Instanz am Leben hält und `Focus()` auf losgelöste Controls dispatchen würde.

  Empfehlung: Handler in eine benannte Methode auslagern und beim Verlassen der Page abmelden (z. B. `OnDisappearing`/`HandlerChanged`), oder ein schwaches Event-Muster verwenden.

- **God-Methode** — `OnFeedTapped`, Z. 50–117 (~68 Zeilen): Dispatch über fünf konzeptuell getrennte Aktionen (Aktualisieren, Umbenennen inkl. `DisplayPromptAsync`, Kategorie ändern inkl. `DisplayActionSheetAsync`, Bearbeiten, Löschen inkl. `DisplayAlertAsync`), jede mit eigener Dialog- und Folgelogik.

  Empfehlung: Die Aktions-Branches in private Methoden auslagern (z. B. `RenameFeedAsync(feed)`, `ChangeCategoryAsync(feed)`, `ConfirmDeleteAsync(feed)`), sodass `OnFeedTapped` nur noch auswählt und delegiert.

- **Fehlerbehandlung (gering) — Cancel-Kollision im Kategorie-ActionSheet** — Z. 87–98: `DisplayActionSheetAsync` liefert bei Abbruch den Cancel-Text (`AppResources.ButtonCancel`) zurück; eine Kategorie, die exakt „Abbrechen"/„Cancel" heißt, ist per `Array.IndexOf(categoryNames, selectedName)` vom Abbruch nicht unterscheidbar und würde irrtümlich zugewiesen. (Gleiches Muster existiert bereits in `UnreadPage.xaml.cs` Z. 50–62 — ggf. einheitlich lösen.)

  Empfehlung: Abbruch robust erkennen, z. B. Kategorienamen, die dem Cancel-Text entsprechen, aus den Optionen ausfiltern oder die Auswahl über einen bestätigenden Dialog statt reinen Textvergleich auflösen.

### FeedsViewModel.Search.cs (FeedsViewModel)

- **Inkonsistenter UI-Zustand im Dubletten-Pfad** — `OfferDirectAddAsync`, Z. 242–251: Kommt die Direkt-Hinzufügen-Bestätigung aus dem Erfolgszweig von `SearchAsync` (`results.Count == 0`), wurde `ShowSearchResults = true` bereits gesetzt (Z. 142); der Dubletten-Return setzt `ShowAddForm = true`, lässt `ShowSearchResults` aber `true`. Nach dem Schließen des Sheets landet der Nutzer auf der noch aktiven, leeren Trefferansicht statt der Feed-Liste — im Fehlerpfad (`catch`, Z. 161–163) und im Abbruchpfad (Z. 235–240) wird `ShowSearchResults` dagegen auf `false` gesetzt.

  Empfehlung: Im Dubletten-Zweig `SearchResults.Clear()`/`ShowSearchResults = false` (bzw. `CloseSearchResults()`) aufrufen, damit das Sheet über der Feed-Liste und nicht über der Trefferansicht wieder öffnet.

### FeedsViewModel.cs (FeedsViewModel)

- **Toter Code-Pfad / veraltete Formularfelder** — `SaveAsync`, Z. 318–331 (`SelectedFeed is null`-Add-Zweig): Der Speichern-Button existiert in `FeedsPage.xaml` nur noch innerhalb des `IsEditMode`-Blocks (Z. 326–327), ein `Entry` für `NewTitle` und ein `Picker` für `SelectedCategory` sind vollständig entfernt. Der Add-Pfad von `SaveAsync` ist über die UI nicht mehr erreichbar und wird nur noch von Tests (`SaveCommand_NewFeed_*`) am Leben gehalten; `NewTitle`/`SelectedCategory` sind nicht mehr nutzereditierbar (nur noch VM-intern für den Edit-Fall).

  Empfehlung: Entweder den Add-Zweig aus `SaveAsync` entfernen (Hinzufügen läuft ausschließlich über Suche/Direkt-Add) oder — falls der Pfad als API bewusst behalten wird — dies im Kommentar festhalten und die Add-Tests entsprechend als Kontrakt- statt UI-Pfad-Tests kennzeichnen.

- **Fehlende Zustandsrücksetzung (gering)** — `OpenAddForm`, Z. 539–545: Setzt `IsEditMode`/`ShowAddForm` und leert Fehlermeldungen, lässt aber `SelectedFeed`, `NewUrl` und `NewTitle` unverändert. Nach der Sequenz „Bearbeiten → Suchen" (Suche setzt `ShowAddForm = false`, ohne den Edit-Zustand zurückzusetzen, Z. 142–143 in `FeedsViewModel.Search.cs`) öffnet der „+"-Button das Sheet im Add-Modus mit dem URL-Wert des zuvor bearbeiteten Feeds und hängendem `SelectedFeed`. Aktuell folgenlos, weil `SaveCommand` im Add-Modus nicht erreichbar ist und alle Ausstiegspfade `ResetForm()` aufrufen — die Invariante „Add-Modus ⇒ kein `SelectedFeed`" ist aber verletzt.

  Empfehlung: In `OpenAddForm` `SelectedFeed = null` setzen (und ggf. `NewUrl`/`NewTitle` leeren), analog zur Reset-Logik in `ResetForm`.

### FeedSyncService.cs (FeedSyncService)

- **Veralteter Kommentar** — Z. 214–215: „The direct-add flow pre-fills the title with the URL host" — die Titel-Vorbelegung wurde in diesem Lauf entfernt (`OfferDirectAddAsync` persistiert jetzt direkt mit `FeedTitleFallback.GetFallbackTitle`). Der Host-Platzhalter-Check bleibt nötig, weil `GetFallbackTitle` bei pfadlosen URLs den Host als Titel liefert — der Kommentar beschreibt aber einen nicht mehr existierenden Mechanismus.

  Empfehlung: Kommentar auf den aktuellen Mechanismus umstellen („direct-add/search fallback titles can equal the URL host or file name — `FeedTitleFallback`").

### AppResources (Reporter.Core)

- **Toter Ressourcenschlüssel** — `PlaceholderFeedTitle` (`AppResources.resx` Z. 131, `AppResources.de.resx`, `AppResources.Designer.cs` Z. 392–394): Der zugehörige `Entry` wurde mit der Formularkarte entfernt; der Schlüssel wird im gesamten Repo nur noch in den resx-/Designer-Dateien referenziert.

  Empfehlung: Schlüssel aus beiden resx-Dateien und dem Designer entfernen (oder — falls ein Titel-Feld im Sheet wieder geplant ist — als bewusst vorgehaltenen Schlüssel kommentieren).

### test-results.md

- **Dokumentationsabweichung (trivial)** — Abschnitt „Iteration 3": behauptet `RoundRectangle 20,20,0,0` für die Sheet-Karte; `FeedsPage.xaml` Z. 237 verwendet `RoundRectangle 12,12,0,0`.

  Empfehlung: Wert in der Doku korrigieren.

## Geprüfte Dateien

Produktivcode (vollständig):
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
- `src/Reporter.Core/ViewModels/BaseViewModel.cs` (Kontext)
- `src/Reporter.Core/Services/FeedTitleFallback.cs` (neu)
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/Services/FeedSearchService.cs` (Lauf-1-Zusammenspiel)
- `src/Reporter.Core/Services/FeedSearchUnavailableException.cs`
- `src/Reporter.Core/Interfaces/IFeedSearchService.cs`
- `src/Reporter.Core/Models/FeedSearchResult.cs`
- `src/Reporter.Core/Models/FeedSearchMatchKind.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` / `AppResources.Designer.cs` (Diff)
- `src/Reporter/MauiProgram.cs` (DI-Lebensdauern)

Tests:
- `src/Reporter.Tests/FeedsViewModelTests.cs` (Diff, ~355 neue Zeilen)
- `src/Reporter.Tests/FeedSyncServiceTests.cs` (Diff)
- `src/Reporter.Tests/FeedTitleFallbackTests.cs` (neu)
- `src/Reporter.Tests/FakeFeedSearchService.cs`
- `src/Reporter.Tests/FeedSearchServiceTests.cs` / `ServiceCollectionTests.cs` (Lauf 1, übernommen)

Sonstiges:
- `scripts/add-license-headers.mjs` (Diff: `SKIP_DIRS` um `.vs`/`TestResults` erweitert — unproblematisch)
- `docs/help/anwendung/mobile-ui-design.md`, `test-results.md`, `docs/features/...` (Doku)
- `test-results/issue-59/uia.ps1` (Hilfsskript, nur grob geprüft: neue `sysback`-Aktion via `mouse_event`/XButton1, kein Produktivcode)
