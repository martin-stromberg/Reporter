# Umsetzungsplan: Repository-Schicht und Domänenmodelle

## Zusammenfassung
Aufbau einer vollständigen Repository-Schicht in `Reporter.Core` (Schnittstellen und Domänenmodelle) und `Reporter.Data` (EF Core-Implementierungen). Alle Repositories werden per DI registriert und mit SQLite In-Memory-Tests abgedeckt. Das Feature betrifft ausschließlich die Datenzugriffsschicht; es wird kein neuer UI-Fluss eingeführt.

## Architekturentscheidungen

- **DbContext-Lebensdauer:** Repositories sind `Singleton`. Damit jeder Datenbankzugriff einen kurzlebigen, thread-sicheren `ReporterDbContext` verwendet, wird `IDbContextFactory<ReporterDbContext>` injiziert. In `MauiProgram` wird `AddDbContextFactory<ReporterDbContext>` verwendet.
- **Mapping:** Manuelles Mapping zwischen `Reporter.Data.Entities` und `Reporter.Core.Models` in privaten statischen Hilfsmethoden der Repository-Klassen (kein AutoMapper, keine zusätzliche Abhängigkeit).
- **Domänenmodelle:** `init`-only `required` Eigenschaften, passend zum bestehenden `Article`-Stil.
- **Vorläufigen `Article`-Code ersetzen:** `Article`, `IArticleRepository`, `ArticleRepository`, `IArticleService` und `ArticleService` werden entfernt und durch `Item`/`IItemRepository`/`ItemRepository` ersetzt.

## Schritte

### 1. Domänenmodelle in `Reporter.Core.Models` anlegen
- `Feed`, `Category`, `Item`, `Keyword`, `Settings`, `SyncLog`
- Eigenschaften spiegeln die Entitäten in `Reporter.Data.Entities` wider.
- Style: `public required T Property { get; init; }`, XML-Doku (`<summary>`, `<returns>`, `<param>`) wegen `CS1591`-Fehler.

### 2. Repository-Schnittstellen in `Reporter.Core.Interfaces` anlegen
- `IFeedRepository`, `ICategoryRepository`, `IItemRepository`, `IKeywordRepository`, `ISettingsRepository`, `ISyncLogRepository`
- Gemeinsames CRUD-Muster:
  - `Task<T?> GetByIdAsync(Guid id)`
  - `Task<IReadOnlyList<T>> GetAllAsync()`
  - `Task AddAsync(T model)`
  - `Task UpdateAsync(T model)`
  - `Task DeleteAsync(Guid id)`
- `IItemRepository` zusätzlich:
  - `Task<IReadOnlyList<Item>> GetUnreadByDateAsync()`
  - `Task<IReadOnlyList<Item>> GetByFeedAsync(Guid feedId)`
  - `Task<IReadOnlyList<Item>> GetByCategoryAsync(Guid categoryId)`
  - `Task<IReadOnlyList<Item>> GetSavedForLaterAsync()`
- `ISettingsRepository`:
  - `Task<Settings> GetAsync()`
  - `Task SaveAsync(Settings settings)`

### 3. Repository-Implementierungen in `Reporter.Data.Repositories` anlegen
- Jeweils `IDbContextFactory<ReporterDbContext>` über Konstruktor injizieren.
- Pro Operation `await using var context = await _factory.CreateDbContextAsync();`.
- Mapping zwischen `Reporter.Data.Entities` und `Reporter.Core.Models` in privaten statischen Hilfsmethoden.
- `async`/`await` für alle Datenbankzugriffe.
- `SettingsRepository` stellt sicher, dass immer genau ein Datensatz mit `Settings.DefaultId` existiert:
  - `GetAsync` legt einen Default-Datensatz an, wenn keiner vorhanden ist.
  - `SaveAsync` normalisiert die übergebene `Id` auf `Settings.DefaultId` und überschreibt immer denselben Datensatz.
- Keine Geschäftslogik, nur Datenzugriff und Abbildung.

### 4. DI-Registrierung in `MauiProgram.cs`
- `builder.Services.AddDbContextFactory<ReporterDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));`
- `AddSingleton` für alle sechs Repository-Schnittstellen/Implementierungen.
- Entfernung der vorläufigen `IArticleRepository`/`ArticleRepository`-Registrierung.

### 5. ViewModels anpassen
- `UnreadViewModel` injiziert `IItemRepository`; `IReadOnlyList<Article>` wird zu `IReadOnlyList<Item>`.
- `FeedsViewModel` injiziert `IItemRepository`; `IReadOnlyList<Article>` wird zu `IReadOnlyList<Item>`.
- Die Views binden aktuell nicht auf die Listen, daher nur Kompilierbarkeit sicherstellen.

### 6. Tests in `Reporter.Tests`
- `ReporterDbContext` mit SQLite In-Memory (`DataSource=:memory:`) erzeugen und in eine `IDbContextFactory<ReporterDbContext>`-Test-Implementierung packen.
- `ArticleRepositoryTests.cs` entfernen; stattdessen `ItemRepositoryTests.cs` (CRUD + Filter/Queries) und jeweils eine Testklasse pro verbleibendem Repository.
- `ServiceCollectionTests.cs` baut ein `ServiceCollection`, registriert Factory und Repositories und löst alle sechs Schnittstellen auf.

## Akzeptanzkriterien → Testfälle

| Akzeptanzkriterium | Testfälle |
|--------------------|-----------|
| 1. CRUD-Tests für alle sechs Repositories | `FeedRepositoryTests`, `CategoryRepositoryTests`, `ItemRepositoryTests`, `KeywordRepositoryTests`, `SettingsRepositoryTests`, `SyncLogRepositoryTests` mit je Add/Get/Update/Delete. |
| 2. Filter-/Sortier-Queries korrekt | `ItemRepositoryTests.GetUnreadByDateAsync_ReturnsUnreadSortedByPublishedAtDescending`, `GetByFeedAsync_ReturnsOnlyMatchingFeed`, `GetByCategoryAsync_ReturnsOnlyMatchingCategory`, `GetSavedForLaterAsync_ReturnsOnlySaved`. |
| 3. Settings immer genau ein Datensatz | `SettingsRepositoryTests.GetAsync_CreatesDefaultRecordWhenMissing`, `GetAsync_AlwaysReturnsSingleRecord`, `SaveAsync_OverwritesSameRecord`. |
| 4. Repositories per DI in ViewModels verfügbar | `ServiceCollectionTests` löst `IItemRepository`, `IFeedRepository`, `ICategoryRepository`, `IKeywordRepository`, `ISettingsRepository`, `ISyncLogRepository` aus einem `ServiceProvider`. |
| 5. Keine Geschäftslogik in Repositories | Code-Review: Repositories enthalten nur `IDbContextFactory`-Zugriff, Mapping und `SaveChangesAsync`. |

## Test-Matrix

### Happy-Path-Tests
- `AddAsync_PersistsModel`: Jedes Repository speichert ein neues Modell.
- `GetByIdAsync_ReturnsAddedModel`: Abruf nach Id liefert gespeichertes Modell.
- `GetAllAsync_ReturnsAllModels`: Mehrere Einträge werden zurückgegeben.
- `UpdateAsync_UpdatesModel`: Änderungen werden persistiert.
- `DeleteAsync_RemovesModel`: Löschen entfernt Datensatz.
- `IItemRepository.GetUnreadByDateAsync` liefert nur `IsRead == false`, sortiert nach `PublishedAt` absteigend (NULLs am Ende).
- `IItemRepository.GetByFeedAsync` liefert nur Items des angegebenen Feeds.
- `IItemRepository.GetByCategoryAsync` liefert nur Items der Feeds in der angegebenen Kategorie.
- `IItemRepository.GetSavedForLaterAsync` liefert nur `IsSavedForLater == true`.
- `ISettingsRepository.GetAsync` liefert den Seed-Datensatz, auch wenn die Tabelle leer ist.

### Negative / Edge-Case-Tests
- `GetByIdAsync_NonExisting_ReturnsNull`: Abruf unbekannter Id liefert `null`.
- `DeleteAsync_NonExisting_DoesNotThrow`: Löschen nicht existierender Id ist idempotent.
- `GetByCategoryAsync_NoFeedsInCategory_ReturnsEmpty`: Kategorie ohne Feeds liefert leere Liste.
- `GetByFeedAsync_NonExistingFeed_ReturnsEmpty`: Feed-Id ohne Items liefert leere Liste.
- `GetSavedForLaterAsync_NoSavedItems_ReturnsEmpty`: Keine gespeicherten Items liefert leere Liste.
- `GetUnreadByDateAsync_AllRead_ReturnsEmpty`: Nur gelesene Items liefern leere Liste.
- `SettingsRepository.GetAsync_NeverReturnsNull`: Immer ein Datensatz, auch ohne Seed.
- `SettingsRepository.SaveAsync_IgnoresDifferentIdAndUpdatesDefault`: Übergabe einer fremden Id wird auf `DefaultId` normalisiert.

### Autorisierung / Sichtbarkeit
- Nicht anwendbar; die Datenzugriffsschicht hat keine Benutzerautorisierung.

## UI-/E2E-Tests
- Nicht anwendbar; das Feature führt keinen neuen UI-Fluss ein. Die bestehenden Views binden nur auf `Title`. Die ViewModels werden lediglich kompilierfähig an die neue DI-Registrierung angepasst.

## Offene Punkte
Keine.
