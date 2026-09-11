# Aufgabenliste – Anforderungsbearbeitung

Branch: `task/issue-26-a32dbfb7fca140d88d166629e421fbe8-einstellungen-aufbewahrungsdau`

| Status | Schritt | Beschreibung | Artefakt |
|--------|---------|--------------|----------|
| [x] | 1 | Branch-Name ermitteln | – |
| [x] | 2 | Verzeichnisstruktur vorbereiten | `docs/features/task/issue-26-a32dbfb7fca140d88d166629e421fbe8-einstellungen-aufbewahrungsdau/` |
| [x] | – | Einstiegspunkt ermitteln (Schritt 3) | – |
| [x] | 3 | Anforderung übersetzen (Unteragent) | `requirement.md` |
| [x] | 4 | Bestandsaufnahme (Unteragent) | `inventory.md`, `inventory/` |
| [x] | 5 | Umsetzungsplanung (Unteragent) | `plan.md` |
| [x] | 5a | Offene Punkte prüfen und ggf. Planung wiederholen | `plan.md` (aktualisiert) |
| [x] | 5b | Plan gegen Anforderung und Testbedarf prüfen (Unteragent, 2 Läufe) | `plan-check.md` |
| [x] | 5c | Planungscommit | Commit `86d9106` |
| [x] | 6 | Implementierung (Unteragent, 2 Iterationen + Fortsetzungslauf) | Codeänderungen |
| [x] | 7 | Plan-Review (Unteragent, bedingt) | `review.md` — `Vollständig umgesetzt` |
| [x] | 8 | Usability-Review (Unteragent, bei UI-Änderungen) | `review-usability.md` — letzte Iteration 2 Restbefunde |
| [x] | 9 | Code-Review (Unteragent) | `review-code.md` — letzte Iteration 1 Restbefund |
| [x] | 10 | Tests ausführen (Unteragent) | `test-results.md` — `Keine Fehler` (144/144) |
| [x] | – | Iteration oder Abschluss entscheiden (Iter. 2: 6 ≥ 6 → Abbruch; Forts.-Iter. 2: 3 ≥ 3 → Abbruch) | – |
| [x] | 11 | Folgeaufgaben dokumentieren (bei Schleifenabbruch) | `continue.md` (3 Restbefunde) |
| [x] | 12 | Dokumentation erstellen (Unteragent) | `docs/help/einstellungen/` + `docs/help/anwendung/` |
| [x] | 12b | README aktualisieren (Unteragent) | `README.md` |
| [x] | 12c | Release Notes aktualisieren (Unteragent) | `docs/RELEASE_NOTES.md` |
| [ ] | 13 | Nacharbeiten abschließen (offene Punkte aus `continue.md`) | `continue-done.md` — nicht erreicht: 5 Restbefunde bleiben |
| [ ] | – | Feature-Verzeichnis löschen | – (nicht ausgeführt: `continue.md` vorhanden) |
| [x] | – | Commit durchführen | Commits `4ac9e69`, `94da958`, `2c454aa` |

## Hinweis zum Abbruch

Die Implementierungs-/Review-Schleifen wurden zweimal regelkonform wegen ausbleibendem
Fortschritt abgebrochen (6 → 6, dann 3 → 3, zuletzt 1 → 5 — jede frische Review-Runde
findet neue Kleinigkeiten). Verbleibende Punkte (alle Schweregrad niedrig): `continue.md`.
