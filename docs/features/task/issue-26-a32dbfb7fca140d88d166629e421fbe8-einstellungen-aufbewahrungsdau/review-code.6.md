# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### `SettingsViewModel.cs` (SettingsViewModel)

- **Fehlerbehandlung / Ressourcen-Lebenszyklus** — Auf dem Erfolgspfad des Debounce wird das `CancellationTokenSource` nie disposed: `PersistRetentionDebouncedAsync` (Zeilen 447–464) kehrt nach `await PersistAsync()` zurück, ohne `cts.Dispose()` aufzurufen oder `_retentionDebounceCts` zu nullen. Das Feld der Singleton-ViewModel (`MauiProgram.cs` Zeile 60: `AddSingleton<SettingsViewModel>`) zeigt danach dauerhaft auf ein abgelaufenes CTS, bis die nächste `ScheduleRetentionPersist`-/`SaveRetention`-Ausführung es ersetzt. Inkonsistent zum etablierten Muster in `ArticleDetailViewModel.MarkReadDelayedAsync` (Zeilen 446–457), das im `finally` das Feld nullt und disposed. Schweregrad: niedrig.

  Empfehlung: In `PersistRetentionDebouncedAsync` einen `finally`-Block ergänzen, der `_retentionDebounceCts` per `ReferenceEquals`-Vergleich nullt und `cts` disposed. Achtung: Da `CancelRetentionDebounce` das CTS ebenfalls cancelt/disposed, muss die Feld-Nullung vor dem Dispose erfolgen bzw. `CancelRetentionDebounce` muss ein `Cancel()` auf bereits disposed Quelle absichern (z. B. Sperre wie `_autoMarkLock` in `ArticleDetailViewModel` oder `try/catch (ObjectDisposedException)`), sonst besteht ein Race: `cts.Cancel()` auf disposed CTS wirft `ObjectDisposedException`.

- **Inkonsistente Konvertierung desselben Werts** — `FormatRetentionDays` (Zeilen 363–366) bildet den Slider-Wert per `(int)days` ab (Abschneiden), während `SaveRetention` (Zeile 421) und `PersistAsync` (Zeile 475) `Math.Round` verwenden. Da der Slider in `SettingsPage.xaml` kein `StepFrequency` setzt, liefert er Gleitkommawerte: Bei 42,9 zeigt die UI „42 Tage", gespeichert wird 43. Auf dem Debounce-Pfad (Änderung ohne `DragCompleted`, genau der Neubefund dieser Runde) wird `_retentionDays` nicht auf den gerundeten Wert zurückgesetzt — Anzeige und Persistenzstand bleiben bis zum nächsten Laden abweichend. Schweregrad: niedrig.

  Empfehlung: `FormatRetentionDays` auf `(int)Math.Round(days)` umstellen, damit der angezeigte Text dem tatsächlich persistierten Wert entspricht (identische Rundung an allen drei Stellen).

### `SettingsViewModelTests_Persist.cs` (SettingsViewModelTests_Persist)

- **Testqualität** — `RetentionDays_RapidChanges_PersistOnlyLastValue` (Zeilen 112–127) verifiziert „nur letzter Wert wird persistiert" nicht robust: `WaitUntilAsync(() => recordingRepository.SavedRetentionDays.Count == 1)` kehrt zurück, sobald der erste Save sichtbar ist; ein fehlerhafter zweiter Save (z. B. bei gebrochenem `CancelRetentionDebounce` würden beim `Advance` alle drei Debounce-Tasks feuern und seriell über `_persistLock` laufen) könnte nach der Assertion einlaufen und bliebe unentdeckt. Der Schwestertest `RetentionDays_DragCompleted_PersistsImmediatelyAndCancelsDebounce` (Zeilen 135–151) macht es korrekt: zusätzliches `_timeProvider.Advance` + Real-Delay vor `Assert.Single`. Schweregrad: niedrig.

  Empfehlung: Nach dem ersten `WaitUntilAsync` zusätzlich `_timeProvider.Advance(...)` bzw. kurze Real-Zeit-Warte einbauen und anschließend `Assert.Single(recordingRepository.SavedRetentionDays)` prüfen, analog zum DragCompleted-Test.

## Geprüfte Schwerpunkte dieser Runde (ohne Befund)

- **Debounce-Korrektheit im Detail** (`SettingsViewModel.cs`, Zeilen 419–464): `CancelRetentionDebounce` nullt das Feld vor `Cancel()`/`Dispose()`; `cts.Token` wird im synchronen Teil von `PersistRetentionDebouncedAsync` ausgewertet, bevor ein Cancel auf demselben (UI-)Thread interleaven kann; `cts.IsCancellationRequested` auf disposed CTS wirft nicht (liest nur den internen State) — Zugriff nach Dispose in Zeile 458 ist sicher. Race Debounce vs. `_persistLock`: Ein bereits die Cancellation-Checks passierter Debounce kann hinter einem `SaveRetention`-Persist einlaufen — beide schreiben denselben Snapshot, idempotent. Race Debounce vs. `_isLoading`: Wird der Debounce während eines laufenden `LoadAsync` gefeuert, wird der Persist konsistent zu `PersistOnChange` übersprungen; `LoadAsync` überschreibt `RetentionDays` ohnehin mit dem Persistenzstand — kein verlorenes Update (Load-wins-Semantik des ViewModels). Fire-and-forget `_ = PersistRetentionDebouncedAsync(cts)` kann faktisch nicht faulten: `Task.Delay` mit konstantem Delay wirft nur `OperationCanceledException` (gefangen), `PersistAsync` fängt alle Exceptions intern (Zeilen 499–502). Reihenfolge in `SaveRetention` (Setter → `ScheduleRetentionPersist` → `CancelRetentionDebounce` → `PersistAsync`) ist erforderlich und korrekt: Das Cancel muss nach der Setter-Zuweisung erfolgen, damit kein hängender Debounce später doppelt speichert.
- **TimeProvider-Injektion**: `TimeProvider` ist nicht im DI-Container registriert (`MauiProgram.cs`, Zeilen 42–68) — der optionale Ctor-Parameter `TimeProvider? timeProvider = null` mit `?? TimeProvider.System` (Zeilen 65/71) ist daher korrekt und identisch zum `AutoRefreshService`-Muster (`AutoRefreshService.cs`, Zeilen 28/32). `Task.Delay(TimeSpan, TimeProvider, CancellationToken)` ist unter `net10.0` verfügbar.
- **Speicher-/Leck-Aspekte bei Page-Verlassen**: `SettingsViewModel` ist Singleton; ein hängender Debounce läuft nach Verlassen der Seite weiter und persistiert die letzte Änderung — gewünschtes Verhalten, der Task hält die VM nur ~500 ms am Leben. Einziges Restthema ist der oben gemeldete nicht-disposed CTS auf dem Erfolgspfad.
- **Testabdeckung**: Die drei neuen Tests decken Debounce-Persist, Koaleszierung und DragCompleted-Cancel jeweils mit einem fachlichen Fall und AAA-Struktur ab; `FakeTimeProvider` macht die Zeitsteuerung deterministisch; `RecordingSettingsRepository`/`GatedSettingsRepository` implementieren `ISettingsRepository` vollständig.
- **Uncommitted resx/`ArticleDetailViewModel`-Änderungen**: `ArticleAutoMarkReadDelayFormat` in `AppResources.resx`/`AppResources.de.resx`/`AppResources.Designer.cs` konsistent angelegt (`{0}`-Platzhalter in beiden Sprachen); `AutoMarkReadLabel`-Setzungen (`ArticleDetailViewModel.cs`, Zeilen 43 und 266–268) nutzen `CultureInfo.CurrentCulture` — Befund aus `review-code.4.md` bleibt korrekt umgesetzt; `SettingsRetentionInfo`/`SettingsKeywordMatchLabel`-Umformulierungen synchron in beiden Sprachen.
- **`SettingsPage.xaml` / `ArticleDetailPage.xaml`**: `DragCompletedCommand`-Bindung, `x:Reference PageRoot`-Command-Binding und Disabled-Pattern (`IsEnabled` + Opacity 0,4) unverändert korrekt; Touch-Targets ≥ 44 pt.
- **Zusatz-Prüfregel (UI-Aktions-Events)**: Keine `RaiseUiActionRequested`-artigen Events im Feature-Diff (grep über `src/` ohne Treffer) — nicht anwendbar.

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
