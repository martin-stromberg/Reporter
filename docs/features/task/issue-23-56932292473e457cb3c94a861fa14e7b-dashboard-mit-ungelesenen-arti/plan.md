# Umsetzungsplan

## Offene Punkte
Keine.

## Änderungen

### 1. Domain-Modell
- Neues `Reporter.Core/Models/ItemListItem.cs` mit Anzeigefeldern:
  - `Id`, `FeedId`, `Title`, `Link`, `PublishedAt`, `IsRead`, `IsSavedForLater`
  - `FeedTitle` (Quelle)
  - `CategoryId`, `CategoryName` (Kategorie)

### 2. Repository
- `IItemRepository` erweitern:
  - `GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null)`
  - `GetUnreadCountAsync(Guid? categoryId = null)`
  - `MarkAllAsReadAsync(Guid? categoryId = null)`
  - `ToggleSavedForLaterAsync(Guid id)`
- `ItemRepository` implementiert diese Methoden.

### 3. ViewModel
- `Reporter.Core/ViewModels/UnreadViewModel.cs` vollständig ersetzen:
  - Properties: `Articles` (ObservableCollection<ItemListItem>), `Categories` (ObservableCollection<CategoryFilterItem>), `SelectedCategory`, `IsSyncing`, `IsLoading`, `IsRefreshing`, `HasMore`, `ErrorMessage`, `HasError`, `LastSyncText`, `UnreadCount`
  - Commands: `LoadCommand`, `LoadMoreCommand`, `RefreshCommand`, `MarkAllReadCommand`, `ToggleSavedCommand`, `OpenArticleCommand`, `MarkReadCommand`
  - Paging: Seite 0 initial, `LoadMoreCommand` lädt nächste Seite.
  - Filter: Auswahl eines Chips setzt `SelectedCategory`, lädt Seite 0 neu, zeigt Anzahl je Kategorie.
  - Sync: `RefreshCommand` ruft `IFeedSyncService.SyncAllAsync()` an, lädt danach neu.

### 4. UI
- `Views/UnreadPage.xaml` ersetzen:
  - Grid mit Header (Titel + Sync-Indikator + Refresh-Button + "Alles gelesen"-Button).
  - Horizontale Kategorie-Chips (`CollectionView` horizontal, `RoundRectangle`-Border, Active-State).
  - `RefreshView` um `CollectionView` (Pull-to-Refresh).
  - `CollectionView` mit Artikelkarten:
    - Quelle, Zeit, Kategorie-Label oben
    - Titel (2 Zeilen), optional Vorschaubild/Image-Platzhalter
    - Lesezeichen- und Als-gelesen-Icon-Buttons (ImageButton 44×44)
    - Ungelesen-Indikator (Dot)
    - `TapGestureRecognizer` für OpenArticle / ActionSheet
  - Last-Sync-Label und "Zum Aktualisieren nach unten ziehen"-Hinweis.
- `Views/UnreadPage.xaml.cs`:
  - `OnAppearing` ruft `LoadCommand`.
  - `OnArticleTapped` zeigt `DisplayActionSheet` mit Aktionen.

### 5. Hilfselemente
- `CategoryFilterItem` (Core/Models oder ViewModel-Inner) mit `Name`, `Count`, `IsSelected`.
- Neuer Styles ggf. in `Styles.xaml` für Chips / Card (bestehende Styles wiederverwenden).

### 6. Lokalisierung
- `AppResources.resx` und `AppResources.de.resx` ergänzen:
  - `ButtonMarkAllRead`, `ButtonBookmark`, `ButtonMarkAsRead`, `LabelNoArticles`, `LabelPullToRefresh`, `LabelJustNow`, `ActionSheetArticle`, `FilterAll`.
- `AppResources.Designer.cs` automatisch via ResGen neu generieren lassen.

### 7. Tests
- `ItemRepositoryTests` erweitern um Paging, Kategoriefilter, MarkAllAsRead, Count, ToggleSavedForLater.
- `UnreadViewModelTests` hinzufügen: Laden, Kategorie-Wechsel, LoadMore, MarkAllRead, Sync.
- `dotnet build Reporter.sln` und `dotnet test Reporter.sln` ausführen.

## E2E-Szenarien
1. Benutzer öffnet "Ungelesen", sieht ungelesene Artikel sortiert nach Datum.
2. Benutzer wählt Kategorie-Chip, Liste zeigt nur Artikel dieser Kategorie.
3. Benutzer zieht herunter, Sync startet, Liste aktualisiert sich.
4. Benutzer scrollt nach unten, weitere Artikel werden nachgeladen.
5. Benutzer tippt auf "Alles gelesen", alle angezeigten Artikel verschwinden.
