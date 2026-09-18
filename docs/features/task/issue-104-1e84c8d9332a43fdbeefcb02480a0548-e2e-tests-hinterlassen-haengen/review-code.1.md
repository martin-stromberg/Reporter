<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### E2EProcessGuard.cs (E2EProcessGuard)

- **Fehlerbehandlung** — `TrackProcess` (Zeile 41): `var job = Job.Value;` wird außerhalb des `try`-Blocks ausgewertet. Wirft `CreateKillOnCloseJob` eine Exception (z. B. `OutOfMemoryException` aus `Marshal.AllocHGlobal`, P/Invoke-Fehler), propagiert sie nach außen — entgegen dem dokumentierten Vertrag „is reported but never throws". Zusätzlich cached `Lazy<T>` die Exception, sodass jeder weitere `TrackProcess`-Aufruf erneut wirft und alle folgenden App-Prozesse ungeschützt bleiben.

  Empfehlung: `var job = Job.Value;` in den `try`-Block verschieben (oder `CreateKillOnCloseJob` intern abfangen und `IntPtr.Zero` zurückgeben).

- **Fehlerbehandlung / Ressourcenleck** — `CreateKillOnCloseJob` (Zeilen 139–174): Das Job-Handle wird nur im `SetInformationJobObject`-Fehlerpfad per `CloseHandle` freigegeben. Wirft eine Operation zwischen `CreateJobObject` und `return handle` (`AllocHGlobal`, `StructureToPtr`), leakt das Handle — und vergiftet über `Lazy` zusätzlich `Job` (siehe vorheriger Befund).

  Empfehlung: Methodenrumpf in `try/catch` fassen, der bei jeder Exception `CloseHandle(handle)` aufruft und `IntPtr.Zero` zurückgibt.

### ReporterAppFixture.cs (ReporterAppFixture)

- **God-Methode** — `InitializeAsync` (Zeilen 49–127) ist ~79 Zeilen lang und kettet mehrere konzeptuell getrennte Aufgaben: Temp-Verzeichnis anlegen, Prozess starten, FlaUI attachen, Watcher-Task registrieren, Main-Window-Wait, Pause, `SetForeground`, plus Backstop-Cleanup.

  Empfehlung: Teilschritte in private Methoden auslagern, z. B. `StartAppProcess()` (StartInfo + Start + `TrackProcess`) und `AttachAndWaitForMainWindowAsync()`.

### DemoSeedTests.cs (DemoSeedTests)

- **Fehlerbehandlung** — `using var automation = new UIA3Automation();` (Zeile 62) steht vor dem `try/finally`. Wirft der Konstruktor, läuft das `finally` nicht und die gestartete `Reporter.exe` bleibt bis zum Test-Host-Ende liegen — genau die Lückenklasse, für die der Backstop in `ReporterAppFixture.InitializeAsync` eingebaut wurde.

  Empfehlung: Die Automation-Erzeugung in den `try`-Block verschieben bzw. den `try` direkt nach `Process.Start`/`TrackProcess` beginnen.

- **Fehlerbehandlung / Konsistenz** — `finally` ruft `KillAndWaitAsync(process)` (Zeile 90) mit dem `Process`-Objekt auf, das zuvor an `Application.Attach(process)` übergeben wurde und beim `using var app`-Dispose (Ende des `try`-Scopes, vor dem `finally`) von FlaUI disposed/ersetzt werden kann. Zugriffe wie `HasExited`/`Kill` können dann `InvalidOperationException`/`ObjectDisposedException` werfen — der Kill entfällt und wird nur als `[E2E]`-Warnung protokolliert. `ReporterAppFixture` umgeht das bewusst über den PID-basierten Overload.

  Empfehlung: `var processId = process.Id;` direkt nach dem Start merken und im `finally` `E2EProcessGuard.KillAndWaitAsync(processId)` aufrufen — analog zur Fixture.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Reporter.E2ETests/E2EProcessGuard.cs`
- `src/Reporter.E2ETests/ReporterAppFixture.cs`
- `src/Reporter.E2ETests/DemoSeedTests.cs`
- `scripts/Run-E2ETests.ps1`
- `docs/help/tests/troubleshooting.md`
