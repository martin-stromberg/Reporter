<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

### Neue Objekte — Reporter.Core

- [x] `IEmailService` (Interface, `src/Reporter.Core/Interfaces/IEmailService.cs`) — angelegt: `IsSupported`, `Task<bool> ComposeAsync(string recipient, string subject, string body, CancellationToken)`
- [x] `IDeviceInfoProvider` (Interface, `src/Reporter.Core/Interfaces/IDeviceInfoProvider.cs`) — angelegt: `AppDeviceInfo GetSnapshot()`
- [x] `IDebugLogRepository` (Interface, `src/Reporter.Core/Interfaces/IDebugLogRepository.cs`) — angelegt: `GetAllAsync` (absteigend nach `Timestamp`), `AddAsync`, `DeleteAllAsync`, `TrimToLatestAsync(int)`
- [x] `IDebugLogService` (Interface, `src/Reporter.Core/Interfaces/IDebugLogService.cs`) — angelegt: `IsEnabled`, `BeginSessionAsync`, `SetEnabled(bool)`, `LogAsync(category, message, details, level, ct)` mit Default `DebugLogLevel.Info`
- [x] `IDebugReportService` (Interface, `src/Reporter.Core/Interfaces/IDebugReportService.cs`) — angelegt: `IsSupported`, `Task<bool> SendReportAsync(CancellationToken)`
- [x] `AppDeviceInfo` (Datenmodell, `src/Reporter.Core/Models/AppDeviceInfo.cs`) — alle sieben init-only Felder vorhanden (`AppName`, `AppVersion`, `AppBuild`, `DeviceModel`, `DeviceManufacturer`, `Platform`, `OsVersion`)
- [x] `DebugLogEntry` (Core-Modell, `src/Reporter.Core/Models/DebugLogEntry.cs`) — `Id`/`Timestamp` required, `Level`/`Category`/`Message`/`Details` je `string?`
- [x] `DebugLogLevel` (Konstantenklasse, `src/Reporter.Core/Services/DebugLogLevel.cs`) — `Info`/`Warning`/`Error`
- [x] `DebugLogCategory` (Konstantenklasse, `src/Reporter.Core/Services/DebugLogCategory.cs`) — `Lifecycle`, `Sync`, `Exception`, `Settings`, `Report`
- [x] `DebugLogService` (Klasse, `src/Reporter.Core/Services/DebugLogService.cs`) — `_enabled`-Flag, `BeginSessionAsync` (DeleteAll + `Settings.DebugCollectionEnabled` laden + Start-Eintrag), `SetEnabled` mit Übergangseintrag (fire-and-forget), `LogAsync` No-op bei deaktiviert + `AddAsync` + `TrimToLatestAsync(MaxStoredEntries = 500)`, fängt eigene Fehler auf `Debug.WriteLine`, optionaler `TimeProvider?`
- [x] `DebugReportService` (Klasse, `src/Reporter.Core/Services/DebugReportService.cs`) — alle sechs Datenquellen inkl. `IDebugLogRepository`; Konstanten `DebugReportRecipient` (Platzhalter `debug@example.com`), `MaxSyncLogEntries = 50`, `MaxDebugLogEntries = 200`; lokalisierter Betreff (`{0}`-Platzhalter) und sieben `DebugReportSection*`-Header; `Report`-Logeintrag über optionalen `IDebugLogService?`; nur `IsSupported == false`/`ComposeAsync == false` → `false`, unerwartete Exceptions propagieren; optionaler `TimeProvider?`

### Neue Objekte — Reporter.Data

- [x] `DebugLogEntry` (Entity, `src/Reporter.Data/Entities/DebugLogEntry.cs`) — settable Properties, gleiche Felder wie Modell
- [x] `DebugLogRepository` (Klasse, `src/Reporter.Data/Repositories/DebugLogRepository.cs`) — `IDbContextFactory`-Muster, `AsNoTracking` + `OrderByDescending(Timestamp)`, `DeleteAllAsync`/`TrimToLatestAsync` via `ExecuteDeleteAsync`, `MapToModel`/`MapToEntity`
- [x] Migration `AddSettingsDebugCollection` (`src/Reporter.Data/Migrations/20260914060413_AddSettingsDebugCollection.cs`) — `settings.debug_collection_enabled` `NOT NULL DEFAULT false`
- [x] Migration `AddDebugLogEntries` (`src/Reporter.Data/Migrations/20260914060416_AddDebugLogEntries.cs`) — Tabelle `debug_log_entries` mit Index `IX_debug_log_entries_timestamp`

### Neue Objekte — Reporter (MAUI)

- [x] `EmailService` (`src/Reporter/Services/EmailService.cs`) — `IsSupported` → `Email.Default.IsComposeSupported`, `ComposeAsync` baut `EmailMessage` mit `BodyFormat.PlainText` und kapselt Plattformfehler auf `false`
- [x] `DeviceInfoProvider` (`src/Reporter/Services/DeviceInfoProvider.cs`) — mappt `AppInfo.Current`/`DeviceInfo.Current` auf `AppDeviceInfo`

### Geänderte bestehende Klassen

- [x] Feld `DebugCollectionEnabled` in `Settings` (Core-Modell, `required bool`) — `src/Reporter.Core/Models/Settings.cs` Z. 91
- [x] Feld `DebugCollectionEnabled` in `Settings` (Entity, `bool` C#-Default `false`) — `src/Reporter.Data/Entities/Settings.cs` Z. 93
- [x] `ReporterDbContext` — `DbSet<DebugLogEntry>` (Z. 56), `ConfigureDebugLogEntry` mit Tabellen-/Spalten-Mapping und `timestamp`-Index (Z. 167–179), Aufruf in `OnModelCreating` (Z. 72), `ConfigureSettings`-Mapping `debug_collection_enabled` `IsRequired().HasDefaultValue(false)` (Z. 148), `entity.HasData(new Settings())` unverändert
- [x] `SettingsRepository.SaveAsync`/`MapToModel` — `DebugCollectionEnabled` ergänzt (Z. 68, 91)
- [x] `SettingsViewModel` — optionale Ctor-Parameter `IDebugReportService?`/`IDebugLogService?` + Felder; `DebugCollectionEnabled`-Setter mit `SetEnabled` + `OnPropertyChanged(DebugSendEnabled)` + `PersistOnChange`; `DebugEmailSupported`/`DebugSendEnabled` get-only; `SendDebugReportCommand`; `SendDebugReportAsync` mit Methoden-Guard (`_debugReportService is null || !DebugCollectionEnabled`), `DebugReportFailed` bei `false` und im Exception-Pfad mit `Debug.WriteLine` + `LogAsync(Report, Error)`; `LoadAsync`/`PersistAsync` um `DebugCollectionEnabled` ergänzt
- [x] `FeedSyncService` — optionaler `IDebugLogService?`-Parameter; `LogAsync(Error/Sync)` im `SyncFeedAsync`-Catch nach `UpdateLogAsync(FeedHealth.Error)`; Notification-Catch loggt `Warning` (Z. 97–101, 197–201)
- [x] `AutoRefreshService` — optionaler `IDebugLogService?`-Parameter; `LogAsync(Error/Sync)` in den Catches von `RunStartupSyncAsync` und `RunLoopAsync` (Z. 66, 160)
- [x] `ArticleDetailViewModel.LoadAsync` — Fallback-`new Settings`-Initializer um `DebugCollectionEnabled = false` ergänzt (Z. 271)
- [x] `App.xaml.cs` — `OnStart`: `IDebugLogService` nach `MigrateAsync()` aufgelöst + `BeginSessionAsync`; `UnhandledException`/`UnobservedTaskException` abonniert; `LogAsync(Lifecycle, Error)` in allen vier OnStart-Catches; Overrides `OnSleep`/`OnResume` mit „App suspended"/„App resumed"; private Handler `OnUnhandledException`/`OnUnobservedTaskException` fire-and-forget `LogAsync(Exception, Error)`
- [x] `SettingsPage.xaml.cs` — `DebugReportFailed` in `OnAppearing`/`OnDisappearing` abonniert/deabonniert; `OnDebugReportFailed` → `DisplayAlertAsync(DebugReportFailedTitle/DebugReportFailedMessage/ButtonOk)`
- [x] `MauiProgram` — alle fünf `AddSingleton`-Registrierungen (`IDebugLogRepository`, `IDebugLogService`, `IEmailService`, `IDeviceInfoProvider`, `IDebugReportService`) im `builder.Services`-Block (Z. 54, 67–70)
- [x] `AppResources.resx`/`AppResources.de.resx`/`AppResources.Designer.cs` — alle 16 geplanten Schlüssel mit Werten in EN und DE vorhanden

### UI (SettingsPage.xaml)

- [x] Section-Header `SettingsSectionDebug` mit `UiLabelStyle`, `Border Padding="16" RoundRectangle 12` `SurfaceCard`-`AppThemeBinding`, `VerticalStackLayout Spacing="12"` (Z. 454–516)
- [x] `Switch`-Zeile `Grid ColumnDefinitions="*,Auto"` mit Label + `MetaStyle`-Hint, `Switch IsToggled="{Binding DebugCollectionEnabled}"`, `SemanticProperties.Description`, 44-pt-Mindestmaß (Z. 462–476)
- [x] Senden-Bereich mit `IsEnabled="{Binding DebugSendEnabled}"` + `Opacity=0.4`-`DataTrigger`, Label + `MetaStyle`-Hint, `Button` mit `SendDebugReportCommand`, `SemanticProperties.Description`, `MinimumHeightRequest="44"` (Z. 477–499)
- [x] Hinweis-`Border` `IsVisible="False"` + `DataTrigger DebugEmailSupported == False` → `IsVisible = True`, Text `SettingsDebugEmailUnsupportedHint` (Z. 500–513)

### Tests und Test-Infrastruktur

- [x] `FakeDeviceInfoProvider` — setzbares `AppDeviceInfo`-Snapshot (`src/Reporter.Tests/FakeDeviceInfoProvider.cs`)
- [x] `FakeDebugLogService` + Record `LoggedEntry` — `IsEnabled` settable, `BeginSessionCallCount`, `SetEnabledCalls`, `LoggedEntries` (`src/Reporter.Tests/FakeDebugLogService.cs`)
- [x] `TestSettingsHelper.SaveAsync` — optionaler Parameter `debugCollectionEnabled`; alle `new Settings`-Initializer in den betroffenen Testklassen ergänzt (Kompilierung + 454 grüne Tests belegen Vollständigkeit)
- [x] `DebugLogRepositoryTests` — alle drei Plan-Szenarien vorhanden (als `GetAllAsync_OrdersByTimestampDescending`, `DeleteAllAsync_RemovesAllEntries`, `TrimToLatestAsync_KeepsNewestEntries`, plus `AddAsync_PersistsAllFields`)
- [x] `DebugLogServiceTests` — alle sieben Plan-Szenarien vorhanden (BeginSession-Reset/-Aktivierung/-Start-Eintrag, `LogAsync` disabled/enabled/trim/never-throws, `SetEnabled`-Übergang, plus Disable-Transition)
- [x] `DebugReportServiceTests` — alle elf Plan-Szenarien vorhanden (`SendReportAsync_*` statt `SendReport_*` benannt), plus `SendReportAsync_LogsReportEntry`
- [x] `FeedSyncServiceTests_DebugLog.SyncFeedAsync_Failure_LogsErrorEntry`, `AutoRefreshServiceTests_DebugLog` (`StartAsync_StartupSyncFails_LogsErrorEntry`, `TimerTick_SyncFails_LogsErrorEntry`)
- [x] `SettingsViewModelTests_Debug` — deckt `Load_PopulatesDebugCollectionEnabled` (`Load_ReadsPersistedDebugSwitch`), `Load_EmailUnsupported_ExposesDebugEmailSupportedFalse` (`DebugSendEnabled_RequiresCollectionAndEmailSupport`), Toggle-persistiert/-ruft-`SetEnabled` (`DebugCollectionEnabled_Toggle_PersistsAndSwitchesLog`), Versand (`SendDebugReport_Enabled_ComposesEmail`), Fehlerpfade (`SendDebugReport_ComposeFalse_RaisesDebugReportFailed`, `SendDebugReport_ComposeThrows_RaisesDebugReportFailed_AndLogs`), Guard (`SendDebugReport_Disabled_DoesNotCompose`)
- [x] `SettingsRepositoryTests.SaveAsync_PersistsDebugCollectionEnabled`; `ReporterDbContextTests_Persistence` (`Settings_DebugCollectionEnabled_PersistRoundtrip`, `DebugLogEntry_PersistRoundtrip`); `ReporterDbContextTests_Schema` inkl. `debug_log_entries`-Tabellen-/Spalten-/Index-Assertions
- [x] `ServiceCollectionTests` — `IDebugLogRepository` in `AddTestRepositories`; Auflösungstests für `IDebugLogRepository`/`IDebugLogService`/`IDebugReportService` mit Gateway-Fakes; `FakeDebugLogService` in der `FeedSyncService`-Registrierung
- [x] E2E (`DebugReportTests_E2E`) — `DebugCollection_PersistedAcrossSessions`, `Report_ComposesThroughRealServicesAndSqlite`, `SessionLog_WritesAndResetsAcrossSessions` (VM/Service → echte Repos → In-Memory-SQLite → Gateway-Fakes, inkl. `debug_log_entries`-Seeding)
- [x] Verifikation dokumentiert: 454/454 Tests grün, `Run-StaticChecks.ps1` Exit-Code 0, manuelle UI-Verifikation 390 × 844 pt Light + Dark mit Screenshots (`test-results/issue-81/manual-*.png`), Eintrag in `docs/help/anwendung/mobile-ui-design.md`

## Offene Aufgaben

- [ ] `SettingsPage.xaml` Abschnitt „Diagnose & Support" — teilweise umgesetzt: Der Plan fordert den Abschnitt „am Ende des `ScrollView` (nach „Sprache")"; implementiert steht er zwischen „Benachrichtigungen & Ruhezeiten" und „Erscheinungsbild" (`src/Reporter/Views/SettingsPage.xaml` Z. 454–516; „Sprache" folgt erst ab Z. 541). Außerdem ist die Senden-Zeile als `Border` (`SurfaceSubtle`, `RoundRectangle 8`) mit `VerticalStackLayout` (Label/Hint über voller Breite, Button darunter) umgesetzt statt als geplante `Grid ColumnDefinitions="*,Auto"`-Zeile; `IsEnabled`-Binding und `Opacity=0.4`-`DataTrigger` sind vorhanden. Inhaltlich sind alle Elemente (Header, Switch-Zeile, Senden-Aktion, Hinweis-`Border`) vollständig — die Abweichung betrifft Position und Zeilenstruktur.
- [ ] `FakeEmailService.ComposeCallCount` — fehlt: Der Plan sieht das Member `ComposeCallCount` vor (`src/Reporter.Tests/FakeEmailService.cs` besitzt nur `IsSupported`, `ComposeResult`, `ComposeException`, `ComposedEmails` + Record `ComposedEmail`). Die Zählung der Compose-Aufrufe erfolgt funktional äquivalent über `ComposedEmails` (`Assert.Single`/`Assert.Empty`).
- [ ] Manuelle Pflicht-Verifikation (Plan-Schritt 14 / E2E-Tabelle „Pflicht (manuell)") — teilweise ausstehend: Der Versand-Test mit echtem, registriertem Mail-Client (vorbefüllter Entwurf inkl. Session-Log-Sektion) und die iOS-Verifikation sind laut `test-results.md` („Ausstehende Verifikationen") noch nicht erbracht — Umgebungslimitation (kein `mailto:`-Client auf dem Prüf-PC, macOS für iOS erforderlich). Alle übrigen manuellen Szenarien (Abschnitt sichtbar, Opt-in persistiert, Übergangseintrag, Session-Reset, Dark Mode, Migration) sind dokumentiert bestanden.

## Hinweise

- Die implementierten Testmethodennamen weichen teils von den Plan-Namen ab (`SendReportAsync_*` statt `SendReport_*`, `BeginSessionAsync_*` statt `BeginSession_*`, `Load_ReadsPersistedDebugSwitch` statt `Load_PopulatesDebugCollectionEnabled`, E2E-Tests in `DebugReportTests_E2E` statt `SettingsViewModelTests_E2E`) — alle geplanten Prüfszenarien sind dennoch abgedeckt.
- Der VM-Fehlerpfad bei `IsSupported == false` (`SendReportAsync` → `false` → `DebugReportFailed`) ist indirekt abgesichert: `SendReportAsync_Unsupported_ReturnsFalse_AndDoesNotCompose` (Service-Ebene) + `SendDebugReport_ComposeFalse_RaisesDebugReportFailed` (VM-Ebene, identischer `!sent`-Pfad). Ein Test, der am VM den Fall `IsSupported == false` bei aktivierter Sammlung durchspielt, existiert nicht — funktional deckt `DebugSendEnabled_RequiresCollectionAndEmailSupport` die Property-Seite ab.
- Die Position der Senden-Aktion als `Border` mit vertikalem Stack statt `Grid *,Auto`-Zeile entspricht implizit der AGENTS.md-Regel „keine mehreren Text-Buttons in einer Zeile auf Mobile"; falls die Plan-Position/Struktur gewünscht ist, muss der Abschnitt ans Ende des `ScrollView` verschoben werden — andernfalls sollte der Plan/die Tasks-Datei als Referenz angepasst werden.
- `DebugReportService.DebugReportRecipient` steht bewusst auf dem Platzhalter `debug@example.com` — laut Plan vom Maintainer zu ersetzen (kein Implementierungsdefizit).
