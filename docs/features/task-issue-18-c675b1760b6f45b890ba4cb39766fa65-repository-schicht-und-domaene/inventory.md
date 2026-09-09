# Bestandsaufnahme – Repository-Schicht und Domänenmodelle

## Projektübersicht
.NET 10 / .NET MAUI-Anwendung mit folgender Schichtung:

| Projekt | Verantwortlichkeit |
|---------|--------------------|
| `Reporter` | MAUI-App, UI, ViewModels, Navigation, DI-Registrierung |
| `Reporter.Core` | Domänenmodelle, Schnittstellen, Anwendungs-Services |
| `Reporter.Data` | EF Core SQLite-Datenbankzugriff, Entitäten, Migrations |
| `Reporter.Tests` | xUnit-Tests |

## Vorhandene Artefakte

### Datenbank- und Entitätslayer (`Reporter.Data`)
- `ReporterDbContext.cs` – konfiguriert EF Core SQLite mit DbSets für `Feed`, `Category`, `Item`, `Keyword`, `Settings`, `SyncLog`.
- `ReporterDbContextFactory.cs` – Design-Time-Factory.
- `Entities/Feed.cs`, `Category.cs`, `Item.cs`, `Keyword.cs`, `Settings.cs`, `SyncLog.cs` – vollständige Entitäten mit XML-Doku.
- `Migrations/20260909214617_InitialCreate.cs` – erstellt alle Tabellen, Indizes und Seed-Datensatz für `Settings`.

### Core (`Reporter.Core`)
- `Models/Article.cs` – vorläufiges, minimales Domänenmodell (`Id`, `Title`, `IsRead`).
- `Interfaces/IArticleRepository.cs` – vorläufiges Repository-Interface für `Article`.
- `Services/IArticleService.cs`, `ArticleService.cs` – vorläufiger Service, delegiert an `IArticleRepository`.

### Data (`Reporter.Data`)
- `Repositories/ArticleRepository.cs` – In-Memory-Stub, liefert leere Listen.

### App (`Reporter`)
- `MauiProgram.cs` – registriert `DbContext`, `IArticleRepository`/`ArticleRepository`, `IArticleService`/`ArticleService`, ViewModels und Pages.
- `ViewModels/UnreadViewModel.cs`, `FeedsViewModel.cs` – nutzen `IArticleService` und `IReadOnlyList<Article>`.
- `Views/*.xaml` – binden aktuell nur auf `Title`, nicht auf Artikellisten.

### Tests (`Reporter.Tests`)
- `ArticleRepositoryTests.cs` – Stub-Tests für `ArticleRepository`.
- `ReporterDbContextTests_Schema.cs` – prüft Tabellen und `Settings`-Seed.
- `ReporterDbContextTests_Persistence.cs` – prüft Persistenz von Feed/Kategorie/Item inkl. Includes.

## Abhängigkeiten
- `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12 in `Reporter.Data` und `Reporter.Tests`.
- `Microsoft.EntityFrameworkCore.Design` 10.0.12 in `Reporter.Data`.
- `xunit` 2.9.3, `Microsoft.NET.Test.Sdk` 17.14.1, `coverlet.collector` 6.0.4 in `Reporter.Tests`.

## Offene Lücken
- Keine vollständigen Domänenmodelle für `Feed`, `Category`, `Item`, `Keyword`, `Settings`, `SyncLog` in `Reporter.Core`.
- Keine Repository-Schnittstellen/-Implementierungen für die sechs Entitäten.
- `ArticleRepository` ist ein In-Memory-Stub ohne EF-Core-Anbindung.
- ViewModels greifen noch nicht auf die neue Repository-Schicht zu.
- Keine CRUD- und Filter-Tests für die sechs Repositories.

## Inventory-Detaildokumente
- Keine weiteren Detaildokumente erforderlich; alle relevanten Dateien sind oben verlinkt.
