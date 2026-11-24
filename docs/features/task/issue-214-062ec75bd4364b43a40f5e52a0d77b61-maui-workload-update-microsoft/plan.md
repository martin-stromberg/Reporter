<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Issue #214 „MAUI-Workload-Update: Microsoft.Maui.Controls 11.0.0 → 11.0.10"

## Übersicht

Der wöchentliche NuGet-Befund `Microsoft.Maui.Controls 11.0.0 → 11.0.10` ist kein Paket-Bump: `Version="$(MauiVersion)"` in `src/Reporter/Reporter.csproj` löst aus dem installierten `maui-windows`-Workload auf. Die Bestandsaufnahme zeigt: Es gibt keinen repo-seitigen Workload-Pin — alle neun `dotnet workload restore`-Stellen floaten auf dem neuesten Manifest des SDK-Bands 11.0.1xx, der tatsächlich installierte Stand ist in keinem Job-Log sichtbar, und die lokale Dev-Maschine (VS-verwaltete Workloads) steht auf `11.0.0`. Umgesetzt werden: ein repo-weiter Workload-Pin über `sdk.workloadVersion` (Workload-Set) in `global.json`, ein `dotnet workload list`-Diagnoseschritt an allen neun Restore-Stellen, eine Entwickler-Dokumentation zum Pin-/Update-Verfahren sowie der lokale Workload-Abgleich auf dieser Maschine. Verbindlicher Auftrag: Nach dem Merge muss `staging-ci.yml` inklusive `package-*`- und `prerelease`-Jobs durchlaufen — die Commit-Message muss daher eine semantic-release-Version auslösen (`fix:`-Präfix). Kein App-Code, keine UI, keine Datenbank.

## Scope-Klärung

Verbindliche fachliche Abgrenzung vor der Umsetzung — übernommen aus dem Abschnitt `Fachlicher Scope` der `requirement.md`, konkretisiert durch die Bestandsaufnahme:

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Der aufgelöste `maui-windows`-Workload-Stand des SDK-Bands 11.0.1xx — festgelegt repo-weit über `sdk.workloadVersion` (Workload-Set) in `global.json` und installiert/aufgelöst an neun Stellen: `pr-staging-ci.yml` (3×), `staging-ci.yml` (3×), `security-scan.yml` (1×), `.github/actions/build-and-package/action.yml` (1×), `.github/actions/package-android/action.yml` (1×), dazu lokale `dotnet workload`-Aufrufe im Repo-Kontext |
| Auslösende Ereignisse | Wöchentliche Paketprüfung `review-nuget.yml` (Issue #213, wiederholt sich bis zur Umsetzung); verbindlicher Auftrag aus dem Issue-Kommentar; System-Auslöser der Verifikation: PR gegen `staging` (`pr-staging-ci.yml`) und Push auf `staging` (`staging-ci.yml` → `version` → `package-*` → `prerelease`) |
| Erwartete Wirkung | `dotnet workload list` zeigt `maui-windows ≥ 11.0.10` (lokal und als Log-Nachweis in allen CI-Jobs); `dotnet list package --outdated` meldet `Microsoft.Maui.Controls` nicht mehr; die `staging-ci`-Kette läuft nach dem Merge vollständig inkl. `package-windows`, `package-android`, ggf. `package-ios` (`IOS_SIGNING_ENABLED`-Gate) und `prerelease` (GitHub-Pre-Release); iOS-Pin `27.2.11039` unverändert wirksam |
| Umgang mit fehlenden Objekten | Repo-weiter `maui-windows`-Pin: **erzeugen** — `sdk.workloadVersion` (Workload-Set) in `global.json` (existiert heute nicht). CI-Diagnoseschritt `dotnet workload list`: **erzeugen** — an allen neun `dotnet workload restore`-Stellen. Entwickler-Doku zum Workload-Abgleich: **erzeugen** — existiert nicht in `docs/help/entwicklung/`. Lokaler `maui-windows`-Stand ≥ 11.0.10 auf dieser Maschine: **erzeugen** via `dotnet workload update` im Repo-Kontext (VS-Verwaltung beachten, Fallback: Visual Studio Installer). Lokaler `ios`-Stand `27.2.11039`: **wiederverwenden** — nach dem Update prüfen und bei Drift per Rollback-File-Muster (`.github/actions/package-ios/action.yml`) wiederherstellen |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine Änderung an App-Code, Features, UI oder `PackageReference`-Einträgen in `src/Reporter/Reporter.csproj` | `$(MauiVersion)` folgt dem installierten Workload; ein `Version=`-Bump wäre wirkungslos bzw. falsch — die Anforderung ist reine Toolchain-/CI-Maßnahme | bestätigt (2026-11-23, durch Issue-Text „kein Paket-Bump" + Umsetzungsmandat) |
| 2 | Kein .NET-SDK-Bandwechsel: `global.json` bleibt `11.0.100` + `rollForward: latestPatch` | Workload-Patches des Bands 11.0.1xx verlangen kein neueres SDK-Band; wird während der Umsetzung verifiziert (Workloads und SDK-Patches desselben Bands sind kompatibel) | bestätigt (2026-11-23, durch Issue-Vorgehen Punkt 3 + Umsetzungsmandat) |
| 3 | iOS-Workload-Pin `27.2.11039`, `xcode-path`/Runner `xcode-27` und `.github/actions/package-ios/action.yml` bleiben unverändert | Bewusster Freeze aus Issue #204 (Xcode-27.2-Kopplung); die Action schreibt ohnehin eine eigene temporäre `global.json` ohne `workloadVersion` und pinnt per eigenem Rollback-File — der neue `sdk.workloadVersion`-Eintrag berührt den iOS-Job nicht | bestätigt (2026-11-23, durch Issue-Abnahmekriterien + Umsetzungsmandat) |
| 4 | Kein eigenständiges Android-Workload-Update | `IncludeAndroidTarget` ist standardmäßig `false`; `package-android` zieht in der CI implizit den gepinnten Workload-Set-Stand mit (bisher Floating) — ein separater Pin-Mechanismus für Android ist nicht Teil der Anforderung | bestätigt (2026-11-23, durch Umsetzungsmandat; PR bleibt Bestätigungsinstanz) |
| 5 | `Microsoft.Extensions.Logging.Debug` (fest `11.0.0` in `Reporter.csproj:77`) wird nicht mitbehandelt | Eigenständiger Paket-Befund ohne Workload-Bezug; bleibt in der wöchentlichen Prüfung als eigener Punkt beobachtbar | bestätigt (2026-11-23, durch Issue-Offene-Punkte-Frage + Umsetzungsmandat) |
| 6 | Aktualisierung anderer Dev-Maschinen erfolgt nicht durch diesen PR | Umgebungszustände sind nicht commitbar; der PR liefert Pin + dokumentierten Update-Pfad, die Ausführung obliegt den jeweiligen Umgebungen (Zeitplan: siehe Offene Punkte) | bestätigt (2026-11-23, durch Issue-Text „Umgebungszustände" + Umsetzungsmandat) |

> Bestätigungsgrundlage: Der Anwender hat die Aufbereitung des Issues (inkl. der dort empfohlenen Vorgehensweise mit repo-weitem Pinning, Punkt 5 des Issue-Vorgehens) veranlasst und anschließend explizit „Erledige das bitte" beauftragt. Alle Ausschlüsse sind Einschränkungen, die der Issue-Text selbst bereits beschreibt oder empfiehlt — kein Ausschluss widerspricht einer im Issue geäußerten Erwartung. Der PR gegen `staging` bleibt als formale Bestätigungsinstanz vor dem Merge offen.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Repo-weiter Workload-Pin | `sdk.workloadVersion` (Workload-Set des Bands 11.0.1xx) in `global.json` | Ein einziger Eintrag wirkt für **alle** neun `dotnet workload restore`-Stellen und für lokale `dotnet workload`-Aufrufe im Repo-Kontext — kein YAML-Churn, keine Duplikation. Die Dev-Maschine ist laut `dotnet --info` bereits im Modus „workload sets" konfiguriert; der Mechanismus ist der repo-native, dokumentierte Standardweg für reproduzierbare Workload-Auflösung. Alternativen verworfen: (a) Rollback-File + `--from-rollback-file` an allen neun Stellen (package-ios-Muster) — 9× YAML-Duplikation, greift nur im Restore-Kommando, nicht beim lokalen `dotnet workload update`; (b) `dotnet workload install maui-windows --version …` in der CI — adressiert nur die CI, nicht lokal↔CI-Reproduzierbarkeit, und benötigt ebenfalls Änderungen an allen Stellen |
| CI-Diagnoseschritt | Inline-Schritt `Log installed workloads` (`dotnet workload list` + `dotnet --version`) direkt nach jedem der neun `Install .NET MAUI workload`-Schritte | Macht den tatsächlich aufgelösten CI-Workload-Stand in jedem Job-Log sichtbar (heute nirgends ablesbar) und liefert den Wirksamkeitsnachweis des Pins ohne Mehraufwand. Kein neues Composite-Artefakt: ein Einzelschritt, der dem bestehenden einfachen YAML-Stil der Workflows entspricht |
| Update-/Pin-Dokumentation | Neue Seite `docs/help/entwicklung/maui-workload-updates.md`, verlinkt in `docs/help/entwicklung/index.md` und querverwiesen aus `docs/help/release-management/wiederkehrende-reviews.md` (Dependabot-Abschnitt, der `$(MauiVersion)` bereits thematisiert) | Der Workload-Abgleich ist ein eigenes, wiederkehrendes Thema (jeder MAUI-Patch löst denselben Befund aus) — eine eigene Seite statt Anbau an `dotnet-11-migration.md`, das eine abgeschlossene Migrationsentscheidung dokumentiert |
| Release-auslösender Commit | Commit-Message mit `fix:`-Präfix (z. B. `fix(deps): …`) und `Closes #214` im Body | `version.outputs.changed` wird nur `true`, wenn semantic-release (Preset `conventionalcommits`) eine Version erzeugt — `fix` → Patch-Release; `chore`/`docs`/`ci` erzeugen kein Release und würden `package-*`/`prerelease` überspringen, womit der verbindliche Verifikationsauftrag nicht erfüllt wäre. `Closes #214` schließt das Issue bei der Promotion nach `main` (Staging-Workflow) |

## Programmabläufe

### Gepinnte Workload-Auflösung (lokal und CI, Repo-Kontext)

1. Repo ausgecheckt → `global.json` legt SDK (`11.0.100`/`latestPatch`) **und** Workload-Set (`sdk.workloadVersion`) fest.
2. CI: `actions/setup-dotnet` installiert das höchste 11.0.1xx-SDK; anschließend `dotnet workload restore Reporter.sln` — im Workload-Set-Modus installiert der Restore die Manifestversionen des gepinnten Sets statt des neuesten Manifests (bisher Floating).
3. Neuer Schritt `Log installed workloads` protokolliert `dotnet --version` und `dotnet workload list` — der Job-Log zeigt `maui-windows 11.0.10/11.0.100` (bzw. den gepinnten Stand).
4. `dotnet restore`/`dotnet build` lösen `$(MauiVersion)` aus dem installierten Workload auf → `Microsoft.Maui.Controls ≥ 11.0.10`.
5. Lokal: `dotnet workload restore`/`update`/`install` im Repo-Verzeichnis arbeiten im selben Workload-Set-Modus — lokale und CI-Auflösung konvergieren auf denselben Pin.

Beteiligte Komponenten: `global.json`, `pr-staging-ci.yml` (`static-checks`, `build-and-test`, `os-interface-tests`), `staging-ci.yml` (gleiche drei Jobs), `security-scan.yml`, `.github/actions/build-and-package/action.yml`, `.github/actions/package-android/action.yml`, `src/Reporter/Reporter.csproj` (unverändert, nur Auflöser von `$(MauiVersion)`).

### Lokaler Workload-Abgleich auf dieser Dev-Maschine

1. `dotnet workload update` im Repo-Kontext auf dem SDK-Band 11.0.x ausführen (im Workload-Set-Modus konvergiert der Stand auf den gepinnten Set- bzw. den aktuellen Band-Stand).
2. `dotnet workload list` verifizieren: `maui-windows ≥ 11.0.10/11.0.100` erwartet.
3. iOS-/maccatalyst-Stand kontrollieren: weicht `ios`/`maccatalyst` vom CI-Pin `27.2.11039` ab (ein pauschales Update aktualisiert alle Manifeste des Bands), den Pin per Rollback-File wiederherstellen — analog dem CI-Verfahren in `.github/actions/package-ios/action.yml` (`dotnet workload update --from-rollback-file <datei>` mit `{"microsoft.net.sdk.ios": "27.2.11039"}` + `--skip-manifest-update`), bzw. die Abweichung bewusst dokumentieren, da iOS-Builds lokal ohnehin nicht ausgeführt werden (`IncludeIosTarget=false`-Konvention).
4. Falls `dotnet workload update` wegen der VS-Verwaltung (`Installationsquelle: SDK 11.0.100, VS 18.11.12402.416`, Typ `Msi`) nicht greift: Update über den Visual Studio Installer ausführen und danach Schritt 2 wiederholen.
5. `dotnet list package --outdated` mit `IncludeIosTarget=false`/`IncludeAndroidTarget=false` — kein `Microsoft.Maui.Controls`-Befund mehr erwartet.

Beteiligte Komponenten: `dotnet workload`-CLI, VS Installer (Fallback), `docs/help/entwicklung/maui-workload-updates.md` (dokumentiert diesen Pfad für alle Dev-Umgebungen).

### Pre-Release-Verifikation nach dem Merge (verbindlicher Auftrag)

1. Merge des `fix:`-Commits auf `staging` → `staging-ci.yml` startet (`detect-backmerge` → `static-checks` + `build-and-test` + `os-interface-tests` non-blocking).
2. `version`: semantic-release-Dry-Run erkennt den `fix:`-Commit → `changed == 'true'`, RC-Version/Tag werden bestimmt.
3. `package-windows` (via `build-and-package`) und `package-android` laufen mit gepinntem Workload — die neuen `dotnet workload list`-Schritte zeigen den gepinnten Stand im Log; Artefakte werden hochgeladen.
4. `package-ios` läuft nur bei `vars.IOS_SIGNING_ENABLED == 'true'` — mit unverändertem iOS-Pin `27.2.11039` auf `xcode-27` (die Action schreibt eine eigene temporäre `global.json` ohne `workloadVersion`; der Repo-Pin ist dort wirkungslos, wie beabsichtigt).
5. `prerelease`: `gh release create` erzeugt das GitHub-Pre-Release `v<version>-rc.<n>` inkl. `update.json` und Release-Assets.
6. Nachweis: Workflow-Lauf grün, Pre-Release auf GitHub sichtbar, `dotnet workload list`-Logs zeigen ≥ 11.0.10. Überwachung fail-fast (`gh run watch`/`gh pr checks --fail-fast`): bei erstem `fail` sofort in die Diagnose gehen (`gh run view --job <id> --log-failed`).

Beteiligte Komponenten: `staging-ci.yml` (Jobs `version`, `package-windows`, `package-android`, `package-ios`, `prerelease`), `release.config.js` (conventionalcommits-Preset), `scripts/release-assets.mjs`, `scripts/create-update-manifest.mjs`.

## Neue Klassen

Keine C#-Klassen (reine Toolchain-/CI-/Dokumentationsänderung). Neu angelegt werden:

| Artefakt | Typ | Zweck |
|----------|-----|-------|
| `docs/help/entwicklung/maui-workload-updates.md` | Dokumentation | Wie `$(MauiVersion)` aufgelöst wird; Funktionsweise des `sdk.workloadVersion`-Pins; Pin-Bump-Verfahren bei künftigen Befunden; lokaler Update-Pfad inkl. VS-Verwaltungs-Fallback; iOS-Pin-Interaktion (`27.2.11039`, Rollback-Wiederherstellung) |

## Änderungen an bestehenden Klassen

Keine C#-Klassen betroffen. Geändert werden Repo-/CI-Artefakte:

### `global.json` (Repo-Root)

- **Neuer Schlüssel:** `sdk.workloadVersion` — Workload-Set-Version des Bands 11.0.1xx, die `maui-windows ≥ 11.0.10` enthält (konkreter Wert in Schritt 1 der Umsetzungsreihenfolge zu ermitteln). `sdk.version`/`rollForward` unverändert.

### `.github/workflows/pr-staging-ci.yml` (Workflow)

- **Neuer Schritt `Log installed workloads`** nach jedem `Install .NET MAUI workload`-Schritt (Zeilen ~75, ~114, ~193): führt `dotnet --version` und `dotnet workload list` aus; reiner Diagnoseschritt ohne Gate.

### `.github/workflows/staging-ci.yml` (Workflow)

- **Neuer Schritt `Log installed workloads`** nach jedem `Install .NET MAUI workload`-Schritt (Zeilen ~72, ~111, ~190): identischer Schritt wie in `pr-staging-ci.yml`.

### `.github/workflows/security-scan.yml` (Workflow)

- **Neuer Schritt `Log installed workloads`** nach dem `Install .NET MAUI workload`-Schritt (Zeile ~28).

### `.github/actions/build-and-package/action.yml` (Composite Action)

- **Neuer Schritt `Log installed workloads`** nach dem `Install .NET MAUI workload`-Schritt (Zeile ~21), `shell: bash` passend zum Action-Stil.

### `.github/actions/package-android/action.yml` (Composite Action)

- **Neuer Schritt `Log installed workloads`** nach dem `Install .NET MAUI workload`-Schritt (Zeile ~21), `shell: bash` passend zum Action-Stil.

### `.github/actions/package-ios/action.yml` (Composite Action)

- **Unverändert** — eigener iOS-Pin-Mechanismus (Rollback-File + `--skip-manifest-update` + Pack-Cleanup), siehe Scope-Ausschluss 3.

### Dokumentation

- `docs/help/entwicklung/maui-workload-updates.md`: neu (siehe oben), mit PolyForm-Lizenzheader.
- `docs/help/entwicklung/index.md`: Eintrag in der Inhaltsliste für die neue Seite.
- `docs/help/release-management/wiederkehrende-reviews.md`: kurzer Querverweis im Dependabot-Abschnitt (wo `$(MauiVersion)` bereits erwähnt wird) auf die neue Seite — dort steht, wie der `$(MauiVersion)`-Befund repo-weit behandelt wird.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `sdk.workloadVersion` in `global.json` | Muss eine existierende Workload-Set-Version des SDK-Bands 11.0.1xx sein, deren `maui-windows`-Manifest ≥ 11.0.10 liefert | Ungültige Version: `dotnet workload restore`/`update` schlägt früh und sichtbar fehl (Fail-fast im ersten CI-Job); Set ohne 11.0.10: `dotnet list package --outdated`-Befund bleibt — vor Commit lokal zu verifizieren |
| Aufgelöste `Microsoft.Maui.Controls`-Version | ≥ 11.0.10 nach Restore, lokal wie in CI | Wöchentlicher Befund wiederholt sich; Nachweis über `dotnet list package --outdated` (lokal) bzw. `dotnet workload list`-Schritt (CI) |
| Neue/änderte Dateien | PolyForm-Lizenzheader-Pflicht (`scripts/add-license-headers.mjs --check`) | `static-checks`-Job und `Run-StaticChecks.ps1` schlagen am License-Header-Check fehl |
| Lokaler `ios`-Stand nach Workload-Update | Soll `27.2.11039` entsprechen (CI-Pin); Abweichung ist zulässig, aber zu dokumentieren | Versehentliche Drift vom CI-Pin → Wiederherstellung per Rollback-File-Muster oder begründete Doku-Notiz |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `sdk.workloadVersion` in `global.json` | Workload-Set-Version (Band 11.0.1xx) | jüngstes verfügbares Set des Bands mit `maui-windows ≥ 11.0.10` — konkreter Wert bei Umsetzung zu ermitteln | Repo-weites Pinning der Workload-Auflösung für `dotnet workload restore`/`install`/`update` — Reproduzierbarkeit lokal ↔ CI und feste Grundlage für `$(MauiVersion)` |
| Schritt `Log installed workloads` (9× in Workflows/Actions) | CI-Diagnoseschritt | `dotnet --version` + `dotnet workload list` | Sichtbarkeit des tatsächlich installierten CI-Workload-Stands in jedem Job-Log (Wirksamkeitsnachweis des Pins) |

Keine Laufzeit- oder Anwendungskonfiguration (`appsettings` o. ä.) betroffen.

## Seiteneffekte und Risiken

- **Zukünftige MAUI-Patches erfordern einen Pin-Bump:** Mit `sdk.workloadVersion` floatet die Auflösung nicht mehr automatisch — der nächste `Microsoft.Maui.Controls`-Befund der Wochenprüfung verlangt das Anheben des `workloadVersion`-Werts. Das ist gewollt (Reproduzierbarkeit) und wird in `docs/help/entwicklung/maui-workload-updates.md` als Wartungsschritt beschrieben.
- **Workload-Set pinnt alle benötigten Manifeste, nicht nur `maui-windows`:** `package-android` installiert damit das `android`-Workload auf dem Set-Stand statt auf Floating-latest; `package-ios` ist davon ausgenommen (eigene temporäre `global.json` ohne `workloadVersion` + eigener Rollback-Pin). Risiko klein — die Set-Versionen sind die offiziell getesteten Kombinationen des Bands —, aber bewusst dokumentiert.
- **VS-verwaltete Workloads lokal:** `dotnet workload update` kann sich mit der VS-Verwaltung überschneiden (`Installationsquelle: VS 18.11.12402.416`, Typ `Msi`); greift der CLI-Pfad nicht, ist der Visual Studio Installer der Update-Weg. Das ist pro Dev-Maschine individuell — die Doku nennt beide Pfade.
- **Lokaler iOS-/maccatalyst-Stand kann driften:** Ein pauschales `dotnet workload update` aktualisiert alle Manifeste des Bands; der lokale `ios`-Stand `27.2.11039` kann vom CI-Pin weglaufen. Konsequenz lokal gering (iOS-Builds laufen nur auf dem macOS-Runner), dennoch als Prüfschritt + Wiederherstellungspfad geplant.
- **PR-Gate kann die Pre-Release-Kette nicht ausüben:** `pr-staging-ci.yml` hat keine `version`/`package-*`/`prerelease`-Jobs — der verbindliche Verifikationsnachweis entsteht erst nach dem Merge auf `staging`. Der `fix:`-Commit-Typ stellt sicher, dass `version.changed == 'true'` und die Kette tatsächlich läuft.
- **`os-interface-tests` bleibt non-blocking** (`continue-on-error: true`) — ein dortiger Fehlschlag blockiert `version`/Pre-Release nicht; Status quo.
- **Lizenzheader-Check:** Die neuen Dateien (Doku-Seite, Plan-/Tasks-Artefakte) benötigen PolyForm-Header; die Bestandsaufnahme hat gezeigt, dass der Check sonst fehlschlägt. In der Umsetzung auf allen neu angelegten Dateien sicherstellen.
- **Bekannte E2E-Umgebungsfehler:** 18/39 E2E-Tests schlagen in der Agenten-Session umgebungsbedingt fehl (identisch zur Issue-#204-Baseline) — nicht Bestandteil und nicht ursächlich für diese Änderung.

## Umsetzungsreihenfolge

1. **Konkrete `sdk.workloadVersion` ermitteln und in `global.json` eintragen**
   - Voraussetzungen: Keine.
   - Beschreibung: Verfügbare Workload-Set-Versionen des Bands 11.0.1xx ermitteln (z. B. via `dotnet workload update`/`install --version` mit Kandidaten-Stand und `dotnet workload list`/Rollback-Ausgabe; lokal ist bereits Workload-Set-Modus aktiv). Das jüngste Set wählen, das `maui-windows ≥ 11.0.10` enthält — erwartbar der aktuelle Band-Stand (`11.0.10` ist laut Befund das neueste MAUI-Patch-Release). Wert in `global.json` unter `sdk` eintragen; `sdk.version`/`rollForward` unverändert lassen. Prüfen, dass `dotnet workload restore Reporter.sln` im Repo-Kontext den gepinnten Stand installiert.

2. **Lokale Dev-Maschine auf den gepinnten/aktuellen Workload-Stand bringen**
   - Voraussetzungen: Schritt 1 (Pin steht; alternativ direkt `dotnet workload update` ohne Pin-Kontext).
   - Beschreibung: `dotnet workload update` auf dem SDK-Band 11.0.x ausführen; bei VS-Verwaltungs-Konflikt über den Visual Studio Installer aktualisieren. `dotnet workload list` verifizieren (`maui-windows ≥ 11.0.10`); `ios`/`maccatalyst`-Stand gegen `27.2.11039` prüfen und bei Drift per Rollback-File wiederherstellen oder die Abweichung dokumentieren.

3. **Diagnoseschritt `Log installed workloads` an allen neun Restore-Stellen einfügen**
   - Voraussetzungen: Keine (YAML-only; unabhängig von Schritt 1, aber sinnvoll danach).
   - Beschreibung: Nach jedem `Install .NET MAUI workload`-Schritt den identischen Diagnoseschritt (`dotnet --version` + `dotnet workload list`) einfügen: `pr-staging-ci.yml` (3×), `staging-ci.yml` (3×), `security-scan.yml` (1×), `build-and-package/action.yml` (1×, `shell: bash`), `package-android/action.yml` (1×, `shell: bash`). YAML-Syntax lokal verifizieren (Einrückung/Shell-Felder).

4. **Entwickler-Dokumentation anlegen und verlinken**
   - Voraussetzungen: Schritte 1–3 (Doku beschreibt den finalen Mechanismus).
   - Beschreibung: `docs/help/entwicklung/maui-workload-updates.md` anlegen (Auflösung von `$(MauiVersion)`, `sdk.workloadVersion`-Pin und dessen Bump-Verfahren, lokaler Update-Pfad inkl. VS-Installer-Fallback und iOS-Pin-Interaktion, Bezug zur wöchentlichen `review-nuget`-Prüfung); Eintrag in `docs/help/entwicklung/index.md`; Querverweis im Dependabot-Abschnitt von `docs/help/release-management/wiederkehrende-reviews.md`. Lizenzheader auf der neuen Datei nicht vergessen.

5. **Lokale Verifikation**
   - Voraussetzungen: Schritte 1–4.
   - Beschreibung: `dotnet restore Reporter.sln -r win-x64`, `dotnet build Reporter.sln -c Release -p:TreatWarningsAsErrors=true`, `dotnet test` (Unit + OsInterface), `dotnet list package --outdated` (kein Maui-Befund), `.\scripts\Run-StaticChecks.ps1` mit Exit-Code 0 ohne Findings (alle neuen Dateien mit Header; Projektregel). Ergebnis im PR dokumentieren.

6. **Commit, PR und Merge auf `staging`**
   - Voraussetzungen: Schritte 1–5 grün.
   - Beschreibung: Commit mit `fix:`-Präfix, z. B. `fix(deps): pin maui-windows workload set to >= 11.0.10`, Body mit `Closes #214` (schließt das Issue bei der Promotion nach `main`). PR gegen `staging` öffnen; PR-Checks mit `gh pr checks --watch --fail-fast` beobachten — bei erstem `fail` sofort `gh run view --job <id> --log-failed` statt weiter zu warten. In den `Log installed workloads`-Outputs des PR-Laufs den gepinnten Stand ≥ 11.0.10 ablesen.

7. **Post-Merge-Verifikation der Pre-Release-Kette (verbindlicher Auftrag)**
   - Voraussetzungen: Schritt 6 gemergt.
   - Beschreibung: `staging-ci.yml`-Lauf nach dem Push verfolgen (fail-fast-Überwachung): `static-checks`/`build-and-test` grün; `version.changed == 'true'` (fix-Commit); `package-windows` + `package-android` liefern Artefakte und zeigen den gepinnten Workload im Log; `package-ios` — sofern `IOS_SIGNING_ENABLED` aktiv — läuft mit unverändertem Pin `27.2.11039` auf `xcode-27`; `prerelease` erstellt das GitHub-Pre-Release `v<version>-rc.<n>` mit `update.json` und Assets. Ergebnis (Run-URL, Pre-Release-Tag) als Nachweis festhalten und dem Anwender melden.

## Tests

### Neue Tests

Keine neuen automatisierten Tests: Die geänderten Artefakte (`global.json`, Workflow-YAML, Doku) haben im Repo keine Testsuite — projektüblich werden CI-Änderungen über echte Workflow-Läufe verifiziert. Der neue `Log installed workloads`-Schritt ist selbst der dauerhafte Wirksamkeitsnachweis im CI-Log.

### Betroffene bestehende Tests

Keine — kein C#-Code, keine Signaturen, keine Datenstrukturen geändert. Die bestehenden Suiten (801 Unit + 4 OsInterface + 36 `node:test`) laufen unverändert und dienen als Regressionsnachweis in Schritt 5.

### E2E-Tests (primärer Funktionsnachweis)

Keine E2E-Tests erforderlich. Begründung: Die Anforderung berührt keinen über UI oder Nutzeraktion erreichbaren Ablauf — keine Page, kein Control, kein Flow in der App ändert sich. Der „Benutzerfluss" dieser Änderung ist die CI-Pipeline selbst; ihr Funktionsnachweis kann ausschließlich über echte Workflow-Läufe (`pr-staging-ci.yml`, `staging-ci.yml` inkl. `prerelease`) erbracht werden, nicht über die FlaUI-Desktop-Suite, die die App-UI ausübt. Zusätzlich: Die E2E-Suite ist in keinem CI-Gate enthalten und liefert für eine Toolchain-Änderung ohne UI-Bezug kein Signal (bekanntes Umgebungsproblem dieser Session: 18/39 Fehlschläge deckungsgleich mit der Issue-#204-Baseline).

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: Repo-Pin + vollständige Pre-Release-Kette | Merge des `fix:`-Commits auf `staging` | `global.json` enthält `sdk.workloadVersion`; alle neun Restore-Stellen haben den Diagnoseschritt; Dev-Maschine vorher auf `maui-windows 11.0.0` | PR-CI grün → Merge → `staging-ci`: `static-checks`/`build-and-test` grün → `version` erzeugt RC → `package-windows`/`package-android` liefern Artefakte → (`package-ios` bei aktivem Signing) → `prerelease` | GitHub-Pre-Release `v<version>-rc.<n>` existiert mit `update.json` und Assets; Job-Logs zeigen `maui-windows 11.0.10/11.0.100` | Echter CI-Lauf (`gh run watch`, fail-fast) + `gh release list` |
| 2 | Lokaler Workload-Abgleich | `dotnet workload update` im Repo-Kontext auf dieser Maschine | `maui-windows 11.0.0/11.0.100` installiert, VS-verwaltet, Workload-Set-Modus aktiv | Update via CLI (Fallback VS Installer) → `dotnet workload list` → `dotnet list package --outdated` | `maui-windows ≥ 11.0.10`; kein `Microsoft.Maui.Controls`-Befund mehr | `dotnet workload list`-Output, `list-package-outdated`-Log |
| 3 | iOS-Stand-Drift nach Update | Globales `dotnet workload update` | Lokaler `ios`-Stand `27.2.11039` (= CI-Pin) kann auf ein neueres Manifest gezogen worden sein | `dotnet workload list` prüfen → bei Drift Rollback-File `{"microsoft.net.sdk.ios": "27.2.11039"}` + `dotnet workload update --from-rollback-file` + `--skip-manifest-update` | `ios`/`maccatalyst` wieder bzw. weiterhin auf `27.2.11039` (oder begründete Dokumentation der Abweichung) | `dotnet workload list`-Output |
| 4 | Fehlerfall: ungültiger Pin-Wert | `dotnet workload restore` mit nicht existierendem `sdk.workloadVersion` | `global.json` enthält ungültige Set-Version | Erster CI-Job bzw. lokaler Restore-Lauf | Restore schlägt früh mit verständlicher SDK-Fehlermeldung fehl — kein stilles Floating auf einen anderen Stand | Fail-fast im ersten Job-Log (Verifikation erfolgt vorab lokal in Schritt 1) |

## Offene Punkte

Keine — beide ursprünglich offenen Punkte sind mit den empfohlenen Vorschlägen entschieden (2026-11-23):

1. **Konkrete `sdk.workloadVersion`-Set-Version:** Jüngstes verfügbares Workload-Set des Bands 11.0.1xx mit `maui-windows ≥ 11.0.10` — Ermittlung in Umsetzungsschritt 1 via `dotnet workload update`/`dotnet workload list`-Kontrolle. Technische Ermittlung, keine Anwenderfrage.
2. **Verantwortlichkeit/Zeitplan übrige Dev-Umgebungen:** Issue-Empfehlung übernommen — Abgleich innerhalb von ~4 Wochen über den dokumentierten Pfad in `docs/help/entwicklung/maui-workload-updates.md`; Kommunikation über den PR bzw. Issue-Kommentar.
