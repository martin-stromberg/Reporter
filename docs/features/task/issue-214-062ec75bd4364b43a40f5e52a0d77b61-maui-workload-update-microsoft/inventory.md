<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: MAUI-Workload-Update Microsoft.Maui.Controls 11.0.0 → 11.0.10

Analysiert wurde die Build-Toolchain (installierte .NET-MAUI-Workloads,
`global.json`, `Reporter.csproj`) und die CI-/Release-Pipeline — bezogen auf
die Anforderung aus `requirement.md` (Issue #214, `Microsoft.Maui.Controls`
wird über `$(MauiVersion)` aus dem Workload aufgelöst, kein Paket-Bump).

## Zusammenfassung

- **Befund lokal exakt reproduziert:** `maui-windows 11.0.0/11.0.100`
  installiert, `dotnet list package --outdated` meldet
  `Microsoft.Maui.Controls 11.0.0 → 11.0.10`; `project.assets.json` löst
  `11.0.0` auf. Kein Security-Befund.
- **Kein repo-seitiger Workload-Pin für `maui-windows`:** `global.json`
  pinnt nur das SDK (`11.0.100` + `latestPatch`), enthält **kein**
  `sdk.workloadVersion`; alle neun `dotnet workload restore`-Aufrufe in
  Workflows/Composite-Actions laufen ungepinnt auf dem neuesten Manifest des
  Bands 11.0.1xx (Floating). Kein `dotnet workload list`-Diagnoseschritt
  vorhanden — der tatsächliche CI-Workload-Stand ist nirgends sichtbar.
- **iOS-Pin (Issue #204) existiert und ist bereits lokal erfüllt:**
  `.github/actions/package-ios/action.yml` pinnt `ios-workload-version`
  `27.2.11039` per Rollback-File + `--skip-manifest-update` + Pack-Cleanup,
  gekoppelt an `xcode-27`-Runner und `/Applications/Xcode_27.2.app`. Lokaler
  `ios`-/`maccatalyst`-Stand ist bereits `27.2.11039` — ein globales
  `dotnet workload update` gefährdet diesen Stand (VS-verwaltete
  Installation, Quelle `SDK 11.0.100, VS 18.11.12402.416`; Update-Modus
  „workload sets" aktiv).
- **Pre-Release-Kette:** `staging-ci.yml` → `version` (semantic-release)
  → `package-windows`/`package-android`/`package-ios` (jeweils nur bei
  `version.changed == 'true'`) → `prerelease`. Ein `chore:`-Commit löst
  **kein** Pre-Release aus — relevant für den verbindlichen
  Verifikationsauftrag. `pr-staging-ci.yml` hat keine `package-*`-Jobs, die
  Verifikation kann erst nach dem Merge erfolgen.
- **Kein App-Code betroffen:** `Reporter.csproj:76` (`$(MauiVersion)`)
  bleibt unverändert; `Microsoft.Extensions.Logging.Debug 11.0.0` (Z. 77)
  ist ein eigenständiger Paket-Befund.

**Test-Ausgangszustand:** Release-Build der Solution (inkl. MAUI-App unter
Workload `11.0.0`, `TreatWarningsAsErrors`) fehlerfrei; Unit-Tests **801/801**
und OsInterface-Lane **4/4** grün; `npm test` **36/36** grün. E2E-Suite
**21/39** bestanden — die 18 Fehlschläge sind exakt derselbe Testfallsatz mit
demselben Fehlerbild (`Aktionen`-Element nicht auffindbar), den die
Issue-#204-Bestandsaufnahme einen Tag zuvor auf derselben Maschine
dokumentiert hat (bekanntes Umgebungsproblem der Agenten-Session).
`Run-StaticChecks.ps1` scheitert nur am Lizenzheader-Check — ausschließlich
durch die hier neu angelegten Nachweisdateien. Details und Nachweise:
[inventory/tests.md](inventory/tests.md).

## Details

- [Toolchain und Workload-Stand](inventory/toolchain.md) — `global.json`,
  `Reporter.csproj`, installierte Workloads, Befund-Reproduktion,
  bestehende Pin-/Abgleich-Mechanismen
- [CI-Workflows und Composite Actions](inventory/ci.md) — alle
  `dotnet workload restore`-Stellen, `staging-ci`-Kette, iOS-Pin-Referenz
- [Tests und Test-Ausgangszustand](inventory/tests.md) — Suiten, Läufe,
  Fehlerbilder, Testlücken
