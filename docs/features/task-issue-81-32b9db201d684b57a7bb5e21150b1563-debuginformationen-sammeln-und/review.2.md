<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

### Neue Objekte — Reporter.Core

- [x] `IEmailService` (Interface, `src/Reporter.Core/Interfaces/IEmailService.cs`) — angelegt: `IsSupported`, `Task<bool> ComposeAsync(string recipient, string subject, string body, CancellationToken)`
- [x] `IDeviceInfoProvider` (Interface, `src/Reporter.Core/Interfaces/IDeviceInfoProvider.cs`) — angelegt: `AppDeviceInfo GetSnapshot()`
- [x] `IDebugLogRepository` (Interface, `src/Reporter.Core/Interfaces/IDebugLogRepository.cs`) — angelegt inkl. `GetLatestAsync(int maxEntries)` (Z. 23) neben `GetAllAsync` (absteigend nach `Timestamp`), `AddAsync`, `DeleteAllAsync`, `TrimToLatestAsync(int)` — ohne `GetById`/`Update`/`Delete(id)` wie geplant
- [x] `IDebugLogService` (Interface, `src/Reporter.Core/Interfaces/IDebugLogService.cs`) — angelegt: `IsEnabled`, `BeginSessionAsync`, `SetEnabled(bool)`, `LogAsync(category, message, details, level, ct)` mit Default `DebugLogLevel.Info`
- [x] `IDebugReportService` (Interface, `src/Reporter.Core/Interfaces/IDebugReportService.cs`) — angelegt: `IsSupported`, `Task<bool> SendReportAsync(CancellationToken)`
- [x] `AppDeviceInfo` (Datenmodell, `src/Reporter.Core/Models/AppDeviceInfo.cs`) — alle sieben init-only Felder vorhanden (`AppName`, `AppVersion`, `AppBuild`, `DeviceModel`, `DeviceManufacturer`, `Platform`, `OsVersion`)
- [x] `DebugLogEntry` (Core-Modell, `src/Reporter.Core/Models/DebugLogEntry.cs`) — `Id`/`Timestamp` required, `Level`/`Category`/`Message`/`Details` je `string?`
- [x] `DebugLogLevel` (Konstantenklasse, `src/Reporter.Core/Services/DebugLogLevel.cs`) — `Info`/`Warning`/`Error`
- [x] `DebugLogCategory` (Konstantenklasse, `src/Reporter.Core/Services/DebugLogCategory.cs`) — `Lifecycle`, `Sync`, `Exception`, `Settings`, `Report`
- [x] `DebugLogService` (Klasse, `src/Reporter.Core/Services/DebugLogService.cs`) — `_enabled`-Flag, `BeginSessionAsync` (DeleteAll + `Settings.DebugCollectionEnabled` laden + Start-Eintrag, Z. 44–62), `SetEnabled` mit Übergangseintrag als fire-and-forget (Z. 65–74), `LogAsync` No-op bei deaktiviert oder abgebrochenem `CancellationToken` + `AddAsync` + `TrimToLatestAsync(MaxStoredEntries = 500)` (Z. 77–103), eigene Fehler auf `Debug.WriteLine`, optionaler `TimeProvider?`
- [x] `DebugReportService` (Klasse, `src/Reporter.Core/Services/DebugReportService.cs`) — alle sechs Datenquellen inkl. `IDebugLogRepository.GetLatestAsync` und `ISyncLogRepository.GetLatestAsync`; Konstanten `DebugReportRecipient` (als Platzhalter `debug@example.com` gekennzeichnet, Z. 27), `MaxSyncLogEntries = 50`, `MaxDebugLogEntries = 200`; lokalisierter Betreff (`{0}`-Platzhalter) und sieben `DebugReportSection*`-Header; `Report`-Logeintrag (`Info` bei Erfolg, `Error` bei `ComposeAsync == false` und `IsSupported == false`) über optionalen `IDebugLogService?`; nur `IsSupported == false`/`ComposeAsync == false` → `false`, unerwartete Exceptions propagieren; optionaler `TimeProvider?`

### Neue Objekte — Reporter.Data

- [x] `DebugLogEntry` (Entity, `src/Reporter.Data/Entities/DebugLogEntry.cs`) — settable Properties, gleiche Felder wie Modell
- [x] `DebugLogRepository` (Klasse, `src/Reporter.Data/Repositories/DebugLogRepository.cs`) — `IDbContextFactory`-Muster, `AsNoTracking` + `OrderByDescending(Timestamp)`, `GetLatestAsync` mit `Take` in der Abfrage (Z. 38–47), `DeleteAllAsync`/`TrimToLatestAsync` via `ExecuteDeleteAsync` mit `CountAsync`-Vorabprüfung (Z. 58–82), `MapToModel`/`MapToEntity`
- [x] Migration `AddSettingsDebugCollection` (`src/Reporter.Data/Migrations/20260914060413_AddSettingsDebugCollection.cs`) — `settings.debug_collection_enabled` `NOT NULL DEFAULT false`
- [x] Migration `AddDebugLogEntries` (`src/Reporter.Data/Migrations/20260914060416_AddDebugLogEntries.cs`) — Tabelle `debug_log_entries` (`id`, `timestamp`, `level`, `category`, `message`, `details`) mit Index `IX_debug_log_entries_timestamp`

### Neue Objekte — Reporter (MAUI)

- [x] `EmailService` (`src/Reporter/Services/EmailService.cs`) — `IsSupported` → `Email.Default.IsComposeSupported`, `ComposeAsync` baut `EmailMessage` mit `BodyFormat.PlainText` und `To`-Liste, kapselt Plattformfehler auf `false` (Z. 34–37)
- [x] `DeviceInfoProvider` (`src/Reporter/Services/DeviceInfoProvider.cs`) — mappt `AppInfo.Current`/`DeviceInfo.Current` auf `AppDeviceInfo`

### Geänderte bestehende Klassen

- [x] Feld `DebugCollectionEnabled` in `Settings` (Core-Modell, `required bool`) — `src/Reporter.Core/Models/Settings.cs` Z. 91
- [x] Feld `DebugCollectionEnabled` in `Settings` (Entity, `bool` C#-Default `false`) — `src/Reporter.Data/Entities/Settings.cs` Z. 93
- [x] `ISyncLogRepository`/`SyncLogRepository` — `GetLatestAsync(int maxEntries)` mit `OrderByDescending(StartedAt)` + `Take` in der Abfrage (`src/Reporter.Core/Interfaces/ISyncLogRepository.cs` Z. 23; `src/Reporter.Data/Repositories/SyncLogRepository.cs` Z. 38–47); `GetAllAsync` unverändert
- [x] `ReporterDbContext` — `DbSet<DebugLogEntry>` (Z. 56), `ConfigureDebugLogEntry` mit Tabellen-/Spalten-Mapping (`level` MaxLength 20, `category` MaxLength 50) und `timestamp`-Index (Z. 167–179), Aufruf in `OnModelCreating` (Z. 72), `ConfigureSettings`-Mapping `debug_collection_enabled` `IsRequired().HasDefaultValue(false)` (Z. 148), `entity.HasData(new Settings())` erbt C#-Default `false`
- [x] `SettingsRepository.SaveAsync`/`MapToModel` — `DebugCollectionEnabled` ergänzt (Z. 68, 91)
- [x] `SettingsViewModel` — optionale Ctor-Parameter `IDebugReportService?`/`IDebugLogService?` + Felder (Z. 86–87, 94–95); `DebugCollectionEnabled`-Setter mit `_isLoading`-Guard für `SetEnabled`, `OnPropertyChanged(DebugSendEnabled)` + `PersistOnChange` (Z. 452–468); `DebugEmailSupported`/`DebugSendEnabled` get-only (Z. 474, 480); `SendDebugReportCommand` (Z. 137, 169); `SendDebugReportAsync` mit Methoden-Guard (`_debugReportService is null || !DebugCollectionEnabled`, kein `CanExecute`-Guard), `DebugReportFailed` bei `false` und im Exception-Pfad mit `Debug.WriteLine` + `LogAsync(Report, Error)` (Z. 896–930); `LoadAsync`/`PersistAsync` um `DebugCollectionEnabled` ergänzt (Z. 630, 814); Event `DebugReportFailed` (Z. 445)
- [x] `FeedSyncService` — optionaler `IDebugLogService?`-Parameter (Z. 50, 60); `LogAsync(Error/Sync)` im `SyncFeedAsync`-Catch nach `UpdateLogAsync(FeedHealth.Error)` (Z. 97–101); Notification-Catch loggt `Warning` (Z. 197–201)
- [x] `AutoRefreshService` — optionaler `IDebugLogService?`-Parameter (Z. 34, 39); `LogAsync(Error/Sync)` in den Catches von `RunStartupSyncAsync` und `RunLoopAsync` (Z. 66, 160)
- [x] `ArticleDetailViewModel.LoadAsync` — Fallback-`new Settings`-Initializer um `DebugCollectionEnabled = false` ergänzt (Z. 271)
- [x] `App.xaml.cs` — `OnStart`: `IDebugLogService` nach `MigrateAsync()` aufgelöst + `await BeginSessionAsync()` (Z. 44–46); `UnhandledException`/`UnobservedTaskException` abonniert (Z. 48–49); `LogAsync(Lifecycle, Error)` in allen vier OnStart-Catches (Z. 60, 74, 86, 98); Overrides `OnSleep`/`OnResume` mit „App suspended"/„App resumed" (Z. 103–114); private Handler `OnUnhandledException`/`OnUnobservedTaskException` mit fire-and-forget `LogAsync(Exception, Error)` (Z. 136–149)
- [x] `SettingsPage.xaml.cs` — `DebugReportFailed` in `OnAppearing`/`OnDisappearing` abonniert/deabonniert (Z. 31, 42); `OnDebugReportFailed` → `DisplayAlertAsync(DebugReportFailedTitle/DebugReportFailedMessage/ButtonOk)` (Z. 61–67)
- [x] `MauiProgram` — alle fünf `AddSingleton`-Registrierungen (`IDebugLogRepository`, `IDebugLogService`, `IEmailService`, `IDeviceInfoProvider`, `IDebugReportService`) im `builder.Services`-Block (Z. 54, 67–70); `ApplyPersistedLanguage` führt `context.Database.Migrate()` vor `BeginSessionAsync` aus (Z. 110)
- [x] `AppResources.resx`/`AppResources.de.resx`/`AppResources.Designer.cs` — alle 18 geplanten Schlüssel (`SettingsSectionDebug`, `SettingsDebugCollection*`/`SettingsDebugSend*`, `SettingsDebugCollectionRequiredHint`, `SettingsDebugEmailUnsupportedHint`, `DebugReportFailed*`, `DebugReportEmailSubject`, sieben `DebugReportSection*`) mit Werten in EN und DE vorhanden

### UI (SettingsPage.xaml)

- [x] Abschnitt „Diagnose & Support" am Ende des `ScrollView` nach „Sprache" (Z. 511–587; Sprache endet Z. 509, `ScrollView` schließt Z. 590): Section-Header `SettingsSectionDebug` mit `UiLabelStyle`, `Border Padding="16" RoundRectangle 12` `SurfaceCard`-`AppThemeBinding`, `VerticalStackLayout Spacing="12"`
- [x] `Switch`-Zeile `Grid ColumnDefinitions="*,Auto"` mit Label + `MetaStyle`-Hint, `Switch IsToggled="{Binding DebugCollectionEnabled}"`, `SemanticProperties.Description`, 44-pt-Mindestmaß (Z. 519–533)
- [x] Senden-Bereich als `Border` (`SurfaceSubtle`, `RoundRectangle 8`) mit `VerticalStackLayout` und vollbreitem `Button` — der im Plan dokumentierten Abweichung vom `Grid *,Auto`-Entwurf entsprechend — mit `IsEnabled="{Binding DebugSendEnabled}"` + `Opacity=0.4`-`DataTrigger`, Label + `MetaStyle`-Hint, `SendDebugReportCommand`, `SemanticProperties.Description`, `MinimumHeightRequest="44"` (Z. 534–556)
- [x] Hinweis-`Border` `IsVisible="False"` + `DataTrigger DebugCollectionEnabled == False` → `IsVisible = True`, Text `SettingsDebugCollectionRequiredHint` (Z. 557–570)
- [x] Hinweis-`Border` `IsVisible="False"` + `DataTrigger DebugEmailSupported == False` → `IsVisible = True`, Text `SettingsDebugEmailUnsupportedHint` (Z. 571–584)

### Tests und Test-Infrastruktur

- [x] `FakeEmailService` + Record `ComposedEmail` (`src/Reporter.Tests/FakeEmailService.cs`) — `IsSupported`/`ComposeResult`/`ComposeException` settable, `ComposedEmails`-Liste, `ComposeCallCount` (Z. 40)
- [x] `FakeDeviceInfoProvider` (`src/Reporter.Tests/FakeDeviceInfoProvider.cs`) — setzbares `AppDeviceInfo`-Snapshot
- [x] `FakeDebugLogService` + Record `LoggedEntry` (`src/Reporter.Tests/FakeDebugLogService.cs`) — `IsEnabled` settable, `BeginSessionCallCount`, `SetEnabledCalls`, `LoggedEntries`
- [x] `TestSettingsHelper.SaveAsync` — optionaler Parameter `debugCollectionEnabled` (Z. 37, 56); alle `new Settings`-Initializer in den betroffenen Testklassen (`SettingsViewModelTests_Load`, `SettingsViewModelTests_Persist`, `SettingsRepositoryTests`, `RetentionCleanupServiceTests`, `AutoRefreshServiceTests`) um die `required`-Property ergänzt
- [x] `DebugLogRepositoryTests` — alle Plan-Szenarien vorhanden (`AddAsync_PersistsAllFields`, `GetAllAsync_OrdersByTimestampDescending`, `GetLatestAsync_ReturnsNewestEntriesLimited`, `DeleteAllAsync_RemovesAllEntries`, `TrimToLatestAsync_KeepsNewestEntries`, `TrimToLatestAsync_UnderLimit_KeepsAllEntries`)
- [x] `DebugLogServiceTests` — alle sieben Plan-Szenarien vorhanden (BeginSession-Reset/-Aktivierung/-Start-Eintrag, `LogAsync` disabled/enabled/trim/never-throws, `SetEnabled`-Übergang, plus Disable-Transition)
- [x] `DebugReportServiceTests` — alle elf Plan-Szenarien vorhanden (`SendReportAsync_*` benannt), plus `SendReportAsync_LogsReportEntry`/`_LogsErrorEntry`/`_Unsupported_LogsErrorEntry` für den `Report`-Logeintrag
- [x] `FeedSyncServiceTests_DebugLog.SyncFeedAsync_Failure_LogsErrorEntry`, `AutoRefreshServiceTests_DebugLog` (`StartAsync_StartupSyncFails_LogsErrorEntry`, `TimerTick_SyncFails_LogsErrorEntry`)
- [x] `SettingsViewModelTests_Debug` — deckt alle Plan-Szenarien ab: `Load_ReadsPersistedDebugSwitch`, `Load_PersistedDebugSwitch_DoesNotCallSetEnabled`, `DebugCollectionEnabled_Toggle_PersistsAndSwitchesLog`, `DebugSendEnabled_RequiresCollectionAndEmailSupport`, `SendDebugReport_Enabled_ComposesEmail`, `SendDebugReport_Disabled_DoesNotCompose`, `SendDebugReport_ComposeFalse_RaisesDebugReportFailed`, `SendDebugReport_ComposeThrows_RaisesDebugReportFailed_AndLogs`, `WithoutDebugServices_SendIsDisabledAndToggleStillPersists`
- [x] `SettingsRepositoryTests.SaveAsync_PersistsDebugCollectionEnabled`; `ReporterDbContextTests_Persistence` (`Settings_DebugCollectionEnabled_PersistRoundtrip`, `DebugLogEntry_PersistRoundtrip`); `ReporterDbContextTests_Schema` inkl. `debug_log_entries`-Tabellen-/Spalten-/Index-Assertions (Z. 37, 52–72) und `debug_collection_enabled`-Property-Assertion (Z. 75)
- [x] `ServiceCollectionTests` — `IDebugLogRepository` in `AddTestRepositories` (Z. 126); Auflösungstests `AddReporterRepositories_ResolvesAllRepositories` (inkl. `IDebugLogRepository`, Z. 38) und `AddReporterServices_ResolvesDebugServices` (`IDebugLogService` + `IDebugReportService` mit Gateway-Fakes, Z. 95–115); `FakeDebugLogService` in der `FeedSyncService`-Registrierung (Z. 80)
- [x] `SyncLogRepositoryTests.GetLatestAsync_ReturnsNewestSyncLogsLimited` (Z. 88)
- [x] E2E (`DebugReportTests_E2E`) — `DebugCollection_PersistedAcrossSessions`, `Report_ComposesThroughRealServicesAndSqlite`, `SessionLog_WritesAndResetsAcrossSessions` (VM/Service → echte Repos → In-Memory-SQLite → Gateway-Fakes, inkl. `debug_log_entries`-Seeding)
- [x] Verifikation dokumentiert: 454/454 Tests grün und `Run-StaticChecks.ps1` Exit-Code 0 (`test-results.md`); manuelle Laufzeit-Verifikation 390 × 844 pt Light + Dark mit Screenshots (`test-results/issue-81/manual-*.png`), dokumentiert in `docs/help/anwendung/mobile-ui-design.md` (Abschnitt „Diagnose & Support (issue-81)")

## Offene Aufgaben

- [ ] Manuelle Pflicht-Verifikation (Plan-Schritt 14 / E2E-Tabelle „Pflicht (manuell)") — teilweise umgesetzt: Der Versand-Test mit einem echten, registrierten Mail-Client (vorbefüllter Entwurf inkl. Session-Log-Sektion) und die iOS-Verifikation stehen weiterhin aus — Umgebungslimitation (kein konfigurierter Mail-Client auf dem Prüf-PC, `net10.0-ios` benötigt macOS). Die übrigen manuellen Szenarien (Abschnitt sichtbar/Layout 390 × 844 Light + Dark, Opt-in-Persistenz, Übergangseintrag, Session-Reset nach App-Neustart) sind per Screenshots und `docs/help/anwendung/mobile-ui-design.md` dokumentiert bestanden.

## Hinweise

- Die implementierten Testmethodennamen weichen teils von den Plan-Namen ab (`SendReportAsync_*` statt `SendReport_*`, `BeginSessionAsync_*` statt `BeginSession_*`, `Load_ReadsPersistedDebugSwitch` statt `Load_PopulatesDebugCollectionEnabled`, E2E-Tests in `DebugReportTests_E2E` statt `SettingsViewModelTests_E2E`, `AddReporterServices_ResolvesDebugServices` statt getrennter `ResolvesDebugLogService`/`ResolvesDebugReportService`) — alle geplanten Prüfszenarien sind dennoch abgedeckt.
- Dokumentationsinkonsistenz: Die aktuelle `test-results.md` (Status „Fehler vorhanden") führt die manuellen Verifikationen pauschal als „Nicht ausgeführt" auf, während `docs/help/anwendung/mobile-ui-design.md` und die Screenshots `test-results/issue-81/manual-*.png` die erfolgte Laufzeit-Verifikation der UI-/Session-Szenarien belegen. Beim nächsten Testlauf sollte `test-results.md` entsprechend korrigiert werden (tatsächlich offen sind nur echter Mail-Client und iOS).
- `DebugReportService.DebugReportRecipient` steht bewusst auf dem Platzhalter `debug@example.com` — laut Plan vom Maintainer zu ersetzen (kein Implementierungsdefizit).
- Der VM-Fehlerpfad bei `IsSupported == false` (Service → `false` → `DebugReportFailed`) ist indirekt abgesichert: `SendReportAsync_Unsupported_ReturnsFalse_AndDoesNotCompose` (Service-Ebene) + `SendDebugReport_ComposeFalse_RaisesDebugReportFailed` (VM-Ebene, identischer `!sent`-Pfad); `DebugSendEnabled_RequiresCollectionAndEmailSupport` deckt die Property-Seite ab.
