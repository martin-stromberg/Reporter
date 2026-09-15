<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik — Bestandsaufnahme

## `FeedSyncService`
Datei: `src/Reporter.Core/Services/FeedSyncService.cs` — implementiert `IFeedSyncService`, registriert als Singleton in `MauiProgram.cs:72`.

Konstruktor-Abhängigkeiten: `IFeedRepository`, `IItemRepository`, `ISyncLogRepository`, `HttpClient`, `INotificationService`, `INetworkStatusService`, `IKeywordFilter`, `IFeedIconService`, optional `IDebugLogService?` (= `null`).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(Guid, CancellationToken)` | `public` | Offline-Check → `SyncLog` anlegen → `Feed` laden → `RunSyncAsync` im `try`; `catch` (außer `OperationCanceledException`) klassifiziert via `FeedSyncErrorKind.Classify`, setzt `FeedHealth.Error` + `LastErrorKind`/`LastErrorMessage`, schließt `SyncLog`, schreibt `IDebugLogService`-Eintrag (`Sync`/`Error`). |
| `SyncAllAsync(CancellationToken)` | `public` | Offline-Check → iteriert alle Feeds, ruft pro Feed `SyncFeedAsync`, aggregiert Status (`Error` > `Warning` > `Ok`), Itemzahlen und Meldungen. |
| `RunSyncAsync(Feed, SyncLog, CancellationToken)` | `private` | Kernpipeline: Bestandsitems laden (`GetByFeedAsync`, `lastPublishedAt`-Maximum) → **Parse** (`GetStreamAsync` → `XmlReader.Create` mit `DtdProcessing.Ignore` → `SyndicationFeed.Load`, Zeilen 156–159) → Keywords laden → `CollectNewItems` → `AddRangeAsync` → `DetermineStatus` → `ResolveFeedTitle` → `TryFindFaviconUrlAsync` → `UpdateFeedHealthAsync`/`UpdateLogAsync` → `NotifyNewItemsAsync` (fehlerisoliert). |
| `CollectNewItems(...)` | `private` | Dedup per `knownKeys`-`HashSet` über `GuidOrHash`; mappt `SyndicationItem` → `Item` (`Link` = erster Link, `PublishedAt` = `PublishDate` außer `MinValue`, `Title`, `ContentHtml`); Keyword-Treffer werden verworfen und in `filteredCount` gezählt. |
| `ResolveFeedTitle(Feed, SyndicationFeed)` | `private static` | Liefert `SyndicationFeed.Title?.Text`, wenn der gespeicherte Titel ein Platzhalter ist (leer, == URL, == Host, == Dateiname via `FeedTitleFallback.IsFileNamePlaceholderTitle`); sonst `null`. |
| `IsHostPlaceholderTitle(Feed)` | `private static` | Prüft, ob `feed.Title` dem Host der Feed-URL entspricht. |
| `DetermineStatus(int, int, int, DateTime)` | `private static` | `Warning`, wenn `fetchedCount < existingCount * 0.5` (und `existingCount > 0`) oder `newItems == 0` und `lastPublishedAt` älter als 30 Tage; sonst `Ok`. |
| `UpdateFeedHealthAsync(Feed, string, FeedHealthUpdate)` | `private` | Persistiert `HealthStatus` (+ `HealthLastChange` bei Wechsel), `LastCheckedAt`, `ResolvedTitle`, `FaviconUrl`, `LastErrorKind`/`LastErrorMessage` via `IFeedRepository.UpdateAsync`. |
| `TryFindFaviconUrlAsync(string, SyndicationFeed, CancellationToken)` | `private` | Liest `alternate`-Link des Feed-Dokuments (`syndicationFeed.Links`) und ruft `IFeedIconService.TryFindFaviconUrlAsync(feedUrl, siteUrl, ct)` — Lookup fehlerisoliert im Icon-Service. |
| `UpdateLogAsync(SyncLog, string, string?)` | `private` | Schließt `SyncLog` mit `FinishedAt`, `Status`, `Message` via `ISyncLogRepository.UpdateAsync`. |
| `GetContentHtml(SyndicationItem)` | `private static` | `item.Content` als `TextSyndicationContent` → `Text`; sonst `item.Summary`; sonst `null`. |
| `NormalizeGuidOrHash(SyndicationItem, string?, DateTime?)` | `private static` | `item.Id` (≤ 500 Zeichen) oder SHA256 über `"{title}\|{link}\|{publishedAt:O}"` als Base64 — `publishedAt = null` (fehlendes Datum bei Atom 0.3 ohne Mapping) verändert den Hash-Input. |

Abonnierte Events: keine (liest `INetworkStatusService.IsOnline` nur ab).
Publizierte Events: keine (Benachrichtigungen laufen über `INotificationService.NotifyNewItemsAsync`).

**Für die Anforderung zentrale Stelle:** Zeilen 156–159 —
`await using var stream = await _httpClient.GetStreamAsync(feed.Url, cancellationToken)` → `XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore })` → `SyndicationFeed.Load(reader)` via `Task.Run`. `SyndicationFeed.Load` akzeptiert nur RSS 2.0 und Atom 1.0 (`http://www.w3.org/2005/Atom`); Atom 0.3 (`http://purl.org/atom/ns#`) erzeugt eine `XmlException`, die im `catch` von `SyncFeedAsync` als `FeedSyncErrorKind.Parse` landet. Der Stream wird nicht gepuffert und nicht seekbar verwendet.

## `FeedSyncErrorKind`
Datei: `src/Reporter.Core/Services/FeedSyncErrorKind.cs` — statische Klasse mit Konstanten + `Classify`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Classify(Exception, string)` | `public static` | `HttpRequestException` mit Statuscode → `HttpStatus`; ohne Statuscode → `InsecureHttpBlocked` bei `http`-URL sonst `Network`; `XmlException` → `Parse`; alles andere → `Unknown`. |

## `FeedSiteResolver`
Datei: `src/Reporter.Core/Services/FeedSiteResolver.cs` — statische Klasse.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ResolveSiteUrl(string, string?)` | `public static` | Liefert `siteUrl` oder die Authority der `feedUrl` als Fallback; `null`, wenn beides leer/ungültig. Wird von `FeedIconService.TryFindFaviconUrlAsync` (`src/Reporter.Core/Services/FeedIconService.cs:90`) und dem Test-Fake `FakeFeedIconService` genutzt. |

## `FeedTitleFallback`
Datei: `src/Reporter.Core/Services/FeedTitleFallback.cs` — statische Klasse; `IsFileNamePlaceholderTitle` wird von `FeedSyncService.ResolveFeedTitle` aufgerufen (Platzhalter-Erkennung).

## Aufrufer von `IFeedSyncService` (unveränderte Nutzung)
- `FeedsViewModel` (`src/Reporter.Core/ViewModels/FeedsViewModel.cs:21`) — manueller Sync
- `UnreadViewModel` (`src/Reporter.Core/ViewModels/UnreadViewModel.cs:23`) — manueller Sync
- `AutoRefreshService` (`src/Reporter.Core/Services/AutoRefreshService.cs:15`) — Timer- und Start-Sync via `SyncAllAsync`
- `ScheduledSyncRunner` (`src/Reporter.Core/Services/ScheduledSyncRunner.cs:14`) — geplanter Hintergrund-Sync
- `IBackgroundRefreshService`-Pfad über `ScheduledSyncRunner`/`AutoRefreshService`
