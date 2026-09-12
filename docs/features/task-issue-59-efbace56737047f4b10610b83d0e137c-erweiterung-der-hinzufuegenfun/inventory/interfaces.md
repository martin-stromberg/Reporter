<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces

Betroffene Contracts in `Reporter.Core/Interfaces`. Ein `IFeedSearchService` (Anforderung) existiert nicht.

## `IFeedRepository`
Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs` — implementiert von `FeedRepository` (`src/Reporter.Data/Repositories/FeedRepository.cs`), als Singleton in `MauiProgram` registriert; konsumiert von `FeedsViewModel` und `FeedSyncService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Feed>>` | Alle Feeds |
| `GetByIdAsync` | `Guid id` | `Task<Feed?>` | Feed per ID |
| `AddAsync` | `Feed feed` | `Task` | Feed anlegen (wird beim Abonnieren eines Suchtreffers weiterhin genutzt) |
| `UpdateAsync` | `Feed feed` | `Task` | Feed aktualisieren |
| `DeleteAsync` | `Guid id` | `Task` | Feed löschen |
| `GetAllWithDetailsAsync` | — | `Task<IReadOnlyList<FeedListItem>>` | Feeds mit Anzeige-Details (`CategoryName`, `UnreadCount`) |
| `GetByUrlAsync` | `string url` | `Task<Feed?>` | Feed per URL — Dublettenprüfung in `FeedsViewModel.SaveAsync` |

## `IFeedSyncService`
Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs` — implementiert von `FeedSyncService`; konsumiert von `FeedsViewModel` (`RefreshCommand`/`RefreshAllCommand`) und `AutoRefreshService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId, CancellationToken = default` | `Task<SyncResult>` | Einzelnen Feed synchronisieren (erster Abruf nach dem Abonnieren) |
| `SyncAllAsync` | `CancellationToken = default` | `Task<SyncResult>` | Alle Feeds synchronisieren |

## `INetworkStatusService`
Datei: `src/Reporter.Core/Interfaces/INetworkStatusService.cs` — implementiert von `Reporter.Services.NetworkStatusService` (MAUI-App); konsumiert von `BaseViewModel` (`TrackConnectivity`), `FeedsViewModel`, `FeedSyncService`, `AutoRefreshService`. Grundlage des Offline-Verhaltens.

| Member | Typ | Zweck |
|--------|-----|-------|
| `ConnectivityChanged` | `event EventHandler?` | Wird bei Online/Offline-Wechsel ausgelöst (UI-Thread) |
| `IsOnline` | `bool` (get) | Aktueller Internet-Status |

## `ICategoryRepository`
Datei: `src/Reporter.Core/Interfaces/ICategoryRepository.cs` — implementiert von `CategoryRepository`; konsumiert von `FeedsViewModel` (Kategorie-Picker).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Category>>` | Alle Kategorien (Picker-Datenquelle) |
| `GetAllWithFeedCountAsync` | — | `Task<IReadOnlyList<CategoryWithCount>>` | Kategorien mit Feed-Anzahl |
| `GetByIdAsync` | `Guid id` | `Task<Category?>` | Kategorie per ID |
| `AddAsync` | `Category category` | `Task` | Kategorie anlegen |
| `UpdateAsync` | `Category category` | `Task` | Kategorie aktualisieren |
| `DeleteAsync` | `Guid id` | `Task` | Kategorie löschen |

## `ILocalNotificationService`
Datei: `src/Reporter.Core/Interfaces/ILocalNotificationService.cs` — implementiert von `Reporter.Services.LocalNotificationService`; in `FeedsViewModel` optional (nullable) injiziert, nur `IsSupported` wird genutzt.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsSupported` | — (Property) | `bool` | Plattform-Support für lokale Benachrichtigungen |
| `RequestAuthorizationAsync` | `CancellationToken = default` | `Task<bool>` | Berechtigung anfordern |
| `GetAuthorizationStatusAsync` | `CancellationToken = default` | `Task<NotificationAuthorizationStatus>` | Status ohne Prompt abfragen |
| `ShowAsync` | `string title, string body, string identifier, IReadOnlyDictionary<string,string>? userInfo = null, CancellationToken = default` | `Task` | Lokale Benachrichtigung anzeigen |

## Nicht vorhanden

- `IFeedSearchService` mit `SearchAsync(string query, CancellationToken)` → `IReadOnlyList<FeedSearchResult>` — in `requirement.md` als neu gefordert.
