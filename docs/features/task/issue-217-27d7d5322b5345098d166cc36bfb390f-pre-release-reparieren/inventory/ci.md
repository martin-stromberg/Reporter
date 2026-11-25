<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# CI/Workflows — Bestandsaufnahme (Issue #217)

## `.github/workflows/staging-ci.yml` („Pre-Release")

Trigger: `push` auf `staging`. Job-Graph (mit `needs`-Kette):

```
detect-backmerge (ubuntu)
  ├─ static-checks (windows)          — Format, Lizenzheader, Security-Scan, Release-Build
  ├─ build-and-test (windows)         — Release-Build + Unit-Tests (ohne OsInterface) + Coverage-Gate 70 %
  ├─ os-interface-tests (windows)     — continue-on-error, nicht in needs-Ketten
  └─ version (ubuntu)                 — semantic-release Dry-Run → outputs changed/version/rc_tag/rc_version
        ├─ package-windows (windows)  — ./.github/actions/build-and-package → release-win-x64.zip
        ├─ package-android (windows)  — ./.github/actions/package-android → release-android.apk
        ├─ package-ios (xcode-27)     — ./.github/actions/package-ios → release-ios.ipa
        └─ prerelease (ubuntu)        — needs [version, package-*], erzeugt RC-Tag + GitHub-Pre-Release
```

Gating-Bedingungen (Ist-Zustand):

- Alle Analyse-/Packaging-Jobs: `needs.detect-backmerge.outputs.is_backmerge != 'true'`.
- `version` benötigt `static-checks` + `build-and-test` (Zeile 238).
- `package-*`: `needs.version.outputs.changed == 'true'` (Zeilen 295, 321).
- `package-ios` zusätzlich `vars.IOS_SIGNING_ENABLED == 'true'` auf Runner-Label `xcode-27` (Zeilen 350–351) — der dokumentierte MT7199-Fehlschlag beweist, dass die Variable gesetzt ist und der Job tatsächlich läuft.
- `prerelease`: `needs: [version, package-windows, package-android, package-ios]` + `!failure() && !cancelled()` (Zeilen 382–383). Übersprungene `needs` führen hier nicht zu `failure()` — wenn `package-ios` gar nicht erst läuft (Signing aus), blockiert das den Job nicht.

Der `prerelease`-Job erzeugt per `gh release create "<rc_tag>" … --prerelease` das RC-Release (Zeilen 407–417) mit Assets aus `release-assets.mjs` und `update.json` aus `create-update-manifest.mjs`.

## `.github/workflows/release.yml` („Release")

Trigger: `push` auf `main` und Tags `v*.*.*`. Nutzt **dieselben** Composite
Actions: `build-and-package` (Zeile 101), `package-android` (Zeile 133),
`package-ios` (Zeile 170) — Korrekturen in den Actions wirken implizit auch
hier. Unterschiede: vorangestellter `resolve`-Job (`resolve-release-version.mjs`)
und `release-gate` (reiner `dotnet test`, Zeile 79); Abschluss `publish`
statt `prerelease`; iOS-Job identisch hinter `vars.IOS_SIGNING_ENABLED` +
Runner `xcode-27` (Zeilen 153–154). Zusätzlich `checkout-release-tag`-Action
für Asset-Repair-Läufe (`release_action == 'upload-existing'`).

## `.github/workflows/pr-staging-ci.yml`

PR-Pendant zu `staging-ci.yml`: identische `static-checks`-, `build-and-test`-
und `os-interface-tests`-Lanes (Testbefehle Zeilen 123–130, 202–207), aber
**keine** Versions-/Paketierungs-Jobs — die Packaging-Pfade sind nur über den
Staging-Push bzw. Release erreichbar und lassen sich nicht per PR verifizieren.

## `.github/actions/*`

Siehe [logic.md](logic.md) für die Schritt-Details. Zusammenfassung:

| Action | Zweck | Bekannter Fehler |
|--------|-------|------------------|
| `build-and-package` | Windows-Publish (`win-x64`, self-contained, `PublishReadyToRun=false`) | läuft erfolgreich |
| `package-android` | Android-APK (`net11.0-android`, kein RID im Restore, `--no-restore` im Publish) | `error NETSDK1094` |
| `package-ios` | iOS-IPA auf `xcode-27`, Xcode-27.2-Pin, Workload-Pin `27.2.11039`, SDK-Gate ≥ 27 | `error MT7199` |
| `checkout-release-tag` | Tag-Checkout für Release-Repair | — |
| `security-scan` | Paket-Vulnerability-Scan | — |

## Testausführung in den Workflows

- `staging-ci.yml` Zeilen 121–127: `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release --no-build -- --coverlet … --filter-not-trait "Category=OsInterface" --report-trx` (+ Coverage-Gate 70 %).
- `staging-ci.yml` Zeilen 201–204: OsInterface-Lane (`--filter-trait "Category=OsInterface"`), `continue-on-error`.
- `release.yml` Zeile 79: `dotnet test … -c Release` (ohne Filter — schließt OsInterface-Tests ein).
- Node-Tests (`npm test` → `node --test "scripts/*.test.mjs"`) laufen **nicht** in CI — nur lokal verfügbar.
- `Reporter.E2ETests` (FlaUI) läuft in keinem Workflow; nur via `scripts/Run-E2ETests.ps1` (interaktive Windows-Desktop-Session).
