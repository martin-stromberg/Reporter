<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: Detailansicht für Feeds

## Fachliche Zusammenfassung

Der Tap auf einen `FeedListItem` in der `FeedsPage` soll nicht mehr das Aktionsblatt (`DisplayActionSheetAsync` in `FeedsPage.OnFeedTapped`) öffnen, sondern auf eine neue Feed-Detailansicht navigieren. Diese zeigt alle `Item`-Datensätze des Feeds – gelesene wie ungelesene – absteigend sortiert nach `PublishedAt` in einer seitenweise nachladenden Liste (Infinite Scroll) und bietet eine Suche zum Wiederfinden älterer Beiträge. Die bisherigen Feed-Aktionen (Aktualisieren, Umbenennen, Kategorie ändern, Bearbeiten, Fehlerdetails anzeigen, Löschen) werden in die Detailansicht verlagert.

## Betroffene Klassen und Komponenten

- **UI-Komponenten / Pages**
  - Neu: `FeedDetailPage` (`src/Reporter/Views/`) – `ContentPage` mit `IQueryAttributable` analog zu `ArticleDetailPage`, Query-Parameter z. B. `feedId`; enthält `SearchBar` und `CollectionView` mit `RemainingItemsThresholdReachedCommand` (Muster: `LaterPage`).
  - Geändert: `FeedsPage.xaml.cs` – `OnFeedTapped` navigiert per `Shell.Current.GoToAsync($"feeddetail?feedId={feed.Id}")` statt `DisplayActionSheetAsync`; die Aktionslogik (`RenameFeedAsync`, `ChangeCategoryAsync`, `ConfirmDeleteFeedAsync`, `ShowFeedErrorDetailsAsync`) wandert in die Detailansicht.
  - Wiederverwendet: `ArticleCardView` für die Beitragskarten; dessen Default-`OpenArticleCommand` navigiert bereits zu `articledetail?itemId=...`.
- **ViewModels**
  - Neu: `FeedDetailViewModel` (`src/Reporter.Core/ViewModels/`) – Paging-Logik mit `LoadCommand`/`LoadMoreCommand`, `HasMore`, `IsLoading`, `ObservableCollection<ItemListItem>` und Suchtext-Property mit Filterung (Muster: `LaterViewModel`, ggf. `partial`-Aufteilung wie `FeedsViewModel`/`FeedsViewModel.Search.cs`); übernimmt die Feed-Aktionen aus `FeedsViewModel` (`RefreshCommand`, `RenameFeedAsync`, `ChangeFeedCategoryAsync`, `DeleteCommand`, `EditCommand`, `GetFeedErrorMessage`).
  - Geändert: `FeedsViewModel` – Feed-Aktionslogik ggf. entfernen oder für die Detailansicht zugänglich machen (Abhängigkeit zu `SelectedFeed`/`FeedListItem` beachten).
- **Interfaces**
  - `IItemRepository`: neue pagede Abfrage pro Feed mit Suchbegriff, z. B. `GetByFeedAsync(Guid feedId, int page, int pageSize, string? searchTerm = null)` mit Rückgabetyp `IReadOnlyList<ItemListItem>` (das vorhandene `GetByFeedAsync(Guid)` liefert nur ungefiltert `Item`-Objekte und ist ungepaged).
- **Datenzugriff**
  - `ItemRepository` (`src/Reporter.Data/Repositories/`): Implementierung der neuen Abfrage – Filter `FeedId`, `OrderByDescending(i => i.PublishedAt)` mit `ThenBy(i => i.Id)`, `Skip`/`Take`, optionaler Suchbegriff-Filter auf den Titel; `SelectListItemRows`-Projektion wiederverwenden.
- **Navigation / DI**
  - `AppShell.xaml.cs`: `Routing.RegisterRoute("feeddetail", typeof(FeedDetailPage))`.
  - `MauiProgram.cs`: `AddTransient<FeedDetailViewModel>()` und `AddTransient<FeedDetailPage>()`.
- **Lokalisierung**
  - `AppResources` (resx + Designer): neue Schlüssel für Seitentitel, Such-Platzhalter, Leer-Text der Liste.
- **Tests**
  - Neu: `FeedDetailViewModelTests` (Paging, Suche, Aktionen) in `src/Reporter.Tests/`.
  - Erweitert: `FeedRepositoryTests`/ItemRepository-Tests für die neue Abfrage; `FeedsViewModelTests` für das geänderte Tap-Verhalten.
  - Ggf. `src/Reporter.E2ETests/` für den Navigationsfluss.

## Implementierungsansatz

- **Navigation:** Shell-Route `feeddetail` mit Query-Parameter `feedId`, exakt nach dem Muster `articledetail`/`itemId` (`AppShell.xaml.cs`, `ArticleDetailPage.ApplyQueryAttributes`). `FeedsPage.OnFeedTapped` ersetzt das Aktionsblatt durch `Shell.Current.GoToAsync`.
- **Infinite Liste:** `CollectionView` mit `RemainingItemsThreshold="2"` und `RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}"`; Paging im ViewModel mit `SemaphoreSlim`-Ladesperre und `HasMore = items.Count == PageSize` (Muster `LaterViewModel`, `PageSize = 20`).
- **Suche:** Lokale Filterung der Feed-Beiträge; der Suchbegriff wird an die Repository-Abfrage weitergereicht und setzt das Paging zurück (Annahme: Suche im Titel, siehe Offene Fragen). `SearchBar`-Style existiert bereits in `Styles.xaml`.
- **Feed-Aktionen:** Die Aktionslogik (`DisplayActionSheetAsync`, `DisplayPromptAsync`, `DisplayAlertAsync`) aus `FeedsPage.xaml.cs` wird in die `FeedDetailPage` verlagert; die Commands/Methoden des `FeedsViewModel` werden entweder ins `FeedDetailViewModel` überführt oder das `FeedDetailViewModel` erhält Zugriff auf dieselben Services (`IFeedRepository`, `IFeedSyncService`, `ICategoryRepository`).
- **Abhängigkeiten:** `IItemRepository` (neue Methode), `IFeedRepository`, `IFeedSyncService`, `ICategoryRepository`, `INetworkStatusService` (Connectivity-Tracking via `BaseViewModel`).
- **UI-Regeln (AGENTS.md):** Kartenbasiertes `CollectionView`, keine verschachtelten `ScrollView`, Liste füllt `Grid`-Zeile `*`, `AppThemeBinding` für Dark Mode, Touch-Targets ≥ 44 pt, Verifikation gegen `design-draft/` (kein vorhandener Entwurf für diese Ansicht) sowie mobile Formfaktoren.

## Konfiguration

Keine Konfiguration erforderlich; das Verhalten ist nicht konfigurierbar angefordert.

## Offene Fragen

- **Suchumfang:** Soll die Suche nur den Beitragstitel oder auch Inhalt/Zusammenfassung durchsuchen? (Annahme: Titel; `Summary`/`ContentHtml` wäre deutlich aufwendiger.)
- **Umgang mit dem bisherigen Kontextmenü in der Übersicht:** Entfällt das Aktionsblatt in der `FeedsPage` vollständig (auch für Long-Press) oder bleibt ein alternativer Zugriff bestehen?
- **Bearbeiten-Aktion:** Das Editier-Sheet (`ShowAddForm`/`IsEditMode`) ist aktuell Teil der `FeedsPage`. Soll die Detailansicht zur `FeedsPage` zurücknavigieren und dort den Edit-Modus öffnen, oder erhält die Detailansicht ein eigenes Bearbeitungs-UI?
- **Gelesen-Status:** Soll der Lesestatus in der Liste visuell unterscheidbar sein (z. B. abgeschwächte Darstellung), und soll ein Tap auf einen Beitrag ihn als gelesen markieren (Verhalten von `ArticleCardView`/`MarkReadCommand`)?
- **Feed-Metadaten:** Soll die Detailansicht Kopfdaten des Feeds (Titel, Kategorie, `HealthStatus`, `UnreadCount`, `FaviconUrl`) anzeigen?
- **Löschen-Navigation:** Nach dem Löschen des Feeds in der Detailansicht – Rücknavigation zur Feed-Übersicht?
- **Pull-to-Refresh:** Soll die Detailansicht ein `RefreshView` für den Einzel-Feed-Sync (`RefreshCommand`) anbieten?
