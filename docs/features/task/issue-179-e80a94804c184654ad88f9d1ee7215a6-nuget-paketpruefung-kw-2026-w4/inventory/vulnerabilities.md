<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Vulnerability- und Deprecated-Scan

Ausgeführt am 2026-11-17 gegen `Reporter.sln` (alle 5 Projekte), Quellen `https://api.nuget.org/v3/index.json` + Fallback-Ordner `C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\`.

## `dotnet list Reporter.sln package --vulnerable --include-transitive`

Exit-Code 0. Ergebnis: **keine verwundbaren Pakete** in keinem Projekt — weder direkt noch transitiv.

```
Für das angegebene Projekt "Reporter" liegen gemäß den aktuellen Quellen keine anfälligen Pakete vor.
Für das angegebene Projekt "Reporter.Core" liegen gemäß den aktuellen Quellen keine anfälligen Pakete vor.
Für das angegebene Projekt "Reporter.Data" liegen gemäß den aktuellen Quellen keine anfälligen Pakete vor.
Für das angegebene Projekt "Reporter.Tests" liegen gemäß den aktuellen Quellen keine anfälligen Pakete vor.
Für das angegebene Projekt "Reporter.E2ETests" liegen gemäß den aktuellen Quellen keine anfälligen Pakete vor.
```

## `dotnet list Reporter.sln package --deprecated --include-transitive`

Exit-Code 0. Ergebnis: **keine als veraltet (deprecated) markierten Pakete** in keinem Projekt.

```
Für das angegebene Projekt "Reporter" liegen gemäß den aktuellen Quellen keine veralteten Pakete vor.
Für das angegebene Projekt "Reporter.Core" liegen gemäß den aktuellen Quellen keine veralteten Pakete vor.
Für das angegebene Projekt "Reporter.Data" liegen gemäß den aktuellen Quellen keine veralteten Pakete vor.
Für das angegebene Projekt "Reporter.Tests" liegen gemäß den aktuellen Quellen keine veralteten Pakete vor.
Für das angegebene Projekt "Reporter.E2ETests" liegen gemäß den aktuellen Quellen keine veralteten Pakete vor.
```

## Einordnung als Faktenlage

- Der NuGet-Vulnerability-Scan ist auch fester Bestandteil der CI (`security-scan`-Composite-Action in `.github/actions/security-scan/action.yml` bzw. `Invoke-SecurityScan` in `scripts/Run-StaticChecks.ps1`, Zeilen 61–87) und schlägt dort fehl, sobald die Ausgabe „has the following vulnerable packages" oder „Severity" enthält.
- Der Deprecated-Scan deckt nur das NuGet-Deprecation-Flag ab; „nicht mehr gepflegt" ohne Flag (z. B. letztes Release sehr alt) erfordert eine separate Prüfung der NuGet.org-Metadaten je Paket — dafür sind die Paket-IDs/Versionen in `packages.md`/`outdated.md` die Arbeitsgrundlage.
