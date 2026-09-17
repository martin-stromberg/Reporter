<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# iOS-Deployment

Der lokale iOS-Buildlauf bringt Reporter vom Quellcode auf ein iPhone/iPad oder bis in den App Store bzw. TestFlight — gesteuert über `scripts/iOS-Deployment.ps1`, auf dem Mac direkt oder von Windows aus mit Build- und Upload-Delegation an einen Mac.

## Inhalt

- [Beschreibung](beschreibung.md)
- [Einrichtung](einrichtung-anwender.md) — einmaliges Setup: Zertifikate, Profile, API-Key, App-Eintrag
- [Ablauf für Anwender](ablauf-anwender.md) — lokales Testen auf Gerät/Simulator, TestFlight-Tester und öffentliche Freigabe, Schritt für Schritt
- [Technischer Ablauf](ablauf-technisch.md)
- [Installation & Konfiguration](installation.md) — Parameter und Umgebungsvariablen
- [Fehlerbehebung](troubleshooting.md)

## Verwandte Bereiche

- [Release-Management](../release-management/index.md) — CI-Pipeline (GitHub Releases). Der iOS-CI-Job `package-ios` ist vorbereitet, aber bis zur Hinterlegung der Signier-Secrets deaktiviert.
- Skript-Notizen: [`scripts/iOS-Deployment.md`](../../../scripts/iOS-Deployment.md)
