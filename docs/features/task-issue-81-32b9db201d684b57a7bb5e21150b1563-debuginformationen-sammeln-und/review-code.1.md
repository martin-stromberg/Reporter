<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

Basisbranch: `origin/staging` (Merge-Base `e7830d0`). Diff umfasst committed Stand plus unstaged Änderungen und untracked neue Dateien (`git status`).

Zusatzprüfung `RaiseUiActionRequested`: Das Muster wird in diesem Branch **nicht** verwendet (kein Treffer im gesamten Repository). Das stattdessen eingeführte UI-Event `SettingsViewModel.DebugReportFailed` besitzt einen korrekt an-/abgemeldeten Handler in `SettingsPage.xaml.cs` (`OnDebugReportFailed`, `OnAppearing`/`OnDisappearing`) — entsprechend `NotificationAuthorizationDenied`-Konvention.

## Befunde

### src/Reporter.Core/Services/DebugReportService.cs (DebugReportService)

- **Fehlende begrenzte Abfrage / ineffizientes Laden** — `SendReportAsync` (Z. 94–95) lädt die **komplette** `sync_logs`-Tabelle und die komplette `debug_log_entries`-Tabelle per `GetAllAsync()` in den Speicher und wendet `Take(MaxSyncLogEntries)`/`Take(MaxDebugLogEntries)` erst im Speicher an. `sync_logs` wächst unbegrenzt (kein Cleanup-Pfad — `ISyncLogRepository.DeleteAsync` wird nirgends aufgerufen); pro Sync-Lauf entsteht eine Zeile je Feed. Bei lang laufender Installation werden tausende Entitäten materialisiert und gemappt, um dann 50 Zeilen zu verwenden.

  Empfehlung: Repository-Methode mit Limit einführen (z. B. `Task<IReadOnlyList<SyncLog>> GetLatestAsync(int maxEntries)` mit `OrderByDescending(...).Take(maxEntries)` in der Query) für `ISyncLogRepository` und `IDebugLogRepository`, statt `GetAllAsync()` + In-Memory-`Take`.

- **Hardcodierter Platzhalter-Wert** — `DebugReportRecipient = "debug@example.com"` (Z. 23) ist ein dokumentierter Platzhalter („to be replaced by the maintainer"), der so in Produktion ausgeliefert wird: Jeder Debugbericht ist an eine nicht existierende Adresse voradressiert.

  Empfehlung: Vor dem Merge/Release die reale Empfängeradresse eintragen (Konstante) oder die Adresse aus einer Konfiguration beziehen.

- **Fehlerbehandlung / irreführender Log-Eintrag** — `SendReportAsync` schreibt den Eintrag `Info | Report | "Debug report composed"` (Z. 103–110) **vor** `ComposeAsync`. Schlägt der Versand fehl (`ComposeAsync` → `false`, in `EmailService` werden Exceptions zu `false` geschluckt), enthält das Session-Log trotzdem „composed" ohne jeden Fehlereintrag — und `SettingsViewModel.SendDebugReportAsync` schreibt im `!sent`-Zweig (Z. 904–906) ebenfalls keinen `Report`/`Error`-Eintrag (nur der `catch`-Zweig tut das, Z. 913–919). Genau die Fehlsituation, für die das Session-Log gedacht ist, bleibt unsichtbar.

  Empfehlung: Das Ergebnis von `ComposeAsync` protokollieren (z. B. Info bei Erfolg, Error bei `false`) oder im `!sent`-Zweig des ViewModels einen `DebugLogCategory.Report`/`DebugLogLevel.Error`-Eintrag schreiben.

### src/Reporter.Core/ViewModels/SettingsViewModel.cs (SettingsViewModel)

- **Inkonsistente Unterdrückung von Setter-Seiteneffekten beim Laden** — Der Setter `DebugCollectionEnabled` (Z. 455–463) ruft `_debugLogService?.SetEnabled(value)` auf, ohne den `_isLoading`-Guard zu prüfen; `PersistOnChange` direkt darunter ist dagegen `_isLoading`-geschützt. `LoadAsync` (Z. 626) löst damit den Session-Log-Schalter aus, obwohl `DebugLogService.BeginSessionAsync` den persistierten Wert bereits geladen hat. Aktuell harmlos (idempotent in `DebugLogService`), verletzt aber die etablierte Konvention und hängt von Implementierungsdetails des Services ab.

  Empfehlung: `SetEnabled`-Aufruf unter `if (!_isLoading)` stellen oder den Wechsel in `PersistAsync` verlagern.

### src/Reporter.Core/Services/DebugLogService.cs (DebugLogService)

- **Ungenutzter Parameter** — `LogAsync` (Z. 77) deklariert `CancellationToken cancellationToken`, verwendet ihn aber nie: weder `ThrowIfCancellationRequested()` noch Weitergabe (die Repository-Methoden besitzen keine Token-Parameter). Das Interface suggeriert eine Abbrechbarkeit, die nicht existiert.

  Empfehlung: Token konsequent verwenden (z. B. `cancellationToken.ThrowIfCancellationRequested()` zu Beginn oder Token-Parameter an `IDebugLogRepository` ergänzen) — oder Parameter aus Interface/Implementierung entfernen.

- **Ineffiziente Trim-Strategie** — Jeder `LogAsync`-Aufruf führt zwei `DbContext`-Operationen aus: `AddAsync` (SaveChanges) **und** `TrimToLatestAsync` (Select aller Ids + `ExecuteDelete`), auch wenn die Tabelle weit unter `MaxStoredEntries = 500` liegt. Bei Fehler-Schüben (z. B. Sync über viele Feeds) verdoppelt das die DB-Roundtrips pro Eintrag.

  Empfehlung: Trim nur ausführen, wenn nötig (Count-Prüfung oder Schwellwert), oder ein kombiniertes `AddAndTrimAsync` im Repository anbieten.

### src/Reporter/App.xaml.cs (App)

- **Fire-and-forget-Logging in Absturzpfaden** — `OnUnhandledException`/`OnUnobservedTaskException` (Z. 137–145) rufen `_ = _debugLogService?.LogAsync(...)` ohne Await auf. Die asynchrone DB-Schreiboperation läuft gegen einen möglicherweise terminierenden Prozess; der Eintrag — der Kernzweck der Instrumentierung — geht dabei regelmäßig verloren. Die Einschränkung ist weder dokumentiert noch abgemildert.

  Empfehlung: Einschränkung zumindest im Kommentar/Dokumentation festhalten; ggf. `LogAsync` einen synchronen/best-effort-flush-Pfad geben (z. B. `GetAwaiter().GetResult()` mit Timeout im Unhandled-Exception-Handler) — bewusste Entscheidung dokumentieren.

## Geprüfte Dateien

Neu (untracked) bzw. geändert gegenüber Merge-Base `e7830d0`:

- `src/Reporter.Core/Interfaces/IDebugLogRepository.cs`
- `src/Reporter.Core/Interfaces/IDebugLogService.cs`
- `src/Reporter.Core/Interfaces/IDebugReportService.cs`
- `src/Reporter.Core/Interfaces/IDeviceInfoProvider.cs`
- `src/Reporter.Core/Interfaces/IEmailService.cs`
- `src/Reporter.Core/Models/AppDeviceInfo.cs`
- `src/Reporter.Core/Models/DebugLogEntry.cs`
- `src/Reporter.Core/Models/Settings.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` / `AppResources.Designer.cs` (neue Schlüssel konsistent zwischen resx/de/Designer)
- `src/Reporter.Core/Services/AutoRefreshService.cs`
- `src/Reporter.Core/Services/DebugLogCategory.cs`
- `src/Reporter.Core/Services/DebugLogLevel.cs`
- `src/Reporter.Core/Services/DebugLogService.cs`
- `src/Reporter.Core/Services/DebugReportService.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Data/Entities/DebugLogEntry.cs`
- `src/Reporter.Data/Entities/Settings.cs`
- `src/Reporter.Data/Migrations/20260914060413_AddSettingsDebugCollection.cs` (+ `.Designer.cs`)
- `src/Reporter.Data/Migrations/20260914060416_AddDebugLogEntries.cs` (+ `.Designer.cs`)
- `src/Reporter.Data/Migrations/ReporterDbContextModelSnapshot.cs`
- `src/Reporter.Data/ReporterDbContext.cs`
- `src/Reporter.Data/Repositories/DebugLogRepository.cs`
- `src/Reporter.Data/Repositories/SettingsRepository.cs`
- `src/Reporter/App.xaml.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter/Services/DeviceInfoProvider.cs`
- `src/Reporter/Services/EmailService.cs`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter/Views/SettingsPage.xaml` / `SettingsPage.xaml.cs`
- `src/Reporter.Tests/AutoRefreshServiceTests.cs` / `AutoRefreshServiceTests_DebugLog.cs`
- `src/Reporter.Tests/DebugLogRepositoryTests.cs`
- `src/Reporter.Tests/DebugLogServiceTests.cs`
- `src/Reporter.Tests/DebugReportServiceTests.cs`
- `src/Reporter.Tests/DebugReportTests_E2E.cs`
- `src/Reporter.Tests/FakeDebugLogService.cs` / `FakeDeviceInfoProvider.cs` / `FakeEmailService.cs`
- `src/Reporter.Tests/FeedSyncServiceTests_DebugLog.cs`
- `src/Reporter.Tests/ReporterDbContextTests_Persistence.cs` / `ReporterDbContextTests_Schema.cs`
- `src/Reporter.Tests/RetentionCleanupServiceTests.cs`
- `src/Reporter.Tests/ServiceCollectionTests.cs`
- `src/Reporter.Tests/SettingsRepositoryTests.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Debug.cs` / `SettingsViewModelTests_Load.cs` / `SettingsViewModelTests_Persist.cs`
- `src/Reporter.Tests/TestSettingsHelper.cs`
- Dokumentation gesichtet: `docs/help/anwendung/mobile-ui-design.md`, `test-results.md`, `docs/features/task-issue-81-.../*`
