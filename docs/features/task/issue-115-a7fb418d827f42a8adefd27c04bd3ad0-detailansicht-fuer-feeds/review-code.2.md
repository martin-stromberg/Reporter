<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedDetailViewModel.cs (FeedDetailViewModel)

- **Einheitlichkeit / Fehlerbehandlung** — `RefreshAsync` (Z. 596–601) ignoriert den `bool`-Rückgabewert von `RunFeedSyncAsync`: Auch wenn der Sync übersprungen wurde (Gerät offline oder bereits laufender Sync), laufen `ReloadFeedAsync` und `RestartListAsync` anschließend trotzdem — die Liste wird geleert und die erste Seite neu aus der Datenbank geladen (Scrollposition geht verloren, unnötige Abfragen). `FeedsViewModel.SyncAsync` (Z. 214–220) prüft denselben Rückgabewert und überspringt das Nachladen bei `false`; die beiden ViewModels nutzen die neue Basismethode damit inkonsistent.

  Empfehlung: `RefreshAsync` an das Muster in `FeedsViewModel.SyncAsync` angleichen — `if (await RunFeedSyncAsync(() => _feedSyncService.SyncFeedAsync(_feedId))) { await ReloadFeedAsync(); await RestartListAsync(); }`.

- **Klassischer Code Smell / Ressourcen-Handling** — `DebounceRestartList` (Z. 491–496) legt pro Tastenschlag eine neue `CancellationTokenSource` an; die abgebrochene Vorgänger-Quelle wird nur gecancelt, nie disposed, und auch die letzte aktive Quelle wird nach Ablauf des Debounce-Tasks nicht freigegeben.

  Empfehlung: Die ersetzte Quelle direkt nach `Cancel()` disposen (`var previous = _searchDebounceSource; _searchDebounceSource = new CancellationTokenSource(); previous?.Cancel(); previous?.Dispose();`) — nach `Cancel()` ist das `Task.Delay` der laufenden Warte bereits aufgelöst, das Dispose ist an dieser Stelle sicher.

### FeedsViewModel.cs (FeedsViewModel)

- **Fehlerbehandlung** — `LoadAsync` (Z. 188–194) ruft `LoadCategoriesWithNoneAsync(_categoryRepository)` und `_feedRepository.GetAllWithDetailsAsync()` weiterhin ohne Fehlerbehandlung auf. Wirft ein Repository, faultet der `AsyncRelayCommand`-`ExecutionTask` unbeobachtet — der Nutzer sieht weder `ErrorMessage` noch einen Hinweis, die Liste bleibt stumm im alten Stand. Exakt dieses Problem wurde in Iteration 1 für `FeedDetailViewModel.LoadAsync` bemängelt und dort behoben (try/catch + `ErrorLoadFailed`); die gleichermaßen im Branch geänderte `FeedsViewModel`-Variante weicht nun vom etablierten Muster ab. (Der Befund ist älter als der Branch, betrifft aber die hier geänderte Methode.)

  Empfehlung: `LoadAsync` mit try/catch absichern und bei Fehler `ErrorMessage = AppResources.ErrorLoadFailed` setzen — analog zum gefixten `FeedDetailViewModel.LoadAsync` (Z. 310–320).

### FeedsViewModelTests.cs (FeedsViewModelTests)

- **Doppelter Code** — Die private Klasse `FakeFeedSyncService` (Z. 1056–1097) bleibt als paralleler zweiter `IFeedSyncService`-Fake neben dem gemeinsamen `Reporter.Tests.FakeFeedSyncService` (`src/Reporter.Tests/FakeFeedSyncService.cs`) bestehen, der in diesem Branch gerade erweitert wurde (`SyncFeedCallCount`, `LastSyncFeedId`, `SyncFeedResult`, `SyncFeedException`) und vom neuen `FeedDetailViewModelTests` genutzt wird. Damit existieren im selben Testprojekt zwei handgeschriebene Fakes mit überlappender Funktionalität (`LastFeedId` ↔ `LastSyncFeedId`, `NextException` ↔ `SyncFeedException`/`SyncAllException`).

  Empfehlung: Den gemeinsamen Fake um ein konfigurierbares `SyncAllResult` ergänzen (einzige fehlende Fähigkeit für `FeedsViewModelTests`, siehe Z. 171) und die private Nested-Klasse entfernen; dieselbe Konsolidierung wäre perspektivisch auch für den identisch aufgebauten privaten Fake in `UnreadViewModelTests` (unveränderte Datei) sinnvoll.

## Verifizierte Fixes aus Iteration 1

Alle 9 Befunde des ersten Reviews wurden adressiert:

1. `SyncAsync`-Duplikat → `BaseViewModel.RunFeedSyncAsync` (Z. 147–188) mit `SetIsSyncing`/`ReportSyncError`-Hooks; beide VMs nutzen ihn.
2. `IsValidFeedUrl`-Duplikat → neue statische `FeedUrlValidator` (`src/Reporter.Core/Services/FeedUrlValidator.cs`), in `FeedDetailViewModel` (Z. 633) und `FeedsViewModel.Search.cs` (Z. 203) verwendet.
3. Kategorie-Pseudo-Eintrag → `BaseViewModel.LoadCategoriesWithNoneAsync` (Z. 197–206), von beiden VMs genutzt.
4. Reset-Block-Duplikat → `ResetAndLoadFirstPageAsync` (Z. 525–540), aus `LoadAsync` und `RestartListAsync` aufgerufen.
5. `FeedDetailViewModel.LoadAsync` — Header-Ladevorgänge jetzt in try/catch mit `ErrorMessage = ErrorLoadFailed` (Z. 310–320); mit drei neuen Fehler-Tests belegt.
6. `SearchText`-Debounce → `DebounceRestartList`/`RestartListDebouncedAsync` mit 300-ms-CTS (Z. 491–513); Test `SearchText_RapidChanges_DebouncesListRestart` belegt genau einen Repository-Aufruf.
7. `OnFeedActionsClicked` u. a. — Handler komplett in try/catch, Fehler via `ErrorActionFailed` auf der Seite sichtbar (Z. 98–162); neue Ressource angelegt.
8. `ApplyQueryAttributes` — setzt `ErrorMessage = ErrorLoadFailed` bei fehlendem/ungültigem `feedId` (Z. 36–47).
9. `EditUrlEntry`-Mehrdeutigkeit → neue Ressource `PlaceholderFeedEditUrl` (XAML Z. 267–268); `WaitForEditUrlEntry` in `E2EPageHelpers` ohne Workaround.
10. Screenreader-Hint der Feed-Karte → neue Ressource `AccessibilityOpenFeedDetails` (`FeedsPage.xaml` Z. 153); `AccessibilityTapForActions` bleibt korrekt an der Suchtreffer-Karte.
11. E2E-Helfer konsolidiert — `WaitForElementByName`/`WaitForElementInScopeByName`/`TryFindElementInScopeByName`, `AddFeedViaUi`, `DismissPopups`, `ScrollListDown`, `WaitForFeedCard` u. a. leben jetzt in `E2EPageHelpers`/`UiRetry` und werden von `SmokeTests` und `FeedDetailTests` geteilt.
12. `MarkAllItemsReadAsync` wirft die `SqliteException` nach dem letzten Retry weiter (`FeedDbAssertions.cs` Z. 220–224).

## Hinweise

- Der Lifecycle-Hinweis zu `RaiseUiActionRequested`-Handlern in Blazor-Komponenten ist nicht anwendbar (.NET MAUI-Projekt).
- Die Implementierung liegt uncommitted im Arbeitsstand vor; geprüft wurden alle geänderten und neuen Quelldateien laut `git status`.
- Kompilations-Check: `Reporter.Tests` und `Reporter.E2ETests` bauen fehlerfrei (0 Warnungen, 0 Fehler). Beim `Reporter`-Projekt schlug lediglich das Kopieren der `apphost.exe` fehl, weil eine `Reporter.exe`-Instanz (PID 24964) lief — ein Umgebungsproblem, keine Code-Verletzung; die Kompilierung selbst war fehlerfrei.
- `IsEditMode`, `SelectedFeed`, `NewTitle`, `FeedNotificationsEnabled`, `SaveCommand` (FeedsViewModel) und die zugehörigen XAML-Blöcke sind restlos entfernt — keine toten Referenzen gefunden.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Reporter.Core/Interfaces/IItemRepository.cs`
- `src/Reporter.Core/Services/FeedUrlValidator.cs` (neu)
- `src/Reporter.Core/ViewModels/BaseViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs` (neu)
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter.Data/Repositories/ItemRepository.cs`
- `src/Reporter/Views/FeedDetailPage.xaml` (neu)
- `src/Reporter/Views/FeedDetailPage.xaml.cs` (neu)
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter/AppShell.xaml.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter.Tests/FeedDetailViewModelTests.cs` (neu)
- `src/Reporter.Tests/FeedsViewModelTests.cs`
- `src/Reporter.Tests/ItemRepositoryTests.cs`
- `src/Reporter.Tests/DelegatingItemRepository.cs`
- `src/Reporter.Tests/FakeFeedSyncService.cs`
- `src/Reporter.E2ETests/FeedDetailTests.cs` (neu)
- `src/Reporter.E2ETests/E2EPageHelpers.cs`
- `src/Reporter.E2ETests/FeedDbAssertions.cs`
- `src/Reporter.E2ETests/SmokeTests.cs`
- `src/Reporter.E2ETests/UiRetry.cs`
- `src/Reporter.E2ETests/Fixtures/paged-feed.xml` (neu, Fixture)
- `src/Reporter.E2ETests/Fixtures/scroll-feed.xml` (neu, Fixture)
