# CI Workflows for a Staging-Based .NET Release Pipeline

This document describes, step by step, how to set up a complete GitHub Actions CI/CD pipeline
for a .NET application (or library) that follows a `staging` → `main` branch model with
automated pre-releases, semantic versioning, and self-healing release-asset repair.

This is not a theoretical template: it is distilled from a real, multi-repository migration
across several production .NET projects (web apps, a WPF desktop app, and a library), where
every pattern below was implemented, broken, debugged, and fixed at least once. The
"Troubleshooting" section at the end lists the concrete bugs that were found this way, so you
don't have to rediscover them.

## Who this is for

- You have (or want) a `main` branch that only ever contains released, tagged code, and a
  `staging` branch that feature branches merge into first.
- You want every PR to staging to run quality gates (format, security, static analysis, tests,
  coverage) before merge.
- You want every push to `staging` to automatically create a versioned pre-release (RC) build.
- You want every push to `main` to automatically create the real release, with zero manual
  version bookkeeping (no hand-edited `<Version>` in a `.csproj`, no manually typed changelog).
- You optionally want the app to be able to self-update (via a library such as
  [msTools.Updater](https://github.com/martin-stromberg/msTools.Updater)) by reading a JSON
  manifest attached to each GitHub release.

## 1. Prerequisites

Before writing any workflow file, set these up once per repository:

1. **Two long-lived branches**: `main` (protected, releases only) and `staging` (protected,
   integration branch). All feature branches target `staging`, never `main` directly.
2. **Branch protection on `staging`**: require a pull request, require the `static checks` and
   `build & test` status checks (added in step 3 below) to pass, require the branch to be up
   to date.
3. **Branch protection on `main`**: require a pull request (the only PRs allowed are the
   automated `staging` → `main` promotion PR created in step 8), restrict who can push.
4. **Two labels** (create them once via `gh label create`, or let the promotion/backmerge
   workflows create them on first run — see steps 8/9):
   - `automated-promotion` (color `0E8A16`) — used on the `staging` → `main` promotion PR.
   - `automated-backmerge` (color `1D76DB`) — used on the `main` → `staging` backmerge PR.
5. **Decide your release-asset shape**: a single self-contained ZIP (desktop app,
   single-runtime web app), one ZIP per runtime identifier (a cross-platform web app publishing
   both `win-x64` and `linux-x64`), or a single NuGet `.nupkg` (a library — see the callout in
   section 8.5).

## 2. The seven workflow files, at a glance

Create these files under `.github/workflows/` in the order shown. Each one is covered in its
own section below.

| # | File | Display name | Trigger | Purpose |
|---|------|--------------|---------|---------|
| 1 | `verify-pr-source.yml` | *(no `name:`, shown as filename)* | `pull_request` → `main` | Rejects any PR into `main` that doesn't come from `staging`. |
| 2 | `pr-staging-ci.yml` | `PR CI for Staging` | `pull_request` → `staging` | Quality gates for every PR. |
| 3 | `staging-ci.yml` | `Pre-Release` | `push` → `staging` | Same gates again, plus version determination and RC pre-release creation. |
| 4 | `staging-to-main-promotion.yml` | `Staging to Main Promotion` | `workflow_run` (after `Pre-Release` succeeds) | Opens a draft PR `staging` → `main`. |
| 5 | `sync-staging-with-main.yml` | `Backmerge Main to Staging` | `push` → `main` | Opens a PR `main` → `staging` after every release, so staging never drifts. |
| 6 | `release.yml` | `Release` | `push` → `main`, or a manually pushed `v*.*.*` tag | Creates the final GitHub release with real, versioned assets. |
| 7 | `security-scan.yml` | `Security Scan` | `schedule` (weekly) | Independent dependency-vulnerability scan, decoupled from code changes. |

Two composite actions live under `.github/actions/` and are shared between the workflows above
(covered in section 6):

- `.github/actions/build-and-package/action.yml` — build, publish, ZIP, and manifest generation.
- `.github/actions/security-scan/action.yml` — the actual vulnerability-scan command, used by
  both `pr-staging-ci.yml`/`staging-ci.yml` (as a blocking gate) and `security-scan.yml` (as a
  standalone weekly check), so the logic exists exactly once.

## 3. Step 1 — `verify-pr-source.yml`

The simplest file. Its only job is to make sure nobody opens a PR into `main` from anywhere
other than `staging` (which would bypass every quality gate above).

```yaml
name: Verify PR Source

on:
  pull_request:
    branches:
      - main

jobs:
  verify-source:
    runs-on: ubuntu-latest
    steps:
      - name: Reject PRs into main that do not come from staging
        run: |
          if [ "${{ github.head_ref }}" != "staging" ]; then
            echo "::error::PRs into main are only allowed from staging (got '${{ github.head_ref }}')."
            exit 1
          fi
```

## 4. Step 2 — `pr-staging-ci.yml` ("PR CI for Staging")

This is the first file with real substance. Two design decisions matter here:

- **The hybrid job model**: `static-checks` (fast: format, security, static analysis) and
  `build-and-test` (slow: the actual test suite) run **in parallel**, with no `needs:` between
  them. A formatting mistake is visible within seconds, without waiting for a multi-minute test
  run to finish first. Do not make `build-and-test` depend on `static-checks` — that was tried
  in an earlier iteration and just made every PR slower for no benefit.
- **Detecting a pure back-merge**: when `sync-staging-with-main.yml` (step 9) opens its
  automated `main` → `staging` PR, that PR's own CI run doesn't need to repeat every gate — the
  code already went through them on `main`. A `detect-backmerge` job checks this up front and
  short-circuits the rest of the workflow when true.

```yaml
name: PR CI for Staging

on:
  pull_request:
    branches:
      - staging
    types:
      - opened
      - synchronize
      - reopened

concurrency:
  group: pr-staging-${{ github.event.pull_request.number }}
  cancel-in-progress: true

permissions:
  contents: read
  checks: write

jobs:
  detect-backmerge:
    runs-on: ubuntu-latest
    outputs:
      is_backmerge: ${{ steps.check.outputs.is_backmerge }}
    steps:
      - name: Checkout
        uses: actions/checkout@v7
        with:
          fetch-depth: 0

      - name: Check whether this PR is a pure back-merge from main
        id: check
        run: |
          git fetch origin main:refs/remotes/origin/main --no-tags

          main_sha="$(git rev-parse origin/main)"
          is_main_merge_parent=false
          for parent in $(git show -s --format=%P HEAD); do
            if [[ "$parent" == "$main_sha" ]]; then
              is_main_merge_parent=true
              break
            fi
          done

          if [[ "$is_main_merge_parent" == "true" ]]; then
            echo "is_backmerge=true" >> "$GITHUB_OUTPUT"
            echo "::notice::Back-merge from main detected - CI checks will be skipped."
          elif git diff --quiet origin/main HEAD; then
            echo "is_backmerge=true" >> "$GITHUB_OUTPUT"
            echo "::notice::Pure back-merge from main detected (tree identical) - CI checks will be skipped."
          else
            echo "is_backmerge=false" >> "$GITHUB_OUTPUT"
          fi

  static-checks:
    name: static checks
    needs: detect-backmerge
    if: needs.detect-backmerge.outputs.is_backmerge != 'true'
    runs-on: ubuntu-latest
    timeout-minutes: 15
    steps:
      - name: Checkout
        uses: actions/checkout@v7

      - name: Setup .NET
        uses: actions/setup-dotnet@v6
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore MyApp.sln

      - name: Format check
        # --severity error restricts this to whitespace/style plus genuine analyzer errors.
        # Without it, dotnet format --verify-no-changes also fails on warning-level analyzer
        # diagnostics that the Static analysis step below deliberately allows (see its own
        # comment) - the two steps would otherwise fight over the same warnings.
        run: dotnet format MyApp.sln --verify-no-changes --no-restore --severity error

      - name: Security scan
        uses: ./.github/actions/security-scan
        with:
          solution-path: MyApp.sln
          artifact-name: vulnerable-packages-pr

      - name: Static analysis
        run: dotnet build MyApp.sln --configuration Release --no-restore -p:TreatWarningsAsErrors=true

  build-and-test:
    name: build & test
    needs: detect-backmerge
    if: needs.detect-backmerge.outputs.is_backmerge != 'true'
    runs-on: ubuntu-latest
    timeout-minutes: 30
    steps:
      - name: Checkout
        uses: actions/checkout@v7

      - name: Setup .NET
        uses: actions/setup-dotnet@v6
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore MyApp.sln

      - name: Build
        run: dotnet build MyApp.sln --configuration Release --no-restore

      - name: Test
        run: >
          dotnet test MyApp.sln --configuration Release --no-build
          --collect:"XPlat Code Coverage"
          --logger "trx;LogFileName=test-results.trx"
          --logger "console;verbosity=normal"

      - name: Install ReportGenerator
        run: dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.5.11

      - name: Generate coverage report
        run: reportgenerator "-reports:**/TestResults/**/coverage.cobertura.xml" "-targetdir:coverage-report" "-reporttypes:TextSummary"

      - name: Enforce coverage threshold
        env:
          COVERAGE_THRESHOLD: '70'
        run: |
          set -euo pipefail
          summary="coverage-report/Summary.txt"
          if ! grep -q "Line coverage:" "$summary"; then
            echo "Could not determine line coverage from the generated report." >&2
            exit 1
          fi
          coverage="$(grep -m1 "Line coverage:" "$summary" | grep -oE '[0-9]+([.,][0-9]+)?' | head -n1 | tr ',' '.')"
          threshold="$COVERAGE_THRESHOLD"
          echo "Line coverage: ${coverage}% (threshold: ${threshold}%)"
          awk -v cov="$coverage" -v thr="$threshold" 'BEGIN { exit !(cov >= thr) }'

      - name: Upload coverage report
        if: always()
        uses: actions/upload-artifact@v7
        with:
          name: coverage-report-pr
          path: coverage-report/
          retention-days: 14

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v7
        with:
          name: test-results-pr
          path: MyApp.Tests/TestResults/*.trx
          retention-days: 14

  back-merge-skip:
    needs: detect-backmerge
    if: needs.detect-backmerge.outputs.is_backmerge == 'true'
    runs-on: ubuntu-latest
    steps:
      - name: Back-merge detected
        run: echo "This PR is a pure back-merge from main - all checks skipped."
```

### If your project has a best-effort test category

If some tests are flaky on hosted runners (browser-driven E2E tests are the classic case), give
them their own `dotnet test` invocation with `continue-on-error: true`, upload their results
separately, and **exclude them from coverage collection** so a flaky UI test can never fail the
whole PR or dilute the coverage number:

```yaml
      - name: Test (blocking)
        run: dotnet test MyApp.Tests/MyApp.Tests.csproj --no-build -c Release --filter "Category!=E2E" --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx"

      - name: Test E2E (best-effort)
        continue-on-error: true
        env:
          PLAYWRIGHT_TRACE: '1'
          PLAYWRIGHT_ARTIFACTS: '1'
        run: dotnet test MyApp.Tests.E2E/MyApp.Tests.E2E.csproj --no-build -c Release --logger "trx;LogFileName=test-results-e2e.trx"
```

See section 11.4 for why the two `PLAYWRIGHT_*` environment variables matter even when your
Playwright fixture already supports tracing.

## 5. Step 3 — `staging-ci.yml` ("Pre-Release")

Same `detect-backmerge` / `static-checks` / `build-and-test` trio as step 2 (copy them
verbatim), plus two more jobs that only run on a genuine push to `staging`.

### 5.1 The `version` job

This determines the next version number **without publishing anything**, using
[semantic-release](https://semantic-release.gitbook.io/) in dry-run mode, then appends an RC
suffix by hand:

```yaml
  version:
    name: version
    needs: [detect-backmerge, static-checks, build-and-test]
    if: needs.detect-backmerge.outputs.is_backmerge != 'true'
    runs-on: ubuntu-latest
    timeout-minutes: 10
    outputs:
      changed: ${{ steps.semver.outputs.changed }}
      version: ${{ steps.semver.outputs.version }}
      rc_tag: ${{ steps.rc.outputs.rc_tag }}
      rc_version: ${{ steps.rc.outputs.rc_version }}
    steps:
      - name: Checkout
        uses: actions/checkout@v7
        with:
          fetch-depth: 0
          fetch-tags: true

      - name: Setup Node.js
        uses: actions/setup-node@v7
        with:
          node-version: '24'

      - name: Install dependencies
        run: npm ci

      - name: Determine next version
        # release.config.js only lists "main" as a release branch (see that file's own
        # comment) - staging is deliberately not a configured prerelease branch there. The
        # --branches override below treats "staging" as a plain release branch for this one
        # dry-run only, so the resulting X.Y.Z carries no prerelease identifier - the RC suffix
        # is appended manually in the next step instead.
        id: semver
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          set -o pipefail
          output="$(npx semantic-release --dry-run --no-ci --branches staging 2>&1 | tee /dev/stderr)"

          version=""
          changed="false"
          if [[ "$output" =~ The\ next\ release\ version\ is\ ([0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?) ]]; then
            version="${BASH_REMATCH[1]}"
            changed="true"
          fi

          echo "version=$version" >> "$GITHUB_OUTPUT"
          echo "changed=$changed" >> "$GITHUB_OUTPUT"

      - name: Determine RC number
        if: steps.semver.outputs.changed == 'true'
        id: rc
        run: |
          version="${{ steps.semver.outputs.version }}"
          rc_number=$(( $(git tag --list "v${version}-rc.*" | wc -l) + 1 ))
          echo "rc_number=$rc_number" >> "$GITHUB_OUTPUT"
          echo "rc_tag=v${version}-rc.${rc_number}" >> "$GITHUB_OUTPUT"
          # RC-suffixed version without the leading "v" (e.g. "1.2.3-rc.1"), distinct from
          # rc_tag ("v1.2.3-rc.1"). This exact output - not the bare `version` above - must
          # feed build-and-package's release-version input in the job below, otherwise
          # update.json's own "version" field and the packaged asset's embedded version end up
          # reporting the plain stable version instead of the actual pre-release. This single
          # wrong wire-up was the single most common bug found across every project that
          # implemented this pattern - see section 11.2.
          echo "rc_version=${version}-rc.${rc_number}" >> "$GITHUB_OUTPUT"
```

**RC tag format**: always `v<major>.<minor>.<patch>-rc.<n>`, lowercase `rc`, `n` starting at 1
and counting existing tags for that exact version. Example: stable `v1.2.3` plus a `feat:`
commit → `v1.3.0-rc.1`; the next push to staging before that ships → `v1.3.0-rc.2`.

### 5.2 The `prerelease` job

```yaml
  prerelease:
    name: prerelease
    needs: [detect-backmerge, version]
    if: needs.detect-backmerge.outputs.is_backmerge != 'true' && needs.version.outputs.changed == 'true'
    runs-on: ubuntu-latest
    timeout-minutes: 20
    steps:
      - name: Checkout
        uses: actions/checkout@v7

      - name: Build and package release candidate
        uses: ./.github/actions/build-and-package
        with:
          release-version: ${{ needs.version.outputs.rc_version }}
          release-tag: ${{ needs.version.outputs.rc_tag }}

      - name: Create GitHub pre-release
        env:
          GH_TOKEN: ${{ github.token }}
        run: |
          tag="${{ needs.version.outputs.rc_tag }}"
          gh release create "$tag" release-win-x64.zip release-linux-x64.zip update.json \
            --target "$GITHUB_SHA" \
            --title "$tag" \
            --prerelease \
            --generate-notes
```

## 6. Composite actions

### 6.1 `build-and-package/action.yml`

Encapsulates build/publish/zip/manifest generation so it exists exactly once, shared between
`staging-ci.yml`'s `prerelease` job and `release.yml`'s final release job:

```yaml
name: Build and package

inputs:
  release-version:
    description: Version without a leading v, written into update.json and passed as -p:Version.
    required: true
  release-tag:
    description: Release tag (e.g. v1.2.3 or v1.2.3-rc.1), used to build each asset's download URL.
    required: true

runs:
  using: composite
  steps:
    - name: Setup .NET
      uses: actions/setup-dotnet@v6
      with:
        dotnet-version: '10.0.x'

    - name: Restore
      run: dotnet restore MyApp.sln
      shell: bash

    - name: Publish (win-x64, self-contained)
      run: >
        dotnet publish MyApp.Web/MyApp.Web.csproj -p:Version=${{ inputs.release-version }}
        --configuration Release --framework net10.0 --runtime win-x64
        --self-contained true --no-restore --output artifacts/publish-win-x64
      shell: bash

    - name: Publish (linux-x64, self-contained)
      run: >
        dotnet publish MyApp.Web/MyApp.Web.csproj -p:Version=${{ inputs.release-version }}
        --configuration Release --framework net10.0 --runtime linux-x64
        --self-contained true --no-restore --output artifacts/publish-linux-x64
      shell: bash

    - name: Create release ZIPs
      # Asset names deliberately fixed (no project name/version baked in) - both the manifest
      # step below and resolve-release-version.mjs's expected-asset list depend on these exact
      # names never changing between versions.
      shell: bash
      run: |
        set -euo pipefail
        (cd artifacts/publish-win-x64 && zip -r ../../release-win-x64.zip .)
        (cd artifacts/publish-linux-x64 && zip -r ../../release-linux-x64.zip .)

    - name: Create update manifest
      shell: bash
      env:
        VERSION: ${{ inputs.release-version }}
        TAG: ${{ inputs.release-tag }}
      run: |
        set -euo pipefail
        win_sha="$(sha256sum release-win-x64.zip | awk '{print $1}')"
        win_size="$(stat -c%s release-win-x64.zip)"
        linux_sha="$(sha256sum release-linux-x64.zip | awk '{print $1}')"
        linux_size="$(stat -c%s release-linux-x64.zip)"

        jq -n \
          --arg version "$VERSION" \
          --arg releaseNotes "MyApp release ${TAG}" \
          --arg publishedAt "$(date -u +'%Y-%m-%dT%H:%M:%SZ')" \
          --arg winUrl "https://github.com/${GITHUB_REPOSITORY}/releases/download/${TAG}/release-win-x64.zip" \
          --arg winSha "$win_sha" --argjson winSize "$win_size" \
          --arg linuxUrl "https://github.com/${GITHUB_REPOSITORY}/releases/download/${TAG}/release-linux-x64.zip" \
          --arg linuxSha "$linux_sha" --argjson linuxSize "$linux_size" \
          '{
            version: $version, releaseNotes: $releaseNotes, publishedAt: $publishedAt,
            assets: [
              { platform: "windows", runtimeIdentifier: "win-x64", assetName: "release-win-x64.zip", assetUrl: $winUrl, sha256: $winSha, sizeBytes: $winSize },
              { platform: "linux", runtimeIdentifier: "linux-x64", assetName: "release-linux-x64.zip", assetUrl: $linuxUrl, sha256: $linuxSha, sizeBytes: $linuxSize }
            ]
          }' > update.json
```

### 6.2 `security-scan/action.yml`

The one command shared by three call sites (`static-checks` in both PR/staging workflows, plus
the standalone weekly `security-scan.yml`). The critical detail is that it must actually **fail**
the check, not merely print a warning:

```yaml
name: Security scan

inputs:
  solution-path:
    required: true
  artifact-name:
    required: true

runs:
  using: composite
  steps:
    - name: Scan for vulnerable packages
      shell: bash
      run: |
        set -o pipefail
        output="$(dotnet list "${{ inputs.solution-path }}" package --vulnerable --include-transitive 2>&1 | tee vulnerable-packages.txt)"
        if echo "$output" | grep -qi "has the following vulnerable packages\|Severity"; then
          echo "::error::Vulnerable packages detected - see the uploaded report."
          exit 1
        fi

    - name: Upload report
      if: always()
      uses: actions/upload-artifact@v7
      with:
        name: ${{ inputs.artifact-name }}
        path: vulnerable-packages.txt
        retention-days: 14
```

## 7. The update manifest, and (optionally) `release-metadata.json`

### 7.1 `update.json` — always generated

The composite action above already produces this. It is a **separate release asset**, sitting
next to the ZIPs, describing what's available to download:

```json
{
  "version": "1.4.2",
  "releaseNotes": "...",
  "publishedAt": "2026-08-31T12:00:00Z",
  "assets": [
    { "platform": "windows", "runtimeIdentifier": "win-x64", "assetName": "release-win-x64.zip", "assetUrl": "https://github.com/<owner>/<repo>/releases/download/<tag>/release-win-x64.zip", "sha256": "<sha256>", "sizeBytes": 12345678 },
    { "platform": "linux", "runtimeIdentifier": "linux-x64", "assetName": "release-linux-x64.zip", "assetUrl": "...", "sha256": "...", "sizeBytes": 0 }
  ]
}
```

### 7.2 `release-metadata.json` — only if the app self-updates via msTools.Updater

This is a **completely different file with a different purpose**, easy to confuse with
`update.json`. If (and only if) your application references
[msTools.Updater](https://github.com/martin-stromberg/msTools.Updater) and relies on its
default `IInstalledVersionProvider`, that library determines "what version am I currently
running?" at runtime by reading `release-metadata.json` from the application's own installation
directory. Nothing generates this file automatically - your own build pipeline must write it
**inside the publish output, before zipping it**:

```yaml
    - name: Write release metadata
      shell: bash
      env:
        RELEASE_VERSION: ${{ inputs.release-version }}
      run: |
        set -euo pipefail
        for rid in win-x64 linux-x64; do
          jq -n \
            --arg version "$RELEASE_VERSION" \
            --arg publishedAt "$(date -u +'%Y-%m-%dT%H:%M:%SZ')" \
            --arg commitSha "$GITHUB_SHA" \
            --arg repository "$GITHUB_REPOSITORY" \
            --arg runtimeIdentifier "$rid" \
            '{version: $version, publishedAt: $publishedAt, commitSha: $commitSha, repository: $repository, runtimeIdentifier: $runtimeIdentifier}' \
            > "artifacts/publish-$rid/release-metadata.json"
        done
```

Insert this step **between** publishing and zipping. If your app does not reference
msTools.Updater, skip this entirely - a real-world audit across six projects found that copying
this step "just in case" (because it was in a template) produced a harmless but pointless file
that nothing ever read. Only add it if you've verified `IInstalledVersionProvider` is actually
in use.

## 8. Step 4 — `release.yml` ("Release")

This is the most involved file, because it has to answer a question no simple `on: push`
trigger can: *"was this release already published, partially published, or never published at
all?"* — and it has to answer that correctly whether the triggering event was an automatic push
to `main` or someone manually pushing a `vX.Y.Z` tag.

### 8.1 The trigger

```yaml
on:
  push:
    branches: [main]
    tags: ['v*.*.*']
```

Both are always configured together. A manually pushed tag is a fully supported, first-class
way to cut a release - not a fallback.

### 8.2 `scripts/resolve-release-version.mjs`

This script is invoked as the very first step and drives every conditional step after it via
`GITHUB_OUTPUT`. Its job, precisely:

1. **Classify the ref.** A tag push (`refType === "tag"`) is a *manual* release: the version
   comes directly from the tag name. A branch push to `main` is *automatic*: the version comes
   from `semantic-release --dry-run`.
2. **Check whether a release for that version already exists**, and if so, whether it already
   has every expected asset uploaded.
   - Fully present → do nothing (`released: false`).
   - Existing but missing an asset → **repair** it (`release_action: "upload-existing"`):
     re-upload just the missing asset(s) to the *existing* release, without creating a new tag.
   - Absent → create it (`release_action: "create"`).
3. If nothing forced a specific version (automatic path, no releasable commits since the last
   tag) **and** no in-progress release needs repairing, fall back to scanning **all** existing
   GitHub releases for one that's missing its asset and repair the oldest one found.

```js
import { execFileSync, spawnSync } from "node:child_process";
import { appendFileSync } from "node:fs";

const VERSION_PATTERN = /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$/;
const AUTOMATIC_RELEASE_BRANCHES = ["main"];

export function parseManualTag(tagName) {
  if (!tagName?.startsWith("v")) {
    throw new Error(`Expected a vX.Y.Z tag, received '${tagName ?? ""}'.`);
  }
  const version = tagName.slice(1);
  if (!VERSION_PATTERN.test(version)) {
    throw new Error(`Tag '${tagName}' is not a valid vX.Y.Z release tag.`);
  }
  return version;
}

export function classifyWorkflowRef({ refType, refName }) {
  if (refType === "tag") {
    return { kind: "manual", version: parseManualTag(refName), tag: refName };
  }
  if (refType === "branch" && AUTOMATIC_RELEASE_BRANCHES.includes(refName)) {
    return { kind: "automatic" };
  }
  throw new Error(`Unsupported release ref '${refType ?? ""}:${refName ?? ""}'.`);
}

export function releaseHasExpectedAsset(release) {
  const expected = ["release-win-x64.zip", "release-linux-x64.zip", "update.json"];
  const assets = release.assets ?? [];
  return expected.every((name) =>
    assets.some((asset) => asset.name === name && asset.state === "uploaded" && asset.size > 0)
  );
}

// CRITICAL GUARD - see section 11.1 for the production incident that made this necessary.
function incompleteReleases(releases) {
  return releases.filter((release) => {
    if (release.prerelease) {
      return false;
    }
    return !releaseHasExpectedAsset(release);
  });
}
```

The full script also needs `getGitHubRelease`, `listGitHubReleases` (paginated), a
`runSemanticReleaseDryRun` helper, and the top-level `resolveReleaseVersion` function that ties
classification + existence-check + fallback-scan together and writes all outputs
(`released`, `reason`, `version`, `tag`, `release_kind`, `release_action`) via
`GITHUB_OUTPUT`. Write unit tests for this file - it is pure logic with no I/O once the
dependencies (`getGitHubRelease`, `listGitHubReleases`, etc.) are injected, and it is exactly
the kind of code where a subtle bug silently breaks production releases (again, section 11.1).

### 8.3 The workflow steps that consume its output

```yaml
jobs:
  release:
    runs-on: ubuntu-latest
    concurrency:
      group: release
      cancel-in-progress: false
    steps:
      - name: Checkout
        uses: actions/checkout@v7
        with:
          fetch-depth: 0

      - name: Setup Node.js
        uses: actions/setup-node@v7
        with:
          node-version: '24'

      - name: Install dependencies
        run: npm ci

      - name: Resolve release version
        id: version
        env:
          GITHUB_TOKEN: ${{ github.token }}
        run: node scripts/resolve-release-version.mjs

      - name: Check out release tag for asset repair
        if: steps.version.outputs.release_action == 'upload-existing'
        run: |
          git fetch origin "refs/tags/${{ steps.version.outputs.tag }}"
          git checkout --detach "${{ steps.version.outputs.tag }}"
          if [ "$(git rev-parse HEAD)" != "$(git rev-parse "${{ steps.version.outputs.tag }}")" ]; then
            echo "::error::Release tag checkout did not resolve to the tagged commit."
            exit 1
          fi

      - name: Release gate - regular tests
        if: steps.version.outputs.released == 'true' && steps.version.outputs.release_action == 'create'
        run: dotnet test MyApp.Tests/MyApp.Tests.csproj --configuration Release

      - name: Build and package
        if: steps.version.outputs.released == 'true'
        uses: ./.github/actions/build-and-package
        with:
          release-version: ${{ steps.version.outputs.version }}
          release-tag: ${{ steps.version.outputs.tag }}

      - name: Create automatic GitHub release
        # Goes through semantic-release's own publish pipeline (release.config.js:
        # commit-analyzer, release-notes-generator, then @semantic-release/github, which
        # creates the tag, the GitHub release, and uploads the assets below).
        if: steps.version.outputs.released == 'true' && steps.version.outputs.release_action == 'create' && steps.version.outputs.release_kind == 'automatic'
        env:
          GITHUB_TOKEN: ${{ github.token }}
          RELEASE_ASSET_PATHS: release-win-x64.zip;release-linux-x64.zip
          RELEASE_MANIFEST_PATH: update.json
          RELEASE_VERSION: ${{ steps.version.outputs.version }}
        run: npm run release

      - name: Create manual-tag GitHub release
        # Plain gh CLI, not semantic-release: semantic-release only knows about
        # conventional-commit-driven releases on configured branches, not about a tag someone
        # pushed by hand. Use gh CLI here (not a third-party action) - see section 11.3.
        if: steps.version.outputs.released == 'true' && steps.version.outputs.release_action == 'create' && steps.version.outputs.release_kind == 'manual'
        env:
          GH_TOKEN: ${{ github.token }}
        run: |
          set -euo pipefail
          tag="${{ steps.version.outputs.tag }}"
          gh release create "$tag" release-win-x64.zip release-linux-x64.zip update.json \
            --title "$tag" \
            --generate-notes

      - name: Upload asset to existing GitHub release
        if: steps.version.outputs.released == 'true' && steps.version.outputs.release_action == 'upload-existing'
        env:
          GH_TOKEN: ${{ github.token }}
        run: |
          set -euo pipefail
          tag="${{ steps.version.outputs.tag }}"
          gh release upload "$tag" release-win-x64.zip release-linux-x64.zip update.json --clobber
```

### 8.4 `release.config.js`

```js
const releasePlugins = [
  ["@semantic-release/commit-analyzer", { preset: "conventionalcommits" }],
  ["@semantic-release/release-notes-generator", { preset: "conventionalcommits" }],
  [
    "@semantic-release/github",
    {
      assets: [
        { path: process.env.RELEASE_ASSET_PATHS?.split(";")[0], name: "release-win-x64.zip" },
        { path: process.env.RELEASE_ASSET_PATHS?.split(";")[1], name: "release-linux-x64.zip" },
        { path: process.env.RELEASE_MANIFEST_PATH, name: "update.json" }
      ],
      // The default GITHUB_TOKEN only has `contents: write` - it cannot comment on the PR(s)
      // associated with a released commit, which the plugin's success/fail steps otherwise
      // attempt by default. Without this, any release whose commit has an associated PR (e.g.
      // the routine staging -> main promotion PR) fails at that final comment step with a
      // GraphQL "Resource not accessible by integration" error - even though the release
      // itself already published successfully. See section 11.1.
      successComment: false,
      failComment: false
    }
  ]
];

// Used instead of releasePlugins whenever resolve-release-version.mjs runs its dry-run-only
// version check - avoids loading @semantic-release/github (and its verifyConditions network
// call) for a call that never actually publishes anything.
const dryRunPlugins = [["@semantic-release/commit-analyzer", { preset: "conventionalcommits" }]];

module.exports = {
  branches: ["main"],
  tagFormat: "v${version}",
  plugins: process.env.RESOLVE_DRY_RUN === "true" ? dryRunPlugins : releasePlugins
};
```

Required npm devDependencies:

```json
{
  "devDependencies": {
    "semantic-release": "25.0.9",
    "@semantic-release/commit-analyzer": "13.0.1",
    "@semantic-release/release-notes-generator": "14.1.1",
    "@semantic-release/github": "12.0.9",
    "conventional-changelog-conventionalcommits": "9.3.1"
  }
}
```

Do not skip `conventional-changelog-conventionalcommits`, and do not omit the `preset:
"conventionalcommits"` option on both plugins - without it, `commit-analyzer` silently falls
back to the Angular preset, which classifies commit messages by different rules. This is easy
to miss because nothing errors; it just quietly determines version bumps differently than the
rest of your commit-message conventions expect.

### 8.5 If you're publishing a library instead of an app

A library has no ZIP, no `update.json`, no self-update story. Its composite action reduces to:

```yaml
    - name: Pack (Release)
      run: dotnet pack MyLibrary/MyLibrary.csproj -c Release -o ./nupkg -p:Version=${{ inputs.release-version }}
      shell: bash
```

and its expected release asset (for `resolve-release-version.mjs`'s asset-repair check) is a
single `.nupkg`, not a ZIP triplet.

## 9. Steps 5–6 — `staging-to-main-promotion.yml` and `sync-staging-with-main.yml`

These two are close to boilerplate and should be copied close to verbatim; the only things to
adjust per project are the display names in comments.

```yaml
# staging-to-main-promotion.yml
name: Staging to Main Promotion

# Critical: this workflow_run trigger references the *display name* of staging-ci.yml, not its
# filename. If staging-ci.yml's `name:` field ever changes, this array must be updated in the
# same commit - otherwise promotion silently stops firing, with no error anywhere. See section
# 11.5 for what happens if this file doesn't exist yet on the default branch.
"on":
  workflow_run:
    workflows: ["Pre-Release"]
    types: [completed]
    branches: [staging]

permissions:
  contents: read
  pull-requests: write
  issues: write

concurrency:
  group: staging-to-main-promotion
  cancel-in-progress: false

jobs:
  promote:
    if: github.event.workflow_run.conclusion == 'success'
    runs-on: ubuntu-latest
    timeout-minutes: 15
    steps:
      - name: Checkout staging
        uses: actions/checkout@v7
        with:
          fetch-depth: 0
          ref: ${{ github.event.workflow_run.head_sha }}

      - name: Fetch main
        run: git fetch origin main

      - name: Check if main is behind staging
        id: diff
        run: |
          changed="$(git diff --name-only origin/main HEAD)"
          if [ -z "$changed" ]; then
            echo "commits_ahead=0" >> "$GITHUB_OUTPUT"
          else
            echo "commits_ahead=$(git rev-list origin/main..HEAD --count)" >> "$GITHUB_OUTPUT"
          fi

      - name: Ensure automated-promotion label exists
        if: steps.diff.outputs.commits_ahead != '0'
        env:
          GH_TOKEN: ${{ github.token }}
        run: gh label create automated-promotion --color "0E8A16" --description "Automated PR from staging to main" --force

      - name: Create promotion pull request
        if: steps.diff.outputs.commits_ahead != '0'
        env:
          GH_TOKEN: ${{ github.token }}
        run: |
          existing="$(gh pr list --base main --head staging --state open --json number)"
          if [ "$(echo "$existing" | jq length)" -eq 0 ]; then
            gh pr create \
              --base main --head staging \
              --title "Automated promotion from staging to main" \
              --body "Automated promotion from staging to main. Contains all changes that have been integrated and successfully validated on staging. Requires manual review and merge by a maintainer." \
              --draft \
              --label "automated-promotion"
          fi
```

```yaml
# sync-staging-with-main.yml
name: Backmerge Main to Staging

on:
  push:
    branches: [main]

permissions:
  contents: read
  pull-requests: write
  issues: write

jobs:
  backmerge:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout main
        uses: actions/checkout@v7
        with:
          fetch-depth: 0

      - name: Fetch staging
        run: git fetch origin staging

      - name: Check if staging is behind main
        id: diff
        run: |
          commits_behind="$(git rev-list HEAD..origin/staging --count)"
          echo "commits_behind=$commits_behind" >> "$GITHUB_OUTPUT"

      - name: Ensure automated-backmerge label exists
        env:
          GH_TOKEN: ${{ github.token }}
        run: gh label create automated-backmerge --color "1D76DB" --description "Automated PR from main to staging" --force

      - name: Create backmerge pull request
        env:
          GH_TOKEN: ${{ github.token }}
        run: |
          existing="$(gh pr list --base staging --head main --state open --json number)"
          if [ "$(echo "$existing" | jq length)" -eq 0 ]; then
            gh pr create \
              --base staging --head main \
              --title "Backmerge from main to staging" \
              --body "Automated back-merge PR after a promotion to main. Please merge with **Create a merge commit** (not Rebase) so the new release tag remains reachable from staging for future version calculations." \
              --label "automated-backmerge"
          fi
```

**This is triggered on every push to `main`, unconditionally** - not only after a successful
`release.yml` run. That's a deliberate decision: even if the release build itself fails,
`staging` should still learn about whatever landed on `main` (e.g. a hotfix merged directly),
so it never silently drifts out of sync.

**The backmerge PR must be merged with "Create a merge commit"**, not "Squash" or "Rebase" -
squashing or rebasing would detach the new release tag from staging's history, breaking the
`git tag --list` count that `staging-ci.yml`'s RC-number step (section 5.1) depends on.

## 10. Step 7 — `security-scan.yml`

```yaml
name: Security Scan

on:
  schedule:
    - cron: '0 4 * * 1'   # Monday 04:00 UTC
  workflow_dispatch:

jobs:
  security-scan:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v7

      - name: Setup .NET
        uses: actions/setup-dotnet@v6
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore MyApp.sln

      - name: Security scan
        uses: ./.github/actions/security-scan
        with:
          solution-path: MyApp.sln
          artifact-name: vulnerable-packages-weekly
```

Deliberately **not** triggered on `pull_request` - that duplication (a second, independent copy
of the same scan logic wired to PRs) is exactly the kind of drift this whole pattern exists to
avoid. The PR-blocking scan already happens inside `static-checks` via the shared composite
action from section 6.2; this file exists only to catch a vulnerability disclosed *after* your
last commit, for a dependency nobody has touched in months.

## 11. Troubleshooting: real bugs found doing this for real

Every item below was an actual production incident or a genuinely broken CI run, not a
hypothetical. If your pipeline breaks in one of these specific ways, the fix is likely the same.

### 11.1 Asset-repair hijacking an unrelated old prerelease

**Symptom:** A routine, otherwise-unrelated release build fails because it checked out an
ancient commit and a composite action (or any file introduced since) doesn't exist there yet -
e.g. `Can't find 'action.yml' for ./.github/actions/build-and-package`.

**Cause:** `incompleteReleases()` (section 8.2) not filtering out prereleases. Without the
`if (release.prerelease) return false;` guard, the fallback scan can pick up a genuinely old,
unrelated RC that never got its asset for legitimate reasons, and try to "repair" it - checking
out its historical commit, where current workflow files don't exist.

**Fix:** the guard shown in section 8.2. Add a regression test asserting a prerelease is never
selected for repair even when it's missing every asset and no new release is pending.

### 11.2 `update.json` reports the wrong version for a pre-release

**Symptom:** `v1.12.1-rc.3`'s `update.json` says `"version": "1.12.1"` (no RC suffix) - the
asset URL is correct, only the reported version is wrong. Anything reading this manifest to
decide "is a newer version available?" gets confused.

**Cause:** the `prerelease` job (section 5.2) passing the bare `version` output instead of
`rc_version` into `build-and-package`'s `release-version` input.

**Fix:** always double-check that exact wire-up. It is a one-line, easy-to-miss mistake with no
compile-time or even CI-time signal that anything is wrong - the pipeline runs green either way.

### 11.3 Two different release-creation mechanisms coexisting

**Symptom:** not a failure exactly, but an inconsistency: some projects in a fleet use
`softprops/action-gh-release` for manual-tag releases, others use the `gh` CLI directly, and
within a single project the "create" and "repair" paths might even use different tools from
each other.

**Fix:** standardize on the `gh` CLI everywhere. The concrete argument for this direction (not
just "pick one"): the asset-repair path (`upload-existing`) already has to use `gh release
upload` in every project, because there is no equivalent third-party action for editing an
existing release. Unifying the "create" path onto `gh` CLI too removes a third-party action
dependency (and its SHA-pinning maintenance burden) rather than adding one. `gh release create
... --generate-notes` is the CLI's exact equivalent of `softprops/action-gh-release`'s
`generate_release_notes: true`.

### 11.4 Nothing to inspect after a failed Playwright/E2E run

**Symptom:** an E2E test fails in CI; the only evidence is a bare pass/fail line in the trx
logger output, with no way to see what the browser was actually doing.

**Cause:** Playwright's own tracing (`context.Tracing.StartAsync`/`StopAsync`, capturing
screenshots, DOM snapshots, and sources) is opt-in and has to be wired up explicitly - it is not
implied by having Playwright tests at all. Two independent failure shapes were found in
practice: (a) tracing code was gated behind environment variables that the workflow file never
set, so it silently never activated even though the code existed; (b) tracing was never
implemented in the test fixture at all.

**Fix:** confirm your Playwright fixture actually calls `Tracing.StartAsync`/`StopAsync` per
test (writing to a fixed directory such as `playwright-traces/`), set any opt-in environment
variables it requires, and add an upload step:

```yaml
      - name: Upload Playwright traces
        if: always()
        uses: actions/upload-artifact@v7
        with:
          name: playwright-traces-pr
          path: playwright-traces/
          retention-days: 14
```

Uploading `if: always()` (not `if: failure()`) matters: it captures diagnostic evidence for a
run that intermittently degrades over a long test session without outright failing every test,
which is a real, observed failure mode on a shared, long-lived browser/server fixture - not
just a hypothetical.

### 11.5 `staging-to-main-promotion.yml` never fires, with no error anywhere

**Symptom:** `staging-ci.yml` ("Pre-Release") succeeds, but no promotion PR ever appears, and
nothing in the Actions log even mentions the promotion workflow.

**Cause:** a `workflow_run` trigger only activates using the version of that workflow file
present on the repository's **default branch** (usually `main`) - not the branch where the
triggering workflow actually ran. The very first time you add this pattern to an existing
repository, `staging-to-main-promotion.yml` (and `sync-staging-with-main.yml`, similarly gated
on `push: main`) exist only on `staging`; `main` doesn't have them yet. Calling the GitHub API
for this workflow returns `HTTP 404: workflow ... not found on the default branch`.

**Fix:** for the very first promotion after introducing this pattern, create the `staging` →
`main` PR by hand (`gh pr create --base main --head staging --draft --label
automated-promotion`, matching the body text step 9 generates). Once that PR is merged, `main`
has the workflow files too, and every subsequent push behaves automatically. This is a one-time
cold-start cost, not an ongoing problem - but if a fleet-wide CI migration lands on `staging`
across several repositories at once and none of them ever gets manually promoted, every single
one of them will independently hit this and stay silently stuck.

### 11.6 A test helper hardcodes `bin/Debug` and breaks only in the Release gate

**Symptom:** `release.yml`'s own "Release gate - regular tests" step (section 8.3) fails with
something like `System.InvalidOperationException: Sequence contains no matching element`, or a
`FileNotFoundException` for a generated asset - while the exact same test suite passes cleanly
in `pr-staging-ci.yml`.

**Cause:** the Release gate is very likely the *first* place in your entire pipeline that
actually runs `dotnet test --configuration Release`. Any test helper that locates a sibling
project's build output by hardcoding `bin/Debug/<tfm>` (common when a test project needs to
inspect or copy another project's published `wwwroot`, or find a companion executable) will
never find anything once that build genuinely happened under `Release`.

**Fix:** derive the configuration the test assembly itself was built under, instead of
hardcoding one:

```csharp
#if DEBUG
    private const string BuildConfiguration = "Debug";
#else
    private const string BuildConfiguration = "Release";
#endif
```

and use `BuildConfiguration` everywhere you'd otherwise have typed `"Debug"`. Since the helper
project is compiled with the exact same `--configuration` flag as everything else in the same
`dotnet test` invocation, this always matches reality.

### 11.7 A direct push to `main` succeeds despite branch protection

**Symptom:** pushing a hotfix commit directly to `main` (bypassing the PR requirement)
succeeds, and the remote prints something like:

```
remote: Bypassed rule violations for refs/heads/main:
remote: - Changes must be made through a pull request.
```

**Cause:** GitHub's branch protection has a separate "allow specified actors to bypass required
pull requests" setting, independent of the rule itself. Repository admins (and, depending on
configuration, certain roles) can be exempted from a rule that still applies to everyone else.
The rule is not actually enforced for that actor, even though the protection is otherwise
correctly configured and enforced for normal contributors.

**Not really a "fix"** - it's a configuration choice for the repository owner to make
deliberately (some maintainers do want an emergency-bypass path for themselves). The actionable
takeaway is: **never treat a successful direct push to a "protected" branch as proof the branch
is genuinely locked down** - verify the actual branch protection ruleset
(`gh api repos/<owner>/<repo>/branches/<branch>/protection`) instead of inferring policy from
one successful bypass.

## 12. Checklist

Copy this list into your repository's setup notes and tick items off as you go:

- [ ] `main` and `staging` branches exist; branch protection configured on both.
- [ ] `automated-promotion` and `automated-backmerge` labels created (or left for the workflows
      to create on first run).
- [ ] `.github/actions/security-scan/action.yml` created.
- [ ] `.github/actions/build-and-package/action.yml` created, including `release-metadata.json`
      generation **only if** the app actually references msTools.Updater.
- [ ] `.github/workflows/verify-pr-source.yml` created.
- [ ] `.github/workflows/pr-staging-ci.yml` created; hybrid `static-checks`/`build-and-test`
      model, no `needs:` between them.
- [ ] `.github/workflows/staging-ci.yml` created; `version` job outputs both `rc_tag` **and**
      `rc_version`, and `rc_version` (not the bare `version`) feeds `build-and-package`.
- [ ] `.github/workflows/staging-to-main-promotion.yml` created; `workflow_run` trigger
      references staging-ci.yml's *display name*, not its filename.
- [ ] `.github/workflows/sync-staging-with-main.yml` created; triggers on every push to `main`
      unconditionally.
- [ ] `.github/workflows/release.yml` created; both `branches: [main]` and `tags: ['v*.*.*']`
      configured as triggers.
- [ ] `scripts/resolve-release-version.mjs` created, with the prerelease guard from 11.1 and a
      test suite.
- [ ] `.github/workflows/security-scan.yml` created; `schedule`-only, no `pull_request` trigger.
- [ ] `release.config.js` created; `preset: "conventionalcommits"` on both `commit-analyzer` and
      `release-notes-generator`; `successComment`/`failComment: false` on `@semantic-release/github`.
- [ ] `package.json` has `conventional-changelog-conventionalcommits` as a devDependency.
- [ ] Coverage threshold (recommended: 70%) enforced in both `pr-staging-ci.yml` and
      `staging-ci.yml`.
- [ ] If the project has Playwright/E2E tests: tracing is actually wired up in the test fixture,
      and an "Upload Playwright traces" step exists in both CI workflows that run them.
- [ ] After the very first merge of this whole pattern into `staging`: manually create the
      first `staging` → `main` promotion PR (section 11.5's cold-start step) - don't wait for
      automation that cannot fire yet.
