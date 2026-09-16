<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Demo-Feed beim ersten Start (Issue #96)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `FirstRunState`-Klasse in `src/Reporter.Core/Models/FirstRunState.cs` anlegen (`IsFirstRun`, `DemoSeedSuppressed`, `ShouldSeedDemoContent`) | Offen | — |
| 2 | Logik | Interface `IDemoContentService` in `src/Reporter.Core/Interfaces/IDemoContentService.cs` anlegen (`EnsureSeededAsync(CancellationToken)`) | Offen | — |
| 3 | Logik | `DemoContentService` in `src/Reporter.Core/Services/DemoContentService.cs` implementieren (Konstanten, Gating, `GetByUrlAsync`-Idempotenz, Kategorie „News" suchen/anlegen, Feed anlegen, Info-Log) | Offen | — |
| 4 | Logik | `MauiProgram.CreateMauiApp`: `isFirstRun = !File.Exists(databasePath)` vor `builder.Build()` erfassen | Offen | — |
| 5 | Konfiguration | `MauiProgram`: `REPORTER_DISABLE_DEMO_SEED` über neue Hilfsmethode `ResolveDemoSeedSuppressed()` auswerten | Offen | — |
| 6 | Konfiguration | `MauiProgram`: `FirstRunState`-Instanz und `IDemoContentService`/`DemoContentService` als Singletons registrieren | Offen | — |
| 7 | Logik | `App.OnStart`: fehlerisolierten Seed-Block (`IDemoContentService.EnsureSeededAsync`) nach der Exception-Handler-Registrierung einhängen | Offen | — |
| 8 | Tests | `DemoContentServiceTests` anlegen: First-Run-Seed legt Kategorie + Feed mit korrekten Defaults an | Offen | — |
| 9 | Tests | `DemoContentServiceTests`: No-op bei `IsFirstRun = false` und bei `DemoSeedSuppressed = true` | Offen | — |
| 10 | Tests | `DemoContentServiceTests`: vorhandene Kategorie „News" wird wiederverwendet; Zweitaufruf und vorhandener Demo-Feed sind idempotent; Info-Log über `FakeDebugLogService` | Offen | — |
| 11 | Tests | `ServiceCollectionTests` um `AddReporterServices_ResolvesDemoContentService` erweitern | Offen | — |
| 12 | E2E-Tests | `ReporterAppFixture`: `REPORTER_DISABLE_DEMO_SEED=1` im `ProcessStartInfo` setzen und `ResolveAppPath` auf `internal` heben | Offen | — |
| 13 | E2E-Tests | `FeedDbAssertions` um `CategoryExistsAsync(databasePath, name, timeout)` erweitern | Offen | — |
| 14 | E2E-Tests | `DemoSeedTests` anlegen (Collection `E2E`): eigener App-Prozess mit frischer `REPORTER_DB_PATH` ohne Disable-Flag; Feed-Zeile, Kategorie „News" und Feed-Karte prüfen; Prozess und Temp-Verzeichnis aufräumen | Offen | — |
| 15 | Dokumentation | `docs/help/anwendung/` (z. B. `beschreibung.md`/`datenmodell.md`) um das Demo-Seed-Verhalten beim ersten Start ergänzen | Offen | — |
| 16 | Verifikation | `dotnet test src/Reporter.Tests` (Release), `npm test`, `.\scripts\Run-StaticChecks.ps1` sowie manuelle Verifikation mit frischer DB; Ergebnisse in `test-results.md` festhalten | Offen | — |
