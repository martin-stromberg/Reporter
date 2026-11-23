<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Abnahmeprüfung – Entwicklungsschritt 1

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

Prüfnachweise (unabhängiger schreibgeschützter Prüfagent, Fundstellen
direkt gelesen):

- TFM-Flip vollständig: `Reporter.csproj:8-13` (6 Listen net11.0-*),
  `UseInterpreter`-Condition net11.0-ios (Z. 52); Core/Data/Tests
  `net11.0` (je Z. 6); E2ETests `net11.0-windows10.0.19041.0`.
- Paketwelle exakt: EF.Design+Sqlite 11.0.0 (Data:18/22),
  EF.Sqlite 11.0.0 (Tests:17), Data.Sqlite 11.0.0 (E2ETests:17),
  Logging.Debug 11.0.0 (Reporter:77), Syndication 11.0.0 (Core:15),
  TimeProvider.Testing 11.0.1 (Tests:18); `$(MauiVersion)` unverändert;
  Test-Stack unverändert (2.6-verifiziert via grüne Läufe).
- Harte Strings: `Run-E2ETests.ps1:13/24`, `iOS-Deployment.ps1:94`,
  `ReporterAppFixture.cs:143` → net11.0.
- `global.json`: SDK 11.0.100 + latestPatch, `test.runner` erhalten.
- `dotnet-tools.json` Repo-Root (neue SDK-Konvention), dotnet-ef 11.0.0.
- G5: Android 24.0 (XA4216-Befund), MacCatalyst 17.0 (Pack-Befund),
  iOS 15.0 / Windows 10.0.17763.0 unverändert — konsistent mit
  `Package.appxmanifest:22-23`.
- Rest-`net10.0`-Treffer ausschließlich Schritt-3-/4-Bereiche
  (.github, ci-instructions.md, README, docs, changes.log) und
  EF-Migrations-Historie — keine Scope-Verletzung.
- Nachweise `test-results.md` im Commit-Verlauf (69e1e8c): Build
  net11.0-windows + net11.0-android grün, `dotnet test` 805/805
  (inkl. MTP `--coverlet`/`--report-trx`), `Run-StaticChecks.ps1`
  Exit 0; keine neue EF-Schema-Migration im Diff.
