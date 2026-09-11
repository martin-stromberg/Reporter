# Tasks: Einstellungen, Aufbewahrungsdauer, Keyword-Filter und Löschlogik (Issue #26)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `Settings`-Entity um `AutoRefreshEnabled` (bool, `true`), `RefreshIntervalMinutes` (int, `30`), `Theme` (string?, `"system"`) erweitern | Offen | — |
| 2 | Datenmodell | `Settings` Core-Modell um dieselben Felder erweitern (`required init` für Skalare, optional `string?` für `Theme`) | Offen | — |
| 3 | Datenmodell | `ReporterDbContext.ConfigureSettings` um `auto_refresh_enabled`, `refresh_interval_minutes`, `theme` (max. 50) erweitern | Offen | — |
| 4 | Datenmodell | EF-Migration `AddSettingsAutoRefreshAndTheme` via `dotnet ef migrations add` erzeugen und Snapshot prüfen | Offen | — |
| 5 | Logik | `IKeywordMatcher`-Interface in `Reporter.Core/Interfaces/` anlegen (`MatchesAny(title, contentHtml, keywords)`) | Offen | — |
| 6 | Logik | `KeywordMatcher` in `Reporter.Core/Services/` implementieren (`Contains`, `OrdinalIgnoreCase`, Titel + ContentHtml) | Offen | — |
| 7 | Logik | `IItemRepository` um `GetExpiredKeywordCandidatesAsync(cutoff, ct)` und `DeleteRangeAsync(ids, ct)` erweitern | Offen | — |
| 8 | Logik | `ItemRepository`: beide neuen Methoden implementieren (Ctor unverändert) | Offen | — |
| 9 | Logik | `SettingsRepository.SaveAsync`/`MapToModel` um die drei neuen Felder erweitern | Offen | — |
| 10 | Logik | `RetentionCleanupService`: Ctor um `IKeywordRepository`+`IKeywordMatcher` erweitern, `CleanupAsync` um Keyword-Löschregel ergänzen (Kandidaten `IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff`) | Offen | — |
| 11 | Logik | `IAutoRefreshService`-Interface anlegen (`StartAsync`, `ApplySettingsAsync`, `StopAsync`) | Offen | — |
| 12 | Logik | `AutoRefreshService` implementieren (`PeriodicTimer` via `TimeProvider`, Overlap-Guard, Fehlerisolierung im Loop, `Math.Clamp` Intervall 1–1440) | Offen | — |
| 13 | Logik | `IAppThemeService`-Interface in Core anlegen (`ApplyTheme(string?)`) | Offen | — |
| 14 | Logik | `AppThemeService` in `src/Reporter/Services/` implementieren (`UserAppTheme`-Mapping `"light"`/`"dark"`/`system`) | Offen | — |
| 15 | Logik | `ArticleDetailViewModel.LoadAsync`: Timer nur bei `AutoMarkReadMode != "off"`, Delay-Bedingung `>= 0`, Fallback-`Settings` um neue `required`-Felder ergänzen | Offen | — |
| 16 | Konfiguration | `AppResources.resx` + `AppResources.de.resx`: alle neuen Settings-Keys (Sektionen, Labels, Hints, Interval-/Delay-/Theme-Optionen, Keyword-Fehler inkl. `ErrorKeywordTooLong`) ergänzen | Offen | — |
| 17 | UI | Optionsklassen `RefreshIntervalOption`, `AutoMarkReadDelayOption`, `ThemeOption` in `Reporter.Core/ViewModels/` anlegen | Offen | — |
| 18 | UI | `SettingsViewModel`: Ctor um `IKeywordRepository`/`IAutoRefreshService`/`IAppThemeService` erweitern; alle bindbaren Eigenschaften + `Keywords`-Collection anlegen | Offen | — |
| 19 | UI | `SettingsViewModel`: `LoadAsync` mit `_isLoading`-Guard (Keywords + alle Optionen befüllen) | Offen | — |
| 20 | UI | `SettingsViewModel`: `PersistAsync` (Settings-Kopie → `SaveAsync` → `ApplyTheme`/`ApplySettingsAsync`) an alle Setter anbinden | Offen | — |
| 21 | UI | `SettingsViewModel`: `AddKeywordCommand` mit Trim/Leer-/Dubletten-Validierung (`OrdinalIgnoreCase`, max 500) und `HasError`/`ErrorMessage` | Offen | — |
| 22 | UI | `SettingsViewModel`: `RemoveKeywordCommand` (`DeleteAsync` + Collection-Update) und `SaveRetentionCommand` (Slider-Commit, Clamp 1–365) | Offen | — |
| 23 | UI | `SettingsPage.xaml`: Sektion „Aufbewahrungsdauer" (Slider 1–365, Wert-/Skalen-Labels, Invarianten-Hinweis) als `Border`-Karte | Offen | — |
| 24 | UI | `SettingsPage.xaml`: Sektion „Keyword-Filter" (Entry + Hinzufügen-Button, Chips via `FlexLayout`/`BindableLayout` mit 44-pt-×-Button, deaktivierter Match-Toggle, Fehler-Label) | Offen | — |
| 25 | UI | `SettingsPage.xaml`: Sektion „Synchronisation & Lesefluss" (`Switch` Auto-Refresh + Intervall-`Picker` mit Dimming, `Switch` Auto-Gelesen + Verzögerungs-`Picker` mit Dimming) | Offen | — |
| 26 | UI | `SettingsPage.xaml`: Sektion „Benachrichtigungen & Ruhezeiten" (`Switch` + zwei `TimePicker` VON/BIS) | Offen | — |
| 27 | UI | `SettingsPage.xaml`: Sektion „Erscheinungsbild" (Theme-`Picker` mit Klartext-Optionen System/Hell/Dunkel) | Offen | — |
| 28 | UI | `SettingsPage`: Abgleich mit `design-draft/.../einstellungen_filter/screen.png` (Light+Dark), `AppThemeBinding` überall, 44-pt-Touch-Ziele, `Grid Auto,*` | Offen | — |
| 29 | Konfiguration | `MauiProgram`: `IKeywordMatcher`, `IAutoRefreshService`, `IAppThemeService` als Singletons registrieren | Offen | — |
| 30 | Konfiguration | `App.OnStart`: Theme anwenden (`GetAsync` + `ApplyTheme`) und `IAutoRefreshService.StartAsync` starten — jeweils fehlerisoliert | Offen | — |
| 31 | Tests | `KeywordMatcherTests` neu: Case-Insensitivity, Teilwort, Titel vs. ContentHtml, null/leer | Offen | — |
| 32 | Tests | `SettingsViewModelTests` neu: Laden, Sofort-Persistierung, Retention-Clamp, Theme-/AutoRefresh-Side-Effects (Fakes), Keyword Add/Remove/Duplikat/Leer | Offen | — |
| 33 | Tests | `RetentionCleanupServiceTests`: neue Keyword-Tests + Ctor/`SetRetentionDaysAsync` an neue Signaturen/`required`-Felder anpassen | Offen | — |
| 34 | Tests | `ItemRepositoryTests`: `GetExpiredKeywordCandidatesAsync`-Prädikat + `DeleteRangeAsync` | Offen | — |
| 35 | Tests | `SettingsRepositoryTests`: Persistenz der neuen Felder + bestehende `Settings`-Initialisierer ergänzen | Offen | — |
| 36 | Tests | `AutoRefreshServiceTests` neu (Voraussetzung: NuGet `Microsoft.Extensions.TimeProvider.Testing` + `FakeFeedSyncService`/`FakeAutoRefreshService`/`FakeAppThemeService`-Hilfsklassen) | Offen | — |
| 37 | Tests | `SettingsViewModelTests.AddKeyword_TooLong_ShowsError`: `NewKeywordText` > 500 Zeichen → `HasError`, `ErrorMessage` (`ErrorKeywordTooLong`), kein `AddAsync` | Offen | — |
| 38 | Tests | `SettingsViewModelTests.Load_InvalidPersistedValues_UsesFallbacks`: ungültige persistierte Werte → Intervall-Fallback 30, Delay-Fallback 5, Theme-Fallback `"system"` | Offen | — |
| 39 | Tests | `AutoRefreshServiceTests.SyncThrows_LoopContinues`: Exception in `SyncAllAsync` beendet Timer-Loop nicht (Exception-Isolation im Loop) | Offen | — |
| 40 | Tests | `AutoRefreshServiceTests.OverlappingTick_SkipsSync`: Overlap-Guard überspringt Ticks bei laufendem Sync | Offen | — |
| 41 | Tests | `AutoRefreshServiceTests.InvalidInterval_Clamped`: `Math.Clamp(RefreshIntervalMinutes, 1, 1440)` bei ungültigem persistiertem Wert | Offen | — |
| 42 | E2E-Tests | E2E-Flusstest „Einstellungen ändern → persistiert → Reload" (`SettingsViewModelTests.E2E_ChangeSettings_PersistRoundtrip`) | Offen | — |
| 43 | E2E-Tests | E2E-Flusstest „Keyword hinzufügen/entfernen" + Fehlerfälle Duplikat/leer (`E2E_KeywordAddRemove_Persists`, `E2E_KeywordDuplicateAndEmpty_Rejected`) | Offen | — |
| 44 | E2E-Tests | E2E-Flusstest „Retention außerhalb 1–365 geclamppt" (`E2E_RetentionOutOfRange_Clamped`) | Offen | — |
| 45 | E2E-Tests | E2E-Flusstest „Keyword-Löschung mit Invarianten" (`RetentionCleanupServiceTests.E2E_KeywordFilterCleanup`) | Offen | — |
| 46 | E2E-Tests | E2E-Flusstest „Auto-Refresh tickt und stoppt" (`AutoRefreshServiceTests.E2E_AutoRefresh_TicksAndStops` mit `FakeTimeProvider`) | Offen | — |
| 47 | Validierung | Manuelle UI-Verifikation 390 × 844 pt Light + Dark gegen `screen.png`; Screenshot + Größen in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md` dokumentieren (inkl. Theme-Umschaltung + Auto-Gelesen-Smoke-Test mit Delay-0-Fall „Sofort") | Offen | — |
| 48 | Validierung | `.\scripts\Run-StaticChecks.ps1` mit Exit-Code 0 ausführen | Offen | — |
