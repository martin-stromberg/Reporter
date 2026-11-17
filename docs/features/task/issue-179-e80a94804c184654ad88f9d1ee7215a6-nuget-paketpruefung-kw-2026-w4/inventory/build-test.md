<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Build-, Test- und Check-Kommandos

## Lokale Umgebung (Ist-Zustand)

- `dotnet --version`: **10.0.403**; `dotnet --list-sdks`: nur `10.0.403`.
- `dotnet workload list`: Workload-Version 10.0.401; installiert `android` 36.1.69, `ios` 26.5.10318, `maccatalyst` 26.5.10318, `maui-windows` 10.0.20 (jeweils `/10.0.100`, Quelle „SDK 10.0.400, VS 18.11.12328.437").
- `global.json`: **keine** `sdk`-Pinning — einzig `{ "test": { "runner": "Microsoft.Testing.Platform" } }` (Test-Runner = Microsoft.Testing.Platform, nicht VSTest).
- CI pinnt .NET auf `dotnet-version: '10.0.x'` via `actions/setup-dotnet@v6` und installiert Workloads per `dotnet workload restore Reporter.sln`.

## Kommandos laut `CONTRIBUTING.md`

- Build: `dotnet build Reporter.sln`
- Tests: `dotnet test Reporter.sln --filter "Category!=E2E"` und `npm test` (npm-Tests für `scripts/*.test.mjs`)
- E2E-Smoke-Tests (nur Windows, interaktive Desktop-Session): `.\scripts\Run-E2ETests.ps1`
- Lokale statische Prüfungen vor Abschluss: `.\scripts\Run-StaticChecks.ps1`
- Neue öffentliche APIs brauchen XML-Doku (`CS1591` als Fehler in allen `.csproj` via `WarningsAsErrors`); neue Dateien brauchen den kurzen Lizenzheader (`scripts/add-license-headers.mjs`).

## `scripts/Run-StaticChecks.ps1` (143 Zeilen) — was es prüft

Parameter: `-Check All|Format|Security|Build|LicenseHeaders|Restore` (Default `All`), `-SkipRestore`. Setzt `$env:IncludeIosTarget = 'false'`; Konfiguration `Release`, Runtime `win-x64`. Ablauf bei `All` (mit Exit 1 bei jedem Fehler):

1. `Invoke-PackageRestore`: `dotnet restore Reporter.sln -r win-x64`
2. `Invoke-FormatCheck`: `dotnet format Reporter.sln --verify-no-changes --no-restore --severity error`
3. `Invoke-LicenseHeaderCheck`: `node scripts/add-license-headers.mjs --check` (Node.js nötig)
4. `Invoke-SecurityScan`: `dotnet list Reporter.sln package --vulnerable --include-transitive` + String-Match auf „has the following vulnerable packages|Severity"; danach `npm audit --package-lock-only --audit-level=high` (Node.js/npm nötig)
5. `Invoke-StaticAnalysisBuild`: `dotnet build Reporter.sln --configuration Release --no-restore -p:TreatWarningsAsErrors=true`

AGENTS.md verlangt: Skript muss mit Exit-Code 0 ohne Findings durchlaufen, bevor ein Feature als fertig gilt.

## CI (`.github/workflows/pr-staging-ci.yml`, `staging-ci.yml` analog)

- Runner `windows-latest`, Env `IncludeIosTarget=false`, `IncludeAndroidTarget=false`.
- Schritte: `dotnet workload restore Reporter.sln` → `dotnet restore Reporter.sln -r win-x64` → Format-Check → Lizenzheader → Security-Scan (Composite `.github/actions/security-scan`: NuGet-Vulnerability + `npm audit --audit-level=high`, Artefakt-Upload der Reports) → `dotnet build ... -p:TreatWarningsAsErrors=true`.
- Test-Job: `dotnet build Reporter.sln -c Release --no-restore`, dann `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release --no-build -- --coverlet --coverlet-output-format cobertura --coverlet-include "[Reporter.*]*" --coverlet-exclude "[*]Reporter.Data.Migrations.*" --report-trx --report-trx-filename test-results.trx`; Coverage-Gate **Line coverage ≥ 70 %** via `dotnet-reportgenerator-globaltool` 5.5.11.
- `dotnet test` nutzt MTP-Syntax (`-- --coverlet ...`) konsistent mit `global.json`-Runner `Microsoft.Testing.Platform`.
- `detect-backmerge`-Job überspringt Checks bei reinen Back-Merges von `main`.

## `ci-instructions.md`

Generische CI-Anleitung/Template (Platzhalter `MyApp.sln`), keine projektspezifischen Versionsvorgaben; die faktischen Kommandos stehen in den Workflow-Dateien und `CONTRIBUTING.md` (oben).

## Hinweise zur Lösung

- `Reporter.sln` enthält die 5 Projekte unter `src/`. Der Solution-Ordner `Solution Items`/`docs` enthält keine weiteren `.csproj`.
- `dotnet list Reporter.sln package --outdated|--vulnerable|--deprecated` funktionierte lokal **ohne** `-p:IncludeIosTarget=false` (iOS-Workload ist installiert; Restore aller TFMs erfolgreich).
