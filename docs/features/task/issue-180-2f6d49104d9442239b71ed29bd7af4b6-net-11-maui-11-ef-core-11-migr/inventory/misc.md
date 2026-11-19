<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Inventar: Dependabot, Plattform-Manifeste, Docs, Sonstiges

## Dependabot

`.github/dependabot.yml` – drei Ökosysteme, alle `target-branch: "staging"`, weekly:

- `nuget` (directory `/`)
- `npm` (directory `/`)
- `github-actions` (directory `/`)

Bekannte offene 11er-Update-PRs (aus Aufgabenbeschreibung):
`System.ServiceModel.Syndication 10.0.11 → 11.0.0` und `Microsoft.Data.Sqlite 10.0.12 → 11.0.0`.
Beide können erst nach/mit der Runtime-Migration gemergt werden; ggf. Dependabot-
Ignore-Regeln oder koordiniertes Mergen im Migrationsplan festlegen.
Hinweis: `Microsoft.Maui.Controls` (`$(MauiVersion)`) kann Dependabot nicht anheben –
dokumentiert in `docs/help/release-management/wiederkehrende-reviews.md:38`.

## Plattform-Manifeste / Mindestversionen

| Datei | Inhalt |
|---|---|
| `src/Reporter/Platforms/iOS/Info.plist` | `LSRequiresIPhoneOS`, `UIDeviceFamily=1` (nur iPhone), `UIBackgroundModes fetch`, `BGTaskSchedulerPermittedIdentifiers`, `NSAppTransportSecurity`, `CFBundleLocalizations en/de` – **keine** `MinimumOSVersion` (kommt aus `SupportedOSPlatformVersion` = iOS 15.0, csproj Z. 45) |
| `src/Reporter/Platforms/MacCatalyst/Info.plist` | `UIDeviceFamily=2`, `LSApplicationCategoryType lifestyle` – MacCatalyst-Ordner existiert, TFM aber nicht aktiv |
| `src/Reporter/Platforms/MacCatalyst/Entitlements.plist` | vorhanden |
| `src/Reporter/Platforms/Android/AndroidManifest.xml` | nur `allowBackup`, `ACCESS_NETWORK_STATE`, `INTERNET` – **kein** `uses-sdk`; `minSdkVersion` kommt aus `SupportedOSPlatformVersion` = 21 (csproj Z. 47) |
| `src/Reporter/Platforms/Windows/Package.appxmanifest` | Z. 22-23 `TargetDeviceFamily` Min `10.0.17763.0`, MaxTested `10.0.19041.0` – Deckt sich mit `SupportedOSPlatformVersion`/`TargetPlatformMinVersion` (csproj Z. 48-49); bei Anhebung synchron ziehen |
| `src/Reporter/Platforms/Windows/app.manifest` | DPI/longPathAware – versionsneutral |
| `src/Reporter/Properties/launchSettings.json` | nur Profil "Windows Machine" – versionsneutral |

Ob MAUI 11 neue Mindest-OS-Versionen erzwingt (iOS/Android/Windows), ist im Plan zu prüfen.

## dotnet-tools / Docker / NuGet-Config

- Keine `.config/dotnet-tools.json` → `dotnet-ef`, ReportGenerator etc. sind nicht
  repo-gepinnt (ReportGenerator wird in CI mit `--version 5.5.11` installiert).
- Kein `Dockerfile`, keine `NuGet.config`/`nuget.config`, kein `Directory.Packages.props`,
  kein `Directory.Build.targets`, keine `.editorconfig`-TFM-Bezüge, kein `.vscode/`.

## package.json / Node

`package.json`: nur `semantic-release`-Toolchain (`release`-Script, devDependencies) –
kein .NET-Bezug. `npm ci` + `npx semantic-release` in CI-`version`-/`publish`-Jobs
(node-version '24', `staging-ci.yml:186-188`, `release.yml:39-41`).

## Doku-/Datei-Referenzen auf net10.0 / .NET 10 (bei Migration zu aktualisieren)

### Repo-Root

- `README.md`: Badge `.NET-10.0` (Z. 4), Voraussetzung ".NET 10 SDK" (Z. 47),
  TFM-Beschreibung + `dotnet run -f net10.0-...` (Z. 57, 63, 66).
- `ci-instructions.md`: generische Workflow-Vorlage mit `10.0.x`/`net10.0` (Z. 194, 228,
  449, 458, 465) – Vorlage, ggf. bewusst stehen lassen oder mitziehen.
- `issue.md`: beschreibt selbst die Migration (verweist net10/net11) – Planungsartefakt.

### docs/help (Auswahl, keine Komplettliste nötig – Muster „net10.0"-Pfade + ".NET SDK 10.0.x")

| Datei | Stellen (Auswahl) |
|---|---|
| `docs/help/release-management/installation.md` | Z. 12 `.NET SDK 10.0.x` |
| `docs/help/release-management/ablauf-technisch.md` | Z. 40-42 (`net10.0-windows/android/ios`-Publish), Z. 81 (`10.0.x`) |
| `docs/help/release-management/architektur.md` | Z. 20-22, 37 |
| `docs/help/release-management/business-rules.md` | Z. 85 (`net10.0-android`-Regel) |
| `docs/help/tests/ablauf-technisch.md` | Z. 15, 31 (E2E-Konventionspfad) |
| `docs/help/tests/api.md` | Z. 119 |
| `docs/help/tests/architektur.md` | Z. 11, 30 |
| `docs/help/tests/installation.md` | Z. 25-28, 38, 43 |
| `docs/help/tests/troubleshooting.md` | Z. 9, 13 |
| `docs/help/ios-deployment/ablauf-anwender.md` | Z. 36, 55 |
| `docs/help/ios-deployment/ablauf-technisch.md` | Z. 9, 23 |
| `docs/help/ios-deployment/troubleshooting.md` | Z. 91 |
| `docs/help/anwendung/mobile-ui-design.md` | Z. 88, 143, 166, 182, 193, 202 |
| `docs/help/benachrichtigungen/installation.md` | Z. 9 |
| `docs/help/einstellungen/architektur.md` | Z. 20 |
| `scripts/iOS-Deployment.md` | Z. 89, 114, 127, 235, 241 |

Vollständige Suche vor der Migration erneut ausführen:
`grep -rn "net10\.0\|10\.0\.x\|\.NET 10" docs/ README.md ci-instructions.md scripts/`.

## Code-Referenzen auf net10-Pfade

- `src/Reporter.E2ETests/ReporterAppFixture.cs:143` – harter Konventionspfad
  `src/Reporter/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Reporter.exe` (Fallback,
  wenn `REPORTER_APP_PATH` nicht gesetzt; dokumentiert in `docs/help/tests/api.md:119`).

## Maintenance-Branch

- `maintenance/net10` existiert auf Remote (`origin/maintenance/net10`, Commit `5492d71`,
  Release-Stand v0.7.0) – laut Aufgabe als .NET-10-Fallback während der Umstellung.
