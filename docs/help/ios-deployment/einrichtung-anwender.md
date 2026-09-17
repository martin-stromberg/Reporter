<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# iOS-Deployment — Einrichtung (einmalig)

Diese Anleitung führt Schritt für Schritt durch das komplette Erst-Setup, bis der erste Build bei Apple hochgeladen werden kann. Alle Schritte, die eine Webseite betreffen, finden unter [developer.apple.com](https://developer.apple.com) bzw. [appstoreconnect.apple.com](https://appstoreconnect.apple.com) statt.

Jeder Abschnitt endet mit den dort erzeugten **Umgebungsvariablen** (`$env:…` in PowerShell). Hinweis: `$env:` gilt nur für die aktuelle Session — dauerhaft setzt du eine Variable pro Benutzer mit `[Environment]::SetEnvironmentVariable("NAME","Wert","User")`.

## Übersicht der Schritte

1. Apple-Developer-Programm-Mitgliedschaft prüfen
2. Bundle-ID (App-ID) registrieren
3. App-Eintrag in App Store Connect anlegen
4. Distribution-Zertifikat erstellen und auf dem Mac installieren
5. App-Store-Provisioning-Profil erstellen und installieren
6. App Store Connect API-Key (.p8) erzeugen und sicher ablegen
7. (Nur Windows) SSH-Zugang zum Mac einrichten

**Wo wird was erzeugt** — die häufigste Fehlerquelle:

| Artefakt | Ort |
|----------|-----|
| Bundle-ID, App-Eintrag, Provisioning-Profil, API-Key | **Webseite** (developer.apple.com / appstoreconnect.apple.com) |
| Distribution-Zertifikat | **Xcode** auf dem Mac (nie über die Webseite — siehe Schritt 4) |
| SSH-Schlüssel | lokal per `ssh-keygen` |

---

## 1. Apple-Developer-Programm-Mitgliedschaft

Voraussetzung ist ein bezahlter Account (99 USD/Jahr). Prüfe unter [developer.apple.com/account](https://developer.apple.com/account), ob die Mitgliedschaft aktiv ist. Ohne Mitgliedschaft gibt es weder Distribution-Zertifikate noch App Store Connect.

**Umgebungsvariable:** keine — dieser Schritt erzeugt keinen Wert.

## 2. Bundle-ID registrieren

Die App verwendet die Bundle-ID `de.martinstromberg.reporter` (steht in `src/Reporter/Platforms/iOS/Info.plist`).

1. Auf [developer.apple.com](https://developer.apple.com) → **Certificates, Identifiers & Profiles** → **Identifiers** → **+**
2. **App IDs** → **App** → weiter
3. **Description:** z. B. `Reporter`, **Bundle ID:** Explicit → `de.martinstromberg.reporter`
4. **Capabilities:** Keine Auswahl nötig. Der Hintergrund-Feed-Abruf ist keine portal-seitige Capability — „Background Modes" existiert nur in Xcode (Signing & Capabilities) und schreibt dort `UIBackgroundModes` in die Entitlements. Die App deklariert das bereits selbst in `src/Reporter/Platforms/iOS/Info.plist` (`UIBackgroundModes` = `fetch` plus `BGTaskSchedulerPermittedIdentifiers`); das Provisioning-Profil übernimmt es implizit. Nur explizite Portal-Capabilities wie Push Notifications müssten hier aktiviert werden — die App nutzt aktuell keine.
5. **Register** klicken

**Umgebungsvariable:** keine — die Bundle-ID steht bereits fest in `Info.plist` und wird nirgends übergeben.

## 3. App-Eintrag in App Store Connect anlegen

1. [appstoreconnect.apple.com](https://appstoreconnect.apple.com) → **Apps** → **+** → **Neue App**
2. **Plattformen:** iOS
3. **Name:** `Reporter` (sichtbarer Store-Name)
4. **Primäre Sprache:** Deutsch
5. **Bundle-ID:** `de.martinstromberg.reporter` auswählen (erscheint nach Schritt 2)
6. **SKU:** frei wählbare interne Kennung, z. B. `reporter-ios`
7. **Benutzerzugriff:** Vollzugriff
8. Erstellen — die App-Kachel existiert jetzt und wartet auf Builds

**Umgebungsvariable:** keine — `store`/`upload` adressieren die App über die Bundle-ID aus dem Build selbst.

## 4. Distribution-Zertifikat erstellen

> **Wichtig:** Das Zertifikat **immer über Xcode** erzeugen — **nicht** über das Developer-Portal. Der Portal-Weg (Certificates → **+**) liefert nur eine `.cer`-Datei mit dem öffentlichen Zertifikat; der private Schlüssel fehlt dann in der Keychain und das Zertifikat steht in Xcode als **„Not in Keychain"** — damit kann nicht signiert werden. Das Portal dient hier nur zum *Widerrufen* alter Zertifikate.

Über Xcode auf dem Mac:

1. **Xcode** → **Settings** (⌘,) → **Accounts** → Apple-ID auswählen → **Manage Certificates**
2. Unten links **+** → **Apple Distribution**
3. Das Zertifikat landet **inklusive privatem Schlüssel** in der Keychain. Der verwendete Name (für `-CodesignKey`) steht dort als `Apple Distribution: <Name> (<Team-ID>)`
4. Prüfen: `security find-identity -v -p codesigning` im Terminal — die Zeile mit „Apple Distribution" notieren

> **Hinweis:** Zeigt die Liste ein altes Zertifikat als „Not in Keychain" oder meldet Xcode „maximum number reached": das alte Zertifikat im Developer-Portal unter **Certificates** widerrufen und Punkt 2 wiederholen. Widerruf beeinträchtigt keine bereits hochgeladenen Builds.

**Umgebungsvariable:** den notierten Zertifikatsnamen setzen —

```powershell
$env:REPORTER_IOS_CODESIGN_KEY = "Apple Distribution: <Name> (<TEAMID>)"
```

## 5. App-Store-Provisioning-Profil erstellen

1. [developer.apple.com](https://developer.apple.com) → **Certificates, Identifiers & Profiles** → **Profiles** → **+**
2. **Distribution** → **App Store Connect** → weiter
3. **App ID:** `de.martinstromberg.reporter` → weiter
4. Das Distribution-Zertifikat aus Schritt 4 wählen → weiter
5. **Provisioning Profile Name:** z. B. `Reporter AppStore` — **genau dieser Name** wird später als `-CodesignProvision` übergeben
6. Herunterladen und **auf dem Mac** per Doppelklick installieren (landet unter `~/Library/MobileDevice/Provisioning Profiles/`). Bei Windows-Builds: die Datei auf den Mac kopieren und dort installieren — ein Doppelklick unter Windows installiert nichts.
7. Auf dem Mac prüfen, dass der Name stimmt — die installierten Profilnamen auslesen (die Dateien heißen `<UUID>.mobileprovision`, der Name steht innen):

   ```bash
   for f in ~/Library/MobileDevice/Provisioning\ Profiles/*.mobileprovision; do
     security cms -D -i "$f" | plutil -extract Name raw -o - - ; echo "  <- $f"
   done
   ```

   Der angezeigte Name ist der Wert für `REPORTER_IOS_PROVISIONING_PROFILE`.

> **Hinweis:** Für `device`-Deploys (Development) bleibt das bestehende Wildcard-Entwicklungsprofil (`VS: WildCard Development`) aus der csproj-PropertyGroup maßgeblich; das Store-Profil wird nur für `store`/`upload` gebraucht.

**Umgebungsvariable:** den gewählten Profilnamen aus Punkt 5 setzen —

```powershell
$env:REPORTER_IOS_PROVISIONING_PROFILE = "Reporter AppStore"
```

## 6. App Store Connect API-Key (.p8)

1. [appstoreconnect.apple.com](https://appstoreconnect.apple.com) → **Benutzer und Zugriff** → **Integrations** → **App Store Connect API** → **Team Keys** → **+**
2. **Name:** z. B. `reporter-upload`, **Rolle:** `App Manager` (reicht für Build-Uploads)
3. **Generieren** → die Seite zeigt **Key-ID** und **Issuer-ID**
4. **Herunterladen** — die `.p8`-Datei kann nur einmal geladen werden!
5. Die Datei auf dem Rechner, von dem das Skript läuft, unter `C:\Users\<du>\keys\AuthKey_<KEY-ID>.p8` ablegen — **außerhalb des Repository**. Das Skript kopiert sie beim ersten `store`-Lauf selbst an den richtigen Ort auf dem Mac.

**Umgebungsvariablen:** die drei Werte aus diesem Schritt setzen —

```powershell
$env:REPORTER_IOS_API_KEY_PATH = "C:\Users\<du>\keys\AuthKey_XXXX.p8"      # Pfad zur .p8 — außerhalb des Repo!
$env:REPORTER_IOS_API_KEY_ID   = "XXXXXXXXXX"                             # Key-ID aus Punkt 3
$env:REPORTER_IOS_API_ISSUER_ID = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"  # Issuer-ID aus Punkt 3
```

## 7. SSH-Zugang zum Mac (nur für Windows-Läufe)

`store`/`upload` führen `codesign`, `security` und `xcrun altool` auf dem Mac aus — von Windows per SSH. Führe die Schritte genau in dieser Reihenfolge aus:

1. **Auf dem Mac:** Systemeinstellungen → **Allgemein** → **Teilen** → **Entfernte Anmeldung** aktivieren
2. **Auf Windows (PowerShell):**

   ```powershell
   ssh-keygen -t ed25519 -f "$env:USERPROFILE\.ssh\id_ed25519"
   ```

   Bei den Passwort-Fragen zweimal **Enter** drücken — der Schlüssel bekommt kein Passwort. (Grund: Das Skript läuft mit `BatchMode=yes` ohne jede Passwortabfrage; ein Key mit Passwort würde nur über einen zusätzlichen `ssh-agent`-Dienst funktionieren — siehe [Fehlerbehebung](troubleshooting.md).)
3. **Öffentlichen Schlüssel auf den Mac kopieren** (PowerShell):

   ```powershell
   Get-Content "$env:USERPROFILE\.ssh\id_ed25519.pub" | ssh <macuser>@<mac> "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"
   ```

   Dieser eine Aufruf fragt noch nach dem Mac-Benutzerpasswort. `<macuser>` ist der **macOS-Kurzname** (wie bei Pair-to-Mac).
4. **Prüfen:**

   ```powershell
   ssh -o BatchMode=yes <macuser>@<mac> "echo ok"
   ```

   muss ohne Passwortabfrage `ok` liefern. Tut es das nicht → [Fehlerbehebung](troubleshooting.md).

**Umgebungsvariablen:** die SSH-/Pair-to-Mac-Zugangsdaten setzen —

```powershell
$env:REPORTER_IOS_MAC_SERVER_ADDRESS = "<mac>.local"      # Hostname oder IP des Mac
$env:REPORTER_IOS_MAC_SERVER_USER    = "<macuser>"        # macOS-Kurzname
```

Für Pair-to-Mac-**Builds** (`build`-Aktion von Windows) kommt zusätzlich `REPORTER_IOS_MAC_SERVER_PASSWORD` hinzu (das Passwort des Mac-Benutzers — nur lokal setzen, niemals committen) sowie bei Bedarf `REPORTER_IOS_MAC_DOTNET_ROOT` für den Remote-.NET-Pfad. Für `store`/`upload` ist das Passwort nicht nötig — SSH läuft über den Schlüssel aus Punkt 3.

## Nach der Einrichtung

Weiter mit dem [Ablauf für Anwender](ablauf-anwender.md): erster Upload, TestFlight-Tester, öffentliche Freigabe.
