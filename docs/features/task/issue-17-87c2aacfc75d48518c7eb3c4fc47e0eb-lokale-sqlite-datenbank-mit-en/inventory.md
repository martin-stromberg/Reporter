# Bestandsaufnahme

## Projektstruktur
- `src/Reporter.Core` – Domain-Modelle (`Article`), Interfaces (`IArticleRepository`) und Services (`ArticleService`).
- `src/Reporter.Data` – Repository-Implementierung (`ArticleRepository`), aktuell nur Stub/In-Memory.
- `src/Reporter` – .NET MAUI-App (`MauiProgram`, `App.xaml.cs`).
- `src/Reporter.Tests` – xUnit-Testprojekt.

## Relevante Dateien
- `src/Reporter.Core/Models/Article.cs`
- `src/Reporter.Core/Interfaces/IArticleRepository.cs`
- `src/Reporter.Core/Services/ArticleService.cs`
- `src/Reporter.Data/Repositories/ArticleRepository.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter/App.xaml.cs`
- `src/Reporter.Data/Reporter.Data.csproj`
- `src/Reporter/Reporter.csproj`
- `src/Reporter.Tests/Reporter.Tests.csproj`

## Feststellungen
- Keine Datenbank vorhanden.
- `Reporter.Data` referenziert bisher nur `Reporter.Core`.
- `ArticleRepository` ist ein Stub ohne Persistenz.
- `MauiProgram` registriert Dienste manuell; bisher kein `DbContext`.
- Projekt verwendet .NET 10 (`net10.0`) und `ImplicitUsings`/`Nullable`.
- `GenerateDocumentationFile` ist aktiviert, XML-Dokumentation erforderlich.
- Testprojekt nutzt xUnit und referenziert `Reporter.Core` und `Reporter.Data`.
- `dotnet-ef` 10.0.10 und .NET SDK 10.0.400 sind verfügbar.
