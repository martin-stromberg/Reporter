<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestand: Konventionen, lokale Checks und Schutzmechanismen

Detaildokument zur [Bestandsaufnahme](../inventory.md) — verbindliche Konventionen
für neue Dateien sowie die vorhandene Prüf-/Verifikationsinfrastruktur.

## Lizenzheader (verbindlich, CI-erzwungen)

- Jede Workflow-Datei beginnt in Zeile 1 mit:
  `# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.` gefolgt von einer Leerzeile (Bestand: alle 8 Workflow-Dateien, alle 5 `action.yml`).
- Markdown-Dateien verwenden denselben Text als XML-Kommentar:
  `<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->`.
- Enforcement:
  - `scripts/add-license-headers.mjs --check` im Job `static checks` von `staging-ci.yml:82` und `pr-staging-ci.yml:85`. `.yml`-Dateien erhalten den Stil `hash` (`add-license-headers.mjs:29`), `.md` den Stil `xml` (`:33`).
  - `.githooks/pre-commit` ruft `node scripts/add-license-headers.mjs --staged` auf — neue Workflow-/Doku-Dateien werden lokal beim Commit geprüft, sofern die Hooks installiert sind.
  - `scripts/Run-StaticChecks.ps1 -Check LicenseHeaders` führt den Check lokal aus.
- `issue.md` steht in `SKIP_FILES` (`add-license-headers.mjs:24`) und bekommt keinen Header.

## Namens- und Strukturkonventionen

- Workflow-Dateinamen: Kleinbuchstaben mit Bindestrichen (`staging-to-main-promotion.yml`, `verify-pr-source.yml`) — deckt sich mit den in `requirement.md` vorgeschlagenen Namen.
- `name:`-Zeile: englische Klartext-Namen („Security Scan", „Staging to Main Promotion", „Backmerge Main to Staging").
- Job-Namen: `kebab-case` als Job-ID (`static-checks`, `release-gate`), optional lesbarer `name:` (`static checks`, `build & test`) — lesbare Namen werden in Branch-Protection/Required Checks referenziert (`installation.md:50-55`).
- Shell: Standard-`bash` auf `ubuntu-latest`; `shell: bash` wird explizit gesetzt, wenn bash auf Windows-Runnern gebraucht wird (`staging-ci.yml:133`).
- YAML-Stil: 2-Leerzeichen-Einrückung, Trigger-Schlüssel `on:` ohne Anführungszeichen.

## Lokale Prüf- und Schutzinfrastruktur

| Artefakt | Zweck | Relevanz für diese Anforderung |
|----------|-------|-------------------------------|
| `scripts/Run-StaticChecks.ps1` | Lokaler Lauf der CI-`static-checks` (Format, Security-Scan, Static-Analysis-Build, `LicenseHeaders`, `Restore`); per `AGENTS.md` vor Abschluss mit Exit-Code 0 erforderlich | Für eine reine Workflow-/Doku-Änderung ist der `LicenseHeaders`-Teil der relevante Check; Format/Build laufen über die Solution |
| `.githooks/pre-commit` | Branch-Blocker für `main`/`staging`, Übersetzungs-/XML-Doc-/Lizenz-Checks | Blockt Direkt-Commits auf `main`/`staging` — Änderungen kommen nur über Feature-Branch + PR |
| `.github/pull_request_template.md` | PR-Checkliste (Lizenzbestätigung, `Run-StaticChecks.ps1`-Lauf, Tests, Mobile-UI-Regeln) | Wird bei jedem PR gegen `staging` vorgelegt |
| `verify-pr-source.yml` | Lehnt PRs nach `main` ab, die nicht aus `staging` stammen | Bestätigt den Bereitstellungspfad Feature-Branch → `staging` → `main` |
| `staging-to-main-promotion.yml` | Automatischer Draft-Promotion-PR `staging` → `main` | Transportweg für die neuen Workflow-Dateien |

## Tests / Verifikation (Bestand)

- Es gibt **keine Testinfrastruktur für GitHub-Actions-Workflows** — weder
  YAML-Lint noch Workflow-Unit-Tests. `npm test` deckt nur die Node-Release-Skripte
  ab (`scripts/*.test.mjs`: `create-update-manifest`, `release-assets`,
  `resolve-release-version`).
- Verifikationswege im Bestand: manueller `workflow_dispatch`-Lauf, Review der
  YAML-Syntax, dokumentierte Admin-Nacharbeit in
  `docs/help/release-management/installation.md` / `troubleshooting.md`.
- Bekannte Cold-Start-Problematik ist dokumentiert: Trigger wirken erst, wenn die
  Workflow-Datei einmal auf `main` liegt (`installation.md:10`, `troubleshooting.md:23`,
  `beschreibung.md:30`) — gilt für `schedule`-Trigger genauso wie für `workflow_run`.

## Hinweis zum `gh`-Aufruf in Workflows

`gh` ist auf allen GitHub-gehosteten Runnern (`ubuntu-latest`, `windows-latest`,
`macos-latest`) vorinstalliert; `jq` auf `ubuntu-latest` ebenfalls. Die bestehenden
Automatisierungen nutzen beides ohne Installations-Schritt.
