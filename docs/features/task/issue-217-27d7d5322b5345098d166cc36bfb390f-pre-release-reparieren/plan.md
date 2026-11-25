<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Issue #217 „Pre-Release reparieren" (MT7199 + NETSDK1094)

## Übersicht

Der `Pre-Release`-Workflow (`staging-ci.yml`) scheitert seit der .NET-11-/Xcode-27-Migration in `package-ios` (`error MT7199`: fehlendes `UIApplicationSceneManifest` in der iOS-`Info.plist`) und `package-android` (`error NETSDK1094`: implizites `PublishReadyToRun` des CoreCLR-Android-Workloads ohne RID-Restore). Umgesetzt werden der von Microsoft dokumentierte UIKit-Scene-Lifecycle (neue `SceneDelegate`-Klassen + Manifest-Einträge in `Platforms/iOS` und `Platforms/MacCatalyst`) sowie `-p:PublishReadyToRun=false` im Publish-Schritt der `package-android`-Action — analog zum bereits bewährten Windows-Pfad (Commit `ee41281`). Keine Änderungen an App-Code, `staging-ci.yml` oder `release.yml` selbst.

## Scope-Klärung

Verbindliche fachliche Abgrenzung vor der Umsetzung — übernommen aus dem Abschnitt `Fachlicher Scope` der `requirement.md`:

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein `Pre-Release`-Lauf von `staging-ci.yml` auf Push nach `staging` mit `version.outputs.changed == 'true'` — Jobs `package-windows`, `package-android`, `package-ios` und Abschlussjob `prerelease` (RC-Tag `v<version>-rc.<n>`, Assets `release-win-x64.zip`, `release-android.apk`, `release-ios.ipa`, `update.json`) |
| Auslösende Ereignisse | Push/Merge auf `staging`, sobald `semantic-release` eine neue Version ermittelt; externer Trigger: Fehlschlag von Run `45486899366` (2026-11-24) nach Merge von Issue #210 |
| Erwartete Wirkung | `Pre-Release`-Lauf endet mit `success` inkl. `prerelease`: RC-Tag und GitHub-Pre-Release mit iOS-, Android- und Windows-Paketen; die iOS-App startet unter iOS 27 ohne Absturz, weil der Scene-Lifecycle wirksam implementiert ist |
| Umgang mit fehlenden Objekten | `UIApplicationSceneManifest` in `Platforms/iOS/Info.plist`: **erzeugen** (`UISceneConfigurations` → `UIWindowSceneSessionRoleApplication` mit `UISceneConfigurationName` `__MAUI_DEFAULT_SCENE_CONFIGURATION__` und `UISceneDelegateClassName` `SceneDelegate`; `UIApplicationSupportsMultipleScenes` `false`). `Platforms/iOS/SceneDelegate.cs`: **erzeugen** (`[Register("SceneDelegate")]`, `MauiUISceneDelegate`). R2R-Runtime-Pack für Android: **verwerfen** (`-p:PublishReadyToRun=false` beim Publish — Entscheidung Variante b, siehe Designentscheidungen). `UIApplicationSceneManifest` + `SceneDelegate.cs` für `Platforms/MacCatalyst`: **erzeugen** (präventiv, siehe Designentscheidungen) |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Kein `ValidateSceneManifest=false` | Deaktiviert nur den Build-Check (MT7199); die App würde unter iOS 27 beim Start weiter abstürzen (`UIScene life cycle is required`) | bestätigt (2026-11-25) |
| 2 | Keine Änderung an `package-windows` / `build-and-package` | Läuft bereits erfolgreich mit `-p:PublishReadyToRun=false` (Commit `ee41281`) | bestätigt (2026-11-25) |
| 3 | Keine funktionalen Änderungen an Features, UI oder Datenmodell | Reine Plattform-Lifecycle- und CI-/Packaging-Korrektur | bestätigt (2026-11-25) |
| 4 | Keine Anhebung von `SupportedOSPlatformVersion` (iOS `15.0`, MacCatalyst `17.0`, Android `24.0`) | Die Scene-Pflicht hängt am Build-SDK (Xcode 27 / iOS SDK ≥ 27), nicht am Deployment-Target | bestätigt (2026-11-25) |
| 5 | Kein separater `release.yml`-Verifikationslauf | `release.yml` nutzt identische Composite-Actions und profitiert implizit; verbindliche Verifikation ist der `staging`-Lauf | bestätigt (2026-11-25) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| `package-android` / NETSDK1094 | Variante (b): `-p:PublishReadyToRun=false` als Parameter im Publish-Schritt der Action | Verifiziert an `Microsoft.Android.Sdk.CoreCLR.targets` 37.2.31 (Zeilen 19–25): Ein globales `-p:PublishReadyToRun=false` verhindert die gesamte R2R-Kette — Zeile 19 (Release-Default `true`), Zeilen 20–23 (`_AndroidReadyToRunWithoutTrimming`/Composite-Umschaltung) und Zeile 24 (`_IsPublishing` → RID-Inferenz) greifen dann alle nicht; kein `Microsoft.NETCore.App.Runtime.*`-Pack nötig. Variante (a) (Restore mit RID + `-p:PublishReadyToRun=true`) verworfen: erfordert RID-Pflege im bislang RID-losen `dotnet restore Reporter.sln` und ein zusätzlich aufzulösendes Runtime-Pack, ohne dokumentierten Nutzen für die APK. Präzedenzfall `ee41281` hat identisches Fehlerbild im Windows-Pfad gelöst |
| Position von `PublishReadyToRun=false` | Kommandozeilen-Parameter in `.github/actions/package-android/action.yml`, nicht in `Reporter.csproj` | Konsistent mit `build-and-package/action.yml` Zeile 49 (gleiches Muster); betrifft ausschließlich den Release-Packaging-Pfad und ändert keine lokalen Debug-/Entwickler-Builds |
| MacCatalyst-Pfad | Manifest + `SceneDelegate.cs` präventiv ergänzen | Microsoft-Migrationsdoku verlangt beide Apple-Plattformordner (`Platforms/iOS` und `Platforms/MacCatalyst`); `net11.0-maccatalyst` ist aktuell nicht in `TargetFrameworks` — Dateien werden erst kompiliert/genutzt, sobald das TFM aktiviert wird, dann aber sofort korrekt. Aufwand minimal (identische Struktur wie iOS). Restrisiko: ohne TFM nicht build-verifizierbar → Sichtprüfung |
| `AppDelegate.FinishedLaunching`-Initialisierungen | Keine Verlagerung — `UNUserNotificationCenter`-Delegate und `DispatchAsync`-`BGTaskScheduler`-Registrierung bleiben im `AppDelegate` | Verifiziert an `MauiUIApplicationDelegate` (MAUI-Quelle, `main`): `FinishedLaunching` läuft auch unter Scene-Lifecycle; bei `HasSceneManifest()` wird lediglich `CreatePlatformWindow` übersprungen, die Lifecycle-Events werden weiterhin aufgerufen. Beide Initialisierungen sind App-weit, nicht Scene-gebunden. Beantwortet offene Frage 4 der `requirement.md` |

## Programmabläufe

### iOS-App-Start unter Scene-Lifecycle

1. `Program.Main` → `UIApplication.Main(args, null, typeof(AppDelegate))` — unverändert (`src/Reporter/Platforms/iOS/Program.cs`).
2. UIKit instanziiert `AppDelegate` und ruft `FinishedLaunching` — unverändert: setzt `UNUserNotificationCenter.Current.Delegate = _notificationDelegate`, ruft `base.FinishedLaunching` (MAUI überspringt hier die Window-Erzeugung, weil `HasSceneManifest()` nun `true` liefert), plant `RegisterBackgroundFetchTask` via `DispatchQueue.MainQueue.DispatchAsync`.
3. UIKit verbindet die Scene-Session anhand des `UIApplicationSceneManifest` in `Info.plist`: `UIWindowSceneSessionRoleApplication` → `UISceneConfigurationName` `__MAUI_DEFAULT_SCENE_CONFIGURATION__`, `UISceneDelegateClassName` `SceneDelegate`.
4. UIKit instanziiert die registrierte Klasse `SceneDelegate` (`[Register("SceneDelegate")]` muss zum plist-Wert passen); `MauiUISceneDelegate` (MAUI-Basisklasse) erzeugt das `UIWindow` in `WillConnect` und aktiviert die MAUI-`App`/Window-Hierarchie.
5. `AppDelegate.CreateMauiApp()` liefert weiterhin `MauiProgram.CreateMauiApp()`; Hintergrund-Refresh (`HandleRefreshTaskAsync` → `IScheduledSyncRunner.RunAsync`) und Notification-Handling (`NotificationDelegate`) funktionieren unverändert.

Beteiligte Klassen/Komponenten: `src/Reporter/Platforms/iOS/Program.cs`, `src/Reporter/Platforms/iOS/AppDelegate.cs`, `src/Reporter/Platforms/iOS/SceneDelegate.cs` (neu), `src/Reporter/Platforms/iOS/Info.plist`, `src/Reporter/MauiProgram.cs`.

### Android-Paketierung im CI (`package-android`)

1. `Setup .NET` 11.0.x → `dotnet workload restore Reporter.sln` (`IncludeAndroidTarget=true`) → `dotnet restore Reporter.sln` (RID-los) — unverändert.
2. `Publish (android)`: `dotnet publish src/Reporter/Reporter.csproj … --configuration Release --framework net11.0-android -p:AndroidPackageFormat=apk -p:PublishReadyToRun=false --no-restore` — **neu:** `-p:PublishReadyToRun=false`. Damit setzen die CoreCLR-Targets des Android-Workloads (`Microsoft.Android.Sdk.Windows` 37.2.31) weder `PublishReadyToRun`/`PublishReadyToRunComposite` noch die R2R-bedingte RID-Inferenz; der RID-lose Restore reicht aus, `error NETSDK1094` entfällt.
3. `Collect Android package` — unverändert: `Resolve-SingleApk` bevorzugt `*-Signed.apk`, kopiert nach `release-android.apk`.

Beteiligte Komponenten: `.github/actions/package-android/action.yml` (Publish-Schritt Zeilen 41–55).

### Pre-Release-Job-Kette (`staging-ci.yml`)

1. Push auf `staging` → Job `version` (semantic-release Dry-Run). Ein `fix:`-Commit erzeugt eine Patch-Version ⇒ `changed == 'true'` — die Paketierungs-Jobs laufen bereits beim Merge dieses Fixes.
2. `package-windows`, `package-android`, `package-ios` laufen parallel; `package-ios` hinter `vars.IOS_SIGNING_ENABLED == 'true'` auf `xcode-27` mit `DEVELOPER_DIR` Xcode 27.2 und Workload-Pin `ios-workload-version=27.2.11039`.
3. `prerelease` (`needs: [version, package-windows, package-android, package-ios]`, `!failure() && !cancelled()`) erzeugt per `gh release create` den RC-Tag `v<version>-rc.<n>` und das GitHub-Pre-Release mit den Assets aus `scripts/release-assets.mjs` plus `update.json` aus `scripts/create-update-manifest.mjs`.
4. Keine Workflow-Änderung erforderlich — der Fix liegt vollständig in Plattformdateien und der `package-android`-Action.

Beteiligte Komponenten: `.github/workflows/staging-ci.yml`, `.github/actions/package-ios/action.yml` (Gate `Verify built SDK version` bleibt wirksam), `.github/actions/package-android/action.yml`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `Reporter.SceneDelegate` (`src/Reporter/Platforms/iOS/SceneDelegate.cs`) | Klasse | `[Register("SceneDelegate")]`, abgeleitet von `MauiUISceneDelegate` (Namespace `Microsoft.Maui`), leerer Rumpf — die Window-Einrichtung übernimmt .NET MAUI. PolyForm-Lizenzheader wie üblich; `using Foundation; using Microsoft.Maui;`; file-scoped `namespace Reporter;` |
| `Reporter.SceneDelegate` (`src/Reporter/Platforms/MacCatalyst/SceneDelegate.cs`) | Klasse | Identische Implementierung für MacCatalyst (gleicher Registrierungsname `SceneDelegate`, wird pro Plattform-Assembly kompiliert — kein Namenskonflikt, da die TFMs getrennt bauen) |

## Änderungen an bestehenden Klassen

Keine C#-Klassen werden geändert. Geändert werden zwei `Info.plist`-Dateien und eine CI-Composite-Action:

### `src/Reporter/Platforms/iOS/Info.plist` (plist-Konfiguration)

- **Neuer Schlüssel:** `UIApplicationSceneManifest` (eigenes `<dict>` im Root-`<dict>`, z. B. nach `CFBundleLocalizations`): `UIApplicationSupportsMultipleScenes` = `false`; `UISceneConfigurations` → `UIWindowSceneSessionRoleApplication` → `<array>` mit einem `<dict>`: `UISceneConfigurationName` = `__MAUI_DEFAULT_SCENE_CONFIGURATION__` (exakt, MAUI-Erwartung), `UISceneDelegateClassName` = `SceneDelegate` (muss zu `[Register("SceneDelegate")]` passen). Tab-Einrückung der Datei beibehalten.
- **Nicht setzen:** `UISceneStoryboardFile` (MAUI erzeugt das Window im Code); `ValidateSceneManifest` bleibt unverändert (nicht auf `false`).

### `src/Reporter/Platforms/MacCatalyst/Info.plist` (plist-Konfiguration)

- **Neuer Schlüssel:** identisches `UIApplicationSceneManifest` wie iOS (gleiche Struktur, gleiche Werte).

### `src/Reporter/Platforms/iOS/AppDelegate.cs` (Klasse)

- **Geprüft — keine Änderung:** `FinishedLaunching` läuft unter Scene-Lifecycle weiterhin (verifiziert an `MauiUIApplicationDelegate`: bei `HasSceneManifest()` wird nur `CreatePlatformWindow` übersprungen); `UNUserNotificationCenter`-Delegate und die `DispatchAsync`-verlagerte `BGTaskScheduler`-Registrierung sind App-weit korrekt. `Program.cs` und `MauiProgram` bleiben unverändert; kein `ConfigureLifecycleEvents` erforderlich.

### `.github/actions/package-android/action.yml` (CI-Composite-Action)

- **Geänderter Schritt `Publish (android)`:** `dotnet publish`-Kommando um `-p:PublishReadyToRun=false` ergänzen; erklärenden Kommentar hinzufügen (seit .NET 11 defaultet der CoreCLR-Android-Runtime `PublishReadyToRun` bei `Release` auf `true`; der RID-lose Restore löst kein R2R-Runtime-Pack auf → NETSDK1094; analog `ee41281` im Windows-Pfad).
- Restore- und Collect-Schritte bleiben unverändert.

### `.github/workflows/staging-ci.yml` / `.github/workflows/release.yml`

- **Keine Änderung:** Der Fix liegt in Plattformdateien und der wiederverwendeten Action; beide Workflows profitieren implizit.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine neuen Eingabe-Validierungen (kein App-Input betroffen). Die korrekte Umsetzung wird durch Build-Zeit-Invarianten abgesichert:

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `UIApplicationSceneManifest` (iOS + MacCatalyst) | Exakte MAUI-Struktur: `__MAUI_DEFAULT_SCENE_CONFIGURATION__`, `UISceneDelegateClassName` `SceneDelegate` identisch zu `[Register("SceneDelegate")]`, `UIApplicationSupportsMultipleScenes` `false` | `error MT7199` beim `package-ios`-Publish (Build-Check bleibt aktiv) bzw. Startabsturz bei Name-Mismatch |
| `PublishReadyToRun` (Android-Publish) | Muss beim Publish explizit `false` sein | `error NETSDK1094` im `package-android`-Job |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `-p:PublishReadyToRun=false` im Schritt `Publish (android)` von `.github/actions/package-android/action.yml` | MSBuild-CLI-Parameter | `false` (explizit gesetzt) | Deaktiviert den R2R-Default des CoreCLR-Android-Workloads (`Microsoft.Android.Sdk.CoreCLR.targets` Z. 19–25); verhindert die Forderung nach einem nicht restaurierten `Microsoft.NETCore.App.Runtime.*`-Pack |

Keine `appsettings`- oder `Reporter.csproj`-Änderungen. `ValidateSceneManifest` wird **nicht** gesetzt (Build-Check bleibt als Guard aktiv).

## Seiteneffekte und Risiken

- **Android-APK ohne R2R-Vorkompilierung:** potenziell minimal langsamerer Kaltstart; entspricht dem .NET-10-Verhalten (MonoVM ohne R2R), kein Funktionsverlust.
- **`release.yml` wird implizit mitgefixt:** gleiche Composite-Actions — positiver Nebeneffekt, kein eigener Eingriff.
- **Manifest/Registrierungs-Mismatch:** Wäre `UISceneDelegateClassName` ≠ `[Register(...)]`, stürzte die App bei Start ab — Namen sind exakt vorgegeben (`SceneDelegate`); zusätzlich prüft MT7199 das Manifest zur Build-Zeit.
- **MacCatalyst-Ergänzung ist nicht build-verifizierbar** (`net11.0-maccatalyst` nicht in `TargetFrameworks`): trivialer, von der MS-Doku übernommener Code — Restrisiko Flüchtigkeitsfehler → Sichtprüfung bei Review.
- **iOS-`SceneDelegate.cs` wird lokal nicht kompiliert** (`net11.0-ios` benötigt macOS/Xcode — Windows-Build und `Run-StaticChecks.ps1` erfassen die Datei nicht): Syntax-/Namenskorrektheit nur per Review und dem CI-`package-ios`-Lauf nachweisbar. Der Lizenzheader-Check erfasst die Datei dagegen textuell — PolyForm-Header Pflicht.
- **Verifikation erst nach Merge auf `staging`:** `package-*`/`prerelease` sind nicht PR- oder lokal ausführbar (`pr-staging-ci.yml` enthält keine Paketierungs-Jobs). Da ein `fix:`-Commit eine Patch-Version auslöst, laufen die Jobs direkt beim Merge (`version.changed == 'true'`).
- **Preexisting E2E-Fehlschläge (18×, FlaUI-Windows-Suite):** stehen in keinem Bezug zum betroffenen Bereich, laufen in keinem Workflow — unverändert als preexisting dokumentiert.

## Umsetzungsreihenfolge

1. **iOS: `SceneDelegate.cs` anlegen**
   - Voraussetzungen: `MauiUISceneDelegate` im installierten iOS-Workload `27.2.11039` / MAUI-Framework vorhanden (.NET-11-Templates und MS-Migrationsdoku verwenden die Klasse — verfügbar).
   - Beschreibung: `src/Reporter/Platforms/iOS/SceneDelegate.cs` mit PolyForm-Header, `using Foundation; using Microsoft.Maui;`, `[Register("SceneDelegate")]`, `public class SceneDelegate : MauiUISceneDelegate` (leerer Rumpf).

2. **iOS: `UIApplicationSceneManifest` in `Info.plist` ergänzen**
   - Voraussetzungen: Schritt 1 (Registrierungsname `SceneDelegate` muss zum Manifest passen).
   - Beschreibung: `UIApplicationSceneManifest`-`<dict>` im Root-`<dict>` von `src/Reporter/Platforms/iOS/Info.plist` ergänzen (Struktur/Werte gemäß oben; Tab-Einrückung beibehalten; kein `UISceneStoryboardFile`).

3. **MacCatalyst: `SceneDelegate.cs` + `UIApplicationSceneManifest` ergänzen**
   - Voraussetzungen: Schritte 1–2 (identische Struktur übernehmen).
   - Beschreibung: `src/Reporter/Platforms/MacCatalyst/SceneDelegate.cs` analog anlegen; identisches Manifest in `src/Reporter/Platforms/MacCatalyst/Info.plist` einfügen. Kein `TargetFrameworks`-Eingriff.

4. **Android: `PublishReadyToRun=false` in `package-android`-Action**
   - Voraussetzungen: Keine (unabhängig von 1–3).
   - Beschreibung: In `.github/actions/package-android/action.yml` den `dotnet publish`-Aufruf im Schritt `Publish (android)` um `-p:PublishReadyToRun=false` erweitern und den Grund-Kommentar ergänzen (CoreCLR-Default seit .NET 11, Präzedenz `ee41281`).

5. **Lokale statische Checks ausführen**
   - Voraussetzungen: Schritte 1–4 abgeschlossen.
   - Beschreibung: `.\scripts\Run-StaticChecks.ps1` — muss Exit-Code 0 ohne Findings liefern (Projektregel; der Format-/Lizenzheader-Check erfasst die neuen `.cs`-Dateien textuell).

6. **Merge und Staging-Verifikation**
   - Voraussetzungen: Schritte 1–5; Merge auf `staging`.
   - Beschreibung: `staging-ci.yml`-Lauf beobachten (`gh pr checks`/`gh run watch` bzw. `gh run view` nach Push): `package-android` ohne NETSDK1094, `package-ios` ohne MT7199 inkl. Gate-Log `DTSDKName=iphoneos≥27`, `prerelease` erzeugt RC-Tag + Release mit vier Assets.

## Tests

### Neue Tests

Keine. Der betroffene Bereich (Plattform-Lifecycle, CI-Packaging) enthält keinen über Unit-/Integrationstests prüfbaren App-Code; die `Info.plist`-Invarianten werden durch den iOS-Build-Check MT7199 selbst erzwungen, der Android-Fix durch den CI-Job.

### Betroffene bestehende Tests

Keine.

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich: Die Anforderung berührt keinen über die UI erreichbaren Benutzerfluss — keine neue oder geänderte MAUI-Page, kein Control, keine Benutzerinteraktion. Die Windows-FlaUI-Suite (`src/Reporter.E2ETests`) ist durch die Plattformdateien `Platforms/iOS`, `Platforms/MacCatalyst` und die CI-Action nicht betroffen; ihre 18 preexisting Fehlschläge bleiben unverändert außerhalb des Scopes.

Der primäre Funktionsnachweis ist der **reale `staging-ci`-Lauf** (siehe Akzeptanzszenarien 1–3): Die fehlschlagenden Pfade (`package-ios` auf macOS/`xcode-27` mit Codesigning, `package-android`-Publish) sind ausschließlich CI-seitig und unter Windows nicht reproduzierbar — ein E2E-/Unit-Test kann die Kette GitHub-Runner → Xcode 27.2 → Workload → IPA/APK → `gh release create` nicht ersetzen. Der App-Start unter iOS 27 (Szene-Lifecycle wirksam) ist zusätzlich als manueller Nachweis der RC-IPA geplant (Akzeptanzszenario 5).

Bestehende E2E-Tests angepasst: Keine.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: vollständiger Pre-Release-Lauf | Merge des `fix:`-Commits auf `staging` | Fehlerzustand von Run `45486899366` behoben; kein RC-Tag für die neue Version vorhanden; `vars.IOS_SIGNING_ENABLED == 'true'` | `static-checks`/`build-and-test` grün → `version` (semantic-release erzeugt Patch-Version, `changed=true`) → `package-windows`, `package-android`, `package-ios` grün → `prerelease` | GitHub-Pre-Release `v<x.y.z>-rc.<n>` mit `release-win-x64.zip`, `release-android.apk`, `release-ios.ipa`, `update.json`; Workflow-Status `success` | GitHub-Actions-Lauf `staging-ci.yml` + Release-Eintrag (reale Abnahme nach Merge) |
| 2 | iOS-Paketierung ohne MT7199 | Job `package-ios` (aus Szenario 1) | `UIApplicationSceneManifest` + `SceneDelegate.cs` vorhanden; Runner `xcode-27`, Xcode 27.2, Workload `27.2.11039` | `Select Xcode` → Workload-Install → `Restore -r ios-arm64` → `Publish (ios)` → `Collect iOS package` → `Verify built SDK version` | Publish endet ohne `error MT7199`; `release-ios.ipa` erzeugt; Gate-Log zeigt `DTSDKName=iphoneos` ≥ 27; TestFlight-Upload bei gesetztem `api-key-p8` | Job-Log `package-ios` |
| 3 | Android-Paketierung ohne NETSDK1094 | Job `package-android` (aus Szenario 1) | RID-loser `dotnet restore`; `PublishReadyToRun` nicht im Repo gesetzt | `Publish (android)` mit `-p:PublishReadyToRun=false` → `Collect Android package` | Publish endet ohne `error NETSDK1094`; `release-android.apk` (bevorzugt `*-Signed.apk`) gesammelt | Job-Log `package-android` |
| 4 | Guard bleibt aktiv: fehlendes Manifest | (Hypothetisch) Publish ohne `UIApplicationSceneManifest` | Manifest/Registrierung fehlt oder `UISceneDelegateClassName` passt nicht zu `[Register]` | `Publish (ios)` | `error MT7199` bzw. Startabsturz — der Build-Check bleibt als Schutz aktiv (`ValidateSceneManifest` wird nicht deaktiviert) | Dokumentiertes Verhalten, kein eigener Testlauf |
| 5 | iOS-App-Start unter iOS 27 | Anwender startet die installierte RC-App | RC-IPA aus Szenario 1 auf iOS-27-Gerät/Simulator (z. B. via TestFlight oder `scripts/iOS-Deployment.ps1`) installiert | App starten | Kein Absturz (`UIScene life cycle is required` entfällt); App zeigt Feedliste | Optionaler manueller Nachweis — Empfehlung vor nächstem `main`-Release; nicht automatisierbar in dieser Änderung |

## Offene Punkte

> Alle Punkte am 2026-11-25 entschieden — Umsetzung erfolgt nach dem empfohlenen Vorschlag (requirement-konform; Ausschluss 1 ist direkt aus der Anforderung abgeleitet).

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
| 1 | **entschieden:** Praktische Verifikation „App startet unter iOS 27 ohne Absturz": genügt der fehlerfreie Build (kein MT7199 + korrektes Manifest + SDK-Gate), oder ist ein Simulator-/Gerätestart unter iOS 27 vorgesehen? | Build-Nachweis ist für diese Änderung ausreichend (die Manifest-Struktur ist der von Microsoft dokumentierte Migrationspfad; `ValidateSceneManifest` bleibt aktiv). Empfohlen: zusätzlich nach dem grünen Staging-Lauf die RC-IPA manuell unter iOS 27 starten (TestFlight-Install oder `scripts/iOS-Deployment.ps1`), bevor der nächste `main`-Release ansteht — als Folgeaufgabe, nicht als Blocker dieses Fixes |
| 2 | **entschieden:** Gilt die Reparatur für `release.yml` implizit als abgedeckt, oder ist eine eigene Verifikation gewünscht, bevor der nächste `main`-Release ansteht? | Implizit abgedeckt — `release.yml` nutzt identisch dieselben Composite-Actions (`package-ios`, `package-android`); der grüne Staging-Lauf ist der verbindliche Nachweis. Kein separater Release-Lauf einplanen |
