<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: NuGet-Paketprüfung KW 2026-W47 (Issue #179)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Prüfung | `dotnet list Reporter.sln package --outdated --include-transitive` erneut ausführen und mit `inventory/outdated.md` abgleichen | Erledigt | Re-Run 2026-11-17 identisch: nur 11.x-Major-Sprünge, keine 10.x-Patch/Minor-Updates |
| 2 | Prüfung | `dotnet list Reporter.sln package --vulnerable --include-transitive` und `--deprecated --include-transitive` erneut ausführen und mit `inventory/vulnerabilities.md` abgleichen | Erledigt | Re-Run 2026-11-17: keine verwundbaren, keine deprecated Pakete (direkt + transitiv) |
| 3 | Prüfung | Offene Dependabot-PRs prüfen (`gh pr list --author app/dependabot`) und ggf. 11.x-PRs für das Folge-Issue vormerken | Erledigt | `gh pr list --repo martin-stromberg/Reporter --author app/dependabot --state open` → keine Einträge |
| 4 | Bewertung | TFM-Anforderungen der 11.x-Zielpakete aus NuGet-Metadaten prüfen (Erwartung: `net11.0` erforderlich) | Erledigt | NuGet-nuspec verifiziert: `EFCore.*` 11.0.0 + `Maui.Controls` 11.0.10 nur `net11.0*`; `Logging.Debug`/`Syndication`/`TimeProvider.Testing`/`Data.Sqlite` 11.0.0 net10.0-kompatibel, ziehen aber 11.x-Stack → Begründung in `result-table.md` |
| 5 | Bewertung | Ergebnistabelle für alle 13 direkten Pakete erstellen (Befund, Empfehlung, Risiko) inkl. Abschnitt transitive Ausreißer | Erledigt | `{feat}/result-table.md` (14 Paket-IDs, 19 PackageReference-Einträge) |
| 6 | Folge-Issue | Folge-Issue „Migration auf .NET 11 und NuGet-Paketwelle 11.x" via `gh issue create` anlegen (TFMs, SDK, CI, MAUI-Workload, Timing-Empfehlung) | Erledigt — Issue #187 | Text vorbereitet: `{feat}/followup-issue.md` |
| 7 | Dokumentation | Kommentar auf Issue #179 mit Ergebnistabelle und Verlinkung des Folge-Issues posten (`gh issue comment 179`) | Erledigt — Kommentar gepostet | Text vorbereitet: `{feat}/issue-comment-179.md` (Platzhalter `{FOLGE_ISSUE}`) |
| 8 | Verifikation | `dotnet test --project src/Reporter.Tests/Reporter.Tests.csproj -c Release` ausführen (grün) | Erledigt | 798/798 Tests bestanden, 0 fehlgeschlagen (2026-11-17) |
| 9 | Verifikation | `.\scripts\Run-StaticChecks.ps1` ausführen — Exit-Code 0, keine Findings | Erledigt | Exit 0: Restore, Format, Lizenzheader, Security-Scan (NuGet + npm high), Release-Build 0 Warnungen/0 Fehler (2026-11-17) |
| 10 | Abschluss | Feature-Unterverzeichnis aufräumen und finalen Commit mit `Closes #179` erstellen | Offen | — |
| 11 | Abschluss | PR gegen `staging` erstellen; PR-Body mit Ergebnistabelle, aufgeschobenen Empfehlungen und Folge-Issue-Link | Offen | — |
| 12 | Abschluss | PR-Checks überwachen (Fail-fast); bei `fail` sofort `gh run view --job <id> --log-failed` diagnostizieren | Offen | — |
