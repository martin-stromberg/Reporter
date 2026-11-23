<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme — Issue #204 „Nachricht von Apple" (ITMS-90725)

Anforderung: [requirement.md](requirement.md) — Apple warnt für das zuletzt
gelieferte iOS-Binary (Feed Reporter, Version 0.8.0, Build 331): mit iOS-26.5-SDK
gebaut; ab April 2027 muss ein Upload mit iOS SDK ≥ 27 (Xcode ≥ 27,
Release-Version) erstellt sein.

Dieses Dokument beschreibt ausschließlich den **Ist-Zustand** des Bestands
(Code, Build-Infrastruktur, CI-Läufe, Test-Basis). Es enthält keine Planung und
keine Implementierungsvorschläge.

## Stand der Untersuchung

- Untersuchungszeitpunkt: 23.11.2026, ~09:00–11:30 Uhr lokaler Zeit (UTC+1)
- Branch: `task/issue-204-f39e1f6e4f8546a593ba6dcd338496b5-nachricht-von-apple`
- HEAD: `a412efa4d390df9f44fe6206a0e0299ed79c5a6e` („Migration auf .NET 11 /
  MAUI 11 / EF Core 11 (Closes #202) (#208)") — identisch mit dem Stand, der
  zuletzt auf `staging` CI-lief (Run 45117817038)
- Working Tree: keine Änderungen an getrackten Dateien; nur die untracked
  Feature-Dokumente unter `docs/features/task/issue-204-…/`
- Lokale Werkzeuge: .NET SDK `11.0.100` (global.json-Pin), Workloads u. a.
  `ios 27.2.11039/11.0.100`, Node.js `v24.15.0`

## Kernergebnisse (Zusammenfassung der Detail-Dokumente)

1. **Apple-Befund ist reproduziert und vollständig erklärt.** Build 331 stammt
   aus dem `staging-ci`-Lauf 44460132570 auf Commit `46e47b2` (Tag
   `v0.8.0-rc.1`, `git rev-list --count` = 331 = `CFBundleVersion`). Dieser
   Stand lag **vor** der .NET-11-Migration: `ios-workload-version`
   `26.5.10318`, SDK-Band `10.0.1xx`, TFM `net10.0-ios` → Build gegen
   `iphoneos26.5` → exakt Apples „built with the iOS 26.5 SDK". Der
   TestFlight-Upload selbst lief erfolgreich durch.
   Details: [inventory/ci-runs.md](inventory/ci-runs.md).

2. **Der Workload-Pin ist bereits auf iOS-27-Bindings angehoben** —
   `.github/actions/package-ios/action.yml` Default `ios-workload-version:
   '27.2.11039'` (seit Migrations-Commit `9a636b0`). Das Pack verlangt jedoch
   **exakt Xcode 27.2**.
   Details: [inventory/ci-build-pipeline.md](inventory/ci-build-pipeline.md).

3. **Aktueller CI-Zustand: kein auslieferbares iOS-Binary.** Der erste
   `package-ios`-Lauf nach der Migration (Run 45117817038, 23.11.2026)
   scheiterte im `Publish (ios)`-Schritt:
   `This version of .NET for iOS (27.2.11039) requires Xcode 27.2. The current
   version of Xcode is 26.6.` Ursache: beide iOS-Jobs laufen mit
   `runs-on: macos-latest` = Image `macos-26-arm64` (Version
   `20261117.0516.1`); dort ist Xcode 26.6 Default, kein Xcode 27 installiert.
   Details: [inventory/ci-runs.md](inventory/ci-runs.md).

4. **Keine Xcode-Auswahl und kein SDK-Gate im Bestand.** Weder die Action noch
   die Workflows enthalten `xcode-select -s`, `DEVELOPER_DIR`,
   `xcodebuild -version` oder eine Prüfung von `DTSDKName`/`DTPlatformVersion`/
   `DTXcode` in der erzeugten `.ipa`. `Collect iOS package` prüft nur
   Existenz/Eindeutigkeit der Datei. Extern belegte Alternative: Runner-Images
   `xcode-27`/`xcode-27-xlarge` (GA seit 15.10.2026) enthalten Xcode
   27.0/27.1(default)/27.2.
   Details: [inventory/ci-build-pipeline.md](inventory/ci-build-pipeline.md).

5. **Manueller Pfad ohne Versionsabsicherung.** `scripts/iOS-Deployment.ps1`
   baut `net11.0-ios` auf einem Mac (direkt oder Pair-to-Mac) mit dem dort per
   `xcode-select` aktiven Xcode; die IPA-Validierung prüft Signatur, Entitlements
   und `Info.plist`-Invarianten, aber keine SDK-/Xcode-Metadaten. Die
   zugehörige Dokumentation enthält keine Xcode-Mindestanforderung;
   vereinzelte Stellen sind noch auf .NET 10 / alte Buildnummer-Formel
   veraltet.
   Details: [inventory/manual-deployment.md](inventory/manual-deployment.md).

6. **Verifikation im PR-Gate nicht möglich.** `pr-staging-ci.yml` hat keinen
   iOS-Job; `package-ios` läuft nur auf `staging` (Gate
   `version.changed && IOS_SIGNING_ENABLED`) bzw. `main`-Release. Ein
   iOS-Pipeline-Fehlschlag blockiert zudem den `prerelease`-Aggregationsjob.

7. **Test-Basis dokumentiert.** Unit-Suites grün: `npm test` 36/36,
   `Reporter.Tests` 801 + 4 OsInterface, alle Exit 0. E2E-Suite (FlaUI/UIA3,
   nicht CI-Gate): 21/39 bestanden, 18 Fehlschläge mit identischem
   `TimeoutException`-Muster (`Element 'Aktionen'` nicht gefunden),
   Exit-Code 2 — dokumentiert als vorhandener Ausgangszustand.
   Details: [inventory/tests.md](inventory/tests.md).

## Betroffene Artefakte im Bestand (Anforderung ↔ Ist-Zustand)

| Artefakt | Rolle | Relevanter Ist-Zustand |
|----------|-------|------------------------|
| `.github/actions/package-ios/action.yml` | Composite Action für iOS-Build + Upload | Workload-Pin `27.2.11039`, SDK-Band `11.0.1xx` + temporäre `global.json` `11.0.100`; Publish `net11.0-ios`/`ios-arm64`; IPA-Eindeutigkeits-Check; iTMSTransporter-Upload bei `api-key-p8`. **Keine** Xcode-Auswahl, **kein** SDK-Metadata-Gate |
| `.github/workflows/release.yml` | Release-Pipeline | Job `package-ios` Zeilen 144–184: `macos-latest`, Gate `released && IOS_SIGNING_ENABLED`, Action ohne Workload-Override |
| `.github/workflows/staging-ci.yml` | RC-Pipeline | Job `package-ios` Zeilen 341–374: `macos-latest`, Gate `version.changed && IOS_SIGNING_ENABLED`, identischer Action-Aufruf |
| `.github/workflows/pr-staging-ci.yml` | PR-Gate | Kein iOS-Job |
| `scripts/iOS-Deployment.ps1` | Manueller Build/Upload | Baut/publisht `net11.0-ios` mit Mac-seitigem Xcode; `Invoke-IpaValidation` ohne SDK-Prüfung; keine Xcode-Mindestversion |
| `src/Reporter/Reporter.csproj` | App-Projekt | `net11.0-ios` auf macOS (Zeile 13); `SupportedOSPlatformVersion` iOS `15.0` (Zeile 45) — bleibt laut Anforderung unverändert |
| `global.json` | SDK-Pin | `11.0.100`/`latestPatch`, MTP-Testrunner |
| `dotnet-tools.json` | Tool-Pins | Nur `dotnet-ef` 11.0.0 — iOS-irrelevant |
| `docs/help/ios-deployment/*`, `docs/help/release-management/*`, `docs/app-store-review.md` | Doku | Kein ITMS-90725-/SDK-Eintrag; `macos-latest` für `package-ios` dokumentiert; keine Xcode-Anforderung |
| Test-Assets | CI-Logs + Testberichte | [inventory/](inventory/) — vollständige Job-Logs beider Nachweis-Runs und alle Test-Ergebnisse |

## Offene Fragen der Anforderung — belegte Antworten im Bestand

| Frage aus `requirement.md` | Belegter Ist-Befund |
|----------------------------|---------------------|
| 1. Build 331 aus .NET-10-Stand oder trotz Pin 27.x mit Xcode < 27? | Aus .NET-10-Stand (`26.5.10318`, `net10.0-ios`), siehe ci-runs.md Run 44460132570 |
| 2. Welches Xcode ist auf `macos-latest` Default? | `macos-26-arm64`, Image `20261117.0516.1`: Xcode 26.6 Default, kein Xcode 27 (Fehlermeldung + Runner-Image-Doku) |
| 3. Existiert ein SDK-Gate? | Nein — weder in der Action noch in `iOS-Deployment.ps1` |
| 4. Upload zur Verifikation | Bestandsfeststellung: letzter Upload (Build 331) erfolgreich; aktuell erzeugt CI keine `.ipa` mehr |
| 5. Manueller Pfad betroffen? | Ja — Skript/Doku kennen keine Xcode-Mindestversion; Ergebnis hängt vom Mac ab |

## Detail-Dokumente

- [inventory/ci-build-pipeline.md](inventory/ci-build-pipeline.md) — Composite
  Action, beide Workflows, `global.json`, csproj-Referenzen, externe
  Runner-Image-Fakten
- [inventory/ci-runs.md](inventory/ci-runs.md) — `gh`-Nachweise: historischer
  Erfolgslauf (Build 331, iOS 26.5) und aktueller Fehlschlag (Xcode 26.6 vs.
  Pack 27.2.11039); vollständige Job-Logs:
  [ci-run-44460132570-package-ios.log](inventory/ci-run-44460132570-package-ios.log),
  [ci-run-45117817038-package-ios.log](inventory/ci-run-45117817038-package-ios.log)
- [inventory/manual-deployment.md](inventory/manual-deployment.md) —
  `iOS-Deployment.ps1`-Funktionsanalyse und Dokumentationsstand
- [inventory/tests.md](inventory/tests.md) — Test-Ausgangszustand,
  Umgebung, exakte Befehle, Exit-Codes, Zählungen, alle E2E-Fehlschläge;
  Berichte unter [inventory/test-results/](inventory/test-results/)
