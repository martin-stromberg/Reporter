<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik

Logikklassen und Services, die für die Anforderung „Artikelbilder lokal speichern" relevant sind.

## `FeedSyncService`

Datei: `src/Reporter.Core/Services/FeedSyncService.cs` (implementiert `IFeedSyncService`)

Konstruktor-Abhängigkeiten: `IFeedRepository`, `IItemRepository`, `ISyncLogRepository`, `HttpClient` (30-s-Timeout, Singleton aus `MauiProgram`), `INotificationService`, `INetworkStatusService`, `IKeywordFilter`, `IFeedIconService`, `IItemContentStore`, `IDebugLogService?` (optional).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(Guid, CancellationToken)` | public | Offline-Guard, `SyncLog` anlegen, Feed laden, `RunSyncAsync` aufrufen; Exceptions → `FeedHealth.Error` + `FeedSyncErrorKind.Classify` + `IDebugLogService.LogAsync` |
| `SyncAllAsync(CancellationToken)` | public | Serienmäßiger Gesamt-Sync hinter `_syncAllLock`; iteriert alle Feeds |
| `RunSyncAsync(Feed, SyncLog, CancellationToken)` | private | Kernablauf: `GetByFeedAsync` → Feed-Stream laden → `SyndicationFeed.Load` → `CollectNewItems` → `AddRangeAsync` → `contentBackfill` per `_contentStore.SetRangeAsync` → Status/Favicon/Log/Notification |
| `CollectNewItems(...)` | private | Ermittelt neue `Item`-Entities inkl. `ContentHtml` (`GetContentHtml`), Duplikat-Erkennung über `GuidOrHash`, Keyword-Filterung, `backfillCandidates` für vorhandene Items ohne Inhalt → `ItemContentEntry`-Liste |
| `ResolveFeedTitle` | private static | Platzhalter-Titel durch Dokumenttitel ersetzen |
| `IsHostPlaceholderTitle` | private static | Hilfsprobe für `ResolveFeedTitle` |
| `DetermineStatus` | private static | Health-Status (`Ok`/`Warning`) aus Abrufzahlen |
| `UpdateFeedHealthAsync` | private | Feed-Update inkl. `FaviconUrl`, `LastErrorKind` |
| `TryFindFaviconUrlAsync` | private | Site-Link aus `SyndicationFeed` + `_feedIconService.TryFindFaviconUrlAsync` — Muster für strikte Fehlerisolation (Icon-Service wirft nie) |
| `UpdateLogAsync` | private | `SyncLog` abschließen |
| `GetContentHtml` | private static | `TextSyndicationContent` aus `item.Content`, Fallback `item.Summary` |
| `NormalizeGuidOrHash` | private static | Item-ID bzw. SHA256-Fallback |
| `PrepareFeedReader` | private static | Atom-0.3-Normalisierung |

Abonnierte Events: keine. Publizierte Events: keine (Benachrichtigungen laufen über `_notificationService.NotifyNewItemsAsync`).

Relevant für die Anforderung: `RunSyncAsync`/`CollectNewItems` ist der zentrale Hook; `_httpClient` steht bereits für Downloads bereit; `TryFindFaviconUrlAsync` zeigt das Fehlerisolation-Muster; `contentBackfill` ist der bestehende Nachlade-Mechanismus für fehlende Inhalte.

## `ItemRepository`

Datei: `src/Reporter.Data/Repositories/ItemRepository.cs` (implementiert `IItemRepository`)

Abhängigkeiten: `IDbContextFactory<ReporterDbContext>`, `IItemContentStore`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync` | public | Alle Items + Content-Hydratisierung via `GetRangeAsync` |
| `GetByIdAsync` | public | Einzelnes Item + `_contentStore.GetAsync` → `MapToModel` |
| `AddAsync` | public | Item speichern; `ContentHtml` per `SetAsync` in Content-Store |
| `UpdateAsync` | public | Felder aktualisieren; `SetAsync` mit Upsert/Lösch-Semantik |
| `DeleteAsync` | public | Item löschen + `_contentStore.DeleteAsync` (Mitlöschung des Contents) |
| `GetUnreadByDateAsync()` | public | Ungelesene + Content-Hydratisierung |
| `GetUnreadByDateAsync(page, pageSize, categoryId, ascending)` | public | Paged-Liste → `SelectListItemRows` + `MapToListItem` |
| `GetUnreadCountAsync` | public | Zähler für Badge |
| `MarkAllAsReadAsync` | public | `ExecuteUpdate` auf `IsRead`/`ReadAt` |
| `ToggleSavedForLaterAsync` | public | Flag umschalten |
| `MarkAsReadAsync` | public | `IsRead`/`ReadAt` setzen |
| `GetByFeedAsync` | public | Items eines Feeds (für `CollectNewItems`-Vergleich) |
| `GetByCategoryAsync` | public | Items aller Feeds einer Kategorie |
| `GetSavedForLaterAsync` | public | Paged Gemerkt-Liste → `MapToListItem` |
| `AddRangeAsync` | public | Batch-Insert + `SetRangeAsync` der `ItemContentEntry`s |
| `DeleteExpiredAsync` | public | Retention-Löschung + `_contentStore.DeleteRangeAsync` |
| `GetExpiredKeywordCandidatesAsync` | public | Keyword-Löschkandidaten |
| `DeleteRangeAsync` | public | Sammel-Löschung + `_contentStore.DeleteRangeAsync` |
| `GetAllIdsAsync` | public | Alle Item-IDs (für Waisen-Sweep) |
| `SelectListItemRows` | private static | Projektion `ItemEntity` → `ItemListRow` (inkl. `Feed.FaviconUrl`, Kategorie) |
| `MapToListItem` | private static | `ItemListRow` + `contentHtml` → `ItemListItem`; setzt `ImageUrl = ExtractImageUrl(contentHtml)` |
| `ExtractImageUrl` | private static | Regex auf erstes `<img ... src='...'>` im `ContentHtml` — Herkunft der Remote-Fallback-URL |
| `ExtractSummary` | private static | Klartext-Teaser |
| `GetContentsAsync<T>` | private | `_contentStore.GetRangeAsync`-Wrapper |
| `MapToModel` / `MapToEntity` | private static | Entity ↔ Domain-Modell |

## `ItemContentRepository`

Datei: `src/Reporter.Data/Repositories/ItemContentRepository.cs` (implementiert `IItemContentStore`)

Abhängigkeit: `IDbContextFactory<ContentDbContext>`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAsync` | public | `content_html` eines Items lesen |
| `GetRangeAsync` | public | Dictionary `ItemId → ContentHtml` für ID-Liste |
| `SetAsync` | public | Upsert; `null`/leer löscht den Eintrag |
| `SetRangeAsync` | public | Batch-Upsert mit Duplikat-Reduktion („letzter gewinnt") |
| `DeleteAsync` | public | Einzel-Löschung |
| `DeleteRangeAsync` | public | `ExecuteDelete` für ID-Liste |
| `GetItemIdsAsync` | public | Alle gespeicherten Item-IDs (Waisen-Sweep) |

## `FeedRepository`

Datei: `src/Reporter.Data/Repositories/FeedRepository.cs` (implementiert `IFeedRepository`)

`DeleteAsync(Guid)` löst die Feed-Kaskade aus: `items`-Zeilen entfernt die DB-Kaskade (`DeleteBehavior.Cascade` auf `FeedId`), die `item_contents`-Zeilen explizit per `_contentStore.DeleteRangeAsync(itemIds)` (Zeilen 91–100). Übrige Methoden: `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `GetAllWithDetailsAsync`, `GetByUrlAsync`.

## `RetentionCleanupService`

Datei: `src/Reporter.Core/Services/RetentionCleanupService.cs` (implementiert `IRetentionCleanupService`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CleanupAsync` | public | `RemoveOrphanedContentAsync` (immer), dann Retention: `DeleteExpiredAsync` + Keyword-Kandidaten per `DeleteRangeAsync` |
| `RemoveOrphanedContentAsync` | private | Waisen-Sweep: `GetItemIdsAsync` (Content-Store) minus `GetAllIdsAsync` (Items) → `DeleteRangeAsync` |

Würde bei Bilddaten in `item_contents` ohne Zusatzlogik mit abdecken.

## `ArticleHtmlSanitizer`

Datei: `src/Reporter.Core/Services/ArticleHtmlSanitizer.cs` (statisch)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Sanitize(string? html, bool forOffline)` | public static | Regex-Bereinigung; `forOffline: true` neutralisiert `<a>` (Text bleibt) und entfernt **alle** `<img>`-Tags vollständig (`ImageTagRegex`, Zeile 59) |

Wird ausschließlich von `ArticleDetailViewModel.RebuildHtml` mit `forOffline: !IsOnline` aufgerufen.

## `FeedIconService`

Datei: `src/Reporter.Core/Services/FeedIconService.cs` (implementiert `IFeedIconService`)

Referenzmuster für einen isolierten Download-Service: `FindFaviconUrlAsync` (HTML-`<link>`-Scan + `/favicon.ico`-Fallback mit `VerifyAsync`-GET-Probe), `TryFindFaviconUrlAsync` (fehlerisoliert — fängt alle Exceptions, liefert `null`). Abhängigkeit: `HttpClient`.

## `ItemContentMigrationService`

Datei: `src/Reporter.Data/ItemContentMigrationService.cs` (implementiert `IContentMigrationService`)

`MigrateLegacyContentAsync`: migriert Content-Schema (`MigrateAsync`), prüft per `PRAGMA table_info('items')` ob die Legacy-Spalte `content_html` existiert und kopiert seitenweise (500er-Pages) in den Content-Store. Wird bei jedem App-Start aufgerufen (`MauiProgram.MigrateContentStoreAndLegacyData`, `App.OnStart`).

## `ArticleDetailViewModel`

Datei: `src/Reporter/ViewModels/ArticleDetailViewModel.cs` (Namespace `Reporter.Core.ViewModels`, Projekt `Reporter`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync(Guid)` | public | Settings + `Item` via `_itemRepository.GetByIdAsync` + Feed laden, `RebuildHtml` |
| `RebuildHtml` | private | `ArticleHtmlSanitizer.Sanitize(Item?.ContentHtml, forOffline: !IsOnline)` → HTML-Dokument mit CSP `img-src * data: blob:` → `HtmlSource` für die `WebView` |
| `OnConnectivityChanged` | protected override | `RebuildHtml` bei Netzwechsel |
| `AttachConnectivity`/`DetachConnectivity` | public | Connectivity-Tracking (aus `BaseViewModel`) |
| `CreateItemCopy` | private | `Item`-Kopie für Statusänderungen (müsste neue Bild-Felder mitführen) |
| weitere | — | `ToggleSavedForLaterAsync`, `ToggleMarkReadAsync`, `MarkReadDelayedAsync`, `OpenLinkInBrowserAsync`, `OpenInBrowserAsync`, `ShareAsync`, `ToggleFontSizeAsync`, `GoBackAsync` |

## `ArticleCardView`

Datei: `src/Reporter/Views/ArticleCardView.xaml` + `.xaml.cs`

Bindable Properties: `OpenArticleCommand`, `ToggleSavedCommand`, `MarkReadCommand`, `IsOnline` (Default `true`; wird von `UnreadPage`/`LaterPage` aus dem jeweiligen ViewModel gebunden).

Thumbnail-Kaskade im XAML (Zeilen 78–134):
1. `Image Source="{Binding ImageUrl}"` — sichtbar wenn `ImageUrl` nicht leer (`StringNotEmptyToBoolConverter`)
2. `Image Source="{Binding FeedFaviconUrl}"` — MultiTrigger: sichtbar nur wenn `FeedFaviconUrl` gesetzt **und** `ImageUrl` leer
3. Kreis mit `FeedInitial` — sichtbar nur wenn beide leer

Der `DataTrigger` auf `IsOnline = False` (Zeilen 86–90) blendet den kompletten Thumbnail-`Border` offline aus — unabhängig davon, ob ein lokales Bild vorhanden wäre.

## `BaseViewModel`

Datei: `src/Reporter.Core/ViewModels/BaseViewModel.cs`

Stellt `IsOnline`, `InitConnectivity`, `TrackConnectivity`/`UntrackConnectivity` (abonniert `INetworkStatusService.ConnectivityChanged`), `RefreshConnectivityStatus` und das virtuelle `OnConnectivityChanged(bool)` bereit. `UnreadViewModel`, `LaterViewModel` und `ArticleDetailViewModel` erben davon.

## `BackupExclusionPlan`

Datei: `src/Reporter.Core/Services/BackupExclusionPlan.cs` (statisch)

`ExcludedPaths(ContentDatabasePath)` → `reporter-content.db` + `-wal`/`-shm`; `IncludedPaths(DatabasePath)` → `reporter.db` + Sidecars. Wird von `App.OnStart` über `IBackupExclusionService` angewendet.

## `MauiProgram`

Datei: `src/Reporter/MauiProgram.cs`

Relevante DI-Registrierungen: `IDbContextFactory<ContentDbContext>` (Z. 80), `IItemContentStore → ItemContentRepository` (Z. 84), `IContentMigrationService → ItemContentMigrationService` (Z. 85), `HttpClient` Singleton 30 s (Z. 90), `IFeedIconService → FeedIconService` (Z. 93), `IFeedSyncService → FeedSyncService` (Z. 94), `IRetentionCleanupService → RetentionCleanupService` (Z. 95). `MigrateContentStoreAndLegacyData` läuft beim Start fehlerisoliert.
