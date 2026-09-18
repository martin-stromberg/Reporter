<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

- [x] `E2EProcessGuard` (`internal static` Klasse, `src/Reporter.E2ETests/E2EProcessGuard.cs`) — angelegt mit kernel32-P/Invoke (`CreateJobObject`, `SetInformationJobObject` mit `JOBOBJECT_EXTENDED_LIMIT_INFORMATION`/`JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`, `AssignProcessToJobObject`, `CloseHandle`) und lazy prozessweiter Job-Instanz (`Lazy<IntPtr> Job`, `E2EProcessGuard.cs:30`)
- [x] Methode `TrackProcess(Process)` in `E2EProcessGuard` — vorhanden (`E2EProcessGuard.cs:39`), best-effort mit `[E2E]`-Konsolenwarnung bei fehlgeschlagener Job-Zuweisung oder fehlendem Job
- [x] Methode `KillAndWaitAsync` in `E2EProcessGuard` — vorhanden als Overloads für `Process` (`E2EProcessGuard.cs:74`) und `int`-PID (`E2EProcessGuard.cs:121`); `Kill(entireProcessTree: true)` plus Timeout-`WaitForExitAsync`, `bool`-Rückgabe als Exit-Bestätigung, `[E2E]`-Warnung bei nicht bestätigtem Exit
- [x] Feld `_appProcessId` (`int?`) in `ReporterAppFixture` — vorhanden (`ReporterAppFixture.cs:20`), wird in `StartAppProcess` direkt nach `Process.Start` gesetzt (`ReporterAppFixture.cs:170`)
- [x] Methode `InitializeAsync` in `ReporterAppFixture` — geändert: PID gemerkt, `E2EProcessGuard.TrackProcess(process)` nach dem Start, alle Folgeschritte (`UIA3Automation`, `Application.Attach`, Exit-Watcher, `GetMainWindow`-Wait, 3-s-Pause, `SetForeground`) in `AttachAndWaitForMainWindowAsync` unter umschließendem `try`/`catch`; `catch` ruft `KillAppProcessAsync` (Kill-Backstop) und rethrowed — ersetzt den Attach-only-Catch (`ReporterAppFixture.cs:49-75`)
- [x] Methode `DisposeAsync` in `ReporterAppFixture` — geändert: `App.Kill()` ersetzt durch `KillAppProcessAsync` → `E2EProcessGuard.KillAndWaitAsync` mit PID aus `App?.ProcessId` und Fallback `_appProcessId` (`ReporterAppFixture.cs:102-117`, `220-236`); nicht bestätigter Exit wird vom Guard als `[E2E]`-Warnung protokolliert; restlicher Teardown unverändert
- [x] Methode `FirstStart_SeedsNewsCategoryAndDemoFeed` in `DemoSeedTests` — geändert: `E2EProcessGuard.TrackProcess(process)` direkt nach `Process.Start` (`DemoSeedTests.cs:61`); im `finally` ersetzt `E2EProcessGuard.KillAndWaitAsync(processId)` das bisherige `process.Kill()` (`DemoSeedTests.cs:92`); `process.Dispose()` und best-effort Temp-Löschung erhalten
- [x] `scripts/Run-E2ETests.ps1` — geändert: `dotnet test` in `try`/`finally` gekapselt, Test-Exit-Code in `$testExitCode` gemerkt, im `finally` werden `Reporter`-Prozesse per `Path -ieq $appOutput` eingegrenzt und mit `Stop-Process -Force` beendet und protokolliert, Skript endet mit `exit $testExitCode` (`Run-E2ETests.ps1:51-71`)
- [x] `docs/help/tests/troubleshooting.md` — Abschnitt „Verwaiste Prozesse oder Temp-Verzeichnisse" erweitert: alle drei Ursachen (Test-Host-Tod ohne `DisposeAsync`, ungesicherte `InitializeAsync`-Fehlerpfade, fehlende Exit-Verifikation nach `App.Kill()`) benannt, dreifache Absicherung (Job Object, verifizierter Tree-Kill, Skript-`finally` mit `$appOutput`-Eingrenzung) dokumentiert, manuelle Bereinigung als Fallback erhalten
- [x] Verifikationslauf Erfolg (Task 11) — ausgeführt, Beleg in `test-results.md`: 2 reguläre `Run-E2ETests.ps1`-Läufe, `Get-Process Reporter` jeweils leer; die beobachteten Temp-Leichname (+4) wurden als Defekte identifiziert und in Iteration 2 behoben (Tasks 15–17)
- [x] Verifikationslauf Fehlschlag (Task 12) — ausgeführt, Beleg in `test-results.md`: beide Suite-Läufe mit je 2 scheiternden Tests, danach 0 `Reporter`-Prozesse
- [x] Verifikationslauf Abbruch (Task 13) — ausgeführt, Beleg in `test-results.md`: `testhost.exe` (PID 32632) hart gekillt während `Reporter.exe` (PID 34132) lief; `KILL_ON_JOB_CLOSE` hat die App mitterminiert, danach kein `Reporter`-Prozess — das reproduzierte Leck-Szenario ist nachgewiesen geschlossen
- [x] Regression (Task 14) — Beleg in `test-results.md`: `dotnet test --filter "Category!=E2E"` 566/566, `npm test` 36/36, `Run-StaticChecks.ps1` Exit 0

## Hinweise

- **Iteration-2-Änderungen sind plangemäße Befundskorrekturen** aus der ersten Testrunde (`test-results.md`), keine neuen Planelemente. Sie wurden als ergänzende Tasks 15–18 in der Tasks-Datei aufgenommen:
  - `KillAndWaitAsync(Process)` wirft nicht mehr selbst bei assoziationslosem `Process`-Objekt (PID vorab via `TryGetProcessId` erfasst, `E2EProcessGuard.cs:81`); `DemoSeedTests.finally` nutzt die robuste PID-Überladung — behebt den `InvalidOperationException`-Defekt, der das Demo-Teardown maskierte.
  - `E2EProcessGuard.TryDeleteDirectoryAsync` (`E2EProcessGuard.cs:159`) ersetzt das planseitig als „unverändert" beschriebene einmalige `Directory.Delete` — retry-basierte Löschung, weil kurz gelockte DB-Dateien Temp-Leichname erzeugten (bewusste Planabweichung im Sinne des Akzeptanzkriteriums „kein neuer Temp-Leichnam").
  - `FeedDbAssertions` nutzt `Pooling=False` (`FeedDbAssertions.cs:113`) — gepoolte SQLite-Verbindungen hielten `reporter.db` im Test-Host offen und blockierten die Temp-Löschung.
  - `E2EProcessGuardTests.cs` (8 Tests) — der Plan sah „keine neuen Tests" vor; die Regressionstests sichern die Befundskorrekturen ab und tragen bewusst kein `Category=E2E`-Trait, laufen also im regulären `Category!=E2E`-Lauf mit.
- **Kein vollständig grüner 9/9-E2E-Lauf erreicht:** `SmokeTests.FeedActionSheet_ChangeCategory_IncludingNone` schlug in beiden Läufen fehl (`TimeoutException`, Action-Sheet-Eintrag „Kategorie ändern" nicht gefunden, `SmokeTests.cs:251`). `SmokeTests.cs` ist im Feature-Diff unverändert — feature-unabhängiger UI-/Timing-Flake, kein Prozess-Leck. `Get-Process Reporter` war dennoch nach jedem Lauf leer.
- **Nach-Fix-Wiederholungslauf nicht dokumentiert:** `test-results.md` belegt die erste Testrunde; die Temp-Leichname-Ursachen wurden danach behoben und sind durch `E2EProcessGuardTests` abgesichert, ein dokumentierter kompletter Suite-Lauf nach den Fixes liegt nicht vor. Falls ein abschließender Clean-Run-Beleg gewünscht ist, `.\scripts\Run-E2ETests.ps1` erneut ausführen (interaktive Session) — der bekannte Flake kann dabei weiterhin fehlschlagen.
- Alle Änderungen liegen uncommittet im Arbeitsbaum vor (letzter Commit ist der Planungscommit `db72fee`); die Prüfung erfolgte gegen den Arbeitsstand.
