<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik — Bestandsaufnahme

Bezug: Anforderung „Artikeldaten getrennt von Nutzerdaten speichern (iCloud-Backup)" (`requirement.md`). Alle `ContentHtml`-Pfade laufen aktuell über die eine Tabelle `items` in `reporter.db`.

## `ItemRepository`

Datei: `src/Reporter.Data/Repositories/ItemRepository.cs`

Implementiert `IItemRepository` über `IDbContextFactory<ReporterDbContext>` (Konstruktor-Parameter `factory`, Zeilen 18–27).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync` | public | Alle Items, `PublishedAt` absteigend; `MapToModel` inkl. `ContentHtml`. |
| `GetByIdAsync` | public | Ein Item per `Id`; `MapToModel` inkl. `ContentHtml`. Wird u. a. von `ArticleDetailViewModel` aufgerufen. |
| `AddAsync` | public | Fügt ein Item ein; `MapToEntity` schreibt `ContentHtml` nach `items.content_html`. |
| `UpdateAsync` | public | Aktualisiert alle Felder inkl. `entity.ContentHtml = item.ContentHtml` (Zeile 76). |
| `DeleteAsync` | public | Löscht ein Item per `Id` (`FindAsync` + `Remove`). Keine Content-Nebenwirkungen nötig, solange Content in derselben Zeile liegt. |
| `GetUnreadByDateAsync` | public | Ungelesene Items; `MapToModel` inkl. `ContentHtml`. |
| `GetUnreadByDateAsync(page, pageSize, categoryId, ascending)` | public | Paged-Liste als `ItemListItem` über `SelectListItemRows`/`MapToListItem` — liest `content_html` in der SQL-Projektion mit (Zeile 310) und leitet `ImageUrl`/`Summary`/`ReadingTimeText` daraus ab. |
| `GetUnreadCountAsync` | public | Zählt ungelesene Items (optional Kategorie). |
| `MarkAllAsReadAsync` | public | `ExecuteUpdate` auf `IsRead`/`ReadAt` — kein Content-Zugriff. |
| `ToggleSavedForLaterAsync` | public | Toggle `IsSavedForLater` — kein Content-Zugriff. |
| `MarkAsReadAsync` | public | Setzt `IsRead`/`ReadAt` — kein Content-Zugriff. |
| `GetByFeedAsync` | public | Items eines Feeds; `MapToModel` inkl. `ContentHtml`. Wird von `FeedSyncService.RunSyncAsync` für die Deduplizierung geladen. |
| `GetByCategoryAsync` | public | Items aller Feeds einer Kategorie; `MapToModel` inkl. `ContentHtml`. |
| `GetSavedForLaterAsync(page, pageSize)` | public | Paged Merkliste als `ItemListItem` — gleiche `content_html`-Projektion. |
| `AddRangeAsync` | public | Batch-Insert via `MapToEntity` — schreibt `ContentHtml` pro Item in `items.content_html`. Wird von `FeedSyncService` aufgerufen. |
| `DeleteExpiredAsync` | public | `ExecuteDelete` auf `IsRead && !IsSavedForLater && (ReadAt ?? PublishedAt) < cutoff`. Wird von `RetentionCleanupService.CleanupAsync` aufgerufen. |
| `GetExpiredKeywordCandidatesAsync` | public | Kandidaten für Keyword-Löschung; `MapToModel` inkl. `ContentHtml` — `RetentionCleanupService` matched danach `Title`+`ContentHtml` gegen `IKeywordFilter.MatchesAny`. |
| `DeleteRangeAsync` | public | `ExecuteDelete` auf `ids.Contains(i.Id)`. Wird von `RetentionCleanupService` aufgerufen. |
| `SelectListItemRows` | private static | Projektion `items` + Feed/Category in `ItemListRow` (enthält `ContentHtml`, Zeile 310). |
| `MapToListItem` | private static | `ItemListRow` → `ItemListItem`; ruft `ExtractImageUrl`, `ExtractSummary`, `ReadingTimeEstimator.EstimateText` auf `row.ContentHtml` auf. |
| `ExtractImageUrl` | private static | Regex auf `<img ... src>` in `contentHtml`. |
| `ExtractSummary` | private static | HTML-Strip + Decode, max. 120 Zeichen. |
| `MapToModel` | private static | Entity → `Core.Models.Item`, kopiert `ContentHtml` (Zeile 383). |
| `MapToEntity` | private static | Modell → Entity, kopiert `ContentHtml` (Zeile 400). |
| `ItemListRow` | private sealed class | Flache Zeile für die Listenseiten (enthält `ContentHtml`). |

Abonnierte Events: keine. Publizierte Events: keine.

## `FeedSyncService`

Datei: `src/Reporter.Core/Services/FeedSyncService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync` | public | Sync eines Feeds; schreibt `SyncLog`, lädt `Feed`, delegiert an `RunSyncAsync`; Fehler werden per `FeedSyncErrorKind.Classify` klassifiziert und in `feeds.last_error_*`/`sync_logs` persistiert. |
| `SyncAllAsync` | public | Sync aller Feeds seriell (`SemaphoreSlim _syncAllLock`); aggregiert `SyncResult`. |
| `RunSyncAsync` | private | Kern-Logik: lädt bestehende Items via `_itemRepository.GetByFeedAsync(feed.Id)` (inkl. `ContentHtml`), parst Feed-XML, ruft `CollectNewItems`, schreibt neue Items via `AddRangeAsync`, benachrichtigt via `INotificationService.NotifyNewItemsAsync` (Fehler isoliert geloggt). |
| `CollectNewItems` | private | Deduplizierung über `HashSet<string>` der `GuidOrHash` bestehender Items (Zeilen 231–233): bereits bekannte Items werden komplett übersprungen — auch `ContentHtml` wird dann nicht aktualisiert/nachgeladen. Neue Items werden mit `ContentHtml = GetContentHtml(feedItem)` erzeugt (Zeile 269) und zuvor gegen `IKeywordFilter.MatchesAny(title, contentHtml, keywordTexts)` geprüft (Zeile 253). |
| `GetContentHtml` | private static | `item.Content` (TextSyndicationContent) oder `item.Summary` → `string?`. |
| `NormalizeGuidOrHash` | private static | `item.Id` (≤ 500 Zeichen) oder SHA256 über `title|link|publishedAt`. |
| `ResolveFeedTitle`/`IsHostPlaceholderTitle`/`DetermineStatus`/`UpdateFeedHealthAsync`/`TryFindFaviconUrlAsync`/`UpdateLogAsync`/`PrepareFeedReader` | private | Titelauflösung, Health-Status, Favicon-Backfill, SyncLog-Update, Atom-0.3-Normalisierung — ohne Content-Bezug. |

Abonnierte Events: keine. Publizierte Events: keine.

## `RetentionCleanupService`

Datei: `src/Reporter.Core/Services/RetentionCleanupService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CleanupAsync` | public | Liest `Settings.RetentionDays`; `DeleteExpiredAsync(cutoff)` löscht abgelaufene gelesene Items. Anschließend Keyword-Pfad: `GetExpiredKeywordCandidatesAsync(cutoff)` → `IKeywordFilter.MatchesAny(i.Title, i.ContentHtml, keywordTexts)` (Zeile 52) → `DeleteRangeAsync(matchedIds)`. Benötigt `ContentHtml` der Kandidaten aus der DB. |

Aufgerufen von `App.CleanupRetainedDataAsync` beim Start. Heute gibt es keine Content-Waisen, weil Content in derselben Zeile liegt und `DeleteExpiredAsync`/`DeleteRangeAsync`/`ExecuteDelete` sowie die Feed-Kaskade (`items.feed_id` → `OnDelete(Cascade)`) ganze Zeilen entfernen.

## `NotificationService`

Datei: `src/Reporter.Core/Services/NotificationService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `NotifyNewItemsAsync` | public | Prüft `feed.NotificationsEnabled`, globale Einstellung, Ruhezeiten; filtert `newItems` per `IKeywordFilter.MatchesAny(i.Title, i.ContentHtml, keywordTexts)` (Zeile 72); sendet Einzel- oder Sammel-Benachrichtigung über `ILocalNotificationService.ShowAsync`. Die `Item`-Objekte liegen hier noch im Arbeitsspeicher vor (aus `FeedSyncService.CollectNewItems`). |
| `IsQuietHoursActive`/`BuildItemUserInfo`/`BuildSummaryIdentifier`/`Truncate` | private | Ruhezeiten-Auswertung via `TimeProvider`, `userInfo` (`itemId`, `link`), Summary-Hash — ohne Storage-Bezug. |

## `BackupExclusionService`

Datei: `src/Reporter/Services/BackupExclusionService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ExcludeFromBackup` | public | `File.Exists`-Guard, dann unter `#if IOS` `NSUrl.SetResource(NSUrl.IsExcludedFromBackupKey, NSNumber.FromBoolean(true))`; Fehler nur per `Debug.WriteLine`. Auf allen Nicht-iOS-Targets No-op. Es existiert **kein** Umkehrpfad (kein `NSNumber.FromBoolean(false)` / Re-Include). |

Wird aufgerufen von `App.ExcludeDatabaseFilesFromBackup` (dreimal: `reporter.db`, `-wal`, `-shm`).

## `App`

Datei: `src/Reporter/App.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnStart` | protected override | Startsequenz (Zeilen 37–59): `MigrateDatabaseAsync` → `debugLogService.BeginSessionAsync` → `ExcludeDatabaseFilesFromBackup` (sync, `RunStartupStep`) → Exception-Hooks → `SeedDemoContentAsync` → `CleanupRetainedDataAsync` → `ApplyThemeAsync` → `StartNetworkMonitoring` → `StartAutoRefreshAsync`. Jeder Schritt über `RunStartupStep`/`RunStartupStepAsync` fehlerisoliert. |
| `MigrateDatabaseAsync` | private static | `context.Database.MigrateAsync()` auf `ReporterDbContext`. |
| `ExcludeDatabaseFilesFromBackup` | private static | `IBackupExclusionService.ExcludeFromBackup` auf `databasePath.FilePath`, `+ "-wal"`, `+ "-shm"` (Zeilen 116–127) — aktuell die komplette `reporter.db`. |
| `SeedDemoContentAsync` | private static | `IDemoContentService.EnsureSeededAsync`. |
| `CleanupRetainedDataAsync` | private static | `IRetentionCleanupService.CleanupAsync`. |
| `ApplyThemeAsync`/`StartNetworkMonitoring`/`StartAutoRefreshAsync` | private static | Theme, Connectivity-Singleton, Auto-Refresh. |
| `RunStartupStep`/`RunStartupStepAsync`/`LogStartupFailure` | private static | Fehlerisolation je Start-Schritt (try/catch → `Debug.WriteLine` + `IDebugLogService.LogAsync`). |
| `OnUnhandledException`/`OnUnobservedTaskException`/`OnSleep`/`OnResume`/`CreateWindow` | private/protected | Crash-/Lebenszyklus-Logging; Windows-Fenster 390×844. |

## `MauiProgram`

Datei: `src/Reporter/MauiProgram.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateMauiApp` | public static | Liest `REPORTER_DB_PATH` (Default `FileSystem.AppDataDirectory/reporter.db`, Zeilen 46–49), legt Verzeichnis an, berechnet `isFirstRun = !File.Exists(databasePath)` (Zeile 61 — vor `builder.Build()`, weil `ApplyPersistedLanguage` migriert). Registriert `AddDbContextFactory<ReporterDbContext>` mit `UseSqlite` (Zeile 73), `new DatabasePath(databasePath)` (Zeile 88), `new FirstRunState {...}` (Zeile 87), `IBackupExclusionService → BackupExclusionService` (Zeile 98) sowie alle Repositories/Services als Singletons. Danach `builder.Build()` + `ApplyPersistedLanguage`. |
| `ResolveFeedSearchEndpoint` | private static | Validiert `REPORTER_FEEDSEARCH_ENDPOINT` (nur absolute http/https-URIs). |
| `ResolveDemoSeedSuppressed` | private static | `REPORTER_DISABLE_DEMO_SEED` ≠ `"0"`/`"false"` → unterdrückt. |
| `ApplyPersistedLanguage` | private static | Migriert synchron (`context.Database.Migrate()`) und wendet `settings.Language` via `AppCulture.Apply` an; Fehler blockieren den Start nicht. |

## `FeedRepository` (Kaskaden-Kontext)

Datei: `src/Reporter.Data/Repositories/FeedRepository.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DeleteAsync` | public | `Feeds.Remove(entity)` — das Entfernen der Items erfolgt allein über die DB-Kaskade `OnDelete(DeleteBehavior.Cascade)` auf `items.feed_id` (`ReporterDbContext` Zeile 119). Bei einer zweiten Datenbank müssten Content-Einträge separat entfernt werden. |

## Weitere Content-Konsumenten (In-Memory, ohne eigenen Storage)

| Klasse | Datei | Bezug zu `ContentHtml` |
|--------|-------|------------------------|
| `KeywordFilter` (`IKeywordFilter`) | `src/Reporter.Core/Services/KeywordFilter.cs` | `GetKeywordTextsAsync` (aus `IKeywordRepository`), `MatchesAny(title, contentHtml, keywordTexts)` delegiert an `IKeywordMatcher`. |
| `KeywordMatcher` (`IKeywordMatcher`) | `src/Reporter.Core/Services/KeywordMatcher.cs` | `MatchesAny(title, contentHtml, keywords)` — Case-insensitiver Substring-Match, rein in-memory. |
| `ReadingTimeEstimator` | `src/Reporter.Core/Services/ReadingTimeEstimator.cs` | `EstimateText(string? contentHtml)` — Lesezeit aus HTML. Aufgerufen aus `ItemRepository.MapToListItem` und `ArticleDetailViewModel`. |
| `ArticleHtmlSanitizer` | `src/Reporter.Core/Services/ArticleHtmlSanitizer.cs` | `Sanitize(string? html, bool forOffline)` — bereinigt HTML für die WebView. Aufgerufen aus `ArticleDetailViewModel.RebuildHtml`. |
| `DemoContentService` | `src/Reporter.Core/Services/DemoContentService.cs` | `EnsureSeededAsync` seedet nur `Category` + `Feed` (keine Items/kein `ContentHtml`). |

## `ArticleDetailViewModel` (indirekt betroffen)

Datei: `src/Reporter/ViewModels/ArticleDetailViewModel.cs`

- Lädt das Item via `_itemRepository.GetByIdAsync(itemId)` (Zeile 286); bei `null` → `GoBackAsync`.
- `ReadingTime = ReadingTimeEstimator.EstimateText(item.ContentHtml)` (Zeile 299).
- `RebuildHtml()` (Zeile 353): `ArticleHtmlSanitizer.Sanitize(Item?.ContentHtml, forOffline: !IsOnline)`; leerer Content → `HtmlSource = string.Empty` (defensiver Pfad existiert, Zeilen 356–360).
- `OnConnectivityChanged` baut HTML bei Netzwechsel neu auf.
- `MarkRead`/`ToggleSaved`-Pfade erzeugen per `ContentHtml = Item.ContentHtml` (Zeile 583) ein `Item`-Update über `IItemRepository.UpdateAsync`.
