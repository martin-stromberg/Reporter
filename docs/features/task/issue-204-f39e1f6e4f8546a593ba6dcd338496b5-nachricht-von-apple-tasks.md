<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Issue #204 „Nachricht von Apple" (ITMS-90725 — iOS-Build auf SDK ≥ 27)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | CI-Infrastruktur | Verfügbarkeit/Abrechnung des Runner-Labels `xcode-27` für das Repo verifizieren (`gh api`/`gh run view` bzw. erster Testlauf) | Offen | — |
| 2 | CI-Infrastruktur | Neuen Input `xcode-path` (Default `/Applications/Xcode_27.2.app`) in `.github/actions/package-ios/action.yml` definieren | Offen | — |
| 3 | CI-Infrastruktur | Schritt `Select Xcode` als ersten Schritt in `package-ios/action.yml` einfügen (Existenzprüfung, `DEVELOPER_DIR` via `$GITHUB_ENV`, `xcodebuild -version`-/`-showsdks`-Log, `::error::`-Abbruch) | Offen | — |
| 4 | CI-Infrastruktur | Schritt `Verify built SDK version` nach `Collect iOS package` in `package-ios/action.yml` einfügen (`.ipa` entpacken, `DTSDKName`/`DTPlatformVersion`/`DTXcode` per `plutil` lesen + loggen, Abbruch wenn `iphoneos` < 27) | Offen | — |
| 5 | CI-Infrastruktur | `runs-on` im Job `package-ios` in `.github/workflows/staging-ci.yml` von `macos-latest` auf `xcode-27` ändern | Offen | — |
| 6 | CI-Infrastruktur | `runs-on` im Job `package-ios` in `.github/workflows/release.yml` von `macos-latest` auf `xcode-27` ändern | Offen | — |
| 7 | CI-Infrastruktur | Kommentare zur Kopplung `ios-workload-version` ↔ `xcode-path` in `package-ios/action.yml` ergänzen | Offen | — |
| 8 | Skript | Funktion `Assert-MinimumXcodeVersion` in `scripts/iOS-Deployment.ps1` implementieren (`xcodebuild -version` via `Invoke-MacCapture`, Minimum 27.2, verständliche Fehlermeldung, Warnung bei nicht ermittelbarer Version) | Offen | — |
| 9 | Skript | `Assert-MinimumXcodeVersion` in `Invoke-Store` nach `Assert-TransporterAvailable` und vor `Update-BuildNumber`/`Invoke-Build` aufrufen | Offen | — |
| 10 | Skript | SDK-Metadaten-Prüfblock (`DTSDKName` ≥ `iphoneos27` → `FEHLER:`/`exit 1`; `DTPlatformVersion`/`DTXcode` loggen) in `Invoke-IpaValidation` ergänzen | Offen | — |
| 11 | Dokumentation | ITMS-90725-Abschnitt in `docs/app-store-review.md` ergänzen (Anforderung, Frist April 2027, Umsetzung über Runner/Pin/Gate) | Offen | — |
| 12 | Dokumentation | `docs/help/release-management/` (`architektur.md`, `installation.md`, `ablauf-technisch.md`, `troubleshooting.md`) auf `xcode-27`-Label, `xcode-path`/Xcode-Auswahl und SDK-Gate aktualisieren; Einschränkung für iOS-Repair alter Stände dokumentieren | Offen | — |
| 13 | Dokumentation | `docs/help/ios-deployment/installation.md`: Xcode-Mindestversion ≥ 27.2 ergänzen und veraltete Angabe „.NET SDK 10.x" korrigieren | Offen | — |
| 14 | Dokumentation | `scripts/iOS-Deployment.md`: Xcode-Mindestversion dokumentieren und veraltete Buildnummer-Formel (`github.run_number` → `git rev-list --count + run_attempt - 1`) korrigieren | Offen | — |
| 15 | Dokumentation | `docs/help/ios-deployment/ablauf-technisch.md` aktualisieren: `Assert-MinimumXcodeVersion` in der `store`-Voraussetzungsliste dokumentieren und die `Invoke-IpaValidation`-Prüfliste (1–7) um den SDK-Metadaten-Prüfblock (`DTSDKName`/`DTPlatformVersion`/`DTXcode`, `exit 1` bei < `iphoneos27`) erweitern | Offen | — |
| 16 | Dokumentation | `docs/help/ios-deployment/troubleshooting.md`: Einträge für den `Assert-MinimumXcodeVersion`-Abbruch („Xcode ≥ 27.2 erforderlich" vor dem Build) und den `FEHLER:`-Ausgang der SDK-Validierung im `store`/`upload`-Pfad ergänzen | Offen | — |
| 17 | Verifikation | `.\scripts\Run-StaticChecks.ps1` ausführen (Exit-Code 0, keine Findings) | Offen | — |
| 18 | Verifikation | `package-ios`-Lauf auf `staging` (bei Versionsänderung) bzw. `main`-Release verifizieren: `Select Xcode` = Xcode 27.2, `Publish (ios)` erfolgreich, Gate-Log `DTSDKName=iphoneos27.x`, `iTMSTransporter` unter `DEVELOPER_DIR`/Kandidatenliste gefunden, Upload ohne ITMS-90725 | Offen | — |
