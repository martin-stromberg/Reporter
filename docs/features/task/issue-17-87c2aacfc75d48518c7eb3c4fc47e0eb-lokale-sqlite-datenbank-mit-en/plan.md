# Umsetzungsplan: Lokale SQLite-Datenbank mit Entity Framework Core

## Zusammenfassung
Einrichtung einer SQLite-Datenbank in `Reporter.Data` mit EF Core, Entitäten für Feeds, Kategorien, Artikel, Keywords, Einstellungen und Sync-Log, Initial-Migration, App-Start-Initialisierung und Testunterstützung.

## Schritte

1. **Abhängigkeiten**
   - `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12 in `Reporter.Data` hinzufügen.
   - `Microsoft.EntityFrameworkCore.Design` 10.0.12 (PrivateAssets="all") in `Reporter.Data` hinzufügen.

2. **Entitäten in `Reporter.Data/Entities` anlegen**
   - `Feed`, `Category`, `Item`, `Keyword`, `Settings`, `SyncLog`
   - Typen: `Guid`-IDs, `string`/`DateTime?`/`bool`/`TimeSpan?` je nach Feld.

3. **`ReporterDbContext` in `Reporter.Data` anlegen**
   - `DbSet`-Properties für alle Entitäten.
   - `OnModelCreating` mit Beziehungen, Indexen und `Settings`-Singleton-Seed.

4. **Migration erzeugen**
   - `dotnet ef migrations add InitialCreate --project src/Reporter.Data --startup-project src/Reporter`

5. **App-Start-Initialisierung**
   - `ReporterDbContext` in `MauiProgram` registrieren (SQLite-Pfad im AppDataDirectory).
   - `Migrate()` in `MauiProgram.CreateMauiApp` aufrufen.

6. **Tests**
   - `ReporterDbContext` in `Reporter.Tests` mit `UseSqlite("DataSource=:memory:")` instanziieren.
   - Schema via `EnsureCreated()` erstellen und Grundfunktion prüfen.

7. **Dokumentation**
   - `docs/help/anwendung/datenmodell.md` aktualisieren.

## Offene Punkte
Keine.
