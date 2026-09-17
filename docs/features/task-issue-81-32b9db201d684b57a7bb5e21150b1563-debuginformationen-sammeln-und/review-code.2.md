<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

Basisbranch: `origin/staging` (Merge-Base `e7830d0`). Diff umfasst committed Stand plus unstaged Änderungen und untracked neue Dateien (`git status`). Verifikation: `dotnet build` (Reporter.Tests, Release) erfolgreich ohne Warnungen; `dotnet test --filter "FullyQualifiedName~Debug"`: 50/50 bestanden.

Zusatzprüfung `RaiseUiActionRequested`: Das Muster wird in diesem Branch **nicht** verwendet (kein Treffer im gesamten Repository). Das stattdessen eingeführte UI-Event `SettingsViewModel.DebugReportFailed` besitzt einen korrekt an-/abgemeldeten Handler in `SettingsPage.xaml.cs` (`OnDebugReportFailed`, Z. 61–68; Anmeldung `OnAppearing` Z. 31, Abmeldung `OnDisappearing` Z. 42) — entsprechend `NotificationAuthorizationDenied`-Konvention.

### Verifikation der Befunde aus Iteration 1 (`review-code.1.md`)

| # | Befund | Status |
|---|--------|--------|
| 1 | `SendReportAsync` lud komplette Tabellen + In-Memory-`Take` | **Behoben** — `ISyncLogRepository.GetLatestAsync(int)` (Z. 23) und `IDebugLogRepository.GetLatestAsync(int)` (Z. 23) neu; `DebugReportService` Z. 102–103 nutzt sie; `Take` läuft in der Query (`SyncLogRepository` Z. 44, `DebugLogRepository` Z. 44) |
| 2 | Platzhalter `DebugReportRecipient = "debug@example.com"` | **Teilweise** — Wert unverändert, jetzt aber explizit als Platzhalter/Produktentscheidung dokumentiert (`DebugReportService` Z. 22–27). Substanz bleibt offen, siehe Befund unten |
| 3 | „Debug report composed" vor `ComposeAsync` geloggt, kein Fehlereintrag | **Behoben** — Eintrag wird nach `ComposeAsync` mit korrektem Level geschrieben (Z. 111–115); `!IsSupported`-Pfad loggt `Error` (Z. 91–94); neue Tests `..._LogsErrorEntry` decken beide Fehlerpfade ab |
| 4 | `SetEnabled` ohne `_isLoading`-Guard im Setter | **Behoben** — `SettingsViewModel.DebugCollectionEnabled` (Z. ~455–459) prüft `!_isLoading`; Test `Load_PersistedDebugSwitch_DoesNotCallSetEnabled` verifiziert es |
| 5 | Ungenutzter `CancellationToken` in `DebugLogService.LogAsync` | **Behoben** — `cancellationToken.IsCancellationRequested`-Check (Z. 79) |
| 6 | Trim-Strategie: zwei DB-Roundtrips pro `LogAsync` | **Behoben** — `TrimToLatestAsync` steigt per `CountAsync() <= maxEntries` früh aus (`DebugLogRepository` Z. 68–71); Test `TrimToLatestAsync_UnderLimit_KeepsAllEntries` |
| 7 | Fire-and-forget-Logging in Absturzpfaden undokumentiert | **Behoben** — bewusste Einschränkung jetzt kommentiert (`App.xaml.cs` `OnUnhandledException`/`OnUnobservedTaskException`) |

## Befunde

### src/Reporter.Core/Services/DebugReportService.cs (DebugReportService)

- **Hardcodierter Platzhalter-Wert (aus Iteration 1 weiterhin offen)** — `DebugReportRecipient = "debug@example.com"` (Z. 27) ist weiterhin eine nicht existierende Adresse, die in Produktion ausgeliefert wird; jeder Debugbericht ist dorthin voradressiert. Die neue `<remarks>`-Dokumentation (Z. 22–27) benennt das als bewusste Produktentscheidung („must replace ... before release"), löst das Problem aber nicht.

  Empfehlung: Vor dem Merge/Release die reale Empfängeradresse als Konstante eintragen oder aus einer Konfiguration beziehen. Falls die Entscheidung bewusst auf nach dem Merge vertagt wurde, ist der Befund als dokumentierter offener Punkt akzeptabel — dann bitte im Feature-Dokument vermerken.

### src/Reporter/Services/EmailService.cs (EmailService)

- **Ungenutzter Parameter** — `ComposeAsync` (Z. 20) deklariert `CancellationToken cancellationToken`, verwendet ihn aber nie: weder `IsCancellationRequested`-/`ThrowIfCancellationRequested()`-Check noch Weitergabe (die MAUI-API `Email.Default.ComposeAsync` besitzt keinen Token-Parameter). Das Interface suggeriert eine Abbrechbarkeit, die nicht existiert — derselbe Befund-Typ wie Iteration 1 bei `DebugLogService.LogAsync`, der dort mit einem `IsCancellationRequested`-Check behoben wurde.

  Empfehlung: `cancellationToken.ThrowIfCancellationRequested()` (oder `IsCancellationRequested`-Check mit Rückgabe `false`) zu Beginn von `ComposeAsync` ergänzen, damit ein bereits abgebrochener Aufruf kein Compose-Fenster öffnet — oder den Parameter aus `IEmailService` entfernen.

## Geprüfte Dateien

Geändert gegenüber Merge-Base `e7830d0` bzw. neu (untracked):

- `src/Reporter.Core/Interfaces/IDebugLogRepository.cs`
- `src/Reporter.Core/Interfaces/IDebugLogService.cs`
- `src/Reporter.Core/Interfaces/IDebugReportService.cs`
- `src/Reporter.Core/Interfaces/IDeviceInfoProvider.cs`
- `src/Reporter.Core/Interfaces/IEmailService.cs`
- `src/Reporter.Core/Interfaces/ISyncLogRepository.cs`
- `src/Reporter.Core/Models/AppDeviceInfo.cs`
- `src/Reporter.Core/Models/DebugLogEntry.cs`
- `src/Reporter.Core/Models/Settings.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` / `AppResources.Designer.cs` (16 neue Schlüssel, konsistent zwischen resx/de/Designer)
- `src/Reporter.Core/Services/AutoRefreshService.cs`
- `src/Reporter.Core/Services/DebugLogCategory.cs`
- `src/Reporter.Core/Services/DebugLogLevel.cs`
- `src/Reporter.Core/Services/DebugLogService.cs`
- `src/Reporter.Core/Services/DebugReportService.cs`
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Data/Entities/DebugLogEntry.cs`
- `src/Reporter.Data/Entities/Settings.cs`
- `src/Reporter.Data/Migrations/20260914060413_AddSettingsDebugCollection.cs` (+ `.Designer.cs`)
- `src/Reporter.Data/Migrations/20260914060416_AddDebugLogEntries.cs` (+ `.Designer.cs`)
- `src/Reporter.Data/Migrations/ReporterDbContextModelSnapshot.cs`
- `src/Reporter.Data/ReporterDbContext.cs`
- `src/Reporter.Data/Repositories/DebugLogRepository.cs`
- `src/Reporter.Data/Repositories/SettingsRepository.cs`
- `src/Reporter.Data/Repositories/SyncLogRepository.cs`
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
- `src/Reporter.Tests/SyncLogRepositoryTests.cs`
- `src/Reporter.Tests/TestSettingsHelper.cs`
- Dokumentation gesichtet: `docs/help/anwendung/mobile-ui-design.md`, `test-results.md`, `docs/features/task-issue-81-.../*`
