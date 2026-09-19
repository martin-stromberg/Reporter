<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Artikelbilder lokal speichern für Offline-Verfügbarkeit (Issue #111)

Analyse des bestehenden Codes bezogen auf die Anforderung, beim Feed-Sync das Artikelbild herunterzuladen, lokal in `reporter-content.db` zu speichern und in `ArticleCardView`/`ArticleDetailPage` offline anzuzeigen. Untersucht wurden Datenmodell, Sync-/Repository-/Cleanup-Logik, Interfaces, UI-Komponenten und Tests unter `src/`.

## Zusammenfassung

- **Content-Store existiert und passt zum Konzept:** `reporter-content.db` mit Tabelle `item_contents` (`item_id` PK, `content_html`) ist bereits vom iCloud-Backup ausgeschlossen (`BackupExclusionPlan`) und besitzt Upsert-, Batch-, Lösch- und Waisen-Sweep-Pfade (`ItemContentRepository` hinter `IItemContentStore`). Bilddaten in dieser Tabelle würden Löschpfade (`ItemRepository.DeleteAsync`/`DeleteExpiredAsync`/`DeleteRangeAsync`, `FeedRepository.DeleteAsync`-Kaskade, `RetentionCleanupService.RemoveOrphanedContentAsync`) ohne Zusatzlogik mit abdecken.
- **Es gibt keine Bild-Persistenz:** `ItemContent`/`item_contents` enthält nur `ContentHtml`; `ItemContentEntry`, `Item`, `ItemListItem` und `IItemContentStore` haben keinerlei Bild-Felder (`byte[]`, MIME-Typ, Origin-URL). Eine EF-Migration für `ContentDbContext` fehlt entsprechend (bislang nur `InitialContentCreate`).
- **Remote-`ImageUrl` existiert bereits:** `ItemRepository.ExtractImageUrl` extrahiert das erste `<img src>` aus `ContentHtml` nach `ItemListItem.ImageUrl`; `ArticleCardView` zeigt die Kaskade `ImageUrl` → `FeedFaviconUrl` → `FeedInitial`, blendet den Thumbnail aber offline komplett aus (`IsOnline`-`DataTrigger`).
- **Sync-Hook vorhanden:** `FeedSyncService.RunSyncAsync`/`CollectNewItems` erzeugt `ContentHtml` und die `contentBackfill`-Liste; `HttpClient` (30 s) ist bereits injiziert; `FeedIconService`/`IFeedIconService` (`TryFindFaviconUrlAsync`) liefern das Muster für fehlerisolierte Downloads. Ein Bild-Download/-Größenbegrenzungs-Service existiert noch nicht.
- **Offline-Detailansicht entfernt Bilder:** `ArticleHtmlSanitizer.Sanitize(…, forOffline: true)` löscht alle `<img>`-Tags; die WebView-CSP in `ArticleDetailViewModel.RebuildHtml` erlaubt bereits `img-src * data: blob:`.
- **Testsuite deckt alle Lösch-/Backfill-Pfade ab:** Content-Roundtrip, Mitlöschung in allen Löschpfaden, Feed-Kaskade, Waisen-Sweep, Favicon-Fehlerisolation sind durch bestehende Tests belegt; `FakeItemContentStore` und `FakeHttpMessageHandler` sind für Bild-Erweiterungen wiederverwendbar. Es gibt keine Bild-bezogenen Tests.

**Test-Ausgangszustand:** Beide Testsuiten liefen vor jeder Änderung vollständig erfolgreich — `Reporter.Tests` (CI-Suite): **566 erfolgreich / 0 fehlgeschlagen / 0 übersprungen**; `Reporter.E2ETests` (FlaUI, lokal): **17 erfolgreich / 0 fehlgeschlagen / 0 übersprungen**. Keine bekannten Fehlschläge, keine Testlücken außer der prinzipiellen Abhängigkeit der E2E-Suite von einer interaktiven Desktop-Session. Nachweis: [inventory/tests.md](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md) — `ItemContent`-Entity, `ContentDbContext`-Mapping, `ItemContentEntry`, `Item`, `ItemListItem`, `Feed`, Pfad-Träger
- [Logik](inventory/logic.md) — `FeedSyncService`, `ItemRepository`, `ItemContentRepository`, `FeedRepository`, `RetentionCleanupService`, `ArticleHtmlSanitizer`, `FeedIconService`, `ItemContentMigrationService`, `ArticleDetailViewModel`, `ArticleCardView`, `BaseViewModel`, `BackupExclusionPlan`, `MauiProgram`
- [Interfaces](inventory/interfaces.md) — `IItemContentStore`, `IItemRepository`, `IFeedRepository`, `IFeedSyncService`, `IRetentionCleanupService`, `IContentMigrationService`, `IFeedIconService`, `INetworkStatusService`, `IDebugLogService`
- [Tests](inventory/tests.md) — Test-Ausgangszustand mit Lauf-Nachweisen (TRX/Console-Logs), Testklassen und Hilfsmethoden
