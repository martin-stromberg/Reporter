<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestand: `gh`-CLI-, Label- und Duplikat-Muster

Detaildokument zur [Bestandsaufnahme](../inventory.md) — die im Repository etablierten
Automatisierungsmuster, die für die Duplikatprüfung und Issue-Erzeugung relevant sind.

## `gh`-CLI-Nutzung

| Muster | Wo | Details |
|--------|-----|---------|
| Auth per Env | `staging-to-main-promotion.yml:47-48`, `sync-staging-with-main.yml:34-35`, `staging-ci.yml:335-336`, `release.yml:240-262` | `env: GH_TOKEN: ${{ github.token }}` wird **pro Schritt** gesetzt (nicht Job-/Workflow-weit). `release.yml:55` nutzt alternativ `GITHUB_TOKEN: ${{ github.token }}` für ein Node-Skript. |
| Label idempotent anlegen | `staging-to-main-promotion.yml:49`, `sync-staging-with-main.yml:36` | `gh label create <name> --color "<RRGGBB>" --description "<text>" --force` — `--force` macht den Aufruf wiederholbar, ohne dass das Label vorab existieren muss. |
| Duplikatprüfung vor Create | `staging-to-main-promotion.yml:56-57`, `sync-staging-with-main.yml:43-44` | `existing="$(gh pr list --state open --json number)"`, dann `if [ "$(echo "$existing" \| jq length)" -eq 0 ]` — `jq` ist auf `ubuntu-latest` vorinstalliert und wird ohne Setup-Schritt benutzt. |
| Objekt anlegen | `staging-to-main-promotion.yml:58-63`, `sync-staging-with-main.yml:45-49` | `gh pr create` mit `--base`, `--head`, `--title`, `--body`, `--label`; optional `--draft`. `gh issue create` kommt im Bestand **nicht** vor. |
| Release-Verwaltung | `staging-ci.yml:340-344`, `release.yml:242,251-253,262` | `gh release create/edit/upload --clobber` — für diese Anforderung nicht relevant, zeigt aber denselben `GH_TOKEN`-Stil. |

Die beiden Automatisierungs-Workflows laufen auf `ubuntu-latest` **ohne Checkout**
des Repository-Inhalts für die `gh`-Operationen (`staging-to-main-promotion.yml`
checkt nur für die Git-Diff-Ermittlung aus; reine gh-Logik bräuchte keinen
`actions/checkout`-Schritt).

## Vorhandene Labels (live im Repository verifiziert via `gh label list`)

| Label | Farbe | Beschreibung | Erstellt durch |
|-------|-------|--------------|----------------|
| `automated-promotion` | `0E8A16` | Automated PR from staging to main | Workflow `staging-to-main-promotion.yml` beim ersten Lauf |
| `automated-backmerge` | `1D76DB` | Automated PR from main to staging | Workflow `sync-staging-with-main.yml` beim ersten Lauf |
| `accessibility`, `bug`, `documentation`, `duplicate`, `enhancement`, `good first issue`, `help wanted`, `invalid`, `question`, `wontfix` | GitHub-Defaults | Standard-Labelset des Repositories | GitHub |

- Es existiert **noch kein** Label für automatisierte Review-Issues (kein
  `automated-review` o. ä.) — ein neues Label müsste analog per
  `gh label create --force` im Workflow selbst angelegt werden.
- Namenskonvention der Automatisierungs-Labels: `automated-<zweck>`, Kleinbuchstaben
  mit Bindestrich, Hex-Farbe ohne `#`.

## Übertragbares Muster für die Issue-Duplikatprüfung

Das etablierte Drei-Schritt-Muster (Label sicherstellen → Liste offener Objekte
prüfen → nur bei leerer Liste anlegen) ist direkt auf Issues übertragbar:

- `gh pr list --state open --json number` → `gh issue list --state open --json number,title,labels`
  (Issue-Listen liefern dieselbe JSON-Struktur; Filterung per `--label` oder Titel-Vergleich möglich).
- `gh pr create --label` → `gh issue create --title --body --label`.
- Erkennungsmerkmal im Bestand ist die **Label-Variante** — Titel-Präfix-Vergleiche
  kommen nirgends vor.

## Einschraenkungen / Beobachtungen

- Die Dedup-Bedingung in `sync-staging-with-main.yml:33,39` vergleicht einen
  String-Output mit einer Zahl (`steps.diff.outputs.commits_behind > 0`) — funktioniert
  per Expression-Koerzion, ist aber unsauber; in `staging-to-main-promotion.yml`
  sauber gelöst (`!= '0'`).
- Mehrzeilige Bash-Blöcke nutzen uneinheitlich `set -euo pipefail` (gesetzt in
  `staging-ci.yml:338`, `release.yml:249`; **nicht** gesetzt in den
  Label-/PR-Dedup-Schritten).
- Die Automatisierungs-PRs sind **Drafts** (`--draft` nur beim Promotion-PR);
  Issues haben keinen Draft-Zustand — für sie gibt es kein Bestands-Vorbild.
