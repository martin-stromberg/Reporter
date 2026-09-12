← [Zurück zur Übersicht](index.md)

# Release-Management — Installation und Konfiguration

## Voraussetzungen

- GitHub-Repository mit den Branches `main` (Standardbranch) und `staging`.
- Die Workflow-Dateien liegen im Repository; `workflow_run`- und `push → main`-Trigger wirken erst, sobald sie einmalig auf dem Standardbranch `main` angekommen sind (Cold-Start, siehe unten).
- `npm ci`-fähige `package.json`/`package-lock.json` mit der gepinnten `semantic-release`-Toolchain (semantic-release `25.0.9`, `@semantic-release/commit-analyzer` `13.0.1`, `@semantic-release/release-notes-generator` `14.1.1`, `@semantic-release/github` `12.0.9`, `conventional-changelog-conventionalcommits` `9.3.1`).
- .NET SDK `10.0.x` inkl. MAUI-Workloads auf den Runnern (`dotnet workload restore Reporter.sln`); `macos-latest` nur für `package-ios`.
- Commits im Conventional-Commits-Format — nur `fix:`/`feat:`/`BREAKING CHANGE` usw. erzeugen neue Versionen.

## Installationsschritte

1. Workflows auf `staging` mergen und einmalig nach `main` bringen (erster Promotion-PR manuell, siehe Hinweis).
2. Labels prüfen — `automated-promotion` (`0E8A16`) und `automated-backmerge` (`1D76DB`) existieren bereits; die Workflows legen sie sonst per `gh label create --force` beim ersten Lauf an.
3. Optional iOS aktivieren (siehe Konfiguration).
4. Branch-Protection einrichten, sobald der Repository-Plan es zulässt (siehe Konfiguration → Branch-Protection).

## Konfiguration

### Workflow-Permissions (im Code verankert)

| Workflow | Permissions |
|----------|-------------|
| `staging-ci.yml` | `contents: write`, `checks: write`, `pull-requests: write` |
| `release.yml` | `contents: write`, `issues: write`, `pull-requests: write` |
| `staging-to-main-promotion.yml`, `sync-staging-with-main.yml` | `contents: read`, `pull-requests: write`, `issues: write` |
| `pr-staging-ci.yml` | `contents: read`, `checks: write` |

`GITHUB_TOKEN`/`github.token` genügt für Releases, PRs und Labels — keine Personal-Access-Tokens nötig.

### Secrets und Variablen

| Name | Art | Pflicht | Beschreibung |
|------|-----|---------|--------------|
| `IOS_CODESIGN_KEY` | Secret | Nein (nur iOS) | iOS-Signatur-Identität → `-p:CodesignKey` beim `dotnet publish` (Input `codesign-key` der Action `package-ios`) |
| `IOS_PROVISIONING_PROFILE` | Secret | Nein (nur iOS) | Provisioning Profile → `-p:CodesignProvision` (Input `provisioning-profile`) |
| `IOS_SIGNING_ENABLED` | Repository-Variable | Nein | `true` aktiviert `package-ios` und nimmt `release-ios.ipa` in `EXPECTED_ASSETS`/`RELEASE_ASSETS`/`update.json` auf — Aktivierung ohne Codeänderung |

**iOS-Aktivierung:** 1. Beide Secrets unter *Settings → Secrets and variables → Actions → Secrets* hinterlegen. 2. Variable `IOS_SIGNING_ENABLED=true` unter *… → Variables* setzen. Danach bauen `staging-ci.yml` und `release.yml` automatisch `release-ios.ipa` mit.

### Branch-Protection (Admin-Nacharbeit — aktuell nicht eingerichtet)

Das Repository ist privat auf einem Free-Account; die Protection- und Rulesets-APIs antworten mit `HTTP 403` („Upgrade to GitHub Pro or make this repository public"). Sobald das Repository public ist oder Pro/Team aktiv ist, folgende Aufrufe ausführen und per `GET` auf denselben Endpunkten verifizieren:

```powershell
# staging: PR-Pflicht, Required Checks "static checks" + "build & test", "up to date"
gh api -X PUT repos/<owner>/<repo>/branches/staging/protection `
  -f "required_status_checks[strict]=true" `
  -f "required_status_checks[checks][]=static checks" `
  -f "required_status_checks[checks][]=build & test" `
  -F "enforce_admins=true" `
  -f "required_pull_request_reviews[dismiss_stale_reviews]=false" `
  -f "required_pull_request_reviews[require_code_owner_reviews]=false" `
  -f "required_pull_request_reviews[required_approving_review_count]=0" `
  -F "restrictions=null" -F "required_linear_history=false" `
  -F "allow_force_pushes=false" -F "allow_deletions=false"

# main: PR-Pflicht, Required Check "verify-source", Push-Restriktion
gh api -X PUT repos/<owner>/<repo>/branches/main/protection `
  -f "required_status_checks[strict]=true" `
  -f "required_status_checks[checks][]=verify-source" `
  -F "enforce_admins=true" `
  -f "required_pull_request_reviews[required_approving_review_count]=0" `
  -f "restrictions[users][]=<owner>" `
  -F "required_linear_history=false" `
  -F "allow_force_pushes=false" -F "allow_deletions=false"
```

Alternativ über *Settings → Branches → Add branch ruleset* (für private Repos ebenfalls nur mit Pro/Team). Bis dahin ist `verify-pr-source.yml` die einzige automatisierte Absicherung der `staging`-only-Regel für `main`.

### Versions-/Release-Konfiguration im Code

| Parameter | Typ | Standardwert | Beschreibung |
|-----------|-----|--------------|--------------|
| `release.config.js → branches` | Array | `["main"]` | Nur `main` erzeugt stabile Releases; RC-Suffix wird manuell in `staging-ci.yml` angehängt |
| `release.config.js → tagFormat` | String | `v${version}` | Tag-Schema `vX.Y.Z` |
| `RESOLVE_DRY_RUN` | Env | — | `true` schränkt die Pluginliste auf `commit-analyzer` ein (Dry-Run für Versionsermittlung) |
| `RELEASE_ASSET_PATHS` | Env | — | `;`-getrennte Asset-Pfade für `@semantic-release/github` (aus `release-assets.mjs`) |
| `RELEASE_MANIFEST_PATH` | Env | — | Pfad des Update-Manifests (`update.json` im `publish`-Job) |
| `RELEASE_ASSETS` | Env | — | `name:platform:runtimeIdentifier`-Liste, Input für `create-update-manifest.mjs` |
| `RELEASE_VERSION` / `RELEASE_TAG` | Env | — | Version ohne `v` / Tag mit `v` für das Manifest |
| `IncludeAndroidTarget` / `IncludeIosTarget` | MSBuild-Property | `false` / `true` (Windows) | TFM-Schalter in `Reporter.csproj` |
| `COVERAGE_THRESHOLD` | Env | `70` | Coverage-Gate in `build-and-test` |

## Umgebungsvariablen

Die Pipeline liest zusätzlich die GitHub-Standardvariablen `GITHUB_TOKEN`, `GITHUB_REPOSITORY`, `GITHUB_REF_TYPE`, `GITHUB_REF_NAME`, `GITHUB_OUTPUT`, `GITHUB_ENV`, `GITHUB_SHA` — alle werden von den Runnern bzw. Workflows gesetzt, keine manuelle Pflege.

## Überprüfung

1. `npm test` lokal: 36 `node:test`-Tests für `resolve-release-version.mjs`, `release-assets.mjs`, `create-update-manifest.mjs` müssen grün sein.
2. `.\scripts\Run-StaticChecks.ps1` lokal: Format, Security-Scan und Static-Analysis-Build ohne Befund.
3. Rollout-Verifikation: Push auf `staging` → Workflow `Pre-Release` grün → GitHub-Pre-Release `vX.Y.Z-rc.N` mit `release-win-x64.zip`, `release-android.apk`, `update.json` sichtbar → Draft-PR `staging` → `main` mit Label `automated-promotion` geöffnet.
4. Nach Merge: Workflow `Release` grün → stabiles Release `vX.Y.Z` → Backmerge-PR `main` → `staging` mit Label `automated-backmerge` geöffnet (per „Create a merge commit" mergen).
