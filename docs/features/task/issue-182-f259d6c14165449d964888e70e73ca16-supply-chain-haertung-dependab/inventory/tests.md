<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-11-16, ~19:22–19:45 +01:00 (CET)
- **Branch und Commit-ID:** `task/issue-182-f259d6c14165449d964888e70e73ca16-supply-chain-haertung-dependab` @ `f935169f1c21fa19933e0d00c94b8a0532400e9c` (`chore: Architektur- und Sicherheitsreview 2026-11 – Ergebnisse und review-nuget.yml (#183)`)
- **Uncommittete Änderungen im getesteten Stand:** nur das untracked Verzeichnis `docs/features/task/issue-182-…/` (Lifecycle-Artefakte: `requirement.md`, `todo.md`, `inventory/` inkl. der hier gespeicherten Testprotokolle). Keine Änderungen an Produktivcode, Workflows oder Tests.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows, Git Bash; Node.js `v24.15.0`, npm `11.10.0`, .NET SDK `10.0.403`, Workload-Set `10.0.401` (`maui-windows 10.0.20`, `ios 26.5.10318`, `maccatalyst 26.5.10318`, `android 36.1.69`), Python `3.13.14` (inkl. PyYAML für die YAML-Syntaxprüfung).
- **Ermittelte Testsuiten und Quellen der Testbefehle** (aus `CONTRIBUTING.md` Zeilen 21–24 und `AGENTS.md`):
  - `dotnet test Reporter.sln --filter "Category!=E2E"` — Unit-Tests (`src/Reporter.Tests`, xunit) plus nicht-E2E-markierte Tests in `src/Reporter.E2ETests`
  - `npm test` — `node --test "scripts/*.test.mjs"` (3 Testdateien für die Release-Skripte)
  - `.\scripts\Run-StaticChecks.ps1` — lokaler Abgleich des CI-Jobs `static-checks` (Format, Lizenzheader, Security-Scan, Static Analysis)
  - `.\scripts\Run-E2ETests.ps1` — FlaUI-Smoke-Tests, erfordert interaktive Desktop-Session → **nicht ausgeführt** (siehe Testlücken)
  - Ergänzend: YAML-Syntaxprüfung aller `.github/**/*.yml` via `yaml.safe_load` (Verifikationsmuster aus der Anforderung) und `npm audit` als Bestandsaufnahme für das geplante npm-Gate.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `npm test` (`node --test "scripts/*.test.mjs"`) | Repo-Root | 0 | 36 | 0 | 0 | [npm-test.log](test-results/npm-test.log) |
| 2 | `node scripts/add-license-headers.mjs --check` | Repo-Root | 1 | — | — | — | [license-header-check.log](test-results/license-header-check.log) |
| 3 | `python -c` (`yaml.safe_load` über alle 16 `.github/**/*.yml`) | Repo-Root | 0 | 16 Dateien | 0 | 0 | [yaml-syntax-check.log](test-results/yaml-syntax-check.log) |
| 4 | `npm audit` | Repo-Root | 1 | — | — | — | [npm-audit.log](test-results/npm-audit.log) |
| 5 | `powershell -File scripts/Run-StaticChecks.ps1` (`-Check All`, mit Restore) | Repo-Root | 1 | — | — | — | [run-static-checks.log](test-results/run-static-checks.log) |
| 6 | `powershell -File scripts/Run-StaticChecks.ps1 -Check Security -SkipRestore` | Repo-Root | 0 | — | — | — | [run-static-checks-security.log](test-results/run-static-checks-security.log) |
| 7 | `powershell -File scripts/Run-StaticChecks.ps1 -Check Build -SkipRestore` | Repo-Root | 0 | — | — | — | [run-static-checks-build.log](test-results/run-static-checks-build.log) |
| 8 | `IncludeIosTarget=false IncludeAndroidTarget=false dotnet test Reporter.sln -c Release --filter "Category!=E2E"` | Repo-Root | 0 | 806 | 0 | 0 | [dotnet-test-solution.log](test-results/dotnet-test-solution.log) |
| 9 | `node scripts/add-license-headers.mjs --check` (Wiederholung nach Ergänzung der Header in den Protokolldateien) | Repo-Root | 1 | — | — | — | [license-header-check-2.log](test-results/license-header-check-2.log) |

`—` = Check ohne Testfallzählung (Skript-/Tool-Exit-Code).

### Nachgewiesene bestehende Testfehler

Es wurden **keine** fachlichen Testfehler nachgewiesen: `npm test` (36/36) und `dotnet test` (806/806: `Reporter.Tests` 798, `Reporter.E2ETests` 8 nicht-E2E-markierte Tests) sind vollständig grün.

Als **befundende Checks** (keine Testfehler im engeren Sinn) sind festzuhalten:

| Check | Befund | Lauf und Nachweis |
|-------|--------|-------------------|
| Lizenzheader-Check | Schlägt fehl, ausschließlich wegen Dateien im untracked Lifecycle-Verzeichnis `docs/features/task/issue-182-…/` (`todo.md` sowie die von dieser Bestandsaufnahme angelegten `.log`-Protokolle). Alle Produktiv-, Workflow- und Doku-Dateien außerhalb dieses Verzeichnisses tragen den Header — die fehlenden Header betreffen nur die Lifecycle-Artefakte selbst. | Lauf 2 ([license-header-check.log](test-results/license-header-check.log)), Lauf 5, Lauf 9 ([license-header-check-2.log](test-results/license-header-check-2.log)) |
| `Run-StaticChecks.ps1` (kompletter Lauf) | Bricht nach dem Lizenzheader-Schritt ab (obiger Grund); Format-Check und Restore waren zuvor grün. Security und Build separat erfolgreich nachgeholt (Läufe 6 und 7). | Lauf 5 ([run-static-checks.log](test-results/run-static-checks.log)) |
| `npm audit` | Exit 1: **35 moderate** Schwachstellen in transitiven Paketen der `semantic-release`-Toolchain (`glob` GHSA-3g3m-2fjr-6pw4, `minimatch` GHSA-g3hv-75wg-m249, beide unter `node_modules/npm` von `@semantic-release/npm`). **0 hohe/kritische.** | Lauf 4 ([npm-audit.log](test-results/npm-audit.log)) |

### Testlücken und Ausführungsprobleme

- **E2E-Tests nicht ausgeführt:** `src/Reporter.E2ETests` (FlaUI.UIA3) benötigt eine interaktive Desktop-Session (sichtbares Fenster) und wird bewusst über `--filter "Category!=E2E"` ausgeschlossen — identisch zum projektüblichen Befehl und zur CI (dort laufen E2E-Tests gar nicht). Umfang der ausgefilterten E2E-Tests: unbekannt (mit `[Trait("Category", "E2E")]` markierte Tests in `src/Reporter.E2ETests/*.cs`); die 8 nicht markierten Tests des Projekts liefen mit.
- **Keine Tests für Workflows/Composite Actions:** Es existieren keine automatisierten Tests für `.github/workflows/*` oder `.github/actions/*` — Verifikation erfolgt über die YAML-Syntaxprüfung (Lauf 3) und die CI selbst.
- **Kein Build-/Infrastrukturfehler** abgesehen der dokumentierten Lizenzheader-Flaggung der Lifecycle-Artefakte (kein Produktivbefund).

## Testklassen

### `scripts/*.test.mjs` (node:test, 3 Dateien, 36 Tests)

Relevant, weil sie die npm-Toolchain absichern, die künftig auch vom Security-Gate auditiert wird:

- `scripts/create-update-manifest.test.mjs` — Suites `parseReleaseAssets` (3 Tests), `createUpdateManifest` (4), `writeUpdateManifest` (1)
- `scripts/release-assets.test.mjs` — Suites `buildReleaseAssets` (2), `releaseAssetEnv` (1), `emitReleaseAssetEnv` (2)
- `scripts/resolve-release-version.test.mjs` — Suites `parseManualTag` (4), `classifyWorkflowRef` (4), `releaseHasExpectedAsset` (6), `semanticReleaseDryRunVersion` (4), `repairIncompleteRelease` (2), `incompleteReleases` (3)

Es gibt **keine** Tests für `add-license-headers.mjs`, `Run-StaticChecks.ps1`, `Run-E2ETests.ps1` oder `iOS-Deployment.ps1`.

### `src/Reporter.Tests` (xunit, 798 Tests)

Umfangreiche Unit-Tests der Anwendungsklassen (ViewModels, Services, Repositories, Parser). Für diese Anforderung (reine CI/CD-Ebene) fachlich nicht betroffen, aber Teil des projektweiten Ausgangszustands.

### `src/Reporter.E2ETests` (xunit + FlaUI, 8 nicht-E2E-Tests im Ausgangslauf)

UIA3-Smoke-Tests gegen die laufende Windows-App (`REPORTER_APP_PATH`). `[Trait("Category", "E2E")]`-markierte Tests wurden nicht ausgeführt (siehe Testlücken).

## Hilfsmethoden

### `src/Reporter.Tests` (Fakes/Delegating-Implementierungen)

- `FakeAppThemeService`, `FakeAutoRefreshService`, `FakeBackgroundRefreshService`, `FakeBackupExclusionService`, `FakeDebugLogService`, `FakeDeviceInfoProvider`, `FakeEmailService`, `FakeFeedIconService`, `FakeFeedSearchService` u. a. — Test-Doubles für Anwendungstests; für diese Anforderung nicht relevant.
