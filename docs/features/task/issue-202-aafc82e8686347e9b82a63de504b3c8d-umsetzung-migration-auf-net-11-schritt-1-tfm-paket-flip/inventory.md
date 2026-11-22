<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme – Schritt 1

Ausführliche Bestandsaufnahme: Projekt-`inventory.md`
(`docs/projects/task/issue-202-…/inventory.md`). Schritt-spezifische
Ergänzungen:

## Verifizierte Stellen (Stand 22.11.2026)

- `src/Reporter/Reporter.csproj`: 6 TFM-Listen Z. 8–13;
  `SupportedOSPlatformVersion`/`TargetPlatformMinVersion` Z. 45–49;
  `UseInterpreter`-Condition Z. 52; `MauiXamlInflator=SourceGen` Z. 30;
  Pakete Z. 76–78 (`$(MauiVersion)`, Logging.Debug 10.0.0, Mvvm 8.4.2).
- `src/Reporter.Core/Reporter.Core.csproj`: TFM Z. 6 `net10.0`;
  Syndication 10.0.11 Z. 15.
- `src/Reporter.Data/Reporter.Data.csproj`: TFM Z. 6 `net10.0`;
  EF.Design + Sqlite 10.0.12 Z. 18–22.
- `src/Reporter.Tests/Reporter.Tests.csproj`: TFM Z. 6 `net10.0`;
  Sqlite 10.0.12, TimeProvider.Testing 10.1.0 + Test-Stack Z. 80–86.
- `src/Reporter.E2ETests/Reporter.E2ETests.csproj`: TFM Z. 6
  `net10.0-windows10.0.19041.0`; Data.Sqlite 10.0.12 Z. 17;
  `FrameworkReference Microsoft.AspNetCore.App` Z. 24.
- `src/Reporter.E2ETests/ReporterAppFixture.cs` ~Z. 143: Konventionspfad
  `bin/Debug/net10.0-windows10.0.19041.0/win-x64/Reporter.exe`.
- `scripts/Run-E2ETests.ps1`: Kommentar Z. 13, `$tfm` Z. 24.
- `scripts/iOS-Deployment.ps1`: `$framework` Z. 94.
- `global.json`: nur `test.runner`; kein `.config/`-Verzeichnis;
  `dotnet ef` global = 10.0.10.
- `Package.appxmanifest` (unter `src/Reporter/Platforms/Windows/`):
  `TargetDeviceFamily` Min `10.0.17763.0` (Z. 22–23).

## Umgebung

- SDK `11.0.100` GA installiert; `dotnet --version` = 11.0.100.
- Workloads im Band `11.0.100-preview.6` installiert (VS-Quelle) — nach
  TFM-Flip `dotnet workload restore` nötig; schreibt ggf. Admin-relevant
  unter `C:\Program Files\dotnet` (zu beobachten).
- NuGet erreichbar; Zielversionen verfügbar: EF/Sqlite/Logging.Debug/
  Syndication = 11.0.0, TimeProvider.Testing = 11.0.1.
- `IncludeIosTarget`/`IncludeAndroidTarget`-Mechanik steuert die TFM-
  Liste; lokaler Windows-Build nutzt `IncludeIosTarget=false`
  (Konvention in `Run-StaticChecks.ps1`/`Run-E2ETests.ps1`).
