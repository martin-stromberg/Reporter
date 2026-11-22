<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan – Schritt 1: SDK-/TFM-Flip + Paketwelle

## Scope-Klärung

- **Verarbeitungseinheit:** die komplette `Reporter.sln` (5 Projekte) plus
  Build-bezogene Hilfsdateien — atomare Umstellung, kein Teilstand.
- **Auslöser/Wirkung:** `dotnet restore`/`build`/`test` laufen nach dem
  Flip unter SDK 11.0.100; Verhalten der App ist unverändert (reiner
  Runtime-/Framework-Wechsel, keine Code-Änderung am Fachverhalten).
- **Scope-Ausschlüsse:** CI-Dateien (`.github/`), `ci-instructions.md`,
  Doku/`README`, `scripts/iOS-Deployment.md` → Projektschritte 3/4.
  Status: `bestätigt` (durch Projektplan vorgegeben).

## Änderungen (Datei → Inhalt)

1. `src/Reporter/Reporter.csproj`
   - Z. 8–13: `net10.0-windows10.0.19041.0` → `net11.0-windows10.0.19041.0`,
     `net10.0-ios` → `net11.0-ios`, `net10.0-android` → `net11.0-android`
     (in allen 6 Listen).
   - Z. 52: Condition `'net10.0-ios'` → `'net11.0-ios'`.
   - Z. 77: `Microsoft.Extensions.Logging.Debug` `10.0.0` → `11.0.0`.
   - `Microsoft.Maui.Controls` `$(MauiVersion)` unverändert.
   - `SupportedOSPlatformVersion`/`TargetPlatformMinVersion` Z. 45–49:
     zunächst unverändert; erst bei G5-Befund anheben (+`Package.appxmanifest`).
2. `src/Reporter.Core/Reporter.Core.csproj`: Z. 6 `net10.0` → `net11.0`;
   Z. 15 `System.ServiceModel.Syndication` `10.0.11` → `11.0.0`.
3. `src/Reporter.Data/Reporter.Data.csproj`: Z. 6 `net10.0` → `net11.0`;
   Z. 18–22 `Microsoft.EntityFrameworkCore.Design` +
   `Microsoft.EntityFrameworkCore.Sqlite` `10.0.12` → `11.0.0`.
4. `src/Reporter.Tests/Reporter.Tests.csproj`: Z. 6 `net10.0` → `net11.0`;
   Z. 17 `Microsoft.EntityFrameworkCore.Sqlite` `10.0.12` → `11.0.0`;
   Z. 18 `Microsoft.Extensions.TimeProvider.Testing` `10.1.0` → `11.0.1`.
5. `src/Reporter.E2ETests/Reporter.E2ETests.csproj`: Z. 6
   `net10.0-windows10.0.19041.0` → `net11.0-windows10.0.19041.0`;
   Z. 17 `Microsoft.Data.Sqlite` `10.0.12` → `11.0.0`.
6. `src/Reporter.E2ETests/ReporterAppFixture.cs` ~Z. 143:
   `net10.0-windows10.0.19041.0` → `net11.0-windows10.0.19041.0`.
7. `scripts/Run-E2ETests.ps1`: Z. 13 Kommentarpfad + Z. 24 `$tfm`
   → `net11.0-windows10.0.19041.0`.
8. `scripts/iOS-Deployment.ps1`: Z. 94 `$framework` → `"net11.0-ios"`.
9. `global.json`: `sdk`-Block ergänzen:
   `{ "sdk": { "version": "11.0.100", "rollForward": "latestPatch" },
      "test": { "runner": "Microsoft.Testing.Platform" } }`.
10. `.config/dotnet-tools.json` neu:
    `dotnet-ef` Version `11.0.0`, `rollForward: false` o. ä. per
    `dotnet new tool-manifest` + `dotnet tool install dotnet-ef --version 11.0.0`.
    Lokal parallel `dotnet tool update -g dotnet-ef` auf 11.x.
11. `dotnet workload restore Reporter.sln` (mit `IncludeIosTarget=false`
    nur maui-windows/android nötig — restore entscheidet selbst).

## Reihenfolge / Vorgehen

1. `git status` sauber; alle Edits 1–10 anwenden.
2. `dotnet workload restore Reporter.sln`.
3. `$env:IncludeIosTarget='false'` → `dotnet restore Reporter.sln -r win-x64`.
4. `dotnet build` (Debug + Release-Check über StaticChecks).
5. `dotnet test src/Reporter.Tests` sowie Core/Data-Testläufe
   (MTP-Aufruf: `dotnet test -- --coverlet --report-trx` prüfen).
6. `scripts/Run-StaticChecks.ps1` → Exit 0.
7. Grep-Verifikation `net10\.0` in `src/`, `global.json`, Scripts.

## Risiken

- Restore zieht `Microsoft.Maui.Controls` 11.x — Compile-Breaks durch
  MAUI-11-API-Änderungen wären im Build sichtbar (keine erwartet;
  Reporter nutzt Standard-MAUI-API).
- `dotnet workload restore` benötigt evtl. Admin/Netz — bei Scheitern
  bleibt `maui-windows` aus dem preview.6-Band ggf. nutzbar; Ergebnis
  dokumentieren.
- EF-11-Tooling: `dotnet-ef` global 10.0.10 → Update auf 11.x.

## Offene Punkte

Keine — alle Entscheidungen im Projektplan getroffen (G4=27.2.11039,
G5 per Build-Befund, global.json-Pin, dotnet-ef-Manifest,
MauiXamlInflator bleibt, ci-instructions mitziehen).
