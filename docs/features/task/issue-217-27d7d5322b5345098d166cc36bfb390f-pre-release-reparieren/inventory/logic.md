<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik — Bestandsaufnahme (Issue #217)

Betroffen sind keine klassischen Logikklassen im App-Code, sondern der
Plattform-Lifecycle (iOS/MacCatalyst) und die CI-Paketierungslogik. Nachfolgend
der tatsächliche Ist-Zustand.

## `Reporter.AppDelegate` (iOS)

Datei: `src/Reporter/Platforms/iOS/AppDelegate.cs`

- `[Register("AppDelegate")]`, Basisklasse `MauiUIApplicationDelegate`.
- Hält eine starke Referenz auf `NotificationDelegate` (`_notificationDelegate`),
  weil `UNUserNotificationCenter.Delegate` eine weak reference ist (Kommentar
  Zeile 22–24).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateMauiApp()` | protected override | Liefert `MauiProgram.CreateMauiApp()` (Zeile 30) |
| `FinishedLaunching(UIApplication, NSDictionary?)` | public override | Setzt `UNUserNotificationCenter.Current.Delegate`, ruft `base.FinishedLaunching`, verlagert die BGTaskScheduler-Registrierung via `DispatchQueue.MainQueue.DispatchAsync` auf den nächsten Main-Queue-Durchlauf (Zeilen 33–42; Kommentar begründet dies mit `_os_unfair_lock_recursive_abort` bei synchronem Aufruf) |
| `RegisterBackgroundFetchTask()` | private | `BGTaskScheduler.Shared.Register(BackgroundRefreshService.RefreshTaskIdentifier, …)`; Ergebnis via `BackgroundRefreshService.NotifyTaskRegistered(registered)` zurückgemeldet; Fehler werden geloggt, nicht geworfen (Zeilen 44–79) |
| `HandleRefreshTaskAsync(BGAppRefreshTask)` | private async | Führt `IScheduledSyncRunner.RunAsync` aus dem DI-Container aus, setzt `ExpirationHandler`, meldet `task.SetTaskCompleted(success)` (Zeilen 81–117) |

Querverweise:
- Wird instanziiert von `Program.Main` → `UIApplication.Main(args, null, typeof(AppDelegate))` (`src/Reporter/Platforms/iOS/Program.cs`, Zeile 18).
- Nutzt `BackgroundRefreshService.RefreshTaskIdentifier` (`"de.martinstromberg.reporter.feedrefresh"`, `src/Reporter/Services/BackgroundRefreshService.cs`, Zeile 27) — derselbe Identifier steht als `BGTaskSchedulerPermittedIdentifiers`-Eintrag in `Platforms/iOS/Info.plist` (Zeilen 31–34).
- `NotificationDelegate` (`src/Reporter/Platforms/iOS/NotificationDelegate.cs`) erbt `UNUserNotificationCenterDelegate` und handhabt Notification-Taps (Shell-Navigation `articledetail?itemId=…`/`//unread`, `Launcher`-Fallback).

## `Reporter.AppDelegate` (MacCatalyst)

Datei: `src/Reporter/Platforms/MacCatalyst/AppDelegate.cs`

- `[Register("AppDelegate")]`, Basisklasse `MauiUIApplicationDelegate`, nur
  `CreateMauiApp()`-Override (Zeilen 10–17). Einstieg analog via
  `Platforms/MacCatalyst/Program.cs` (Zeile 18).

## `MauiProgram`

Datei: `src/Reporter/MauiProgram.cs`

- `public static class MauiProgram` mit `CreateMauiApp()` (Zeile 26/28);
  `builder.UseMauiApp<App>()` (Zeile 32).
- **Kein** `ConfigureLifecycleEvents`-Aufruf vorhanden — plattformübergreifende
  Lifecycle-Hooks existieren im Ist-Zustand nicht.

## `SceneDelegate`

- **Nicht vorhanden.** Es existiert weder `Platforms/iOS/SceneDelegate.cs` noch
  `Platforms/MacCatalyst/SceneDelegate.cs`; repo-weit kein Treffer für
  `SceneDelegate`, `MauiUISceneDelegate` oder `UIApplicationSceneManifest`
  außerhalb der Anforderungsdokumente.

## CI-Paketierungslogik

### `.github/actions/package-android/action.yml`

| Schritt | Befehl / Funktion | Relevanz |
|---------|-------------------|----------|
| Setup .NET | `actions/setup-dotnet` `11.0.x` | |
| Install .NET MAUI workload | `dotnet workload restore Reporter.sln` (env `IncludeIosTarget=false`, `IncludeAndroidTarget=true`) | Workload-Version **ungepinnt** — `workload restore` zieht das jeweils aktuelle Manifest des 11.0.100-Bands |
| Restore | `dotnet restore Reporter.sln` (Zeile 29) — **ohne RID** | Erzeugt kein RID-spezifisches Runtime-Pack in der assets-Datei |
| Publish (android) | `dotnet publish src/Reporter/Reporter.csproj … --configuration Release --framework net11.0-android -p:AndroidPackageFormat=apk --no-restore` (Zeilen 47–51) | Kein `RuntimeIdentifier`-Parameter, kein `PublishReadyToRun`-Parameter |
| Collect Android package | pwsh-Funktion `Resolve-SingleApk`: bevorzugt `*-Signed.apk` unter `publish/`, Fehler bei mehreren distinkten APKs oder keinem Fund (Zeilen 57–83) | |

### `.github/actions/package-ios/action.yml`

| Schritt | Befehl / Funktion | Relevanz |
|---------|-------------------|----------|
| Select Xcode | `DEVELOPER_DIR=/Applications/Xcode_27.2.app/Contents/Developer` (Input `xcode-path`), Fail-fast bei fehlendem Bundle (Zeilen 65–81) | Workload-Pin verlangt exakt Xcode 27.2 |
| Setup .NET | `11.0.1xx` | |
| Pin SDK band | schreibt CI-lokale `global.json` `11.0.100`/`latestPatch` (Zeilen 97–100) — absichtlich nicht committed | |
| Install .NET MAUI workload | Rollback-File pinnt `microsoft.net.sdk.ios` auf Input `ios-workload-version` (Default `27.2.11039`), `workload install maui-ios ios --skip-manifest-update`, Entfernen anderer installierter iOS-Pack-Versionen (Zeilen 108–124) | |
| Restore | `dotnet restore src/Reporter/Reporter.csproj -r ios-arm64` (Zeile 128) | RID steht im assets file |
| Publish (ios) | `dotnet publish … -f net11.0-ios -p:RuntimeIdentifier=ios-arm64 -p:ArchiveOnBuild=true -p:MauiXamlInflator=XamlC --no-restore` (Zeilen 183–192) | Hier schlägt `error MT7199` zu (fehlendes `UIApplicationSceneManifest`) |
| Collect iOS package | `.ipa`-Suche unter `bin/Release`, Dedupe per Basename, Fehler bei 0/>1 distinkten IPAs (Zeilen 197–221) | |
| Verify built SDK version | liest `DTSDKName`/`DTPlatformVersion`/`DTXcode` aus der App-`Info.plist`; schlägt fehl, wenn `DTSDKName` nicht `iphoneos≥27` (Zeilen 230–256) | ITMS-90725-Gate |
| Upload to TestFlight | nur bei gesetztem `api-key-p8`; iTMSTransporter-Suche + Standalone-Fallback (Zeilen 262–312) | |

### `.github/actions/build-and-package/action.yml` (Windows)

- Restore `dotnet restore Reporter.sln -r win-x64` (Zeile 28).
- Publish `dotnet publish … -f net11.0-windows10.0.19041.0 --runtime win-x64 --self-contained true -p:PublishReadyToRun=false --no-restore` (Zeile 49).
- Historie: `-p:PublishReadyToRun=false` wurde mit Commit `ee41281`
  („fix: Disable PublishReadyToRun in release packaging to avoid missing
  runtime pack", 2026-09-10) ergänzt — gleiches Fehlerbild wie jetzt beim
  Android-Pfad (NETSDK1094), dort bereits gelöst.

## Herkunft von `PublishReadyToRun` beim Android-Publish (verifiziert)

Repo-weit wird `PublishReadyToRun` nirgends gesetzt (nur `=false` im
Windows-Pfad und Erwähnungen in Doku). Die Auswertung des installierten
Android-Workload-Packs zeigt:

- `Microsoft.Android.Sdk.Windows` **37.2.31** (`targets/Microsoft.Android.Sdk.DefaultProperties.targets`, Zeilen 33–35): `_AndroidRuntime` defaultet auf **`CoreCLR`** (nur `PublishAot` → `NativeAOT`, `UseMonoRuntime=true` → `MonoVM`).
- `targets/Microsoft.Android.Sdk.CoreCLR.targets`:
  - Zeile 19: `PublishReadyToRun` defaultet auf `true`, wenn `Configuration == 'Release'` (oder Publish ohne Debug-Symbole).
  - Zeilen 20–23: Ist `PublishTrimmed` nicht gesetzt, wird `PublishReadyToRun` auf `false` zurückgesetzt, dafür `PublishReadyToRunComposite=true` — der R2R-Pfad bleibt aktiv und benötigt das `Microsoft.NETCore.App.Runtime.*`-Pack.
  - Zeile 24: `_IsPublishing` wird auf `true` gesetzt, sobald `PublishReadyToRun` wirkt — damit greift die RID-Inferenz des .NET SDK (`Microsoft.NET.RuntimeIdentifierInference.targets`), die `PublishRuntimeIdentifier`/`RuntimeIdentifier` setzt.
- Folge: `dotnet publish … -f net11.0-android -c Release --no-restore` fordert das R2R-Runtime-Pack für den inferierten Android-RID, das der RID-lose `dotnet restore Reporter.sln` nicht aufgelöst hat → `error NETSDK1094`. Der Windows-Pfad vermeidet das durch explizites `-p:PublishReadyToRun=false`.
- Verifiziert lokal: `dotnet msbuild -getProperty` (TFM `net11.0-android`) liefert `PublishReadyToRun` leer, `SelfContained=true` — der R2R-Default greift erst innerhalb des Publish-Targets über die CoreCLR-Targets, nicht in der Projektdatei.

## Release-Skripte (Node)

| Skript | Zweck | Bezug |
|--------|-------|-------|
| `scripts/release-assets.mjs` | `buildReleaseAssets({iosSigningEnabled})` → Asset-Liste `release-win-x64.zip`, `release-android.apk`, ggf. `release-ios.ipa`; `releaseAssetEnv`/`emitReleaseAssetEnv` schreiben `RELEASE_ASSET_FILES` etc. (Zeilen 6–39) | Wird im `prerelease`-Job (`staging-ci.yml` Zeile 399) und `publish`-Job (`release.yml` Zeile 227) aufgerufen |
| `scripts/resolve-release-version.mjs` | Tag-/Branch-Klassifikation, semantic-release-Dry-Run, Repair unvollständiger Releases (`resolveReleaseVersion`, `classifyWorkflowRef`, `releaseHasExpectedAsset`, `incompleteReleases`) | Nur `release.yml` (Zeile 58); `staging-ci.yml` nutzt stattdessen einen Inline-Dry-Run (Zeilen 267–280) |
| `scripts/create-update-manifest.mjs` | erzeugt `update.json` | `staging-ci.yml` Zeile 405, `release.yml` Zeile 233 |
