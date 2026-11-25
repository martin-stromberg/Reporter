<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Manueller iOS-Auslieferungspfad und Dokumentation

Analysierte Artefakte: `scripts/iOS-Deployment.ps1`, `scripts/iOS-Deployment.md`,
`docs/help/ios-deployment/*`, `docs/help/release-management/*`,
`docs/app-store-review.md`, `docs/RELEASE_NOTES.md`.

## `scripts/iOS-Deployment.ps1` (1422 Zeilen)

Zweck: lokaler/manueller Build- und Deployment-Pfad für `net11.0-ios`
(`$framework`, Zeile 94) — von Windows via Pair-to-Mac/SSH oder direkt auf
einem Mac. Aktionen `build`, `simulator`, `device`, `store`, `upload`,
`list`, `menu` (Zeilen 46–90, Dispatcher Zeilen 1373–1416).

### Für die Anforderung relevante Funktionen

| Funktion | Zeilen | Rolle |
|----------|--------|-------|
| `Invoke-Build` | 849–922 | `dotnet build` bzw. `dotnet publish` (bei Signaturangaben `-p:ArchiveOnBuild=true` → `.ipa`) auf `src/Reporter/Reporter.csproj -f net11.0-ios`; Parameter via temporärer Response-Datei `.ios-deploy-*.rsp`. Auf Windows werden `ServerAddress`/`ServerUser`/`ServerPassword`/`TcpPort`/`_DotNetRootRemoteDirectory` als Pair-to-Mac-Properties mitgegeben — **der Build läuft dann auf dem Mac mit dessen `xcode-select`-Xcode**. |
| `Invoke-Store` | 788–801 | `Assert-StorePrerequisites` → `Assert-CodesigningForAction` → `Assert-TransporterAvailable` → `Update-BuildNumber` (csproj-Bump, Opt-out `-NoBumpBuildNumber`) → `Invoke-Build` → `Get-LatestIpa` → `Copy-ApiKeyToMac` → `Get-MacIpaPath` → `Invoke-IpaValidation` → `Invoke-StoreUpload` |
| `Invoke-Upload` | 803–814 | Wie `Invoke-Store`, aber ohne Bump und ohne Build — Quelle `-IpaPath` oder neueste `.ipa` unter `bin/Release` |
| `Assert-StorePrerequisites` | 398–443 | Pflichtparameter, `.p8` außerhalb Repo, kein Development-`CodesignKey`, SSH-Zwang unter Windows — **keine Xcode-/SDK-Prüfung** |
| `Invoke-IpaValidation` | 696–766 | Remote-Bash auf dem Mac: `.ipa` entpacken → `codesign --verify --deep --strict` → `get-task-allow=false` in `embedded.mobileprovision` → `PrivacyInfo.xcprivacy` vorhanden + plist-Lint → `Info.plist`-Invarianten (`UIDeviceFamily` iPhone-only, `CFBundleLocalizations` en+de, `ITSAppUsesNonExemptEncryption=false`) → Icon-Encoding in `Assets.car` (nur Warnung) → `iTMSTransporter -m verify` (optional, Warnungs-Fallback). **Es wird weder `DTSDKName`, `DTPlatformVersion` noch `DTXcode` gelesen** — die SDK-Version der `.ipa` ist für das Skript unsichtbar. |
| `Invoke-StoreUpload` | 768–786 | `iTMSTransporter -m upload -assetFile <ipa>` — harter Fehler ohne gefundenes Werkzeug |
| `Get-TransporterFindScript` | 445–492 | Sucht `iTMSTransporter` in Transporter.app, `/usr/local/itms`, `PATH`, `xcrun -f`, `ContentDeliveryServices.framework` unter `xcode-select -p`, Spotlight; `-TransporterPath`-Override |
| `Invoke-DeviceViaSsh` / `Invoke-SimulatorViaSsh` | 1092–1158 / 1160–1266 | Windows-Pfade: Build via Pair-to-Mac (Xcode des Macs), Installation via `devicectl`/`simctl` remote |
| `Update-BuildNumber` | 518–552 | Erhöht `<ApplicationVersion>` in der csproj; `-Version x.y.z` setzt `ApplicationDisplayVersion` und setzt die Buildnummer auf 1 zurück |

### Xcode-Bezug im Skript

- Es gibt **keine** Stelle, die die Xcode-Version des Macs ausliest oder
  gegen ein Minimum prüft (`xcodebuild -version`, `xcode-select -p` nur zur
  Pfadauflösung für `iTMSTransporter`, Zeile 475, und `Simulator.app`,
  Zeile 1231).
- Welches iOS-SDK eine `store`-`.ipa` erhält, hängt vollständig davon ab,
  welches Xcode auf dem Build-Mac per `xcode-select` aktiv ist — weder
  Skript noch Doku legen eine Mindestversion fest.
- Workload-Seite: Der lokale Build verwendet die auf dem Mac installierte
  Workload (`dotnet workload restore`/VS-Installation); ein Pin wie in der
  CI-Action existiert im Skript nicht. Auf dem Auswertungsrechner ist
  `ios 27.2.11039/11.0.100` installiert (Nachweis `dotnet workload list`,
  s. tests.md → Umgebung), d. h. die Pack-Version ist hier bereits der
  Zielstand — ohne passendes Xcode würde derselbe „requires Xcode 27.2"-
  Fehler wie in CI auftreten.

## `scripts/iOS-Deployment.md` (269 Zeilen, Skript-Notizen)

- Dokumentiert Ablauf, Voraussetzungen, bekannte Probleme und den CI-Pfad
  (Abschnitt „CI-Pfad: Release → TestFlight", Zeilen 207–229: Secrets-Liste,
  `IOS_SIGNING_ENABLED`, Buildnummer = `github.run_number` — *veraltet*:
  die Action nutzt inzwischen `git rev-list --count + run_attempt - 1`,
  Zeile 151).
- Enthält **keine** Aussage zu Xcode-Mindestversion oder iOS-SDK-Version.
- Hinweis „seit Xcode 16 nicht mehr in Xcode enthalten" (iTMSTransporter,
  Zeilen 62–68, 156–176) — sonst keine SDK/Xcode-Anforderung.

## `docs/help/ios-deployment/` (Anwenderdoku)

| Datei | Relevanter Inhalt |
|-------|-------------------|
| `installation.md` | Voraussetzung Zeile 9: „macOS mit Xcode (kompatibel zur installierten .NET-MAUI-Version)" — keine konkrete Mindestversion. Zeile 10 nennt veraltet „.NET SDK 10.x" (Migration auf .NET 11 ist erfolgt; `docs/help/release-management/installation.md` Zeile 12 sagt korrekt `11.0.x`). |
| `ablauf-technisch.md` | Beschreibt `Invoke-IpaValidation` samt aller Bundle-Gates (Zeilen 33–43) — spiegelt exakt den Skriptstand, kein SDK-Gate. |
| `einrichtung-anwender.md` | Zertifikats-/Profil-/API-Key-Einrichtung; Xcode nur als Werkzeug für „Manage Certificates" und Transporter-Hinweis (seit Xcode 16 ohne `iTMSTransporter`). |
| `troubleshooting.md` | Keine Xcode-Versions-/ITMS-90725-Thematik; dokumentiert u. a. Missing-Compliance, Icon-Alpha (ITMS-90717), `iTMSTransporter`-Suche. |
| `beschreibung.md` / `ablauf-anwender.md` / `index.md` | Funktionale Beschreibung, keine Build-Toolchain-Anforderungen. |

## `docs/help/release-management/` (CI-Doku)

| Datei | Relevanter Inhalt |
|-------|-------------------|
| `architektur.md` | Tabelle Zeile 22: `package-ios` = „iOS-Publish `net11.0-ios`/`ios-arm64` auf `macos-latest`" — Runner-Label dokumentiert, Xcode nicht. |
| `installation.md` | Zeile 12: „`macos-latest` nur für `package-ios`"; Secrets-Tabelle `IOS_*`; keine Xcode-/SDK-Anforderung. |
| `ablauf-technisch.md` | Zeile 42: iOS-Job-Ablauf; Fehlerbehandlung-Tabelle Zeile 169 (kein/.mehrere `.ipa` → Abbruch); kein SDK-Gate erwähnt. |
| `troubleshooting.md` | „`package-ios` wird übersprungen / `release-ios.ipa` fehlt" (`IOS_SIGNING_ENABLED`-Erklärung) — kein Xcode-/ITMS-90725-Eintrag. |
| `business-rules.md` / `wiederkehrende-reviews.md` | Keine iOS-Build-Toolchain-Regeln. |

## `docs/app-store-review.md`

- Enthält Abschnitte zu bereits erlebten Apple-Prüfpunkten (Privacy Label,
  iCloud-Backup, ATS, Altersfreigabe, iPad-Entscheidung, ITMS-90717
  Icon-Alpha, `iTMSTransporter`-Wechsel von `altool`).
- **Kein** Abschnitt zu ITMS-90725/SDK-Version — wäre der naheliegende Ort,
  die neue Apple-Anforderung dauerhaft zu dokumentieren.

## `docs/RELEASE_NOTES.md`

- Aktuelle (0.8.0-)Notes; erwähnt die .NET-11-Umstellung nur als
  Mindest-Prozessoranforderung — kein iOS-/SDK-Hinweis.

## `issue.md` (Repo-Root, Arbeitskontext)

- Original-Issue #204 „Nachricht von Apple" mit ITMS-90725-Volltext:
  „This app was built with the iOS 26.5 SDK. Starting April 2027, iOS and
  iPadOS apps must be built with the iOS 27 SDK or later, included in
  Xcode 27 or later…" (Version 0.8.0, Build 331, App Apple ID 6812664201).
