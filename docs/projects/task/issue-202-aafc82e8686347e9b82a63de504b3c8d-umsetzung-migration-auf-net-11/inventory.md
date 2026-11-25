<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme – Projekt .NET-11-Migration

Ermittelt am 22.11.2026 gegen `staging`-Stand (Task-Branch = `origin/staging`,
keine Divergenz). Der Migrationsplan `docs/help/entwicklung/dotnet-11-migration.md`
enthält bereits eine vollständige Stellen-Analyse (Datei:Zeile); dieses Dokument
verifiziert den tatsächlichen Ist-Stand und ergänzt Umgebungsbefunde.

## Lösungsstruktur

`Reporter.sln` mit 5 Projekten unter `src/`:

| Projekt | TFM (IST) | Relevante Pakete |
|---------|-----------|------------------|
| `Reporter` (MAUI App) | 6 bedingte TFM-Listen `net10.0-{windows10.0.19041.0,ios,android}` (`Reporter.csproj:8-13`); `UseInterpreter` auf `net10.0-ios` (Z. 52-56); `SupportedOSPlatformVersion` iOS/macCatalyst 15.0, Android 21.0, Windows 10.0.17763.0 (Z. 45-49); `WindowsPackageType=None`; `MauiXamlInflator=SourceGen` | `Microsoft.Maui.Controls` `$(MauiVersion)`, `Microsoft.Extensions.Logging.Debug` 10.0.0, `CommunityToolkit.Mvvm` 8.4.2 |
| `Reporter.Core` | `net10.0` | `CommunityToolkit.Mvvm` 8.4.2, `System.ServiceModel.Syndication` 10.0.11 |
| `Reporter.Data` | `net10.0` | `Microsoft.EntityFrameworkCore.Design` 10.0.12, `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12 |
| `Reporter.Tests` | `net10.0` | `coverlet.MTP` 10.1.1, `EFCore.Sqlite` 10.0.12, `Microsoft.Extensions.TimeProvider.Testing` 10.1.0, `Microsoft.NET.Test.Sdk` 18.12.0, `TrxReport` 2.5.3, `xunit.v3` 4.0.3, `xunit.runner.visualstudio` 4.0.1 |
| `Reporter.E2ETests` | `net10.0-windows10.0.19041.0` | `FlaUI.UIA3` 5.0.0, `Microsoft.Data.Sqlite` 10.0.12, `Microsoft.NET.Test.Sdk` 18.12.0, `xunit.v3` 4.0.3, `xunit.runner.visualstudio` 4.0.1; `FrameworkReference Microsoft.AspNetCore.App` (Z. 24) |

- `global.json`: nur `test.runner = Microsoft.Testing.Platform`, **kein** SDK-Pin.
- Kein `.config/` (kein Tool-Manifest); `dotnet ef` global = **10.0.10**.
- MTP-Teststack; `dotnet test -- --coverlet --report-trx` in CI.

## Harte TFM-/Versions-Strings (verifiziert)

- `src/Reporter.E2ETests/ReporterAppFixture.cs:143` — Konventionspfad
  `bin/Debug/net10.0-windows10.0.19041.0/win-x64/Reporter.exe`.
- `scripts/Run-E2ETests.ps1:13` (Kommentar), `:24` (`$tfm`).
- `scripts/iOS-Deployment.ps1:94` (`$framework = "net10.0-ios"`).
- `scripts/iOS-Deployment.md` — 4+ Vorkommen `net10.0-ios` (Doku).
- `ci-instructions.md` — `10.0.x` (Z. 194/228/449/1018), `net10.0` (Z. 458/465).

## CI/CD (verifiziert)

`dotnet-version: '10.0.x'` in:
- Workflows: `staging-ci.yml` (Z. 70, 109, 188), `pr-staging-ci.yml`
  (Z. 73, 112, 191), `release.yml` (Z. 75), `security-scan.yml` (Z. 26).
- Actions: `build-and-package/action.yml` (Z. 19 + `--framework net10.0-windows10.0.19041.0` Z. 48),
  `package-android/action.yml` (Z. 19 + `--framework net10.0-android` Z. 50).
- `package-ios/action.yml` (komplett eingesehen): `ios-workload-version`
  Default `26.5.10318` (Z. 47), `dotnet-version '10.0.1xx'` (Z. 59),
  temporäre `global.json` `10.0.100` (Z. 68), Pack-Cleanup-Glob
  `Microsoft.iOS.Sdk.net10.0_*` (Z. 87), `--framework net10.0-ios` (Z. 156),
  `MauiXamlInflator=XamlC`-Override (Z. 161) mit SourceGen-Bug-Kommentar
  (Z. 143-146). setup-dotnet SHA-gepinnt `@0f4a354...` = v6.
- `dotnet workload restore Reporter.sln`-Aufrufe bleiben unverändert
  (ziehen mit 11er-SDK die 11er-Workloads).
- Coverage-Ausschluss `[*]Reporter.Data.Migrations.*` (`staging-ci.yml`,
  `pr-staging-ci.yml`).

## EF-Core-Stand (verifiziert)

- `Reporter.Data`: `DatabaseMigrationService.cs:34-44` —
  `ItemContentMigrationService.MigrateLegacyContentAsync` vor
  `context.Database.MigrateAsync()` (Reihenfolge-Invariante);
  `ItemContentMigrationService.cs` nutzt `GetDbConnection()`/`DbCommand`
  mit `PRAGMA table_info('items')` (Z. ~97); Design-Time-Factories
  `ReporterDbContextFactory`, `ContentDbContextFactory` vorhanden;
  Migrationen: 15 + 2 (laut Plan).
- Tests: `EnsureCreatedAsync`-Factories (`ReporterDbContextTests_*`,
  `ContentDbContextTests`) vs. Migrations-Tests mit `MigrateAsync`.
- Vorbefund G6: kein `OwnsOne`/`OwnsMany`, kein `AsSplitQuery`, kein
  `EFOptimizeContext`, `Tags` = `string?` → vier von fünf EF-11-Breaking-
  Changes treffen nicht; bei Migration final re-greppen.

## Lokale Umgebung

- .NET SDKs: `10.0.404`, **`11.0.100`** (GA) — `dotnet --version` = 11.0.100.
- Workloads (Installationsquelle VS 18.11, Band `11.0.100-preview.6`):
  `android` 37.0.0-preview.6.59, `ios`/`maccatalyst` 26.5.11720-net11-p6,
  `maui-windows` 11.0.0-preview.6.26360.8. → Nach TFM-Flip
  `dotnet workload restore Reporter.sln` nötig (zieht GA-Manifeste
  `11.0.0` ins Band `11.0.100`).
- NuGet-Verfügbarkeit geprüft: `EFCore.*`/`Microsoft.Data.Sqlite`/
  `Logging.Debug`/`Syndication` = **11.0.0**; `TimeProvider.Testing` =
  **11.0.1**; `Microsoft.Maui.Controls` = 11.0.10 (kommt via Workload);
  `Microsoft.NET.Sdk.iOS.Manifest-11.0.100` = **27.2.11039** (einzige
  GA-Version; `net11.0_26.5` hat nur Preview/RC — G4 geklärt).
- `dotnet ef` global: 10.0.10 → für 11er-Projekt 11.x nötig
  (`dotnet tool update -g dotnet-ef` oder Tool-Manifest).

## Einschränkungen der Umgebung

- Windows, kein Mac-Pairing: `net11.0-ios` lokal nicht baubar → iOS-Build
  nur via CI (`macos-latest`, `IOS_SIGNING_ENABLED`).
- FlaUI-E2E benötigt interaktive Session mit sichtbarem Fenster —
  Ausführung wird versucht, Ergebnis dokumentiert.
- `maintenance/net10` (v0.7.0) für Bestands-DB-Test nutzbar (Worktree).

## Konventionen

- Conventional-Commits (`feat:`, `fix:`, `docs:`, `merge:`, `plan:`).
- Projektartefakte `docs/projects/{branchname}/`, Feature-Artefakte
  `docs/features/{schritt-branch}/`.
- Statische Checks: `scripts/Run-StaticChecks.ps1` (Restore/Format/
  Security/Build/LicenseHeaders) — Exit 0 Pflicht vor Abschluss.
- PRs gegen `staging` mit Closing-Keyword fürs Issue.
