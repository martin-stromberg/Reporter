<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plattform-Konfiguration — Bestandsaufnahme

## `src/Reporter/Platforms/iOS/Info.plist`

Enthält `LSRequiresIPhoneOS`, `UIDeviceFamily`, `UIRequiredDeviceCapabilities`, `UISupportedInterfaceOrientations(~ipad)`, `XSAppIconAssets`, `CFBundleIdentifier` (`de.martinstromberg.reporter`). **Kein `NSAppTransportSecurity`-Schlüssel** — ATS-Standardverhalten aktiv, Klartext-HTTP wird blockiert.

## `src/Reporter/Platforms/MacCatalyst/Info.plist`

Enthält `UIDeviceFamily`, `LSApplicationCategoryType`, `UIRequiredDeviceCapabilities`, `UISupportedInterfaceOrientations(~ipad)`, `XSAppIconAssets` sowie auskommentierte `ITSAppUsesNonExemptEncryption`/`LSApplicationCategoryType`-Hinweise. **Kein `NSAppTransportSecurity`-Schlüssel** — dieselbe ATS-Thematik.

Eine Suche nach `NSAppTransportSecurity`/`NSAllowsArbitraryLoads` in `src/` liefert keinen Treffer.

## `src/Reporter/Reporter.csproj` (Target-Steuerung)

- `IncludeIosTarget` default `true`, `IncludeAndroidTarget` default `false` (PropertyGroup, Kopf der Datei).
- Windows: `net10.0-windows10.0.19041.0` (+ `net10.0-ios` je nach Flags); macOS: `net10.0-ios`.
- `SupportedOSPlatformVersion`: iOS/MacCatalyst `15.0`.
- CI setzt `IncludeIosTarget: false`, `IncludeAndroidTarget: false` (`.github/workflows/staging-ci.yml`, `pr-staging-ci.yml`).

## Weitere Plattform-/Build-Artefakte

- `src/Reporter/Platforms/iOS/`: `AppDelegate.cs`, `NotificationDelegate.cs`, `Program.cs`, `Resources/`
- `src/Reporter/Platforms/MacCatalyst/`: `AppDelegate.cs`, `Entitlements.plist`, `Program.cs`
- `scripts/iOS-Deployment.ps1` + `scripts/iOS-Deployment.md` — vorhandenes Deployment-/Verifikations-Skript für iOS.
- `HttpClient`-Registrierung: `src/Reporter/MauiProgram.cs:55` — `AddSingleton<HttpClient>(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(30) })`, kein plattformspezifischer Handler (iOS/MacCatalyst-Default: `NSUrlSessionHandler` → ATS greift).
