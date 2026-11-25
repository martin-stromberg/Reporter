<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plattform-Dateien — Bestandsaufnahme (Issue #217)

## `src/Reporter/Platforms/iOS/Info.plist`

Vorhandene Schlüssel im Root-`<dict>` (Zeilen 6–47):

| Schlüssel | Wert | Zweck |
|-----------|------|-------|
| `LSRequiresIPhoneOS` | `true` | iPhone-only |
| `UIDeviceFamily` | `[1]` | iPhone |
| `UIRequiredDeviceCapabilities` | `[arm64]` | |
| `UISupportedInterfaceOrientations` | Portrait, LandscapeLeft, LandscapeRight | |
| `XSAppIconAssets` | `Assets.xcassets/appicon.appiconset` | |
| `CFBundleIdentifier` | `de.martinstromberg.reporter` | = `ApplicationId` aus `Reporter.csproj` |
| `UIBackgroundModes` | `[fetch]` | Background-Fetch für `BGAppRefreshTask` |
| `BGTaskSchedulerPermittedIdentifiers` | `[de.martinstromberg.reporter.feedrefresh]` | muss zu `BackgroundRefreshService.RefreshTaskIdentifier` passen |
| `NSAppTransportSecurity` | `NSAllowsArbitraryLoads=true` | HTTP-Feeds zulässig |
| `ITSAppUsesNonExemptEncryption` | `false` | App-Store-Export-Frage |
| `CFBundleLocalizations` | `[en, de]` | |

**Nicht vorhanden:** `UIApplicationSceneManifest` (kein `UISceneConfigurations`,
kein `UIApplicationSupportsMultipleScenes`) — genau das Fehlen löst `error
MT7199` beim Build gegen iOS SDK 27 aus.

Weitere Dateien im Verzeichnis: `Program.cs` (Einstieg,
`UIApplication.Main(..., typeof(AppDelegate))`), `AppDelegate.cs`,
`NotificationDelegate.cs`, `Resources/PrivacyInfo.xcprivacy`.
**Kein `SceneDelegate.cs` vorhanden.**

## `src/Reporter/Platforms/MacCatalyst/Info.plist`

Vorhandene Schlüssel (Zeilen 6–48): `UIDeviceFamily=[2]`,
`LSApplicationCategoryType=public.app-category.lifestyle`,
`UIRequiredDeviceCapabilities=[arm64]`, `UISupportedInterfaceOrientations`
(+`~ipad`-Variante), `XSAppIconAssets`, `NSAppTransportSecurity`,
`ITSAppUsesNonExemptEncryption=false`.

**Nicht vorhanden:** `UIApplicationSceneManifest`; **kein `SceneDelegate.cs`**
im Verzeichnis (nur `AppDelegate.cs`, `Program.cs`, `Entitlements.plist`).

Hinweis: `net11.0-maccatalyst` ist **nicht** in `TargetFrameworks` der
`Reporter.csproj` enthalten (Zeilen 8–13: nur `net11.0-windows10.0.19041.0`,
`net11.0-ios`, `net11.0-android` — bedingt über `IncludeIosTarget`/
`IncludeAndroidTarget`). Das `SupportedOSPlatformVersion`-Property für
`maccatalyst` (Zeile 46, `17.0`) ist vorbereitet, aber ohne TFM wirkungslos.
Kein CI-Job baut die Plattform.

## `src/Reporter/Platforms/iOS/Entitlements.plist` / `MacCatalyst/Entitlements.plist`

Vorhanden (MacCatalyst); iOS besitzt kein `Entitlements.plist` im
Platform-Verzeichnis (nur `Resources/PrivacyInfo.xcprivacy`). Für den
Scene-Lifecycle nicht relevant.

## `src/Reporter/Reporter.csproj` — plattformrelevante Properties

| Property | Wert | Zeile |
|----------|------|-------|
| `IncludeIosTarget` / `IncludeAndroidTarget` | Defaults `true` / `false`; steuern die `TargetFrameworks`-Bedingungen | 6–7 |
| `TargetFrameworks` | Windows: `net11.0-windows10.0.19041.0` + optional `net11.0-ios`, `net11.0-android`; macOS: `net11.0-ios` bzw. `net11.0-android` | 8–13 |
| `MauiXamlInflator` | `SourceGen` (iOS-CI-Publish überschreibt mit `XamlC` wegen SourceGen-Fehlern auf dem Runner, vgl. `package-ios/action.yml` Zeilen 173–176) | 30 |
| `ApplicationDisplayVersion` / `ApplicationVersion` | `1.0` / `16` (CI überschreibt per `-p:`) | 39–40 |
| `SupportedOSPlatformVersion` | iOS `15.0`, MacCatalyst `17.0`, Android `24.0`, Windows `10.0.17763.0` | 45–48 |
| `UseInterpreter` (nur `net11.0-ios`) | `true` — EF-Core-`Expression.Compile()` bräuchte sonst AOT-Codegen (ExecutionEngineException auf Device) | 52–56 |
| `PublishReadyToRun` | **nicht gesetzt** | — |
| `ValidateSceneManifest` | **nicht gesetzt** | — |
