<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Keine Befunde

Basisbranch: `origin/staging` (Merge-Base `e7830d0`). Diff umfasst committed Stand plus unstaged Änderungen und untracked neue Dateien (`git status`). Verifikation: `dotnet build` (Reporter.Tests, Release) erfolgreich ohne Warnungen/Fehler; `dotnet test --filter "FullyQualifiedName~Debug"` — 51/51 bestanden.

Zusatzprüfung `RaiseUiActionRequested`: Das Muster wird in diesem Branch **nicht** verwendet (kein Treffer im gesamten Repository außer in den früheren Review-Dokumenten). Das stattdessen eingeführte UI-Event `SettingsViewModel.DebugReportFailed` besitzt einen korrekt an-/abgemeldeten Handler in `SettingsPage.xaml.cs` (`OnDebugReportFailed`, Z. 61–68; Anmeldung `OnAppearing` Z. 31, Abmeldung `OnDisappearing` Z. 42) — entsprechend der `NotificationAuthorizationDenied`-Konvention.

### Verifikation der Befunde aus Iteration 1 + 2 (`review-code.1.md`, `review-code.2.md`)

| # | Befund | Status |
|---|--------|--------|
| 1 | `SendReportAsync` lud komplette Tabellen + In-Memory-`Take` | **Behoben** — `ISyncLogRepository.GetLatestAsync(int)` (Z. 23) und `IDebugLogRepository.GetLatestAsync(int)` (Z. 24); `DebugReportService` Z. 102–103 nutzt sie; `Take` läuft in der Query (`SyncLogRepository` Z. 44, `DebugLogRepository` Z. 45) |
| 2 | Platzhalter `DebugReportRecipient = "debug@example.com"` | **Nicht-Befund** — vom Anwender bewusst gewählter, im Code dokumentierter Platzhalter (`DebugReportService` Z. 22–27); wird nicht erneut geführt |
| 3 | „Debug report composed" vor `ComposeAsync`, kein Fehlereintrag | **Behoben** — Eintrag nach `ComposeAsync` mit korrektem Level (Z. 111–115); `!IsSupported`-Pfad loggt `Error` (Z. 91–94); Tests `SendReportAsync_ComposeReturnsFalse_LogsErrorEntry` / `_Unsupported_LogsErrorEntry` |
| 4 | `SetEnabled` ohne `_isLoading`-Guard im Setter | **Behoben** — `SettingsViewModel.DebugCollectionEnabled` (Z. 458–460) prüft `!_isLoading`; Test `Load_PersistedDebugSwitch_DoesNotCallSetEnabled` |
| 5 | Ungenutzter `CancellationToken` in `DebugLogService.LogAsync` | **Behoben** — `cancellationToken.IsCancellationRequested`-Check (Z. 83) |
| 6 | Trim-Strategie: zwei DB-Roundtrips pro `LogAsync` | **Behoben** — `TrimToLatestAsync` steigt per `CountAsync() <= maxEntries` früh aus (`DebugLogRepository` Z. 71–74); Test `TrimToLatestAsync_UnderLimit_KeepsAllEntries` |
| 7 | Fire-and-forget-Logging in Absturzpfaden undokumentiert | **Behoben** — bewusste Einschränkung kommentiert (`App.xaml.cs` Z. 138–140, 147) |
| 8 | Ungenutzter `CancellationToken` in `EmailService.ComposeAsync` | **Behoben** — `cancellationToken.ThrowIfCancellationRequested()` (Z. 22), bewusst vor dem `try`, sodass `OperationCanceledException` korrekt propagiert statt als `false` verschluckt zu werden |

### Verifikation der Iteration-3-Änderungen

- **`DeleteAllAsync` → `DeleteAllExceptErrorsAsync`:** Interface (`IDebugLogRepository` Z. 33–39, Doc beschreibt Erhalt der `Error`-Einträge), Implementierung via `ExecuteDeleteAsync` auf `Level != DebugLogLevel.Error` (`DebugLogRepository` Z. 59–65; EF-Core-NULL-Semantik-Kompensation löscht auch `NULL`-Level-Zeilen — dokumentiertes Verhalten „alles außer Error" stimmt), Aufruf in `DebugLogService.BeginSessionAsync` (Z. 52) mit erläuterndem Kommentar (Z. 50–51). Keine verbliebenen `DeleteAllAsync`-Referenzen im Code. Abgedeckt durch `DebugLogRepositoryTests.DeleteAllExceptErrorsAsync_*`, `DebugLogServiceTests.BeginSessionAsync_ResetsPreviousEntries_KeepsErrors`, `DebugReportTests_E2E.SessionLog_WritesAndResetsAcrossSessions`.
- **Hinweistexte:** `SettingsDebugCollectionHint`, `SettingsDebugSendHint` und `SettingsDebugCollectionRequiredHint` beschreiben die Übernahme der Absturz-Einträge der Vor-Sitzung korrekt in `AppResources.resx` und `AppResources.de.resx`; Designer regeneriert (+162 Zeilen, alle 18 Schlüssel vorhanden).
- **`EmailService.ComposeAsync`:** `ThrowIfCancellationRequested()` wie empfohlen ergänzt (s. o.).

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
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` / `AppResources.Designer.cs` (18 neue Schlüssel, konsistent zwischen resx/de/Designer)
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
- Dokumentation gesichtet: `docs/help/anwendung/mobile-ui-design.md`, `test-results.md`, `test-results/issue-81/manual-*.png`, `docs/features/task-issue-81-.../*`
