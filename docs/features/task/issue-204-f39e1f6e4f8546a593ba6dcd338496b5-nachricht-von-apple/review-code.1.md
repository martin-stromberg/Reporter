<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### scripts/iOS-Deployment.ps1 (Assert-MinimumXcodeVersion)

- **Fehlerbehandlung / fehlende Validierung der Vorbedingung** — Zeilen 518–547: Die Prüfung erzwingt nur `$version -lt $minimum` (also ≥ 27.2), während der gekoppelte Workload-Pin `Microsoft.iOS.Sdk 27.2.11039` laut eigenen Kommentaren und sämtlicher Dokumentation **exakt** Xcode 27.2 verlangt. Auf einem Build-Mac mit neuerem Release-Xcode (z. B. 27.3) passiert die Assert-Funktion, und der Build bricht anschließend mitten im Lauf mit genau dem Pack-Fehler `requires Xcode 27.2` ab, dessen verständliche Vorab-Meldung der Sinn der Funktion ist („Ohne diese Vorab-Pruefung kaeme erst mitten im Build der Pack-Fehler").

  Empfehlung: Prüfung gegen die exakt gepinnte Version schärfen (z. B. `$version -ne [version]'27.2'` → Fehler bzw. Warnung, weil neuere Xcode zwar SDK ≥ 27 liefert, aber zur Pin-Kopplung passt nur 27.2). Falls das Pack tatsächlich neuere 27.x toleriert, stattdessen die „exakt"-Aussage in den Kommentaren (`action.yml` Zeilen 45/49/57–59, `docs/help/*`, `scripts/iOS-Deployment.md`) korrigieren — aktuell widersprechen sich Vertrag und Durchsetzung.

### .github/actions/package-ios/action.yml (Select Xcode)

- **Fehlerbehandlung / unzureichende Eingabevalidierung** — Zeilen 67–71: Geprüft wird nur `[ ! -d "$XCODE_APP" ]`. Existiert der Pfad, ist aber kein Xcode-Bundle, zeigt `DEVELOPER_DIR` auf ein nicht existierendes `Contents/Developer`; der Schritt scheitert zwar noch fail-fast am nachfolgenden `xcodebuild -version` (`set -e`), aber mit dem generischen xcode-select-Tool-Fehler statt der eigens dafür formulierten `::error::`-Meldung — der Schritt erfüllt seinen Diagnose-Zweck nur teilweise.

  Empfehlung: Die Vorbedingung präzisieren, z. B. `if [ ! -d "$XCODE_APP/Contents/Developer" ]` (Deckt fehlende App und Nicht-Xcode-Verzeichnis ab) bzw. `xcodebuild -version` mit eigener `::error::`-Meldung absichern.

### scripts/iOS-Deployment.ps1 (Assert-MinimumXcodeVersion / Invoke-IpaValidation)

- **Validierung gegen den deklarierten Vertrag: Beta-Ausschluss wird nicht erzwungen** — Zeilen 530–540 und 766–780: Fehlermeldung und Kommentare behaupten „(Release-Xcode, keine Beta)", aber `xcodebuild -version` wird nur numerisch ausgewertet (Beta-Builds liefern normale Versionszeilen und passieren ≥ 27.2 stillschweigend) und die IPA-Gate-Prüfung wertet nur `DTSDKName` aus — eine mit Beta-Xcode gebaute `.ipa` enthält ebenfalls `iphoneos27.x` und passiert das Gate (`DTXcodeBuild` wird zwar geloggt, aber nicht ausgewertet). Im CI-Pfad ist die Beta per `xcode-path`-Pin auf `Xcode_27.2.app` ausgeschlossen; im manuellen `store`-/`upload`-Pfad bleibt die Lücke: Ein mit Beta erzeugtes Binary würde hochgeladen und erst von Apples Verarbeitung abgelehnt.

  Empfehlung: Entweder Beta erkennbar machen (z. B. `xcodebuild -version`-Ausgabe auf „beta" prüfen oder `DTXcodeBuild` gegen bekannte Beta-Build-Nummern/`xcode-select -p`-Pfad mit `beta`/`Beta` im App-Namen prüfen) oder die „keine Beta"-Aussage in Kommentar/Doku als Restrisiko kennzeichnen statt sie als durchgesetzte Anforderung zu formulieren.

## Geprüfte Dateien

Quell-/Konfigurationsdateien (vollständig gelesen bzw. vollständiger Diff gegen Merge-Base `a412efa`, inkl. uncommitteter Working-Tree-Änderungen):

- `.github/actions/package-ios/action.yml`
- `.github/workflows/release.yml`
- `.github/workflows/staging-ci.yml`
- `scripts/iOS-Deployment.ps1`

Dokumentation (Diff auf technische Konsistenz mit der Implementierung geprüft, keine Befunde):

- `docs/app-store-review.md`
- `docs/help/ios-deployment/ablauf-technisch.md`
- `docs/help/ios-deployment/installation.md`
- `docs/help/ios-deployment/troubleshooting.md`
- `docs/help/release-management/ablauf-technisch.md`
- `docs/help/release-management/architektur.md`
- `docs/help/release-management/installation.md`
- `docs/help/release-management/troubleshooting.md`
- `scripts/iOS-Deployment.md`

Planungs-/Tracking-Artefakte (kein Quellcode, nur stichprobenartig auf Konsistenz geprüft):

- `docs/features/task/issue-204-f39e1f6e4f8546a593ba6dcd338496b5-nachricht-von-apple-tasks.md`
- `docs/features/task/issue-204-f39e1f6e4f8546a593ba6dcd338496b5-nachricht-von-apple/{inventory.md,plan.md,plan-check.md,plan-check.1.md,requirement.md,todo.md}` sowie `inventory/*.md`

## Verifizierte Schwerpunkte (ohne Befund)

- **Bash-Logik `Select Xcode`/`Verify built SDK version`:** korrektes Quoting; `plutil -extract … raw -o -` korrekt; `ditto -x -k`-Fallback zu `unzip` unter `set -euo pipefail` sicher (`||`-Kette); `find … | head -1` in `$(…)` kann nicht abbrechen, leere `APP` wird abgefangen; `[[ "$SDK_NAME" =~ ^iphoneos([0-9]+) ]]` + `[ "$SDK_MAJOR" -lt 27 ]` ist für leere/nicht-numerische Werte abgesichert (`-z`-Teil greift zuerst, kurzgeschlossen).
- **`DEVELOPER_DIR` via `$GITHUB_ENV`:** greift für alle Folgeschritte des Jobs (Composite-Action-Schritte inklusive), auch für die `xcode-select -p`-Auflösung in `Upload to TestFlight`; das in-step `export` ist nötig, weil `$GITHUB_ENV` erst ab dem Folgeschritt wirkt und `xcodebuild -version` im selben Schritt sonst das Image-Default-Xcode prüfen würde.
- **Gate ohne `api-key-p8`:** `Verify built SDK version` hat kein `if:` und läuft vor dem konditionierten Upload — sinnvoll, da das Artefakt auch ohne TestFlight-Upload via `upload-artifact` weitergereicht wird.
- **`upload`-Pfad im Skript:** `Invoke-Upload` ruft `Invoke-IpaValidation` auf — der SDK-Block (Zeilen 766–780) greift dort ebenso wie bei `store` (Zeilen 845/858). Reihenfolge `Assert-MinimumXcodeVersion` vor `Update-BuildNumber`/`Invoke-Build` ist korrekt (kein Buildnummer-Bump bei abgelehnter Toolchain).
- **Remote-Bash in `Invoke-IpaValidation`:** läuft über `Invoke-OnMac` → `bash` (siehe Zeilen 618–634), `[[ =~ ]]`/`BASH_REMATCH` also zulässig; PowerShell-Here-String-Escaping (`` `$ ``/`` `$( ``) korrekt; `|| true` innerhalb der `$(…)`-Zuweisungen verhindert `set -e`-Abbruch bei fehlenden plist-Keys.
- **`runs-on: xcode-27`:** in beiden Workflows konsistent gesetzt; laut `plan.md` (Offene Punkte #1) GA-Standard-Label für öffentliche Repos — dokumentierte Entscheidung, kein Befund.
