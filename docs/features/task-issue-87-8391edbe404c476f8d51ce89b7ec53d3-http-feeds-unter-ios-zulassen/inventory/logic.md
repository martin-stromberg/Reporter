<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik — Bestandsaufnahme

## `FeedSyncService`

Datei: `src/Reporter.Core/Services/FeedSyncService.cs` — implementiert `IFeedSyncService`.

Abhängigkeiten (Konstruktor, `:41-61`): `IFeedRepository`, `IItemRepository`, `ISyncLogRepository`, `HttpClient`, `INotificationService`, `INetworkStatusService`, `IKeywordFilter`, `IFeedIconService`, optional `IDebugLogService?`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(Guid, CancellationToken)` | public | Offline-Check (`:66`), legt `SyncLog` mit `FeedId` an (`:71-78`), lädt Feed, ruft `RunSyncAsync`; catch (`:92-103`) schreibt `HealthStatus = Error` via `UpdateFeedHealthAsync` und `SyncLog.Message = $"Synchronization failed: {ex.Message}"` via `UpdateLogAsync`; loggt `ex.ToString()` in `IDebugLogService` (Kategorie `Sync`, Level `Error`); gibt `SyncResult(Error, 0, message)` zurück |
| `SyncAllAsync(CancellationToken)` | public | Offline-Check, iteriert alle Feeds via `SyncFeedAsync`, aggregiert `SyncResult` mit `"Titel: Message"`-Liste (`:107-143`) |
| `RunSyncAsync(Feed, SyncLog, CancellationToken)` | private | **Kernstelle:** `_httpClient.GetStreamAsync(feed.Url, cancellationToken)` (`:155`) — kein Scheme-Handling, kein Retry; parst via `XmlReader`/`SyndicationFeed.Load`; `CollectNewItems` + Keyword-Filter; `DetermineStatus`; `ResolveFeedTitle`; Favicon-Backfill via `TryFindFaviconUrlAsync` (`:182`); `UpdateFeedHealthAsync` + `UpdateLogAsync`; Benachrichtigung isoliert in eigenem catch (`:189-202`) |
| `CollectNewItems(...)` | private | Dedupe via `GuidOrHash`-`HashSet`, Keyword-Filterung, baut `Item`-Liste |
| `ResolveFeedTitle(Feed, SyndicationFeed)` | private static | Ersetzt Placeholder-Titel durch Dokumenttitel |
| `IsHostPlaceholderTitle(Feed)` | private static | Prüft Titel == Host der Feed-URL |
| `DetermineStatus(...)` | private static | `Warning` bei >50 % Item-Verlust oder >30 Tage ohne neue Items, sonst `Ok` |
| `UpdateFeedHealthAsync(Feed, string, string?, string?)` | private | Schreibt `Feed` via `_feedRepository.UpdateAsync` (`:299-319`): `LastCheckedAt`, `HealthStatus`, `HealthLastChange` (nur bei Änderung via `FeedHealth.Changed`), `Title`, `FaviconUrl` — **keine** Fehlermeldungs-Eigenschaft |
| `TryFindFaviconUrlAsync(string, SyndicationFeed, CancellationToken)` | private | Ermittelt `alternate`-Link des Feeddokuments, ruft `IFeedIconService.TryFindFaviconUrlAsync` |
| `UpdateLogAsync(SyncLog, string, string?)` | private | Schreibt `SyncLog` (`Status`, `Message`, `FinishedAt`) via `_syncLogRepository.UpdateAsync` (`:334-345`) |
| `GetContentHtml` / `NormalizeGuidOrHash` | private static | Content-Extraktion bzw. GUID/SHA256-Fallback |

Publizierte Events: keine. Abonnierte Events: keine.

## `FeedsViewModel`

Dateien: `src/Reporter.Core/ViewModels/FeedsViewModel.cs` und `FeedsViewModel.Search.cs` (partial). Erbt `BaseViewModel` (`IsOnline`, `TrackConnectivity`, `OnConnectivityChanged`).

Abhängigkeiten (Konstruktor, `FeedsViewModel.cs:48-76`): `IFeedRepository`, `ICategoryRepository`, `IFeedSyncService`, `IFeedSearchService`, `IFeedIconService`, `INetworkStatusService`, optional `ILocalNotificationService?`. **Keine** `ISyncLogRepository`-Abhängigkeit.

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `LoadCommand`, `SaveCommand`, `EditCommand`, `DeleteCommand`, `RefreshCommand`, `RefreshAllCommand`, `OpenAddFormCommand`, `CloseAddFormCommand`, `SearchCommand`, `DirectAddCommand`, `SubscribeResultCommand`, `CloseSearchResultsCommand` | public | `AsyncRelayCommand`/`RelayCommand`-Instanzen |
| `NewUrl`, `NewTitle`, `FeedNotificationsEnabled`, `ErrorMessage`/`HasError`, `SyncErrorMessage`/`HasSyncError`, `SearchErrorMessage`/`HasSearchError`, `ShowAddForm`, `IsEditMode`, `IsSyncing`, `IsSearching`, `SelectedFeed`, `Categories`, `Feeds`, `SearchResults`, `ShowSearchResults`, `NotificationsSupported` | public | Bindbare Properties |
| `ConfirmDirectAddAsync` | public (`Func<string, Task<bool>>?`) | Callback aus dem Code-Behind (`FeedsPage.ConfirmDirectAddAsync`) — vorhandenes Muster für UI-Interaktion ohne UI-Abhängigkeit im ViewModel |
| `SyncAsync(Func<Task<SyncResult>>)` | private | Reentrancy-Guard `_isSyncInProgress`; bei `result.Status == FeedHealth.Error` oder Exception: `SyncErrorMessage = AppResources.SyncStatusError` (`:423-435`) — **generischer Text, konkrete `SyncResult.Message`/`SyncLog.Message` werden nicht angezeigt**; danach `LoadAsync` |
| `SaveAsync()` | private | Edit-Pfad: validiert `NewUrl` via `IsValidFeedUrl` (akzeptiert `http` **und** `https`), Duplikatprüfung `GetByUrlAsync` (exakter String-Vergleich), `UpdateAsync` via `ToFeed` — **kein** HTTPS-Upgrade |
| `IsValidFeedUrl(string)` | private static | `Uri.TryCreate` + Scheme `http`/`https` (`:445-449`) |
| `OnConnectivityChanged(bool)` | protected override | Setzt `SyncErrorMessage`/`SearchErrorMessage` zurück |
| `RenameFeedAsync`, `ChangeFeedCategoryAsync` | public | Partial-Updates via `ToFeed` |
| `ToFeed(FeedListItem, url, title, categoryId, notificationsEnabled)` | private static | Rekonstruiert `Feed` aus `FeedListItem` (`:513-527`) |
| `MakeUniqueOptionLabels(IReadOnlyList<string>)` | public static | Kollisionsfreie Action-Sheet-Labels (`:538-554`) |
| `SearchAsync()` | private (Search.cs) | `TryResolveSearchUrl` → `_feedSearchService.SearchAsync`; Fehler → `HandleSearchFailure` (`FeedSearchUnavailable`/`FeedSearchUnavailableRetry`); bei leerem Ergebnis + Direkt-URL `OfferDirectAddAsync` |
| `TryResolveSearchUrl(string, out string, out bool)` | private static | Direkte URL unverändert (inkl. `http`), sonst `TryNormalizeDomainUrl` |
| `TryNormalizeDomainUrl(string, out string)` | private static | Bare Domain → `"https://" + input` (`:217-234`) — einzige vorhandene Scheme-Hochstufung |
| `DirectAddAsync()`, `OfferDirectAddAsync(string)`, `SubscribeResultAsync(FeedSearchResult?)` | private | Add-Pfade; alle gehen durch `TryPersistNewFeedAsync` |
| `TryPersistNewFeedAsync(string url, string title, string? siteUrl)` | private | Duplikatprüfung `GetByUrlAsync` (scheme-sensitiv, exakter Vergleich), Favicon via `TryFindFaviconUrlAsync`, `AddAsync` mit `HealthStatus = FeedHealth.Ok` (`:301-329`) — persistiert URL **unverändert**, kein Upgrade |
| `TryFindFaviconUrlAsync(string, string?)` | private | Nur online; delegiert an `IFeedIconService.TryFindFaviconUrlAsync` |
| `FinishAddFlowAsync`, `CloseSearchResults`, `HandleSearchFailure`, `IsStaleInput` | private | Flow-/Fehlerbehandlung |

## `FeedSearchService`

Datei: `src/Reporter.Core/Services/FeedSearchService.cs` — implementiert `IFeedSearchService`. Nutzt denselben `HttpClient`-Singleton.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SearchAsync(string, CancellationToken)` | public | 2-s-Budget (`SearchTimeout`); `SearchDirectoryAsync` (feedsearch.dev, `https`-Endpoint) → Fallback `DiscoverFeedsAsync`; beide fehlgeschlagen → `FeedSearchUnavailableException`; Dedupe nach `FeedUrl` (OrdinalIgnoreCase — scheme-sensitiv gegenüber http/https-Varianten), Sortierung `MatchKind`, `Score`, `FeedUrl` |
| `SearchDirectoryAsync` | private | JSON-Parsing; liefert `FeedSearchResult` mit `FeedUrl` aus dem Verzeichnis — **kann `http`-URLs enthalten** |
| `DiscoverFeedsAsync` | private | GET auf Query-URL; Feed-Content-Type → `ExactUrl`-Treffer; HTML → `ExtractFeedLinks` (akzeptiert `http`/`https`, `:194-195`); sonst `ProbeStandardPathsAsync` |
| `ProbeStandardPathsAsync` | private | Probiert `StandardFeedPaths` (`/feed`, `/rss`, …) am Origin; `HttpRequestException` pro Kandidat isoliert |
| `ExtractFeedLinks`, `IsFeedMediaType`, `GetString` | private static | HTML-/JSON-Hilfen |

## `FeedIconService`

Datei: `src/Reporter.Core/Services/FeedIconService.cs` — implementiert `IFeedIconService`. Nutzt denselben `HttpClient`-Singleton; bei `http`-Feeds unter iOS ebenfalls ATS-blockiert (Fehlschlag isoliert → `null`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `FindFaviconUrlAsync(string, CancellationToken)` | public | Akzeptiert `http`/`https` (`:43-44`); GET Site-HTML → `ExtractIconLinks`; `/favicon.ico`-Fallback; `VerifyAsync` pro Kandidat |
| `TryFindFaviconUrlAsync(string, string?, CancellationToken)` | public | `FeedSiteResolver.ResolveSiteUrl` → `FindFaviconUrlAsync`; fängt alle Exceptions → `null` (`:95-99`) |
| `ExtractIconLinks`, `VerifyAsync` | private | Link-Tag-Parsing (akzeptiert `http`/`https`, `:134-135`); Kandidaten-Verifikation |

## `FeedSiteResolver`

Datei: `src/Reporter.Core/Services/FeedSiteResolver.cs` — `static`; `ResolveSiteUrl(feedUrl, siteUrl)` fällt auf die Authority der Feed-URL zurück (`GetLeftPart(UriPartial.Authority)`) — bei `http`-Feed-URL ist auch die Site-URL `http`.

## `FeedTitleFallback`

Datei: `src/Reporter.Core/Services/FeedTitleFallback.cs` — `static`; `GetFallbackTitle(url)` (letztes Pfadsegment → Host → URL), `IsFileNamePlaceholderTitle(title, url)`. Wird in `FeedsViewModel` (Add-Pfade) und `FeedSyncService.ResolveFeedTitle` genutzt.

## `MauiProgram`

Datei: `src/Reporter/MauiProgram.cs`

- `AddSingleton<HttpClient>(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(30) })` (`:55`) — ein gemeinsamer `HttpClient` für `FeedSyncService`, `FeedSearchService`, `FeedIconService`; **kein** plattformspezifischer Handler konfiguriert (auf iOS/MacCatalyst greift der MAUI-Default `NSUrlSessionHandler` mit ATS-Policy).
- `AddSingleton<ISyncLogRepository, SyncLogRepository>` (`:53`) — im Container registriert, aktuell nicht von `FeedsViewModel` konsumiert.
- `context.Database.Migrate()` beim Start in `ApplyPersistedLanguage` (`:110`).
