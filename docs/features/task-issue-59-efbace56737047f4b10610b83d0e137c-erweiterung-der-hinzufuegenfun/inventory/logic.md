<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik — Bestandsaufnahme

## `FeedsViewModel` (partial)
Dateien: `src/Reporter.Core/ViewModels/FeedsViewModel.cs`, `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
Basisklasse: `BaseViewModel` (`src/Reporter.Core/ViewModels/BaseViewModel.cs`) — liefert `IsOnline`, `TrackConnectivity` und den Hook `OnConnectivityChanged`.

Konstruktor-Injektion: `IFeedRepository`, `ICategoryRepository`, `IFeedSyncService`, `IFeedSearchService`, `INetworkStatusService`, optional `ILocalNotificationService?` (Default `null`).

### Eigenschaften (Hauptdatei)

| Eigenschaft | Typ | Zweck |
|-------------|-----|-------|
| `NewUrl` | `string` | URL-/Sucheingabe; der Setter leert `SearchResults`, setzt `ShowSearchResults = false` und `SearchErrorMessage = ""` (Zeilen 102–114) |
| `NewTitle` | `string` | Titel-Eingabe des Formulars; soll laut Anforderung entfallen |
| `NotificationsSupported` | `bool` (get) | `_localNotificationService?.IsSupported == true`; steuert Enabled/Opacity des Notification-Switch |
| `FeedNotificationsEnabled` | `bool` | Formular-Switch für `Feed.NotificationsEnabled`, Default `true` |
| `ErrorMessage` / `HasError` | `string` / `bool` | Formular-Fehlerkanal |
| `SyncErrorMessage` / `HasSyncError` | `string` / `bool` | Sync-Fehlerkanal (Label über der Liste) |
| `IsSyncing` | `bool` | Bindung an `RefreshView.IsRefreshing`; Setter ruft `NotifyCanExecuteChanged` auf `RefreshCommand`/`RefreshAllCommand` |
| `SelectedFeed` | `FeedListItem?` | Aktuell bearbeiteter Feed (Edit-Modus) |
| `SelectedCategory` | `Category?` | Gewählte Kategorie im Picker |
| `Categories` | `ObservableCollection<Category>` | Picker-Quelle inkl. `CategoryNone`-Pseudo-Eintrag (`Guid.Empty`) |
| `Feeds` | `ObservableCollection<FeedListItem>` | Quelle der Feed-`CollectionView` |

### Eigenschaften (Search-Partial)

| Eigenschaft | Typ | Zweck |
|-------------|-----|-------|
| `SearchResults` | `ObservableCollection<FeedSearchResult>` | Trefferliste |
| `ShowSearchResults` | `bool` | Sichtbarkeit der Trefferansicht (bleibt bei leerem Ergebnis `true` für die EmptyView) |
| `IsSearching` | `bool` | `ActivityIndicator`; Setter aktualisiert `SearchCommand.CanExecute` |
| `SearchErrorMessage` / `HasSearchError` | `string` / `bool` | Such-Fehlerkanal |
| `ConfirmDirectAddAsync` | `Func<string, Task<bool>>?` | Callback ins Code-Behind (hält das ViewModel UI-frei); wird in `FeedsPage`-Konstruktor verdrahtet |

### Commands und Methoden (Hauptdatei)

| Methode / Command | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadCommand` → `LoadCommandAsync` | public / private | Leert Fehlerkanäle, ruft `LoadAsync`; von `FeedsPage.OnAppearing` ausgelöst |
| `LoadAsync` | private | Lädt `Categories` (mit `Guid.Empty`/`CategoryNone`-Eintrag an Position 0) und `Feeds` via `GetAllWithDetailsAsync`; setzt `SelectedCategory` auf den ersten Eintrag, wenn nichts/Ungültiges gewählt (Zeilen 239–255) |
| `SaveCommand` → `SaveAsync` | public / private | Validiert `NewUrl` (`IsValidFeedUrl` → `ErrorFeedUrlInvalid`) und `NewTitle` (`ErrorFeedTitleEmpty`), Dublettenprüfung via `GetByUrlAsync` (`ErrorFeedDuplicate`), danach `AddAsync` (neu) oder `UpdateAsync` (Edit via `SelectedFeed`), jeweils mit `CategoryId` (`Guid.Empty` → `null`) und `FeedNotificationsEnabled`; abschließend `ResetForm` + `LoadAsync` (Zeilen 257–316) |
| `EditCommand` → `EditAsync(FeedListItem?)` | public / private | Lädt `SelectedFeed`, `NewUrl`, `NewTitle`, `FeedNotificationsEnabled`, `SelectedCategory` ins Formular (Zeilen 318–331) |
| `DeleteCommand` → `DeleteAsync(FeedListItem?)` | public / private | `DeleteAsync(feed.Id)`, `ResetForm` falls der Edit-Feed gelöscht wurde, `LoadAsync` |
| `RefreshCommand` → `RefreshAsync(FeedListItem?)` | public / private | Einzel-Sync via `SyncAsync(() => _feedSyncService.SyncFeedAsync(id))`; CanExecute `!IsSyncing` |
| `RefreshAllCommand` → `RefreshAllAsync` | public / private | Gesamt-Sync via `SyncAsync(() => _feedSyncService.SyncAllAsync())`; CanExecute `!IsSyncing` |
| `ResetForm` | private | Setzt `SelectedFeed`, `NewUrl`, `NewTitle` zurück, `FeedNotificationsEnabled = true`, `SelectedCategory` auf ersten Eintrag (Zeilen 350–357) |
| `SyncAsync(Func<Task<SyncResult>>)` | private | Reentrancy-Guard `_isSyncInProgress` (unabhängig vom gebundenen `IsSyncing`), Offline-Abbruch ohne Fehlertext, mappt `FeedHealth.Error`/Exception auf `AppResources.SyncStatusError`, danach `LoadAsync` (Zeilen 385–425) |
| `IsValidFeedUrl` | private static | `Uri.TryCreate` + Scheme `http`/`https` |
| `OnConnectivityChanged` | protected override | Leert `SyncErrorMessage`/`SearchErrorMessage`, aktualisiert `SearchCommand.CanExecute` (Zeilen 434–439) |

### Commands und Methoden (Search-Partial)

| Methode / Command | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SearchCommand` → `SearchAsync` | public / private | CanExecute `IsOnline && !IsSearching`. Leereingabe/Offline → Abbruch. `TryResolveSearchUrl` unterscheidet direkte URL (`isDirectUrl`) vs. Domain-Normalisierung; Freitext → leere Trefferansicht ohne Service-Call. Ruft `_feedSearchService.SearchAsync`; `IsStaleInput` verwirft Ergebnis/Fehler bei geändertem `NewUrl`. Bei 0 Treffern + direkter URL bzw. `FeedSearchUnavailableException` + direkter URL → `OfferDirectAddAsync` (Zeilen 102–174) |
| `IsStaleInput` | private | Vergleicht `NewUrl.Trim()` mit dem Such-Input (Ordinal) |
| `TryResolveSearchUrl` / `TryNormalizeDomainUrl` | private static | Direkte URL vs. `https://`-Präfix; verweigert Eingaben mit `://` oder Whitespace sowie Hosts ohne Punkt |
| `OfferDirectAddAsync` | private | Ruft `ConfirmDirectAddAsync`-Callback (Exception-safe); bei Bestätigung + leerem `NewTitle` Vorbelegung `NewTitle = hostUri.Host` — **kein** direktes Persistieren (Zeilen 215–241) |
| `CloseSearchResultsCommand` → `CloseSearchResults` | public / private | Leert `SearchResults`, `ShowSearchResults = false` |
| `SubscribeResultCommand` → `SubscribeResultAsync(FeedSearchResult?)` | public / private | Dublettenprüfung `GetByUrlAsync` → `ErrorFeedDuplicate`; `AddAsync` mit `Title = result.Title.Trim()` bzw. Fallback **`result.FeedUrl`** (volle URL, noch kein Dateiname), `CategoryId` aus `SelectedCategory`, `NotificationsEnabled` aus `FeedNotificationsEnabled`, `HealthStatus = FeedHealth.Ok`; danach Suchzustand leeren, `ResetForm`, `LoadAsync` (Zeilen 249–282) |

Abonnierte Events: `INetworkStatusService.ConnectivityChanged` (via `TrackConnectivity` in `BaseViewModel`).
Publizierte Events: keine eigenen; `ConfirmDirectAddAsync` ist ein Delegate-Callback.

## `FeedsPage` (Code-Behind)
Datei: `src/Reporter/Views/FeedsPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `FeedsPage(FeedsViewModel)` | public | `InitializeComponent`, `BindingContext`, verdrahtet `viewModel.ConfirmDirectAddAsync` |
| `OnAppearing` | protected override | Führt `LoadCommand` aus |
| `OnFeedTapped` | private | `DisplayActionSheetAsync` mit `ButtonRefresh`, `ButtonEdit`, `ButtonDelete` (Abbrechen: `ButtonCancel`); „Löschen" mit `DisplayAlertAsync`-Bestätigung (`ConfirmDeleteFeedTitle`/`Message`); routet zu `RefreshCommand`/`EditCommand`/`DeleteCommand` (Zeilen 43–79). **Noch keine** Einträge für „Umbenennen"/„Kategorie ändern" |
| `OnSearchResultTapped` | private | `DisplayAlertAsync`-Bestätigung (`ConfirmSubscribeFeedTitle`/`Message`, Anzeigename = `Title` oder `FeedUrl`), dann `SubscribeResultCommand` (Zeilen 87–105) |
| `ConfirmDirectAddAsync` | private | `DisplayAlertAsync` „URL direkt hinzufügen?" (`FeedSearchNoResultsTitle`/`FeedSearchNoResultsAddUrl`) → `Task<bool>` (Zeilen 113–120) |

`DisplayPromptAsync` wird im gesamten Codebestand nicht verwendet — ein Eingabedialog für „Umbenennen" wäre ein neues Pattern.

## `FeedSyncService`
Datei: `src/Reporter.Core/Services/FeedSyncService.cs` — implementiert `IFeedSyncService`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(Guid, CancellationToken)` | public | Offline → `SyncResult(Error, OfflineHint)` ohne Log; legt `SyncLog` an, lädt Feed via `GetByIdAsync`, delegiert an `RunSyncAsync`; Exceptions → `FeedHealth.Error` + Log (Zeilen 52–87) |
| `SyncAllAsync(CancellationToken)` | public | Offline → Error; iteriert `GetAllAsync`, aggregiert Status/NewItems, baut `{feed.Title}: {message}`-Liste (Zeilen 90–126) |
| `RunSyncAsync(Feed, SyncLog, CancellationToken)` | private | Liest Feed via `HttpClient.GetStreamAsync` + `XmlReader` (DtdProcessing.Ignore) + `SyndicationFeed.Load`; dedupliziert Items via `GetByGuidOrHashAsync`; **Platzhalter-Titel-Erkennung** (Zeilen 186–192): `IsNullOrWhiteSpace(feed.Title)` \|\| `Title == Url` (OrdinalIgnoreCase) \|\| `IsHostPlaceholderTitle` → bei gesetztem `SyndicationFeed.Title` wird `resolvedTitle` in `UpdateFeedHealthAsync` übernommen; Benachrichtigungen via `_notificationService.NotifyNewItemsAsync` (Fehler werden geschluckt) |
| `IsHostPlaceholderTitle` | private static | `Title == Uri.Host` (OrdinalIgnoreCase) — deckt den bisherigen Direkt-Hinzufügen-Fallback (`NewTitle = host`) ab; **Dateiname als Platzhalter (letztes Pfadsegment) ist noch nicht erkannt** (Zeilen 215–219) |
| `DetermineStatus` | private static | Warning bei >50 % Item-Rückgang oder keinen neuen Items seit >30 Tagen |
| `UpdateFeedHealthAsync` | private | `UpdateAsync` mit Status/`LastCheckedAt`/`HealthLastChange` (bei Änderung) und optionalem `resolvedTitle` (Zeilen 237–256) |
| `UpdateLogAsync` | private | Persistiert `SyncLog`-Abschluss |
| `GetContentHtml` / `NormalizeGuidOrHash` | private static | Item-Content-Extraktion bzw. Guid/SHA256-Fallback |

## `FeedSearchService`
Datei: `src/Reporter.Core/Services/FeedSearchService.cs` — implementiert `IFeedSearchService`.

`SearchAsync(query)` fragt das Verzeichnis `https://feedsearch.dev/api/v1/search` ab und fällt auf clientseitige Autodiscovery zurück (HTML-`<link>`-Tags + `StandardFeedPaths`: `/feed`, `/rss`, `/rss.xml`, `/atom.xml`, `/feed.xml`, `/index.xml`); gemeinsames Zeitbudget `SearchTimeout = 2 s`. Wirft `FeedSearchUnavailableException`, wenn beide Quellen scheitern. Vom ViewModel-Verhalten unverändert genutzt.

## `FeedRepository`
Datei: `src/Reporter.Data/Repositories/FeedRepository.cs` — implementiert `IFeedRepository` mit `IDbContextFactory<ReporterDbContext>`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync` | public | Alle Feeds, `OrderBy(Title)` |
| `GetByIdAsync` | public | Feed per Id oder `null` |
| `AddAsync` | public | Insert |
| `UpdateAsync` | public | `FindAsync` + Feld-Update inkl. `Url`, `Title`, `CategoryId`, `NotificationsEnabled`; No-Op bei fehlender Entity (Zeilen 56–73) — genügt für „Umbenennen"/„Kategorie ändern" |
| `DeleteAsync` | public | Löscht per Id |
| `GetAllWithDetailsAsync` | public | Projiziert auf `FeedListItem` inkl. `CategoryName` (Join) und `UnreadCount` (Items-Subquery), `OrderBy(Title)` |
| `GetByUrlAsync` | public | Exakter URL-Match für die Dublettenprüfung |

## `CategoryRepository`
Datei: `src/Reporter.Data/Repositories/CategoryRepository.cs` — implementiert `ICategoryRepository`; `GetAllAsync` (`OrderBy(Name)`) liefert die Daten für `Categories` in `FeedsViewModel.LoadAsync`.

## `BaseViewModel`
Datei: `src/Reporter.Core/ViewModels/BaseViewModel.cs`

`ObservableObject` (CommunityToolkit.Mvvm) mit `IsOnline`, `InitConnectivity`/`TrackConnectivity`/`UntrackConnectivity`, `RefreshConnectivityStatus` und virtuellem `OnConnectivityChanged(bool)` — von `FeedsViewModel` überschrieben.
