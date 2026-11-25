<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-11-23
Abbruchgrund: Maximale Iterationsanzahl erreicht (3 Iterationen)

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [ ] `.github/actions/package-ios/action.yml`, Schritt `Select Xcode` (Zeilen 65–81): Keine Versions-/Beta-Validierung des per `xcode-path`-Input gewählten Xcode. Der Schritt prüft nur Existenz von `xcode-path` und `Contents/Developer`; `xcodebuild -version` wird nur geloggt. Ein Input-Override auf `Xcode_27.0.app`/`Xcode.app` (→27.1) oder `Xcode_27.3_beta.app` passiert beide Checks — falsches Release-Xcode bricht erst mit dem Pack-Fehler im Build, eine Beta-App könnte theoretisch bis zum Upload laufen. Empfehlung aus `review-code.md`: `xcodebuild -version`-Ausgabe auswerten — `beta` ablehnen und Major.Minor gegen `ios-workload-version` abgleichen (analog `Assert-RequiredXcodeVersion`). Betrifft nur abweichende Input-Overrides; die Defaults (`Xcode_27.2.app` ↔ Pin `27.2.11039`) sind korrekt gekoppelt.

## Usability-Befunde

Keine.

## Fehlgeschlagene Tests

Keine — alle ausführbaren Suiten grün (`npm test` 36/36, `Reporter.Tests` 801+4, `Run-StaticChecks.ps1` Exit 0). Die 5 Akzeptanzszenarien der realen End-to-End-Abnahme sind `Nicht durchführbar` in dieser Umgebung und per dokumentierter Festlegung an den nächsten regulären `package-ios`-CI-Lauf nach Merge delegiert (siehe `test-results.md`); der Gate-Schritt liefert dort den automatisierten Nachweis.

## Anmerkung

- Die Verifikationsaufgabe „CI-Lauf `package-ios` auf `xcode-27` + Gate-Log + Store-Verarbeitung ohne ITMS-90725" ist planbedingt erst nach Merge + Versionsänderung ausführbar (`docs/features/task/…-nachricht-von-apple-tasks.md`, letzte offene Aufgabe).
- Separater Befund außerhalb des Scopes: `package-android` schlug im selben CI-Lauf ebenfalls fehl (`NETSDK1094`) — `prerelease` bleibt bis zu dessen Klärung blockiert.
