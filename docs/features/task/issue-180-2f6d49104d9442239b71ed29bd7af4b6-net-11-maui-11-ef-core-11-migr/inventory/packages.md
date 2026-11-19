<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Inventar: NuGet-Pakete

Kein Central Package Management – alle Versionen direkt in den .csproj. Gruppierung nach
den Update-Wellen aus Issue #180 / requirement.md.

## Welle 1: MAUI (erfordert net11.0-TFMs + 11er-Workloads)

| Paket | IST | Datei:Zeile | SOLL |
|---|---|---|---|
| `Microsoft.Maui.Controls` | `$(MauiVersion)` (workload-gesteuert, aktuell ≈ 10.0.20) | `src/Reporter/Reporter.csproj:76` | 11.x via Workload |

Hinweis: `Microsoft.Maui.Essentials` ist nicht separat referenziert (Teil von `UseMaui`/`SingleProject`).
`$(MauiVersion)` wird vom installierten MAUI-Workload bereitgestellt – siehe [workloads.md](workloads.md).
Dependabot kann diese Zeile nicht anheben (dokumentiert in
`docs/help/release-management/wiederkehrende-reviews.md:38`).

## Welle 2: EF Core / SQLite (EF Core 11 erfordert zwingend .NET 11)

| Paket | IST | Datei:Zeile | SOLL |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore.Design` | 10.0.12 (PrivateAssets=all) | `src/Reporter.Data/Reporter.Data.csproj:18-21` | 11.x |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.12 | `src/Reporter.Data/Reporter.Data.csproj:22` | 11.x |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.12 | `src/Reporter.Tests/Reporter.Tests.csproj:17` | 11.x |
| `Microsoft.Data.Sqlite` | 10.0.12 | `src/Reporter.E2ETests/Reporter.E2ETests.csproj:17` | 11.x |

Bekannter offener Dependabot-PR: `Microsoft.Data.Sqlite 11.0.0` (blockiert bis Runtime-Migration).

## Welle 3: Microsoft.*-Runtime-Pakete (nur konsistent mit .NET 11 anheben)

| Paket | IST | Datei:Zeile | SOLL |
|---|---|---|---|
| `Microsoft.Extensions.Logging.Debug` | 10.0.0 | `src/Reporter/Reporter.csproj:77` | 11.x |
| `System.ServiceModel.Syndication` | 10.0.11 | `src/Reporter.Core/Reporter.Core.csproj:15` | 11.x |
| `Microsoft.Extensions.TimeProvider.Testing` | 10.1.0 | `src/Reporter.Tests/Reporter.Tests.csproj:18` | 11.x |

Bekannter offener Dependabot-PR: `System.ServiceModel.Syndication 11.0.0`.

Weitere `Microsoft.Extensions.*`-Nutzung kommt transitiv/Framework-implizit
(`Microsoft.Extensions.DependencyInjection`, `Logging` in `src/Reporter/MauiProgram.cs:4-6`) –
keine eigenen PackageReference nötig.

## Welle 4: Test-Stack (unabhängig von .NET, aber mitprüfen)

| Paket | IST | Datei:Zeile |
|---|---|---|
| `coverlet.MTP` | 10.1.1 | `src/Reporter.Tests/Reporter.Tests.csproj:16` |
| `Microsoft.NET.Test.Sdk` | 18.12.0 | `src/Reporter.Tests/Reporter.Tests.csproj:19`, `src/Reporter.E2ETests/Reporter.E2ETests.csproj:18` |
| `Microsoft.Testing.Extensions.TrxReport` | 2.5.3 | `src/Reporter.Tests/Reporter.Tests.csproj:20` |
| `xunit.v3` | 4.0.3 | `src/Reporter.Tests/Reporter.Tests.csproj:21`, `src/Reporter.E2ETests/Reporter.E2ETests.csproj:19` |
| `xunit.runner.visualstudio` | 4.0.1 | `src/Reporter.Tests/Reporter.Tests.csproj:22`, `src/Reporter.E2ETests/Reporter.E2ETests.csproj:20` |
| `FlaUI.UIA3` | 5.0.0 | `src/Reporter.E2ETests/Reporter.E2ETests.csproj:16` |

Tests laufen über Microsoft.Testing.Platform (global.json `test.runner`); CI ruft
`dotnet test` mit `--`-MTP-Argumenten (`coverlet`, `report-trx`) auf – siehe
`.github/workflows/staging-ci.yml:121-126`.

## FrameworkReference

- `Microsoft.AspNetCore.App` in `src/Reporter.E2ETests/Reporter.E2ETests.csproj:24`
  (Kestrel-Stub für Feed-Suche). Folgt der Ziel-Runtime; CI-Runner brauchen das
  .NET-11-ASP.NET-Shared-Framework (setup-dotnet `11.0.x` liefert es).

## Versionsunabhängige Pakete (keine Aktion nötig, nur Vollständigkeit)

| Paket | IST | Datei:Zeile |
|---|---|---|
| `CommunityToolkit.Mvvm` | 8.4.2 | `src/Reporter/Reporter.csproj:78`, `src/Reporter.Core/Reporter.Core.csproj:14` |

## Global Tools (CI)

- `dotnet-reportgenerator-globaltool --version 5.5.11` in
  `.github/workflows/staging-ci.yml:129` und `.github/workflows/pr-staging-ci.yml:132` –
  Kompatibilität mit .NET-11-SDK bei Migration prüfen.
