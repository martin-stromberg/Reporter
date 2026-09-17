<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces

Alle Contracts liegen in `src/Reporter.Core/Interfaces/` und sind MAUI-frei (`Reporter.Core` targetet `net10.0`). **Es existieren noch keine `IEmailService`-/`IDeviceInfoProvider`-Contracts.**

## `ISettingsRepository`
Datei: `src/Reporter.Core/Interfaces/ISettingsRepository.cs`
Implementiert von `SettingsRepository`; genutzt u. a. von `SettingsViewModel`, `NotificationService`, `RetentionCleanupService`, `MauiProgram.ApplyPersistedLanguage`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync` | `CancellationToken = default` | `Task<Settings>` | Singleton-Settings lesen (legt Default an, falls fehlend) |
| `SaveAsync` | `Settings settings` | `Task` | Singleton-Settings schreiben |

## `ISyncLogRepository`
Datei: `src/Reporter.Core/Interfaces/ISyncLogRepository.cs`
Implementiert von `SyncLogRepository`; geschrieben von `FeedSyncService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<SyncLog>>` | Alle Einträge, absteigend nach `StartedAt` — Report-Datenquelle |
| `GetByIdAsync` | `Guid id` | `Task<SyncLog?>` | Einzeleintrag |
| `AddAsync` | `SyncLog syncLog` | `Task` | Eintrag anlegen |
| `UpdateAsync` | `SyncLog syncLog` | `Task` | Eintrag aktualisieren |
| `DeleteAsync` | `Guid id` | `Task` | Eintrag löschen |

Keine `CancellationToken`-Parameter und keine Begrenzung/Paging — `GetAllAsync` liefert das vollständige Protokoll.

## `IFeedRepository`
Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs`
Implementiert von `FeedRepository`; genutzt u. a. von `FeedSyncService`, `FeedsViewModel`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Feed>>` | Alle Feeds (enthält `HealthStatus`/`LastCheckedAt`/`HealthLastChange`) |
| `GetByIdAsync` | `Guid id` | `Task<Feed?>` | Einzelfeed |
| `AddAsync` | `Feed feed` | `Task` | Feed anlegen |
| `UpdateAsync` | `Feed feed` | `Task` | Feed aktualisieren |
| `DeleteAsync` | `Guid id` | `Task` | Feed löschen |
| `GetAllWithDetailsAsync` | — | `Task<IReadOnlyList<FeedListItem>>` | Feeds inkl. `CategoryName`/`UnreadCount` |
| `GetByUrlAsync` | `string url` | `Task<Feed?>` | Feed per URL |

## `INetworkStatusService`
Datei: `src/Reporter.Core/Interfaces/INetworkStatusService.cs`
Implementiert von `NetworkStatusService` (MAUI `Connectivity`); genutzt von `FeedSyncService`, `AutoRefreshService`, `FeedsViewModel` u. a.

| Member | Typ | Zweck |
|--------|-----|-------|
| `ConnectivityChanged` | `event EventHandler?` | Online-/Offline-Wechsel (auf UI-Thread ausgelöst) |
| `IsOnline` | `bool` | Aktueller Internetzugang — mögliche Report-Angabe |

## `ILocalNotificationService`
Datei: `src/Reporter.Core/Interfaces/ILocalNotificationService.cs`
Implementiert von `LocalNotificationService` (iOS-only, sonst No-op); genutzt von `NotificationService` und `SettingsViewModel` (optionaler Ctor-Parameter). Referenzmuster für ein optionales Plattform-Gateway mit `IsSupported` + `CancellationToken`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsSupported` (Property) | — | `bool` | Plattformfähigkeit |
| `RequestAuthorizationAsync` | `CancellationToken = default` | `Task<bool>` | Autorisierung anfordern |
| `GetAuthorizationStatusAsync` | `CancellationToken = default` | `Task<NotificationAuthorizationStatus>` | Status ohne Prompt abfragen |
| `ShowAsync` | `string title`, `string body`, `string identifier`, `IReadOnlyDictionary<string, string>? userInfo = null`, `CancellationToken = default` | `Task` | Lokale Notification anzeigen |

## `IAppThemeService`
Datei: `src/Reporter.Core/Interfaces/IAppThemeService.cs`
Implementiert von `AppThemeService`; genutzt von `SettingsViewModel`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ApplyTheme` | `string? theme` | `void` | Theme anwenden |

## `INotificationService`
Datei: `src/Reporter.Core/Interfaces/INotificationService.cs`
Implementiert von `NotificationService`; genutzt von `FeedSyncService`. Referenz für den geplanten Orchestrierungs-Service (`DebugReportService`): Regelwerk in Core, Plattformwirkung über injizierte Gateways.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `NotifyNewItemsAsync` | `Feed feed`, `IReadOnlyList<Item> newItems`, `CancellationToken = default` | `Task` | Benachrichtigungsregeln auswerten und anzeigen |
