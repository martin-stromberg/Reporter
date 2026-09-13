<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedsViewModel.cs (FeedsViewModel)

- **Doppelter Code** — Die beiden catch-Blöcke in `SearchAsync` (Z. 571–581 und Z. 582–593) sind bis auf das `Debug.WriteLine` identisch: beide setzen `SearchErrorMessage`, leeren `SearchResults`, verstecken die Ergebnisliste und rufen bei `isDirectUrl` `OfferDirectAddAsync()` auf. Zudem wird das Muster `if (isDirectUrl) { await OfferDirectAddAsync(); }` dreimal wiederholt (Z. 566–569, 577–580, 589–592).

  Empfehlung: Zu einem einzigen `catch (Exception ex)` zusammenführen und die Art der Exception nur für das Logging unterscheiden (`if (ex is not FeedSearchUnavailableException) Debug.WriteLine(...)`), oder eine gemeinsame `HandleSearchFailureAsync()`-Methode extrahieren.

- **God-Methode** — `SearchAsync` (Z. 502–598) umfasst ~97 Zeilen und erledigt mehrere konzeptuell getrennte Aufgaben: Eingabe-Validierung, URL-Normalisierung, Definition einer lokalen Funktion für den Direct-Add-Dialog, Service-Aufruf sowie dreifache Fehlerbehandlung. Die lokale Funktion `OfferDirectAddAsync` wird mitten in der Methodenlogik deklariert, was den Fluss zusätzlich erschwert.

  Empfehlung: `OfferDirectAddAsync` als eigene private Methode auslagern und die Fehlerbehandlung (s. o.) zusammenführen; ggf. die Eingabe-Normalisierung in eine eigene Methode `ResolveSearchUrl` extrahieren.

- **Fehlerbehandlung** — Der allgemeine `catch (Exception)`-Block in `SearchAsync` (Z. 582) fängt auch Exceptions, die `OfferDirectAddAsync`/`ConfirmDirectAddAsync` selbst im try-Block (Z. 568) wirft, und ruft `OfferDirectAddAsync` dann im catch erneut auf — ein Fehler im Dialog führt so zu einem doppelten Dialog. Wirft `OfferDirectAddAsync` im catch-Block selbst, propagiert die Exception unbehandelt aus dem Command.

  Empfehlung: `OfferDirectAddAsync` aus dem try-Block herauslösen (nach dem catch ausführen, z. B. über ein Flag `offerDirectAdd`) oder den Aufruf im catch mit eigenem Schutz versehen.

### FeedSearchService.cs (FeedSearchService)

- **Fehlerbehandlung** — Die `catch (Exception)`-Blöcke in `SearchAsync` (Z. 76–79 und Z. 87–90) schlucken auch `OperationCanceledException`, die durch das vom Aufrufer übergebene `cancellationToken` ausgelöst wird. Ein externer Abbruch wird dadurch nicht als Abbruch propagiert, sondern — je nach Kombination — als `FeedSearchUnavailableException` (Z. 93–96) oder als leere Ergebnisliste kaschiert. Das `cancellationToken` verliert damit seine Bedeutung.

  Empfehlung: Abbruch durch den Aufrufer vom internen Zeitbudget unterscheiden, z. B. `catch (Exception) when (!cancellationToken.IsCancellationRequested)` oder `OperationCanceledException` rethrowen, wenn `cancellationToken.IsCancellationRequested` gilt (das eigene `timeout`-Token darf weiterhin als Quellen-Fehler behandelt werden).

### FeedsPage.xaml.cs (FeedsPage)

- **Toter Code** — Der Parameter `url` von `ConfirmDirectAddAsync` (Z. 113) ist dokumentiert („The URL the user entered"), wird aber im Methodenrumpf nicht verwendet: Der Dialog (`FeedSearchNoResultsAddUrl`) nennt die betroffene URL nicht. Der Parameter ist damit deklariert, aber ungenutzt.

  Empfehlung: Entweder die URL in der Dialog-Nachricht anzeigen (`{0}`-Platzhalter in `FeedSearchNoResultsAddUrl` ergänzen und `string.Format` wie in `OnSearchResultTapped` verwenden) oder die Signatur auf `Func<Task<bool>>` vereinfachen.

### AppResources.resx / AppResources.de.resx / AppResources.Designer.cs

- **Toter Code** — Der Ressourcen-Schlüssel `PlaceholderFeedUrl` wird seit der Umstellung des `NewUrl`-Entry auf `PlaceholderFeedSearch` (FeedsPage.xaml Z. 22) nirgends mehr referenziert, existiert aber weiterhin in beiden .resx-Dateien (Z. 134) und im Designer (Z. 401–403).

  Empfehlung: `PlaceholderFeedUrl` aus `AppResources.resx`, `AppResources.de.resx` und `AppResources.Designer.cs` entfernen.

### FeedsViewModelTests.cs (FeedsViewModelTests)

- **Testqualität** — `SearchCommand_NoResultsAndValidUrl_InvokesConfirmDirectAdd` prüft zwei fachliche Fälle in einer Methode: den bestätigten Dialog (Titel-Vorbelegung aus dem Host) und den abgelehnten Dialog (keine Vorbelegung, `ShowSearchResults` bleibt aus) über zwei separat aufgebaute ViewModels.

  Empfehlung: In zwei Tests aufteilen, z. B. `..._Confirmed_PrefillsTitleFromHost` und `..._Declined_KeepsFormState`.

### FeedSearchServiceTests.cs (FeedSearchServiceTests)

- **Testqualität** — `SearchAsync_DirectoryAndDiscoveryOverlap_DedupesByFeedUrl` (Z. 256–286) prüft zwei getrennte Szenarien mit zwei separaten `StubHttpMessageHandler`-Setups: Dedupe innerhalb der Directory-Antwort und Dedupe innerhalb der Autodiscovery-Treffer.

  Empfehlung: In zwei Tests aufteilen (Directory-Dedupe und Discovery-Dedupe).

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
