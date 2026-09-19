<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# UI — Bestandsaufnahme

Bestehende Seiten, Views, Styles und Design-Entwürfe, die für die neue `FeedDetailPage` relevant sind. Eine `FeedDetailPage` existiert noch nicht.

## `FeedsPage.xaml`
Datei: `src/Reporter/Views/FeedsPage.xaml`

- `ContentPage`, `x:DataType="vm:FeedsViewModel"`, `Shell.NavBarIsVisible="False"`, `Grid RowDefinitions="Auto,*"`.
- Kopfbereich: Titel (`HeadlineStyle`), „Feed hinzufügen"-Button (`OpenAddFormCommand`), Fehler-/Offline-/Suchfehler-Labels.
- Feed-Liste: `RefreshView` (`IsRefreshing="{Binding IsSyncing}"`, `Command="{Binding RefreshAllCommand}"`) um `CollectionView` (`ItemsSource="{Binding Feeds}"`, `EmptyView` `PlaceholderFeeds`). Karten-`DataTemplate` (`x:DataType="models:FeedListItem"`): `Border`-Karte mit `TapGestureRecognizer Tapped="OnFeedTapped"`, Favicon/`FeedInitial`-Fallback-Avatar, Titel, `HealthStatus`-Indikatorpunkt, Kategorie/`LastCheckedAt`, `UnreadCount` und Status-Badge (`HealthStatus*Label` mit `DataTrigger`n für `Warning`/`Error`).
- Suchergebnis-Ansicht: eigenes `Grid` mit `CollectionView` (`SearchResults`), sichtbar via `DataTrigger` auf `ShowSearchResults`; Tap → `OnSearchResultTapped`.
- Add/Edit-Sheet: Bottom-Sheet-`Grid` (`DataTrigger` auf `ShowAddForm`), `Entry NewUrlEntry` (`Keyboard="Url"`, `ReturnCommand="{Binding SearchCommand}"`), Such-/Direkt-Add-Buttons (im `IsEditMode` verborgen), Notification-`Switch` + `SaveCommand`-Button (nur `IsEditMode` sichtbar). Sheet-Titel wechselt via Trigger `FeedAddSheetTitle` ↔ `FeedEditSheetTitle`.
- Kein `SearchBar`-Element vorhanden — die Feed-Suche läuft über den `Entry` im Add-Sheet.

## `LaterPage.xaml` — Referenzlayout für die Detailansicht
Datei: `src/Reporter/Views/LaterPage.xaml`

`Grid RowDefinitions="Auto,*,Auto"`: Kopf (Titel, Offline-Hinweis-`Border` via `DataTrigger IsOnline=False`, `ErrorMessage`-Label), `CollectionView` in Zeile `*` (`RemainingItemsThreshold="2"`, `RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}"`, `EmptyView` `PlaceholderLater`), `ActivityIndicator` an `IsLoading`. ItemTemplate: `ArticleCardView` mit `x:Reference PageRoot`-Command-Bindungen. Erfüllt die AGENTS.md-Regeln (kartenbasierte Liste, keine verschachtelten ScrollViews, `Grid`-Zeile `*`).

## `ArticleCardView.xaml`
Datei: `src/Reporter/Views/ArticleCardView.xaml`

Wiederverwendbare Beitragskarte für `ItemListItem` (bereits in `UnreadPage` und `LaterPage` im Einsatz); Default-`OpenArticleCommand` navigiert zu `articledetail?itemId=...`.

## Styles
Datei: `src/Reporter/Resources/Styles/Styles.xaml`

Ein `Style TargetType="SearchBar"` existiert (Zeile ~204) — der in der Anforderung erwähnte vorhandene `SearchBar`-Style. Außerdem vorhanden: `HeadlineStyle`, `HeadlineSmallStyle`, `BodySmallStyle`, `MetaStyle`, `LabelMetaStyle`, `StringNotEmptyToBoolConverter` sowie `Light*`/`Dark*`-Ressourcen für `AppThemeBinding`.

## Design-Entwürfe
Verzeichnis: `design-draft/stitch_local_rss_feed_reader/`

Vorhandene Screens (je light + dark): `ungelesen_dashboard`, `feeds_health_status`, `f_r_sp_ter_bewahren` (Später-Liste), `artikel_lesemodus`, `einstellungen_filter`, Logo-Referenz. **Kein Entwurf für eine Feed-Detailansicht vorhanden** — deckt sich mit der Anmerkung in der Anforderung.

## Lokalisierung
Dateien: `src/Reporter.Core/Resources/Strings/AppResources.resx` (Default), `AppResources.de.resx`, `AppResources.Designer.cs`

Vorhandene, für die Feed-Aktionen genutzte Schlüssel (Auswahl): `ActionSheetTitleFeed`, `ButtonRefresh`, `ButtonRename`, `ButtonChangeCategory`, `ButtonEdit`, `ButtonDelete`, `ButtonShowErrorDetails`, `ButtonCancel`, `ButtonOk`, `ButtonYes`, `ButtonNo`, `ButtonSave`, `ButtonSearch`, `ButtonDirectAdd`, `PromptRenameFeedTitle`, `PromptRenameFeedMessage`, `LabelFeedCategory`, `LabelFeedUnreadCount`, `ConfirmDeleteFeedTitle`, `ConfirmDeleteFeedMessage`, `FeedErrorDetailsTitle`, `FeedErrorKindInsecureHttpBlocked`, `FeedErrorKindHttpStatus`, `FeedErrorKindNetwork`, `FeedErrorKindParse`, `FeedErrorKindUnknown`, `PageTitleFeeds`, `PageTitleLater`, `PlaceholderFeeds`, `PlaceholderLater`, `ErrorLoadFailed`, `OfflineHint`, `SyncStatusError`, `CategoryNone`, `HealthStatus*Label`, `AccessibilityTapForActions`, `AccessibilityDismissSheet`, `FeedAddSheetTitle`, `FeedEditSheetTitle`, `FeedNotificationsLabel`, `FeedNotificationsHint`, `NotificationsIosOnlyHint`. Für Seitentitel, Such-Platzhalter und Leer-Text der neuen Ansicht gibt es noch keine dedizierten Schlüssel.
