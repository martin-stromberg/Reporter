# Detail: Release-Tooling (Skripte und npm-/semantic-release-Konfiguration)

## `scripts/resolve-release-version.mjs`

Datei: `scripts/resolve-release-version.mjs` (164 Zeilen). Wird von `release.yml` Zeile 47 als erster fachlicher Step ausgeführt und steuert alle nachfolgenden Schritte über `GITHUB_OUTPUT`.

| Element | Zeilen | Ist-Zustand |
|---------|--------|-------------|
| `VERSION_PATTERN` | 4 | SemVer-Regex `vX.Y.Z` mit optionalem Prerelease-Suffix |
| `AUTOMATIC_RELEASE_BRANCHES` | 5 | `["main"]` — entspricht der Vorlage |
| `EXPECTED_ASSETS` | 6 | `["release-win-x64.zip", "update.json"]` — entspricht dem aktuellen Ein-Plattform-Stand; bei zusätzlichen Plattform-Artefakten anzupassen |
| `setOutput` | 8–14 | schreibt `name=value` in `GITHUB_OUTPUT`; wirft, wenn die Variable nicht gesetzt ist |
| `parseManualTag(tagName)` (exportiert) | 16–25 | validiert `vX.Y.Z`-Tags, liefert Version ohne `v` |
| `classifyWorkflowRef({refType, refName})` (exportiert) | 27–35 | `tag` → `manual` (Version aus Tag), Branch `main` → `automatic`, sonst Fehler |
| `releaseHasExpectedAsset(release)` (exportiert) | 37–42 | prüft, ob alle `EXPECTED_ASSETS` mit `state === "uploaded"` und `size > 0` vorhanden sind |
| `incompleteReleases(releases)` (intern) | 44–51 | filtert Releases ohne vollständige Assets; **Prerelease-Guard vorhanden** (Z. 46–48: `if (release.prerelease) return false;`) — Vorlagen-Fix aus 11.1 ist eingebaut |
| `ghApi(args)` | 53–71 | Wrapper um `gh api` (`GH_TOKEN` aus `GITHUB_TOKEN`); `HTTP 404` → `null`; parst JSON |
| `getGitHubRelease(tag)` | 73–76 | `repos/<owner>/<repo>/releases/tags/<tag>` via `GITHUB_REPOSITORY` |
| `listGitHubReleases()` | 78–94 | paginiert (`per_page=100`) über alle Releases |
| `runSemanticReleaseDryRun()` | 96–109 | `npx semantic-release --dry-run --no-ci` mit `RESOLVE_DRY_RUN: "true"`; extrahiert „the next release version is X.Y.Z" |
| `resolveManualRelease(version, tag)` | 111–120 | existiert+vollständig → `released: false`/`release_action: none`; existiert+unvollständig → `upload-existing`; fehlt → `create` |
| `resolveAutomaticRelease()` | 122–143 | Dry-Run ohne neue Version → Fallback-Scan aller Releases, repariert das **älteste** unvollständige (`incomplete[incomplete.length - 1]`, Z. 130–131); sonst Existenz-/Vollständigkeitsprüfung wie manueller Pfad |
| `resolveReleaseVersion()` (exportiert) | 145–162 | klassifiziert `GITHUB_REF_TYPE`/`GITHUB_REF_NAME`, schreibt Outputs `released`, `reason`, `version`, `tag`, `release_kind`, `release_action` |
| Top-Level-Aufruf | 164 | `resolveReleaseVersion()` wird **beim Modulimport ausgeführt** |

**Auffälligkeiten:**

- Die exportierten Funktionen (`parseManualTag`, `classifyWorkflowRef`, `releaseHasExpectedAsset`) sind offensichtlich für Unit-Tests vorgesehen (Vorlage Abschnitt 8.2 verlangt eine Testsuite inkl. Regressionstest für den Prerelease-Guard). **Es existieren keine Tests für dieses Skript** (kein `*.test.*`, kein Node-Test-Runner, kein `test`-Script in `package.json`).
- Der unbedingte Top-Level-Aufruf in Zeile 164 hat einen Import-Seiteneffekt: Jeder `import` des Moduls führt `resolveReleaseVersion()` aus, das ohne gesetztes `GITHUB_REF_TYPE`/`GITHUB_REF_NAME` mit „Unsupported release ref" bzw. ohne `GITHUB_OUTPUT` mit „GITHUB_OUTPUT is not set" wirft — Unit-Tests können die Funktionen so nicht ohne Weiteres importieren.
- `execFileSync` wird in Zeile 1 importiert, aber nirgends verwendet.

## `release.config.js`

Datei: `release.config.js` (26 Zeilen). CommonJS (`module.exports`), wird von `semantic-release` (`npm run release`) und indirekt vom Dry-Run gelesen.

| Element | Zeilen | Ist-Zustand | Vorlagen-Referenz (Abschnitt 8.4) |
|---------|--------|-------------|------------------------------------|
| `releasePlugins` | 1–15 | `@semantic-release/commit-analyzer` und `@semantic-release/release-notes-generator` jeweils mit `preset: "conventionalcommits"`; `@semantic-release/github` mit `successComment: false`, `failComment: false` (Vorlagen-Fix 11.1/8.4 eingebaut) | entspricht der Vorlage |
| Asset-Liste `@semantic-release/github` | 7–10 | `RELEASE_ASSET_PATHS?.split(";")[0]` → `release-win-x64.zip`; `RELEASE_MANIFEST_PATH` → `update.json`; `.filter(asset => asset.path !== undefined)` (Repo-Zusatz; Vorlage filtert nicht) | Vorlage hat zusätzlich `split(";")[1]` für `release-linux-x64.zip` |
| `dryRunPlugins` | 17 | nur `commit-analyzer` — vermeidet `verifyConditions`-Netzwerkaufruf von `@semantic-release/github` im Dry-Run | entspricht der Vorlage |
| `branches` | 20–23 | **`["main", { name: "staging", prerelease: "rc" }]`** | Vorlage verlangt `branches: ["main"]` — der RC-Suffix wird in `staging-ci.yml` manuell angehängt und der Dry-Run nutzt `--branches staging` als Override. Abweichung: `staging` ist hier zusätzlich als semantic-release-Prerelease-Branch konfiguriert |
| `tagFormat` | 24 | `v${version}` | entspricht der Vorlage |
| Plugin-Umschaltung | 25 | `RESOLVE_DRY_RUN === "true"` → `dryRunPlugins` | entspricht der Vorlage |

## `package.json` / `package-lock.json`

Datei: `package.json` (14 Zeilen); `package-lock.json` vorhanden (~193 KB), Voraussetzung für `npm ci` in `staging-ci.yml` (Z. 146) und `release.yml` (Z. 34).

- `"name": "reporter"`, `"private": true`, Script `"release": "semantic-release"` (Z. 5).
- devDependencies (Z. 8–12): `semantic-release ^25.0.9`, `@semantic-release/commit-analyzer ^13.0.1`, `@semantic-release/release-notes-generator ^14.1.1`, `@semantic-release/github ^12.0.9`, `conventional-changelog-conventionalcommits ^9.3.1`.
- **Abweichung zur Vorlage:** Alle Versionen mit `^`-Range; die Vorlage (Abschnitt 8.4) pinnt exakt (`"25.0.9"` usw.). Über die Lock-Datei ist der installierte Stand dennoch reproduzierbar.
- Kein `test`-Script, kein Node-Test-Framework — bestätigt das Fehlen von Skript-Tests.

## Weitere relevante Skripte

### `scripts/Run-StaticChecks.ps1` (110 Zeilen)

Lokales Gegenstück zum `static-checks`-CI-Job (vgl. `AGENTS.md`-Regel „Local Static Checks"): `Invoke-PackageRestore` (`dotnet restore Reporter.sln -r win-x64`), `Invoke-FormatCheck` (`dotnet format --verify-no-changes --severity error`), `Invoke-SecurityScan` (`dotnet list package --vulnerable --include-transitive`), `Invoke-StaticAnalysisBuild` (`dotnet build -c Release -p:TreatWarningsAsErrors=true`). Setzt `$env:IncludeIosTarget = 'false'` (Z. 31). Parameter `-Check {All|Format|Security|Build|Restore}`, `-SkipRestore`. Muss aus dem Repo-Root laufen.

### `scripts/iOS-Deployment.ps1` (479 Zeilen) + `iOS-Deployment.md`

Lokales Build-/Deployment-Skript für das iOS-Target (`net10.0-ios`, Z. 57): Aktionen `build`/`simulator`/`device`/`list`/`menu`; auf Windows via Pair-to-Mac (`ServerAddress`/`ServerUser`/`ServerPassword`, `_DotNetRootRemoteDirectory`), Codesigning über `IOS_CODESIGN_KEY`/`IOS_PROVISIONING_PROFILE`/`CodesignEntitlements` (`ArchiveOnBuild=true` → `.ipa`, Z. 214–253). Zeigt: iOS-Signing ist lokal bereits vorgesehen, in der CI aber noch nicht angebunden (keine Secrets/kein `macos`-Job vorhanden).

### `.githooks/`

Enthält `pre-commit`, `pre-push` sowie Python-Prüfskripte (`csproj-xmldoc-check.py`, `enum-coverage-check.py`, `no-notimplemented-check.py`, `razor-l10n-check.py`, `razor-usage-check.py`, `translation-check.py`) und `install-hooks.cmd`/`.sh`. Für die Anforderung nicht direkt relevant, aber Teil der lokalen Qualitätssicherung.
