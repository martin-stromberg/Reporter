<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# iOS-Deployment — Fehlerbehebung

## „Fehlende Pflichtparameter für den Store-Upload"

**Symptom:** `store`/`upload` bricht sofort mit einer Parameterliste ab.

**Ursache:** Mindestens eine Angabe fehlt (`CodesignKey`, `CodesignProvision`, `ApiKeyPath`, `ApiKeyId`, `ApiIssuerId`).

**Lösung:** Fehlende Parameter als `-Parameter` oder `REPORTER_IOS_*`-Umgebungsvariable setzen (siehe [Installation](installation.md)).

## „Development-Zertifikat" wird abgelehnt

**Symptom:** `CodesignKey ('Apple Development: …') ist ein Development-Zertifikat.`

**Ursache:** Für App-Store-Builds ist ein `Apple Distribution`-Zertifikat Pflicht; das csproj-Default-Development-Zertifikat gilt nur für lokale Entwicklung.

**Lösung:** Distribution-Zertifikat erzeugen ([Einrichtung](einrichtung-anwender.md) Schritt 4) und dessen Namen als `-CodesignKey`/`REPORTER_IOS_CODESIGN_KEY` setzen.

## Zertifikat „Not in Keychain" / `CodesignKey` wird nicht gefunden

**Symptom:** In Xcode → Settings → Accounts → Manage Certificates steht ein „Apple Distribution"-Zertifikat mit Status **„Not in Keychain"**, und `security find-identity -v -p codesigning` zeigt keinen `Apple Distribution: …`-Eintrag. Der iOS-Build meldet, der Signaturschlüssel sei nicht im Schlüsselbund.

**Ursache:** Xcode listet alle Zertifikate des Accounts — auch solche, deren **privater Schlüssel auf diesem Mac fehlt** (z. B. auf einem anderen Rechner erzeugt). Ohne privaten Schlüssel kann nicht signiert werden. Der Name `Apple Distribution: <Name> (<TEAM-ID>)` ist der Keychain-Anzeigename — er erscheint erst, sobald Zertifikat + privater Schlüssel lokal installiert sind.

**Lösung (eine von beiden):**

- Zertifikat existiert auf einem anderen Mac: dort in der Schlüsselbundverwaltung Zertifikat **mit privatem Schlüssel** als `.p12` exportieren, auf diesen Mac kopieren, importieren.
- Sonst: in Xcode unter Manage Certificates → **+** → **Apple Distribution** ein **neues** Zertifikat erzeugen (legt privaten Schlüssel + Zertifikat in der Keychain ab). Das alte „Not in Keychain"-Zertifikat kann im Developer-Portal widerrufen werden — bereits hochgeladene Builds sind davon nicht betroffen.

Danach `security find-identity -v -p codesigning` prüfen und die Zeile `Apple Distribution: … (<TEAM-ID>)` als `REPORTER_IOS_CODESIGN_KEY` setzen.

## „Das angegebene iOS-Bereitstellungsprofil wurde nicht gefunden"

**Symptom:** Der Build meldet, das angegebene Provisioning-Profil existiere nicht — obwohl es im Developer-Portal angelegt wurde.

**Ursache:** Das Portal registriert das Profil nur — installiert wird es erst durch Download + Doppelklick **auf dem Mac**, auf dem der Build läuft (`~/Library/MobileDevice/Provisioning Profiles/`). Bei Pair-to-Mac-Builds von Windows muss es auf dem **Mac** liegen, nicht auf Windows.

**Lösung:**

1. `.mobileprovision` im Portal herunterladen (Profiles → Profil → Download) und auf dem Mac per Doppelklick installieren.
2. Installierte Profilnamen auf dem Mac auslesen (Dateien heißen `<UUID>.mobileprovision`, der Name steht innen):

   ```bash
   for f in ~/Library/MobileDevice/Provisioning\ Profiles/*.mobileprovision; do
     security cms -D -i "$f" | plutil -extract Name raw -o - - ; echo "  <- $f"
   done
   ```

3. Der gefundene Name muss exakt `REPORTER_IOS_PROVISIONING_PROFILE` entsprechen — sonst Variable oder Parameter anpassen.

## „Keine iOS-Signaturidentität entspricht dem Bereitstellungsprofil"

**Symptom:** Profil ist installiert und wird gefunden, aber der Build meldet, keine Signaturidentität passe zum Profil.

**Ursache:** Ein Provisioning-Profil ist an die **konkreten Zertifikate** gebunden, die bei seiner Erstellung angekreuzt waren. Wurde danach in Xcode ein neues Distribution-Zertifikat erzeugt (z. B. weil das alte „Not in Keychain" war), verweist das Profil auf das alte Zertifikat, das in der Keychain fehlt.

**Lösung:** Im Developer-Portal → **Profiles** → Profil → **Edit** → unter **Certificates** das aktuell in der Keychain liegende Distribution-Zertifikat ankreuzen → **Save** → neu **herunterladen** und auf dem Mac installieren. Danach `store` erneut ausführen.

## „API-Key-Datei nicht gefunden" / „muss auf eine .p8-Datei zeigen" / „darf nicht innerhalb des Repository liegen"

**Ursache:** `-ApiKeyPath` zeigt auf eine nicht existierende Datei, eine Nicht-`.p8`-Datei oder einen Pfad im Repo.

**Lösung:** Den echten Download von App Store Connect verwenden und außerhalb des Projektverzeichnisses ablegen.

## SSH schlägt fehl (`BatchMode`)

**Symptom:** `SSH-Ausfuehrung … nicht moeglich oder Befehl fehlgeschlagen`, das Skript gibt Mac-Befehle zum manuellen Lauf aus.

**Ursache:** Kein schlüsselbasierter SSH-Zugang; das Skript verwendet `-o BatchMode=yes` und fragt bewusst kein Passwort ab.

**Lösung:** Schlüssel einrichten (siehe [Einrichtung](einrichtung-anwender.md) Schritt 7) und `ssh -o BatchMode=yes <user>@<mac> "echo ok"` prüfen. Alternativ das Skript direkt auf dem Mac ausführen.

**Häufige Ursachen im Detail:**

- **Schlüssel liegt unter anderem Namen/Pfad:** `ssh` sucht nur `$env:USERPROFILE\.ssh\id_ed25519`, `id_rsa` usw. Ein Key z. B. als `C:\Users\<du>\MacBookSSHKey` erzeugt wird nicht gefunden — das Paar nach `.ssh\` verschieben und in `id_ed25519`/`id_ed25519.pub` umbenennen oder neu erzeugen.
- **Schlüssel hat ein Passwort:** `BatchMode=yes` verbietet die Passwortabfrage — der Key schlägt fehl, solange er nicht im `ssh-agent` liegt. Auf Windows (Admin-PowerShell):

  ```powershell
  Set-Service ssh-agent -StartupType Automatic; Start-Service ssh-agent
  ssh-add "$env:USERPROFILE\.ssh\id_ed25519"
  ```

  Der Agent fragt das Passwort einmalig beim `ssh-add` ab; danach läuft `ssh` passwortlos. Simpler für den Anwendungsfall: Schlüssel ohne Passwort neu erzeugen (Passwort-Fragen mit Enter beantworten).

## „exakt Xcode 27.2" / Beta-Toolchain / kein Xcode selektiert — `store` bricht vor dem Build ab

**Symptom:** `store` bricht vor dem Build mit einer dieser Meldungen ab (`Assert-RequiredXcodeVersion`):

- `Fehler: xcode-select zeigt auf dem Build-Mac auf '<Pfad>' - nicht auf ein Xcode.app.`
- `Fehler: Auf dem Build-Mac ist 'Xcode <Version>' aktiv - der Store-Build benoetigt exakt Xcode 27.2.`
- `Fehler: Auf dem Build-Mac ist eine Beta-Toolchain aktiv ('…').`

**Ursache:** Drei mögliche Fälle — (a) `xcode-select` auf dem Build-Mac zeigt nicht auf `<Xcode>.app/Contents/Developer`, sondern z. B. auf die Command Line Tools (`/Library/Developer/CommandLineTools`, enthalten kein `xcodebuild`): aus dem `xcode-select -p`-Pfad eindeutig erkennbar, daher harter Abbruch statt implizitem Fehler mitten im Build; (b) `xcode-select` zeigt auf ein Release-Xcode ≠ 27.2 (älter oder neuer); (c) eine Beta-Toolchain ist aktiv (erkannt an `beta`/`Beta` in der `xcodebuild -version`-Ausgabe oder im `xcode-select -p`-Pfad, z. B. `Xcode_*_beta.app`/`Xcode-beta.app`). Der installierte iOS-Workload-Pin `Microsoft.iOS.Sdk 27.2.11039` verlangt exakt Xcode 27.2 — ohne die Vorab-Prüfung käme mitten im Build der Pack-Fehler `This version of .NET for iOS (…) requires Xcode 27.2`. Apple verlangt für Store-Uploads zudem ein Build-SDK ≥ 27 (ITMS-90725, Frist April 2027) und lehnt Builds aus Beta-Xcode ab.

**Lösung:**
1. Release-Xcode 27.2 auf dem Mac installieren (Apple Developer Downloads / App Store) — kann parallel zu anderen Xcode-Versionen liegen.
2. Aktivieren: `sudo xcode-select -s /Applications/Xcode_27.2.app`, dann `xcode-select -p` und `xcodebuild -version` zur Kontrolle — der Pfad muss `…/Xcode_27.2.app/Contents/Developer` liefern (zeigt er weiterhin auf `/Library/Developer/CommandLineTools`, war der `xcode-select`-Aufruf wirkungslos, z. B. ohne `sudo`).
3. Bei bewusst **neuerem** Xcode (> 27.2): Die exakte Versionsanforderung ist an den Workload-Pin gekoppelt — den Pin auf das zum Xcode passende `Microsoft.iOS.Sdk`-Pack anheben (in der CI: Input `ios-workload-version` zusammen mit `xcode-path`; lokal: `dotnet workload restore` ohne Pin) **und** `$requiredXcodeVersion` am Anfang der Funktion `Assert-RequiredXcodeVersion` in `scripts/iOS-Deployment.ps1` auf dieselbe Xcode-Version setzen. Ohne diese Skript-Anpassung bricht `store` weiterhin mit „benoetigt exakt Xcode 27.2" ab — es gibt keinen Override-Schalter. Pin, `$requiredXcodeVersion` und Xcode-Version müssen versionsgleich bleiben.
4. `store` erneut ausführen. Kommt es trotz passendem Xcode zum Pack-Fehler, stimmt die installierte Workload-Version nicht (`dotnet workload list` — erwartet `ios 27.2.11039/11.0.100`).

## „FEHLER: Build-SDK … App Store verlangt iOS SDK >= 27" bei `store`/`upload`

**Symptom:** Die IPA-Validierung bricht mit `FEHLER: Build-SDK 'iphoneos<26.x>' - App Store verlangt iOS SDK >= 27 (Release-Xcode); mit passendem Xcode/Workload neu bauen statt hochladen.` ab; es erfolgt kein Upload.

**Ursache:** Die zu validierende `.ipa` wurde mit einem Build-SDK < `iphoneos27` erstellt (`DTSDKName` in der `Info.plist` des App-Bundles) — typisch bei einer älteren, mit Xcode 26.x gebauten Datei, die per `upload` eingereicht werden soll. Ein solcher Upload würde Apples ITMS-90725-Warnung auslösen; der Gate-Block in `Invoke-IpaValidation` verhindert das bewusst.

**Lösung:** Nicht hochladen, sondern neu bauen: Release-Xcode 27.2 auf dem Build-Mac aktivieren (`sudo xcode-select -s`), Workload-Stand `27.2.11039` sicherstellen und `store` ausführen — die erzeugte `.ipa` enthält dann `DTSDKName=iphoneos27.x` (sichtbar im Validierungsprotokoll direkt über der Fehlermeldung).

> **Grenze des Gates:** Eine mit **Beta-Xcode** gebaute `.ipa` erfüllt `DTSDKName` ≥ `iphoneos27` ebenso wie ein Release-Build — die Metadaten markieren Beta-Toolchains nicht eindeutig, das Gate kann sie daher nicht abfangen. Beim `store`-Pfad lehnt `Assert-RequiredXcodeVersion` Beta-Xcode bereits vor dem Build ab; im `upload`-Pfad bleibt die serverseitige Ablehnung durch App Store Connect die letzte Sicherung.

## „Mehrere verschiedene .ipa-Dateien"

**Ursache:** Unter `bin/Release/net11.0-ios/ios-arm64` liegen IPAs mit unterschiedlichen Dateinamen (z. B. alte Builds).

**Lösung:** Alte Artefakte löschen oder mit `-IpaPath` gezielt eine Datei wählen.

## `iTMSTransporter`: „duplicate bundle version" / Build schon hochgeladen

**Ursache:** Jede `CFBundleVersion` darf bei Apple nur einmal vorkommen.

**Lösung:** `store` erhöht `ApplicationVersion` automatisch — der Fehler tritt typischerweise bei `upload` einer bereits hochgeladenen IPA oder bei `-NoBumpBuildNumber` auf. Neuen `store`-Lauf ohne Opt-out ausführen.

## `iTMSTransporter -m verify`: „Invalid Signature" / Profil-Fehler

**Ursache:** IPA ist mit Development-Profil oder falschem Zertifikat signiert; oder Profil gehört zu einer anderen Bundle-ID.

**Lösung:** Sicherstellen, dass `-CodesignProvision` ein **App-Store-**Profil für `de.martinstromberg.reporter` ist (kein Ad-hoc-/Development-Profil). Der `get-task-allow`-Check des Skripts fängt Development-Profile bereits vor dem Upload ab.

## Warnung „iTMSTransporter -m verify fehlgeschlagen oder nicht unterstuetzt" / „nicht gefunden"

**Ursache:** Der Remote-Validierungsschritt `iTMSTransporter -m verify` ist fehlgeschlagen, das Werkzeug wurde nicht gefunden oder `-m verify` wird auf dem Ziel-Mac für iOS-IPAs nicht unterstützt. Das Skript läuft bewusst mit Warnung weiter — der Schritt ist optional.

**Lösung:** Kein Eingriff nötig: Die lokale `codesign`-/`embedded.mobileprovision`-Prüfung ist bereits gelaufen, und der Upload (`-m upload`) validiert serverseitig. Soll die Remote-Validierung doch laufen, auf dem Mac `/Applications/Transporter.app/Contents/itms/bin/iTMSTransporter -m verify -assetFile <ipa> -apiKey <id> -apiIssuer <issuer>` manuell ausführen und die Fehlermeldung prüfen (häufig fehlende/veraltete Transporter-Installation — siehe „`iTMSTransporter` nicht gefunden").

> **Hinweis:** Der Upload selbst läuft unter `set -e` und bricht bei einem Fehler hart ab — eine fehlgeschlagene Remote-Validierung allein verhindert die Einreichung nicht.

## Build in TestFlight: „Fehlende Übereinstimmung" (Missing Compliance)

**Ursache:** Apple fragt die Export-Compliance zu Verschlüsselung ab.

**Lösung:** In TestFlight beim Build „Verwalten" → „Keine nicht-freigestellte Verschlüsselung" wählen (die App nutzt nur Standard-HTTPS). Ab dem aktuellen Stand ist `ITSAppUsesNonExemptEncryption` = `false` in beiden `Info.plist`-Dateien gesetzt — bei Builds ab diesem Stand entfällt die Frage dauerhaft; sie betrifft nur Builds, die vorher hochgeladen wurden.

## App-Review lehnt wegen HTTP-Feeds ab

**Ursache:** `NSAllowsArbitraryLoads` in `Info.plist` erlaubt unverschlüsselte Feed-URLs.

**Lösung:** In den Review-Informationen begründen: Benutzer tragen beliebige Feed-URLs ein, die nicht zwingend HTTPS unterstützen. Das ist eine anerkannte Begründung, muss aber erklärt werden — der konsolidierte englische Wortlaut für den Review-Dialog steht in [`docs/app-store-review.md`](../../app-store-review.md) (Abschnitt „ATS-Begründung").

## App stürzt direkt nach dem Start ab (`ExecutionEngineException`, „aot-only mode")

**Ursache:** Release-Builds sind auf iOS Full-AOT — `Expression.Compile()`/`Reflection.Emit` (EF Core nutzt beides, z. B. im `DbSetInitializer` beim ersten `DbContext`) kann nicht JIT-kompiliert werden und wirft `System.ExecutionEngineException`. Debug-Builds laufen, weil dort der Interpreter aktiv ist.

**Lösung:** `<UseInterpreter>true</UseInterpreter>` in der iOS-PropertyGroup der `Reporter.csproj` (gesetzt) — dynamisch erzeugte Delegates laufen dann interpretiert. Diagnose-Wege: `device` mit `-Console` streamt die Exception ins Terminal; alternativ `xcrun devicectl device console --device <udid>`.

## Remote-Bash bricht: `set: -: invalid option`, `trap: EXIT: invalid signal`, `\r` in Pfaden

**Ursache:** Die PowerShell-Here-Strings übernehmen die CRLF-Zeilenenden der `.ps1`-Datei; das per Base64 übertragene Remote-Bash bricht dann an `set -e\r`, `trap ... EXIT\r` und `\r` in Pfaden.

**Lösung:** Seit dem Fix normalisieren `Invoke-OnMac`/`Invoke-MacCapture` die Zeilenenden vor dem Base64-Encoding auf LF — das Problem kann nicht mehr auftreten. Bei einer älteren Skriptversion: Datei aktualisieren.

## `iTMSTransporter` nicht gefunden

**Ursache:** `iTMSTransporter` ersetzt das deprecated `xcrun altool`. Seit Xcode 16 liefert Xcode das Werkzeug nicht mehr mit — `xcrun iTMSTransporter` meldet dann „iTMSTransporter is now part of Transporter". Es gehört zur **Transporter-App** aus dem Mac App Store und liegt unter `/Applications/Transporter.app/Contents/itms/bin/iTMSTransporter`. Das Skript löst diesen Pfad automatisch auf (`xcrun -f` nur als Fallback für ältere Xcode-Versionen); `xcrun iTMSTransporter` direkt aufzurufen funktioniert auf aktuellem Xcode nicht mehr.

**Lösung:** **Transporter-App** aus dem Mac App Store installieren, dann `/Applications/Transporter.app/Contents/itms/bin/iTMSTransporter --version` prüfen. Die Skript-Notizen (`scripts/iOS-Deployment.md`) dokumentieren den Wechsel von `altool`.
