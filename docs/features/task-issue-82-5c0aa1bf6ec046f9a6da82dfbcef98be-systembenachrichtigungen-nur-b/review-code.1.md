<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### IBackgroundRefreshService.cs / BackgroundRefreshService.cs / FakeBackgroundRefreshService.cs

- **Toter Code / Speculative Generality** — `IBackgroundRefreshService.IsSupported` (`IBackgroundRefreshService.cs:19`) wird von keinem Consumer gelesen: `AutoRefreshService` ruft `ApplySettingsAsync` unbedingt auf (`AutoRefreshService.cs:87`), `AppDelegate` ruft `RunScheduledSyncAsync` unbedingt auf (`AppDelegate.cs:60`). Im Gegensatz dazu wird das analoge `ILocalNotificationService.IsSupported` tatsächlich ausgewertet (`SettingsViewModel.NotificationsSupported`, `SettingsViewModel.cs:393`). Ebenso ist `FakeBackgroundRefreshService.RunScheduledSyncResult` (`FakeBackgroundRefreshService.cs:30`) konfigurierbar, wird aber in keinem Test gesetzt oder ausgewertet.

  Empfehlung: Entweder `IsSupported` an einen Consumer verdrahten (z. B. Settings-Hinweis „Hintergrundaktualisierung nur unter iOS") oder das Member aus dem Interface entfernen; `RunScheduledSyncResult` aus dem Fake entfernen oder in einem Test nutzen.

### BackgroundRefreshService.cs / AutoRefreshService.cs

- **Doppelter Code** — Die Clamp-Grenzen `MinRefreshIntervalMinutes = 1` und `MaxRefreshIntervalMinutes = 1440` sind identisch in `AutoRefreshService.cs:14-15` (Reporter.Core) und `BackgroundRefreshService.cs:27-28` (Reporter) dupliziert, inklusive des `Math.Clamp(settings.RefreshIntervalMinutes, …)`-Aufrufs. Es ist dieselbe fachliche Invariante an zwei Stellen in zwei Assemblies — bei einer Änderung der Grenzen laufen Timer und OS-Task auseinander.

  Empfehlung: Die Konstanten an einer gemeinsamen Stelle in `Reporter.Core` definieren (z. B. als `public const` am `Settings`-Modell oder einer kleinen `RefreshIntervalLimits`-Klasse) und aus beiden Services referenzieren.

### BackgroundRefreshService.cs (BackgroundRefreshService)

- **Testqualität / fehlende Testabdeckung** — `RunScheduledSyncAsync` (`BackgroundRefreshService.cs:85-113`) enthält die fachlich kritischste neue Logik (fehlerisolierter `SyncAllAsync`, Neuplanung des Folgeabrufs via `ISettingsRepository` auch im Fehler- und Kancellierungsfall, `bool`-Ergebnis für `SetTaskCompleted`), liegt aber in der `Reporter`-Assembly, die `Reporter.Tests` nicht referenziert (Test-Projekt referenziert nur `Reporter.Core` + `Reporter.Data`). Die Methode ist damit komplett ohne Unit-Test — und auf Windows nicht einmal kompilierbar.

  Empfehlung: Die plattformunabhängige Orchestrierung (Sync + Neuplanung + Ergebnis) nach `Reporter.Core` verlagern — z. B. ein `ScheduledSyncRunner`, der `IFeedSyncService`/`ISettingsRepository`/`IDebugLogService` nutzt und die Neuplanung über `IBackgroundRefreshService.ApplySettingsAsync` abstrahiert; der `Reporter`-Dienst enthält dann nur noch die `BGTaskScheduler`-Aufrufe. Anschließend Tests für Erfolg, Sync-Fehler (Neuplanung erfolgt trotzdem, Ergebnis `false`) und Neuplanungs-Fehler ergänzen.

### AppDelegate.cs (AppDelegate)

- **Fehlerbehandlung** — `HandleRefreshTaskAsync` (`AppDelegate.cs:63-67`) schluckt `catch (Exception)` ohne jegliche Protokollierung (kein `Debug.WriteLine`, kein `IDebugLogService`) — alle anderen neuen catch-Blöcke des Features protokollieren. Schlägt die Service-Auflösung über `IPlatformApplication.Current?.Services` fehl, ist der Ausfall unsichtbar; zudem wird dann `RunScheduledSyncAsync` nie erreicht und damit auch kein Folgeabruf eingeplant (selbstheilend erst beim nächsten App-Start über `StartAsync`).

  Empfehlung: Im catch-Block mindestens `Debug.WriteLine` mit der Exception ergänzen (analog `BackgroundRefreshService.cs:95`); optional — sofern der `IDebugLogService` auflösbar ist — einen Warning-Eintrag schreiben.

## Geprüfte Dateien

Quelldateien:
- `src/Reporter.Core/Interfaces/IBackgroundRefreshService.cs` (neu)
- `src/Reporter.Core/Services/AutoRefreshService.cs` (geändert)
- `src/Reporter/Services/BackgroundRefreshService.cs` (neu)
- `src/Reporter/MauiProgram.cs` (geändert)
- `src/Reporter/Platforms/iOS/AppDelegate.cs` (geändert)
- `src/Reporter/Platforms/iOS/NotificationDelegate.cs` (geändert)
- `src/Reporter/Platforms/iOS/Info.plist` (geändert)
- `src/Reporter.Tests/FakeBackgroundRefreshService.cs` (neu)
- `src/Reporter.Tests/AutoRefreshServiceTests.cs` (geändert)
- `src/Reporter.Tests/AutoRefreshServiceTests_DebugLog.cs` (geändert)
- `src/Reporter.Tests/ServiceCollectionTests.cs` (geändert)

Dokumentation (Konsistenz zum Code geprüft, keine Befunde):
- `docs/help/benachrichtigungen/ablauf-technisch.md`
- `docs/help/benachrichtigungen/architektur.md`
- `docs/help/einstellungen/ablauf-technisch.md`
- `docs/help/einstellungen/architektur.md`

## Zusatzprüfungen (ohne Befund)

- **iOS-API-Vertrag (Sichtprüfung, da auf Windows nicht kompilierbar):** `BGTaskScheduler.Shared.Register(string, DispatchQueue?, Action<BGTask>)` mit `null`-Queue (= Haupt-Queue), `BGAppRefreshTaskRequest(string)`, `EarliestBeginDate`/`NSDate.FromTimeIntervalSinceNow`, `Submit(request, out NSError)`, `Cancel(identifier)`, `BGTask.ExpirationHandler` (`Action`, Methodengruppe `cts.Cancel` auflösbar) und `SetTaskCompleted(bool)` entsprechen den .NET-iOS-Bindings. Registrierung erfolgt vor Rückkehr aus `FinishedLaunching` (Apple-Vorgabe); `Info.plist` enthält `UIBackgroundModes`=`fetch` und `BGTaskSchedulerPermittedIdentifiers` identisch zu `RefreshTaskIdentifier` (`de.martinstromberg.reporter.feedrefresh`). `IPlatformApplication`/`MainThread` sind über die MAUI-Implicit-Usings abgedeckt (`UseMaui` + `ImplicitUsings` in `Reporter.csproj`).
- **`RaiseUiActionRequested`:** Muster wird in diesem Branch nicht verwendet — Regel nicht zutreffend.
- **`NotificationDelegate`:** `UNNotificationPresentationOptions.None` ist korrekt (Vordergrund-Darstellung vollständig unterdrückt); `DidReceiveNotificationResponse` unverändert und behandelt weiterhin Hintergrund-Taps.
- **`AutoRefreshService`:** Weiterleitung ans Gateway ist korrekt fehlerisoliert und liegt vor dem `AutoRefreshEnabled`-Early-Return; die drei neuen Tests (`ApplySettings_ForwardsToBackgroundRefresh`, `…_StillForwardsToBackgroundRefresh`, `…_TimerStillConfigured`) decken den neuen Pfad ab.
