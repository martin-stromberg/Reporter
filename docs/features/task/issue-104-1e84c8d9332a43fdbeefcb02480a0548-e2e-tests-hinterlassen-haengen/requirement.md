<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: E2E-Tests hinterlassen hängenden `Reporter.exe`-Prozess (Issue #104)

## Fachliche Zusammenfassung

Nach einem Lauf der E2E-Suite (`scripts/Run-E2ETests.ps1`) bleibt wiederholt ein laufender `Reporter.exe`-Prozess zurück, der bisher manuell per `Stop-Process` bzw. `taskkill /F` beendet werden musste. Der Lebenszyklus der vom Test-Fixture gestarteten App-Instanz(en) ist so abzusichern, dass nach jedem Lauf — Erfolg, Fehlschlag und Abbruch des Test-Hosts — kein `Reporter.exe`-Prozess mehr existiert und keine Dateien des Build-Ausgabeverzeichnisses blockiert bleiben. Die neun bestehenden E2E-Tests und das fachliche Verhalten der App bleiben unverändert; die gefundene Ursache ist in `docs/help/tests/troubleshooting.md` zu dokumentieren.

## Betroffene Klassen und Komponenten

- `scripts/Run-E2ETests.ps1` — um eine Absicherung für den Fehler-/Abbruchfall erweitern (z. B. `try`/`finally` mit einem auf den Build-Output-Pfad `$appOutput` eingegrenzten `Stop-Process`), damit auch ein hart beendeter `dotnet test`-Test-Host keinen verwaisten `Reporter.exe` zurücklässt. Das Skript startet die App nicht selbst; es setzt nur `REPORTER_APP_PATH`.
- `src/Reporter.E2ETests/ReporterAppFixture.cs`:
  - `DisposeAsync` — aktuell nur `App.Kill()` (FlaUI-`Application`) unter `!App.HasExited`, alle Ausnahmen werden geschluckt, es gibt keinen `WaitForExit`-/Exit-Nachweis; harte Absicherung per `Process.Kill()` (ggf. `Kill(entireProcessTree: true)`) plus Exit-Verifikation ergänzen.
  - `InitializeAsync` — Fehlerpfade nach erfolgreichem `Process.Start`/`Application.Attach` prüfen (Attach-Fehler ist bereits abgesichert; Timeout bzw. `null`-Rückgabe von `GetMainWindow` sowie die 3-s-Pause/`SetForeground` nicht), damit kein Pfad den gestarteten Prozess entkommen lässt.
- `src/Reporter.E2ETests/DemoSeedTests.cs` — startet eine zweite, eigene `Reporter.exe`-Instanz; `process.Kill()` steht bereits im `finally`, ggf. ebenfalls um `WaitForExit`/Tree-Kill härten.
- `src/Reporter.E2ETests/E2ETestCollection.cs` — vermutlich unverändert (nur Kontext: geteiltes `ICollectionFixture<ReporterAppFixture>`, serielle Ausführung).
- `src/Reporter/` (App-Shutdown-Pfad, z. B. `App.xaml.cs` mit Fire-and-forget-Logging in `OnSleep`/Exception-Handlern sowie Hintergrunddienste `IAutoRefreshService`/`INetworkStatusService`/`IDebugLogService`) — nur falls die Analyse zeigt, dass die App beim Beenden hängt (Hypothese, zu verifizieren; aktuell kein `Application.Quit`-Pfad gefunden).
- `docs/help/tests/troubleshooting.md` — Abschnitt „Verwaiste Prozesse oder Temp-Verzeichnisse" um die gefundene Ursache und Lösung ergänzen, sofern ein Eintrag sinnvoll ist.
- Tests — kein neuer fachlicher Test erforderlich; die bestehenden neun E2E-Tests (`SmokeTests`, `ArticleLinkTests`, `DemoSeedTests`) müssen unverändert bestehen. Der Nachweis erfolgt über Prozess-Freiheit (`Get-Process Reporter`) nach mehreren Läufen inkl. Fehlschlag und Abbruch.

## Implementierungsansatz

1. Ursachenanalyse vor dem Fix (Reihenfolge der Hypothesen):
   - Prüfen, ob `DisposeAsync` den tatsächlichen `Reporter.exe`-PID erwischt — FlaUI ersetzt beim Attach/Fenster-Wait ggf. das interne `Process`-Objekt (siehe Kommentar in `InitializeAsync`); ein davon abweichender Prozess könnte entkommen.
   - Prüfen, ob `App.Kill()` selbst eine Ausnahme wirft (wird aktuell still geschluckt) oder der Prozess nach `Kill()` nicht terminiert — es erfolgt kein `WaitForExit`.
   - Prüfen, ob die mit `UseShellExecute = false` gestartete App geerbte Konsolen-/Pipe-Handles hält, die Terminierung oder Exit-Erkennung verhindern (stdout/stderr werden nicht umgeleitet).
   - Prüfen, ob bei Abbruch (`Ctrl+C`, Test-Host-Kill) `DisposeAsync` überhaupt läuft — andernfalls ist die Skript-Ebene zuständig.
   - Prüfen, ob die MAUI-App selbst beim Schließen hängt (Hintergrunddienste, Shutdown-Pfad).
   - Vor der Reproduktion prüfen, ob bereits ein `Reporter.exe`-Leichnam aus früheren Läufen läuft, und diesen ggf. vorab beenden.
2. Fixture-Härtung: nach dem FlaUI-`Kill()`/`Close()` die PID via `Process.GetProcessById` verifizieren und bei Bedarf `Process.Kill()`/`Kill(entireProcessTree: true)` mit `WaitForExit(Timeout)` nachziehen; gleiche Behandlung für die `DemoSeedTests`-Instanz.
3. Skript-Härtung: `dotnet test` in `try`/`finally` kapseln; im `finally` verbliebene `Reporter`-Prozesse gezielt beenden — eingegrenzt auf den Pfad `$appOutput`, damit eine parallel laufende Nutzer-Installation nicht beendet wird.
4. Optional/alternativ (vor Umsetzung entscheiden): Windows Job Object im Test-Fixture, damit der App-Prozess automatisch mit dem Test-Host stirbt — deckt auch harte Abbruchsignale ab.
5. Dokumentation der gefundenen Ursache im Troubleshooting-Abschnitt zu verwaisten Prozessen.
6. Verifikation: `.\scripts\Run-E2ETests.ps1` mehrfach erfolgreich sowie je einmal mit Test-Fehlschlag und mit Abbruch ausführen; nach jedem Lauf `Get-Process Reporter` leer. `dotnet test Reporter.sln --filter "Category!=E2E"` und `npm test` müssen unverändert grün bleiben; abschließend `.\scripts\Run-StaticChecks.ps1` fehlerfrei.

## Konfiguration

Keine neue Konfiguration erforderlich. Die bestehenden Env-Overrides (`REPORTER_APP_PATH`, `REPORTER_FEEDSEARCH_ENDPOINT`, `REPORTER_DB_PATH`, `REPORTER_DISABLE_DEMO_SEED`) bleiben unverändert.

## Offene Fragen

- Tritt das Leck auch nach erfolgreichen Läufen auf (dann liegt die Ursache im `DisposeAsync`/FlaUI-`App.Kill()`-Pfad) oder nur bei Fehlschlag/Abbruch (dann genügt ggf. die Skript-/`finally`-Absicherung)? — im Rahmen der Ursachenanalyse zu klären.
- Darf das Skript-Cleanup grundsätzlich alle `Reporter.exe`-Prozesse beenden, oder muss es auf den Build-Output-Pfad eingegrenzt werden? — Annahme: Eingrenzung auf `$appOutput`, um eine parallel laufende App-Instanz des Anwenders nicht zu beenden.
- Ist ein Windows Job Object als Lösungsansatz zulässig (native Abhängigkeit/P/Invoke im Testprojekt), oder soll ausschließlich im PowerShell-`finally` aufgeräumt werden?
