<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Issue #217 „Pre-Release reparieren" (MT7199 + NETSDK1094)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | iOS-Plattform | `src/Reporter/Platforms/iOS/SceneDelegate.cs` anlegen (PolyForm-Header, `[Register("SceneDelegate")]`, `public class SceneDelegate : MauiUISceneDelegate`, `using Foundation; using Microsoft.Maui;`) | Erledigt | Kein direkter Test — Lizenzheader-/Format-Check (`Run-StaticChecks.ps1`, Exit 0); CI-`package-ios`-Build prüft Registrierung gegen Manifest (MT7199) |
| 2 | iOS-Plattform | `UIApplicationSceneManifest` in `src/Reporter/Platforms/iOS/Info.plist` ergänzen (`UIApplicationSupportsMultipleScenes=false`, `UISceneConfigurations` → `UIWindowSceneSessionRoleApplication` → `UISceneConfigurationName=__MAUI_DEFAULT_SCENE_CONFIGURATION__`, `UISceneDelegateClassName=SceneDelegate`) | Erledigt | Kein direkter Test — CI-Build-Check `error MT7199` schlägt fehl, wenn das Manifest fehlt/fehlerhaft ist (`package-ios`) |
| 3 | MacCatalyst-Plattform | `src/Reporter/Platforms/MacCatalyst/SceneDelegate.cs` anlegen (identisch zu iOS-Variante) | Erledigt | Kein direkter Test — Lizenzheader-/Format-Check (`Run-StaticChecks.ps1`, Exit 0); nicht build-verifizierbar (`net11.0-maccatalyst` nicht in `TargetFrameworks`) |
| 4 | MacCatalyst-Plattform | `UIApplicationSceneManifest` in `src/Reporter/Platforms/MacCatalyst/Info.plist` ergänzen (identische Struktur wie iOS) | Erledigt | Kein direkter Test — nicht build-verifizierbar (kein MacCatalyst-TFM aktiv); Sichtprüfung |
| 5 | CI-Packaging | `-p:PublishReadyToRun=false` im Schritt `Publish (android)` von `.github/actions/package-android/action.yml` ergänzen (inkl. Kommentar zum CoreCLR-R2R-Default seit .NET 11) | Erledigt | Kein direkter Test — CI-`package-android`-Job schlägt mit `error NETSDK1094` fehl, wenn der Parameter fehlt |
| 6 | Verifikation | `.\scripts\Run-StaticChecks.ps1` ausführen (Exit-Code 0, keine Findings) | Erledigt | `Run-StaticChecks.ps1` lokal ausgeführt: Format, Lizenzheader, NuGet-/npm-Audit und Static-Analysis-Build ohne Findings, Exit-Code 0 |
| 7 | Verifikation | `staging-ci`-Lauf nach Merge verifizieren: `package-android` ohne NETSDK1094, `package-ios` ohne MT7199 inkl. `DTSDKName=iphoneos≥27`-Gate, `prerelease` erzeugt RC-Tag + Release mit vier Assets | Offen | — |
| 8 | Verifikation (optional/manuell) | RC-IPA unter iOS 27 starten (TestFlight/Simulator bzw. `scripts/iOS-Deployment.ps1`) — kein Absturz, Feedliste sichtbar | Offen | — |
