<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# iOS-Deployment — Technischer Ablauf

## Übersicht

`scripts/iOS-Deployment.ps1` orchestriert `dotnet build/publish` für `net10.0-ios` und — für Store-Läufe — die macOS-Werkzeuge `codesign`, `security` und `iTMSTransporter`. Unter Windows delegiert es Remote-Schritte per `ssh`/`scp` an den Pair-to-Mac-Host.

## Ablauf `store`

### 1. Voraussetzungen prüfen

`Assert-PairToMacAvailable` (nur Windows: `ServerAddress` nötig), `Assert-StorePrerequisites` (Pflichtparameter `CodesignKey`, `CodesignProvision`, `ApiKeyPath`, `ApiKeyId`, `ApiIssuerId`; `.p8` muss existieren und außerhalb des Repo-Roots liegen; `CodesignKey` darf nicht `Development` enthalten; unter Windows werden SSH-Zugangsdaten verlangt), `Assert-CodesigningForAction`, `Assert-TransporterAvailable` (Remote-Check: `iTMSTransporter` aus der Transporter-App bzw. via `xcrun -f` vorhanden — schlägt sonst vor dem Build mit Installationshinweis fehl).

### 2. Buildnummer erhöhen

`Update-BuildNumber` liest `<ApplicationVersion>` aus `src/Reporter/Reporter.csproj`, inkrementiert und schreibt UTF-8 ohne BOM zurück. Opt-out: `-NoBumpBuildNumber`.

### 3. Signierter Release-Build

`Invoke-Build` führt `dotnet publish src/Reporter/Reporter.csproj -f net10.0-ios -c Release -p:RuntimeIdentifier=ios-arm64 -p:ArchiveOnBuild=true -p:CodesignKey=… -p:CodesignProvision=…` aus. Parameter werden über eine temporäre Response-Datei (`.ios-deploy-*.rsp`, gitignored via `*.rsp`) übergeben; die Ausgabe landet unter `src/Reporter/bin/Release/net10.0-ios/ios-arm64/`. Auf Windows läuft der Build remote auf dem Pair-to-Mac-Host, die `.ipa` wird zurückkopiert.

### 4. API-Key auf den Mac spiegeln

`Copy-ApiKeyToMac` prüft per Remote-`test -f`, ob `$HOME/.appstoreconnect/private_keys/AuthKey_<ApiKeyId>.p8` existiert; falls nicht, wird das Verzeichnis angelegt (`chmod 700`) und die lokale `.p8` dorthin kopiert (macOS: `Copy-Item`, Windows: `scp`). `iTMSTransporter` sucht den Schlüssel automatisch in diesem Pfad (identisch zu `altool`).

### 5. IPA auf den Mac bringen

`Get-MacIpaPath` gibt auf macOS den lokalen Pfad zurück; unter Windows wird die Datei per `scp` nach `$HOME/ios-uploads/<name>.ipa` kopiert.

### 6. Validierung

`Invoke-IpaValidation` führt auf dem Mac (lokal oder via `Invoke-OnMac` über SSH, Skript wird Base64-kodiert übertragen) aus:

1. `ditto -x -k` (Fallback `unzip`) entpackt die IPA in ein Temp-Verzeichnis
2. `codesign --verify --deep --strict -vvv Payload/Reporter.app`
3. `security cms -D -i embedded.mobileprovision` → Abbruch, wenn `get-task-allow` auf `true` steht (Development-Profil)
4. `PrivacyInfo.xcprivacy` muss im Bundle-Root liegen und als plist linten — Abbruch sonst (Packaging-Fehler; die App deklariert Required-Reason-APIs → ITMS-91053)
5. `Info.plist`-Invarianten via `plutil -extract`: `UIDeviceFamily` muss iPhone (`1`) enthalten und darf iPad (`2`) nicht enthalten, `CFBundleLocalizations` muss `en`+`de` enthalten, `ITSAppUsesNonExemptEncryption` muss `false` sein — Abbruch sonst (Regressions-Gate gegen Änderungen an der Store-Konfiguration)
6. `xcrun assetutil --info Assets.car` liest das Encoding des Marketing-Icons (`appiconItunesArtwork`) — reine Information/Warnung: ARGB mit opaken Pixeln ist der MAUI-Normalzustand, ITMS-90717 würde erst bei echter Transparenz greifen
7. `iTMSTransporter -m verify -assetFile <ipa> -apiKey <id> -apiIssuer <issuer>` — das Binary wird aus der Transporter-App (`/Applications/Transporter.app/Contents/itms/bin/iTMSTransporter`, seit Xcode 16 nicht mehr Teil von Xcode) mit `xcrun -f`-Fallback für ältere Xcode-Versionen aufgelöst. Für Apps verlangt iTMSTransporter `-assetFile` (`-f` gilt nur für `.itmsp`-Pakete); fehlt das Werkzeug, schlägt der Aufruf fehl oder wird `-m verify` für iOS-IPAs nicht unterstützt, läuft die Validierung mit Warnung weiter (dokumentierter Fallback: lokale `codesign`-Prüfung + serverseitige Validierung beim Upload)

### 7. Upload

`Invoke-StoreUpload`: `iTMSTransporter -m upload -assetFile <ipa> -apiKey <id> -apiIssuer <issuer>` (gleiche Binary-Auflösung wie bei der Validierung; fehlt das Werkzeug, bricht der Schritt mit Installationshinweis ab — harter Fehler, kein Fallback). Danach verarbeitet Apple den Build; er erscheint in App Store Connect unter TestFlight.

### 8. Protokoll

`Start-DeployLog`/`Stop-DeployLog` klammern `build`/`simulator`/`device`/`store`/`upload` in ein `Start-Transcript` nach `logs/ios-deploy-<timestamp>.log` (gitignored).

## Ablauf `upload`

Wie `store` ab Schritt 4, ohne Buildnummer-Bump und Build; Quelle ist `-IpaPath` oder die neueste `.ipa` unter `bin/Release` (`Get-LatestIpa`, eindeutiger Name erzwungen).

## Ablauf `device`

`Invoke-Run -Action device`: `dotnet build -t:Run` mit `_DeviceName=<UDID>` und optional `CodesignKey`/`CodesignProvision` — nur auf macOS, da `-t:Run` für iOS auf Windows nicht unterstützt wird.

**Windows-Pfad (`Invoke-DeviceViaSsh`):** Sind `-ServerAddress`/`-ServerUser` gesetzt, baut das Skript stattdessen per `dotnet publish` (Pair-to-Mac, `ios-arm64`, mit Dev-Signatur) eine `.ipa` — das lokale `bin/.../Reporter.app` bleibt bei Pair-to-Mac-Builds leer, die `.ipa` wird aber zuverlässig zurückkopiert. Sie wird per scp nach `~/ios-uploads/` auf den Mac gebracht, dort mit `ditto` entpackt und das enthaltene `Payload/*.app` via `xcrun devicectl device install app --device <udid>` installiert; Start via `xcrun devicectl device process launch`. Der Schalter `-Console` hängt `--console` an den Launch — die App-Ausgabe (inkl. unbehandelter Managed-Exceptions) wird bis Ctrl+C ins lokale Terminal gestreamt. Voraussetzung auf dem Gerät: Entwicklermodus an und Gerät im Development-Profil registriert.

## Fehlerbehandlung

- Fehlende/ungültige Parameter → sofortiger Abbruch mit Exit 1 und benannter Ursache
- SSH/SCP-Fehlschlag unter Windows → das Skript gibt die Mac-Befehle zur manuellen Ausführung aus (`Invoke-OnMac`)
- Mehrere verschiedene `.ipa`-Dateien → Abbruch statt Raten (`Get-LatestIpa`)
- Development-Signatur oder Development-Profil → Abbruch vor dem Upload
