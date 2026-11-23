<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### `.github/actions/package-ios/action.yml` (Composite Action)

- [x] Input `xcode-path` (string, optional, Default `/Applications/Xcode_27.2.app`) — vorhanden (Zeilen 48–51); Description dokumentiert die Kopplung an `ios-workload-version` (Pack 27.2.x ↔ exakt Xcode 27.2) und das Beta-Verbot
- [x] Schritt `Select Xcode` — vorhanden als erster Schritt vor `Setup .NET` (Zeilen 64–76): Existenzprüfung `[ ! -d ]` mit `::error::`-Abbruch (Fail-fast), `DEVELOPER_DIR=<xcode-path>/Contents/Developer` nach `$GITHUB_ENV` (plus `export` für den Schritt selbst), Versionsprotokoll via `xcodebuild -version` und `xcodebuild -showsdks | grep -i iphoneos`
- [x] Schritt `Verify built SDK version` — vorhanden nach `Collect iOS package`, vor `Upload to TestFlight` (Zeilen 225–251): entpackt `release-ios.ipa` (`ditto -x -k` mit `unzip`-Fallback), liest `DTSDKName`/`DTPlatformVersion`/`DTXcode`/`DTXcodeBuild` per `plutil -extract`, protokolliert alle Werte, bricht mit `::error::` + `exit 1` ab, wenn `DTSDKName` nicht `iphoneos` mit Major ≥ 27 ist
- [x] Kommentare zur Kopplung `ios-workload-version` ↔ `xcode-path` — vorhanden in der Input-Description (Zeile 45) und im Kommentarblock über `Select Xcode` (Zeilen 56–63)

### Workflows

- [x] `runs-on: xcode-27` im Job `package-ios` in `.github/workflows/staging-ci.yml` — vorhanden (Zeile ~350) inkl. Kommentar zur Runner-/Pack-Kopplung
- [x] `runs-on: xcode-27` im Job `package-ios` in `.github/workflows/release.yml` — vorhanden (Zeile ~153) inkl. Kommentar; Aufruf-Signatur der Action unverändert (neuer Input nutzt Default)

### `scripts/iOS-Deployment.ps1`

- [x] Funktion `Assert-MinimumXcodeVersion` — vorhanden (Zeilen 518–547): `xcodebuild -version` auf dem Build-Mac via `Invoke-MacCapture` (lokal auf macOS via `bash`, unter Windows per SSH), Versionsvergleich gegen Minimum `27.2`, Abbruch mit verständlicher Meldung inkl. Verweis auf Workload-Pin `27.2.11039`, Apple-Anforderung und `xcode-select`-Abhilfe; nicht ermittelbare/nicht auswertbare Version → Warnung statt Abbruch
- [x] Aufruf in `Invoke-Store` — vorhanden (Zeile 838), exakt in der geplanten Reihenfolge: `Assert-StorePrerequisites` → `Assert-CodesigningForAction` → `Assert-TransporterAvailable` → `Assert-MinimumXcodeVersion` → `Update-BuildNumber` → `Invoke-Build` (Zeilen 835–840)
- [x] SDK-Metadaten-Prüfblock in `Invoke-IpaValidation` — vorhanden (Zeilen 766–780), positioniert nach den `Info.plist`-Invarianten und vor dem App-Icon-Check: liest/protokolliert `DTSDKName`/`DTPlatformVersion`/`DTXcode`, `FEHLER:` + `exit 1` wenn `DTSDKName` nicht `iphoneos` ≥ 27; deckt `store` (Zeile 845) und `upload` (Zeile 858) ab; `Invoke-Upload` erhält bewusst keine eigene Xcode-Prüfung (kein Build) — plangemäß

### Dokumentation

- [x] `docs/app-store-review.md` — neuer Abschnitt „Build-SDK-Anforderung (ITMS-90725)" (Anforderung, Frist April 2027, Umsetzung über Runner-Label/Xcode-Auswahl/SDK-Gate/manuellen Pfad, Nachweis)
- [x] `docs/help/release-management/architektur.md` — `xcode-27`-Label, `DEVELOPER_DIR`/`xcode-path`, SDK-Gate in Komponenten- und Abhängigkeitstabellen
- [x] `docs/help/release-management/installation.md` — `xcode-27` als einziges Image mit Xcode 27.x für `package-ios` dokumentiert
- [x] `docs/help/release-management/ablauf-technisch.md` — `package-ios`-Jobbeschreibung um `Select Xcode`/`Verify built SDK version` erweitert; Fehlerfall-Liste um die drei neuen Abbruchpfade ergänzt
- [x] `docs/help/release-management/troubleshooting.md` — neuer Abschnitt „requires Xcode 27.2"/SDK-Gate (alle drei Abbruchpunkte mit Ursache/Lösung) plus Eintrag zur bewussten Einschränkung „iOS-Repair alter Release-Stände" (Offener Punkt #3, entschieden: hinnehmen und dokumentieren)
- [x] `docs/help/ios-deployment/installation.md` — Xcode ≥ 27.2 (Release, keine Beta) als Voraussetzung; veraltete Angabe „.NET SDK 10.x" auf `11.x` korrigiert
- [x] `docs/help/ios-deployment/ablauf-technisch.md` — `Assert-MinimumXcodeVersion` in der `store`-Voraussetzungsliste; `Invoke-IpaValidation`-Prüfliste um den SDK-Metadaten-Prüfblock erweitert und entsprechend der Skriptreihenfolge auf 1–8 umnummeriert (neuer Punkt 6)
- [x] `docs/help/ios-deployment/troubleshooting.md` — zwei neue Einträge: `Assert-MinimumXcodeVersion`-Abbruch „Xcode ≥ 27.2 erforderlich" vor dem Build und `FEHLER:`-Ausgang der SDK-Validierung im `store`/`upload`-Pfad
- [x] `scripts/iOS-Deployment.md` — Xcode-Mindestversion ≥ 27.2 in den Voraussetzungen; Buildnummer-Formel von `github.run_number` auf `git rev-list --count HEAD + github.run_attempt - 1` korrigiert

### Verifikation

- [x] Runner-Label-Entscheidung `xcode-27` — als Offener Punkt #1 im Plan geklärt (GA-Standard-Label für public-Repos, keine Larger-Runner-Abrechnung; Image enthält Xcode 27.0/27.1/27.2)
- [x] `.\scripts\Run-StaticChecks.ps1` — im Rahmen dieses Reviews ausgeführt: **Exit-Code 0**, Format/Lizenzheader/Security (NuGet + npm, Schwelle `high`)/Static-Analysis-Build alle ohne Findings

## Hinweise

- **Task 18 bleibt plan- und designbedingt offen** (kein Umsetzungsdefizit): Der `package-ios`-Lauf lässt sich vor dem Merge nicht ausführen — `pr-staging-ci.yml` hat keinen iOS-Job und `package-ios` in `staging-ci.yml` ist an `version.changed` gegated. Der Nachweis (`Select Xcode` = Xcode 27.2, `Publish` erfolgreich, Gate-Log `DTSDKName=iphoneos27.x`, `iTMSTransporter`-Fund, Upload ohne ITMS-90725) entsteht mit der nächsten Versionsänderung auf `staging` bzw. einem Release auf `main`.
- Keine automatisierten Tests möglich/planmäßig vorgesehen (kein Test-Framework für `.github/`-Artefakte und `iOS-Deployment.ps1` im Repo); Vorab-Nachweise in diesem Review: YAML-Parse der drei Workflow-/Action-Dateien erfolgreich, PowerShell-AST-Parse von `iOS-Deployment.ps1` fehlerfrei, `Run-StaticChecks.ps1` Exit 0.
- Die `[[ =~ ]]`-Bash-Syntax im neuen `Invoke-IpaValidation`-Block ist abgedeckt: `Invoke-OnMac`/`Invoke-MacCapture` führen Remote-Skripte stets über `bash` aus (Zeilen 186/188, 628/632).
- Verbleibende `macos-latest`-Nennungen im Repo sind ausschließlich historische Kontextdokumente (`docs/help/entwicklung/dotnet-11-migration.md`, `changes.log`, Feature-/Inventory-Dokumente) — nicht Gegenstand des Plans.
- `package-android`-Fehlschlag (`NETSDK1094`, Run 45117817038) ist laut Plan separater Befund außerhalb dieses Scopes; der `prerelease`-Aggregationsjob bleibt bis zu dessen Klärung rot.
