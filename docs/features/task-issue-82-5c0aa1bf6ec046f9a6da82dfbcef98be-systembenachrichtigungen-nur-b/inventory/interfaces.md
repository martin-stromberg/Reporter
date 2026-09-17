<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces

Alle Interfaces liegen unter `src/Reporter.Core/Interfaces/`; Implementierungen befinden sich in `Reporter.Core/Services/` (plattformneutral) bzw. `Reporter/Services/` bzw. `Reporter/Platforms/` (Gateway-Muster).

## `IFeedSyncService`
Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs` — Implementierung: `FeedSyncService`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId`, `CancellationToken` | `Task<SyncResult>` | Synchronisiert einen einzelnen Feed. **Kein Auslöser-/Origin-Parameter.** |
| `SyncAllAsync` | `CancellationToken` | `Task<SyncResult>` | Synchronisiert alle Feeds; aggregiert `SyncResult`. **Kein Auslöser-/Origin-Parameter.** |

Aufrufer: `FeedsViewModel.RefreshAsync`/`RefreshAllAsync`, `UnreadViewModel.RefreshAsync`, `AutoRefreshService.RunLoopAsync`/`RunStartupSyncAsync`.

## `INotificationService`
Datei: `src/Reporter.Core/Interfaces/INotificationService.cs` — Implementierung: `NotificationService`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `NotifyNewItemsAsync` | `Feed feed`, `IReadOnlyList<Item> newItems`, `CancellationToken` | `Task` | Wertet das Benachrichtigungs-Regelwerk aus und zeigt Mitteilungen via `ILocalNotificationService`. Einziger Aufrufer: `FeedSyncService.RunSyncAsync`. |

## `ILocalNotificationService`
Datei: `src/Reporter.Core/Interfaces/ILocalNotificationService.cs` — Implementierung: `LocalNotificationService` (Reporter-Projekt, iOS-spezifisch, sonst No-Op)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsSupported` (Property) | — | `bool` | Plattformunterstützung (nur iOS `true`). |
| `RequestAuthorizationAsync` | `CancellationToken` | `Task<bool>` | Fragt die System-Berechtigung an (iOS `UNAuthorizationOptions.Alert|Badge|Sound`). Aufgerufen aus `SettingsViewModel`. |
| `GetAuthorizationStatusAsync` | `CancellationToken` | `Task<NotificationAuthorizationStatus>` | Liest den Berechtigungsstatus ohne Dialog. Aufgerufen aus `SettingsViewModel`. |
| `ShowAsync` | `string title`, `string body`, `string identifier`, `IReadOnlyDictionary<string,string>? userInfo`, `CancellationToken` | `Task` | Zeigt eine lokale Mitteilung sofort; gleicher Identifier ersetzt statt zu duplizieren. Einziger Aufrufer: `NotificationService`. |

## `IAutoRefreshService`
Datei: `src/Reporter.Core/Interfaces/IAutoRefreshService.cs` — Implementierung: `AutoRefreshService`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `StartAsync` | `CancellationToken` | `Task` | Lädt Settings, startet Timer-Loop und ggf. Start-Abruf. Aufgerufen aus `App.OnStart`. |
| `ApplySettingsAsync` | `Settings settings` | `Task` | Wendet Settings an (Timer neu/stoppen). Aufgerufen aus `SettingsViewModel.PersistAsync`. |
| `StopAsync` | — | `Task` | Stoppt den Timer-Loop. |

## `INetworkStatusService` (Gateway-Muster-Referenz)
Datei: `src/Reporter.Core/Interfaces/INetworkStatusService.cs` — Implementierung: `NetworkStatusService` (Reporter-Projekt)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ConnectivityChanged` (Event) | — | `EventHandler?` | Online-/Offline-Übergänge, auf dem UI-Thread ausgelöst. |
| `IsOnline` (Property) | — | `bool` | Aktueller Verbindungsstatus; Guard in `FeedSyncService`, `AutoRefreshService` und den ViewModels. |

## Nicht vorhandene Interfaces
Es existiert **kein** `IBackgroundRefreshService`/`IBackgroundFetchService` (OS-Hintergrundabruf-Gateway) und **kein** `IAppStateService` (Vordergrund-/Hintergrundzustand) — beide kommen in der Anforderung nur als mögliche neue Arbeitsnamen vor.
