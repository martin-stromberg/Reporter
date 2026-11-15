<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestand: GitHub-Actions-Workflows und Composite Actions

Detaildokument zur [Bestandsaufnahme](../inventory.md) — Auflistung aller bestehenden
CI/CD-Artefakte unter `.github/` mit ihren Triggern, Permissions und Mustern.

## Workflows (`.github/workflows/`, 8 Dateien)

| Datei | Anzeigename (`name:`) | Trigger | `permissions:` | `concurrency` | Runner | `timeout-minutes` |
|-------|-----------------------|---------|----------------|---------------|--------|-------------------|
| `security-scan.yml` | Security Scan | `schedule` (`cron: '0 6 * * 1'`, Montag 06:00 UTC) + `workflow_dispatch` | `contents: read` | — (kein Block) | `windows-latest` | 15 |
| `staging-ci.yml` | Pre-Release | `push` → `staging` | `contents: write`, `checks: write`, `pull-requests: write` | `staging-ci`, `cancel-in-progress: true` | `ubuntu-latest` / `windows-latest` / `macos-latest` (je Job) | 10–30 je Job |
| `pr-staging-ci.yml` | PR CI for Staging | `pull_request` → `staging` (`opened`, `synchronize`, `reopened`) | `contents: read`, `checks: write` | `pr-staging-${{ github.event.pull_request.number }}`, `cancel-in-progress: true` | `ubuntu-latest` / `windows-latest` | 20–30 je Job |
| `release.yml` | Release | `push` → `main` + Tags `v*.*.*` | `contents: write`, `issues: write`, `pull-requests: write` | `release`, `cancel-in-progress: false` | `ubuntu-latest` / `windows-latest` / `macos-latest` | 15–30 je Job |
| `staging-to-main-promotion.yml` | Staging to Main Promotion | `workflow_run` (Workflow „Pre-Release", `completed`, Branch `staging`) | `contents: read`, `pull-requests: write`, `issues: write` | `staging-to-main-promotion`, `cancel-in-progress: false` | `ubuntu-latest` | 15 |
| `sync-staging-with-main.yml` | Backmerge Main to Staging | `push` → `main` | `contents: read`, `pull-requests: write`, `issues: write` | — (kein Block) | `ubuntu-latest` | — (nicht gesetzt) |
| `verify-pr-source.yml` | Verify PR Source | `pull_request` → `main` | — (kein `permissions:`-Block) | — (kein Block) | `ubuntu-latest` | — (nicht gesetzt) |
| `deploy-pages.yml` | Deploy Pages | `push` → `main` (nur `website/**` und eigene Datei) + `workflow_dispatch` | `contents: read`, `pages: write`, `id-token: write` | `pages`, `cancel-in-progress: false` | `ubuntu-latest` | — (nicht gesetzt) |

**Feststellungen:**

- `security-scan.yml` ist der **einzige bestehende Workflow mit `schedule`/Cron-Trigger** — er ist die einzige Vorlage für zeitgesteuerte Läufe. `workflow_dispatch` kommt zusätzlich in `deploy-pages.yml` vor.
- `issues: write` ist in `release.yml`, `staging-to-main-promotion.yml` und `sync-staging-with-main.yml` bereits vergeben — in den beiden Letzteren offenbar wegen `gh label create`/`gh pr create` (Label-Anlage erfordert Issue-Schreibrecht).
- `concurrency` und `timeout-minutes` sind **nicht durchgängig** gesetzt: `sync-staging-with-main.yml`, `verify-pr-source.yml`, `deploy-pages.yml` und `security-scan.yml` verzichten ganz oder teilweise darauf.
- `verify-pr-source.yml` ist der einzige Workflow **ohne `permissions:`-Block**.

## Relevante Jobs im Detail

### `security-scan.yml` — einziger Schedule-Vorläufer

- Ein einzelner Job `security-scan`, `runs-on: windows-latest` (wegen .NET-Build, nicht wegen `gh`).
- Schritte: `actions/checkout@v7` → `actions/setup-dotnet@v6` (`dotnet-version: '10.0.x'`) → `dotnet workload restore` → `dotnet restore` → Composite Action `./.github/actions/security-scan`.
- Läuft wöchentlich montags 06:00 UTC; erzeugt ein Artefakt `vulnerable-packages-weekly` (kein Issue).

### `staging-to-main-promotion.yml` — Vorlage für „Label sicherstellen → Duplikat prüfen → anlegen"

- Job `promote` mit `if: github.event.workflow_run.conclusion == 'success'`.
- Schritt „Ensure automated-promotion label exists" (Zeile 45–49):
  `gh label create automated-promotion --color "0E8A16" --description "Automated PR from staging to main" --force` mit `env: GH_TOKEN: ${{ github.token }}`.
- Schritt „Create promotion pull request" (Zeile 51–64): Duplikatprüfung per
  `gh pr list --base main --head staging --state open --json number` + `jq length`,
  `gh pr create --draft --label automated-promotion` nur bei leerer Liste.

### `sync-staging-with-main.yml` — zweites Exemplar desselben Musters

- Identisches Label-/List-/Create-Muster mit Label `automated-backmerge` (`--color "1D76DB"`),
  `gh pr create` **ohne** `--draft` (Zeilen 32–50).
- Kein `concurrency`, kein `timeout-minutes`.

### `staging-ci.yml` — Ort des Lizenzheader-Gates

- Job `static checks` (Zeilen 54–91) enthält den Schritt `License header check`:
  `node scripts/add-license-headers.mjs --check` — dieser Check erfasst auch neue
  Workflow-Dateien (`.yml` → Hash-Kommentarstil).
- Job `prerelease` (Zeilen 307–344) zeigt `gh release create` mit `GH_TOKEN` und
  `set -euo pipefail` in mehrzeiligen Bash-Skripten.

## Composite Actions (`.github/actions/`)

| Verzeichnis | Zweck |
|-------------|-------|
| `security-scan/action.yml` | `dotnet list package --vulnerable --include-transitive`, Artefakt-Upload via `actions/upload-artifact@v7`; Inputs `solution-path`, `artifact-name` |
| `build-and-package/action.yml` | Windows-Publish → `release-win-x64.zip` |
| `package-android/action.yml` | Android-Publish → `release-android.apk` |
| `package-ios/action.yml` | iOS-Publish auf `macos-latest` → `release-ios.ipa` |
| `checkout-release-tag/action.yml` | Detach-Checkout eines Release-Tags (Asset-Repair) |

Für reine Issue-Erzeugung ist keine Composite Action erforderlich — die bestehenden
Automatisierungen verwenden Inline-`run:`-Bash mit `gh`.

## Verwendete Action-Versionen (Bestand)

`actions/checkout@v7`, `actions/setup-dotnet@v6` (`.NET 10.0.x`), `actions/setup-node@v7`
(Node `24`), `actions/upload-artifact@v7` / `actions/download-artifact@v7`
(`retention-days: 14`), `actions/configure-pages@v5`, `actions/upload-pages-artifact@v3`,
`actions/deploy-pages@v4`. Versionen sind überall per Major-Tag (`@vN`) gepinnt, nicht per SHA.

## Nicht vorhanden

- Kein Workflow erzeugt bisher GitHub-**Issues** (`gh issue create` kommt im gesamten
  Bestand nicht vor — nur `gh pr`, `gh label`, `gh release`).
- Kein `.github/ISSUE_TEMPLATE/`-Verzeichnis, kein `dependabot.yml`, kein `nuget.config`,
  kein `Directory.Packages.props` (kein Central Package Management).
- Kein Account-/Bot-Mechanismus für `@softwarewerkstatt` im Repository — die Erwähnung
  wäre ausschließlich Text im Issue-Body; eine Reaktion darauf ist außerhalb des Repos
  angesiedelt (kommt nur in `issue.md`/`requirement.md` vor).
