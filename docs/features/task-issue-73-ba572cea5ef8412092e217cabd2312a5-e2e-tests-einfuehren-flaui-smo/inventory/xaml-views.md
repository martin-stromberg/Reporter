<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# XAML-Views: Compiled-Bindings-Status, Code-Behind, Automation-Anker

Bestandsaufnahme aller XAML-Dateien unter `src/Reporter/` bezogen auf die Anforderung
(Compiled Bindings via `x:DataType` nachrüsten, UI-Ankerpunkte für die FlaUI-Suite).

## Übersicht: `x:DataType` je Datei

| Datei | `x:DataType` vorhanden? | Details |
|-------|--------------------------|---------|
| `src/Reporter/Views/FeedsPage.xaml` | **Nein** — weder auf `ContentPage`- noch auf `DataTemplate`-Ebene | `xmlns:vm` (→ `Reporter.Core.ViewModels`, assembly `Reporter.Core`) bereits deklariert (Zeile 7); `xmlns:models` fehlt; `x:Name="Page"` vorhanden (Zeile 9) |
| `src/Reporter/Views/CategoriesPage.xaml` | **Nein** | weder `xmlns:vm` noch `xmlns:models` deklariert; kein `x:Name` auf der Page |
| `src/Reporter/Views/ArticleCardView.xaml` | **Nein** | `ContentView` mit `x:Name="Card"` (Zeile 7); Binding-Kontext ist `ItemListItem` (wird in `DataTemplate`s von `UnreadPage`/`LaterPage` mit `x:DataType="models:ItemListItem"` instanziiert — die Templates sind getypt, das `ContentView` selbst nicht) |
| `src/Reporter/Views/ArticleDetailPage.xaml` | Ja — `x:DataType="vm:ArticleDetailViewModel"` auf Seitenebene (Zeile 9) | `xmlns:vm` zeigt auf `Reporter.Core.ViewModels`, **assembly=Reporter** (nicht Reporter.Core, da `ArticleDetailViewModel` unter `src/Reporter/ViewModels/` liegt) |
| `src/Reporter/Views/UnreadPage.xaml` | Teilweise — nur in `DataTemplate`s (Zeilen 115, 196) | kein Seiten-`x:DataType`; `xmlns:models` deklariert; `x:Name="PageRoot"` |
| `src/Reporter/Views/LaterPage.xaml` | Teilweise — nur im `DataTemplate` (Zeile 45) | kein Seiten-`x:DataType`; `xmlns:models` deklariert; `x:Name="PageRoot"` |
| `src/Reporter/Views/SettingsPage.xaml` | Teilweise — nur im `DataTemplate` (Zeile 84) | kein Seiten-`x:DataType`; `xmlns:models` deklariert; `x:Name="PageRoot"` |
| `src/Reporter/App.xaml` | entfällt | keine Bindings; nur leeres `ResourceDictionary` |
| `src/Reporter/AppShell.xaml` | entfällt | keine Bindings; Shell-Struktur wird im Code-Behind (`AppShell.xaml.cs`) aufgebaut |
| `src/Reporter/Platforms/Windows/App.xaml` | entfällt | `MauiWinUIApplication`, keine Bindings |
| `src/Reporter/Resources/Styles/Colors.xaml`, `Styles.xaml` | entfällt | `ResourceDictionary`s; nur `x:Name` auf `VisualState`s, keine Daten-Bindings |

Abweichung zur Anforderungsannahme: Die Anforderung nennt nur `FeedsPage.xaml`,
`CategoriesPage.xaml` und `ArticleCardView.xaml` als Kandidaten. Tatsächlich fehlt
das **seitenebene** `x:DataType` zusätzlich auf `UnreadPage.xaml`, `LaterPage.xaml`
und `SettingsPage.xaml` — dort sind lediglich die `DataTemplate`s typisiert, die
Seiten-Bindings (Commands, Fehler-/Statuslabels) laufen weiterhin unkompiliert.

## `FeedsPage.xaml` — relevante Bindings und Anker

Datei: `src/Reporter/Views/FeedsPage.xaml` (436 Zeilen), Code-Behind `FeedsPage.xaml.cs`.

- Seiten-Bindings gegen `FeedsViewModel`: `OpenAddFormCommand`, `ErrorMessage`, `HasError`,
  `IsOnline` (DataTrigger), `SearchErrorMessage`, `HasSearchError`, `SyncErrorMessage`,
  `HasSyncError`, `ShowSearchResults`, `SearchResults`, `CloseSearchResultsCommand`,
  `IsSyncing` (RefreshView, TwoWay), `RefreshAllCommand`, `Feeds`, `ShowAddForm`,
  `CloseAddFormCommand`, `NewUrl`, `IsSearching`, `SearchCommand`, `DirectAddCommand`,
  `IsEditMode` (mehrere DataTrigger), `NotificationsSupported`, `FeedNotificationsEnabled`,
  `SaveCommand`.
- `DataTemplate` Suchergebnisse (Zeilen 70–119): bindet `FeedSearchResult`-Mitglieder
  `DisplayTitle`, `Description`, `SiteName` (TargetNullValue/FallbackValue `—`),
  `SiteUrl`, `FeedUrl`, `Title`; `TapGestureRecognizer Tapped="OnSearchResultTapped"`
  mit `CommandParameter="{Binding .}"`.
- `DataTemplate` Feed-Liste (Zeilen 145–298): bindet `FeedListItem`-Mitglieder `Title`,
  `FaviconUrl`, `FeedInitial`, `CategoryName`, `LastCheckedAt`, `UnreadCount`,
  `HealthStatus` (DataTrigger-Werte `OK`/`Warning`/`Error` — `FeedHealth`-Konstanten);
  `TapGestureRecognizer Tapped="OnFeedTapped"` mit `CommandParameter="{Binding .}"`.
- `x:Reference`-Bindings, die beim Setzen von `x:DataType` typkorrekt bleiben müssen:
  `BindingContext.IsOnline, Source={x:Reference Page}` in `BindingCondition`s
  (Zeilen 176, 191) — `IsOnline` kommt aus `BaseViewModel`.
- `x:Name`/Semantik-Anker: `Page` (Zeile 9), `NewUrlEntry` (Zeile 359, mit
  `SemanticProperties.Description`=PlaceholderFeedSearch), `SemanticProperties.Description`
  auf den Listenelementen (`{Binding DisplayTitle}` bzw. `{Binding Title}`) und auf der
  Dismiss-Fläche des Add-Sheets (`AccessibilityDismissSheet`, Zeile 312), Switch mit
  `FeedNotificationsLabel` (Zeile 409).
- Buttons ohne eigenen `x:Name`/`AutomationId`: „+", „Suchen", „Direkt hinzufügen",
  „Abbrechen", „Speichern", „Suchergebnisse schließen" — per `Text` (lokalisiert,
  `x:Static strings:…`) bzw. UIA-Name ansprechbar.

## `FeedsPage.xaml.cs` — Code-Behind-Verdrahtung

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `FeedsPage(FeedsViewModel)` | public ctor | `InitializeComponent`, `BindingContext`, verdrahtet `viewModel.ConfirmDirectAddAsync` mit der Page-Methode |
| `OnAppearing` / `OnDisappearing` | protected override | (de-)abonniert `_viewModel.PropertyChanged` → `OnViewModelPropertyChanged`; führt `LoadCommand` aus |
| `OnFeedTapped(sender, TappedEventArgs)` | private async void | `DisplayActionSheetAsync` mit Aktionen Aktualisieren/Umbenennen/Kategorie ändern/Bearbeiten/(Fehlerdetails)/Löschen; routet zu `RefreshCommand`, `RenameFeedAsync`, `ChangeCategoryAsync`, `EditCommand`, `ShowFeedErrorDetailsAsync`, `ConfirmDeleteFeedAsync` |
| `RenameFeedAsync` | private | `DisplayPromptAsync` → `viewModel.RenameFeedAsync(feed, newTitle)` |
| `ChangeCategoryAsync` | private | `DisplayActionSheetAsync` über `viewModel.Categories` (inkl. Pseudo-Eintrag „Keine Kategorie" mit `Id == Guid.Empty`, erzeugt in `FeedsViewModel.LoadAsync`); Eindeutigkeit via `FeedsViewModel.MakeUniqueOptionLabels`; ruft `ChangeFeedCategoryAsync` |
| `ConfirmDeleteFeedAsync` | private | `DisplayAlertAsync` (Ja/Nein) → `DeleteCommand` |
| `ShowFeedErrorDetailsAsync` | private | `DisplayAlertAsync` mit `viewModel.GetFeedErrorMessage(feed)` |
| `OnSearchResultTapped(sender, TappedEventArgs)` | private async void | `DisplayAlertAsync` (Abo-Bestätigung) → `SubscribeResultCommand` |
| `OnBackButtonPressed` | protected override | schließt das Add-Sheet (`CloseAddFormCommand`), wenn `ShowAddForm` |
| `ConfirmDirectAddAsync(string url)` | private | `DisplayAlertAsync`-Fallback „URL direkt hinzufügen?"; wird vom ViewModel als `Func<string, Task<bool>>` aufgerufen |
| `OnViewModelPropertyChanged` | private | bei `ShowAddForm == true`: `Dispatcher.Dispatch(() => NewUrlEntry.Focus())` — Fokus-Verdrahtung für den „+"-Smoke-Test |

## `CategoriesPage.xaml` — relevante Bindings und Anker

Datei: `src/Reporter/Views/CategoriesPage.xaml` (71 Zeilen), Code-Behind `CategoriesPage.xaml.cs`.

- Seiten-Bindings gegen `CategoriesViewModel`: `NewCategoryName`, `SaveCommand`
  (auch als `ReturnCommand` des Entry), `ErrorMessage`, `HasError`, `Categories`.
- `DataTemplate` (Zeilen 40–67): bindet `CategoryWithCount`-Mitglieder `Name`,
  `FeedCount`; `TapGestureRecognizer Tapped="OnCategoryTapped"` mit `CommandParameter="{Binding .}"`.
- Anker: `x:Name="CategoryNameEntry"` (Zeile 21, mit `SemanticProperties.Description`),
  `SemanticProperties.Description="{Binding Name}"` auf den Kategorie-Karten (Zeile 45).
- Code-Behind: `OnAppearing` → `LoadCommand`; `OnCategoryTapped` →
  `DisplayActionSheetAsync` (Bearbeiten/Löschen), Bearbeiten → `EditCommand` +
  `CategoryNameEntry?.Focus()`, Löschen → `DisplayAlertAsync` + `DeleteCommand`.

## `ArticleCardView.xaml` — relevante Bindings und Anker

Datei: `src/Reporter/Views/ArticleCardView.xaml` (244 Zeilen), Code-Behind `ArticleCardView.xaml.cs`.

- Item-Bindings (Kontext `ItemListItem`): `IsRead`, `FeedTitle`, `PublishedAt`,
  `CategoryName`, `Title`, `ImageUrl`, `FeedFaviconUrl`, `FeedInitial`, `Summary`,
  `ReadingTimeText`, `IsSavedForLater`.
- `x:Reference Card`-Bindings auf die eigenen `BindableProperty`s des ContentView:
  `IsOnline` (Zeile 85), `ToggleSavedCommand` (183), `MarkReadCommand` (212),
  `OpenArticleCommand` (238); jeweils `CommandParameter="{Binding .}"`.
- Code-Behind definiert `BindableProperty`s `OpenArticleCommand` (Default:
  `AsyncRelayCommand` → `Shell.Current.GoToAsync("articledetail?itemId=…")`),
  `ToggleSavedCommand`, `MarkReadCommand`, `IsOnline` (Default `true`).
- Anker: `x:Name="Card"`, `SemanticProperties.Description` für Lesezeichen-,
  Gelesen- und Öffnen-Aktionen (lokalisiert).

## Sonstige für die FlaUI-Suite relevante Anker

- `UnreadPage.xaml`: `x:Name="ArticlesCollection"` (Zeile 190), Refresh-/„Alle als
  gelesen"-Buttons mit `SemanticProperties.Description`, Karten-Description
  `{Binding AccessibilityDescription}` (Zeile 121).
- `SettingsPage.xaml`: zahlreiche `SemanticProperties.Description` auf allen
  Settings-Controls.
- `ArticleDetailPage.xaml`: `x:Name="ArticleWebView"` (Zeile 332), Descriptions für
  Zurück, Lesezeichen, Schriftgröße, Gelesen, Teilen, „Im Browser öffnen".
- `AppShell.xaml.cs`: Tabs werden im Code-Behind mit lokalisierten Titeln
  (`AppResources.TabUnread/TabFeeds/TabLater/TabCategories/TabSettings`) aufgebaut —
  Tab-Umschaltung per UIA-Name möglich.
- `App.xaml.cs` `CreateWindow`: Windows-Fenster startet fest mit 390 × 844 pt.
