<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Feed-spezifische Stichworte sind pro Feed pflegbar (Hinzufügen/Entfernen) im Bearbeiten-Sheet der `FeedDetailPage` | `FeedDetailViewModel` (`IKeywordRepository`-Abhängigkeit, `FeedKeywords`, `AddFeedKeywordCommand`, `RemoveFeedKeywordCommand`), `FeedDetailPage.xaml` Stichwort-Abschnitt (Z. 116–128, Programmablauf Z. 25–31) | `FeedDetailViewModelTests`: `LoadAsync_LoadsFeedKeywords`, `AddFeedKeyword_Valid_PersistsWithFeedId`, `RemoveFeedKeyword_DeletesFromRepository`, `CloseEditForm_ResetsKeywordInput`; E2E: `FeedDetail_Edit_AddKeyword_PersistsAndShowsChip`, `FeedDetail_Edit_RemoveKeyword_RemovesChipAndRow` | Abgedeckt |
| Feed-Stichworte wirken als Blacklist beim Abruf genau dieses Feeds | `FeedSyncService.RunSyncAsync` → `GetKeywordTextsAsync(feed.Id)` (Z. 36–37, 102) | `FeedSyncServiceTests`: `SyncFeedAsync_FeedKeywordMatch_NotSaved`, `SyncFeedAsync_FeedKeyword_OtherFeedUnaffected`; `KeywordFilterTests_E2E`: `E2E_KeywordFilter_FeedScoped_NotInUnreadList`; E2E: `FeedDetail_Keyword_FiltersItemsOnSync_OnlyForOwnFeed` | Abgedeckt |
| Wirksame Liste = Union aus globalen und Feed-Stichworten; globale Liste bleibt für alle Feeds wirksam | `IKeywordFilter.GetKeywordTextsAsync(Guid? feedId)`, `GetEffectiveForFeedAsync` (Z. 14, 84, 94–98) | `KeywordRepositoryTests.GetEffectiveForFeedAsync_ReturnsGlobalAndFeedUnion`, `FeedSyncServiceTests.SyncFeedAsync_FeedKeyword_CombinedWithGlobal` | Abgedeckt |
| Filter gilt auch für die Benachrichtigungs-Unterdrückung (`NotificationService`) | `NotifyNewItemsAsync` → `GetKeywordTextsAsync(feed.Id)` (Z. 43–44, 106) | `NotificationServiceTests`: `NotifyNewItemsAsync_FeedKeywordMatch_SkipsItem`, `NotifyNewItemsAsync_FeedKeyword_OtherFeedScope` | Abgedeckt |
| Keyword-Löschregel (`RetentionCleanupService`) wirkt feed-spezifisch gruppiert | `CleanupAsync` gruppiert Kandidaten nach `Item.FeedId`, wirksame Liste pro Gruppe, angepasster Frühausstieg (Z. 50–54, 110) | `RetentionCleanupServiceTests`: `CleanupAsync_FeedKeywordMatch_DeletesOnlyOwnFeed`, `CleanupAsync_GlobalKeyword_StillDeletesAcrossFeeds` | Abgedeckt |
| Persistenz: `keywords.feed_id` (nullable FK), Unique-Index auf `(feed_id, keyword_text)` plus erhaltene globale Eindeutigkeit, EF-Migration | `Entities.Keyword`/`Models.Keyword` um `FeedId`, `ConfigureKeyword` (Composite- + gefilterter Unique-Index `WHERE feed_id IS NULL`), Migration `AddKeywordFeedId` (Z. 72–80, 143) | `KeywordRepositoryTests`: `GetByFeedAsync_*`, `AddAsync_SameTextInDifferentFeeds_Succeeds`, `AddAsync_DuplicateInSameFeed_Throws`, `AddAsync_DuplicateGlobal_Throws` | Abgedeckt |
| Beim Löschen eines Feeds werden dessen Stichworte mitentfernt | FK `DeleteBehavior.Cascade` auf `keywords.feed_id` (Z. 58–62, 80) | `FeedRepositoryTests.DeleteAsync_RemovesFeedScopedKeywords_KeepsGlobal`; E2E-Bereinigung via `FeedDbAssertions.DeleteAllFeedsAsync`-Erweiterung | Abgedeckt |
| Validierung der Stichwort-Eingabe (nicht leer, ≤ `MaxKeywordLength` 500, kein Duplikat pro Feed) mit sichtbarer Fehlermeldung | Eigenes Fehlerpaar `FeedKeywordErrorMessage`/`HasFeedKeywordError` + Fehler-`Label` im Sheet (Z. 16, 27, 120, 149–151) | Unit: `AddFeedKeyword_Empty_ShowsError`, `AddFeedKeyword_TooLong_ShowsError`, `AddFeedKeyword_Duplicate_ShowsError`; E2E: `FeedDetail_Edit_KeywordValidation_ShowsErrors` deckt alle drei sichtbaren Fehlerfälle einzeln ab | Abgedeckt |
| Globale `SettingsPage` zeigt weiterhin nur globale Stichworte (Feed-Stichworte bleiben dort verborgen) | `SettingsViewModel.LoadAsync` → `GetByFeedAsync(null)` (Z. 18, 114, 160) | Unit: `SettingsViewModelTests_Keywords.LoadAsync_ExcludesFeedScopedKeywords`; E2E: `FeedDetail_Keyword_NotShownInGlobalSettings` mit positivem Kontroll-Chip als Nachweis des Renderns | Abgedeckt |
| Neue UI-Texte lokalisiert (EN + DE) inkl. Accessibility-Texte | Neue `AppResources`-Schlüssel `FeedKeywordsLabel`, `FeedKeywordPlaceholder`, `FeedKeywordAdd`, `FeedKeywordRemoveFormat`, `FeedKeywordsInfo` in beiden resx + Designer; `SemanticProperties.Description` über `FeedKeywordRemoveFormat`/`WaitForFeedKeywordEntry` (Z. 130–133, 236) | Indirekt über die Sheet-E2E-Tests (lokalisierte Fehlertexte in `FeedDetail_Edit_KeywordValidation_ShowsErrors`, Entry-Lokalisierung per `SemanticProperties.Description`) | Abgedeckt |
| Mobile-UI-Vorgaben (390 × 844 pt, `AppThemeBinding`, 44×44-Touch-Targets, kein verschachteltes `CollectionView`/`ScrollView`) | Sheet-Inhalt in höhenbegrenzte `ScrollView`, `AppThemeBinding`, Touch-Targets, `FlexLayout` statt `CollectionView` (Z. 128, 195) | Manuelle Verifikation in Schritt 10 „Verifikation" (Z. 207) dokumentieren | Abgedeckt |
| Match-Semantik bleibt fest verdrahtet (`OrdinalIgnoreCase`, Titel + ContentHtml), kein Whitelist-Modus | `IKeywordMatcher`/`KeywordMatcher` unverändert (Z. 19, 39) | Bestehende `KeywordMatcherTests` bleiben unverändert gültig | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

—

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Stichwort im Bearbeiten-Sheet hinzufügen, Chip sichtbar, persistent über Sheet-Schließen/-Öffnen und in DB (`feed_id`) | `FeedDetail_Edit_AddKeyword_PersistsAndShowsChip` (`FeedDetailTests`) | Abgedeckt |
| Stichwort per „×"-Chip über die UI entfernen, Chip und DB-Zeile weg | `FeedDetail_Edit_RemoveKeyword_RemovesChipAndRow` (`FeedDetailTests`) | Abgedeckt |
| Feed-spezifische Filterwirkung beim Sync: Feed A filtert Stub-Artikel, Feed B speichert denselben Artikel | `FeedDetail_Keyword_FiltersItemsOnSync_OnlyForOwnFeed` (`FeedDetailTests`) | Abgedeckt |
| Sichtbare Validierungsfehler im Sheet: leere Eingabe, überlange Eingabe (501 Zeichen), Duplikat — jeder Zweig einzeln gegen das Fehler-`Label` geprüft | `FeedDetail_Edit_KeywordValidation_ShowsErrors` (`FeedDetailTests`) | Abgedeckt |
| Sichtbarkeitsregel: Feed-Stichworte bleiben in der globalen `SettingsPage`-Stichwortliste verborgen (mit sichtbarem Kontroll-Stichwort als Render-Nachweis) | `FeedDetail_Keyword_NotShownInGlobalSettings` (`FeedDetailTests`) | Abgedeckt |
| Benachrichtigungs-Unterdrückung pro Feed (`NotificationService`) | nur `NotificationServiceTests` (Unit) | Nicht erforderlich mit Begründung: Lokale Benachrichtigungen erscheinen im Windows-Benachrichtigungscenter außerhalb des FlaUI-Fensters und sind nicht zuverlässig UI-beobachtbar; die App hat keine UI, die unterdrückte Benachrichtigungen sichtbar macht. Logik auf Service-Ebene nachgewiesen (Z. 258). |
| Keyword-Löschregel im Retention-Lauf pro Feed (`RetentionCleanupService`) | nur `RetentionCleanupServiceTests` (Unit) | Nicht erforderlich mit Begründung: Die Bereinigung läuft als Hintergrund-Task beim App-Start (`App.xaml.cs` Z. 161–165) ohne auslösende Nutzeraktion und ohne eigenes sichtbares UI-Ergebnis; ein E2E-Test müsste dieselbe Service-Logik wie die Unit-Tests aufrufen (Z. 259). |
| Feed-Löschung entfernt dessen Stichworte (Kaskade) | nur `FeedRepositoryTests.DeleteAsync_RemovesFeedScopedKeywords_KeepsGlobal` (Integration) | Nicht erforderlich mit Begründung: Feed-Stichworte sind ausschließlich im Bearbeiten-Sheet des jeweiligen Feeds sichtbar — nach dem Löschen des Feeds existiert keine UI-Fläche mehr, auf der verwaiste Stichworte beobachtbar wären. Der UI-sichtbare Teil des Flusses (Löschen → Rückkehr zur Liste) ist durch den bestehenden E2E-Test `FeedDetail_Delete_ReturnsToList` abgedeckt; die Kaskade ist ein DB-Effekt und wird auf Repository-Ebene nachgewiesen. |

## Fehlende oder unvollständige Planbestandteile

—

## Hinweise

- Die drei in `plan-check.1.md` gemeldeten E2E-Lücken sind geschlossen: Das kombinierte Szenario `FeedDetail_Edit_KeywordValidation_ShowsErrors` deckt jetzt alle drei sichtbaren Validierungsfälle (leer, zu lang, Duplikat) einzeln ab; `FeedDetail_Keyword_NotShownInGlobalSettings` weist die Sichtbarkeitsregel in der `SettingsPage` nach — inklusive sinnvoller Absicherung der negativen Assertion durch einen sichtbaren Kontroll-Chip und der korrekt angenommenen `LoadCommand`-Ausführung bei jedem `OnAppearing` (`SettingsPage.xaml.cs` Z. 24–32).
- Die Nicht-E2E-Begründungen für `NotificationService` und `RetentionCleanupService` sind nachvollziehbar und gegen den Code verifiziert (`App.xaml.cs` Z. 161–165: Retention läuft nur beim App-Start ohne UI-Trigger).
- Stichprobe der Planfakten gegen den Code bestanden: `ILocalNotificationService` ist tatsächlich der letzte, optionale Konstruktorparameter des `FeedDetailViewModel` (Z. 65) — die geplante Pflicht-Abhängigkeit `IKeywordRepository` davor ist konsistent; `FeedDbAssertions.DeleteAllFeedsAsync` existiert (Z. 217) und ist der richtige Erweiterungspunkt für die Test-Bereinigung.
- Bei der manuellen Mobile-UI-Verifikation (Schritt 10) sollte gemäß `AGENTS.md` zusätzlich der Abgleich mit dem aktuellen `design-draft`-Screen des Flows erfolgen und der getestete Formfaktor dokumentiert werden — der Plan benennt die Verifikationsparameter (390 × 844 pt, Light/Dark) bereits, der Design-Draft-Vergleich ist darin implizit mitzuführen.
- `ServiceCollectionTests` ist zu Recht nicht als betroffen gelistet: Es entstehen keine neuen DI-Registrierungen (`IKeywordRepository` ist bereits Singleton, `MauiProgram` Z. 86).
