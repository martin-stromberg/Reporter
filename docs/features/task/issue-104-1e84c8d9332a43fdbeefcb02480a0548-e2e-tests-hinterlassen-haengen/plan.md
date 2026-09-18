<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: E2E-Tests hinterlassen hängenden `Reporter.exe`-Prozess (Issue #104)

## Übersicht

Der Lebenszyklus der von der E2E-Suite gestarteten `Reporter.exe`-Instanzen wird dreifach abgesichert: (1) ein Windows-Job-Objekt mit `KILL_ON_JOB_CLOSE` im Testprojekt, das alle vom Test-Host gestarteten App-Prozesse auch bei hartem Abbruch des `testhost.exe` mitsterben lässt; (2) gehärtete Kill-/Exit-Verifikation in `ReporterAppFixture.DisposeAsync`, den `InitializeAsync`-Fehlerpfaden und dem `finally` von `DemoSeedTests`; (3) ein `try`/`finally` in `scripts/Run-E2ETests.ps1`, das verbliebene `Reporter`-Prozesse — eingegrenzt auf den Build-Output-Pfad `$appOutput` — beendet. Keine Änderung am fachlichen Verhalten der App und an den neun bestehenden E2E-Tests; die Ursache wird in `docs/help/tests/troubleshooting.md` dokumentiert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Absicherung gegen harten Test-Host-Abbruch | Windows Job Object (`JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`) via P/Invoke im Testprojekt (`E2EProcessGuard`) | Einzig der Kernel garantiert den Tod der App-Prozesse, wenn `testhost.exe` hart beendet wird — das reproduzierte Leck-Szenario. Deckt zusätzlich direkte `dotnet test`-/IDE-Läufe ab, die nicht über `Run-E2ETests.ps1` laufen und vom Skript-`finally` nicht erreicht würden. Das Testprojekt ist ohnehin Windows-only (`net10.0-windows10.0.19041.0`, FlaUI UIA3); kernel32-P/Invoke benötigt kein NuGet-Paket. |
| Skript-Cleanup-Scope | Pfadvergleich: nur `Reporter`-Prozesse beenden, deren `Path` exakt `$appOutput` entspricht | Eine parallel laufende Nutzer-Installation (`Reporter.exe` aus einem anderen Verzeichnis) darf nicht beendet werden — beantwortet die Scope-Frage aus der Anforderung mit der dort genannten Annahme. |
| Kill-Härtung in Fixture/Test | `Process.Kill(entireProcessTree: true)` + `WaitForExit` mit Timeout, gekapselt in `E2EProcessGuard.KillAndWaitAsync` | Der Ist-Stand ruft `Kill()` ohne Exit-Nachweis auf und schluckt alle Ausnahmen; ein überlebender Prozess bliebe unsichtbar. Tree-Kill statt Single-Kill, weil FlaUI das interne `Process`-Objekt beim Fenster-Wait ersetzen kann und ggf. Kindprozesse entkommen würden. Timeout-basiertes `WaitForExit` verhindert ein ewiges Hängen des Teardowns. |
| `InitializeAsync`-Fehlerpfade | Ein umschließender `catch` um alle Schritte nach `Process.Start` (Attach, Fenster-Wait, 3-s-Pause, `SetForeground`), der den gestarteten Prozess via `KillAndWaitAsync` beendet und rethrowed | Heute ist nur der Attach-Fehler lokal abgesichert; bei Fehlern vor der `App`-Zuweisung greift auch der `App is not null`-Guard in `DisposeAsync` nicht (xunit-2.9.3-Verhalten bei Collection-Fixture-Init-Fehlschlag nicht garantiert). Der Backstop-Catch ist runner-unabhängig. |
| Job-Zuweisung | Eine prozessweit geteilte Job-Instanz (`E2EProcessGuard` statisch, lazy); Zuweisung best-effort mit Warnung statt hartem Fehler | Läuft der Test-Host selbst in einem Job (z. B. Test-Explorer/CI-Agent), kann `AssignProcessToJobObject` scheitern — dann muss die Suite trotzdem laufen, das Skript-`finally` und der Kill-Backstop bleiben als zweite Linie. Explizites `Dispose` ist nicht nötig: das Handle muss bis zum Test-Host-Ende offen bleiben und wird beim Prozess-Exit vom OS geschlossen — genau dann wirkt `KILL_ON_JOB_CLOSE`. |
| App-Shutdown-Pfad (`src/Reporter/`) | Keine Änderung | Die Bestandsaufnahme zeigt: Hintergrundarbeiten (`IAutoRefreshService`-Loop, Fire-and-forget-Logging) laufen auf ThreadPool-Threads und blockieren den Prozess-Exit nicht; ein hängendes reguläres Beenden ist im Ist-Code nicht ersichtlich. Nach der Fixture-Härtung (`WaitForExit`-Nachweis + Tree-Kill-Fallback) wäre ein hängender Prozess zusätzlich erkenn- und behebbar. |

## Programmabläufe

### App-Start im Fixture (`InitializeAsync`)

1. `Server.InitializeAsync` startet den `StubFeedServer` (unverändert).
2. Temp-Verzeichnis `%TEMP%/reporter-e2e-{guid}` und `DatabasePath` werden angelegt (unverändert).
3. `ProcessStartInfo` mit `REPORTER_FEEDSEARCH_ENDPOINT`, `REPORTER_DB_PATH`, `REPORTER_DISABLE_DEMO_SEED=1`; `Process.Start` (unverändert).
4. **Neu:** gestartete PID in `_appProcessId` merken und Prozess via `E2EProcessGuard.TrackProcess(process)` dem Kill-on-Close-Job zuweisen.
5. **Neu:** alle folgenden Schritte (`new UIA3Automation()`, `Application.Attach`, Exit-Watcher-Registrierung, `GetMainWindow`-Wait, 3-s-Pause, `SetForeground`) laufen in einem `try`-Block; ein umschließender `catch` ruft `E2EProcessGuard.KillAndWaitAsync` für die gemerkte PID bzw. `App.ProcessId` auf und rethrowed — ersetzt den bisherigen Attach-only-Catch.
6. Erfolgspfad unverändert: `MainWindow` gesetzt, Suite läuft.

Beteiligte Klassen/Komponenten: `ReporterAppFixture`, `E2EProcessGuard`, `StubFeedServer`, FlaUI `Application`/`UIA3Automation`

### App-Teardown im Fixture (`DisposeAsync`)

1. **Geändert:** PID bestimmen — `App?.ProcessId`, Fallback `_appProcessId` (deckt Fälle ab, in denen FlaUI das `Process`-Objekt ersetzt oder `App` `null` ist).
2. **Geändert:** statt nur `App.Kill()`: `E2EProcessGuard.KillAndWaitAsync(pid, timeout)` — Kill (Tree), dann `WaitForExit` mit Timeout; läuft der Prozess danach noch, wird ein eindeutiger `[E2E]`-Warnhinweis auf die Konsole geschrieben (statt still geschluckt).
3. `App.Dispose()`, `Automation.Dispose()`, `Server.DisposeAsync()` (unverändert).
4. Best-effort `Directory.Delete(_tempDirectory)` (unverändert — durch den verifizierten Exit ist die Erfolgsaussicht des Löschens jetzt gegeben).

Beteiligte Klassen/Komponenten: `ReporterAppFixture`, `E2EProcessGuard`

### Zweite App-Instanz in `DemoSeedTests.FirstStart_SeedsNewsCategoryAndDemoFeed`

1. Temp-DB `reporter-e2e-demo-{guid}`, `ProcessStartInfo` ohne `REPORTER_DISABLE_DEMO_SEED`, `Process.Start` (unverändert).
2. **Neu:** `E2EProcessGuard.TrackProcess(process)` direkt nach dem Start.
3. Assertions und FlaUI-Attach unverändert.
4. **Geändert:** im `finally` ersetzt `E2EProcessGuard.KillAndWaitAsync(process, timeout)` das bisherige `process.Kill()` ohne Exit-Nachweis; `process.Dispose()` und best-effort Temp-Löschung bleiben.

Beteiligte Klassen/Komponenten: `DemoSeedTests`, `E2EProcessGuard`

### Skript-Lauf (`Run-E2ETests.ps1`)

1. Build, `$appOutput`-Prüfung, `REPORTER_APP_PATH` setzen (unverändert).
2. **Geändert:** `dotnet test` läuft in `try`; `$LASTEXITCODE` wird sofort nach dem Testlauf in einer Variablen gemerkt.
3. **Neu:** im `finally` werden alle laufenden `Reporter`-Prozesse ermittelt (`Get-Process -Name Reporter`), per `Path`-Vergleich auf `$appOutput` eingegrenzt und via `Stop-Process -Force` beendet; Treffer werden protokolliert.
4. Exit mit dem gemerkten Test-Exit-Code.

Beteiligte Klassen/Komponenten: `Run-E2ETests.ps1`, `Get-Process`/`Stop-Process`

### Harter Test-Host-Abbruch (Szenario, kein Code-Ablauf)

1. `testhost.exe` wird beendet/abstürzt (Ctrl+C-Vollkill, `taskkill`, Crash) → `DisposeAsync` läuft nicht.
2. Das OS schließt das Job-Handle des Test-Hosts → `KILL_ON_JOB_CLOSE` terminiert alle zugewiesenen `Reporter.exe`-Instanzen (Fixture-Instanz und ggf. laufende `DemoSeedTests`-Instanz).
3. Lief der Lauf über `Run-E2ETests.ps1`, räumt zusätzlich das `finally` verbliebene Prozesse unter `$appOutput` weg (z. B. falls die Job-Zuweisung fehlgeschlagen war).

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `E2EProcessGuard` (`src/Reporter.E2ETests/E2EProcessGuard.cs`) | `internal static` Klasse mit P/Invoke | Kapselt ein prozessweites Windows Job Object (`CreateJobObject`, `SetInformationJobObject` mit `JOBOBJECT_EXTENDED_LIMIT_INFORMATION`/`JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`, `AssignProcessToJobObject`, `CloseHandle`). Methoden: `TrackProcess(Process)` — weist einen gestarteten App-Prozess dem Job zu, best-effort mit `[E2E]`-Konsolenwarnung bei Scheitern; `KillAndWaitAsync(...)` — `Kill(entireProcessTree: true)` für `Process` oder PID plus `WaitForExit` mit Timeout, Rückgabe/ob-Protokoll ob der Exit bestätigt wurde. |

## Änderungen an bestehenden Klassen

### `ReporterAppFixture` (xunit-Collection-Fixture)

- **Neue Felder:** `_appProcessId` (`int?`) — die beim `Process.Start` vergebene PID, unabhängig vom FlaUI-`Process`-Objekt.
- **Geänderte Methoden:** `InitializeAsync` — PID merken, `E2EProcessGuard.TrackProcess` aufrufen, alle Schritte nach `Process.Start` in `try`/`catch` mit `KillAndWaitAsync`-Backstop hüllen (ersetzt den Attach-only-Catch).
- **Geänderte Methoden:** `DisposeAsync` — `App.Kill()` durch `E2EProcessGuard.KillAndWaitAsync` ersetzen (PID aus `App?.ProcessId`, Fallback `_appProcessId`); bei nicht bestätigtem Exit `[E2E]`-Warnung statt stillem Schlucken; restlicher Teardown unverändert.

### `DemoSeedTests` (Testklasse)

- **Geänderte Methoden:** `FirstStart_SeedsNewsCategoryAndDemoFeed` — nach `Process.Start` `E2EProcessGuard.TrackProcess(process)` aufrufen; im `finally` `process.Kill()` durch `E2EProcessGuard.KillAndWaitAsync` ersetzen.

### `Run-E2ETests.ps1` (Skript)

- `dotnet test`-Aufruf in `try`/`finally` kapseln; Test-Exit-Code vor dem `finally` merken; im `finally` `Reporter`-Prozesse mit `Path -ieq $appOutput` per `Stop-Process -Force` beenden und protokollieren.

### `troubleshooting.md` (Dokumentation)

- Abschnitt „Verwaiste Prozesse oder Temp-Verzeichnisse" erweitern: Ursache nun vollständig (Test-Host-Tod ohne `DisposeAsync`, ungesicherte `InitializeAsync`-Fehlerpfade, fehlende Exit-Verifikation nach `App.Kill()`); eingebaute Absicherung (Job Object + Skript-`finally` + verifizierter Kill) benennen; manuelle Bereinigung bleibt als Fallback beschrieben.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine. Die bestehenden Env-Overrides (`REPORTER_APP_PATH`, `REPORTER_FEEDSEARCH_ENDPOINT`, `REPORTER_DB_PATH`, `REPORTER_DISABLE_DEMO_SEED`) bleiben unverändert.

## Seiteneffekte und Risiken

- **Job-Zuweisung in Job-Umgebungen:** Läuft der Test-Host selbst in einem Job ohne Breakaway-Rechte (z. B. bestimmte CI-Agents/IDE-Runner), schlägt `AssignProcessToJobObject` fehl. `TrackProcess` ist daher best-effort mit Warnung; Skript-`finally` und Kill-Backstop bleiben wirksam.
- **Parallele E2E-Läufe:** Das Skript-`finally` beendet alle `Reporter`-Prozesse unter `$appOutput` — zwei gleichzeitige E2E-Läufe auf derselben Maschine würden sich gegenseitig aufräumen. Die Suite ist ohnehin auf serielle Ausführung ausgelegt (`E2ETestCollection`, interaktive Session); als bekanntes Restrisiko dokumentiert.
- **`Kill(entireProcessTree: true)`:** beendet ggf. von `Reporter.exe` gestartete Kindprozesse — gewollt, da das Leck sonst über Kindprozesse entkommen könnte; die App startet im Ist-Stand keine dokumentierten Kindprozesse, die erhalten bleiben müssten.
- **Bestehender Warm-up-Flake:** `SmokeTests.AppStarts_FeedListRenders` schlägt als isolierter Einzellauf direkt nach App-Start reproduzierbar fehl (Bestandsaufnahme, Läufe 3+4) — unabhängig von diesem Issue; die Verifikationsläufe müssen diesen bekannten Befund von einem Prozess-Leck unterscheiden.
- **Kein Eingriff in `src/Reporter/`:** Falls die `WaitForExit`-Verifikation zeigt, dass die App den Kill überlebt (unbelegte Hypothese „hängender App-Shutdown"), wird der Fall durch die `[E2E]`-Warnung sichtbar; ein fachlicher Fix am App-Shutdown wäre dann ein separates Issue.

## Umsetzungsreihenfolge

1. **`E2EProcessGuard` anlegen**
   - Voraussetzungen: Keine — `net10.0-windows10.0.19041.0`, `System.Diagnostics`, kernel32-P/Invoke sind im Projekt verfügbar; kein NuGet-Paket nötig.
   - Beschreibung: Neue interne statische Klasse in `src/Reporter.E2ETests/` mit Job-Object-P/Invoke (lazy Singleton-Job, `KILL_ON_JOB_CLOSE`), `TrackProcess(Process)` (best-effort + Warnung) und `KillAndWaitAsync` (Tree-Kill + Timeout-`WaitForExit` + Exit-Bestätigung).

2. **`ReporterAppFixture` härten**
   - Voraussetzungen: `E2EProcessGuard` aus Schritt 1.
   - Beschreibung: `_appProcessId`-Feld, `TrackProcess` nach `Process.Start`, umschließender `catch` für alle `InitializeAsync`-Schritte nach dem Start (Kill-Backstop + Rethrow), `DisposeAsync` auf `KillAndWaitAsync` mit PID-Fallback und Exit-Warnung umstellen.

3. **`DemoSeedTests` härten**
   - Voraussetzungen: `E2EProcessGuard` aus Schritt 1.
   - Beschreibung: `TrackProcess` nach `Process.Start`; `finally` auf `KillAndWaitAsync` umstellen.

4. **`Run-E2ETests.ps1` absichern**
   - Voraussetzungen: Keine (unabhängig von Schritten 1–3).
   - Beschreibung: `dotnet test` in `try`/`finally`, Exit-Code merken, `finally` mit pfadeingegrenztem `Get-Process`/`Stop-Process -Force` auf `$appOutput`.

5. **Troubleshooting-Dokumentation**
   - Voraussetzungen: Schritte 1–4 (die Doku beschreibt die eingebaute Absicherung und die tatsächlich gefundene Ursache).
   - Beschreibung: Abschnitt „Verwaiste Prozesse oder Temp-Verzeichnisse" in `docs/help/tests/troubleshooting.md` um Ursache und Lösung ergänzen.

6. **Verifikation und Rest-Analyse**
   - Voraussetzungen: Schritte 1–4; interaktive Windows-Desktop-Session.
   - Beschreibung: Vor dem ersten Lauf `Get-Process Reporter` und `%TEMP%/reporter-e2e-*`-Leichname prüfen/bereinigen. `.\scripts\Run-E2ETests.ps1` mehrfach erfolgreich, einmal mit absichtlichem Test-Fehlschlag und einmal mit hartem `testhost.exe`-Kill laufen lassen; nach jedem Lauf `Get-Process Reporter` leer und kein neues verwaistes Temp-Verzeichnis. Dabei die verbleibenden Analyse-Hypothesen verifizieren (tritt das Leck auch im Erfolgspfad auf? — die `[E2E]`-Warnung bzw. der Watcher zeigt es). `dotnet test Reporter.sln --filter "Category!=E2E"`, `npm test` und `.\scripts\Run-StaticChecks.ps1` müssen grün bleiben.

## Tests

### Neue Tests

Kein neuer fachlicher Test — die Anforderung verlangt ausdrücklich unveränderte neun E2E-Tests; der Funktionsnachweis erfolgt über Prozess-Freiheit (siehe E2E-Abschnitt). Neue Hilfsmethoden:

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `E2EProcessGuard.TrackProcess` | `E2EProcessGuard` (Hilfsklasse, kein Test) | Weist einen `Process` dem Kill-on-Close-Job zu; best-effort mit `[E2E]`-Warnung. |
| `E2EProcessGuard.KillAndWaitAsync` | `E2EProcessGuard` (Hilfsklasse, kein Test) | Tree-Kill + Timeout-`WaitForExit`; wird von `DisposeAsync`, `InitializeAsync`-Backstop und `DemoSeedTests` verwendet. |

### Betroffene bestehende Tests

Keine — Signaturen und Verhalten der neun E2E-Tests (`SmokeTests`, `ArticleLinkTests`, `DemoSeedTests`) bleiben unverändert; `DemoSeedTests` erhält nur interne Härtung.

### E2E-Tests (primärer Funktionsnachweis)

Die Anforderung berührt keinen Benutzerfluss: Es wird keine UI-Funktion geändert, sondern der Prozess-Lebenszyklus der Test-Infrastruktur abgesichert. Eine E2E-Testmethode kann den Nachweis „kein `Reporter.exe` nach testhost-Tod" nicht erbringen, weil der zu prüfende Zustand erst **nach** dem Ende des Test-Prozesses eintritt — ein In-Process-Assert ist definitionsgemäß unmöglich. Der primäre Nachweis erfolgt daher über kontrollierte Skript-Läufe mit externem Prozess-Check (geplant als Verifikationsschritt 6):

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Erfolgreicher Lauf: `.\scripts\Run-E2ETests.ps1`, danach `Get-Process Reporter` leer | Manueller Verifikationslauf (kein Testcode) | Nach Erfolg kein `Reporter.exe`, keine blockierten Build-Output-Dateien, kein neues `%TEMP%/reporter-e2e-*`-Leichnam | Prüfung des realen Gesamtablaufs inkl. `DisposeAsync`-Pfad; nur außerhalb des Test-Prozesses beobachtbar. |
| Pflicht | Fehlschlag-Lauf: Suite mit mindestens einem scheiternden Test, danach `Get-Process Reporter` leer | Manueller Verifikationslauf | Nach Test-Fehlschlag kein `Reporter.exe` | Deckt den `DisposeAsync`-/Backstop-Pfad nach Assert-Fehlschlag ab. |
| Pflicht | Abbruch-Lauf: `testhost.exe` während des Laufs hart beenden, danach `Get-Process Reporter` leer | Manueller Verifikationslauf | Nach Test-Host-Tod kein `Reporter.exe` — das reproduzierte Leck-Szenario | Nur so lässt sich die Job-Object-Absicherung (Test-Host-Tod → OS killt Job-Mitglieder) und das Skript-`finally` real nachweisen. |

Welche bestehenden E2E-Tests müssen angepasst werden?

Keine — alle neun Tests bleiben fachlich unverändert und müssen weiterhin bestehen.

## Offene Punkte

Keine.
