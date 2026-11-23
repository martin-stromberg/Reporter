<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

Zweite Prüfung: Die beiden in `plan-check.1.md` gemeldeten Dokumentationslücken sind geschlossen — `docs/help/ios-deployment/ablauf-technisch.md` (Abschnitte „1. Voraussetzungen prüfen" und „6. Validierung" inkl. Umnummerierung der Prüfliste 1–7) und `docs/help/ios-deployment/troubleshooting.md` (beide neuen Abbruchpfade) sind jetzt in `plan.md` (Zeilen 95–96) und in Umsetzungsschritt 5 (Zeile 151) konkret beschrieben. Die Abschnittsbezeichnungen wurden gegen den Bestand verifiziert und stimmen überein.

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Nächste gelieferte `.ipa` ist mit iOS-SDK ≥ 27 (Release-Xcode ≥ 27) gebaut | `runs-on: xcode-27` in `staging-ci.yml` (Zeile ~347) und `release.yml` (Zeile ~150); neuer Action-Schritt `Select Xcode` (Input `xcode-path`, Default `/Applications/Xcode_27.2.app`, `DEVELOPER_DIR` via `$GITHUB_ENV`); Workload-Pin `27.2.11039` wiederverwendet und zum Runner-Xcode passend | Akzeptanzszenario 1 (Happy Path: Push auf `staging` mit Versionsänderung → Job-Log zeigt `DTSDKName=iphoneos27.x`); Umsetzungsschritt 7 (CI-Verifikation) | Abgedeckt |
| SDK-Version des Binary ist im Build-Prozess verifizierbar (`DTSDKName`/`DTPlatformVersion`) | Gate-Schritt `Verify built SDK version` nach `Collect iOS package`, vor `Upload to TestFlight`: `plutil -extract` + `::error::`/`exit 1` bei `DTSDKName` < `iphoneos27` | Szenarien 1 und 3 (Gate-Ausgabe im Job-Log; Code-Review der Bedingung) | Abgedeckt |
| Kein ITMS-90725 mehr bei der nächsten Lieferung; keine inkompatible `.ipa` erreicht App Store Connect | Runner-/Xcode-Fix plus Gate vor dem Upload | Szenario 1 (App-Store-Connect-Verarbeitungsbericht), Szenario 3 (Job schlägt vor Upload fehl) | Abgedeckt |
| Release-Xcode, kein Beta | Expliziter `xcode-path` auf `Xcode_27.2.app` schließt die seit Image 20261119 installierte Xcode-27.3-**beta** aus; Pack-Pin verlangt ohnehin Release-Xcode | Szenario 2 (fehlende App → Fail-fast-Abbruch mit klarer Meldung) | Abgedeckt |
| Manueller Pfad `iOS-Deployment.ps1` (`store`/`upload`) abgesichert | Neue Funktion `Assert-MinimumXcodeVersion` (`xcodebuild -version` via `Invoke-MacCapture`, Mindestversion 27.2) in `Invoke-Store` vor dem Build; SDK-Metadaten-Prüfblock in `Invoke-IpaValidation` deckt `upload` ohne Build ab | Szenarien 4 und 5 (Abbruch vor Build bzw. `FEHLER:` in der Remote-Validierung) | Abgedeckt |
| Umgang mit fehlenden Objekten: geeignetes Xcode auf dem Runner **aktivieren**, Workload-Pin **wiederverwenden**, veralteten lokalen Build-Pfad **aktivieren** (Mindestversion prüfen/dokumentieren) | Scope-Klärung-Tabelle vollständig gefüllt; alle drei Fälle mit konkreter Maßnahme (Label + `DEVELOPER_DIR`; Pin unverändert; `Assert-MinimumXcodeVersion` + Doku) | Szenarien 1–5 | Abgedeckt |
| Scope-Ausschlüsse eingehalten: kein App-Code/UI, `SupportedOSPlatformVersion` 15.0 unverändert, keine Windows-/Android-Pfade, kein dedizierter Verifikations-Upload | Vier Scope-Ausschlüsse mit Begründung und Status `bestätigt`; keine csproj-/App-Änderung im Plan; Android-Nebenbefund (`NETSDK1094`) als separater Befund dokumentiert | n/a (Ausschlüsse) | Abgedeckt |
| Betroffene Dokumentation aktualisiert (`docs/help/ios-deployment/`, `docs/help/release-management/`, `docs/app-store-review.md`, `scripts/iOS-Deployment.md`) | Schritt 5 deckt `app-store-review.md`, `release-management/{architektur,installation,ablauf-technisch,troubleshooting}.md`, `ios-deployment/{installation,ablauf-technisch,troubleshooting}.md` und `scripts/iOS-Deployment.md` ab; `docs/RELEASE_NOTES.md` konditional mit Begründung behandelt | — | Abgedeckt (beide Lücken aus `plan-check.1.md` geschlossen) |

## Fehlende oder unvollständige Testanforderungen

Keine. Das Fehlen automatisierter Tests für `.github/actions`/`workflows` und `iOS-Deployment.ps1` ist im Plan nachvollziehbar begründet (keine Testinfrastruktur für diese Artefakte; der `node:test`-Suite betrifft nur unveränderte Release-Asset-Skripte). Funktionsnachweis über CI-Job-Log/Gate-Ausgabe, App-Store-Connect-Verarbeitungsbericht, manuellen Skriptlauf und Code-Review; alle fünf Akzeptanzszenarien tragen einen konkreten Nachweis samt Ausgangszustand. Betroffene bestehende Tests: keine (`release-ios.ipa`-Name und Asset-Listen unverändert).

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| CI-iOS-Build (`staging-ci`/`release`) bis TestFlight/App Store Connect | Kein klassischer E2E-Test; Funktionsnachweis = `package-ios`-Job-Log (Ausgaben von `Select Xcode` und `Verify built SDK version`) plus App-Store-Connect-Verarbeitungsbericht (Akzeptanzszenario 1, Umsetzungsschritt 7) | Nicht erforderlich mit Begründung — kein über App-UI/Nutzeraktion erreichbarer Ablauf; die FlaUI-Desktop-Suite kann macOS-CI-Artefakte nicht ausüben (im Plan explizit begründet) |
| Manueller `store`-Pfad (`iOS-Deployment.ps1 -Action store`) | Manueller Skriptlauf bzw. Review der neuen `Assert-MinimumXcodeVersion`-Prüfung (Szenario 4) | Nicht erforderlich mit Begründung — externes CLI-Skript, keine Testinfrastruktur; Abbruchpfad und Fehlertext sind als Szenario dokumentiert |
| Manueller `upload`-Pfad (`iOS-Deployment.ps1 -Action upload`) | Manueller Skriptlauf bzw. Review des erweiterten `Invoke-IpaValidation`-Blocks (Szenario 5) | Nicht erforderlich mit Begründung — wie oben |

## Fehlende oder unvollständige Planbestandteile

Keine. `Scope-Klärung` ist vollständig gefüllt (Verarbeitungseinheit, auslösende Ereignisse, erwartete Wirkung, Umgang mit fehlenden Objekten); alle vier Scope-Ausschlüsse tragen Begründung und Status `bestätigt`; `Akzeptanzszenarien` enthält einen vollständigen Happy Path (Szenario 1) plus vier Szenarien mit benanntem Ausgangszustand (fehlendes Xcode, SDK-Regression, veralteter Build-Mac, veraltete `.ipa`); Wiederaufnahme-/Blockadewege sind definiert (Fail-fast mit `::error::`, `FEHLER:`-Meldungen mit Abhilfe); Seiteneffekte und Risiken sind benannt (Runner-Image-Wechsel, Repair-Pfad-Einschränkung, Android-Nebenbefund, `version.changed`-Gate, fehlender iOS-Job im PR-Gate); alle offenen Punkte aus der Anforderung sind im Abschnitt „Offene Punkte" aufgelöst.

## Hinweise

- Gegen den Bestand verifiziert: `docs/help/ios-deployment/ablauf-technisch.md` enthält die Abschnitte „1. Voraussetzungen prüfen" (Zeile 13) und „6. Validierung" (Zeile 33) mit der nummerierten Prüfliste 1–7, die `Invoke-IpaValidation` eins zu eins abbildet — die im Plan beschriebene Erweiterung (SDK-Metadaten-Block nach Punkt 5, Umnummerierung) passt exakt. `Invoke-MacCapture` (Skript-Zeile 179), `Invoke-Store` (788), `Invoke-Upload` (803), `Invoke-IpaValidation` (696) existieren; `Assert-MinimumXcodeVersion` ist korrekt als neu markiert. `runs-on: macos-latest` steht in `staging-ci.yml:347` und `release.yml:150` — die Zeilenangaben des Plans stimmen.
- `docs/RELEASE_NOTES.md` ist in der Anforderung nur konditional genannt; der Plan begründet den Verzicht auf ein eigenes Update (Hinweis kann beim nächsten regulären Release mitaufgenommen werden) — kein Blocker.
- In Umsetzungsschritt 7 lohnt zusätzlich der Blick ins Log, ob `Upload to TestFlight` `iTMSTransporter` unter dem per `DEVELOPER_DIR` gesetzten Developer-Dir bzw. über die Kandidatenliste/Fallback-Pkg findet (Suche nutzt `xcode-select -p`) — der Plan nennt dies explizit.
- Die Endverifikation des `xcode-27`-Labels und des Gate-Schritts ist erst im echten `package-ios`-Lauf möglich (`version.changed`-Gate; `pr-staging-ci.yml` hat keinen iOS-Job) — als Risiko im Plan korrekt benannt; Vorab-Nachweis bleibt YAML-Sichtprüfung/`actionlint`.
