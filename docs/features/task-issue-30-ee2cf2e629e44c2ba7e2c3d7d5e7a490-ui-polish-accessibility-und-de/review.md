<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Neue Klassen

- [x] `ReadingTimeEstimator` (statische Hilfsklasse, `src/Reporter.Core/Services/ReadingTimeEstimator.cs`) — `EstimateText(string? contentHtml)` mit HTML-Strip, Wortzählung (200 wpm), `Math.Max(1, …)`, Formatierung via `AppResources.ArticleReadingTimeFormat`

### `IItemRepository` / `ItemRepository`

- [x] `GetSavedForLaterAsync(int page, int pageSize)` — im Interface (`IItemRepository.cs:111`) und implementiert mit `Skip`/`Take`, `PublishedAt desc` + `ThenBy(Id)` (`ItemRepository.cs:258-300`)
- [x] `AddRangeAsync(IReadOnlyList<Item>)` — im Interface (`IItemRepository.cs:118`) und implementiert mit einem `DbContext` + einem `SaveChangesAsync` (`ItemRepository.cs:303-313`)
- [x] `GetByGuidOrHashAsync` entfernt — aus Interface und Implementierung; kein Aufrufer mehr im Repo
- [x] Parameterloses `GetSavedForLaterAsync()` entfernt — durch paged Variante ersetzt
- [x] `ItemListItem`-Projektionen füllen `ReadingTimeText` via `ReadingTimeEstimator.EstimateText` — `GetUnreadByDateAsync` (`ItemRepository.cs:158`) und `GetSavedForLaterAsync` (`ItemRepository.cs:298`)

### `ItemListItem` (Datenmodell)

- [x] `ReadingTimeText` (`string?`, init) — `ItemListItem.cs:73`

### `FeedSyncService`

- [x] `RunSyncAsync` umgestellt — In-Memory-`HashSet<string>`-Dedup über `GuidOrHash` aus `existingItems` inkl. Dedup innerhalb des Feed-Dokuments (`knownKeys.Add`), Sammeln in `newItemEntities`, einmaliger `AddRangeAsync`-Aufruf (`FeedSyncService.cs:144-182`); `existingCount`/`lastPublishedAt` unverändert

### `LaterViewModel`

- [x] `PageSize = 20`, `SavedItems` als `ObservableCollection<ItemListItem>`, `IsLoading`, `HasMore`, `_currentPage`, `LoadMoreCommand` (`AsyncRelayCommand`, CanExecute `HasMore && !IsLoading`) → `LoadMoreAsync` (`LaterViewModel.cs:19-137`)
- [x] `LoadAsync` resettet Paging und lädt Seite 0 via `GetSavedForLaterAsync(0, PageSize)`
- [x] `ToggleSavedAsync` entfernt in-place aus `SavedItems`; `MarkReadAsync` ersetzt Eintrag in-place durch `IsRead = true`-Kopie inkl. `ReadingTimeText` (`LaterViewModel.cs:139-181`)

### `UnreadViewModel`

- [x] `SelectedCategoryText` samt Zuweisungen entfernt; `Categories`/`SelectedCategory`/`SelectCategoryCommand`/`UpdateCategorySelection` unverändert als Chip-Datenbasis
- [x] `ToggleSavedAsync`-In-place-Kopie um `ReadingTimeText` ergänzt (`UnreadViewModel.cs:432`)

### `ArticleDetailViewModel`

- [x] `CalculateReadingTime` ersetzt durch `ReadingTimeEstimator.EstimateText(item.ContentHtml)` (`ArticleDetailViewModel.cs:297`); private Hilfsmethode und `WordsPerMinute`/`HtmlTagRegex`-Felder entfernt

### `UnreadPage` (View + Code-Behind)

- [x] Horizontale Chip-`CollectionView` (`LinearItemsLayout Orientation="Horizontal"`, `ItemsSource="{Binding Categories}"`) zwischen Aktionszeile und Fehler-Labels (`UnreadPage.xaml:108-174`)
- [x] Chip-Template: äußerer transparenter `Border` (`MinimumHeightRequest="44"`, `TapGestureRecognizer` → `SelectCategoryCommand`, `CommandParameter="{Binding .}"`, `SemanticProperties.Description="{Binding Name}"`), innerer Pill-`Border` (`Padding="12,6"`, `RoundRectangle 9999`, `SurfaceCard`/`BorderSubtle`, `DataTrigger` auf `IsSelected` → `Primary`-Fläche + `OnPrimary`-Text), Count-Kapsel mit `ChipCountCapsule`-Trigger
- [x] Funnel-`Border`, `OnFilterClicked` (Code-Behind) und `SelectedCategoryText`-`Label` entfernt; `ColumnDefinitions` der Aktionszeile = `Auto,*,Auto`
- [x] `SemanticProperties.Description` auf Refresh-`Border` (`ButtonRefresh`) und „Alles gelesen"-`Border` (`ButtonMarkAllRead`)

### `ArticleDetailPage` (View)

- [x] `Grid.Row="5"`-Container → schwebende Pill-`Border` (`Margin="16,0,16,12"`, `RoundRectangle 26`, `SurfaceCardTranslucent`, `BorderSubtle`-Stroke, `Shadow` 0,4/16/0,12) (`ArticleDetailPage.xaml:177-334`); sechs Aktions-`Border` mit unveränderten Commands/SemanticProperties
- [x] Quellen-Footer (Row 4) auf `SurfaceCard`/`BorderSubtle` umgestellt

### `FeedsPage` (View)

- [x] Statuszeile (Grid.Row 2) → Micro-Pill-Badge: `Padding="8,2"`, `RoundRectangle 9999`, `HorizontalOptions="Start"`, 6-pt-`BoxView`-Dot, `LabelMetaStyle`-Label; `DataTrigger` auf `HealthStatus` mappen `Status*Tint`-Hintergrund, `Status*`-Dot und `Status*Text`-Text inkl. Label-Textwechsel (`FeedsPage.xaml:209-256`)
- [x] Titel-Dot von 12 auf 8 pt verkleinert (`FeedsPage.xaml:169-186`)
- [x] Feed-Karten und Suchtreffer-Karten auf `SurfaceCard`/`BorderSubtle`; `SemanticProperties.Description` + `Hint` (`AccessibilityTapForActions`) auf beiden Karten-Typen
- [x] Backdrop-`BoxView` mit `SemanticProperties.Description` (`AccessibilityDismissSheet`); `NewUrlEntry` mit `SemanticProperties.Description` (`PlaceholderFeedSearch`)

### `ArticleCardView` (View)

- [x] Karten-`Stroke` → `BorderSubtle` (`ArticleCardView.xaml:13`)
- [x] Kopfzeilen-Dot → 6-pt-`Secondary`-Ungelesen-Dot, `DataTrigger` `IsRead=False` → sichtbar (`ArticleCardView.xaml:22-33`)
- [x] Lesezeit-`Label` bindet `ReadingTimeText`, Sichtbarkeit via `StringNotEmptyToBoolConverter` (`ArticleCardView.xaml:110-127`)
- [x] Bookmark-`DataTrigger` `Fill` → `BookmarkGold` (`ArticleCardView.xaml:159-161`)
- [x] `SemanticProperties.Description`: Bookmark-`Border` = `ArticleBookmarkSet` mit Trigger `IsSavedForLater=True` → `ArticleBookmarkRemove`; MarkRead-`Border` = `ArticleMarkAsRead`; Tap-Overlay-`Grid` = `{Binding Title}` + `Hint` (`AccessibilityTapForActions`)

### `LaterPage` (View)

- [x] `RemainingItemsThreshold="2"` + `RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}"` + `ActivityIndicator` (`IsLoading`) am unteren Rand (`LaterPage.xaml:35-53`)

### `CategoriesPage` (View)

- [x] `SemanticProperties.Description` auf `CategoryNameEntry` (`LabelCategoryName`) und Karten-`Border` (`{Binding Name}` + `Hint` `AccessibilityTapForActions`); Karten-Tokens `SurfaceCard`/`BorderSubtle` (`CategoriesPage.xaml:16-66`)

### `SettingsPage` (View)

- [x] Alle sechs Sektions-Karten auf `SurfaceCard` umgestellt; `Stroke` via implizitem `Border`-Style → `BorderSubtle`

### `AppShell.xaml.cs` (Code-Behind)

- [x] `Icon` pro Tab gesetzt (`tab_unread.png`, `tab_feeds.png`, `tab_later.png`, `tab_categories.png`, `tab_settings.png`); fünf neue `tab_*.svg`-MauiImage-Assets unter `Resources/Images/` vorhanden

### `Colors.xaml` (Ressource)

- [x] Neue Tokens jeweils Light/Dark mit den im Plan spezifizierten Werten: `SurfaceCardTranslucent` (`#E6FFFFFF`/`#E6182234`), `StatusOkTint`/`StatusWarningTint`/`StatusErrorTint` (`#1A…`), `StatusOkText`/`StatusWarningText`/`StatusErrorText` (`#047857`/`#B45309`/`#B91C1C` Light, `Status*`-Farben Dark), `ChipCountCapsule` (`#33FFFFFF`/`#33000000`)
- [x] `DarkOnSurfaceVariant` verifiziert: `#d8c3ad` (`Colors.xaml:55`), entspricht Entwurf, keine Änderung

### `Styles.xaml` (Ressource)

- [x] `LabelMdStyle` (InterMedium 12), `LabelSmStyle` (InterSemiBold 11), `LabelMetaStyle` (InterSemiBold 10), alle `TextSecondary`
- [x] `FontAutoScalingEnabled="True"` in allen benannten Typo-Styles explizit gesetzt
- [x] `MetaStyle`-`TextColor` → `TextSecondary` (Kontrast)
- [x] Impliziter `Border`-Style: `Stroke` → `BorderSubtle`

### `AppResources.resx` / `.de.resx` / Designer

- [x] `AccessibilityDismissSheet` („Close"/„Schließen") und `AccessibilityTapForActions` („Double-tap for actions"/„Tippen für Aktionen") in beiden resx + Designer-Properties (`AppResources.Designer.cs:1175-1188`)

### `Reporter.csproj` / App-Assets

- [x] `MauiIcon`- und `MauiSplashScreen`-`Color`: `#512BD4` → `#1e293b`
- [x] `appicon.svg`, `appiconfg.svg`, `splash.svg` als Vektor-Motiv neu gezeichnet (Zeitungs-Glyph + Akzent-Dot auf `#1e293b`)
- [x] `dotnet_bot.png` gelöscht und `MauiImage Update`-Zeile entfernt; fünf `tab_*.svg` vorhanden

### Tests

- [x] `ItemRepositoryTests`: `AddRangeAsync_InsertsAllItems`, `AddRangeAsync_EmptyList_DoesNothing`, `GetSavedForLaterAsync_Paged_ReturnsPage`, `GetSavedForLaterAsync_ProjectsReadingTimeText`, `GetUnreadByDateAsync_Paged_ProjectsReadingTimeText`; `GetSavedForLaterAsync_ReturnsOnlySaved`/`…_OrdersByPublishedAtDescending` auf paged Signatur umgestellt
- [x] `LaterViewModelTests`: `LoadMoreCommand_AppendsNextPage`, `LoadMoreCommand_WhenNoMoreItems_DoesNothing`, `MarkReadCommand_UpdatesItemInPlace`, `ToggleSavedCommand_RemovesItemFromSavedItems` (in-place-Entfernen), `MarkReadCommand_SetsReadAndKeepsItemInList`
- [x] `FeedSyncServiceTests`: `SyncFeedAsync_DuplicatesWithinSameDocument_InsertsOnce`, `SyncFeedAsync_MixedNewAndExisting_BatchInsertsOnlyNew`; `SyncFeedAsync_Duplicates_SkipsExistingItems` weiterhin gültig
- [x] `ReadingTimeEstimatorTests` (neu): `EstimateText_NullContent_ReturnsEmpty`, `…_EmptyContent_ReturnsEmpty`, `…_ShortContent_ReturnsOneMinute`, `…_HtmlContent_StripsTags`, `…_LongContent_ReturnsRoundedMinutes`
- [x] `UnreadViewModelTests`: `SelectCategoryCommand_UpdatesChipSelectionState`; `FailingItemRepository`-Fake an geändertes Interface angepasst (delegiert `GetSavedForLaterAsync(page,pageSize)` + `AddRangeAsync`, entfernte Member entfallen)
- [x] `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` — **347/347 bestanden** (bei diesem Review erneut ausgeführt)

### Audit-Ergebnisse (Plan-Schritt 14)

- [x] `AutoRefreshService` marshal-frei verifiziert: `PeriodicTimer` (`AutoRefreshService.cs:116`), kein Dispatcher/UI-Zugriff
- [x] `FontAutoScalingEnabled` nirgends deaktiviert; explizit `True` in den Typo-Styles
- [x] Kontrast-Audit umgesetzt: `MetaStyle` → `TextSecondary`, Badge-Texte `Status*Text`, Dots ≥ 3:1 mit Text gekoppelt

## Offene Aufgaben

Keine Implementierungslücken — alle Planelemente sind im Code vorhanden. Planvorgesehen offen bleiben (kein Planverstoß, explizit im Plan-Schritt 16 als Nachweis-/Folgeschritte vorgesehen):

- [ ] Task 41: Manuelle UI-Verifikation 390 × 844 pt (Light + Dark, UIA, Accessibility-Insights-FastPass, Narrator, Screenshots `test-results/issue-30/`, Protokoll in `test-results.md` + `docs/help/anwendung/mobile-ui-design.md`) — im Plan als manueller Verifikationsschritt vorgesehen, noch nicht durchgeführt
- [ ] Task 43: iOS-Verifikation (`net10.0-ios`-Build, `scripts/iOS-Deployment.ps1`) — im Plan als dokumentierte Folgeaufgabe auf macOS vorgesehen; `test-results.md` enthält noch keinen Issue-#30-Abschnitt (wird mit Task 41 ergänzt)

## Hinweise

Geringfügige Abweichungen von den Plan-Details (funktional ohne Lücke, zur Nachverfolgung dokumentiert):

- `IItemRepository.AddRangeAsync` ist ohne den im Plan genannten optionalen `CancellationToken cancellationToken = default`-Parameter deklariert (`IItemRepository.cs:118`). Batch-Insert-Funktionalität vollständig vorhanden; Signaturabweichung.
- Die Count-Kapsel im `UnreadPage`-Chip verwendet `LabelSmStyle` (InterSemiBold 11) statt des im Plan benannten `LabelMetaStyle` (InterSemiBold 10) (`UnreadPage.xaml:159`).
- `SemanticProperties.Description` der Suchtreffer-Karten bindet `FeedUrl` statt `Title` (`FeedsPage.xaml:75`) — sinnvoll, da `Title` bei Suchtreffern leer sein kann; im Plan war `{Binding Title}` genannt.
- Chip-`CollectionView` nutzt `LinearItemsLayout Orientation="Horizontal"` statt der Kurzform `ItemsLayout="HorizontalList"` — funktional äquivalent.
- `FeedsPage`-Badge setzt `OK`-Darstellung als Default und triggert nur `Warning`/`Error` — funktional äquivalent zu drei Triggern.
- `docs/help/anwendung/datenmodell.md` und `docs/help/benachrichtigungen/ablauf-technisch.md` wurden angepasst (nicht explizit im Plan, aber konsistente Folge der Repository-Änderungen).
