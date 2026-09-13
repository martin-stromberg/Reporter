<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces — Bestandsaufnahme (Issue #77)

Alle Contracts liegen in `src\Reporter.Core\Interfaces\`; Implementierungen in `src\Reporter.Data\Repositories\` bzw. `src\Reporter\Services\` / `src\Reporter.Core\Services\`. **Nicht vorhanden** (für R2/R7 gefordert): `IFeedIconService`, `IDebugInfoService`/`IDiagnosticsService`, `IEmailService`.

## `IItemRepository` (R1, R2, R4)
Datei: `src\Reporter.Core\Interfaces\IItemRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | – | `Task<IReadOnlyList<Item>>` | Alle Items, `PublishedAt` absteigend |
| `GetByIdAsync` | `id` | `Task<Item?>` | Einzelnes Item |
| `AddAsync` / `UpdateAsync` / `DeleteAsync` | `item` / `id` | `Task` | CRUD |
| `GetUnreadByDateAsync` | – | `Task<IReadOnlyList<Item>>` | Ungelesene Items (unpaged) |
| `GetUnreadByDateAsync` | `page`, `pageSize`, `categoryId = null` | `Task<IReadOnlyList<ItemListItem>>` | Paged-Abfrage Ungelesen — **fest absteigend; R4 braucht Sortierparameter (Signaturbruch → `DelegatingItemRepository` + Fakes mitziehen)** |
| `GetUnreadCountAsync` | `categoryId = null` | `Task<int>` | Ungelesen-Zähler |
| `MarkAllAsReadAsync` | `categoryId = null` | `Task` | Alle ungelesenen als gelesen markieren |
| `ToggleSavedForLaterAsync` / `MarkAsReadAsync` | `id` | `Task` | Status-Umschaltung |
| `GetByFeedAsync` / `GetByCategoryAsync` | `feedId` / `categoryId` | `Task<IReadOnlyList<Item>>` | Items pro Feed/Kategorie |
| `GetSavedForLaterAsync` | `page`, `pageSize` | `Task<IReadOnlyList<ItemListItem>>` | Paged-Abfrage Später (fest absteigend) |
| `AddRangeAsync` | `items` | `Task` | Batch-Insert |
| `DeleteExpiredAsync` / `GetExpiredKeywordCandidatesAsync` / `DeleteRangeAsync` | `cutoff`/`ids`, `ct` | `Task<int>` / `Task<IReadOnlyList<Item>>` | Retention-/Keyword-Cleanup |

Implementierung: `ItemRepository` (`src\Reporter.Data\Repositories\ItemRepository.cs`); Test-Dekorator `DelegatingItemRepository` implementiert alle Member `virtual`.

## `IFeedRepository` (R2, R7)
Datei: `src\Reporter.Core\Interfaces\IFeedRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | – | `Task<IReadOnlyList<Feed>>` | Alle Feeds, nach Titel sortiert |
| `GetByIdAsync` / `GetByUrlAsync` | `id` / `url` | `Task<Feed?>` | Einzelfeed |
| `AddAsync` / `UpdateAsync` / `DeleteAsync` | `feed` / `id` | `Task` | CRUD — `UpdateAsync` kopiert nur bekannte Felder; neue `Feed`-Eigenschaften (Favicon) müssen hier nachgezogen werden |
| `GetAllWithDetailsAsync` | – | `Task<IReadOnlyList<FeedListItem>>` | Feed-Liste inkl. Kategorie/UnreadCount |

Implementierung: `FeedRepository` (Mappings `MapToModel`/`MapToEntity` ohne Bild-Felder).

## `ISettingsRepository` (R3, R4, R5, R7)
Datei: `src\Reporter.Core\Interfaces\ISettingsRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync` | `ct` | `Task<Settings>` | Singleton-Settings (legt Datensatz an, falls fehlend) |
| `SaveAsync` | `settings` | `Task` | Überschreibt immer den Singleton-Datensatz — `SaveAsync` kopiert nur bekannte Felder (neue Settings-Felder nachziehen) |

Implementierung: `SettingsRepository`.

## `ISyncLogRepository` (R7)
Datei: `src\Reporter.Core\Interfaces\ISyncLogRepository.cs`

`GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` — Zugriff auf das Sync-Protokoll; Quelle für das Debug-Paket. Implementierung: `SyncLogRepository`.

## `IFeedSearchService` (R2)
Datei: `src\Reporter.Core\Interfaces\IFeedSearchService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchAsync` | `query`, `ct` | `Task<IReadOnlyList<FeedSearchResult>>` | Directory + Autodiscovery; wirft `FeedSearchUnavailableException` |

Implementierung: `FeedSearchService` (`HttpClient`-basiert, Singleton in `MauiProgram`).

## `IFeedSyncService` (R3, R8)
Datei: `src\Reporter.Core\Interfaces\IFeedSyncService.cs`

`SyncFeedAsync(feedId, ct)` und `SyncAllAsync(ct)` → `Task<SyncResult>`. Implementierung: `FeedSyncService`.

## `IAutoRefreshService` (R3)
Datei: `src\Reporter.Core\Interfaces\IAutoRefreshService.cs`

`StartAsync(ct)`, `ApplySettingsAsync(settings)`, `StopAsync()`. Implementierung: `AutoRefreshService`; Start-Hook in `App.OnStart`.

## `INotificationService` (R8)
Datei: `src\Reporter.Core\Interfaces\INotificationService.cs`

`NotifyNewItemsAsync(feed, newItems, ct)` → `Task`. Implementierung: `NotificationService`.

## `ILocalNotificationService` (R8)
Datei: `src\Reporter.Core\Interfaces\ILocalNotificationService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsSupported` | – | `bool` | Plattform-Support (nur iOS `true`) |
| `RequestAuthorizationAsync` | `ct` | `Task<bool>` | Berechtigung anfragen |
| `GetAuthorizationStatusAsync` | `ct` | `Task<NotificationAuthorizationStatus>` | Status ohne Prompt |
| `ShowAsync` | `title`, `body`, `identifier`, `userInfo?`, `ct` | `Task` | Sofortige lokale Mitteilung, ersetzt gleiche Identifier |

Implementierung: `LocalNotificationService` (`#if IOS`, sonst No-Op). Gateway-Muster-Vorbild für `IEmailService` (R7).

## `INetworkStatusService` (R3)
Datei: `src\Reporter.Core\Interfaces\INetworkStatusService.cs`

Event `ConnectivityChanged`, Property `IsOnline`. Implementierung: `NetworkStatusService` (MAUI `Connectivity`). Wird für den `IsOnline`-Guard des Start-Syncs (R3) benötigt.

## `IAppThemeService` (R7-Muster)
Datei: `src\Reporter.Core\Interfaces\IAppThemeService.cs`

`ApplyTheme(string? theme)` → `void`. Implementierung: `AppThemeService` (`Application.UserAppTheme`) — einfachstes Gateway-Muster-Beispiel (Interface in Core, MAUI-Implementierung in App).

## `IRetentionCleanupService`, `IKeywordRepository`, `IKeywordFilter`, `IKeywordMatcher`, `ICategoryRepository`
Dateien: `src\Reporter.Core\Interfaces\*.cs`

Bestehend, von den Anforderungen nicht direkt betroffen (Retention-Cleanup läuft in `App.OnStart`; Keyword-Filter wirken in Sync und Benachrichtigungen).
