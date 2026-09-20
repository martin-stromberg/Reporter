<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

- [x] `Reporter.Data.Entities.Keyword` (Entity) — erweitert um `FeedId` (`Guid?`) und `Feed`-Navigation (`Feed?`) — `src/Reporter.Data/Entities/Keyword.cs:23,28`
- [x] Feld `FeedId` (`Guid?`, `init`) in `Reporter.Core.Models.Keyword` — vorhanden — `src/Reporter.Core/Models/Keyword.cs:23`
- [x] Methode `ConfigureKeyword` in `ReporterDbContext` — `feed_id`-Spalte (nullable), FK `feeds.id` mit `DeleteBehavior.Cascade`, Composite-Unique `(feed_id, keyword_text)` plus gefilterter Unique-Index `keyword_text` mit `HasFilter("feed_id IS NULL")` — `src/Reporter.Data/ReporterDbContext.cs:128-132`
- [x] EF-Migration `AddKeywordFeedId` — angelegt (`src/Reporter.Data/Migrations/20260920094729_AddKeywordFeedId.cs`), `ReporterDbContextModelSnapshot` aktualisiert (feed_id, Filter, Composite-Index, FK vorhanden)
- [x] Methoden `GetByFeedAsync(Guid? feedId)` und `GetEffectiveForFeedAsync(Guid feedId)` in `IKeywordRepository` — vorhanden — `src/Reporter.Core/Interfaces/IKeywordRepository.cs:23,30`
- [x] Methoden `GetByFeedAsync`/`GetEffectiveForFeedAsync` in `KeywordRepository` (sortiert nach `KeywordText`) sowie `FeedId` in `MapToModel`/`MapToEntity` — vorhanden — `src/Reporter.Data/Repositories/KeywordRepository.cs:38-59,113,123`
- [x] Methode `GetKeywordTextsAsync(Guid? feedId)` in `IKeywordFilter`/`KeywordFilter` — `null` → `GetByFeedAsync(null)`, Wert → `GetEffectiveForFeedAsync(feedId)` — `src/Reporter.Core/Interfaces/IKeywordFilter.cs:15`, `src/Reporter.Core/Services/KeywordFilter.cs:29-35`
- [x] `FeedSyncService.RunSyncAsync` — `GetKeywordTextsAsync(feed.Id)` — `src/Reporter.Core/Services/FeedSyncService.cs:184`
- [x] `NotificationService.NotifyNewItemsAsync` — `GetKeywordTextsAsync(feed.Id)` — `src/Reporter.Core/Services/NotificationService.cs:70`
- [x] `RetentionCleanupService.CleanupAsync` — Kandidaten nach `Item.FeedId` gruppiert, `GetKeywordTextsAsync(group.Key)` pro Gruppe, alter globaler `keywordTexts.Count > 0`-Frühausstieg entfernt — `src/Reporter.Core/Services/RetentionCleanupService.cs:54-67`
- [x] `SettingsViewModel.LoadAsync` — lädt `GetByFeedAsync(null)` statt `GetAllAsync()` — `src/Reporter.Core/ViewModels/SettingsViewModel.cs:642`
- [x] `FeedDetailViewModel`: Pflicht-Konstruktorparameter `IKeywordRepository` (vor dem optionalen `ILocalNotificationService`) — `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs:71`
- [x] `FeedDetailViewModel`: Konstante `MaxKeywordLength = 500` (private const) — Z. 26
- [x] `FeedDetailViewModel`: Eigenschaften `FeedKeywords` (`ObservableCollection<Keyword>`), `NewFeedKeywordText` (Setter leert `FeedKeywordErrorMessage`), `FeedKeywordErrorMessage`, `HasFeedKeywordError` — Z. 325-360
- [x] `FeedDetailViewModel`: Commands `AddFeedKeywordCommand` (`AsyncRelayCommand`) → `AddFeedKeywordAsync` (Trim → `ErrorKeywordEmpty` → `ErrorKeywordTooLong` → `OrdinalIgnoreCase`-Duplikatprüfung → `Keyword` mit `FeedId = _feedId` → `AddAsync` → `FeedKeywords.Add`) und `RemoveFeedKeywordCommand` (`AsyncRelayCommand<Keyword?>`) → `RemoveFeedKeywordAsync` — Z. 89-90, 799-850
- [x] `FeedDetailViewModel`: `LoadAsync` befüllt `FeedKeywords` via `GetByFeedAsync(feedId)`; `Edit()` und `ResetEditForm()` setzen `NewFeedKeywordText`/`FeedKeywordErrorMessage` zurück — Z. 377-382, 705-706, 758-759
- [x] `FeedDetailPage.xaml`: Stichwort-Abschnitt im Bearbeiten-Sheet (Abschnitts-Label `FeedKeywordsLabel`, `Entry` mit `NewFeedKeywordText`/`FeedKeywordPlaceholder`/`ReturnCommand`, Add-`Button` `FeedKeywordAdd`, Fehler-`Label` `FeedKeywordErrorMessage`/`HasFeedKeywordError`, `FlexLayout Wrap="Wrap"` mit `BindableLayout.ItemsSource="{Binding FeedKeywords}"` + Chip-`DataTemplate` mit „×"-`Button` (`RemoveFeedKeywordCommand`, `FeedKeywordRemoveFormat`), Info-`Border` `FeedKeywordsInfo`); Sheet-Inhalt in `ScrollView MaximumHeightRequest="560"`; `AppThemeBinding` und 44-pt-Touch-Targets — `src/Reporter/Views/FeedDetailPage.xaml:288-406`
- [x] `AppResources`: Schlüssel `FeedKeywordsLabel`, `FeedKeywordPlaceholder`, `FeedKeywordAdd`, `FeedKeywordRemoveFormat`, `FeedKeywordsInfo` in `AppResources.resx`, `AppResources.de.resx` und `AppResources.Designer.cs` — vorhanden
- [x] `MauiProgram` — korrekterweise unverändert (`IKeywordRepository` bereits Singleton, Z. 86)
- [x] `FeedDetailViewModelTests.CreateViewModel` — `KeywordRepository`-Instanz auf `_factory` ergänzt und übergeben — `src/Reporter.Tests/FeedDetailViewModelTests.cs:35,60`
- [x] `KeywordRepositoryTests`: `GetByFeedAsync_Null_ReturnsOnlyGlobalKeywords`, `GetByFeedAsync_FeedId_ReturnsFeedKeywords`, `GetEffectiveForFeedAsync_ReturnsGlobalAndFeedUnion`, `AddAsync_SameTextInDifferentFeeds_Succeeds`, `AddAsync_DuplicateInSameFeed_Throws`, `AddAsync_DuplicateGlobal_Throws` — vorhanden — `src/Reporter.Tests/KeywordRepositoryTests.cs:117-208`
- [x] `FeedRepositoryTests.DeleteAsync_RemovesFeedScopedKeywords_KeepsGlobal` — vorhanden — `src/Reporter.Tests/FeedRepositoryTests.cs:342`
- [x] `FeedSyncServiceTests`: `SyncFeedAsync_FeedKeywordMatch_NotSaved`, `SyncFeedAsync_FeedKeyword_OtherFeedUnaffected`, `SyncFeedAsync_FeedKeyword_CombinedWithGlobal` — vorhanden — `src/Reporter.Tests/FeedSyncServiceTests.cs:871,898,925`
- [x] `KeywordFilterTests_E2E.E2E_KeywordFilter_FeedScoped_NotInUnreadList` — vorhanden — `src/Reporter.Tests/KeywordFilterTests_E2E.cs:159`
- [x] `NotificationServiceTests`: `NotifyNewItemsAsync_FeedKeywordMatch_SkipsItem`, `NotifyNewItemsAsync_FeedKeyword_OtherFeedScope` — vorhanden — `src/Reporter.Tests/NotificationServiceTests.cs:300,326`
- [x] `RetentionCleanupServiceTests`: `CleanupAsync_FeedKeywordMatch_DeletesOnlyOwnFeed`, `CleanupAsync_GlobalKeyword_StillDeletesAcrossFeeds` — vorhanden — `src/Reporter.Tests/RetentionCleanupServiceTests.cs:315,357`
- [x] `FeedDetailViewModelTests`: `LoadAsync_LoadsFeedKeywords`, `AddFeedKeyword_Valid_PersistsWithFeedId`, `AddFeedKeyword_Empty_ShowsError`, `AddFeedKeyword_TooLong_ShowsError`, `AddFeedKeyword_Duplicate_ShowsError`, `RemoveFeedKeyword_DeletesFromRepository`, `CloseEditForm_ResetsKeywordInput` — vorhanden — `src/Reporter.Tests/FeedDetailViewModelTests.cs:1067-1203`
- [x] `SettingsViewModelTests_Keywords.LoadAsync_ExcludesFeedScopedKeywords` — vorhanden — `src/Reporter.Tests/SettingsViewModelTests_Keywords.cs:143`
- [x] `FeedDbAssertions.KeywordExistsForFeedAsync` (Join `keywords.feed_id` ↔ `feeds`) und `DeleteAllFeedsAsync` mit `DELETE FROM keywords WHERE feed_id IS NOT NULL` — vorhanden — `src/Reporter.E2ETests/FeedDbAssertions.cs:85,272`
- [x] `E2EPageHelpers.WaitForFeedKeywordEntry` (Entry per `SemanticProperties.Description` = `FeedKeywordPlaceholder`) — vorhanden — `src/Reporter.E2ETests/E2EPageHelpers.cs:533`
- [x] `FeedDetailTests.FeedDetail_FeedKeyword_FiltersOnlyOwnFeed` — umgesetzt (abweichender Methodenname, deckt das geplante Szenario `FeedDetail_Keyword_FiltersItemsOnSync_OnlyForOwnFeed` ab: Feed A gefiltert, Feed B ungefiltert, DB-Nachweis) — `src/Reporter.E2ETests/FeedDetailTests.cs:455`
- [x] `FeedDetailTests.FeedDetail_Edit_FeedKeywordValidation_ShowsErrors` — umgesetzt (abweichender Methodenname, alle drei Validierungsfälle `ErrorKeywordEmpty`/`ErrorKeywordTooLong`/`ErrorKeywordDuplicate` mit Label-Sichtbarkeit) — `src/Reporter.E2ETests/FeedDetailTests.cs:491`
- [x] `FeedDetailTests.FeedDetail_FeedKeyword_NotInGlobalSettings` — umgesetzt (abweichender Methodenname, deckt `FeedDetail_Keyword_NotShownInGlobalSettings` ab: Kontroll-Stichwort in Settings-Liste sichtbar, Feed-Stichwort abwesend) — `src/Reporter.E2ETests/FeedDetailTests.cs:518`
- [x] Mobile-UI-Verifikation dokumentiert in `docs/help/anwendung/mobile-ui-design.md` (Abschnitt „Stichwort-Filter pro Feed (issue-116)": statische XAML-Prüfung + automatisierte FlaUI-E2E-Verifikation)

## Offene Aufgaben

- [ ] `FeedDetail_Edit_RemoveKeyword_RemovesChipAndRow` (E2E-Test in `FeedDetailTests`) — fehlt vollständig: Es existiert kein E2E-Test, der ein Stichwort über den „×"-Chip-Button entfernt und sowohl das Verschwinden des Chips als auch das Löschen der `keywords`-Zeile prüft. In `src/Reporter.E2ETests/FeedDetailTests.cs` ist kein Entfernen-Szenario vorhanden.
- [ ] `FeedDetail_Edit_AddKeyword_PersistsAndShowsChip` (E2E-Test in `FeedDetailTests`) — teilweise umgesetzt: Hinzufügen, Chip-Anzeige und DB-Nachweis (`KeywordExistsForFeedAsync` mit `feed_id`) werden innerhalb von `FeedDetail_FeedKeyword_FiltersOnlyOwnFeed` mitgeprüft (Z. 455-467), aber der geplante Persistenznachweis über Schließen und erneutes Öffnen des Sheets (Chip bleibt nach Reopen sichtbar) fehlt; es gibt keinen eigenständigen Add-Persistenz-Test.

## Hinweise

- `RetentionCleanupService.CleanupAsync`: Der Plan sah als Ersatz für den entfernten globalen Frühausstieg konkret vor, Kandidaten nur zu laden, wenn mindestens ein Stichwort existiert. Implementiert ist stattdessen eine äquivalente Prüfung pro Feed-Gruppe (`keywordTexts.Count == 0 → continue`, `RetentionCleanupService.cs:58-62`) — das beobachtbare Verhalten ist identisch (keine Löschung ohne Stichworte), nur die Kandidatenabfrage läuft immer.
- Drei E2E-Testmethodennamen weichen vom Plan ab (`FeedDetail_FeedKeyword_FiltersOnlyOwnFeed`, `FeedDetail_Edit_FeedKeywordValidation_ShowsErrors`, `FeedDetail_FeedKeyword_NotInGlobalSettings`); die Szenarien entsprechen den geplanten Pflicht-Tests.
- Die Ausführung von `.\scripts\Run-StaticChecks.ps1` (Tasks-#33) hinterlässt kein Artefakt und ist im Review nicht verifizierbar — vor dem finalen Commit ausführen.
