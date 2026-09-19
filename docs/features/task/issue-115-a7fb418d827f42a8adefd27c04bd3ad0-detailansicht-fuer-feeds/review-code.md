<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedDetailViewModel.cs (FeedDetailViewModel)

- **Fehlerbehandlung** — `SaveEditAsync` (Z. 630–661) ruft `_feedRepository.GetByUrlAsync` und `_feedRepository.UpdateAsync` ohne Fehlerbehandlung auf. `SaveEditCommand` ist direkt im XAML gebunden (`FeedDetailPage.xaml` Z. 321–323): Wirft das Repository, faultet der `AsyncRelayCommand`-`ExecutionTask` unbeobachtet — das Sheet bleibt offen, ohne dass `ErrorMessage` gesetzt wird, der Nutzer sieht keinen Hinweis. Dieselbe Lücke hat `ReloadFeedAsync` (Z. 595–598): `GetAllWithDetailsAsync` kann werfen; der Aufruf aus `RefreshAsync` (Z. 600–607) läuft über den XAML-gebundenen `RefreshView.Command`-Pfad (Pull-to-Refresh) am Page-Catch in `OnFeedActionsClicked` vorbei — ebenfalls unbeobachteter faulted Task. Das im Branch etablierte Muster (`LoadAsync` Z. 310–320, `LoadPageCoreAsync` Z. 583–588) fängt Repository-Fehler und meldet `ErrorLoadFailed`; die beiden neuen Methoden weichen davon ab. (Die Aktionsmethoden `RenameFeedAsync`, `ChangeFeedCategoryAsync` und `DeleteFeedAsync` sind über den try/catch in `FeedDetailPage.OnFeedActionsClicked` abgedeckt; `ToggleSavedAsync`/`MarkReadAsync` folgen dem Bestandsmuster aus `LaterViewModel`/`UnreadViewModel`.)

  Empfehlung: `SaveEditAsync` mit try/catch absichern und bei Fehler `ErrorMessage = AppResources.ErrorActionFailed` setzen (Sheet bleibt offen, Fehler erscheint im Sheet-Label); `ReloadFeedAsync` bzw. `RefreshAsync` ebenfalls mit try/catch versehen und `ErrorMessage = AppResources.ErrorLoadFailed` setzen.

### FeedDbAssertions.cs (FeedDbAssertions)

- **Doppelter Code** — `MarkAllItemsReadAsync` (Z. 204–237) und `DeleteAllFeedsAsync` (Z. 249–285) enthalten jeweils ~25 Zeilen identischen Retry-Gerüsts: `File.Exists`-Guard, 5-Versuche-Schleife, `SqliteConnection` mit `Default Timeout=5;Pooling=False`, `catch (SqliteException)` mit Re-Throw beim letzten Versuch und `Task.Delay(500)`. Einziger Unterschied ist der `CommandText`.

  Empfehlung: Das Gerüst in eine private Hilfsmethode auslagern, z. B. `ExecuteWriteWithRetryAsync(string databasePath, string commandText)`, die beide öffentlichen Methoden aufrufen.

### FakeFeedSyncService.cs (FakeFeedSyncService)

- **Einheitlichkeit / Hardcodierter Wert** — `SyncFeedResult` wird mit `new("OK", 0)` (Z. 51) initialisiert, während `SyncAllResult` dreißig Zeilen weiter oben (Z. 17) `new(FeedHealth.Ok, 0)` verwendet — für dasselbe Konzept (der `SyncResult.Status`-String) einmal das Konstanten-Symbol und einmal das Magic-String-Literal.

  Empfehlung: `SyncFeedResult` ebenfalls mit `FeedHealth.Ok` initialisieren.

## Verifizierte Fixes aus Iteration 2

Alle 4 Befunde des zweiten Reviews wurden adressiert, plus der Usability-Fix:

1. `RefreshAsync` nutzt den `bool`-Rückgabewert von `RunFeedSyncAsync` — `ReloadFeedAsync`/`RestartListAsync` laufen nur bei tatsächlich ausgeführtem Sync (`FeedDetailViewModel.cs` Z. 600–607); Test `RefreshCommand_WhenOffline_DoesNotReloadList` belegt, dass keine Repository-Abfrage und kein Listen-Reset erfolgt.
2. `DebounceRestartList` disposed die abgebrochene `CancellationTokenSource` direkt nach `Cancel()` (Z. 491–500).
3. `FeedsViewModel.LoadAsync` hat try/catch mit `ErrorMessage = AppResources.ErrorLoadFailed` (`FeedsViewModel.cs` Z. 190–207); zwei neue Tests (`LoadCommand_WhenFeedRepositoryFails_…`, `LoadCommand_WhenCategoryRepositoryFails_…`) belegen das Verhalten.
4. Der private `FakeFeedSyncService` in `FeedsViewModelTests` ist entfernt; die Tests nutzen den Shared-Fake mit neuer `SyncAllResult`-Property; `ThrowingFeedRepository`/`ThrowingCategoryRepository` sind als Shared-Fakes angelegt und in beiden Testklassen im Einsatz.
5. Usability-Fix umgesetzt: `EmptyView` der Detail-Liste unterscheidet per `DataTrigger` auf `SearchText` zwischen `PlaceholderFeedDetail` und `FeedDetailSearchNoResults` (`FeedDetailPage.xaml` Z. 218–231); Ressourcen in beiden resx-Dateien und im Designer vorhanden.

## Hinweise

- Der Lifecycle-Hinweis zu `RaiseUiActionRequested`-Handlern in Blazor-Komponenten ist nicht anwendbar (.NET MAUI-Projekt).
- Die Implementierung liegt uncommitted im Arbeitsstand vor; geprüft wurden alle geänderten und neuen Quelldateien laut `git status`. Es lief kein `Reporter.exe`-Prozess, der den Build blockiert hätte.
- Kompilations-Check: `Reporter.Tests` und `Reporter.E2ETests` bauen fehlerfrei (0 Warnungen, 0 Fehler).
- Kleinigkeiten ohne Befund-Charakter: Die Klassen-Zusammenfassung von `E2EPageHelpers` (Z. 11–13) nennt als Nutzer nur `SmokeTests` und `ArticleLinkTests` — seit diesem Branch auch `FeedDetailTests`. In `SmokeTests.cs` (Z. 141–144) steht das Komma des `Assert.True`-Aufrufs auf einer eigenen Zeile — unübliche, aber kompilierende Formatierung.
- Keine toten Referenzen: Alle Member des Shared-Fake (`SyncAllResult`, `SyncAllBlocker`, `SyncFeedResult`, `SyncFeedException`, Zähler) werden genutzt; die entfernten `FeedsViewModel`-Member (`IsEditMode`, `SelectedFeed`, `NewTitle`, `FeedNotificationsEnabled`, `SaveCommand`) haben keine Referenzen mehr.
- `test-results/` ist über `.gitignore` ausgeschlossen; die E2E-Fixtures `paged-feed.xml`/`scroll-feed.xml` nutzen den `{name}`-Platzhalter des `StubFeedServer` korrekt (Fallback auf `stub-feed.xml` für unbekannte Namen greift für `nav-target` u. a.).

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Reporter.Core/Interfaces/IItemRepository.cs`
- `src/Reporter.Core/Services/FeedUrlValidator.cs`
- `src/Reporter.Core/ViewModels/BaseViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter.Data/Repositories/ItemRepository.cs`
- `src/Reporter/Views/FeedDetailPage.xaml`
- `src/Reporter/Views/FeedDetailPage.xaml.cs`
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter/AppShell.xaml.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter.Tests/FeedDetailViewModelTests.cs`
- `src/Reporter.Tests/FeedsViewModelTests.cs`
- `src/Reporter.Tests/ItemRepositoryTests.cs`
- `src/Reporter.Tests/DelegatingItemRepository.cs`
- `src/Reporter.Tests/FakeFeedSyncService.cs`
- `src/Reporter.Tests/ThrowingFeedRepository.cs`
- `src/Reporter.Tests/ThrowingCategoryRepository.cs`
- `src/Reporter.E2ETests/FeedDetailTests.cs`
- `src/Reporter.E2ETests/E2EPageHelpers.cs`
- `src/Reporter.E2ETests/FeedDbAssertions.cs`
- `src/Reporter.E2ETests/SmokeTests.cs`
- `src/Reporter.E2ETests/UiRetry.cs`
- `src/Reporter.E2ETests/Fixtures/paged-feed.xml`
- `src/Reporter.E2ETests/Fixtures/scroll-feed.xml`
