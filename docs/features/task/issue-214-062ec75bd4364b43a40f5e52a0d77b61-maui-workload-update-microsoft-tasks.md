<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Issue #214 „MAUI-Workload-Update: Microsoft.Maui.Controls 11.0.0 → 11.0.10"

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Konfiguration | Workload-Set-Version des Bands 11.0.1xx mit `maui-windows ≥ 11.0.10` ermitteln (`dotnet workload`-CLI/`dotnet workload list`) | Offen | — |
| 2 | Konfiguration | `sdk.workloadVersion` in `global.json` eintragen; `dotnet workload restore Reporter.sln` im Repo-Kontext gegen den Pin verifizieren | Offen | — |
| 3 | CI | Diagnoseschritt `Log installed workloads` (`dotnet --version` + `dotnet workload list`) an allen drei Restore-Stellen in `.github/workflows/pr-staging-ci.yml` einfügen | Offen | — |
| 4 | CI | Gleichen Diagnoseschritt an allen drei Restore-Stellen in `.github/workflows/staging-ci.yml` einfügen | Offen | — |
| 5 | CI | Gleichen Diagnoseschritt in `.github/workflows/security-scan.yml` nach `Install .NET MAUI workload` einfügen | Offen | — |
| 6 | CI | Gleichen Diagnoseschritt in `.github/actions/build-and-package/action.yml` einfügen (`shell: bash`) | Offen | — |
| 7 | CI | Gleichen Diagnoseschritt in `.github/actions/package-android/action.yml` einfügen (`shell: bash`) | Offen | — |
| 8 | Lokale Umgebung | `dotnet workload update` auf dem SDK-Band 11.0.x ausführen (Fallback: Visual Studio Installer bei VS-verwalteten Workloads); `dotnet workload list` ≥ `maui-windows 11.0.10` verifizieren | Offen | — |
| 9 | Lokale Umgebung | Lokalen `ios`/`maccatalyst`-Stand gegen CI-Pin `27.2.11039` prüfen; bei Drift per Rollback-File (`--from-rollback-file` + `--skip-manifest-update`) wiederherstellen oder Abweichung dokumentieren | Offen | — |
| 10 | Dokumentation | `docs/help/entwicklung/maui-workload-updates.md` anlegen (`$(MauiVersion)`-Auflösung, `sdk.workloadVersion`-Pin + Bump-Verfahren, lokaler Update-Pfad inkl. VS-Installer-Fallback, iOS-Pin-Interaktion, Bezug zu `review-nuget`) — mit Lizenzheader | Offen | — |
| 11 | Dokumentation | Eintrag in `docs/help/entwicklung/index.md` und Querverweis im Dependabot-Abschnitt von `docs/help/release-management/wiederkehrende-reviews.md` ergänzen | Offen | — |
| 12 | Verifikation | Lokale Verifikation: `dotnet restore` + Release-Build (`TreatWarningsAsErrors`), `dotnet test` (Unit + OsInterface), `dotnet list package --outdated` ohne Maui-Befund | Offen | — |
| 13 | Verifikation | `.\scripts\Run-StaticChecks.ps1` mit Exit-Code 0 ohne Findings (alle neuen Dateien mit PolyForm-Header) | Offen | — |
| 14 | Verifikation | Commit mit `fix:`-Präfix + `Closes #214`, PR gegen `staging` öffnen, PR-Checks mit `gh pr checks --watch --fail-fast` beobachten; `dotnet workload list`-Log im PR-Lauf auf ≥ 11.0.10 prüfen | Offen | — |
| 15 | Verifikation | Nach dem Merge `staging-ci.yml` verfolgen: `version.changed == 'true'`, `package-windows`/`package-android` (+ `package-ios` bei aktivem Signing) mit Artefakten, `prerelease` erstellt GitHub-Pre-Release | Offen | — |
