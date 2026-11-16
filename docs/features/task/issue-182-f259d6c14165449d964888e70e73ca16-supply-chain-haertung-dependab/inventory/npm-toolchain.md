<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: npm-Toolchain und NuGet-Abhängigkeiten

## `package.json` (Repo-Root)

| Feld | Wert |
|------|------|
| `name` | `reporter` (`"private": true`, `"license": "PolyForm-Noncommercial-1.0.0"`) |
| `scripts.release` | `semantic-release` |
| `scripts.test` | `node --test "scripts/*.test.mjs"` |

### `devDependencies` (5 — es gibt **keine** Produktiv-Abhängigkeiten)

| Paket | Gepinnte Version |
|-------|------------------|
| `semantic-release` | `25.0.9` |
| `@semantic-release/commit-analyzer` | `13.0.1` |
| `@semantic-release/github` | `12.0.9` |
| `@semantic-release/release-notes-generator` | `14.1.1` |
| `conventional-changelog-conventionalcommits` | `9.3.1` |

**Konsequenz für das Audit-Gate:** `npm audit --omit=dev` würde nichts prüfen — sämtliche Abhängigkeiten sind `devDependencies` (bestätigt Offene Frage 1 der Anforderung).

## `package-lock.json` (Repo-Root)

- `lockfileVersion: 3`, `requires: true`, ~193 KB.
- Root-Paket `"reporter"` deklariert dieselben 5 `devDependencies`; alle `node_modules/*`-Einträge tragen `"dev": true`.
- `npm audit` arbeitet auf dem Lockfile + Registry-Metadaten — funktioniert ohne installiertes `node_modules` und ohne `npm ci` (lokal verifiziert).
- **Audit-Bestand (2026-11-16):** `npm audit` meldet **35 moderate** Schwachstellen, **0 hohe/kritische**; Exit-Code 1. Befund liegt in transitiven Paketen unter `node_modules/npm` (eingebettetes npm von `@semantic-release/npm` → `semantic-release`): `glob` (GHSA-3g3m-2fjr-6pw4) und `minimatch` (GHSA-g3hv-75wg-m249). `npm audit fix --force` würde `semantic-release@21.1.2` installieren — ein **Downgrade** mit Breaking Change gegenüber dem gepinnten `25.0.9`. Nachweis: [test-results/npm-audit.log](test-results/npm-audit.log).
- **Konsequenz:** Ein neues Gate, das bei *jedem* Befund oder ab `moderate` fehlschlägt, würde ab sofort fehlschlagen. Bei `--audit-level=high` würde es aktuell bestehen.

## `release.config.js` (Repo-Root)

- `branches: ["main"]`, `tagFormat: "v${version}"`.
- Plugins normal: `commit-analyzer`, `release-notes-generator` (Preset `conventionalcommits`), `@semantic-release/github` (Assets aus `RELEASE_ASSET_PATHS` + `RELEASE_MANIFEST_PATH`, `successComment`/`failComment: false`).
- `RESOLVE_DRY_RUN === "true"` → nur `commit-analyzer` (für Dry-Run-Versionsermittlung in `staging-ci.yml`/`release.yml`).

## npm-Nutzung in der CI

- `staging-ci.yml` Job `version`: `actions/setup-node@v7` (Node 24) + `npm ci` + `npx semantic-release --dry-run --no-ci --branches staging`.
- `release.yml` Jobs `resolve` und `publish`: `actions/setup-node@v7` (Node 24) + `npm ci`; `publish` ruft `npm run release` auf.
- Im `security-scan`-Gate (Composite Action und Workflow `security-scan.yml`) gibt es **keinen** `setup-node`-Step — die Action läuft auf `windows-latest`, wo Node/npm vorinstalliert sind (Annahme der Anforderung; nicht repo-intern verifizierbar).

## NuGet-Abhängigkeiten (Bestand für das `nuget`-Ökosystem)

Erfasst aus `Reporter.sln` + `src/**/*.csproj` (5 Projekte). `dotnet list package --vulnerable --include-transitive` meldet **keine** verwundbaren Pakete (siehe [tests.md](tests.md)).

| Paket | Version | Projekte |
|-------|---------|----------|
| `CommunityToolkit.Mvvm` | 8.4.2 | Reporter |
| `FlaUI.UIA3` | 5.0.0 | Reporter.E2ETests |
| `Microsoft.Data.Sqlite` | 10.0.12 | Reporter.E2ETests |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.12 | Reporter.Data |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.12 | Reporter.Data, Reporter.Tests |
| `Microsoft.Extensions.Logging.Debug` | 10.0.0 | Reporter |
| `Microsoft.Extensions.TimeProvider.Testing` | 10.1.0 | Reporter.Tests |
| `Microsoft.Maui.Controls` | `$(MauiVersion)` | Reporter |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | Reporter.Tests, Reporter.E2ETests |
| `System.ServiceModel.Syndication` | 10.0.11 | Reporter.Core |
| `coverlet.collector` | 6.0.4 | Reporter.Tests |
| `xunit` | 2.9.3 | Reporter.Tests, Reporter.E2ETests |
| `xunit.runner.visualstudio` | 3.1.4 | Reporter.Tests, Reporter.E2ETests |

Hinweis: `Microsoft.Maui.Controls` referenziert `$(MauiVersion)` (SDK-gesteuert) — Dependabot kann diesen Wert nicht direkt anheben; NuGet-Updates betreffen primär die übrigen gepinnten Pakete.

## Bestehende Dependency-Überwachung ohne Dependabot

- `review-nuget.yml` (wöchentlich, `0 6 * * 1`): erzeugt Issue `NuGet-Paketprüfung KW <JJJJ-Www>` mit Label `scheduled-nuget-review` für die externe `@softwarewerkstatt`-Automatisierung — reine Sichtprüfung, keine PRs.
- `security-scan.yml` + Gate in `static-checks`: schlägt bei verwundbaren NuGet-Paketen fehl (wöchentlich + bei jedem Push/PR auf `staging`).
- Für `github-actions` und `npm` existiert **keine** Update-Automatisierung — genau die Lücke, die `.github/dependabot.yml` schließen soll.
