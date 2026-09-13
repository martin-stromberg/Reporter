<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Aufgabenliste – Anforderungsbearbeitung

Branch: `task/issue-59-efbace56737047f4b10610b83d0e137c-erweiterung-der-hinzufuegenfun`

Neue Anforderung (2. Lauf auf diesem Branch): Hinzufügen-Formular einklappbar/hinter „+"-Button, Titel-Eingabe entfällt (Dateiname als Fallback), Kontextmenü-Aktionen „Umbenennen" und „Kategorie ändern".

| Status | Schritt | Beschreibung | Artefakt |
|--------|---------|--------------|----------|
| [x] | 1 | Branch-Name ermitteln | – |
| [x] | 2 | Verzeichnisstruktur vorbereiten | `docs/features/{branchname}/` |
| [x] | – | Einstiegspunkt ermitteln (Schritt 3 — Artefakte wurden nach Lauf 1 gelöscht) | – |
| [x] | 3 | Anforderung übersetzen (Unteragent) | `requirement.md` |
| [x] | 4 | Bestandsaufnahme (Unteragent) | `inventory.md`, `inventory/` |
| [x] | 5 | Umsetzungsplanung (Unteragent) | `plan.md` |
| [x] | 5a | Offene Punkte prüfen und ggf. Planung wiederholen | `plan.md` (aktualisiert, 5 Antworten eingearbeitet) |
| [x] | 5b | Plan gegen Anforderung und Testbedarf prüfen (Unteragent) | `plan-check.md` (Plan vollständig, 3. Iteration) |
| [x] | 5c | Planungscommit | Commit 813c271 |
| [x] | 6 | Implementierung (Unteragent) | Codeänderungen (Listenansicht + Bottom-Sheet, `FeedTitleFallback`, Umbenennen/Kategorie, 303 Tests grün, Static Checks Exit 0, manuelle UI-Verifikation 390×844 Dark+Light) |
| [x] | 7 | Plan-Review (Unteragent, bedingt) | `review.md` (Vollständig umgesetzt) |
| [x] | 8 | Usability-Review (Unteragent, bei UI-Änderungen) | `review-usability.md` (3 Befunde) |
| [x] | 9 | Code-Review (Unteragent) | `review-code.md` (8 Befunde) |
| [x] | 10 | Tests ausführen (Unteragent) | `test-results.md` (Keine Fehler) |
| [x] | – | Iteration oder Abschluss entscheiden | It. 1: 11 offene Punkte → It. 2 → 9 offene Punkte → It. 3 → 3 offene Punkte, Iterationsmaximum erreicht → Abbruch |
| [x] | 11 | Folgeaufgaben dokumentieren (bei Schleifenabbruch) | `continue.md` (3 Restbefunde aus `review-code.md` It. 3) |
| [ ] | 12 | Dokumentation erstellen (Unteragent) | `docs/help/` |
| [ ] | 12b | README aktualisieren (Unteragent) | `README.md` |
| [ ] | 12c | Release Notes aktualisieren (Unteragent) | `docs/RELEASE_NOTES.md` |
| [ ] | – | Feature-Verzeichnis löschen | – |
| [ ] | – | Commit durchführen | – |
