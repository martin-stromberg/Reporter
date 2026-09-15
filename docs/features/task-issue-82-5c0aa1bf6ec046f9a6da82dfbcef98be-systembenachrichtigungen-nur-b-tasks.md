<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Systembenachrichtigungen nur bei Hintergrundabruf (Issue #82)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | Interface `IBackgroundRefreshService` in `src/Reporter.Core/Interfaces/` anlegen (`IsSupported`, `ApplySettingsAsync(Settings, CancellationToken)`, `RunScheduledSyncAsync(CancellationToken)` → `Task<bool>`) | Offen | — |
| 2 | Logik | `BackgroundRefreshService` in `src/Reporter/Services/` anlegen: `RefreshTaskIdentifier`-Konstante (`de.martinstromberg.reporter.feedrefresh`), `IsSupported` nur unter `#if IOS`, Konstruktor mit `IFeedSyncService`/`ISettingsRepository`/`IDebugLogService?` | Offen | — |
| 3 | Logik | `BackgroundRefreshService.ApplySettingsAsync` implementieren: bei `AutoRefreshEnabled` `BGAppRefreshTaskRequest` mit `EarliestBeginDate` = geclampptes `RefreshIntervalMinutes` (1–1440) submitten, sonst `BGTaskScheduler.Shared.Cancel`; No-Op auf Nicht-iOS | Offen | — |
| 4 | Logik | `BackgroundRefreshService.RunScheduledSyncAsync` implementieren: fehlerisolierter `IFeedSyncService.SyncAllAsync` mit `IDebugLogService`-Protokollierung (`DebugLogCategory.Sync`), anschließende Neuplanung via `ISettingsRepository` + `ApplySettingsAsync` (auch im Fehlerfall), `Task<bool>`-Ergebnis | Offen | — |
| 5 | Konfiguration | `src/Reporter/Platforms/iOS/Info.plist`: `UIBackgroundModes` mit `fetch` ergänzen | Offen | — |
| 6 | Konfiguration | `Info.plist`: `BGTaskSchedulerPermittedIdentifiers` mit `de.martinstromberg.reporter.feedrefresh` ergänzen (identisch zu `RefreshTaskIdentifier`) | Offen | — |
| 7 | Logik | `AppDelegate` (`src/Reporter/Platforms/iOS/`): `RegisterBackgroundFetchTask` + `HandleRefreshTaskAsync(BGAppRefreshTask)` hinzufügen (Registration, `ExpirationHandler` → Cancellation, lazy `IBackgroundRefreshService`-Auflösung via `IPlatformApplication.Current.Services`, `SetTaskCompleted`) und aus `FinishedLaunching` aufrufen | Offen | — |
| 8 | Logik | `NotificationDelegate.WillPresentNotification` auf `UNNotificationPresentationOptions.None` ändern, XML-Doc anpassen | Offen | — |
| 9 | Logik | `AutoRefreshService`: Pflicht-Konstruktorparameter `IBackgroundRefreshService` ergänzen (Feld `_backgroundRefreshService`) | Offen | — |
| 10 | Logik | `AutoRefreshService.ApplySettingsAsync`: `settings` fehlerisoliert an `_backgroundRefreshService.ApplySettingsAsync` weiterleiten — nach `StopLoopAsync`, vor dem `AutoRefreshEnabled`-Early-Return | Offen | — |
| 11 | Konfiguration | `MauiProgram.CreateMauiApp`: `AddSingleton<IBackgroundRefreshService, BackgroundRefreshService>` registrieren | Offen | — |
| 12 | Tests | `FakeBackgroundRefreshService` in `src/Reporter.Tests/` anlegen (`AppliedSettings`-Aufzeichnung, konfigurierbares `RunScheduledSyncResult`, optionale Exception) | Offen | — |
| 13 | Tests | `AutoRefreshServiceTests`: alle `new AutoRefreshService(...)`-Aufrufe um `FakeBackgroundRefreshService` ergänzen | Offen | — |
| 14 | Tests | `AutoRefreshServiceTests_DebugLog`: alle `new AutoRefreshService(...)`-Aufrufe um `FakeBackgroundRefreshService` ergänzen | Offen | — |
| 15 | Tests | `AutoRefreshServiceTests`: `ApplySettings_ForwardsToBackgroundRefresh` hinzufügen | Offen | — |
| 16 | Tests | `AutoRefreshServiceTests`: `ApplySettings_WhenAutoRefreshDisabled_StillForwardsToBackgroundRefresh` hinzufügen | Offen | — |
| 17 | Tests | `AutoRefreshServiceTests`: `ApplySettings_BackgroundRefreshThrows_TimerStillConfigured` hinzufügen | Offen | — |
| 18 | Tests | `ServiceCollectionTests`: `AddReporterServices_ResolvesAutoRefreshService` hinzufügen (spiegelt `MauiProgram`, inkl. `FakeBackgroundRefreshService`) | Offen | — |
| 19 | Dokumentation | `docs/help/einstellungen/architektur.md`: Aussage „kein OS-seitiger Background-Fetch" (Timer-Lebensdauer) korrigieren | Offen | — |
| 20 | Dokumentation | `docs/help/einstellungen/ablauf-technisch.md`: Abschnitt 5 um OS-Hintergrundabruf und Weiterleitung in `ApplySettingsAsync` ergänzen | Offen | — |
| 21 | Dokumentation | `docs/help/benachrichtigungen/architektur.md`: `NotificationDelegate`-Rolle, `Info.plist`-Aussage und Abhängigkeiten/Datenfluss um `IBackgroundRefreshService`/`BGTaskScheduler` aktualisieren | Offen | — |
| 22 | Dokumentation | `docs/help/benachrichtigungen/ablauf-technisch.md`: Abschnitt 4 (`WillPresentNotification` → `None`) und neuen Hintergrundabruf-Abschnitt ergänzen, Übersicht/Diagramm anpassen | Offen | — |
| 23 | E2E-Tests | Manuelle iOS-Verifikation via `scripts/iOS-Deployment.ps1`: Vordergrund-Syncs (manuell + Timer) ohne Banner/Sound/Mitteilungszentrum; simulierter `BGAppRefreshTask` (`_simulateLaunchForTaskWithIdentifier`) erzeugt Mitteilung; Expiration (`_simulateExpirationForTaskWithIdentifier`) stabil; `AutoRefreshEnabled`-Kopplung; Tap-Navigation unverändert — Ergebnisse in `test-results.md` dokumentieren | Offen | — |
| 24 | E2E-Tests | Manuelle iOS-Verifikation: Kaltstart der App in den Vordergrund mit aktiviertem `RefreshOnStartupEnabled` und neuen Artikeln → `RunStartupSyncAsync` läuft, aber kein Banner/kein Sound/kein Mitteilungszentrum-Eintrag — Ergebnis in `test-results.md` dokumentieren | Offen | — |
| 25 | Verifikation | `dotnet test` (Release, Coverlet-Settings) und `.\scripts\Run-StaticChecks.ps1` fehlerfrei durchlaufen lassen | Offen | — |
