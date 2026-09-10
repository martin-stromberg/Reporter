# Bestandsaufnahme

## Architektur
.NET MAUI-App `Reporter` mit SQLite über Entity Framework Core.
- `Reporter.Core`: Models, ViewModels, Interfaces, Services, Ressourcen.
- `Reporter.Data`: Entities, DbContext, Repositories.
- `Reporter`: UI-Seiten, Styles, DI.
- `Reporter.Tests`: xUnit-Tests für Repositories, ViewModels und DbContext.

## Bereits vorhanden
- `Views/UnreadPage.xaml` und `.cs` sowie `ViewModels/UnreadViewModel.cs` als rudimentäre Startseite.
- `IItemRepository` mit `GetUnreadByDateAsync()` (alle ungelesen, sortiert).
- `ICategoryRepository`, `IFeedRepository`, `IFeedSyncService`.
- `AppShell` mit Tabs: Ungelesen, Gespeichert, Feeds, Kategorien, Einstellungen.
- Theming via `Colors.xaml` und `Styles.xaml` mit `AppThemeBinding`.
- Lokalisierung `AppResources.resx` / `AppResources.de.resx`.
- Test-Infrastruktur (`TestDbContextFactory`, xUnit).

## Lücken gegenüber der Anforderung
- `Item`-Model kennt Feed-Titel und Kategorie nicht; Repository liefert Item ohne Quelle.
- Kein Paging in `IItemRepository`.
- `UnreadViewModel` lädt nur statisch, ohne Filter, Sync, Pagination, Aktionen.
- `UnreadPage` zeigt keine Karten mit Quelle/Kategorie/Aktionen, keine Chips, kein Pull-to-Refresh.
- Keine Navigationsziel-Artikeldetailansicht in der App.

## Wichtige Quellen
- `src/Reporter/Views/UnreadPage.xaml` – aktuelles Layout.
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs` – aktueller ViewModel-Zustand.
- `src/Reporter.Data/Repositories/ItemRepository.cs` – Datenbeschaffung.
- `src/Reporter/Views/FeedsPage.xaml` / `.cs` – Muster für Tap-ActionSheet, RefreshView.
- `design-draft/stitch_local_rss_feed_reader/ungelesen_dashboard/screen.png` – Ziel-Design.
