<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme — Plattform-Metadaten, Skripte und Dokumentation

## `Info.plist` (iOS)
Datei: `src/Reporter/Platforms/iOS/Info.plist`

| Schlüssel | Ist-Wert | Relevanz / Befund |
|-----------|----------|-------------------|
| `LSRequiresIPhoneOS` | `true` (Zeile 7–8) | iPhone-only-Systemvoraussetzung |
| `UIDeviceFamily` | `[1, 2]` (Zeilen 9–13) | iPhone **und** iPad — Entscheidung Punkt 7 offen |
| `UIRequiredDeviceCapabilities` | `[arm64]` (Zeilen 14–17) | — |
| `UISupportedInterfaceOrientations` | Portrait, LandscapeLeft, LandscapeRight (Zeilen 18–23) | iPhone-Orientierungen |
| `UISupportedInterfaceOrientations~ipad` | Portrait, PortraitUpsideDown, LandscapeLeft, LandscapeRight (Zeilen 24–30) | nur bei iPad-Beibehaltung relevant |
| `XSAppIconAssets` | `Assets.xcassets/appicon.appiconset` (Zeilen 31–32) | Icon-Asset-Verweis |
| `CFBundleIdentifier` | `de.martinstromberg.reporter` (Zeilen 33–34) | — |
| `UIBackgroundModes` | `[fetch]` (Zeilen 35–38) | OS-Hintergrundabruf |
| `BGTaskSchedulerPermittedIdentifiers` | `[de.martinstromberg.reporter.feedrefresh]` (Zeilen 39–42) | muss zu `BackgroundRefreshService.RefreshTaskIdentifier` passen |
| `NSAppTransportSecurity` | `NSAllowsArbitraryLoads = true` (Zeilen 43–47) | ATS-Ausnahme — begründungspflichtig (Punkt 4); Begründung existiert ansatzweise in `docs/help/anwendung/architektur.md` (Zeile 50) und `docs/help/ios-deployment/ablauf-anwender.md` (Zeile ~89) |
| `ITSAppUsesNonExemptEncryption` | `false` (Zeilen 48–49) | Krypto-Fragebogen bereits beantwortet |
| `CFBundleLocalizations` | **nicht vorhanden** | Punkt 9: `en`/`de` zu ergänzen; deckt sich mit `AppResources.resx` + `AppResources.de.resx` (`src/Reporter.Core/Resources/Strings/`) und `Settings.Language` (`system`/`de`/`en`) |

Die Mac-Catalyst-`Info.plist` (`src/Reporter/Platforms/MacCatalyst/Info.plist`) enthält ebenfalls `NSAppTransportSecurity`/`NSAllowsArbitraryLoads` (Zeilen 41–43) und `UIDeviceFamily` (Zeile 16) — MacCatalyst ist kein konfiguriertes Target (`TargetFrameworks` ohne `maccatalyst`), die Datei ist aber vorhanden.

## `PrivacyInfo.xcprivacy`
Datei: `src/Reporter/Platforms/iOS/Resources/PrivacyInfo.xcprivacy`

Ist-Stand (Punkt 5):

| Eintrag | Status |
|---------|--------|
| `NSPrivacyAccessedAPITypes` → `NSPrivacyAccessedAPICategoryFileTimestamp` mit Reason `C617.1` | aktiv (Zeilen 18–24) |
| `NSPrivacyAccessedAPITypes` → `NSPrivacyAccessedAPICategorySystemBootTime` mit Reason `35F9.1` | aktiv (Zeilen 25–32) |
| `NSPrivacyAccessedAPITypes` → `NSPrivacyAccessedAPICategoryDiskSpace` mit Reason `E174.1` | aktiv (Zeilen 33–40) |
| `NSPrivacyAccessedAPITypes` → `NSPrivacyAccessedAPICategoryUserDefaults` mit Reason `CA92.1` | **auskommentiert** (Zeilen 41–50) — „only needed when you're using the Preferences API" |
| `NSPrivacyTracking` | **nicht vorhanden** |
| `NSPrivacyTrackingDomains` | **nicht vorhanden** |
| `NSPrivacyCollectedDataTypes` | **nicht vorhanden** |

Hinweis im Dateikopf: Minimum-Manifest für .NET MAUI; app-spezifische Einträge sind vom Projekt zu ergänzen. `Microsoft.Maui.Essentials`-`Preferences`/UserDefaults-Nutzung ist im Code nicht ersichtlich — Einstellungen liegen in SQLite (`Settings`-Tabelle), nicht in `NSUserDefaults`.

## `Reporter.csproj` / App-Icon
Datei: `src/Reporter/Reporter.csproj`, `src/Reporter/Resources/AppIcon/`

- Zeile 60: `<MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#1e293b" />` — opake Hintergrundfarbe ist gesetzt (Punkt 10: Alpha-Kanal-Verifikation des **generierten** Assets steht noch aus).
- `appicon.svg`: 456×456, einziges Element `<rect fill="#1e293b">` — komplett opaker Hintergrund.
- `appiconfg.svg`: 400×400 Vordergrund (Badge-Rahmen, RSS-Wellen, Amber-Dot), `fill="none"` am Root — transparente Bereiche werden durch die `Color`-Fläche gedeckt.
- `TargetFrameworks` unter Windows: `net10.0-windows10.0.19041.0;net10.0-ios` (`IncludeIosTarget` default `true`, `IncludeAndroidTarget` default `false`); unter macOS `net10.0-ios`.
- iOS-PropertyGroup (Zeilen 52–56): `SupportedOSPlatformVersion` iOS `15.0`, `<UseInterpreter>true</UseInterpreter>` (EF-Core-`Expression.Compile()` braucht den Interpreter auf AOT-Devices).
- `ApplicationDisplayVersion` `1.0`, `ApplicationVersion` `10` (CFBundleVersion — wird von `Update-BuildNumber` im Deployment-Skript inkrementiert).
- `Directory.Build.props` (Zeilen 7–11): `DebugReportRecipient = mstromberg84+reporter@gmail.com` — kommentiert als zu ersetzende Platzhalter-/Support-Adresse; potenzieller Kontakt für die Datenschutzerklärung.

## `scripts/iOS-Deployment.ps1`
Datei: `scripts/iOS-Deployment.ps1` (1045 Zeilen)

Betroffene Funktionen (Punkt 11 — `xcrun altool` ist bei Apple deprecated):

| Funktion | Zeilen | Ist-Stand |
|----------|--------|-----------|
| `Invoke-IpaValidation` | 549–572 | macOS-Bash-Skript: `ditto`-Entpacken, `codesign --verify --deep --strict -vvv`, `embedded.mobileprovision`-Check auf `get-task-allow=true` (Abbruch bei Dev-Profil), dann **`xcrun altool --validate-app -f "$IPA" --type ios --apiKey … --apiIssuer …` (Zeilen 567–568)** |
| `Invoke-StoreUpload` | 574–582 | **`xcrun altool --upload-app -f "$MacIpa" --type ios --apiKey … --apiIssuer …` (Zeile 578)** |
| `Copy-ApiKeyToMac` | 518–536 | spiegelt `AuthKey_<ApiKeyId>.p8` nach `$HOME/.appstoreconnect/private_keys/` — fester `altool`-Suchpfad; Kompatibilität des Ersatztools (z. B. `iTMSTransporter`, das denselben Pfad nutzt) zu prüfen |
| `Assert-StorePrerequisites` | 365–410 | erzwingt `.p8` außerhalb des Repo-Roots, Distribution-Zertifikat, SSH (Windows) |
| `Invoke-OnMac` | 461–486 | führt Bash-Skripte lokal (macOS) oder per `ssh -o BatchMode=yes` (Windows) aus — alle `xcrun`-Aufrufe laufen auf dem Mac |
| `Update-BuildNumber` | 412–426 | `ApplicationVersion`-Bump im csproj (Opt-out `-NoBumpBuildNumber`) |
| `Invoke-Store` / `Invoke-Upload` | 584–608 | Orchestrierung: Prereqs → Bump → Build → `Copy-ApiKeyToMac` → `Get-MacIpaPath` → `Invoke-IpaValidation` → `Invoke-StoreUpload` |

API-Key-Auth (`--apiKey`/`--apiIssuer`) ist für `altool` und `iTMSTransporter` identisch.

## Bestehende Dokumentation (Update-Bedarf bzw. Quellen)

| Datei | Relevanz |
|-------|----------|
| `scripts/iOS-Deployment.md` | dokumentiert den `store`/`upload`-Ablauf inkl. beider `altool`-Aufrufe (Zeilen 81–82); Abschnitt „`altool` ist deprecated" (Zeilen 136–141) nennt bereits `iTMSTransporter`/Transporter-App als Ausweichweg; Suchpfad-Hinweis `~/.appstoreconnect/private_keys/` (Zeile 61) |
| `docs/help/ios-deployment/ablauf-technisch.md` | Schritte 6–7 dokumentieren `altool --validate-app`/`--upload-app` (Zeilen 40, 44) und den `Copy-ApiKeyToMac`-Suchpfad (Zeile 27) |
| `docs/help/ios-deployment/einrichtung-anwender.md` | Zeile 125: `store`/`upload` führen `codesign`, `security`, `xcrun altool` aus |
| `docs/help/ios-deployment/troubleshooting.md` | `altool`-Fehlerbilder (Zeilen 95, 101) und Abschnitt „`xcrun altool` nicht gefunden / deprecated" (Zeilen 125–129) |
| `docs/help/ios-deployment/ablauf-anwender.md` | App-Store-Connect-Ablauf; enthält bereits Hinweise auf ATS-Begründung (`NSAllowsArbitraryLoads`, Zeile ~89), App-Privacy-Fragebogen („sammelt keine Daten"), Altersfreigabe und Pflicht-Screenshots inkl. iPad 13″ (Zeile ~86) — **iPad-Screenshots setzen die `UIDeviceFamily`-Entscheidung faktisch voraus** |
| `docs/help/anwendung/architektur.md` | Zeile 50: ATS-Begründung (`NSAllowsArbitraryLoads` wegen anwenderdefinierter `http`-Feed-URLs; `NSExceptionDomains` kann beliebige Hosts nicht abdecken); Zeile 77: beschreibt das bisherige Offline-only-Blocking in `OnWebViewNavigating` — wird durch Punkt 3 überholt |
| `docs/help/anwendung/artikeldetailansicht.md` | Zeilen 24–33: Offline-Verhalten der Detailansicht (Links nicht anklickbar, Hinweisdialog) — Online-Verhalten externer Links ist dort nicht beschrieben |
| `docs/help/anwendung/offline.md` | Zeile 22: Offline-Verhalten der Detailansicht inkl. Hinweisdialog |
| `docs/help/anwendung/mobile-ui-design.md` | Ort für die nach `AGENTS.md` geforderte UI-Verifikations-Doku (Screenshot + getestete Größen) |
| `docs/help/tests/index.md` | beschreibt Unit-/Integrationstests, E2E-Suite, Compiled Bindings |

## Noch nicht vorhandene Dokumentationsartefakte (aus der Anforderung)

- Datenschutzerklärung (`docs/privacy-policy.md` o. ä.) — **existiert nicht**
- App-Privacy-Label-Angaben — **existiert nicht**
- Review-Notizen inkl. ATS-Begründung, „kein Login"/Demo-Feed-Hinweis, Altersfreigabe (`docs/app-store-review.md` o. ä.) — **existiert nicht**
- Dokumentierte iPad-Entscheidung — **existiert nicht**
- App-Icon-Verifikation (ITMS-90717) — **existiert nicht**
