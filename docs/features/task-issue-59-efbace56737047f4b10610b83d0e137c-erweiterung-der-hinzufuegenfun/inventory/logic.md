<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik

Betroffene Logikklassen für den Hinzufügen-/Feed-Verwaltungsbereich. Ein Such-Service (Anforderung: `RssAtlasFeedSearchService`) existiert nicht.

## `FeedsViewModel`
Datei: `src/Reporter.Core/ViewModels/FeedsViewModel.cs`

`partial class`, abgeleitet von `BaseViewModel` (`CommunityToolkit.Mvvm`). Konstruktor-Abhängigkeiten: `IFeedRepository`, `ICategoryRepository`, `IFeedSyncService`, `INetworkStatusService`, optional `ILocalNotificationService`.

Commands (alle im Konstruktor verdrahtet):

| Command | Typ | Handler |
|---------|-----|---------|
| `LoadCommand` | `AsyncRelayCommand` | `LoadCommandAsync` → `LoadAsync` |
| `SaveCommand` | `AsyncRelayCommand` | `SaveAsync` |
| `EditCommand` | `AsyncRelayCommand<FeedListItem?>` | `EditAsync` |
| `DeleteCommand` | `AsyncRelayCommand<FeedListItem?>` | `DeleteAsync` |
| `RefreshCommand` | `AsyncRelayCommand<FeedListItem?>` | `RefreshAsync` (CanExecute: `!IsSyncing`) |
| `RefreshAllCommand` | `AsyncRelayCommand` | `RefreshAllAsync` (CanExecute: `!IsSyncing`) |

Öffentliche Eigenschaften:

| Eigenschaft | Typ | Zweck |
|-------------|-----|-------|
| `NewUrl` | `string` | URL-Eingabe des Formulars (gebunden an `Entry` in `FeedsPage.xaml`) |
| `NewTitle` | `string` | Titel-Eingabe |
| `NotificationsSupported` | `bool` (get) | `_localNotificationService?.IsSupported == true` — steuert Enabled/Hint des Notification-Switch |
| `FeedNotificationsEnabled` | `bool` | Notification-Flag des Formulars (Default `true`) |
| `ErrorMessage` / `HasError` | `string` / `bool` | Formular-Fehlerkanal |
| `SyncErrorMessage` / `HasSyncError` | `string` / `bool` | Sync-Fehlerkanal oberhalb der Liste |
| `IsSyncing` | `bool` | Sync-Indikator; triggert `NotifyCanExecuteChanged` auf Refresh-Commands |
| `SelectedFeed` | `FeedListItem?` | Aktuell editierter Feed (`null` = Neuanlage) |
| `SelectedCategory` | `Category?` | Gewählte Kategorie des Formulars |
| `Categories` | `ObservableCollection<Category>` | Kategorieliste inkl. `Guid.Empty`-Eintrag `CategoryNone` |
| `Feeds` | `ObservableCollection<FeedListItem>` | Angezeigte Feed-Liste |

Private Methoden:

| Methode | Kurzbeschreibung |
|---------|------------------|
| `LoadCommandAsync` | Leert `ErrorMessage`/`SyncErrorMessage`, ruft `LoadAsync` |
| `LoadAsync` | Lädt Kategorien (mit `CategoryNone`-Pseudo-Eintrag) und `GetAllWithDetailsAsync`; setzt `SelectedCategory`-Fallback |
| `SaveAsync` | Validierung: URL leer/ungültig (`Uri.TryCreate` absolute, Scheme http/https) → `ErrorFeedUrlInvalid`; Titel leer → `ErrorFeedTitleEmpty`; Dublette via `GetByUrlAsync` (mit `SelectedFeed`-Ausnahme) → `ErrorFeedDuplicate`. Dann `AddAsync` (Neu: `HealthStatus = "OK"`) oder `UpdateAsync`, `ResetForm`, `LoadAsync` |
| `EditAsync` | Befüllt Formular aus `FeedListItem` (Url, Title, `NotificationsEnabled`, Kategorie) |
| `DeleteAsync` | `DeleteAsync(feed.Id)`, `ResetForm` falls der editierte Feed gelöscht wurde, `LoadAsync` |
| `ResetForm` | Setzt `SelectedFeed`, `NewUrl`, `NewTitle` zurück; `FeedNotificationsEnabled = true`; Kategorie auf ersten Eintrag |
| `RefreshAsync` | `SyncAsync(() => _feedSyncService.SyncFeedAsync(feedId))` |
| `RefreshAllAsync` | `SyncAsync(() => _feedSyncService.SyncAllAsync())` |
| `SyncAsync` | Reentrancy-Guard `_isSyncInProgress`; offline → `IsSyncing = false` + Return; bei `FeedHealth.Error` oder Exception → `SyncErrorMessage = AppResources.SyncStatusError`; danach `LoadAsync` |
| `OnConnectivityChanged` (override) | Leert `SyncErrorMessage` bei Connectivity-Wechsel |

Abonnierte Events: `INetworkStatusService.ConnectivityChanged` (via `TrackConnectivity` im Konstruktor).
Publizierte Events: nur `INotifyPropertyChanged` über `ObservableObject`/`SetProperty`.

Nicht vorhanden: `SearchQuery`, `SearchResults`, `IsSearching`, `SearchCommand`, `SubscribeResultCommand` o. ä.

## `BaseViewModel`
Datei: `src/Reporter.Core/ViewModels/BaseViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `IsOnline` | public get / private set | Aktueller Online-Status |
| `InitConnectivity(INetworkStatusService)` | protected | Speichert Service, setzt `IsOnline`, ohne Event-Abo |
| `TrackConnectivity(INetworkStatusService)` | protected | `InitConnectivity` + Event-Abo |
| `TrackConnectivity()` | protected | Abonniert `ConnectivityChanged` (idempotent) |
| `UntrackConnectivity()` | protected | Deabonniert (idempotent) |
| `RefreshConnectivityStatus()` | protected | Liest `IsOnline` neu, ruft `OnConnectivityChanged` bei Änderung |
| `OnConnectivityChanged(bool)` | protected virtual | Hook für abgeleitete ViewModels |

## `FeedSyncService`
Datei: `src/Reporter.Core/Services/FeedSyncService.cs`

Implementiert `IFeedSyncService`. Konstruktor-Abhängigkeiten: `IFeedRepository`, `IItemRepository`, `ISyncLogRepository`, `HttpClient` (aus DI), `INotificationService`, `INetworkStatusService`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(Guid, CancellationToken)` | public | Offline → `SyncResult(Error, OfflineHint)`; legt `SyncLog` an; lädt Feed; `RunSyncAsync`; Exception → Health `Error` + Log |
| `SyncAllAsync(CancellationToken)` | public | Offline → Error; iteriert alle Feeds, aggregiert Status/NewItems |
| `RunSyncAsync(Feed, SyncLog, CancellationToken)` | private | `_httpClient.GetStreamAsync(feed.Url)` → `SyndicationFeed.Load` → Dedupe via `GetByGuidOrHashAsync` → `Item`s anlegen → Health/Log → `NotifyNewItemsAsync` |
| `DetermineStatus(...)` | private static | `Warning` bei <50 % Abruf oder >30 Tage ohne neue Items |
| `UpdateFeedHealthAsync` / `UpdateLogAsync` | private | Persistiert Health bzw. SyncLog |
| `GetContentHtml` / `NormalizeGuidOrHash` | private static | Content-Extraktion / GUID-oder-SHA256-Hash |

Abonnierte Events: keine. Liest `_networkStatusService.IsOnline`.
Der Service bleibt laut Anforderung unverändert; er liefert nach dem Abonnieren den ersten Abruf (`SyncFeedAsync`).

## `FeedRepository`
Datei: `src/Reporter.Data/Repositories/FeedRepository.cs`

Implementiert `IFeedRepository` über `IDbContextFactory<ReporterDbContext>` (SQLite/EF Core). Für die Anforderung relevant:

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetByUrlAsync(string url)` | public | `FirstOrDefaultAsync(f => f.Url == url)` mit `AsNoTracking`, gemappt auf `Feed`; Grundlage der Dublettenprüfung |
| `AddAsync(Feed)` | public | Legt Feed-Entity an |
| `UpdateAsync(Feed)` | public | Aktualisiert Feed-Entity |
| `GetAllWithDetailsAsync()` | public | Projektion auf `FeedListItem` inkl. `CategoryName`/`UnreadCount` |

## `MauiProgram`
Datei: `src/Reporter/MauiProgram.cs`

`CreateMauiApp()` — DI-Registrierungen (alle Singleton, sofern nicht anders angegeben): `IDbContextFactory<ReporterDbContext>` (SQLite, `reporter.db` im AppDataDirectory), `IFeedRepository`→`FeedRepository`, `ICategoryRepository`, `IItemRepository`, `IKeywordRepository`, `ISettingsRepository`, `ISyncLogRepository`, `HttpClient` mit `Timeout = 30 s` (Zeile 52), `IFeedSyncService`→`FeedSyncService`, `IRetentionCleanupService`, `IKeywordMatcher`, `IAutoRefreshService`, `IAppThemeService`, `INotificationService`, `ILocalNotificationService`, `INetworkStatusService`, ViewModels (`FeedsViewModel` u. a. Singleton; `ArticleDetailViewModel` Transient), Pages Transient (`FeedsPage` u. a.), `AppShell`.

Kein `IFeedSearchService`-Eintrag vorhanden.

## `FeedsPage` (Code-Behind)
Datei: `src/Reporter/Views/FeedsPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `FeedsPage(FeedsViewModel)` | public ctor | `InitializeComponent`, `BindingContext = viewModel` (Ctor-DI) |
| `OnAppearing` | protected override | `viewModel.LoadCommand.Execute(null)` |
| `OnFeedTapped` | private async void | Tap auf Feed-Karte → `DisplayActionSheetAsync` (Aktualisieren/Bearbeiten/Löschen); Delete zusätzlich über `DisplayAlertAsync` bestätigt; ruft `RefreshCommand`/`EditCommand`/`DeleteCommand` mit dem `FeedListItem` |

Dieses Muster (`TapGestureRecognizer` → ActionSheet/Alert im Code-Behind) ist der Referenzpunkt für die in der Anforderung geforderten Confirm-Dialoge der Fallback-Fälle.

## `FeedHealth`
Datei: `src/Reporter.Core/Services/FeedHealth.cs`

Statische Klasse mit String-Konstanten `Ok = "OK"`, `Warning`, `Error` und Hilfsmethode `Changed(string? current, string? next)`. Wird von `FeedSyncService`, `FeedsViewModel` und XAML-DataTriggern verwendet.

## `SyncResult`
Datei: `src/Reporter.Core/Services/SyncResult.cs`

`record SyncResult(string Status, int NewItems, string? Message = null)` — Rückgabetyp von `IFeedSyncService`.
