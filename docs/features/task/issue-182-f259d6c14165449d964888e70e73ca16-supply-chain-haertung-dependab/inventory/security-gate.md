<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Security-Scan-Gate

## Composite Action `.github/actions/security-scan/action.yml`

Datei: `.github/actions/security-scan/action.yml` (30 Zeilen, PolyForm-Header vorhanden)

### Inputs

| Input | Pflicht | Zweck |
|-------|---------|-------|
| `solution-path` | ja | Solution für `dotnet list package --vulnerable` |
| `artifact-name` | ja | Name des Upload-Artefakts |

### Steps (2)

| Step | Mechanik |
|------|----------|
| `Scan for vulnerable packages` | `shell: bash`; `dotnet list "${{ inputs.solution-path }}" package --vulnerable --include-transitive 2>&1 \| tee vulnerable-packages.txt`; Gate: `grep -qi "has the following vulnerable packages\|Severity"` → `::error::` + `exit 1` |
| `Upload report` | `if: always()`; `uses: actions/upload-artifact@v7` mit `name: ${{ inputs.artifact-name }}`, `path: vulnerable-packages.txt`, `retention-days: 14` |

**Bestand:** Nur NuGet wird geprüft. Es gibt keinen npm-Schritt; `package-lock.json` wird nirgends auditiert.

## Aufrufstellen (3 — alle greifen automatisch auf die Composite-Logik zu)

| Datei | Zeile | `artifact-name` | Kontext |
|-------|-------|-----------------|---------|
| `.github/workflows/pr-staging-ci.yml` | 88 | `vulnerable-packages-pr` | Job `static-checks`, `pull_request → staging` |
| `.github/workflows/staging-ci.yml` | 85 | `vulnerable-packages-staging` | Job `static-checks`, `push → staging` |
| `.github/workflows/security-scan.yml` | 35 | `vulnerable-packages-weekly` | eigener Job, `schedule '0 6 * * 1'` + `workflow_dispatch` |

## Lokale Parität: `scripts/Run-StaticChecks.ps1`

Datei: `scripts/Run-StaticChecks.ps1` (131 Zeilen)

| Funktion | Zeilen | Prüfung | CI-Pendant |
|----------|--------|---------|------------|
| `Invoke-PackageRestore` | 41–49 | `dotnet restore Reporter.sln -r win-x64` | Restore-Step |
| `Invoke-FormatCheck` | 51–59 | `dotnet format --verify-no-changes --no-restore --severity error` | Format-Check-Step |
| `Invoke-SecurityScan` | 61–75 | `dotnet list Reporter.sln package --vulnerable --include-transitive`, Match auf `has the following vulnerable packages\|Severity` | Composite Action `security-scan` |
| `Invoke-LicenseHeaderCheck` | 77–89 | `node scripts/add-license-headers.mjs --check` | License-Header-Step |
| `Invoke-StaticAnalysisBuild` | 91–99 | `dotnet build -c Release --no-restore -p:TreatWarningsAsErrors=true` | Static-Analysis-Step |

- Parameter: `-Check All|Format|Security|Build|LicenseHeaders|Restore`, `-SkipRestore`; setzt `IncludeIosTarget=false`.
- `Invoke-SecurityScan` spiegelt **nur die NuGet-Prüfung** — kein npm-Audit. Erweitert sich das CI-Gate um npm, driftet der lokale Check (Offene Frage 5 der Anforderung).
- AGENTS.md verlangt: `.\scripts\Run-StaticChecks.ps1` muss vor Feature-Abschluss mit Exit-Code 0 ohne Befunde durchlaufen.

## Lizenzheader-Mechanik (relevant für neue `.yml`-Dateien)

Datei: `scripts/add-license-headers.mjs`

- `--check` listet Dateien ohne `Licensed under the PolyForm Noncommercial License 1.0.0 …` und endet mit Exit-Code 1; `.yml`/`.yaml` erhalten den `#`-Stil (`STYLE_BY_EXT`, Zeile 29).
- Prüft repo-weit (auch `docs/features/**` und `.log`-Dateien — siehe [tests.md](tests.md): der aktuelle Lauf schlägt allein wegen Lifecycle-Artefakten fehl).
- Pre-Commit-Hook `.githooks/pre-commit` ruft `add-license-headers.mjs --staged` (nur gestagte Dateien) auf — neue `.yml`-Dateien werden also lokal beim Commit geprüft, in CI vollständig.

## `ci-instructions.md`-Referenz (Doku-Drift)

- §6.2 (Zeilen 492–527) enthält die Generierungsvorlage von `security-scan/action.yml` — identisch mit dem Ist-Stand (nur NuGet-Step + Upload via `actions/upload-artifact@v7`).
- Abschnitt 2 (Zeilen 48–69) beschreibt „The seven workflow files" plus zwei Composite Actions; Ist-Stand hat inzwischen 11 Workflows und 5 Composite Actions — die Vorlage ist bereits älter als die Implementierung (Drift besteht schon heute).
