<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: E2E-Tests hinterlassen hängenden `Reporter.exe`-Prozess (Issue #104)

Analysiert wurden der Prozess-Lebenszyklus der von den E2E-Tests gestarteten `Reporter.exe`-Instanzen (`src/Reporter.E2ETests/`, `scripts/Run-E2ETests.ps1`) sowie der Shutdown-Pfad der App (`src/Reporter/`), bezogen auf die Anforderung, nach jedem E2E-Lauf — Erfolg, Fehlschlag und Abbruch — keinen `Reporter.exe`-Prozess und keine blockierten Build-Output-Dateien zurückzulassen.

## Zusammenfassung

- **`scripts/Run-E2ETests.ps1` startet die App nicht selbst**, sondern baut sie (Debug/`win-x64`), setzt `REPORTER_APP_PATH` und ruft `dotnet test` auf — **ohne `try`/`finally` und ohne jegliches Prozess-Cleanup**. Es existiert keine Skript-Ebene, die verwaiste Prozesse einsammelt.
- **`ReporterAppFixture.DisposeAsync` ruft nur `App.Kill()`** unter `!App.HasExited` auf, schluckt alle Ausnahmen und verzichtet auf `WaitForExit`/Exit-Verifikation sowie `Kill(entireProcessTree)`. FlaUI ersetzt beim Attach/Fenster-Wait das interne `Process`-Objekt — der Prozess ist ausschließlich über `App` erreichbar (Code-Kommentar `ReporterAppFixture.cs:68–69`).
- **`InitializeAsync` sichert nur den `Application.Attach`-Fehlerpfad lokal ab** (best-effort `process.Kill()`). Alle anderen Fehlerpfade nach `Process.Start` — `new UIA3Automation()` vor dem Attach (`App` bleibt `null`), der 2-min-`GetMainWindow`-Wait, `SetForeground` — haben keinen eigenen Cleanup; bei Pfaden vor der `App`-Zuweisung greift zusätzlich der `App is not null`-Guard in `DisposeAsync` nicht (Details in `logic.md`).
- **`DemoSeedTests` startet eine zweite `Reporter.exe`** mit eigenem `finally`-Cleanup — gleiche Härte-Defizite: `process.Kill()` ohne `WaitForExit`, ohne Tree-Kill.
- **App-Shutdown-Pfad:** kein `Application.Quit`-, `Window.Closed`- oder `Destroying`-Handler vorhanden; `IAutoRefreshService.StopAsync` wird nirgends aufgerufen. Die laufenden Hintergrundarbeiten (PeriodicTimer-Loop, Fire-and-forget-Logging) laufen auf ThreadPool-Threads und blockieren den Prozess-Exit nicht — eine „hängende App beim regulären Beenden" ist im Ist-Code nicht ersichtlich, wurde aber auch nicht produktiv verifiziert (kein `Application.Quit`-Pfad existiert, `App.Kill()` terminiert hart).
- **Beleg aus der Umgebung:** 37 verwaiste `%TEMP%/reporter-e2e-*`-Verzeichnisse aus früheren Läufen (19 reguläre + 18 `reporter-e2e-demo-*`) — konsistent mit Teardowns, die den Prozess bzw. die Dateilocks nicht erwischten.

**Test-Ausgangszustand:** Unit-Tests grün (566/566 via `dotnet test Reporter.sln --filter "Category!=E2E"`, Exit 0) und Node-Tests grün (36/36 via `npm test`, Exit 0). **Prozess-Leck reproduziert:** nach hartem Kill des `testhost.exe` während eines E2E-Laufs lief `Reporter.exe` weiter; nach regulären Läufen (inkl. Test-Fehlschlag) endete der Prozess sauber via `App.Kill()`. Ein bekannter Test-Fehlschlag: `SmokeTests.AppStarts_FeedListRenders` scheiterte 2× reproduzierbar als isolierter Einzellauf (20-s-Timeout, kein gerenderter Inhalt), bestand aber im Gruppenlauf — Warm-up-/Timing-Verdacht, Ursache ungeklärt. Testlücken: `ArticleLinkTests`, `DemoSeedTests`, `AddButton_OpensSheet_FocusesUrlEntry` (Abbruch-Zeitpunkt) und der vollständige `Run-E2ETests.ps1`-Durchlauf wurden nicht ausgeführt — Details und Nachweise in [Tests](inventory/tests.md).

## Details

- [Logik — Prozess-Lebenszyklus, Skript, App-Shutdown-Pfad](inventory/logic.md)
- [Interfaces — shutdown-relevante Contracts](inventory/interfaces.md)
- [Tests — Ausgangszustand, Läufe, Fehler, Lücken, Leck-Reproduktion](inventory/tests.md)
