<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces — Bestandsaufnahme

## `IFeedRepository`
Datei: `src/Reporter.Core/Interfaces/IFeedRepository.cs` — Implementierung: `FeedRepository` (`src/Reporter.Data/Repositories/FeedRepository.cs`); im ViewModel als `_feedRepository`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Feed>>` | Alle Feeds (in `FeedSyncService.SyncAllAsync` genutzt) |
| `GetByIdAsync` | `Guid id` | `Task<Feed?>` | Feed per ID (Sync lädt darüber den Feed) |
| `AddAsync` | `Feed feed` | `Task` | Neuen Feed speichern (`SaveAsync`, `SubscribeResultAsync`) |
| `UpdateAsync` | `Feed feed` | `Task` | Bestehenden Feed aktualisieren — ausreichend für „Umbenennen" (`Title`) und „Kategorie ändern" (`CategoryId`); keine Interface-Erweiterung nötig |
| `DeleteAsync` | `Guid id` | `Task` | Feed löschen (`DeleteAsync` im ViewModel) |
| `GetAllWithDetailsAsync` | — | `Task<IReadOnlyList<FeedListItem>>` | Liste mit `CategoryName`/`UnreadCount` für `LoadAsync` |
| `GetByUrlAsync` | `string url` | `Task<Feed?>` | Dublettenprüfung in `SaveAsync`/`SubscribeResultAsync` |

## `ICategoryRepository`
Datei: `src/Reporter.Core/Interfaces/ICategoryRepository.cs` — Implementierung: `CategoryRepository` (`src/Reporter.Data/Repositories/CategoryRepository.cs`); im ViewModel als `_categoryRepository`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAllAsync` | — | `Task<IReadOnlyList<Category>>` | Kategorien für `Categories`/`SelectedCategory`; auch Datenquelle für einen „Kategorie ändern"-Dialog |
| `GetAllWithFeedCountAsync` | — | `Task<IReadOnlyList<CategoryWithCount>>` | Kategorien-Übersichtsseite |
| `GetByIdAsync` | `Guid id` | `Task<Category?>` | Einzelne Kategorie |
| `AddAsync` | `Category category` | `Task` | Kategorie anlegen |
| `UpdateAsync` | `Category category` | `Task` | Kategorie umbenennen |
| `DeleteAsync` | `Guid id` | `Task` | Kategorie löschen |

## `IFeedSyncService`
Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs` — Implementierung: `FeedSyncService`; im ViewModel als `_feedSyncService`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId, CancellationToken` | `Task<SyncResult>` | Einzel-Feed-Sync (`RefreshCommand`); enthält die Platzhalter-Titel-Auflösung |
| `SyncAllAsync` | `CancellationToken` | `Task<SyncResult>` | Gesamt-Sync (`RefreshAllCommand`/Pull-to-Refresh) |

## `IFeedSearchService`
Datei: `src/Reporter.Core/Interfaces/IFeedSearchService.cs` — Implementierung: `FeedSearchService`; im ViewModel als `_feedSearchService` (Search-Partial).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchAsync` | `string query, CancellationToken` | `Task<IReadOnlyList<FeedSearchResult>>` | Verzeichnis- + Autodiscovery-Suche; wirft `FeedSearchUnavailableException`, wenn beide Quellen ausfallen |

## `INetworkStatusService`
Datei: `src/Reporter.Core/Interfaces/INetworkStatusService.cs` — an `BaseViewModel.TrackConnectivity` übergeben.

| Member | Typ | Zweck |
|--------|-----|-------|
| `ConnectivityChanged` | `event EventHandler?` | Online/Offline-Wechsel; `FeedsViewModel.OnConnectivityChanged` leert Fehlerkanäle und aktualisiert `SearchCommand.CanExecute` |
| `IsOnline` | `bool` (get) | Aktueller Online-Status; CanExecute-Bedingung von `SearchCommand` und Offline-Abbruch in `SyncAsync`/`SearchAsync` |

## `ILocalNotificationService`
Datei: `src/Reporter.Core/Interfaces/ILocalNotificationService.cs` — optional injiziert (`= null`).

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsSupported` | — | `bool` | Steuert `FeedsViewModel.NotificationsSupported` → Enabled/Opacity des `FeedNotificationsEnabled`-Switch und iOS-Hinweis |
| `RequestAuthorizationAsync` | `CancellationToken` | `Task<bool>` | Autorisierung anfordern |
| `GetAuthorizationStatusAsync` | `CancellationToken` | `Task<NotificationAuthorizationStatus>` | Status ohne Prompt abfragen |
| `ShowAsync` | `title, body, identifier, userInfo?, CancellationToken` | `Task` | Lokale Benachrichtigung anzeigen |
