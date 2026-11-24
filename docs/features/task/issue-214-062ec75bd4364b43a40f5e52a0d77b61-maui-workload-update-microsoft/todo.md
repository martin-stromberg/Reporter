<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Aufgabenliste – Anforderungsbearbeitung

Branch: `task/issue-214-062ec75bd4364b43a40f5e52a0d77b61-maui-workload-update-microsoft`
Basisbranch: `staging`

| Status | Schritt | Beschreibung | Artefakt |
|--------|---------|--------------|----------|
| [x] | 1 | Branch-Name ermitteln | – |
| [x] | 2 | Verzeichnisstruktur vorbereiten | `docs/features/{branchname}/` |
| [x] | – | Einstiegspunkt ermitteln (keine Artefakte → Schritt 3) | – |
| [x] | 3 | Anforderung übersetzen (Unteragent) | `requirement.md` |
| [x] | 4 | Bestandsaufnahme (Unteragent) | `inventory.md`, `inventory/` |
| [x] | 5 | Umsetzungsplanung (Unteragent) | `plan.md` |
| [x] | 5a | Offene Punkte prüfen und ggf. Planung wiederholen | `plan.md` (aktualisiert) |
| [x] | 5b | Plan gegen Anforderung und Testbedarf prüfen (Unteragent) | `plan-check.md` |
| [x] | 5c | Planungscommit | – |
| [ ] | 6 | Implementierung (Unteragent) — ZURÜCKGESTELLT: externer Blocker, kein Workload-Manifest ≥ 11.0.10 (Publish-Lag); Anwender-Entscheidung 2026-11-24: zurückstellen bis zum Manifest (Option 1) | `continue.md` |
| [ ] | 7 | Plan-Review (Unteragent, bedingt) | `review.md` |
| [ ] | 8 | Usability-Review (Unteragent, bei UI-Änderungen) | `review-usability.md` |
| [ ] | 9 | Code-Review (Unteragent) | `review-code.md` |
| [ ] | 10 | Tests ausführen inkl. Sichtprüfung der Abnahme-Screenshots (Unteragent) | `test-results.md` |
| [ ] | – | Iteration oder Abschluss entscheiden | – |
| [ ] | 11 | Folgeaufgaben dokumentieren (bei Schleifenabbruch) | `continue.md` |
| [ ] | 12 | Dokumentation erstellen (Unteragent) | `docs/help/` |
| [ ] | 12b | README aktualisieren (Unteragent) | `README.md` |
| [ ] | 12c | Release Notes aktualisieren (Unteragent) | `docs/RELEASE_NOTES.md` |
| [ ] | – | Feature-Verzeichnis löschen | – |
| [ ] | – | Commit durchführen | – |
