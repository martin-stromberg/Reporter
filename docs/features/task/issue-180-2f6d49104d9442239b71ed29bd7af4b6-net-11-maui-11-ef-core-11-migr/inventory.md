<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Inventar: .NET 11 / MAUI 11 / EF Core 11 – betroffene Stellen

Bestandsaufnahme für den Migrationsplan (Issue #180). Stand: Branch
`task/issue-180-...-net-11-maui-11-ef-core-11-migr` (staging-Head).

Alle Pfade relativ zum Repo-Root. Details in den verlinkten Unterdokumenten.

## Übersicht

| Bereich | Umfang | Detail |
|---|---|---|
| TargetFrameworks | 5 .csproj, davon 1 mit 6 bedingten TFM-Listen | [inventory/tfms.md](inventory/tfms.md) |
| NuGet-Pakete | 16 PackageReference + 1 FrameworkReference, 4 Update-Wellen | [inventory/packages.md](inventory/packages.md) |
| SDK / Workloads | global.json ohne SDK-Pin, `$(MauiVersion)` workload-gesteuert, iOS-Pin `26.5.10318` | [inventory/workloads.md](inventory/workloads.md) |
| EF Core | 2 DbContexte, 17 Migrations, `MigrateAsync()`-Pfad, iOS-`UseInterpreter` | [inventory/efcore.md](inventory/efcore.md) |
| CI/CD | 5 Workflows mit `dotnet-version: '10.0.x'`, 4 Composite Actions, 3 Skripte mit hartem net10-TFM | [inventory/ci.md](inventory/ci.md) |
| Dependabot & Sonstiges | dependabot.yml (nuget/npm/github-actions), Docs/README, Plattform-Manifeste | [inventory/misc.md](inventory/misc.md) |

## Kernbefunde

- **Kein zentrales Paketmanagement**: keine `Directory.Packages.props`, keine `NuGet.config`,
  kein `Directory.Build.targets`. Alle Versionen stehen direkt in den .csproj.
- **`global.json`** (`global.json:1-5`) enthält nur `{"test":{"runner":"Microsoft.Testing.Platform"}}` –
  **kein SDK-Pin**. Lokal darf jedes installierte SDK laufen; der einzige SDK-Pin existiert
  als CI-Artefakt in `.github/actions/package-ios/action.yml:68` (schreibt `10.0.100`).
- **`Microsoft.Maui.Controls` nutzt `$(MauiVersion)`** (`src/Reporter/Reporter.csproj:76`) –
  die Version kommt aus dem installierten MAUI-Workload, nicht aus einem Pin im Repo.
- **`UseInterpreter=true` für iOS** (`src/Reporter/Reporter.csproj:52-56`) wegen EF Core
  `Expression.Compile()`/Reflection.Emit auf AOT-only-Geräten – bei EF Core 11 erneut verifizieren.
- **EF Core 11 erfordert zwingend .NET 11** – `Microsoft.EntityFrameworkCore.*` und
  `Microsoft.Data.Sqlite` können nicht isoliert angehoben werden.
- **Harte net10-Strings** in CI-Actions und Skripten (nicht nur in .csproj):
  `--framework net10.0-windows10.0.19041.0` / `net10.0-ios` / `net10.0-android`,
  Pack-Glob `Microsoft.iOS.Sdk.net10.0_*`, E2E-Konventionspfad in `ReporterAppFixture.cs:143`.
- **Bekannte offene Dependabot-PRs** (Info aus Aufgabe): `System.ServiceModel.Syndication 11.0.0`,
  `Microsoft.Data.Sqlite 11.0.0` – blockiert bis zur Runtime-Migration.
- **Maintenance-Branch** `maintenance/net10` existiert bereits auf `main`-Head `5492d71` (v0.7.0).

## Nicht betroffen / explizit geprüft

- Kein Dockerfile, keine `.config/dotnet-tools.json`, kein `.vscode/`.
- npm-Toolchain (`package.json`) betrifft nur `semantic-release` – keine .NET-Abhängigkeit.
- Workflows `deploy-pages.yml`, `sync-staging-with-main.yml`, `staging-to-main-promotion.yml`,
  `verify-pr-source.yml`, `review-*.yml` referenzieren kein .NET (ubuntu + gh/node).
- `IncludeIosTarget`/`IncludeAndroidTarget`-Schalter bleiben konzeptionell gleich; nur die
  TFM-Strings ändern sich.
