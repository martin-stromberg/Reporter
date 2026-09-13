<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedSyncService.cs (FeedSyncService)

- **Fehlerbehandlung / fehlende Cancellation-Weitergabe** — `RunSyncAsync` reicht den `cancellationToken` an alle anderen asynchronen Aufrufe weiter (`_httpClient.GetStreamAsync`, `Task.Run(... SyndicationFeed.Load ...)`, `NotifyNewItemsAsync`), aber die neue private Methode `TryFindFaviconUrlAsync` (Zeile 311) nimmt keinen Token entgegen und ruft `_feedIconService.TryFindFaviconUrlAsync(feedUrl, siteUrl)` (Zeile 317) ohne ihn auf — obwohl die Interface-Signatur einen `CancellationToken`-Parameter anbietet. Eine abgebrochene Synchronisation wartet dadurch noch bis zu zwei HTTP-Roundtrips (je bis zum 30-s-Client-Timeout) ab, bevor sie zurückkehrt. Die Weitergabe ist sicher: Der Icon-Service schluckt `OperationCanceledException` intern und liefert dann `null` (durch `FeedIconServiceTests.TryFindFaviconUrlAsync_WhenCancelled_ReturnsNull` vertraglich abgesichert).

  Empfehlung: `TryFindFaviconUrlAsync` um einen `CancellationToken`-Parameter ergänzen und an `_feedIconService.TryFindFaviconUrlAsync(feedUrl, siteUrl, cancellationToken)` sowie am Aufruf in `RunSyncAsync` (Zeile 173) durchreichen.

### ReadingTimeEstimator.cs (ReadingTimeEstimator)

- **Toter Code** — `var minutes = Math.Max(1, (int)Math.Round(wordCount / WordsPerMinute));` (Zeile 34) klemmt das Ergebnis auf mindestens 1, doch seit dem neuen Early-Return `if (minutes <= 1) return string.Empty;` (Zeilen 35–38) ist die Untergrenze 1 nicht mehr beobachtbar: Jeder Wert ≤ 1 verlässt die Methode, bevor `minutes` verwendet wird. `Math.Max(1, …)` ist ein wirkungsloser Rest aus der Zeit, als „1 Minute" noch angezeigt wurde.

  Empfehlung: Die Klemmung entfernen (`var minutes = (int)Math.Round(wordCount / WordsPerMinute);`) — das Verhalten bleibt identisch, da Werte ≤ 1 ohnehin `string.Empty` liefern.

## Geprüfte Dateien

Liste aller geprüften Dateien (Basisbranch `origin/staging`, Merge-Base `d9100c6`; Implementierungsänderungen liegen uncommitted im Working Tree vor, `docs/`-Änderungen, `test-results*` und Planungsartefakte ausgenommen):

- `src/Reporter.Core/Interfaces/IFeedIconService.cs`
- `src/Reporter.Core/Interfaces/IItemRepository.cs`
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
- `src/Reporter.Core/Services/FeedSiteResolver.cs`
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
