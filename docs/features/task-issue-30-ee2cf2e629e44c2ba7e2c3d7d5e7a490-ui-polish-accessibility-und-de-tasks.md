<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: UI-Polish, Accessibility und Design-System-Vollständigkeit (Issue #30)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Audit | Alle Seiten gegen die `design-draft`-Referenzen (`screen.png`/`code.html`, Light + Dark) abgleichen und Abweichungsliste erstellen | Erledigt | Umsetzung gemäß Plan-Abweichungsliste |
| 2 | Ressourcen | Neue resx-Schlüssel `AccessibilityDismissSheet` und `AccessibilityTapForActions` in `AppResources.resx` + `AppResources.de.resx` eintragen | Erledigt | Build + Designer-Properties (`AppResources.Designer.cs`) |
| 3 | Ressourcen | `Colors.xaml`: Tokens `SurfaceCardTranslucent`, `StatusOkTint`/`StatusWarningTint`/`StatusErrorTint`, `StatusOkText`/`StatusWarningText`/`StatusErrorText`, `ChipCountCapsule` (jeweils Light/Dark) ergänzen | Erledigt | Build |
| 4 | Ressourcen | `Colors.xaml`: `DarkOnSurfaceVariant` (`#d8c3ad`) gegen `reporter_editorial_dark/DESIGN.md` verifizieren | Erledigt | Entspricht dem Entwurf, keine Änderung |
| 5 | Ressourcen | `Styles.xaml`: `LabelMdStyle`, `LabelSmStyle`, `LabelMetaStyle` ergänzen | Erledigt | Build |
| 6 | Ressourcen | `Styles.xaml`: `MetaStyle`-`TextColor` auf `TextSecondary` umstellen (Kontrast) | Erledigt | Build |
| 7 | Ressourcen | `Styles.xaml`: impliziter `Border`-Style `Stroke` auf `BorderSubtle` umstellen | Erledigt | Build |
| 8 | Ressourcen | `Styles.xaml`: `FontAutoScalingEnabled="True"` in den benannten Typo-Styles explizit setzen | Erledigt | Build |
| 9 | Logik | `ReadingTimeEstimator` in `src/Reporter.Core/Services/` anlegen (`EstimateText`, 200 wpm, `ArticleReadingTimeFormat`) | Erledigt | `ReadingTimeEstimatorTests` (`EstimateText_*`, 5 Tests) |
| 10 | Datenmodell | `ItemListItem` um `ReadingTimeText` (`string?`) erweitern | Erledigt | `ItemRepositoryTests.*_ProjectsReadingTimeText` |
| 11 | Logik | `ArticleDetailViewModel.CalculateReadingTime` auf `ReadingTimeEstimator` umstellen | Erledigt | Build |
| 12 | Datenmodell | `IItemRepository`: `GetSavedForLaterAsync(int page, int pageSize)` und `AddRangeAsync` hinzufügen | Erledigt | `ItemRepositoryTests` |
| 13 | Datenmodell | `IItemRepository`/`ItemRepository`: `GetByGuidOrHashAsync` und parameterloses `GetSavedForLaterAsync()` entfernen | Erledigt | Build |
| 14 | Datenmodell | `ItemRepository`: paged `GetSavedForLaterAsync` implementieren (`Skip`/`Take`, `ThenBy(Id)`, Projektion inkl. `ReadingTimeText`) | Erledigt | `GetSavedForLaterAsync_Paged_ReturnsPage` |
| 15 | Datenmodell | `ItemRepository`: `AddRangeAsync` implementieren (ein `DbContext`, ein `SaveChangesAsync`) | Erledigt | `AddRangeAsync_InsertsAllItems`, `AddRangeAsync_EmptyList_DoesNothing` |
| 16 | Datenmodell | `ItemRepository`: `ItemListItem`-Projektionen (`GetUnreadByDateAsync`, `GetSavedForLaterAsync`) um `ReadingTimeText` erweitern | Erledigt | `GetSavedForLaterAsync_ProjectsReadingTimeText`, `GetUnreadByDateAsync_Paged_ProjectsReadingTimeText` |
| 17 | Logik | `FeedSyncService.RunSyncAsync` auf In-Memory-`HashSet`-Dedup + `AddRangeAsync`-Batch-Insert umstellen | Erledigt | `SyncFeedAsync_DuplicatesWithinSameDocument_InsertsOnce`, `SyncFeedAsync_MixedNewAndExisting_BatchInsertsOnlyNew` |
| 18 | Logik | `LaterViewModel`: `SavedItems` → `ObservableCollection<ItemListItem>`, `PageSize`/`IsLoading`/`HasMore`/`LoadMoreCommand`/`LoadMoreAsync`/`_currentPage` einführen | Erledigt | `LaterViewModelTests` (`LoadMoreCommand_AppendsNextPage`, `LoadMoreCommand_WhenNoMoreItems_DoesNothing`) |
| 19 | Logik | `LaterViewModel`: `ToggleSavedAsync` (in-place entfernen) und `MarkReadAsync` (in-place `IsRead`-Kopie) umstellen | Erledigt | `ToggleSavedCommand_RemovesItemFromSavedItems`, `MarkReadCommand_UpdatesItemInPlace` |
| 20 | Logik | `UnreadViewModel`: `SelectedCategoryText` samt Zuweisungen entfernen | Erledigt | Build |
| 21 | UI | `UnreadPage`: horizontale Chip-`CollectionView` (HorizontalList, Pill-Template mit `IsSelected`-`DataTrigger`n, Count-Kapsel, `SemanticProperties.Description` = `Name`) einbauen | Erledigt | Build (XAML-SourceGen), manuell in 41 |
| 22 | UI | `UnreadPage`: Funnel-`Border`, `OnFilterClicked` (Code-Behind) und `SelectedCategoryText`-`Label` entfernen; `ColumnDefinitions` der Aktionszeile neu | Erledigt | Build |
| 23 | UI | `UnreadPage`: `SemanticProperties.Description` auf Refresh- (`ButtonRefresh`) und „Alles gelesen"-`Border` (`ButtonMarkAllRead`) ergänzen | Erledigt | Build |
| 24 | UI | `ArticleCardView`: `Stroke` → `BorderSubtle`, Kopfzeilen-Dot → 6-pt-`Secondary`-Ungelesen-Dot (`DataTrigger` `IsRead=False`) | Erledigt | Build |
| 25 | UI | `ArticleCardView`: Lesezeit-`Label` auf `ReadingTimeText` binden (statt `"—"`), Sichtbarkeit via `StringNotEmptyToBoolConverter` | Erledigt | `*_ProjectsReadingTimeText` |
| 26 | UI | `ArticleCardView`: Bookmark-`DataTrigger`-`Fill` → `BookmarkGold`; `SemanticProperties` auf Bookmark- (`ArticleBookmarkSet`/`ArticleBookmarkRemove` per Trigger), MarkRead- (`ArticleMarkAsRead`) und Tap-Overlay (`{Binding Title}` + `Hint`) setzen | Erledigt | Build |
| 27 | UI | `FeedsPage`: Statuszeile → Micro-Pill-Badge (Padding 8×2, `RoundRectangle 9999`, 6-pt-Dot, `LabelMetaStyle`, `Status*Tint`-/`Status*Text`-`DataTrigger`); Titel-Dot auf 8 pt verkleinern | Erledigt | Build |
| 28 | UI | `FeedsPage`: `SemanticProperties`/`Hint` auf Feed-Karten, Suchtreffer-Karten, Sheet-Backdrop und `NewUrlEntry` ergänzen; Karten-Tokens `SurfaceCard`/`BorderSubtle` | Erledigt | Build |
| 29 | UI | `ArticleDetailPage`: Row-5-Leiste → schwebende Pill-`Border` (`Margin 16,0,16,12`, `RoundRectangle 26`, `SurfaceCardTranslucent`, `BorderSubtle`-Stroke, `Shadow`) | Erledigt | Build |
| 30 | UI | `LaterPage`: `RemainingItemsThreshold` + `RemainingItemsThresholdReachedCommand` + `ActivityIndicator` (`IsLoading`) ergänzen | Erledigt | Build |
| 31 | UI | `CategoriesPage`: `SemanticProperties` auf `CategoryNameEntry` (`LabelCategoryName`) und Karten (`{Binding Name}` + `Hint`); Karten-Tokens `SurfaceCard`/`BorderSubtle` | Erledigt | Build |
| 32 | UI | `SettingsPage`: Sektions-Karten auf `SurfaceCard`/`BorderSubtle` umstellen | Erledigt | Build |
| 33 | UI | `AppShell.xaml.cs`: `Icon` pro Tab setzen (fünf neue `tab_*.svg`-Assets unter `Resources/Images/`) | Erledigt | Build (MauiImage) |
| 34 | Ressourcen | `appicon.svg`, `appiconfg.svg`, `splash.svg` als handerstelltes SVG-Vektor-Motiv nach Referenz-PNG neu zeichnen; `MauiIcon`/`MauiSplashScreen`-`Color` auf `#1e293b` (Design-Palette) setzen; `dotnet_bot.png` + `MauiImage Update`-Zeile entfernen | Erledigt | Build; Motiv-Abnahme per Screenshot in 41 |
| 35 | Audit | Performance-/A11y-Audit abschließen: `AutoRefreshService`-Marshal-Freiheit, `Image`-Caching-Defaults, `FontAutoScalingEnabled`-Audit, Kontrast-Audit dokumentieren | Erledigt | Audit: `AutoRefreshService` marshal-frei (`PeriodicTimer`, `ConfigureAwait(false)`, kein Dispatcher/UI-Zugriff); `Image`-Caching = `UriImageSource`-Default (aktiv, 1 Tag); `FontAutoScalingEnabled` nirgends deaktiviert, in Typo-Styles explizit `True`; Kontrast-Audit: `MetaStyle` → `TextSecondary` (≈ 4,8:1), Badge-Texte `Status*Text`, Dots ≥ 3:1 mit Text gekoppelt |
| 36 | Tests | `ItemRepositoryTests`: `AddRangeAsync_InsertsAllItems`, `AddRangeAsync_EmptyList_DoesNothing`, `GetSavedForLaterAsync_Paged_ReturnsPage` neu; `GetSavedForLaterAsync_*`-Tests auf paged Signatur umstellen | Erledigt | 347 Tests bestanden |
| 37 | Tests | `LaterViewModelTests`: `LoadMoreCommand_AppendsNextPage`, `LoadMoreCommand_WhenNoMoreItems_DoesNothing`, `ToggleSavedCommand_RemovesItemInPlace`, `MarkReadCommand_UpdatesItemInPlace` | Erledigt | 347 Tests bestanden (`ToggleSavedCommand_RemovesItemFromSavedItems` deckt in-place-Entfernen) |
| 38 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_DuplicatesWithinSameDocument_InsertsOnce`, `SyncFeedAsync_MixedNewAndExisting_BatchInsertsOnlyNew` | Erledigt | 347 Tests bestanden |
| 39 | Tests | `ReadingTimeEstimatorTests` anlegen (`EstimateText`-Minuten, leerer Inhalt, HTML-Strip) | Erledigt | 347 Tests bestanden |
| 40 | Tests | `UnreadViewModelTests`: `SelectCategoryCommand_UpdatesChipSelectionState`; `FailingItemRepository` an geändertes `IItemRepository` anpassen | Erledigt | 347 Tests bestanden |
| 41 | E2E-Tests | Manuelle Verifikation 390 × 844 pt (Light + Dark, UIA + Screenshots `test-results/issue-30/`): Chip-Leiste, Floating Bar, Pill-Badges, Later-Paging, Accessibility-`Name`-Properties + Accessibility-Insights-FastPass (kritisch = FastPass-Fehler) + Narrator-Durchlauf, Kontrast/Dark-Mode, Icon/Splash per Screenshot-Vergleich mit Referenz-PNG, Sync-Performance-Beobachtung (optional Sync-Dauer vorher/nachher); Protokoll in `test-results.md` + `docs/help/anwendung/mobile-ui-design.md` | Offen | — |
| 42 | Verifikation | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` und `.\scripts\Run-StaticChecks.ps1` ohne Befund | Erledigt | 347/347 bestanden (im Review erneut ausgeführt); Static-Checks Exit-Code 0 (Format, LicenseHeaders, Security, Build) |
| 43 | Verifikation | iOS-Verifikation (`net10.0-ios`-Build, `scripts/iOS-Deployment.ps1`) als dokumentierte Folgeaufgabe in `test-results.md` vermerken (wie bei Issues #27/#28) | Offen | — |
