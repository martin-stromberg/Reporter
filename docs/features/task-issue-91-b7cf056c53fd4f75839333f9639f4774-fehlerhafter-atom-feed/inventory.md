<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Fehlerhafter Atom-Feed (Issue #91)

Analysiert wurde der Feed-Synchronisationspfad in `Reporter.Core` — vom HTTP-Abruf über `SyndicationFeed.Load` bis zur Item-Persistierung, Health-Aktualisierung und Benachrichtigung — bezogen auf die Anforderung, zusätzlich Atom-0.3-Dokumente (Namespace `http://purl.org/atom/ns#`) zu unterstützen.

## Zusammenfassung

- **Einzige Parse-Stelle:** `FeedSyncService.RunSyncAsync` (`src/Reporter.Core/Services/FeedSyncService.cs:156-159`) lädt den `HttpClient`-Stream direkt per `XmlReader.Create` in `SyndicationFeed.Load`. Es existiert **keine** Format-Erkennung, keine Pufferung des Streams und kein eigener `SyndicationFeedFormatter` oder `XmlReader`-Wrapper — Atom 0.3 wird nirgends gesondert behandelt.
- **Nachgelagerte Pipeline arbeitet ausschließlich auf `SyndicationFeed`/`SyndicationItem`:** `CollectNewItems` (Dedup über `GuidOrHash`, `Link` aus `feedItem.Links.FirstOrDefault()`, `PublishedAt` aus `feedItem.PublishDate` mit `DateTimeOffset.MinValue`→`null`-Regel), `NormalizeGuidOrHash` (Item-`Id` ≤ 500 Zeichen, sonst SHA256-Fallback aus `title|link|publishedAt`), `GetContentHtml` (`item.Content` → `item.Summary` als `TextSyndicationContent`), `ResolveFeedTitle`, `TryFindFaviconUrlAsync` (feed-level `alternate`-Link → `IFeedIconService.TryFindFaviconUrlAsync`), `DetermineStatus` (50-%-Schwund- und 30-Tage-Warnregel), `UpdateFeedHealthAsync`, `UpdateLogAsync`, `INotificationService.NotifyNewItemsAsync`.
- **Fehlerpfad bereits vorhanden:** `catch` in `SyncFeedAsync` → `FeedSyncErrorKind.Classify` bildet `XmlException` auf `FeedSyncErrorKind.Parse` ab → `FeedHealth.Error` + `LastErrorKind`/`LastErrorMessage` + `SyncLog` + optionaler `IDebugLogService`-Eintrag (`DebugLogCategory.Sync`, `DebugLogLevel.Error`).
- **Keine Interface-/Datenmodell-/DI-Änderung nötig aus heutiger Sicht:** `IFeedSyncService` hat nur `SyncFeedAsync`/`SyncAllAsync`; `FeedSyncService` wird in `MauiProgram.cs:72` als Singleton registriert; Konstruktor injiziert Repositories, `HttpClient`, Notification/Network/Keyword/Icon/DebugLog-Dienste.
- **Tests:** `TestFeedXml` kann nur RSS 2.0 (`Rss`) und Atom 1.0 (`Atom`) erzeugen — kein `Atom03`-Helfer. `FeedSyncServiceTests` enthält 38 Facts darüber, u. a. Atom-1.0-Pfad (`SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved`) und Parse-Fehlerpfad (`SyncFeedAsync_InvalidXml_ClassifiedAsParse`). Kein Atom-0.3-Test vorhanden.
- **Test-Ausgangszustand:** Beide Testsuiten grün — `Reporter.Tests` 492/492 erfolgreich, `Reporter.E2ETests` 7/7 erfolgreich, keine Fehlschläge, keine übersprungenen Tests. Nachweis: [inventory/tests.md](inventory/tests.md) mit TRX-Reports und Konsolen-Logs unter `inventory/test-results/`.

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums und Konstanten](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
