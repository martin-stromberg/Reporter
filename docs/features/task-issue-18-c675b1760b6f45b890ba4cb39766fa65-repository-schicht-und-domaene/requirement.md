# Übersetzte Anforderung

## Ziel
Bereitstellung einer testbaren, sauberen Datenzugriffsschicht über Repositories, die alle CRUD- und Filteroperationen für die App kapselt und in den ViewModels per DI verfügbar ist.

## Scope
- Anlegen von Domänenmodellen in `Reporter.Core.Models` für:
  - `Feed`, `Category`, `Item`, `Keyword`, `Settings`, `SyncLog`
- Definition von Repository-Schnittstellen in `Reporter.Core.Interfaces`:
  - `IFeedRepository`, `ICategoryRepository`, `IItemRepository`, `IKeywordRepository`, `ISettingsRepository`, `ISyncLogRepository`
- Implementierung der Repositories in `Reporter.Data.Repositories` mit `async`/`await` und Entity Framework Core.
- Bereitstellung unterstützender Queries in `IItemRepository`:
  - Ungelesene Artikel nach Datum sortiert
  - Artikel pro Feed
  - Artikel pro Kategorie
  - Gespeicherte Artikel
- CRUD-Operationen für `Feed`, `Category`, `Keyword`, `SyncLog`.
- `ISettingsRepository` stellt immer genau einen Datensatz bereit (Singleton-Muster).
- Registrierung aller Repositories in der Dependency Injection in `MauiProgram`.
- Unit-Tests für alle Repositories mit einer Test-SQLite-Datenbank (In-Memory).

## Akzeptanzkriterien
1. Alle sechs Repositories besitzen vollständige CRUD-Tests.
2. Filter-/Sortier-Queries liefern korrekte Ergebnisse.
3. `SettingsRepository` stellt immer genau einen Datensatz bereit.
4. Repositories sind über DI in den ViewModels verfügbar.
5. Keine Geschäftslogik in den Repositories; reiner Datenzugriff.

## Lieferzustand
Datenzugriffsschicht ist funktionsfähig und durch Tests abgedeckt; bereit für UI-Integration.

## Offene Punkte
Keine.
