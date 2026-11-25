<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung — Issue #204 „Nachricht von Apple" (ITMS-90725)

## Fachliche Zusammenfassung

Apple hat für die zuletzt an App Store Connect gelieferte .ipa (Feed Reporter, Version 0.8.0, Build 331) die Warnung ITMS-90725 ausgesprochen: Das Binary wurde mit dem iOS-26.5-SDK erstellt. Ab April 2027 müssen iOS-Uploads mit dem iOS-27-SDK oder neuer (Xcode 27 oder neuer) gebaut sein. Die Build-Infrastruktur (GitHub-Actions-iOS-Pipeline und dokumentierter lokaler Deployment-Pfad) ist so zu korrigieren, dass das nächste ausgelieferte iOS-Binary die Apple-Anforderung erfüllt und der Upload ohne diese Warnung akzeptiert wird.

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein iOS-Release-Binary (`.ipa` für `net11.0-ios`, RID `ios-arm64`), das aus der Release-Pipeline (`release.yml`), der Staging-RC-Pipeline (`staging-ci.yml`) oder dem manuellen Skript-Pfad (`scripts/iOS-Deployment.ps1`, Aktionen `store`/`upload`) an App Store Connect / TestFlight geliefert wird.
- **Auslösende Ereignisse:**
  - Externer Trigger: Apples ITMS-90725-Hinweis zur Lieferung von Build 331 (Auftraggeber: „Erledige diese Aufgabe").
  - Konkreter Build-Auslöser im System: Push auf `main` bzw. Tag `v*.*.*` (Release), Push auf `staging` mit Versionsänderung (RC) sowie manueller Aufruf von `iOS-Deployment.ps1`.
- **Erwartete Wirkung:** Das nächste an App Store Connect hochgeladene Binary ist mit iOS SDK ≥ 27 (Xcode ≥ 27, Release-Version — Beta-Xcode-Builds lehnt der Store ab) gebaut; die Verarbeitung durch App Store Connect meldet kein ITMS-90725-Problem mehr. Die SDK-Version des gebauten Binary ist im Build-Prozess verifizierbar (z. B. über `DTSDKName`/`DTPlatformVersion` in der `Info.plist` der `.ipa`).
- **Umgang mit fehlenden Objekten:**
  - Geeignetes Release-Xcode ≥ 27 auf dem `macos-latest`-Runner: prüfen, ob die Runner-Default-Xcode (`xcode-select`) bereits ≥ 27 ist; falls nicht oder nicht reproduzierbar, explizit auswählen (z. B. `xcode-select`-Schritt bzw. Runner-Image-Pin) — **erzeugen/aktivieren**.
  - Passendes `Microsoft.iOS.Sdk`-Pack ≥ 27.x: der Workload-Pin existiert bereits — `ios-workload-version` Default `27.2.11039` in `.github/actions/package-ios/action.yml` (mit der .NET-11-Migration, Commit `9a636b0`, eingeführt) — **wiederverwenden**, ggf. Versionsstand gegen Apple-Anforderung und Runner-Xcode verifizieren.
  - Falls ein veralteter Build-Pfad (lokaler Mac mit Xcode 26.x) verwendet wird: Mindest-Xcode-Version dokumentieren bzw. im Skript prüfen — **aktivieren**.
- **Scope-Ausschlüsse:**
  - Keine Änderung am App-Code, an Features oder an der UI — es handelt sich um eine reine Build-/CI-Korrektur.
  - Keine Anhebung des Deployment-Targets: `SupportedOSPlatformVersion` iOS 15.0 in `src/Reporter/Reporter.csproj` bleibt unverändert (Build-SDK-Version ≠ minimale iOS-Zielversion; Apple verlangt nur das neuere Build-SDK).
  - Keine Änderungen an den Windows- und Android-Packaging-Pfaden.
  - Kein sofortiger erneuter Upload nötig: Die Lieferung von Build 331 war erfolgreich; Apple verlangt die Korrektur erst für die nächste Lieferung (Frist April 2027). Ob ein Verifikations-Upload sinnvoll ist, ist eine offene Frage (s. u.).

## Betroffene Klassen und Komponenten

Es sind keine C#-Datenmodell-, Logik- oder UI-Klassen betroffen. Betroffen sind Build-/CI-Artefakte:

- `.github/actions/package-ios/action.yml` — zentrale Composite-Action des iOS-Packagings: Workload-Pin (`ios-workload-version`, Default `27.2.11039`), SDK-Band-Pin (`11.0.1xx` + temporäre `global.json`), `dotnet publish`-Schritt, TestFlight-Upload. Ggf. Erweiterung um explizite Xcode-Auswahl und/oder SDK-Verifikation des Ergebnisses.
- `.github/workflows/release.yml` — Job `package-ios` (`runs-on: macos-latest`, Gate `IOS_SIGNING_ENABLED`).
- `.github/workflows/staging-ci.yml` — Job `package-ios` (RC-Builds, gleiche Action).
- `scripts/iOS-Deployment.ps1` + `scripts/iOS-Deployment.md` — manueller `store`/`upload`-Pfad; verwendet das lokal installierte Xcode (Pair-to-Mac / Mac).
- `global.json` — SDK-Pin `11.0.100` (nur indirekt, Band-Kompatibilität des Workload-Manifests).
- `src/Reporter/Reporter.csproj` — nur Verifikation, dass `net11.0-ios`/`SupportedOSPlatformVersion` unverändert bleiben; keine Änderung erwartet.
- Dokumentation: `docs/help/ios-deployment/`, `docs/help/release-management/`, `docs/app-store-review.md`, `docs/RELEASE_NOTES.md` (je nach Ergebnis der Umsetzung).
- Tests/Verifikation: kein Unit-/UI-Test-Bedarf; Verifikation über den CI-Build (produzierte `.ipa` bzw. deren SDK-Metadaten) und ggf. über den App-Store-Connect-Verarbeitungsbericht nach einem Upload.

## Implementierungsansatz

- Erweiterungspunkt ist ausschließlich die bestehende `package-ios`-Composite-Action (und deren Aufrufer), nicht der App-Code:
  1. Feststellen, welche Xcode-Version(en) auf dem verwendeten Runner-Image (`macos-latest`) installiert und per `xcode-select` voreingestellt ist. Falls die Default-Version < 27 oder schwankend ist: Xcode gezielt auswählen — Optionen: `sudo xcode-select -s /Applications/Xcode_<Version>.app` im Action-Schritt, neuer Input (z. B. `xcode-path`) oder Pin des Runner-Labels auf ein Image mit passendem Default.
  2. Verifizieren, dass der gepinnte `Microsoft.iOS.Sdk`-Pack (`27.2.11039`, Band `net11.0_27.2`) zur ausgewählten Release-Xcode passt (Pack und Xcode müssen versionskompatibel sein; der Action-Kommentar beschreibt den Pin bereits als „Release-Xcode-kompatibel"). **Annahme:** Build 331 wurde noch auf dem .NET-10-Stand vor der Migration (Pack 26.x ↔ iOS-26.5-SDK) gebaut — die Ursache liegt damit ggf. bereits behoben vor und es fehlt nur noch die Absicherung/Verifikation.
  3. Optional (empfohlen, da Apple-Warnungen sonst erst nach dem Upload sichtbar werden): Guard-Schritt, der die SDK-Version der erzeugten `.ipa` prüft (`Info.plist` → `DTSDKName`/`DTPlatformVersion`) und den Job fehlschlagen lässt, wenn sie < 27 liegt.
- Abhängigkeiten: `IOS_SIGNING_ENABLED`-Repo-Variable und Signing-Secrets steuern, ob die iOS-Jobs überhaupt laufen; der TestFlight-Upload hängt zusätzlich an `IOS_API_KEY_*`-Secrets. Der manuelle Pfad `iOS-Deployment.ps1` nutzt das lokale Xcode — dort ist ggf. nur Doku/Prüfung der Mindestversion nötig.
- Keine neuen Klassen, Interfaces oder Enums erforderlich.

## Konfiguration

Keine Laufzeit- oder Anwendungskonfiguration nötig. Auf Build-Ebene sinnvoll (je nach gewählter Lösung): neuer Action-Input für die Xcode-Auswahl bzw. Anhebung des `ios-workload-version`-Defaults sowie ggf. Pin des Runner-Labels (`runs-on`). Beides liegt auf Repository-/Workflow-Ebene, nicht in der App.

## Offene Fragen

1. Wurde Build 331 (Version 0.8.0) noch aus dem .NET-10-Stand vor der Migration erzeugt, oder lief der iOS-Build trotz Workload-Pin `27.2.11039` mit einem Xcode < 27? — Zu klären anhand des Release-Tags/Commits und des Runner-Image-Logs; entscheidet, ob der Pin bereits ausreicht oder die Xcode-Auswahl der eigentliche Fix ist.
2. Welche Xcode-Versionen stellt `macos-latest` aktuell bereit und welche ist Default? Ist ein explizites `xcode-select` oder ein Runner-Image-Pin (`macos-26` o. ä.) erforderlich, damit reproduzierbar ein Release-Xcode ≥ 27 (kein Beta) verwendet wird?
3. Soll ein SDK-Gate in die `package-ios`-Action eingebaut werden (Build schlägt fehl, wenn `.ipa` < iOS 27 SDK), damit künftige Runner-/Xcode-Änderungen nicht unbemerkt wieder zu Apple-Warnungen führen?
4. Soll zur Verifikation der Korrektur zeitnah ein neues Binary (RC oder Patch-Release) zu TestFlight/App Store Connect hochgeladen werden, oder genügt der Nachweis über die `.ipa`-Metadaten im CI-Protokoll bis zur nächsten regulären Lieferung?
5. Gilt die Anforderung auch für den manuellen `store`-Pfad (`iOS-Deployment.ps1`): Soll das Skript die installierte Xcode-Version prüfen bzw. die Mindestversion dokumentiert werden?
