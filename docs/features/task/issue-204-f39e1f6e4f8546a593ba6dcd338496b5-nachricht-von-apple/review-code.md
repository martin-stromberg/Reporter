<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

Iteration 3 — alle vier Befunde aus `review-code.2.md` sind umgesetzt (Umbenennung zu `Assert-RequiredXcodeVersion` inkl. aller Namensreferenzen in Quelle und Doku; harter Abbruch bei Nicht-Xcode-`xcode-select`-Pfad; `$requiredXcodeVersion`/`$workloadPin` als zentrale Konstanten inkl. bereinigtem Doku-Vertrag in `troubleshooting.md` Schritt 3; Abhilfe-Block in `$remedy` dedupliziert). Beim vollständigen Durchgang mit frischem Blick ist ein neuer Befund auf der CI-Seite entstanden.

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### .github/actions/package-ios/action.yml (Select Xcode)

- **Fehlerbehandlung / feststellbare Fehlkonfiguration läuft in den Mid-Build-Fehler statt in den Fail-fast** — Zeilen 65–81: Der Schritt validiert nur, dass `xcode-path` existiert und `Contents/Developer` enthält; `xcodebuild -version` (Zeile 79) wird nur geloggt, nicht ausgewertet. Auf dem `xcode-27`-Image existieren aber weitere gültige Pfade, die beide Checks passieren: `Xcode_27.0.app`, `Xcode_27.1.app` bzw. der Symlink `/Applications/Xcode.app` (Image-Default 27.1) und seit Image-Version 20261119 `Xcode_27.3_beta.app`. Zwei konkrete Fehlbilder bleiben damit unentdeckt: (a) `xcode-path` zeigt auf Xcode ≠ 27.2 bei Default-Pin `27.2.11039` → der Job bricht erst mitten im `Publish (ios)` mit `requires Xcode 27.2` ab — genau der Fehlermodus, den das Feature per Fail-fast verhindern soll und den `Assert-RequiredXcodeVersion` im lokalen `store`-Pfad jetzt vorab abfängt; (b) `xcode-path` zeigt auf eine `Xcode_*_beta.app` → der Beta-Build passiert sogar das SDK-Gate (`iphoneos27.x ≥ 27` ist für Beta-Builds erfüllt, dokumentierte Grenze) und wird zu TestFlight hochgeladen → serverseitige Ablehnung. Die Kopplung `xcode-path` ↔ `ios-workload-version` ist nur in den Input-Beschreibungen dokumentiert, nicht erzwungen.

  Empfehlung: In `Select Xcode` die `xcodebuild -version`-Ausgabe auswerten — (a) `beta` (case-insensitive) in Versionszeile oder App-Pfad mit `::error::` ablehnen (analog `Assert-RequiredXcodeVersion`), (b) Major.Minor der Xcode-Version gegen Major.Minor von `inputs.ios-workload-version` vergleichen (Pack `27.2.x` ↔ Xcode `27.2`, z. B. `cut -d. -f1-2`) und bei Abweichung mit `::error::` auf die gekoppelten Inputs abbrechen.

## Geprüfte Dateien

Quell-/Konfigurationsdateien (vollständig gelesen; Diff gegen Merge-Base `a412efa`, inkl. uncommitteter Working-Tree-Änderungen):

- `.github/actions/package-ios/action.yml` (Befund oben)
- `.github/workflows/release.yml`
- `.github/workflows/staging-ci.yml`
- `scripts/iOS-Deployment.ps1` (PowerShell-AST-Parse: fehlerfrei)

Dokumentation (Diff auf technische Konsistenz mit der Implementierung geprüft):

- `docs/app-store-review.md`
- `docs/help/ios-deployment/ablauf-technisch.md`
- `docs/help/ios-deployment/installation.md`
- `docs/help/ios-deployment/troubleshooting.md`
- `docs/help/release-management/ablauf-technisch.md`
- `docs/help/release-management/architektur.md`
- `docs/help/release-management/installation.md`
- `docs/help/release-management/troubleshooting.md`
- `scripts/iOS-Deployment.md`

Planungs-/Tracking-Artefakte (kein Quellcode, nur stichprobenartig auf Konsistenz geprüft; Namensreferenzen dort absichtlich unverändert):

- `docs/features/task/issue-204-f39e1f6e4f8546a593ba6dcd338496b5-nachricht-von-apple-tasks.md`
- `docs/features/task/issue-204-f39e1f6e4f8546a593ba6dcd338496b5-nachricht-von-apple/{inventory.md,plan.md,plan-check.md,plan-check.1.md,requirement.md,todo.md,review.md,review-usability.md}` sowie `inventory/*.md`

## Verifizierte Schwerpunkte (ohne Befund)

- **Fix-Verifikation Iteration 2, Befund 1 (Umbenennung):** Funktion heißt `Assert-RequiredXcodeVersion` (`iOS-Deployment.ps1` Zeile 518), Aufrufstelle `Invoke-Store` Zeile 887, Kommentarreferenz Zeile 818; alle Produktions-Doku-Referenzen umgestellt (`ablauf-technisch.md` Zeile 15, `troubleshooting.md` Zeilen 91/102/113, `app-store-review.md` Zeile ~165, `iOS-Deployment.md` Zeile 59). Verbleibende `Assert-MinimumXcodeVersion`-Treffer liegen ausschließlich in Tracking-Artefakten unter `docs/features/task/` — absichtlich unverändert.
- **Fix-Verifikation Iteration 2, Befund 2 (harter Abbruch bei Nicht-Xcode-Pfad):** Zeilen 535–549 — `$selectPath` aus `xcode-select -p`-Zeile (`^\s*/`), Abbruch mit `exit 1` wenn nicht `\.app/Contents/Developer$`; CLT-Pfad `/Library/Developer/CommandLineTools` wird deterministisch abgelehnt, bevor `store` in den impliziten Mid-Build-Fehler läuft. Warnpfad (Zeilen 550–553) bleibt nur für nicht ermittelbare Version bei gültigem Xcode-Pfad — korrekt, da leere Remote-Ausgabe (z. B. SSH-Problem vs. kein Developer-Dir) nicht unterscheidbar ist.
- **Fix-Verifikation Iteration 2, Befund 3 (Konstanten + Doku-Vertrag):** `$requiredXcodeVersion`/`$workloadPin` zentral am Funktionsanfang (Zeilen 527–528); `troubleshooting.md` Schritt 3 verlangt jetzt die Skript-Anpassung (`$requiredXcodeVersion` setzen) und benennt den fehlenden Override-Schalter — der dokumentierte Ausweg ist jetzt fachlich gültig statt zirkulär.
- **Fix-Verifikation Iteration 2, Befund 4 (Deduplikation):** `$remedy`-Array (Zeilen 529–532) wird in allen drei Abbruchpfaden wiederverwendet (Zeilen 547, 565, 586) — kein doppelter Block mehr.
- **PowerShell-Semantik:** `[version]'27.2'`-Vergleich mit `-ne` trifft ältere und neuere Versionen inkl. Patch-Abweichungen; getrennte `<`/`>`-Meldungen sachlich korrekt; Beta-Erkennung über `-match 'beta'` (case-insensitive) auf `xcodebuild`-Ausgabe und `xcode-select -p`-Pfad; unerreichbarer `else`-Zweig (Zeilen 571–574) weiterhin tolerierbare Defensive.
- **Remote-Bash-Block `Invoke-IpaValidation` (Zeilen 819–829):** Here-String-Escaping (`` `$ ``, `` `${ ``) konsistent; `[[ =~ ]]`/`BASH_REMATCH` korrekt (via `Invoke-OnMac` → bash); `-z` greift vor `-lt`; `|| true` innerhalb `$(…)` unter `set -e` sicher.
- **`Select Xcode`/`Verify built SDK version` (action.yml):** `DEVELOPER_DIR` via `$GITHUB_ENV` (Folgeschritte) plus in-step `export`; `ditto`→`unzip`-Fallback, `trap`-Cleanup, `BASH_REMATCH`/`pipefail`-Absicherung korrekt; `xcode-select -p` im Upload-Schritt (Zeile 279) honoriert `DEVELOPER_DIR`.
- **Runner-Label `xcode-27`:** Verifiziert gegen GitHub-Changelog/runner-images — GA-Label (arm64), Image enthält Xcode 27.0/27.1 (Default)/27.2 unter `/Applications/Xcode_27.2.app` mit SDK `iphoneos27.2`; passt exakt zum Pin `27.2.11039`; in beiden Workflows identisch gesetzt.
- **Aufrufreihenfolge `Invoke-Store` (Zeilen 884–889):** `Assert-RequiredXcodeVersion` vor `Update-BuildNumber`/`Invoke-Build` — kein Buildnummer-Bump bei abgelehnter Toolchain; `Invoke-Upload` prüft zu Recht nicht (kein Build), wird über den SDK-Block in `Invoke-IpaValidation` abgesichert.
- **Doku-Konsistenz:** `installation.md` (Xcode 27.2-Release-Anforderung + SDK-11-Korrektur), `iOS-Deployment.md` (Buildnummer `git rev-list --count HEAD + run_attempt - 1` deckt sich mit action.yml Zeile 182), release-management-Doku (Runner-Label, Gate-Schritte, Repair-Einschränkung alter Stände) — jeweils mit der Implementierung deckungsgleich.
