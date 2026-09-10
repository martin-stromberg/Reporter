# Bestandsaufnahme

## Repository

- Projekt: `Reporter` (.NET MAUI, .NET 10).
- Lösung: `Reporter.sln` im Repo-Root.
- Projekte:
  - `src/Reporter/Reporter.csproj` (MAUI-App, TargetFrameworks abhängig vom OS)
  - `src/Reporter.Core/Reporter.Core.csproj`
  - `src/Reporter.Data/Reporter.Data.csproj`
  - `src/Reporter.Tests/Reporter.Tests.csproj` (xUnit, coverlet.collector, EF Core SQLite)
- Tests: xUnit, coverlet.collector 6.0.4.
- Aktuell keine `.github/workflows/` oder `.github/actions/`.

## Technische Details

- TargetFramework: `net10.0` (Core/Data/Tests), MAUI conditional `net10.0-windows10.0.19041.0` / `net10.0-ios`.
- Lokale Tests laufen mit `dotnet test Reporter.sln` / `dotnet test src/Reporter.Tests/Reporter.Tests.csproj`.
- Format-Check: `dotnet format Reporter.sln --verify-no-changes --no-restore --severity error`.
- Security-Scan: `dotnet list Reporter.sln package --vulnerable --include-transitive`.

## Einschränkungen

- Branch-Protection und Labels können nicht durch Dateien im Repo konfiguriert werden; diese werden in der Dokumentation/Commit-Beschreibung vermerkt.
- MAUI-iOS/Android-Runner sind nicht Teil dieses Issues; Build lokal auf `windows-latest`.
- Pre-Release/Promotion-Workflows sind Teil eines späteren Arbeitspakets.
