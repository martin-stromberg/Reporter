# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### AutoRefreshService.cs (AutoRefreshService)

- **Toter Code / Testqualität** — Der `_syncRunning`-Guard (Zeilen 21, 117–120, 134–137) ist unerreichbar: `RunLoopAsync` führt `WaitForNextTickAsync` und `SyncAllAsync` streng sequenziell in einer einzigen Schleife aus; `_syncRunning` wird im `finally` derselben Iteration zurückgesetzt, bevor der nächste Tick ausgewertet wird, und `PeriodicTimer` koalesciert verpasste Ticks ohnehin. Zwei parallele Loops sind durch `_stateLock` + Await in `StopLoopAsync` ausgeschlossen. Der Guard kann daher niemals auslösen. Der Test `OverlappingTick_SkipsSync` (`AutoRefreshServiceTests.cs`, Zeilen 157–174) suggeriert, dieses Verhalten zu prüfen, würde aber auch ohne den Guard grün sein — er verifiziert nur die sequenzielle Abarbeitung des Timers.

  Empfehlung: Entweder den `_syncRunning`-Mechanismus entfernen und den Test entsprechend umbenennen/umdokumentieren (z. B. „kein zweiter Sync bei koalescierten Ticks"), oder den Guard bewusst als defensive Absicherung mit Kommentar behalten und den XML-Kommentar des Tests klarstellen, dass der Überlappungspfad strukturell nicht erreichbar ist.

### AutoRefreshServiceTests.cs (AutoRefreshServiceTests)

- **Testqualität** — `StopAsync_DisposesLoopCancellationTokenSource` (Zeilen 203–216) liest per Reflection das private Feld `_loopCts` und prüft `ObjectDisposedException` am Token. Der Test koppelt sich an den internen Feldnamen und die Implementierungsform statt an fachliches Verhalten; eine Umbenennung des Felds oder ein Wechsel der Cancellation-Strategie bricht den Test, obwohl das Verhalten korrekt bleibt.

  Empfehlung: Die Dispose-Eigenschaft verhaltensnah prüfen (z. B. wiederholte Start/Stop-Zyklen ohne Fehler) oder den Reflection-Zugriff zumindest in eine Hilfsmethode kapseln, damit nur eine Stelle vom Feldnamen abhängt.

### ArticleDetailViewModel.cs / SettingsViewModel.cs / AppThemeService.cs

- **Hardcodierte Werte / Doppelter Code** — Die Settings-Werte `"on_open"` und `"off"` liegen als private Konstanten in `SettingsViewModel` (Zeilen 21–22) vor, werden aber in `ArticleDetailViewModel` als Literale dupliziert (Zeilen 243, 273, 392). Ebenso wird `"system"` in `SettingsViewModel` Zeile 84 als Literal verwendet, obwohl die Konstante `ThemeSystem` (Zeile 23) existiert, und `"light"`/`"dark"` sind als Literale in `AppThemeService` (Zeilen 20–21) wiederholt. Bei einer Umbenennung eines Mode-Werts laufen die Stellen auseinander.

  Empfehlung: Zentrale Konstanten einführen (z. B. `public const` auf `Reporter.Core.Models.Settings` oder eine `SettingsValues`-Klasse in `Reporter.Core`) und alle Literale — inklusive `SettingsViewModel` Zeile 84 — darauf umstellen.

### TestWaitHelper.cs (TestWaitHelper)

- **Doppelter Code** — Die beiden `WaitUntilAsync`-Overloads (Zeilen 14–28 und 36–50) duplizieren die komplette Polling-Schleife (Deadline-Berechnung, `while`-Schleife, `Task.Delay(10)`, abschließendes `Assert.True`).

  Empfehlung: Der synchrone Overload kann an den asynchronen delegieren (`WaitUntilAsync(() => Task.FromResult(condition()), timeoutMilliseconds)`), sodass die Schleife nur einmal existiert.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Reporter.Core/Interfaces/IAppThemeService.cs`
- `src/Reporter.Core/Interfaces/IAutoRefreshService.cs`
- `src/Reporter.Core/Interfaces/IItemRepository.cs`
- `src/Reporter.Core/Interfaces/IKeywordMatcher.cs`
- `src/Reporter.Core/Models/Settings.cs`
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
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml.cs`
- `src/Reporter.Tests/AutoRefreshServiceTests.cs`
- `src/Reporter.Tests/FakeAppThemeService.cs`
- `src/Reporter.Tests/FakeAutoRefreshService.cs`
- `src/Reporter.Tests/FakeFeedSyncService.cs`
- `src/Reporter.Tests/ItemRepositoryTests.cs`
- `src/Reporter.Tests/KeywordMatcherTests.cs`
- `src/Reporter.Tests/Reporter.Tests.csproj`
- `src/Reporter.Tests/RetentionCleanupServiceTests.cs`
- `src/Reporter.Tests/SettingsRepositoryTests.cs`
- `src/Reporter.Tests/SettingsViewModelTests_E2E.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Keywords.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Load.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Persist.cs`
- `src/Reporter.Tests/TestWaitHelper.cs`
