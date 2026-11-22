<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Anforderung – Schritt 1: SDK-/TFM-Flip + Paketwelle

Quelle: Projektplan `docs/projects/task/issue-202-…/project-plan.md`,
Schritt 1 (= Migrationsplan `docs/help/entwicklung/dotnet-11-migration.md`,
Phase 1 Tasks 1.1–1.7 + Phase 2 Tasks 2.1–2.6).

## Ziel

Die Lösung `Reporter.sln` zielt atomar auf .NET 11: alle Target-Frameworks
und alle runtime-gebundenen Paketversionen werden gleichzeitig umgestellt,
so dass die Lösung unter dem .NET-11-SDK restored, baut und die
Nicht-UI-Tests grün laufen.

## Anforderungen

1. **TFM-Flip:**
   - `src/Reporter/Reporter.csproj` Z. 8–13: alle 6 bedingten
     `TargetFrameworks`-Listen `net10.0-*` → `net11.0-*`
     (`net11.0-windows10.0.19041.0`, `net11.0-ios`, `net11.0-android`).
   - `src/Reporter/Reporter.csproj` Z. 52: `UseInterpreter`-Condition
     `'net10.0-ios'` → `'net11.0-ios'`.
   - `src/Reporter.Core`, `src/Reporter.Data`, `src/Reporter.Tests`:
     `net10.0` → `net11.0`.
   - `src/Reporter.E2ETests`: `net10.0-windows10.0.19041.0` →
     `net11.0-windows10.0.19041.0`.
2. **Paketwelle (atomar):**
   - `Microsoft.EntityFrameworkCore.Design` + `...Sqlite` 10.0.12 → 11.0.0
     (`Reporter.Data`), `...Sqlite` (`Reporter.Tests`),
     `Microsoft.Data.Sqlite` (`Reporter.E2ETests`).
   - `Microsoft.Extensions.Logging.Debug` 10.0.0 → 11.0.0 (`Reporter`).
   - `System.ServiceModel.Syndication` 10.0.11 → 11.0.0 (`Reporter.Core`).
   - `Microsoft.Extensions.TimeProvider.Testing` 10.1.0 → 11.0.1
     (`Reporter.Tests`).
   - `Microsoft.Maui.Controls` bleibt `$(MauiVersion)` — 11er kommt über
     den Workload.
   - Test-Stack (coverlet.MTP 10.1.1, NET.Test.Sdk 18.12.0, TrxReport
     2.5.3, xunit.v3 4.0.3, runner.visualstudio 4.0.1, FlaUI.UIA3 5.0.0)
     nur anheben, wenn tatsächlich inkompatibel.
3. **Harte TFM-Strings:** `scripts/Run-E2ETests.ps1` (Kommentar Z. 13 +
   `$tfm` Z. 24), `scripts/iOS-Deployment.ps1:94`,
   `src/Reporter.E2ETests/ReporterAppFixture.cs` Konventionspfad.
4. **`global.json`:** `sdk.version 11.0.100` + `rollForward latestPatch`
   ergänzen (bestehender `test.runner`-Eintrag bleibt).
5. **`.config/dotnet-tools.json`:** neu mit `dotnet-ef` 11.0.0;
   globalen `dotnet ef` für die lokale Verifikation auf 11.x aktualisieren.
6. **Gate G5:** Falls MAUI 11 die Plattform-Mindestversionen anhebt,
   `SupportedOSPlatformVersion`/`TargetPlatformMinVersion` +
   `Package.appxmanifest` synchron ziehen; sonst unverändert lassen.
7. `dotnet workload restore Reporter.sln` ausführen (Band 11.0.100).

## Akzeptanzkriterien

- `dotnet restore` + `dotnet build` für `net11.0-windows10.0.19041.0`
  erfolgreich.
- `dotnet test` auf Core/Data/Tests erfolgreich; MTP-Aufruf mit
  `--coverlet`/`--report-trx`-Argumenten läuft unter .NET-11-SDK.
- `scripts/Run-StaticChecks.ps1` Exit 0.
- Kein `net10.0`-Verweis mehr in `src/**/*.csproj`, `global.json` und den
  genannten Scripts/Fixtures.
- Keine neue EF-Schema-Migration.

## Nicht-Ziele

- Keine CI-/Workflow-Änderungen (Schritt 3).
- Keine Doku-/README-Änderungen (Schritt 4).
- Keine Änderung an `dependabot.yml`, `SupportedOSPlatformVersion`-
  Werten (außer G5 greift) oder App-Code.
