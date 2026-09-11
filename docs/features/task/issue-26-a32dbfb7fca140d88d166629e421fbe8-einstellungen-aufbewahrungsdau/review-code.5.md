# Code-Review

## Ergebnis

**Status:** Keine Befunde

## Befunde

Keine. Der Befund der vorherigen Runde (`review-code.4.md`) wurde vollständig umgesetzt:

- Neuer Ressourcen-Key `ArticleAutoMarkReadDelayFormat` in `AppResources.resx` („Auto-read ({0} s)"), `AppResources.de.resx` („Auto-Gelesen ({0} s)") und `AppResources.Designer.cs` angelegt; `{0}`-Platzhalter in beiden Sprachen vorhanden.
- `ArticleDetailViewModel.cs`: Feldinitialisierer (Zeile 43) und `LoadAsync` (Zeilen 266–268) setzen `AutoMarkReadLabel` jetzt über `string.Format(CultureInfo.CurrentCulture, AppResources.ArticleAutoMarkReadDelayFormat, …)`; im deaktivierten Zustand weiterhin `AppResources.ArticleAutoMarkReadDisabled`. Die zuvor gemischte Sprachausgabe desselben UI-Elements ist beseitigt.
- `SettingsRetentionInfo` und `SettingsKeywordMatchLabel` in beiden resx-Dateien konsistent umformuliert; Designer-Kommentare synchron.

## Geprüfte Schwerpunkte dieser Runde (ohne Befund)

- **resx-Konsistenz**: Vollständige Key-Parität DE/EN verifiziert (Diff der `data name`-Listen leer); neuer Key in beiden Dateien und im Designer vorhanden; `string.Format`-Aufrufe korrekt mit `CultureInfo.CurrentCulture`.
- **`ArticleDetailViewModel.LoadAsync`** (Zeilen 262–268): `IsAutoMarkReadAvailable`-Gate vor Label-Auswahl korrekt; Verzögerung `>= 0` zulässig (Option „Sofort" = 0 s → `TimeSpan.FromSeconds(0)` sofortiges Markieren, konsistent mit den Delay-Optionen in `SettingsViewModel`).
- **Build-Verifikation**: `dotnet build src/Reporter.Core/Reporter.Core.csproj` — 0 Warnungen, 0 Fehler.
- **`SettingsViewModel.PersistAsync`** (Zeilen 425–458): Snapshot innerhalb `_persistLock`, Theme-/AutoRefresh-Seiteneffekte nur bei Änderung gegenüber dem zuletzt gespeicherten Stand; `SaveRetention`-Clamp konsistent mit `PersistAsync`-Clamp.
- **`AutoRefreshService`**: `_stateLock` serialisiert Start/Stop/ApplySettings atomar; `StopLoopAsync` awaitet den Loop-Task, `PeriodicTimer` koalesciert verpasste Ticks — keine parallelen Syncs; Exceptions im Sync werden geloggt und stoppen den Loop nicht; `OperationCanceledException`-Filter korrekt.
- **`RetentionCleanupService` / `ItemRepository`**: Keyword-Löschpass läuft nach `DeleteExpiredAsync`; Kandidaten-Filter (`IsRead && !IsSavedForLater`, `PublishedAt ?? ReadAt`) ist bewusst anders als bei `DeleteExpiredAsync` (`ReadAt ?? PublishedAt`) und im Interface-XML-Doc sowie durch Tests dokumentiert; `DeleteRangeAsync` mit Leerlisten-Guard.
- **XAML**: `RemoveKeywordCommand`-Binding via `x:Reference PageRoot` mit `x:DataType="models:Keyword"` korrekt; Disabled-Pattern (`IsEnabled` + Opacity-Trigger 0.4) einheitlich auf allen abhängigen Einstellungsgruppen; Touch-Targets ≥ 44 pt.
- **Testqualität**: Neue Tests prüfen jeweils genau einen fachlichen Fall mit AAA-Struktur (Keyword-Kandidaten-Selektion, PublishedAt-vs-ReadAt-Semantik, Clamp-Verhalten, Concurrency des ApplySettings, QuietHours-Session-Retention); `TestWaitHelper` duplikationsfrei.
- **Zusatz-Prüfregel (UI-Aktions-Events)**: Keine `RaiseUiActionRequested`-artigen Events in der Codebasis vorhanden (grep über `src/` ohne Treffer) — nicht anwendbar.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Reporter.Core/Interfaces/IAppThemeService.cs`
- `src/Reporter.Core/Interfaces/IAutoRefreshService.cs`
- `src/Reporter.Core/Interfaces/IItemRepository.cs`
- `src/Reporter.Core/Interfaces/IKeywordMatcher.cs`
- `src/Reporter.Core/Models/Settings.cs`
- `src/Reporter.Core/Models/SettingsValues.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Services/AutoRefreshService.cs`
- `src/Reporter.Core/Services/KeywordMatcher.cs`
- `src/Reporter.Core/Services/RetentionCleanupService.cs`
- `src/Reporter.Core/ViewModels/AutoMarkReadDelayOption.cs`
- `src/Reporter.Core/ViewModels/RefreshIntervalOption.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Core/ViewModels/ThemeOption.cs`
- `src/Reporter.Data/Entities/Settings.cs`
- `src/Reporter.Data/Migrations/20260911080630_AddSettingsAutoRefreshAndTheme.cs`
- `src/Reporter.Data/Migrations/20260911080630_AddSettingsAutoRefreshAndTheme.Designer.cs`
- `src/Reporter.Data/Migrations/ReporterDbContextModelSnapshot.cs`
- `src/Reporter.Data/ReporterDbContext.cs`
- `src/Reporter.Data/Repositories/ItemRepository.cs`
- `src/Reporter.Data/Repositories/SettingsRepository.cs`
- `src/Reporter/App.xaml.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter/Services/AppThemeService.cs`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter/Views/ArticleDetailPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter.Tests/AutoRefreshServiceTests.cs`
- `src/Reporter.Tests/FakeAppThemeService.cs`
- `src/Reporter.Tests/FakeAutoRefreshService.cs`
- `src/Reporter.Tests/FakeFeedSyncService.cs`
- `src/Reporter.Tests/ItemRepositoryTests.cs`
- `src/Reporter.Tests/KeywordMatcherTests.cs`
- `src/Reporter.Tests/Reporter.Tests.csproj`
- `src/Reporter.Tests/RetentionCleanupServiceTests.cs`
- `src/Reporter.Tests/SettingsRepositoryTests.cs`
- `src/Reporter.Tests/SettingsValuesTests_AutoMarkRead.cs`
- `src/Reporter.Tests/SettingsViewModelTests_E2E.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Keywords.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Load.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Persist.cs`
- `src/Reporter.Tests/TestWaitHelper.cs`
