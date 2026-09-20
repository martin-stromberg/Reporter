<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

## Fehlgeschlagene Tests

### Reporter.E2ETests.ArticleLinkTests

- **ExternalLinkInArticle_OpensSystemBrowser** — `System.TimeoutException : Element not found within 00:00:20: name 'link-feed article'` (preexisting/instabil: derselbe Test ist bereits in Iteration 1 flaky fehlgeschlagen und laut `inventory/tests.md`-Kontext flake-anfällig; in Iteration 2 bestand er. Kein Bezug zum Feature-Delta — das einzige Delta seit dem letzten Lauf ist der lokalisierte Hinweistext `FeedKeywordsInfo`, den kein E2E-Test assertiert.)

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Stichwort im Bearbeiten-Sheet hinzufügen, Chip sichtbar, persistent über Sheet-Schließen/-Öffnen und in DB (`feed_id`) | `FeedDetail_Edit_AddKeyword_PersistsAndShowsChip` (`FeedDetailTests`) | Bestanden |
| Stichwort per „×"-Chip über die UI entfernen, Chip und DB-Zeile weg | `FeedDetail_Edit_RemoveKeyword_RemovesChipAndRow` (`FeedDetailTests`) | Bestanden |
| Feed-spezifische Filterwirkung beim Sync: Feed A filtert Stub-Artikel, Feed B speichert denselben Artikel | `FeedDetail_FeedKeyword_FiltersOnlyOwnFeed` (`FeedDetailTests`, entspricht geplantem `FeedDetail_Keyword_FiltersItemsOnSync_OnlyForOwnFeed`) | Bestanden |
| Sichtbare Validierungsfehler im Sheet: leer, überlang (501 Zeichen), Duplikat — jeder Zweig einzeln | `FeedDetail_Edit_FeedKeywordValidation_ShowsErrors` (`FeedDetailTests`, entspricht geplantem `FeedDetail_Edit_KeywordValidation_ShowsErrors`) | Bestanden |
| Sichtbarkeitsregel: Feed-Stichworte bleiben in der globalen `SettingsPage`-Liste verborgen (mit Kontroll-Chip) | `FeedDetail_FeedKeyword_NotInGlobalSettings` (`FeedDetailTests`, entspricht geplantem `FeedDetail_Keyword_NotShownInGlobalSettings`) | Bestanden |
| Benachrichtigungs-Unterdrückung pro Feed (`NotificationService`) | `NotificationServiceTests` (Unit, begründet kein E2E — siehe `plan-check.md`) | Bestanden (Unit) |
| Keyword-Löschregel im Retention-Lauf pro Feed (`RetentionCleanupService`) | `RetentionCleanupServiceTests` (Unit, begründet kein E2E — siehe `plan-check.md`) | Bestanden (Unit) |
| Feed-Löschung entfernt dessen Stichworte (Kaskade) | `FeedRepositoryTests.DeleteAsync_RemovesFeedScopedKeywords_KeepsGlobal` (Integration, begründet kein E2E — siehe `plan-check.md`) | Bestanden (Unit/Integration) |

## Zusammenfassung

- Gesamt: 699
- Bestanden: 698
- Fehlgeschlagen: 1
- Übersprungen: 0

(Unit/Integration `Reporter.Tests`: 668 bestanden, 0 fehlgeschlagen, 0 übersprungen — inkl. aller geplanten neuen Tests wie `LoadAsync_LoadsFeedKeywords`, `AddFeedKeyword_*`, `RemoveFeedKeyword_*`, `GetEffectiveForFeedAsync_ReturnsGlobalAndFeedUnion`, `SyncFeedAsync_FeedKeyword*`, `CleanupAsync_FeedKeywordMatch_DeletesOnlyOwnFeed`. E2E `Reporter.E2ETests`: 31 gesamt, 30 bestanden, 1 fehlgeschlagen. Alle 5 geplanten E2E-Pflichtszenarien sind implementiert und bestanden im Gesamtlauf. Beide `ArticleImageTests` bestanden in diesem Lauf — der einzige Fehlschlag ist der dokumentierte Flaky-Test `ArticleLinkTests.ExternalLinkInArticle_OpensSystemBrowser`. Vorausgehender `dotnet build Reporter.sln` (mit `IncludeIosTarget=false`): 0 Warnungen, 0 Fehler.)

## Testabdeckung

**Abdeckung:** 86,6 % (Zeilen, gesamt; Coverlet `XPlat Code Coverage`, `coverage.cobertura.xml` unter `src/Reporter.Tests/TestResults/`)

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\Models\CategoryFilterItem.cs` | 22,2 % |
| `Reporter.Core\Resources\Strings\AppResources.Designer.cs` | 24,8 % (generierte Datei) |
| `Reporter.Core\Services\FeedSearchUnavailableException.cs` | 33,3 % |
| `Reporter.Data\Entities\Keyword.cs` | 75,0 % |
