<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review – Schritt 1

## Ergebnis

**Status:** Vollständig umgesetzt

Alle 11 Planelemente sind im Arbeitsverzeichnis umgesetzt (unabhängige
Prüfung durch schreibgeschützten Review-Agenten):

- `Reporter.csproj`: 6 TFM-Listen auf net11.0-*; `UseInterpreter`-
  Condition net11.0-ios; `Logging.Debug` 11.0.0; `$(MauiVersion)`
  unverändert; `SupportedOSPlatformVersion` android 24.0 / maccatalyst
  17.0 (G5-Befund, plan-konform); iOS 15.0 / Windows 17763 unverändert;
  `Package.appxmanifest` korrekt unverändert.
- `Reporter.Core`: net11.0, Syndication 11.0.0.
- `Reporter.Data`: net11.0, EF.Design + Sqlite 11.0.0.
- `Reporter.Tests`: net11.0, EF.Sqlite 11.0.0, TimeProvider.Testing
  11.0.1; `coverlet.MTP` 10.1.1 bewusst unverändert (2.6-verifiziert).
- `Reporter.E2ETests`: TFM net11.0-windows…, `Microsoft.Data.Sqlite`
  11.0.0; `ReporterAppFixture.cs` Konventionspfad net11.0.
- `Run-E2ETests.ps1` (Z. 13/24), `iOS-Deployment.ps1` (Z. 94) geflippt.
- `global.json`: `sdk.version 11.0.100` + `latestPatch`, `test.runner`
  erhalten.
- Tool-Manifest: `dotnet-tools.json` im **Repo-Root** (neue
  SDK-10/11-Konvention statt `.config/` — `dotnet new tool-manifest`
  erzeugt sie dort; Resolver akzeptiert beide Orte), `dotnet-ef` 11.0.0.
- `dotnet workload restore` erfolgreich (GA-Band 11.0.100).

Gegenprobe: keine echten `net10.0`-Reste in Quelldateien (nur
Windows-OS-Versionen `10.0.19041.0`/`10.0.17763.0` und historische
EF-Migrations-Annotationen `10.0.12` — bewusst unangetastet).
Keine neue EF-Schema-Migration (neueste bleibt `AddItemTags`).

## Offene Aufgaben

Keine.

## Hinweise

Pfadabweichung Tool-Manifest (Root statt `.config/`) in `plan.md`/
`requirement.md` nachgedokumentiert; verifizierte Läufe siehe
`test-results.md` (Build windows+android, 805/805 Tests, MTP-Flags,
StaticChecks Exit 0); iOS-Build/E2E deklariert auf CI/Schritt 4.
