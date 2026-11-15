<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

Geprüft wurde ausschließlich die Workflow-Implementierung (Tasks 1–4) gegen den Plan. Die Dokumentationsaufgaben (Tasks 5–10) sind bewusst offen und werden in Lifecycle-Schritt 9 (update-docs) umgesetzt; die Verifikationsaufgaben 11–12 hängen davon bzw. vom Merge ab.

## Umgesetzte Planelemente

- [x] `.github/workflows/review-architecture.yml` (GitHub-Actions-Workflow) — angelegt; Hash-Lizenzheader in Zeile 1, `name: Scheduled Architecture Review`, Trigger `schedule` (`cron: '0 6 1 * *'`) + `workflow_dispatch` (ohne Inputs), `permissions: issues: write`, Concurrency-Gruppe `review-architecture` mit `cancel-in-progress: false`, Job `create-review-issue` auf `ubuntu-latest` mit `timeout-minutes: 10`, kein `actions/checkout`
- [x] Schritt „Ensure scheduled-architecture-review label exists" in `review-architecture.yml` — `gh label create scheduled-architecture-review --color "5319E7" --description "..." --force` mit `env: GH_TOKEN: ${{ github.token }}`
- [x] Schritt „Create review issue if none open" in `review-architecture.yml` — `set -euo pipefail`, Dedup via `gh issue list --label scheduled-architecture-review --state open --json number` + `jq length`, `gh issue create` mit Titel `Architektur- und Sicherheitsreview $(date -u +'%Y-%m')`, Body wortgleich zum Plan inkl. führender `@softwarewerkstatt`-Erwähnung, Else-Zweig mit Log-Ausgabe statt Issue-Erzeugung
- [x] `.github/workflows/review-nuget.yml` (GitHub-Actions-Workflow) — angelegt; gleiches Gerüst, `name: Scheduled NuGet Review`, Cron `0 6 * * 1`, Concurrency-Gruppe `review-nuget`, Label `scheduled-nuget-review` mit Farbe `0075CA`, Titel `NuGet-Paketprüfung KW $(date -u +'%G-W%V')`, NuGet-Body wortgleich zum Plan
- [x] `.github/workflows/review-compliance.yml` (GitHub-Actions-Workflow) — angelegt; gleiches Gerüst, `name: Scheduled Compliance Review`, Cron `0 6 1 1 *`, Concurrency-Gruppe `review-compliance`, Label `scheduled-compliance-review` mit Farbe `D93F0B`, Titel `Compliance- und Copyright-Review $(date -u +'%Y')`, Compliance-Body wortgleich zum Plan
- [x] Designentscheidungen umgesetzt — kein `actions/checkout`; ausschließlich `issues: write` als Permission; Dedup gilt auch bei `workflow_dispatch` (kein `force`-Input); festes deutsches Titel-Präfix + locale-unabhängige Datumsangabe via `date -u`; `@softwarewerkstatt` nur im Issue-Body (kein `--assignee`); Authentifizierung ausschließlich über `GH_TOKEN: ${{ github.token }}` auf Schrittebene, keine Secrets/Variablen
- [x] YAML-Syntax-Verifikation (Plan Schritt 4 / Task 4) — `python yaml.safe_load` für alle drei Dateien fehlerfrei; `node scripts/add-license-headers.mjs --check` meldet 0 fehlende Header; Cron-Ausdrücke und wörtliche Body-Texte gegen Plan-/Anforderungstexte sichtgeprüft

## Hinweise

- **Außerhalb des Review-Umfangs (bewusst offen, nicht als Lücke bewertet):** Tasks 5–10 (Dokumentationsseite `wiederkehrende-reviews.md` sowie Verlinkungen in `docs/help/release-management/index.md`, `architektur.md`, `installation.md`, `docs/help/index.md` und `README.md`) werden in Lifecycle-Schritt 9 umgesetzt.
- **Task 11** (`Run-StaticChecks.ps1` vollständig) ist nach den Doku-Änderungen auszuführen; der für die Workflow-Dateien relevante `LicenseHeaders`-Teilcheck ist bereits grün.
- **Task 12** (manueller `workflow_dispatch`-Erst- und Zweitlauf zur Dedup-Verifikation) ist erst nach Merge und Promotion auf `main` möglich — planmäßig vorgesehen, keine Implementierungslücke.
- Das Drei-Schritt-Dedup-Muster entspricht exakt dem Referenzmuster aus `staging-to-main-promotion.yml` (Label per `--force` sicherstellen → `gh ... list --state open --json number` → `jq length` → nur bei leerer Liste anlegen).
