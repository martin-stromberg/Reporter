<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Systembenachrichtigungen nur bei Hintergrundabruf (Issue #82)

Analysiert wurde der Benachrichtigungs- und Sync-Pfad der Reporter-App (Reporter.Core, MAUI-Projekt, iOS-Plattformcode, Reporter.Tests) bezogen auf die Anforderung, lokale Benachrichtigungen künftig nur bei OS-seitigem Hintergrundabruf auszulösen.

## Zusammenfassung

- Der Benachrichtigungspfad existiert vollständig: `FeedSyncService.RunSyncAsync` ruft bei neuen Items fehlerisoliert `INotificationService.NotifyNewItemsAsync` auf; `NotificationService` wertet das Regelwerk (`NotificationsEnabled`, `Feed.NotificationsEnabled`, Ruhezeiten, Keyword-Filter, `NotificationSummaryEnabled`) aus und delegiert an `ILocalNotificationService.ShowAsync`.
- `NotificationDelegate.WillPresentNotification` (iOS) gibt heute `Banner | List | Sound` zurück — Mitteilungen erscheinen daher auch im Vordergrund.
- Es gibt **keine** Hintergrundabruf-Infrastruktur: kein `BGTaskScheduler`, kein `UIBackgroundModes` in `Info.plist`, keine `PerformFetch`-Implementierung. `AppDelegate` setzt in `FinishedLaunching` nur den `UNUserNotificationCenter.Delegate`.
- Es existiert **keine Auslöser-Unterscheidung** (kein `SyncOrigin`/`SyncTrigger`): `IFeedSyncService.SyncFeedAsync`/`SyncAllAsync` kennen nur `feedId` und `CancellationToken`. Alle Aufrufer (manuell, Timer, Start-Abruf) laufen durch denselben Pfad.
- `ILocalNotificationService` folgt dem Gateway-Muster (Interface in `Reporter.Core`, Implementierung `LocalNotificationService` im MAUI-Projekt, No-Op außerhalb iOS). Ein `IBackgroundRefreshService`/`IAppStateService`-Gateway existiert noch nicht.
- Aufrufer des Syncs: `FeedsViewModel.RefreshAsync`/`RefreshAllAsync` (manuell/Pull-to-Refresh), `UnreadViewModel.RefreshAsync` (Pull-to-Refresh), `AutoRefreshService.RunLoopAsync` (Timer) und `RunStartupSyncAsync` (Start-Abruf bei `RefreshOnStartupEnabled`).
- Test-Ausgangszustand: **481/481 .NET-Tests bestanden** (`dotnet test`, Release, inkl. Coverage-Report) sowie **36/36 Node-Skripttests** (`npm test`). Keine nachgewiesenen Fehlschläge, keine übersprungenen Tests. Nachweis: [inventory/tests.md](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md) — `Feed`, `Settings`, `Item` (benachrichtigungsrelevante Eigenschaften)
- [Logik](inventory/logic.md) — `FeedSyncService`, `NotificationService`, `LocalNotificationService`, `AutoRefreshService`, iOS-Plattformcode (`AppDelegate`, `NotificationDelegate`), `App`, `MauiProgram`, Sync-Aufrufer in den ViewModels
- [Enums und Konstanten](inventory/enums.md) — `NotificationAuthorizationStatus`, `FeedHealth`, `FeedSyncErrorKind`, `DebugLogCategory`
- [Interfaces](inventory/interfaces.md) — `IFeedSyncService`, `INotificationService`, `ILocalNotificationService`, `IAutoRefreshService`, `INetworkStatusService` (Gateway-Muster-Referenz)
- [Tests](inventory/tests.md) — Test-Ausgangszustand mit Nachweisen, bestehende Testklassen und Hilfsmethoden
