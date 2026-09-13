<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Logikklassen (ViewModels / Services / Code-Behind)

## `UnreadViewModel`
Datei: `src/Reporter.Core/ViewModels/UnreadViewModel.cs`

Konstante `PageSize = 20`. Abhängigkeiten: `IItemRepository`, `ICategoryRepository`, `IFeedSyncService`, `INetworkStatusService` (Connectivity via `BaseViewModel.TrackConnectivity`).

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `LoadCommand` → `LoadAsync` | public Command / private | Lädt Kategorien + Unread-Counts (`ICategoryRepository.GetAllAsync`, `IItemRepository.GetUnreadCountAsync` pro Kategorie + gesamt), baut `Categories` (erster Eintrag `FilterAll`, `CategoryId = null`), wählt `SelectedCategory`, lädt Seite 0 |
| `LoadMoreCommand` → `LoadMoreAsync` | public Command / private | Inkrementelles Paging (`CanExecute`: `!IsLoading && HasMore`) |
| `LoadPageAsync(page, append)` | private | `IItemRepository.GetUnreadByDateAsync(page, PageSize, categoryId)`; `HasMore = items.Count == PageSize`; aktualisiert `SelectedCategory.Count`, `UnreadCount`, `SelectedCategoryText`, `UnreadCountText`; Fehler → `ErrorMessage` = `ErrorLoadFailed` |
| `RefreshCommand` → `RefreshAsync` | public Command / private | Offline → `IsSyncing=false`, Abbruch. Sonst `IFeedSyncService.SyncAllAsync`; `Status == FeedHealth.Error` oder Exception → `SyncErrorMessage` = `SyncStatusError`; danach `LoadAsync` |
| `MarkAllReadCommand` → `MarkAllReadAsync` | public Command / private | `IItemRepository.MarkAllAsReadAsync(SelectedCategory?.CategoryId)` + `LoadAsync` |
| `SelectCategoryCommand` → `SelectCategoryAsync` | public Command (`CategoryFilterItem?`) / private | Setzt `SelectedCategory`, resettet Paging, lädt Seite 0 — direkt für eine Chip-Leiste nutzbar |
| `ToggleSavedCommand` → `ToggleSavedAsync` | public Command (`ItemListItem?`) / private | `ToggleSavedForLaterAsync` + In-Place-Austausch des Eintrags in `Articles` |
| `MarkReadCommand` → `MarkReadAsync` | public Command (`ItemListItem?`) / private | `MarkAsReadAsync`, entfernt Eintrag, dekrementiert `SelectedCategory.Count`; bei leerer Liste `LoadAsync` |
| `Articles` | public | `ObservableCollection<ItemListItem>` |
| `Categories` | public | `ObservableCollection<CategoryFilterItem>` — Datenbasis für Chip-Leiste |
| `SelectedCategory` | public | Setzt `IsSelected` auf allen Einträgen (`UpdateCategorySelection`) und `SelectedCategoryText` = `"{Name} ({Count})"` |
| `SelectedCategoryText` | public | Wird auf `UnreadPage` als `MetaStyle`-Zeile angezeigt; bei sichtbaren Chips ggf. obsolet |
| `IsSyncing`, `IsLoading`, `HasMore`, `UnreadCount`, `UnreadCountText`, `LastSyncText`, `ErrorMessage`/`HasError`, `SyncErrorMessage`/`HasSyncError` | public | Status-/Fehlerproperties |
| `OnConnectivityChanged` | protected override | Leert `SyncErrorMessage` |

Abonnierte Events: `INetworkStatusService.ConnectivityChanged` (via `BaseViewModel`).

## `LaterViewModel`
Datei: `src/Reporter.Core/ViewModels/LaterViewModel.cs`

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `LoadCommand` → `LoadAsync` | public Command / private | `SavedItems = await IItemRepository.GetSavedForLaterAsync()` — **lädt die komplette Liste ohne Paging** |
| `ToggleSavedCommand` → `ToggleSavedAsync` | public Command (`ItemListItem?`) / private | `ToggleSavedForLaterAsync` + vollständiges `LoadAsync` |
| `MarkReadCommand` → `MarkReadAsync` | public Command (`ItemListItem?`) / private | `MarkAsReadAsync` + vollständiges `LoadAsync` |
| `SavedItems` | public | `IReadOnlyList<ItemListItem>` (keine `ObservableCollection`, kein `HasMore`/`LoadMoreCommand`) |
| `Title` | public | Seitentitel |

Abonnierte Events: `INetworkStatusService.ConnectivityChanged` (via `BaseViewModel.TrackConnectivity`).

## `FeedSyncService`
Datei: `src/Reporter.Core/Services/FeedSyncService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(feedId, ct)` | public | Offline → `SyncResult(Error)` ohne SyncLog; legt `SyncLog` an, lädt `Feed`; ruft `RunSyncAsync`; fängt alle Nicht-`OperationCanceledException` → `FeedHealth.Error` + `UpdateFeedHealthAsync` + `UpdateLogAsync` |
| `SyncAllAsync(ct)` | public | Offline → `SyncResult(Error)`; iteriert alle Feeds **sequenziell** über `SyncFeedAsync` (Fehlerisolation pro Feed), aggregiert Status (`Error` > `Warning` > `Ok`) und Messages |
| `RunSyncAsync(feed, log, ct)` | private | **N+1-Muster:** lädt `existingItems` via `GetByFeedAsync` (nur für Count + `lastPublishedAt` genutzt), dann pro Feed-Item `GetByGuidOrHashAsync` + `AddAsync` — jedes `AddAsync` öffnet eigenen `ReporterDbContext` + `SaveChanges`; `existingItems` wird **nicht** für In-Memory-Dedup verwendet |
| `DetermineStatus` | private static | `Warning` bei <50 % Abrufmenge oder >30 Tage ohne neue Items, sonst `Ok` |
| `UpdateFeedHealthAsync` | private | Setzt `HealthStatus`, `HealthLastChange` (nur bei `FeedHealth.Changed`), `LastCheckedAt`, ggf. aufgelösten Titel |
| `UpdateLogAsync` | private | `ISyncLogRepository.UpdateAsync` mit `FinishedAt`, `Status`, `Message` |
| `GetContentHtml`, `NormalizeGuidOrHash` (SHA256-Fallback), `IsHostPlaceholderTitle` | private static | Content-Extraktion, Dedup-Schlüssel, Platzhalter-Titel-Erkennung (`FeedTitleFallback.IsFileNamePlaceholderTitle`) |

Nach dem Insert: `INotificationService.NotifyNewItemsAsync` für `newItemEntities`, Fehler dort isoliert (nur `Debug.WriteLine`). Alle Await-Pfade mit `ConfigureAwait(false)` — kein UI-Thread-Marshal im Service.

## `AutoRefreshService`
Datei: `src/Reporter.Core/Services/AutoRefreshService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartAsync(ct)` | public | Lädt `Settings` via `ISettingsRepository.GetAsync` → `ApplySettingsAsync` |
| `ApplySettingsAsync(settings)` | public | Unter `SemaphoreSlim _stateLock`: `StopLoopAsync`, dann bei `AutoRefreshEnabled` `PeriodicTimer`-Loop mit auf 1–1440 min geclamptem Intervall (`MinRefreshIntervalMinutes`/`MaxRefreshIntervalMinutes`) |
| `StopAsync` | public | Stoppt Loop unter `_stateLock` |
| `StopLoopAsync` | private | Cancel + Await des Loop-Tasks, Dispose des CTS |
| `RunLoopAsync(interval, ct)` | private | `PeriodicTimer` (`TimeProvider`-fähig); pro Tick: Skip wenn `!IsOnline`; `SyncAllAsync`; Nicht-Cancel-Exceptions geloggt, Loop läuft weiter |

Keine UI-Abhängigkeit/Marshal im Code (reine `Reporter.Core`-Klasse ohne `Dispatcher`).

## `AppThemeService`
Datei: `src/Reporter/Services/AppThemeService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ApplyTheme(theme)` | public | Mappt `SettingsValues.ThemeLight`/`ThemeDark` auf `Application.Current.UserAppTheme`; `system`/unbekannt → `AppTheme.Unspecified`; `null`-Application-sicher |

## `ArticleDetailViewModel`
Datei: `src/Reporter/ViewModels/ArticleDetailViewModel.cs` (Namespace `Reporter.Core.ViewModels`, Projekt `Reporter`)

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `LoadAsync(itemId)` | public | Lädt `Settings` (Fallback-Defaults bei Fehler), `Item`, `Feed`; setzt `IsAutoMarkReadAvailable`, `AutoMarkReadLabel`, `FeedName`, `PublishedAtText`, `ReadingTime` (`CalculateReadingTime`, 200 wpm); `RebuildHtml`; startet `MarkReadDelayedAsync` bei Auto-Mark |
| `RebuildHtml` | private | Wrapped `ArticleHtmlSanitizer.Sanitize(ContentHtml, forOffline: !IsOnline)` in HTML-Dokument mit `<meta name='color-scheme' content='light dark'>`, CSP (`script-src 'none'` etc.), `@media (prefers-color-scheme: dark)`-CSS, Newsreader-Font, `baseSize` 19/22 px je `FontSizeIndex` |
| `AttachConnectivity`/`DetachConnectivity` | public | `TrackConnectivity`/`UntrackConnectivity` + `CancelAutoMarkRead` (von `ArticleDetailPage.OnAppearing`/`OnDisappearing` aufgerufen) |
| `OnConnectivityChanged` | protected override | Leert `ErrorMessage`, `RebuildHtml` |
| `GoBackCommand`, `ToggleSavedForLaterCommand`, `ToggleMarkReadCommand`, `ToggleAutoMarkReadCommand`, `OpenInBrowserCommand` (offline → `OfflineHint`-Fehler), `ShareCommand`, `ToggleFontSizeCommand` (A/A+, `FontSizeLabel`) | public Commands | Aktionen der Bottom-Bar |
| `BookmarkButtonLabel`, `MarkAsReadButtonLabel` | public | Lokalisierte Accessibility-Labels (`ArticleBookmarkSet/Remove`, `ArticleMarkAsRead/ArticleAlreadyRead`) |
| `Item`, `FeedName`, `FeedIconUrl`, `PublishedAtText`, `ReadingTime`, `HtmlSource`, `AutoMarkReadLabel`, `IsAutoMarkRead`, `IsAutoMarkReadAvailable`, `FontSizeIndex`, `ErrorMessage`/`HasError` | public | Bindungs-State |

## `FeedsViewModel` (+ `FeedsViewModel.Search`)
Dateien: `src/Reporter.Core/ViewModels/FeedsViewModel.cs`, `FeedsViewModel.Search.cs`

Commands: `LoadCommand`, `SaveCommand`, `EditCommand`, `DeleteCommand`, `RefreshCommand` (`FeedListItem`), `RefreshAllCommand`, `OpenAddFormCommand`, `CloseAddFormCommand`, `SearchCommand`, `DirectAddCommand`, `SubscribeResultCommand`, `CloseSearchResultsCommand`. Properties u. a. `Feeds` (`ObservableCollection<FeedListItem>` — Quelle der Badge-Darstellung), `Categories`, `SearchResults`, `ShowAddForm`, `ShowSearchResults`, `IsEditMode`, `IsSyncing`, `IsSearching`, `NewUrl`, `NewTitle`, `FeedNotificationsEnabled`, `NotificationsSupported`, `ErrorMessage`/`HasError`, `SyncErrorMessage`/`HasSyncError`, `SearchErrorMessage`/`HasSearchError`, `SelectedFeed`, `ConfirmDirectAddAsync` (Dialog-Delegate, von `FeedsPage` gesetzt). Hilfsmethoden: `RenameFeedAsync`, `ChangeFeedCategoryAsync`, `MakeUniqueOptionLabels` (für das Kategorie-ActionSheet).

## `CategoriesViewModel`
Datei: `src/Reporter.Core/ViewModels/CategoriesViewModel.cs`

Commands: `LoadCommand`, `SaveCommand`, `EditCommand`, `DeleteCommand`. Properties: `Categories` (`ObservableCollection<CategoryWithCount>`), `NewCategoryName`, `SelectedCategory`, `ErrorMessage`/`HasError`. `SaveAsync` validiert leere/duplikate Namen (`ErrorCategoryNameEmpty`/`ErrorCategoryDuplicate`).

## `BaseViewModel`
Datei: `src/Reporter.Core/ViewModels/BaseViewModel.cs`

`ObservableObject` mit `IsOnline`, `InitConnectivity`/`TrackConnectivity`/`UntrackConnectivity`, `RefreshConnectivityStatus`, virtuelles `OnConnectivityChanged(bool)`.

## `ArticleHtmlSanitizer` / `WebViewNavigationGuard` / `FeedHealth` / `FeedTitleFallback`
Dateien: `src/Reporter.Core/Services/`

- `ArticleHtmlSanitizer.Sanitize(html, forOffline)` — Regex-Bereinigung (script/iframe/style/object/embed/form/applet/audio/video, `on*`-Attribute, `javascript:`); bei `forOffline` werden `<a>` durch Inneretext ersetzt und `<img>` entfernt. (Best-Effort, kein vollwertiger Sanitizer — Code-Kommentar.)
- `WebViewNavigationGuard.IsExternalUrl(url)` — `true` nur bei `http(s)://`; wird in `ArticleDetailPage.OnWebViewNavigating` verwendet.
- `FeedHealth` — siehe `enums.md`.
- `FeedTitleFallback` — Platzhalter-Titel-Erkennung (Dateiname/Host), genutzt von `FeedSyncService` und `FeedsViewModel`.

## View-Code-Behind (UI-Logik)

- `UnreadPage.xaml.cs` — `OnAppearing` → `LoadCommand`; `OnFilterClicked` → `DisplayActionSheetAsync` über `Categories`-Namen → `SelectCategoryCommand` (Zeilen 48–74).
- `FeedsPage.xaml.cs` — `OnFeedTapped` (ActionSheet: Refresh/Rename/ChangeCategory/Edit/Delete), `RenameFeedAsync`, `ChangeCategoryAsync`, `ConfirmDeleteFeedAsync`, `OnSearchResultTapped` (Subscribe-Dialog), `OnBackButtonPressed` (schließt Sheet), `ConfirmDirectAddAsync`-Implementierung, Fokus-Logik für `NewUrlEntry`.
- `ArticleDetailPage.xaml.cs` — `IQueryAttributable.ApplyQueryAttributes` (`itemId`), `AttachConnectivity`/`DetachConnectivity`, `OnWebViewNavigating` (Offline-Linkblock + Alert).
- `LaterPage.xaml.cs`, `CategoriesPage.xaml.cs` (`OnCategoryTapped` → ActionSheet Edit/Delete + Delete-Alert), `SettingsPage.xaml.cs` (`NotificationAuthorizationDenied`-Alert, `OnOpenNotificationSettingsClicked`).
- `AppShell.xaml.cs` — baut `TabBar` mit 5 Tabs im Code, registriert Route `articledetail`.
