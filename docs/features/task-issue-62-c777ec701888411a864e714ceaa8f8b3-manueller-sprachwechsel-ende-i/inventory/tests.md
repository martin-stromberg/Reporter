<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-09-13, ca. 12:47 Uhr +0200 (CEST)
- Branch und Commit-ID: `task/issue-62-c777ec701888411a864e714ceaa8f8b3-manueller-sprachwechsel-ende-i` @ `ef1e05eb2f00db0633cc1e355cb5ce29ef8ae3cd`
- Uncommittete Änderungen im getesteten Stand: nur untracked `docs/features/task-issue-62-c777ec701888411a864e714ceaa8f8b3-manueller-sprachwechsel-ende-i/` (Anforderungs-/Lifecycle-Dokumente, inkl. der während des Laufs erzeugten Inventory-Nachweise). Kein produktiver Code und keine Tests verändert.
- Testumgebung und Runtime-/SDK-Versionen: Windows (Git Bash), .NET SDK `10.0.401` (einzige installierte SDK-Version), Testprojekt `net10.0` mit xunit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, EF Core Sqlite 10.0.12 (In-Memory-DB via `TestDbContextFactory`), coverlet.collector 6.0.4, Microsoft.Extensions.TimeProvider.Testing 10.1.0. Solution-Build in Debug-Konfiguration (Default); das MAUI-Projekt baute die TFs `net10.0-windows10.0.19041.0` und `net10.0-ios`.
- Ermittelte Testsuiten und Quellen der Testbefehle: Einzige Testsuite des Projekts ist `src/Reporter.Tests/Reporter.Tests.csproj` (xunit). Testbefehl aus `.github/workflows/pr-staging-ci.yml` (Job `build-and-test`, Zeilen ~121–128): `dotnet build Reporter.sln` gefolgt von `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --no-build` mit trx-Logger; lokal analog in Debug statt Release ausgeführt. Coverage-Runsettings `src/Reporter.Tests/coverlet.runsettings` (schließt `Reporter.Data.Migrations.*` aus) wurden im Baseline-Lauf nicht verwendet.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Build-Baseline | `dotnet build Reporter.sln` | Repo-Root | 0 (0 Fehler, 1 Warnung CS8765 in `Platforms/iOS/AppDelegate.cs` — preexisting, iOS-Target) | — | — | — | [build-baseline.log](test-results/build-baseline.log) |
| Test-Baseline | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --no-build --logger "trx;LogFileName=test-results-baseline.trx" --results-directory .` | `docs/features/task-issue-62-…/inventory/test-results` | 0 | 316 | 0 | 0 | [test-baseline.log](test-results/test-baseline.log), [test-results-baseline.trx](test-results/test-results-baseline.trx) |

### Nachgewiesene bestehende Testfehler

Keine. Im Ausgangslauf sind alle 316 Tests erfolgreich gelaufen (0 fehlgeschlagen, 0 übersprungen, 0 nicht ausgeführt; laut trx: `total="316" executed="316" passed="316" failed="0"`).

### Testlücken und Ausführungsprobleme

- Keine fehlgeschlagenen, übersprungenen oder abgebrochenen Tests; keine Build-/Setup-/Infrastrukturfehler.
- Für das Sprach-Feature existieren noch **keine** Tests: kein `Language`-Roundtrip im `SettingsRepository`, kein `LanguageOptions`/`SelectedLanguage`-Test im `SettingsViewModel`, `TestSettingsHelper` hat keinen `language`-Parameter.
- Die MAUI-App selbst (`src/Reporter`) ist nicht testabgedeckt — UI/App-Start-Pfad (Kultur-Setzung, `SettingsPage`-Picker) ist nur manuell prüfbar.

## Testklassen

### `SettingsViewModelTests_Load` (`src/Reporter.Tests/SettingsViewModelTests_Load.cs`)

- `Load_PopulatesAllOptions` — `LoadCommand` befüllt alle Options-Properties aus persistierten Settings (inkl. `Theme="dark"` → `SelectedTheme`) und Keywords.
- `Load_InvalidPersistedValues_UsesFallbacks` — Unbekannte persistierte Werte (z. B. `Theme="sepia"`, Intervall 45) fallen auf Defaults zurück (`SelectedTheme` → `"system"`).
- `Load_PopulatesNotificationSummaryEnabled` — Flag wird ins ViewModel geladen.
- `Load_PermissionDenied_SetsNotificationPermissionDenied`, `Load_PermissionGranted_ClearsNotificationPermissionDenied`, `Load_PermissionNotDetermined_SetsNotificationPermissionNotDetermined` — Abbildung des `ILocalNotificationService`-Status auf die Hint-Properties.
- `Load_NotificationsSupported_ReflectsPlatformSupport` — `NotificationsSupported` spiegelt `IsSupported` bzw. fehlenden Service.
- `Load_NotificationsDisabled_HidesNotificationPermissionDenied` — Kein Denied-Hint bei ausgeschalteten Benachrichtigungen.
- `Load_WithoutQuietHours_QuietHoursDisabled` — Ruhezeiten aus, Werte `null`.
- `ThemeOptions_ExposePersistedValues` — `ThemeOptions` liefert exakt `["system","light","dark"]` (Muster für einen `LanguageOptions`-Test).

### `SettingsViewModelTests_Persist` (`src/Reporter.Tests/SettingsViewModelTests_Persist.cs`)

- `PropertyChange_PersistsImmediately` — Property-Änderung persistiert sofort.
- `RetentionDays_OutOfRange_Clamped` (Theory: 0→1, 400→365) — Clamp + Persist auf DragCompleted.
- `RetentionDays_ChangeWithoutDragCompleted_PersistsAfterDebounce`, `RetentionDays_RapidChanges_PersistOnlyLastValue`, `RetentionDays_DragCompleted_PersistsImmediatelyAndCancelsDebounce` — Debounce-Verhalten via `FakeTimeProvider`.
- `NotificationSummaryEnabled_Change_Persists` — Sofort-Persist.
- `NotificationsEnabled_TurnedOn_RequestsAuthorization`, `…_Denied_RaisesNotificationAuthorizationDenied`, `…_UnsupportedPlatform_SkipsRequest`, `…_TurnedOff_ClearsNotificationPermissionDenied`/`…NotDetermined` — Berechtigungsfluss.
- `RequestNotificationPermission_NotDetermined_RequestsAndClearsHint`.
- `ThemeChange_AppliesTheme` — `SelectedTheme`-Änderung ruft `IAppThemeService.ApplyTheme` (Muster: Sprachwechsel würde analog persistieren, aber keinen Service aufrufen).
- `AutoRefreshChange_AppliesSettings` — ApplySettingsAsync bei geänderten Auto-Refresh-Werten.
- `QuietHoursEnabled_*` (5 Tests) — Null-Persistierung, Session-Wiederherstellung, Defaults, Reload-Verhalten.
- `Persist_QueuedBehindRunningSave_AppliesThemeOnce` — Serialisierung unter `_persistLock`, Vergleich gegen frisch gespeicherten Stand.
- Private Hilfsklassen in der Datei: `RecordingSettingsRepository`, `GatedSettingsRepository` (Decorator-Fakes über `ISettingsRepository`).

### `SettingsViewModelTests_Keywords` (`src/Reporter.Tests/SettingsViewModelTests_Keywords.cs`)

- `AddKeyword_Valid_AddsToRepository`, `AddKeyword_DuplicateCaseInsensitive_ShowsError`, `AddKeyword_Empty_ShowsError`, `AddKeyword_TooLong_ShowsError`, `RemoveKeyword_DeletesFromRepository`.

### `SettingsViewModelTests_E2E` (`src/Reporter.Tests/SettingsViewModelTests_E2E.cs`)

- `E2E_ChangeSettings_PersistRoundtrip` — Änderung aller Optionen → Persist → Reload in neuer ViewModel-Instanz (inkl. `Theme="light"`); direkter E2E-Musterkandidat für `Language`.
- `E2E_NotificationSummary_PersistRoundtrip`, `E2E_KeywordAddRemove_Persists`, `E2E_KeywordEmpty_Rejected`, `E2E_KeywordDuplicate_Rejected`.

### `SettingsRepositoryTests` (`src/Reporter.Tests/SettingsRepositoryTests.cs`)

- `GetAsync_ReturnsSeededSettings`, `GetAsync_CreatesDefaultRecordWhenMissing`, `GetAsync_AlwaysReturnsSingleRecord` — Singleton-Verhalten/Seed.
- `SaveAsync_OverwritesSameRecord`, `SaveAsync_IgnoresDifferentIdAndUpdatesDefault`, `SaveAsync_PersistsNewFields` (Auto-Refresh + `Theme="dark"`-Roundtrip), `SaveAsync_PersistsNotificationSummaryEnabled`.

### `SettingsValuesTests_AutoMarkRead` (`src/Reporter.Tests/SettingsValuesTests_AutoMarkRead.cs`)

- `IsAutoMarkReadEnabled_EvaluatesMode` (Theory) — Mapping der persistierten Modi auf `bool`.

### `ReporterDbContextTests_Schema` / `ReporterDbContextTests_Persistence`

- `EnsureCreatedAsync_CreatesQueryableTables` — alle Tabellen abfragbar, `settings` enthält genau 1 Seed-Datensatz.

### `ServiceCollectionTests` (`src/Reporter.Tests/ServiceCollectionTests.cs`)

- Prüft u. a. DI-Registrierung von `ISettingsRepository` (Zeilen 33/42).

## Hilfsmethoden

### `TestSettingsHelper` (`src/Reporter.Tests/TestSettingsHelper.cs`)

- `SaveAsync(ISettingsRepository, bool? notificationsEnabled, bool? notificationSummaryEnabled, TimeSpan? quietHoursStart, TimeSpan? quietHoursEnd)` — lädt persistierte Settings und speichert sie mit Overrides zurück; `null` = Wert behalten. Kopiert feldweise (inkl. `Theme = settings.Theme`, Zeile 44); **kein `language`-Parameter** — bei Ergänzung von `Settings.Language` müsste das Feld hier mitkopiert werden, sonst würden Helper-Aufrufe es implizit zurücksetzen.

### `TestDbContextFactory` (`src/Reporter.Tests/TestDbContextFactory.cs`)

- `IDbContextFactory<ReporterDbContext>` auf Shared-In-Memory-SQLite (`Mode=Memory;Cache=Shared`, Keep-Alive-Connection, `Default Timeout=30`); `EnsureCreated()` im Konstruktor; `CreateDbContext()` liefert eigene Contexts; `IDisposable`.

### `TestWaitHelper` (`src/Reporter.Tests/TestWaitHelper.cs`)

- `WaitUntilAsync(...)` — Polling-Helfer, um fire-and-forget `PersistAsync()`-Effekte abzuwarten (genutzt von allen Persist-Tests; auch für eine `SelectedLanguage`-Persistierung relevant).

### Fakes (`src/Reporter.Tests/`)

- `FakeAppThemeService` — `IAppThemeService`, sammelt Aufrufe in `AppliedThemes` (Vorbild für einen ggf. nötigen `FakeLanguageService`).
- `FakeAutoRefreshService` — `IAutoRefreshService`, sammelt `AppliedSettings`.
- `FakeLocalNotificationService` — `IsSupported`, `AuthorizationStatus`, `AuthorizationResult`, `RequestAuthorizationCallCount`.
- `FakeNetworkStatusService`, `FakeNotificationService`, `FakeFeedSearchService`, `FakeFeedSyncService` — nicht settings-bezogen.
