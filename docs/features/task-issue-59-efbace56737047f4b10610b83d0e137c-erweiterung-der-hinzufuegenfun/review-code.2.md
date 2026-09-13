<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedsViewModel.cs (FeedsViewModel)

- **God-Klasse** — Die Klasse ist mit diesem Feature auf ~687 Zeilen angewachsen und deckt jetzt drei klar abgrenzbare fachliche Verantwortlichkeiten ab: Feed-CRUD-Formular (`SaveAsync`, `EditAsync`, `DeleteAsync`, `ResetForm`), Sync-Orchestrierung (`RefreshAsync`, `RefreshAllAsync`, `SyncAsync`) sowie die komplette Feed-Suche inkl. Ergebnis-Abonnement (`SearchAsync`, `TryResolveSearchUrl`, `TryNormalizeDomainUrl`, `OfferDirectAddAsync`, `SubscribeResultAsync`, `CloseSearchResults` plus drei Commands und fünf eigene Zustandsfelder, zusammen ~200 neue Zeilen).

  Empfehlung: Den Such-Block in eine eigene Klasse extrahieren, z. B. ein `FeedSearchViewModel`/Child-ViewModel, an das `FeedsViewModel` delegiert, oder mindestens in eine eigene `partial`-Datei (`FeedsViewModel.Search.cs`) auslagern, damit die Verantwortlichkeit greifbar getrennt bleibt.

- **Fehlende Validierung von Vorbedingungen** — `SearchAsync` (Z. 508–571) kann veraltete Ergebnisse anzeigen: Der `NewUrl`-Setter (Z. 122–134) räumt bei jeder Eingabeänderung den Suchzustand ab, das `Entry` bleibt aber während `IsSearching` editierbar. Läuft eine Suche, während der Benutzer die URL ändert, schreibt der abschließende `await` (Z. 537–544) die Treffer der **alten** Query zurück in `SearchResults` und blendet die Ergebnisliste wieder ein (`ShowSearchResults = true`).

  Empfehlung: Nach dem `await` prüfen, ob `NewUrl.Trim()` noch dem gesuchten `input` entspricht, und die Ergebnisse sonst verwerfen; alternativ das `Entry` per `IsEnabled="{Binding IsSearching, Converter=...}"` während der Suche sperren.

- **Namenskonventionen und Einheitlichkeit** — Dasselbe Konzept ist in derselben Datei zweifach geschrieben: `SaveAsync` setzt `HealthStatus = "OK"` (Z. 375, String-Literal), das neue `SubscribeResultAsync` verwendet `HealthStatus = FeedHealth.Ok` (Z. 644, Konstante aus `FeedHealth.cs`).

  Empfehlung: In `SaveAsync` ebenfalls `FeedHealth.Ok` verwenden, damit innerhalb der Datei einheitlich auf die Konstante zugegriffen wird.

### FeedsPage.xaml / FeedsViewModel.cs

- **Doppelter Code** — Der Offline-Hinweis `FeedSearchOfflineHint` ist zweimal verdrahtet: einmal als permanentes `Label` per `DataTrigger` auf `IsOnline == false` (XAML Z. 94–103) und zusätzlich über `SearchErrorMessage = AppResources.FeedSearchOfflineHint` im Offline-Pfad von `SearchAsync` (Z. 516–520). Der Pfad ist über die UI weitgehend unerreichbar, weil `SearchCommand.CanExecute` bereits `IsOnline` prüft (Z. 69) — sollte er dennoch auslösen (z. B. `ReturnCommand`), steht derselbe Text zweimal übereinander im Formular.

  Empfehlung: Auf einen Mechanismus reduzieren. Konkret: das permanente Label behalten und im `!IsOnline`-Zweig von `SearchAsync` nur `return` ausführen (Test `SearchCommand_WhenOffline_DoesNotCallService_AndSetsHint` entsprechend auf „kein Service-Aufruf, kein zusätzlicher Fehlertext" anpassen), oder umgekehrt das Label entfernen und die `IsOnline`-Bedingung aus `CanExecute` streichen, damit der Fehlerkanal tatsächlich greift.

### FeedSearchService.cs (FeedSearchService)

- **Fehlerbehandlung / fachliche Korrektheit** — Bei Redirects wird die ursprüngliche statt der finalen URL gespeichert: `DiscoverFeedsAsync` setzt bei einem Feed-Dokument `FeedUrl = query` (Z. 179), `ProbeStandardPathsAsync` setzt `FeedUrl = candidate.AbsoluteUri` (Z. 233). Folgt der Server einem Redirect (häufig: `http→https`, Trailing-Slash, `www`), landet die weiterleitende Adresse im Abo und wird bei jedem Sync erneut verfolgt; zudem kann die FeedUrl-Dedupe (Z. 102) Vor-/Nach-Redirect-Dubletten nicht zusammenführen.

  Empfehlung: Als `FeedUrl` die finale Request-URI verwenden, also `response.RequestMessage?.RequestUri?.AbsoluteUri ?? query` bzw. `?? candidate.AbsoluteUri`.

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
