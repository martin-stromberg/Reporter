# Reporter

Lokaler RSS-/Feed-Reader als .NET MAUI-App.

## Projektstruktur

| Projekt | Verantwortlichkeit |
| --- | --- |
| `Reporter` | .NET MAUI-App, UI, ViewModels, Navigation |
| `Reporter.Core` | Domänenmodelle, Schnittstellen, Anwendungs-Services |
| `Reporter.Data` | Datenbankzugriff und Repositories |
| `Reporter.Tests` | Unit- und Integrationstests |

## Bauen

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

- `Microsoft.Extensions.DependencyInjection` für alle Services und ViewModels
- `CommunityToolkit.Mvvm` als Basis für ViewModels
- Shell-Navigation mit vier Tabs: Ungelesen, Feeds, Später, Einstellungen
- Resx-Ressourcen für Deutsch und Englisch unter `src/Reporter/Resources/Strings`
- Standard .NET MAUI Light/Dark-Styles
