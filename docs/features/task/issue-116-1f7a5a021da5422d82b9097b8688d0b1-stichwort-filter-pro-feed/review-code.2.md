<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Keine Befunde

## Befunde

Keine. Die drei Befunde aus `review-code.1.md` sind behoben:

- Die geteilte Stichwort-Validierung liegt jetzt in `src/Reporter.Core/Services/KeywordValidator.cs` (`TryValidate` + `MaxTextLength = 500`) und wird sowohl in `SettingsViewModel.AddKeywordAsync` als auch in `FeedDetailViewModel.AddFeedKeywordAsync` verwendet; die doppelte Konstante `MaxKeywordLength` ist entfernt.
- `AddFeedKeywordAsync`/`RemoveFeedKeywordAsync` setzen im `catch`-Block `FeedKeywordErrorMessage = AppResources.ErrorActionFailed`, und `AddFeedKeywordAsync` hat einen `HasFeed`-Guard, der `FeedId = Guid.Empty` verhindert (`FeedDetailViewModel.cs:802`, `FeedDetailViewModel.cs:821-825`, `FeedDetailViewModel.cs:840-844`).
- `RetentionCleanupService.CleanupAsync` steigt über `IKeywordFilter.HasKeywordsAsync` → `IKeywordRepository.AnyAsync` früh aus, wenn keinerlei Stichworte existieren, und lädt die Kandidaten erst danach (`RetentionCleanupService.cs:57-62`).

## Geprüfte Dateien

Liste aller geprüften Dateien (Basisbranch `origin/staging`, inkl. uncommitteter Änderungen und untracked neuer Dateien):

- `src/Reporter.Core/Interfaces/IKeywordFilter.cs`
- `src/Reporter.Core/Interfaces/IKeywordRepository.cs`
- `src/Reporter.Core/Models/Keyword.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/Services/KeywordFilter.cs`
- `src/Reporter.Core/Services/KeywordValidator.cs`
- `src/Reporter.Core/Services/NotificationService.cs`
- `src/Reporter.Core/Services/RetentionCleanupService.cs`
- `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Data/Entities/Keyword.cs`
- `src/Reporter.Data/Migrations/20260920094729_AddKeywordFeedId.cs`
- `src/Reporter.Data/Migrations/20260920094729_AddKeywordFeedId.Designer.cs`
- `src/Reporter.Data/Migrations/ReporterDbContextModelSnapshot.cs`
- `src/Reporter.Data/ReporterDbContext.cs`
- `src/Reporter.Data/Repositories/KeywordRepository.cs`
- `src/Reporter.E2ETests/E2EPageHelpers.cs`
- `src/Reporter.E2ETests/FeedDbAssertions.cs`
- `src/Reporter.E2ETests/FeedDetailTests.cs`
- `src/Reporter.Tests/DelegatingKeywordRepository.cs`
- `src/Reporter.Tests/FeedDetailViewModelTests.cs`
- `src/Reporter.Tests/FeedRepositoryTests.cs`
- `src/Reporter.Tests/FeedSyncServiceTests.cs`
- `src/Reporter.Tests/KeywordFilterTests_E2E.cs`
- `src/Reporter.Tests/KeywordRepositoryTests.cs`
- `src/Reporter.Tests/NotificationServiceTests.cs`
- `src/Reporter.Tests/RetentionCleanupServiceTests.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Keywords.cs`
- `src/Reporter.Tests/TestDataSeeder.cs`
- `src/Reporter/Views/FeedDetailPage.xaml`

## Zusatzprüfungen (Projektregeln)

- **`RaiseUiActionRequested`:** nicht im Diff und nirgends im Repository verwendet — nichts zu prüfen.
- **Mobile-UI-Regeln (`FeedDetailPage.xaml`):** erfüllt — keine horizontalen Datentabellen; Touch-Ziele der neuen Buttons ≥ 44 × 44 pt (`MinimumHeightRequest="44"` an Cancel/Add/Save, feste 44 × 44 am Chip-„×"); alle neuen Farben über `AppThemeBinding`; keine verschachtelte `CollectionView`/`ScrollView` (die Artikel-`CollectionView` ist Geschwister des Sheet-Overlays, die Stichwort-Chips nutzen `FlexLayout` + `BindableLayout` in der höhenbegrenzten Sheet-`ScrollView` mit `MaximumHeightRequest="560"`). UI-Verifikation ist in `docs/help/anwendung/mobile-ui-design.md` dokumentiert.
- **DI-Verdrahtung:** `IKeywordRepository` ist in `MauiProgram.cs:86` als Singleton registriert; der neue `FeedDetailViewModel`-Konstruktorparameter wird vom Container aufgelöst.
- **Build-Verifikation:** `dotnet build` für `Reporter.Tests` und `Reporter.E2ETests` (inkl. `Reporter.Core`, `Reporter.Data`) erfolgreich — 0 Warnungen, 0 Fehler.
