<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung — Issue #217 „Pre-Release reparieren"

## Fachliche Zusammenfassung

Der `Pre-Release`-Workflow (`.github/workflows/staging-ci.yml`) scheitert seit dem Merge des .NET-11-/Xcode-27-Stands in den Paketierungs-Jobs `package-ios` und `package-android`, sodass der Folgejob `prerelease` übersprungen wird und kein RC-Tag bzw. keine RC-Pakete entstehen. Die iOS-Paketierung bricht mit `error MT7199` ab, weil `src/Reporter/Platforms/iOS/Info.plist` kein `UIApplicationSceneManifest` enthält — Builds gegen das iOS-27-SDK müssen den UIKit-Scene-Lifecycle verwenden. Die Android-Paketierung bricht mit `error NETSDK1094` ab, weil `PublishReadyToRun` beim Publish wirkt, ohne dass Restore das R2R-Runtime-Package für den Ziel-RID aufgelöst hat. Zu reparieren sind der Scene-Lifecycle der App (iOS, ggf. MacCatalyst) und die konsistente Behandlung von `PublishReadyToRun` im Android-Restore/-Publish-Pfad.

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein `Pre-Release`-Lauf des Workflows `staging-ci.yml` auf Push nach `staging` mit Versionsänderung (`version.outputs.changed == 'true'`) — umfasst die Paketierungs-Jobs `package-windows`, `package-android`, `package-ios` und den Abschlussjob `prerelease` (GitHub-Pre-Release mit RC-Tag `v<version>-rc.<n>` sowie Assets `release-win-x64.zip`, `release-android.apk`, `release-ios.ipa`, `update.json`). Letzter fehlgeschlagener Lauf: GitHub Actions Run `45486899366` (2026-11-24).
- **Auslösende Ereignisse:** Push/Merge auf den `staging`-Branch, sobald `semantic-release` eine neue Version ermittelt. Externer Trigger dieser Anforderung: der fehlgeschlagene Workflow-Lauf nach dem Merge der .NET-11-Migration bzw. der Xcode-27-Umstellung (Issue #210, ITMS-90725).
- **Erwartete Wirkung:** Der `Pre-Release`-Lauf auf `staging` endet mit `success` einschließlich Job `prerelease`: RC-Tag und GitHub-Pre-Release mit iOS-, Android- und Windows-Paketen werden erzeugt und als Artefakte bereitgestellt (iOS zusätzlich TestFlight-fähig, sofern die Signing-/API-Secrets gesetzt sind). Die ausgelieferte iOS-App startet unter iOS 27 ohne Absturz, weil der Scene-Lifecycle wirksam implementiert ist.
- **Umgang mit fehlenden Objekten:**
  - `UIApplicationSceneManifest` in `src/Reporter/Platforms/iOS/Info.plist` — **erzeugen** (`UISceneConfigurations` → `UIWindowSceneSessionRoleApplication` mit `UISceneConfigurationName` `__MAUI_DEFAULT_SCENE_CONFIGURATION__` und `UISceneDelegateClassName`, `UIApplicationSupportsMultipleScenes` `false`).
  - `SceneDelegate`-Implementierung für iOS (`Platforms/iOS/SceneDelegate.cs`, von `MauiUISceneDelegate` abgeleitet, `[Register("SceneDelegate")]`) — **erzeugen**; `AppDelegate` (`MauiUIApplicationDelegate`) bleibt parallel bestehen.
  - R2R-Runtime-Package für den Android-Ziel-RID — entweder **erzeugen/auflösen** (Restore mit `-p:PublishReadyToRun=true` und passendem RID) oder den R2R-Schritt **verwerfen** (`-p:PublishReadyToRun=false` beim RC-Publish, analog zum Windows-Pfad).
  - `UIApplicationSceneManifest` für `Platforms/MacCatalyst/Info.plist` (inkl. ggf. `Platforms/MacCatalyst/SceneDelegate.cs`) — prüfen und ggf. **erzeugen**; Mac Catalyst unterliegt derselben SDK-27-Anforderung.
- **Scope-Ausschlüsse:**
  - Ein reines `ValidateSceneManifest=false` ist ausdrücklich **keine** Lösung — es deaktiviert nur den Build-Check (MT7199); die App würde unter iOS 27 weiterhin beim Start abstürzen.
  - Keine Änderung an `package-windows` (`.github/actions/build-and-package` läuft bereits erfolgreich mit `-p:PublishReadyToRun=false`).
  - Keine funktionalen Änderungen an Features, UI oder Datenmodell — es handelt sich um eine Plattform-Lifecycle- und CI-/Packaging-Korrektur.
  - Keine Anhebung von `SupportedOSPlatformVersion` (iOS `15.0`, MacCatalyst `17.0`, Android `24.0` in `src/Reporter/Reporter.csproj`): Die Scene-Pflicht hängt am Build-SDK (Xcode 27 / iOS SDK ≥ 27), nicht am Deployment-Target.
  - `release.yml` verwendet dieselben Composite-Actions (`package-ios`, `package-android`) und profitiert implizit von der Korrektur; die verbindliche Verifikation erfolgt jedoch über den `staging`-Lauf — ein separater Release-Lauf ist nicht Teil dieser Anforderung.

## Betroffene Klassen und Komponenten

- `src/Reporter/Platforms/iOS/Info.plist` — `UIApplicationSceneManifest`-Dictionary ergänzen (eigenes `<dict>` im Root-`<dict>`; `UISceneDelegateClassName` muss zur C#-Registrierung passen).
- `src/Reporter/Platforms/iOS/SceneDelegate.cs` — **neu**: `[Register("SceneDelegate")]`, Basisklasse `MauiUISceneDelegate` (Namespace `Reporter`, PolyForm-Lizenzheader wie üblich). Die eigentliche Window-Einrichtung übernimmt .NET MAUI.
- `src/Reporter/Platforms/iOS/AppDelegate.cs` — bleibt als `MauiUIApplicationDelegate` bestehen; zu **prüfen** ist, ob die in `FinishedLaunching` verankerte Initialisierung (`UNUserNotificationCenter.Current.Delegate = _notificationDelegate`, asynchrone `BGTaskScheduler`-Registrierung via `DispatchQueue.MainQueue.DispatchAsync`) im Scene-Lifecycle unverändert korrekt läuft oder teilweise in den Scene-Lifecycle bzw. MAUI-Lifecycle-Events verlagert werden muss.
- `src/Reporter/Platforms/MacCatalyst/Info.plist` und ggf. `src/Reporter/Platforms/MacCatalyst/SceneDelegate.cs` — dieselbe Ergänzung wie iOS. Hinweis: `net11.0-maccatalyst` ist aktuell **nicht** in `TargetFrameworks` der `Reporter.csproj` enthalten — kein CI-Job baut die Plattform derzeit; dennoch unterliegt das Platfform-Verzeichnis derselben SDK-27-Anforderung, sobald das TFM aktiviert würde.
- `.github/actions/package-android/action.yml` — Restore-Schritt (`dotnet restore Reporter.sln`, ohne RID) und Publish-Schritt (`dotnet publish ... --framework net11.0-android --no-restore`): `PublishReadyToRun` konsistent ausrichten (Restore mit `-p:PublishReadyToRun=true` + RID oder `-p:PublishReadyToRun=false` beim Publish).
- `.github/workflows/staging-ci.yml` — voraussichtlich keine direkte Änderung nötig (Fix liegt in Action/Projektdateien); betroffen sind die Job-Ergebnisse `package-ios`, `package-android`, `prerelease`.
- `.github/workflows/release.yml` — indirekt betroffen (nutzt dieselben Actions); keine eigene Änderung erwartet.
- **Datenmodellklassen / Logikklassen / Interfaces / Enums:** keine betroffen bzw. neu erforderlich.
- **UI-Komponenten / Controller:** keine betroffen (kein neuer/veränderter MAUI-`Page`- oder Control-Code).
- **Tests:** kein Unit-Test-Bedarf; Verifikation über den grünen CI-Lauf, die erzeugten `.ipa`/`.apk`-Artefakte sowie den Launch-Test der App unter iOS 27 (Simulator/Gerät bzw. `scripts/iOS-Deployment.ps1`).

## Implementierungsansatz

- **iOS (MT7199):** Microsoft-Migrationspfad für den UIKit-Scene-Lifecycle (.NET-MAUI-Dokumentation): neue `SceneDelegate`-Klasse parallel zum bestehenden `AppDelegate` plus `UIApplicationSceneManifest` in `Platforms/iOS/Info.plist` mit exakt dem Konfigurationsnamen `__MAUI_DEFAULT_SCENE_CONFIGURATION__`. Der `AppDelegate` bleibt für `CreateMauiApp` und die bestehenden Launch-Callbacks zuständig — der Scene-Delegate arbeitet daneben, nicht stattdessen.
- **Android (NETSDK1094):** Der Fehler entsteht, weil `dotnet publish` `PublishReadyToRun` anwendet, ohne dass der vorangegangene `dotnet restore Reporter.sln` das R2R-Runtime-Package für den Ziel-RID aufgelöst hat. Zwei zulässige Varianten: (a) Restore um RID/`-p:PublishReadyToRun=true` erweitern, damit die Assets-Datei das Runtime-Pack enthält; (b) `-p:PublishReadyToRun=false` beim Publish übergeben. **Annahme:** Variante (b) ist projektkonsistent — der Windows-Pfad setzt bereits explizit `PublishReadyToRun=false` (`.github/actions/build-and-package/action.yml`, Zeile 49); ReadyToRun bringt für die Android-APK ohnehin keinen dokumentierten Nutzen im Projekt.
- **MacCatalyst:** Prüfen, ob das Scene-Manifest präventiv ergänzt wird (gleiche `Info.plist`-Struktur wie iOS). Da `net11.0-maccatalyst` nicht gebaut wird, entsteht kein Build-Fehler — die Entscheidung ist in der Planung zu treffen und ggf. als offene Frage zu dokumentieren.
- **Abhängigkeiten:** `package-ios` läuft nur mit `vars.IOS_SIGNING_ENABLED == 'true'` auf dem `xcode-27`-Runner (Xcode 27.2 via `DEVELOPER_DIR`, Workload-Pin `ios-workload-version: 27.2.11039`) — der MT7199-Fehler beweist, dass dieser Pfad aktiv ist. `prerelease` hängt via `needs` an allen drei Paketierungs-Jobs. Keine neuen Klassen außerhalb der Platform-Ordner, keine Interfaces, keine Enums.
- **Erweiterungspunkte:** ausschließlich `Platforms/*/Info.plist`, neue `SceneDelegate.cs` und `.github/actions/package-android/action.yml`; kein Eingriff in `MauiProgram`, App-Code oder Workflows erwartet.

## Konfiguration

Keine Laufzeit- oder Anwendungskonfiguration erforderlich. Auf Build-Ebene betroffen: MSBuild-Property `PublishReadyToRun` (Kommandozeilen-Parameter in der `package-android`-Action oder Property in `Reporter.csproj` — Entscheidung in der Planung). `ValidateSceneManifest` darf **nicht** auf `false` gesetzt werden.

## Offene Fragen

1. Woher stammt `PublishReadyToRun=true` beim Android-Publish — SDK-Default für den RID-bezogenen `net11.0-android`-Publish oder ein transitiv gesetztes Property? Die Antwort entscheidet, ob der Fix im Restore-Schritt (Variante a) oder als explizites `=false` beim Publish (Variante b) anzusetzen ist; das Fehlerbild legt (b) nahe, ist aber in der Bestandsaufnahme zu verifizieren.
2. Soll der MacCatalyst-Pfad (`Info.plist` + `SceneDelegate.cs`) präventiv ergänzt werden, obwohl `net11.0-maccatalyst` derzeit nicht gebaut wird und kein Job die Plattform validiert?
3. Wie wird das Akzeptanzkriterium „App startet unter iOS 27 ohne Absturz" praktisch verifiziert — genügt der fehlerfreie Build ohne MT7199 plus korrektem Manifest, oder ist ein Simulator-/Gerätestart unter iOS 27 (lokal via `scripts/iOS-Deployment.ps1` oder auf dem Runner) vorgesehen?
4. Greifen die im `AppDelegate.FinishedLaunching` verankerten Initialisierungen (`BGTaskScheduler`-Registrierung, `UNUserNotificationCenter`-Delegate) im Scene-Lifecycle unverändert, oder müssen Teile davon in den Scene-Lifecycle bzw. in MAUI-`ConfigureLifecycleEvents` verlagert werden?
5. Gilt die Reparatur für `release.yml` implizit als abgedeckt (identische Actions), oder ist dort eine eigene Verifikation gewünscht, bevor der nächste `main`-Release ansteht?
