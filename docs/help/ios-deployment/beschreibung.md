<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# iOS-Deployment — Beschreibung

## Zweck

iOS-Apps können nicht wie Windows-Programme einfach kopiert werden: Apple verlangt signierte Pakete und einen definierten Vertriebsweg. Dieses Feature stellt einen einzigen lokalen Buildlauf bereit, der beide Ziele abdeckt — die Entwicklungsinstallation auf einem eigenen Gerät und den Upload zu App Store Connect für TestFlight-Tester und die spätere öffentliche Freigabe.

## Funktionsweise

Das Skript `scripts/iOS-Deployment.ps1` kennt folgende Aktionen:

| Aktion | Ergebnis |
|--------|----------|
| `build` | iOS-App bauen; mit Signaturangaben entsteht eine installierbare `.ipa`-Datei |
| `simulator` | App bauen, im iOS-Simulator starten und einen Screenshot ablegen (nur auf dem Mac) |
| `device` | Signierten Build auf einem am Mac angebundenen Gerät installieren und starten — auf dem Mac direkt oder von Windows per SSH (Release-`.ipa` via Pair-to-Mac bauen, per scp auf den Mac, dort entpacken und per `devicectl` installieren/starten; mit `-Console` wird die App-Ausgabe ins Terminal gestreamt) |
| `store` | Release-Build signieren, Buildnummer automatisch erhöhen, Paket validieren und zu App Store Connect hochladen — danach steht es in TestFlight bereit |
| `upload` | Eine vorhandene `.ipa` erneut validieren und hochladen, ohne neu zu bauen |
| `list` | Verfügbare Simulatoren und Geräte anzeigen (auf dem Mac direkt oder von Windows per SSH) |

Auf Windows läuft der Build über Pair-to-Mac auf dem verbundenen Mac; Validierung und Upload werden per SSH an denselben Mac delegiert. Auf dem Mac selbst läuft alles lokal. Jeder Lauf schreibt ein Protokoll unter `logs/ios-deploy-<zeitstempel>.log`.

## Beispiele

- Vor der öffentlichen Version: `store` hochladen, in App Store Connect eine TestFlight-Testergruppe zuweisen — Tester erhalten die App über die TestFlight-App.
- Schneller Gerätetest: `device` mit Gerätekennung baut, installiert und startet die App in einem Schritt.
- Fehlgeschlagener Upload (z. B. Netzabbruch): `upload` wiederholt nur Validierung und Upload der bereits gebauten Datei.

## Einschränkungen

- Simulator-Deployment ist nur direkt auf dem Mac möglich — Microsoft unterstützt `dotnet build -t:Run` für iOS auf Windows nicht. Geräte-Deployment (`device`) läuft von Windows über SSH/`devicectl` mit.
- Ein App-Store-Upload verlangt einen bezahlten Apple-Developer-Account, ein Distribution-Zertifikat, ein App-Store-Profil und einen API-Schlüssel — die Einrichtung ist in [Einrichtung](einrichtung-anwender.md) beschrieben.
- Jede hochgeladene Buildnummer darf bei Apple nur einmal vorkommen; `store` erhöht sie deshalb automatisch (abschaltbar über `-NoBumpBuildNumber`).
