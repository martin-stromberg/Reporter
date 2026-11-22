<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Abnahmeprüfung – Entwicklungsschritt 2

Geprüft: `git diff task/issue-205-d6993097de13489ea5fc427493e1a908-flaky-test-haertung-umgebungss...HEAD` (Commit `5857874`, 14 Dateien, +856/−6) sowie die relevanten Quelldateien. Zusätzlich empirische Verifikation der MTP-Filter und des Retry-Skripts mit dem gebauten Testprojekt (.NET SDK 11.0.100, pwsh 7.6.10).

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

### Verifikation je Anforderungspunkt

- **Blockierende deterministische Lane:** `build-and-test` in `.github/workflows/pr-staging-ci.yml` (Zeilen 123–130) und `staging-ci.yml` (Zeilen 120–127) bleibt unverändert blockierend (kein `continue-on-error`, unverändert in den `needs` der Gate-/Release-Jobs) und schließt `OsInterface` per MTP-Filter `-- --filter-not-trait "Category=OsInterface"` aus. Empirisch geprüft: `--list-tests` mit diesem Filter meldet 801 Tests, `Category=OsInterface` genau die 4 `TempDirectoryCleanupTests`.
- **Separater `os-interface tests`-Job:** In beiden Workflows vorhanden (pr-staging-ci.yml Zeilen 174–237, staging-ci.yml Zeilen 171–234) mit `continue-on-error: true`, eigenem `timeout-minutes: 45` (vs. 30 in `build-and-test`), Filter `--filter-trait "Category=OsInterface"`, eigenem TRX `test-results-os-interface.trx`, eigenen Artefakten `test-results-os-interface-pr`/`-staging` (Glob `TestResults/*os-interface*.trx`, erfasst auch `-flaky-retry.trx`, Retention 14 d). `needs` enthält nur `detect-backmerge`; kein Gate-/Release-Job (`version`, `package-*`, `prerelease`) referenziert den Job — in `staging-ci.yml` bleibt `version` bei `needs: [detect-backmerge, static-checks, build-and-test]`.
- **Genau ein gefilterter Retry, kein Blanket-Retry:** Der Schritt „Retry flaky tests once" läuft nur `if: failure()` und ruft `scripts/Retry-FlakyTests.ps1` auf. Das Skript bildet die Schnittmenge „fehlgeschlagen im Primär-TRX ∩ `Category=Flaky`-Menge" und startet genau einen Re-Run über `--filter "(Category=Flaky)&(FullyQualifiedName~…|…)"`. End-to-end geprüft mit fabriziertem Fehlschlag-TRX: 4 Fehlschläge ohne Flaky-Markierung → „keine Flaky-Fehlschlaege", Exit 0.
- **Skripte:** `scripts/Retry-FlakyTests.ps1`, `scripts/Analyze-FlakyTestResults.ps1`, `scripts/lib/Trx.ps1` (Namespace-/XPath-kapselndes TRX-Parsing inkl. `<Times start>`-Zeitstempel) und `scripts/lib/TestDiscovery.ps1` (`--list-tests -- --filter-trait "Category=Flaky"`) sind vollständig implementiert. Ehrliches Fehlverhalten: persistente/unbestimmbare Retry-Ergebnisse → Exit 1 mit `::warning::`/`::error::`-Annotationen; fehlendes TRX, nicht löschbares Stale-Retry-TRX und leere Flake-Menge sind dokumentiert und korrekt umgesetzt. Schwellwerte dokumentiert (Skript-Parameter + Doku-Tabelle): Fenster 20 Läufe, Quarantäne ≥ 2 Flake-Ereignisse oder ≥ 20 % Fehlrate bei ≥ 5 Ausführungen, Beobachtung ≥ 1 Ereignis oder ≥ 10 %.
- **MTP-Kompatibilität:** Projekt läuft über xUnit v3 (`xunit.v3` 4.0.3, `Microsoft.Testing.Extensions.TrxReport`) mit MTP-Runner (`global.json` → `"test": { "runner": "Microsoft.Testing.Platform" }`). Empirisch verifiziert: `--filter-trait`, `--filter-not-trait`, `--filter "(Category=OsInterface)&(FullyQualifiedName~X)"` und `--report-trx --report-trx-filename` (TRX landet in `TestResults/` relativ zum Ausführungsverzeichnis) funktionieren unter `dotnet test -- …`; ein leerer Trait-Filter meldet MTP-Exit-Code 8 (in Doku und Skript-Kommentar berücksichtigt; `$PSNativeCommandUseErrorActionPreference` ist `false`, der List-Lauf wirft daher nicht).
- **E2E-Abgrenzung:** Kein Workflow referenziert `Reporter.E2ETests` oder `Run-E2ETests.ps1`; `os-interface-tests.md` §„Warum `Reporter.E2ETests` nicht in der CI läuft" (Zeilen 129–131) begründet die lokale/on-demand-Rolle (interaktive Desktop-Session).
- **Dokumentation:** `docs/help/tests/os-interface-tests.md` (135 Zeilen) deckt Klassifikation, lokale MTP-Befehle, CI-Lane-Trennung, Retry-/Re-Run-Regeln (inkl. `gh run rerun --job`-Konvention), Flake-Statistik und Quarantäne-Schwellwerte ab; verlinkt aus `index.md`, `architektur.md`, `troubleshooting.md` (neuer Abschnitt) und `README.md`; `changes.log` und `docs/RELEASE_NOTES.md` sind aktualisiert.

### Beobachtungen ohne Abweichungscharakter

- Die Tasks-Datei `docs/features/task/…-schritt-2-ci-retry-doku-tasks.md` steht weiterhin komplett auf „Offen" (Schritt-1-Datei wurde auf „Erledigt" samt Testnachweisen aktualisiert). Reine Lifecycle-Buchführung, kein Anforderungspunkt — sollte im Abschluss des Schritts nachgezogen werden.
- `if: failure()` am Retry-Schritt greift bewusst auch bei Build-/Restore-Fehlschlägen; das Skript beendet sich dann ehrlich mit „keine Flaky-Fehlschlaege" (Exit 0), sodass der ursprünglich fehlgeschlagene Schritt das führende Signal bleibt — im Workflow-Kommentar und in der Doku dokumentiert.
- Der Analyzer statistiert pro rohem TRX-`testName` (Theory-Datenzeilen einzeln), während der Retry auf Methoden-FQN normalisiert und damit alle Datenzeilen einer fehlgeschlagenen Methode einmalig mitwiederholt — dokumentiertes, konsistentes Verhalten, kein Blanket-Retry.
- `staging-ci.yml` reagiert nur auf Push nach `staging`; die Lane ist daher wie in `project-plan-check.md` vermerkt erst nach Merge empirisch im Staging-Lauf nachweisbar — die Semantik ist deckungsgleich zur PR-Lane.
- Lizenzheader (PolyForm) sind in allen neuen `.ps1`/`.md`-Dateien vorhanden und entsprechen dem Format von `scripts/add-license-headers.mjs` (`hash`-Stil).
