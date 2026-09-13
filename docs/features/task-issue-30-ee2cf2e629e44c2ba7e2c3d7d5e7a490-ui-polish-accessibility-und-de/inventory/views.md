<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Views / XAML-Oberflächen

Ist-Zustand der von der Anforderung betroffenen Seiten und Controls in `src/Reporter/Views/` sowie `src/Reporter/AppShell.*`. Bezugsgrößen: `design-draft/stitch_local_rss_feed_reader/` (Screens `screen.png`, Markup `code.html`, Tokens in `editorial_feed/DESIGN.md` und `reporter_editorial_dark/DESIGN.md`).

## `UnreadPage` (`src/Reporter/Views/UnreadPage.xaml` + `.xaml.cs`)

- Grundlayout: `Grid` `RowDefinitions="Auto,*"`, `Padding="16,8"`, `RowSpacing="12"`; `Shell.NavBarIsVisible="False"`.
- Header-Bereich (`VerticalStackLayout`):
  - Titel-`Label` mit `HeadlineStyle`.
  - Aktionszeile `Grid` `ColumnDefinitions="Auto,Auto,*,Auto"`:
    - Refresh-Button: `Border` 44×44, `RoundRectangle 22`, `SurfaceContainer`-Hintergrund, `TapGestureRecognizer` → `RefreshCommand`, `Path`-Icon (Pfeil nach unten in Kreis), `DataTrigger` auf `IsOnline=False` → `Opacity 0.4` bzw. `Stroke` → `TextSecondary`. **Kein `SemanticProperties.Description`.**
    - Filter-Button: `Border` 44×44, `RoundRectangle 22`, `TapGestureRecognizer` → `OnFilterClicked` (Code-Behind), `Path`-Funnel-Icon. **Kein `SemanticProperties.Description`.**
    - Statusspalte: `UnreadCountText` (`BodySmallStyle`), `LabelPullToRefresh` (`MetaStyle`), `OfflineHint` (`MetaStyle`, per `DataTrigger` auf `IsOnline` ein-/ausgeblendet).
    - „Alle gelesen"-Pille: `Border` `Padding="12,8"`, `RoundRectangle 22`, `Primary`-Hintergrund, `MinimumHeightRequest/MinimumWidthRequest="44"`, `TapGestureRecognizer` → `MarkAllReadCommand`, `Path`-Doppelhaken + `Label` `ButtonMarkAllRead` (`FontSize="12"`, `OnPrimary`). **Kein `SemanticProperties.Description`.**
  - `SelectedCategoryText`-`Label` (`MetaStyle`) zeigt `{Name} ({Count})` der Auswahl — **keine sichtbare Chip-Leiste**; Kategorieauswahl läuft ausschließlich über `OnFilterClicked` → `DisplayActionSheetAsync` (`UnreadPage.xaml.cs`, Zeilen 48–74), das die `Categories`-Namen als Optionen anzeigt und `SelectCategoryCommand` ausführt.
  - Fehler-`Label`s `ErrorMessage`/`SyncErrorMessage` (`BodySmallStyle`, `Error`-Farbe).
- Liste: `RefreshView` (`IsRefreshing` ↔ `IsSyncing`, `Command` → `RefreshCommand`) mit `CollectionView` `Articles`, `RemainingItemsThreshold="2"` + `RemainingItemsThresholdReachedCommand` → `LoadMoreCommand`, `EmptyView` = `PlaceholderUnread`, `ItemTemplate` = `ArticleCardView` mit gebundenen `ToggleSavedCommand`/`MarkReadCommand`/`IsOnline`. `ActivityIndicator` für `IsLoading` am unteren Rand.
- Entwurfsbezug: `ungelesen_dashboard/code.html` sieht eine horizontal scrollbare `filter-chip`-Leiste mit Count-Kapsel (`rounded-full`, aktiv: `bg-primary`/`text-on-primary`, inaktiv: `bg-surface-card` + `border-border-hairline`) vor — im Code nicht vorhanden.

## `ArticleDetailPage` (`src/Reporter/Views/ArticleDetailPage.xaml` + `.xaml.cs`)

- Grundlayout: `Grid` `RowDefinitions="Auto,Auto,Auto,*,Auto,Auto"`, `SafeAreaEdges="All"`.
- Zeile 0: Status-Pill (8-pt-`BoxView`-Dot `StatusOk` + `ArticleReadLabel`, sichtbar wenn `Item.IsRead`) und Auto-Mark-Read-`Label` + `Switch` (`SemanticProperties.Description` = `AutoMarkReadLabel`, `OnColor`/`ThumbColor` = `Secondary`/`OnSecondary`, 44-pt-Mindestmaße).
- Zeile 1 (Header): `FlexLayout` (Wrap) mit `Image` `FeedIconUrl` (16×16), 8-pt-`Primary`-Dot, `FeedName`, `PublishedAtText`, `ReadingTime` (alle `MetaStyle`, Trenner `•`); Titel-`Label` `HeadlineSmallStyle`, `WordWrap`.
- Zeile 2: Offline-Hinweis-`Border` (`RoundRectangle 8`, `SurfaceSubtle`) + `ErrorMessage`-`Label`.
- Zeile 3: `WebView` `ArticleWebView` mit `HtmlWebViewSource` `Html` ↔ `HtmlSource`, `Navigating` → `OnWebViewNavigating` (Code-Behind blockiert externe URLs offline via `WebViewNavigationGuard.IsExternalUrl` + `DisplayAlertAsync`).
- Zeile 4: Quellen-Footer-`Border` (`Padding="16,12"`, `Margin="16,8"`, `RoundRectangle 12`, `SurfaceCard`, `Stroke` = `Outline`) mit `ArticleFullContentAvailable` + `Item.Link` + `Button` `ArticleOpenInBrowser`.
- Zeile 5 (Aktionsleiste): **flache, vollbreite `Grid`-Leiste** (`Padding="8,8,8,24"`, 6 Spalten `*`, `Background` = `SurfaceContainer`) — keine schwebende Pill-Bar. 6 identisch aufgebaute `Border` 44×44 `RoundRectangle 22` `Background="Transparent"` mit `TapGestureRecognizer` + `Path`-/`Label`-Icon:
  1. Zurück → `GoBackCommand`, `SemanticProperties.Description` = `AccessibilityBack`
  2. Bookmark → `ToggleSavedForLaterCommand`, `Description` = `BookmarkButtonLabel`, `DataTrigger` auf `Item.IsSavedForLater=True` → `Fill` = `BookmarkGold`
  3. Schriftgröße → `ToggleFontSizeCommand`, `Description` = `AccessibilityFontSize`, `Label` `FontSizeLabel` (`UiLabelStyle`)
  4. Gelesen → `ToggleMarkReadCommand`, `Description` = `MarkAsReadButtonLabel`, `DataTrigger` auf `Item.IsRead=True` → `Stroke` = `Primary`
  5. Teilen → `ShareCommand`, `Description` = `AccessibilityShare`
  6. Browser → `OpenInBrowserCommand`, `Description` = `ArticleOpenInBrowser`
- Code-Behind: `IQueryAttributable` (`itemId`), `OnAppearing`/`OnDisappearing` rufen `AttachConnectivity`/`DetachConnectivity`.
- Entwurfsbezug: `artikel_lesemodus/code.html` sieht eine schwebende Pill-Bar (52 pt, `rounded-full`, 16-pt-Ränder, transluzent) vor — aktuell flache Leiste.

## `FeedsPage` (`src/Reporter/Views/FeedsPage.xaml` + `.xaml.cs`)

- Grundlayout wie `UnreadPage` (`Grid` `Auto,*`, `Padding="16,8"`).
- Header: Titel, `Button` `ActionAddFeed` → `OpenAddFormCommand` (44 pt), Fehler-/Offline-/Sync-Fehler-`Label`s.
- Suchtreffer-`CollectionView` (sichtbar bei `ShowSearchResults`): Karten-`Border` `Padding="16"`, `Margin="0,6"`, `MinimumHeightRequest="44"`, `RoundRectangle 12`, `SurfaceContainer` (kein expliziter `Stroke` → impliziter `Border`-Style liefert `Outline`-Stroke); `TapGestureRecognizer` → `OnSearchResultTapped` (Bestätigungsdialog → `SubscribeResultCommand`). **Keine `SemanticProperties`.**
- Feed-`CollectionView` (in `RefreshView` → `RefreshAllCommand`): Karten-`Border` `RoundRectangle 12`, `SurfaceContainer`; `TapGestureRecognizer` → `OnFeedTapped` → `DisplayActionSheetAsync` (Aktualisieren/Umbenennen/Kategorie/Bearbeiten/Löschen). **Keine `SemanticProperties`.**
- Health-Indikator pro Karte: 12-pt-`BoxView`-Dot (Ecke oben rechts) mit `DataTrigger`n auf `HealthStatus` `OK`/`Warning`/`Error` → `StatusOk`/`StatusWarning`/`StatusError`; plus vollbreites `Label` (Grid.Row 2) mit lokalisiertem Statustext (`HealthStatusOkLabel`/`WarningLabel`/`ErrorLabel`) in Statusfarbe — **kein Micro-Pill-Badge** wie im Entwurf (`feeds_health_status/code.html`: `padding 2×8`, 6-pt-Dot, `label-meta`, 10-%-Tint-Hintergrund).
- Add-/Edit-Sheet: Vollbild-`Grid`-Overlay (Backdrop-`BoxView` `Opacity="0.5"`, `TapGestureRecognizer` → `CloseAddFormCommand`, **kein `SemanticProperties`**) + `Border` `RoundRectangle 12,12,0,0` `SurfaceContainerLowest` mit `Entry` `NewUrl`, `Button`s `ButtonSearch`/`ButtonDirectAdd`/`ButtonSave`, `Switch` `FeedNotificationsEnabled` (mit `SemanticProperties.Description` = `FeedNotificationsLabel`, Zeile 332).
- Code-Behind: `FeedsPage.xaml.cs` — `OnFeedTapped`, `RenameFeedAsync`, `ChangeCategoryAsync` (nutzt `FeedsViewModel.MakeUniqueOptionLabels`), `ConfirmDeleteFeedAsync`, `OnSearchResultTapped`, `OnBackButtonPressed` (schließt Sheet), `ConfirmDirectAddAsync`-Delegate, `PropertyChanged`-Fokus auf `NewUrlEntry`.

## `ArticleCardView` (`src/Reporter/Views/ArticleCardView.xaml` + `.xaml.cs`)

- Wiederverwendbare Karte (`ContentView`, `x:Name="Card"`): `Border` `Padding="16"`, `Margin="0,6"`, **`RoundRectangle 16`** (= `rounded-lg` des Entwurfs), `Background` = `SurfaceCard`, **`Stroke` = `Outline`** (nicht `BorderSubtle`/Hairline).
- Inhalt (`Grid` `Auto,Auto,Auto,Auto` × `*,Auto`):
  - Kopfzeile: 8-pt-`Primary`-Dot + `FeedTitle` (`BodySmallStyle`) + `•` + `PublishedAt` (`MetaStyle`). Der 8-pt-Dot ist ein reiner Feed-Akzent — **kein separater Ungelesen-Indikator** (Entwurf: 6-pt-Cobalt/`Secondary`-Dot).
  - Kategorie-Pill: `Border` `Padding="8,3"`, `RoundRectangle 10`, `SurfaceContainer`, `Label` `FontSize="11"` `Bold` `TextSecondary`; ausgeblendet bei leerem/`null` `CategoryName`.
  - Titel `HeadlineSmallStyle`, `TailTruncation`, `MaxLines="2"`.
  - Thumbnail: `Border` 80×80 `RoundRectangle 12` `SurfaceContainer` mit `Image` `AspectFill`; per `DataTrigger` auf `IsOnline=False` ausgeblendet. `Image` ohne explizites Caching.
  - `Summary` `BodySmallStyle` `TextSecondary`, `MaxLines="2"`.
  - Fußzeile: Uhr-`Path`-Icon + `Label` `Text="—"` (`MetaStyle`, hartkodiert — keine echte Lesezeit) und zwei Aktions-`Border` 44×44 `RoundRectangle 22` `Transparent` (`ToggleSavedCommand` → Bookmark-`Path` mit `DataTrigger` `IsSavedForLater=True` → `Fill` = **`Primary`** — Entwurf sieht `BookmarkGold`; `MarkReadCommand` → Haken-`Path`). **Beide ohne `SemanticProperties.Description`.**
  - Tap-Overlay: transparentes `Grid` über Zeilen 0–2 mit `TapGestureRecognizer` → `OpenArticleCommand` (Standard: `Shell.Current.GoToAsync("articledetail?itemId=…")` in `ArticleCardView.xaml.cs`); **kein `SemanticProperties`**.
- Bindable Properties (Code-Behind): `OpenArticleCommand` (Default = `AsyncRelayCommand` `OpenArticleAsync`), `ToggleSavedCommand`, `MarkReadCommand`, `IsOnline` (Default `true`).

## `LaterPage` (`src/Reporter/Views/LaterPage.xaml` + `.xaml.cs`)

- `Grid` `Auto,*`, Titel + Offline-Hinweis-`Border`, `CollectionView` `SavedItems` mit `ArticleCardView`-Template. **Kein `RefreshView`, kein Paging** (`RemainingItemsThreshold` nicht gesetzt). `OnAppearing` führt `LoadCommand` aus.

## `CategoriesPage` (`src/Reporter/Views/CategoriesPage.xaml` + `.xaml.cs`)

- `Grid` `Auto,*`; Eingabe-Karte `Border` `Padding="16"` `RoundRectangle 12` `SurfaceContainer` mit `Entry` `NewCategoryName` (ReturnCommand → `SaveCommand`), `ErrorMessage`, `Button` `ButtonSave`. **Kein `SemanticProperties` auf `Entry`.**
- Kategorien-`CollectionView`: Karten `Border` `Padding="16"` `Margin="0,6"` `RoundRectangle 12` `SurfaceContainer`, `TapGestureRecognizer` → `OnCategoryTapped` → `DisplayActionSheetAsync` (Bearbeiten/Löschen) + `DisplayAlertAsync`-Bestätigung. **Keine `SemanticProperties`.**
- Für diese Seite existiert kein eigener Entwurfs-Screen (Annahme in der Anforderung: Karten-Listen-Muster übernehmen).

## `SettingsPage` (`src/Reporter/Views/SettingsPage.xaml` + `.xaml.cs`)

- `Grid` `Auto,*`, `ScrollView` mit `VerticalStackLayout`; Abschnitts-Karten `Border` `Padding="16"` `RoundRectangle 12` `SurfaceContainer`: Aufbewahrung (`Slider` 1–365 + `SaveRetentionCommand`), Keyword-Filter (`Entry` + `Button`, `FlexLayout`-Chips `RoundRectangle 22` `SurfaceVariant` mit ×-`Button` 44×44, Match-Status-Pill `RoundRectangle 12` `SurfaceVariant`), Sync & Lesefluss (`Switch` AutoRefresh + `Picker` Intervall, `Switch` AutoMarkRead + `Picker` Verzögerung), Benachrichtigungen (`Switch` + Summary-`Switch` + Quiet-Hours-`Switch` + zwei `TimePicker` in `RoundRectangle 8`-Karten + Permission-Banner mit `Button`s), Erscheinungsbild (`Picker` `ThemeOptions`), Sprache (`Picker` `LanguageOptions`).
- Accessibility-Ist: `SemanticProperties.Description` ist auf den meisten Eingabe-Controls gesetzt (Slider, Entry, Switches, Picker, TimePicker, Remove-Buttons, Permission-Buttons — 16 Vorkommen). Disabled-Zustände über `Opacity 0.4`-Trigger.
- Code-Behind: `NotificationAuthorizationDenied`-Event → `DisplayAlertAsync` + `AppInfo.Current.ShowSettingsUI()`; `OnOpenNotificationSettingsClicked`.

## `AppShell` (`src/Reporter/AppShell.xaml` + `.xaml.cs`)

- `AppShell.xaml` ist leer (nur `Title="Reporter"`); die `TabBar` wird im Code-Behind aufgebaut: 5 `Tab`s/`ShellContent` mit Titeln `TabUnread`, `TabFeeds`, `TabLater`, `TabCategories`, `TabSettings` — **keine Tab-Icons gesetzt**; Route `articledetail` → `ArticleDetailPage` registriert.
- Tab-Bar-Farben kommen aus dem impliziten `Shell`-Style in `Styles.xaml` (`TabBarBackgroundColor` = `Surface`, `TabBarForegroundColor`/`TabBarTitleColor` = `Primary`, `TabBarUnselectedColor` = `TextSecondary`).
