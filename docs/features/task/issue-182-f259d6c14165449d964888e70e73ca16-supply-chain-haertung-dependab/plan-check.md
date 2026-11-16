<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

Die `requirement.md` enthält keinen formal nummerierten Abschnitt „Akzeptanzkriterien"; die Kriterien wurden aus `Fachlicher Scope` → `Erwartete Wirkung`, `Umgang mit fehlenden Objekten` und `Betroffene Klassen und Komponenten` abgeleitet.

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| `.github/dependabot.yml` neu mit `version: 2`, drei `updates`-Einträgen (`nuget`, `npm`, `github-actions`), je `directory: "/"`, `schedule.interval: weekly`, `target-branch: "staging"` | Scope-Klärung (fehlende Objekte: erzeugen); Abschnitt „Änderungen" → `dependabot.yml`; Umsetzungsschritt 2; Programmablauf „Dependabot-Update-Lauf" | Akzeptanzszenario 1 (Happy Path: Promotion → Dependabot-Lauf → PRs gegen `staging`); Umsetzungsschritt 10 (manuelle Verifikation nach Merge/Promotion, dokumentiert in `test-results.md`); `yaml.safe_load` (Tests-Tabelle, Schritt 8) | Abgedeckt |
| Alle 49 externen `uses:`-Referenzen (45 in 7 Workflows, 4 in Composite Actions) auf Commit-SHAs gepinnt, Tag als `# vN`-Kommentar; 12 lokale Referenzen unverändert | Designentscheidungen (Pinning-Reichweite, -Schreibweise); Programmablauf „SHA-Pinning"; Dateitabelle mit exakter Referenzverteilung (17/13/7/4/2/1/1 + 4 Composite); Umsetzungsschritte 1, 3, 4 | Akzeptanzszenario 4 (Pinning-Vollständigkeit); `grep`-Nachprüfung in Validierungsregeln, Tests-Tabelle und Schritt 3; `yaml.safe_load` | Abgedeckt |
| Security-Scan-Gate schlägt zusätzlich bei verwundbaren npm-Abhängigkeiten aus `package-lock.json` fehl; Audit-Ergebnis landet im Scan-Report-Artefakt; greift automatisch an allen 3 Aufrufstellen | Designentscheidungen (`--package-lock-only`, `--audit-level=high`, getrennte `npm-audit.txt` im bestehenden Artefakt); Programmablauf „Security-Scan-Lauf mit npm-Gate"; Änderungsabschnitt `security-scan/action.yml`; Umsetzungsschritt 5 | Akzeptanzszenario 2 (Gate grün inkl. Artefakt mit beiden Reports) und 3 (Fehlerfall rot bei `high`/`critical`, Negativprobe via `--audit-level=moderate`); Umsetzungsschritt 9 (lokale Gate-Simulation, Exit 0 / Exit 1); CI-Lauf in Schritt 10 | Abgedeckt |
| Dependabot hält gepinnte SHAs aktuell (`github-actions`-Ökosystem erfasst Workflows *und* `action.yml`-Dateien) | `dependabot.yml`-Spezifikation (`github-actions`, `directory: "/"`); Designentscheidung Pinning-Reichweite; Seiteneffekt „Pinning-Erhaltung" (fachlich notwendige Bedingung) | Akzeptanzszenario 1 (Actions-PRs heben SHAs + `# vN`-Kommentare an); Schritt 10 | Abgedeckt |
| Neue/geänderte `.yml`-Dateien tragen den PolyForm-Lizenzheader (`#`-Kommentar) | Scope-Klärung (fehlende Objekte: Header erzeugen); `dependabot.yml`-Spezifikation Zeile 1; Validierungsregeln | `node scripts/add-license-headers.mjs --check` in Tests-Tabelle und Schritt 8 | Abgedeckt |
| Lokale Parität: `scripts/Run-StaticChecks.ps1` spiegelt das erweiterte Gate (Offene Frage 5 → entschieden: erweitern) | Designentscheidung „Lokale Parität"; Programmablauf „Lokaler Static-Check-Lauf"; Änderungsabschnitt `Run-StaticChecks.ps1` (`Invoke-SecurityScan`); Umsetzungsschritt 6 | Tests-Tabelle (`Run-StaticChecks.ps1` Exit-Code 0 inkl. neuem npm-Teil, AGENTS.md-Pflicht in Schritt 8); identische Befehlszeile/Schwelle wie CI-Gate | Abgedeckt |
| Dokumentation nachgezogen (Komponententabelle, Gate-Beschreibung, Dependabot↔Review-Issues, `ci-instructions.md`-Drift) | Änderungsabschnitt „Dokumentation" (4 Dateien: `architektur.md`, `ablauf-technisch.md`, `wiederkehrende-reviews.md`, `ci-instructions.md`); Designentscheidung `ci-instructions.md`; Umsetzungsschritt 7 | Indirekt über Gesamtlauf `Run-StaticChecks.ps1` (Lizenzheader) und Schritt-7-Voraussetzung „Schritte 2–6 abgeschlossen" | Abgedeckt |
| Scope-Ausschlüsse der Anforderung eingehalten (kein Auto-Merge, kein Pinning lokaler Referenzen, keine Versionsupdates, keine weiteren Ökosysteme, keine Trigger-/Permissions-Änderungen, Review-Issues bleiben) | Scope-Ausschluss-Tabelle (6 Einträge, alle mit Begründung und Status `bestätigt`); Ausschluss 3 konkretisiert die Versions-Sperre auf die 35 moderaten npm-Befunde | Verifikation implizit über `grep`-Nachprüfung (lokale `uses:` unverändert) und Gate-Schwelle `--audit-level=high` | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

— (Status `Plan vollständig`; keine fehlenden Testanforderungen festgestellt.)

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Dependabot-Update-Lauf (wöchentlich, PRs gegen `staging`) | Kein E2E-Test — Nachweis über realen Dependabot-Lauf nach Promotion + Dependabot-Tab („Check for updates"), dokumentiert in `test-results.md` (Akzeptanzszenario 1, Schritt 10) | Nicht erforderlich mit Begründung: kein UI-/Nutzerfluss der App; Akteur ist GitHub Dependabot, nicht durch FlaUI-Suite abbildbar (Abschnitt „E2E-Tests" begründet nachvollziehbar) |
| Security-Scan-Lauf mit npm-Gate (grün/rot, Artefakt) | Kein E2E-Test — Nachweis über realen `staging-ci`-/PR-Lauf inkl. Artefakt-Download (Akzeptanzszenarien 2+3, Schritt 10) plus lokale Gate-Simulation (Schritt 9) | Nicht erforderlich mit Begründung: Pipeline-Verhalten, kein Endanwender-Ablauf; die CI selbst ist der Funktionsnachweis |
| SHA-Pinning-Migration | Kein E2E-Test — Nachweis über `grep`-Vollständigkeitsprüfung, `yaml.safe_load`, Lizenzheader-Check (Akzeptanzszenario 4) | Nicht erforderlich mit Begründung: statische Konfigurationsänderung ohne Laufzeit-Nutzerfluss |
| Lokaler Static-Check-Lauf (Parität) | Kein E2E-Test — Nachweis über `Run-StaticChecks.ps1` Exit-Code 0 (Tests-Tabelle, Schritt 8) | Nicht erforderlich mit Begründung: Entwickler-Tooling, kein App-Fluss |

Die E2E-Begründung im Plan (kein über UI oder Nutzeraktion erreichbarer Ablauf; FlaUI-Suite kann Pipeline-Verhalten nicht abbilden) ist nachvollziehbar und korrekt — die Anforderung berührt ausschließlich CI/CD-Konfiguration.

## Fehlende oder unvollständige Planbestandteile

— (Status `Plan vollständig`; alle geprüften Planbestandteile vorhanden: Scope-Klärung vollständig, 6 Scope-Ausschlüsse mit Begründung + Status, Designentscheidungen zu allen 6 offenen Fragen der Anforderung, Programmabläufe mit beteiligten Komponenten, 10-schrittige Umsetzungsreihenfolge mit Voraussetzungen, Seiteneffekte/Risiken, Validierungsregeln, Konfigurationsänderungen, Akzeptanzszenarien mit vollständigem Happy Path inkl. Wartesituation und Fehlerfall, entschiedener Offener Punkt.)

## Hinweise

1. **Kleinere Inkonsistenz in Schritt 5:** Die Voraussetzung „Node/npm auf dem Runner (… wird in Schritt 9 verifiziert)" verweist auf die lokale Gate-Simulation — die Verfügbarkeit auf dem `windows-latest`-Runner kann erst in Schritt 10 (realer CI-Lauf) verifiziert werden. Der Fallback (`actions/setup-node` gepinnt in den Aufrufer-Jobs) ist in den Seiteneffekten korrekt benannt; nur die Schritt-Referenz ist beim Umsetzen richtigzustellen. Kein Vollständigkeitsmangel.
2. **Optionale Doku-Dateien nicht adressiert:** Die Anforderung nennt „ggf. `README.md`/`docs/help/index.md`" — der Plan trifft dazu keine Aussage. Da die Aufnahme in der Anforderung optional („ggf.") ist, kein Mangel; beim Umsetzen kurz prüfen, ob `index.md` die `release-management`-Doku automatisch einbindet.
3. **Lizenzheader-Check auf Lifecycle-Artefakte:** Laut Bestandsaufnahme schlägt `add-license-headers.mjs --check` aktuell auf den untracked Lifecycle-Dateien (`todo.md`, `.log`-Protokolle) fehl. Damit Schritt 8 (`Run-StaticChecks.ps1` Exit 0) erreicht wird, müssen auch diese Artefakte Header erhalten bzw. vor dem Lauf bereinigt sein.
4. **Wirksamkeit von `dependabot.yml`:** Erst nach Promotion `staging → main` aktiv (im Plan als Seiteneffekt korrekt benannt) — bei der Abnahme von Akzeptanzszenario 1 den zeitlichen Versatz berücksichtigen.
