<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

- [x] `E2EProcessGuard` (`internal static` Klasse, `src/Reporter.E2ETests/E2EProcessGuard.cs`) — angelegt mit kernel32-P/Invoke (`CreateJobObject`, `SetInformationJobObject` mit `JOBOBJECT_EXTENDED_LIMIT_INFORMATION`/`JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`, `AssignProcessToJobObject`, `CloseHandle`) und lazy prozessweiter Job-Instanz
- [x] Methode `TrackProcess(Process)` in `E2EProcessGuard` — vorhanden, best-effort mit `[E2E]`-Konsolenwarnung bei fehlgeschlagener Job-Zuweisung oder fehlendem Job
- [x] Methode `KillAndWaitAsync` in `E2EProcessGuard` — vorhanden als Overloads für `Process` und `int`-PID; `Kill(entireProcessTree: true)` plus Timeout-`WaitForExitAsync`, `bool`-Rückgabe als Exit-Bestätigung, `[E2E]`-Warnung bei nicht bestätigtem Exit
- [x] Feld `_appProcessId` (`int?`) in `ReporterAppFixture` — vorhanden, wird direkt nach `Process.Start` gesetzt
- [x] Methode `InitializeAsync` in `ReporterAppFixture` — geändert: PID gemerkt, `E2EProcessGuard.TrackProcess(process)` nach dem Start, alle Folgeschritte (`UIA3Automation`, `Application.Attach`, Exit-Watcher, `GetMainWindow`-Wait, 3-s-Pause, `SetForeground`) in `try`/`catch`; `catch` ruft `KillAndWaitAsync(App?.ProcessId ?? _appProcessId.Value)` und rethrowed — ersetzt den Attach-only-Catch
- [x] Methode `DisposeAsync` in `ReporterAppFixture` — geändert: `App.Kill()` ersetzt durch `E2EProcessGuard.KillAndWaitAsync` mit PID aus `App?.ProcessId` und Fallback `_appProcessId`; nicht bestätigter Exit wird vom Guard als `[E2E]`-Warnung protokolliert; restlicher Teardown unverändert
- [x] Methode `FirstStart_SeedsNewsCategoryAndDemoFeed` in `DemoSeedTests` — geändert: `E2EProcessGuard.TrackProcess(process)` direkt nach `Process.Start`; im `finally` ersetzt `KillAndWaitAsync(process)` das bisherige `process.Kill()`; `process.Dispose()` und best-effort Temp-Löschung unverändert
- [x] `scripts/Run-E2ETests.ps1` — geändert: `dotnet test` in `try`/`finally` gekapselt, Test-Exit-Code in `$testExitCode` gemerkt, im `finally` werden `Reporter`-Prozesse per `Path -ieq $appOutput` eingegrenzt und mit `Stop-Process -Force` beendet und protokolliert, Skript endet mit `exit $testExitCode`
- [x] `docs/help/tests/troubleshooting.md` — Abschnitt „Verwaiste Prozesse oder Temp-Verzeichnisse" erweitert: alle drei Ursachen (Test-Host-Tod ohne `DisposeAsync`, ungesicherte `InitializeAsync`-Fehlerpfade, fehlende Exit-Verifikation) benannt, dreifache Absicherung (Job Object, verifizierter Tree-Kill, Skript-`finally` mit `$appOutput`-Eingrenzung) dokumentiert, manuelle Bereinigung als Fallback erhalten

## Offene Aufgaben

- [ ] Verifikationslauf Erfolg (Task 11) — nicht ausgeführt: `.\scripts\Run-E2ETests.ps1` erfolgreich, danach `Get-Process Reporter` leer und kein neuer `%TEMP%/reporter-e2e-*`-Leichnam; erfordert interaktive Windows-Desktop-Session
- [ ] Verifikationslauf Fehlschlag (Task 12) — nicht ausgeführt: Lauf mit mindestens einem scheiternden Test, danach `Get-Process Reporter` leer
- [ ] Verifikationslauf Abbruch (Task 13) — nicht ausgeführt: harter `testhost.exe`-Kill während des Laufs, danach `Get-Process Reporter` leer — der eigentliche Nachweis der Job-Object-Absicherung
- [ ] Regression (Task 14) — kein Ausführungsbeleg im Repo: `dotnet test Reporter.sln --filter "Category!=E2E"`, `npm test` und `.\scripts\Run-StaticChecks.ps1` müssen grün laufen

## Hinweise

- Alle geplanten Code- und Dokumentationsänderungen (Umsetzungsschritte 1–5 des Plans) sind vollständig implementiert; die neun bestehenden E2E-Tests blieben fachlich unverändert.
- Die offenen Punkte sind ausschließlich die manuellen Verifikationsläufe (Schritt 6 des Plans) — sie lassen sich nicht aus dem Code ableiten und benötigen eine interaktive Windows-Desktop-Session; im Repo liegt kein `test-results.md` oder sonstiger Ausführungsbeleg vor.
- Zum Review-Zeitpunkt lief kein `Reporter`-Prozess und es existierten keine `%TEMP%/reporter-e2e-*`-Leichname.
- Empfohlene Reihenfolge für die Nachverifikation: zuerst Task 14 (Regression ohne E2E), dann Tasks 11–13 (E2E-Läufe inkl. Abbruch-Szenario); dabei den bekannten Warm-up-Flake `SmokeTests.AppStarts_FeedListRenders` von einem Prozess-Leck unterscheiden.
