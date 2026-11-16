<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Release-Management — Business Rules

## Versionsformat und RC-Zählung

**Beschreibung:** Stabile Releases und Release Candidates folgen festen Tag-Formaten, damit `semantic-release`, die RC-Zählung und die Asset-Auflösung konsistent arbeiten.

**Bedingungen:**
- Stabiles Release: Tag `vX.Y.Z` (`tagFormat: "v${version}"` in `release.config.js`), Versionsnummer ausschließlich aus Conventional-Commits-Analyse auf `main`.
- RC-Pre-Release: Tag `vX.Y.Z-rc.N`; `N` beginnt pro Zielversion bei 1 und wird aus `git tag --list "v<version>-rc.*" | wc -l` + 1 abgeleitet.
- `release.config.js` führt `branches: ["main"]` — `staging` ist bewusst **kein** semantic-release-Prerelease-Branch; der RC-Suffix wird in `staging-ci.yml` manuell angehängt. Eine Doppelkonfiguration (`{ name: "staging", prerelease: "rc" }`) würde den Suffix zweimal vergeben.
- Manuelle Tags müssen `v` + SemVer entsprechen (`parseManualTag`/`VERSION_PATTERN`); ein führendes `v` ohne gültige Version oder Nicht-SemVer wird abgelehnt.

**Verhalten:**
- Push auf `staging` mit releasefähigem Commit → `rc_version = <version>-rc.<N>`, Pre-Release `v<version>-rc.<N>`.
- Push auf `staging` ohne releasefähige Commits → `changed=false`, kein Pre-Release, kein Promotion-PR-Anlass (der `promote`-Job prüft zusätzlich den Diff).
- Push auf `main` → `semantic-release` erzeugt Tag + stabiles Release automatisch; manueller Tag-Push → `gh release create` im `publish`-Job.

**Umsetzung:** `version`-Job in `staging-ci.yml` (`steps.semver`/`steps.rc`), `resolve-release-version.mjs` (`classifyWorkflowRef`, `parseManualTag`), `release.config.js`.

## Backmerge-Erkennung und Gate-Überspringung

**Beschreibung:** Reine Backmerges von `main` nach `staging` enthalten ausschließlich bereits auf `main` geprüften Code; ein erneuter Durchlauf der Gates wäre redundant und würde RC-Zählung sowie PR-Gates unnötig belasten.

**Bedingungen:**
- Ein Head-Commit gilt als Backmerge, wenn `origin/main` direkter Merge-Parent ist **oder** der Baum identisch ist (`git diff --quiet origin/main HEAD`).
- Erkannt wird in `staging-ci.yml` (Push) und `pr-staging-ci.yml` (PR) jeweils im Job `detect-backmerge` (Output `is_backmerge`).

**Verhalten:**
- `is_backmerge == 'true'` → `static-checks`, `build-and-test` und `version` werden übersprungen; in `pr-staging-ci.yml` meldet `back-merge-skip` ein grünes Ergebnis.
- `is_backmerge != 'true'` → normale Gates.

**Umsetzung:** Job `detect-backmerge` in `staging-ci.yml` bzw. `pr-staging-ci.yml`.

## Release-Auflösung: `create` / `upload-existing` / `none`

**Beschreibung:** Der `resolve`-Job entscheidet vor jeder Veröffentlichung, ob ein Release neu angelegt, repariert oder übersprungen wird — dadurch sind Pipeline-Läufe idempotent und unterbrochene Asset-Uploads heilen sich selbst.

**Bedingungen:**
- `EXPECTED_ASSETS` = `release-win-x64.zip`, `release-android.apk`, `update.json`, plus `release-ios.ipa` nur wenn `IOS_SIGNING_ENABLED == 'true'` (Env aus `vars.IOS_SIGNING_ENABLED`).
- Ein Release gilt als vollständig, wenn jedes erwartete Asset mit `state == "uploaded"` und `size > 0` existiert (`releaseHasExpectedAsset`).
- Prerelease-Guard: `incompleteReleases` ignoriert `prerelease == true` — RC-Releases werden niemals als „unvollständig" repariert.

**Verhalten:**
- Ref = manueller Tag: Release existiert + vollständig → `none`; existiert + unvollständig → `upload-existing`; fehlt → `create` (`release_kind=manual`).
- Ref = `main`-Push, Dry-Run liefert Version: gleiche Prüfung auf `v<version>` → `create` / `upload-existing` / `none` (`release_kind=automatic`).
- Ref = `main`-Push, Dry-Run liefert **keine** Version: alle Releases werden paginiert gescannt; das **älteste** unvollständige Nicht-Prerelease wird als `upload-existing` repariert; keine Treffer → `none`.

**Umsetzung:** `resolve-release-version.mjs` (`resolveManualRelease`, `resolveAutomaticRelease`, `incompleteReleases`, `repairIncompleteRelease`).

## Verbindliche vs. konditionale Assets (iOS-Gate)

**Beschreibung:** `release-ios.ipa` ist Teil der Asset-Logik, darf aber Releases ohne iOS-Signierung nicht als unvollständig markieren oder das Manifest verfälschen.

**Bedingungen:**
- `vars.IOS_SIGNING_ENABLED == 'true'` → `buildReleaseAssets` ergänzt `release-ios.ipa`; sonst nicht.
- Dieselbe Variable steuert `package-ios`-Jobs, `EXPECTED_ASSETS`, `RELEASE_ASSETS`/`RELEASE_ASSET_PATHS`/`RELEASE_ASSET_FILES` und damit `update.json` sowie die `gh`-Argumentlisten.

**Verhalten:**
- Variable gesetzt → iOS-Artefakt wird gebaut, ins Manifest aufgenommen und hochgeladen; fehlt es, gilt das Release als unvollständig.
- Variable nicht gesetzt → Job übersprungen, Asset taucht in keiner Liste auf; die Aggregation läuft durch (`skipped`-Needs sind kein Fehlschlag).

**Umsetzung:** `scripts/release-assets.mjs` (`buildReleaseAssets`, `emitReleaseAssetEnv`), `resolve-release-version.mjs` (`EXPECTED_ASSETS`), `if: vars.IOS_SIGNING_ENABLED == 'true'` in `staging-ci.yml`/`release.yml`.

## Promotion- und Backmerge-Regeln

**Beschreibung:** `staging` und `main` werden ausschließlich über PRs synchronisiert; direkte Pushes auf `main` aus Feature-Branches sind untersagt.

**Bedingungen:**
- Promotion: erfolgreicher `Pre-Release`-Lauf + `git diff origin/main..HEAD` nicht leer + kein offener PR `staging` → `main` → Draft-PR mit Label `automated-promotion`.
- Backmerge: Push auf `main` + `git rev-list origin/staging..HEAD --count > 0` + kein offener PR `main` → `staging` → PR mit Label `automated-backmerge`.
- PRs nach `main` mit `head_ref != 'staging'` werden von `verify-pr-source.yml` abgelehnt.

**Verhalten:**
- Der Backmerge-PR muss per **„Create a merge commit"** gemergt werden — Rebase oder Squash würden den Release-Tag aus der `staging`-Historie lösen und die RC-Zählung (`git tag --list "v*-rc.*"`) sowie die semantic-release-Analyse brechen.
- Bereits offene Promotion-/Backmerge-PRs werden nicht dupliziert.

**Umsetzung:** `staging-to-main-promotion.yml`, `sync-staging-with-main.yml`, `verify-pr-source.yml`.

## TFM-Schalter für lokale Builds vs. Packaging

**Beschreibung:** `net10.0-android` darf lokale Builds und die Gates nicht mit dem Android-Workload belasten; nur die Packaging-Jobs aktivieren das TFM.

**Bedingungen:**
- `IncludeAndroidTarget` (Default `false`), `IncludeIosTarget` (Default `true` auf Windows) steuern `TargetFrameworks` in `Reporter.csproj` über `Condition`-Kombinationen je Betriebssystem.
- Die Gates (`static-checks`, `build-and-test`, `release-gate`) setzen `IncludeIosTarget: false` **und** `IncludeAndroidTarget: false`; `build-and-package` setzt `IncludeIosTarget: false` (Android bleibt über den Default `false` aus); `package-android` setzt `IncludeAndroidTarget: true`/`IncludeIosTarget: false`; `package-ios` läuft auf macOS mit `IncludeAndroidTarget: false`.

**Verhalten:**
- Ohne Schalter baut die Solution wie zuvor nur Windows (+ iOS auf macOS).
- `dotnet workload restore`/`restore`/`publish` laufen in `package-android` ausschließlich mit `IncludeAndroidTarget: true`.

**Umsetzung:** `src/Reporter/Reporter.csproj` (`IncludeAndroidTarget`, `TargetFrameworks`-Conditions), `env:`-Blöcke in den Workflows und Composite Actions.

## Unterschiedliche Fail-Schwellen im Security-Gate

**Beschreibung:** Das Security-Gate blockiert Änderungen mit bekannten Schwachstellen in den Abhängigkeiten. NuGet und npm werden mit absichtlich unterschiedlichen Schwellen geprüft.

**Bedingungen:**
- NuGet: `dotnet list Reporter.sln package --vulnerable --include-transitive` — **jeder** gemeldete Befund schlägt fehl.
- npm: `npm audit --package-lock-only --audit-level=high` gegen `package-lock.json` — nur Befunde der Severity `high`/`critical` schlagen fehl; moderate Befunde werden im Report `npm-audit.txt` weiterhin ausgewiesen, blockieren aber nicht.
- Sämtliche npm-Abhängigkeiten des Repositories sind `devDependencies` (semantic-release-Toolchain) — ein Audit mit `--omit=dev` wäre wirkungslos, daher wird das vollständige Lockfile geprüft. Ein `npm ci`/`node_modules` ist dank `--package-lock-only` nicht nötig; Node/npm ist auf den gehosteten Runnern vorinstalliert.

**Verhalten:**
- Bei Befund oberhalb der jeweiligen Schwelle → `::error::` + `exit 1` im Job `static checks` bzw. `security-scan`; das Report-Artefakt (`vulnerable-packages.txt` + `npm-audit.txt`) wird trotzdem hochgeladen (`if: always()`).
- Die Schwelle `high` ist bewusst so gewählt: vorhandene moderate Advisories der Toolchain wären nur per Breaking-Downgrade behebbar und würden das Gate ab Tag 1 dauerrot machen — sie bleiben sichtbar, ohne die Pipeline zu blockieren.

**Umsetzung:** `.github/actions/security-scan/action.yml` (Steps `Scan for vulnerable packages`, `Audit npm toolchain`, `Upload report`); lokale Spiegelung mit identischer Befehlszeile in `Invoke-SecurityScan` (`scripts/Run-StaticChecks.ps1`).

## SHA-Pinning externer Action-Referenzen

**Beschreibung:** Externe GitHub Actions werden über den Commit-SHA des Tags referenziert statt über den beweglichen Tag — ein von Dritten nachträglich übernommener Tag kann die Pipeline so nicht mehr verändern.

**Bedingungen:**
- Jede Referenz `uses: <owner>/<repo>@<ref>` in `.github/workflows/*.yml` und `.github/actions/*/action.yml` steht auf einem 40-stelligen Commit-SHA; der Original-Tag bleibt als Kommentar erhalten (`uses: actions/checkout@<sha>  # v7`).
- Lokale Referenzen `uses: ./.github/actions/...` tragen keinen `@<ref>` und sind vom Pinning ausgenommen.
- Der `github-actions`-Eintrag in `.github/dependabot.yml` ist die zugehörige Pflichtbedingung: Er hält die gepinnten SHAs (inkl. Kommentar) per wöchentlichem Update-PR aktuell — ohne ihn würden die Pins veralten.

**Verhalten:**
- Neue externe Action-Referenz → Tag-Auflösung per `git ls-remote` und gepinnte Form eintragen; Dependabot pflegt die Aktualisierung.
- Dependabot-**Security**-Update-PRs ignorieren `target-branch` und zielen auf `main` — sie scheitern an `verify-pr-source.yml` und sind separat zu behandeln (Details siehe [Wiederkehrende Reviews](wiederkehrende-reviews.md)).

**Umsetzung:** `.github/dependabot.yml` (`updates[].package-ecosystem: "github-actions"`); Konvention ist auch in `ci-instructions.md` §2 verankert.
