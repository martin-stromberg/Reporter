<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Cleanup- und Deadline-Bestand

| Komponente | Vorhandenes Verhalten | Bewertung |
|-----------|-----------------------|-----------|
| `E2EProcessGuard` | Windows-Job-Objekt mit `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`, `Kill(entireProcessTree: true)`, `WaitForExitAsync` mit Standard-Timeout 30 s, `TryDeleteDirectoryAsync` mit Retry/Backoff und Logging statt Throw. | Entspricht weitgehend der geforderten Härtung; wiederverwenden statt duplizieren. |
| `ReporterAppFixture` | Startet echte `Reporter.exe`, wartet bis 2 min auf Hauptfenster, hängt Prozess an Guard, killt in `DisposeAsync`, löscht Temp-Ordner best-effort über Guard. | Host-Budget > Child-/Phase-Budget erfüllt; Cleanup bereits richtig. |
| `UiRetry` | Default-Lookup-Timeout 20 s, Poll-Intervall 200 ms, UIA-Ausnahmen werden als „noch nicht bereit" behandelt. | Deterministisches Budget dokumentieren; kein eingebautes Host-vs-Kind-Problem. |
| `FeedDbAssertions` | DB-Polling meist 15 s; SQLite-Schreibzugriffe mit bis zu 5 Retries à 500 ms. | Budget liegt unter Fixture-/Host-Fenstern; Regel einhalten. |
| `DemoSeedTests` | Startet eigene `Reporter.exe`, verwendet eigenes Temp-Verzeichnis und finally-Kill/Delete. | Besonders OS-sensitiv; sicherstellen, dass Prozess vor Verzeichnisdelete beendet wird und Fehler den Assert nicht maskieren. |
| `Run-E2ETests.ps1` | Baut App, führt E2E-Projekt aus und stoppt im `finally` verbliebene `Reporter.exe` aus dem exakten Build-Pfad. | Lokale Härtung vorhanden; dokumentieren. |

## Regel für neue Tests

Host-Poll-, Fixture- oder Cleanup-Deadlines müssen größer sein als das Budget der jeweiligen Kind-/Phasenaktion. Fehler aus Cleanup werden geloggt/diagnostiziert, aber nicht als `AggregateException` über den eigentlichen Testfehler geworfen.
