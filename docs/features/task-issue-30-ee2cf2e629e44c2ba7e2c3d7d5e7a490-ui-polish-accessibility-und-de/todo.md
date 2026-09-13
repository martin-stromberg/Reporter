<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Aufgabenliste – Anforderungsbearbeitung

Branch: `task/issue-30-ee2cf2e629e44c2ba7e2c3d7d5e7a490-ui-polish-accessibility-und-de`

| Status | Schritt | Beschreibung | Artefakt |
|--------|---------|--------------|----------|
| [x] | 1 | Branch-Name ermitteln | – |
| [x] | 2 | Verzeichnisstruktur vorbereiten | `docs/features/{branchname}/` |
| [x] | – | Einstiegspunkt ermitteln (Schritt 3 — `requirement.md` fehlt) | – |
| [x] | 3 | Anforderung übersetzen (Unteragent) | `requirement.md` |
| [x] | 4 | Bestandsaufnahme (Unteragent) | `inventory.md`, `inventory/` |
| [x] | 5 | Umsetzungsplanung (Unteragent) | `plan.md` |
| [x] | 5a | Offene Punkte prüfen und ggf. Planung wiederholen | `plan.md` (aktualisiert) |
| [x] | 5b | Plan gegen Anforderung und Testbedarf prüfen (Unteragent) | `plan-check.md` |
| [x] | 5c | Planungscommit | – |
| [x] | 6 | Implementierung (Unteragent) — Iteration 1 | Codeänderungen |
| [x] | 7 | Plan-Review (Unteragent, bedingt) — `Vollständig umgesetzt` | `review.md` |
| [x] | 8 | Usability-Review (Unteragent) — `Befunde vorhanden` (3) | `review-usability.md` |
| [x] | 9 | Code-Review (Unteragent) — `Befunde vorhanden` (4) | `review-code.md` |
| [x] | 10 | Tests ausführen (Unteragent) — Iteration 1: 347 grün, 1 E2E-Befund, ausstehende Verifikationen | `test-results.md` |
| [x] | – | Iteration oder Abschluss entscheiden — 3 Iterationen, Abbruch → continue.md | – |
| [x] | 6 | Implementierung (Unteragent) — Iteration 3 | Codeänderungen |
| [x] | 7–10 | Reviews+Tests Iteration 3 — usability: 0, code: 1 Befund, tests: 350 grün, ausstehende Umgebungs-Verifikationen | `review-usability.md`, `review-code.md`, `test-results.md` |
| [x] | 6 | Implementierung (Unteragent) — Iteration 2 | Codeänderungen (alle 8 Review-Befunde behoben; 348 Tests grün; Static Checks 0; UIA-Fix verifiziert, s. `test-results.md` „Iteration 2") |
| [x] | 11 | Folgeaufgaben dokumentieren (bei Schleifenabbruch) | `continue.md` |
| [x] | 12 | Dokumentation erstellen (Unteragent) | `docs/help/` |
| [x] | 12b | README aktualisieren (Unteragent) | `README.md` |
| [x] | 12c | Release Notes aktualisieren (Unteragent) | `docs/RELEASE_NOTES.md` |
| [x] | 6–10 | Fortsetzungslauf (continue.md): Implementierung + Reviews + Tests — usability: `Keine Befunde`, code: `Keine Befunde`, tests: 350 grün, Static Checks 0 | `review-usability.md`, `review-code.md`, `test-results.md` |
| [ ] | 13 | Nacharbeiten abschließen (offene Punkte aus `continue.md`) — lösbare Punkte erledigt, Umgebungs-Verifikationen weiterhin offen | `continue-done.md` |
| [ ] | – | Feature-Verzeichnis löschen | – |
| [x] | – | Commit durchführen (`5971f84`) | – |
