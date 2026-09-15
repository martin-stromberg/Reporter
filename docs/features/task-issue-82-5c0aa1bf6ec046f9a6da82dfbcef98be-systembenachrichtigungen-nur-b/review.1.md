<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

- [x] `IBackgroundRefreshService` (Interface) — angelegt in `src/Reporter.Core/Interfaces/IBackgroundRefreshService.cs`
- [x] Property `IsSupported` in `IBackgroundRefreshService` — vorhanden (`IBackgroundRefreshService.cs:19`)
- [x] Methode `ApplySettingsAsync(Settings, CancellationToken)` in `IBackgroundRefreshService` — vorhanden (`IBackgroundRefreshService.cs:30`)
- [x] Methode `RunScheduledSyncAsync(CancellationToken)` → `Task<bool>` in `IBackgroundRefreshService` — vorhanden (`IBackgroundRefreshService.cs:39`)
- [x] `BackgroundRefreshService` (Klasse) — angelegt in `src/Reporter/Services/BackgroundRefreshService.cs`
- [x] Konstante `RefreshTaskIdentifier` (`de.martinstromberg.reporter.feedrefresh`) in `BackgroundRefreshService` — vorhanden (`BackgroundRefreshService.cs:25`)
- [x] `IsSupported` nur unter `#if IOS` `true`, sonst `false` — vorhanden (`BackgroundRefreshService.cs:48-53`)
- [x] Konstruktor-Abhängigkeiten `IFeedSyncService`, `ISettingsRepository`, `IDebugLogService?` — vorhanden (`BackgroundRefreshService.cs:40`)
- [x] `ApplySettingsAsync`: `BGAppRefreshTaskRequest` mit `EarliestBeginDate` = geclampptes `RefreshIntervalMinutes` (1–1440) bei `AutoRefreshEnabled`, sonst `BGTaskScheduler.Shared.Cancel`; No-Op auf Nicht-iOS — vorhanden (`BackgroundRefreshService.cs:56-82`)
- [x] `RunScheduledSyncAsync`: fehlerisolierter `SyncAllAsync` mit `IDebugLogService`-Protokollierung (`Sync`, `Error`), Neuplanung via `ISettingsRepository` + `ApplySettingsAsync` auch im Fehlerfall, `Task<bool>`-Ergebnis — vorhanden (`BackgroundRefreshService.cs:85-113`)
- [x] `FakeBackgroundRefreshService` (Test-Hilfsklasse) — angelegt in `src/Reporter.Tests/FakeBackgroundRefreshService.cs` mit `AppliedSettings`, `IsSupported`, `RunScheduledSyncResult`, `ApplySettingsException`
- [x] `NotificationDelegate.WillPresentNotification` → `UNNotificationPresentationOptions.None` — umgesetzt (`NotificationDelegate.cs:27`); Klassen-XML-Doc angepasst (`NotificationDelegate.cs:10-15`); `DidReceiveNotificationResponse` unverändert
- [x] `AppDelegate.RegisterBackgroundFetchTask` — `BGTaskScheduler.Shared.Register(RefreshTaskIdentifier, null, launchHandler)` vorhanden (`AppDelegate.cs:38-47`)
- [x] `AppDelegate.HandleRefreshTaskAsync(BGAppRefreshTask)` — `CancellationTokenSource` + `ExpirationHandler`, lazy Auflösung via `IPlatformApplication.Current?.Services`, `RunScheduledSyncAsync` awaited, `SetTaskCompleted(success)` im `finally` — vorhanden (`AppDelegate.cs:49-72`)
- [x] `AppDelegate.FinishedLaunching` ruft `RegisterBackgroundFetchTask` nach `base.FinishedLaunching` auf — vorhanden (`AppDelegate.cs:30-36`)
- [x] `AutoRefreshService`: Pflicht-Konstruktorparameter `IBackgroundRefreshService` vor den optionalen Parametern `TimeProvider?`/`IDebugLogService?`, Feld `_backgroundRefreshService` — vorhanden (`AutoRefreshService.cs:20,36,41`)
- [x] `AutoRefreshService.ApplySettingsAsync`: fehlerisolierte Weiterleitung nach `StopLoopAsync` und vor dem `AutoRefreshEnabled`-Early-Return (`Debug.WriteLine` + `IDebugLogService`, `Sync`, `Warning`) — vorhanden (`AutoRefreshService.cs:79-98`)
- [x] `MauiProgram.CreateMauiApp`: `AddSingleton<IBackgroundRefreshService, BackgroundRefreshService>` nach `ILocalNotificationService`/`INetworkStatusService` — vorhanden (`MauiProgram.cs:67`)
- [x] `Info.plist`: `UIBackgroundModes` → `fetch` — vorhanden (`Info.plist:35-38`)
- [x] `Info.plist`: `BGTaskSchedulerPermittedIdentifiers` → `de.martinstromberg.reporter.feedrefresh` (identisch zu `RefreshTaskIdentifier`) — vorhanden (`Info.plist:39-42`)
- [x] Doku `docs/help/einstellungen/architektur.md` — Aussage „kein OS-seitiger Background-Fetch" korrigiert, `IBackgroundRefreshService`-Abhängigkeit ergänzt
- [x] Doku `docs/help/einstellungen/ablauf-technisch.md` — Abschnitt 5 um OS-Hintergrundabruf und Weiterleitung ergänzt
- [x] Doku `docs/help/benachrichtigungen/architektur.md` — Komponententabelle, `Info.plist`-Aussage und Datenfluss um `IBackgroundRefreshService`/`BGTaskScheduler` erweitert
- [x] Doku `docs/help/benachrichtigungen/ablauf-technisch.md` — Abschnitt 4 auf `None` umgestellt, neuer Abschnitt 5 „Hintergrundabruf (iOS `BGAppRefreshTask`)" ergänzt, Übersicht/Mermaid-Diagramm angepasst
- [x] Test `ApplySettings_ForwardsToBackgroundRefresh` in `AutoRefreshServiceTests` — vorhanden (`AutoRefreshServiceTests.cs:385`), bestanden
- [x] Test `ApplySettings_WhenAutoRefreshDisabled_StillForwardsToBackgroundRefresh` in `AutoRefreshServiceTests` — vorhanden (`AutoRefreshServiceTests.cs:402`), bestanden
- [x] Test `ApplySettings_BackgroundRefreshThrows_TimerStillConfigured` in `AutoRefreshServiceTests` — vorhanden (`AutoRefreshServiceTests.cs:418`), bestanden
- [x] Test `AddReporterServices_ResolvesAutoRefreshService` in `ServiceCollectionTests` — vorhanden (`ServiceCollectionTests.cs:123`), bestanden
- [x] Konstruktoranpassungen aller `new AutoRefreshService(...)`-Aufrufe in `AutoRefreshServiceTests` (`:34`) und `AutoRefreshServiceTests_DebugLog` (`:33`) — vorhanden
- [x] Verifikation `dotnet test` (Release, Coverlet) + `Run-StaticChecks.ps1` — dokumentiert in `test-results.md`: 485/485 bestanden, Static Checks ohne Befund
- [x] Unveränderte Signaturen `IFeedSyncService`/`INotificationService`/`IAutoRefreshService` — keine Änderungen an `FeedSyncService`/`NotificationService`/ViewModels im Diff; bestehende Tests/Fakes unberührt

## Offene Aufgaben

- [ ] `E2E-Verifikation: Vordergrund-Unterdrückung + BGAppRefreshTask-Verhalten (manuell)` — fehlt vollständig: manuelle iOS-Verifikation via `scripts/iOS-Deployment.ps1` nicht ausgeführt (manueller Sync und Timer-Abruf ohne Banner/Sound/Mitteilungszentrum; `_simulateLaunchForTaskWithIdentifier` erzeugt Mitteilung; `_simulateExpirationForTaskWithIdentifier` stabil; `AutoRefreshEnabled`-Kopplung; Tap-Navigation unverändert). Unter Windows nicht ausführbar — macOS/iOS-Gerät erforderlich; als „Nicht ausgeführt" in `test-results.md` dokumentiert (Tasks-Datei Nr. 23).
- [ ] `E2E-Verifikation: Start-Abruf bei RefreshOnStartupEnabled (manuell)` — fehlt vollständig: Kaltstart in den Vordergrund mit neuen Artikeln → `RunStartupSyncAsync` läuft, aber keine Systemmitteilung. Ebenfalls nur auf macOS/iOS-Gerät prüfbar; als ausstehend dokumentiert (Tasks-Datei Nr. 24).

## Hinweise

- Der iOS-Plattformcode (`AppDelegate`, `NotificationDelegate`, `#if IOS`-Blöcke in `BackgroundRefreshService`) wird unter Windows nicht kompiliert — die Compile-Verifikation erfolgt erst beim macOS-Build (CI baut mit `IncludeIosTarget=false`). API-Namen wurden laut `test-results.md` gegen net-ios-10.0-Bindings abgeglichen; eine tatsächliche Kompilierung des iOS-Targets steht aus.
- Reihenfolge-Hinweis aus dem Plan (Schritt 5: `WillPresentNotification` → `None` sollte nicht ohne Schritt 4 ausgeliefert werden) ist eingehalten — beide Änderungen liegen gemeinsam im Arbeitsverzeichnis vor.
- Keine Abweichungen zum Plan in der Implementierung festgestellt; die beiden offenen Punkte betreffen ausschließlich die manuelle E2E-Verifikation auf Apple-Hardware.
