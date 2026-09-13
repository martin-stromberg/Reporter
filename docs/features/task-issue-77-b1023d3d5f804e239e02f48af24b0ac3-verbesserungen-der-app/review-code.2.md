<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FakeFeedIconService.cs (FakeFeedIconService)

- **Doppelter Code** — `TryFindFaviconUrlAsync` (Zeilen 39–57) repliziert die Site-URL-Auflösung und Fehlerisolierung aus `FeedIconService.TryFindFaviconUrlAsync` (`src/Reporter.Core/Services/FeedIconService.cs` Zeilen 86–105) nahezu wörtlich: gleicher `IsNullOrWhiteSpace`-Check, gleicher Authority-Fallback via `Uri.TryCreate` + `GetLeftPart(UriPartial.Authority)`, gleiches schluckendes `catch (Exception)` mit `return null`. Läuft die Auflösungslogik im Produktivcode auseinander (z. B. anderer Fallback), merkt es kein Test — die Caller-Tests prüfen dann gegen das veraltete Verhalten des Fakes.

  Empfehlung: Die gemeinsame Auflösung in einen gemeinsam genutzten Helper auslagern (z. B. `internal static string? FeedSiteResolver.Resolve(string feedUrl, string? siteUrl)` in `Reporter.Core`), den sowohl `FeedIconService` als auch der Fake aufrufen; alternativ den Fake vereinfachen, sodass `TryFindFaviconUrlAsync` nur die übergebenen Argumente aufzeichnet und an `FindFaviconUrlAsync` delegiert — die Auflösung selbst ist bereits durch `FeedIconServiceTests` abgedeckt.

### FeedSyncService.cs (FeedSyncService)

- **Methodenname beschreibt die Methode nicht mehr** — `UpdateFeedHealthAsync` (ab ca. Zeile 280) führt seit dieser Änderung zusätzlich die Favicon-Netzwerksuche durch (`TryFindFaviconUrlAsync` mit `_feedIconService`, Zeilen 292–296). Ein Aufrufer erwartet hinter dem Namen ein reines Persistenz-Update des Health-Status, nicht einen HTTP-Lookup als versteckten Seiteneffekt.

  Empfehlung: Die Favicon-Auflösung in den Aufrufer `SyncFeedAsync` vorziehen (dort ist `syndicationFeed` ohnehin bekannt) und der Persistenzmethode die bereits aufgelöste `faviconUrl` übergeben, oder die Methode passend umbenennen (z. B. `PersistSyncedFeedAsync`). Nebenbei: `TryFindFaviconUrlAsync` nimmt den gesamten `Feed` entgegen, nutzt aber nur `feed.Url` — ein `string`-Parameter würde die tatsächliche Abhängigkeit zeigen.

## Geprüfte Dateien

Liste aller geprüften Dateien (Basisbranch `origin/staging`; Implementierungsänderungen liegen uncommitted im Working Tree vor, `docs/`-Änderungen und Planungsartefakte ausgenommen):

- `src/Reporter.Core/Interfaces/IItemRepository.cs`
- `src/Reporter.Core/Interfaces/IFeedIconService.cs`
- `src/Reporter.Core/Models/Feed.cs`
- `src/Reporter.Core/Models/FeedAvatar.cs`
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
- `src/Reporter.Tests/FeedListItemTests.cs`
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
