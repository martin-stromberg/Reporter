<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### nul (Repository-Artefakt)

- **Versehentlich angelegte Datei** — Im Repository-Stammverzeichnis liegt eine leere, untracked Datei namens `nul` (0 Bytes, sichtbar in `git status` als `?? nul`). Sie ist ein Artefakt einer Shell-Umleitung (`> nul` erzeugt unter Git Bash eine reale Datei statt des Windows-Geräte-NUL) und gehört nicht zum Feature. Da sie nicht per `.gitignore` ausgeschlossen ist, würde sie bei einem `git add -A` mitcommittet; der Name `nul` ist zudem unter Windows ein reservierter Gerätename und kann bei Checkout/Tooling Probleme verursachen.

  Empfehlung: Die Datei `nul` aus dem Working Tree löschen, bevor die Änderungen committet werden (ggf. in `.gitignore` aufnehmen).

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
- **Mobile-UI-Regeln (`FeedDetailPage.xaml`):** erfüllt — keine horizontalen Datentabellen und keine neue Text-Button-Reihe; Touch-Ziele der neuen Elemente ≥ 44 × 44 pt (`MinimumHeightRequest="44"` an Cancel/„+ Hinzufügen"/Save, feste 44 × 44 am Chip-„×"); alle neuen Farben über `AppThemeBinding`; keine verschachtelte `CollectionView`/`ScrollView` (die Artikel-`CollectionView` ist Geschwister des Sheet-Overlays, die Stichwort-Chips nutzen `FlexLayout` + `BindableLayout` innerhalb der höhenbegrenzten Sheet-`ScrollView` mit `MaximumHeightRequest="560"`). Die UI-Verifikation ist in `docs/help/anwendung/mobile-ui-design.md` (Abschnitt „Stichwort-Filter pro Feed (issue-116)") dokumentiert.
- **Delta seit `review-code.2.md`:** Die einzige Code-relevante Änderung ist der neue Ressourcentext `FeedKeywordsInfo` in `AppResources.resx`, `AppResources.de.resx` und `AppResources.Designer.cs` — konsistent benannt, in beiden Sprachen vorhanden und im XAML (`FeedDetailPage.xaml`, Info-Border im Edit-Sheet) korrekt via `x:Static` referenziert. Die Neutral-Prüfung des gesamten Branch-Diffs bestätigt die Bewertung aus Iteration 2: Die früheren Befunde (geteilte `KeywordValidator`-Validierung, sichtbare Fehler im `catch`-Block, `HasFeed`-Guard, Frühausstieg über `HasKeywordsAsync`/`AnyAsync`) bleiben korrekt umgesetzt.
- **Konsistenz:** Alle Aufrufer von `IKeywordFilter.GetKeywordTextsAsync` wurden auf die neue `Guid? feedId`-Signatur umgestellt (`FeedSyncService`, `NotificationService`, `RetentionCleanupService`); `IKeywordRepository` wird außer durch `KeywordRepository` nur vom Test-Decorator `DelegatingKeywordRepository` implementiert, der alle neuen Member abdeckt.
