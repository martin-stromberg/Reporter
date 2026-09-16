<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik

## `App` (Startsequenz)

Datei: `src/Reporter/App.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `App(IServiceProvider)` | public | Konstruktor; lädt `Colors`/`Styles`-Ressourcen |
| `OnStart()` | protected override (async void) | Startsequenz, siehe unten |
| `OnSleep()` | protected override | Schreibt „App suspended" ins Debug-Log (fire-and-forget) |
| `OnResume()` | protected override | Schreibt „App resumed" ins Debug-Log |
| `CreateWindow(IActivationState?)` | protected override | Erstellt `Window` mit `AppShell`; auf Windows 390 × 844 pt |
| `OnUnhandledException` | private | Loggt unbehandelte Exceptions (`DebugLogCategory.Exception`) |
| `OnUnobservedTaskException` | private | Loggt unobserved Task-Exceptions |

`OnStart`-Reihenfolge (Zeilen 36–100), alle Schritte ab Zeile 51 fehlerisoliert mit `try/catch` + `Debug.WriteLine` + `debugLogService.LogAsync(DebugLogCategory.Lifecycle, …, DebugLogLevel.Error)`:

1. `scope` via `_services.CreateScope()`; `ReporterDbContext` auflösen (Zeile 40–41)
2. `context.Database.MigrateAsync()` (Zeile 42) — **nicht** fehlerisoliert
3. `IDebugLogService` auflösen, `_debugLogService` merken, `BeginSessionAsync()` (Zeile 44–46)
4. `UnhandledException`/`UnobservedTaskException`-Handler registrieren (Zeile 48–49)
5. `IRetentionCleanupService.CleanupAsync()` (Zeile 51–61)
6. `ISettingsRepository.GetAsync()` + `IAppThemeService.ApplyTheme(settings.Theme)` (Zeile 63–75)
7. `INetworkStatusService` auflösen, damit Monitoring startet (Zeile 77–87)
8. `IAutoRefreshService.StartAsync()` (Zeile 89–99)

Publizierte Events: keine. Registriert `AppDomain.CurrentDomain.UnhandledException` und `TaskScheduler.UnobservedTaskException`.

**Wichtig für First-Start-Erkennung:** `MauiProgram.ApplyPersistedLanguage` läuft bereits **vor** `OnStart` (in `CreateMauiApp` nach `builder.Build()`) und ruft synchron `context.Database.Migrate()` auf (`MauiProgram.cs:141`). Eine migrationshistorie-basierte Erkennung (`GetAppliedMigrationsAsync` leer = frische DB) müsste daher vor dieser Stelle oder in `MauiProgram` selbst erfolgen — in `App.OnStart` ist die Migrationshistorie bereits befüllt.

## `MauiProgram`

Datei: `src/Reporter/MauiProgram.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateMauiApp()` | public static | Baut die `MauiApp`: Fonts, Env-Variablen, DI-Registrierungen, `ApplyPersistedLanguage` |
| `ResolveFeedSearchEndpoint(string?)` | private static | Validiert `REPORTER_FEEDSEARCH_ENDPOINT` (nur absolute http/https-URIs) |
| `ApplyPersistedLanguage(MauiApp)` | private static | `context.Database.Migrate()` synchron + `ISettingsRepository.GetAsync()` + `AppCulture.Apply(settings.Language)`; fehlerisoliert |

Umgebungsvariablen (Konvention `REPORTER_*`, Zeilen 43–57):
- `REPORTER_DB_PATH` — DB-Pfad-Override (E2E-Isolierung); Default `FileSystem.AppDataDirectory/reporter.db`; Verzeichnis wird angelegt (Zeile 49–53). **Nur hier ist `databasePath` bekannt** — relevant für eine dateibasierte First-Start-Erkennung (`File.Exists` vor Migration).
- `REPORTER_FEEDSEARCH_ENDPOINT` — Stub-Endpunkt für die Feed-Suche in E2E-Läufen.
- `REPORTER_APP_PATH` — nur in `ReporterAppFixture` (E2E), nicht in `MauiProgram`.

DI-Registrierungen (Zeilen 59–99): `AddDbContextFactory<ReporterDbContext>` (SQLite, `Data Source={databasePath}`), alle Repositories und Core-Services als Singleton, `HttpClient` (30 s Timeout), `IFeedSearchService` als Factory mit `feedSearchEndpoint`, ViewModels als Singleton, Pages + `AppShell` als Transient. Ein neuer Seed-Service würde nach dem Muster `.AddSingleton<IRetentionCleanupService, RetentionCleanupService>()` (Zeile 73) registriert.

## `CategoryRepository`

Datei: `src/Reporter.Data/Repositories/CategoryRepository.cs` — implementiert `ICategoryRepository`, arbeitet über `IDbContextFactory<ReporterDbContext>`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync()` | public | Alle Kategorien, `AsNoTracking`, nach `Name` sortiert |
| `GetAllWithFeedCountAsync()` | public | `GroupJoin` auf `Feeds` → `CategoryWithCount` mit `FeedCount` |
| `GetByIdAsync(Guid)` | public | Kategorie per ID oder `null` |
| `AddAsync(Category)` | public | `Add` + `SaveChangesAsync`; wirft `DbUpdateException` bei Unique-Verletzung auf `name` |
| `UpdateAsync(Category)` | public | `FindAsync`, nur `Name` wird übernommen; No-op bei fehlender Entity |
| `DeleteAsync(Guid)` | public | `Remove` + `SaveChangesAsync`; FK `feeds.category_id` wird per `SetNull` gelöst |

Es gibt **kein** `GetByNameAsync` — eine Dublettenprüfung für „News" müsste über `GetAllAsync()` laufen. `CategoriesViewModel.SaveAsync` prüft Duplikate in-memory mit `StringComparison.OrdinalIgnoreCase` (`CategoriesViewModel.cs:127-133`); der Unique-Index ist case-sensitiv (SQLite-Default).

## `FeedRepository`

Datei: `src/Reporter.Data/Repositories/FeedRepository.cs` — implementiert `IFeedRepository`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync()` | public | Alle Feeds, `AsNoTracking`, nach `Title` sortiert |
| `GetByIdAsync(Guid)` | public | Feed per ID oder `null` |
| `AddAsync(Feed)` | public | `Add` + `SaveChangesAsync`; wirft bei Unique-Verletzung auf `url` |
| `UpdateAsync(Feed)` | public | Übernimmt `Url`, `Title`, `CategoryId`, `LastCheckedAt`, `HealthStatus`, `HealthLastChange`, `NotificationsEnabled`, `FaviconUrl`, `LastErrorKind`, `LastErrorMessage`; No-op bei fehlender Entity |
| `DeleteAsync(Guid)` | public | `Remove` + `SaveChangesAsync`; `items` kaskadieren |
| `GetAllWithDetailsAsync()` | public | Projektion auf `FeedListItem` inkl. `CategoryName` (Join) und `UnreadCount` |
| `GetByUrlAsync(string)` | public | `FirstOrDefaultAsync(f => f.Url == url)` — Dublettenprüfung, exakter String-Vergleich |

## `SettingsRepository`

Datei: `src/Reporter.Data/Repositories/SettingsRepository.cs` — implementiert `ISettingsRepository`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAsync(CancellationToken)` | public | Liest Singleton-Zeile (`DefaultId`); legt sie bei Fehlen mit Entity-Defaults an |
| `SaveAsync(Settings)` | public | `FindAsync(DefaultId)`, kopiert alle Felder einzeln; legt Zeile bei Fehlen an |

Relevant für Erkennungsvariante (d): `SaveAsync` mappt die Property-Liste explizit — eine neue `settings`-Spalte müsste in Entity, Modell, diesem Mapping und einer neuen Migration gepflegt werden.

## `FeedsViewModel` (+ `FeedsViewModel.Search`)

Dateien: `src/Reporter.Core/ViewModels/FeedsViewModel.cs`, `FeedsViewModel.Search.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync()` | private | Lädt Kategorien (mit `Guid.Empty`-Pseudo-Eintrag `CategoryNone`) und `GetAllWithDetailsAsync` → `Feeds` |
| `TryPersistNewFeedAsync(url, title, siteUrl)` | private | **Referenz-Defaults für neue Feeds** (Zeile 301–329): `GetByUrlAsync`-Dublettenprüfung → `AddAsync(new Feed { Id = Guid.NewGuid(), Url, Title, CategoryId = null, LastCheckedAt = null, HealthStatus = FeedHealth.Ok, HealthLastChange = null, NotificationsEnabled = true, FaviconUrl = favicon })`; Favicon via `TryFindFaviconUrlAsync` (nur online) |
| `SaveAsync()` | private | Nur Edit-Modus: `GetByUrlAsync`-Dublettenprüfung (ausgenommen eigene ID), `UpdateAsync` |
| `RenameFeedAsync(FeedListItem?, string?)` | public | Umbenennen via `UpdateAsync` |
| `ChangeFeedCategoryAsync(FeedListItem?, Category?)` | public | Kategorie-Zuordnung ändern; `Guid.Empty` = „keine" |
| `DeleteAsync` / `RefreshAsync` / `RefreshAllAsync` / `SyncAsync` | private | Löschen bzw. Sync-Aufrufe mit Reentrancy-Guard (`_isSyncInProgress`) |
| `MakeUniqueOptionLabels` | public static | Eindeutige Actionsheet-Labels bei gleichnamigen Kategorien |
| `SearchAsync` / `DirectAddAsync` / `SubscribeResultAsync` / `OfferDirectAddAsync` | private | Add-Flows; enden alle in `TryPersistNewFeedAsync` + `FinishAddFlowAsync` |

Der geseedete Demo-Feed würde als normale Karte erscheinen und über `RefreshAsync`/`RefreshAllAsync` synchronisierbar sein.

## `CategoriesViewModel`

Datei: `src/Reporter.Core/ViewModels/CategoriesViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync()` | private | `GetAllWithFeedCountAsync` → `Categories` |
| `SaveAsync()` | private | Trim, Leer-Check, **OrdinalIgnoreCase-Duplikatprüfung** gegen geladene Liste (Zeile 127–133), dann `AddAsync`/`UpdateAsync` |
| `EditAsync` / `DeleteAsync` | private | Auswahl bzw. `DeleteAsync` + Reload |

## `FeedSyncService`

Datei: `src/Reporter.Core/Services/FeedSyncService.cs` — implementiert `IFeedSyncService`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(Guid, CancellationToken)` | public | Offline-Check → `SyncLog` anlegen → `RunSyncAsync`; Fehler → `UpdateFeedHealthAsync(Error)` + `FeedSyncErrorKind.Classify` + Debug-Log |
| `SyncAllAsync(CancellationToken)` | public | `_syncAllLock`-Semaphore (kein paralleler Gesamt-Sync); offline → `Error`; iteriert alle Feeds; bei 0 Feeds `Ok` mit „No feeds configured." |
| `RunSyncAsync` | private | Download via `_httpClient.GetStreamAsync(feed.Url)` — **realer Netzverkehr**, nicht über den gestubbten Search-Endpunkt; Items deduplizieren (`GuidOrHash`), `IKeywordFilter`, `AddRangeAsync`, Status via `DetermineStatus`, `ResolveFeedTitle`, Favicon-Backfill, `UpdateFeedHealthAsync`, `NotifyNewItemsAsync` (fehlerisoliert) |
| `ResolveFeedTitle` | private static | Ersetzt Platzhaltertitel (leer, = URL, = Host, = Dateiname via `FeedTitleFallback.IsFileNamePlaceholderTitle`) durch den Dokumenttitel — `"rss-feed.rss"` würde beim ersten erfolgreichen Sync durch den echten Titel ersetzt |
| `UpdateFeedHealthAsync` | private | `UpdateAsync` mit `LastCheckedAt = UtcNow`, Statuswechsel-Zeitstempel, `ResolvedTitle`/`FaviconUrl` |
| `TryFindFaviconUrlAsync` | private | `alternate`-Link des Dokuments → `IFeedIconService.TryFindFaviconUrlAsync` (Zeile 341–348) |
| `CollectNewItems` / `NormalizeGuidOrHash` / `DetermineStatus` / `GetContentHtml` / `PrepareFeedReader` | private | Item-Filterung, Guid/SHA256-Normalisierung, Warning-Heuristik, Atom 0.3-Normalisierung |

Konstruktor-Abhängigkeiten: `IFeedRepository`, `IItemRepository`, `ISyncLogRepository`, `HttpClient`, `INotificationService`, `INetworkStatusService`, `IKeywordFilter`, `IFeedIconService`, optional `IDebugLogService`.

## `AutoRefreshService`

Datei: `src/Reporter.Core/Services/AutoRefreshService.cs` — implementiert `IAutoRefreshService`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartAsync(CancellationToken)` | public | `GetAsync` → `ApplySettingsAsync` → bei `RefreshOnStartupEnabled && IsOnline` fire-and-forget `RunStartupSyncAsync()` (Zeile 49–52) |
| `ApplySettingsAsync(Settings)` | public | Unter `_stateLock`: Loop stoppen, `IBackgroundRefreshService.ApplySettingsAsync` (fehlerisoliert, auch bei deaktiviertem Auto-Refresh), `PeriodicTimer`-Loop starten wenn `AutoRefreshEnabled` |
| `StopAsync()` | public | Loop canceln + awaiten + CTS disposen |
| `RunStartupSyncAsync()` | private | `SyncAllAsync` mit `try/catch` → `Debug.WriteLine` + `DebugLogCategory.Sync`-Log |
| `RunLoopAsync` / `StopLoopAsync` | private | `PeriodicTimer`-Loop; offline Ticks werden übersprungen; Sync-Fehler geloggt, Loop läuft weiter |

Konstruktor: `ISettingsRepository`, `IFeedSyncService`, `INetworkStatusService`, `IBackgroundRefreshService`, optional `TimeProvider` + `IDebugLogService`. Wird von `App.OnStart` (Zeile 91–92) aufgerufen — der geseedete Demo-Feed würde beim ersten Start sofort synchronisiert (sofern online).

## `NotificationService`

Datei: `src/Reporter.Core/Services/NotificationService.cs` — implementiert `INotificationService`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `NotifyNewItemsAsync(Feed, IReadOnlyList<Item>, CancellationToken)` | public | Gating: `feed.NotificationsEnabled` → globale `Settings.NotificationsEnabled` → Quiet Hours → Keyword-Filter; `NotificationSummaryEnabled` → eine Sammel-Benachrichtigung, sonst **eine `ILocalNotificationService.ShowAsync` pro Item** (Zeile 89–92) |
| `IsQuietHoursActive` / `BuildSummaryIdentifier` / `BuildItemUserInfo` / `Truncate` | private | Quiet-Hours-Auswertung, Summary-ID (SHA256), Deep-Link-Payload |

Beim ersten Sync des Demo-Feeds sind alle Artikel „neu" → mit `NotificationsEnabled = true` und Default `NotificationSummaryEnabled = false` würden Einzel-Benachrichtigungen pro Artikel ausgelöst (nur wirksam auf iOS; `LocalNotificationService.IsSupported` ist sonst `false`, `ShowAsync` No-op).

## `LocalNotificationService`

Datei: `src/Reporter/Services/LocalNotificationService.cs` — implementiert `ILocalNotificationService`, plattformspezifisch.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `IsSupported` | public | `true` nur unter `#if IOS`; sonst `false` |
| `RequestAuthorizationAsync` | public | iOS: `UNUserNotificationCenter`-Berechtigung; sonst `false` |
| `GetAuthorizationStatusAsync` | public | iOS: Status-Mapping; sonst `Unsupported` |
| `ShowAsync(title, body, identifier, userInfo, ct)` | public | iOS: `EnsureAuthorizedAsync` → `UNNotificationRequest`; sonst No-op. Fordert auf iOS bei `NotDetermined` die Berechtigung an |

## `RetentionCleanupService` (Referenzmuster für neuen Core-Service)

Datei: `src/Reporter.Core/Services/RetentionCleanupService.cs` — implementiert `IRetentionCleanupService`; hängt nur an `ISettingsRepository`, `IItemRepository`, `IKeywordFilter`. Ein `DemoContentService` könnte analog nur an `ICategoryRepository`, `IFeedRepository`, optional `IDebugLogService` hängen und bliebe damit plattformneutral testbar (`Reporter.Core` hat keine MAUI-Referenz — `Reporter.Core.csproj` targetet `net10.0` ohne MAUI-Pakete).

## `FeedTitleFallback`

Datei: `src/Reporter.Core/Services/FeedTitleFallback.cs` — static.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetFallbackTitle(string url)` | public static | Letztes nicht-leeres URL-decodiertes Pfadsegment → Host → URL selbst. Für `https://www.apple.com/newsroom/rss-feed.rss` → `"rss-feed.rss"` |
| `IsFileNamePlaceholderTitle(title, url)` | public static | Prüft, ob `title` dem Dateinamen-Fallback entspricht (OrdinalIgnoreCase) — Grundlage für `FeedSyncService.ResolveFeedTitle` |
