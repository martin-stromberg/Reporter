# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### AutoRefreshService.cs (AutoRefreshService)

- **Fehlerbehandlung / Ressourcenmanagement** — `ApplySettingsAsync` (Zeilen 44–60) erzeugt pro Aufruf ein neues `CancellationTokenSource` (Zeile 54). `StopAsync` (Zeilen 63–85) ruft `Cancel()` und setzt `_loopCts` auf `null`, das `CancellationTokenSource` wird aber nie disposed. Bei jeder Änderung der Auto-Refresh-Einstellungen sowie bei jedem `StartAsync` verbleibt ein undisposed CTS samt seiner Timer-Callback-Registrierung (`WaitForNextTickAsync` registriert am Token).

  Empfehlung: In `StopAsync` nach dem erfolgreichen Await des Loop-Tasks `cts.Dispose()` aufrufen (das gecancelte CTS vor dem Nullen des Felds in einer lokalen Variable merken und nach `await loopTask` freigeben).

- **Fehlerbehandlung / Nebenläufigkeit** — `ApplySettingsAsync` ist nicht nebenläufigkeitssicher: `await StopAsync()` (Zeile 46) und das Setzen von `_loopCts`/`_loopTask` (Zeilen 55–59) liegen in getrennten kritischen Abschnitten. Zwei parallele `ApplySettingsAsync`-Aufrufe können beide `RunLoopAsync` starten; der zuerst registrierte Loop wird danach vom zweiten überschrieben, ist über `_loopCts`/`_loopTask` nicht mehr erreichbar und tickt unkontrolliert bis zum Prozessende weiter (der `_syncRunning`-Guard verhindert zwar parallele Syncs, der verwaiste Timer bleibt aber aktiv). Aktuell nur durch Aufrufer-Disziplin (Semaphore in `SettingsViewModel`, einmaliger `OnStart`-Aufruf) abgesichert — das Interface selbst garantiert die Sicherheit nicht.

  Empfehlung: `ApplySettingsAsync` und `StopAsync` mit einem `SemaphoreSlim` (oder einem einzigen Lock über die gesamte Stop/Start-Sequenz inklusive des Awaits) atomar machen, damit Stop+Restart nicht überlappen kann.

### SettingsViewModel.cs (SettingsViewModel)

- **Fehlerbehandlung / Nebenläufigkeit** — In `PersistAsync` (Zeilen 385–426) werden der `updated`-Snapshot und `previous = _settings` vor `_persistLock.WaitAsync()` gebaut. Dadurch ist `previous` bei schnell aufeinanderfolgenden Änderungen möglicherweise veraltet (der vorherige Save hat `Settings` noch nicht aktualisiert), sodass die Vergleiche in Zeilen 408 und 413 `ApplyTheme`/`ApplySettingsAsync` redundant auslösen (z. B. Theme doppelt anwenden, Refresh-Timer unnötig neu starten). Zudem hängt die Persistierreihenfolge implizit an der FIFO-Warteschlange des `SemaphoreSlim` — bei nicht garantiert FIFO-Reihenfolge könnte ein älterer Snapshot einen neueren überschreiben.

  Empfehlung: Snapshot-Erstellung und das Lesen von `previous`/`_settings` in den Semaphore-Block verlegen, damit jeder Persist-Vorgang den zum Speicherzeitpunkt aktuellen Zustand serialisiert und Seiteneffekt-Vergleiche gegen den tatsächlich zuletzt gespeicherten Stand laufen.

### AppResources.resx / AppResources.de.resx / AppResources.Designer.cs

- **Toter Code** — Die Ressource `SettingsSaved` („Changes saved" / „Änderungen gespeichert", `AppResources.Designer.cs` Zeile ~840) wurde in beiden resx-Dateien und im Designer angelegt, wird aber in keiner Quelldatei oder XAML-Datei referenziert.

  Empfehlung: Eintrag in beiden resx-Dateien und die Designer-Property entfernen — oder die Stelle ergänzen, an der die Bestätigung angezeigt werden soll.

### SettingsViewModelTests_E2E.cs (SettingsViewModelTests_E2E)

- **Testqualität** — `E2E_KeywordDuplicateAndEmpty_Rejected` (Zeilen 121–139) prüft zwei getrennte fachliche Fälle in einer Testmethode: Ablehnung eines leeren Keywords und Ablehnung eines Duplikats. Beide Fälle sind in `SettingsViewModelTests_Keywords` bereits isoliert abgedeckt (`AddKeyword_Empty_ShowsError`, `AddKeyword_DuplicateCaseInsensitive_ShowsError`).

  Empfehlung: Den E2E-Test auf einen der beiden Fälle beschränken oder in zwei Testmethoden aufteilen.

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
