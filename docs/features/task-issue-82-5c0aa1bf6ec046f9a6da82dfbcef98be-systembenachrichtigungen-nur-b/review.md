<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

### Neue Klassen / Interfaces

- [x] `IBackgroundRefreshService` (Interface, `src/Reporter.Core/Interfaces/IBackgroundRefreshService.cs`) — angelegt mit `IsSupported` (`bool`, Zeile 19) und `ApplySettingsAsync(Settings, CancellationToken)` (Zeile 30). Das im Plan vorgesehene `RunScheduledSyncAsync(CancellationToken)` → `Task<bool>` wurde in Iteration 2 in das Interface `IScheduledSyncRunner` verlagert (siehe Hinweise).
- [x] `IScheduledSyncRunner` (Interface, `src/Reporter.Core/Interfaces/IScheduledSyncRunner.cs`) — angelegt mit `RunAsync(CancellationToken)` → `Task<bool>` (Zeile 18); übernimmt die planmäßige `RunScheduledSyncAsync`-Semantik (Sync ausführen + Folgeabruf planen, Rückgabewert für `SetTaskCompleted`).
- [x] `BackgroundRefreshService` (Klasse, `src/Reporter/Services/BackgroundRefreshService.cs`) — angelegt; Konstante `RefreshTaskIdentifier` = `"de.martinstromberg.reporter.feedrefresh"` (Zeile 25); `IsSupported` nur unter `#if IOS` `true`, sonst `false` (Zeilen 39–44); Konstruktor mit optionalem `IDebugLogService?` (Zeile 33); die Sync-/Neuplanungs-Abhängigkeiten `IFeedSyncService`/`ISettingsRepository` liegen in `ScheduledSyncRunner` (Iteration-2-Verlagerung).
- [x] Methode `ApplySettingsAsync` in `BackgroundRefreshService` (Zeilen 47–73) — `#if IOS`: bei `AutoRefreshEnabled` `BGTaskScheduler.Shared.Submit(new BGAppRefreshTaskRequest(RefreshTaskIdentifier) { EarliestBeginDate = now + SettingsValues.ClampRefreshIntervalMinutes(RefreshIntervalMinutes) })` mit Submit-Fehler-Protokollierung (`Debug.WriteLine` + `IDebugLogService`, `Sync`, `Warning`); sonst `BGTaskScheduler.Shared.Cancel(RefreshTaskIdentifier)`; No-Op (`Task.CompletedTask`) auf anderen Targets. Clamp-Grenzen 1–1440 identisch zum Timer-Loop.
- [x] `ScheduledSyncRunner` (Klasse, `src/Reporter.Core/Services/ScheduledSyncRunner.cs`) — angelegt; `RunAsync` führt `IFeedSyncService.SyncAllAsync` fehlerisoliert aus (Fehler → `IDebugLogService`, `Sync`, `Error`, Ergebnis `false`, Zeilen 38–47), lädt anschließend `Settings` über `ISettingsRepository` und plant den Folgeabruf via `IBackgroundRefreshService.ApplySettingsAsync` neu — auch im Fehler- und Kancellierungsfall (Neuplanungs-Fehler → `Sync`, `Warning`, Zeilen 51–60).
- [x] `FakeBackgroundRefreshService` (Test-Hilfsklasse, `src/Reporter.Tests/FakeBackgroundRefreshService.cs`) — angelegt nach dem Fake-Muster; `AppliedSettings`-Aufzeichnung, `IsSupported` und `ApplySettingsException` konfigurierbar. Das planmäßige `RunScheduledSyncResult` entfällt mit der Verlagerung von `RunScheduledSyncAsync`.

### Änderungen an bestehenden Klassen

- [x] Methode `WillPresentNotification` in `NotificationDelegate` (`src/Reporter/Platforms/iOS/NotificationDelegate.cs:19-28`) — liefert `UNNotificationPresentationOptions.None`; Klassen-XML-Doc angepasst (Vordergrund-Darstellung unterdrückt, Tap-Handling bleibt); `DidReceiveNotificationResponse` unverändert.
- [x] Methode `RegisterBackgroundFetchTask` in `AppDelegate` (`src/Reporter/Platforms/iOS/AppDelegate.cs:40-56`) — `BGTaskScheduler.Shared.Register(BackgroundRefreshService.RefreshTaskIdentifier, null, launchHandler)`; Handler filtert auf `BGAppRefreshTask`. Iteration-3-Ergänzung: bei `registered == false` Protokollierung per `Debug.WriteLine` + `IDebugLogService`-`Warning` (Zeilen 50–55).
- [x] Methode `HandleRefreshTaskAsync(BGAppRefreshTask)` in `AppDelegate` (`AppDelegate.cs:58-91`) — `CancellationTokenSource` + `task.ExpirationHandler = cts.Cancel`; lazy Auflösung von `IScheduledSyncRunner` über `IPlatformApplication.Current?.Services`; `RunAsync` awaited; `SetTaskCompleted(success)` im `finally`. Iteration-3-Ergänzung: nicht auflösbarer `IScheduledSyncRunner` wird per `Debug.WriteLine` + `IDebugLogService`-`Warning` protokolliert (Zeilen 74–78); catch-Block protokolliert per `Debug.WriteLine` + `IDebugLogService`-`Warning` und setzt `success = false` (Zeilen 80–86).
- [x] Methode `FinishedLaunching` in `AppDelegate` (`AppDelegate.cs:32-38`) — ruft `RegisterBackgroundFetchTask` nach `base.FinishedLaunching` auf, vor Rückkehr aus `FinishedLaunching`.
- [x] Konstruktor-Abhängigkeit `IBackgroundRefreshService` in `AutoRefreshService` (`src/Reporter.Core/Services/AutoRefreshService.cs:33`) — Pflichtparameter vor den optionalen `TimeProvider?`/`IDebugLogService?`; Feld `_backgroundRefreshService` (Zeile 17).
- [x] Methode `ApplySettingsAsync` in `AutoRefreshService` (`AutoRefreshService.cs:71-109`) — fehlerisolierte Weiterleitung nach `StopLoopAsync` und vor dem `AutoRefreshEnabled`-Early-Return (`Debug.WriteLine` + `IDebugLogService`, `Sync`, `Warning`, Zeilen 82–93), hinter `IsSupported`-Prüfung (Zeile 84, Iteration-2-Anpassung).
- [x] Felder `MinRefreshIntervalMinutes`/`MaxRefreshIntervalMinutes` + Methode `ClampRefreshIntervalMinutes` in `SettingsValues` (`src/Reporter.Core/Models/SettingsValues.cs:58-94`) — zentrale Clamp-Konstanten; Verwender `AutoRefreshService.cs:100` und `BackgroundRefreshService.cs:53` referenzieren die zentrale Stelle.
- [x] `MauiProgram.CreateMauiApp` (`src/Reporter/MauiProgram.cs:67-68`) — `AddSingleton<IBackgroundRefreshService, BackgroundRefreshService>` und `AddSingleton<IScheduledSyncRunner, ScheduledSyncRunner>` nach `ILocalNotificationService`/`INetworkStatusService` registriert.

### Konfiguration

- [x] `Info.plist` `UIBackgroundModes` → `fetch` (`src/Reporter/Platforms/iOS/Info.plist:35-38`) — vorhanden.
- [x] `Info.plist` `BGTaskSchedulerPermittedIdentifiers` → `de.martinstromberg.reporter.feedrefresh` (`Info.plist:39-42`) — vorhanden, identisch zu `BackgroundRefreshService.RefreshTaskIdentifier`.

### Tests

- [x] `AutoRefreshServiceTests` — alle `new AutoRefreshService(...)`-Aufrufe um `FakeBackgroundRefreshService` ergänzt (`AutoRefreshServiceTests.cs:34`).
- [x] `AutoRefreshServiceTests_DebugLog` — Konstruktoraufruf angepasst (`AutoRefreshServiceTests_DebugLog.cs:33`).
- [x] Test `ApplySettings_ForwardsToBackgroundRefresh` — vorhanden, bestanden.
- [x] Test `ApplySettings_WhenAutoRefreshDisabled_StillForwardsToBackgroundRefresh` — vorhanden, bestanden.
- [x] Test `ApplySettings_BackgroundRefreshThrows_TimerStillConfigured` — vorhanden, bestanden.
- [x] Test `ApplySettings_BackgroundRefreshUnsupported_DoesNotForward` — vorhanden, bestanden (Iteration-2-Zusatz für die `IsSupported`-Verdrahtung).
- [x] `ScheduledSyncRunnerTests` — `RunAsync_SyncSucceeds_ReturnsTrueAndReschedules`, `RunAsync_SyncFails_ReturnsFalseAndStillReschedules`, `RunAsync_SyncCancelled_ReturnsFalseAndStillReschedules`, `RunAsync_ReschedulingFails_ReturnsTrue` — vorhanden, bestanden (deckt die nach `Reporter.Core` verlagerte Orchestrierung ab).
- [x] Test `AddReporterServices_ResolvesAutoRefreshService` (`ServiceCollectionTests`) — vorhanden, bestanden.
- [x] Test `AddReporterServices_ResolvesScheduledSyncRunner` (`ServiceCollectionTests`) — vorhanden, bestanden.

### Dokumentation

- [x] `docs/help/einstellungen/architektur.md` — Aussage „kein OS-seitiger Background-Fetch" korrigiert (Timer-Lebensdauer vs. `BGAppRefreshTask`, `IsSupported`-No-Op vermerkt).
- [x] `docs/help/einstellungen/ablauf-technisch.md` — Abschnitt 5 um OS-Hintergrundabruf, `IsSupported`-Gate, `ScheduledSyncRunner` und die Weiterleitung in `ApplySettingsAsync` ergänzt.
- [x] `docs/help/benachrichtigungen/architektur.md` — Komponententabelle (`NotificationDelegate`-Rolle mit `None`, `IBackgroundRefreshService`/`BackgroundRefreshService`, `IScheduledSyncRunner`/`ScheduledSyncRunner`, `AppDelegate`), `Info.plist`-Aussage und Abhängigkeiten/Datenfluss um `BGTaskScheduler` erweitert.
- [x] `docs/help/benachrichtigungen/ablauf-technisch.md` — Abschnitt 4 auf `UNNotificationPresentationOptions.None` geändert, neuer Abschnitt 5 „Hintergrundabruf (iOS `BGAppRefreshTask`)" eingefügt, Übersicht und Mermaid-Diagramm angepasst.

### Verifikation

- [x] `dotnet test` (Release, Coverlet-Settings): 491/491 bestanden, 0 fehlgeschlagen, 0 übersprungen — dokumentiert in `test-results.md`; die Iteration-3-Änderung betrifft ausschließlich iOS-Plattformcode (`AppDelegate.cs`), der unter Windows nicht kompiliert wird und das Testergebnis nicht beeinflusst.
- [x] `Run-StaticChecks.ps1` laut Tasks-Datei ohne Befund (Aufgabe 25).

## Offene Aufgaben

- [ ] E2E-Pflichtszenario (Tasks #23, Plan-Tabelle „E2E-Tests"): Manuelle iOS-Verifikation via `scripts/iOS-Deployment.ps1` — Vordergrund-Syncs (manuell + Timer) ohne Banner/Sound/Mitteilungszentrum; simulierter `BGAppRefreshTask` (`_simulateLaunchForTaskWithIdentifier`) erzeugt Mitteilung; Expiration (`_simulateExpirationForTaskWithIdentifier`) stabil; `AutoRefreshEnabled`-Kopplung; Tap-Navigation unverändert — nicht ausführbar unter Windows (macOS/iOS-Gerät erforderlich); als ausstehend in `test-results.md` dokumentiert.
- [ ] E2E-Pflichtszenario (Task #24, Plan-Tabelle „E2E-Tests"): Kaltstart in den Vordergrund mit aktiviertem `RefreshOnStartupEnabled` und neuen Artikeln → `RunStartupSyncAsync` läuft, aber keine Systemmitteilung — nicht ausführbar unter Windows; als ausstehend in `test-results.md` dokumentiert.

## Hinweise

- **Iteration-3-Ergänzung (dieser Durchlauf):** `AppDelegate.RegisterBackgroundFetchTask` protokolliert eine fehlgeschlagene `BGTaskScheduler.Register`-Rückgabe (`registered == false`) per `Debug.WriteLine` + `IDebugLogService`-`Warning` (`AppDelegate.cs:50-55`); `HandleRefreshTaskAsync` protokolliert einen nicht auflösbaren `IScheduledSyncRunner` ebenfalls per `Debug.WriteLine` + `IDebugLogService`-`Warning` (`AppDelegate.cs:74-78`). Beide Befunde aus Review-Iteration 2 sind damit geschlossen.
- **Architektur-Anpassung in Iteration 2 (bewusste Umsetzung des Plans):** Der Plan sah `RunScheduledSyncAsync` als Member von `IBackgroundRefreshService` in `src/Reporter/` vor. Umgesetzt wurde `IScheduledSyncRunner`/`ScheduledSyncRunner` in `Reporter.Core` — die gesamte planmäßige Semantik (fehlerisolierter `SyncAllAsync`, `Task<bool>` für `SetTaskCompleted`, Neuplanung aus persistierten Settings via `ISettingsRepository` + Gateway, auch im Fehler-/Kancellierungsfall) ist erhalten und unit-testbar. `IBackgroundRefreshService` bleibt frei von iOS-Typen; der `AppDelegate` löst `IScheduledSyncRunner` auf.
- **`IsSupported`-Gate:** `AutoRefreshService.ApplySettingsAsync` prüft `IsSupported` vor der Weiterleitung (`AutoRefreshService.cs:84`) — sinnvolle Ergänzung gegenüber der im Plan vorgesehenen unbedingten Weiterleitung (No-Op-Aufruf auf Nicht-iOS wird vermieden); semantisch äquivalent.
- **Zentrale Clamp-Konstanten:** `SettingsValues.ClampRefreshIntervalMinutes` (`MinRefreshIntervalMinutes = 1`, `MaxRefreshIntervalMinutes = 1440`) hält die planmäßigen Grenzen 1–1440 an einer Stelle für Timer-Loop und `EarliestBeginDate`.
- **iOS-Plattformcode:** `AppDelegate`, `NotificationDelegate` und die `#if IOS`-Blöcke von `BackgroundRefreshService` werden unter Windows nicht kompiliert (`IncludeIosTarget=false`); die Verifikation dieser Dateien beruht auf Sichtprüfung — der macOS-Build und die manuelle Geräte-Verifikation stehen noch aus (siehe offene Aufgaben).
- **Tasks-Datei:** `docs/features/task-issue-82-…-tasks.md` wurde gegen die Prüfergebnisse abgeglichen und ist aktuell — Aufgaben 1–22 und 25 `Erledigt`, die manuellen iOS-E2E-Aufgaben 23–24 bleiben `Offen`.
