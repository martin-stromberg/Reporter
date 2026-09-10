# Bestandsaufnahme

## Vorhandene Bausteine
- `Reporter.Core/Models/Feed.cs` und `Reporter.Data/Entities/Feed.cs`: enthalten bereits `LastCheckedAt`, `HealthStatus`, `HealthLastChange`.
- `Reporter.Core/Models/Item.cs` und `Reporter.Data/Entities/Item.cs`: enthalten `GuidOrHash`, `ContentHtml`, `PublishedAt`, `IsRead`.
- `Reporter.Core/Models/SyncLog.cs` und `Reporter.Data/Entities/SyncLog.cs`: Vorlage für Sync-Log-Einträge.
- Repository-Schnittstellen `IFeedRepository`, `IItemRepository`, `ISyncLogRepository` sowie Implementierungen in `Reporter.Data/Repositories`.
- `ReporterDbContext` mit `OnModelCreating`-Konfiguration für `Feeds`, `Items`, `SyncLogs`.
- `MauiProgram.cs` mit DI-Registrierung aller Repositories und ViewModels.
- `FeedsViewModel`/`FeedsPage.xaml` aus Issue #21: Feed-Liste mit Edit/Delete, noch ohne Refresh.
- `xunit`-Testprojekt `Reporter.Tests` mit `TestDbContextFactory` (In-Memory-SQLite).

## Lücken
- Kein `IFeedSyncService` / `FeedSyncService`.
- Kein RSS/Atom-Parser und keine HTTP-Abfrage-Infrastruktur.
- `IItemRepository` bietet keine Methode, um ein Item anhand `FeedId` + `GuidOrHash` zu suchen.
- `FeedsViewModel` hat keine Refresh-Commands (einzeln / alle).
- `FeedsPage.xaml` enthält keine Refresh-Buttons.
- Keine Lokalisierungstexte für Sync/Refresh.
- Keine Tests für Sync-Logik, Health-Berechnung und ViewModel-Refresh.
