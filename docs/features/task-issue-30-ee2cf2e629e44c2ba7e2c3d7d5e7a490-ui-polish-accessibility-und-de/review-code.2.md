<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### LaterViewModel.cs (LaterViewModel)

- **Fehlerbehandlung** — `LoadPageCoreAsync` (Zeile 146) und `LoadAsync` (Zeile 107) enthalten keinerlei Fehlerbehandlung. Schlägt `GetSavedForLaterAsync` fehl, landet die Exception nur in `AsyncRelayCommand.ExecutionTask`; `LoadAsync` hat `SavedItems` bereits per `Clear()` geleert und `HasMore = true` gesetzt — die Seite bleibt leer und ohne jeden Fehlerhinweis. Zusätzlich wird `LoadCommand` aus `LaterPage.OnAppearing` via `await viewModel.LoadCommand.ExecuteAsync(null)` in einer `async void`-Methode ohne try/catch aufgerufen (`UnreadPage.OnAppearing` fängt denselben Fall mit try/catch + `Debug.WriteLine` ab). Inkonsistent zu `UnreadViewModel.LoadPageAsync`, das Exceptions auffängt und eine lokalisierte `ErrorMessage` setzt.

  Empfehlung: Fehlerbehandlung analog `UnreadViewModel` ergänzen — `ErrorMessage`/`HasError`-Properties in `LaterViewModel`, `try/catch` um den Repository-Aufruf mit `AppResources.ErrorLoadFailed`, Fehler-Label in `LaterPage.xaml`. Alternativ zumindest `SavedItems.Clear()` erst nach erfolgreichem Laden der ersten Seite ausführen, damit ein Fehler die zuletzt angezeigten Daten nicht verwirft.

### FeedsPage.xaml

- **Doppelter Code** — Die neue Property `FeedSearchResult.DisplayTitle` (`Title` mit Fallback auf `FeedUrl`) implementiert denselben Fallback, den die Überschrift des Suchergebnis-Eintrags (Zeilen 83–91) bereits separat als `Text="{Binding FeedUrl}"` plus `DataTrigger` auf `Title` kodiert. Die Fallback-Regel liegt damit zweimal vor und müsste bei einer Änderung an zwei Stellen gepflegt werden.

  Empfehlung: Das Label direkt auf `Text="{Binding DisplayTitle}"` binden und den `DataTrigger` entfernen.

### AppResources.resx

- **Neue Zeichenfolge ohne Pluralbehandlung** — `AccessibilityCategoryFilter` („{0} filter, {1} unread articles, {2}") erzeugt bei `Count == 1` das grammatisch falsche „1 unread articles" (deutsche Variante „{1} ungelesene Artikel" ist für alle Werte korrekt). Der String ist Bestandteil der Screen-Reader-Ausgabe der Filter-Chips.

  Empfehlung: Formulierung plural-neutral wählen (z. B. „{0} filter, {1} unread, {2}") oder einen eigenen Singular-Ressourcenschlüssel ergänzen.

## Geprüfte Dateien

- `src/Reporter.Core/Interfaces/IItemRepository.cs`
- `src/Reporter.Core/Models/CategoryFilterItem.cs`
- `src/Reporter.Core/Models/FeedSearchResult.cs`
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

## Anmerkungen zur Prüfung

- Build (`src/Reporter.Tests`) erfolgreich ohne Warnungen; `dotnet test`: 348 Tests bestanden, 0 Fehler.
- `AGENTS.md`-Regeln geprüft: Touch-Targets ≥ 44 pt (Chips über `MinimumHeightRequest`, Aktionsleiste über 44×44-Grids mit transparentem `Button`), Dark Mode durchgehend per `AppThemeBinding`, keine verschachtelten `CollectionView`/`ScrollView` (horizontale Chip-Liste und Artikelliste sind Geschwister in einem `Grid`, keine ScrollView-Verschachtelung).
- Keine toten Code-Reste: `GetByGuidOrHashAsync`, `SelectedCategoryText`, `OnFilterClicked` und `dotnet_bot.png` sind vollständig entfernt und ohne verbleibende Referenzen; `MapToModel`/`MapToEntity` werden weiterhin genutzt.
