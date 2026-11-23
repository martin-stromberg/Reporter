<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

Iteration 2 — die drei Befunde aus `review-code.1.md` sind umgesetzt (exakte Versionsprüfung `-ne [version]'27.2'` mit getrennten Meldungen für älter/neuer; Beta-Erkennung über `xcodebuild`-Ausgabe + `xcode-select -p`-Pfad; eigener `Contents/Developer`-Fail-fast in `Select Xcode`; `DTXcodeBuild` in beiden Gates protokolliert). Beim erneuten vollständigen Durchgang sind folgende neue Befunde entstanden.

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### scripts/iOS-Deployment.ps1 (Assert-MinimumXcodeVersion)

- **Namenskonventionen / Methodenname beschreibt das Verhalten nicht** — Zeile 518 bzw. 546–567: Der Funktionsname `Assert-MinimumXcodeVersion` verspricht eine Mindestversionsprüfung (≥), die Umstellung auf `$version -ne $required` erzwingt aber **exakt** 27.2 und lehnt ausdrücklich auch neuere Xcode-Versionen ab (eigener `else`-Zweig für `$version -gt $required`). Wer den Namen liest, erwartet das Gegenteil des Verhaltens — der Name ist seit Iteration 2 falsch.

  Empfehlung: Funktion umbenennen, z. B. `Assert-RequiredXcodeVersion` oder `Assert-ExactXcodeVersion`, inkl. Aufrufstelle (`Invoke-Store`, Zeile 866) und sämtlicher Namensreferenzen in `docs/help/ios-deployment/ablauf-technisch.md` (Zeile 15), `docs/help/ios-deployment/troubleshooting.md` (Zeilen 91–93), `docs/app-store-review.md` (Zeile ~158), `scripts/iOS-Deployment.md` (Zeile ~55).

### scripts/iOS-Deployment.ps1 (Assert-MinimumXcodeVersion)

- **Fehlerbehandlung / feststellbare Fehlkonfiguration läuft in den Warnpfad statt in den Abbruch** — Zeilen 525–530: Ist `xcode-select` auf dem Build-Mac auf die Command Line Tools (`/Library/Developer/CommandLineTools`) oder einen sonstigen Nicht-Xcode-Pfad gestellt, liefert `xcodebuild -version` keine `Xcode <n>`-Zeile — die Funktion gibt dann nur eine Warnung aus und `store` läuft weiter bis zum impliziten Mid-Build-Fehler, genau dem Szenario, das die Vorab-Prüfung verhindern soll. Die Fehlkonfiguration ist aber aus dem bereits erfassten `xcode-select -p`-Output (`$lines`) deterministisch erkennbar: Der Pfad endet nicht auf `*.app/Contents/Developer`.

  Empfehlung: Vor/neben der Versionszeilen-Auswertung den `xcode-select -p`-Pfad prüfen — zeigt er nicht auf ein Xcode-Bundle (`*-match '\.app/Contents/Developer$'` bzw. `*\.app/Contents/Developer`), mit `exit 1` und eigener Meldung („xcode-select zeigt nicht auf ein Xcode.app — `sudo xcode-select -s /Applications/Xcode_27.2.app`") abbrechen. Nur tatsächlich unleserliche Remote-Ausgabe bleibt Warnung.

### scripts/iOS-Deployment.ps1 + docs/help/ios-deployment/troubleshooting.md (Vertragskonflikt)

- **Validierung strenger als der reale Vertrag / dokumentierter Ausweg kann nicht funktionieren** — `Assert-MinimumXcodeVersion` (Zeile 546) kodiert die Annahme „installierter Workload = `Microsoft.iOS.Sdk 27.2.11039`" fest in `[version]'27.2'`, kann den lokal installierten Workload aber gar nicht sehen. Die eigene Troubleshooting-Doku (`docs/help/ios-deployment/troubleshooting.md`, Lösungsschritt 3, Zeile ~98) empfiehlt als Ausweg für den `store`-Abbruch, bei bewusst neuerm Xcode den Workload-Pin anzuheben und `store` erneut auszuführen — mit Xcode 27.3 + passendem Pack 27.3.x wäre der Build fachlich gültig (Gate `iphoneos27.x ≥ 27` bestünde), die Funktion bricht aber weiterhin unvermeidlich mit „benoetigt exakt Xcode 27.2" ab: Der dokumentierte Lösungsschritt führt für das beschriebene Symptom in denselben Abbruch zurück, es gibt kein Override.

  Empfehlung: Entweder die Doku klarstellen — `store` erfordert immer exakt Xcode 27.2 (Pin-Anhebung gilt nur für den CI-Pfad `ios-workload-version`/`xcode-path`), Schritt 3 aus dem `store`-Troubleshooting-Eintrag entfernen bzw. entsprechend einschränken — oder die Prüfung an den tatsächlich installierten Workload koppeln (z. B. `dotnet workload list` remote auswerten) bzw. einen expliziten Override-Schalter einführen.

### scripts/iOS-Deployment.ps1 (Assert-MinimumXcodeVersion)

- **Doppelter Code (gering)** — Zeilen 542–543 und 564–565: Der identische Abhilfe-Block (`Abhilfe auf dem Mac: Release-Xcode 27.2 installieren und aktivieren, z. B.` + `sudo xcode-select -s …`) steht zweimal wortgleich in der Funktion (Beta-Zweig und Versions-Zweig).

  Empfehlung: In eine lokale Variable/einen kleinen Hilfsabschnitt auslagern oder einmal nach dem jeweiligen Begründungsteil ausgeben (beide Abbruchpfade enden mit derselben Abhilfe — nach dem `if/else` der Begründungen gemeinsam drucken).

## Geprüfte Dateien

Quell-/Konfigurationsdateien (vollständig gelesen; Diff gegen Merge-Base `a412efa`, inkl. uncommitteter Working-Tree-Änderungen):

- `.github/actions/package-ios/action.yml`
- `.github/workflows/release.yml`
- `.github/workflows/staging-ci.yml`
- `scripts/iOS-Deployment.ps1`

Dokumentation (Diff auf technische Konsistenz mit der Implementierung geprüft):

- `docs/app-store-review.md`
- `docs/help/ios-deployment/ablauf-technisch.md`
- `docs/help/ios-deployment/installation.md`
- `docs/help/ios-deployment/troubleshooting.md` (Befund oben)
- `docs/help/release-management/ablauf-technisch.md`
- `docs/help/release-management/architektur.md`
- `docs/help/release-management/installation.md`
- `docs/help/release-management/troubleshooting.md`
- `scripts/iOS-Deployment.md`

Planungs-/Tracking-Artefakte (kein Quellcode, nur stichprobenartig auf Konsistenz geprüft):

- `docs/features/task/issue-204-f39e1f6e4f8546a593ba6dcd338496b5-nachricht-von-apple-tasks.md`
- `docs/features/task/issue-204-f39e1f6e4f8546a593ba6dcd338496b5-nachricht-von-apple/{inventory.md,plan.md,plan-check.md,plan-check.1.md,requirement.md,todo.md}` sowie `inventory/*.md`

## Verifizierte Schwerpunkte (ohne Befund)

- **Exakte Versionsprüfung, PowerShell-Semantik:** `[version]'27.2'` ist valide; `$versionLine -match '^Xcode\s+([0-9]+(\.[0-9]+)*)'` + `[version]$Matches[1]` korrekt; `-ne` trifft ältere und neuere Versionen; getrennte Meldungen für `<`/`>` sachlich richtig. Nicht ermittelbare/nicht parsebare Ausgabe → Warnpfad (bewusst, dokumentiert). Der `else`-Zweig des zweiten `-match` (Zeile 550–553) ist formal unerreichbar (die erste Bedingung `^Xcode\s+[0-9]` garantiert den Treffer) — defensive Ausführung, tolerierbar.
- **Beta-Erkennung:** `-match 'beta'` ist case-insensitive und prüft beide Informationsquellen in einer Remote-Runde (`xcodebuild -version` + `xcode-select -p`, `Invoke-MacCapture` liefert Zeilen-Array); deckt `Xcode-beta.app`, `Xcode_*_beta.app` und `… beta` in der Versionsausgabe ab; verbleibendes Restrisiko (umbenannte Beta-App, Beta ohne `beta`-Marker, RC) ist im `upload`-Pfad ehrlich dokumentiert (serverseitige Ablehnung als letzte Sicherung).
- **Remote-Bash-Block `Invoke-IpaValidation`:** neue Zuweisungen laufen unter `set -e` sicher (`|| true` innerhalb der `$(…)`); `[[ =~ ]]`/`BASH_REMATCH` korrekt (läuft via `Invoke-OnMac` → bash); `-z`-Teil greift vor `-lt` (kein Fehler bei leerem/nicht-numerischem `SDK_MAJOR`); Here-String-Escaping (`` `$ ``) konsistent.
- **`Select Xcode` (action.yml):** Beide Existenzprüfungen mit eigenem `::error::`; `DEVELOPER_DIR` via `$GITHUB_ENV` (wirkt ab Folgeschritt) plus in-step `export` (für `xcodebuild -version` im selben Schritt); `xcodebuild -showsdks | grep -i iphoneos || true` unter `set -euo pipefail` abgesichert.
- **`Verify built SDK version` (action.yml):** Position nach `Collect iOS package`, vor `Upload to TestFlight`, kein `if:` — greift auch ohne `api-key-p8`; `ditto`→`unzip`-Fallback und `trap`-Cleanup korrekt; alle vier Metadaten inkl. `DTXcodeBuild` protokolliert.
- **Kopplung der Defaults:** `xcode-path`-Default `/Applications/Xcode_27.2.app` passt zum `ios-workload-version`-Default `27.2.11039`; beide Workflows nutzen die Defaults unverändert.
- **`runs-on: xcode-27`:** in beiden Workflows identisch; dokumentierte Entscheidung in `plan.md` (GA-Image mit Xcode 27.0/27.1/27.2); alle Doku-Verweise auf das Label konsistent.
- **Aufrufreihenfolge `Invoke-Store`:** `Assert-MinimumXcodeVersion` vor `Update-BuildNumber`/`Invoke-Build` — kein Buildnummer-Bump bei abgelehnter Toolchain; `Invoke-Upload` prüft zu Recht nicht (kein Build), wird über den SDK-Block in `Invoke-IpaValidation` abgesichert.
