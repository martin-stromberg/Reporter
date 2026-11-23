<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Issue #204 „Nachricht von Apple" (ITMS-90725 — iOS-Build auf SDK ≥ 27)

## Übersicht

Apple hat für Build 331 (Version 0.8.0) die Warnung ITMS-90725 ausgesprochen: Das Binary wurde mit dem iOS-26.5-SDK erstellt; ab April 2027 müssen Uploads mit iOS SDK ≥ 27 (Release-Xcode ≥ 27) gebaut sein. Die Bestandsaufnahme hat die Ursache vollständig belegt und zugleich gezeigt, dass die aktuelle CI gar keine `.ipa` mehr erzeugt: Der Workload-Pin `27.2.11039` verlangt exakt Xcode 27.2, aber beide `package-ios`-Jobs laufen auf `macos-latest` (= `macos-26-arm64`, Default-Xcode 26.6, kein Xcode 27 installiert). Umgesetzt werden: Runner-Label `xcode-27`, explizite Xcode-Auswahl und ein SDK-Gate in der `package-ios`-Action, eine Xcode-Mindestversionsprüfung plus SDK-Metadaten-Prüfung im manuellen Pfad `iOS-Deployment.ps1` sowie Dokumentationsaktualisierungen. Kein App-Code, keine UI, keine Datenbank.

## Scope-Klärung

Verbindliche fachliche Abgrenzung vor der Umsetzung — übernommen aus dem Abschnitt `Fachlicher Scope` der `requirement.md`, konkretisiert durch die Bestandsaufnahme:

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein iOS-Release-Binary (`.ipa` für `net11.0-ios`, RID `ios-arm64`), erzeugt aus `release.yml`, `staging-ci.yml` (Job `package-ios`, Composite-Action `.github/actions/package-ios`) oder dem manuellen Pfad `scripts/iOS-Deployment.ps1` (`store`/`upload`) |
| Auslösende Ereignisse | Push auf `main` bzw. Tag `v*.*.*` (Release), Push auf `staging` mit `version.changed == 'true'` (RC), manueller Aufruf von `iOS-Deployment.ps1` |
| Erwartete Wirkung | Die nächste an App Store Connect gelieferte `.ipa` ist mit `DTSDKName` = `iphoneos` ≥ 27 (Release-Xcode) gebaut; der Build-Prozess selbst verifiziert die SDK-Version (Gate-Schritt) und lässt Apple-Warnungen nicht mehr erst nach dem Upload entstehen |
| Umgang mit fehlenden Objekten | Release-Xcode ≥ 27 auf dem CI-Runner: **aktivieren** — Runner-Label `xcode-27` (Image enthält Xcode 27.0/27.1/27.2) plus `DEVELOPER_DIR`-Auswahl von `/Applications/Xcode_27.2.app` in der Action (Default des Images ist Xcode 27.1, das Pack verlangt exakt 27.2). `Microsoft.iOS.Sdk`-Pack ≥ 27.x: **wiederverwenden** — Pin `27.2.11039` existiert bereits und ist korrekt. Veraltetes Xcode (< 27.2) auf einem manuellen Build-Mac: **aktivieren** — Versionsprüfung im Skript mit verständlicher Fehlermeldung statt implizitem Pack-Fehler |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine Änderung an App-Code, Features oder UI | Reine Build-/CI-Korrektur; das Binary-Inhaltliche ist nicht der Apple-Befund | bestätigt (2026-11-23, Agent-Festlegung — Anwender-Abstimmung läuft asynchron über Issue-Kommentare; Ausschluss ist reine Aufgabenabgrenzung) |
| 2 | `SupportedOSPlatformVersion` iOS `15.0` in `src/Reporter/Reporter.csproj` bleibt unverändert | Apple verlangt das neuere **Build**-SDK, nicht ein höheres Deployment-Target | bestätigt (2026-11-23, Agent-Festlegung — fachlich eindeutig: Build-SDK ≠ Deployment-Target) |
| 3 | Keine Änderungen an Windows- und Android-Packaging-Pfaden | Außerhalb der Anforderung; der im selben CI-Lauf ebenfalls rote `package-android`-Job (`NETSDK1094`, Run 45117817038) ist ein separater Befund — `prerelease` bleibt bis zu dessen Klärung blockiert | bestätigt (2026-11-23, Agent-Festlegung — außerhalb des Issue-Scopes; als separater Befund dokumentiert) |
| 4 | Kein dedizierter Verifikations-Upload zu TestFlight/App Store Connect außerhalb des regulären Rhythmus | Der Nachweis erfolgt über das SDK-Gate im CI-Protokoll (`DTSDKName` der erzeugten `.ipa`); die Store-Akzeptanz bestätigt der nächste reguläre RC-/Release-Upload — bis dahin entsteht ohnehin kein neues Binary | bestätigt (2026-11-23, Agent-Festlegung — Gate-Log ist belastbarer Nachweis; Mehrwert eines Extra-Uploads gering) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Runner für `package-ios` | Runner-Label `xcode-27` statt `macos-latest` (in beiden Workflows) | Auf `macos-latest` (= `macos-26-arm64`) ist kein Xcode 27 installiert und kann nicht ausgewählt werden; das GA-Image `xcode-27` enthält Xcode 27.0/27.1/27.2. Nachinstallation via `xcodes` auf `macos-26` verworfen: >10 GB Download pro Lauf, lange Laufzeit, fragil |
| Xcode-Auswahl innerhalb der Action | Neuer Input `xcode-path` (Default `/Applications/Xcode_27.2.app`); Schritt `Select Xcode` schreibt `DEVELOPER_DIR=<xcode-path>/Contents/Developer` in `$GITHUB_ENV` | `DEVELOPER_DIR` braucht kein `sudo`, gilt automatisch für alle Folgeschritte (inkl. `xcode-select -p` für die iTMSTransporter-Suche) und ist auf den Job beschränkt. Input statt Hardcode: Die Kopplung Workload-Pin ↔ Xcode-Version wird explizit und beim nächsten Pin-Wechsel anpassbar |
| Position der Xcode-Auswahl | Erster Schritt der Action, vor `Setup .NET` | Fail-fast: Fehlt die Xcode-App auf dem Runner, bricht der Job in Sekunden mit klarer Meldung ab statt nach Workload-Installation im `Publish (ios)` mit dem Pack-Fehler „requires Xcode 27.2" |
| SDK-Absicherung des Ergebnisses | Eigener Gate-Schritt `Verify built SDK version` nach `Collect iOS package`, vor `Upload to TestFlight` | Apples Warnung wäre sonst erst nach dem Upload sichtbar; der Schritt protokolliert `DTSDKName`/`DTPlatformVersion`/`DTXcode` und lässt den Job fehlschlagen, bevor eine inkompatible `.ipa` ausgeliefert wird — schützt dauerhaft gegen künftige Runner-/Pin-Drift |
| Manueller Pfad | Neue Funktion `Assert-MinimumXcodeVersion` (Remote-`xcodebuild -version` via `Invoke-MacCapture`) in `Invoke-Store` vor dem Build; SDK-Metadaten-Prüfung zusätzlich in `Invoke-IpaValidation` | Beim `store`-Pfad wird die Blockade **vor** dem Build dem Anwender erklärt; `upload` (ohne Build) wird über den erweiterten Validierungsblock in `Invoke-IpaValidation` abgesichert — deckt beide Aktionen ohne doppelte Xcode-Prüfung |

## Programmabläufe

### CI-iOS-Release-Build (`package-ios`)

1. Job `package-ios` (`staging-ci.yml` bzw. `release.yml`) startet auf Runner-Label `xcode-27` (arm64-Image mit Xcode 27.0/27.1(default)/27.2).
2. Neuer Action-Schritt `Select Xcode` (erster Schritt): prüft Existenz von `inputs.xcode-path`; schreibt `DEVELOPER_DIR` in `$GITHUB_ENV`; protokolliert `xcodebuild -version` und die `iphoneos`-SDKs (`xcodebuild -showsdks`); bricht mit `::error::` ab, wenn die App fehlt.
3. `Setup .NET`, `Pin SDK band for this job`, `Install .NET MAUI workload`, `Restore`, `Install signing certificate and provisioning profile` — unverändert; `DEVELOPER_DIR` gilt für alle Folgeschritte.
4. `Publish (ios)` — unverändert; läuft jetzt gegen Xcode 27.2, das Pack `Microsoft.iOS.Sdk 27.2.11039` akzeptiert das SDK `iphoneos27.2`.
5. `Collect iOS package` — unverändert.
6. Neuer Schritt `Verify built SDK version`: entpackt `release-ios.ipa`, liest `DTSDKName`/`DTPlatformVersion`/`DTXcode`/`DTXcodeBuild` aus `Payload/*.app/Info.plist` per `plutil -extract`, protokolliert die Werte und bricht mit `::error::` + `exit 1` ab, wenn `DTSDKName` nicht `iphoneos` mit Major-Version ≥ 27 ist.
7. `Upload to TestFlight`, `Remove signing keychain` — unverändert (laufen nur bei gesetztem `api-key-p8` bzw. `always()`).

Beteiligte Komponenten: `.github/actions/package-ios/action.yml`, `.github/workflows/staging-ci.yml` (Job `package-ios`), `.github/workflows/release.yml` (Job `package-ios`).

### Manueller Store-/Upload-Pfad (`iOS-Deployment.ps1`)

1. `Invoke-Store`: `Assert-StorePrerequisites` → `Assert-CodesigningForAction` → `Assert-TransporterAvailable` → **neu:** `Assert-MinimumXcodeVersion` → `Update-BuildNumber` → `Invoke-Build` → `Get-LatestIpa` → `Copy-ApiKeyToMac` → `Get-MacIpaPath` → `Invoke-IpaValidation` → `Invoke-StoreUpload`.
2. `Assert-MinimumXcodeVersion`: führt `xcodebuild -version` auf dem Build-Mac aus (`Invoke-MacCapture` — lokal auf macOS, per SSH unter Windows); bei Version < 27.2 Abbruch mit verständlicher Meldung (installierter Workload-Pin `27.2.11039` verlangt exakt Xcode 27.2; Apple verlangt SDK ≥ 27).
3. `Invoke-IpaValidation`: neuer Block im Remote-Bash nach den bestehenden `Info.plist`-Invarianten — `DTSDKName`/`DTPlatformVersion`/`DTXcode` lesen, protokollieren, `exit 1` wenn `DTSDKName` nicht `iphoneos` ≥ 27 (harte Prüfung wie die übrigen Invarianten). Deckt `store` und `upload` ab.
4. `Invoke-Upload` (ohne Build): Xcode-Prüfung nicht nötig (kein Build); die erweiterte `Invoke-IpaValidation` fängt eine veraltete `.ipa` ab.
5. `Invoke-StoreUpload`, `Invoke-Build`, `Assert-TransporterAvailable` — unverändert.

Beteiligte Komponenten: `scripts/iOS-Deployment.ps1` (`Assert-MinimumXcodeVersion` neu, `Invoke-Store`, `Invoke-IpaValidation`), `Invoke-MacCapture` (vorhandener Helfer).

## Neue Klassen

Keine C#-Klassen (reine Build-/CI-/Skript-Änderung). Es wird eine neue PowerShell-Funktion `Assert-MinimumXcodeVersion` in `scripts/iOS-Deployment.ps1` angelegt (siehe Änderungen).

## Änderungen an bestehenden Klassen

Keine C#-Klassen betroffen. Geändert werden Build-/CI-Artefakte und ein PowerShell-Skript:

### `package-ios` (Composite Action — `.github/actions/package-ios/action.yml`)

- **Neuer Input:** `xcode-path` (string, optional, Default `/Applications/Xcode_27.2.app`) — Pfad zur Xcode-App auf dem Runner; muss zum `ios-workload-version`-Pin passen (Pack `27.2.11039` verlangt exakt Xcode 27.2). Description dokumentiert die Kopplung und das Beta-Verbot.
- **Neuer Schritt `Select Xcode`** (Position: vor `Setup .NET`): Existenzprüfung des Pfads, `DEVELOPER_DIR` nach `$GITHUB_ENV`, Versionsprotokoll (`xcodebuild -version`, `xcodebuild -showsdks`), `::error::`-Abbruch bei fehlender App.
- **Neuer Schritt `Verify built SDK version`** (Position: nach `Collect iOS package`, vor `Upload to TestFlight`): `.ipa` entpacken (`ditto`/`unzip`), `plutil -extract` von `DTSDKName`, `DTPlatformVersion`, `DTXcode`, `DTXcodeBuild`; Werte ins Log; `::error::` + `exit 1`, wenn `DTSDKName` nicht `iphoneos` ≥ 27.
- **Geänderte Kommentare:** Hinweis bei `ios-workload-version` um die explizite Kopplung an `xcode-path` ergänzen.

### Job `package-ios` in `.github/workflows/staging-ci.yml` (Zeile ~347)

- **Geändert:** `runs-on: macos-latest` → `runs-on: xcode-27`. Aufruf-Signatur der Action unverändert (neuer Input nutzt Default).

### Job `package-ios` in `.github/workflows/release.yml` (Zeile ~150)

- **Geändert:** `runs-on: macos-latest` → `runs-on: xcode-27`. Aufruf-Signatur der Action unverändert (neuer Input nutzt Default).

### `scripts/iOS-Deployment.ps1` (PowerShell-Skript)

- **Neue Funktion:** `Assert-MinimumXcodeVersion` — `xcodebuild -version` auf dem Mac via `Invoke-MacCapture`; Versionsvergleich gegen Mindestversion `27.2`; bei Unterschreitung Abbruch mit verständlicher Meldung (inkl. Verweis auf Workload-Pin `27.2.11039` und Apple-Anforderung). Bei nicht ermittelbarer Version Warnung statt Abbruch (der Pack-eigene Fehler bleibt letzte Sicherung).
- **Geänderte Funktion `Invoke-Store`:** Aufruf `Assert-MinimumXcodeVersion` nach `Assert-TransporterAvailable`, vor `Update-BuildNumber`/`Invoke-Build`.
- **Geänderte Funktion `Invoke-IpaValidation`:** zusätzlicher Prüfblock im Remote-Bash — `DTSDKName`/`DTPlatformVersion`/`DTXcode` aus der entpackten `Info.plist` lesen und protokollieren; `exit 1` mit `FEHLER:`-Meldung, wenn `DTSDKName` nicht `iphoneos` ≥ 27.

### Dokumentation

- `docs/app-store-review.md`: neuer Abschnitt zu ITMS-90725 (Anforderung, Frist April 2027, wie die Pipeline sie erfüllt).
- `docs/help/release-management/architektur.md`, `installation.md`, `ablauf-technisch.md`, `troubleshooting.md`: Runner-Label `xcode-27`, Xcode-Auswahl via `xcode-path`/`DEVELOPER_DIR`, SDK-Gate-Schritt dokumentieren; Troubleshooting-Eintrag für „requires Xcode 27.2" / Gate-Fehlschlag.
- `docs/help/ios-deployment/installation.md`: Xcode-Mindestversion (≥ 27.2 passend zum Workload-Pin) ergänzen; veraltete Angabe „.NET SDK 10.x" auf 11.x korrigieren.
- `docs/help/ios-deployment/ablauf-technisch.md`: Abschnitt „Ablauf `store` → 1. Voraussetzungen prüfen" um `Assert-MinimumXcodeVersion` ergänzen (Funktion, Mindestversion 27.2, Abbruch vor dem Build); Abschnitt „6. Validierung" — die nummerierte Prüfliste 1–7 bildet `Invoke-IpaValidation` eins zu eins ab und muss um den neuen SDK-Metadaten-Prüfblock erweitert werden (`DTSDKName`/`DTPlatformVersion`/`DTXcode` via `plutil -extract` lesen und protokollieren, `exit 1` wenn `DTSDKName` nicht `iphoneos` ≥ 27). Position im Skript nach den `Info.plist`-Invarianten (bisheriger Punkt 5) → Liste entsprechend der Skriptreihenfolge umdurchnummerieren.
- `docs/help/ios-deployment/troubleshooting.md`: neue Einträge für die beiden neuen Abbruchpfade — (a) `Assert-MinimumXcodeVersion`-Abbruch „Xcode ≥ 27.2 erforderlich" vor dem Build (Ursache: `xcode-select` auf dem Build-Mac zeigt auf Xcode < 27.2; Lösung: Release-Xcode 27.2 installieren und per `sudo xcode-select -s` aktivieren; Verweis auf Workload-Pin `27.2.11039`), (b) `FEHLER:`-Ausgang der SDK-Validierung in `Invoke-IpaValidation` bei `store`/`upload` (`.ipa` mit `DTSDKName` < `iphoneos27` → mit passendem Xcode/Workload neu bauen statt hochzuladen).
- `scripts/iOS-Deployment.md`: Xcode-Mindestversion dokumentieren; veraltete Buildnummer-Formel (`github.run_number`) auf `git rev-list --count + run_attempt - 1` korrigieren.
- `docs/RELEASE_NOTES.md`: in der Anforderung nur konditional („je nach Ergebnis der Umsetzung") genannt — kein eigenes Update eingeplant; der Hinweis auf die SDK-27-Umstellung kann beim nächsten regulären Release mitaufgenommen werden.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `xcode-path` (Action-Input) | Pfad muss auf dem Runner existieren und ein Xcode mit `iphoneos`-SDK ≥ 27 enthalten | `::error::` + Job-Abbruch im `Select Xcode`-Schritt, bevor Workload/Build Zeit verbraucht |
| `DTSDKName` der erzeugten `.ipa` (CI-Gate) | Muss `iphoneos` mit Major-Version ≥ 27 sein | `::error::` + `exit 1` vor dem Upload — keine inkompatible `.ipa` erreicht App Store Connect |
| `DTXcode` / `DTPlatformVersion` (CI-Gate) | Werden protokolliert (Diagnosewerte, kein eigener Abbruchgrund); Beta-Ausschluss ergibt sich implizit über den Pack-Pin (Pack verlangt Release-Xcode) | — |
| Xcode-Version auf dem Build-Mac (`Assert-MinimumXcodeVersion`) | `xcodebuild -version` ≥ 27.2 (passend zum lokalen Workload-Pin `27.2.11039`) | Abbruch vor dem Build mit verständlicher Meldung; nicht ermittelbare Version → Warnung |
| `DTSDKName` der `.ipa` (manueller Pfad, `Invoke-IpaValidation`) | `iphoneos` Major ≥ 27 | `FEHLER:` + `exit 1` im Remote-Validierungsskript vor dem Upload |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `runs-on` im Job `package-ios` (`staging-ci.yml`, `release.yml`) | Workflow-Label | `xcode-27` | Runner-Image mit Xcode 27.0/27.1/27.2; `macos-latest` enthält kein Xcode 27 |
| `xcode-path` (neuer Input in `package-ios/action.yml`) | string | `/Applications/Xcode_27.2.app` | Explizite Xcode-Auswahl; muss zum `ios-workload-version`-Pin passen |

Keine Laufzeit- oder Anwendungskonfiguration (`appsettings` o. ä.) betroffen.

## Seiteneffekte und Risiken

- **Runner-Image-Wechsel (`macos-26` → `xcode-27`):** Anderes Basis-Image (macOS 27.x) — vorinstallierte Werkzeugversionen können abweichen (z. B. anderes Runner-.NET-SDK → weiterhin durch temporäre `global.json` gepinnt; iTMSTransporter weiterhin über die Kandidatenliste/Fallback-Pkg gesucht). Nachweis über einen vollständigen `package-ios`-Lauf erforderlich.
- **Repair-Pfad `upload-existing` (`release.yml` + `checkout-release-tag`):** Eine Reparatur **alter** Release-Stände (mit Workload-Pin 26.x) baut bewusst den alten Code — auf `xcode-27` ohne Xcode 26 nicht mehr möglich. Risiko: iOS-Repair für Releases < 0.8.x funktioniert nicht mehr; als bewusste Einschränkung in `docs/help/release-management/troubleshooting.md` dokumentieren.
- **`package-android` ist im selben CI-Lauf ebenfalls fehlgeschlagen** (`NETSDK1094`, Run 45117817038): separater Befund außerhalb dieser Anforderung. Konsequenz: Der `prerelease`-Aggregationsjob bleibt auch nach diesem Fix rot, bis der Android-Fehlschlag separat behoben ist.
- **Verifikation erst bei Versionsänderung:** `package-ios` in `staging-ci.yml` ist an `version.changed` gegated — der Merge dieses Fixes allein löst noch keinen iOS-Lauf aus. Der Nachweis entsteht mit der nächsten Versionsänderung auf `staging` oder einem Release.
- **`package-ios` läuft nicht im PR-Gate:** `pr-staging-ci.yml` hat keinen iOS-Job; die Änderungen lassen sich lokal/im PR nicht ausführen. Review und YAML-Korrektheit (z. B. `actionlint` falls vorhanden, sonst Sichtprüfung) sind der einzige Vorab-Nachweis.

## Umsetzungsreihenfolge

1. **Verfügbarkeit des `xcode-27`-Labels für das Repo prüfen**
   - Voraussetzungen: Keine.
   - Beschreibung: `gh api repos/{owner}/{repo}/actions/runners` bzw. Dokumentation/erster Testlauf — prüfen, ob das Label für dieses Repo auflösbar ist und ob Larger-Runner-Abrechnung greift. Ergebnis entscheidet, ob `xcode-27` oder `xcode-27-xlarge` verwendet wird (oder ob ein alternativer Weg nötig ist — siehe Offene Punkte).

2. **Composite Action erweitern (`.github/actions/package-ios/action.yml`)**
   - Voraussetzungen: Bestehende Action (vorhanden).
   - Beschreibung: Input `xcode-path` hinzufügen; Schritt `Select Xcode` als ersten Schritt einfügen; Schritt `Verify built SDK version` nach `Collect iOS package` einfügen; Kommentare zur Pack↔Xcode-Kopplung ergänzen.

3. **Runner-Label in beiden Workflows umstellen**
   - Voraussetzungen: Schritt 1 (Label auflösbar).
   - Beschreibung: `runs-on: macos-latest` → `xcode-27` in `staging-ci.yml` (Job `package-ios`) und `release.yml` (Job `package-ios`); kurze Kommentarzeile zur Kopplung ans Image.

4. **`scripts/iOS-Deployment.ps1` absichern**
   - Voraussetzungen: Vorhandene Helfer `Invoke-MacCapture`, `Invoke-OnMac` (vorhanden).
   - Beschreibung: `Assert-MinimumXcodeVersion` implementieren und in `Invoke-Store` vor dem Build aufrufen; SDK-Metadaten-Prüfblock (`DTSDKName` ≥ `iphoneos27`) in `Invoke-IpaValidation` ergänzen.

5. **Dokumentation aktualisieren**
   - Voraussetzungen: Schritte 2–4 (Doku beschreibt den finalen Stand).
   - Beschreibung: `docs/app-store-review.md` (ITMS-90725-Abschnitt), `docs/help/release-management/*` (Runner/Xcode/Gate), `docs/help/ios-deployment/installation.md` (Xcode ≥ 27.2 + .NET-11-Korrektur), `docs/help/ios-deployment/ablauf-technisch.md` (`Assert-MinimumXcodeVersion` in der `store`-Voraussetzungsliste + SDK-Metadaten-Prüfblock in der `Invoke-IpaValidation`-Prüfliste), `docs/help/ios-deployment/troubleshooting.md` (neue Abbruchpfade: `Assert-MinimumXcodeVersion`, `FEHLER:` bei SDK-Validierung im `store`/`upload`-Pfad), `scripts/iOS-Deployment.md` (Xcode-Mindestversion + Buildnummer-Formel).

6. **Lokale statische Checks ausführen**
   - Voraussetzungen: Schritte 2–5 abgeschlossen.
   - Beschreibung: `.\scripts\Run-StaticChecks.ps1` — muss Exit-Code 0 ohne Findings liefern (Projektregel; die Änderungen sind YAML/PowerShell/Doku, der Check prüft C#-Format — dennoch ausführen und Ergebnis dokumentieren).

7. **Verifikation über CI-Lauf**
   - Voraussetzungen: Schritte 1–5 gemergt auf `staging`; `version.changed` für einen `package-ios`-Lauf (nächste RC-Versionsänderung) bzw. ein Release auf `main`.
   - Beschreibung: Im `package-ios`-Job-Log prüfen: `Select Xcode` meldet Xcode 27.2; `Publish (ios)` läuft durch; `Verify built SDK version` protokolliert `DTSDKName=iphoneos27.2`; TestFlight-Upload erfolgreich; App-Store-Connect-Verarbeitung ohne ITMS-90725. Zusätzlich prüfen, ob `Upload to TestFlight` `iTMSTransporter` findet: Die Auflösung nutzt u. a. `xcode-select -p` (respektiert `DEVELOPER_DIR`) — der Fund soll über die `ContentDeliveryServices.framework`-Kandidatenliste unter dem per `DEVELOPER_DIR` gesetzten Developer-Dir bzw. über das Fallback-Standalone-Pkg erfolgen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| — | — | Keine neuen automatisierten Tests möglich: Für `.github/actions`/`workflows` und `iOS-Deployment.ps1` existiert keine Testinfrastruktur im Repo; der `node:test`-Suite (`scripts/*.test.mjs`) betrifft nur die Release-Asset-Skripte, die unverändert bleiben. Funktionsnachweis läuft projektüblich über den CI-Lauf (Job-Log des Gate-Schritts) und den App-Store-Connect-Verarbeitungsbericht. |

### Betroffene bestehende Tests

Keine. Der Artefaktname `release-ios.ipa` und der Ablauf der Asset-Listen bleiben unverändert; `release-assets.test.mjs`/`resolve-release-version.test.mjs`/`create-update-manifest.test.mjs` sind nicht betroffen.

### E2E-Tests (primärer Funktionsnachweis)

Keine E2E-Tests erforderlich — und zwar nicht nur wegen vorhandener Unit-Tests, sondern weil die Anforderung **keinen über UI oder Nutzeraktion in der App erreichbaren Ablauf** betrifft: Es ändern sich ausschließlich CI-Infrastruktur (Runner-Label, Action-Schritte) und ein externes Deployment-CLI-Skript. Die FlaUI-Desktop-Suite (`src/Reporter.E2ETests`) prüft die Windows-App und kann macOS-CI-Artefakte oder das App-Store-Ergebnis nicht ausüben. Der tatsächliche „Benutzerfluss" ist der CI-Job bzw. der Skriptlauf; dessen Funktionsnachweis ist der `package-ios`-Job-Log (Gate-Ausgabe) plus App-Store-Connect-Verarbeitung — beides in den Akzeptanzszenarien festgehalten.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: RC-Build mit iOS-27-SDK | Push auf `staging` mit Versionsänderung | `IOS_SIGNING_ENABLED=true`, Signatur-/API-Secrets gesetzt; auf dem Runner ist Xcode 27.2 **nicht** Default (Image-Default 27.1) | `package-ios` startet auf `xcode-27` → `Select Xcode` aktiviert `Xcode_27.2.app` → Workload-Install (Pin `27.2.11039`) → `Publish (ios)` → `Collect iOS package` → `Verify built SDK version` → `Upload to TestFlight` | Job grün; Log zeigt `DTSDKName=iphoneos27.x`; `.ipa` in App Store Connect verarbeitet **ohne** ITMS-90725 | CI-Job-Log (Gate-Ausgabe) + App-Store-Connect-Verarbeitungsbericht |
| 2 | Runner ohne passendes Xcode | Wie #1, aber `xcode-path` nicht vorhanden (z. B. Image-Update entfernt Xcode 27.2) | Wie #1 | `Select Xcode` findet `/Applications/Xcode_27.2.app` nicht | Job bricht sofort mit verständlicher `::error::`-Meldung ab — statt des kryptischen Pack-Fehlers nach ~8 s im `Publish`-Schritt | Job-Log des Schritts (Fail-fast); durch Code-Review der Bedingung nachgewiesen |
| 3 | SDK-Regression unterhalb 27 | Wie #1, aber erzeugte `.ipa` hat `DTSDKName` < `iphoneos27` (z. B. durch falschen Workload-Pin) | Wie #1 | Build läuft durch, `Verify built SDK version` liest `DTSDKName` | Job schlägt **vor** dem Upload fehl — keine `.ipa` erreicht App Store Connect | Gate-Implementierung (Code-Review); zusätzlich schützt der Schritt alle künftigen Läufe |
| 4 | Manueller `store`-Pfad auf Mac mit Xcode < 27.2 | `iOS-Deployment.ps1 -Action store` auf Windows/Mac | Build-Mac hat z. B. Xcode 26.6 per `xcode-select` aktiv | `Assert-StorePrerequisites` → `Assert-TransporterAvailable` → `Assert-MinimumXcodeVersion` | Abbruch **vor** dem Build mit verständlicher Meldung (Xcode ≥ 27.2 erforderlich, Workload-Pin `27.2.11039`) | Manueller Skriptlauf bzw. Review; Fehlertext benennt Ursache und Abhilfe |
| 5 | Manueller `upload`-Pfad mit veralteter `.ipa` | `iOS-Deployment.ps1 -Action upload` auf eine mit Xcode < 27 gebaute `.ipa` | `.ipa` mit `DTSDKName iphoneos26.x` liegt vor | `Invoke-IpaValidation` liest SDK-Metadaten | Validierung schlägt mit `FEHLER:` fehl; kein Upload | Manueller Skriptlauf bzw. Review des Validierungsblocks |

## Offene Punkte

Alle Punkte geklärt (2026-11-23):

| # | Offener Punkt | Auflösung |
|---|---------------|-----------|
| 1 | Ist das Runner-Label `xcode-27` für das Repo verfügbar/auf den Plan abrechenbar (Larger-Runner-Abrechnung vs. Standard-Label)? | **Geklärt — `xcode-27` verwenden.** GitHub-Changelog 2026-10-15: `xcode-27`/`xcode-27-xlarge` GA; `xcode-27` ist ein Standard-Label der GitHub-hosted-Runner-Tabelle für **öffentliche** Repositories (dieses Repo ist `public`) — keine Larger-Runner-Abrechnung. Image `xcode-27-arm64` enthält Xcode 27.0/27.1 (Default)/27.2 unter `/Applications/Xcode_27.2.app` mit SDK `iphoneos27.2` — passt exakt zum Workload-Pin `27.2.11039`. Hinweis: Seit Image-Version 20261119 ist zusätzlich Xcode 27.3 **beta** installiert — der explizite `xcode-path` auf `Xcode_27.2.app` schließt die Beta aus. |
| 2 | Genügt der CI-Nachweis (Gate-Log) bis zur nächsten regulären Lieferung, oder soll zur Verifikation zeitnah ein RC/Patch-Upload erfolgen? | **Entschieden — kein dedizierter Verifikations-Upload** (entspricht Scope-Ausschluss #4): Die Gate-Ausgabe im Job-Log ist der belastbare Nachweis der SDK-Version; die Store-Akzeptanz bestätigt der nächste reguläre RC-/Release-Upload. Ein Sonder-Upload hätte ohne Versionsänderung keinen Auslöser und brächte keinen Mehrwert. |
| 3 | iOS-Repair alter Release-Stände (`upload-existing` + `checkout-release-tag`) ist auf `xcode-27` für Stände mit Pack-Pin 26.x nicht mehr möglich — bewusst hinnehmen? | **Entschieden — hinnehmen und dokumentieren** (in `docs/help/release-management/troubleshooting.md`): Reparatur neuerer Stände bleibt möglich; alte iOS-Binaries liegen bereits ausgeliefert in App Store Connect und müssen nicht erneut gebaut werden. |
