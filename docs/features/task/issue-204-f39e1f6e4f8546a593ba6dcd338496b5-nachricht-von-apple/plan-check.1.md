<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Nächste gelieferte `.ipa` ist mit iOS-SDK ≥ 27 (Release-Xcode ≥ 27) gebaut | `runs-on: xcode-27` in beiden Workflows; neuer Action-Schritt `Select Xcode` (Input `xcode-path`, Default `/Applications/Xcode_27.2.app`, `DEVELOPER_DIR` via `$GITHUB_ENV`); Workload-Pin `27.2.11039` wiederverwendet und zum Runner-Xcode passend | Akzeptanzszenario 1 (Happy Path: Push auf `staging` mit Versionsänderung → Job-Log zeigt `DTSDKName=iphoneos27.x`); Umsetzungsschritt 7 (CI-Verifikation) | Abgedeckt |
| SDK-Version des Binary ist im Build-Prozess verifizierbar (`DTSDKName`/`DTPlatformVersion`) | Gate-Schritt `Verify built SDK version` nach `Collect iOS package`, vor `Upload to TestFlight`: `plutil -extract` + `::error::`/`exit 1` bei `DTSDKName` < `iphoneos27` | Szenarien 1 und 3 (Gate-Ausgabe im Job-Log; Code-Review der Bedingung) | Abgedeckt |
| Kein ITMS-90725 mehr bei der nächsten Lieferung; keine inkompatible `.ipa` erreicht App Store Connect | Runner-/Xcode-Fix plus Gate vor dem Upload | Szenario 1 (App-Store-Connect-Verarbeitungsbericht), Szenario 3 (Job schlägt vor Upload fehl) | Abgedeckt |
| Release-Xcode, kein Beta | Expliziter `xcode-path` auf `Xcode_27.2.app` schließt die seit Image 20261119 installierte Xcode-27.3-**beta** aus; Pack-Pin verlangt ohnehin Release-Xcode | Szenario 2 (fehlende App → Fail-fast-Abbruch mit klarer Meldung) | Abgedeckt |
| Manueller Pfad `iOS-Deployment.ps1` (`store`/`upload`) abgesichert | Neue Funktion `Assert-MinimumXcodeVersion` (`xcodebuild -version` via `Invoke-MacCapture`, Mindestversion 27.2) in `Invoke-Store` vor dem Build; SDK-Metadaten-Prüfblock in `Invoke-IpaValidation` deckt `upload` ohne Build ab | Szenarien 4 und 5 (Abbruch vor Build bzw. `FEHLER:` in der Remote-Validierung) | Abgedeckt |
| Scope-Ausschlüsse eingehalten: kein App-Code/UI, `SupportedOSPlatformVersion` 15.0 unverändert, keine Windows-/Android-Pfade, kein dedizierter Verifikations-Upload | Vier Scope-Ausschlüsse mit Begründung und Status `bestätigt`; keine csproj-/App-Änderung im Plan; Android-Nebenbefund (`NETSDK1094`) als separater Befund dokumentiert | n/a (Ausschlüsse) | Abgedeckt |
| Betroffene Dokumentation aktualisiert (`docs/help/ios-deployment/`, `docs/help/release-management/`, `docs/app-store-review.md`, `scripts/iOS-Deployment.md`) | Schritt 5 deckt `app-store-review.md`, `release-management/{architektur,installation,ablauf-technisch,troubleshooting}.md`, `ios-deployment/installation.md` und `scripts/iOS-Deployment.md` ab | — | Lücke — siehe unten |

## Fehlende oder unvollständige Testanforderungen

Keine fehlenden Testanforderungen festgestellt. Die verbleibende Lücke betrifft ausschließlich Planbestandteile (Dokumentation), nicht die Testabdeckung. Das Fehlen automatisierter Tests für `.github/actions`/`workflows` und `iOS-Deployment.ps1` ist im Plan nachvollziehbar begründet (keine Testinfrastruktur für diese Artefakte; Funktionsnachweis über CI-Job-Log/Gate-Ausgabe, manuellen Skriptlauf und Code-Review); die Akzeptanzszenarien tragen jeweils einen konkreten Nachweis.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| CI-iOS-Build (`staging-ci`/`release`) bis TestFlight/App Store Connect | Kein klassischer E2E-Test; Funktionsnachweis = `package-ios`-Job-Log (Ausgaben von `Select Xcode` und `Verify built SDK version`) plus App-Store-Connect-Verarbeitungsbericht (Akzeptanzszenario 1, Umsetzungsschritt 7) | Nicht erforderlich mit Begründung — kein über App-UI/Nutzeraktion erreichbarer Ablauf; die FlaUI-Desktop-Suite kann macOS-CI-Artefakte nicht ausüben (im Plan explizit begründet) |
| Manueller `store`-Pfad (`iOS-Deployment.ps1 -Action store`) | Manueller Skriptlauf bzw. Review der neuen `Assert-MinimumXcodeVersion`-Prüfung (Szenario 4) | Nicht erforderlich mit Begründung — externes CLI-Skript, keine Testinfrastruktur; Abbruchpfad und Fehlertext sind als Szenario dokumentiert |
| Manueller `upload`-Pfad (`iOS-Deployment.ps1 -Action upload`) | Manueller Skriptlauf bzw. Review des erweiterten `Invoke-IpaValidation`-Blocks (Szenario 5) | Nicht erforderlich mit Begründung — wie oben |

## Fehlende oder unvollständige Planbestandteile

- [ ] **`docs/help/ios-deployment/ablauf-technisch.md` in Umsetzungsschritt 5 ergänzen:** Die Datei dokumentiert die Prüfschritte von `Invoke-IpaValidation` eins zu eins (Abschnitt „6. Validierung", nummerierte Liste 1–7). Der Plan fügt genau dieser Funktion einen SDK-Metadaten-Prüfblock (`DTSDKName`/`DTPlatformVersion`/`DTXcode`, `exit 1` bei < `iphoneos27`) hinzu — ohne Doc-Update bleibt die verbindliche Aufzählung der Validierungsgates veraltet.
- [ ] **`docs/help/ios-deployment/troubleshooting.md` (oder Äquivalent):** Einträge für die neuen Abbruchpfade fehlen — `Assert-MinimumXcodeVersion` („Xcode ≥ 27.2 erforderlich" vor dem Build) und der neue `FEHLER:`-Ausgang der SDK-Validierung bei `upload`. Analog dazu ist für den CI-Pfad ein Troubleshooting-Eintrag („requires Xcode 27.2" / Gate-Fehlschlag) bereits eingeplant; der manuelle Pfad braucht dasselbe.

## Hinweise

- `docs/RELEASE_NOTES.md` ist in der Anforderung nur konditional („je nach Ergebnis der Umsetzung") genannt; der Plan nennt es nicht — kann optional beim nächsten regulären Release erwähnt werden, kein Blocker.
- In Umsetzungsschritt 7 lohnt zusätzlich der Blick ins Log, ob `Upload to TestFlight` `iTMSTransporter` unter dem per `DEVELOPER_DIR` gesetzten Developer-Dir bzw. über die Kandidatenliste/Fallback-Pkg findet (Suche nutzt `xcode-select -p`).
- Die Endverifikation des `xcode-27`-Labels und des Gate-Schritts ist erst im echten `package-ios`-Lauf möglich (`version.changed`-Gate; `pr-staging-ci.yml` hat keinen iOS-Job) — als Risiko im Plan korrekt benannt; Vorab-Nachweis bleibt YAML-Sichtprüfung/`actionlint`.
