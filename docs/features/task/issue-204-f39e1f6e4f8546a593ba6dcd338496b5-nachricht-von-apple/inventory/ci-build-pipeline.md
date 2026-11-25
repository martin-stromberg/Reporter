<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: CI-Build-Pipeline iOS (Workflows und Composite Action)

Analysierte Artefakte: `.github/workflows/release.yml`, `.github/workflows/staging-ci.yml`,
`.github/workflows/pr-staging-ci.yml`, `.github/actions/package-ios/action.yml`,
`global.json`, `src/Reporter/Reporter.csproj`.

## `.github/actions/package-ios/action.yml` — zentrale Composite Action

Wird ausschließlich von `release.yml` (Job `package-ios`) und `staging-ci.yml`
(Job `package-ios`) aufgerufen. Inputs (`action.yml` Zeilen 5–47):

| Input | Pflicht | Default | Zweck |
|-------|---------|---------|-------|
| `release-version` | ja | — | Marketing-Version → `-p:Version` |
| `release-tag` | ja | — | Stabile Aufrufsignatur (wird inhaltlich nicht genutzt) |
| `codesign-key` | nein | `''` | → `-p:CodesignKey` |
| `provisioning-profile` | nein | `''` | → `-p:CodesignProvision` |
| `api-key-id` / `api-issuer-id` / `api-key-p8` | nein | `''` | App-Store-Connect-API; `api-key-p8 != ''` aktiviert den TestFlight-Upload |
| `certificate-p12-base64` / `certificate-p12-password` / `provisioning-profile-base64` | nein | `''` | Signaturmaterial-Import (temporäre Keychain) |
| `ios-workload-version` | nein | `'27.2.11039'` | Pin des `Microsoft.iOS.Sdk`-Packs/Manifests; Beschreibungstext verlangt explizit Kompatibilität zu einem Release-Xcode („App Store rejects builds from beta Xcode") |

Schritte der Action (Zeilen 49–258):

| Schritt | Zeilen | Inhalt |
|---------|--------|--------|
| `Setup .NET` | 55–58 | `actions/setup-dotnet` (SHA-Pin, `# v6`), `dotnet-version: '11.0.1xx'` |
| `Pin SDK band for this job` | 65–69 | Schreibt temporäre, nicht committete `global.json` (`11.0.100`, `latestPatch`, `allowPrerelease: false`), damit der Host-Resolver nicht ein höheres Runner-SDK wählt |
| `Install .NET MAUI workload` | 77–90 | Rollback-File `{"microsoft.net.sdk.ios": "<ios-workload-version>"}` → `dotnet workload update --from-rollback-file` → `dotnet workload install maui-ios ios --skip-manifest-update` → Lösch-Schleife: alle `Microsoft.iOS.Sdk.net11.0_*`-Packs ungleich der Pin-Version werden aus `$HOME/.dotnet/packs` entfernt (TPV-Resolution würde sonst das neueste Pack wählen) |
| `Restore` | 96–98 | `dotnet restore src/Reporter/Reporter.csproj -r ios-arm64` |
| `Install signing certificate and provisioning profile` | 106–131 | Nur wenn `certificate-p12-base64` gesetzt: temporäre Keychain, `.p12`-Import, `.mobileprovision` nach `~/Library/MobileDevice/Provisioning Profiles/` |
| `Publish (ios)` | 146–164 | `dotnet publish` `net11.0-ios`, `ios-arm64`, `ArchiveOnBuild=true`, `MauiXamlInflator=XamlC`, `--no-restore`; `BUILD_NUMBER = git rev-list --count HEAD + github.run_attempt - 1`; `ApplicationDisplayVersion` = `release-version` ohne Prerelease-Suffix |
| `Collect iOS package` | 166–190 | Sucht genau eine `.ipa` unter `src/Reporter/bin/Release` (Fehler bei 0 oder >1 distinkten Namen) → `release-ios.ipa` |
| `Upload to TestFlight` | 196–246 | Nur wenn `api-key-p8 != ''`: legt `AuthKey_<id>.p8` unter `~/.appstoreconnect/private_keys/` ab, sucht `iTMSTransporter` (Transporter.app, `/usr/local/itms`, `$DEVDIR/../SharedFrameworks/ContentDeliveryServices.framework` via `xcode-select -p`, Fallback: Standalone-pkg von Apple-CDN via `installer`) → `iTMSTransporter -m upload -assetFile release-ios.ipa -apiKey … -apiIssuer …` |
| `Remove signing keychain` | 249–258 | `if: always()`, räumt temporäres Signaturmaterial ab |

### Feststellungen zur Xcode-/SDK-Auswahl

- **Keine Xcode-Auswahl:** Die Action enthält kein `xcode-select -s`, kein `DEVELOPER_DIR`,
  kein `xcodebuild -version` und keinen Schritt, der die Xcode-Version des Runners
  protokolliert oder prüft. Die einzige `xcode-select`-Verwendung ist `-p` zum
  Auffinden von `iTMSTransporter` (Zeile 213).
- **Keine SDK-Verifikation des Ergebnisses:** `Collect iOS package` prüft nur
  Existenz/Eindeutigkeit der `.ipa`. Es gibt keinen Guard, der `DTSDKName`/
  `DTPlatformVersion`/`DTXcode` aus der `Info.plist` des App-Bundles liest.
- Der Inline-Kommentar bei `ios-workload-version` (Zeile 45) und beim
  Workload-Install-Schritt (Zeilen 71–76) dokumentiert bereits die
  Beta-vs.-Release-Problematik: `workload restore` ohne Pin würde `latest`
  ziehen — laut Kommentar „aktuell 27.x mit Beta-Xcode-Anforderung", weshalb
  auf `27.2.11039` („Release-Xcode-kompatibel") gepinnt wird.
- Historie: Vor der .NET-11-Migration (Commit `9a636b0`, 22.11.2026) war der
  Default `26.5.10318` mit SDK-Band `10.0.1xx` und TFM `net10.0-ios`
  (nachgewiesen via `git show 46e47b2:.github/actions/package-ios/action.yml`).

## `.github/workflows/release.yml` — Job `package-ios` (Zeilen 144–184)

- `runs-on: macos-latest` (Zeile 150), `timeout-minutes: 30`.
- Gate: `needs.resolve.outputs.released == 'true' && vars.IOS_SIGNING_ENABLED == 'true'`
  (Zeile 149) — läuft nur, wenn die Repo-Variable `IOS_SIGNING_ENABLED=true` gesetzt
  ist (sie ist es: der Job lief in den belegten Runs, s. [ci-runs.md](ci-runs.md)).
- Übergibt alle Signatur-/API-Secrets an die Action; kein `ios-workload-version`-Override
  → der Action-Default greift.
- Artefakt `release-ios` (`release-ios.ipa`, Retention 14 Tage), fließt in
  `update.json`/`EXPECTED_ASSETS` ein (Skripte `release-assets.mjs`,
  `create-update-manifest.mjs`).
- Bei `release_action == 'upload-existing'` (Repair-Pfad) checkt die Action
  `checkout-release-tag` den getaggten Stand aus — eine Reparatur älterer
  Releases baut damit bewusst den alten Stand inklusive dessen Workload-Pin.

## `.github/workflows/staging-ci.yml` — Job `package-ios` (Zeilen 341–374)

- `runs-on: macos-latest` (Zeile 347), `timeout-minutes: 30`.
- Gate: `needs.version.outputs.changed == 'true' && vars.IOS_SIGNING_ENABLED == 'true'`
  (Zeile 346) — RC-Builds nur bei Versionsänderung.
- Identischer Action-Aufruf wie `release.yml`; Artefakt `release-ios` → GitHub-Pre-Release.
- Ein übersprungener/fehlgeschlagener `package-ios` blockiert per
  `!failure() && !cancelled()` die Aggregation im `prerelease`-Job — der
  Fehlschlag im Run 45117817038 führte zu `prerelease: skipped` (s. ci-runs.md).

## `.github/workflows/pr-staging-ci.yml`

- Enthält **keinen** iOS-Job (Jobs: `detect-backmerge`, `static checks`,
  `build & test`, `os-interface tests`, `back-merge-skip`; alle auf
  `windows-latest`/`ubuntu-latest` mit `IncludeIosTarget: false`).
- Konsequenz: Änderungen an `package-ios`/Runner-Label lassen sich im PR-Gate
  nicht auf macOS verifizieren; erst der Push auf `staging` (mit
  `version.changed == 'true'`) oder ein `main`-Release übt den Pfad aus.

## `global.json` (Repo-Root)

```json
{ "sdk": { "version": "11.0.100", "rollForward": "latestPatch" },
  "test": { "runner": "Microsoft.Testing.Platform" } }
```

- Pinnt das lokale SDK-Band auf 11.0.1xx; im CI-iOS-Job wird ohnehin eine
  strengere temporäre `global.json` geschrieben (s. Action oben).
- `test.runner: Microsoft.Testing.Platform` — die Tests laufen im MTP-Modus
  (betrifft die `dotnet test`-Syntax mit `--`-Separator).

## `src/Reporter/Reporter.csproj` (nur Verifikation, kein Änderungsbedarf)

- `TargetFrameworks` konditional: auf macOS `net11.0-ios`
  (Zeile 13, bei `IncludeAndroidTarget != 'true'`).
- `SupportedOSPlatformVersion` iOS = `15.0` (Zeile 45) — Deployment-Target,
  unabhängig vom Build-SDK; laut Anforderung explizit unverändert zu lassen.
- `<UseInterpreter>true</UseInterpreter>` in der `net11.0-ios`-PropertyGroup
  (Zeilen 52–56) — EF-Core-AOT-Workaround, nicht Teil dieser Anforderung.
- `ApplicationDisplayVersion`/`ApplicationVersion` (Zeilen 39–40) sind nur
  lokale Defaults — die Action überschreibt beide per `-p:`.

## `dotnet-tools.json` / `Directory.Build.props`

- `dotnet-tools.json`: nur `dotnet-ef` 11.0.0 — iOS-irrelevant.
- `Directory.Build.props`: Lizenz + `DebugReportRecipient` — iOS-irrelevant.

## Runner-Images (extern, Stand 23.11.2026)

Quellen: `gh run view`-Job-Logs (Nachweis in [ci-runs.md](ci-runs.md)) sowie
`actions/runner-images` READMEs.

- **`macos-latest` löst auf `macos-26` (arm64) auf.** Aktuelles Image
  `20261117.0516.1` (macOS 26.7.2): installierte Xcodes 26.0.1–26.6,
  **Default Xcode 26.6** → iOS-SDKs ausschließlich `iphoneos26.x` (höchstes:
  **iOS 26.5**). Kein Xcode 27 auf diesem Image.
- **`xcode-27` / `xcode-27-xlarge`** (nur arm64): GA seit 15.10.2026
  (GitHub-Changelog 2026-10-15 „Xcode 27 runner image generally available"),
  vorher Public Preview seit 16.07.2026 (`runner-images` Issue #14404).
  Basis macOS 27.x; Image `20261104.0337.1` listet Xcode **27.0**, **27.1
  (default)**, **27.2** — die SDKs `iphoneos27.0/27.1/27.2`. Neuere Image-
  Releases (z. B. `20261119.0378.1`) ergänzen Xcode 27.3 **beta**.
  Der Default `27.1` passt **nicht** zum Pack-Pin `27.2.11039` (Pack verlangt
  exakt Xcode 27.2, s. Fehlermeldung unten) — auf diesem Image wäre ein
  explizites `xcode-select -s /Applications/Xcode_27.2.app` nötig.
- GitHub-Policy zu Xcode auf den Images: pro macOS-Version nur eine
  Xcode-Hauptversion (alle Minors), Beta/RC „as-is" im neuesten Image.

## Belegte Ursachenkette für ITMS-90725 und den aktuellen CI-Fehlschlag

1. Build 331 (Apple-Meldung, Version 0.8.0) entstand im `staging-ci`-Run
   44460132570 zum Tag `v0.8.0-rc.1` (Commit `46e47b2`, 19.11.2026) —
   `git rev-list --count` des Tag-Commits = **331** = `CFBundleVersion`
   (Formel `BUILD_NUMBER`, Zeile 151 der Action). Damaliger Stand:
   `ios-workload-version: 26.5.10318`, SDK-Band `10.0.1xx`, `net10.0-ios` →
   Pack `Microsoft.iOS.Sdk 26.5` → Build gegen `iphoneos26.5` → exakt Apples
   Befund „built with the iOS 26.5 SDK". Der TestFlight-Upload lief im
   selben Job erfolgreich durch.
2. Nach der .NET-11-Migration (`a412efa` auf `staging`) schlug der erste
   `package-ios`-Lauf (Run 45117817038, 23.11.2026) mit
   `Xamarin.Shared.Sdk.targets(3190,3): error : This version of .NET for iOS
   (27.2.11039) requires Xcode 27.2. The current version of Xcode is 26.6.`
   fehl — auf `macos-26-arm64` ist kein Xcode 27 installiert.
   **Der aktuelle `staging`-Stand kann in CI gar keine `.ipa` mehr erzeugen.**
3. Folge: Der Workload-Pin ist bereits korrekt auf iOS-27.2-Bindings gesetzt;
   es fehlt die Runner-/Xcode-Seite (Label `xcode-27` und/oder explizites
   `xcode-select`), plus optional ein SDK-Gate auf der erzeugten `.ipa`.
