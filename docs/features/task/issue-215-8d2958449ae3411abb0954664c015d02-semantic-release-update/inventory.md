<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme — Issue #215 „Semantic Release Update"

Reine Faktensammlung zum Stand auf Branch
`task/issue-215-8d2958449ae3411abb0954664c015d02-semantic-release-update`
(Worktree-Stand, keine lokalen Änderungen außer `docs/features/task/.../`).

## 1. Stellen mit `actions/setup-node` und `node-version`

Exakt drei Stellen im Repo, alle mit demselben SHA-gepinnten Action-Release
(`actions/setup-node@949feb2413d6458794dcd2491c4babbbce0c15c1  # v7`) und
`node-version: '24'`:

| Datei | Zeilen | Job | Zweck |
|---|---|---|---|
| `.github/workflows/release.yml` | 38–41 | `resolve` | `npm ci` + `node scripts/resolve-release-version.mjs` (Z. 44, 57) |
| `.github/workflows/release.yml` | 202–205 | `publish` | `npm ci`, Node-Skripte, `npm run release` (Z. 208, 225, 231, 239) |
| `.github/workflows/staging-ci.yml` | 254–257 | `version` | `npm ci` + `npx semantic-release --dry-run` (Z. 260, 269) |

Weitere (dokumentarische) Vorkommen von `node-version: '24'`:

- `ci-instructions.md` Z. 341–344 — wiedergegebener Ausschnitt des
  `version`-Jobs aus `staging-ci.yml` (`actions/setup-node@v7`)
- `ci-instructions.md` Z. 722–725 — wiedergegebener Ausschnitt des
  Release-Jobs (`actions/setup-node@v7`)

In `.github/actions/` gibt es **keine** `setup-node`-Verwendung. Die fünf
Composite Actions sind: `build-and-package`, `checkout-release-tag`,
`package-android`, `package-ios`, `security-scan` (nur `action.yml` je
Verzeichnis; `security-scan` ruft `npm audit` auf, siehe Abschnitt 4).

## 2. Aufrufstellen von semantic-release

| Aufruf | Ort | Ausführender Node |
|---|---|---|
| `npx semantic-release --dry-run --no-ci --branches staging` | `.github/workflows/staging-ci.yml` Z. 269, Schritt „Determine next version" (`id: semver`), Job `version` | gepinnter Node `24` (Z. 254–257) |
| `node scripts/resolve-release-version.mjs` | `.github/workflows/release.yml` Z. 57, Schritt „Resolve release version", Job `resolve` | gepinnter Node `24` (Z. 38–41) |
| `npm run release` | `.github/workflows/release.yml` Z. 239, Schritt „Create automatic GitHub release", Job `publish`; Script `"release": "semantic-release"` in `package.json` Z. 6 | gepinnter Node `24` (Z. 202–205) |

Detail zu `scripts/resolve-release-version.mjs` (Job `resolve` ruft nicht
`npx` direkt, sondern das Skript):

- Z. 118–129 `runSemanticReleaseDryRun()`: `spawnSync("npx",
  ["semantic-release", "--dry-run", "--no-ci"], …)` mit
  `env: { ...process.env, RESOLVE_DRY_RUN: "true" }` — läuft also unter dem
  via `setup-node` installierten Node 24, `npx` zieht die per `npm ci`
  installierte Version 26.0.1 aus `node_modules`.
- Z. 105–116 `semanticReleaseDryRunVersion()`: wertet stdout/stderr aus,
  Regex `the next release version is X.Y.Z`; wirft bei `status !== 0` den
  Fehler `semantic-release dry-run failed (exit code N): <stderr>`
  (Z. 110–112).
- Z. 147–168 `resolveAutomaticRelease()`: Dry-Run bestimmt Version → Tag
  `v<version>`; Fallback prüft über `gh api` (Z. 62–80 `ghApi()`, spawnt
  `gh api …` mit `GH_TOKEN=process.env.GITHUB_TOKEN`) auf unvollständige
  Releases.
- Z. 9 `VERSION_PATTERN`, Z. 10 `AUTOMATIC_RELEASE_BRANCHES = ["main"]`.

`release.config.js` (Root):

- `branches: ["main"]`, `tagFormat: "v${version}"`.
- Plugins bei `RESOLVE_DRY_RUN === "true"`: nur
  `@semantic-release/commit-analyzer` (conventionalcommits-Preset).
- Sonst: `commit-analyzer`, `release-notes-generator`, `@semantic-release/github`
  (Assets aus `RELEASE_ASSET_PATHS` + `RELEASE_MANIFEST_PATH`/`update.json`,
  `successComment`/`failComment` deaktiviert).
- `staging-ci.yml` übersteuert die Branch-Liste per `--branches staging`;
  Kommentar in `ci-instructions.md` Z. 349–354 dokumentiert das.

Es existieren **keine** `.nvmrc`-, `.node-version`- oder
`.tool-versions`-Dateien im Repo-Root (geprüft via `ls -a`).

## 3. package.json / package-lock.json

`package.json` (komplett):

- `"scripts"`: `"release": "semantic-release"` (Z. 6),
  `"test": "node --test \"scripts/*.test.mjs\""` (Z. 7).
- `devDependencies` (Z. 9–15): `@semantic-release/commit-analyzer` `13.0.1`,
  `@semantic-release/github` `12.0.9`,
  `@semantic-release/release-notes-generator` `14.1.1`,
  `conventional-changelog-conventionalcommits` `9.3.1`,
  `semantic-release` `26.0.1`.
- **Kein `engines`-Feld** in `package.json` — die Node-Version ist nur über
  die Workflow-Pins festgelegt.

`package-lock.json` (`lockfileVersion: 3`):

- `node_modules/semantic-release` = `26.0.1`, `engines.node = ">=26.13"`
  (Z. ~4996–5023); `bin`: `bin/semantic-release.js`.
- Gebündelte Unter-Abhängigkeiten von `semantic-release@26.0.1`
  (`node_modules/semantic-release/node_modules/…`):
  `@semantic-release/commit-analyzer` `14.0.0-beta.4`
  (`node: ^22.22.2 || >=24.15`),
  `@semantic-release/release-notes-generator` `15.0.0-beta.3`
  (`node: ^22.22.2 || >=24.15`).
- Direkte Top-Level-Abhängigkeiten von `semantic-release` im Lock:
  `@semantic-release/core` `1.0.0-beta.13` (`node: >=26.13`),
  `@semantic-release/npm` `14.0.0-beta.2` (`node: ^22.22.2 || >=24.15`),
  `npm` `12.2.3` (`node: ^22.22.2 || ^24.15.0 || >=26.0.0`, inkl. großer
  Transitivmenge `node_modules/npm/node_modules/*` mit derselben Range).
- Top-Level-Engines der übrigen devDependencies:
  `@semantic-release/commit-analyzer` `13.0.1` → `>=20.8.1`;
  `@semantic-release/github` `12.0.9` → `^22.14.0 || >= 24.10.0`;
  `@semantic-release/release-notes-generator` `14.1.1` → `>=20.8.1`;
  `conventional-changelog-conventionalcommits` `9.3.1` → `>=18`.
- Befund: Unter dem gefundenen `v24.22.3` verletzen nur
  `semantic-release` selbst und `@semantic-release/core` die Engines-Range
  (`>=26.13`); die Beta-Plugins fordern bereits `>=24.15` bzw.
  `>=24.10.0`, was `24.22.3` erfüllt — die harte Grenze ist `>=26.13`.
- `npm ci` läuft trotzdem durch (npm erzwingt `engines` nur bei
  `engine-strict=true`; im Repo liegt keine `.npmrc` vor); der Fehler
  entsteht erst zur Laufzeit von `semantic-release`.

## 4. Node-Skripte in den betroffenen Jobs

Aufrufe von `node`/`npm`/`npx` in den Workflows (Vollständigkeit via
`grep` über `.github/workflows/*.yml`):

- `release.yml` Job `resolve` (Node 24): `npm ci` (Z. 44),
  `node scripts/resolve-release-version.mjs` (Z. 57).
- `release.yml` Job `publish` (Node 24): `npm ci` (Z. 208),
  `node scripts/release-assets.mjs` (Z. 225),
  `node scripts/create-update-manifest.mjs` (Z. 231),
  `npm run release` (Z. 239).
- `staging-ci.yml` Job `version` (Node 24): `npm ci` (Z. 260),
  `npx semantic-release --dry-run --no-ci --branches staging` (Z. 269).
- `staging-ci.yml` Job `prerelease` (**kein** setup-node →
  Runner-Default-Node auf `ubuntu-latest`):
  `node scripts/release-assets.mjs` (Z. 398),
  `node scripts/create-update-manifest.mjs` (Z. 404).
- `staging-ci.yml` Job `static-checks` (**kein** setup-node,
  `windows-latest`): `node scripts/add-license-headers.mjs --check` (Z. 82).
- `pr-staging-ci.yml` Job `static-checks` (**kein** setup-node,
  `windows-latest`): `node scripts/add-license-headers.mjs --check` (Z. 85).
- `.github/actions/security-scan/action.yml` Z. 24–31:
  `npm audit --package-lock-only --audit-level=high` (Runner-Default-npm,
  kein eigenes setup-node).

Genutzte Node-APIs der Skripte (`scripts/*.mjs`, jeweils nur
`node:`-Standardmodule):

- `resolve-release-version.mjs`: `node:child_process` (`spawnSync`),
  `node:fs` (`appendFileSync`), `node:url` (`pathToFileURL`); importiert
  `buildReleaseAssets` aus `release-assets.mjs`.
- `release-assets.mjs`: `node:fs`, `node:url`; baut nur Asset-Liste
  (`release-win-x64.zip`, `release-android.apk`, optional
  `release-ios.ipa`) und schreibt `GITHUB_OUTPUT`/`RELEASE_ASSET_*`.
- `create-update-manifest.mjs`: `node:crypto` (`createHash`), `node:fs`,
  `node:path`, `node:url`; erzeugt `update.json` mit SHA-256-Hashes.
- `add-license-headers.mjs`: `node:child_process` (`execFileSync` für
  `git ls-files`), `node:fs`, `node:fs/promises`, `node:path`, `node:url`.

Kein Skript nutzt versionsspezifische/experimentelle APIs; alle Module sind
langjährig stabil — eine Anhebung auf Node 26 betrifft diese Skripte nicht
ersichtlich. `scripts/lib/` enthält nur PowerShell-Hilfen
(`TestDiscovery.ps1`, `Trx.ps1`).

## 5. Lauf der betroffenen Jobs bei PRs gegen staging

- `staging-ci.yml` („Pre-Release"): Trigger ausschließlich `push` auf
  `staging` (Z. 5–9). Läuft **nicht** bei `pull_request`.
- `pr-staging-ci.yml` („PR CI for Staging"): Trigger `pull_request` →
  `staging`, Typen `opened/synchronize/reopened` (Z. 5–12). Jobs:
  `detect-backmerge`, `static-checks`, `build-and-test`,
  `os-interface-tests` (`continue-on-error: true`, Z. 178),
  `back-merge-skip`. **Kein `version`/`semver`-Job, kein
  semantic-release-Aufruf, kein `setup-node`, kein `npm ci`.** Einzige
  Node-Nutzung: `node scripts/add-license-headers.mjs --check` (Z. 85) auf
  Runner-Default-Node.
- `verify-pr-source.yml`: nur `pull_request` → `main`; lehnt PRs ab, deren
  `head_ref` nicht `staging` ist (Z. 14–19).
- Folge: Bei einem PR gegen `staging` läuft der fehlerhafte Schritt
  **nicht** — die PR-Checks können den Fix nicht verifizieren. Nachweis
  erst im Push-Lauf von `staging-ci.yml` nach dem Merge (Job `version`)
  bzw. in `release.yml` bei Push auf `main`/Tag.

## 6. Herkunft der Fehler-Logzeile

Log laut Issue:

```
Run set -euo pipefail
[semantic-release]: node version >=26.13 is required. Found v24.22.3.
```

Abgleich mit den Workflow-Schritten:

- `staging-ci.yml` „Determine next version" (Z. 262–279): Das `run:`-Skript
  beginnt exakt mit `set -euo pipefail` (Z. 268) und ruft
  `npx semantic-release …` direkt auf, Ausgabe via `tee /dev/stderr`
  sichtbar (Z. 269). → **eindeutige Übereinstimmung**.
- `release.yml` „Resolve release version" (Z. 52–57): Run-Zeile wäre
  `Run node scripts/resolve-release-version.mjs`; ein scheiternder Dry-Run
  würde zusätzlich den Wrapper-Fehler `semantic-release dry-run failed
  (exit code 1): …` aus `resolve-release-version.mjs` Z. 112 werfen.
- `release.yml` „Create automatic GitHub release" (Z. 233–239): Run-Zeile
  wäre `Run npm run release`.
- Weitere `set -euo pipefail`-Blöcke rufen kein semantic-release:
  `release.yml` Z. 252 („Create manual-tag GitHub release"), Z. 263
  („Upload assets to existing GitHub release"), `staging-ci.yml` Z. 141
  („Enforce coverage threshold"), Z. 410 („Create GitHub pre-release").
- `staging-ci.yml` Z. 17 (coverage step) nutzt `set -o pipefail` ohne
  `-euo`; `security-scan/action.yml` Z. 17, 27 ebenfalls `set -o pipefail`.
- Fazit: Der gepostete Log stammt aus `staging-ci.yml`, Job `version`,
  Schritt „Determine next version". `release.yml` ist mit denselben
  Node-24-Pins betroffen, produziert aber anders geformte Logs.

## 7. Relevante Historie (git log)

- `db72e70` (2026-11-23, `dependabot[bot]`): „chore(deps-dev): bump
  semantic-release from 25.0.9 to 26.0.1" — ändert `package.json` (+2/−1)
  und `package-lock.json` (1649 Zeilen Diff). Das ist der Commit, der die
  `>=26.13`-Anforderung ins Repo brachte.
- Die `node-version`-Pins wurden mit der Release-Pipeline eingeführt:
  `git log -S "node-version"` → `9183c5b` („feat: Vollstaendige
  Release-Pipeline"), `e608338` („feat: Release-Management, Pre-Releases
  und Staging-Promotion (Issue #29)").
- Jüngste Workflow-Änderungen (sowohl `staging-ci.yml` als auch
  `release.yml`): `22658b6` (iOS/Xcode 27.2, Closes #204), `9a636b0`
  (.NET 11-Umstellung), `5857874`, `7c522ea`, `2354c26`
  („Supply-Chain-Härtung — Dependabot, SHA-Pinning, npm-Audit-Gate").
- `.github/dependabot.yml`: wöchentliche Updates für `nuget`, `npm` und
  `github-actions`, alle `target-branch: "staging"` — weitere
  Major-Bumps von `semantic-release` treffen also zuerst `staging`.
- Historie `package.json`/`package-lock.json` (Auszug): `db72e70`,
  `2f17049`, `a162fe3`, `e608338`, `840c312` („chore: Add package-lock.json
  for npm ci"), `9183c5b`.

## 8. Testinfrastruktur für diese Änderung

- `npm test` = `node --test "scripts/*.test.mjs"` (`package.json` Z. 7).
  Vorhandene Testdateien: `scripts/resolve-release-version.test.mjs`,
  `scripts/release-assets.test.mjs`,
  `scripts/create-update-manifest.test.mjs` — testen die Skript-Logik
  (Version-Parsing, Asset-Listen, Manifest), **nicht** die Workflows.
- Es existieren keine Workflow-/YAML-Tests und kein Test, der die
  `node-version`-Pins gegen `engines` prüft.
- `scripts/Run-StaticChecks.ps1` (prüft, was der CI-Job `static-checks`
  prüft): Restore (`dotnet restore`), Format
  (`dotnet format --verify-no-changes --severity error`), LicenseHeaders
  (`node scripts/add-license-headers.mjs --check`), Security
  (`dotnet list package --vulnerable` **und**
  `npm audit --package-lock-only --audit-level=high`), Build
  (`dotnet build -c Release -p:TreatWarningsAsErrors=true`). Kein
  Node-Versions-Check; der Workflow-Fix tangiert das Skript nicht
  (betrifft keine C#-Dateien, aber `npm audit` nutzt `package-lock.json`).

## 9. Sonstige Befunde

- `issue.md` (Repo-Root) enthält den Original-Issuetext (#215): PR gegen
  `staging`, Admin-Merge nach erfolgreichen Checks, Fehlerfreiheit
  sicherstellen.
- `docs/help/release-management/troubleshooting.md` dokumentiert bereits
  verwandte Fehlerbilder (`semantic-release --dry-run` ohne
  releasefähige Änderung, `npm audit`-Befunde in der
  semantic-release-Toolchain) — kein Eintrag zum Node-Versionsfehler.
- `staging-ci.yml` Job `version` hängt an `needs: [detect-backmerge,
  static-checks, build-and-test]` (Z. 238) und läuft nur, wenn kein
  Backmerge erkannt wird (Z. 239).
