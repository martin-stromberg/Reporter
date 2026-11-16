<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Workflows und externe Action-Referenzen

Bestand unter `.github/workflows/` (11 Dateien) und `.github/actions/` (5 Composite Actions).
Alle YAML-Dateien tragen bereits den PolyForm-Lizenzheader als `#`-Kommentar.
`.github/dependabot.yml` existiert **nicht** (`.github/` enthält nur `actions/`, `pull_request_template.md`, `workflows/`).

## Workflow-Übersicht

| Datei | Workflow-Name | Trigger | Externe `uses:` | Lokale `uses:` |
|-------|---------------|---------|-----------------|----------------|
| `staging-ci.yml` | `Pre-Release` | `push → staging` | 17 | 4 |
| `release.yml` | `Release` | `push → main`, `tags v*.*.*` | 13 | 6 |
| `pr-staging-ci.yml` | `PR CI for Staging` | `pull_request → staging` | 7 | 1 |
| `deploy-pages.yml` | `Deploy Pages` | `push → main` (paths `website/**`), `workflow_dispatch` | 4 | 0 |
| `security-scan.yml` | `Security Scan` | `schedule '0 6 * * 1'`, `workflow_dispatch` | 2 | 1 |
| `staging-to-main-promotion.yml` | `Staging to Main Promotion` | `workflow_run` (`Pre-Release` completed) | 1 | 0 |
| `sync-staging-with-main.yml` | `Backmerge Main to Staging` | `push → main` | 1 | 0 |
| `verify-pr-source.yml` | `Verify PR Source` | `pull_request → main` | 0 | 0 |
| `review-architecture.yml` | `Scheduled Architecture Review` | `schedule '0 6 1 * *'`, `workflow_dispatch` | 0 | 0 |
| `review-nuget.yml` | `Scheduled NuGet Review` | `schedule '0 6 * * 1'`, `workflow_dispatch` | 0 | 0 |
| `review-compliance.yml` | `Scheduled Compliance Review` | `schedule '0 6 1 1 *'`, `workflow_dispatch` | 0 | 0 |
| **Summe Workflows** | | | **45** | **12** |

Composite Actions enthalten weitere 4 externe Referenzen → **49 externe `uses:`-Referenzen insgesamt** (deckt sich mit der Anforderung).

## Externe Referenzen je Datei (Pinning-Bestand)

Alle externen Referenzen nutzen bewegliche Major-Tags (`@vN`), keine Commit-SHAs.
Lokale Referenzen (`uses: ./.github/actions/...`) tragen keinen `@<ref>` und sind nicht pinnbar (Scope-Ausschluss der Anforderung).

### `.github/workflows/staging-ci.yml` — 17 externe, 4 lokale Referenzen

| Zeile | Referenz | Job / Kontext |
|-------|----------|---------------|
| 26 | `actions/checkout@v7` | `detect-backmerge` (`fetch-depth: 0`) |
| 65 | `actions/checkout@v7` | `static-checks` |
| 68 | `actions/setup-dotnet@v6` | `static-checks` (`dotnet-version: '10.0.x'`) |
| 85 | `./.github/actions/security-scan` | `static-checks` (lokal, Gate) |
| 104 | `actions/checkout@v7` | `build-and-test` |
| 107 | `actions/setup-dotnet@v6` | `build-and-test` |
| 153 | `actions/upload-artifact@v7` | `build-and-test` (coverage-report-staging) |
| 161 | `actions/upload-artifact@v7` | `build-and-test` (test-results-staging) |
| 180 | `actions/checkout@v7` | `version` (`fetch-depth: 0`, `fetch-tags`) |
| 186 | `actions/setup-node@v7` | `version` (`node-version: '24'`) |
| 230 | `actions/checkout@v7` | `package-windows` |
| 235 | `./.github/actions/build-and-package` | `package-windows` (lokal) |
| 241 | `actions/upload-artifact@v7` | `package-windows` (`release-win-x64`) |
| 255 | `actions/checkout@v7` | `package-android` |
| 260 | `./.github/actions/package-android` | `package-android` (lokal) |
| 266 | `actions/upload-artifact@v7` | `package-android` (`release-android`) |
| 282 | `actions/checkout@v7` | `package-ios` |
| 287 | `./.github/actions/package-ios` | `package-ios` (lokal) |
| 301 | `actions/upload-artifact@v7` | `package-ios` (`release-ios`) |
| 315 | `actions/checkout@v7` | `prerelease` |
| 318 | `actions/download-artifact@v7` | `prerelease` (`pattern: release-*`) |

### `.github/workflows/release.yml` — 13 externe, 6 lokale Referenzen

| Zeile | Referenz | Job / Kontext |
|-------|----------|---------------|
| 33 | `actions/checkout@v7` | `resolve` |
| 39 | `actions/setup-node@v7` | `resolve` |
| 70 | `actions/checkout@v7` | `release-gate` |
| 73 | `actions/setup-dotnet@v6` | `release-gate` |
| 88 | `actions/checkout@v7` | `package-windows` |
| 95 | `./.github/actions/checkout-release-tag` | `package-windows` (lokal, nur `upload-existing`) |
| 100 | `./.github/actions/build-and-package` | `package-windows` (lokal) |
| 106 | `actions/upload-artifact@v7` | `package-windows` |
| 120 | `actions/checkout@v7` | `package-android` |
| 127 | `./.github/actions/checkout-release-tag` | `package-android` (lokal) |
| 132 | `./.github/actions/package-android` | `package-android` (lokal) |
| 138 | `actions/upload-artifact@v7` | `package-android` |
| 154 | `actions/checkout@v7` | `package-ios` |
| 161 | `./.github/actions/checkout-release-tag` | `package-ios` (lokal) |
| 166 | `./.github/actions/package-ios` | `package-ios` (lokal) |
| 180 | `actions/upload-artifact@v7` | `package-ios` |
| 194 | `actions/checkout@v7` | `publish` |
| 200 | `actions/setup-node@v7` | `publish` |
| 214 | `actions/download-artifact@v7` | `publish` |

### `.github/workflows/pr-staging-ci.yml` — 7 externe, 1 lokale Referenz

| Zeile | Referenz | Job / Kontext |
|-------|----------|---------------|
| 29 | `actions/checkout@v7` | `detect-backmerge` |
| 68 | `actions/checkout@v7` | `static-checks` |
| 71 | `actions/setup-dotnet@v6` | `static-checks` |
| 88 | `./.github/actions/security-scan` | `static-checks` (lokal, Gate) |
| 107 | `actions/checkout@v7` | `build-and-test` |
| 110 | `actions/setup-dotnet@v6` | `build-and-test` |
| 156 | `actions/upload-artifact@v7` | `build-and-test` (coverage-report-pr) |
| 164 | `actions/upload-artifact@v7` | `build-and-test` (test-results-pr) |

### `.github/workflows/deploy-pages.yml` — 4 externe Referenzen

| Zeile | Referenz | Job / Kontext |
|-------|----------|---------------|
| 31 | `actions/checkout@v7` | `deploy` |
| 34 | `actions/configure-pages@v5` | `deploy` |
| 37 | `actions/upload-pages-artifact@v3` | `deploy` (`path: website/`) |
| 43 | `actions/deploy-pages@v4` | `deploy` (`id: deployment`) |

### `.github/workflows/security-scan.yml` — 2 externe, 1 lokale Referenz

| Zeile | Referenz | Kontext |
|-------|----------|---------|
| 21 | `actions/checkout@v7` | Job `security-scan` (`windows-latest`) |
| 24 | `actions/setup-dotnet@v6` | `dotnet-version: '10.0.x'` |
| 35 | `./.github/actions/security-scan` | lokal, `artifact-name: vulnerable-packages-weekly` |

### `.github/workflows/staging-to-main-promotion.yml` — 1 externe Referenz

| Zeile | Referenz | Kontext |
|-------|----------|---------|
| 27 | `actions/checkout@v7` | Checkout von `workflow_run.head_sha` |

### `.github/workflows/sync-staging-with-main.yml` — 1 externe Referenz

| Zeile | Referenz | Kontext |
|-------|----------|---------|
| 19 | `actions/checkout@v7` | Checkout `main` (`fetch-depth: 0`) |

### Workflows ohne `uses:`-Referenzen (unverändert bleiben)

- `verify-pr-source.yml` — nur `run:`-Schritt (prüft `github.head_ref == 'staging'`).
- `review-architecture.yml`, `review-nuget.yml`, `review-compliance.yml` — nur `run:`-Schritte mit `gh` CLI (Label-/Issue-Verwaltung), kein Checkout.

## Externe Referenzen in Composite Actions — 4 Referenzen

| Datei | Zeile | Referenz | Kontext |
|-------|-------|----------|---------|
| `.github/actions/security-scan/action.yml` | 26 | `actions/upload-artifact@v7` | Report-Upload (`if: always()`) |
| `.github/actions/build-and-package/action.yml` | 17 | `actions/setup-dotnet@v6` | `dotnet-version: '10.0.x'` |
| `.github/actions/package-android/action.yml` | 17 | `actions/setup-dotnet@v6` | `dotnet-version: '10.0.x'` |
| `.github/actions/package-ios/action.yml` | 57 | `actions/setup-dotnet@v6` | `dotnet-version: '10.0.1xx'` |
| `.github/actions/checkout-release-tag/action.yml` | — | keine | nur `run:`-Schritt |

Ob die Referenzen *innerhalb* der Composite Actions gepinnt werden, ist eine offene Frage der Anforderung (Empfehlung dort: pinnen — gleicher Supply-Chain-Vektor).

## Distinkte externe Actions

| Action | Referenzierter Tag | Vorkommen (Workflows / Actions / gesamt) |
|--------|--------------------|------------------------------------------|
| `actions/checkout` | `v7` | 21 / 0 / 21 |
| `actions/upload-artifact` | `v7` | 10 / 1 / 11 |
| `actions/setup-dotnet` | `v6` | 6 / 3 / 9 |
| `actions/setup-node` | `v7` | 3 / 0 / 3 |
| `actions/download-artifact` | `v7` | 2 / 0 / 2 |
| `actions/configure-pages` | `v5` | 1 / 0 / 1 |
| `actions/upload-pages-artifact` | `v3` | 1 / 0 / 1 |
| `actions/deploy-pages` | `v4` | 1 / 0 / 1 |
| **Summe** | | **45 / 4 / 49** |

Alle Referenzen stammen ausschließlich aus dem `actions/*`-Namensraum (GitHub-eigene Actions) — keine Drittanbieter-Actions.
