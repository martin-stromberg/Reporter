<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik — Bestandsaufnahme Lesepakete

## `UnreadViewModel`

Datei: `src/Reporter.Core/ViewModels/UnreadViewModel.cs` (746 Zeilen)

Konstante: `private const int PageSize = 20;` (Zeile 20)

Zustand: `_currentPage` (Paging-Index), `_articles` (`ObservableCollection<ItemListItem>` mit `CollectionChanged`-Handler), `_selectedCategory`, `_isLoading`, `_hasMore`, `_cardPosition` u. a. Es existiert kein Feld für einen Tages-/Paketkontext.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `UnreadViewModel(...)` | public ctor | Verdrahtet `IItemRepository`, `ICategoryRepository`, `IFeedSyncService`, `ISettingsRepository`, `INetworkStatusService`; erzeugt alle Commands (Zeilen 51–70). |
| `LoadAsync()` | private | Frisches Laden: setzt `_currentPage = 0`, `HasMore = false`, liest Settings (`UnreadListLayout`), lädt Kategorien + `GetUnreadCountAsync` pro Kategorie und für „Alle", setzt `UnreadCount`, wählt ggf. den ersten Chip, ruft `LoadPageAsync(_currentPage)` (Zeilen 421–466). Aufgerufen von `LoadCommand`, `RefreshAsync`, `MarkAllReadAsync` und aus `MarkReadAsync` bei leerer Liste. |
| `LoadMoreAsync()` | private | Guard `IsLoading \|\| !HasMore`; erhöht `_currentPage`, ruft `LoadPageAsync(page, append: true)` (Zeilen 468–477). |
| `LoadPageAsync(int page, bool append)` | private | Kern der Lade-Pipeline: liest Settings, `ascending = settings.UnreadSortOrder == SettingsValues.SortOrderAscending`, ruft `_itemRepository.GetUnreadByDateAsync(page, PageSize, categoryId, ascending)`, ersetzt bzw. appended `Articles`, setzt `HasMore = items.Count == PageSize`, aktualisiert Chip-Count/`UnreadCount` (Zeilen 479–522). Fehler → `ErrorMessage = AppResources.ErrorLoadFailed`. |
| `RefreshAsync()` | private | Offline-Guard (setzt ggf. `IsSyncing` zurück), ruft `_feedSyncService.SyncAllAsync()`, dann `LoadAsync()` (Zeilen 524–570). |
| `MarkAllReadAsync()` | private | Ruft `_itemRepository.MarkAllAsReadAsync(SelectedCategory?.CategoryId)` — **über alle ungelesenen Artikel aller Tage** —, danach `LoadAsync()` (Zeilen 572–577). |
| `SelectCategoryAsync(CategoryFilterItem?)` | private | Setzt `SelectedCategory`, `_currentPage = 0`, `HasMore = false`, ruft `LoadPageAsync(0)` — frischer Neuaufbau der Liste (Zeilen 579–590). |
| `MarkReadAsync(ItemListItem?)` | private | `MarkAsReadAsync(item.Id)`, entfernt Artikel aus `Articles`, dekrementiert `SelectedCategory.Count`, passende Chips und `UnreadCount`; ruft `LoadAsync()` nur bei `Articles.Count == 0` (Zeilen 608–640). Keine Neubewertung beim einzelnen Entfernen. |
| `ToggleSavedAsync(ItemListItem?)` | private | `ToggleSavedForLaterAsync`, ersetzt Listeneintrag per `CopyWith` (Zeilen 592–606). |
| `ToggleLayoutAsync()` | private | Layout umschalten und via `SaveAsync(settings.WithUnreadListLayout(...))` persistieren (Zeilen 655–668). |
| `GoToNextCard()` / `GoToPreviousCard()` | private | Kartenstapel-Navigation; `GoToNextCard` triggert `LoadMoreCommand` ab Position `Count - 3` (Zeilen 670–690). |
| `OnArticlesCollectionChanged` / `NotifyListStateChanged` / `NotifyCardStateChanged` | private | Kartenposition clampen, abhängige Properties benachrichtigen (Zeilen 705–729). |
| `UpdateUnreadCountText()` | private | Setzt `UnreadCountText`, benachrichtigt `UnreadBadgeText`, `CardPositionText` (Zeilen 731–738). |
| `OnConnectivityChanged(bool)` | protected override | Leert `SyncErrorMessage`, benachrichtigt `SyncStatusText` (Zeilen 741–745). |

Relevante Properties: `Articles` (Setter resubskribiert `CollectionChanged`, hält Kartenposition auf denselben Artikel), `HasArticles`, `ShowSkeleton`, `ShowEmptyState`, `UnreadCount` (public get, private set), `UnreadBadgeText` (Format `{0} ungelesen` über `AppResources.UnreadCountFormat` auf Basis der **Gesamt**-Ungelesenzahl, Zeilen 218–221), `SelectedCategory`, `IsLoading`, `HasMore`, `ListLayout`, `IsCardLayout`.

Abonnierte Events: `_articles.CollectionChanged` → `OnArticlesCollectionChanged`; Konnektivität via `BaseViewModel.TrackConnectivity` → `OnConnectivityChanged`.
Publizierte Events: keine eigenen; Änderungen über `INotifyPropertyChanged` (`SetProperty`/`OnPropertyChanged`).

## `ItemRepository`

Datei: `src/Reporter.Data/Repositories/ItemRepository.cs` (582 Zeilen; `IDbContextFactory<ReporterDbContext>` + `IItemContentStore`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetUnreadByDateAsync()` | public | Alle ungelesenen `Item` absteigend nach `PublishedAt`, ohne Paging/Kategoriefilter (Zeilen 114–124). |
| `GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId, bool ascending)` | public | Seitenweise Ungelesen-Abfrage: Filter `!i.IsRead`; bei `categoryId` Einschränkung über `feedIds`-Subquery (`Feed.CategoryId`); Sortierung `OrderBy(PublishedAt).ThenByDescending(Id)` (aufsteigend) bzw. `OrderByDescending(PublishedAt).ThenBy(Id)`; `Skip(page*pageSize).Take(pageSize)`; Projektion via `SelectListItemRows` + `MapToListItem` (Zeilen 127–155). **Keine Tagesbegrenzung.** |
| `GetUnreadCountAsync(Guid? categoryId)` | public | `CountAsync` über alle ungelesenen Artikel, optional kategoriefiltert (Zeilen 165–182). |
| `MarkAllAsReadAsync(Guid? categoryId)` | public | `ExecuteUpdate` setzt `IsRead = true`, `ReadAt = DateTime.UtcNow` für **alle** ungelesenen Artikel (optional kategoriefiltert) — keine Datums-/Tagesgrenze (Zeilen 185–201). |
| `MarkAsReadAsync(Guid id)` | public | Einzelnes Item: `IsRead = true`, `ReadAt = DateTime.UtcNow` (Zeilen 218–230). |
| `ToggleSavedForLaterAsync(Guid id)` | public | Toggle des Gespeichert-Flags (Zeilen 204–215). |
| `SelectListItemRows(IQueryable<ItemEntity>)` | private static | Gemeinsame Projektion auf `ItemListRow` inkl. `i.Feed.CategoryId`/`Category.Name` (Zeilen 408–424). |
| `SavedItemsQuery(context, categoryId)` | private static | Gespeichert-Filter (verwendet `i.Feed.CategoryId` direkt — anders als die feedIds-Subquery der Ungelesen-Methoden) (Zeilen 426–435). |
| `MapToListItem(row, contentHtml, hasLocalImage)` | private | Erzeugt `ItemListItem` (Title-Trim, `ExtractImageUrl`, `ExtractSummary`, `ReadingTimeEstimator.EstimateText`) (Zeilen 437–457). |

Es existiert kein tag-begrenzter Member (keine `GetUnreadByDayAsync`/`MarkDayAsReadAsync`/`GetOldestUnreadDateAsync` o. ä.).

## `UnreadPage` (Code-Behind)

Datei: `src/Reporter/Views/UnreadPage.xaml.cs` (467 Zeilen)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnAppearing()` | protected override | Abonniert `PropertyChanged`, führt `LoadCommand.ExecuteAsync(null)` aus (Zeilen 47–67). |
| `OnMarkAllReadTapped(object, TappedEventArgs)` | private | Guard `!HasArticles`; `DisplayActionSheetAsync` mit Titel `UnreadBadgeText` (Gesamtzahl), `ButtonCancel`, `ButtonMarkAllRead`; bei Bestätigung `MarkAllReadCommand.ExecuteAsync` (Zeilen 135–152). |
| `OnViewModelPropertyChanged` | private | Reagiert auf `IsSyncing`, `IsCardLayout`, `SelectedCategory`, `Articles` (Filter-Reveal-Animation) (Zeilen 87–105). |
| `DismissCardAsync()` / `AdvanceAsync(bool)` / `ResolvePanAsync()` | private | Karten-Gesten: Wisch nach oben → `MarkReadCommand`, links/rechts → `NextCardCommand`/`PreviousCardCommand` (Zeilen 322–430). |

Zugehöriges XAML (`src/Reporter/Views/UnreadPage.xaml`): Doppelhaken-Button (`Path` M18,6 L7,17 …) mit `TapGestureRecognizer Tapped="OnMarkAllReadTapped"` und `SemanticProperties.Description = AppResources.ButtonMarkAllRead` (Zeilen 68–77); darunter Label mit `UnreadBadgeText` (Zeilen 105–107).

## `FeedSyncService` (Herkunft von `PublishedAt`)

Datei: `src/Reporter.Core/Services/FeedSyncService.cs`

- Zeile 321–323: `publishedAt = feedItem.PublishDate == DateTimeOffset.MinValue ? null : feedItem.PublishDate.UtcDateTime` — **`PublishedAt` wird als UTC-`DateTime` gespeichert**; ohne Feed-Datum `null`.
- Zeile 364: `PublishedAt = publishedAt` beim Anlegen neuer `Item`.

## `SettingsRepository` / `Settings`

`ISettingsRepository.GetAsync()` liefert `Settings` inkl. `UnreadSortOrder` (Default `"desc"`) und `UnreadListLayout`; `SaveAsync(Settings)` persistiert. `UnreadViewModel` liest die Settings bei jedem `LoadAsync` und `LoadPageAsync` erneut — die Sortierreihenfolge ist damit je Ladevorgang aktuell.
