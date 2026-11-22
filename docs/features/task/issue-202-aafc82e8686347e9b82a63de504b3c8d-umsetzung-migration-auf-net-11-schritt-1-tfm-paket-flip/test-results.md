<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Testergebnisse – Schritt 1: SDK-/TFM-Flip + Paketwelle

Datum: 22.11.2026 · SDK: `11.0.100` (global.json-Pin, rollForward latestPatch)

## Ausgeführte Prüfungen

| Prüfung | Ergebnis | Nachweis |
|---------|----------|----------|
| `dotnet workload restore Reporter.sln` | ✅ Bestanden | GA-Band 11.0.100 installiert (maui-windows, android, ios, maccatalyst) |
| `dotnet restore Reporter.sln -r win-x64` (`IncludeIosTarget=false`) | ✅ Bestanden | alle 5 Projekte restored, 0 Fehler |
| `dotnet build` `net11.0-windows10.0.19041.0` (Debug, win-x64) | ✅ Bestanden | 0 Warnungen, 0 Fehler (~21s) |
| `dotnet build` `net11.0-android` (Release) | ✅ Bestanden | 0 Fehler; 2× CS0169 (vorbestehende Windows-only-Felder `#if WINDOWS` in `UnreadPage.xaml.cs`, plattformbedingt, kein Migrationsbefund) |
| `dotnet test` `Reporter.Tests` (Debug, net11.0) | ✅ Bestanden | **805/805** grün, ~8s |
| `dotnet test` mit MTP-CI-Argumenten (Release: `--coverlet --coverlet-output-format cobertura --coverlet-include/--exclude` + `--report-trx`) | ✅ Bestanden | 805/805 grün; `TestResults/test-results.trx` + `coverage.cobertura.*.xml` erzeugt (CI-Glob `**/coverage.cobertura*.xml` passt) |
| `scripts/Run-StaticChecks.ps1` (Restore, Format, LicenseHeaders, NuGet-Security, npm-Audit, Static-Analysis-Release-Build) | ✅ Bestanden | **Exit 0**, alle Checks grün; Static-Analysis-Build 0 Warnungen |
| EF-Core-11-Restore | ✅ | `Microsoft.EntityFrameworkCore.*` 11.0.0 resolved + kompiliert |
| Gate G5 (Plattform-Mindestversionen MAUI 11) | ✅ Befund eingearbeitet | Android-Min: **API 24.0** (Build-Fehler XA4216 vor Fix, nach Anhebung `SupportedOSPlatformVersion` 21→24 Build grün); iOS-Min bleibt **15.0** (`iOSMinSupportedOSPlatformVersion` im net11.0_27.2-Pack); MacCatalyst-Min **17.0** (Pack-Befund, csproj synchron gezogen); Windows **10.0.17763.0** unverändert |

## Test-Stack-Kompatibilität (Task 2.6)

`coverlet.MTP` 10.1.1, `Microsoft.NET.Test.Sdk` 18.12.0,
`Microsoft.Testing.Extensions.TrxReport` 2.5.3, `xunit.v3` 4.0.3,
`xunit.runner.visualstudio` 4.0.1 — alle unter .NET-11-SDK grün
(s. obige Läufe) → **keine Anhebung nötig** (Versionen bleiben).

## Reale End-to-End-Abnahme

- **Umfang dieses Schritts:** Build-/Runtime-Wechsel ohne Fachlogik-
  Änderung. Der reale Nachweis „Anwendung baut und läuft auf .NET 11"
  ist durch Build + `dotnet test` (805 reale Ausführungen inkl.
  EF-11-Integration gegen SQLite) und den Static-Analysis-Release-Build
  erbracht. GUI-Start/App-Interaktion ist nicht Gegenstand dieses
  Schritts — die echten Plattform-Läufe (E2E FlaUI, iOS-Gerät) sind
  im Projektplan als Restpunkte deklariert (Projektschritt 4 / CI /
  Anwender-Abnahme). **Bewertung: Bestanden** für diesen Schritt.

## Fehlgeschlagene Tests

Keine.

## Nicht durchgeführte Läufe (Umgebung)

- `net11.0-ios`-Build: auf Windows ohne Mac-Pairing technisch nicht
  möglich → CI (`macos-latest`), siehe Projektschritt 3/PR-Check.
- E2E FlaUI: geplanter Versuch in Projektschritt 4.
