<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logikklassen und Services

## `FeedSearchService`

Datei: `src/Reporter.Core/Services/FeedSearchService.cs`

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `DirectoryEndpoint` | private const string (Zeile 17) | Harter Endpunkt `https://feedsearch.dev/api/v1/search` — **nicht konfigurierbar**; einzige Verwendung in `SearchDirectoryAsync` (Zeile 112) |
| `SearchTimeout` / `RegexTimeout` | private static readonly | 2 s Zeitbudget pro Suchlauf; 1 s Regex-Timeout |
| `FeedMediaTypes` / `FeedLinkMediaTypes` / `StandardFeedPaths` | private static readonly | Akzeptierte Feed-MIME-Typen und Well-Known-Pfade `/feed`, `/rss`, `/rss.xml`, `/atom.xml`, `/feed.xml`, `/index.xml` (Zeilen 37–45) — exakt die Pfade, die `stubserver.py` bedient |
| `FeedSearchService(HttpClient)` | public ctor (Zeile 56) | Einziger Konstruktor; kein Endpoint-Parameter |
| `SearchAsync(query, ct)` | public (`IFeedSearchService`) | Verknüpft Directory-Suche und Autodiscovery; wirft `FeedSearchUnavailableException` nur wenn beide Quellen fehlschlagen; dedupliziert nach `FeedUrl` (case-insensitive), sortiert nach `MatchKind` → `Score` desc → `FeedUrl` |
| `SearchDirectoryAsync` | private (Zeile 110) | GET `{DirectoryEndpoint}?url=…&info=true&favicon=false&opml=false&skip_crawl=true`; erwartet JSON-Array mit Feldern `url`, `title`, `description`, `site_name`, `site_url`, `score`, `bozo`; `bozo`-Einträge werden verworfen; URL == Query → `MatchKind.ExactUrl` |
| `DiscoverFeedsAsync` | private (Zeile 165) | GET auf die eingegebene URL; Feed-Dokument → `ExactUrl`-Treffer; HTML → `<link rel="alternate">`-Extraktion (nur `<head>`), danach Fallback `ProbeStandardPathsAsync` |
| `ProbeStandardPathsAsync` | private (Zeile 216) | probt `StandardFeedPaths` gegen die Site-Origin; `HttpRequestException` pro Pfad toleriert |
| `ExtractFeedLinks` | private static | Regex-basierte `<link>`-Attributextraktion |
| `IsFeedMediaType` / `GetString` | private static | MIME-Prüfung / JSON-String-Helper |

Bestehende Instanziierungsstellen: `MauiProgram.cs` Zeile 56 (DI-Singleton),
`ServiceCollectionTests.cs` Zeile 51–52 (spiegelt die DI-Registrierung),
`FeedSearchServiceTests.cs` Zeile 17 (`new FeedSearchService(new HttpClient(handler))`),
`test-results/issue-59/probe.fsx` Zeile 8.

## `MauiProgram`

Datei: `src/Reporter/MauiProgram.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateMauiApp` | public static | Baut die `MauiApp`; registriert alle Services/ViewModels/Pages |
| `ApplyPersistedLanguage` | private static | migriert die DB synchron (`context.Database.Migrate()`) und wendet die gespeicherte Sprache an, bevor `AppShell` die Seiten erzeugt |

Relevante Details für die Anforderung:

- Zeile 43: `var databasePath = Path.Combine(FileSystem.AppDataDirectory, "reporter.db");` — **fester Pfad, kein Override**; Zeile 44 legt das Verzeichnis an.
- Zeile 55–56: `AddSingleton<HttpClient>(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(30) })` und `AddSingleton<IFeedSearchService, FeedSearchService>()` — die Registrierung übergibt keinen Endpoint; ein Override müsste hier angebunden werden.
- `App.xaml.cs` `CreateWindow` erzeugt das Hauptfenster über `AppShell` aus dem DI-Container; Windows-Fenstergröße fest 390 × 844.
- `WindowsPackageType` ist `None` (`Reporter.csproj` Zeile 43): unverpackte WinUI-3-Exe → Umgebungsvariablen und Kommandozeilenargumente greifen unmittelbar.

## `FeedsViewModel` (+ `FeedsViewModel.Search.cs`)

Dateien: `src/Reporter.Core/ViewModels/FeedsViewModel.cs`, `FeedsViewModel.Search.cs`

Für die Anforderung relevante Oberfläche (ausführliche Liste in `xaml-views.md`):

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `SearchCommand` / `DirectAddCommand` / `SubscribeResultCommand` / `CloseSearchResultsCommand` | public Commands | Suche, Direkt-Add, Ergebnis-Abo, Ergebnisansicht schließen; `SearchCommand`/`DirectAddCommand` gesperrt im `IsEditMode` bzw. während `IsSearching` |
| `OpenAddFormCommand` / `CloseAddFormCommand` | public Commands | Add-Sheet öffnen (`ResetForm` + `ShowAddForm = true`) bzw. schließen |
| `LoadCommand`, `SaveCommand`, `EditCommand`, `DeleteCommand`, `RefreshCommand`, `RefreshAllCommand` | public Commands | Laden, Edit-Speichern, Editieren, Löschen, Sync |
| `SearchResults`, `ShowSearchResults`, `IsSearching`, `SearchErrorMessage`, `HasSearchError` | public Properties | Zustand der Ergebnisansicht |
| `ConfirmDirectAddAsync` | public `Func<string, Task<bool>>?` | UI-Rückfrage-Callback, von `FeedsPage.xaml.cs` gesetzt (Zeile 28) |
| `NewUrl`, `NewTitle`, `ShowAddForm`, `IsEditMode`, `Feeds`, `Categories`, `NotificationsSupported`, `FeedNotificationsEnabled`, `IsSyncing`, `ErrorMessage`/`HasError`, `SyncErrorMessage`/`HasSyncError` | public Properties | Formular-/Listenzustand |
| `RenameFeedAsync(feed, newTitle)` | public | Umbenennen nach `DisplayPromptAsync` |
| `ChangeFeedCategoryAsync(feed, category)` | public | `category.Id == Guid.Empty` (Pseudo-Eintrag „Keine Kategorie") löscht die Zuordnung |
| `MakeUniqueOptionLabels(names)` | public static | kollisionsfreie ActionSheet-Labels für `ChangeCategoryAsync` im Code-Behind |
| `GetFeedErrorMessage(feed)` | public | lokalisierte Fehlerdetail-Texte |
| `TryResolveSearchUrl` / `TryNormalizeDomainUrl` | private static | URL-Erkennung; Freitext ohne Punkt im Host wird abgelehnt |
| `TryPersistNewFeedAsync` / `FinishAddFlowAsync` / `OfferDirectAddAsync` | private | gemeinsamer Persist- und Abschlusspfad aller Add-Flows; `FeedTitleFallback.GetFallbackTitle` liefert Platzhaltertitel |
| `SyncAsync` | private | serialisiert Sync-Aufrufe (`_isSyncInProgress`), Offline-Pfad setzt `IsSyncing = false` |

Abonnierte Events: `INetworkStatusService` via `TrackConnectivity` (`BaseViewModel`).
Publizierte Events: `PropertyChanged` (wird von `FeedsPage` für den Fokus abonniert).

## `CategoriesViewModel`

Datei: `src/Reporter.Core/ViewModels/CategoriesViewModel.cs`

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `LoadCommand`, `SaveCommand`, `EditCommand`, `DeleteCommand` | public Commands | Laden, Anlegen/Bearbeiten, Edit-Modus, Löschen |
| `NewCategoryName`, `SelectedCategory`, `ErrorMessage`, `HasError`, `Categories` | public Properties | Formular- und Listenzustand (`Categories`: `ObservableCollection<CategoryWithCount>`) |

## `FeedTitleFallback`

Datei: `src/Reporter.Core/Services/FeedTitleFallback.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetFallbackTitle(url)` | public static | letztes nicht-leeres Pfadsegment → Host → URL; funktioniert auch mit IP-/Port-URLs (für `http://127.0.0.1:{port}/heise-atom.xml` → `heise-atom.xml`) |
| `IsFileNamePlaceholderTitle(title, url)` | public static | erkennt automatisch erzeugte Platzhaltertitel beim Sync |

## `FeedSyncService` (Kontext für den E2E-Direkt-Add-/Sync-Pfad)

Datei: `src/Reporter.Core/Services/FeedSyncService.cs`

- Ctor (Zeile 41): `IFeedRepository`, `IItemRepository`, `ISyncLogRepository`,
  `HttpClient`, `INotificationService`, `INetworkStatusService`, `IKeywordFilter`,
  `IFeedIconService`, `IDebugLogService?`.
- Nutzt denselben DI-`HttpClient` (30 s Timeout) wie `FeedSearchService` und
  `FeedIconService` — Abrufe der Feed-URLs laufen in der E2E-App gegen denselben
  Client, also gegen den Stub, wenn die Feed-URL auf `127.0.0.1:{port}` zeigt.
- `SyncFeedAsync`/`SyncAllAsync` geben bei Offline sofort `FeedHealth.Error` zurück.

## `FeedSiteResolver` / `FeedIconService`

- `FeedIconService(HttpClient)` (Zeile 35): `TryFindFaviconUrlAsync(feedUrl, siteUrl)`
  wird in den Add-Flows aufgerufen (`TryFindFaviconUrlAsync` im ViewModel), nur online.
- `FeedSiteResolver` existiert als Service (`src/Reporter.Core/Services/FeedSiteResolver.cs`),
  wird von der Feed-Suche selbst nicht verwendet.

## `App` / `AppShell` (Startpfad der E2E-App)

- `App.OnStart` (`App.xaml.cs`): migriert die DB (`MigrateAsync`), startet
  `IDebugLogService.BeginSessionAsync`, Retention-Cleanup, Theme-Anwendung,
  `INetworkStatusService`-Init, `IAutoRefreshService.StartAsync` — alle in
  try/catch gekapselt, Fehler verhindern den Start nicht.
- `AppShell`-Ctor löst alle fünf Tab-Seiten eager aus dem DI-Container.
