<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

Kontext: Issue #204 / ITMS-90725 — ausschließlich CI-/Build-Artefakte geändert
(`.github/actions/package-ios/action.yml`, `runs-on: xcode-27` in `release.yml`
und `staging-ci.yml`, `scripts/iOS-Deployment.ps1`, Dokumentation). Kein
C#-Code, keine UI. Die ausgeführten Suiten dienen als Regressionstest.

## Ergebnis

**Status:** Keine Fehler

Hinweis: Die automatisierten Suiten sind fehlerfrei durchgelaufen. Die reale
End-to-End-Abnahme der Akzeptanzszenarien ist in dieser Windows-Umgebung
`Nicht durchführbar` (macOS-Runner, Signing-Secrets, App Store Connect nicht
verfügbar) und benötigt gemäß Lifecycle-Definition eine bewusste
Anwender-Entscheidung — siehe Abschnitt `Reale End-to-End-Abnahme`.

## E2E-Abdeckung

Geplante E2E-Szenarien aus `plan.md` / `plan-check.md`: Im Plan ausdrücklich
als **nicht erforderlich** begründet — die Anforderung betrifft keinen über
App-UI oder Nutzeraktion erreichbaren Ablauf (nur CI-Infrastruktur und ein
externes Deployment-CLI-Skript); die FlaUI-Desktop-Suite kann macOS-CI-Artefakte
nicht ausüben. Funktionsnachweis projektüblich über den `package-ios`-Job-Log
und den App-Store-Connect-Verarbeitungsbericht.

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| CI-iOS-Build (`staging-ci`/`release`) bis TestFlight/App Store Connect | Kein klassischer E2E-Test geplant (Funktionsnachweis = Job-Log/Gate-Ausgabe + Store-Verarbeitungsbericht) | Nicht erforderlich (plan.md, Abschnitt „E2E-Tests") |
| Manueller `store`-Pfad (`iOS-Deployment.ps1 -Action store`) | Kein E2E-Test geplant (keine Testinfrastruktur für PS-Deployment-Skript) | Nicht erforderlich (mit Begründung) |
| Manueller `upload`-Pfad (`iOS-Deployment.ps1 -Action upload`) | Kein E2E-Test geplant (keine Testinfrastruktur) | Nicht erforderlich (mit Begründung) |

## Reale End-to-End-Abnahme

Eine reale Abnahme ist grundsätzlich erforderlich (externe Dienste/Integrationen
betroffen: CI-Pipeline, App Store Connect), in dieser Windows-Umgebung jedoch
**nicht durchführbar**: Die Akzeptanzszenarien laufen ausschließlich im echten
`package-ios`-CI-Lauf auf dem macOS-`xcode-27`-Runner (gated auf
`version.changed`/`released` + `IOS_SIGNING_ENABLED`; `pr-staging-ci.yml` hat
keinen iOS-Job) bzw. auf einem verbundenen Build-Mac mit Apple-Signing-Material.
Der nächste reguläre `package-ios`-Lauf bleibt der im Plan vorgesehene
Funktionsnachweis (Umsetzungsschritt 7).

| Szenario | Durchgeführt über | Ergebnis |
|----------|-------------------|----------|
| 1 — Happy Path: RC-Build mit iOS-27-SDK (`package-ios` grün, `DTSDKName=iphoneos27.x`, Store-Verarbeitung ohne ITMS-90725) | Nur im echten `package-ios`-CI-Lauf möglich (macOS-Runner, Signing-Secrets, App Store Connect fehlen lokal) | Nicht durchführbar |
| 2 — Runner ohne passendes Xcode (Fail-fast im Schritt `Select Xcode`) | Nur auf macOS-Runner auslösbar; lokal: Bash-Syntaxprüfung + Code-Review der Bedingung | Nicht durchführbar (Teil-Nachweis lokal, s. u.) |
| 3 — SDK-Regression unterhalb 27 (Gate schlägt vor Upload fehl) | Nur im CI-Lauf mit echter `.ipa`; lokal: Logik-Simulation der Gate-Bedingung | Nicht durchführbar (Teil-Nachweis lokal, s. u.) |
| 4 — `store`-Pfad auf Mac mit Xcode < 27.2 (Abbruch vor Build) | Benötigt macOS-Build-Host bzw. SSH-Ziel; lokal: PS-Syntaxprüfung + Logik-Simulation | Nicht durchführbar (Teil-Nachweis lokal, s. u.) |
| 5 — `upload`-Pfad mit veralteter `.ipa` (`FEHLER:` in `Invoke-IpaValidation`) | Benötigt Build-Mac/SSH für das Remote-Validierungsskript; lokal: identische Gate-Logik simuliert | Nicht durchführbar (Teil-Nachweis lokal, s. u.) |

`Nicht durchführbar` gilt nicht als Erfolg. Auflösung per Anwender-Entscheidung:
entweder manuelle Abnahme beim nächsten regulären `package-ios`-Lauf
(`Bestanden (manuell, Datum)`) oder dokumentierter Verzicht.

**Festgelegter Auflösungsweg (2026-11-23, dokumentiert):** Die Abnahme kann in
dieser Umgebung (Windows, kein macOS-Runner, keine Signing-Secrets, kein
App-Store-Connect-Zugang) grundsätzlich nicht stattfinden — sie ist erst nach
dem Merge im echten CI-Lauf möglich. Die Abnahme wird deshalb **an den nächsten
regulären `package-ios`-Lauf delegiert** (Push auf `staging` mit Versionsänderung
bzw. Release auf `main`). Der neu eingebaute Gate-Schritt `Verify built SDK
version` erzeugt dabei den automatisierten Funktionsnachweis
(`DTSDKName=iphoneos27.x` im Job-Log), zusätzlich schlägt die Pipeline vor dem
Upload fehl, falls das SDK < 27 wäre. Der Anwender trifft die Entscheidung über
den PR-Merge und kann die Abnahme anhand des Gate-Logs und des
App-Store-Connect-Verarbeitungsberichts einsehen — der PR-Text weist
ausdrücklich auf diesen Nachweisweg hin. Damit liegt die Entscheidung beim
Anwender: Merge = Fortsetzung mit Abnahme beim nächsten CI-Lauf.

### Lokale Teil-Nachweise (23.11.2026, Windows, Git Bash/PowerShell)

| Nachweis | Befehl / Methode | Ergebnis |
|----------|------------------|----------|
| YAML-Syntax der geänderten CI-Dateien | `yaml.safe_load` auf `action.yml`, `release.yml`, `staging-ci.yml` | OK — alle drei Dateien parsebar |
| Bash-Syntax der neuen Action-Schritte | `bash -n` auf extrahierte `run:`-Blöcke `Select Xcode` und `Verify built SDK version` (mit `${{…}}`→Platzhalter) | OK |
| PowerShell-Syntax | `[System.Management.Automation.Language.Parser]::ParseFile` auf `scripts/iOS-Deployment.ps1` | OK — keine Parser-Fehler |
| Gate-Logik `Verify built SDK version` | Exakte Regex/`[[ … =~ ^iphoneos([0-9]+) ]]` + `-lt 27`-Bedingung in bash simuliert | `iphoneos27.2`/`iphoneos27` → PASS; `iphoneos26.5`, `iphoneos`, leer, `macosx26.0`, `iphonesimulator27.1` → blockiert |
| Gate-Logik `Assert-RequiredXcodeVersion` (ps1) | Regexe/Versionsvergleich in PowerShell mit Beispiel-`xcodebuild -version`-/`xcode-select -p`-Ausgaben simuliert | `Xcode 27.2` → PASS; `Xcode 26.6` → Abbruch; `Xcode 27.3 beta 2` (Beta-Pfad `Xcode_27.3_beta.app`) → Abbruch; `xcode-select` auf CommandLineTools → Abbruch |

## Sichtprüfung der Abnahme-Screenshots

**Status:** Nicht erforderlich — keine UI-Änderung, keine UI-Szenarien, keine
Abnahme-Screenshots vorhanden.

## Zusammenfassung

Ausgeführte Suiten (Regression, alle nicht von der Änderung betroffen):

| Lauf | Befehl | Gesamt | Bestanden | Fehlgeschlagen | Übersprungen |
|------|--------|--------|-----------|----------------|--------------|
| node:test (Release-Skripte) | `npm test` (`node --test "scripts/*.test.mjs"`) | 36 | 36 | 0 | 0 |
| Reporter.Tests (regular-Lane) | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release --no-build -- --filter-not-trait "Category=OsInterface"` (mit Coverlet) | 801 | 801 | 0 | 0 |
| Reporter.Tests (OsInterface-Lane) | `dotnet test … -- --filter-trait "Category=OsInterface"` | 4 | 4 | 0 | 0 |
| **Gesamt** | | **841** | **841** | **0** | **0** |

Voraussetzung `dotnet test`: `dotnet build Reporter.sln -c Release` mit
`IncludeIosTarget=false`/`IncludeAndroidTarget=false` (wie CI-Job
`build-and-test`) — 0 Warnungen, 0 Fehler. TRX-Nachweise:
`TestResults/test-results-issue204-regular.trx`,
`TestResults/test-results-issue204-osinterface.trx`.

Statische Checks: `.\scripts\Run-StaticChecks.ps1` — **Exit-Code 0**,
„Alle gewaehlten Static-Checks wurden erfolgreich ausgefuehrt."
(Format, Lizenzheader, Security-Scan, Static-Analysis-Build mit
`TreatWarningsAsErrors` — 0 Warnungen, 0 Fehler.)

Nicht ausgeführte Suite: `src/Reporter.E2ETests` (FlaUI-Desktop-Suite) — bekannt
vorbelasteter Ausgangszustand **18 von 39 Fehlschlägen**, dokumentiert in
`inventory/tests.md` (alle mit identischem Fehlerbild
`TimeoutException: Element not found … 'Aktionen'`; Suite ist kein CI-Gate und
kann die geänderten macOS-CI-Artefakte nicht ausüben). Erneuter Lauf
bewusst unterlassen — kein Bezug zur Änderung.

## Testabdeckung

**Abdeckung:** 96,1 % Zeilenabdeckung (Coverlet, Cobertura, regular-Lane;
`[Reporter.*]*`, ohne `Reporter.Data.Migrations.*`; Report:
`TestResults/coverage.cobertura.231126101547874.xml`,
line-rate 0,9607 / branch-rate 0,8613)

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core/Models/CategoryFilterItem.cs` | 0,0 % |
| `Reporter.Core/Models/FeedSearchResult.cs` | 0,0 % |
| `Reporter.Core/Services/FeedSearchUnavailableException.cs` | 33,3 % |
| `Reporter.Core/Services/Atom03NormalizingXmlReader.cs` | 78,4 % |

## Fehlende Tests

Quelle: `Coverage-Daten` (0 %-Dateien) plus Artefakte ohne Testinfrastruktur.

- `Reporter.Core/Models/CategoryFilterItem.cs` — 0 % Abdeckung (einfaches Model; Bestand, nicht von Issue #204 betroffen)
- `Reporter.Core/Models/FeedSearchResult.cs` — 0 % Abdeckung (einfaches Model; Bestand, nicht von Issue #204 betroffen)
- `.github/actions/package-ios/action.yml`, `.github/workflows/{release,staging-ci}.yml`, `scripts/iOS-Deployment.ps1` — keine Testinfrastruktur im Repo für CI-/Skript-Artefakte (im Plan begründet); Nachweisweg = lokale Teil-Nachweise (oben) + echter `package-ios`-Lauf bei der nächsten Versionsänderung auf `staging`/Release
