<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Stichwort-Filter pro Feed (Issue #116)

## Übersicht

Der bisher rein globale Stichwort-Filter (`keywords`-Tabelle, gepflegt in der `SettingsPage`, angewendet in `FeedSyncService`, `NotificationService` und `RetentionCleanupService`) wird um eine feed-spezifische Variante erweitert: Die `keywords`-Tabelle erhält eine nullable `feed_id`-Spalte, die Filterpipeline liefert pro Feed die wirksame Stichwortliste, und die Pflege der Feed-Stichworte erfolgt im Bearbeiten-Sheet der `FeedDetailPage` — demselben Ort, an dem heute `NotificationsEnabled` feed-spezifisch verwaltet wird, und nach demselben UI-/ViewModel-Muster wie die globale Keyword-Karte in `SettingsPage`/`SettingsViewModel`. Die in der Anforderung formulierte Erweiterung der Feed-Einstellung „um einen Sichten" wurde geklärt und ist als „Stichwort-Filter pro Feed" zu verstehen.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Persistenz der Feed-Stichworte | Gemeinsame `keywords`-Tabelle mit nullablem `feed_id` (`null` = global) statt separater Tabelle | Ein Repository, ein Mapping, einfache Union-Abfrage; Kaskadenlöschung per FK wie bei `items.feed_id`. Eine zweite Tabelle würde Repository, Mapping und Filter-Logik duplizieren, ohne fachlichen Mehrwert. |
| Wirksame Stichwortliste | `IKeywordFilter.GetKeywordTextsAsync(Guid? feedId)` als einziger Einstiegspunkt: `null` = nur globale Liste, `feedId` = Union aus globalen und Feed-Stichworten | Die Union-Regel wird zentral im Filter gekapselt (Service Layer) statt an drei Aufruferstellen dupliziert; die Aufrufer (`FeedSyncService`, `NotificationService`, `RetentionCleanupService`) übergeben nur ihren Feed-Kontext. |
| Eindeutigkeit der Stichworte | Composite-Unique-Index `(feed_id, keyword_text)` **plus** gefilterter Unique-Index `keyword_text WHERE feed_id IS NULL` | SQLite wertet `NULL`-FK-Werte im Composite-Index als verschieden — allein der Composite-Index würde die bisherige globale Eindeutigkeit verlieren. Der Partial Index erhält sie. Duplikate desselben Textes in unterschiedlichen Feeds sowie global und in einem Feed gleichzeitig sind erlaubt (Cross-Scope-Duplikate sind bei Union-Semantik harmlos); eine Cross-Scope-Warnung ist nicht vorgesehen. |
| Stichwort-Fehleranzeige im Edit-Sheet | Eigenes Property-Paar `FeedKeywordErrorMessage`/`HasFeedKeywordError` mit eigenem Label im Stichwort-Abschnitt | Das Sheet nutzt `ErrorMessage`/`HasError` bereits für die URL-Validierung; eine gemeinsame Anzeige würde Stichwort-Fehler mit URL-Fehlern vermischen und beim Tippen im URL-Feld löschen. |
| Persistierungszeitpunkt der Feed-Stichworte | Sofort-Persistierung bei Hinzufügen/Entfernen (wie `SettingsViewModel.AddKeywordAsync`/`RemoveKeywordAsync`), nicht gebündelt in `SaveEditAsync` | Folgt exakt dem etablierten Keyword-Muster; die Speichern/Abbrechen-Semantik des Sheets betrifft weiterhin nur URL und Benachrichtigungs-Schalter. |
| Pflegeort und Sichtbarkeit der Feed-Stichworte | Pflege ausschließlich im Bearbeiten-Sheet der `FeedDetailPage`; `SettingsPage` zeigt weiterhin nur globale Stichworte (`GetByFeedAsync(null)`); kein Übernahme-Dialog „global → Feed" | Der Feed-Kontext ist über die Shell-Route `feeddetail?feedId={id}` bereits etabliert — keine neue Auswahlinteraktion nötig. Feed-Stichworte in der globalen Einstellungsliste oder ein Übernahme-Dialog sind aus der Anforderung nicht ableitbar und wurden vom Anwender nicht gewünscht. |
| Wirkungsbereich und Filter-Richtung | Der Feed-Filter gilt an allen drei Anwendungsstellen (`FeedSyncService`, `NotificationService`, `RetentionCleanupService` — gruppiert nach `Item.FeedId`) und ist ausschließlich eine Blacklist | So bleibt die Filtersemantik über den gesamten Artikel-Lebenszyklus konsistent (Abruf, Benachrichtigung, Bestandsbereinigung). Die Match-Semantik (`IKeywordMatcher`, `OrdinalIgnoreCase`, Titel+Content) bleibt wie beim globalen Filter fest verdrahtet; ein Whitelist-Modus wäre ein separates Feature. |

## Programmabläufe

### Stichwort-Pflege im Bearbeiten-Sheet

1. Anwender öffnet die `FeedDetailPage` über `feeddetail?feedId={id}` → `LoadAsync(feedId)` lädt Feed, Kategorien, erste Artikelseite **und** die Feed-Stichworte via `IKeywordRepository.GetByFeedAsync(feedId)` in `FeedKeywords`.
2. Feed-Aktions-Sheet → `ButtonEdit` → `EditCommand`/`Edit()`: setzt `EditUrl`, `EditNotificationsEnabled`, leert `NewFeedKeywordText` und `FeedKeywordErrorMessage`, `ShowEditForm = true`.
3. Eingabe im Stichwort-`Entry` → `AddFeedKeywordCommand`/`AddFeedKeywordAsync`: Trim → Leer-Prüfung (`ErrorKeywordEmpty`) → Längenprüfung `MaxKeywordLength = 500` (`ErrorKeywordTooLong`) → case-insensitive Duplikatprüfung gegen `FeedKeywords` (`ErrorKeywordDuplicate`) → `new Keyword { Id = Guid.NewGuid(), KeywordText = text, FeedId = _feedId }` → `_keywordRepository.AddAsync` → `FeedKeywords.Add`, Eingabefeld leeren.
4. „×"-Button auf einem Chip → `RemoveFeedKeywordCommand`/`RemoveFeedKeywordAsync(keyword)` → `_keywordRepository.DeleteAsync(keyword.Id)` → `FeedKeywords.Remove`.
5. `CloseEditFormCommand`/`ResetEditForm` oder `SaveEditAsync`: schließt das Sheet und setzt `NewFeedKeywordText`/`FeedKeywordErrorMessage` zurück; die bereits persistierten Stichworte bleiben bestehen (Abbrechen verwirft nur URL/Switch-Eingaben, analog der Sofort-Persistierung in den Einstellungen).

Beteiligte Klassen/Komponenten: `FeedDetailViewModel`, `FeedDetailPage` (XAML), `IKeywordRepository`/`KeywordRepository`, `AppResources`

### Feed-Abruf mit feed-spezifischem Filter

1. `FeedSyncService.SyncFeedAsync(feedId)` lädt den `Feed` und ruft `RunSyncAsync(feed, log, ct)`.
2. `RunSyncAsync` ruft `_keywordFilter.GetKeywordTextsAsync(feed.Id)` — liefert die Union aus globalen (`feed_id IS NULL`) und feed-spezifischen Stichworten (`feed_id = feed.Id`).
3. Die Liste wird wie bisher als `CollectContext.KeywordTexts` an `CollectNewItems` gereicht; `MatchesAny` verwirft Treffer unverändert (`filteredCount++`), der `", N filtered"`-Log-Suffix bleibt.

Beteiligte Klassen/Komponenten: `FeedSyncService`, `IKeywordFilter`/`KeywordFilter`, `IKeywordRepository`, `KeywordMatcher` (unverändert)

### Benachrichtigungs-Unterdrückung pro Feed

1. `NotificationService.NotifyNewItemsAsync(feed, newItems, ct)` (Feed-Parameter bereits vorhanden) ruft `_keywordFilter.GetKeywordTextsAsync(feed.Id)` statt der parameterlosen Variante.
2. `MatchesAny` auf die wirksame Liste schließt Treffer aus Einzel- und Summary-Benachrichtigungen aus — Verhalten wie bisher, nur mit der Feed-spezifischen Liste.

Beteiligte Klassen/Komponenten: `NotificationService`, `IKeywordFilter`

### Keyword-Löschregel im Retention-Lauf pro Feed

1. `RetentionCleanupService.CleanupAsync` holt nach `DeleteExpiredAsync` die Kandidaten via `GetExpiredKeywordCandidatesAsync(cutoff)` (Items tragen `FeedId`).
2. Die Kandidaten werden nach `Item.FeedId` gruppiert; pro Gruppe wird `_keywordFilter.GetKeywordTextsAsync(feedId)` aufgerufen (Union aus globalen und Feed-Stichworten) und `MatchesAny` nur auf die Items dieser Gruppe angewendet.
3. Feeds ohne jegliche Stichworte liefern eine leere Liste — `MatchesAny` liefert `false`, nichts wird gelöscht. Der `keywordTexts.Count > 0`-Frühausstieg entfällt bzw. wird durch eine Prüfung ersetzt, ob überhaupt Stichworte existieren (z. B. `GetKeywordTextsAsync(null)` global plus eine Prüfung auf feed-spezifische Zeilen — konkret: Kandidaten nur laden, wenn mindestens ein Stichwort existiert; die wirksame Liste pro Gruppe kann leer sein).

Beteiligte Klassen/Komponenten: `RetentionCleanupService`, `IKeywordFilter`, `IItemRepository`

### Feed-Löschung mit Stichwort-Kaskade

1. `FeedRepository.DeleteAsync(id)` entfernt den Feed wie bisher; `items` und jetzt auch `keywords`-Zeilen mit `feed_id = id` entfernt die DB-Kaskade (`DeleteBehavior.Cascade`, wie bei `items` — EF Core aktiviert `PRAGMA foreign_keys` für SQLite-Verbindungen).
2. Globale Stichworte (`feed_id IS NULL`) sind nicht betroffen.
3. Kein zusätzlicher Code in `DeleteAsync` nötig; die Kaskade wird per Repository-Test nachgewiesen.

Beteiligte Klassen/Komponenten: `FeedRepository`, `ReporterDbContext` (FK-Konfiguration)

## Neue Klassen

Keine — alle Änderungen erweitern bestehende Klassen. (Die EF-Migrationsklasse wird im Abschnitt „Datenbankmigrationen" geführt.)

## Änderungen an bestehenden Klassen

### `Reporter.Data.Entities.Keyword` (Entity)

- **Neue Eigenschaften:** `FeedId` (`Guid?`) — optionale Feed-Zuordnung, `null` = global; `Feed` (`Feed?`) — Navigation für die FK-Kaskade.

### `Reporter.Core.Models.Keyword` (Datenmodellklasse)

- **Neue Eigenschaften:** `FeedId` (`Guid?`, `init`, Standard `null`) — Feed-Zuordnung im Domain-Modell.

### `ReporterDbContext` (DbContext)

- **Geänderte Methoden:** `ConfigureKeyword` — Spalte `feed_id` (nullable), `HasOne(e => e.Feed).WithMany().HasForeignKey(e => e.FeedId).OnDelete(DeleteBehavior.Cascade)`; Unique-Index auf `keyword_text` ersetzt durch Composite-Unique `(feed_id, keyword_text)` plus gefiltertem Unique-Index `keyword_text` mit `HasFilter("feed_id IS NULL")`.

### `IKeywordRepository` (Interface)

- **Neue Methoden:** `GetByFeedAsync(Guid? feedId)` → `Task<IReadOnlyList<Keyword>>` — Einträge mit `FeedId == feedId` (`null` = nur globale), sortiert nach `KeywordText`; `GetEffectiveForFeedAsync(Guid feedId)` → `Task<IReadOnlyList<Keyword>>` — Union `feed_id IS NULL OR feed_id = feedId`, sortiert nach `KeywordText`.
- **Unverändert:** `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` (`AddAsync`/`UpdateAsync` tragen `FeedId` über das Modell mit).

### `KeywordRepository` (Repository)

- **Neue Methoden:** `GetByFeedAsync`, `GetEffectiveForFeedAsync` — Implementierung der Interface-Erweiterung.
- **Geänderte Methoden:** `MapToModel`/`MapToEntity` — `FeedId` mitführen.

### `IKeywordFilter` (Interface)

- **Geänderte Methoden:** `GetKeywordTextsAsync()` → `GetKeywordTextsAsync(Guid? feedId)` — `null` liefert nur globale Stichworte, ein Wert die Union aus globalen und Feed-Stichworten.

### `KeywordFilter` (Service)

- **Geänderte Methoden:** `GetKeywordTextsAsync(Guid? feedId)` — ruft bei `null` `GetByFeedAsync(null)`, sonst `GetEffectiveForFeedAsync(feedId)`; projiziert weiterhin auf `KeywordText`.

### `FeedSyncService` (Service)

- **Geänderte Methoden:** `RunSyncAsync` — `GetKeywordTextsAsync(feed.Id)` statt parameterlos.

### `NotificationService` (Service)

- **Geänderte Methoden:** `NotifyNewItemsAsync` — `GetKeywordTextsAsync(feed.Id)` statt parameterlos.

### `RetentionCleanupService` (Service)

- **Geänderte Methoden:** `CleanupAsync` — Keyword-Regel gruppiert die Kandidaten nach `Item.FeedId` und ruft `GetKeywordTextsAsync(feedId)` pro Gruppe auf; der Frühausstieg prüft, ob überhaupt Stichworte existieren.

### `SettingsViewModel` (ViewModel)

- **Geänderte Methoden:** `LoadAsync` — lädt `GetByFeedAsync(null)` statt `GetAllAsync`, damit Feed-Stichworte nicht in der globalen Liste erscheinen.

### `FeedDetailViewModel` (ViewModel)

- **Neue Abhängigkeit:** `IKeywordRepository` (Pflicht-Konstruktorparameter, vor dem optionalen `ILocalNotificationService`).
- **Neue Konstanten:** `MaxKeywordLength = 500` (private const, wie `SettingsViewModel`).
- **Neue Eigenschaften:** `FeedKeywords` (`ObservableCollection<Keyword>`) — Stichworte des aktuellen Feeds; `NewFeedKeywordText` (`string`, Setter setzt `FeedKeywordErrorMessage` zurück); `FeedKeywordErrorMessage` (`string`) und `HasFeedKeywordError` (`bool`) — eigenes Fehlerpaar für den Stichwort-Abschnitt.
- **Neue Commands:** `AddFeedKeywordCommand` (`AsyncRelayCommand`) → `AddFeedKeywordAsync`; `RemoveFeedKeywordCommand` (`AsyncRelayCommand<Keyword>`) → `RemoveFeedKeywordAsync`.
- **Geänderte Methoden:** `LoadAsync` — befüllt zusätzlich `FeedKeywords` via `GetByFeedAsync(feedId)`; `Edit` — setzt `NewFeedKeywordText`/`FeedKeywordErrorMessage` zurück; `ResetEditForm` — räumt `NewFeedKeywordText`/`FeedKeywordErrorMessage` zusätzlich auf.
- **Unverändert:** `SaveEditAsync`/`ToFeed` — Stichworte sind nicht Teil des `Feed`-Modells und laufen nicht über den Speichern-Button.

### `FeedDetailPage.xaml` (View)

- Erweiterung des Bearbeiten-Sheets (Border-Inhalt, Z. 288–346) um einen Stichwort-Abschnitt nach dem Muster der `SettingsPage`-Keyword-Karte (Z. 61–142): Abschnitts-Label, `Grid` aus `Entry` (`NewFeedKeywordText`, `FeedKeywordPlaceholder`, `ReturnCommand` = `AddFeedKeywordCommand`) + Hinzufügen-`Button`, Fehler-`Label` (`FeedKeywordErrorMessage`/`HasFeedKeywordError`), `FlexLayout Wrap="Wrap"` mit `BindableLayout.ItemsSource="{Binding FeedKeywords}"` und Chip-`DataTemplate` (`x:DataType="models:Keyword"`, „×"-`Button` mit `RemoveFeedKeywordCommand`, `FeedKeywordRemoveFormat`), Info-`Border` (`FeedKeywordsInfo`).
- Mobile-UI-Vorgaben (AGENTS.md): `AppThemeBinding` für alle neuen Farben, Touch-Targets ≥ 44 × 44 pt, kein verschachteltes `CollectionView`/`ScrollView` — der wachsende Sheet-Inhalt wird in eine `ScrollView` mit Höhenbegrenzung eingebettet (`FlexLayout` ist darin zulässig; die Artikel-`CollectionView` liegt außerhalb des Sheets).

### `AppResources` (`AppResources.resx` EN, `AppResources.de.resx` DE, `AppResources.Designer.cs`)

- Neue Schlüssel: `FeedKeywordsLabel`, `FeedKeywordPlaceholder`, `FeedKeywordAdd`, `FeedKeywordRemoveFormat`, `FeedKeywordsInfo`. Die Designer-Datei wird wie bei früheren Features manuell mitgepflegt (Vorbild: Tasks der issue-115-Umsetzung).
- Wiederverwendet: `ErrorKeywordEmpty`, `ErrorKeywordTooLong`, `ErrorKeywordDuplicate`.

### `MauiProgram` (DI)

- Keine Änderung: `IKeywordRepository` ist bereits als Singleton registriert (Z. 86); die neue `FeedDetailViewModel`-Abhängigkeit wird von der DI automatisch aufgelöst.

## Datenbankmigrationen

| Migrationsname | Betroffene Tabellen/Spalten | Beschreibung der Änderung |
|----------------|----------------------------|---------------------------|
| `AddKeywordFeedId` (Konvention `<timestamp>_AddKeywordFeedId`) | `keywords`: neue Spalte `feed_id` (nullable, FK `feeds.id`, `ON DELETE CASCADE`); Index `IX_keywords_keyword_text` ersetzt durch `IX_keywords_feed_id_keyword_text` (unique) und `IX_keywords_keyword_text` (unique, `WHERE feed_id IS NULL`) | Feed-Zuordnung der Stichworte; Eindeutigkeit pro Feed plus erhaltene globale Eindeutigkeit. Scaffold via `dotnet ef migrations add AddKeywordFeedId --project src/Reporter.Data` (Design-Time-Factory `ReporterDbContextFactory` vorhanden); `ReporterDbContextModelSnapshot` wird mit aktualisiert. Bestehende Zeilen bleiben global (`feed_id = NULL`) — kein Daten-Backfill nötig. |

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `NewFeedKeywordText` | Nach Trim nicht leer | `FeedKeywordErrorMessage` = `AppResources.ErrorKeywordEmpty` |
| `NewFeedKeywordText` | Länge ≤ `MaxKeywordLength` (500) | `FeedKeywordErrorMessage` = `AppResources.ErrorKeywordTooLong` |
| `NewFeedKeywordText` | Kein Duplikat innerhalb desselben Feeds (`OrdinalIgnoreCase` gegen `FeedKeywords`) | `FeedKeywordErrorMessage` = `AppResources.ErrorKeywordDuplicate` |
| `keywords`-Zeile | DB-seitig unique pro `(feed_id, keyword_text)`; global unique für `feed_id IS NULL` | `DbUpdateException` bei Regelverletzung (zusätzliche Schutzschicht zur UI-Prüfung) |

## Konfigurationsänderungen

Keine — die Feed-Stichworte sind pro Datensatz in der SQLite-Datenbank persistiert; `appsettings`/Konfigurationsklassen bleiben unverändert. Die Match-Semantik ist laut Business Rule fest verdrahtet und nicht konfigurierbar.

## Seiteneffekte und Risiken

- **`SettingsViewModel`:** `LoadAsync` nutzt bisher `GetAllAsync` — ohne Umstellung auf `GetByFeedAsync(null)` würden Feed-Stichworte plötzlich in der globalen Keyword-Karte auftauchen und dort global gelöscht werden können. Umstellung ist eingeplant.
- **`FeedDbAssertions.DeleteAllFeedsAsync` (E2E-Helper):** Löscht `items`/`feeds` explizit, weil die Test-Verbindung keine FK-Durchsetzung garantiert — Feed-Stichworte würden zwischen Tests zurückbleiben und Folgetests verfälschen. Muss um `DELETE FROM keywords WHERE feed_id IS NOT NULL` ergänzt werden.
- **`FeedDetail_Edit_PersistsChanges` (E2E):** Das Sheet wächst um den Stichwort-Abschnitt; die Einbettung in eine `ScrollView` muss sicherstellen, dass `EditUrlEntry` und der Speichern-Button erreichbar bleiben. Funktionale Anpassung nicht erwartet, aber zu verifizieren.
- **Signaturänderung `IKeywordFilter.GetKeywordTextsAsync`:** Alle drei Produktiv-Aufrufer werden angepasst; die Tests instanziieren `KeywordFilter` konkret (keine Interface-Fakes), daher kein Compile-Bruch in Test-Doubles — direkte Aufrufe der alten Signatur in Tests sind ggf. mitzuziehen.
- **Feed-Löschung:** Die `keywords`-Kaskade hängt an der FK-Durchsetzung der SQLite-Verbindung — wie bei `items` bereits etabliert; Nachweis per `FeedRepositoryTests`.
- **Sync-Log:** Der `", N filtered"`-Zähler umfasst künftig auch Feed-Stichwort-Treffer — Semantik unverändert, keine Formatänderung.

## Umsetzungsreihenfolge

1. **Datenmodell und Migration**
   - Voraussetzungen: `dotnet ef`-Tooling (Design-Time-Factory `ReporterDbContextFactory` vorhanden; falls das Tool fehlt: Migration manuell nach Vorbild `20260911181811_AddFeedNotificationsEnabled` anlegen).
   - Beschreibung: `Entities.Keyword` um `FeedId`/`Feed` erweitern, `Models.Keyword` um `FeedId`, `ConfigureKeyword` anpassen (FK-Kaskade, Composite-Unique, gefilterter Unique-Index), Migration `AddKeywordFeedId` scaffolden und Snapshot prüfen.

2. **Repository-Erweiterung**
   - Voraussetzungen: Schritt 1 (Entity/Modell tragen `FeedId`).
   - Beschreibung: `IKeywordRepository` um `GetByFeedAsync(Guid? feedId)` und `GetEffectiveForFeedAsync(Guid feedId)` erweitern; `KeywordRepository` implementieren inkl. `FeedId`-Mapping.

3. **Filterpipeline feed-spezifisch machen**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: `IKeywordFilter`/`KeywordFilter` auf `GetKeywordTextsAsync(Guid? feedId)` umstellen; Aufrufer `FeedSyncService.RunSyncAsync` (`feed.Id`), `NotificationService.NotifyNewItemsAsync` (`feed.Id`) und `RetentionCleanupService.CleanupAsync` (Gruppierung nach `Item.FeedId`) anpassen.

4. **Globale Einstellungsliste absichern**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: `SettingsViewModel.LoadAsync` auf `GetByFeedAsync(null)` umstellen.

5. **Lokalisierung**
   - Voraussetzungen: Keine.
   - Beschreibung: Neue `AppResources`-Schlüssel in `AppResources.resx`, `AppResources.de.resx` und `AppResources.Designer.cs` anlegen.

6. **ViewModel-Stichwortverwaltung**
   - Voraussetzungen: Schritt 2 (Repository-Methoden), Schritt 5 (Fehler-/Beschriftungsschlüssel vorhanden — `ErrorKeyword*` bereits vorhanden).
   - Beschreibung: `FeedDetailViewModel` um `IKeywordRepository`-Abhängigkeit, `FeedKeywords`, `NewFeedKeywordText`, `FeedKeywordErrorMessage`/`HasFeedKeywordError`, `AddFeedKeywordCommand`, `RemoveFeedKeywordCommand` und Laden in `LoadAsync` erweitern.

7. **Sheet-UI**
   - Voraussetzungen: Schritte 5 und 6.
   - Beschreibung: `FeedDetailPage.xaml` Edit-Sheet um den Stichwort-Abschnitt nach `SettingsPage`-Muster erweitern (Entry + Add-Button, Fehler-Label, Chip-`FlexLayout`, Info-Box), Sheet-Inhalt in höhenbegrenzte `ScrollView` einbetten; `AppThemeBinding` und 44×44-Touch-Targets einhalten.

8. **Unit-/Integrationstests**
   - Voraussetzungen: Schritte 1–7 für die jeweils betroffenen Bereiche; Testinfrastruktur (`TestDbContextFactory`, `TestDataSeeder`, `TestFeedXml`, `FakeHttpMessageHandler`, `FakeLocalNotificationService`) vorhanden.
   - Beschreibung: Neue Repository-/Service-/ViewModel-Tests (s. Abschnitt „Tests") schreiben und `FeedDetailViewModelTests.CreateViewModel` um die `KeywordRepository`-Instanz ergänzen.

9. **E2E-Tests (primärer Funktionsnachweis)**
   - Voraussetzungen: Schritte 5–7; `ReporterAppFixture`, `E2EPageHelpers`, `FeedDbAssertions`, `StubFeedServer` vorhanden.
   - Beschreibung: `FeedDbAssertions` um Keyword-Assertions erweitern und `DeleteAllFeedsAsync` um Stichwort-Bereinigung ergänzen; `E2EPageHelpers.WaitForFeedKeywordEntry` hinzufügen; neue Szenarien in `FeedDetailTests` (s. Abschnitt „E2E-Tests").

10. **Verifikation**
    - Voraussetzungen: Schritte 1–9.
    - Beschreibung: Manuelle Mobile-UI-Verifikation des erweiterten Sheets bei 390 × 844 pt (Light/Dark, `AppThemeBinding`, Touch-Targets, Scrollen im Sheet) dokumentieren; `.\scripts\Run-StaticChecks.ps1` ohne Findings ausführen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `GetByFeedAsync_Null_ReturnsOnlyGlobalKeywords` | `KeywordRepositoryTests` | Nur `feed_id IS NULL`-Zeilen werden geliefert |
| `GetByFeedAsync_FeedId_ReturnsFeedKeywords` | `KeywordRepositoryTests` | Nur Zeilen des angegebenen Feeds |
| `GetEffectiveForFeedAsync_ReturnsGlobalAndFeedUnion` | `KeywordRepositoryTests` | Union aus globalen und Feed-Stichworten, ohne Stichworte anderer Feeds |
| `AddAsync_SameTextInDifferentFeeds_Succeeds` | `KeywordRepositoryTests` | Composite-Unique lässt gleichen Text in zwei Feeds zu |
| `AddAsync_DuplicateInSameFeed_Throws` | `KeywordRepositoryTests` | Composite-Unique verbietet Duplikat in einem Feed |
| `AddAsync_DuplicateGlobal_Throws` | `KeywordRepositoryTests` | Gefilterter Unique-Index erhält globale Eindeutigkeit |
| `DeleteAsync_RemovesFeedScopedKeywords_KeepsGlobal` | `FeedRepositoryTests` | FK-Kaskade entfernt Feed-Stichworte, globale bleiben |
| `SyncFeedAsync_FeedKeywordMatch_NotSaved` | `FeedSyncServiceTests` | Feed-Stichwort filtert Artikel des eigenen Feeds |
| `SyncFeedAsync_FeedKeyword_OtherFeedUnaffected` | `FeedSyncServiceTests` | Gleicher Artikeltitel wird in einem Feed ohne Stichwort gespeichert |
| `SyncFeedAsync_FeedKeyword_CombinedWithGlobal` | `FeedSyncServiceTests` | Union: globaler und Feed-Treffer werden beide verworfen |
| `E2E_KeywordFilter_FeedScoped_NotInUnreadList` | `KeywordFilterTests_E2E` | Voller Fluss: Feed-Stichwort → Sync → gefilterter Artikel fehlt in Unread-Liste |
| `NotifyNewItemsAsync_FeedKeywordMatch_SkipsItem` | `NotificationServiceTests` | Feed-Stichwort unterdrückt Benachrichtigung |
| `NotifyNewItemsAsync_FeedKeyword_OtherFeedScope` | `NotificationServiceTests` | Stichwort eines anderen Feeds wirkt nicht |
| `CleanupAsync_FeedKeywordMatch_DeletesOnlyOwnFeed` | `RetentionCleanupServiceTests` | Pro-Feed-Gruppierung: Treffer im eigenen Feed gelöscht, gleicher Text in anderem Feed bleibt |
| `CleanupAsync_GlobalKeyword_StillDeletesAcrossFeeds` | `RetentionCleanupServiceTests` | Regression: globale Stichworte wirken weiter feed-übergreifend |
| `LoadAsync_LoadsFeedKeywords` | `FeedDetailViewModelTests` | `FeedKeywords` wird beim Laden befüllt (nur eigene Feed-Stichworte) |
| `AddFeedKeyword_Valid_PersistsWithFeedId` | `FeedDetailViewModelTests` | Stichwort landet mit `FeedId` im Repository und in `FeedKeywords` |
| `AddFeedKeyword_Empty_ShowsError` / `AddFeedKeyword_TooLong_ShowsError` / `AddFeedKeyword_Duplicate_ShowsError` | `FeedDetailViewModelTests` | Validierung mit `FeedKeywordErrorMessage`/`HasFeedKeywordError` |
| `RemoveFeedKeyword_DeletesFromRepository` | `FeedDetailViewModelTests` | Entfernen löscht Zeile und Chip |
| `CloseEditForm_ResetsKeywordInput` | `FeedDetailViewModelTests` | Sheet-Reset räumt Eingabe/Fehler, lässt `FeedKeywords` bestehen |
| `LoadAsync_ExcludesFeedScopedKeywords` | `SettingsViewModelTests_Keywords` | Globale Liste zeigt keine Feed-Stichworte |
| `KeywordExistsForFeedAsync` / `WaitForFeedKeywordEntry` | `FeedDbAssertions` / `E2EPageHelpers` (E2E-Helfer) | DB-Assertion `keywords.feed_id ↔ feeds`-Join; Entry-Lokalisierung per `SemanticProperties.Description` |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `FeedDetailViewModelTests.CreateViewModel` | Neue Pflicht-Abhängigkeit `IKeywordRepository` im Konstruktor — `KeywordRepository` auf `_factory` instanzieren |
| `KeywordFilterTests_E2E`, `FeedSyncServiceTests`, `FeedSyncServiceTests_DebugLog`, `NotificationServiceTests`, `RetentionCleanupServiceTests` | Nutzen konkrete `KeywordFilter`-Instanzen — kompilieren weiter; nur falls die alte parameterlose Signatur direkt aufgerufen wird, Aufrufe auf `GetKeywordTextsAsync(null)`/`(feedId)` umstellen |
| `FeedRepositoryTests` (Delete-Tests) | Unverändert lauffähig; Kaskaden-Nachweis kommt als neuer Test dazu |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | `FeedDetail_Edit_AddKeyword_PersistsAndShowsChip` — Edit-Sheet öffnen, Stichwort eingeben und hinzufügen; Chip erscheint, Sheet schließen und erneut öffnen, Chip bleibt; DB-Nachweis `keywords`-Zeile mit `feed_id` des Feeds | `FeedDetailTests` (`src/Reporter.E2ETests`) | Stichwort-Pflege ist im Feed-Kontext erreichbar und persistent | Weist den kompletten Benutzerfluss UI → ViewModel → Repository → SQLite nach, den Unit-Tests nicht abdecken |
| Pflicht | `FeedDetail_Edit_RemoveKeyword_RemovesChipAndRow` — Stichwort hinzufügen, per „×"-Chip entfernen; Chip verschwindet, DB-Zeile ist weg | `FeedDetailTests` | Entfernen funktioniert über die UI | Interaktion (Chip-Button) + Persistenz nur über die laufende App verifizierbar |
| Pflicht | `FeedDetail_Keyword_FiltersItemsOnSync_OnlyForOwnFeed` — Feed A mit Stichwort (matcht Stub-Artikeltitel) synchronisiert den Artikel nicht (DB-`items`-Check); Feed B ohne Stichwort speichert denselben generischen Stub-Artikel normal | `FeedDetailTests` | Feed-spezifische Filterwirkung beim Abruf und Scoping auf den eigenen Feed | Der eigentliche Feature-Zweck (Filter pro Feed) ist nur im Gesamtablauf Sync+DB nachweisbar |
| Pflicht | `FeedDetail_Edit_KeywordValidation_ShowsErrors` — im geöffneten Edit-Sheet nacheinander alle drei sichtbaren Validierungsfälle durchspielen: (1) Hinzufügen-Button bei leerem Eingabefeld → Fehler-`Label` zeigt `AppResources.ErrorKeywordEmpty`; (2) Eingabe mit `MaxKeywordLength` + 1 (501) Zeichen → `ErrorKeywordTooLong`; (3) gültiges Stichwort hinzufügen (Chip erscheint, Label verschwindet) und denselben Text erneut eingeben → `ErrorKeywordDuplicate` | `FeedDetailTests` | Sichtbare Fehlermeldung bei leerer, überlanger und doppelter Stichwort-Eingabe (`FeedKeywordErrorMessage`/`HasFeedKeywordError` auf sichtbarem `Label`) | Alle drei Fehlerfälle sind über die UI auslösbar und für Anwender sichtbar; ein gemeinsamer Test teilt sich Sheet-Öffnung und App-Laufzeit, prüft aber jeden Validierungszweig einzeln — die Label-Sichtbarkeit ist eine UI-Eigenschaft, die Unit-Tests allein nicht nachweisen |
| Pflicht | `FeedDetail_Keyword_NotShownInGlobalSettings` — Feed-Stichwort `e2e-feed-only` über das Edit-Sheet anlegen, Sheet schließen, `SelectTab(AppResources.TabSettings)`; in der globalen Keyword-Karte das Kontroll-Stichwort `e2e-global` über das `SettingsKeywordPlaceholder`-Entry und den `SettingsKeywordAdd`-Button hinzufügen und dessen Chip abwarten; per `UiRetry.WaitFor` prüfen, dass kein UI-Element den Namen `e2e-feed-only` trägt | `FeedDetailTests` | Feed-Stichworte bleiben in der globalen `SettingsPage`-Stichwortliste verborgen (`SettingsViewModel.LoadAsync` → `GetByFeedAsync(null)`) | Die Sichtbarkeitsregel ist direkt über die UI beobachtbar. `SettingsPage.OnAppearing` führt `LoadCommand` bei jedem Tab-Wechsel erneut aus — die Liste ist nach dem Anlegen frisch geladen, und der sichtbare Kontroll-Chip beweist, dass die Karte Stichworte rendert (negative Assertion ist nicht trivial erfüllt) |

Über die UI nicht sinnvoll nachweisbare Regeln — begründete Nicht-Erforderlichkeit von E2E-Tests:

- **Benachrichtigungs-Unterdrückung pro Feed (`NotificationService.NotifyNewItemsAsync`):** Lokale Benachrichtigungen erscheinen außerhalb des App-Fensters (Windows-Benachrichtigungscenter) und sind über FlaUI nicht zuverlässig beobachtbar; zudem gibt es in der App keine UI, die unterdrückte Benachrichtigungen sichtbar macht. Die feed-spezifische Unterdrückung ist über `NotificationServiceTests` (`NotifyNewItemsAsync_FeedKeywordMatch_SkipsItem`, `NotifyNewItemsAsync_FeedKeyword_OtherFeedScope`) auf Service-Ebene nachgewiesen.
- **Keyword-Löschregel im Retention-Lauf pro Feed (`RetentionCleanupService.CleanupAsync`):** Hintergrund-Bereinigung ohne direkt auslösende Nutzeraktion und ohne eigenes sichtbares UI-Ergebnis — ein E2E-Test könnte die Regel nicht über die UI auslösen, sondern müsste dieselbe Service-Logik wie die Unit-Tests aufrufen. Nachweis über `RetentionCleanupServiceTests` (`CleanupAsync_FeedKeywordMatch_DeletesOnlyOwnFeed`, `CleanupAsync_GlobalKeyword_StillDeletesAcrossFeeds`).

Betroffene bestehende E2E-Tests:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `FeedDetailTests.FeedDetail_Edit_PersistsChanges` | Sheet-Markup wächst (ScrollView-Einbettung) — verifizieren, dass `EditUrlEntry` und Speichern-Button weiter erreichbar sind; Anpassung nur falls das neue Layout die Elemente verdeckt |
| `FeedDbAssertions.DeleteAllFeedsAsync` (Infrastruktur, kein Test) | Muss Feed-Stichworte explizit mitlöschen, sonst verfälschen Restzeilen nachfolgende E2E-Tests |

## Offene Punkte

Keine.
