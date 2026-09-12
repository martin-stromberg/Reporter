# Umsetzungsplan: Release-Management, Pre-Releases und Staging-Promotion

## Übersicht

Die vorhandene, teilweise deaktivierte GitHub-Actions-Release-Pipeline (`staging` → RC-Pre-Release → Promotion-PR → `main` → stabiles Release → Backmerge-PR) wird reaktiviert und gegen die Vorlage `ci-instructions.md` (Abschnitte 4, 5, 6.1, 8, 9, 11) vervollständigt. Betroffen sind ausschließlich CI-/Infrastruktur-Artefakte: die Workflows unter `.github/workflows/`, die Composite Actions unter `.github/actions/`, das Release-Tooling (`release.config.js`, `package.json`, `package-lock.json`, `scripts/resolve-release-version.mjs` plus neue Skripte/Tests) und `src/Reporter/Reporter.csproj` (zusätzliches Android-TFM). Es gibt keine C#-Produktivcode-Änderungen und keine Datenbankmigrationen.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Mehrplattform-Build-Topologie | Getrennte `package-<plattform>`-Jobs pro Zielplattform plus ein Aggregations-Job, der alle Artefakte per `actions/download-artifact` einsammelt, das Manifest erzeugt und das Release anlegt | Eine Composite Action läuft zwingend im Runner des aufrufenden Jobs; ein iOS-Publish benötigt `macos-latest`, kann also nicht im bisherigen `windows-latest`-Job stecken. Getrennte Jobs laufen zudem parallel; die Aggregation hält Release-Erzeugung und Manifest an genau einer Stelle. |
| `update.json`-Erzeugung | Neues Node-Skript `scripts/create-update-manifest.mjs`, aufgerufen im Aggregations-Job | Das Manifest kann erst entstehen, wenn alle Plattform-Artefakte vorliegen — es kann daher nicht mehr in der Windows-Composite-Action `build-and-package` erzeugt werden. Ein Node-Skript ist konsistent mit `resolve-release-version.mjs`, läuft auf `ubuntu-latest` ohne Zusatztools und ist unit-testbar. |
| Android-TFM-Aktivierung | Neuer MSBuild-Schalter `IncludeAndroidTarget` (Default `false`), analog zum bestehenden `IncludeIosTarget` | Damit bleiben lokale Builds, `Run-StaticChecks.ps1` und die Gates (`static-checks`, `build-and-test`) ohne Android-Workload lauffähig; nur die Packaging-Jobs setzen den Schalter. |
| Semantic-Release-Branch-Modell | `release.config.js`: `branches: ["main"]`; RC-Suffix weiterhin manuell in `staging-ci.yml` (`rc_version`) | Vorlagenkonform (Abschnitt 8.4); der Dry-Run nutzt bereits `--branches staging` als Override. Die bisherige Doppelkonfiguration (`{ name: "staging", prerelease: "rc" }`) würde RC-Suffixe zweimal vergeben (semantic-release-Prerelease + manuell). |
| Windows-Artefaktform | Weiterhin `release-win-x64.zip` (unpackaged, `WindowsPackageType` bleibt `None`) — **entschieden** | Bestehendes, bereits produktiv genutztes Asset-Format; ein `.msix` erforderte `WindowsPackageType`-Umstellung plus Signierung ohne erkennbaren Mehrwert im Issue-Scope. |
| Struktur `release.yml` | Aufteilung des bisherigen Einzeljobs in `resolve` → (`release-gate`, `package-windows`, `package-android`, `package-ios`) → `publish` | Die Plattform-Jobs benötigen die vom Resolver ermittelte Version/den Tag, bevor das Release erzeugt wird; ein einzelner sequentieller Job kann nicht plattformübergreifend (`windows-latest` + `macos-latest`) laufen. |
| iOS-Artefakt-Aktivierung | `package-ios`-Job und `package-ios`-Action werden angelegt, aber per Repository-Variable `vars.IOS_SIGNING_ENABLED == 'true'` gated — **entschieden** | Secrets können in `if:`-Bedingungen nicht referenziert werden; eine `vars.*`-Variable ist der saubere Schalter. Ohne Signierungs-Secrets ist kein verteilbares `.ipa` erzeugbar — die Secrets werden später extern bereitgestellt; die Aktivierung erfolgt dann ausschließlich durch Setzen der Variable, ohne Codeänderung. |
| Verbindliche Asset-Liste | Verbindlich: `release-win-x64.zip`, `release-android.apk`, `update.json`; konditional: `release-ios.ipa` nur bei `vars.IOS_SIGNING_ENABLED == 'true'`; `maccatalyst` nicht im Scope — **entschieden** | Das Issue nennt die Plattformen nur exemplarisch („etc."); Windows + Android decken die verteilbaren Zielplattformen ab, iOS ist bis zur Signierung nicht verteilbar. Damit `EXPECTED_ASSETS`, `RELEASE_ASSETS` und `RELEASE_ASSET_PATHS` synchron bleiben, wird der iOS-Eintrag überall konditional über denselben Schalter ergänzt (s. u. `resolve-release-version.mjs` und Workflow-Env). |
| Release-Manifest `update.json` | `update.json` wird weiterhin als Release-Asset erzeugt; `release-metadata.json` wird **nicht** erzeugt — **entschieden** | Das Issue-Akzeptanzkriterium verlangt `update.json` explizit; `release-metadata.json` ist in der Vorlage (Abschnitt 7.2) an einen referenzierten `IInstalledVersionProvider`/`msTools.Updater` gebunden, der im Repo nicht existiert — ein ungenutztes zweites Manifest wäre spekulative Zusatzarbeit. |
| Branch-Protection und Labels | Werden im Umsetzungsschritt vom ausführenden Agenten direkt per `gh api` gesetzt und per GET verifiziert — **entschieden**, keine Admin-Bypass-Ausnahme | `gh` ist in der Ausführungsumgebung bereits mit repo-Scope authentifiziert; das Vorgehen ist damit direkt ausführbar statt nur dokumentiert. Die dokumentierten `gh api`-Aufrufe bleiben im Plan festgehalten (Nachvollziehbarkeit/Wiederholbarkeit). |

## Programmabläufe

### RC-Pre-Release auf `staging` (Push → `staging-ci.yml`)

1. Push auf `staging` triggert `staging-ci.yml` (`concurrency: staging-ci`, `cancel-in-progress: true`).
2. Job `detect-backmerge` (`ubuntu-latest`) prüft, ob der Head-Commit ein reiner Backmerge von `main` ist (Merge-Parent == `origin/main` oder Baum identisch via `git diff --quiet origin/main HEAD`) und setzt Output `is_backmerge`.
3. Jobs `static-checks` (`static checks`) und `build-and-test` (`build & test`) laufen parallel auf `windows-latest`, jeweils `needs: detect-backmerge` und `if: needs.detect-backmerge.outputs.is_backmerge != 'true'` — unveränderte Inhalte (Format-Check, `security-scan`-Action, Static Analysis; Build, `dotnet test` auf `src/Reporter.Tests/Reporter.Tests.csproj` mit `coverlet.runsettings`, Coverage-Gate 70 %, Artefakt-Uploads).
4. Job `version` (`ubuntu-latest`, `needs: [detect-backmerge, static-checks, build-and-test]`, `if: needs.detect-backmerge.outputs.is_backmerge != 'true'`) ermittelt per `npx semantic-release --dry-run --no-ci --branches staging` (mit `RESOLVE_DRY_RUN: 'true'`) die nächste Version und die RC-Nummer aus `git tag --list "v<version>-rc.*"`; Outputs `changed`, `version`, `rc_tag`, `rc_version` bleiben unverändert.
5. Jobs `package-windows` (`windows-latest`), `package-android` (`windows-latest`) und `package-ios` (`macos-latest`, zusätzlich `if: vars.IOS_SIGNING_ENABLED == 'true'`) laufen parallel, jeweils `needs: version` und `if: needs.version.outputs.changed == 'true'`; jeder Job ruft seine Composite Action auf, lädt das erzeugte Artefakt (`release-win-x64.zip`, `release-android.apk`, `release-ios.ipa`) via `actions/upload-artifact` hoch.
6. Job `prerelease` (`ubuntu-latest`, `needs: [version, package-windows, package-android, package-ios]`, `if: needs.version.outputs.changed == 'true' && !failure() && !cancelled()`) lädt alle Artefakte herunter, baut die Asset-Liste konditional auf (`release-win-x64.zip`, `release-android.apk` verbindlich; `release-ios.ipa` nur bei `vars.IOS_SIGNING_ENABLED == 'true'`, als `RELEASE_ASSETS`-Env), ruft `scripts/create-update-manifest.mjs` auf (erzeugt `update.json` mit allen vorhandenen Assets) und erstellt via `gh release create "$rc_tag" <assets> update.json --prerelease --generate-notes --target $GITHUB_SHA` das GitHub-Pre-Release. Ein übersprungener `package-ios`-Job blockiert die Aggregation nicht (`!failure() && !cancelled()` wertet `skipped`-Needs nicht als Fehlschlag).
7. Bei erfolgreichem Abschluss feuert `staging-to-main-promotion.yml` (`workflow_run` auf Display-Name `Pre-Release`, `branches: [staging]`) und öffnet — falls `git diff origin/main..HEAD` Änderungen zeigt und noch kein offener PR existiert — einen Draft-PR `staging` → `main` mit Label `automated-promotion`. Unverändert.

Beteiligte Komponenten: `staging-ci.yml`, `build-and-package`, `package-android` (neu), `package-ios` (neu), `scripts/create-update-manifest.mjs` (neu), `staging-to-main-promotion.yml`.

### Stabiles Release auf `main` (Push/Tag → `release.yml`)

1. Push auf `main` oder manueller Push eines Tags `v*.*.*` triggert `release.yml` (`concurrency: release`, `cancel-in-progress: false`).
2. Job `resolve` (`ubuntu-latest`): Checkout (`fetch-depth: 0`, `fetch-tags: true`), Setup Node.js, `npm ci`, Entfernen lokaler RC-Tags (`git tag -d 'v*-rc.*'`), Aufruf `node scripts/resolve-release-version.mjs`. Outputs `released`, `reason`, `version`, `tag`, `release_kind`, `release_action` (bestehende Semantik: `create` / `upload-existing` / `none`, inkl. Fallback-Scan und Prerelease-Guard).
3. Job `release-gate` (`windows-latest`, `needs: resolve`, `if: needs.resolve.outputs.released == 'true' && needs.resolve.outputs.release_action == 'create'`): `actions/setup-dotnet` (`10.0.x`) und `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release`.
4. Jobs `package-windows` / `package-android` / `package-ios` (`needs: [resolve, release-gate]`, `if: needs.resolve.outputs.released == 'true'` bzw. zusätzlich iOS-Gate-Variable): Bei `release_action == 'upload-existing'` checken sie zuerst den Release-Tag aus (`git fetch origin refs/tags/<tag>` + `git checkout --detach <tag>` inkl. HEAD-Verifikation, Logik aus dem bisherigen Step übernommen), danach Packaging wie in Ablauf 1, Upload der Artefakte.
5. Job `publish` (`ubuntu-latest`, `needs: [resolve, package-windows, package-android, package-ios]`, `if: needs.resolve.outputs.released == 'true' && !failure() && !cancelled()`): Checkout, Setup Node.js, `npm ci`, Download aller Artefakte, konditionaler Aufbau der Asset-Liste (`release-win-x64.zip`, `release-android.apk` verbindlich; `release-ios.ipa` nur bei `vars.IOS_SIGNING_ENABLED == 'true'`), `scripts/create-update-manifest.mjs` → `update.json`. Danach genau einer der drei Pfade:
   - `release_action == 'create' && release_kind == 'automatic'`: `npm run release` (semantic-release erzeugt Tag, GitHub-Release und Asset-Upload gemäß `release.config.js`; `RELEASE_ASSET_PATHS` enthält die Semikolon-Liste der verbindlichen Assets plus konditional `release-ios.ipa`, `RELEASE_MANIFEST_PATH: update.json`, `RELEASE_VERSION`).
   - `release_action == 'create' && release_kind == 'manual'`: `gh release create "$tag" <assets> update.json --title "$tag" --generate-notes`.
   - `release_action == 'upload-existing'`: `gh release upload "$tag" <assets> update.json --clobber`.
6. Jeder Push auf `main` triggert zusätzlich `sync-staging-with-main.yml`, das — falls `staging` hinter `main` liegt (`git rev-list origin/staging..HEAD --count`) und kein PR offen ist — einen PR `main` → `staging` mit Label `automated-backmerge` öffnet; Merge ausschließlich per „Create a merge commit". Unverändert.

Beteiligte Komponenten: `release.yml`, `scripts/resolve-release-version.mjs`, `scripts/create-update-manifest.mjs` (neu), `build-and-package`, `package-android` (neu), `package-ios` (neu), `release.config.js`, `sync-staging-with-main.yml`.

### PR-Gates auf `staging` (Pull Request → `pr-staging-ci.yml`)

1. `pull_request` → `staging` (`opened`, `synchronize`, `reopened`) triggert `pr-staging-ci.yml` (`concurrency: pr-staging-<pr-number>`).
2. Job `detect-backmerge` wie in `staging-ci.yml`; die Gates `static-checks`/`build-and-test` hängen an `needs: detect-backmerge` mit `if: is_backmerge != 'true'`.
3. Job `back-merge-skip` (`needs: detect-backmerge`, `if: is_backmerge == 'true'`) gibt ein grünes Signal für Backmerge-PRs (vom Backmerge-Workflow geöffnete `main` → `staging`-PRs durchlaufen die Gates nicht erneut, weil der Code bereits auf `main` geprüft wurde).

Beteiligte Komponenten: `pr-staging-ci.yml`, `security-scan`-Action.

### PR-Quellenprüfung nach `main` (`verify-pr-source.yml`)

Unverändert aktiv: lehnt jeden PR nach `main` ab, dessen `github.head_ref` nicht `staging` ist. Ergänzend dient die Branch-Protection auf `main` (PR-Pflicht, Required Check `verify-source`) als Repository-Absicherung — wird in Schritt 10 per `gh api` eingerichtet.

Beteiligte Komponenten: `verify-pr-source.yml` (keine Änderung).

## Neue Komponenten

Es werden keine C#-Klassen angelegt; neu sind folgende Dateien/Komponenten:

| Komponente | Typ | Zweck |
|------------|-----|-------|
| `scripts/create-update-manifest.mjs` | Node-Skript | Erzeugt `update.json` aus den im Aggregations-Job vorliegenden Artefakten: liest eine Asset-Liste aus `RELEASE_ASSETS` (Format `name:platform:runtimeIdentifier`, Semikolon-getrennt), `RELEASE_VERSION`, `RELEASE_TAG` und `GITHUB_REPOSITORY`, berechnet `sha256`/`sizeBytes` pro Datei, schreibt das Manifest im bisherigen Schema (`version`, `releaseNotes`, `publishedAt`, `assets[]` mit `platform`, `runtimeIdentifier`, `assetName`, `assetUrl`, `sha256`, `sizeBytes`). Wirft einen Fehler, wenn eine erwartete Datei fehlt. |
| `.github/actions/package-android/action.yml` | Composite Action | `actions/setup-dotnet` (`10.0.x`), `dotnet workload restore Reporter.sln` mit `IncludeAndroidTarget: true`/`IncludeIosTarget: false`, Restore, `dotnet publish src/Reporter/Reporter.csproj -f net10.0-android -c Release -p:Version=<release-version>` (APK-Format), Ergebnis als `release-android.apk` im Repo-Root bereitstellen. Inputs: `release-version`, `release-tag` (analog `build-and-package`). |
| `.github/actions/package-ios/action.yml` | Composite Action | Auf `macos-latest` nutzbar: Setup .NET, MAUI-Workload-Restore, `dotnet publish src/Reporter/Reporter.csproj -f net10.0-ios -c Release` mit Codesigning-Parametern aus Inputs (`codesign-key`, `provisioning-profile` o. ä., vom Caller aus `secrets.*` befüllt), Ergebnis als `release-ios.ipa`. Bleibt per `vars.IOS_SIGNING_ENABLED` deaktiviert, bis die Signierungs-Secrets extern bereitgestellt sind — Aktivierung dann ohne Codeänderung. |
| `scripts/resolve-release-version.test.mjs` | Node-Testdatei (`node:test`) | Unit-Tests für `resolve-release-version.mjs` (siehe Abschnitt „Tests"). |
| `scripts/create-update-manifest.test.mjs` | Node-Testdatei (`node:test`) | Unit-Tests für das Manifest-Skript (Asset-Einträge, Hash/Größe, Fehler bei fehlender Datei). |

## Änderungen an bestehenden Komponenten

### `.github/workflows/staging-ci.yml` (Workflow `Pre-Release`)

- **Reaktivierung:** auskommentierter `on: push → branches: [staging]`-Block (Zeilen 3–6) wieder einkommentieren.
- **Neuer Job `detect-backmerge`:** wörtlich aus Vorlage Abschnitt 4 (`ubuntu-latest`, Output `is_backmerge`, Merge-Parent-/Tree-Vergleich gegen `origin/main`).
- **Job `static-checks`:** `needs: detect-backmerge` + `if: needs.detect-backmerge.outputs.is_backmerge != 'true'` ergänzen; Schritte unverändert.
- **Job `build-and-test`:** ebenso `needs`/`if` ergänzen; Schritte unverändert.
- **Job `version`:** `needs` von `[static-checks, build-and-test]` auf `[detect-backmerge, static-checks, build-and-test]` ändern; `if: success()` durch `if: needs.detect-backmerge.outputs.is_backmerge != 'true'` ersetzen. Outputs und Schritte unverändert (`rc_version`-Verdrahtung ist bereits korrekt — Vorlagen-Bug 11.2 liegt nicht vor).
- **Job `prerelease` umbauen:** Der bisherige Einzeljob wird ersetzt durch `package-windows`, `package-android`, `package-ios` (siehe Programmablauf) und einen Aggregations-Job `prerelease`, der das Manifest erzeugt und `gh release create --prerelease` mit der vollständigen Asset-Liste ausführt. `build-and-package` wird weiterhin mit `release-version: rc_version` (nicht `version`) und `release-tag: rc_tag` aufgerufen.

### `.github/workflows/pr-staging-ci.yml` (Workflow `PR CI for Staging`)

- **Reaktivierung:** `on: pull_request → staging`-Block (Zeilen 3–10) einkommentieren.
- **Neue Jobs:** `detect-backmerge` und `back-merge-skip` wörtlich aus Vorlage Abschnitt 4.
- **`static-checks`/`build-and-test`:** `needs: detect-backmerge` + `if: is_backmerge != 'true'` ergänzen; hybrides Parallelmodell bleibt (kein `needs` zwischen den Gates).

### `.github/workflows/release.yml` (Workflow `Release`)

- **Reaktivierung:** `on: push → branches: [main], tags: ['v*.*.*']`-Block (Zeilen 3–6) einkommentieren.
- **Job-Aufteilung:** Der bisherige Einzeljob `release` wird in `resolve`, `release-gate`, `package-windows`, `package-android`, `package-ios` und `publish` aufgeteilt (siehe Programmablauf und Designentscheidung).
- **Neu im `release-gate`:** `actions/setup-dotnet@v6` mit `dotnet-version: '10.0.x'` vor dem `dotnet test`-Schritt, damit die SDK-Version explizit gepinnt ist statt vom Runner-Image abzuhängen (Rest der Pipeline pinnt bereits `10.0.x`). `dotnet test` restauriert weiterhin implizit; `Reporter.Tests` (`net10.0`, kein MAUI) benötigt keinen Workload.
- **Tag-Checkout für Repair:** Der Step „Check out release tag for asset repair" wandert vom Einzeljob in die `package-*`-Jobs (jeder Job checkt bei `release_action == 'upload-existing'` den Tag aus).
- **Asset-Listen:** `RELEASE_ASSET_PATHS`, `RELEASE_ASSETS`, die `gh release create`-/`gh release upload`-Argumente und das Manifest enthalten künftig die vollständige Asset-Liste — verbindlich `release-win-x64.zip`, `release-android.apk`, `update.json`; `release-ios.ipa` wird nur bei `vars.IOS_SIGNING_ENABLED == 'true'` ergänzt (konditionaler Listenaufbau im `publish`-Job, z. B. per Env-Setzung in einem vorgelagerten Step).
- **Permissions:** für `workflow_run`-übergreifenden Artefaktaustausch keine Änderung nötig; `contents/issues/pull-requests: write` bleibt.

### `.github/actions/build-and-package/action.yml` (Composite Action)

- **Entfernen:** Schritt „Create update manifest" (Zeilen 45–68) entfällt — die Manifest-Erzeugung liegt im Aggregations-Job (`scripts/create-update-manifest.mjs`), weil dort erst alle Plattform-Artefakte vorliegen.
- **Unverändert:** Inputs `release-version`/`release-tag`, Setup .NET, Workload-Restore (`IncludeIosTarget: false`), Restore `-r win-x64`, Publish `net10.0-windows10.0.19041.0`/`win-x64`, `Compress-Archive` → `release-win-x64.zip`.
- **Hinweis:** `release-tag` wird nach dem Entfernen des Manifest-Schritts in der Action selbst nicht mehr benötigt (keine URL-Erzeugung mehr); der Input bleibt zunächst bestehen, damit die Aufrufsignaturen stabil bleiben — oder wird mit den Callern konsistent entfernt (Implementierungsdetail, kein Funktionsunterschied).

### `scripts/resolve-release-version.mjs`

- **Import-Seiteneffekt beseitigen:** Der unbedingte Top-Level-Aufruf `resolveReleaseVersion()` (Zeile 164) wird hinter eine Main-Modul-Prüfung gestellt (`import.meta.url` gegen `process.argv[1]` vergleichen), damit die exportierten Funktionen in Tests importierbar sind.
- **`incompleteReleases` exportieren** (derzeit intern, Zeilen 44–51), damit der Prerelease-Guard (Vorlage 11.1) direkt regressionstestbar ist.
- **`EXPECTED_ASSETS` (Zeile 6)** auf die finale Asset-Liste setzen: verbindlich `release-win-x64.zip`, `release-android.apk`, `update.json`; `release-ios.ipa` wird konditional ergänzt, wenn die Umgebungsvariable `IOS_SIGNING_ENABLED == 'true'` ist (der `resolve`-Job reicht `vars.IOS_SIGNING_ENABLED` als Env an den Resolver-Step durch). Damit bleibt die Liste synchron zu `RELEASE_ASSETS`/`RELEASE_ASSET_PATHS`, ohne dass ein deaktiviertes iOS-Artefakt jedes Release als unvollständig markiert.
- **Ungenutzten Import `execFileSync` (Zeile 1)** entfernen.

### `release.config.js`

- **`branches` (Zeilen 20–23):** auf `["main"]` reduzieren — `{ name: "staging", prerelease: "rc" }` entfällt (siehe Designentscheidung).
- **Asset-Liste `@semantic-release/github` (Zeilen 7–10):** statt fester Slots `split(";")[0]`/Manifest jeden Pfad aus `RELEASE_ASSET_PATHS` auf den jeweiligen Dateinamen mappen (`name` = Basename des Pfads) plus `RELEASE_MANIFEST_PATH` → `update.json`; `.filter(asset => asset.path !== undefined)` beibehalten.
- **Unverändert:** `preset: "conventionalcommits"` auf `commit-analyzer`/`release-notes-generator`, `successComment`/`failComment: false`, `dryRunPlugins`, `RESOLVE_DRY_RUN`-Umschaltung, `tagFormat: "v${version}"`.

### `package.json` / `package-lock.json`

- **devDependencies pinnen:** `^`-Ranges durch exakte Versionen ersetzen (Vorlage Abschnitt 8.4: `semantic-release 25.0.9`, `@semantic-release/commit-analyzer 13.0.1`, `@semantic-release/release-notes-generator 14.1.1`, `@semantic-release/github 12.0.9`, `conventional-changelog-conventionalcommits 9.3.1` bzw. die im Lockfile tatsächlich aufgelösten Stände).
- **Script `"test": "node --test scripts/"` ergänzen** für die neuen Skript-Tests.
- **`package-lock.json` über `npm install` regenerieren** — zwingend konsistent, weil die Workflows `npm ci` verwenden.

### `src/Reporter/Reporter.csproj`

- **Neue Property `IncludeAndroidTarget`** (Default `false`), analog `IncludeIosTarget`.
- **`TargetFrameworks`-Bedingungen erweitern:** `net10.0-android` wird nur ergänzt, wenn `IncludeAndroidTarget == 'true'` — auf Windows zusätzlich zu den bisherigen TFMs, auf macOS/Linux als Alternative zu `net10.0-ios` (Bedingungslogik dem `IncludeIosTarget`-Muster nachempfunden). `SupportedOSPlatformVersion` für `android` (21.0) existiert bereits (Zeile 41).
- **Unverändert:** `WindowsPackageType` bleibt `None` (ZIP-Artefakt, siehe Designentscheidung); `ApplicationDisplayVersion`/`ApplicationVersion` bleiben, der Publish-Schritt überschreibt die Version per `-p:Version`.

### Unveränderte Komponenten

- `staging-to-main-promotion.yml`, `sync-staging-with-main.yml`, `verify-pr-source.yml`, `security-scan.yml`, `.github/actions/security-scan/action.yml` — entsprechen der Vorlage bzw. sind außerhalb des Scopes.
- `Reporter.sln`, `src/Reporter.Tests/` — keine inhaltlichen Änderungen.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Release-Tag (manueller Push) | `parseManualTag` verlangt `vX.Y.Z` (SemVer, optional mit Prerelease-Suffix) — bestehend | `classifyWorkflowRef` wirft bei ungültigem Tag bzw. nicht unterstütztem Ref |
| Release-Assets vor Veröffentlichung | `create-update-manifest.mjs` prüft, dass jede Datei der `RELEASE_ASSETS`-Liste im Aggregations-Job vorhanden und nicht leer ist — neu | Job schlägt fehl, bevor ein unvollständiges Release entsteht |
| `EXPECTED_ASSETS` ↔ erzeugte Assets | Die Liste in `resolve-release-version.mjs` muss exakt den in den Workflows/`release.config.js` hinterlegten Asset-Namen entsprechen — neu zu synchronisieren | Asset-Repair (`upload-existing`) würde sonst Assets vermissen bzw. ignorieren; Konsistenz durch gemeinsame Namenskonvention und Test auf Listengleichheit sichern |
| `is_backmerge`-Erkennung | Merge-Parent == `origin/main` ODER `git diff --quiet origin/main HEAD` (Baum identisch) — neu aus Vorlage | Bei `true` werden Gates/Versionierung/Pre-Release übersprungen |
| Line Coverage | ≥ 70 % in `build-and-test` — bestehend, unverändert | Gate schlägt bei Unterschreitung fehl |
| PR-Quelle nach `main` | `github.head_ref == "staging"` — bestehend (`verify-pr-source.yml`) | PR-Check schlägt fehl |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `release.config.js` → `branches` | semantic-release-Config | `["main"]` | Nur `main` erzeugt stabile Releases; RC-Suffix manuell in `staging-ci.yml` |
| `release.config.js` → `@semantic-release/github` assets | semantic-release-Config | aus `RELEASE_ASSET_PATHS`/`RELEASE_MANIFEST_PATH` abgeleitet | Vollständige Asset-Upload-Liste statt Einzel-ZIP |
| `package.json` → `devDependencies` | npm-Config | exakte Pins (statt `^`) | Reproduzierbarkeit laut Vorlage 8.4 |
| `package.json` → `scripts.test` | npm-Config | `node --test scripts/` | Ausführung der neuen Skript-Tests |
| `Reporter.csproj` → `IncludeAndroidTarget` | MSBuild-Property | `false` | Schalter für `net10.0-android`-TFM, analog `IncludeIosTarget` |
| `Reporter.csproj` → `TargetFrameworks` | MSBuild-Property | + `net10.0-android` (nur bei `IncludeAndroidTarget=true`) | Android-Publish ermöglichen |
| `release.yml` → `RELEASE_ASSET_PATHS` | Workflow-Env | `release-win-x64.zip;release-android.apk`; `release-ios.ipa` konditional bei `vars.IOS_SIGNING_ENABLED == 'true'` | Asset-Liste für semantic-release-Upload |
| `staging-ci.yml`/`release.yml` → `RELEASE_ASSETS` | Workflow-Env | `release-win-x64.zip:windows:win-x64;release-android.apk:android:android`; iOS-Eintrag `release-ios.ipa:ios:ios` konditional bei `vars.IOS_SIGNING_ENABLED == 'true'` | Input für `create-update-manifest.mjs` |
| `resolve-release-version.mjs` → `IOS_SIGNING_ENABLED` (Env) | Umgebungsvariable im `resolve`-Job | aus `vars.IOS_SIGNING_ENABLED` durchgereicht | Steuert, ob `release-ios.ipa` in `EXPECTED_ASSETS` enthalten ist |
| GitHub-Variable `IOS_SIGNING_ENABLED` | Repository-Variable (`vars.*`) | nicht gesetzt / `false` | Gate für den `package-ios`-Job, bis Signierungs-Secrets vorliegen |
| iOS-Signing-Secrets | Repository-Secrets | — (werden später extern bereitgestellt; Aktivierung dann ohne Codeänderung über `vars.IOS_SIGNING_ENABLED`) | `CodesignKey`/Provisioning Profile für `.ipa`-Erzeugung |
| Label `automated-promotion` / `automated-backmerge` | Repo-Labels | Farben `0E8A16`/`1D76DB` | Werden vom ausführenden Agenten per `gh api`/`gh label create` angelegt und verifiziert; die Workflows legen sie zusätzlich per `gh label create --force` lazy an (idempotent) |
| Branch-Protection `staging` | Repo-Einstellung (`gh api`) | PR-Pflicht, Required Checks `static checks` + `build & test`, „up to date" | Voraussetzung des Branch-Modells; vom Agenten per `gh api` gesetzt und per GET verifiziert |
| Branch-Protection `main` | Repo-Einstellung (`gh api`) | PR-Pflicht, Push-Restriktion, Required Check `verify-source` | Nur Promotion-PRs aus `staging`; kombiniert mit `verify-pr-source.yml`; vom Agenten per `gh api` gesetzt und per GET verifiziert; keine Admin-Bypass-Ausnahme |

## Seiteneffekte und Risiken

- **Required Checks auf `staging`:** Sobald `static checks`/`build & test` als Required Checks eingerichtet sind, müssen Backmerge-PRs mergebar bleiben — dafür dienen `detect-backmerge`/`back-merge-skip`. Zu verifizieren: GitHub wertet via `if:` übersprungene Required Checks korrekt (der `back-merge-skip`-Job liefert das explizite grüne Signal).
- **Coverage-Gate 70 %:** Die Reaktivierung von `pr-staging-ci.yml`/`staging-ci.yml` setzt das bisher pausierte Coverage-Gate wieder scharf; die aktuelle Line Coverage ist lokal nicht gemessen (239/239 Tests grün, Coverage unbekannt). Erster Lauf kann am Gate scheitern — dann ist Coverage nachzubessern, nicht die Schwelle zu senken.
- **`workflow_run`-/`push → main`-Trigger lesen die Datei auf `main`:** Die aktualisierten `staging-to-main-promotion.yml`/`sync-staging-with-main.yml`-Abhängigkeiten (Display-Name `Pre-Release`) wirken erst nach Promotion der Änderungen auf `main`. Der Cold-Start selbst ist bereits überstanden (Dateien existieren auf `main`), aber die alten Stände auf `main` enthalten kein `detect-backmerge` — Inkonsistenzen sind bis zum ersten Promotion-Merge möglich.
- **`release.config.js`-Branch-Änderung:** Ohne `staging` als Prerelease-Branch analysiert ein versehentlicher Dry-Run ohne `--branches`-Override nur noch `main` — gewünscht; der bisherige `--branches staging`-Override im `version`-Job bleibt dafür essenziell.
- **`IncludeAndroidTarget`:** Ist der Schalter versehentlich in Gates/Lokal-Builds gesetzt, scheitern Restore/Build ohne Android-Workload — die Gates setzen explizit `IncludeAndroidTarget: false` (neben dem vorhandenen `IncludeIosTarget: false`).
- **`update.json`-Schema:** Erweitert sich um weitere `assets[]`-Einträge; `msTools.Updater` ist im Repo nicht referenziert, externe Konsumenten des Manifests sind nicht im Code sichtbar. `release-metadata.json` wird bewusst nicht erzeugt (entschieden, s. Designentscheidungen).
- **Reaktivierte `release.yml` auf `main`:** Nach Promotion löst jeder Push auf `main` den Resolver aus; bei fehlender neuer Version greift der Fallback-Scan (Prerelease-Guard ist vorhanden und wird per Test abgesichert).
- **Lokale Static Checks:** `scripts/Run-StaticChecks.ps1` muss weiterhin ohne Android-Workload durchlaufen (Schalter-Default `false`) — Abschlusskriterium gemäß `AGENTS.md`.

## Umsetzungsreihenfolge

1. **`resolve-release-version.mjs` testbar machen**
   - Voraussetzungen: Node.js ≥ 20 für `node:test` (im Repo-Umfeld Node 24 vorhanden); keine neuen Pakete nötig.
   - Beschreibung: Main-Modul-Guard um den Top-Level-Aufruf, `incompleteReleases` exportieren, `execFileSync`-Import entfernen, `EXPECTED_ASSETS` auf die finale Asset-Liste setzen (verbindlich `release-win-x64.zip`, `release-android.apk`, `update.json`; `release-ios.ipa` konditional bei `IOS_SIGNING_ENABLED == 'true'`).

2. **Node-Testsuite und `package.json`-Anpassungen**
   - Voraussetzungen: Schritt 1 (exportierte, importierbare Funktionen).
   - Beschreibung: `scripts/resolve-release-version.test.mjs` (inkl. Prerelease-Guard-Regression), `"test": "node --test scripts/"` in `package.json`, devDependencies pinnen, `package-lock.json` via `npm install` regenerieren, `npm test` lokal grün.

3. **`release.config.js` vereinheitlichen**
   - Voraussetzungen: Schritt 2 (Lockfile/Script-Stand stabil).
   - Beschreibung: `branches: ["main"]`, Asset-Liste dynamisch aus `RELEASE_ASSET_PATHS` + `RELEASE_MANIFEST_PATH`.

4. **`Reporter.csproj` um `net10.0-android` erweitern**
   - Voraussetzungen: Keine (MSBuild-Schalter-Muster `IncludeIosTarget` existiert bereits als Vorbild).
   - Beschreibung: `IncludeAndroidTarget` (Default `false`) einführen, `TargetFrameworks`-Bedingungen ergänzen; lokal verifizieren: `dotnet restore`/`build` ohne Schalter unverändert, `dotnet restore Reporter.sln -p:IncludeAndroidTarget=true` (bzw. Env) funktioniert mit installiertem Android-Workload.

5. **`scripts/create-update-manifest.mjs` + Tests**
   - Voraussetzungen: Node-Test-Infrastruktur aus Schritt 2; finale Asset-Namenskonvention aus Schritt 1.
   - Beschreibung: Manifest-Skript mit `RELEASE_ASSETS`-Parsing, SHA-256/Größen, Fehler bei fehlender Datei; zugehörige Testdatei.

6. **Composite Actions: `build-and-package` bereinigen, `package-android`/`package-ios` anlegen**
   - Voraussetzungen: Schritt 4 (Android-TFM) für `package-android`; Schritt 5 übernimmt Manifest-Aufgabe.
   - Beschreibung: Manifest-Schritt aus `build-and-package` entfernen; `package-android`-Action anlegen; `package-ios`-Action anlegen (Signierungs-Inputs vorgesehen, wird per `vars.IOS_SIGNING_ENABLED` aktiviert).

7. **`staging-ci.yml` reaktivieren und umbauen**
   - Voraussetzungen: Schritte 5–6 (Actions/Manifest-Skript vorhanden).
   - Beschreibung: `on:`-Block einkommentieren; `detect-backmerge` ergänzen; `needs`/`if` an `static-checks`, `build-and-test`, `version`; `prerelease` in `package-*`-Jobs + Aggregations-Job aufteilen; `RELEASE_ASSETS`-Env und `gh release create` mit voller Asset-Liste.

8. **`pr-staging-ci.yml` reaktivieren und ergänzen**
   - Voraussetzungen: Keine außer den Dateien selbst.
   - Beschreibung: `on:`-Block einkommentieren; `detect-backmerge` + `back-merge-skip` ergänzen; `needs`/`if` an beiden Gates.

9. **`release.yml` reaktivieren und umbauen**
   - Voraussetzungen: Schritte 5–6; finale Asset-Liste (Schritt 1).
   - Beschreibung: `on:`-Block einkommentieren; Job-Aufteilung (`resolve` → `release-gate` + `package-*` → `publish`); `setup-dotnet` im Gate; Tag-Checkout-Repair-Logik in `package-*`-Jobs; `RELEASE_ASSET_PATHS`/`RELEASE_ASSETS`/alle `gh`-Aufrufe auf die volle Liste.

10. **Repository-Einstellungen einrichten/verifizieren (direkt durch den ausführenden Agenten)**
    - Voraussetzungen: `gh` ist authentifiziert mit repo-Scope (in der Ausführungsumgebung gegeben); die Required-Check-Namen müssen den tatsächlichen Job-Anzeigenamen entsprechen (`static checks`, `build & test`, `verify-source`) — die Jobs existieren ab Schritt 7–9 im Workflow-Code.
    - Beschreibung: Der Agent richtet die Einstellungen direkt per `gh api` ein und verifiziert per GET (nicht per Test-Push — Vorlage 11.7):
      - Branch-Protection `staging`: `PUT repos/martin-stromberg/Reporter/branches/staging/protection` mit PR-Pflicht, Required Status Checks `static checks` + `build & test`, `strict: true` („up to date").
      - Branch-Protection `main`: `PUT repos/martin-stromberg/Reporter/branches/main/protection` mit PR-Pflicht, Push-Restriktion und Required Check `verify-source` (erzwingt faktisch „nur PRs aus `staging`", da `verify-pr-source.yml` alle anderen Quell-Branches ablehnt); **keine** Admin-Bypass-Ausnahme (`enforce_admins` bleibt wirksam).
      - Labels `automated-promotion` (`0E8A16`) und `automated-backmerge` (`1D76DB`): per `gh label create`/`gh api repos/.../labels` anlegen und per `gh label list` verifizieren.
      - `vars.IOS_SIGNING_ENABLED` bleibt ungesetzt (deaktiviert); iOS-Signing-Secrets werden später extern bereitgestellt — Aktivierung dann ohne Codeänderung.
      - Das `gh api`-Vorgehen bleibt zusätzlich in der Konfigurationstabelle/Dokumentation festgehalten.

11. **Lokale Verifikation**
    - Voraussetzungen: Schritte 1–9 im Branch umgesetzt.
    - Beschreibung: `npm test` (Skript-Suite), `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` (239 Tests, Baseline-Stand), `.\scripts\Run-StaticChecks.ps1` mit Exit-Code 0 (AGENTS.md-Pflicht).

12. **Rollout-Verifikation (Funktionsnachweis über die Pipeline selbst)**
    - Voraussetzungen: Schritte 1–10 gemergt auf `staging`.
    - Beschreibung: Push auf `staging` → `Pre-Release`-Lauf erzeugt `vX.Y.Z-rc.N` mit allen Assets + `update.json` (Manifest `version` enthält RC-Suffix — Prüfung gegen Vorlagen-Bug 11.2); Promotion-Workflow öffnet Draft-PR `staging` → `main`; nach Merge: `Release`-Lauf erzeugt stabiles Release; Backmerge-PR `main` → `staging` erscheint und wird per „Create a merge commit" gemergt; Backmerge-PR zeigt `back-merge-skip` statt Gates.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `parseManualTag` — gültige/ungültige Tags | `scripts/resolve-release-version.test.mjs` (`node:test` + `node:assert`) | `v1.2.3` → `1.2.3`; fehlendes `v`, Nicht-SemVer, leerer Input → Fehler |
| `classifyWorkflowRef` — Klassifizierung | `scripts/resolve-release-version.test.mjs` | `tag`-Ref → `manual` mit Version/Tag; `branch: main` → `automatic`; andere Refs → Fehler |
| `releaseHasExpectedAsset` — Asset-Vollständigkeit | `scripts/resolve-release-version.test.mjs` | alle `EXPECTED_ASSETS` mit `state: "uploaded"` + `size > 0` → `true`; fehlendes Asset / `size: 0` / anderer State → `false`; erwartete Liste = `release-win-x64.zip`, `release-android.apk`, `update.json`; zusätzlich Fall `IOS_SIGNING_ENABLED=true` → `release-ios.ipa` wird mitverlangt |
| `incompleteReleases` — Prerelease-Guard (Regression, Vorlage 11.1) | `scripts/resolve-release-version.test.mjs` | Ein Prerelease-Release ohne jegliche Assets wird **niemals** als reparaturbedürftig selektiert; ältestes unvollständiges stabiles Release wird selektiert |
| Manifest-Erzeugung | `scripts/create-update-manifest.test.mjs` | `update.json` enthält alle `RELEASE_ASSETS`-Einträge mit korrekten `assetUrl`s (`releases/download/<tag>/<name>`), `sha256` (64 hex), `sizeBytes` > 0, `version` = Input inkl. RC-Suffix |
| Manifest-Fehlerfall | `scripts/create-update-manifest.test.mjs` | fehlende Artefaktdatei → Prozess/Exception mit aussagekräftigem Fehler |
| `npm test` | `package.json` | `node --test scripts/` führt beide Suites aus; läuft lokal und kann in den Gates ergänzt werden (optionaler Step in `version`-/`resolve`-Job — nicht erforderlich) |

### Betroffene bestehende Tests

Keine. `Reporter.Tests` (239 Tests, Baseline grün) wird weder geändert noch in Signaturen/Verhalten beeinflusst; die Workflows referenzieren die Suite bereits korrekt (`src/Reporter.Tests/Reporter.Tests.csproj`, kein `MyApp.Tests`). Der in Vorlage 11.6 beschriebene Stolperstein (Test-Helper mit hartcodiertem `bin/Debug`) trifft nicht zu: Eine Suche über `src/` ergab keine solchen Helper, und der Baseline-Lauf lief bereits erfolgreich unter `--configuration Release`.

### E2E-Tests (primärer Funktionsnachweis)

Keine E2E-Tests erforderlich — Begründung: Die Anforderung betrifft ausschließlich CI-/Infrastruktur-Artefakte (Workflows, Composite Actions, Release-Skripte). Es existiert kein über UI oder Endanwenderaktion erreichbarer Ablauf; die App `Reporter` selbst ändert sich funktional nicht (lediglich ein zusätzliches TFM hinter einem Build-Schalter). Der Funktionsnachweis wird stattdessen über die Pipeline selbst erbracht: Der geordnete Rollout (Schritt 12 der Umsetzungsreihenfolge) verifiziert RC-Erzeugung, Asset-Vollständigkeit, Manifest-Inhalt, Promotion-PR, stabiles Release und Backmerge-PR an realen Workflow-Läufen — das ist für diese Feature-Art das Äquivalent zum Happy-Path-Nachweis. `Reporter.Tests` bleibt die fachliche Absicherung und läuft in den Gates.

## Offene Punkte

Keine.
