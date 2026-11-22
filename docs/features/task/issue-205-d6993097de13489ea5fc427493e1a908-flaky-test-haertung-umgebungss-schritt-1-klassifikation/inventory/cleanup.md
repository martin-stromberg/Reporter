<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestehende Cleanup-/Deadline-Infrastruktur

## E2EProcessGuard

`E2EProcessGuard` bietet bereits:

- Windows-Job-Objekt mit `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` für Hard-Abbrüche des Testhosts.
- `TrackProcess` als best-effort Prozess-Guard.
- `KillAndWaitAsync(Process, TimeSpan?)` und `KillAndWaitAsync(int, TimeSpan?)` mit Default-Deadline von 30 Sekunden.
- `TryDeleteDirectoryAsync` mit fünf Versuchen und 200 ms Pause, ohne Teardown-Fehler nach außen zu werfen.

`DemoSeedTests` nutzt bereits die PID-basierte Kill-Variante und den best-effort Temp-Cleanup.

## Reporter.Tests

Es gibt keine zentrale Temp-Cleanup-Hilfe. Für diesen Schritt wird eine kleine, testprojektinterne Hilfe geplant, die das bewährte Muster aus `E2EProcessGuard.TryDeleteDirectoryAsync` übernimmt und einen injizierbaren Wartemechanismus besitzt, damit Tests den Retry-Pfad ohne lange Wartezeiten prüfen können.

## Deadlines

- Prozess-Teardown: bestehende 30-Sekunden-Default-Deadline.
- Temp-Cleanup: fünf Versuche mit kurzen Abständen.
- UI-Timeouts bleiben bestehen und werden nicht geändert.
