<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Feed-spezifische Stichworte sind pro Feed pflegbar (Hinzufügen/Entfernen) im Bearbeiten-Sheet der `FeedDetailPage` | `FeedDetailViewModel` (IKeywordRepository, `FeedKeywords`, `AddFeedKeywordCommand`, `RemoveFeedKeywordCommand`), `FeedDetailPage.xaml` Stichwort-Abschnitt (Z. 116–128, Programmablauf Z. 25–31) | `FeedDetailViewModelTests`: `LoadAsync_LoadsFeedKeywords`, `AddFeedKeyword_Valid_PersistsWithFeedId`, `RemoveFeedKeyword_DeletesFromRepository`, `CloseEditForm_ResetsKeywordInput`; E2E: `FeedDetail_Edit_AddKeyword_PersistsAndShowsChip`, `FeedDetail_Edit_RemoveKeyword_RemovesChipAndRow` | Abgedeckt |
| Feed-Stichworte wirken als Blacklist beim Abruf genau dieses Feeds | `FeedSyncService.RunSyncAsync` → `GetKeywordTextsAsync(feed.Id)` (Z. 36–37, 102) | `FeedSyncServiceTests`: `SyncFeedAsync_FeedKeywordMatch_NotSaved`, `SyncFeedAsync_FeedKeyword_OtherFeedUnaffected`; `KeywordFilterTests_E2E`: `E2E_KeywordFilter_FeedScoped_NotInUnreadList`; E2E: `FeedDetail_Keyword_FiltersItemsOnSync_OnlyForOwnFeed` | Abgedeckt |
| Wirksame Liste = Union aus globalen und Feed-Stichworten; globale Liste bleibt für alle Feeds wirksam | `IKeywordFilter.GetKeywordTextsAsync(Guid? feedId)`, `GetEffectiveForFeedAsync` (Z. 14, 84, 94–98) | `KeywordRepositoryTests.GetEffectiveForFeedAsync_ReturnsGlobalAndFeedUnion`, `FeedSyncServiceTests.SyncFeedAsync_FeedKeyword_CombinedWithGlobal` | Abgedeckt |
| Filter gilt auch für die Benachrichtigungs-Unterdrückung (`NotificationService`) | `NotifyNewItemsAsync` → `GetKeywordTextsAsync(feed.Id)` (Z. 43–44, 106) | `NotificationServiceTests`: `NotifyNewItemsAsync_FeedKeywordMatch_SkipsItem`, `NotifyNewItemsAsync_FeedKeyword_OtherFeedScope` | Abgedeckt |
| Keyword-Löschregel (`RetentionCleanupService`) wirkt feed-spezifisch gruppiert | `CleanupAsync` gruppiert Kandidaten nach `Item.FeedId`, wirksame Liste pro Gruppe (Z. 50–54, 110) | `RetentionCleanupServiceTests`: `CleanupAsync_FeedKeywordMatch_DeletesOnlyOwnFeed`, `CleanupAsync_GlobalKeyword_StillDeletesAcrossFeeds` | Abgedeckt |
| Persistenz: `keywords.feed_id` (nullable FK), Unique-Index auf `(feed_id, keyword_text)` plus erhaltene globale Eindeutigkeit, EF-Migration | `Entities.Keyword`/`Models.Keyword` um `FeedId`, `ConfigureKeyword` (Composite- + gefilterter Unique-Index), Migration `AddKeywordFeedId` (Z. 72–80, 143) | `KeywordRepositoryTests`: `GetByFeedAsync_*`, `AddAsync_SameTextInDifferentFeeds_Succeeds`, `AddAsync_DuplicateInSameFeed_Throws`, `AddAsync_DuplicateGlobal_Throws` | Abgedeckt |
| Beim Löschen eines Feeds werden dessen Stichworte mitentfernt | FK `DeleteBehavior.Cascade` auf `keywords.feed_id` (Z. 58–62, 80) | `FeedRepositoryTests.DeleteAsync_RemovesFeedScopedKeywords_KeepsGlobal`; E2E-Bereinigung via `FeedDbAssertions.DeleteAllFeedsAsync`-Erweiterung | Abgedeckt |
| Validierung der Stichwort-Eingabe (nicht leer, ≤ `MaxKeywordLength` 500, kein Duplikat pro Feed) mit sichtbarer Fehlermeldung | Eigenes Fehlerpaar `FeedKeywordErrorMessage`/`HasFeedKeywordError` + Fehler-`Label` im Sheet (Z. 16, 27, 120, 149–151) | Unit: `AddFeedKeyword_Empty_ShowsError`, `AddFeedKeyword_TooLong_ShowsError`, `AddFeedKeyword_Duplicate_ShowsError`; E2E nur für Duplikat: `FeedDetail_Edit_KeywordDuplicate_ShowsError` | Lücke — sichtbare Fehlerfälle „leer" und „zu lang" ohne E2E-Szenario |
| Globale `SettingsPage` zeigt weiterhin nur globale Stichworte (Feed-Stichworte bleiben dort verborgen) | `SettingsViewModel.LoadAsync` → `GetByFeedAsync(null)` (Z. 18, 114, 160) | Unit: `SettingsViewModelTests_Keywords.LoadAsync_ExcludesFeedScopedKeywords` — kein E2E, keine Begründung | Lücke — UI-beobachtbare Sichtbarkeitsregel ohne E2E-Nachweis |
| Neue UI-Texte lokalisiert (EN + DE) inkl. Accessibility-Texte | Neue `AppResources`-Schlüssel `FeedKeywordsLabel`, `FeedKeywordPlaceholder`, `FeedKeywordAdd`, `FeedKeywordRemoveFormat`, `FeedKeywordsInfo` in beiden resx + Designer (Z. 130–133); `SemanticProperties.Description` über `FeedKeywordRemoveFormat`/`WaitForFeedKeywordEntry` | Indirekt über die Sheet-E2E-Tests (Lokalisierung des Fehlerlabels in `FeedDetail_Edit_KeywordDuplicate_ShowsError`) | Abgedeckt |
| Mobile-UI-Vorgaben (390 × 844 pt, `AppThemeBinding`, 44×44-Touch-Targets, kein verschachteltes `CollectionView`/`ScrollView`) | Sheet-Inhalt in höhenbegrenzte `ScrollView`, `AppThemeBinding`, Touch-Targets (Z. 128, 195) | Manuelle Verifikation in Schritt 10 „Verifikation" (Z. 207) dokumentieren | Abgedeckt |
| Match-Semantik bleibt fest verdrahtet (`OrdinalIgnoreCase`, Titel + ContentHtml), kein Whitelist-Modus | `IKeywordMatcher`/`KeywordMatcher` unverändert (Z. 19, 39) | Bestehende `KeywordMatcherTests` bleiben unverändert gültig | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] E2E-Szenario für die Sichtbarkeitsregel „Feed-Stichworte dürfen nicht in der globalen Stichwortliste der `SettingsPage` erscheinen" (Akzeptanzkriterium: globale Liste bleibt rein global; der Plan benennt das Risiko selbst in „Seiteneffekte und Risiken", plant aber nur den Unit-Test `LoadAsync_ExcludesFeedScopedKeywords` — die Regel ist über die UI direkt beobachtbar, daher ist ein E2E-Nachweis oder eine nachvollziehbare Begründung erforderlich)
- [ ] E2E-Szenario für den sichtbaren Fehlerfall „leere Stichwort-Eingabe → `ErrorKeywordEmpty`-Label im Bearbeiten-Sheet" (Akzeptanzkriterium: Validierung mit sichtbarer Fehlermeldung; bisher nur Unit-Test `AddFeedKeyword_Empty_ShowsError`)
- [ ] E2E-Szenario für den sichtbaren Fehlerfall „überlange Stichwort-Eingabe (> 500 Zeichen) → `ErrorKeywordTooLong`-Label im Bearbeiten-Sheet" (bisher nur Unit-Test `AddFeedKeyword_TooLong_ShowsError`)

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Stichwort im Bearbeiten-Sheet hinzufügen, Chip sichtbar, persistent über Sheet-Schließen/-Öffnen und in DB (`feed_id`) | `FeedDetail_Edit_AddKeyword_PersistsAndShowsChip` (`FeedDetailTests`) | Abgedeckt |
| Stichwort per „×"-Chip über die UI entfernen, Chip und DB-Zeile weg | `FeedDetail_Edit_RemoveKeyword_RemovesChipAndRow` (`FeedDetailTests`) | Abgedeckt |
| Feed-spezifische Filterwirkung beim Sync: Feed A filtert Stub-Artikel, Feed B speichert denselben Artikel | `FeedDetail_Keyword_FiltersItemsOnSync_OnlyForOwnFeed` (`FeedDetailTests`) | Abgedeckt |
| Sichtbarer Validierungsfehler (Duplikat) im Sheet | `FeedDetail_Edit_KeywordDuplicate_ShowsError` (`FeedDetailTests`) | Abgedeckt |
| Sichtbarer Validierungsfehler „leere Eingabe" im Sheet | — (nur Unit-Test) | Lücke |
| Sichtbarer Validierungsfehler „Eingabe zu lang" im Sheet | — (nur Unit-Test) | Lücke |
| Sichtbarkeitsregel: Feed-Stichworte bleiben in der globalen `SettingsPage`-Stichwortliste verborgen | — (nur Unit-Test `LoadAsync_ExcludesFeedScopedKeywords`) | Lücke |
| Benachrichtigungs-Unterdrückung pro Feed | nur `NotificationServiceTests` (Unit) | Nicht erforderlich mit Begründung: Lokale Benachrichtigungen erscheinen außerhalb des FlaUI-Fensters und sind nicht zuverlässig UI-beobachtbar; die Logik ist auf Service-Ebene nachgewiesen |
| Keyword-Löschregel im Retention-Lauf pro Feed | nur `RetentionCleanupServiceTests` (Unit) | Nicht erforderlich mit Begründung: Hintergrund-Bereinigung ohne direkt auslösende Nutzeraktion und ohne eigenes sichtbares UI-Ergebnis; Logik auf Service-Ebene nachgewiesen |

## Fehlende oder unvollständige Planbestandteile

- [ ] Keine Umsetzungslücken in den Produktivbestandteilen festgestellt — die Lücken betreffen ausschließlich die E2E-Testabdeckung (s. Abschnitt „Fehlende oder unvollständige Testanforderungen"). Zu ergänzen ist entweder das jeweilige E2E-Szenario oder eine nachvollziehbare Begründung, warum der betreffende über die UI beobachtbare Fall bewusst nur auf Unit-/Integrationsebene nachgewiesen wird.

## Hinweise

- Der E2E-Test `FeedDetail_Edit_KeywordDuplicate_ShowsError` weist den Fehleranzeige-Mechanismus (Binding `FeedKeywordErrorMessage`/`HasFeedKeywordError` auf ein sichtbares `Label`) bereits nach. Falls die Nachplanung die Varianten „leer"/„zu lang" als durch denselben Mechanismus abgedeckt bewertet, sollte der Plan dies explizit begründen — aktuell fehlt jede Begründung für die fehlende E2E-Abdeckung dieser sichtbaren Fehlerfälle.
- Die restliche Planqualität ist hoch: Alle in `requirement.md` genannten Komponenten sind mit konkreten Änderungen versehen, die Umsetzungsreihenfolge enthält ausführbare Schritte mit Voraussetzungen, sämtliche offenen Fragen sind in den Designentscheidungen aufgelöst (Union-Semantik, Wirkungsbereich auf alle drei Services, Pflegeort Bearbeiten-Sheet, Cross-Scope-Duplikate erlaubt, reine Blacklist), betroffene Bestandstests und E2E-Infrastruktur (`FeedDbAssertions.DeleteAllFeedsAsync`, `KeywordExistsForFeedAsync`, `WaitForFeedKeywordEntry`) sowie Testdaten/Fixtures sind vollständig benannt.
- `ServiceCollectionTests` wurde zu Recht nicht als betroffen gelistet: Es entstehen keine neuen DI-Registrierungen (`IKeywordRepository` ist bereits Singleton, `MauiProgram` Z. 86), die neue `FeedDetailViewModel`-Abhängigkeit wird automatisch aufgelöst.
