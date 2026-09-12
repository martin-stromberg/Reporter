# Tasks: Release-Management, Pre-Releases und Staging-Promotion

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Release-Tooling | `resolve-release-version.mjs`: Top-Level-Aufruf hinter Main-Modul-Guard stellen (Importierbarkeit für Tests) | Offen | — |
| 2 | Release-Tooling | `resolve-release-version.mjs`: `incompleteReleases` exportieren und ungenutzten `execFileSync`-Import entfernen | Offen | — |
| 3 | Release-Tooling | `resolve-release-version.mjs`: `EXPECTED_ASSETS` auf finale Asset-Liste setzen (`release-win-x64.zip`, `release-android.apk`, `update.json`; `release-ios.ipa` konditional bei `IOS_SIGNING_ENABLED == 'true'`) | Offen | — |
| 4 | Release-Tooling | `release.config.js`: `branches` auf `["main"]` reduzieren (Eintrag `{ name: "staging", prerelease: "rc" }` entfernen) | Offen | — |
| 5 | Release-Tooling | `release.config.js`: `@semantic-release/github`-Assetliste dynamisch aus `RELEASE_ASSET_PATHS` (Basename-Mapping) + `RELEASE_MANIFEST_PATH` erzeugen | Offen | — |
| 6 | Release-Tooling | `scripts/create-update-manifest.mjs` anlegen (Asset-Liste aus `RELEASE_ASSETS`, sha256/sizeBytes, `assetUrl` aus `RELEASE_TAG`/`GITHUB_REPOSITORY`, Fehler bei fehlender Datei) | Offen | — |
| 7 | Release-Tooling | `package.json`: devDependencies auf exakte Versionen pinnen und Script `"test": "node --test scripts/"` ergänzen | Offen | — |
| 8 | Release-Tooling | `package-lock.json` via `npm install` regenerieren und `npm ci`-Konsistenz sicherstellen | Offen | — |
| 9 | Projektkonfiguration | `Reporter.csproj`: MSBuild-Schalter `IncludeAndroidTarget` (Default `false`) einführen | Offen | — |
| 10 | Projektkonfiguration | `Reporter.csproj`: `TargetFrameworks`-Bedingungen um `net10.0-android` erweitern (nur bei `IncludeAndroidTarget=true`) | Offen | — |
| 11 | Composite Actions | `build-and-package/action.yml`: Schritt „Create update manifest" entfernen (Manifest wandert in Aggregations-Job) | Offen | — |
| 12 | Composite Actions | `.github/actions/package-android/action.yml` anlegen (Setup .NET, Workload-Restore mit `IncludeAndroidTarget: true`, Publish `net10.0-android` → `release-android.apk`) | Offen | — |
| 13 | Composite Actions | `.github/actions/package-ios/action.yml` anlegen (macOS-Publish `net10.0-ios` mit Signierungs-Inputs → `release-ios.ipa`) | Offen | — |
| 14 | Workflows | `staging-ci.yml`: `on: push → staging`-Trigger einkommentieren | Offen | — |
| 15 | Workflows | `staging-ci.yml`: Job `detect-backmerge` gemäß Vorlage Abschnitt 4 ergänzen | Offen | — |
| 16 | Workflows | `staging-ci.yml`: `needs: detect-backmerge` + `if: is_backmerge != 'true'` an `static-checks` und `build-and-test` ergänzen | Offen | — |
| 17 | Workflows | `staging-ci.yml`: `version`-Job auf `needs: [detect-backmerge, static-checks, build-and-test]` und `if: is_backmerge != 'true'` umstellen | Offen | — |
| 18 | Workflows | `staging-ci.yml`: `prerelease` in `package-windows`/`package-android`/`package-ios`-Jobs plus Aggregations-Job (Manifest + `gh release create --prerelease` mit voller Asset-Liste) umbauen | Offen | — |
| 19 | Workflows | `pr-staging-ci.yml`: `on: pull_request → staging`-Trigger einkommentieren | Offen | — |
| 20 | Workflows | `pr-staging-ci.yml`: Jobs `detect-backmerge` und `back-merge-skip` ergänzen; `needs`/`if` an beiden Gates setzen | Offen | — |
| 21 | Workflows | `release.yml`: `on: push → main` + `tags: ['v*.*.*']`-Trigger einkommentieren | Offen | — |
| 22 | Workflows | `release.yml`: Einzeljob in `resolve` → `release-gate` + `package-*` → `publish` aufteilen (inkl. Tag-Checkout-Repair-Logik in den `package-*`-Jobs) | Offen | — |
| 23 | Workflows | `release.yml`: `actions/setup-dotnet` (`10.0.x`) im `release-gate`-Job vor `dotnet test` ergänzen | Offen | — |
| 24 | Workflows | `release.yml`: `RELEASE_ASSET_PATHS`, `RELEASE_ASSETS` und alle `gh release create`/`upload`-Aufrufe auf die vollständige Asset-Liste aktualisieren (iOS-Eintrag konditional bei `vars.IOS_SIGNING_ENABLED == 'true'`); `IOS_SIGNING_ENABLED` als Env an den Resolver-Step durchreichen | Offen | — |
| 25 | Tests | `scripts/resolve-release-version.test.mjs` anlegen (`parseManualTag`, `classifyWorkflowRef`, `releaseHasExpectedAsset`) | Offen | — |
| 26 | Tests | Regressionstest Prerelease-Guard in `resolve-release-version.test.mjs` (Vorlage 11.1: Prerelease nie für Repair selektieren) | Offen | — |
| 27 | Tests | `scripts/create-update-manifest.test.mjs` anlegen (Asset-Einträge, URLs, Hash/Größe, Fehlerfall fehlende Datei) | Offen | — |
| 28 | Konfiguration | Branch-Protection `staging` per `gh api` setzen (PR-Pflicht, Required Checks `static checks` + `build & test`, `strict: true`) und per GET verifizieren — Ausführung direkt durch den Agenten | Offen | — |
| 29 | Konfiguration | Branch-Protection `main` per `gh api` setzen (PR-Pflicht, Push-Restriktion, Required Check `verify-source`; keine Admin-Bypass-Ausnahme) und per GET verifizieren — Ausführung direkt durch den Agenten, nicht per Test-Push | Offen | — |
| 30 | Konfiguration | Labels `automated-promotion` (`0E8A16`) und `automated-backmerge` (`1D76DB`) per `gh label create`/`gh api` anlegen und per `gh label list` verifizieren — Ausführung direkt durch den Agenten | Offen | — |
| 31 | Konfiguration | iOS-Aktivierungspfad dokumentieren: `vars.IOS_SIGNING_ENABLED` bleibt ungesetzt; Signing-Secrets werden später extern bereitgestellt (Aktivierung dann ohne Codeänderung) | Offen | — |
| 32 | Verifikation | `npm test` lokal ausführen (Skript-Testsuite grün) | Offen | — |
| 33 | Verifikation | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` ausführen (239 Tests grün) | Offen | — |
| 34 | Verifikation | `.\scripts\Run-StaticChecks.ps1` ausführen (Exit-Code 0, alle Checks ohne Befund) | Offen | — |
| 35 | Verifikation | Rollout-Nachweis: RC-Pre-Release auf `staging` inkl. aller Assets + `update.json` mit RC-Suffix in `version` (Prüfung gegen Vorlagen-Bug 11.2) | Offen | — |
| 36 | Verifikation | Rollout-Nachweis: Promotion-PR `staging` → `main` (Draft, Label `automated-promotion`), nach Merge stabiles Release + Backmerge-PR mit „Create a merge commit" und `back-merge-skip` | Offen | — |
