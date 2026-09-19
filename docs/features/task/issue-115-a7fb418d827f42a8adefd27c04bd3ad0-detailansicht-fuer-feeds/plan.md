<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Detailansicht für Feeds

## Übersicht

Der Tap auf eine Feed-Karte in der `FeedsPage` navigiert künftig per Shell-Route `feeddetail?feedId=…` auf eine neue `FeedDetailPage`, statt das Aktionsblatt zu öffnen. Die Detailansicht zeigt alle `Item`-Datensätze des Feeds (gelesen und ungelesen) als pagede, suchbare `ArticleCardView`-Liste (Infinite Scroll, `PageSize = 20`) und übernimmt die bisherigen Feed-Aktionen (Aktualisieren, Umbenennen, Kategorie ändern, Bearbeiten, Fehlerdetails, Löschen). Betroffen sind `IItemRepository`/`ItemRepository` (neue pagede Abfrage), das neue `FeedDetailViewModel` + `FeedDetailPage`, `FeedsPage`/`FeedsViewModel` (Abbau der verlagerten Aktions- und Edit-Logik), Navigation/DI sowie `AppResources`, Unit- und E2E-Tests.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Feed-Stammdaten in der Detailansicht | `FeedListItem` via `IFeedRepository.GetAllWithDetailsAsync()` + `FirstOrDefault(f => f.Id == feedId)` laden (kein neues Repository-Member) | Liefert `CategoryName` und `UnreadCount` für den Kopfbereich und exakt den Parametertyp `FeedListItem`, den die aus `FeedsViewModel` übernommenen Aktionsmethoden (`RenameFeedAsync`, `ChangeFeedCategoryAsync`, `GetFeedErrorMessage`, `ToFeed`) bereits erwarten. `GetByIdAsync` liefert nur `Feed` ohne `CategoryName`/`UnreadCount` und würde alle übernommenen Methoden umschreiben. Feed-Anzahl ist klein, der Mehraufwand der Gesamtprojektion ist unkritisch. |
| Feed-Aktionslogik | Methoden aus `FeedsViewModel` in `FeedDetailViewModel` verschieben (Umzug, nicht Delegation an das Singleton-VM) | Vermeidet eine Querabhängigkeit zwischen ViewModels und entfernt Totcode aus `FeedsViewModel`. Das Aktionsblatt bleibt UI-Ebene (`DisplayActionSheetAsync` im Code-Behind der `FeedDetailPage`), wie bisher in `FeedsPage` und gefordert durch AGENTS.md. |
| „Bearbeiten"-Aktion | Eigenes Edit-Sheet in `FeedDetailPage` (URL-`Entry` + Benachrichtigungs-`Switch` + Speichern), kein Rücksprung zur `FeedsPage` | Hält den Nutzer im Detailkontext; das Sheet der `FeedsPage` wird auf den reinen Add-Modus reduziert (die `IsEditMode`-Variante entfällt dort). Rücknavigation + Sheet-Öffnung auf der anderen Page wäre ein instabiler Cross-Page-Zustand über das Singleton-`FeedsViewModel`. (Vom Anwender bestätigte Entscheidung.) |
| Repository-Abfrage | Neue Überladung `GetByFeedAsync(Guid feedId, int page, int pageSize, string? searchTerm = null)` → `Task<IReadOnlyList<ItemListItem>>` im `ItemRepository` (Repository-Muster, `SelectListItemRows`-Projektion wiederverwenden) | Bestehende `GetByFeedAsync(Guid)` bleibt für `FeedSyncService` und dessen Tests unverändert; die Projektion auf `ItemListItem` inkl. Content-Store-/Bild-Hydration existiert bereits für `GetSavedForLaterAsync`/`GetUnreadByDateAsync`. |
| Suchfilter | Datenbankseitig auf `Item.Title` (`Contains`), an die pagede Abfrage weitergereicht | `Title` liegt als Spalte in `items` vor — `Summary`/`ContentHtml` leben im separaten `IItemContentStore` und wären nicht SQL-filterbar. (Vom Anwender bestätigte Entscheidung.) |
| `Feed == null`-Absicherung | Aktions-Button via `IsEnabled="{Binding HasFeed}"` deaktiviert + Früh-Returns in `OnFeedActionsClicked`, `SaveEditAsync` und `DeleteFeedAsync` | Bei unbekannter `feedId` bricht `LoadAsync` mit `ErrorLoadFailed` ab und `Feed` bleibt `null` — ohne Guard würden Aktionsblatt, Edit-Speichern und Löschen auf `null` zugreifen. Doppelsicherung (deaktivierter Button + Früh-Return) entspricht der Parität zum bisherigen `SaveCommand_WithoutSelectedFeed_DoesNotAddFeed`-Früh-Return. |
| DI-Lebensdauer / Ablageort | `FeedDetailViewModel` + `FeedDetailPage` transient (`AddTransient`), VM in `src/Reporter.Core/ViewModels/` (Namespace `Reporter.Core.ViewModels`, MAUI-frei) | Pro Navigation eine frische Instanz mit eigenem `feedId`-Kontext — Muster `ArticleDetailViewModel`/`ArticleDetailPage`. Ablage in `Reporter.Core` folgt der Anforderung und hält das VM in `Reporter.Tests` testbar (Abweichung vom `ArticleDetailViewModel`-Ablageort `src/Reporter/ViewModels/`, aber gleicher Namespace und gleiche Testbarkeit). |

## Programmabläufe

### Navigation zur Detailansicht und erstmaliges Laden

1. Tap auf eine Feed-Karte → `FeedsPage.OnFeedTapped` → `Shell.Current.GoToAsync($"feeddetail?feedId={feed.Id}")` (Fehler wie in `ArticleCardView.OpenArticleAsync` per `try/catch` + `Debug.WriteLine` abgefangen).
2. Shell löst die Route `feeddetail` auf, erstellt die transient registrierte `FeedDetailPage` (inkl. transientem `FeedDetailViewModel`) und ruft `FeedDetailPage.ApplyQueryAttributes` auf (Muster `ArticleDetailPage`).
3. `ApplyQueryAttributes` liest `query["feedId"]`, parst `Guid` und ruft `_viewModel.LoadAsync(feedId)`; ungültige/fehlende Werte brechen still ab (Muster `ArticleDetailPage`).
4. `FeedDetailViewModel.LoadAsync(feedId)`:
   - `_feedId` setzen; `Categories` = Pseudo-Eintrag `Guid.Empty`/`AppResources.CategoryNone` + `ICategoryRepository.GetAllAsync()`.
   - `Feed` = `(await _feedRepository.GetAllWithDetailsAsync()).FirstOrDefault(f => f.Id == feedId)`; bei `null` → `ErrorMessage = AppResources.ErrorLoadFailed` und Abbruch.
   - Listen-Reset (`Items.Clear()`, `_currentPage = 0`, `HasMore = true`) und erste Seite via `LoadPageCoreAsync` laden — alles unter der `_loadLock`-`SemaphoreSlim`-Sperre (Muster `LaterViewModel.LoadAsync`).

Beteiligte Klassen/Komponenten: `FeedsPage`, `FeedDetailPage`, `FeedDetailViewModel`, `IFeedRepository`, `ICategoryRepository`, `AppShell`, `MauiProgram`

### Infinite Scroll (seitenweises Nachladen)

1. `CollectionView.RemainingItemsThresholdReachedCommand` (Schwelle `2`) → `FeedDetailViewModel.LoadMoreCommand` (CanExecute `HasMore && !IsLoading`).
2. `LoadMoreAsync` prüft `IsLoading`/`HasMore` vor und innerhalb der `_loadLock`-Sperre (Doppel-Check-Muster aus `LaterViewModel`).
3. `LoadPageCoreAsync` ruft `_itemRepository.GetByFeedAsync(_feedId, _currentPage, PageSize, SearchTermOrNull)`, hängt die `ItemListItem`s an `Items`, inkrementiert `_currentPage` und setzt `HasMore = items.Count == PageSize`; bei Exception → `ErrorMessage = AppResources.ErrorLoadFailed`, `HasMore = false`.

Beteiligte Klassen/Komponenten: `FeedDetailViewModel`, `IItemRepository`, `ItemRepository`, `CollectionView`

### Suche in den Feed-Beiträgen

1. `SearchBar` im Seitenkopf (`PlaceholderFeedDetailSearch`, Style aus `Styles.xaml`) bindet `Text` an `FeedDetailViewModel.SearchText`.
2. Der `SearchText`-Setter startet die Liste neu: `Items.Clear()`, `_currentPage = 0`, `HasMore = true`, dann `LoadPageCoreAsync` unter `_loadLock`; leerer/Whitespace-Text wird als `null`-`searchTerm` weitergereicht.
3. `ItemRepository` filtert bei gesetztem `searchTerm` zusätzlich auf `i.Title.Contains(searchTerm)`; Paging (`Skip`/`Take`) und `EmptyView` (`PlaceholderFeedDetail`) gelten unverändert auch für Suchergebnisse.

Beteiligte Klassen/Komponenten: `FeedDetailPage`, `FeedDetailViewModel`, `IItemRepository`

### Feed-Aktionen über das Aktionsblatt der Detailansicht

1. Tap auf den Aktions-Button im Seitenkopf → `FeedDetailPage.OnFeedActionsClicked` → `DisplayActionSheetAsync` mit `ButtonRefresh`, `ButtonRename`, `ButtonChangeCategory`, `ButtonEdit`, bei `Feed.HealthStatus == FeedHealth.Error` zusätzlich `ButtonShowErrorDetails`, plus `ButtonDelete` (unverändert übernommen aus `FeedsPage.OnFeedTapped`). Absicherung: `OnFeedActionsClicked` bricht bei `Feed == null` still ab (Feed nicht geladen, z. B. unbekannte `feedId`); der Button ist zusätzlich über `IsEnabled="{Binding HasFeed}"` deaktiviert.
2. Verteilung analog dem bisherigen Code:
   - `ButtonRefresh` → `FeedDetailViewModel.RefreshCommand` → `RefreshAsync` → `SyncAsync(() => _feedSyncService.SyncFeedAsync(feedId))` mit `_isSyncInProgress`-Reentrancy-Guard, Offline-Early-Return, `SyncErrorMessage` bei `FeedHealth.Error`/Exception; danach `ReloadFeedAsync()` + Listen-Neustart.
   - `ButtonRename` → `FeedDetailPage.RenameFeedAsync` (`DisplayPromptAsync` mit `Feed.Title` als `initialValue`) → `FeedDetailViewModel.RenameFeedAsync(feed, newTitle)` → `UpdateAsync(ToFeed(...))` → `ReloadFeedAsync()`; leerer Titel → `ErrorMessage = AppResources.ErrorFeedTitleEmpty`.
   - `ButtonChangeCategory` → `FeedDetailPage.ChangeCategoryAsync` (`DisplayActionSheetAsync` über `viewModel.Categories`, Cancel-Namen ausgeschlossen, Labels via `FeedDetailViewModel.MakeUniqueOptionLabels`) → `FeedDetailViewModel.ChangeFeedCategoryAsync(feed, category)` (`Guid.Empty` löscht die Zuordnung) → `UpdateAsync` → `ReloadFeedAsync()`.
   - `ButtonEdit` → `FeedDetailViewModel.EditCommand` → öffnet das Edit-Sheet (`ShowEditForm = true`, Vorbefüllung `EditUrl = Feed.Url`, `EditNotificationsEnabled = Feed.NotificationsEnabled`).
   - `ButtonShowErrorDetails` → `DisplayAlertAsync` mit `FeedDetailViewModel.GetFeedErrorMessage(feed)` (Mapping `LastErrorKind` → `FeedErrorKind*` + technische Meldung).
   - `ButtonDelete` → `FeedDetailPage.ConfirmDeleteFeedAsync` (`DisplayAlertAsync` `ConfirmDeleteFeedTitle/Message`) → `FeedDetailViewModel.DeleteFeedAsync()` → `IFeedRepository.DeleteAsync(feed.Id)` (kaskadiert Items/Content/Bilder) → `Shell.Current.GoToAsync("..")` zurück zur Feed-Übersicht. Früh-Return bei `Feed == null`.

Beteiligte Klassen/Komponenten: `FeedDetailPage`, `FeedDetailViewModel`, `IFeedRepository`, `IFeedSyncService`, `ICategoryRepository`

### Bearbeiten-Sheet in der Detailansicht

1. `EditCommand` öffnet das Sheet (`ShowEditForm`), befüllt `EditUrl`/`EditNotificationsEnabled` und setzt `ErrorMessage` zurück; die Page fokussiert `EditUrlEntry` über `PropertyChanged` auf `ShowEditForm` (Muster `FeedsPage.OnViewModelPropertyChanged` für `NewUrlEntry`).
2. `SaveEditCommand` → `SaveEditAsync`: Früh-Return bei `Feed == null` (Parität zum bisherigen `SaveCommand_WithoutSelectedFeed_DoesNotAddFeed`), dann Validierung `EditUrl` (nicht leer + `IsValidFeedUrl` http/https → sonst `ErrorFeedUrlInvalid`; `GetByUrlAsync(url)` mit abweichender Id → `ErrorFeedDuplicate`), dann `UpdateAsync(ToFeed(feed, url, feed.Title, feed.CategoryId, EditNotificationsEnabled))` — `feed.CategoryId` bleibt erhalten —, Sheet schließen, `ReloadFeedAsync()`.
3. `CloseEditFormCommand` bzw. Tap auf den Dismiss-`BoxView`/`OnBackButtonPressed` schließt das Sheet und setzt den Edit-Zustand zurück (Muster `CloseAddFormCommand`/`OnBackButtonPressed` der `FeedsPage`).

Beteiligte Klassen/Komponenten: `FeedDetailPage`, `FeedDetailViewModel`, `IFeedRepository`, `ILocalNotificationService` (optional, `NotificationsSupported`)

### Beitragskarten: Öffnen, Lesestatus, „Später lesen"

1. `ItemTemplate` nutzt `ArticleCardView` (Muster `LaterPage`): Default-`OpenArticleCommand` navigiert zu `articledetail?itemId=…`, `ToggleSavedCommand`/`MarkReadCommand`/`IsOnline` werden über `x:Reference PageRoot` an das `FeedDetailViewModel` gebunden.
2. `MarkReadAsync(item)` → `_itemRepository.MarkAsReadAsync(item.Id)` + Ersetzen des Eintrags durch `item.CopyWith(isRead: true)`; `ToggleSavedAsync(item)` → `ToggleSavedForLaterAsync(item.Id)` + Ersetzen durch `CopyWith(isSavedForLater: !item.IsSavedForLater)` — der Beitrag bleibt (im Gegensatz zur `LaterPage`) in der Liste, da die Detailansicht alle Items des Feeds zeigt.
3. Pull-to-Refresh: `RefreshView` um die `CollectionView`, `IsRefreshing="{Binding IsSyncing}"`, `Command="{Binding RefreshCommand}"` (Einzel-Feed-Sync).
4. Connectivity: `OnAppearing` → `FeedDetailViewModel.AttachConnectivity()`, `OnDisappearing` → `DetachConnectivity()` (Muster `ArticleDetailPage`/`ArticleDetailViewModel`); `OnConnectivityChanged` leert `SyncErrorMessage`.
5. Zurück-Button im Kopf → `GoBackCommand` → `Shell.Current.GoToAsync("..")` (Muster `ArticleDetailViewModel`).

Beteiligte Klassen/Komponenten: `FeedDetailPage`, `FeedDetailViewModel`, `ArticleCardView`, `IItemRepository`, `INetworkStatusService`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `FeedDetailViewModel` (`src/Reporter.Core/ViewModels/FeedDetailViewModel.cs`) | Klasse (`BaseViewModel`, transient) | Paging-Liste der Feed-Beiträge inkl. Suche, Einzel-Feed-Sync, übernommene Feed-Aktionen, Edit-Sheet-Zustand |
| `FeedDetailPage` (`src/Reporter/Views/FeedDetailPage.xaml` + `.xaml.cs`) | `ContentPage` mit `IQueryAttributable` (transient) | Detailansicht: Kopfdaten, `SearchBar`, `CollectionView` mit Infinite Scroll, Aktions-Button + Aktionsblatt, Edit-Sheet |

## Änderungen an bestehenden Klassen

### `IItemRepository` (Interface)

- **Neue Methoden:** `GetByFeedAsync(Guid feedId, int page, int pageSize, string? searchTerm = null)` — liefert `Task<IReadOnlyList<ItemListItem>>`; pagede, titelgefilterte Feed-Beitragsliste für die Detailansicht.

### `ItemRepository` (Klasse)

- **Neue Methoden:** `GetByFeedAsync(Guid feedId, int page, int pageSize, string? searchTerm = null)` — Implementierung: `Where(i => i.FeedId == feedId)`, optional `i.Title.Contains(searchTerm)` bei nicht-leerem Begriff, `OrderByDescending(i => i.PublishedAt).ThenBy(i => i.Id)`, `Skip(page * pageSize).Take(pageSize)`, `SelectListItemRows`-Projektion sowie `GetContentsAsync`/`GetImageIdsAsync`/`MapToListItem`-Hydration exakt wie `GetSavedForLaterAsync`.

### `FeedDetailViewModel` ist neu — siehe „Neue Klassen". Seine Member im Überblick:

- `PageSize` (`const int = 20`), `Items` (`ObservableCollection<ItemListItem>`), `Feed` (`FeedListItem?`), `HasFeed` (`bool`, abgeleitet aus `Feed != null`, `PropertyChanged` im `Feed`-Setter — steuert `IsEnabled` des Aktions-Buttons), `Categories` (`ObservableCollection<Category>`), `SearchText`, `IsLoading`, `HasMore`, `IsSyncing`, `ErrorMessage`/`HasError`, `SyncErrorMessage`/`HasSyncError`, `ShowEditForm`, `EditUrl`, `EditNotificationsEnabled`, `NotificationsSupported`
- `LoadAsync(Guid feedId)`, `LoadMoreCommand`, `RefreshCommand`, `GoBackCommand`, `ToggleSavedCommand`, `MarkReadCommand`, `EditCommand`, `SaveEditCommand`, `CloseEditFormCommand`, `RenameFeedAsync(FeedListItem?, string?)`, `ChangeFeedCategoryAsync(FeedListItem?, Category?)`, `DeleteFeedAsync()`, `GetFeedErrorMessage(FeedListItem)`, `MakeUniqueOptionLabels(IReadOnlyList<string>)` (static), `AttachConnectivity()`/`DetachConnectivity()`
- Abhängigkeiten: `IItemRepository`, `IFeedRepository`, `ICategoryRepository`, `IFeedSyncService`, `INetworkStatusService`, optional `ILocalNotificationService`

### `FeedsViewModel` (Klasse, partial mit `FeedsViewModel.Search.cs`)

- **Entfernte Commands/Methoden** (Umzug nach `FeedDetailViewModel`): `RefreshCommand`/`RefreshAsync`, `EditCommand`/`EditAsync`, `DeleteCommand`/`DeleteAsync`, `SaveCommand`/`SaveAsync`, `RenameFeedAsync`, `ChangeFeedCategoryAsync`, `GetFeedErrorMessage`, `MakeUniqueOptionLabels`, `ToFeed`, `IsValidFeedUrl`.
- **Entfernte Eigenschaften:** `SelectedFeed`, `NewTitle`, `FeedNotificationsEnabled`, `IsEditMode`, `NotificationsSupported`; Konstruktor-Parameter `ILocalNotificationService? localNotificationService` und Feld `_localNotificationService` entfallen (nur vom Edit-Sheet genutzt).
- **Geänderte Methoden:** `ResetForm` — entfällt das Zurücksetzen der Edit-Felder; `IsSyncing`-Setter — `RefreshCommand.NotifyCanExecuteChanged()` entfällt; `SearchCommand`/`DirectAddCommand`-CanExecute in `FeedsViewModel.cs`/`FeedsViewModel.Search.cs` — `!IsEditMode`-Klauseln und die `IsEditMode`-Guards (Search.cs Zeilen ~120/~280) entfallen.
- **Unverändert:** `RefreshAllCommand`/`RefreshAllAsync`, `SyncAsync`, `IsSyncing`, `SyncErrorMessage`/`HasSyncError`, `LoadCommand`/`LoadAsync`, `ShowAddForm`, `NewUrl`, `ErrorMessage`/`HasError`, `Categories`, `Feeds`, Add-/Such-/DirectAdd-Flows, `ConfirmDirectAddAsync`, `OnConnectivityChanged`.

### `FeedsPage` (Code-Behind + XAML)

- **Geänderte Methoden:** `OnFeedTapped` — statt `DisplayActionSheetAsync` nur noch `Shell.Current.GoToAsync($"feeddetail?feedId={feed.Id}")` mit `try/catch` + `Debug.WriteLine`.
- **Entfernte Methoden:** `RenameFeedAsync`, `ChangeCategoryAsync`, `ConfirmDeleteFeedAsync`, `ShowFeedErrorDetailsAsync` (Umzug nach `FeedDetailPage`).
- **XAML:** Edit-Variante des Add-Sheets entfernen — `IsEditMode`-`DataTrigger` am Sheet-Titel, an den Search-/DirectAdd-Buttons und am Offline-Hinweis sowie der komplette `IsEditMode`-Block (Notification-`Switch`-`Grid`, `NotificationsIosOnlyHint`-`Border`, `ButtonSave`); Sheet-Titel bleibt fest `FeedAddSheetTitle`. `OnBackButtonPressed` und `OnViewModelPropertyChanged` (`ShowAddForm`-Fokus) bleiben.

### `AppShell` (Klasse)

- **Neu:** `Routing.RegisterRoute("feeddetail", typeof(FeedDetailPage))` — neben der bestehenden `articledetail`-Registrierung.

### `MauiProgram` (Klasse)

- **Neu:** `.AddTransient<FeedDetailViewModel>()` und `.AddTransient<FeedDetailPage>()` in der Service-Kette.

### `AppResources` (resx + de.resx + Designer)

- **Neue Schlüssel:** `PageTitleFeedDetail` (Fallback-Seitentitel), `PlaceholderFeedDetailSearch` (`SearchBar`-Platzhalter), `PlaceholderFeedDetail` (`EmptyView`), `ButtonFeedActions` (Aktions-Button-Text/-Beschreibung). Alle bisherigen Aktions-/Dialog-Schlüssel (`ActionSheetTitleFeed`, `ButtonRefresh` …, `PromptRenameFeed*`, `ConfirmDeleteFeed*`, `FeedErrorDetailsTitle`, `FeedErrorKind*`, `FeedEditSheetTitle`, `FeedNotifications*`, `NotificationsIosOnlyHint`, `ErrorFeedUrlInvalid`, `ErrorFeedDuplicate`, `ErrorFeedTitleEmpty`, `ErrorLoadFailed`, `OfflineHint`, `SyncStatusError`, `CategoryNone`, `AccessibilityBack`, `AccessibilityDismissSheet`) werden wiederverwendet.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `EditUrl` (Edit-Sheet) | Nicht leer, absolute `http`/`https`-URI (`IsValidFeedUrl`) | `ErrorMessage = AppResources.ErrorFeedUrlInvalid`, Sheet bleibt offen |
| `EditUrl` (Edit-Sheet) | `GetByUrlAsync(url)` darf keinen anderen Feed liefern (`Id != Feed.Id`) | `ErrorMessage = AppResources.ErrorFeedDuplicate`, Sheet bleibt offen |
| `newTitle` (Rename-Prompt) | Nicht leer/Whitespace | `ErrorMessage = AppResources.ErrorFeedTitleEmpty`, Titel bleibt |
| `feedId` (Query-Parameter) | Muss parsebare `Guid` sein | `ApplyQueryAttributes` kehrt ohne Ladung zurück (Muster `ArticleDetailPage`) |
| `Feed` (Detailansicht) | `Feed == null` (Laden fehlgeschlagen, `ErrorLoadFailed`) | Aktions-Button deaktiviert (`HasFeed`); `OnFeedActionsClicked`, `SaveEditAsync` und `DeleteFeedAsync` brechen still ab (Früh-Return) |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **`FeedsViewModel`-Schnittstelle schrumpft:** Alle ~15 verschobenen Member werden entfernt — `FeedsViewModelTests` verliert die zugehörigen Testgruppen (sie wandern nach `FeedDetailViewModelTests`). `CreateViewModel()` in den Tests ändert sich (kein `ILocalNotificationService` mehr).
- **`FeedsPage`-Sheet:** Wird Add-only; die `IsEditMode`-`DataTrigger` und der Edit-Block entfallen — kein funktionaler Verlust, da „Bearbeiten" in der Detailansicht lebt.
- **E2E-Bruch:** `SmokeTests.FeedActionSheet_Rename_UpdatesTitle` und `FeedActionSheet_ChangeCategory_IncludingNone` treiben genau das entfernte Aktionsblatt und müssen auf die Detailansicht umgeschrieben werden.
- **Interface-Bruch in Tests:** Die neue `IItemRepository`-Methode muss sofort in `DelegatingItemRepository` nachgezogen werden, sonst kompiliert `Reporter.Tests` nicht.
- **Kaskadierendes Löschen:** `FeedRepository.DeleteAsync` löscht Items/Content/Bilder kaskadierend — gilt jetzt auch für den Detail-Löschpfad (bereits testbelegt).
- **`GetAllWithDetailsAsync` beim Detail-Load:** Projiziert alle Feeds inkl. `UnreadCount`-Subqueries nur für einen Eintrag — bei kleiner Feed-Anzahl unkritisch; kein neues Repository-Member nötig.
- **`FeedSyncService`:** Nutzt weiterhin `GetByFeedAsync(Guid)` — unverändert.
- **Risiko `Shell.Current` in VM-Methoden:** `GoBackAsync`/`DeleteFeedAsync` navigieren über `Shell.Current` — Präzedenzfall `ArticleDetailViewModel.GoBackAsync`; in Unit-Tests nicht aufrufbar (Lösch-Test prüft nur Repository-Effekt, Navigation ist E2E-Thema).

## Umsetzungsreihenfolge

1. **`IItemRepository`-Überladung + `ItemRepository`-Implementierung + `DelegatingItemRepository`-Nachzug**
   - Voraussetzungen: Keine (Projektion `SelectListItemRows`/`MapToListItem`/`GetContentsAsync`/`GetImageIdsAsync` existieren).
   - Beschreibung: Interface-Methode `GetByFeedAsync(Guid feedId, int page, int pageSize, string? searchTerm = null)` deklarieren, im `ItemRepository` mit `FeedId`-Filter, optionalem `Title.Contains`-Filter, `OrderByDescending(PublishedAt).ThenBy(Id)`, `Skip`/`Take` und `ItemListItem`-Projektion implementieren; `DelegatingItemRepository` (Test-Helfer) um die Delegation erweitern — sonst kompiliert `Reporter.Tests` nicht.

2. **`AppResources`-Schlüssel ergänzen**
   - Voraussetzungen: Keine.
   - Beschreibung: `PageTitleFeedDetail`, `PlaceholderFeedDetailSearch`, `PlaceholderFeedDetail`, `ButtonFeedActions` in `AppResources.resx`, `AppResources.de.resx` und `AppResources.Designer.cs` anlegen.

3. **`FeedDetailViewModel` anlegen**
   - Voraussetzungen: Schritte 1 und 2; Basisklasse `BaseViewModel` (`TrackConnectivity`/`OnConnectivityChanged`), `ItemListItem.CopyWith`, `FeedHealth`, `FeedSyncErrorKind` existieren.
   - Beschreibung: Neues transientes VM in `src/Reporter.Core/ViewModels/` mit Paging (`_loadLock`, `LoadAsync(Guid)`, `LoadMoreCommand`, `LoadPageCoreAsync`, `HasMore`, `IsLoading`), `Feed`/`Categories`-Laden, `SearchText`-Reset, Karten-Commands (`ToggleSavedCommand`, `MarkReadCommand`), Feed-Aktionen (`RefreshCommand`/`SyncAsync`, `RenameFeedAsync`, `ChangeFeedCategoryAsync`, `DeleteFeedAsync` inkl. `GoToAsync("..")`, `GetFeedErrorMessage`, `MakeUniqueOptionLabels`, `ToFeed`, `IsValidFeedUrl`, `GoBackCommand`), Edit-Sheet-Zustand (`EditCommand`, `SaveEditCommand`, `CloseEditFormCommand`, `EditUrl`, `EditNotificationsEnabled`, `ShowEditForm`, `NotificationsSupported`) sowie `AttachConnectivity`/`DetachConnectivity`.

4. **`FeedDetailPage` anlegen**
   - Voraussetzungen: Schritte 2 und 3; `ArticleCardView`, `SearchBar`-Style (`Styles.xaml`), `LaterPage`-/`FeedsPage`-XAML-Muster existieren.
   - Beschreibung: `ContentPage` + `IQueryAttributable` (`feedId`-Parsing → `LoadAsync`); XAML `Grid RowDefinitions="Auto,*,Auto"`: Kopf (Back-Button, Favicon/`FeedInitial`-Avatar, Titel, Kategorie/`LastCheckedAt`, `HealthStatus`-Badge, `UnreadCount`, Aktions-Button mit `IsEnabled="{Binding HasFeed}"`, Offline-`Border`, `ErrorMessage`/`SyncErrorMessage`-Labels, `SearchBar`), `RefreshView` + `CollectionView` (Schwelle `2`, `ArticleCardView`-Template via `x:Reference`, `EmptyView`), `ActivityIndicator`; Edit-Sheet-Overlay (`ShowEditForm`-Trigger, Dismiss-`BoxView`, `EditUrlEntry`, Notification-`Switch`, Speichern/Abbrechen); Code-Behind: `OnFeedActionsClicked` (Aktionsblatt + Verteilung, Früh-Return bei `Feed == null`), `RenameFeedAsync`, `ChangeCategoryAsync`, `ConfirmDeleteFeedAsync`, `ShowFeedErrorDetailsAsync`, `OnBackButtonPressed` (Sheet schließt), `PropertyChanged`-Fokus für `EditUrlEntry`, `OnAppearing`/`OnDisappearing` → `AttachConnectivity`/`DetachConnectivity`. AGENTS.md-Regeln explizit: Alle neuen Elemente (Aktions-Button, Edit-Sheet-Controls, Dismiss-`BoxView`, Karten-Buttons) verwenden `AppThemeBinding` für Farben (Dark Mode) und halten Touch-Targets ≥ 44 × 44 pt ein; keine verschachtelten `ScrollView`, Liste füllt `Grid`-Zeile `*`.

5. **Route + DI registrieren**
   - Voraussetzungen: Schritt 4.
   - Beschreibung: `Routing.RegisterRoute("feeddetail", typeof(FeedDetailPage))` in `AppShell.xaml.cs`; `.AddTransient<FeedDetailViewModel>()`/`.AddTransient<FeedDetailPage>()` in `MauiProgram.cs`.

6. **`FeedsPage`/`FeedsViewModel` abbauen**
   - Voraussetzungen: Schritt 5 (Route muss existieren, bevor navigiert wird).
   - Beschreibung: `OnFeedTapped` → `Shell.Current.GoToAsync`; die vier Aktions-Hilfsmethoden entfernen; Edit-Block und `IsEditMode`-Trigger aus `FeedsPage.xaml` entfernen; in `FeedsViewModel`/`FeedsViewModel.Search.cs` die unter „Änderungen" gelisteten Member abbauen.

7. **`FakeFeedSyncService` um `SyncFeedAsync`-Zähler erweitern**
   - Voraussetzungen: Keine (Test-Infrastruktur vorhanden).
   - Beschreibung: Aufrufzählung/FeedId-Aufzeichnung für `SyncFeedAsync`, damit `RefreshCommand`-Tests des `FeedDetailViewModel` den Einzel-Sync nachweisen können (bisher liefert das Fake nur ein festes `SyncResult`).

8. **Unit-/Integrationstests schreiben und anpassen**
   - Voraussetzungen: Schritte 1–7; `TestDbContextFactory`, `TestDataSeeder`, `BlockingItemRepository`-Muster, Fakes vorhanden.
   - Beschreibung: `ItemRepositoryTests` um Paged-/Such-Tests erweitern; `FeedDetailViewModelTests` neu anlegen (inkl. der aus `FeedsViewModelTests` überführten Aktions-Tests); `FeedsViewModelTests` um die entfernten Member-Tests bereinigen und `CreateViewModel()` an die neue Signatur anpassen.

9. **E2E-Tests anpassen und ergänzen**
   - Voraussetzungen: Schritte 4–6; `ReporterAppFixture`, `E2EPageHelpers`, `UiRetry`, `StubFeedServer`, `FeedDbAssertions` vorhanden.
   - Beschreibung: `E2EPageHelpers` um „Feed-Detail öffnen" (Karte tappen → Anker der Detailseite abwarten) und ggf. einen Scroll-Helper (FlaUI `ScrollPattern`/`EnsureVisible` auf der `CollectionView`) erweitern; neue `FeedDetailTests` für Navigation, Suche, Infinite Scroll, Aktionsblatt-Aktionen (Aktualisieren, Umbenennen, Kategorie, Bearbeiten, Fehlerdetails-Sichtbarkeit, Löschen+Rücknavigation) sowie Soll-Test Pull-to-Refresh; `SmokeTests.FeedActionSheet_*` auf den Detail-Fluss umschreiben.

10. **UI-Verifikation + statische Checks**
    - Voraussetzungen: Schritte 1–9.
    - Beschreibung: Manuelle Prüfung der `FeedDetailPage` auf mobilem Formfaktor (Windows-Fenster 390 × 844 pt, handysize) — kein `design-draft`-Entwurf vorhanden, Referenz ist `LaterPage`; dabei explizit `AppThemeBinding` (Dark Mode wechseln und prüfen), Touch-Targets ≥ 44 × 44 pt, Infinite-Scroll-Nachladen per Wischgeste und Pull-to-Refresh (falls der FlaUI-Test nicht stabil ist) manuell verifizieren; Screenshot/Getestete-Größen in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` dokumentieren (AGENTS.md); `.\scripts\Run-StaticChecks.ps1` muss mit Exit-Code 0 ohne Findings durchlaufen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `GetByFeedAsync_Paged_ReturnsOnlyMatchingFeed` | `ItemRepositoryTests` | Filter `FeedId`, liefert `ItemListItem` statt `Item` |
| `GetByFeedAsync_Paged_ReturnsPage` | `ItemRepositoryTests` | `Skip`/`Take`-Blättern über mehrere Seiten |
| `GetByFeedAsync_Paged_OrdersByPublishedAtDescending` | `ItemRepositoryTests` | Absteigende Sortierung inkl. `Id`-Tiebreak |
| `GetByFeedAsync_Paged_FiltersBySearchTerm` | `ItemRepositoryTests` | `Title.Contains`-Filter, Case-Verhalten, leerer Begriff = kein Filter |
| `LoadAsync_PopulatesFeedAndItems` | `FeedDetailViewModelTests` | `Feed`-Kopfdaten (`FeedListItem` inkl. `CategoryName`/`UnreadCount`) + erste Seite |
| `LoadAsync_OrdersByPublishedAtDescending` | `FeedDetailViewModelTests` | Sortierung der Liste |
| `LoadAsync_UnknownFeed_SetsErrorMessage` | `FeedDetailViewModelTests` | `ErrorLoadFailed` bei unbekannter `feedId` |
| `LoadMoreCommand_AppendsNextPage` / `_WhenNoMoreItems_DoesNothing` | `FeedDetailViewModelTests` | Infinite-Scroll-Verhalten, `HasMore`-Logik |
| `SearchText_Changed_ResetsPagingAndFilters` | `FeedDetailViewModelTests` | Suchbegriff filtert serverseitig, setzt Liste/Seite zurück |
| `LoadCommand_WhenRepositoryFails_SetsLocalizedErrorMessage` | `FeedDetailViewModelTests` | Fehlerpfad `ErrorLoadFailed` (via `BlockingItemRepository`-Muster aus `LaterViewModelTests`) |
| `RefreshCommand_InvokesSyncFeed_AndReloads` / `_WhenOffline_SkipsSync` / `_WhenSyncReturnsError_SetsLocalizedSyncErrorMessage` | `FeedDetailViewModelTests` | Einzel-Feed-Sync inkl. `FakeFeedSyncService`-Zähler, Offline-Pfad, `SyncStatusError` |
| `RenameFeedAsync_UpdatesTitle` / `_EmptyTitle_SetsError` / `_NullFeed_DoesNothing` / `_PreservesFaviconUrl` / `_PreservesLastError` | `FeedDetailViewModelTests` | Aus `FeedsViewModelTests` überführt |
| `ChangeFeedCategoryAsync_SetsCategoryId` / `_EmptyGuid_ClearsCategory` / `_NullArguments_DoNothing` | `FeedDetailViewModelTests` | Aus `FeedsViewModelTests` überführt |
| `GetFeedErrorMessage_MapsKindToLocalizedText` / `_FallsBackToUnknown` / `_AppendsTechnicalMessage` | `FeedDetailViewModelTests` | Aus `FeedsViewModelTests` überführt |
| `MakeUniqueOptionLabels_*` (3 Tests) | `FeedDetailViewModelTests` | Aus `FeedsViewModelTests` überführt |
| `EditCommand_PrefillsForm` | `FeedDetailViewModelTests` | `EditUrl`/`EditNotificationsEnabled`/`ShowEditForm` befüllt |
| `SaveEditCommand_PersistsUrlAndNotifications` / `_InvalidUrl_SetsError` / `_DuplicateUrl_SetsError` | `FeedDetailViewModelTests` | Edit-Validierung + `UpdateAsync` (Nachfolger der `SaveCommand_*`-Tests) |
| `SaveEditCommand_PreservesCategoryId` | `FeedDetailViewModelTests` | `feed.CategoryId` bleibt beim URL-Speichern via `ToFeed` erhalten (Nachfolger von `SaveCommand_EditMode_PreservesCategoryId`) |
| `SaveEditCommand_WithoutFeed_DoesNothing` | `FeedDetailViewModelTests` | Früh-Return bei `Feed == null` (Nachfolger von `SaveCommand_WithoutSelectedFeed_DoesNotAddFeed`) |
| `SaveEditCommand_ResetsEditState` | `FeedDetailViewModelTests` | Sheet schließt und Edit-Zustand (`EditUrl`/`EditNotificationsEnabled`) wird zurückgesetzt (Nachfolger von `SaveCommand_ResetsFeedNotificationsEnabled`) |
| `DeleteFeedAsync_RemovesFeed` / `_WithoutFeed_DoesNothing` | `FeedDetailViewModelTests` | Repository-Löschung + Früh-Return bei `Feed == null` (Rücknavigation ist E2E-Thema, `Shell.Current` nicht mockbar) |
| `MarkReadCommand_MarksItemRead` / `ToggleSavedCommand_TogglesFlag_KeepsItemInList` | `FeedDetailViewModelTests` | `CopyWith`-Ersetzen; Item bleibt in `Items` (anders als `LaterPage`) |
| `ConnectivityChanged_UpdatesIsOnline` | `FeedDetailViewModelTests` | Connectivity-Tracking via `FakeNetworkStatusService` |
| `SyncFeedAsync`-Aufrufzähler | `FakeFeedSyncService` (Hilfs-Fake) | Zählt/protokolliert Einzel-Syncs für Refresh-Tests |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `FeedsViewModelTests`: `RefreshCommand_*` (5 Tests), `EditCommand_*`/`EditAsync_*`/`SaveCommand_*`/`DeleteCommand_*` (~10 Tests), `RenameFeedAsync_*` (5), `ChangeFeedCategoryAsync_*` (3), `GetFeedErrorMessage_*` (3), `MakeUniqueOptionLabels_*` (3) | Member wurden nach `FeedDetailViewModel` verschoben — Tests werden überführt bzw. entfernt; `CreateViewModel()` an neue Konstruktor-Signatur (ohne `ILocalNotificationService`) anpassen |
| `FeedsViewModelTests`: `SearchCommand_*`/`DirectAddCommand_*` mit `IsEditMode`-Bezug | `IsEditMode` entfällt — Guards/CanExecute vereinfachen sich |
| `DelegatingItemRepository` | Neue `IItemRepository`-Methode muss delegiert werden (Kompilier-Voraussetzung) |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Tap auf Feed-Karte öffnet Detailansicht mit Beitragsliste (`FeedDetail_Navigation_ShowsFeedItems`) | `FeedDetailTests` (neu, `src/Reporter.E2ETests/`) | Tap navigiert statt Aktionsblatt; `Item`-Datensätze des Feeds werden angezeigt | Kern-Benutzerfluss der Anforderung (Navigation + Listen-Rendering) — Unit-Tests können Shell-Routing/`IQueryAttributable` nicht prüfen |
| Pflicht | Suche filtert die Beitragsliste (`FeedDetail_Search_FiltersItems`) | `FeedDetailTests` | Suchfeld findet ältere Beiträge, nicht passende verschwinden | Interaktion `SearchBar` → Repository-Filterung → `CollectionView` nur über UI verifizierbar |
| Pflicht | Aktionsblatt „Umbenennen" aktualisiert Titel (`FeedDetail_Rename_UpdatesTitle`) | `FeedDetailTests` (ersetzt `SmokeTests.FeedActionSheet_Rename_UpdatesTitle`) | Feed-Aktionen in Detailansicht verlagert | `DisplayActionSheetAsync`/`DisplayPromptAsync` sind reine UI-Mechanik |
| Pflicht | Aktionsblatt „Kategorie ändern" inkl. Pseudo-Eintrag „Keine" (`FeedDetail_ChangeCategory_IncludingNone`) | `FeedDetailTests` (ersetzt `SmokeTests.FeedActionSheet_ChangeCategory_IncludingNone`) | Kategorie-Zuordnung ändern/entfernen über Detailansicht | Gleicher UI-Fluss, anderer Trigger-Punkt; Assertion auf Feed-Karte nach Rücknavigation |
| Pflicht | Aktionsblatt „Löschen" entfernt Feed und navigiert zurück (`FeedDetail_Delete_ReturnsToList`) | `FeedDetailTests` | Lösch-Bestätigung, kaskadierendes Löschen, Rücksprung zur Übersicht | Navigations-Nebeneffekt nach `DeleteAsync` nur in der laufenden App prüfbar |
| Pflicht | Aktionsblatt „Bearbeiten" öffnet Sheet; URL-/Benachrichtigungs-Änderung persistiert (`FeedDetail_Edit_PersistsChanges`) | `FeedDetailTests` | Edit-Sheet in Detailansicht (bestätigte Entscheidung: eigenes Sheet statt Rücksprung zur `FeedsPage`) | Sheet-UI + Persistenz; Assertion ggf. via `FeedDbAssertions` auf `reporter.db` |
| Pflicht | Aktionsblatt „Aktualisieren" löst Einzel-Feed-Sync aus (`FeedDetail_RefreshAction_SyncsFeed`) | `FeedDetailTests` | Aktion „Aktualisieren" in der Detailansicht (`SyncFeedAsync`-Einzel-Sync) | Der Aktionsblatt-Pfad ist per Klick automatisierbar (anders als die Pull-Geste); Sync-Effekt via `FeedDbAssertions` (neue Items/`LastCheckedAt` in `reporter.db`) oder sichtbar aktualisierter Liste nachweisen — `StubFeedServer` liefert nach dem ersten Sync ein geändertes Feed-Dokument |
| Pflicht | Scrollen ans Listenende lädt nächste Seite nach (`FeedDetail_InfiniteScroll_LoadsNextPage`) | `FeedDetailTests` | Seitenweise nachladende Liste (Infinite Scroll, `PageSize = 20`) | Nachladen wird durch die Nutzeraktion „Scrollen" ausgelöst — Unit-/Integrationstests (`LoadMoreCommand_*`, `GetByFeedAsync_Paged_ReturnsPage`) ersetzen die E2E-Abdeckung nicht. Umsetzung: > `PageSize` Beiträge seeden (`TestDataSeeder`/DB-Seed), Liste via FlaUI-`ScrollPattern`/`EnsureVisible` ans Ende scrollen, Sichtbarkeit eines Beitrags der zweiten Seite abwarten (`UiRetry`). Erweist sich die Scroll-Geste als nicht zuverlässig automatisierbar, gilt analog Pull-to-Refresh: begründeter Soll-Test + dokumentierte manuelle Verifikation in Schritt 10 |
| Pflicht | Aktionsblatt „Fehlerdetails anzeigen" nur bei `HealthStatus == Error` sichtbar + Alert (`FeedDetail_ErrorDetails_OnlyForErrorFeed`) | `FeedDetailTests` | Aktion „Fehlerdetails anzeigen" — bedingte Sichtbarkeit | Die Sichtbarkeitsregel liegt im Code-Behind (`FeedDetailPage.OnFeedActionsClicked`) und ist ausschließlich über die UI prüfbar — `GetFeedErrorMessage_*`-Unit-Tests decken nur das Textmapping. Umsetzung: fehlerhaften Feed über `StubFeedServer` (fehlschlagende Feed-URL → `HealthStatus == Error` nach Sync) und gesunden Feed seeden; bei Error-Feed enthält das Aktionsblatt den Eintrag und der Alert zeigt den lokalisierten Fehlertext, beim gesunden Feed fehlt der Eintrag |
| Soll | Pull-to-Refresh löst Einzel-Sync aus (`FeedDetail_PullToRefresh_SyncsFeed`) | `FeedDetailTests` | `RefreshView` gebunden (bestätigte Entscheidung) | Geste schwer automatisierbar — FlaUI-`RefreshView`-Triggerung ggf. instabil; der Aktionsblatt-Pfad ist bereits durch `FeedDetail_RefreshAction_SyncsFeed` (Pflicht) abgedeckt; falls der Test nicht stabil läuft: manuelle Verifikation in Schritt 10 dokumentieren |

Betroffene bestehende E2E-Tests:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `SmokeTests.FeedActionSheet_Rename_UpdatesTitle` | Aktionsblatt existiert an alter Stelle nicht mehr — Test wird durch `FeedDetailTests.FeedDetail_Rename_UpdatesTitle` ersetzt |
| `SmokeTests.FeedActionSheet_ChangeCategory_IncludingNone` | Wird durch `FeedDetailTests.FeedDetail_ChangeCategory_IncludingNone` ersetzt |
| `E2EPageHelpers` | Neuer Helper „Feed-Detail öffnen" (Karte tappen → Detail-Anker abwarten, z. B. `PlaceholderFeedDetailSearch`/`ButtonFeedActions`); ggf. Scroll-Helper für `FeedDetail_InfiniteScroll_LoadsNextPage` (FlaUI `ScrollPattern`/`EnsureVisible`); privates `TryPopPushedPage` ggf. als öffentlicher Back-Helper für Rücknavigations-Assertions nutzen |

## Offene Punkte

Keine.
