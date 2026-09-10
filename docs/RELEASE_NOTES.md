# Release Notes

## Unreleased

### Repository-Schicht und Domänenmodelle

- Domänenmodelle `Feed`, `Category`, `Item`, `Keyword`, `Settings` und `SyncLog` in `Reporter.Core.Models` hinzugefügt.
- Repository-Schnittstellen `IFeedRepository`, `ICategoryRepository`, `IItemRepository`, `IKeywordRepository`, `ISettingsRepository` und `ISyncLogRepository` in `Reporter.Core.Interfaces` definiert.
- Repository-Implementierungen in `Reporter.Data.Repositories` mit `async`/`await` und Entity Framework Core umgesetzt.
- Repositories verwenden `IDbContextFactory<ReporterDbContext>` für kurzlebige, thread-sichere DbContext-Instanzen.
- `IItemRepository` bietet Filter-/Sortier-Queries für ungelesene Artikel, Artikel pro Feed/Kategorie und gespeicherte Artikel.
- `ISettingsRepository` stellt immer genau einen Datensatz bereit (Singleton-Muster).
- Alle Repositories sind in `MauiProgram` per Dependency Injection registriert und in den ViewModels verfügbar.
- Unit-Tests für alle Repositories in `Reporter.Tests` hinzugefügt.
