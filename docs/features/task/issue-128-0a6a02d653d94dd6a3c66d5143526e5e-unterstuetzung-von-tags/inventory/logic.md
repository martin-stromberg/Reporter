<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik — Bestandsaufnahme

## `FeedSyncService`

Datei: `src/Reporter.Core/Services/FeedSyncService.cs`

Implementiert `IFeedSyncService`. Der Feed-Parse-Pfad läuft über `XmlReader` → `PrepareFeedReader` (normalisiert Atom 0.3 via `Atom03NormalizingXmlReader`) → `SyndicationFeed.Load`; unbekannte Erweiterungselemente landen dadurch in `SyndicationItem.ElementExtensions`. Der Service liest derzeit **keine** `ElementExtensions` aus — das ist die Stelle, an der `<app:tags>` extrahiert werden müsste.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(Guid, CancellationToken)` | public | Einzel-Feed-Sync mit per-Feed-Semaphore; Offline-Guard, SyncLog, Fehlerklassifikation via `FeedSyncErrorKind` |
| `SyncAllAsync(CancellationToken)` | public | Serieller Gesamt-Sync über `_syncAllLock`; aggregiert `SyncResult` |
| `RunSyncAsync(Feed, SyncLog, CancellationToken)` | private | Kern: lädt `SyndicationFeed`, ruft `CollectNewItems`, `DownloadImagesAsync`, `AddRangeAsync`, `SetRangeAsync` (Content-Backfill), Health-Update, Benachrichtigung |
| `CollectNewItems(CollectContext, CancellationToken)` | private | Iteriert `context.FeedItems`; Dedup über `knownKeys` (`GuidOrHash`); bei bekannten Items Content-/Image-Backfill (`backfillCandidates`, `imageBackfillCandidates`); legt neue `Item`-Objekte an (Zeilen 337–348: kein Tag-Feld); Keyword-Filter via `_keywordFilter.MatchesAny` |
| `DownloadImagesAsync(...)` | private | Sequentielle Bild-Downloads; mergt Image-Backfill in `contentBackfill` |
| `DetermineHealth(...)` | private static | Health-Entscheidung (Ok/Warning `NoItems`/`NoRecentItems`) |
| `GetContentHtml(SyndicationItem)` | private static | `Content`/`Summary` als `TextSyndicationContent` → HTML-String |
| `NormalizeGuidOrHash(...)` | private static | `item.Id` (≤ 500 Zeichen) oder SHA256 aus `title\|link\|publishedAt` |
| `PrepareFeedReader(XmlReader)` | private static | Atom-0.3-Normalisierung |
| `UpdateFeedHealthAsync` / `UpdateLogAsync` / `TryFindFaviconUrlAsync` / `ResolveFeedTitle` / `IsHostPlaceholderTitle` | private | Persistenz-/Hilfsmethoden |

Records: `CollectContext` (Zeilen 554–559), `CollectResult` (570–574), `HealthDecision` (585).

**Vorarbeit für ElementExtensions:** `ItemImageService` (siehe unten) demonstriert bereits das Auslesen von `SyndicationItem.ElementExtensions` per `OuterNamespace`/`OuterName` und `GetReader()` — dort für `media:thumbnail`/`media:content` (`http://search.yahoo.com/mrss/`) und `itunes:image`.

## `ItemRepository`

Datei: `src/Reporter.Data/Repositories/ItemRepository.cs`

Implementiert `IItemRepository` über `IDbContextFactory<ReporterDbContext>` + `IItemContentStore` (Alias `ItemEntity = Reporter.Data.Entities.Item`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync` / `GetByIdAsync` | public | Voll-`Item` inkl. ContentHtml/Image aus dem Content-Store |
| `AddAsync` / `AddRangeAsync` / `UpdateAsync` | public | `MapToEntity` → `context.Items`; Content/Image separat via `_contentStore`. `UpdateAsync` kopiert die Entity-Felder einzeln (Zeilen 84–91) — eine neue `Tags`-Spalte wäre hier zu ergänzen |
| `DeleteAsync` / `DeleteRangeAsync` / `DeleteExpiredAsync` | public | Löschpfade inkl. Content-Store-Bereinigung |
| `GetByFeedAsync(Guid)` | public | Alle `Item` eines Feeds (wird von `FeedSyncService.RunSyncAsync` für `existingItems` genutzt) |
| `GetByFeedAsync(Guid, int, int, string?)` | public | **Zielmethode der Anforderung.** Zeilen 245–272: filtert derzeit nur `i.Title.Contains(term)` (Zeile 259), sortiert `PublishedAt` desc + `Id`, projiziert via `SelectListItemRows` auf `ItemListRow` und mappt zu `ItemListItem` |
| `GetUnreadByDateAsync` (2 Überladungen) / `GetUnreadCountAsync` / `GetSavedForLaterAsync` / `GetSavedCountAsync` / `GetByCategoryAsync` | public | Weitere List-/Count-Abfragen über dieselbe `ItemListRow`-Projektion |
| `MarkAllAsReadAsync` / `MarkAsReadAsync` / `ToggleSavedForLaterAsync` | public | Status-Updates |
| `GetExpiredKeywordCandidatesAsync` / `GetAllIdsAsync` | public | Keyword-Retention-Kandidaten / ID-Liste |
| `SelectListItemRows(IQueryable<ItemEntity>)` | private static | Projektion auf `ItemListRow` inkl. Feed-/Category-Join (Zeilen 389–405) — `Tags` ist nicht Bestandteil der Projektion |
| `SavedItemsQuery` | private static | Gemeinsame Baseline-Query für Saved-Listen |
| `MapToModel(ItemEntity, string?, ItemImage?)` / `MapToEntity(Item)` | private static | Mapping Domain ↔ Entity (Zeilen 478–510) — beide müssten `Tags` transportieren |
| `MapToListItem` / `ExtractImageUrl` / `ExtractSummary` / `LoadImageStreamAsync` / `GetContentsAsync` | private | Listen-Mapping und Content-Hilfen |
| `ItemListRow` | private sealed class | DB-Zeile der Listenprojektion (Zeilen 515–538): Id, FeedId, Title, Link, PublishedAt, IsRead, IsSavedForLater, FeedTitle, FeedFaviconUrl, CategoryId, CategoryName |

## `ArticleDetailViewModel`

Datei: `src/Reporter/ViewModels/ArticleDetailViewModel.cs` (Namespace `Reporter.Core.ViewModels`)

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `LoadAsync(Guid)` | public | Lädt Settings, `Item` via `_itemRepository.GetByIdAsync`, Feed; setzt `Item`, `FeedName`, `FeedIconUrl`, `PublishedAtText`, `ReadingTime`; ruft `RebuildHtml()` |
| `RebuildHtml()` | private | Zeilen 353–405: `ArticleHtmlSanitizer.Sanitize(Item?.ContentHtml, forOffline: !IsOnline, localImage: Item?.Image)` → `content`; umschließt es mit `<!DOCTYPE html>` + Inline-CSS (`color-scheme: light dark`, `@media (prefers-color-scheme: dark)`, CSP-Meta `default-src 'none'`) und `<body>{content}</body>` — **hier würde der Tag-Block angehängt.** Abbruch bei leerem `content` (dann `HtmlSource = ""`) |
| `Item`, `HtmlSource`, `FeedName`, `FeedIconUrl`, `PublishedAtText`, `ReadingTime` | public properties | Bindings der `ArticleDetailPage` (`WebView` zeigt `HtmlSource`) |
| `FontSizeIndex` / `ToggleFontSizeAsync` | public/private | Schriftgrößen-Umschalter, ruft `RebuildHtml` |
| `CreateItemCopy(bool, bool, DateTime?)` | private | Rekonstruiert `Item` mit neuen Flags (Zeilen 566–587) — müsste `Tags` durchreichen, falls das Modell erweitert wird |
| `OpenLinkInBrowserAsync` / `OpenInBrowserAsync` / `ShareAsync` / `ToggleSavedForLaterAsync` / `ToggleMarkReadAsync` / `MarkReadDelayedAsync` | public/private | Aktionspfade; `MarkReadDelayedAsync`/`ToggleMarkReadAsync` nutzen `CreateItemCopy` |
| `AttachConnectivity` / `DetachConnectivity` / `OnConnectivityChanged` | public/protected | Connectivity-Tracking; `OnConnectivityChanged` ruft `RebuildHtml` |

## `FeedDetailViewModel`

Datei: `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs` (Namespace `Reporter.Core.ViewModels`)

| Methode / Member | Sichtbarkeit | Kurzbeschreibung |
|------------------|-------------|------------------|
| `SearchText` | public property | Setter ruft `DebounceRestartList` (Zeilen 259–269) |
| `SearchTermOrNull` | private | `null` bei leerem/Whitespace-Suchtext (Zeilen 556–557) |
| `DebounceRestartList` / `RestartListDebouncedAsync` / `RestartListAsync` | private | 300-ms-Debounce (`SearchDebounceDelay`, Zeile 26) → `ResetAndLoadFirstPageAsync` |
| `ResetAndLoadFirstPageAsync` / `LoadMoreAsync` / `LoadPageCoreAsync` | private | Paging unter `_loadLock`; `LoadPageCoreAsync` ruft `_itemRepository.GetByFeedAsync(_feedId, _currentPage, PageSize, SearchTermOrNull)` (Zeile 645) — die Suchsemantik liegt vollständig im Repository |
| `PageSize` | public const | 20 |
| `LoadAsync(Guid)` / `RefreshAsync` / `ReloadFeedAsync` | public/private | Lade-/Sync-Pfade |
| Feed-Verwaltung: `RenameFeedAsync`, `ChangeFeedCategoryAsync`, `DeleteFeedAsync`, `GetFeedMessage`, `MakeUniqueOptionLabels`, Edit-Sheet (`Edit`/`SaveEditAsync`/`ResetEditForm`), Feed-Keywords (`AddFeedKeywordAsync`/`RemoveFeedKeywordAsync`) | public/private | Feed-bezogene Aktionen; `FeedKeywords` nutzt `IKeywordRepository` (Stichwort-Filter, nicht Artikel-Tags) |

## `ArticleHtmlSanitizer`

Datei: `src/Reporter.Core/Services/ArticleHtmlSanitizer.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Sanitize(string? html, bool forOffline, ItemImage? localImage = null)` | public static | Regex-Pipeline: entfernt `script`, `iframe`, `style`, `object`, `embed`, `form`, `applet`/`audio`/`video`, `on*`-Attribute, `javascript:`-URLs; bei `forOffline` ersetzt `<a>` durch Text und entfernt `<img>` (Ausnahme: `localImage` → `data:`-URI bzw. Header-Bild). Rückgabe `string?` (null-bedingt) |

Wird nur von `ArticleDetailViewModel.RebuildHtml` aufgerufen. Ein nach `Sanitize` angehängter Tag-Block unterläuft die Sanitization — die Anforderung verlangt daher explizit `WebUtility.HtmlEncode` auf die Tag-Texte.

## `KeywordFilter` / `KeywordMatcher`

Dateien: `src/Reporter.Core/Services/KeywordFilter.cs`, `src/Reporter.Core/Services/KeywordMatcher.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `KeywordFilter.GetKeywordTextsAsync(Guid?)` | public | Effektive Stichwortliste (global ∪ feed-scoped) via `IKeywordRepository` |
| `KeywordFilter.HasKeywordsAsync` | public | Ob überhaupt Stichworte existieren |
| `KeywordFilter.MatchesAny(string? title, string? contentHtml, IReadOnlyList<string>)` | public | Delegiert an `IKeywordMatcher`; bezieht derzeit Titel + ContentHtml — **nicht** Tags (laut Anforderung auch nicht gefordert) |

Wird von `FeedSyncService.CollectNewItems` (Zeile 331) und `NotificationService` genutzt.

## `ItemImageService`

Datei: `src/Reporter.Core/Services/ItemImageService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ResolveImageUrl(SyndicationItem, string?, string?, string?)` | public | Priorisierte Bildquellen: Enclosure (`image/*`) → MediaRSS → iTunes → erstes `<img>` im Content |
| `ResolveMediaRssUrl` / `ResolveItunesImageUrl` | private static | Zeilen 111–171: **Referenzimplementierung für das Lesen von `SyndicationItem.ElementExtensions`** — vergleicht `extension.OuterNamespace` und `extension.OuterName`, liest per `extension.GetReader()` |
| `TryDownloadImageAsync` / `ExtractFirstImageUrl` / `ReadCappedAsync` u. a. | public/private static | Download- und Extraktionshilfen |
