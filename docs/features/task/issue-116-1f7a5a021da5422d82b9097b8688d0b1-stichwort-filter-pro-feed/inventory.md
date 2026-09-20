<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Stichwort-Filter pro Feed (Issue #116)

Bestandsaufnahme des bestehenden Keyword-Filter-Bereichs (globaler Stichwort-Filter in Einstellungen, Sync, Benachrichtigung, Retention) bezogen auf die Anforderung, einen feed-spezifischen Stichwort-Filter im Bearbeiten-Sheet der `FeedDetailPage` einzuführen (`requirement.md`).

## Zusammenfassung

- **Stichwort-Modell ist rein global:** `Reporter.Data.Entities.Keyword` (`src/Reporter.Data/Entities/Keyword.cs`) und `Reporter.Core.Models.Keyword` (`src/Reporter.Core/Models/Keyword.cs`) besitzen nur `Id` und `KeywordText` — keine Feed-Zuordnung. `ReporterDbContext.ConfigureKeyword` setzt einen **Unique-Index auf `keyword_text`** (tabellenweit).
- **Filteranwendung an drei Stellen, alle global:** `FeedSyncService.RunSyncAsync` lädt die Stichwortliste einmal pro Sync via `IKeywordFilter.GetKeywordTextsAsync()` **ohne Feed-Parameter** und filtert neue Items in `CollectNewItems` über `MatchesAny`; `NotificationService.NotifyNewItemsAsync` (hat den `Feed`-Parameter bereits) und `RetentionCleanupService.CleanupAsync` (feed-übergreifende Kandidatenliste) rufen dieselbe globale Liste ab.
- **Match-Semantik fest verdrahtet:** `KeywordMatcher.MatchesAny` = Teilwort-Vergleich `OrdinalIgnoreCase` auf `Title` und `ContentHtml`.
- **Pflege-UI existiert nur global:** `SettingsViewModel` (`Keywords`, `NewKeywordText`, `AddKeywordCommand`, `RemoveKeywordCommand`, `MaxKeywordLength = 500`, Fehler `ErrorKeywordEmpty`/`ErrorKeywordTooLong`/`ErrorKeywordDuplicate`) + Keyword-Karte in `SettingsPage.xaml`. Das Bearbeiten-Sheet der `FeedDetailPage` verwaltet heute nur `EditUrl` und `EditNotificationsEnabled`; `FeedDetailViewModel` hat keinen `IKeywordRepository`-/`IKeywordFilter`-Zugriff.
- **Feed-Löschung:** `FeedRepository.DeleteAsync` entfernt Items per DB-Kaskade (`DeleteBehavior.Cascade` auf `items.feed_id`) und Contents explizit; eine Stichwort-Behandlung existiert nicht (es gibt auch keine Stichwort-Zeilen mit Feed-Bezug).
- **Migrationen:** Konvention `<timestamp>_<Name>` unter `src/Reporter.Data/Migrations/`; `AddFeedNotificationsEnabled` ist das Vorbild für feed-spezifische Spalten. Keine `feed_id`-Spalte an `keywords` vorhanden.
- **Enums:** Keine relevanten Enums vorhanden (`FeedHealth`, `FeedSyncErrorKind` sind statische Konstanten-Klassen, keine Enums).
- **Test-Ausgangszustand:** `Reporter.Tests` (Release, mit Coverage): **641 erfolgreich, 0 fehlgeschlagen, 0 übersprungen** (Exit 0). `Reporter.E2ETests` (Debug, FlaUI): **24 erfolgreich, 2 fehlgeschlagen, 0 übersprungen** (Exit 1) — beide Fehlschläge in `ArticleImageTests` (Feed-Persistierung/App-Prozess beendet), nicht im Keyword-Bereich. Details und Nachweise: [inventory/tests.md](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md) — `Keyword`, `Feed`, `Item` (Entity + Domain), `FeedListItem`, `ReporterDbContext`-Konfiguration, Migrationen
- [Logik](inventory/logic.md) — `KeywordFilter`, `KeywordMatcher`, `FeedSyncService`, `NotificationService`, `RetentionCleanupService`, `KeywordRepository`, `FeedRepository`, `SettingsViewModel`, `FeedDetailViewModel`, `MauiProgram`-DI, Shell-Routing
- [Interfaces](inventory/interfaces.md) — `IKeywordFilter`, `IKeywordMatcher`, `IKeywordRepository`, `IFeedRepository`, `INotificationService`, `IRetentionCleanupService`, `IItemRepository`, `IFeedSyncService`
- [UI](inventory/ui.md) — `FeedDetailPage`-Bearbeiten-Sheet, `SettingsPage`-Keyword-Karte, `AppResources`-Schlüssel
- [Tests](inventory/tests.md) — Test-Ausgangszustand, Testklassen, Hilfsmethoden, gesicherte Logs unter [inventory/test-results/](inventory/test-results/)
