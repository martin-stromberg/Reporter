# Interfaces — Bestandsaufnahme

Bestehende Contracts in `src/Reporter.Core/Interfaces/`. **Ein `INetworkStatusService`/`IConnectivityService` existiert nicht** — es müsste nach dem Gateway-Muster von `IAppThemeService`/`ILocalNotificationService` neu angelegt werden (Interface in `Reporter.Core`, Implementierung im MAUI-Projekt `Reporter`).

## `IFeedSyncService`

Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs` — implementiert von `FeedSyncService` (`src/Reporter.Core/Services/FeedSyncService.cs`), aufgerufen von `UnreadViewModel.RefreshAsync`, `FeedsViewModel.RefreshAsync`/`RefreshAllAsync` und `AutoRefreshService.RunLoopAsync`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId`, `CancellationToken` | `Task<SyncResult>` | Einzelnen Feed synchronisieren |
| `SyncAllAsync` | `CancellationToken` | `Task<SyncResult>` | Alle Feeds synchronisieren |

`SyncResult` ist ein Record (`Status: string` mit `FeedHealth`-Werten, `NewItems: int`, `Message: string?`) — `src/Reporter.Core/Services/SyncResult.cs`.

## `IAutoRefreshService`

Datei: `src/Reporter.Core/Interfaces/IAutoRefreshService.cs` — implementiert von `AutoRefreshService`, aufgerufen von `App.OnStart` und `SettingsViewModel.PersistAsync`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `StartAsync` | `CancellationToken` | `Task` | Settings laden und Timer ggf. starten |
| `ApplySettingsAsync` | `Settings` | `Task` | Timer mit neuem Intervall neu starten oder stoppen |
| `StopAsync` | – | `Task` | Timer stoppen |

## `ISettingsRepository`

Datei: `src/Reporter.Core/Interfaces/ISettingsRepository.cs` — implementiert von `SettingsRepository` (`src/Reporter.Data/Repositories/SettingsRepository.cs`), genutzt u. a. von `ArticleDetailViewModel`, `SettingsViewModel`, `AutoRefreshService`, `NotificationService`, `App.OnStart`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync` | `CancellationToken` | `Task<Settings>` | Singleton-Einstellungen lesen (legt Default-Record an, falls fehlend) |
| `SaveAsync` | `Settings` | `Task` | Singleton-Einstellungen schreiben |

## `IAppThemeService` (Gateway-Referenzmuster)

Datei: `src/Reporter.Core/Interfaces/IAppThemeService.cs` — implementiert von `AppThemeService` (`src/Reporter/Services/AppThemeService.cs`), registriert in `MauiProgram.CreateMauiApp` (Zeile 55), genutzt von `App.OnStart` und `SettingsViewModel`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ApplyTheme` | `string? theme` | `void` | `Application.Current.UserAppTheme` anhand `SettingsValues.Theme*` setzen |

## `ILocalNotificationService` (Gateway-Referenzmuster)

Datei: `src/Reporter.Core/Interfaces/ILocalNotificationService.cs` — implementiert von `LocalNotificationService` (`src/Reporter/Services/LocalNotificationService.cs`), in ViewModels optional (`ILocalNotificationService?`) injiziert.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsSupported` (Property) | – | `bool` | Plattform unterstützt lokale Benachrichtigungen |
| `RequestAuthorizationAsync` | `CancellationToken` | `Task<bool>` | Systemberechtigung anfragen |
| `GetAuthorizationStatusAsync` | `CancellationToken` | `Task<NotificationAuthorizationStatus>` | Berechtigungsstatus abfragen |
| `ShowAsync` | `title`, `body`, `identifier`, `IReadOnlyDictionary<string,string>? userInfo`, `CancellationToken` | `Task` | Lokale Benachrichtigung anzeigen |

## `INotificationService`

Datei: `src/Reporter.Core/Interfaces/INotificationService.cs` — implementiert von `NotificationService` (`src/Reporter.Core/Services/NotificationService.cs`), aufgerufen von `FeedSyncService.RunSyncAsync`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `NotifyNewItemsAsync` | `Feed feed`, `IReadOnlyList<Item> newItems`, `CancellationToken` | `Task` | Benachrichtigungsregeln auswerten und an `ILocalNotificationService` delegieren |

## Repositories (Lokaldaten, für die Offline-Lesepfade relevant)

| Interface | Datei | Zentrale Methoden |
|-----------|-------|-------------------|
| `IItemRepository` | `src/Reporter.Core/Interfaces/IItemRepository.cs` | `GetByIdAsync`, `GetUnreadByDateAsync` (paged + Kategoriefilter), `GetUnreadCountAsync`, `GetSavedForLaterAsync`, `MarkAsReadAsync`, `MarkAllAsReadAsync`, `ToggleSavedForLaterAsync`, `GetByFeedAsync`, `GetByGuidOrHashAsync`, `DeleteExpiredAsync`, `GetExpiredKeywordCandidatesAsync`, `DeleteRangeAsync` u. a. |
| `IFeedRepository` | `src/Reporter.Core/Interfaces/IFeedRepository.cs` | `GetAllAsync`, `GetByIdAsync`, `GetByUrlAsync`, `GetAllWithDetailsAsync` (`FeedListItem`), `AddAsync`, `UpdateAsync`, `DeleteAsync` |
| `ICategoryRepository` | `src/Reporter.Core/Interfaces/ICategoryRepository.cs` | `GetAllAsync`, `GetAllWithFeedCountAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` |
| `ISyncLogRepository` | `src/Reporter.Core/Interfaces/ISyncLogRepository.cs` | Schreiben/Lesen der `sync_logs`-Einträge (von `FeedSyncService` genutzt) |
| `IKeywordRepository` / `IKeywordMatcher` / `IRetentionCleanupService` | `src/Reporter.Core/Interfaces/` | Keyword-Filter und Aufbewahrungs-Bereinigung (nur indirekt relevant) |

Alle Repositories arbeiten ausschließlich lokal über `IDbContextFactory<ReporterDbContext>` (SQLite `reporter.db`) — die Lesepfade sind damit bereits offline-fähig.
