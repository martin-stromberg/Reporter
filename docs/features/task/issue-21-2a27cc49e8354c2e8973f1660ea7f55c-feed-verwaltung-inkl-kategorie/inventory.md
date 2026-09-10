# Bestandsaufnahme

## Vorhandene Bausteine
- `Reporter.Core/Models/Feed.cs`: Domänenmodell bereits mit `Url`, `Title`, `CategoryId`, `LastCheckedAt`, `HealthStatus`, `HealthLastChange`.
- `Reporter.Core/Models/Category.cs` und `CategoryWithCount.cs`: Kategorien vorhanden.
- `Reporter.Core/Interfaces/IFeedRepository.cs`: Grund-CRUD für Feeds.
- `Reporter.Data/Repositories/FeedRepository.cs`: Implementierung gegen `ReporterDbContext`.
- `Reporter.Data/Repositories/ItemRepository.cs` und `IItemRepository`: Artikel inkl. ungelesen-Filter.
- `Reporter.Data/ReporterDbContext.cs`: Schema hat `feeds`, `categories`, `items` mit Cascade-Delete für Items.
- `Reporter.Core/ViewModels/CategoriesViewModel.cs` und `Views/CategoriesPage.xaml`: Muster für CRUD-Seite mit CollectionView.
- `Reporter/Resources/Strings/AppResources.resx` und `.de.resx`: Lokalisierungs-Grundgerüst.
- `Reporter/Resources/Styles/Colors.xaml`: Status-Farben `Light/DarkStatusOk/Warning/Error`.
- `Reporter/ViewModels/FeedsViewModel.cs` und `Views/FeedsPage.xaml`: Platzhalter, müssen ersetzt werden.
- `Reporter/MauiProgram.cs`: DI für Repository und ViewModels.

## Lücken
- Feeds-Seite ist reiner Platzhalter.
- Fehlende ViewModel-Logik für Hinzufügen/Bearbeiten/Löschen/Validierung.
- Keine Übersicht mit Kategorie und ungelesenen Artikeln.
- Health-Status wird nicht in der UI dargestellt.
- Keine Feed-spezifischen Lokalisierungstexte.
