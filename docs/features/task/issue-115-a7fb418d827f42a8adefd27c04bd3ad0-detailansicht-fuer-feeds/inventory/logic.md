<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik — Bestandsaufnahme

ViewModels, Page-Code-Behind, Repositories und Navigations-/DI-Infrastruktur, die für die Feed-Detailansicht relevant sind. Ein `FeedDetailViewModel` existiert noch nicht (kein Treffer im gesamten `src/`).

## `FeedsViewModel`
Dateien: `src/Reporter.Core/ViewModels/FeedsViewModel.cs`, `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs` (partial)

`public partial class FeedsViewModel : BaseViewModel`. DI-Lebensdauer: **Singleton** (`MauiProgram.cs`, Zeile 116). Abhängigkeiten: `IFeedRepository`, `ICategoryRepository`, `IFeedSyncService`, `IFeedSearchService`, `IFeedIconService`, `INetworkStatusService`, optional `ILocalNotificationService`.

### Commands

| Methode / Command | Sichtbarkeit | Kurzbeschreibung |
|-------------------|-------------|------------------|
| `LoadCommand` → `LoadCommandAsync`/`LoadAsync` | public Command / private | Lädt Kategorien (inkl. Pseudo-Eintrag `Guid.Empty`/`AppResources.CategoryNone`) und `FeedListItem`s via `GetAllWithDetailsAsync`; wird von `FeedsPage.OnAppearing` ausgeführt |
| `SaveCommand` → `SaveAsync` | public Command / private | Persistiert den im Edit-Modus bearbeiteten `SelectedFeed` (URL-/Titel-/Duplikat-Validierung); kehrt ohne `SelectedFeed` sofort zurück |
| `EditCommand` → `EditAsync(FeedListItem?)` | public Command / private | Befüllt Formular (`NewUrl`, `NewTitle`, `FeedNotificationsEnabled`), setzt `IsEditMode`/`ShowAddForm` — öffnet das Add/Edit-Sheet der `FeedsPage` |
| `DeleteCommand` → `DeleteAsync(FeedListItem?)` | public Command / private | Löscht Feed via `_feedRepository.DeleteAsync`, setzt Formular zurück und lädt neu |
| `RefreshCommand` → `RefreshAsync(FeedListItem?)` | public Command / private | Einzel-Feed-Sync via `_feedSyncService.SyncFeedAsync(feed.Id)`; CanExecute `!IsSyncing` |
| `RefreshAllCommand` → `RefreshAllAsync` | public Command / private | `_feedSyncService.SyncAllAsync()`; an `RefreshView` der `FeedsPage` gebunden |
| `SearchCommand` → `SearchAsync` | public Command / private (Search.cs) | Feed-Suche; CanExecute `IsOnline && !IsSearching && !IsEditMode` |
| `DirectAddCommand` → `DirectAddAsync` | public Command / private (Search.cs) | Direkt-Add ohne Suche; offline verfügbar, nicht im Edit-Modus |
| `SubscribeResultCommand` → `SubscribeResultAsync(FeedSearchResult?)` | public Command / private (Search.cs) | Abonniert ein Suchergebnis |
| `CloseSearchResultsCommand` → `CloseSearchResults` | public Command / private (Search.cs) | Schließt die Ergebnisansicht |
| `OpenAddFormCommand` → `OpenAddForm` | public Command / private | Öffnet das Sheet im Add-Modus (ruft `ResetForm`) |
| `CloseAddFormCommand` → `ResetForm` | public Command / private | Schließt Sheet, setzt kompletten Formularzustand zurück |

### Weitere Methoden / Eigenschaften

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `RenameFeedAsync(FeedListItem?, string?)` | public | Umbenennen über `FeedRepository.UpdateAsync` + `ToFeed`-Rekonstruktion; leerer Titel setzt `ErrorMessage` |
| `ChangeFeedCategoryAsync(FeedListItem?, Category?)` | public | Kategorie-Zuordnung ändern; `Guid.Empty` löscht die Zuordnung |
| `GetFeedErrorMessage(FeedListItem)` | public | Baut lokalisierten Fehlertext aus `LastErrorKind` (`FeedErrorKind*`-Ressourcen) + technischer Nachricht |
| `MakeUniqueOptionLabels(IReadOnlyList<string>)` | public static | Erzeugt kollisionsfreie Aktionsblatt-Labels („Name (2)"); wird von `FeedsPage.ChangeCategoryAsync` verwendet |
| `SyncAsync(Func<Task<SyncResult>>)` | private | Reentrancy-Guard `_isSyncInProgress`; Offline-Early-Return setzt `IsSyncing=false` zurück; Fehler → `SyncErrorMessage = AppResources.SyncStatusError`; danach `LoadAsync` |
| `ToFeed(FeedListItem, url, title, categoryId, notificationsEnabled)` | private static | Rekonstruiert `Feed` aus `FeedListItem` für Partial-Updates |
| `OnConnectivityChanged(bool)` | protected override | Leert `SyncErrorMessage`/`SearchErrorMessage`, benachrichtigt `SearchCommand` |
| `ConfirmDirectAddAsync` (`Func<string, Task<bool>>?`) | public Property | Callback, den `FeedsPage` mit `DisplayAlertAsync` verdrahtet |
| `Feeds`, `Categories` | public Properties | `ObservableCollection<FeedListItem>` / `ObservableCollection<Category>` |
| `SelectedFeed`, `NewUrl`, `NewTitle`, `FeedNotificationsEnabled`, `ShowAddForm`, `IsEditMode`, `IsSyncing`, `ErrorMessage`/`HasError`, `SyncErrorMessage`/`HasSyncError` | public Properties | Formular-/Anzeigezustand |
| `SearchResults`, `ShowSearchResults`, `IsSearching`, `SearchErrorMessage`/`HasSearchError`, `NotificationsSupported` | public Properties (Search.cs) | Suchzustand |

Abonnierte Events: `INetworkStatusService.ConnectivityChanged` (via `BaseViewModel.TrackConnectivity`).
Publizierte Events: keine eigenen; `PropertyChanged` (von `FeedsPage` für `ShowAddForm`-Fokus abonniert).

## `LaterViewModel` — Paging-Muster für die Detailansicht
Datei: `src/Reporter.Core/ViewModels/LaterViewModel.cs`

`public partial class LaterViewModel : BaseViewModel`. DI-Lebensdauer: **Singleton**. Abhängigkeiten: `IItemRepository`, `INetworkStatusService`.

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `PageSize` | public const `int = 20` | Seitengröße des Infinite-Scroll |
| `LoadCommand` → `LoadAsync` | public Command / private | Lädt Seite 0 neu: `_currentPage = 0`, `HasMore = true`, `SavedItems.Clear()` |
| `LoadMoreCommand` → `LoadMoreAsync` | public Command / private | Nächste Seite; CanExecute `HasMore && !IsLoading`; doppelte Prüfung innerhalb der Sperre |
| `ToggleSavedCommand` → `ToggleSavedAsync(ItemListItem?)` | public Command / private | `ToggleSavedForLaterAsync` + Entfernen aus `SavedItems` |
| `MarkReadCommand` → `MarkReadAsync(ItemListItem?)` | public Command / private | `MarkAsReadAsync` + Ersetzen durch `item.CopyWith(isRead: true)` |
| `LoadPageCoreAsync` | private | `_itemRepository.GetSavedForLaterAsync(_currentPage, PageSize)`; `HasMore = items.Count == PageSize`; Fehler → `ErrorMessage = AppResources.ErrorLoadFailed`, `HasMore = false` |
| `_loadLock` (`SemaphoreSlim(1,1)`) | private | Ladesperre gegen parallele Seitenladungen |
| `SavedItems` | public `ObservableCollection<ItemListItem>` | Listendatenquelle |
| `IsLoading`, `HasMore`, `ErrorMessage`/`HasError`, `Title` | public Properties | Anzeigezustand |

## `BaseViewModel`
Datei: `src/Reporter.Core/ViewModels/BaseViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `IsOnline` | public Property | Aktueller Connectivity-Status |
| `InitConnectivity` / `TrackConnectivity` | protected | Service setzen / `ConnectivityChanged` abonnieren (idempotent) |
| `UntrackConnectivity` | protected | Abmeldung (idempotent); z. B. von `ArticleDetailViewModel` beim Verlassen genutzt |
| `RefreshConnectivityStatus` | protected | Status neu lesen, `OnConnectivityChanged` bei Änderung |
| `OnConnectivityChanged(bool)` | protected virtual | Erweiterungspunkt |

## `FeedsPage` (Code-Behind)
Datei: `src/Reporter/Views/FeedsPage.xaml.cs`

`public partial class FeedsPage : ContentPage`. DI-Lebensdauer: **transient**; Singleton-`FeedsViewModel` wird über `BindingContext` gehalten, `PropertyChanged` nur zwischen `OnAppearing`/`OnDisappearing` abonniert.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnFeedTapped(object?, TappedEventArgs)` | private | **Zu ändernder Einstiegspunkt.** Öffnet aktuell `DisplayActionSheetAsync` mit `ButtonRefresh`, `ButtonRename`, `ButtonChangeCategory`, `ButtonEdit`, bei `HealthStatus == FeedHealth.Error` zusätzlich `ButtonShowErrorDetails`, plus `ButtonDelete`; verteilt auf ViewModel-Commands/Hilfsmethoden |
| `RenameFeedAsync(FeedsViewModel, FeedListItem)` | private | `DisplayPromptAsync` → `viewModel.RenameFeedAsync` |
| `ChangeCategoryAsync(FeedsViewModel, FeedListItem)` | private | `DisplayActionSheetAsync` über Kategorienamen (Cancel-Name ausgeschlossen, Labels via `MakeUniqueOptionLabels`) → `ChangeFeedCategoryAsync` |
| `ConfirmDeleteFeedAsync(FeedsViewModel, FeedListItem)` | private | `DisplayAlertAsync` → `DeleteCommand.ExecuteAsync(feed)` |
| `ShowFeedErrorDetailsAsync(FeedsViewModel, FeedListItem)` | private | `DisplayAlertAsync` mit `viewModel.GetFeedErrorMessage(feed)` |
| `OnSearchResultTapped(object?, TappedEventArgs)` | private | Subscribe-Bestätigung für `FeedSearchResult` |
| `OnBackButtonPressed` | protected override | Schließt offenes `ShowAddForm`-Sheet statt Navigation |
| `ConfirmDirectAddAsync(string)` | private | `DisplayAlertAsync`-Callback für `FeedsViewModel.ConfirmDirectAddAsync` |
| `OnViewModelPropertyChanged` | private | Fokussiert `NewUrlEntry`, wenn `ShowAddForm` sichtbar wird |

## `LaterPage` — Infinite-Scroll-Muster (XAML + Code-Behind)
Dateien: `src/Reporter/Views/LaterPage.xaml`, `src/Reporter/Views/LaterPage.xaml.cs`

`ContentPage` mit `Grid RowDefinitions="Auto,*,Auto"`: `CollectionView` (Zeile `*`) mit `ItemsSource="{Binding SavedItems}"`, `RemainingItemsThreshold="2"`, `RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}"`, `EmptyView` aus `AppResources.PlaceholderLater`; `ActivityIndicator` an `IsLoading` gebunden. `ItemTemplate` nutzt `ArticleCardView` mit über `x:Reference PageRoot` gebundenen Commands. Code-Behind führt `LoadCommand` in `OnAppearing` aus.

## `ArticleDetailPage` — `IQueryAttributable`-Muster
Datei: `src/Reporter/Views/ArticleDetailPage.xaml.cs`

`public partial class ArticleDetailPage : ContentPage, IQueryAttributable`. `ApplyQueryAttributes` liest `query["itemId"]`, parst `Guid`, ruft `_viewModel.LoadAsync(itemId)`. `OnAppearing`/`OnDisappearing` rufen `AttachConnectivity`/`DetachConnectivity` des ViewModels. Transient registriert (`MauiProgram.cs` Zeile 121).

## `ArticleDetailViewModel`
Datei: `src/Reporter/ViewModels/ArticleDetailViewModel.cs` (Ablage im App-Projekt, Namespace `Reporter.Core.ViewModels`)

`BaseViewModel` mit `LoadAsync(Guid itemId)`, Commands `GoBackCommand`, `ToggleSavedForLaterCommand`, `ToggleMarkReadCommand`, `ToggleAutoMarkReadCommand`, `OpenInBrowserCommand`; Abhängigkeiten `IItemRepository`, `IFeedRepository`, `ISettingsRepository`, `INetworkStatusService`.

## `ArticleCardView`
Dateien: `src/Reporter/Views/ArticleCardView.xaml`, `src/Reporter/Views/ArticleCardView.xaml.cs`

`ContentView` mit Bindable Properties `OpenArticleCommand` (Default: `AsyncRelayCommand<ItemListItem?>` → `Shell.Current.GoToAsync($"articledetail?itemId={item.Id}")`), `ToggleSavedCommand`, `MarkReadCommand`, `IsOnline` (Default `true`, steuert Thumbnail-Sichtbarkeit).

## `ItemRepository`
Datei: `src/Reporter.Data/Repositories/ItemRepository.cs`

`public class ItemRepository : IItemRepository`; Singleton (`MauiProgram.cs` Zeile 83). Abhängigkeiten: `IDbContextFactory<ReporterDbContext>`, `IItemContentStore`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetByFeedAsync(Guid feedId)` | public | **Bestehende, ungeeignete Variante:** liefert `IReadOnlyList<Item>` (volle Domänenobjekte inkl. Content-Hydration), ungepaged, `OrderByDescending(i => i.PublishedAt)` ohne `ThenBy`-Tiebreak, kein Suchfilter |
| `GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId, bool ascending)` | public | Paged-Muster: `Where(i => !i.IsRead)`, optionaler Feed-ID-Subquery für Kategorie, `OrderByDescending(PublishedAt).ThenBy(Id)` (bzw. aufsteigend mit `ThenByDescending(Id)`), `Skip`/`Take`, `SelectListItemRows`-Projektion |
| `GetSavedForLaterAsync(int page, int pageSize)` | public | Paged-Muster wie oben mit `Where(i => i.IsSavedForLater)` |
| `SelectListItemRows(IQueryable<ItemEntity>)` | private static | Gemeinsame Projektion auf `ItemListRow` inkl. `Feed.Title`, `Feed.FaviconUrl`, `Feed.CategoryId`, `Feed.Category.Name` — vom Anforderungsdokument zur Wiederverwendung vorgesehen |
| `MapToListItem(ItemListRow, contentHtml, hasLocalImage)` | private | Baut `ItemListItem` mit `ImageUrl` (`ItemImageService.ExtractFirstImageUrl`), `LocalImageLoader`, `Summary` (`ExtractSummary`), `ReadingTimeText` (`ReadingTimeEstimator.EstimateText`) |
| `GetContentsAsync`, `LoadImageStreamAsync`, `ExtractImageUrl`, `ExtractSummary` | private | Content-Store-/Projektionshelfer |
| `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `GetUnreadByDateAsync()`, `GetUnreadCountAsync`, `MarkAllAsReadAsync`, `ToggleSavedForLaterAsync`, `MarkAsReadAsync`, `GetByCategoryAsync`, `AddRangeAsync`, `DeleteExpiredAsync`, `GetExpiredKeywordCandidatesAsync`, `DeleteRangeAsync`, `GetAllIdsAsync` | public | Übrige `IItemRepository`-Implementierung |

## `FeedRepository`
Datei: `src/Reporter.Data/Repositories/FeedRepository.cs`

Relevant für die Detailansicht: `GetByIdAsync(Guid)` (Feed-Stammdaten laden) und `GetAllWithDetailsAsync()` (projiziert `FeedListItem` inkl. `UnreadCount`-Subquery und Kategorie-Join). `DeleteAsync` löscht Items kaskadierend inkl. Content/Bildern (E2E-/Unit-Test-belegt).

## `AppShell`
Dateien: `src/Reporter/AppShell.xaml`, `src/Reporter/AppShell.xaml.cs`

Registriert bisher genau eine Route: `Routing.RegisterRoute("articledetail", typeof(ArticleDetailPage))` (Zeile 22). Tabs (`unread`, `feeds`, `later`, `categories`, `settings`) werden programmatisch mit DI-aufgelösten Pages aufgebaut. Eine Route `feeddetail` existiert nicht.

## `MauiProgram`
Datei: `src/Reporter/MauiProgram.cs`

DI-Registrierungen (Zeilen 78–127): Repositories/Services als Singletons; `UnreadViewModel`, `FeedsViewModel`, `LaterViewModel`, `CategoriesViewModel`, `SettingsViewModel` als Singletons; `ArticleDetailViewModel`, `ArticleDetailPage`, `UnreadPage`, `FeedsPage`, `LaterPage`, `CategoriesPage`, `SettingsPage`, `AppShell` als Transients. `FeedDetailViewModel`/`FeedDetailPage` sind nicht registriert.
