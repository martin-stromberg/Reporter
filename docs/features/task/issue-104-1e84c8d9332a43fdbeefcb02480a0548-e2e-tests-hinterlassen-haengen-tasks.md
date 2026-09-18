<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: E2E-Tests hinterlassen hängenden `Reporter.exe`-Prozess (Issue #104)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Prozess-Guard | `E2EProcessGuard` in `src/Reporter.E2ETests/` anlegen: kernel32-P/Invoke (`CreateJobObject`, `SetInformationJobObject` mit `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`, `AssignProcessToJobObject`, `CloseHandle`) und lazy prozessweite Job-Instanz | Offen | — |
| 2 | Prozess-Guard | `E2EProcessGuard.TrackProcess(Process)` implementieren: Prozess dem Job zuweisen, best-effort mit `[E2E]`-Konsolenwarnung bei Fehlschlag | Offen | — |
| 3 | Prozess-Guard | `E2EProcessGuard.KillAndWaitAsync` implementieren: `Kill(entireProcessTree: true)` für `Process`/PID plus `WaitForExit` mit Timeout und Exit-Bestätigung | Offen | — |
| 4 | Fixture | `ReporterAppFixture`: Feld `_appProcessId` ergänzen und nach `Process.Start` setzen; `E2EProcessGuard.TrackProcess` aufrufen | Offen | — |
| 5 | Fixture | `ReporterAppFixture.InitializeAsync`: alle Schritte nach `Process.Start` in `try`/`catch` hüllen; `catch` ruft `KillAndWaitAsync` (PID/`App.ProcessId`) und rethrowed — ersetzt den Attach-only-Catch | Offen | — |
| 6 | Fixture | `ReporterAppFixture.DisposeAsync`: `App.Kill()` durch `KillAndWaitAsync` ersetzen (PID aus `App?.ProcessId`, Fallback `_appProcessId`); bei nicht bestätigtem Exit `[E2E]`-Warnung ausgeben | Offen | — |
| 7 | Tests | `DemoSeedTests.FirstStart_SeedsNewsCategoryAndDemoFeed`: `TrackProcess` nach `Process.Start`; `finally` auf `KillAndWaitAsync` umstellen | Offen | — |
| 8 | Skript | `Run-E2ETests.ps1`: `dotnet test` in `try`/`finally` kapseln, Test-Exit-Code vor `finally` merken, mit `exit` weiterreichen | Offen | — |
| 9 | Skript | `Run-E2ETests.ps1`: im `finally` `Reporter`-Prozesse mit `Path -ieq $appOutput` per `Stop-Process -Force` beenden und protokollieren | Offen | — |
| 10 | Dokumentation | `docs/help/tests/troubleshooting.md`: Abschnitt „Verwaiste Prozesse oder Temp-Verzeichnisse" um gefundene Ursache und eingebaute Absicherung ergänzen | Offen | — |
| 11 | Verifikation | Erfolgs-Lauf `.\scripts\Run-E2ETests.ps1`: danach `Get-Process Reporter` leer, kein neues `%TEMP%/reporter-e2e-*`-Leichnam | Offen | — |
| 12 | Verifikation | Fehlschlag-Lauf (mind. ein scheiternder Test): danach `Get-Process Reporter` leer | Offen | — |
| 13 | Verifikation | Abbruch-Lauf (harter `testhost.exe`-Kill): danach `Get-Process Reporter` leer | Offen | — |
| 14 | Verifikation | Regression: `dotnet test Reporter.sln --filter "Category!=E2E"`, `npm test` und `.\scripts\Run-StaticChecks.ps1` grün | Offen | — |
