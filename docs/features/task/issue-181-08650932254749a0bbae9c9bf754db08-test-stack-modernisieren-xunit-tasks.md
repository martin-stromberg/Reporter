<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Test-Stack modernisieren — xunit.v3, Microsoft.NET.Test.Sdk 18, coverlet (GitHub Issue #181)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Pakete | `Reporter.Tests.csproj`: `xunit` 2.9.3 → `xunit.v3` 4.0.3, `xunit.runner.visualstudio` 3.1.4 → 4.0.1, `Microsoft.NET.Test.Sdk` 17.14.1 → 18.12.0 | Erledigt | Build + Testlauf |
| 2 | Pakete | `Reporter.E2ETests.csproj`: `xunit` 2.9.3 → `xunit.v3` 4.0.3, `xunit.runner.visualstudio` 3.1.4 → 4.0.1, `Microsoft.NET.Test.Sdk` 17.14.1 → 18.12.0 | Erledigt | Build + `--list-tests` (39 Tests entdeckt) |
| 3 | Pakete | `coverlet.collector` 6.0.4 → `coverlet.MTP` 10.1.1 (nativer MTP-Nachfolger des Collectors; `coverlet.collector` ist VSTest-only und unter xunit.v3/MTP v2 + .NET-10-SDK nicht lauffähig) | Erledigt | `--coverlet`-Lauf erzeugt `coverage.cobertura*.xml` |
| 4 | Pakete | `Microsoft.Testing.Extensions.TrxReport` 2.5.3 zu `Reporter.Tests` hinzufügen (TRX-Reporter ist MTP-Erweiterung, nicht eingebaut) | Erledigt | `TestResults/test-results.trx` erzeugt |
| 5 | Code | `IAsyncLifetime`/`IAsyncDisposable`-Signaturen in `StubFeedServer` und `ReporterAppFixture` auf `ValueTask` umstellen (xunit.v3 Breaking Change) | Erledigt | E2E-Build 0 Fehler, Discovery 39 Tests |
| 6 | Code | `<NoWarn>xUnit1051</NoWarn>` in beiden Testprojekten (443 neue Analyzer-Hinweise `TestContext.Current.CancellationToken` — sonst schlägt das `TreatWarningsAsErrors`-Gate fehl; kein Verhaltensdelta) | Erledigt | Release-Build 0 Warnungen |
| 7 | Plattform | `global.json` mit `"test": { "runner": "Microsoft.Testing.Platform" }` anlegen — aktiviert den MTP-Modus von `dotnet test` (VSTest-Modus ist auf .NET-10-SDK + MTP v2 gesperrt) | Erledigt | `dotnet test --project ...` läuft |
| 8 | CI | `pr-staging-ci.yml` + `staging-ci.yml`: Test-Step auf MTP-Aufruf umstellen (`--project`, `--coverlet` Cobertura, Include `[Reporter.*]*`, Exclude `[*]Reporter.Data.Migrations.*`, `--report-trx`); ReportGenerator-Glob `**/coverage.cobertura*.xml`; TRX-Upload-Pfad `TestResults/*.trx` | Erledigt | Lokaler Lauf: 798/798 grün, Coverage 96,07 % |
| 9 | CI | `release.yml` Release-Gate auf `dotnet test --project` umstellen | Erledigt | Syntax geprüft |
| 10 | Skripte | `Run-E2ETests.ps1`: `dotnet test` → `dotnet test --project` | Erledigt | `--list-tests`-Lauf erfolgreich |
| 11 | Aufräumen | `src/Reporter.Tests/coverlet.runsettings` entfernen (VSTest-Datacollector-Konfiguration ohne Funktion unter MTP) | Erledigt | Datei gelöscht, keine Referenzen mehr |
| 12 | Verifikation | `dotnet test --project Reporter.Tests` lokal mit CI-Argumenten: 798/798 Tests, `coverage.cobertura*.xml` (Line-Rate 96,07 % über `Reporter.Core`+`Reporter.Data`), `test-results.trx` | Erledigt | Lauf vom 2026-11-16 |
| 13 | Verifikation | `dotnet list package --deprecated` → keine Befunde; `--outdated` → nur EFCore/Sqlite 10.x→11.0.0 (kein Test-Stack-Paket) | Erledigt | CLI-Ausgabe |
| 14 | Verifikation | `scripts\Run-StaticChecks.ps1` — Restore, Format, Lizenzheader, Security, Static-Analysis-Build ohne Befund | Erledigt | Exit-Code 0 |
| 15 | Dokumentation | `docs/help/release-management/ablauf-technisch.md`, `docs/help/tests/{ablauf-technisch,installation,architektur}.md`, `docs/help/benachrichtigungen/installation.md` auf MTP-Aufrufe und `xunit.v3` aktualisieren | Erledigt | Diff-Review |
| 16 | E2E | FlaUI-E2E-Lauf in interaktiver Session (benötigt sichtbares Fenster, `Run-E2ETests.ps1`) | Offen | Discovery verifiziert; Lauf erfordert interaktive Desktop-Session — Hinweis im PR |
