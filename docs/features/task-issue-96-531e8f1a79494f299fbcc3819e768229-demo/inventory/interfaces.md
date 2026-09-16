<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces

Alle Contracts liegen in `src/Reporter.Core/Interfaces/`. Ein neues `IDemoContentService`/`IDemoDataSeeder` würde hier angesiedelt; die Implementierung in `src/Reporter.Core/Services/` (kein MAUI-Verweis in `Reporter.Core`).

## `ICategoryRepository`

Datei: `src/Reporter.Core/Interfaces/ICategoryRepository.cs` — implementiert von `CategoryRepository`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | – | `Task<IReadOnlyList<Category>>` | Alle Kategorien (nach Name sortiert) — einzige Möglichkeit, eine Kategorie „News" per Namen zu finden |
| `GetAllWithFeedCountAsync` | – | `Task<IReadOnlyList<CategoryWithCount>>` | Kategorien inkl. Feed-Anzahl (Anzeige in `CategoriesViewModel`) |
| `GetByIdAsync` | `Guid id` | `Task<Category?>` | Kategorie per ID |
| `AddAsync` | `Category category` | `Task` | Kategorie anlegen — wirft `DbUpdateException` bei Unique-Verletzung auf `categories.name` |
| `UpdateAsync` | `Category category` | `Task` | Name aktualisieren |
| `DeleteAsync` | `Guid id` | `Task` | Kategorie löschen (`feeds.category_id` → `SetNull`) |

## `IFeedRepository`

Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs` — implementiert von `FeedRepository`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | – | `Task<IReadOnlyList<Feed>>` | Alle Feeds (nach Titel sortiert); u. a. von `FeedSyncService.SyncAllAsync` genutzt |
| `GetByIdAsync` | `Guid id` | `Task<Feed?>` | Feed per ID |
| `AddAsync` | `Feed feed` | `Task` | Feed anlegen — wirft bei Unique-Verletzung auf `feeds.url` |
| `UpdateAsync` | `Feed feed` | `Task` | Feed aktualisieren |
| `DeleteAsync` | `Guid id` | `Task` | Feed löschen (Items kaskadieren) |
| `GetAllWithDetailsAsync` | – | `Task<IReadOnlyList<FeedListItem>>` | Anzeige-Projektion inkl. `CategoryName`, `UnreadCount` |
| `GetByUrlAsync` | `string url` | `Task<Feed?>` | Dublettenprüfung per exakter URL — verwendet von `TryPersistNewFeedAsync`, wiederverwendbar für den Seed |

## `ISettingsRepository`

Datei: `src/Reporter.Core/Interfaces/ISettingsRepository.cs` — implementiert von `SettingsRepository`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync` | `CancellationToken` | `Task<Settings>` | Singleton-Settings lesen (legt Default-Zeile bei Fehlen an) |
| `SaveAsync` | `Settings settings` | `Task` | Singleton-Settings speichern (Feld-für-Feld-Mapping — neue Spalte müsste hier ergänzt werden) |

## `IDebugLogService`

Datei: `src/Reporter.Core/Interfaces/IDebugLogService.cs` — implementiert von `DebugLogService` (`src/Reporter.Core/Services/DebugLogService.cs`). Implementierungen werfen nie.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsEnabled` (Property) | – | `bool` | Ob die Sammlung aktiv ist (`Settings.DebugCollectionEnabled`) |
| `BeginSessionAsync` | `CancellationToken` | `Task` | Session-Reset + Enabled-Flag laden; muss **nach** `MigrateAsync` laufen — begründet die Reihenfolge in `App.OnStart` (Seed-Logging erst nach `BeginSessionAsync` möglich) |
| `SetEnabled` | `bool enabled` | `void` | Sammlung zur Laufzeit umschalten |
| `LogAsync` | `string category, string message, string? details, string level, CancellationToken` | `Task` | Eintrag schreiben; No-op wenn deaktiviert |

## `IAutoRefreshService`

Datei: `src/Reporter.Core/Interfaces/IAutoRefreshService.cs` — implementiert von `AutoRefreshService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `StartAsync` | `CancellationToken` | `Task` | Timer-Loop konfigurieren + Start-Abruf (`RefreshOnStartupEnabled && IsOnline` → `SyncAllAsync` fire-and-forget) — holt den geseedeten Feed beim ersten Start ab |
| `ApplySettingsAsync` | `Settings settings` | `Task` | Timer neu konfigurieren |
| `StopAsync` | – | `Task` | Timer stoppen |

## `IRetentionCleanupService`

Datei: `src/Reporter.Core/Interfaces/IRetentionCleanupService.cs` — implementiert von `RetentionCleanupService`. Vorbild für einen neuen Core-Service: ein Methoden-Contract, nur Repository-Abhängigkeiten, Singleton-Registrierung.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CleanupAsync` | `CancellationToken` | `Task<int>` | Abgelaufene gelesene Items löschen; Anzahl gelöschter Items |

## `IFeedSyncService`

Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs` — implementiert von `FeedSyncService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId, CancellationToken` | `Task<SyncResult>` | Einzelnen Feed synchronisieren |
| `SyncAllAsync` | `CancellationToken` | `Task<SyncResult>` | Alle Feeds synchronisieren (serialisiert via `_syncAllLock`) |

## `INotificationService`

Datei: `src/Reporter.Core/Interfaces/INotificationService.cs` — implementiert von `NotificationService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `NotifyNewItemsAsync` | `Feed feed, IReadOnlyList<Item> newItems, CancellationToken` | `Task` | Benachrichtigungsregeln auswerten (Feed-Schalter → globaler Schalter → Quiet Hours → Keywords → Summary vs. Einzel) und an `ILocalNotificationService` delegieren |

## `ILocalNotificationService`

Datei: `src/Reporter.Core/Interfaces/ILocalNotificationService.cs` — implementiert von `LocalNotificationService` (plattformspezifisch in `src/Reporter/Services/`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsSupported` (Property) | – | `bool` | `true` nur auf iOS |
| `RequestAuthorizationAsync` | `CancellationToken` | `Task<bool>` | Systemberechtigung anfordern |
| `GetAuthorizationStatusAsync` | `CancellationToken` | `Task<NotificationAuthorizationStatus>` | Status ohne Prompt abfragen |
| `ShowAsync` | `string title, string body, string identifier, IReadOnlyDictionary<string,string>? userInfo, CancellationToken` | `Task` | Lokale Benachrichtigung anzeigen (No-op außerhalb iOS) |

## `INetworkStatusService`

Datei: `src/Reporter.Core/Interfaces/INetworkStatusService.cs` — implementiert von `NetworkStatusService` (`src/Reporter/Services/`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ConnectivityChanged` (Event) | – | `EventHandler?` | Online/Offline-Wechsel; abonniert von `BaseViewModel.TrackConnectivity` |
| `IsOnline` (Property) | – | `bool` | Aktueller Netzwerkstatus — Gate für Start-Abruf und Feed-Syncs |
