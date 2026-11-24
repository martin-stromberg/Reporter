<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung — Issue #214 „MAUI-Workload-Update: Microsoft.Maui.Controls 11.0.0 → 11.0.10"

## Fachliche Zusammenfassung

Die wöchentliche Paketprüfung (`dotnet list package --outdated`, Issue #213) meldet `Microsoft.Maui.Controls` 11.0.0 → 11.0.10 in `src/Reporter/Reporter.csproj`. Die Version wird über `Version="$(MauiVersion)"` aus dem installierten .NET-MAUI-Workload des SDK-Bands 11.0.1xx aufgelöst — es handelt sich um ein Workload-/Toolchain-Update, nicht um einen Paket-Bump. Der `maui-windows`-Workload ist auf den Dev-Umgebungen und (implizit) in der CI auf einen Stand ≥ 11.0.10 zu bringen, ohne den bewusst gepinnten iOS-Workload-Stand `27.2.11039` (Xcode-27.2-Kopplung aus Issue #204) zu gefährden. Zusätzlicher verbindlicher Auftrag aus dem Issue-Kommentar: Nach der Änderung muss die Pre-Release-Pipeline (`staging-ci.yml` inkl. `prerelease`-Job) weiterhin funktionieren.

## Fachlicher Scope

- **Verarbeitungseinheit:** Der installierte .NET-Workload-Stand des SDK-Bands 11.0.1xx — konkret das `maui-windows`-Manifest/-Pack, aus dem `$(MauiVersion)` die `Microsoft.Maui.Controls`-Version beim Restore/Build auflöst. Zum Scope gehören alle Stellen, an denen dieser Stand installiert oder aufgelöst wird: lokale Dev-Umgebungen (`dotnet workload update`/`list`) und die Windows-CI-Jobs (`dotnet workload restore Reporter.sln` in `pr-staging-ci.yml`, `staging-ci.yml`, `security-scan.yml` sowie den Packaging-Actions). Nicht zum Scope gehört der App-Code selbst.
- **Auslösende Ereignisse:**
  - Wöchentliche NuGet-Paketprüfung (Issue #213, KW 2026-W48): Meldung „Microsoft.Maui.Controls 11.0.0 → 11.0.10 (Patch)". Der Befund wiederholt sich wöchentlich, bis er umgesetzt ist.
  - Verbindlicher Auftrag im Issue-Kommentar: Umsetzung inkl. Sicherstellung, dass das Pre-Release am Ende funktioniert.
  - Konkreter System-Auslöser für die Verifikation: PR gegen `staging` (`pr-staging-ci.yml`) und Push auf `staging` (`staging-ci.yml` → `version` → `package-*` → `prerelease`).
- **Erwartete Wirkung:**
  - `dotnet workload list` zeigt `maui-windows ≥ 11.0.10` auf den betroffenen Dev-Umgebungen; `dotnet list package --outdated` meldet `Microsoft.Maui.Controls` nicht mehr als veraltet.
  - Die aufgelöste `Microsoft.Maui.Controls`-Version ist im Build-Log lokal und in der CI nachweisbar ≥ 11.0.10.
  - Die Pre-Release-Pipeline funktioniert unverändert: `static checks`, `build & test` (PR-CI) und die `staging-ci`-Gates laufen fehlerfrei; `package-windows`, `package-android` und — sofern `IOS_SIGNING_ENABLED` aktiv — `package-ios` erzeugen ihre Artefakte; der `prerelease`-Job erstellt bei Versionsänderung weiterhin das GitHub-Pre-Release.
  - Der iOS-CI-Pin `27.2.11039` (Input `ios-workload-version` in `.github/actions/package-ios/action.yml`, gekoppelt an `xcode-path` `/Applications/Xcode_27.2.app` und Runner `xcode-27`) bleibt unverändert wirksam.
- **Umgang mit fehlenden Objekten:**
  - Lokaler `maui-windows`-Workload ≥ 11.0.10: **erzeugen** via `dotnet workload update` auf dem SDK-Band 11.0.x; bei VS-verwalteten Workloads (Installationsquelle `VS 18.11.…`) ggf. über den Visual Studio Installer.
  - Lokaler `ios`-Workload-Stand `27.2.11039`: nach einem globalen `dotnet workload update` **prüfen und ggf. wiederherstellen** — ein pauschales Update aktualisiert alle Manifeste des Bands und kann den lokalen `ios`-Stand vom CI-Pin weglaufen lassen; Wiederherstellung über den Rollback-File-Mechanismus analog `.github/actions/package-ios/action.yml` (`dotnet workload update --from-rollback-file` + `--skip-manifest-update`).
  - Repo-weiter Workload-Pin (optional): **erzeugen** via `sdk.workloadVersion` (Workload-Set) in `global.json` oder explizites `dotnet workload install maui-windows --version …` in der CI — entscheidet die offene Frage „Pinning vs. Floating".
  - CI-Diagnoseschritt `dotnet workload list` (optional): **erzeugen**, um den tatsächlich aufgelösten CI-Stand sichtbar zu machen.
- **Scope-Ausschlüsse:**
  - Keine Code-, Feature- oder UI-Änderung an der App — reine Toolchain-/CI-Maßnahme; `src/Reporter/Reporter.csproj` benötigt keine `PackageReference`-Änderung (`$(MauiVersion)` folgt dem Workload).
  - Kein .NET-SDK-Upgrade: `global.json` bleibt auf `11.0.100` + `rollForward: latestPatch`, sofern der Workload `11.0.10` kein neueres Band verlangt (nach dem Update prüfen).
  - Keine Änderung am iOS-Workload-Pin, am Xcode-Pin oder am `package-ios`-Runner — der bewusste Freeze aus Issue #204 wird nicht angefasst.
  - Kein Android-Workload-Update verlangt (`IncludeAndroidTarget` ist standardmäßig `false`; in der Windows-CI explizit deaktiviert).
  - `Microsoft.Extensions.Logging.Debug` (fest gepinnt auf `11.0.0` in `Reporter.csproj`) ist ein separater Paket-Befund — Mitbehandlung ist eine offene Frage, kein impliziter Scope-Bestandteil.
  - Kein Security-Fix: `dotnet list package --vulnerable --include-transitive` ist ohne Befund; die Dringlichkeit ergibt sich aus der wöchentlichen Wiederholungsmeldung.

## Betroffene Klassen und Komponenten

Es sind keine C#-Datenmodell-, Logik- oder UI-Klassen betroffen. Betroffen sind Toolchain-/CI-Artefakte und Dev-Umgebungen:

- Dev-Umgebungen: `dotnet workload update` / `dotnet workload list` auf den betroffenen Maschinen (Workloadversion `11.0.100.1`, `maui-windows 11.0.0/11.0.100` verifiziert; Installationsquelle `SDK 11.0.100, VS 18.11.12402.416` → VS-Verwaltung beachten).
- `global.json` — SDK-Pin `11.0.100`; optional `sdk.workloadVersion` für ein repo-weites Workload-Set-Pinning.
- `.github/workflows/staging-ci.yml` — Jobs `static-checks`, `build-and-test`, `os-interface-tests` (jeweils `windows-latest` + `setup-dotnet 11.0.x` + `dotnet workload restore Reporter.sln`), `version` (semantic-release), `package-windows`/`package-android`/`package-ios`, `prerelease` (GitHub-Pre-Release-Erstellung).
- `.github/workflows/pr-staging-ci.yml` — gleiche Windows-Workload-Restore-Pfade für den PR-Gate.
- `.github/workflows/security-scan.yml` — gleicher `dotnet workload restore`-Pfad.
- `.github/workflows/release.yml` — Windows-Build-Jobs (gleiche Toolchain, betroffen bei der nächsten Release-Promotion).
- `.github/actions/build-and-package/action.yml` und `.github/actions/package-android/action.yml` — `setup-dotnet 11.0.x` + `dotnet workload restore Reporter.sln`; lösen den Workload je Job frisch, ungepinnt, auf dem neuesten Manifest des Bands auf.
- `.github/actions/package-ios/action.yml` — Referenz-Implementierung des Workload-Pins (Rollback-File `$RUNNER_TEMP/workload-rollback.json`, `dotnet workload update --from-rollback-file`, `dotnet workload install … --skip-manifest-update`, Entfernen vorinstallierter fremder `Microsoft.iOS.Sdk`-Packs); selbst unverändert zu belassen.
- `src/Reporter/Reporter.csproj` — nur Verifikation: `Version="$(MauiVersion)"` bleibt; `IncludeIosTarget`/`IncludeAndroidTarget`-Defaults und TFMs unverändert.
- `scripts/Run-StaticChecks.ps1` — lokale Verifikation (Format, Security, Static Analysis), muss ohne Befund durchlaufen.
- Dokumentation: `docs/help/entwicklung/` o. ä. (Hinweis zum Workload-Abgleich, falls Pinning eingeführt oder die Abweichung bewusst dokumentiert wird), `test-results.md` (Nachweis der Verifikation).
- Tests: kein Unit-/UI-Test-Bedarf; Verifikation über `dotnet workload list`, `dotnet list package --outdated`, vollständigen Build + `dotnet test` und den CI-Lauf (PR-CI + staging-ci inkl. Pre-Release-Pfad).

## Implementierungsansatz

1. **Lokal (gezielt):** `dotnet workload update` auf dem SDK-Band 11.0.x ausführen; anschließend `dotnet workload list` verifizieren — erwartet `maui-windows ≥ 11.0.10/11.0.100`. Gleichzeitig den `ios`-Stand kontrollieren: weicht er vom CI-Pin `27.2.11039` ab, den Pin per Rollback-File wiederherstellen oder die Abweichung bewusst dokumentieren. Bei VS-verwalteten Workloads das Update ggf. über den Visual Studio Installer durchführen.
2. **CI (verifizieren, Pinning entscheiden):** Feststellen, welchen Stand `dotnet workload restore` auf `windows-latest` tatsächlich installiert (aus einem Job-Log oder via Diagnoseschritt `dotnet workload list`). Falls Reproduzierbarkeit lokal ↔ CI gefordert ist: Workload-Stand pinnen (`sdk.workloadVersion` in `global.json` oder `dotnet workload install maui-windows --version …`).
3. **`global.json`:** nur anheben, falls Workload `11.0.10` ein neueres SDK-Band verlangt (nach dem Update prüfen; `11.0.100` + `latestPatch` ist mit dem Patchband voraussichtlich kompatibel — Annahme).
4. **Pre-Release-Absicherung (verbindlicher Auftrag):** Nach dem Merge nach `staging` muss `staging-ci.yml` durchlaufen. Zu beachten: der `prerelease`-Job (und die `package-*`-Jobs) laufen nur, wenn `version.outputs.changed == 'true'` — die Commit-Message muss also eine semantic-release-kompatible Version auslösen (z. B. `fix:`/`feat:`; ein reines `chore:` erzeugt kein RC). Falls `IOS_SIGNING_ENABLED` aktiv ist, muss `package-ios` mit dem unveränderten Pin `27.2.11039` + `xcode-27`-Runner weiterhin bauen; das SDK-Gate in der Action (`DTSDKName` ≥ `iphoneos27`) bleibt wirksam.
5. **Verifikation:** vollständiger Build (bei Bedarf `-p:IncludeIosTarget=false` für den lokalen Verifikationsbuild), `.\scripts\Run-StaticChecks.ps1`, `dotnet test`; im Build-Log die aufgelöste `Microsoft.Maui.Controls`-Version bestätigen; `dotnet list package --outdated` ohne Maui-Befund.

## Konfiguration

Keine Laufzeit- oder Anwendungskonfiguration. Auf Repository-/Build-Ebene optional:

- `sdk.workloadVersion` (Workload-Set-Version) in `global.json` für reproduzierbare Workload-Auflösung lokal ↔ CI, oder
- explizites `dotnet workload install maui-windows --version …` in den CI-Jobs, oder
- bewusstes Floating auf dem neuesten Manifest des SDK-Bands (Status quo) plus dokumentierter lokaler Update-Pfad.

## Offene Fragen

1. Soll der Workload-Stand repo-weit gepinnt werden (`sdk.workloadVersion` in `global.json` bzw. `--version` in der CI), oder ist bewusstes Floating auf dem neuesten Stand des SDK-Bands akzeptabel? (Zielkonflikt: Reproduzierbarkeit vs. Pflegeaufwand; ohne Pin kann `dotnet list package --outdated` lokal↔CI divergieren.)
2. Soll der lokale `ios`-Workload dem CI-Pin `27.2.11039` folgen, oder darf er lokal neuer sein? (Auswirkung primär bei iOS-/macOS-Builds; ein neueres `Microsoft.iOS.Sdk`-Pack kann ein neueres Xcode verlangen, vgl. Issue #204.)
3. Sind auf den betroffenen Dev-Maschinen VS-verwaltete Workloads im Einsatz (Installationsquelle `VS …`)? Dort ggf. Update über den Visual Studio Installer statt über die CLI.
4. Wird `Microsoft.Extensions.Logging.Debug` (fest `11.0.0` in `Reporter.csproj`) in diesem Zug mitbehandelt oder separat in der Wochenprüfung beobachtet?
5. Mit welchem Commit-Typ wird die Änderung auf `staging` gebracht, damit semantic-release eine neue RC-Version erzeugt und der `prerelease`-Job tatsächlich läuft — oder ist für diesen Task ein leerer Verifikations-Lauf ohne neues Pre-Release ausreichend?
6. Verantwortlichkeit und Zeitplan für die Koordination der Dev-Umgebungen (Empfehlung im Issue: innerhalb von ~4 Wochen, spätestens bis zum nächsten monatlichen Patch-Zyklus).
