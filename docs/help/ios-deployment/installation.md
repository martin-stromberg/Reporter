<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# iOS-Deployment — Installation und Konfiguration

## Voraussetzungen

- macOS mit Xcode (kompatibel zur installierten .NET-MAUI-Version) — als Pair-to-Mac-Build-Host unter Windows oder als direkte Ausführungsplattform
- .NET SDK 10.x mit MAUI-Workload (`dotnet workload restore Reporter.sln`)
- Apple Developer Program-Mitgliedschaft, Distribution-Zertifikat in der Mac-Keychain, App-Store-Provisioning-Profil, App-Store-Connect-API-Key (`.p8`) — Einrichtung siehe [Einrichtung](einrichtung-anwender.md)
- Für `store`/`upload` unter Windows: OpenSSH-Client (in Windows enthalten) mit schlüsselbasierter Anmeldung am Mac

## Konfiguration

| Parameter | Typ | Standardwert | Beschreibung |
|-----------|-----|--------------|--------------|
| `-Action` | `build`/`simulator`/`device`/`store`/`upload`/`list`/`menu` | `menu` | Auszuführender Schritt |
| `-Device` | String | — | UDID/Name des Simulators oder Geräts (bei `device` ohne Angabe: interaktive Auswahl) |
| `-Configuration` | `Debug`/`Release` | `Debug` (simulator) / `Release` | Build-Konfiguration |
| `-RuntimeIdentifier` | `iossimulator-arm64`/`iossimulator-x64`/`ios-arm64` | auto | Zielarchitektur |
| `-CodesignKey` | String | Env `REPORTER_IOS_CODESIGN_KEY` | Signatur-Identität (Store: `Apple Distribution: …`) |
| `-CodesignProvision` | String | Env `REPORTER_IOS_PROVISIONING_PROFILE` | Name des Provisioning-Profils |
| `-CodesignEntitlements` | String | — | Pfad zur Entitlements-Datei |
| `-ApiKeyPath` | String | Env `REPORTER_IOS_API_KEY_PATH` | Pfad zur `.p8`-Datei — muss außerhalb des Repo liegen |
| `-ApiKeyId` | String | Env `REPORTER_IOS_API_KEY_ID` | Key-ID aus App Store Connect |
| `-ApiIssuerId` | String | Env `REPORTER_IOS_API_ISSUER_ID` | Issuer-ID aus App Store Connect |
| `-IpaPath` | String | neueste `.ipa` unter `bin/Release` | Quelle für `upload` |
| `-Console` | Switch | aus | Bei `device` (SSH-Pfad): App-Ausgabe via `devicectl --console` streamen |
| `-NoBumpBuildNumber` | Switch | aus | Verhindert den automatischen `ApplicationVersion`-Bump bei `store` |
| `-LogDir` | String | `logs` | Ablage der Transcript-Logs |
| `-ServerAddress`/`-ServerUser`/`-ServerPassword`/`-TcpPort`/`-DotNetRootRemoteDirectory` | String | Env `REPORTER_IOS_MAC_*` | Pair-to-Mac-/SSH-Zugangsdaten |

## Interaktive Signatur-Auswahl

Fehlen `-CodesignKey`/`-CodesignProvision` (und die Env-Variablen) oder `-Device`, liest das Skript die verfügbaren Werte vom Mac aus und bietet eine nummerierte Auswahl:

- **Geräte** (bei `device` ohne `-Device`): `xcrun devicectl list devices -j` — Auswahl als `Name (UDID) [Status]`
- **Zertifikate:** `security find-identity -v -p codesigning` — gefiltert nach Aktion (`store`/`upload`: nur `Apple Distribution:`, `device`: nur `Apple Development:`)
- **Profile:** Namen + Entwicklungs-Flag (`get-task-allow`) aus allen installierten `.mobileprovision`-Dateien — entsprechend gefiltert, nach Name dedupliziert

Die getroffene Wahl wird pro Aktion in `.ios-deploy.user.json` (Repo-Root, gitignored) gespeichert und ist beim nächsten Lauf der vorgeschlagene Standard — Enter bestätigt. `-NoPrompt` deaktiviert die Abfrage komplett (für CI/automatisierte Läufe). Ein unpassender vorgegebener Schlüssel (z. B. ein Distribution-Zertifikat bei `device`) wird verworfen und die Auswahl stattdessen angeboten.

## Umgebungsvariablen

| Variable | Geltungsbereich | Pflicht | Beispielwert | Beschreibung |
|----------|-----------------|---------|--------------|--------------|
| `REPORTER_IOS_CODESIGN_KEY` | Team | für `device`/`store`/`upload` (sonst interaktive Auswahl) | `Apple Distribution: Max Muster (AB12CD34EF)` | Signaturzertifikat |
| `REPORTER_IOS_PROVISIONING_PROFILE` | **App** | für `device`/`store`/`upload` (sonst interaktive Auswahl) | `Reporter AppStore` | Profilname |
| `REPORTER_IOS_API_KEY_PATH` | Team | für `store`/`upload` | `C:\Users\x\keys\AuthKey_X.p8` | `.p8` außerhalb des Repo |
| `REPORTER_IOS_API_KEY_ID` | Team | für `store`/`upload` | `AB12CD34EF` | API-Key-ID |
| `REPORTER_IOS_API_ISSUER_ID` | Team | für `store`/`upload` | `69a6de7-…` | Issuer-ID |
| `REPORTER_IOS_MAC_SERVER_ADDRESS` | Maschine | Windows | `mac-mini.local` | Mac-Hostname/-IP |
| `REPORTER_IOS_MAC_SERVER_USER` | Maschine | Windows | `mstromberg` | macOS-Kurzname |
| `REPORTER_IOS_MAC_SERVER_PASSWORD` | Maschine | Windows (Build) | — | nur Pair-to-Mac-Build, nie committed |
| `REPORTER_IOS_MAC_DOTNET_ROOT` | Maschine | Windows (Build) | `/Users/x/Library/Caches/maui/PairToMac/SDKs/dotnet/` | Remote-.NET-Pfad |

**Geltungsbereich:** *Team*-Werte gelten für alle Apps desselben Apple-Developer-Accounts — der App-Store-Connect-API-Key und das Distribution-Zertifikat sind account-weit und können bei weiteren Apps unverändert wiederverwendet werden. *Maschine*-Werte hängen nur am Build-Mac. Einzig *App*-spezifisch ist das Provisioning-Profil (gebunden an die Bundle-ID). Alle Variablen sind nur Standardwerte: für eine andere App genügt `-CodesignProvision "AnderesProfil"` als Parameter beim Aufruf — die übrigen Variablen bleiben gleich.

## Überprüfung

1. Syntax: `Get-Command -Syntax .\scripts\iOS-Deployment.ps1` bzw. Aufruf ohne Parameter öffnet das Menü
2. `-Action upload` ohne Parameter muss die fehlenden Pflichtparameter sauber auflisten (Exit 1)
3. `ssh -o BatchMode=yes <macuser>@<mac> "echo ok"` liefert `ok` ohne Passwortabfrage
4. Auf dem Mac: `security find-identity -v -p codesigning` zeigt das „Apple Distribution"-Zertifikat; `ls ~/Library/MobileDevice/Provisioning\ Profiles` zeigt das Store-Profil
