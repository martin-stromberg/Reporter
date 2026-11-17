<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Outdated-Scan

Befehl: `dotnet list Reporter.sln package --outdated --include-transitive` (ausgeführt im Repo-Root, Exit-Code 0; alle 5 Projekte erfolgreich restauriert). Quellen: `https://api.nuget.org/v3/index.json`, `C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\`. Datum der Ausführung: 2026-11-17 (lokale Umgebung, SDK 10.0.403).

## Veraltete direkte (Top-Level-)Pakete

| Projekt | TFM | Paket | Angefordert | Aufgelöst | Neueste | Sprung |
|---------|-----|-------|-------------|-----------|---------|--------|
| Reporter | net10.0-ios + net10.0-windows | `Microsoft.Extensions.Logging.Debug` | 10.0.0 | 10.0.0 | 11.0.0 | Major |
| Reporter | net10.0-ios + net10.0-windows | `Microsoft.Maui.Controls` | 10.0.20 (`$(MauiVersion)`) | 10.0.20 | 11.0.10 | Major |
| Reporter.Core | net10.0 | `System.ServiceModel.Syndication` | 10.0.11 | 10.0.11 | 11.0.0 | Major |
| Reporter.Data | net10.0 | `Microsoft.EntityFrameworkCore.Design` | 10.0.12 | 10.0.12 | 11.0.0 | Major |
| Reporter.Data | net10.0 | `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.12 | 10.0.12 | 11.0.0 | Major |
| Reporter.Tests | net10.0 | `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.12 | 10.0.12 | 11.0.0 | Major |
| Reporter.Tests | net10.0 | `Microsoft.Extensions.TimeProvider.Testing` | 10.1.0 | 10.1.0 | 11.0.0 | Major |
| Reporter.E2ETests | net10.0-windows | `Microsoft.Data.Sqlite` | 10.0.12 | 10.0.12 | 11.0.0 | Major |

**Auffällig:** Sämtliche verfügbaren Updates direkter Pakete sind Major-Sprünge auf die **11.x-Linie** (nächste .NET-Hauptversion). Es existiert keine neuere 10.x-Version für die direkt referenzierten Pakete — d. h. innerhalb der aktuellen Major-Linie ist alles aktuell.

## Direkte Pakete ohne gemeldetes Update (auf neuester stabiler Version)

`CommunityToolkit.Mvvm` 8.4.2, `coverlet.MTP` 10.1.1, `Microsoft.NET.Test.Sdk` 18.12.0, `Microsoft.Testing.Extensions.TrxReport` 2.5.3, `xunit.v3` 4.0.3, `xunit.runner.visualstudio` 4.0.1, `FlaUI.UIA3` 5.0.0.

## Veraltete transitive Pakete (Auswahl, vollständige Rohausgabe unten)

| Paket | Aufgelöst | Neueste | Sichtbar in |
|-------|-----------|---------|-------------|
| `Microsoft.Testing.Platform` | 2.5.2 | 2.5.3 | E2ETests |
| `Microsoft.Testing.Platform.MSBuild` | 2.5.2 | 2.5.3 | Tests, E2ETests |
| `Microsoft.Testing.Extensions.Telemetry` | 2.5.2 | 2.5.3 | Tests, E2ETests |
| `Microsoft.Testing.Extensions.TrxReport.Abstractions` | 2.5.2 | 2.5.3 | E2ETests |
| `Microsoft.ApplicationInsights` | 2.23.0 | 3.2.0 | Tests, E2ETests |
| `Humanizer.Core` | 2.14.1 | 3.0.10 | Data (via EFCore.Design) |
| `Microsoft.CodeAnalysis.*` (Roslyn, 6 Pakete) | 3.11.0 / 5.0.0 | 5.12.0 | Data |
| `Microsoft.Build.Framework` | 18.0.2 | 18.12.2 | Data |
| `SQLitePCLRaw.*` | 2.1.12 | 3.0.5 / 3.53.3 / 2.1.14 (ios-lib) | alle Projekte mit EFCore.Sqlite |
| `Microsoft.Maui.*` (Controls.Core, Xaml, Build.Tasks, Core, Essentials, Graphics, Resizetizer, Graphics.Win2D.WinUI.Desktop) | 10.0.20 | 11.0.10 | Reporter |
| `Microsoft.Extensions.*` (Caching, Configuration, DI, Logging, Options, Primitives u. a.) | 10.0.0–10.0.14 | 11.0.0 | Reporter, Data, Tests |
| `Microsoft.EntityFrameworkCore.*` (Core, Abstractions, Analyzers, Relational, Sqlite.Core) | 10.0.12 | 11.0.0 | Reporter, Data, Tests |
| `Microsoft.Data.Sqlite.Core` | 10.0.12 | 11.0.0 | Reporter, Data, Tests, E2ETests |
| `System.ServiceModel.Syndication` | 10.0.11 | 11.0.0 | transitiv in Reporter/Data/Tests/E2ETests (direkt in Core) |
| `System.Composition.*` (5 Pakete) | 9.0.0 | 11.0.0 | Data |
| `Microsoft.Graphics.Win2D` | 1.3.2 | 1.4.0 | Reporter (windows) |
| `Microsoft.IO.RecyclableMemoryStream` | 3.0.1 | 4.0.1 | Reporter (windows) |
| `Microsoft.Web.WebView2` | 1.0.3179.45 | 1.0.4314.35 | Reporter (windows) |
| `Microsoft.Windows.SDK.BuildTools` | 10.0.22621.756 | 10.0.28000.2957 | Reporter (windows) |
| `Microsoft.WindowsAppSDK` | 1.7.250909003 | 2.6.5 | Reporter (windows) |
| `Microsoft.Bcl.AsyncInterfaces` | 6.0.0 | 11.0.0 | Tests, E2ETests |
| `System.Management`, `System.Security.Permissions`, `System.Windows.Extensions`, `Microsoft.Win32.SystemEvents`, `System.CodeDom`, `System.Configuration.ConfigurationManager` (8.0.1), `System.Diagnostics.PerformanceCounter` (8.0.1), `System.Drawing.Common` (8.0.10), `System.Security.Cryptography.ProtectedData` | 6.0.0–8.0.10 | 11.0.0 | E2ETests (via FlaUI) |

## Rohausgabe (gekürzt, vollständiger Lauf)

```
[net10.0-ios] / [net10.0-windows10.0.19041.0] Reporter:
  > Microsoft.Extensions.Logging.Debug   10.0.0   10.0.0   11.0.0
  > Microsoft.Maui.Controls              10.0.20  10.0.20  11.0.10
  (transitiv: EF Core 10.0.12→11.0.0, Maui.* 10.0.20→11.0.10, Win2D 1.3.2→1.4.0,
   WebView2 1.0.3179.45→1.0.4314.35, WindowsAppSDK 1.7.250909003→2.6.5,
   Windows.SDK.BuildTools 10.0.22621.756→10.0.28000.2957, SQLitePCLRaw 2.1.12→3.0.5/3.53.3,
   Syndication 10.0.11→11.0.0, Extensions.* →11.0.0)

[net10.0] Reporter.Core:
  > System.ServiceModel.Syndication      10.0.11  10.0.11  11.0.0

[net10.0] Reporter.Data:
  > Microsoft.EntityFrameworkCore.Design 10.0.12  10.0.12  11.0.0
  > Microsoft.EntityFrameworkCore.Sqlite 10.0.12  10.0.12  11.0.0
  (transitiv: Humanizer.Core 2.14.1→3.0.10, Build.Framework 18.0.2→18.12.2,
   CodeAnalysis.* 3.11.0/5.0.0→5.12.0, Composition.* 9.0.0→11.0.0, EF Core→11.0.0)

[net10.0] Reporter.Tests:
  > Microsoft.EntityFrameworkCore.Sqlite        10.0.12  10.0.12  11.0.0
  > Microsoft.Extensions.TimeProvider.Testing   10.1.0   10.1.0   11.0.0
  (transitiv: Testing.Platform.MSBuild/Telemetry 2.5.2→2.5.3, AppInsights 2.23.0→3.2.0,
   EF Core 10.0.12→11.0.0, Extensions.* 10.0.12/10.0.14→11.0.0)

[net10.0-windows10.0.19041.0] Reporter.E2ETests:
  > Microsoft.Data.Sqlite                10.0.12  10.0.12  11.0.0
  (transitiv: Testing.* 2.5.2→2.5.3, System.* 8.0.x→11.0.0, AppInsights 2.23.0→3.2.0)
```
