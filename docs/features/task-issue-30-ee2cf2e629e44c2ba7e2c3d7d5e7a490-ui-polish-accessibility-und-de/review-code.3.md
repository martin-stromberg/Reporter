<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### `LaterViewModelTests.cs` (`FailingItemRepository`, `GatedItemRepository`)

- **Doppelter Code** — `LaterViewModelTests.FailingItemRepository` (ca. Zeilen 451–525) ist nahezu identisch mit `UnreadViewModelTests.FailingItemRepository` (ca. Zeilen 521–590): Beide implementieren `IItemRepository` vollständig als reine Delegation an ein inneres Repository (~16 Member, ~75 Zeilen) und unterscheiden sich nur darin, welche einzelne Methode fehlschlägt (`GetSavedForLaterAsync` vs. `GetUnreadByDateAsync`). Zusätzlich wiederholt `GatedItemRepository` (ca. Zeilen 375–448) in derselben Datei denselben Delegations-Boilerplate ein drittes Mal. Insgesamt ~200 Zeilen kopierte Interface-Delegation; jede zukünftige `IItemRepository`-Änderung muss an drei Stellen gepflegt werden (diese Iteration musste `GetSavedForLaterAsync(int, int)`/`AddRangeAsync` bereits in allen drei Fakes nachziehen).

  Empfehlung: Einmalige Basisklasse `DelegatingItemRepository : IItemRepository` im Testprojekt (z. B. eigene Datei `src/Reporter.Tests/DelegatingItemRepository.cs`) mit `virtual`-Membern, die an `_inner` weiterreichen. `FailingItemRepository` und `GatedItemRepository` erben davon und überschreiben nur die jeweils relevante Methode.

## Geprüfte Dateien

Liste aller geprüften Dateien (gesamter Diff des Working Tree gegen `HEAD`):

- `src/Reporter.Core/Interfaces/IItemRepository.cs`
- `src/Reporter.Core/Models/CategoryFilterItem.cs`
- `src/Reporter.Core/Models/FeedSearchResult.cs`
- `src/Reporter.Core/Models/ItemListItem.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
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
- `src/Reporter/Views/LaterPage.xaml.cs`
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/UnreadPage.xaml`
- `src/Reporter/Views/UnreadPage.xaml.cs`
- `docs/help/anwendung/datenmodell.md`
- `docs/help/anwendung/mobile-ui-design.md`
- `docs/help/benachrichtigungen/ablauf-technisch.md`
