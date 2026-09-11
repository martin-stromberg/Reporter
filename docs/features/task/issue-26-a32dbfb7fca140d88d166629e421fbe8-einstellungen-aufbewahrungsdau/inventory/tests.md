# Tests — Bestandsaufnahme

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-11 09:37:12 +0200 (lokale Zeit, Deutschland)
- **Branch und Commit-ID:** `task/issue-26-a32dbfb7fca140d88d166629e421fbe8-einstellungen-aufbewahrungsdau` @ `f354f5b6c4d32a8bcef5305922cd22264e79c265` („Für später bewahren"-Funktion und separate Ansicht (#58), 2026-09-11 09:28:31 +0200)
- **Uncommittete Änderungen im getesteten Stand:** keine am Code — einzige Änderung ist das untracked Feature-Verzeichnis `docs/features/task/issue-26-.../` (`requirement.md`, `todo.md`, `inventory/`); `src/` ist unverändert.
- **Testumgebung und Runtime-/SDK-Versionen:** Windows (Git Bash), .NET SDK `10.0.401` (`C:\Program Files\dotnet\sdk`), Testhost .NET 10.0.12, xUnit.net VSTest Adapter 3.1.4, xunit 2.9.3, coverlet.collector 6.0.4.
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - Einzige Testsuite: `src/Reporter.Tests/Reporter.Tests.csproj` (`net10.0`, xUnit, EF Core SQLite In-Memory). Referenziert nur `Reporter.Core` und `Reporter.Data` — **kein** MAUI-Workload nötig.
  - CI-Befehl aus `.github/workflows/pr-staging-ci.yml` (Job `build-and-test`, `windows-latest`, `IncludeIosTarget=false`): `dotnet build Reporter.sln --configuration Release --no-restore`, dann `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal"`. Coverage-Schwelle 70 % Zeilen via ReportGenerator.
  - Lokale Static Checks: `scripts\Run-StaticChecks.ps1` (Format / Security / Release-Build mit `TreatWarningsAsErrors`) — für die Bestandsaufnahme nicht ausgeführt (kein Testlauf).
  - `ci-instructions.md` ist eine generische Vorlage (Platzhalter `MyApp.sln`), nicht die konkrete Pipeline.
  - `test-results.md` (Repo-Root) dokumentiert denselben `dotnet test`-Befehl als Projektkonvention.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Baseline-Build | `dotnet build src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` | Repo-Root | 0 | – (Build) | – | – | 0 Warnungen, 0 Fehler |
| Baseline-Test | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx" --logger "console;verbosity=normal" --results-directory docs/features/task/issue-26-.../inventory/test-results` | Repo-Root | 0 | 90 | 0 | 0 | [Log](test-results/dotnet-test-baseline.log), [TRX](test-results/test-results.trx), [Coverage](test-results/2fa357ca-9dbc-4d28-8483-63e3693d3bd4/coverage.cobertura.xml) |

Anmerkung zum Umfang: Der lokale Build umfasst nur `Reporter.Tests` inkl. transitiver Projekte (`Reporter.Core`, `Reporter.Data`). Der CI-Job baut zusätzlich die gesamte Solution inkl. MAUI-App `src/Reporter` (Windows-Target); die MAUI-App selbst enthält keine ausführbaren Tests. Gesamtdauer Testlauf: ca. 5,2 s.

### Nachgewiesene bestehende Testfehler

Keine — alle 90 Tests bestanden im Ausgangslauf.

### Testlücken und Ausführungsprobleme

- Keine fehlgeschlagenen, übersprungenen oder abgebrochenen Tests.
- Kein `SettingsViewModelTests`, keine Tests für Keyword-Matching und keine Auto-Refresh-Tests vorhanden (für die Anforderung neu zu erstellen).
- Die MAUI-App (`src/Reporter`) wird im lokalen Baseline-Lauf nicht kompiliert (CI baut die ganze Solution mit `-r win-x64`); für die bestehende Suite nicht erforderlich.

## Testklassen (anforderungsrelevant)

### `RetentionCleanupServiceTests`
Datei: `src/Reporter.Tests/RetentionCleanupServiceTests.cs`

- `CleanupAsync_DeletesExpiredButKeepsSaved` — löscht abgelaufene gelesene Items, behält gespeicherte und ungelesene.
- `CleanupAsync_ZeroOrNegativeRetentionDays_Skips` (Theory, `InlineData(0)`, `InlineData(-5)`) — kein Cleanup bei `RetentionDays <= 0`.
- `CleanupAsync_RespectsConfiguredRetentionDays` — Cutoff folgt konfigurierten `RetentionDays`.
- `CleanupAsync_CancelledToken_ThrowsOperationCanceled` — Cancellation wird durchgereicht.
- Hilfsmethode `SetRetentionDaysAsync(int)` — schreibt vollständiges `Settings`-Objekt (alle `required init`-Felder).

### `SettingsRepositoryTests`
Datei: `src/Reporter.Tests/SettingsRepositoryTests.cs`

- `GetAsync_ReturnsSeededSettings` — Seed-Datensatz mit `DefaultId`.
- `GetAsync_CreatesDefaultRecordWhenMissing` — Default-Anlage bei fehlendem Datensatz.
- `SaveAsync_OverwritesSameRecord` — Singleton-Update.
- `SaveAsync_IgnoresDifferentIdAndUpdatesDefault` — `Id` des Parameters wird ignoriert.
- `GetAsync_AlwaysReturnsSingleRecord` — genau ein Datensatz.

### `KeywordRepositoryTests`
Datei: `src/Reporter.Tests/KeywordRepositoryTests.cs`

- `AddAsync_ThenGetByIdAsync_ReturnsKeyword`, `GetAllAsync_ReturnsKeywordsOrderedByText`, `UpdateAsync_PersistsChanges`, `DeleteAsync_RemovesKeyword`, `GetByIdAsync_NonExisting_ReturnsNull`.
- Kein Test zum Unique-Index-Verhalten bei Duplikaten.

### `ItemRepositoryTests` (Auszug, `DeleteExpiredAsync`)
Datei: `src/Reporter.Tests/ItemRepositoryTests.cs` (23 Testfälle gesamt)

- `DeleteExpiredAsync_RemovesExpiredReadItems` (Zeile 485)
- `DeleteExpiredAsync_KeepsSavedForLaterItems` (Zeile 524)
- `DeleteExpiredAsync_KeepsUnreadItems` (Zeile 552)
- `DeleteExpiredAsync_KeepsNonExpiredItems` (Zeile 579)
- `DeleteExpiredAsync_UsesReadAtOverPublishedAt` (Zeile 607)
- `DeleteExpiredAsync_CancelledToken_ThrowsOperationCanceled` (Zeile 697)

### `ServiceCollectionTests`
Datei: `src/Reporter.Tests/ServiceCollectionTests.cs`

- `AddReporterRepositories_ResolvesAllRepositories` — löst alle sechs Repository-Interfaces aus einem nachgebauten `ServiceCollection` auf (spiegelt `MauiProgram`-Registrierung, ohne Services/ViewModels).

## Hilfsmethoden

### `TestDbContextFactory`
Datei: `src/Reporter.Tests/TestDbContextFactory.cs`

- `TestDbContextFactory()` — öffnet SQLite-In-Memory-Verbindung (`DataSource=:memory:`), `EnsureCreated()` legt Schema inkl. Seed-Settings an.
- `CreateDbContext()` — neuer `ReporterDbContext` auf derselben Verbindung; implementiert `IDbContextFactory<ReporterDbContext>` + `IDisposable`.

### `TestDataSeeder`
Datei: `src/Reporter.Tests/TestDataSeeder.cs`

- `SeedFeedAsync(TestDbContextFactory)` — legt einen Feed (`https://example.com/feed`, „Example Feed") an und liefert die `feedId`.
