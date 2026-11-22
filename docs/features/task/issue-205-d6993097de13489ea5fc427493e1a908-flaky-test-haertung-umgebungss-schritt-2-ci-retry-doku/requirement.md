<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Anforderung: CI-Lanes, Retry-/Statistik-Integration und Dokumentation

## Übersetzte Anforderung

Issue #205 verlangt, die aus Schritt 1 vorhandene `OsInterface`-/`Flaky`-Klassifikation in beiden Staging-Workflows umzusetzen: `build-and-test` bleibt die deterministische blockierende Lane, ein separater `os-interface tests`-Job läuft best effort mit `continue-on-error`. Fehlgeschlagene `Flaky`-Tests werden genau einmal gefiltert wiederholt; TRX-Dateien beider Lanes werden getrennt als Artefakte hochgeladen. Ein Report-Schritt wertet Flake-Ereignisse mit dokumentiertem Schwellwert aus. Die E2E-Desktop-Suite bleibt lokal/on-demand und bekommt keinen CI-Job.

## Fachlicher Scope

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | GitHub-Workflow-Jobs, PowerShell-Hilfsskripte, TRX-Testartefakte und Testdokumentation |
| Auslösende Ereignisse | Pull Request gegen `staging` und Push auf `staging` |
| Erwartete Wirkung | Deterministische Tests blockieren; OS-sensitive Tests werden sichtbar, können das Gate aber nicht blockieren; Retry gilt nur für `Flaky` |
| Umgang mit fehlenden Objekten | Keine `Flaky`-Tests → kein Re-Run; fehlendes TRX → kein Retry; fehlende TRX-Statistik → leerer Report |

## Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Kein CI-Job für `Reporter.E2ETests` | Die Suite benötigt eine interaktive Windows-Desktop-Session; ein Self-hosted-/Display-Runner wäre ein größerer separater Scope | bestätigt |
| 2 | Kein automatisches Issue-Create aus dem Analyzer | Der Issue-Text verlangt eine Empfehlung/Schwelle, keinen produktiven Issue-Bot; Anlage bleibt Maintainer-Entscheidung | bestätigt |

## Akzeptanzkriterien

1. `build-and-test` führt `Category!=OsInterface` über MTP aus.
2. Beide Workflows enthalten `os-interface tests` mit `continue-on-error`, eigenem Timeout und `Category=OsInterface`.
3. Die OS-Lane steht nicht in `needs`-Ketten der Gate-/Release-Jobs.
4. `Retry-FlakyTests.ps1` wiederholt nur fehlgeschlagene `Category=Flaky`-Tests genau einmal.
5. `Analyze-FlakyTestResults.ps1` wertet Primär-/Retry-TRX aus und meldet Quarantäne-Kandidaten mit dokumentierten Schwellwerten.
6. TRX-Artefaktnamen von deterministischer und OS-Lane kollidieren nicht.
7. Dokumentation beschreibt Klassifikation, lokale Befehle, Retry-Regel, Job-Rerun-Regel und Flake-Statistik.
