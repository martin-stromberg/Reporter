<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: UI-Polish, Accessibility und Design-System-Vollständigkeit (Issue #30)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Audit | Alle Seiten gegen die `design-draft`-Referenzen (`screen.png`/`code.html`, Light + Dark) abgleichen und Abweichungsliste erstellen | Offen | — |
| 2 | Ressourcen | Neue resx-Schlüssel `AccessibilityDismissSheet` und `AccessibilityTapForActions` in `AppResources.resx` + `AppResources.de.resx` eintragen | Offen | — |
| 3 | Ressourcen | `Colors.xaml`: Tokens `SurfaceCardTranslucent`, `StatusOkTint`/`StatusWarningTint`/`StatusErrorTint`, `StatusOkText`/`StatusWarningText`/`StatusErrorText`, `ChipCountCapsule` (jeweils Light/Dark) ergänzen | Offen | — |
| 4 | Ressourcen | `Colors.xaml`: `DarkOnSurfaceVariant` (`#d8c3ad`) gegen `reporter_editorial_dark/DESIGN.md` verifizieren | Offen | — |
| 5 | Ressourcen | `Styles.xaml`: `LabelMdStyle`, `LabelSmStyle`, `LabelMetaStyle` ergänzen | Offen | — |
| 6 | Ressourcen | `Styles.xaml`: `MetaStyle`-`TextColor` auf `TextSecondary` umstellen (Kontrast) | Offen | — |
| 7 | Ressourcen | `Styles.xaml`: impliziter `Border`-Style `Stroke` auf `BorderSubtle` umstellen | Offen | — |
| 8 | Ressourcen | `Styles.xaml`: `FontAutoScalingEnabled="True"` in den benannten Typo-Styles explizit setzen | Offen | — |
| 9 | Logik | `ReadingTimeEstimator` in `src/Reporter.Core/Services/` anlegen (`EstimateText`, 200 wpm, `ArticleReadingTimeFormat`) | Offen | — |
| 10 | Datenmodell | `ItemListItem` um `ReadingTimeText` (`string?`) erweitern | Offen | — |
| 11 | Logik | `ArticleDetailViewModel.CalculateReadingTime` auf `ReadingTimeEstimator` umstellen | Offen | — |
| 12 | Datenmodell | `IItemRepository`: `GetSavedForLaterAsync(int page, int pageSize)` und `AddRangeAsync` hinzufügen | Offen | — |
| 13 | Datenmodell | `IItemRepository`/`ItemRepository`: `GetByGuidOrHashAsync` und parameterloses `GetSavedForLaterAsync()` entfernen | Offen | — |
| 14 | Datenmodell | `ItemRepository`: paged `GetSavedForLaterAsync` implementieren (`Skip`/`Take`, `ThenBy(Id)`, Projektion inkl. `ReadingTimeText`) | Offen | — |
| 15 | Datenmodell | `ItemRepository`: `AddRangeAsync` implementieren (ein `DbContext`, ein `SaveChangesAsync`) | Offen | — |
| 16 | Datenmodell | `ItemRepository`: `ItemListItem`-Projektionen (`GetUnreadByDateAsync`, `GetSavedForLaterAsync`) um `ReadingTimeText` erweitern | Offen | — |
| 17 | Logik | `FeedSyncService.RunSyncAsync` auf In-Memory-`HashSet`-Dedup + `AddRangeAsync`-Batch-Insert umstellen | Offen | — |
| 18 | Logik | `LaterViewModel`: `SavedItems` → `ObservableCollection<ItemListItem>`, `PageSize`/`IsLoading`/`HasMore`/`LoadMoreCommand`/`LoadMoreAsync`/`_currentPage` einführen | Offen | — |
| 19 | Logik | `LaterViewModel`: `ToggleSavedAsync` (in-place entfernen) und `MarkReadAsync` (in-place `IsRead`-Kopie) umstellen | Offen | — |
| 20 | Logik | `UnreadViewModel`: `SelectedCategoryText` samt Zuweisungen entfernen | Offen | — |
| 21 | UI | `UnreadPage`: horizontale Chip-`CollectionView` (HorizontalList, Pill-Template mit `IsSelected`-`DataTrigger`n, Count-Kapsel, `SemanticProperties.Description` = `Name`) einbauen | Offen | — |
| 22 | UI | `UnreadPage`: Funnel-`Border`, `OnFilterClicked` (Code-Behind) und `SelectedCategoryText`-`Label` entfernen; `ColumnDefinitions` der Aktionszeile neu | Offen | — |
| 23 | UI | `UnreadPage`: `SemanticProperties.Description` auf Refresh- (`ButtonRefresh`) und „Alles gelesen"-`Border` (`ButtonMarkAllRead`) ergänzen | Offen | — |
| 24 | UI | `ArticleCardView`: `Stroke` → `BorderSubtle`, Kopfzeilen-Dot → 6-pt-`Secondary`-Ungelesen-Dot (`DataTrigger` `IsRead=False`) | Offen | — |
| 25 | UI | `ArticleCardView`: Lesezeit-`Label` auf `ReadingTimeText` binden (statt `"—"`), Sichtbarkeit via `StringNotEmptyToBoolConverter` | Offen | — |
| 26 | UI | `ArticleCardView`: Bookmark-`DataTrigger`-`Fill` → `BookmarkGold`; `SemanticProperties` auf Bookmark- (`ArticleBookmarkSet`/`ArticleBookmarkRemove` per Trigger), MarkRead- (`ArticleMarkAsRead`) und Tap-Overlay (`{Binding Title}` + `Hint`) setzen | Offen | — |
| 27 | UI | `FeedsPage`: Statuszeile → Micro-Pill-Badge (Padding 8×2, `RoundRectangle 9999`, 6-pt-Dot, `LabelMetaStyle`, `Status*Tint`-/`Status*Text`-`DataTrigger`); Titel-Dot auf 8 pt verkleinern | Offen | — |
| 28 | UI | `FeedsPage`: `SemanticProperties`/`Hint` auf Feed-Karten, Suchtreffer-Karten, Sheet-Backdrop und `NewUrlEntry` ergänzen; Karten-Tokens `SurfaceCard`/`BorderSubtle` | Offen | — |
| 29 | UI | `ArticleDetailPage`: Row-5-Leiste → schwebende Pill-`Border` (`Margin 16,0,16,12`, `RoundRectangle 26`, `SurfaceCardTranslucent`, `BorderSubtle`-Stroke, `Shadow`) | Offen | — |
| 30 | UI | `LaterPage`: `RemainingItemsThreshold` + `RemainingItemsThresholdReachedCommand` + `ActivityIndicator` (`IsLoading`) ergänzen | Offen | — |
| 31 | UI | `CategoriesPage`: `SemanticProperties` auf `CategoryNameEntry` (`LabelCategoryName`) und Karten (`{Binding Name}` + `Hint`); Karten-Tokens `SurfaceCard`/`BorderSubtle` | Offen | — |
| 32 | UI | `SettingsPage`: Sektions-Karten auf `SurfaceCard`/`BorderSubtle` umstellen | Offen | — |
| 33 | UI | `AppShell.xaml.cs`: `Icon` pro Tab setzen (fünf neue `tab_*.svg`-Assets unter `Resources/Images/`) | Offen | — |
| 34 | Ressourcen | `appicon.svg`, `appiconfg.svg`, `splash.svg` als handerstelltes SVG-Vektor-Motiv nach Referenz-PNG neu zeichnen; `MauiIcon`/`MauiSplashScreen`-`Color` auf `#1e293b` (Design-Palette) setzen; `dotnet_bot.png` + `MauiImage Update`-Zeile entfernen | Offen | — |
| 35 | Audit | Performance-/A11y-Audit abschließen: `AutoRefreshService`-Marshal-Freiheit, `Image`-Caching-Defaults, `FontAutoScalingEnabled`-Audit, Kontrast-Audit dokumentieren | Offen | — |
| 36 | Tests | `ItemRepositoryTests`: `AddRangeAsync_InsertsAllItems`, `AddRangeAsync_EmptyList_DoesNothing`, `GetSavedForLaterAsync_Paged_ReturnsPage` neu; `GetSavedForLaterAsync_*`-Tests auf paged Signatur umstellen | Offen | — |
| 37 | Tests | `LaterViewModelTests`: `LoadMoreCommand_AppendsNextPage`, `LoadMoreCommand_WhenNoMoreItems_DoesNothing`, `ToggleSavedCommand_RemovesItemInPlace`, `MarkReadCommand_UpdatesItemInPlace` | Offen | — |
| 38 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_DuplicatesWithinSameDocument_InsertsOnce`, `SyncFeedAsync_MixedNewAndExisting_BatchInsertsOnlyNew` | Offen | — |
| 39 | Tests | `ReadingTimeEstimatorTests` anlegen (`EstimateText`-Minuten, leerer Inhalt, HTML-Strip) | Offen | — |
| 40 | Tests | `UnreadViewModelTests`: `SelectCategoryCommand_UpdatesChipSelectionState`; `FailingItemRepository` an geändertes `IItemRepository` anpassen | Offen | — |
| 41 | E2E-Tests | Manuelle Verifikation 390 × 844 pt (Light + Dark, UIA + Screenshots `test-results/issue-30/`): Chip-Leiste, Floating Bar, Pill-Badges, Later-Paging, Accessibility-`Name`-Properties + Accessibility-Insights-FastPass (kritisch = FastPass-Fehler) + Narrator-Durchlauf, Kontrast/Dark-Mode, Icon/Splash per Screenshot-Vergleich mit Referenz-PNG, Sync-Performance-Beobachtung (optional Sync-Dauer vorher/nachher); Protokoll in `test-results.md` + `docs/help/anwendung/mobile-ui-design.md` | Offen | — |
| 42 | Verifikation | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` und `.\scripts\Run-StaticChecks.ps1` ohne Befund | Offen | — |
| 43 | Verifikation | iOS-Verifikation (`net10.0-ios`-Build, `scripts/iOS-Deployment.ps1`) als dokumentierte Folgeaufgabe in `test-results.md` vermerken (wie bei Issues #27/#28) | Offen | — |
