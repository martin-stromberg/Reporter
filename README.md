# Reporter

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)

Lokaler RSS-/Feed-Reader als .NET MAUI-App.

## Features

- .NET MAUI-App mit Shell-Navigation
- Untere Navigationsleiste mit **Ungelesen** (Dashboard mit Kategoriefilter, Pull-to-Refresh und Infinity-Scroll), **Feeds**, **Später**, **Kategorien** und **Einstellungen**
- Light/Dark-Theme-Unterstützung über .NET MAUI `AppThemeBinding`
- Design-System mit den Schriftarten **Newsreader** und **Inter** sowie Farb- und Typografie-Tokens aus dem `design-draft`
- Dependency Injection mit `Microsoft.Extensions.DependencyInjection`
- ViewModel-Basen mit `CommunityToolkit.Mvvm`
- Mehrsprachigkeits-Rüstung über RESX-Dateien (Deutsch/Englisch), Tab-Titel und Platzhaltertexte sind bereits an `AppResources` gebunden
- RSS-/Atom-Feed-Abruf, Parsing und Speicherung neuer Artikel inklusive Feed-Health (`OK`/`Warning`/`Error`) und Sync-Log
- Artikeldetailansicht mit WebView-Volltextdarstellung, automatischem Gelesen-Markieren, `Für später bewahren`-Toggle, Teilen und Öffnen im Browser (Issue #24)

## Projektstruktur

| Projekt | Verantwortlichkeit |
| --- | --- |
| `Reporter` | .NET MAUI-App, UI, Navigation |
| `Reporter.Core` | Domänenmodelle, Schnittstellen, ViewModels, mehrsprachige RESX-Ressourcen und Anwendungs-Services |
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

`Reporter` ist auf `net10.0-windows10.0.19041.0` und `net10.0-ios` ausgerichtet; `dotnet build Reporter.sln` baut beide Ziele. Für iOS-Geräte-Deployment/Signing ist Xcode auf macOS erforderlich.

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

Fuer gezieltes Build/Deployment (IPA, Simulator, physisches Geraet) steht `scripts/iOS-Deployment.ps1` bereit:

**Wichtig:** `simulator` und `device` setzen voraus, dass das Skript direkt auf einem Mac ausgefuehrt wird.
Microsoft unterstuetzt `dotnet build -t:Run` fuer iOS/tvOS aktuell **nicht von Windows aus**.
Auf Windows koennen mit `build` die iOS-Kompilate (bzw. mit Codesigning `.ipa`) erzeugt werden;
das Deployment muss dann ueber Visual Studio oder manuell auf dem Mac erfolgen.

Auf dem Mac startet `simulator` die App im iOS-Simulator, wartet kurz und speichert
einen Screenshot unter `src/Reporter/bin/<config>/net10.0-ios/<rid>/`.

```powershell
# Menue starten
.\scripts\iOS-Deployment.ps1

# Beispiele
.\scripts\iOS-Deployment.ps1 -Action build -CodesignKey "Apple Distribution: ..." -CodesignProvision "ReporterProfile" -ServerAddress 192.168.1.10 -ServerUser me
.\scripts\iOS-Deployment.ps1 -Action simulator -Device "E25BBE37-69BA-4720-B6FD-D54C97791E79"
.\scripts\iOS-Deployment.ps1 -Action device -Device "DEINE-UDID" -CodesignKey "..." -CodesignProvision "..."
```

## Architektur

- `MauiProgram.CreateMauiApp()` konfiguriert DI, Fonts und MAUI.
- `AppShell` definiert die Tabs **Ungelesen**, **Feeds**, **Später**, **Kategorien** und **Einstellungen**.
- `Colors.xaml` und `Styles.xaml` implementieren das Design-System (Light/Dark, Newsreader/Inter, Farbtokens).
- `Reporter.Core` enthält die Domänenmodelle (`Feed`, `Category`, `Item`, `Keyword`, `Settings`, `SyncLog`), Repository-Schnittstellen (`IFeedRepository`, `ICategoryRepository`, `IItemRepository`, `IKeywordRepository`, `ISettingsRepository`, `ISyncLogRepository`) und den Anwendungs-Service `IFeedSyncService` / `FeedSyncService`.
- `Reporter.Data` stellt die EF Core-Entitäten und Repository-Implementierungen bereit; Repositories verwenden `IDbContextFactory<ReporterDbContext>` für kurzlebige, thread-sichere DbContext-Instanzen.
- `Reporter` (MAUI-Projekt) enthält die Seiten (`FeedsPage` usw.) und das App-Shell-Setup.
- `Reporter.Core` enthält `BaseViewModel`, die ViewModels (`FeedsViewModel`, `CategoriesViewModel`, `UnreadViewModel`, `LaterViewModel`, `SettingsViewModel`) und `AppResources`.
- `FeedsViewModel` nutzt `IFeedSyncService` für manuelles Refresh einzelner oder aller Feeds.

## Tests

```bash
dotnet test Reporter.sln
```

- `Reporter.Tests` referenziert `Reporter.Core` und `Reporter.Data`, sodass ViewModels (z. B. `FeedsViewModel`) und Services direkt getestet werden können.
- `FeedsViewModelTests` deckt die UI-nahen Refresh-Commands ab: Refresh für einen Feed, Refresh aller Feeds und Fehleranzeige.

## CI/CD

Das Repository verwendet GitHub Actions für Qualitätsgates:

- `.github/workflows/pr-staging-ci.yml` — führt bei PRs nach `staging` parallel `static checks` (Format, Security-Scan, statische Analyse) und `build & test` (inkl. Coverage-Threshold 70 %) aus.
- `.github/workflows/verify-pr-source.yml` — erlaubt PRs nach `main` nur aus `staging`.
- `.github/workflows/security-scan.yml` — wöchentlicher Sicherheits-Scan der Abhängigkeiten.
- `.github/actions/security-scan/action.yml` — wiederverwendbare Composite Action für den Vulnerability-Scan.

**Manuelle Schritte nach dem Merge in `staging`:**
- Branch-Protection für `staging` aktivieren und die Status Checks `static checks` und `build & test` als erforderlich markieren.
- Labels `automated-promotion` (`0E8A16`) und `automated-backmerge` (`1D76DB`) anlegen.

## Changelog

Siehe [changes.log](changes.log).

## Dokumentation

- [Hilfe / Anwenderdokumentation](docs/help/index.md)
- [Code-Review des aktuellen Branches](docs/features/task/issue-16-7860f6e24bde49d788fdd9a6ed58f33d-net-maui-projektscaffolding-un/review-code.md)



CI validation test
