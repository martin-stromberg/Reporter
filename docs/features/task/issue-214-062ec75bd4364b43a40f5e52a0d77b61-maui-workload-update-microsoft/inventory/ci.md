<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: CI-Workflows und Composite Actions

Alle Stellen, an denen der MAUI-Workload in der CI installiert oder aufgelöst
wird, sowie der bestehende iOS-Pin (Referenz-Implementierung aus Issue #204).

## Übersicht: `dotnet workload restore`-Aufrufe (ungepinnt)

| Datei | Job | Zeile | Kontext |
|-------|-----|-------|---------|
| `.github/workflows/pr-staging-ci.yml` | `static-checks` | 75–76 | `windows-latest`, `setup-dotnet '11.0.x'`, `IncludeIosTarget=false`, `IncludeAndroidTarget=false` |
| `.github/workflows/pr-staging-ci.yml` | `build-and-test` | 114–115 | gleiche Umgebung |
| `.github/workflows/pr-staging-ci.yml` | `os-interface-tests` | 193–194 | gleiche Umgebung, `continue-on-error: true` (nicht blockierend) |
| `.github/workflows/staging-ci.yml` | `static-checks` | 72–73 | gleiche Umgebung |
| `.github/workflows/staging-ci.yml` | `build-and-test` | 111–112 | gleiche Umgebung |
| `.github/workflows/staging-ci.yml` | `os-interface-tests` | 190–191 | gleiche Umgebung, `continue-on-error: true` |
| `.github/workflows/security-scan.yml` | `security-scan` | 28–29 | `windows-latest`, `setup-dotnet '11.0.x'`, `IncludeIosTarget=false`; wöchentlicher Cron `0 6 * * 1` |
| `.github/actions/build-and-package/action.yml` | (Schritt) | 21–22 | `setup-dotnet '11.0.x'`, `IncludeIosTarget=false`; genutzt von `package-windows` in `staging-ci.yml` und `release.yml` |
| `.github/actions/package-android/action.yml` | (Schritt) | 21–22 | `setup-dotnet '11.0.x'`, `IncludeIosTarget=false`, `IncludeAndroidTarget=true`; genutzt von `package-android` in `staging-ci.yml` und `release.yml` |

Gemeinsames Muster: `actions/setup-dotnet@v6` (SHA-gepinnt
`0f4a354dd5547a28c70b7853dbd95d5b67886e73`) mit `dotnet-version: '11.0.x'`
→ höchstes 11er-SDK; anschließend `dotnet workload restore Reporter.sln`
ohne Versionsangabe → jeweils neuestes Workload-Manifest des Bands 11.0.1xx
(**Floating**). Kein Diagnoseschritt (`dotnet workload list`) in einem dieser
Jobs — der tatsächlich aufgelöste CI-Workload-Stand ist aus den YAML-Dateien
nicht ablesbar und aktuell in keinem Job-Log explizit dokumentiert.

## `.github/workflows/staging-ci.yml` — Pre-Release-Kette

Jobs und Abhängigkeiten (verbindlicher Auftrag: Pipeline muss weiterhin laufen):

- `detect-backmerge` (Z. 20–52): überspringt CI bei reinem main-Backmerge.
- `static-checks` (Z. 54–91), `build-and-test` (Z. 93–166),
  `os-interface-tests` (Z. 171–234, non-blocking).
- `version` (Z. 236–289): `needs: [detect-backmerge, static-checks,
  build-and-test]`; semantic-release-Dry-Run; Output `changed` nur `true`,
  wenn ein release-auslösender Commit-Typ vorliegt (`fix:`/`feat:` …).
- `package-windows` (Z. 291–314), `package-android` (Z. 316–339): jeweils
  `if: needs.version.outputs.changed == 'true'` → laufen nur bei
  Versionsänderung.
- `package-ios` (Z. 346–377): zusätzlich
  `vars.IOS_SIGNING_ENABLED == 'true'`, Runner `xcode-27`.
- `prerelease` (Z. 379–416): `needs: [version, package-windows,
  package-android, package-ios]` mit `!failure() && !cancelled()`; erzeugt
  das GitHub-Pre-Release via `gh release create` inkl. `update.json`
  (`scripts/create-update-manifest.mjs`) und Asset-Liste
  (`scripts/release-assets.mjs`).

Konsequenz für die Anforderung: ein reiner `chore:`-Commit löst kein neues
Pre-Release aus — `package-*`/`prerelease` würden übersprungen (offene Frage 5).

## `.github/workflows/release.yml`

- `release-gate` (Z. 59–78): `windows-latest` + `setup-dotnet '11.0.x'` —
  **kein** `dotnet workload restore`; `dotnet test` nur über
  `Reporter.Tests.csproj` (benötigt keinen MAUI-Workload).
- `package-windows`/`package-android`/`package-ios` (Z. 80–187): nutzen
  dieselben Composite Actions wie `staging-ci.yml` → gleiche Workload-Pfade.
- `package-ios` (Z. 149–187): Runner `xcode-27`, `IOS_SIGNING_ENABLED`-Gate.

## `.github/workflows/pr-staging-ci.yml`

Deckt dieselben drei Windows-Jobs wie `staging-ci.yml` ab (Z. 57–237); kein
`version`-/`package-*`-/`prerelease`-Job — der PR-Gate kann die
Pre-Release-Kette nicht ausüben (offene Frage: Verifikation erst nach Merge).

## `.github/actions/package-ios/action.yml` — Referenz-Pin (unverändert zu belassen)

Vollständiger Workload-Pin-Mechanismus aus Issue #204:

| Stelle | Inhalt |
|--------|--------|
| Z. 44–47 | Input `ios-workload-version`, Default `27.2.11039` — explizit an `xcode-path` gekoppelt |
| Z. 48–51 | Input `xcode-path`, Default `/Applications/Xcode_27.2.app` |
| Z. 65–81 | Schritt `Select Xcode`: Fail-fast ohne passende App; setzt `DEVELOPER_DIR` |
| Z. 86–89 | `setup-dotnet` mit `dotnet-version: '11.0.1xx'` |
| Z. 96–100 | Schritt `Pin SDK band for this job`: schreibt temporäre `global.json` (`11.0.100`, `latestPatch`, `allowPrerelease: false`) — bewusst nicht committed |
| Z. 108–123 | Schritt `Install .NET MAUI workload`: Rollback-File `$RUNNER_TEMP/workload-rollback.json` (`{"microsoft.net.sdk.ios": "27.2.11039"}`), `dotnet workload update --from-rollback-file`, `dotnet workload install maui-ios ios --skip-manifest-update`, danach Entfernen vorinstallierter `Microsoft.iOS.Sdk.net11.0_*`-Packs anderer Version (TPV-Resolution würde sonst das neueste Pack wählen) |
| Z. 230–256 | Schritt `Verify built SDK version`: Gate auf `DTSDKName` ≥ `iphoneos27` in der erzeugten `.ipa` (ITMS-90725) |

Das Muster „Rollback-File + `--skip-manifest-update` + Pack-Cleanup" ist die
einzige repo-seitig vorhandene Pin-Technik und direkt auf `maui-windows`
übertragbar (offene Frage 1). Es greift ausschließlich im `package-ios`-Job —
die übrigen CI-Jobs bleiben ungepinnt.

## Weitere Workflows ohne Workload-Bezug

`deploy-pages.yml`, `staging-to-main-promotion.yml`,
`sync-staging-with-main.yml`, `verify-pr-source.yml`, `review-architecture.yml`,
`review-compliance.yml`, `review-nuget.yml` — kein `setup-dotnet`-/
`dotnet workload`-Schritt (verifiziert per Suche). `review-nuget.yml`
(cron `0 6 * * 1`) erzeugt lediglich das wöchentliche Review-Issue, das den
Befund meldet (Issue #213 → #214).

## Sonstige Toolchain-Artefakte

- `dotnet-tools.json` (Repo-Root): nur `dotnet-ef 11.0.0` — kein
  Workload-Bezug.
- `package.json` / `release.config.js`: semantic-release-Stack (npm);
  `npm test` führt 36 `node:test`-Tests der Release-Skripte aus.
- `.github/dependabot.yml`: wöchentliche PRs für `nuget`, `npm`,
  `github-actions` gegen `staging` — kann `$(MauiVersion)` nicht anheben.
