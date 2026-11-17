<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Direkte Paketreferenzen je Projekt

Ermittelt aus den `.csproj`-Dateien unter `src/` (Stand Branch `task/issue-179-...-nuget-paketpruefung-kw-2026-w4`). Kein Central Package Management (`Directory.Packages.props` nicht vorhanden), keine `packages.lock.json`, kein `NuGet.config` im Repo. Alle Versionen stehen inline.

## `$(MauiVersion)`-Auflösung

- `Microsoft.Maui.Controls` wird mit `Version="$(MauiVersion)"` referenziert; der Wert kommt aus dem installierten MAUI-Workload, nicht aus der `.csproj`.
- Installierte Umgebung (`dotnet --list-sdks` / `dotnet workload list`):
  - .NET SDK: **10.0.403** (`C:\Program Files\dotnet\sdk`)
  - Workload-Version: **10.0.401**
  - `android` 36.1.69/10.0.100, `ios` 26.5.10318/10.0.100, `maccatalyst` 26.5.10318/10.0.100, `maui-windows` **10.0.20**/10.0.100
- Workload-Manifest `C:\Program Files\dotnet\sdk-manifests\10.0.100\microsoft.net.sdk.maui\10.0.20\WorkloadManifest.json` deklariert Pack `Microsoft.Maui.Controls` **10.0.20** → `$(MauiVersion)` = **10.0.20** (deckt sich mit der Auflösung im Outdated-Scan: „Angefordert/Aufgelöst 10.0.20").
- Zusätzlich liegt Manifest `10.0.110` im Manifest-Verzeichnis (nicht aktiv installiert laut `dotnet workload list`).

## `src/Reporter/Reporter.csproj` (MAUI-App)

TFMs: `net10.0-windows10.0.19041.0` immer; `net10.0-ios` über `IncludeIosTarget` (Default `true`), `net10.0-android` über `IncludeAndroidTarget` (Default `false`). `UseMaui=true`, `MauiXamlInflator=SourceGen`.

| Paket | Version | Bemerkung |
|-------|---------|-----------|
| `Microsoft.Maui.Controls` | `$(MauiVersion)` = 10.0.20 | aus `maui-windows`-Workload |
| `Microsoft.Extensions.Logging.Debug` | 10.0.0 | |
| `CommunityToolkit.Mvvm` | 8.4.2 | |

## `src/Reporter.Core/Reporter.Core.csproj` (`net10.0`)

| Paket | Version | Bemerkung |
|-------|---------|-----------|
| `CommunityToolkit.Mvvm` | 8.4.2 | |
| `System.ServiceModel.Syndication` | 10.0.11 | RSS/Syndication |

## `src/Reporter.Data/Reporter.Data.csproj` (`net10.0`)

| Paket | Version | Bemerkung |
|-------|---------|-----------|
| `Microsoft.EntityFrameworkCore.Design` | 10.0.12 | `PrivateAssets=all`, `IncludeAssets` auf Build/Analyzer beschränkt |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.12 | |

## `src/Reporter.Tests/Reporter.Tests.csproj` (`net10.0`, `Microsoft.Testing.Platform` via `global.json`)

| Paket | Version | Bemerkung |
|-------|---------|-----------|
| `coverlet.MTP` | 10.1.1 | Coverage für MTP |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.12 | |
| `Microsoft.Extensions.TimeProvider.Testing` | 10.1.0 | |
| `Microsoft.NET.Test.Sdk` | 18.12.0 | |
| `Microsoft.Testing.Extensions.TrxReport` | 2.5.3 | |
| `xunit.v3` | 4.0.3 | |
| `xunit.runner.visualstudio` | 4.0.1 | |

## `src/Reporter.E2ETests/Reporter.E2ETests.csproj` (`net10.0-windows10.0.19041.0`)

| Paket | Version | Bemerkung |
|-------|---------|-----------|
| `FlaUI.UIA3` | 5.0.0 | UI-Automatisierung |
| `Microsoft.Data.Sqlite` | 10.0.12 | |
| `Microsoft.NET.Test.Sdk` | 18.12.0 | |
| `xunit.v3` | 4.0.3 | |
| `xunit.runner.visualstudio` | 4.0.1 | |

Zusätzlich `FrameworkReference Microsoft.AspNetCore.App` (kein NuGet-PackageReference).

## Sonstige Fakten

- `Directory.Build.props` (Repo-Root) enthält nur Lizenz-Metadaten und `DebugReportRecipient` — **keine** Paket-/Versionsverwaltung.
- `global.json` pinnnt **keine** SDK-Version; einzig `"test": { "runner": "Microsoft.Testing.Platform" }`.
- Es existiert ein `package-lock.json`/`package.json` im Root (npm-Toolchain für Skripte/Release-Config, nicht für die .NET-App); `npm audit` ist Teil der Static Checks.
- Projekt-Referenzgraph: `Reporter` → `Reporter.Core` + `Reporter.Data`; `Reporter.Data` → `Reporter.Core`; `Reporter.Tests` → `Reporter.Core` + `Reporter.Data`; `Reporter.E2ETests` → `Reporter.Core`.
