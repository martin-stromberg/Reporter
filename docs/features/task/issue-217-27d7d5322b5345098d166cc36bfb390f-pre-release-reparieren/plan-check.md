<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| `Pre-Release`-Lauf endet mit `success` inkl. `prerelease` (RC-Tag `v<version>-rc.<n>`, GitHub-Pre-Release mit `release-win-x64.zip`, `release-android.apk`, `release-ios.ipa`, `update.json`) | Schritte 1–4 beheben beide Blocker; Schritt 6 „Merge und Staging-Verifikation"; Programmablauf „Pre-Release-Job-Kette" beschreibt die `needs`-Kette und `gh release create` | Akzeptanzszenario 1 (Happy Path, komplette Kette bis Release-Eintrag), Szenarien 2–3 (Teilnachweise pro Job) | Abgedeckt |
| `error MT7199` behoben: `UIApplicationSceneManifest` in `Platforms/iOS/Info.plist` erzeugen (exakte MAUI-Struktur: `__MAUI_DEFAULT_SCENE_CONFIGURATION__`, `UISceneDelegateClassName` `SceneDelegate`, `UIApplicationSupportsMultipleScenes` `false`) | Schritt 2 + Abschnitt „Änderungen an bestehenden Klassen → iOS/Info.plist" mit exakter Schlüsselstruktur | Akzeptanzszenario 2 (`package-ios` ohne MT7199, IPA erzeugt, SDK-Gate `DTSDKName=iphoneos≥27`); Szenario 4 (Guard bleibt aktiv) | Abgedeckt |
| `SceneDelegate.cs` für iOS erzeugen (`[Register("SceneDelegate")]`, `MauiUISceneDelegate`, `AppDelegate` bleibt parallel) | Schritt 1 + Tabelle „Neue Klassen" (Namespace `Reporter`, PolyForm-Header, leerer Rumpf) | Akzeptanzszenario 2 (Build-Nachweis) + Szenario 5 (Start unter iOS 27, manuell) | Abgedeckt |
| `error NETSDK1094` behoben: `PublishReadyToRun` konsistent im Android-Restore/-Publish-Pfad | Schritt 4: Variante (b) `-p:PublishReadyToRun=false` im Publish-Schritt der `package-android`-Action; Designentscheidung verifiziert an `Microsoft.Android.Sdk.CoreCLR.targets` 37.2.31 (Z. 19–25), Präzedenz `ee41281` | Akzeptanzszenario 3 (Publish ohne NETSDK1094, `release-android.apk` gesammelt) | Abgedeckt |
| MacCatalyst-Pfad (`Info.plist` + ggf. `SceneDelegate.cs`) geprüft und ggf. erzeugt | Schritt 3: beide Dateien präventiv ergänzen; Designentscheidung begründet (MS-Migrationsdoku verlangt beide Plattformordner; TFM derzeit inaktiv → Sichtprüfung als Restrisiko benannt) | Sichtprüfung im Review; Restrisiko „nicht build-verifizierbar" explizit dokumentiert | Abgedeckt |
| `AppDelegate.FinishedLaunching`-Initialisierungen (`UNUserNotificationCenter`-Delegate, `DispatchAsync`-`BGTaskScheduler`-Registrierung) unter Scene-Lifecycle geprüft | Designentscheidung „Keine Verlagerung", verifiziert an `MauiUIApplicationDelegate` (`FinishedLaunching` läuft weiter; nur `CreatePlatformWindow` wird bei `HasSceneManifest()` übersprungen); Abschnitt „Geprüft — keine Änderung" | Indirekt über Szenario 5 (App-Start + bestehende Funktionen wie Background-Refresh bleiben wirksam) | Abgedeckt |
| iOS-App startet unter iOS 27 ohne Absturz (Scene-Lifecycle wirksam) | Richtiger Scene-Lifecycle statt Build-Check-Deaktivierung (Ausschluss 1); `UISceneDelegateClassName`/`[Register]`-Konsistenz als Validierungsregel | Akzeptanzszenario 5 (manueller Start der RC-IPA via TestFlight/`scripts/iOS-Deployment.ps1`); als optionale Folgeaufgabe entschieden (Offener Punkt 1), nicht automatisierbar in dieser Änderung | Abgedeckt |
| iOS-Paket TestFlight-fähig bei gesetzten Signing-/API-Secrets | Unverändert — `package-ios`-Action inkl. TestFlight-Upload-Schritt bleibt bestehen | Akzeptanzszenario 2 nennt TestFlight-Upload bei gesetztem `api-key-p8` | Abgedeckt |
| Scope-Ausschluss: kein `ValidateSceneManifest=false` | Ausschluss 1 (bestätigt); Validierungsregel „`ValidateSceneManifest` wird nicht gesetzt"; Abschnitt „Nicht setzen" bei `Info.plist` | Szenario 4 (Guard bleibt aktiv, MT7199 greift bei fehlendem Manifest weiter) | Abgedeckt |
| Scope-Ausschluss: keine Änderung an `package-windows`/`build-and-package`, keine Feature-/UI-/Datenmodell-Änderungen, keine `SupportedOSPlatformVersion`-Anhebung, kein separater `release.yml`-Verifikationslauf | Ausschlüsse 2–5 (alle `bestätigt`); „Änderungen an bestehenden Klassen" beschränkt sich auf Plist-/Action-Dateien; `staging-ci.yml`/`release.yml` explizit „Keine Änderung" | Schritt 5 (`Run-StaticChecks.ps1` ohne Findings) + Szenario 1 (Windows-Pfad läuft im selben Lauf weiter grün) | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| `Pre-Release`-Job-Kette (Merge auf `staging` → `package-*` → `prerelease` → GitHub-Pre-Release) | Kein automatisierter Test möglich: Kette GitHub-Runner → Xcode 27.2 → Workload → IPA/APK → `gh release create` ist ausschließlich CI-seitig und unter Windows nicht reproduzierbar. Primärer Funktionsnachweis = realer `staging-ci`-Lauf (Akzeptanzszenarien 1–3, Schritt 6 mit `gh run watch`/`gh run view`) | Nicht erforderlich mit Begründung — Plan begründet nachvollziehbar und definiert die reale Abnahme als Nachweis |
| iOS-App-Start unter iOS 27 (Anwender startet RC-App → Feedliste sichtbar, kein Absturz) | Kein automatisierbarer iOS-E2E-Test vorhanden/möglich (`Reporter.E2ETests` ist Windows-only FlaUI; kein macOS-Gerätepool). Geplant: manueller Nachweis der RC-IPA unter iOS 27 (TestFlight-Install oder `scripts/iOS-Deployment.ps1`) als empfohlene Folgeaufgabe vor nächstem `main`-Release | Nicht erforderlich mit Begründung — kein neuer/geänderter UI-Fluss; manuelle Verifikation explizit eingeplant (Szenario 5, Offener Punkt 1 entschieden) |
| Bestehende Windows-FlaUI-Suite (`src/Reporter.E2ETests`) | Keine Anpassung: Plattformdateien `Platforms/iOS`, `Platforms/MacCatalyst` und CI-Action berühren die Windows-App nicht; 18 preexisting Fehlschläge (`TimeoutException name 'Aktionen'`/`'Ja'`) dokumentiert und unverändert außerhalb des Scopes | Nicht erforderlich mit Begründung |

## Hinweise

- **Verifikation erst nach Merge:** Die wirksame Abnahme (`package-android` ohne NETSDK1094, `package-ios` ohne MT7199, `prerelease` mit vier Assets) ist erst auf `staging` prüfbar — PR-Checks können sie nicht vorwegnehmen. Da ein `fix:`-Commit semantic-release auslöst (`version.changed == 'true'`), laufen die Paketierungs-Jobs direkt beim Merge. Bei Restfehlschlag gilt fix-forward: `gh run view --job <id> --log-failed` diagnostizieren und nachpatchen (impliziter Wiederaufnahmeweg, könnte im Plan noch explizit benannt werden).
- **MacCatalyst-Restrisiko:** Dateien werden mangels TFM nicht kompiliert — die eingeplante Sichtprüfung im Review ist als einzige Absicherung bewusst gewählt und benannt.
- **iOS-`SceneDelegate.cs` lokal nicht kompilierbar** (kein macOS/Xcode unter Windows): Korrektheit hängt an Review + CI-`package-ios`-Lauf; Lizenzheader-Check erfasst die Datei textuell — PolyForm-Header ist im Plan Pflichtbestandteil.
- **Konsistenz `[Register("SceneDelegate")]` ↔ `UISceneDelegateClassName`:** als Validierungsregel + Risiko bereits benannt; beim Review besonders auf exakte Schreibweise achten.
