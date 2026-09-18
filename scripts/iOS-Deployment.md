<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# iOS-Deployment (Notizen)

Dieses Dokument ergaenzt `iOS-Deployment.ps1` und fasst den aktuellen Stand,
offene Probleme sowie moegliche Weiterfuehrungen zusammen.

## Ziel

`iOS-Deployment.ps1` soll ausgehend von Windows (mit Pair-to-Mac) oder direkt
auf einem Mac folgende Aktionen ermoeglichen:

- `build`    : iOS-App bauen (optional mit Codesigning -> `.ipa`)
- `simulator`: iOS-App bauen, im iOS-Simulator starten und Screenshot speichern
- `device`   : iOS-App bauen und auf einem physischen Geraet starten
- `store`    : signierten Release-Build erzeugen, validieren und zu
               App Store Connect hochladen (TestFlight); erhoeht automatisch
               `ApplicationVersion` im csproj (Opt-out: `-NoBumpBuildNumber`)
- `upload`   : eine vorhandene `.ipa` validieren und hochladen (`-IpaPath`)

## Was aktuell funktioniert

### `build` (Windows mit Pair-to-Mac)

Ein unsignierter iOS-Build laesst sich von Windows aus ueber Pair-to-Mac
bauen, sobald `_DotNetRootRemoteDirectory` auf den .NET-Cache der passenden
Visual-Studio-Pair-to-Mac-Version zeigt:

- Visual Studio 2022:
  `/Users/<macOS-Kurzname>/Library/Caches/Xamarin/XMA/SDKs/dotnet/`
- Neuere Pair-to-Mac-Versionen (z. B. VS 2026):
  `/Users/<macOS-Kurzname>/Library/Caches/maui/PairToMac/SDKs/dotnet/`

Der Pfad kann ueber `REPORTER_IOS_MAC_DOTNET_ROOT` oder `-DotNetRootRemoteDirectory`
uebersteuert werden.

Mit `-CodesignKey` und `-CodesignProvision` erzeugt `build` stattdessen
eine `.ipa` via `dotnet publish -p:ArchiveOnBuild=true`.

### `build` / `simulator` / `device` auf dem Mac

Wenn das Skript direkt auf einem Mac lauft, funktionieren alle Aktionen
(außer Pair-to-Mac-Parameter natuerlich nicht noetig).

## App Store / TestFlight (`store` und `upload`)

### Voraussetzungen

- Apple Developer Program-Account, App-Eintrag in App Store Connect
  (Bundle-ID `de.martinstromberg.reporter`)
- **Apple Distribution**-Zertifikat in der Mac-Keychain (`-CodesignKey`,
  z. B. `Apple Distribution: Vorname Nachname (TEAMID)`)
- App-Store-Provisioning-Profil fuer die Bundle-ID, installiert auf dem Mac
  (`-CodesignProvision`, Profilname)
- App Store Connect API Key (`.p8`) mit Rolle „App Manager" oder hoeher,
  plus Key-ID und Issuer-ID (`-ApiKeyPath`/`-ApiKeyId`/`-ApiIssuerId` bzw.
  `REPORTER_IOS_API_KEY_PATH`, `REPORTER_IOS_API_KEY_ID`, `REPORTER_IOS_API_ISSUER_ID`)
- Die `.p8`-Datei muss **ausserhalb des Repository** liegen (wird vom
  Skript erzwungen). Das Skript spiegelt sie nach
  `~/.appstoreconnect/private_keys/AuthKey_<KeyId>.p8` auf dem Mac —
  dem festen Suchpfad von `xcrun iTMSTransporter` (identisch zu `altool`).
- Auf Windows: zusaetzlich schluesselbasiertes SSH zum Mac
  (`ssh <macuser>@<mac>` muss ohne Passwort funktionieren). Die von
  Visual Studio Pair-to-Mac angelegten Keys liegen unter
  `%LOCALAPPDATA%\Xamarin\MonoTouch` — ein eigenes `ssh-keygen` +
  Eintrag in `~/.ssh/authorized_keys` auf dem Mac ist zuverlaessiger.

### Ablauf `store`

1. `Assert-StorePrerequisites` prueft alle Parameter, die Existenz der
   `.p8`-Datei (ausserhalb Repo) und dass `CodesignKey` kein
   Development-Zertifikat ist.
2. `ApplicationVersion` im csproj wird um 1 erhoeht (Apple akzeptiert jede
   `CFBundleVersion` nur einmal). Opt-out: `-NoBumpBuildNumber`.
3. `dotnet publish -c Release -p:ArchiveOnBuild=true` mit
   Distribution-Signing erzeugt die `.ipa`
   (`src/Reporter/bin/Release/net10.0-ios/ios-arm64/`).
4. Auf dem Mac (lokal bzw. per SSH von Windows): IPA wird entpackt,
   `codesign --verify --deep --strict` und ein Check auf
   `get-task-allow=false` im `embedded.mobileprovision` laufen,
   anschliessend `xcrun iTMSTransporter -m verify` (Remote-Validierung;
   entfaellt mit Warnung, falls `-m verify` auf dem Ziel-Mac fuer
   iOS-IPAs nicht unterstuetzt wird — der Upload validiert serverseitig).
5. `xcrun iTMSTransporter -m upload -assetFile <ipa> -apiKey <id> -apiIssuer <issuer>`
   laedt die IPA hoch (`-assetFile` statt `-f` — `-f` ist fuer
   `.itmsp`-Pakete reserviert). Nach der Verarbeitung (wenige Minuten)
   erscheint der Build in App Store Connect unter TestFlight.

`upload` startet bei Schritt 4 mit einer vorhandenen IPA (`-IpaPath`,
sonst die neueste unter `bin/Release`).

Alle Laeufe schreiben ein Transcript nach `logs/ios-deploy-<zeitstempel>.log`
(gitignored).

Fuer `simulator` wird die App per `xcrun simctl` gebootet, installiert,
gestartet und ein Screenshot gespeichert. Der Screenshot liegt unter
`src/Reporter/bin/<config>/net10.0-ios/<rid>/simulator-screenshot-<zeitstempel>.png`
und wird automatisch in der Vorschau geoeffnet.

## Bekannte Probleme und Limitierungen

### `simulator` / `device` auf Windows: `hostpolicy.dll` Fehler

`dotnet build -t:Run` fuer iOS/tvOS wird von Microsoft auf Windows **nicht**
unterstuetzt. Das bewirkt, dass `dotnet` versucht, die iOS-DLL lokal als
.NET-Programm zu starten:

```text
A fatal error was encountered. The library 'hostpolicy.dll' required to
execute the application was not found in '...\net10.0-ios\iossimulator-x64\'.
```

Dies ist ein bekanntes .NET-MAUI/.NET-iOS-Problem. Visual Studio umgeht es,
indem es eine eigene IDE-interne Deployment-Pipeline nutzt (Pair-to-Mac ->
`xcrun simctl` / `mlaunch` auf dem Mac).

### `Microsoft.iOS` wurde nicht gefunden

Wenn `_DotNetRootRemoteDirectory` fehlt, sucht der Build auf dem Mac unter
`/usr/local/share/dotnet/packs/...` nach `Microsoft.iOS.Sdk`. Dort ist das
Workload-Pack typischerweise aber nicht installiert; Visual Studio bzw.
Pair-to-Mac legt es in den oben genannten Cache-Pfaden ab.

### `ServerUser` muss der macOS-Kurzname sein

Pair-to-Mac erwartet den Kurznamen (z. B. `mstromberg`), nicht den
vollstaendigen Anzeigenamen (z. B. `Martin Stromberg`).

### Codesigning

- `build` ohne Codesigning: moeglich, erzeugt aber keine `.ipa`.
- `simulator`: sollte ohne Codesigning funktionieren.
- `device` und `.ipa`-Erzeugung: erfordern ein gueltiges Signing-Zertifikat
  (`CodesignKey`) und ein Provisioning-Profil (`CodesignProvision`).
- `store`/`upload`: erfordern zusaetzlich ein **Distribution**-Zertifikat
  und App-Store-Profil sowie den API-Key; ein Development-`CodesignKey`
  wird vom Skript abgelehnt.

### `altool` wurde durch `iTMSTransporter` ersetzt

`xcrun altool` ist bei Apple deprecated; das Skript nutzt daher
`xcrun iTMSTransporter` (`-m verify` zur Remote-Validierung, `-m upload`
fuer den Upload — beide mit `-assetFile`, da `-f` nur fuer
`.itmsp`-Pakete gilt). Authentifizierung und Schluesselsuchpfad
(`~/.appstoreconnect/private_keys/`) sind identisch — `Copy-ApiKeyToMac`
bleibt kompatibel. Vorab pruefbar auf dem Ziel-Mac:
`xcrun iTMSTransporter --version` (bei Xcode bzw. installierter
Transporter-App vorhanden). Falls `-m verify` fuer iOS-IPAs nicht
unterstuetzt wird, entfaellt der Remote-Schritt dokumentiert (Warnung im
Lauf) — die lokale `codesign`-Pruefung bleibt und der Upload validiert
serverseitig. Ausweichweg bleibt die Transporter-App.

### SSH-Delegation von Windows

`store`/`upload` muessen `iTMSTransporter`, `codesign` und `security` auf dem Mac
ausfuehren. Von Windows delegiert das Skript Bash-Skripte per
`ssh -o BatchMode=yes` (schlaegt ohne schluesselbasierte Auth sofort fehl)
und kopiert `.p8`/`.ipa` per `scp`. Bei einem SSH-Fehlschlag gibt das
Skript die auszufuehrenden Mac-Befehle aus, damit sie manuell auf dem Mac
laufen koennen.

### Sicherheit / Passwort

Waehrend der Fehlersuche wurde das Pair-to-Mac-Passwort im Terminal-Output
angezeigt. Das Skript maskiert es inzwischen (`***`), das Passwort sollte
trotzdem erneuert werden.

## Moegliche Weiterfuehrung

### SSH-basiertes Deployment von Windows aus

Fuer `store`/`upload` ist die SSH-Delegation umgesetzt (siehe oben);
`device` laeuft von Windows ebenfalls ueber SSH (`Invoke-DeviceViaSsh`):
`dotnet publish` via Pair-to-Mac erzeugt eine `.ipa` (das lokale
`bin/.../Reporter.app` bleibt bei Pair-to-Mac leer), die per scp nach
`~/ios-uploads/` geht, auf dem Mac mit `ditto` entpackt wird und deren
`Payload/Reporter.app` per `xcrun devicectl device install app` +
`devicectl device process launch` installiert/gestartet wird. `-Console` haengt `--console` an den Launch und streamt die
App-Ausgabe (inkl. Managed-Exceptions bei Absturz) ins lokale Terminal.
`list` fragt per SSH `devicectl list devices` + `simctl list` ab.

Offen bleibt der SSH-Pfad fuer `simulator` — das Skript koennte nach dem
Build dieselben `xcrun simctl`-Befehle remote ausfuehren, die
`Invoke-SimulatorMac` lokal nutzt (boot/install/launch/screenshot).

Dieser Workaround ist **nicht offiziell unterstuetzt** und koennte bei
.NET-/Xcode-Updates wieder brechen.

### Automatisierte UI-Tests mit Appium / WinAppDriver

Fuer eine echte autonome Erkennung von Layout-Problemen auf Windows koennte
ein UI-Test-Projekt mit Appium + WinAppDriver aufgesetzt werden. Das
Windows-Fenster laesst sich dabei in Handysize starten (siehe
`src/Reporter/App.xaml.cs`) und Tests koennen pruefen, ob Elemente
ausserhalb des sichtbaren Bereichs liegen oder Screenshots erzeugen.

## Hilfreiche Links

- [Pair to Mac for iOS development (.NET MAUI)](https://learn.microsoft.com/en-us/dotnet/maui/ios/pair-to-mac)
- [Build an iOS app on macOS with .NET CLI (.NET MAUI)](https://learn.microsoft.com/en-us/dotnet/maui/ios/cli)
- [.NET iOS Known Issues - Launching from the command line is awkward](https://github.com/dotnet/macios/wiki/Known-issues-in-.NET)
