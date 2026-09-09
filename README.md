# Reporter

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

Lokaler RSS-/Feed-Reader als .NET MAUI-App.

## Features

- .NET MAUI-App mit Shell-Navigation
- Untere Navigationsleiste mit **Ungelesen**, **Feeds**, **Später** und **Einstellungen**
- Light/Dark-Theme-Unterstützung über .NET MAUI `AppThemeBinding`
- Design-System mit den Schriftarten **Newsreader** und **Inter** sowie Farb- und Typografie-Tokens aus dem `design-draft`
- Dependency Injection mit `Microsoft.Extensions.DependencyInjection`
- ViewModel-Basen mit `CommunityToolkit.Mvvm`
- Mehrsprachigkeits-Rüstung über RESX-Dateien (Deutsch/Englisch), Tab-Titel und Platzhaltertexte sind bereits an `AppResources` gebunden

## Projektstruktur

| Projekt | Verantwortlichkeit |
| --- | --- |
| `Reporter` | .NET MAUI-App, UI, ViewModels, Navigation |
| `Reporter.Core` | Domänenmodelle, Schnittstellen, Anwendungs-Services |
| `Reporter.Data` | Datenbankzugriff und Repositories |
| `Reporter.Tests` | Unit- und Integrationstests |

## Voraussetzungen

- .NET 10 SDK
- Windows: Windows 10 Build 19041 oder höher
- iOS/macOS: Xcode (auf macOS)

## Installation / Setup

```bash
dotnet build Reporter.sln
```

Auf Windows wird automatisch das `net10.0-windows10.0.19041.0`-Ziel gebaut. Für iOS ist die entsprechende Xcode-Umgebung auf macOS erforderlich.

## Git Hooks

Die Repository enthält Git-Hooks im Ordner `.githooks` (aus dem [Pattern-Collection](https://github.com/martin-stromberg/Pattern-Collection/tree/main/Git-Hooks)-Repo).

Aktivieren:

```bash
git config --local core.hooksPath .githooks
```

Oder unter Windows `install-hooks.cmd` / unter macOS/Linux `install-hooks.sh` ausführen.

Die Hooks prüfen unter anderem:
- Konsistenz der RESX-Lokalisierung (`translation-check.py`)
- Platzhalter-Implementierungen (`no-notimplemented-check.py`)
- Razor-Lokalisierung und -Verwendung (`razor-l10n-check.py`, `razor-usage-check.py`)
- Enum-Testabdeckung (`enum-coverage-check.py`)
- XML-Dokumentation in `.cs`/`.csproj` (`csproj-xmldoc-check.py`)

Hinweis: `pre-push` blockiert direkte Pushes auf `main` und `staging`.

Die Projekte sind bereits so konfiguriert, dass `GenerateDocumentationFile` aktiviert und `CS1591` als Fehler behandelt wird. Neue öffentliche APIs müssen also mit XML-Dokumentation (`<summary>`, `<param>`, `<returns>`) versehen werden.

## Starten

Auf Windows:

```bash
dotnet run --project src/Reporter/Reporter.csproj -f net10.0-windows10.0.19041.0
```

Auf macOS/iOS:

```bash
dotnet build src/Reporter/Reporter.csproj -f net10.0-ios
dotnet run --project src/Reporter/Reporter.csproj -f net10.0-ios
```

## Architektur

- `MauiProgram.CreateMauiApp()` konfiguriert DI, Fonts und MAUI.
- `AppShell` definiert die Tabs **Ungelesen**, **Feeds**, **Später** und **Einstellungen**.
- `Colors.xaml` und `Styles.xaml` implementieren das Design-System (Light/Dark, Newsreader/Inter, Farbtokens).
- `Reporter.Core` enthält die Domänenmodelle (`Feed`, `Category`, `Item`, `Keyword`, `Settings`, `SyncLog`) und die Repository-Schnittstellen (`IFeedRepository`, `ICategoryRepository`, `IItemRepository`, `IKeywordRepository`, `ISettingsRepository`, `ISyncLogRepository`).
- `Reporter.Data` stellt die EF Core-Entitäten und Repository-Implementierungen bereit; Repositories verwenden `IDbContextFactory<ReporterDbContext>` für kurzlebige, thread-sichere DbContext-Instanzen.
- `BaseViewModel` dient als Basis für alle ViewModels.

## Tests

```bash
dotnet test Reporter.sln
```

## Changelog

Siehe [changes.log](changes.log).

## Dokumentation

- [Hilfe / Anwenderdokumentation](docs/help/index.md)
- [Code-Review des aktuellen Branches](docs/features/task/issue-16-7860f6e24bde49d788fdd9a6ed58f33d-net-maui-projektscaffolding-un/review-code.md)
