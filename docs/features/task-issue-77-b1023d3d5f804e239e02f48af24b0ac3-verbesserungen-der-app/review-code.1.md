<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedsViewModel.Search.cs (FeedsViewModel) / FeedSyncService.cs (FeedSyncService)

- **Doppelter Code** — `TryFindFaviconUrlAsync` ist in beiden Klassen nahezu identisch implementiert (`FeedsViewModel.Search.cs` ca. Zeile 336–364, `FeedSyncService.cs` ca. Zeile 311–335): Beide Methoden lösen eine Site-URL auf (Fallback auf die Authority der Feed-URL via `Uri.TryCreate` + `GetLeftPart(UriPartial.Authority)`), rufen `_feedIconService.FindFaviconUrlAsync` auf und schlucken im `catch (Exception)` alle Fehler mit `Debug.WriteLine` + `return null`. Einziger Unterschied ist die Quelle der Site-URL (Syndication-`alternate`-Link vs. `FeedSearchResult.SiteUrl`).

  Empfehlung: Die gemeinsame Logik (Authority-Fallback + fehlerisolierter Icon-Lookup) an einer Stelle bündeln — z. B. `IFeedIconService.FindFaviconUrlAsync` um einen optionalen `feedUrl`-Fallback-Parameter erweitern oder eine gemeinsame Hilfsmethode im Core auslagern, sodass beide Aufrufer nur noch die Site-URL-Quelle liefern.

### FeedListItem.cs (FeedListItem) / ItemListItem.cs (ItemListItem)

- **Doppelter Code** — Die berechnete Eigenschaft `FeedInitial` (`string.IsNullOrWhiteSpace(x) ? "?" : x[..1].ToUpperInvariant()`) ist in beiden Modellklassen identisch dupliziert (`FeedListItem.cs` Zeile 68, `ItemListItem.cs` Zeile 72).

  Empfehlung: In eine gemeinsame Hilfsmethode auslagern (z. B. `internal static string Initial(string? title)` in einer kleinen Helper-Klasse in `Reporter.Core.Models`) oder zumindest den Befund bewusst dokumentieren.

### appiconfg.svg

- **Toter Code** — Der `<defs><style>`-Block (Zeilen 7–17) definiert die Klasse `.brand-text` und importiert per `@import` ein Google-Fonts-Stylesheet. Kein Element in der Datei referenziert `.brand-text` (das App-Icon enthält keinen Text). Zudem kann ein Remote-`@import` beim buildseitigen SVG-Rastering durch MAUI gar nicht aufgelöst werden.

  Empfehlung: Den kompletten `<defs>`-Block entfernen.

- **Asset-Geometrie** — `viewBox="0 0 400 460"`, aber der sichtbare Badge-Inhalt endet bei y≈370; die unteren ~90 px (ca. 20 % der Höhe) sind leer. Das Icon wird dadurch mit ungenutztem Freiraum gerendert bzw. wirkt nach oben verschoben (in `splash.svg` ist der untere Bereich dagegen durch den Schriftzug belegt und die Höhe begründet).

  Empfehlung: `viewBox`/`width`/`height` auf den tatsächlichen Inhalt beschneiden (z. B. `0 0 400 400`) oder den Badge im Viewport zentrieren.

### FeedsPage.xaml

- **Fehlende Behandlung des Offline-Falls** — Der `MultiTrigger` des Favicon-`Image` (ca. Zeile 174–183) verlangt zusätzlich `BindingContext.IsOnline == true`; der Initialen-Fallback-`Border` (ca. Zeile 186–198) wird jedoch nur bei leerem `FaviconUrl` eingeblendet. Ein Feed **mit** gespeichertem Favicon zeigt offline weder Icon noch Initialen — es bleibt eine leere 40×40-Fläche.

  Empfehlung: Den Fallback-Trigger so erweitern, dass er auch bei `IsOnline == false` greift (z. B. Initialen-Border sichtbar, wenn `FaviconUrl` leer **oder** die Seite offline ist — ggf. über einen zweiten `DataTrigger` auf denselben Setter).

## Geprüfte Dateien

Liste aller geprüften Dateien (Basisbranch `origin/staging`; Quellcode-Änderungen liegen uncommitted im Working Tree vor, `docs/` und `test-results*` ausgenommen):

- `src/Reporter.Core/Interfaces/IItemRepository.cs`
- `src/Reporter.Core/Interfaces/IFeedIconService.cs`
- `src/Reporter.Core/Models/Feed.cs`
- `src/Reporter.Core/Models/FeedListItem.cs`
- `src/Reporter.Core/Models/ItemListItem.cs`
- `src/Reporter.Core/Models/Settings.cs`
- `src/Reporter.Core/Models/SettingsValues.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Services/AutoRefreshService.cs`
- `src/Reporter.Core/Services/FeedIconService.cs`
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/Services/ReadingTimeEstimator.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/SortOrderOption.cs`
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs`
- `src/Reporter.Data/Entities/Feed.cs`
- `src/Reporter.Data/Entities/Settings.cs`
- `src/Reporter.Data/Migrations/20260913211308_AddFeedFaviconUrl.cs`
- `src/Reporter.Data/Migrations/20260913211308_AddFeedFaviconUrl.Designer.cs`
- `src/Reporter.Data/Migrations/20260913211351_AddSettingsStartupRefreshAndSortOrder.cs`
- `src/Reporter.Data/Migrations/20260913211351_AddSettingsStartupRefreshAndSortOrder.Designer.cs`
- `src/Reporter.Data/Migrations/ReporterDbContextModelSnapshot.cs`
- `src/Reporter.Data/ReporterDbContext.cs`
- `src/Reporter.Data/Repositories/FeedRepository.cs`
- `src/Reporter.Data/Repositories/ItemRepository.cs`
- `src/Reporter.Data/Repositories/SettingsRepository.cs`
- `src/Reporter.Tests/AutoRefreshServiceTests.cs`
- `src/Reporter.Tests/DelegatingItemRepository.cs`
- `src/Reporter.Tests/FakeFeedIconService.cs`
- `src/Reporter.Tests/FeedIconServiceTests.cs`
- `src/Reporter.Tests/FeedRepositoryTests.cs`
- `src/Reporter.Tests/FeedSyncServiceTests.cs`
- `src/Reporter.Tests/FeedsViewModelTests.cs`
- `src/Reporter.Tests/ItemRepositoryTests.cs`
- `src/Reporter.Tests/KeywordFilterTests_E2E.cs`
- `src/Reporter.Tests/ReadingTimeEstimatorTests.cs`
- `src/Reporter.Tests/RetentionCleanupServiceTests.cs`
- `src/Reporter.Tests/ServiceCollectionTests.cs`
- `src/Reporter.Tests/SettingsRepositoryTests.cs`
- `src/Reporter.Tests/SettingsViewModelTests_E2E.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Load.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Persist.cs`
- `src/Reporter.Tests/TestSettingsHelper.cs`
- `src/Reporter.Tests/UnreadViewModelTests.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter/Resources/AppIcon/appiconfg.svg`
- `src/Reporter/Resources/Splash/splash.svg`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter/Views/ArticleCardView.xaml`
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml`
