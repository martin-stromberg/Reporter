# Detail: GitHub-Actions-Workflows

Alle sieben von `ci-instructions.md` (Abschnitt 2) geforderten Workflow-Dateien existieren bereits unter `.github/workflows/`. Drei davon sind durch Auskommentieren der `on:`-Blöcke deaktiviert — dieser Zustand stammt aus dem Branch `task/914985b55d4b46bda3d814608516a888-cicd-pipeline-pausieren` („CI/CD-Pipeline pausieren"). Auf `origin/main` sind die `on:`-Trigger derselben Dateien noch aktiv; `git diff origin/main..origin/staging -- .github/` zeigt als einzige `.github`-Änderung das Auskommentieren der drei Trigger.

## `staging-ci.yml` — `name: Pre-Release`

**Status:** Trigger deaktiviert. `on: push → staging` ist auskommentiert (Zeilen 3–6).

| Element | Zeilen | Ist-Zustand | Vorlagen-Referenz (ci-instructions.md) |
|---------|--------|-------------|-----------------------------------------|
| `concurrency: staging-ci`, `cancel-in-progress: true` | 8–10 | vorhanden | Abschnitt 5 |
| `permissions: contents/checks/pull-requests: write` | 12–15 | vorhanden | — |
| Job `static-checks` (`name: static checks`) | 18–49 | `windows-latest`, `IncludeIosTarget: false`, MAUI-Workload-Restore (`dotnet workload restore Reporter.sln`), `dotnet restore Reporter.sln -r win-x64`, `dotnet format --verify-no-changes --severity error`, Composite Action `security-scan` (Artifact `vulnerable-packages-staging`), `dotnet build -c Release -p:TreatWarningsAsErrors=true` | Abschnitt 4/5; auf Reporter/Windows/MAUI adaptiert |
| Job `build-and-test` (`name: build & test`) | 51–120 | `windows-latest`, Restore `-r win-x64`, `dotnet build -c Release`, `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release --no-build` mit `coverlet.runsettings` + TRX-/Console-Logger (Zeilen 75–81), ReportGenerator 5.5.11, Coverage-Threshold 70 % (Zeilen 90–104), Artefakt-Uploads `coverage-report-staging` / `test-results-staging` | Abschnitt 4/5; adaptiert |
| Job `version` | 122–175 | `needs: [static-checks, build-and-test]` (Z. 124), `if: success()` (Z. 125), `ubuntu-latest`; Outputs `changed`, `version`, `rc_tag`, `rc_version` (Z. 128–132); `npm ci`; `npx semantic-release --dry-run --no-ci --branches staging` mit `RESOLVE_DRY_RUN: 'true'` (Z. 148–165, Regex toleriert Klein-/Großschreibung `[Tt]he`); RC-Nummer via `git tag --list "v${version}-rc.*" | wc -l` + 1 (Z. 167–175) | Abschnitt 5.1 |
| Job `prerelease` | 177–203 | `needs: [version]` (Z. 179), `if: needs.version.outputs.changed == 'true'` (Z. 180), `windows-latest`; ruft `./.github/actions/build-and-package` mit `release-version: rc_version` und `release-tag: rc_tag` (Z. 187–191); `gh release create "$tag" release-win-x64.zip update.json --prerelease --generate-notes --target $GITHUB_SHA` (Z. 193–203) | Abschnitt 5.2 |

**Abweichungen / fehlende Teile gegenüber der Vorlage:**

- **`detect-backmerge`-Job fehlt vollständig.** Die Vorlage (Abschnitte 4 und 5) verlangt `needs: [detect-backmerge, static-checks, build-and-test]` am `version`-Job und `needs: [detect-backmerge, version]` am `prerelease`-Job, jeweils mit `if: needs.detect-backmerge.outputs.is_backmerge != 'true'`. Stattdessen hängt `version` nur an `[static-checks, build-and-test]` mit pauschalem `if: success()`.
- `rc_version` ist korrekt in `build-and-package` verdrahtet (Z. 190) — der in Vorlagen-Abschnitt 11.2 beschriebene Bug liegt hier **nicht** vor.
- Die Asset-Liste des `gh release create`-Aufrufs (Z. 199) enthält nur `release-win-x64.zip` und `update.json` — keine iOS-/Android-Artefakte.

## `release.yml` — `name: Release`

**Status:** Trigger deaktiviert. `on: push → branches: [main], tags: ['v*.*.*']` ist auskommentiert (Zeilen 3–6).

| Element | Zeilen | Ist-Zustand | Vorlagen-Referenz |
|---------|--------|-------------|--------------------|
| `permissions: contents/issues/pull-requests: write` | 8–11 | vorhanden | — |
| Job `release`, `windows-latest`, `IncludeIosTarget: false`, `concurrency: release`, `cancel-in-progress: false` | 14–20 | vorhanden | Abschnitt 8.3 |
| Checkout `fetch-depth: 0`, `fetch-tags: true` | 22–26 | vorhanden | — |
| Setup Node.js 24 + `npm ci` | 28–34 | vorhanden | — |
| „Remove local pre-release tags from main branch" (`git tag -d 'v*-rc.*'`) | 36–41 | **zusätzlich zur Vorlage** — verhindert, dass semantic-release RC-Tags als Basis interpretiert | nicht in Vorlage |
| `node scripts/resolve-release-version.mjs` (`id: version`) | 43–47 | vorhanden | Abschnitt 8.3 |
| Tag-Checkout für Asset-Repair (`upload-existing`, inkl. HEAD-vs-Tag-Verifikation) | 49–57 | vorhanden | Abschnitt 8.3 |
| Release-Gate: `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` | 59–61 | auf `Reporter.Tests` adaptiert (kein `MyApp.Tests`); läuft nur bei `released == 'true' && release_action == 'create'`; **ohne** `--no-build`, `--no-restore` und Coverage | Abschnitt 8.3 |
| `build-and-package` mit `release-version: version`, `release-tag: tag` | 63–68 | vorhanden, nur bei `released == 'true'` | Abschnitt 8.3 |
| Automatisches Release via `npm run release` (semantic-release) | 70–77 | `RELEASE_ASSET_PATHS: release-win-x64.zip` (nur ein Asset), `RELEASE_MANIFEST_PATH: update.json`, `RELEASE_VERSION` | Abschnitt 8.3/8.4 |
| Manuelles Tag-Release via `gh release create ... --generate-notes` | 79–89 | vorhanden, Assets `release-win-x64.zip update.json` | Abschnitt 8.3/11.3 |
| Repair-Pfad `gh release upload ... --clobber` | 91–99 | vorhanden | Abschnitt 8.3 |

**Auffälligkeiten:**

- Der Workflow enthält **keinen** `actions/setup-dotnet`-Step und kein `dotnet restore`/`dotnet workload restore` in den eigenen Steps. Das Release-Gate (`dotnet test`, Z. 61) läuft vor der Composite Action, die das SDK-Setup erst intern ausführt. Es verlässt sich auf das im `windows-latest`-Runner-Image vorinstallierte .NET SDK; `dotnet test` ohne `--no-restore` restauriert implizit. Da `Reporter.Tests` auf `net10.0` zielt und kein MAUI referenziert, ist kein Workload nötig — die SDK-Version des Runner-Images muss jedoch .NET 10 können.
- `RELEASE_ASSET_PATHS` (Z. 74) und die Asset-Listen der `gh`-Aufrufe (Z. 87, 99) enthalten nur das Windows-ZIP — keine weiteren Plattform-Artefakte.

## `staging-to-main-promotion.yml` — `name: Staging to Main Promotion`

**Status:** aktiv. Trigger `workflow_run` auf `"Pre-Release"`, `types: [completed]`, `branches: [staging]` (Zeilen 3–7). Der Display-Name `Pre-Release` stimmt mit `staging-ci.yml` Zeile 1 überein (Vorlagen-Hinweis 11.5 beachtet).

- `permissions: contents: read, pull-requests: write, issues: write` (Z. 9–12); `concurrency: staging-to-main-promotion`, `cancel-in-progress: false` (Z. 14–16).
- Job `promote` (Z. 19–62): nur bei `workflow_run.conclusion == 'success'`; Checkout auf `workflow_run.head_sha` mit `fetch-depth: 0`; `git fetch origin main`; Diff-Check via `git diff --name-only origin/main HEAD` bzw. `git rev-list origin/main..HEAD --count` → Output `commits_ahead` (Z. 33–41); `gh label create automated-promotion --color 0E8A16 --force` (Z. 43–47); Draft-PR `staging → main` mit Label `automated-promotion`, nur wenn kein offener PR existiert (Z. 49–62).
- Entspricht der Vorlage (Abschnitt 9) nahezu wörtlich.

## `sync-staging-with-main.yml` — `name: Backmerge Main to Staging`

**Status:** aktiv. Trigger `push → main` (Zeilen 3–5) — feuert auf **jeden** Push, nicht nur nach erfolgreichem Release (Vorlage Abschnitt 9, Design-Entscheidung).

- `permissions: contents: read, pull-requests: write, issues: write` (Z. 7–10).
- Job `backmerge` (Z. 13–48): Checkout `main` mit `fetch-depth: 0`; `git fetch origin staging`; `commits_behind` via `git rev-list origin/staging..HEAD --count` (Z. 27); Label `automated-backmerge` (`1D76DB`) per `gh label create --force` (Z. 30–34); PR `main → staging` mit Label, nur wenn `commits_behind > 0` und kein offener PR existiert (Z. 36–48); PR-Body enthält den Hinweis auf „Create a merge commit" (Z. 46).

**Abweichung von der Vorlage:** Die Vorlage (Zeile ~933) zählt `git rev-list HEAD..origin/staging --count` (Commits, die in `staging`, aber nicht in `main` sind); die Repo-Version zählt `origin/staging..HEAD` — also Commits, die `staging` hinter `main` zurückliegt. Das ist für den benannten Zweck („staging is behind main") die korrekte Richtung; die Vorlagen-Fassung würde in diesem Punkt das Gegenteil messen. Zusätzlich sind Label- und PR-Step im Repo mit `if: commits_behind > 0` belegt (Vorlage: unbedingt).

## `verify-pr-source.yml` — `name: Verify PR Source`

**Status:** aktiv. Trigger `pull_request → main` (Zeilen 3–6). Job `verify-source` lehnt jeden PR nach `main` ab, dessen `github.head_ref` nicht `staging` ist (Z. 12–17). Abweichung zur Vorlage (Abschnitt 3): Die Vorlage sieht **kein** `name:`-Feld vor (Anzeige als Dateiname); die Datei setzt `name: Verify PR Source` — funktional ohne Auswirkung.

## `pr-staging-ci.yml` — `name: PR CI for Staging`

**Status:** Trigger deaktiviert. `on: pull_request → staging` (`opened`, `synchronize`, `reopened`) ist auskommentiert (Zeilen 3–10).

- `concurrency: pr-staging-${{ github.event.pull_request.number }}`, `cancel-in-progress: true` (Z. 12–14); `permissions: contents: read, checks: write` (Z. 16–18).
- Jobs `static-checks` (Z. 21–52) und `build-and-test` (Z. 54–123) sind inhaltlich identisch zu den gleichnamigen Jobs in `staging-ci.yml` (gleiche Schritte, andere Artefaktnamen `vulnerable-packages-pr`, `coverage-report-pr`, `test-results-pr`), laufen parallel ohne `needs:` — hybrides Jobmodell der Vorlage eingehalten.

**Abweichungen / fehlende Teile gegenüber der Vorlage (Abschnitt 4):**

- **`detect-backmerge`-Job fehlt** — die Vorlage sieht ihn auch hier vor (Voraussetzung, um den vom Backmerge-Workflow geöffneten `main → staging`-PR von den Gates auszunehmen).
- Der in der Vorlage vorhandene `back-merge-skip`-Job fehlt entsprechend ebenfalls.

## `security-scan.yml` — `name: Security Scan`

**Status:** aktiv. Trigger `schedule` (`cron: '0 6 * * 1'`, montags 06:00 UTC; Vorlage: `0 4 * * 1`) plus `workflow_dispatch` (Zeilen 3–6). Einziger Job `security-scan` auf `windows-latest` mit MAUI-Workload-Restore und Aufruf der Composite Action `security-scan` (Artefakt `vulnerable-packages-weekly`, Z. 12–36). Kein `pull_request`-Trigger — entspricht der Vorlagen-Entscheidung (Abschnitt 10). Kein Änderungsbedarf im Scope der Anforderung.

## Bestand auf `origin/main` vs. `origin/staging` (Cold-Start-Relevanz)

- Alle sieben Workflow-Dateien existieren bereits auf `origin/main` **mit aktiven Triggern**; das Auskommentieren geschah nur auf `staging`. Damit ist der in Vorlagen-Abschnitt 11.5 beschriebene Cold-Start für `staging-to-main-promotion.yml` (`workflow_run`) und `sync-staging-with-main.yml` (`push → main`) bereits überstanden — beide Dateien liegen auf dem Default-Branch.
- Achtung: Die auf `main` liegenden Workflow-Versionen enthalten denselben Stand wie `staging` abzüglich der Kommentare — d. h. auch dort fehlt `detect-backmerge` usw. Änderungen an diesen Dateien wirken für `workflow_run`-/`push → main`-Trigger erst, wenn sie auf `main` ankommen.
- Git-Tags (lokal vorhanden, 10 Stück): `v0.0.1` (einziges stabiles Release-Tag), `v0.0.2-rc.1`, `v0.1.0-rc.1` bis `v0.1.0-rc.7`, `v1.0.0-rc.1`. Die Pipeline hat also bereits RCs und ein stabiles Release erzeugt, bevor sie pausiert wurde.
