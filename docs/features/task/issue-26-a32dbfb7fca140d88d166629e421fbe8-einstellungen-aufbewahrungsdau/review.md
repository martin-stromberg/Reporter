# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Neue Klassen / Interfaces

- [x] `IKeywordMatcher` (Interface) — angelegt in `src/Reporter.Core/Interfaces/IKeywordMatcher.cs` (`MatchesAny(string? title, string? contentHtml, IEnumerable<string> keywords)`)
- [x] `KeywordMatcher` (Klasse) — angelegt in `src/Reporter.Core/Services/KeywordMatcher.cs` (`Contains` + `StringComparison.OrdinalIgnoreCase` auf `Title` und `ContentHtml`, Blank-Keywords übersprungen)
- [x] `IAutoRefreshService` (Interface) — angelegt in `src/Reporter.Core/Interfaces/IAutoRefreshService.cs` (`StartAsync`, `ApplySettingsAsync`, `StopAsync`)
- [x] `AutoRefreshService` (Klasse) — angelegt in `src/Reporter.Core/Services/AutoRefreshService.cs` (`PeriodicTimer` über injiziertem `TimeProvider` mit `TimeProvider.System`-Default, Overlap-Guard via `Interlocked`, Exception-Isolation im Loop per `Debug.WriteLine`, `Math.Clamp(RefreshIntervalMinutes, 1, 1440)`; Iteration 2: `SemaphoreSlim`-State-Lock, CTS-Dispose in `StopLoopAsync`)
- [x] `IAppThemeService` (Interface) — angelegt in `src/Reporter.Core/Interfaces/IAppThemeService.cs` (`ApplyTheme(string? theme)`)
- [x] `AppThemeService` (Klasse) — angelegt in `src/Reporter/Services/AppThemeService.cs` (`"light"` → `AppTheme.Light`, `"dark"` → `AppTheme.Dark`, sonst `AppTheme.Unspecified`)
- [x] `RefreshIntervalOption` (Datenmodellklasse) — angelegt in `src/Reporter.Core/ViewModels/RefreshIntervalOption.cs` (`Minutes` + `Label`)
- [x] `AutoMarkReadDelayOption` (Datenmodellklasse) — angelegt in `src/Reporter.Core/ViewModels/AutoMarkReadDelayOption.cs` (`Seconds` + `Label`)
- [x] `ThemeOption` (Datenmodellklasse) — angelegt in `src/Reporter.Core/ViewModels/ThemeOption.cs` (`Value` + `Label`)

### Änderungen an bestehenden Klassen

- [x] `Reporter.Data.Entities.Settings` — `AutoRefreshEnabled` (`bool`, `true`), `RefreshIntervalMinutes` (`int`, `30`), `Theme` (`string?`, `"system"`) vorhanden (Zeilen 51, 56, 61)
- [x] `Reporter.Core.Models.Settings` — `AutoRefreshEnabled`/`RefreshIntervalMinutes` als `required init`, `Theme` als optionales `string?` mit `"system"`-Default vorhanden (Zeilen 51, 56, 61)
- [x] `ReporterDbContext.ConfigureSettings` — Spalten `auto_refresh_enabled` (required, Default true), `refresh_interval_minutes` (required, Default 30), `theme` (max. 50, nullable) konfiguriert (Zeilen 131–133)
- [x] EF-Migration `20260911080630_AddSettingsAutoRefreshAndTheme` — vorhanden inkl. `UpdateData` auf den Singleton-Datensatz; `ReporterDbContextModelSnapshot` enthält die drei Spalten
- [x] `SettingsRepository.SaveAsync`/`MapToModel` — kopieren bzw. mappen alle drei neuen Felder (`src/Reporter.Data/Repositories/SettingsRepository.cs` Zeilen 59–61, 77–79)
- [x] `IItemRepository` — `GetExpiredKeywordCandidatesAsync(DateTime, CancellationToken)` und `DeleteRangeAsync(IReadOnlyList<Guid>, CancellationToken)` deklariert (Zeilen 139, 147)
- [x] `ItemRepository` — beide Methoden implementiert (`GetExpiredKeywordCandidatesAsync` mit `IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff` Zeilen 314–322; `DeleteRangeAsync` per `ExecuteDeleteAsync` auf IDs Zeilen 325–336); Konstruktor unverändert (nur `IDbContextFactory<ReporterDbContext>`)
- [x] `RetentionCleanupService` — Konstruktor um `IKeywordRepository` + `IKeywordMatcher` erweitert; `CleanupAsync` führt nach `DeleteExpiredAsync` die Keyword-Regel aus (`RetentionDays <= 0` → 0, Keywords laden → `GetExpiredKeywordCandidatesAsync` → `MatchesAny` im Speicher → `DeleteRangeAsync`, Summenrückgabe)
- [x] `SettingsViewModel` — Konstruktor um `IKeywordRepository`/`IAutoRefreshService`/`IAppThemeService` erweitert; alle geplanten Eigenschaften (`Keywords`, `NewKeywordText`, `RetentionDays`, `RetentionDaysText`, `AutoRefreshEnabled`, `RefreshIntervalOptions`/`SelectedRefreshInterval`, `AutoMarkReadEnabled`, `AutoMarkReadDelayOptions`/`SelectedAutoMarkReadDelay`, `NotificationsEnabled`, `QuietHoursStart`/`QuietHoursEnd`, `ThemeOptions`/`SelectedTheme`, `HasError`, `ErrorMessage`) sowie `AddKeywordCommand`, `RemoveKeywordCommand`, `SaveRetentionCommand`, `PersistAsync` mit `_isLoading`-Guard, Seiteneffekten (`ApplyTheme`, `ApplySettingsAsync`) und Validierungen (leer / > 500 Zeichen / `OrdinalIgnoreCase`-Dublett, Retention-Clamp 1–365) vorhanden; Iteration 2: `PersistAsync` läuft unter `_persistLock`-`SemaphoreSlim`
- [x] `ArticleDetailViewModel.LoadAsync` — `AutoMarkReadMode != "off"`-Gate (Zeile 273), Delay-Bedingung `>= 0` (Zeile 252), Fallback-`Settings` um die neuen `required`-Felder ergänzt (Zeilen 239–248)
- [x] `App.OnStart` — Theme-Anwendung (`GetAsync` + `IAppThemeService.ApplyTheme`, Zeilen 51–62) und `IAutoRefreshService.StartAsync` (Zeilen 64–73) jeweils mit eigenem `try/catch` + `Debug.WriteLine`; bestehender Cleanup-Block unverändert
- [x] `MauiProgram` — `AddSingleton<IKeywordMatcher, KeywordMatcher>()`, `AddSingleton<IAutoRefreshService, AutoRefreshService>()`, `AddSingleton<IAppThemeService, AppThemeService>()` (Zeilen 53–55)
- [x] `SettingsPage.xaml` — vollständiger Ausbau mit fünf `Border`-Karten-Sektionen (`RoundRectangle 12`, `AppThemeBinding SurfaceContainer`) im `ScrollView` unter `Grid RowDefinitions="Auto,*"`: Aufbewahrungsdauer (Slider 1–365 + `DragCompletedCommand` + Skalen-/Info-Labels), Keyword-Filter (`Entry` + `ReturnCommand` + Button, Chips via `FlexLayout Wrap="Wrap"` + `BindableLayout` mit 44×44-pt-×-Button, deaktivierter aktiver Match-`Switch`, Fehler-`Label` mit `HasError`/`LightError`/`DarkError`), Synchronisation & Lesefluss (`Switch` + `Picker` mit `IsEnabled`/`Opacity`-Dimming), Benachrichtigungen & Ruhezeiten (`Switch` + 2× `TimePicker` VON/BIS mit Dimming), Erscheinungsbild (Theme-`Picker`); `Shell.NavBarIsVisible="False"`, `HeadlineStyle`-Header, keine `CollectionView`-Verschachtelung
- [x] `SettingsPage.xaml.cs` — minimaler Code-Behind (`LoadCommand` in `OnAppearing`)
- [x] `AppResources.resx` / `AppResources.de.resx` / `AppResources.Designer.cs` — alle 40 geplanten Keys in beiden resx-Dateien und im Designer vorhanden (u. a. `SettingsSection*`, `SettingsRetention*`, `SettingsKeyword*`, `SettingsInterval*`, `SettingsDelay*`, `SettingsQuietHours*`, `SettingsTheme*`, `ErrorKeyword*` inkl. `ErrorKeywordTooLong`, `SettingsSaved`)

### Tests

- [x] `KeywordMatcherTests` — in `src/Reporter.Tests/KeywordMatcherTests.cs` (7 Tests: `MatchesAny_TitleCaseInsensitive`, `MatchesAny_ContentHtml`, `MatchesAny_Substring`, `MatchesAny_NoMatch`, `MatchesAny_NullTitleAndContent_ReturnsFalse`, `MatchesAny_EmptyKeywords_ReturnsFalse`, `MatchesAny_BlankKeywords_Skipped`)
- [x] `SettingsViewModelTests_Load` — `Load_PopulatesAllOptions`, `Load_InvalidPersistedValues_UsesFallbacks` (Intervall-Fallback 30, Delay-Fallback 5, Theme-Fallback `"system"`)
- [x] `SettingsViewModelTests_Persist` — `PropertyChange_PersistsImmediately`, `RetentionDays_OutOfRange_Clamped` (Theory 0→1, 400→365), `ThemeChange_AppliesTheme`, `AutoRefreshChange_AppliesSettings`; Iteration 2 ergänzt: `Persist_QueuedBehindRunningSave_AppliesThemeOnce`
- [x] `SettingsViewModelTests_Keywords` — `AddKeyword_Valid_AddsToRepository`, `AddKeyword_DuplicateCaseInsensitive_ShowsError`, `AddKeyword_Empty_ShowsError`, `AddKeyword_TooLong_ShowsError`, `RemoveKeyword_DeletesFromRepository`
- [x] `RetentionCleanupServiceTests` — Konstruktor (`IKeywordRepository`, `IKeywordMatcher`) und `SetRetentionDaysAsync` an neue Signaturen/`required`-Felder angepasst; 3 neue Keyword-Tests (`CleanupAsync_DeletesKeywordMatchedExpired`, `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved`, `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt`)
- [x] `ItemRepositoryTests` — 5 neue Tests (`GetExpiredKeywordCandidatesAsync_UsesPublishedAtOverReadAt`, `_KeepsUnreadAndSaved`, `_FallsBackToReadAt`, `DeleteRangeAsync_DeletesOnlyGivenIds`, `DeleteRangeAsync_EmptyList_ReturnsZero`)
- [x] `SettingsRepositoryTests` — `SaveAsync_PersistsNewFields` neu; bestehende `Settings`-Initialisierer um `AutoRefreshEnabled`/`RefreshIntervalMinutes`/`Theme` ergänzt
- [x] `AutoRefreshServiceTests` — 8 Methoden/9 Fälle mit `FakeTimeProvider` (`StartAsync_InvokesSyncAfterInterval`, `ApplySettings_Disabled_Stops`, `ApplySettings_ChangesInterval`, `SyncThrows_LoopContinues`, `OverlappingTick_SkipsSync`, `InvalidInterval_Clamped` als Theory; Iteration 2 ergänzt: `StopAsync_DisposesLoopCancellationTokenSource`, `ApplySettings_Concurrent_LeavesSingleActiveLoop`); NuGet `Microsoft.Extensions.TimeProvider.Testing` 10.1.0 im Testprojekt referenziert
- [x] Hilfsklassen `FakeFeedSyncService`, `FakeAutoRefreshService`, `FakeAppThemeService`, `TestWaitHelper` — vorhanden
- [x] E2E-Flusstests — `SettingsViewModelTests_E2E` mit `E2E_ChangeSettings_PersistRoundtrip`, `E2E_KeywordAddRemove_Persists`, `E2E_KeywordEmpty_Rejected`, `E2E_KeywordDuplicate_Rejected` (über echte Repositories bis in die SQLite-In-Memory-DB); die übrigen geplanten E2E-Szenarien sind durch gleichwertige Flow-Tests abgedeckt (siehe Hinweise)

### Validierung

- [x] Manuelle UI-Verifikation (Tasks-Datei #47) — durchgeführt und dokumentiert: App auf Windows im 390 × 844-pt-Fenster gestartet, 7 Screenshots unter `test-results/issue-26-manual-*.png` im Repo-Root; Theme-Umschaltung (Picker „Dunkel"/„Hell" → `settings.theme` in DB + sichtbares Rendering) und Delay-0-„Sofort"-Smoke-Test (`auto_mark_read_delay_seconds=0` persistiert, Artikel beim Öffnen sofort als gelesen markiert) live verifiziert; dokumentiert in Root-`test-results.md`, Abschnitt „Manuelle UI-Verifikation (Iteration 2, durchgeführt)"
- [x] `Run-StaticChecks.ps1` — dokumentiert mit Exit-Code 0 in `test-results.md` (Iteration 2: 132 Tests bestanden, 0 fehlgeschlagen)

## Hinweise

- **E2E-Testnamen weichen vom Plan ab (unverändert zu Review 1):** Die im Plan benannten Tests `E2E_RetentionOutOfRange_Clamped`, `RetentionCleanupServiceTests.E2E_KeywordFilterCleanup` und `AutoRefreshServiceTests.E2E_AutoRefresh_TicksAndStops` existieren nicht unter diesen Namen. Die beschriebenen Flüsse sind vollständig über echte Repositories bis in die Datenbank abgedeckt: `SettingsViewModelTests_Persist.RetentionDays_OutOfRange_Clamped`, die drei neuen `RetentionCleanupServiceTests`-Keyword-Tests sowie `AutoRefreshServiceTests.StartAsync_InvokesSyncAfterInterval`/`ApplySettings_Disabled_Stops`/`ApplySettings_ChangesInterval`. Neu in Iteration 2: Der kombinierte `E2E_KeywordDuplicateAndEmpty_Rejected` wurde in `E2E_KeywordEmpty_Rejected` + `E2E_KeywordDuplicate_Rejected` aufgeteilt — beide Fehlerfälle bleiben abgedeckt; der Testnachweis in der Tasks-Datei (#43) wurde entsprechend korrigiert.
- **`AppThemeService` und `App.OnStart`-Erweiterungen** liegen im nicht testbaren MAUI-Projekt; ihre Korrektheit ist durch die inzwischen durchgeführte manuelle UI-Verifikation (Theme-Umschaltung live geprüft, DB-Nachweis) abgedeckt.
- **Teststand laut `test-results.md` (Iteration 2):** 132 bestanden, 0 fehlgeschlagen; Build ohne Warnungen/Fehler; Static Checks Exit-Code 0.
