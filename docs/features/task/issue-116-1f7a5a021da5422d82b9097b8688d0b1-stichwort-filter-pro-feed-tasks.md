<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Stichwort-Filter pro Feed

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `Reporter.Data.Entities.Keyword` um `FeedId` (`Guid?`) und `Feed`-Navigation (`Feed?`) erweitern | Offen | — |
| 2 | Datenmodell | `Reporter.Core.Models.Keyword` um `FeedId` (`Guid?`, `init`, Standard `null`) erweitern | Offen | — |
| 3 | Datenmodell | `ReporterDbContext.ConfigureKeyword` anpassen: `feed_id`-Spalte (nullable), FK `feeds.id` mit `DeleteBehavior.Cascade`, Unique-Index `keyword_text` ersetzen durch Composite-Unique `(feed_id, keyword_text)` plus gefiltertem Unique-Index `keyword_text` mit `HasFilter("feed_id IS NULL")` | Offen | — |
| 4 | Migrationen | EF-Migration `AddKeywordFeedId` scaffolden (`dotnet ef migrations add AddKeywordFeedId --project src/Reporter.Data`), `ReporterDbContextModelSnapshot` aktualisieren und generierte Migration gegen Schritt-3-Konfiguration prüfen | Offen | — |
| 5 | Datenzugriff | `IKeywordRepository` um `GetByFeedAsync(Guid? feedId)` → `Task<IReadOnlyList<Keyword>>` (`null` = nur globale) und `GetEffectiveForFeedAsync(Guid feedId)` → `Task<IReadOnlyList<Keyword>>` (Union global + Feed) erweitern | Offen | — |
| 6 | Datenzugriff | `KeywordRepository`: `GetByFeedAsync`/`GetEffectiveForFeedAsync` implementieren (sortiert nach `KeywordText`) und `MapToModel`/`MapToEntity` um `FeedId` ergänzen | Offen | — |
| 7 | Logik | `IKeywordFilter.GetKeywordTextsAsync()` auf `GetKeywordTextsAsync(Guid? feedId)` umstellen und in `KeywordFilter` implementieren (`null` → `GetByFeedAsync(null)`, sonst `GetEffectiveForFeedAsync(feedId)`); `MatchesAny` unverändert | Offen | — |
| 8 | Logik | `FeedSyncService.RunSyncAsync`: Stichwortliste per `GetKeywordTextsAsync(feed.Id)` laden | Offen | — |
| 9 | Logik | `NotificationService.NotifyNewItemsAsync`: `GetKeywordTextsAsync(feed.Id)` statt parameterlosem Aufruf verwenden | Offen | — |
| 10 | Logik | `RetentionCleanupService.CleanupAsync`: Keyword-Löschkandidaten nach `Item.FeedId` gruppieren und pro Gruppe `GetKeywordTextsAsync(feedId)` anwenden; Frühausstieg auf „irgendwelche Stichworte vorhanden" umstellen | Offen | — |
| 11 | ViewModel | `SettingsViewModel.LoadAsync`: Stichworte per `GetByFeedAsync(null)` statt `GetAllAsync()` laden (nur globale Einträge in der Einstellungsliste) | Offen | — |
| 12 | ViewModel | `FeedDetailViewModel`: `IKeywordRepository` als Pflicht-Konstruktorparameter ergänzen; `MaxKeywordLength = 500`, `FeedKeywords` (`ObservableCollection<Keyword>`), `NewFeedKeywordText`, `FeedKeywordErrorMessage`/`HasFeedKeywordError` anlegen | Offen | — |
| 13 | ViewModel | `FeedDetailViewModel`: `AddFeedKeywordCommand`/`AddFeedKeywordAsync` (Trim → `ErrorKeywordEmpty` → `ErrorKeywordTooLong` → `OrdinalIgnoreCase`-Duplikatprüfung gegen `FeedKeywords` → `Keyword` mit `FeedId = _feedId` → `AddAsync` → Collection) und `RemoveFeedKeywordCommand`/`RemoveFeedKeywordAsync` implementieren | Offen | — |
| 14 | ViewModel | `FeedDetailViewModel`: `LoadAsync` befüllt `FeedKeywords` via `GetByFeedAsync(feedId)`; `Edit()` und `ResetEditForm()` setzen `NewFeedKeywordText`/`FeedKeywordErrorMessage` zurück | Offen | — |
| 15 | Lokalisierung | `AppResources`-Schlüssel `FeedKeywordsLabel`, `FeedKeywordPlaceholder`, `FeedKeywordAdd`, `FeedKeywordRemoveFormat`, `FeedKeywordsInfo` in `AppResources.resx`, `AppResources.de.resx` und `AppResources.Designer.cs` anlegen | Offen | — |
| 16 | UI | `FeedDetailPage.xaml`: Stichwort-Abschnitt im Bearbeiten-Sheet nach `SettingsPage`-Keyword-Muster einbauen (Abschnitts-Label, `Entry` + Add-`Button`, Fehler-`Label`, Chip-`FlexLayout` mit `FeedKeywords`, Info-`Border`); `AppThemeBinding` für alle neuen Farben, Touch-Targets ≥ 44 × 44 pt, Sheet-Inhalt in höhenbegrenzte `ScrollView` einbetten (keine `CollectionView`-/`ScrollView`-Verschachtelung) | Offen | — |
| 17 | Tests | `FeedDetailViewModelTests.CreateViewModel`: `KeywordRepository`-Instanz auf `_factory` ergänzen und als `IKeywordRepository` übergeben | Offen | — |
| 18 | Tests | `KeywordRepositoryTests`: `GetByFeedAsync_Null_ReturnsOnlyGlobalKeywords`, `GetByFeedAsync_FeedId_ReturnsFeedKeywords`, `GetEffectiveForFeedAsync_ReturnsGlobalAndFeedUnion`, `AddAsync_SameTextInDifferentFeeds_Succeeds`, `AddAsync_DuplicateInSameFeed_Throws`, `AddAsync_DuplicateGlobal_Throws` | Offen | — |
| 19 | Tests | `FeedRepositoryTests`: `DeleteAsync_RemovesFeedScopedKeywords_KeepsGlobal` (FK-Kaskade) | Offen | — |
| 20 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_FeedKeywordMatch_NotSaved`, `SyncFeedAsync_FeedKeyword_OtherFeedUnaffected`, `SyncFeedAsync_FeedKeyword_CombinedWithGlobal` | Offen | — |
| 21 | Tests | `KeywordFilterTests_E2E`: `E2E_KeywordFilter_FeedScoped_NotInUnreadList` (Feed-Stichwort → Sync → Unread-Liste) | Offen | — |
| 22 | Tests | `NotificationServiceTests`: `NotifyNewItemsAsync_FeedKeywordMatch_SkipsItem`, `NotifyNewItemsAsync_FeedKeyword_OtherFeedScope` | Offen | — |
| 23 | Tests | `RetentionCleanupServiceTests`: `CleanupAsync_FeedKeywordMatch_DeletesOnlyOwnFeed`, `CleanupAsync_GlobalKeyword_StillDeletesAcrossFeeds` | Offen | — |
| 24 | Tests | `FeedDetailViewModelTests`: `LoadAsync_LoadsFeedKeywords`, `AddFeedKeyword_Valid_PersistsWithFeedId`, `AddFeedKeyword_Empty_ShowsError`, `AddFeedKeyword_TooLong_ShowsError`, `AddFeedKeyword_Duplicate_ShowsError`, `RemoveFeedKeyword_DeletesFromRepository`, `CloseEditForm_ResetsKeywordInput` | Offen | — |
| 25 | Tests | `SettingsViewModelTests_Keywords`: `LoadAsync_ExcludesFeedScopedKeywords` | Offen | — |
| 26 | E2E-Tests | `FeedDbAssertions`: `KeywordExistsForFeedAsync` (Join `keywords.feed_id` ↔ `feeds`) hinzufügen; `DeleteAllFeedsAsync` um `DELETE FROM keywords WHERE feed_id IS NOT NULL` ergänzen | Offen | — |
| 27 | E2E-Tests | `E2EPageHelpers`: `WaitForFeedKeywordEntry`-Helper (Entry per `SemanticProperties.Description` = `FeedKeywordPlaceholder`) | Offen | — |
| 28 | E2E-Tests | `FeedDetailTests`: `FeedDetail_Edit_AddKeyword_PersistsAndShowsChip` (Hinzufügen → Chip + DB-Zeile mit `feed_id`, Persistenz über Sheet-Reopen), `FeedDetail_Edit_RemoveKeyword_RemovesChipAndRow`, `FeedDetail_Keyword_FiltersItemsOnSync_OnlyForOwnFeed` (Feed A gefiltert, Feed B ungefiltert) | Offen | — |
| 29 | E2E-Tests | `FeedDetailTests`: `FeedDetail_Edit_KeywordValidation_ShowsErrors` — alle drei sichtbaren Validierungsfälle im Edit-Sheet: leere Eingabe → `ErrorKeywordEmpty`, 501 Zeichen → `ErrorKeywordTooLong`, Duplikat → `ErrorKeywordDuplicate` (Label-Sichtbarkeit jeweils prüfen) | Offen | — |
| 30 | E2E-Tests | `FeedDetailTests`: `FeedDetail_Keyword_NotShownInGlobalSettings` — Feed-Stichwort über Edit-Sheet anlegen, `SelectTab(AppResources.TabSettings)`, globales Kontroll-Stichwort `e2e-global` über Settings-UI hinzufügen (Chip sichtbar), per `UiRetry.WaitFor` prüfen, dass kein Element den Feed-Stichwort-Text trägt | Offen | — |
| 31 | Verifikation | Mobile-UI-Verifikation des erweiterten Bearbeiten-Sheets bei 390 × 844 pt (Light/Dark via `AppThemeBinding`, Touch-Targets ≥ 44 pt, Scroll-Verhalten) dokumentieren und `.\scripts\Run-StaticChecks.ps1` ohne Findings ausführen | Offen | — |
