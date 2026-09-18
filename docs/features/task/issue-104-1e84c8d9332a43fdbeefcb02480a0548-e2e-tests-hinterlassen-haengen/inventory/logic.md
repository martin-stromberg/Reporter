<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik — Prozess-Lebenszyklus der E2E-App-Instanzen und App-Shutdown-Pfad

## `ReporterAppFixture`

Datei: `src/Reporter.E2ETests/ReporterAppFixture.cs`

xunit-Collection-Fixture (`IAsyncLifetime`), das die gesamte E2E-Umgebung besitzt: `StubFeedServer`, eine gestartete `Reporter.exe`, FlaUI-UIA3-Attach, isolierte Temp-SQLite-DB. Wird über `E2ETestCollection` (`ICollectionFixture<ReporterAppFixture>`) von allen E2E-Testklassen geteilt — serielle Ausführung.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `InitializeAsync()` | `public` | Startet Stub-Server (Z. 50), legt `%TEMP%/reporter-e2e-{guid}` mit `reporter.db` an (Z. 52–54), startet `Reporter.exe` via `Process.Start` mit `UseShellExecute = false` und Env `REPORTER_FEEDSEARCH_ENDPOINT`, `REPORTER_DB_PATH`, `REPORTER_DISABLE_DEMO_SEED=1` (Z. 56–65), attached FlaUI via `Application.Attach(process)` (Z. 72), registriert einen Exit-Watcher-Task, der `[E2E] Reporter.exe exited …` mit Exit-Code auf die Konsole schreibt (Z. 93–105), wartet bis zu 2 min auf das Hauptfenster via `App.GetMainWindow(Automation, …)` (Z. 108–111), pausiert 3 s (Z. 116), ruft `MainWindow.SetForeground()` (Z. 118). |
| `GetMainWindow()` | `public` | Re-resolvt das Hauptfenster für Tests; wirft `InvalidOperationException`, wenn `App.HasExited` oder kein Fenster auffindbar (Z. 127–143). |
| `DisposeAsync()` | `public` | Teardown: ruft `App.Kill()` nur unter `!App.HasExited`, **alle Ausnahmen werden geschluckt, kein `WaitForExit`, keine Exit-Verifikation, kein `Kill(entireProcessTree)`** (Z. 148–163); danach `App.Dispose()`, `Automation.Dispose()`, `Server.DisposeAsync()`, best-effort `Directory.Delete(_tempDirectory, recursive: true)` mit geschluckten Fehlern (Z. 169–179). |
| `ResolveAppPath()` | `internal static` | `REPORTER_APP_PATH`-Override (wenn Datei existiert), sonst Aufstieg von `AppContext.BaseDirectory` bis zum Konventionspfad `src/Reporter/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Reporter.exe`; wirft `FileNotFoundException` (Z. 186–212). Wird auch von `DemoSeedTests` wiederverwendet. |

### Absicherungslücken im Lebenszyklus (Ist-Stand)

- **Attach-Fehler ist der einzige lokal abgesicherte Pfad:** schlägt `Application.Attach` fehl, wird `process.Kill()` best-effort nachgezogen und `process` disposed, bevor die Ausnahme weiterfliegt (Z. 74–89). Kommentar dazu: „Attach failed before App was assigned, so DisposeAsync cannot see the process" — `DisposeAsync` prüft `App is not null`, ein `null`-`App` überspringt den Kill.
- **Weitere `InitializeAsync`-Fehlerpfade nach `Process.Start` sind lokal ungesichert:** wirft `new UIA3Automation()` (Z. 67, vor dem Attach → `App` bleibt `null`), der 2-min-`GetMainWindow`-Wait (Z. 108–111, `App` bereits gesetzt) oder `SetForeground()` (Z. 118), gibt es im Fixture keinen Cleanup. Ob `DisposeAsync` nach einem `InitializeAsync`-Fehlschlag überhaupt läuft, hängt vom Runner ab — das Projekt nutzt xunit **2.9.3** (v2: `DisposeAsync` wurde laut xunit-Maintainer-Historie bei Collection-Fixtures nach Init-Fehlschlag noch aufgerufen; v3 änderte dies auf „kein Dispose ohne erfolgreiche Initialisierung"). Selbst wenn `DisposeAsync` läuft, greift sein `App is not null`-Guard nicht für Pfade vor der `App`-Zuweisung — der gestartete Prozess kann entkommen.
- **FlaUI ersetzt das `Process`-Objekt:** Kommentar Z. 68–69 („FlaUI replaces the Process object inside Application when waiting for the main window, so the process is only managed through App") — der ursprüngliche `process`-Handle wird nach `Attach` nicht mehr verwaltet; einzig `App.Kill()`/`App.HasExited`/`App.ProcessId` sind maßgeblich.
- **`DisposeAsync` ohne Exit-Nachweis:** `App.Kill()` ohne anschließendes `WaitForExit`/PID-Verify; schlägt `Kill()` selbst (z. B. Prozess-Objekt bereits weggeräumt oder Berechtigungsproblem), bleibt der Fehler unsichtbar (catch-all, Z. 157–160).
- **Temp-Dir-Löschung scheitert bei lebendem Prozess:** `Directory.Delete` (Z. 173) schlägt bei vom Prozess gehaltenen Dateilocks still fehl — korreliert mit den beobachteten verwaisten `%TEMP%/reporter-e2e-*`-Ordnern (siehe `tests.md`).

Abonnierte Events: keine. Publizierte Events: keine. (Interner Fire-and-forget-Exit-Watcher via `Process.GetProcessById(App.ProcessId).WaitForExitAsync()`, Z. 93–105.)

## `DemoSeedTests` (eigene zweite App-Instanz)

Datei: `src/Reporter.E2ETests/DemoSeedTests.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `FirstStart_SeedsNewsCategoryAndDemoFeed()` | `public` (`[Fact]`, `[Trait("Category","E2E")]`) | Startet eine **zweite, eigene** `Reporter.exe` gegen eine frische Temp-DB (`%TEMP%/reporter-e2e-demo-{guid}`) ohne `REPORTER_DISABLE_DEMO_SEED` (wird aktiv aus dem Prozess-Env entfernt, Z. 51–59); prüft Seed-Zeilen via `FeedDbAssertions` und die gerenderte Feed-Card via FlaUI (`Application.Attach(process)` im `using`, Z. 71). |
| `SelectFeedsTab(Window)` | `private static` | Wählt den Feeds-Tab via `UiRetry.SelectTab`, assertiert auf Erfolg (Z. 115–119). |
| `finally`-Block | — | `process.Kill()` unter `!process.HasExited`, catch-all, `process.Dispose()`; danach best-effort Temp-Dir-Löschung (Z. 85–109). **Kein `WaitForExit`, kein `Kill(entireProcessTree)`, keine Exit-Verifikation** — gleiches Härtungsdefizit wie `DisposeAsync`. |

## `E2ETestCollection`

Datei: `src/Reporter.E2ETests/E2ETestCollection.cs`

| Member | Beschreibung |
|--------|--------------|
| `[CollectionDefinition("E2E")]` | xunit-Collection `E2E` mit `ICollectionFixture<ReporterAppFixture>` — ein Fixture für alle E2E-Tests, keine Parallelisierung, keine Testreihenfolge erzwungen. |
| `CollectionName` (`public const string`) | Konstante `"E2E"`, referenziert von allen `[Collection]`-Attributen. |

## `scripts/Run-E2ETests.ps1`

Datei: `scripts/Run-E2ETests.ps1`

| Schritt | Zeilen | Kurzbeschreibung |
|---------|--------|------------------|
| Fehlerpräferenz/Env | 20–22 | `$ErrorActionPreference = "Stop"`, `$env:IncludeIosTarget = 'false'`. |
| Pfadauflösung | 24–28 | `$tfm = net10.0-windows10.0.19041.0`, `$runtime = win-x64`, `$configuration = Debug`; `$appOutput` = `src\Reporter\bin\Debug\<tfm>\<runtime>\Reporter.exe` (Repo-Root-relativ). |
| Vorab-Check | 30–33 | Abbruch mit Exit 1, wenn `Reporter.sln` fehlt (Skript muss aus dem Repo-Root laufen). |
| Build | 35–45 | `dotnet build src/Reporter/Reporter.csproj -c Debug -f <tfm> -r win-x64`; Exit bei Build-Fehler bzw. fehlendem `$appOutput`. |
| Env-Setzen | 47–48 | `$env:REPORTER_APP_PATH = $appOutput` — das Skript **startet die App nicht selbst**, steuert nur den Suchpfad des Fixtures. |
| Testlauf | 50–52 | `dotnet test src/Reporter.E2ETests/Reporter.E2ETests.csproj -c Debug -f <tfm>`; `exit $LASTEXITCODE`. **Kein `try`/`finally`, kein Prozess-Cleanup** — ein hart beendeter `dotnet test`-Test-Host hinterlässt die gestartete `Reporter.exe` unbeaufsichtigt. |

## App-Shutdown-Pfad (`src/Reporter/`)

### `App` (`src/Reporter/App.xaml.cs`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnStart()` | `protected override async void` | Startup-Sequenz: Content-/DB-Migration, `BeginSessionAsync` (Debug-Log), Backup-Handling, Registrierung von `AppDomain.UnhandledException`/`TaskScheduler.UnobservedTaskException`, Demo-Seed, Retention-Cleanup, Theme, `INetworkStatusService`-Auflösung (Monitoring-Start), `IAutoRefreshService.StartAsync` (Z. 37–60). |
| `OnSleep()` | `protected override` | Nur `base.OnSleep()` + fire-and-forget `LogAsync("App suspended")` (Z. 63–67). **Kein Stoppen der Hintergrunddienste.** |
| `OnResume()` | `protected override` | Nur Logging (Z. 70–74). |
| `CreateWindow()` | `protected override` | Erzeugt `Window(AppShell)`; auf Windows feste Handysize 390×844 (Z. 81–94). |
| `OnUnhandledException` / `OnUnobservedTaskException` | `private` | Fire-and-forget-`LogAsync` — bewusste Einschränkung im Absturzpfad (Z. 96–109). |

- **Kein `Application.Quit`-Pfad, kein `Window.Closed`-/`Destroying`-Handler, kein `IDisposable` im App-Shutdown gefunden** (Volltextsuche über `src/`: einzige `Quit`/`Closed`-Treffer entfallen auf `OnSleep`-Signatur). Der reguläre Windows-Exit erfolgt über den WinUI3-/MAUI-Default.
- `IAutoRefreshService.StopAsync` wird **nirgends im Produktivcode aufgerufen** (einziger Aufrufort von `StartAsync`: `App.xaml.cs:184`) — der Auto-Refresh-Timer läuft bis zum Prozessende.

### `AutoRefreshService` (`src/Reporter.Core/Services/AutoRefreshService.cs`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartAsync(CancellationToken)` | `public` | Lädt Settings, `ApplySettingsAsync`, optional fire-and-forget `RunStartupSyncAsync` bei `RefreshOnStartupEnabled && IsOnline` (Z. 44–53). |
| `ApplySettingsAsync(Settings)` | `public` | Unter `_stateLock`: `StopLoopAsync`, fehlerisoliertes `IBackgroundRefreshService.ApplySettingsAsync`, bei `AutoRefreshEnabled` neuer `CancellationTokenSource` + `RunLoopAsync`-Task (Z. 71–109). |
| `StopAsync()` | `public` | Stoppt den Timer-Loop unter `_stateLock` via `StopLoopAsync` (Cancel + `await loopTask`) — **wird nirgends aufgerufen** (Z. 112–123). |
| `RunLoopAsync(interval, ct)` | `private` | `PeriodicTimer`-Loop: bei `IsOnline` `IFeedSyncService.SyncAllAsync`; läuft auf ThreadPool-Threads (blockiert Prozess-Exit nicht) (Z. 154–185). |

### `DebugLogService` (`src/Reporter.Core/Services/DebugLogService.cs`)

Sitzt in allen Fehler-/Lifecycle-Pfaden (`LogAsync` fire-and-forget in `App.xaml.cs`), wirft bewusst nie. `BeginSessionAsync` setzt das Session-Log zurück (Error-Einträge bleiben). Kein Shutdown-Hook.

## Hilfsklassen der E2E-Suite (Testlogik)

| Klasse | Datei | Rolle |
|--------|-------|-------|
| `StubFeedServer` | `src/Reporter.E2ETests/StubFeedServer.cs` | In-Process-Kestrel auf `http://127.0.0.1:0`: `/directory` (feedsearch-Stil), `/feeds/{name}.xml` (Fixture-Templates mit `{name}`/`{baseUrl}`-Platzhaltern), `/external-link` (Hit-Zähler `ExternalLinkHitCount`), `/site`, `/empty`, Fallback 404. `IAsyncLifetime` — Start/Stop in `InitializeAsync`/`DisposeAsync` (Z. 46–138). |
| `UiRetry` | `src/Reporter.E2ETests/UiRetry.cs` | Statische UIA-Polling-/Interaktionshelfer (20-s-Default, 200-ms-Poll): `WaitForElement`/`TryFindElement`, `…ByName`, `…InScope` (durchsucht alle Top-Level-Fenster/Popups der Prozess-ID), `SelectTab` (SelectionItem → Invoke/Klick → Overflow-Flyout `TopNavOverflowButton`), `WaitForCard` (Group-first, Fallback beliebiges Element), `WaitFor`, `InvokeOrClick` (Invoke → SelectionItem → Maus-Klick), `SetText` (Value-Pattern → Tastatur). |
| `E2EPageHelpers` | `src/Reporter.E2ETests/E2EPageHelpers.cs` | `SelectTab` mit Seiten-Anker-Waits (`ActionAddFeed`/`LabelCategoryName`/`ButtonMarkAllRead`), `OpenAddSheet`, `WaitForUrlEntry`. |
| `FeedDbAssertions` | `src/Reporter.E2ETests/FeedDbAssertions.cs` | Read-only-SQLite-Polling (15-s-Default, `Mode=ReadOnly`) gegen die isolierte `reporter.db`: `FeedExistsAsync`, `CategoryExistsAsync`, `ItemExistsAsync`. |

## Dokumentationsstelle

`docs/help/tests/troubleshooting.md` enthält bereits den Abschnitt **„Verwaiste Prozesse oder Temp-Verzeichnisse"** (Z. 75–83): Symptom hängende `Reporter.exe`/`reporter-e2e-*`-Ordner nach Abbruch, Ursache „Harter Abbruch des Testprozesses, bevor `DisposeAsync` lief", Lösung = manuelles Beenden/Löschen. Der Abschnitt benennt bisher nur den Abbruch-Fall, nicht die ungesicherten `InitializeAsync`-Fehlerpfade oder die fehlende Exit-Verifikation nach `App.Kill()`.
