<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# iOS-Deployment — Ablauf: Upload, TestFlight, öffentliche Freigabe

## Voraussetzungen

- [Einrichtung](einrichtung-anwender.md) vollständig durchlaufen (Zertifikat, Profil, API-Key, App-Eintrag)
- Beim ersten Upload kann es nach der Einrichtung des App-Eintrags einige Minuten dauern, bis Apple die Bundle-ID serverseitig kennt.

## Lokales Testen vor dem Upload

### Auf einem physischen iPhone (von Windows aus)

1. iPhone per USB an den **Mac** anschließen (oder nach erfolgreichem Pairing per WLAN), entsperren und „Diesem Computer vertrauen" bestätigen
2. Auf dem iPhone: **Einstellungen → Datenschutz & Sicherheit → Entwicklermodus** einschalten (Neustart nötig)
3. Dann hier unter Windows:

```powershell
.\scripts\iOS-Deployment.ps1 -Action device -Console
```

Das Skript baut eine signierte `.ipa` via Pair-to-Mac, installiert sie per `devicectl` und startet die App. Fehlt `-Device`, `-CodesignKey` oder `-CodesignProvision`, werden die am Mac verfügbaren Geräte/Entwicklungs-Zertifikate/Dev-Profile zur Auswahl gestellt (Enter = letzte Wahl, gespeichert in `.ios-deploy.user.json`). Mit `-Console` läuft die App-Ausgabe live ins Terminal — bei einem Absturz steht die .NET-Exception dort im Klartext (Ctrl+C löst die Verbindung).

> **Hinweis:** Gerät-Deploys brauchen ein **Development**-Zertifikat und ein Dev-Profil, in dem das iPhone registriert ist. Ein mit dem App-Store-Profil signierter Build lässt sich nicht auf dem Gerät installieren.

### Im iOS-Simulator (nur direkt auf dem Mac)

`dotnet build -t:Run` für iOS wird von Windows nicht unterstützt — Simulator-Läufe sind nur auf dem Mac selbst möglich:

```powershell
./scripts/iOS-Deployment.ps1 -Action simulator
```

Baut ein Debug-Bundle, bootet einen iPhone-Simulator (oder `-Device <udid>`), installiert und startet die App und legt einen Screenshot unter `src/Reporter/bin/Debug/net10.0-ios/iossimulator-arm64/` ab. Keine Signatur nötig.

## Schritt-für-Schritt: Build in TestFlight bringen

### 1. Build hochladen

Aus dem Repository-Root:

```powershell
.\scripts\iOS-Deployment.ps1 -Action store
```

Das Skript erhöht dabei automatisch die Buildnummer (`ApplicationVersion` in `src/Reporter/Reporter.csproj`), baut signiert, validiert die Signatur und lädt zu App Store Connect hoch. Jeder Lauf schreibt ein Protokoll unter `logs/`.

> **Hinweis:** Die vom Auto-Bump geänderte `Reporter.csproj` mitcommitten, damit die Buildnummer im Versionsstand bleibt.

Alternativ nur hochladen, ohne neu zu bauen:

```powershell
.\scripts\iOS-Deployment.ps1 -Action upload -IpaPath "src\Reporter\bin\Release\net10.0-ios\ios-arm64\Reporter.ipa"
```

### 2. Verarbeitung abwarten

In App Store Connect → App „Reporter" → **TestFlight** erscheint der Build zunächst als „Wird verarbeitet". Nach wenigen Minuten ist er bereit. Bei älteren Builds kann noch „Fehlende Übereinstimmung/Missing Compliance" stehen — einmal die Export-Frage beantworten: Die App verwendet keine nicht-freigestellte Verschlüsselung (nur Standard-HTTPS) → **„Nein"** wählen. Seit `ITSAppUsesNonExemptEncryption` in der `Info.plist` steht, entfällt die Frage bei neuen Builds automatisch.

### 3. Tester freischalten

**Interne Tester** (eigenes Team, bis zu 100 Personen, kein Apple-Review nötig):

1. App Store Connect → **Benutzer und Zugriff** → Person hinzufügen (Rolle z. B. „App Manager" oder „Entwickler")
2. TestFlight → Gruppe **„Interne Tester"** (bzw. eigene interne Gruppe) → Build zuweisen → Tester anklicken
3. Die Person bekommt eine E-Mail und installiert die App über die TestFlight-App auf dem iPhone

**Externe Tester** (beliebige E-Mail-Adressen oder öffentlicher Link, bis zu 10.000):

1. TestFlight → **+** bei „Externe Tester" → Gruppe anlegen (z. B. „Beta")
2. Build der Gruppe zuweisen → **„Beta-App-Review zur Prüfung einreichen"** — Apple prüft den Build einmalig (meist < 24 h)
3. „Was getestet werden soll" (Beta-Informationen) ausfüllen, Kontakt-E-Mail hinterlegen
4. Nach dem Review: Tester per E-Mail einladen oder den **öffentlichen Link** aktivieren und teilen

> **Hinweis:** Für externe Builds muss der erste Build jeder neuen Version erneut durch das Beta-Review; Folge-Builds derselben Version sind danach sofort verfügbar.

### 4. Öffentliche Freigabe (App Store)

Wenn TestFlight stabil läuft:

1. App Store Connect → App → **+ Version oder Plattform** → iOS, Versionsnummer `1.0` (entspricht `ApplicationDisplayVersion`)
2. Pflichtinhalte ausfüllen:
   - **Screenshots:** nur iPhone 6,9″ (z. B. iPhone 16 Pro Max) — die App läuft ausschließlich auf iPhone (`UIDeviceFamily` = `[1]`, iPad ist kein deklariertes Target; Beschluss „Variante B" in [`docs/app-store-review.md`](../../app-store-review.md)). Screenshots aus dem Simulator (`simulator`-Aktion) oder vom Gerät
   - **Beschreibung, Keywords, Support-URL, Marketing-URL (optional)**
   - **App-Datenschutz** (Fragebogen): Reporter sammelt keine Daten — alle Antworten entsprechend setzen; eine **Datenschutzerklärungs-URL** wird dennoch verlangt → die GitHub-URL von [`docs/privacy-policy.md`](../../privacy-policy.md) auf dem Default-Branch hinterlegen (Antworten im Einzelnen: [`docs/app-store-review.md`](../../app-store-review.md))
   - **Altersfreigabe** ausfüllen — Empfehlung 12+ (ggf. 17+), Fragen zu nicht kontrollierbaren Feed-Inhalten wahrheitsgemäß bejahen (siehe [`docs/app-store-review.md`](../../app-store-review.md))
   - **App-Review-Informationen:** Kontaktdaten; aus [`docs/app-store-review.md`](../../app-store-review.md) übernehmen: die ATS-Begründung für `NSAllowsArbitraryLoads` (anwenderdefinierte Feeds/Bilder teils nur per HTTP) sowie den Hinweis **„kein Login erforderlich — beim ersten Start wird ein Demo-Feed angelegt"**
   - **Versionsfreigabe:** manuell oder automatisch wählen
3. Build auswählen (der aus TestFlight hochgeladene) → **Zur Prüfung einreichen**
4. Nach Apples Review (typisch 24–48 h) erscheint die App im Store bzw. wird zum gewählten Zeitpunkt freigegeben

### 5. Folge-Versionen

`ApplicationDisplayVersion` in `src/Reporter/Reporter.csproj` erhöhen (z. B. `1.1`), normal committen, dann wieder `-Action store` — die Buildnummer wird weiter automatisch hochgezählt.

## Ergebnis

Nach dem Upload ist der Build in TestFlight für die zugewiesenen Tester installierbar; nach Zuweisung zu einer Store-Version und erfolgreichem Review ist die App öffentlich im App Store verfügbar.
