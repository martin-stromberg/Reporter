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
- Mehrsprachigkeits-Rüstung über RESX-Dateien (Deutsch/Englisch)

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
- `Reporter.Core` enthält das Domänenmodell (`Article`), `IArticleRepository` und `IArticleService`.
- `Reporter.Data` stellt `ArticleRepository` bereit.
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
