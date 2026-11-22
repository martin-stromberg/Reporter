<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: CI-Lanes, Retry-/Statistik-Integration und Dokumentation

## Übersicht

Beide Staging-Workflows erhalten eine deterministische Test-Lane ohne `OsInterface` und eine separate, sichtbare Best-Effort-Lane für OS-sensitive Tests. PowerShell-Hilfsskripte übernehmen Flaky-Discovery, genau einen gefilterten Re-Run und die TRX-Flake-Statistik.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | `Reporter.Tests`-CI-Lanes und deren TRX-Artefakte |
| Auslösende Ereignisse | PR gegen `staging`, Push auf `staging`, Fehlschlag im OS-Testschritt |
| Erwartete Wirkung | OS-sensitive Fehler werden gemeldet und statistisch ausgewertet, blockieren aber nicht |
| Umgang mit fehlenden Objekten | Kein `Flaky`-Test oder kein TRX → kein Retry; keine TRX → leerer Analyzer-Report |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Kein `Reporter.E2ETests`-CI-Job | Interaktive Desktop-Session erforderlich | bestätigt |
| 2 | Keine automatische Quarantäne-Issue-Erstellung | Analyzer liefert Empfehlung; Issue-Text fordert nur Report/Schwelle | bestätigt |

## Programmabläufe

### Deterministische Lane

1. `build-and-test` baut wie bisher.
2. `dotnet test` läuft mit `-- --filter-not-trait "Category=OsInterface"` plus Coverlet/TRX.
3. Das bisherige Coverage-Gate und `test-results-*` bleiben blockierend.

### OS-Interface-Lane

1. Neuer Job `os-interface tests` läuft unter Windows mit `continue-on-error: true`.
2. Der Job baut die Solution und führt `Reporter.Tests` mit `-- --filter-trait "Category=OsInterface"` und `test-results-os-interface.trx` aus.
3. Bei Fehlschlag ruft `Retry flaky tests once` `scripts/Retry-FlakyTests.ps1` auf.
4. `Analyze flaky test results` läuft `if: always()` über `TestResults`.
5. `test-results-os-interface-*` lädt nur `TestResults/*os-interface*.trx` hoch.

### Retry/Statistik

1. `Get-TrxTestResults` liest Primär- und Retry-TRX.
2. `Get-FlakyTestNames` ermittelt `Category=Flaky` per MTP-VSTest-Filter.
3. `Retry-FlakyTests` normalisiert Theory-Namen, schneidet Fehlschläge mit Flaky-Menge und führt genau einen gefilterten Re-Run aus.
4. `Analyze-FlakyTestResults` zählt fail→retry-pass und fail→nächster-Run-pass, klassifiziert `stabil`, `beobachten`, `Quarantaene-Kandidat`.

## Änderungen an bestehenden Dateien

- `.github/workflows/pr-staging-ci.yml`: Filter im Test-Step, neuer OS-Job.
- `.github/workflows/staging-ci.yml`: gleiche Änderung.
- `docs/help/tests/index.md`, `architektur.md`, `beschreibung.md`, `README.md`, `changes.log`: Lanes, Retry und Statistik dokumentieren.

## Neue Dateien

| Datei | Zweck |
|-------|-------|
| `scripts/lib/Trx.ps1` | TRX-Parsing |
| `scripts/lib/TestDiscovery.ps1` | MTP-Testliste `Category=Flaky` |
| `scripts/Retry-FlakyTests.ps1` | genau ein gefilterter Flaky-Re-Run |
| `scripts/Analyze-FlakyTestResults.ps1` | Flake-Statistik und Schwellen |
| `docs/help/tests/os-interface-tests.md` | Betreiber-/Entwicklerdoku |

## Tests

- Lokale Skriptprüfung mit realem OS-TRX: Retry-Skript Exit 0 bei keinen Fehlschlägen; Analyzer erzeugt Report.
- MTP-Filterliste `Category!=OsInterface`/`Category=OsInterface` erneut verifizieren.
- `dotnet test Reporter.Tests` vollständig.
- `scripts/*.test.mjs` bleibt unverändert; `npm test` ausführen.
- YAML durch Review und `git diff --check` prüfen; Workflow-Ausführung erfolgt später über PR-Checks.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Deterministische Lane | PR/Push | `Reporter.Tests` mit 4 `OsInterface`-Tests | Build + gefilterter Test | 801 blockierende Tests, keine OS-Tests | Workflow-Befehl + lokale Filterliste |
| 2 | OS-Best-Effort-Lane | PR/Push | Kategorien aus Schritt 1 | eigener `continue-on-error`-Job | 4 OS-Tests laufen separat; Gate bleibt frei | Workflow-Befehl |
| 3 | Kein Flaky vorhanden | OS-Testschritt fehlschlägt oder TRX wird lokal geprüft | keine `Category=Flaky`-Tests | Retry-Skript | „keine Flaky-Fehlschläge“, kein Blanket-Retry | lokale Skriptausführung |
| 4 | Statistik | `if: always()` | Primär-/Retry-TRX | Analyzer | Tabelle stabil/beobachten/Quarantäne | lokale Skriptausführung |

## Offene Punkte

Keine.
