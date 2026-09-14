<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Debuginformationen sammeln und per E-Mail versenden (Issue #81)

Analysiert wurden die `SettingsPage` mit `SettingsViewModel`, das `Settings`-Datenmodell samt Persistenz (`settings`-Tabelle, `ISyncLogRepository`, `Feed.HealthStatus`) sowie das Gateway-/DI-Muster der .NET-MAUI-App `Reporter` — bezogen auf die Anforderung in `requirement.md`.

## Zusammenfassung

**Vorhanden:**

- Etabliertes Gateway-Muster für den MAUI-Plattformzugriff aus `Reporter.Core` (net10.0, ohne MAUI-Referenz): Interfaces in `src/Reporter.Core/Interfaces/` (`ILocalNotificationService`, `INetworkStatusService`, `IAppThemeService`), Implementierungen in `src/Reporter/Services/`, DI-Registrierung als `AddSingleton` in `MauiProgram`.
- `Settings`-Singleton (Core-Modell + Entity + `settings`-Tabelle) mit dem kompletten Erweiterungspfad für neue bool-Eigenschaften: `required`-Property im Modell, Default im Entity, `ConfigureSettings`-Mapping (snake_case, `IsRequired().HasDefaultValue(...)`), `entity.HasData(new Settings())`, `SettingsRepository.SaveAsync`/`MapToModel`, EF-Migrationen nach `AddSettings*`-Namenskonvention; `MauiProgram.ApplyPersistedLanguage` führt `context.Database.Migrate()` beim Start aus.
- `SettingsViewModel` mit `PersistOnChange`/`PersistAsync`-Muster (`_persistLock`, `_isLoading`-Guard), `LoadAsync` und dem Fehlerpfad-Muster `NotificationAuthorizationDenied`-Event → `DisplayAlertAsync` im Code-Behind der `SettingsPage`.
- Report-Datenquellen existieren bereits: `ISyncLogRepository.GetAllAsync` liefert alle `sync_logs`-Einträge absteigend nach `StartedAt`; `Feed.HealthStatus`/`LastCheckedAt`/`HealthLastChange` mit `FeedHealth`-Konstanten (`OK`/`Warning`/`Error`, geschrieben von `FeedSyncService`); `INetworkStatusService.IsOnline`; `IFeedRepository.GetAllAsync`/`GetAllWithDetailsAsync`.
- `SettingsPage.xaml` mit der beschriebenen Karten-/Zeilen-Konvention (Section-Header `UiLabelStyle` + `Border` `RoundRectangle 12` + `SurfaceCard`-`AppThemeBinding`, `Switch`-/`Picker`-Zeilen, Hint-`Border` mit `MultiTrigger`, 44-pt-Touch-Ziele, `SemanticProperties.Description`) — bestehende Sektionen: Aufbewahrungsdauer, Keyword-Filter, Synchronisation & Lesefluss, Benachrichtigungen & Ruhezeiten, Erscheinungsbild, Sprache.
- Test-Infrastruktur mit handgeschriebenen Fakes (`FakeLocalNotificationService`, `FakeNetworkStatusService`, `FakeAppThemeService`, `FakeAutoRefreshService`), `TestDbContextFactory` (In-Memory-SQLite), `TestSettingsHelper`, `TestDataSeeder`, `TestWaitHelper` sowie den benannten Testklassen `SettingsViewModelTests_*`, `SettingsRepositoryTests`, `ReporterDbContextTests_*`, `SyncLogRepositoryTests`, `ServiceCollectionTests`.

**Offensichtlich fehlend:**

- Keine E-Mail-/Geräteinfo-Funktionalität: keine `IEmailService`-/`IDeviceInfoProvider`-Interfaces, keine `EmailService`-/`DeviceInfoProvider`-Implementierungen, kein `DebugReportService`. Einzige `AppInfo`-Nutzung ist `AppInfo.Current.ShowSettingsUI()` in `SettingsPage.xaml.cs`; `Email.ComposeAsync`/`DeviceInfo` werden nirgends verwendet.
- `Settings.DebugCollectionEnabled` fehlt in Modell, Entity, `ConfigureSettings`, `SettingsRepository`-Mapping und Migrationen.
- `SettingsPage` hat keinen Diagnose-/Support-Abschnitt; kein Entwurfs-Screen dafür unter `design-draft/` (Referenz: `einstellungen_filter[_dark_mode]/screen.png`).
- `AppResources.resx`/`.de.resx` enthalten keine `Debug*`-/Support-Schlüssel.
- `sync_logs` wächst unbegrenzt — `RetentionCleanupService` bereinigt nur `items`; für den Report ist keine Begrenzung/Anhang-Infrastruktur vorhanden.
- Keine Fakes/Tests für E-Mail- oder Geräteinfo-Gateways (`FakeEmailService`, `FakeDeviceInfoProvider`, `DebugReportServiceTests` fehlen).

**Test-Ausgangszustand:** `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results-baseline.trx" --logger "console;verbosity=normal"` → **409/409 Tests erfolgreich, 0 fehlgeschlagen, 0 übersprungen** (Exit-Code 0, 2026-09-14 07:00 MESZ, Commit `e7830d0`). Keine bekannten Fehlschläge. Testlücke: Das MAUI-Projekt `Reporter` ist nicht Teil der Testsuite — UI-/E-Mail-Verifikation bleibt manuell. Nachweis und Details: [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Views / UI](inventory/views.md)
- [Tests](inventory/tests.md)
