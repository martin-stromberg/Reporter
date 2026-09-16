<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Lokaler iOS-Buildlauf (Issue #74)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Sicherheit | `.gitignore` um `*.p12`, `*.p8`, `*.cer`, `*.mobileprovision`, `*.ipa`, `logs/` ergänzen | Erledigt | Kein direkter Test (`git check-ignore` manuell prüfbar) |
| 2 | Skript-Parameter | `iOS-Deployment.ps1`: Parameter `ApiKeyPath`, `ApiKeyId`, `ApiIssuerId`, `IpaPath`, `NoBumpBuildNumber`, `LogDir` + Env-Fallbacks ergänzen | Erledigt | Manueller Aufruf `-Action upload` listet fehlende Pflichtparameter |
| 3 | Skript-Funktionen | `Assert-StorePrerequisites` (Pflichtparameter + Distribution-Key-Check + p8-Existenz außerhalb Repo) | Erledigt | Manuell verifiziert: fehlende Params, Dev-Zertifikat, fehlende/nicht-.p8-Datei werden abgelehnt |
| 4 | Skript-Funktionen | `Get-LatestIpa` (genau eine `.ipa` unter `bin/<config>/net10.0-ios/ios-arm64` finden) | Erledigt | Kein direkter Test (kein Mac/IPA auf Windows); Pfadlogik geprüft |
| 5 | Skript-Funktionen | `Update-BuildNumber` (`ApplicationVersion` im csproj inkrementieren, automatisch bei `store`, Opt-out `-NoBumpBuildNumber`) | Erledigt | Kein direkter Test; Regex/Encoding manuell geprüft |
| 6 | Skript-Funktionen | `Invoke-OnMac`/`Invoke-OnMacCapture` (lokal auf macOS bzw. SSH-Delegation von Windows mit Befehlsausgabe-Fallback) | Erledigt | Parse-Check; SSH-Pfad nur auf echter Verbindung verifizierbar |
| 7 | Skript-Funktionen | `Copy-ApiKeyToMac` (`.p8` nach `$HOME/.appstoreconnect/private_keys/AuthKey_<id>.p8`) | Erledigt | Kein direkter Test auf Windows; Existenz-Check per Remote-`test -f` |
| 8 | Skript-Aktionen | `Invoke-IpaValidation` (`codesign --verify`, `get-task-allow`-Check, `altool --validate-app`) | Erledigt | Parse-Check; echte Ausführung erfordert Mac + signierte IPA |
| 9 | Skript-Aktionen | `Invoke-StoreUpload` (`altool --upload-app` mit `--apiKey`/`--apiIssuer`) | Erledigt | Parse-Check; echte Ausführung erfordert Mac + Credentials |
| 10 | Skript-Aktionen | `ValidateSet`/Menü/Switch um `store` und `upload` erweitern; `Assert-CodesigningForAction` anpassen | Erledigt | Manueller Aufruf `-Action upload` greift in die Validierung |
| 11 | Logging | Transcript-Logging nach `logs/ios-deploy-<ts>.log` für `build`/`device`/`store`/`upload` (+`simulator`) | Erledigt | Log-Datei wurde bei Testaufrufen angelegt (`logs/ios-deploy-*.log`) |
| 12 | Skript-Doku | `iOS-Deployment.md` um Store-/Upload-Kapitel, `.p8`-Ablage, SSH-Grenzen, altool-Status ergänzen | Erledigt | Dokument eingesehen |
| 13 | Doku | `docs/help/ios-deployment/` anlegen (index, beschreibung, einrichtung-anwender, ablauf-anwender, ablauf-technisch, installation, troubleshooting) inkl. Schritt-für-Schritt App-Store-/TestFlight-Anleitung | Erledigt | Dokumente eingesehen; in `docs/help/index.md` verlinkt |
| 14 | Verifikation | PowerShell-Parse-Check des Skripts + `Run-StaticChecks.ps1` grün | Erledigt | `SYNTAX OK`; Static Checks alle bestanden (Format, Lizenzheader, Security, Static Analysis) |
