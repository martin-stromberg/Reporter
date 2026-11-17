<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: NuGet-Paketprüfung KW 2026-W47 (Issue #179)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Prüfung | `dotnet list Reporter.sln package --outdated --include-transitive` erneut ausführen und mit `inventory/outdated.md` abgleichen | Offen | — |
| 2 | Prüfung | `dotnet list Reporter.sln package --vulnerable --include-transitive` und `--deprecated --include-transitive` erneut ausführen und mit `inventory/vulnerabilities.md` abgleichen | Offen | — |
| 3 | Prüfung | Offene Dependabot-PRs prüfen (`gh pr list --author app/dependabot`) und ggf. 11.x-PRs für das Folge-Issue vormerken | Offen | — |
| 4 | Bewertung | TFM-Anforderungen der 11.x-Zielpakete aus NuGet-Metadaten prüfen (Erwartung: `net11.0` erforderlich) | Offen | — |
| 5 | Bewertung | Ergebnistabelle für alle 13 direkten Pakete erstellen (Befund, Empfehlung, Risiko) inkl. Abschnitt transitive Ausreißer | Offen | — |
| 6 | Folge-Issue | Folge-Issue „Migration auf .NET 11 und NuGet-Paketwelle 11.x" via `gh issue create` anlegen (TFMs, SDK, CI, MAUI-Workload, Timing-Empfehlung) | Offen | — |
| 7 | Dokumentation | Kommentar auf Issue #179 mit Ergebnistabelle und Verlinkung des Folge-Issues posten (`gh issue comment 179`) | Offen | — |
| 8 | Verifikation | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release` ausführen (grün) | Offen | — |
| 9 | Verifikation | `.\scripts\Run-StaticChecks.ps1` ausführen — Exit-Code 0, keine Findings | Offen | — |
| 10 | Abschluss | Feature-Unterverzeichnis aufräumen und finalen Commit mit `Closes #179` erstellen | Offen | — |
| 11 | Abschluss | PR gegen `staging` erstellen; PR-Body mit Ergebnistabelle, aufgeschobenen Empfehlungen und Folge-Issue-Link | Offen | — |
| 12 | Abschluss | PR-Checks überwachen (Fail-fast); bei `fail` sofort `gh run view --job <id> --log-failed` diagnostizieren | Offen | — |
