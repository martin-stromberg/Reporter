<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### LaterViewModel.cs (LaterViewModel)

- **Fehlerbehandlung / konkurrierende Ladezustände** — `LoadAsync` (Zeilen 106–112) resettet `_currentPage`, setzt `HasMore = true` und leert `SavedItems`, delegiert dann an `LoadMoreAsync` — das bei `IsLoading == true` sofort zurückkehrt (Zeilen 116–119). Da `LoadCommand` und `LoadMoreCommand` zwei unabhängige `AsyncRelayCommand`-Instanzen sind, kann ein noch laufender `LoadMoreAsync`-Aufruf (z. B. per `RemainingItemsThresholdReached` ausgelöst, kurz bevor `OnAppearing` erneut `LoadCommand` feuert) dazu führen, dass `LoadAsync` die Liste leert, aber nichts nachlädt; der in-flight laufende Aufruf hängt anschließend eine veraltete Seite an die geleerte Liste an und erhöht den bereits zurückgesetzten `_currentPage`-Zähler — Seiten gehen verloren bzw. die Liste zeigt eine mittlere Seite ohne die vorherigen Einträge.

  Empfehlung: In `LoadAsync` nicht auf das abgesicherte `LoadMoreAsync` delegieren, sondern das Laden direkt implementieren (`IsLoading` selbst setzen, Seite 0 laden — analog `UnreadViewModel.LoadPageAsync`, das keinen Early-Return bei `IsLoading` hat). Alternativ den Reset in `LoadMoreAsync` verschieben oder einen laufenden Load vor dem Neuladen abwarten/abbrechen.

### ItemRepository.cs (ItemRepository)

- **Doppelter Code** — Die anonyme `Select`-Projektion samt anschließendem `ItemListItem`-Mapping (inkl. `ExtractImageUrl`, `ExtractSummary`, `ReadingTimeEstimator.EstimateText`) ist in `GetUnreadByDateAsync(page, pageSize, categoryId)` (Zeilen 128–159) und `GetSavedForLaterAsync(page, pageSize)` (Zeilen 268–299) nahezu identisch dupliziert (~30 Zeilen). Beide Blöcke wurden in diesem Change um `ReadingTimeText` erweitert — künftige Projektionsänderungen müssen an zwei Stellen gepflegt werden.

  Empfehlung: Gemeinsame Projektion auslagern, z. B. eine `private static`-Hilfsmethode `MapToListItem`-Mapping für das anonyme Ergebnis oder eine geteilte `IQueryable`-Projektionsmethode, die beide Queries nach dem `Where`/`OrderBy`-Teil verwenden.

### LaterViewModel.cs / UnreadViewModel.cs (LaterViewModel, UnreadViewModel)

- **Doppelter Code / fehlende Kapselung** — `LaterViewModel.MarkReadAsync` (Zeilen 165–180) kopiert alle 14 Eigenschaften von `ItemListItem` manuell in eine `IsRead = true`-Kopie; `UnreadViewModel.ToggleSavedAsync` (Zeilen 418–433) enthält denselben Voll-Kopiervorgang mit `IsSavedForLater = !…`. Mit diesem Change existiert das Muster nun zweimal.

  Empfehlung: Kopierlogik auf `ItemListItem` kapseln — entweder die Klasse als `record` führen und `item with { IsRead = true }` nutzen, oder eine `CopyWith(bool? isRead = null, bool? isSavedForLater = null)`-Methode ergänzen.

### UnreadPage.xaml (UnreadPage)

- **Accessibility / hartcodierte Höhe** — Die Chip-`CollectionView` hat eine fixe `HeightRequest="44"` (Zeile 109). Da die neuen Typo-Styles explizit `FontAutoScalingEnabled = True` setzen, wächst der Pill-Inhalt (`LabelMdStyle`, 12 pt) bei vergrößerter Systemschrift über 44 pt hinaus und wird vertikal abgeschnitten — die äußere `Border` hat zwar `MinimumHeightRequest="44"`, kann aber nicht über die fixe Höhe der CollectionView hinaus wachsen.

  Empfehlung: `HeightRequest="44"` durch `MinimumHeightRequest="44"` ersetzen, damit die Chip-Leiste bei dynamischer Schrift mitwachsen kann.

## Geprüfte Dateien

- `src/Reporter.Core/Interfaces/IItemRepository.cs`
- `src/Reporter.Core/Models/ItemListItem.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/Services/ReadingTimeEstimator.cs` (neu)
- `src/Reporter.Core/ViewModels/LaterViewModel.cs`
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs`
- `src/Reporter.Data/Repositories/ItemRepository.cs`
- `src/Reporter.Tests/FeedSyncServiceTests.cs`
- `src/Reporter.Tests/ItemRepositoryTests.cs`
- `src/Reporter.Tests/LaterViewModelTests.cs`
- `src/Reporter.Tests/ReadingTimeEstimatorTests.cs` (neu)
- `src/Reporter.Tests/UnreadViewModelTests.cs`
- `src/Reporter/AppShell.xaml.cs`
- `src/Reporter/Reporter.csproj`
- `src/Reporter/Resources/AppIcon/appicon.svg`
- `src/Reporter/Resources/AppIcon/appiconfg.svg`
- `src/Reporter/Resources/Images/tab_categories.svg` (neu)
- `src/Reporter/Resources/Images/tab_feeds.svg` (neu)
- `src/Reporter/Resources/Images/tab_later.svg` (neu)
- `src/Reporter/Resources/Images/tab_settings.svg` (neu)
- `src/Reporter/Resources/Images/tab_unread.svg` (neu)
- `src/Reporter/Resources/Splash/splash.svg`
- `src/Reporter/Resources/Styles/Colors.xaml`
- `src/Reporter/Resources/Styles/Styles.xaml`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter/Views/ArticleCardView.xaml`
- `src/Reporter/Views/ArticleDetailPage.xaml`
- `src/Reporter/Views/CategoriesPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/LaterPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/UnreadPage.xaml`
- `src/Reporter/Views/UnreadPage.xaml.cs`
