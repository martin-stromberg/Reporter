# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

### Neue Klassen / Interfaces

- [x] `IKeywordMatcher` (Interface) — angelegt in `src/Reporter.Core/Interfaces/IKeywordMatcher.cs` (`MatchesAny(string? title, string? contentHtml, IEnumerable<string> keywords)`)
- [x] `KeywordMatcher` (Klasse) — angelegt in `src/Reporter.Core/Services/KeywordMatcher.cs` (`Contains` + `OrdinalIgnoreCase` auf Titel und ContentHtml, leere Keywords übersprungen)
- [x] `IAutoRefreshService` (Interface) — angelegt in `src/Reporter.Core/Interfaces/IAutoRefreshService.cs` (`StartAsync`, `ApplySettingsAsync`, `StopAsync`)
- [x] `AutoRefreshService` (Klasse) — angelegt in `src/Reporter.Core/Services/AutoRefreshService.cs` (`PeriodicTimer` über injiziertem `TimeProvider` mit `TimeProvider.System`-Default, Overlap-Guard via `Interlocked`, Exception-Isolation im Loop per `Debug.WriteLine`, `Math.Clamp(RefreshIntervalMinutes, 1, 1440)`)
- [x] `IAppThemeService` (Interface) — angelegt in `src/Reporter.Core/Interfaces/IAppThemeService.cs` (`ApplyTheme(string?)`)
- [x] `AppThemeService` (Klasse) — angelegt in `src/Reporter/Services/AppThemeService.cs` (`"light"` → `AppTheme.Light`, `"dark"` → `AppTheme.Dark`, sonst `AppTheme.Unspecified`)
- [x] `RefreshIntervalOption` (Datenmodellklasse) — angelegt in `src/Reporter.Core/ViewModels/RefreshIntervalOption.cs` (`Minutes` + `Label`)
- [x] `AutoMarkReadDelayOption` (Datenmodellklasse) — angelegt in `src/Reporter.Core/ViewModels/AutoMarkReadDelayOption.cs` (`Seconds` + `Label`)
- [x] `ThemeOption` (Datenmodellklasse) — angelegt in `src/Reporter.Core/ViewModels/ThemeOption.cs` (`Value` + `Label`)

### Änderungen an bestehenden Klassen

- [x] `Reporter.Data.Entities.Settings` — `AutoRefreshEnabled` (`bool`, `true`), `RefreshIntervalMinutes` (`int`, `30`), `Theme` (`string?`, `"system"`) vorhanden
- [x] `Reporter.Core.Models.Settings` — `AutoRefreshEnabled`/`RefreshIntervalMinutes` als `required init`, `Theme` als optionales `string?` mit `"system"`-Default vorhanden
- [x] `ReporterDbContext.ConfigureSettings` — Spalten `auto_refresh_enabled` (required, Default true), `refresh_interval_minutes` (required, Default 30), `theme` (max. 50, nullable) konfiguriert
- [x] EF-Migration `20260911080630_AddSettingsAutoRefreshAndTheme` — vorhanden inkl. `UpdateData` auf den Singleton-Datensatz; `ReporterDbContextModelSnapshot` enthält die drei Spalten
- [x] `SettingsRepository.SaveAsync`/`MapToModel` — kopieren bzw. mappen alle drei neuen Felder (`src/Reporter.Data/Repositories/SettingsRepository.cs` Zeilen 59–61, 77–79)
- [x] `IItemRepository` — `GetExpiredKeywordCandidatesAsync(DateTime, CancellationToken)` und `DeleteRangeAsync(IReadOnlyList<Guid>, CancellationToken)` deklariert (Zeilen 139, 147)
- [x] `ItemRepository` — beide Methoden implementiert mit `ExecuteDeleteAsync` bzw. `PublishedAt ?? ReadAt`-Kandidatenprädikat (Zeilen 314–336); Konstruktor unverändert
- [x] `RetentionCleanupService` — Konstruktor um `IKeywordRepository` + `IKeywordMatcher` erweitert; `CleanupAsync` führt nach `DeleteExpiredAsync` die Keyword-Regel aus (Keywords laden → `GetExpiredKeywordCandidatesAsync` → `MatchesAny` im Speicher → `DeleteRangeAsync`, Summenrückgabe)
- [x] `SettingsViewModel` — Konstruktor um `IKeywordRepository`/`IAutoRefreshService`/`IAppThemeService` erweitert; alle geplanten Eigenschaften (`Keywords`, `NewKeywordText`, `RetentionDays`, `RetentionDaysText`, `AutoRefreshEnabled`, `RefreshIntervalOptions`/`SelectedRefreshInterval`, `AutoMarkReadEnabled`, `AutoMarkReadDelayOptions`/`SelectedAutoMarkReadDelay`, `NotificationsEnabled`, `QuietHoursStart`/`QuietHoursEnd`, `ThemeOptions`/`SelectedTheme`, `HasError`, `ErrorMessage`) sowie `AddKeywordCommand`, `RemoveKeywordCommand`, `SaveRetentionCommand`, `PersistAsync` mit `_isLoading`-Guard und Seiteneffekten (`ApplyTheme`, `ApplySettingsAsync`) vorhanden
- [x] `ArticleDetailViewModel.LoadAsync` — `AutoMarkReadMode != "off"`-Gate (Zeile 273), Delay-Bedingung `>= 0` (Zeile 252), Fallback-`Settings` um die neuen `required`-Felder ergänzt (Zeilen 239–248)
- [x] `App.OnStart` — Theme-Anwendung (`GetAsync` + `IAppThemeService.ApplyTheme`) und `IAutoRefreshService.StartAsync` jeweils mit eigenem `try/catch` + `Debug.WriteLine` (Zeilen 51–73); bestehender Cleanup-Block unverändert
- [x] `MauiProgram` — `AddSingleton<IKeywordMatcher, KeywordMatcher>()`, `AddSingleton<IAutoRefreshService, AutoRefreshService>()`, `AddSingleton<IAppThemeService, AppThemeService>()` (Zeilen 53–55)
- [x] `SettingsPage.xaml` — vollständiger Ausbau mit fünf `Border`-Karten-Sektionen (`RoundRectangle 12`, `AppThemeBinding SurfaceContainer`) im `ScrollView` unter `Grid RowDefinitions="Auto,*"`: Aufbewahrungsdauer (Slider 1–365 + `DragCompletedCommand` + Skalen-/Info-Labels), Keyword-Filter (`Entry` + `ReturnCommand` + Button, Chips via `FlexLayout Wrap="Wrap"` + `BindableLayout` mit 44×44-pt-×-Button, deaktivierter aktiver Match-`Switch`, Fehler-`Label`), Synchronisation & Lesefluss (`Switch` + `Picker` mit `IsEnabled`/`Opacity`-Dimming), Benachrichtigungen & Ruhezeiten (`Switch` + 2× `TimePicker` VON/BIS mit Dimming), Erscheinungsbild (Theme-`Picker`); `Shell.NavBarIsVisible="False"`, `HeadlineStyle`-Header
- [x] `SettingsPage.xaml.cs` — minimaler Code-Behind (`LoadCommand` in `OnAppearing`)
- [x] `AppResources.resx` / `AppResources.de.resx` / `AppResources.Designer.cs` — alle geplanten Keys in beiden resx-Dateien und im Designer vorhanden (u. a. `SettingsSection*`, `SettingsRetention*`, `SettingsKeyword*`, `SettingsInterval*`, `SettingsDelay*`, `SettingsQuietHours*`, `SettingsTheme*`, `ErrorKeyword*` inkl. `ErrorKeywordTooLong`, `SettingsSaved`)

### Tests

- [x] `KeywordMatcherTests` — neu in `src/Reporter.Tests/KeywordMatcherTests.cs` (7 Tests: Case-Insensitivity, Teilwort, Titel/ContentHtml, null/leer/blank-Keywords)
- [x] `SettingsViewModelTests` — neu aufgeteilt in `SettingsViewModelTests_Load.cs` (`Load_PopulatesAllOptions`, `Load_InvalidPersistedValues_UsesFallbacks`), `SettingsViewModelTests_Persist.cs` (`PropertyChange_PersistsImmediately`, `RetentionDays_OutOfRange_Clamped`, `ThemeChange_AppliesTheme`, `AutoRefreshChange_AppliesSettings`), `SettingsViewModelTests_Keywords.cs` (`AddKeyword_Valid_AddsToRepository`, `AddKeyword_DuplicateCaseInsensitive_ShowsError`, `AddKeyword_Empty_ShowsError`, `AddKeyword_TooLong_ShowsError`, `RemoveKeyword_DeletesFromRepository`)
- [x] `RetentionCleanupServiceTests` — Konstruktor und `SetRetentionDaysAsync` an neue Signaturen/`required`-Felder angepasst; 3 neue Keyword-Tests (`CleanupAsync_DeletesKeywordMatchedExpired`, `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved`, `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt`)
- [x] `ItemRepositoryTests` — 5 neue Tests (`GetExpiredKeywordCandidatesAsync_UsesPublishedAtOverReadAt`, `_KeepsUnreadAndSaved`, `_FallsBackToReadAt`, `DeleteRangeAsync_DeletesOnlyGivenIds`, `DeleteRangeAsync_EmptyList_ReturnsZero`)
- [x] `SettingsRepositoryTests` — `SaveAsync_PersistsNewFields` neu; bestehende `Settings`-Initialisierer um `AutoRefreshEnabled`/`RefreshIntervalMinutes`/`Theme` ergänzt
- [x] `AutoRefreshServiceTests` — neu (6 Methoden/7 Fälle: `StartAsync_InvokesSyncAfterInterval`, `ApplySettings_Disabled_Stops`, `ApplySettings_ChangesInterval`, `SyncThrows_LoopContinues`, `OverlappingTick_SkipsSync`, `InvalidInterval_Clamped` als Theory); NuGet `Microsoft.Extensions.TimeProvider.Testing` 10.1.0 im Testprojekt referenziert
- [x] Hilfsklassen `FakeFeedSyncService`, `FakeAutoRefreshService`, `FakeAppThemeService`, `TestWaitHelper` — vorhanden
- [x] E2E-Flusstests — `SettingsViewModelTests_E2E` mit `E2E_ChangeSettings_PersistRoundtrip`, `E2E_KeywordAddRemove_Persists`, `E2E_KeywordDuplicateAndEmpty_Rejected` vorhanden; die übrigen geplanten E2E-Szenarien sind durch gleichwertige Flow-Tests mit echten Repositories abgedeckt (siehe Hinweise)
- [x] `Run-StaticChecks.ps1` — dokumentiert mit Exit-Code 0 in `test-results.md` (128 Tests bestanden)

## Offene Aufgaben

- [ ] `Manuelle UI-Verifikation` (Tasks-Datei #47) — fehlt vollständig: In `test-results.md` ist dokumentiert, dass die interaktive Verifikation (App-Start auf Windows im 390 × 844-pt-Fenster, Screenshots Light + Dark, Smoke-Tests inkl. Theme-Umschaltung und Delay-0-„Sofort"-Fall) noch aussteht, da in der Umgebung kein GUI-Launch möglich war. Nur der statische XAML-Abgleich ist erfolgt und dokumentiert.

## Hinweise

- **E2E-Testnamen weichen ab:** Die im Plan benannten Tests `E2E_RetentionOutOfRange_Clamped`, `RetentionCleanupServiceTests.E2E_KeywordFilterCleanup` und `AutoRefreshServiceTests.E2E_AutoRefresh_TicksAndStops` existieren nicht unter diesen Namen. Die beschriebenen Flüsse sind jedoch vollständig über echte Repositories bis in die Datenbank abgedeckt: `SettingsViewModelTests_Persist.RetentionDays_OutOfRange_Clamped` (VM → SaveRetentionCommand → Repo → DB), die drei neuen `RetentionCleanupServiceTests`-Keyword-Tests (decken gemeinsam das beschriebene Vier-Varianten-Szenario ab) sowie `AutoRefreshServiceTests.StartAsync_InvokesSyncAfterInterval` / `ApplySettings_Disabled_Stops` / `ApplySettings_ChangesInterval`. Falls die exakten Testnamen gefordert sind, wären nur Umbenennungen/Zusammenfassungen nötig — keine neue Abdeckung.
- **`AppThemeService` und `App.OnStart`-Erweiterungen** liegen im nicht testbaren MAUI-Projekt; ihre Korrektheit hängt an der noch ausstehenden manuellen UI-Verifikation (Task #47).
- **`SettingsViewModel` ist als Singleton registriert**, `SettingsPage` als Transient — entspricht dem bestehenden Schema der anderen ViewModels.
- Teststand laut `test-results.md`: 128 bestanden, 0 fehlgeschlagen (Baseline 90); Static Checks Exit-Code 0.
