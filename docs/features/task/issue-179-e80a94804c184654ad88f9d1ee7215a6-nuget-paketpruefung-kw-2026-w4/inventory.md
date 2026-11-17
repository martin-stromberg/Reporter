<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: NuGet-Paketprüfung KW 2026-W47 (Issue #179)

Bestandsaufnahme aller in `Reporter.sln` referenzierten NuGet-Pakete sowie der dazugehörigen Update-, Sicherheits- und Wartungsfakten. Grundlage ist die übersetzte Anforderung in `requirement.md` (wiederkehrender Review-Workflow `.github/workflows/review-nuget.yml`, Label `scheduled-nuget-review`).

## Zusammenfassung

- **5 Projekte** unter `src/` mit insgesamt **16 direkten `PackageReference`-Einträgen** (13 verschiedene Paket-IDs); Versionsverwaltung dezentral inline in den `.csproj`-Dateien, kein `Directory.Packages.props`, keine `packages.lock.json`, kein `NuGet.config` vorhanden.
- **Veraltete direkte Pakete (7 von 13):** `Microsoft.Maui.Controls` 10.0.20 → 11.0.10 (via `$(MauiVersion)` aus installiertem Workload `maui-windows` 10.0.20), `Microsoft.Extensions.Logging.Debug` 10.0.0 → 11.0.0, `System.ServiceModel.Syndication` 10.0.11 → 11.0.0, `Microsoft.EntityFrameworkCore.Design`/`.Sqlite` 10.0.12 → 11.0.0, `Microsoft.Extensions.TimeProvider.Testing` 10.1.0 → 11.0.0, `Microsoft.Data.Sqlite` 10.0.12 → 11.0.0. Alle verfügbaren Updates sind Major-Sprünge auf die .NET-11-Welle — es gibt **keine offenen Patch-/Minor-Updates innerhalb der 10er-Linie** für die direkten Pakete.
- **Aktuell (keine Updates gemeldet):** `CommunityToolkit.Mvvm` 8.4.2, `coverlet.MTP` 10.1.1, `Microsoft.NET.Test.Sdk` 18.12.0, `Microsoft.Testing.Extensions.TrxReport` 2.5.3, `xunit.v3` 4.0.3, `xunit.runner.visualstudio` 4.0.1, `FlaUI.UIA3` 5.0.0.
- **Keine verwundbaren Pakete** (direkt + transitiv, `--vulnerable --include-transitive`) und **keine als veraltet (deprecated) markierten Pakete** in keinem der 5 Projekte.
- **Keine offenen Dependabot-PRs** im Repo `martin-stromberg/Reporter`; einziger offener PR ist #184 (Automated promotion staging → main, DRAFT). Dependabot ist für nuget/npm/github-actions wöchentlich gegen `staging` konfiguriert.
- **Relevante transitive Ausreißer:** `Microsoft.Testing.Platform`/`.MSBuild`/`.TrxReport.Abstractions`/`.Telemetry` 2.5.2 → 2.5.3 (Patch-Delta innerhalb des Test-Stacks), `Microsoft.ApplicationInsights` 2.23.0 → 3.2.0, `Humanizer.Core` 2.14.1 → 3.0.10, `SQLitePCLRaw.*` 2.1.12 → 3.0.5, `Microsoft.WindowsAppSDK` 1.7.250909003 → 2.6.5, `Microsoft.Web.WebView2` 1.0.3179.45 → 1.0.4314.35.
- **Build-Umgebung:** .NET SDK 10.0.403, Workload-Version 10.0.401, `maui-windows` 10.0.20 (daraus `$(MauiVersion)` = **10.0.20**); CI nutzt `dotnet-version: '10.0.x'` + `dotnet workload restore Reporter.sln` mit `IncludeIosTarget=false`/`IncludeAndroidTarget=false`.
- `global.json` enthält **keine SDK-Pinning** — nur `"test": { "runner": "Microsoft.Testing.Platform" }`.
- `scripts/Run-StaticChecks.ps1` bündelt: Restore (`-r win-x64`), `dotnet format --verify-no-changes`, Lizenzheader-Check (Node.js), NuGet-Vulnerability-Scan + `npm audit` (high), Release-Build mit `TreatWarningsAsErrors=true`.

## Details

- [Direkte Paketreferenzen je Projekt](inventory/packages.md)
- [Outdated-Scan (direkt + transitiv)](inventory/outdated.md)
- [Vulnerability- und Deprecated-Scan](inventory/vulnerabilities.md)
- [Dependabot, offene PRs und Review-Workflow](inventory/dependabot.md)
- [Build-, Test- und Check-Kommandos](inventory/build-test.md)
