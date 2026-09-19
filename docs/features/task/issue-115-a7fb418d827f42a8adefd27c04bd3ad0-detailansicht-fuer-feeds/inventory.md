<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Detailansicht für Feeds

Analyse des bestehenden Projektstands bezogen auf die Anforderung „Detailansicht für Feeds" (`requirement.md`): Der Tap auf einen `FeedListItem` in der `FeedsPage` soll künftig auf eine neue `FeedDetailPage` navigieren statt das Aktionsblatt zu öffnen; die Feed-Aktionen wandern in die Detailansicht, die eine pagede, suchbare `Item`-Liste anzeigt.

## Zusammenfassung

- **Nichts vom Feature existiert:** Keine `FeedDetailPage`, kein `FeedDetailViewModel`, keine Route `feeddetail`, keine DI-Registrierung — alles muss neu angelegt werden.
- **Tap-Verhalten ist zentralisiert:** `FeedsPage.OnFeedTapped` (`src/Reporter/Views/FeedsPage.xaml.cs`) öffnet aktuell das `DisplayActionSheetAsync` und verteilt auf `RenameFeedAsync`, `ChangeCategoryAsync`, `ConfirmDeleteFeedAsync`, `ShowFeedErrorDetailsAsync` (Code-Behind) sowie `RefreshCommand`/`EditCommand`/`DeleteCommand` des `FeedsViewModel`.
- **Alle Muster sind vorhanden:** `IQueryAttributable`-Navigation mit `Guid`-Query-Parameter (`ArticleDetailPage`, Route `articledetail?itemId=`), Infinite-Scroll-Paging mit `SemaphoreSlim`, `HasMore`, `PageSize = 20` (`LaterViewModel`/`LaterPage`), wiederverwendbare Beitragskarte (`ArticleCardView`), `SearchBar`-Style in `Styles.xaml`.
- **Repository-Lücke:** `IItemRepository.GetByFeedAsync(Guid)` liefert ungepaged `Item`-Objekte ohne Suchfilter; die paged-Projektion (`SelectListItemRows`, `MapToListItem`) existiert bereits intern und wird von `GetUnreadByDateAsync`/`GetSavedForLaterAsync` genutzt.
- **Aktionslogik im `FeedsViewModel`:** `RefreshCommand`, `RenameFeedAsync`, `ChangeFeedCategoryAsync`, `DeleteCommand`, `EditCommand`, `GetFeedErrorMessage`, `MakeUniqueOptionLabels` sind vorhanden; das Edit-UI (`ShowAddForm`/`IsEditMode`-Sheet) ist fest in der `FeedsPage` verankert — die Anforderung listet das als offene Frage.
- **Lokalisierung:** Alle Schlüssel für die bestehenden Feed-Aktionen existieren in `AppResources` (resx + de.resx); Titel/Such-Platzhalter/Leer-Text der neuen Seite fehlen noch.
- **Test-Ausgangszustand:** Unit-Tests 622/622 grün, Node-Tests 36/36 grün. E2E-Suite: 17/19 grün — 2 instabile Fehlschläge in `ArticleImageTests` (Direkt-Add persistierte Feed nicht rechtzeitig; Folgefehler „Reporter.exe process is not running"), im isolierten Wiederholungslauf beide grün → flaky, kein persistenter Defekt. Nachweis: [inventory/tests.md](inventory/tests.md). Testlücke: keine Tests für die neue Ansicht; `SmokeTests.FeedActionSheet_*` prüft genau das zu ersetzende Aktionsblatt.

## Details

- [Datenmodell](inventory/models.md) — `Feed`, `FeedListItem`, `Item`, `ItemListItem`, `Category`, `SyncResult`, EF-Entities
- [Logik](inventory/logic.md) — `FeedsViewModel` (+`Search`-Partial), `FeedsPage`, `LaterViewModel`/`LaterPage` (Paging-Muster), `ArticleDetailPage`/`ArticleDetailViewModel` (Query-Parameter-Muster), `ArticleCardView`, `ItemRepository`, `FeedRepository`, `AppShell`, `MauiProgram`, `BaseViewModel`
- [Enums / Konstanten](inventory/enums.md) — `FeedHealth`, `FeedSyncErrorKind` (keine echten Enums)
- [Interfaces](inventory/interfaces.md) — `IItemRepository`, `IFeedRepository`, `IFeedSyncService`, `ICategoryRepository`, `INetworkStatusService` u. a.
- [UI](inventory/ui.md) — `FeedsPage`/`LaterPage`-XAML-Struktur, `ArticleCardView`, `Styles.xaml`, `AppResources`-Schlüssel, `design-draft/` (kein Entwurf für die Detailansicht)
- [Tests](inventory/tests.md) — Test-Ausgangszustand mit Nachweisen, Testklassen, Hilfsmethoden/Fakes
