<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: CI-Lanes, Retry-/Statistik-Integration und Dokumentation

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Skripte | `scripts/lib/Trx.ps1` anlegen | Erledigt | Parser-Check, TRX-Smoke-Tests, `Run-StaticChecks.ps1` |
| 2 | Skripte | `scripts/lib/TestDiscovery.ps1` anlegen | Erledigt | MTP-Discovery `Category=Flaky`/`OsInterface`, Parser-Check |
| 3 | Skripte | `scripts/Retry-FlakyTests.ps1` anlegen | Erledigt | reale CLI-Abnahme gegen OS-TRX; Review-Smoke-Tests für recovered/persistent/indeterminate |
| 4 | Skripte | `scripts/Analyze-FlakyTestResults.ps1` anlegen | Erledigt | reale CLI-Abnahme gegen `TestResults`, Schwellen/Report geprüft |
| 5 | CI | `build-and-test` in `pr-staging-ci.yml` auf `Category!=OsInterface` filtern | Erledigt | Workflow-Diff + lokale MTP-Filterliste 801 Tests |
| 6 | CI | `os-interface tests` in `pr-staging-ci.yml` ergänzen | Erledigt | YAML-/Semantikprüfung; `continue-on-error`, eigene Artefakte, keine Gate-`needs` |
| 7 | CI | `build-and-test` in `staging-ci.yml` auf `Category!=OsInterface` filtern | Erledigt | Workflow-Diff + lokale MTP-Filterliste 801 Tests |
| 8 | CI | `os-interface tests` in `staging-ci.yml` ergänzen | Erledigt | YAML-/Semantikprüfung; `continue-on-error`, eigene Artefakte, keine Gate-`needs` |
| 9 | Doku | `docs/help/tests/os-interface-tests.md` erstellen und verlinken | Erledigt | Seite erstellt; `index.md`, `architektur.md`, `troubleshooting.md`, README verlinken |
| 10 | Doku | README, Testdoku und `changes.log` aktualisieren | Erledigt | `README.md`, `changes.log`, `docs/RELEASE_NOTES.md`, `docs/help/tests/*` aktualisiert |
| 11 | Verifikation | Retry- und Analyzer-Skript lokal mit TRX prüfen | Erledigt | echtes OS-TRX: Retry Exit 0 ohne Fehlschläge, Analyzer 4 stabil |
| 12 | Verifikation | Filter und `Reporter.Tests` vollständig prüfen | Erledigt | Release-Build 0 Warnungen; 805/805 Tests; 801/4 Filter-Mengen; Static Checks Exit 0 |
