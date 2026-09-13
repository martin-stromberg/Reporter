<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik / Services — Bestandsaufnahme (Issue #77, R1–R8)

## `ReadingTimeEstimator` (R1)
Datei: `src/Reporter.Core/Services/ReadingTimeEstimator.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `EstimateText(string? contentHtml)` | `public static` | Entfernt HTML-Tags per `HtmlTagRegex`, zählt Wörter, `Math.Max(1, Round(wordCount / 200))` → formatierter Text über `AppResources.ArticleReadingTimeFormat`; `string.Empty` bei leerem Inhalt. **Es gibt keine Minuten-Rückgabe** — die 1-Minuten-Klemme ist nur im formatierten String erkennbar. |

Aufgerufen von: `ItemRepository.MapToListItem` (Liste) und `ArticleDetailViewModel.LoadAsync` (Detailansicht). Eine Änderung der Rückgabe wirkt auf beide Stellen; die Anforderung betrifft nur die Auflistung.

## `ItemRepository` (R1, R2, R4)
Datei: `src\Reporter.Data\Repositories\ItemRepository.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetUnreadByDateAsync(int page, int pageSize, Guid? categoryId = null)` | `public` | Paged-Abfrage Ungelesen: **fest** `OrderByDescending(i => i.PublishedAt).ThenBy(i => i.Id)` + `Skip`/`Take` (Zeilen 107–131). R4 braucht hier einen Richtungsparameter inkl. `ThenBy`-Umkehr. |
| `GetSavedForLaterAsync(int page, int pageSize)` | `public` | Gleiche feste absteigende Sortierung für **Später** (Zeilen 229–242). |
| `SelectListItemRows(IQueryable<ItemEntity>)` | `private static` | Projektion in `ItemListRow` inkl. `i.Feed.Title`, `i.Feed.CategoryId`, `i.Feed.Category?.Name` (Zeilen 297–313). **Kein Feed-Bild projiziert** — R2-Erweiterungspunkt. |
| `MapToListItem(ItemListRow)` | `private static` | Setzt `ImageUrl = ExtractImageUrl(ContentHtml)`, `Summary`, `ReadingTimeText = ReadingTimeEstimator.EstimateText(...)` (Zeilen 315–333). R1-Unterdrückung würde hier greifen (`ReadingTimeText = null` bei 1 Minute). |
| `ExtractImageUrl(string?)` | `private static` | Regex `<img … src='…'`; `null` bei bildlosem Inhalt (R2-Auslöser). |
| `ExtractSummary(string?)` | `private static` | Plain-Text-Auszug, max. 120 Zeichen + „…". |
| `ItemListRow` | `private sealed class` | Flache DB-Zeile (Zeilen 403–426); ohne Feed-Bild-Feld. |
| weitere | `public` | `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, `GetUnreadByDateAsync()` (unpaged), `GetUnreadCountAsync`, `MarkAllAsReadAsync` (`ExecuteUpdate`), `ToggleSavedForLaterAsync`, `MarkAsReadAsync`, `GetByFeedAsync`, `GetByCategoryAsync`, `AddRangeAsync`, `DeleteExpiredAsync`, `GetExpiredKeywordCandidatesAsync`, `DeleteRangeAsync` |

## `FeedSyncService` (R2, R8)
Datei: `src\Reporter.Core\Services\FeedSyncService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(Guid, CancellationToken)` | `public` | Offline-Guard (`INetworkStatusService.IsOnline`), legt `SyncLog` an, lädt Feed, ruft `RunSyncAsync`; Fehler → `FeedHealth.Error` + Log. |
| `SyncAllAsync(CancellationToken)` | `public` | Offline-Guard; iteriert alle Feeds sequenziell, aggregiert `SyncResult` (Status/NewItems/Message). Wird aufgerufen von `AutoRefreshService.RunLoopAsync`, `UnreadViewModel.RefreshAsync`, `FeedsViewModel` (Refresh/RefreshAll). |
| `RunSyncAsync(Feed, SyncLog, CancellationToken)` | `private` | Lädt Feed-Dokument (`HttpClient.GetStreamAsync` + `SyndicationFeed.Load`), `CollectNewItems` (Dedup via `GuidOrHash`, Keyword-Filter), `AddRangeAsync`, `DetermineStatus`, `ResolveFeedTitle`, `UpdateFeedHealthAsync`, `UpdateLogAsync`. **Ruft bei neuen Items immer `INotificationService.NotifyNewItemsAsync`** (Zeilen 169–180, fehlerisoliert) — R8-Anker: kein Unterschied zwischen Vordergrund-/Hintergrundabruf. |
| `UpdateFeedHealthAsync(Feed, string, string?)` | `private` | Persistiert `LastCheckedAt`, `HealthStatus`, `HealthLastChange`, ggf. aufgelösten Titel über `FeedRepository.UpdateAsync` (Zeilen 276–295) — R2-Nachrüstpunkt für nachträgliches Favicon beim ersten Sync. |

## `AutoRefreshService` (R3, R8)
Datei: `src\Reporter.Core\Services\AutoRefreshService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartAsync(CancellationToken)` | `public` | Lädt Settings, delegiert an `ApplySettingsAsync`. **Kein Sofort-Sync beim Start** — der erste Tick kommt erst nach `RefreshIntervalMinutes`. |
| `ApplySettingsAsync(Settings)` | `public` | Stoppt laufenden Loop (`_stateLock`), startet `RunLoopAsync` mit `PeriodicTimer` nur wenn `AutoRefreshEnabled`. Intervall geklemmt 1–1440 min. |
| `StopAsync()` | `public` | Stoppt den Loop. |
| `RunLoopAsync(TimeSpan, CancellationToken)` | `private` | `PeriodicTimer`; pro Tick `IsOnline`-Guard + `SyncAllAsync` (fehlerisoliert, `Debug.WriteLine`). |

Aufgerufen von `App.OnStart` (isolierter try/catch-Block, `src/Reporter/App.xaml.cs` Zeilen 77–86). Für R3 existiert kein Start-Sync-Pfad.

## `NotificationService` (R8)
Datei: `src\Reporter.Core\Services\NotificationService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `NotifyNewItemsAsync(Feed, IReadOnlyList<Item>, CancellationToken)` | `public` | Gates: `feed.NotificationsEnabled` → `settings.NotificationsEnabled` → `IsQuietHoursActive` → Keyword-Filter; dann pro Item `ILocalNotificationService.ShowAsync` bzw. bei `NotificationSummaryEnabled` eine Sammel-Meldung (`BuildSummaryIdentifier` SHA-256 über Item-IDs). |

Publizierte Events: keine. Abonnierte Events: keine. **Kein App-Zustands-Check** (Vordergrund/Hintergrund) — R8-Unterdrückung wäre hier oder im Delegate anzusiedeln.

## `LocalNotificationService` (R8)
Datei: `src\Reporter\Services\LocalNotificationService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `IsSupported` | `public` Property | `true` nur unter `#if IOS`; Windows/Android/MacCatalyst → `false` (No-Op). |
| `RequestAuthorizationAsync` | `public` | iOS: `EnsureAuthorizedAsync` (fragt bei `NotDetermined` Alert+Badge+Sound an). |
| `GetAuthorizationStatusAsync` | `public` | iOS: `UNUserNotificationCenter.GetNotificationSettingsAsync` → `MapStatus`. |
| `ShowAsync(title, body, identifier, userInfo, ct)` | `public` | iOS: `UNMutableNotificationContent` + `UNNotificationRequest` (ohne Trigger = sofort); ersetzt gleiche Identifier. |

## `NotificationDelegate` (R8, iOS)
Datei: `src\Reporter\Platforms\iOS\NotificationDelegate.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `WillPresentNotification` | `public override` | **Gibt immer `Banner \| List \| Sound` zurück** — Vordergrund-Banner werden aktuell gezeigt (R8-Anker). |
| `DidReceiveNotificationResponse` | `public override async void` | Tap-Handling: `itemId` → `articledetail`-Route, `feedId` → `//unread`, Fallback `Launcher.OpenAsync(link)`. |

Registriert in `Platforms/iOS/AppDelegate.cs` `FinishedLaunching` (`UNUserNotificationCenter.Current.Delegate = _notificationDelegate`).

## `FeedsViewModel` — Search-Teil (R2)
Datei: `src\Reporter.Core\ViewModels\FeedsViewModel.Search.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `TryPersistNewFeedAsync(string url, string title)` | `private` | **Zentraler Persist-Pfad aller drei Anlage-Wege** (`DirectAddAsync`, `OfferDirectAddAsync`, `SubscribeResultAsync`): Duplikat-Check via `_feedRepository.GetByUrlAsync`, dann `AddAsync` mit Defaults (`HealthStatus = FeedHealth.Ok`, `NotificationsEnabled = true`). Signatur kennt nur `url`/`title` — `FeedSearchResult.SiteUrl` wird aktuell verworfen. |
| `SearchAsync` | `private` | `TryResolveSearchUrl` → `IFeedSearchService.SearchAsync` → `SearchResults`; `OfferDirectAdd` bei leerem Ergebnis/Fehler + Direkt-URL. |
| `OfferDirectAddAsync` / `DirectAddAsync` / `SubscribeResultAsync` | `private` | Die drei Anlage-Wege, enden in `TryPersistNewFeedAsync` + `FinishAddFlowAsync`. |

Konstruktor-Abhängigkeiten (`FeedsViewModel.cs`): `IFeedRepository`, `ICategoryRepository`, `IFeedSyncService`, `IFeedSearchService`, `INetworkStatusService`, `ILocalNotificationService?` — **kein** Icon-/Bild-Service vorhanden.

## `FeedSearchService` (R2)
Datei: `src\Reporter.Core\Services\FeedSearchService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SearchAsync(string, CancellationToken)` | `public` | feedsearch.dev-Directory (`SearchDirectoryAsync`, `favicon=false` in der Query) → Fallback `DiscoverFeedsAsync`; 2 s Zeitbudget; `FeedSearchUnavailableException` wenn beide Quellen scheitern. |
| `DiscoverFeedsAsync` | `private` | HTML-Autodiscovery: `ExtractFeedLinks` (rel=alternate + Feed-MediaType), sonst `ProbeStandardPathsAsync` (`/feed`, `/rss`, …); setzt `SiteUrl` aus dem Authority-Teil der Antwort-URL. |
| `ExtractFeedLinks(string html)` | `private static` | `LinkTagPattern`/`AttributePattern`-Regex über dem `<head>`-Bereich — wiederverwendbares Parsing-Muster für die Favicon-Suche (`rel=icon` o. ä.) in R2. |

**Fehlt:** kein Favicon-Discovery (Directory-Request deaktiviert es sogar explizit: `favicon=false`), kein `IFeedIconService`/`FeedIconService`.

## `SettingsViewModel` (R3, R4, R5, R7)
Datei: `src\Reporter.Core\ViewModels\SettingsViewModel.cs`

| Methode / Property | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `AutoRefreshEnabled`, `SelectedRefreshInterval`, `AutoMarkReadEnabled`, `SelectedAutoMarkReadDelay`, `NotificationsEnabled`, `NotificationSummaryEnabled`, `QuietHoursEnabled`, `QuietHoursStart/End`, `SelectedTheme`, `SelectedLanguage`, `RetentionDays` | `public` | Bindbare Optionen; Setter → `PersistOnChange()` → `_ = PersistAsync()` (Sofort-Persistierung, `_isLoading`-Guard). |
| `LoadAsync` | `private` | `_isLoading = true` → `ISettingsRepository.GetAsync` → alle Properties setzen → `_isLoading = false` → `RefreshNotificationPermissionAsync`. |
| `PersistAsync` | `private` | Baut neues `Settings`-Objekt aus den Properties, `SaveAsync`, `ApplyTheme` bei Theme-Änderung, `ApplySettingsAsync` bei AutoRefresh-/Intervall-Änderung. |
| `SelectedLanguage`-Setter | `public` | Nur `PersistOnChange()` — **kein Änderungsvergleich, kein `LanguageRestartHintVisible`-Flag** (R5). |
| `RequestNotificationAuthorizationAsync`, `RefreshNotificationPermissionAsync`, `ApplyAuthorizationStatus` | `private` | iOS-Berechtigungsfluss (`NotificationAuthorizationDenied`-Event → `SettingsPage`-Dialog). |
| Keywords | `public`/`private` | `AddKeywordAsync`/`RemoveKeywordAsync` + `Keywords`-Collection. |

Abhängigkeiten: `ISettingsRepository`, `IKeywordRepository`, `IAutoRefreshService`, `IAppThemeService`, `TimeProvider?`, `ILocalNotificationService?`. **Fehlt für R7:** E-Mail-/Debug-Gateway-Abhängigkeit und Versand-Command.

## `UnreadViewModel` (R4)
Datei: `src\Reporter.Core\ViewModels\UnreadViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadPageAsync(int page, bool append)` | `private` | Ruft `_itemRepository.GetUnreadByDateAsync(page, PageSize=20, categoryId)` — **kein Sortierparameter, keine `ISettingsRepository`-Abhängigkeit** (R4-Anker). |
| `LoadAsync` / `LoadMoreAsync` / `RefreshAsync` / `SelectCategoryAsync` | `private` | Laden/Paging/Pull-to-Refresh/Kategoriefilter; alle enden in `LoadPageAsync`. |
| `MarkAllReadAsync` / `ToggleSavedAsync` / `MarkReadAsync` | `private` | Mutieren `Articles` in-place (`CopyWith`). |

Abhängigkeiten: `IItemRepository`, `ICategoryRepository`, `IFeedSyncService`, `INetworkStatusService` (via `BaseViewModel.TrackConnectivity`).

## `LaterViewModel` (R4-Kontext)
Datei: `src\Reporter.Core\ViewModels\LaterViewModel.cs`

Nutzt `_itemRepository.GetSavedForLaterAsync(_currentPage, PageSize)` — feste absteigende Sortierung; laut Anforderung unverändert.

## `ArticleDetailViewModel` (R1/R2-Kontext)
Datei: `src\Reporter\ViewModels\ArticleDetailViewModel.cs`

`LoadAsync` setzt `ReadingTime = ReadingTimeEstimator.EstimateText(item.ContentHtml)` (Zeile 297, Detailansicht bleibt von R1 unberührt) und **`FeedIconUrl = string.Empty`** (Zeile 295) — die Detailseite bindet bereits ein Feed-Icon-`Image` (`ArticleDetailPage.xaml` Zeilen 66–67), das nie befüllt wird; potenzieller R2-Anschlusspunkt.

## `App` / `MauiProgram` (R3, R7, R8)
Dateien: `src\Reporter\App.xaml.cs`, `src\Reporter\MauiProgram.cs`

`App.OnStart` (Zeilen 34–87): `MigrateAsync` → isolierte try/catch-Blöcke für `IRetentionCleanupService.CleanupAsync`, `ISettingsRepository`+`IAppThemeService.ApplyTheme`, `INetworkStatusService`-Auflösung, `IAutoRefreshService.StartAsync`. **Kein `SyncAllAsync`-Block** — R3-Erweiterungspunkt.

`MauiProgram.CreateMauiApp` (Zeilen 46–77): alle Repositories/Services/ViewModels als Singletons, `ArticleDetailViewModel`/Pages als Transients; `HttpClient` mit 30 s Timeout; `ApplyPersistedLanguage` vor `CreateWindow` (migriert synchron + `AppCulture.Apply`). Neue Services (R2/R7) wären hier als Singletons zu registrieren.
