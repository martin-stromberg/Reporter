# Umsetzungsplan

## Offene Punkte
Keine.

## Änderungen

### 1. Domain-Modell
- Neues `Reporter.Core/Models/FeedListItem.cs` mit Anzeigefeldern inkl. `CategoryName`, `UnreadCount`, `HealthStatus`, `HealthLastChange`.

### 2. Repository
- `IFeedRepository` erweitern:
  - `GetAllWithDetailsAsync()`
  - `GetByUrlAsync(string url)`
- `FeedRepository` implementiert beide Methoden.
- Ungelesene Artikel pro Feed zählen über `context.Items` Subquery.

### 3. ViewModel
- `Reporter/ViewModels/FeedsViewModel.cs` vollständig ersetzen:
  - Properties: `Feeds` (ObservableCollection<FeedListItem>), `Categories` (ObservableCollection<Category>), `NewUrl`, `NewTitle`, `SelectedCategory`, `SelectedFeed`, `ErrorMessage`, `HasError`.
  - Commands: `LoadCommand`, `SaveCommand`, `EditCommand`, `DeleteCommand`.
  - Validierung: URL-Validität, Duplikat-Prüfung.
  - Kategorie-Option „—“ (leere Id) für „Keine Kategorie“.

### 4. UI
- `Views/FeedsPage.xaml` ersetzen:
  - Eingabemaske: URL, Title, Picker für Kategorie, Speichern-Button.
  - CollectionView mit Grid-Spalten: Name, Kategorie, letzter Abruf, Ungelesen, Status, Bearbeiten, Löschen.
  - Health-Status als farbiges Label mit DataTrigger.
- `Views/FeedsPage.xaml.cs`:
  - `OnAppearing` ruft `LoadCommand`.
  - Delete-Handler zeigt `DisplayAlert`-Bestätigung und ruft `DeleteCommand`.

### 5. Lokalisierung
- `AppResources.resx` und `AppResources.de.resx` um Feed-Texte erweitern.
- `AppResources.Designer.cs` um neue `public static string`-Properties ergänzen.

### 6. Tests
- `dotnet build Reporter.sln` und `dotnet test Reporter.sln` ausführen.
- Ergebnisse in `test-results.md`.

## E2E-Szenarien
1. Benutzer öffnet Feeds-Tab, fügt Feed mit URL und Titel hinzu, sieht ihn in der Liste.
2. Benutzer weist einem Feed eine Kategorie zu und sieht Kategorie in der Liste.
3. Benutzer löscht einen Feed; Bestätigungsdialog erscheint; nach Bestätigung verschwindet der Feed.
4. Health-Status wird als farbiger Text in der Liste angezeigt.
