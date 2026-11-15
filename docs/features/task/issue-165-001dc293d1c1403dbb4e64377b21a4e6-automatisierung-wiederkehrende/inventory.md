<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews

Analysiert wurde die CI/CD-Infrastruktur des Repositories (`.github/workflows/`, `.github/actions/`, `scripts/`, `docs/help/`) bezogen auf die Anforderung aus `requirement.md` (Issue #165): drei neue zeitgesteuerte Workflows, die dedupliziert Issues mit `@softwarewerkstatt`-Erwähnung anlegen. Die Anforderung betrifft **keinen** C#-Code — Datenmodell, Services, Interfaces, Enums und UI bleiben unberührt.

## Zusammenfassung

- **Schedule-Vorlage existiert genau einmal:** `security-scan.yml` ist der einzige Workflow mit `schedule` (`cron: '0 6 * * 1'`, Montag 06:00 UTC) + `workflow_dispatch`. Für reine `gh`-CLI-Logik genügt `ubuntu-latest` (Vorbild: Promotion-/Backmerge-Workflows), kein .NET-Setup nötig.
- **Dedup-/Label-Muster ist direkt übertragbar:** `staging-to-main-promotion.yml` und `sync-staging-with-main.yml` etablieren exakt das geforderte Muster — Label per `gh label create --force` idempotent sicherstellen, offene Objekte per `gh ... list --state open --json` + `jq` prüfen, nur bei leerer Liste anlegen. Auth läuft einheitlich per `env: GH_TOKEN: ${{ github.token }}` auf Schrittebene.
- **Labels:** Live im Repo existieren nur `automated-promotion` (`0E8A16`) und `automated-backmerge` (`1D76DB`) neben den GitHub-Defaults (via `gh label list` verifiziert). Ein Review-Label (z. B. `automated-review`) existiert noch nicht und würde — wie die beiden anderen — vom Workflow selbst beim ersten Lauf angelegt.
- **Issue-Erzeugung ist Neuland im Bestand:** `gh issue create`/`gh issue list` kommen bisher nirgends vor; es gibt kein `ISSUE_TEMPLATE`-Verzeichnis und kein Vorbild für `issues: write` als alleinige Permission (bisher immer gebündelt mit `pull-requests: write`/`contents`).
- **Lizenzheader ist Pflicht und CI-erzwungen:** Jede neue `.yml`-Datei braucht die Hash-Kopfzeile, jede neue `.md`-Datei den XML-Kommentar — geprüft durch `node scripts/add-license-headers.mjs --check` in `staging-ci.yml`/`pr-staging-ci.yml` sowie lokal via `.githooks/pre-commit` und `scripts/Run-StaticChecks.ps1`.
- **Konventionen sind nicht ganz uniform:** `concurrency` und `timeout-minutes` fehlen in `sync-staging-with-main.yml`, `verify-pr-source.yml`, `deploy-pages.yml`, `security-scan.yml`; `verify-pr-source.yml` hat gar keinen `permissions:`-Block; `set -euo pipefail` wird uneinheitlich verwendet. Die gängigere Variante (permissions-Block + `concurrency` + `timeout-minutes` + GH_TOKEN pro Schritt) ist in den Automatisierungs-Workflows am stärksten ausgeprägt.
- **Bereitstellungspfad bestätigt:** `.githooks/pre-commit` blockt Direkt-Commits auf `main`/`staging`; `verify-pr-source.yml` erzwingt PRs nach `main` nur aus `staging`; der Promotion-Workflow transportiert Änderungen `staging` → `main`. Die Cold-Start-Problematik (Trigger wirken erst ab `main`, Cron nur auf dem Default-Branch) ist bereits in `docs/help/release-management/` dokumentiert.
- **Kein Bot-Mechanismus im Repo:** `@softwarewerkstatt` kommt nur in `issue.md`/`requirement.md` vor — die Erwähnung ist reiner Issue-Text; eine darauf reagierende Automatisierung liegt außerhalb dieses Repositories.
- **Dokumentationskonvention:** Themenverzeichnisse unter `docs/help/` mit `index.md` + Rückverweis-Kopfzeile, Registrierung in `docs/help/index.md` und `README.md` („Weitere Informationen"). `docs/help/release-management/` dokumentiert bereits die `gh`-Automatisierungen und enthält Tabellen zu Workflow-Permissions — naheliegender Ablageort (Alternativen siehe Detaildokument).
- **Keine Workflow-Tests vorhanden:** Verifikation läuft im Bestand über manuellen `workflow_dispatch`, YAML-Review und dokumentierte Admin-Nacharbeit — deckt sich mit der Annahme in `requirement.md`.

## Details

- [Bestand: GitHub-Actions-Workflows und Composite Actions](inventory/workflows.md) — alle 8 Workflows mit Triggern, Permissions, Concurrency, Runnern und Timeouts; die 5 Composite Actions; verwendete Action-Versionen; was im Bestand fehlt.
- [Bestand: `gh`-CLI-, Label- und Duplikat-Muster](inventory/automatisierungs-muster.md) — das etablierte Drei-Schritt-Muster mit Zeilenreferenzen, live verifizierte Label-Liste, Übertragbarkeit auf `gh issue`.
- [Bestand: Konventionen, lokale Checks und Schutzmechanismen](inventory/konventionen-checks.md) — Lizenzheader-Pflicht und -Enforcement, Namenskonventionen, `Run-StaticChecks.ps1`, Git-Hooks, PR-Template, Bereitstellungspfad.
- [Bestand: Dokumentationsstruktur und Ablageorte](inventory/dokumentation.md) — `docs/help/`-Aufbau, Seitenkonventionen, passfähige Ablageorte, Verlinkungs-Pflichten in `index.md`/`README.md`.
