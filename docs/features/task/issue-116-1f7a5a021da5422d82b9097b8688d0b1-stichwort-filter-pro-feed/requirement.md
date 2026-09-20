<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung — Stichwort-Filter pro Feed (Issue #116)

## Fachliche Zusammenfassung

Der Stichwort-Filter (Keyword-Blacklist) ist derzeit nur global in der `SettingsPage` pflegbar und wird beim Feed-Abruf in `FeedSyncService.RunSyncAsync` auf neue Artikel angewendet: Treffer in Titel oder Inhalt werden verworfen, bevor sie gespeichert werden. Die Anforderung erweitert die Einstellungen der einzelnen Feeds um einen feed-spezifischen Stichwort-Filter — pro Feed sollen eigene Stichworte verwaltet werden können, die beim Abruf genau dieses Feeds wirken. Die Pflege gehört fachlich an die Stelle, an der heute bereits die feed-spezifische Einstellung `NotificationsEnabled` verwaltet wird: das Bearbeiten-Sheet („Feed bearbeiten") der `FeedDetailPage`. Der bestehende globale Filter bleibt erhalten; wie globale und feed-spezifische Stichworte zusammenwirken, ist zu klären (siehe Offene Fragen).

## Betroffene Klassen und Komponenten

### Datenmodell

- `Reporter.Data.Entities.Keyword` — Erweiterung um eine optionale Feed-Zuordnung (z. B. `FeedId` als nullable FK auf `feeds`; `null` = globaler Eintrag) oder alternativ neue Entität/Tabelle für feed-spezifische Stichworte.
- `Reporter.Core.Models.Keyword` — entsprechende Erweiterung des Domain-Modells.
- `ReporterDbContext.ConfigureKeyword` — der bestehende Unique-Index auf `keyword_text` muss angepasst werden (z. B. Composite `(feed_id, keyword_text)`), damit dasselbe Stichwort in unterschiedlichen Feeds existieren kann; Löschverhalten beim Entfernen eines Feeds festlegen (naheliegend `DeleteBehavior.Cascade` wie bei `Item`).
- Neue EF-Core-Migration in `src/Reporter.Data/Migrations` (Konvention: `AddKeywordFeedId` o. ä.).
- `FeedRepository.DeleteAsync` — beim Löschen eines Feeds müssen dessen feed-spezifische Stichworte mitentfernt werden (per FK-Kaskade oder explizit).

### Logikklassen / Services

- `IKeywordFilter` / `KeywordFilter` — `GetKeywordTextsAsync` um eine feed-bezogene Variante erweitern (z. B. `GetKeywordTextsAsync(Guid feedId)`), die die für den Feed wirksame Stichwortliste liefert.
- `FeedSyncService.RunSyncAsync` — Aufruf von `GetKeywordTextsAsync` mit der `feed.Id` des gerade synchronisierten Feeds; `CollectNewItems`/`MatchesAny`-Aufruf bleibt strukturell gleich.
- `NotificationService.NotifyNewItemsAsync` — wendet den Keyword-Filter erneut auf neue Artikel an; hat den `Feed`-Parameter bereits und müsste ggf. die feed-spezifische Liste verwenden.
- `RetentionCleanupService.CleanupAsync` — die Keyword-Löschregel prüft gespeicherte Bestandstreffer feed-übergreifend; feed-spezifische Stichworte erfordern hier eine Gruppierung der Kandidaten pro Feed (oder eine bewusste Entscheidung, die Regel nur auf globale Stichworte anzuwenden — siehe Offene Fragen).
- `IKeywordMatcher` / `KeywordMatcher` — unverändert; die Match-Semantik (Teilwort, `OrdinalIgnoreCase`, auf `Title` und `ContentHtml`) ist laut Business Rules fest verdrahtet und nicht konfigurierbar.
- `MauiProgram` — ggf. neue/angepasste DI-Registrierungen, falls ein weiteres Repository oder ein neuer Service entsteht.

### Interfaces

- `IKeywordRepository` — Erweiterung um feed-bezogene Abfrage (z. B. `GetByFeedAsync(Guid? feedId)` bzw. Trennung globaler/feed-spezifischer Einträge); die bisherigen CRUD-Methoden müssen die Feed-Zuordnung mitführen.
- `IKeywordFilter` — neue/erweiterte Signatur für die feed-bezogene Stichwortliste.
- `IFeedRepository` — voraussichtlich unverändert, sofern Stichworte nicht am `Feed`-Modell selbst hängen; andernfalls `Feed`/`FeedListItem` um eine Stichwort-Angabe erweitern.

### Enums

- Keine neuen Enums aus der Anforderung ableitbar.

### UI-Komponenten

- `FeedDetailViewModel` (`src/Reporter.Core/ViewModels`) — Keyword-Verwaltung für den aktuellen Feed: analog `SettingsViewModel` (`Keywords`, `NewKeywordText`, `AddKeywordCommand`, `RemoveKeywordCommand`, Duplikat-/Längenprüfung mit `MaxKeywordLength`, Fehlertexte `ErrorKeywordEmpty`/`ErrorKeywordTooLong`/`ErrorKeywordDuplicate`).
- `FeedDetailPage.xaml` — Erweiterung des Bearbeiten-Sheets um eine Stichwort-Verwaltung (Eingabefeld + Hinzufügen-Button + Chip-Liste, Muster aus `SettingsPage.xaml` Keyword-Karte).
- `AppResources` (`AppResources.resx` EN + `AppResources.de.resx` DE) — neue Ressourcenschlüssel für Beschriftungen, Placeholder und Accessibility-Texte.
- `SettingsPage` / `SettingsViewModel` — nur betroffen, falls die globale Ansicht feed-spezifische Stichworte zusätzlich anzeigen oder verwalten soll (siehe Offene Fragen).
- Feed-Auswahl/Identifikation: Der Feed-Kontext ist bereits etabliert — die `FeedDetailPage` wird über die Shell-Route `feeddetail?feedId={id}` geöffnet; es ist keine neue Auswahlinteraktion nötig. Ein Auswahl-Dialog „globales Stichwort auf Feed übernehmen" ist aus der Anforderung nicht ableitbar (offene Frage).

### Tests

- `FeedDetailViewModelTests` — Keyword-Verwaltung im Feed-Kontext (Hinzufügen, Entfernen, Duplikat-/Leer-/Längenprüfung, Persistenz).
- `KeywordRepositoryTests` — feed-bezogene Abfragen, Composite-Eindeutigkeit, Kaskadenlöschung beim Feed-Delete.
- `FeedSyncServiceTests` / `KeywordFilterTests_E2E` — Filterung pro Feed (Feed A filtert, Feed B nicht; Kombination mit globaler Liste).
- `NotificationServiceTests`, `RetentionCleanupServiceTests` — je nach geklärter Reichweite des Feed-Filters.
- `ServiceCollectionTests` — bei neuen DI-Registrierungen.
- `FeedDetailTests` (`src/Reporter.E2ETests`, FlaUI) — UI-Fluss der Stichwort-Pflege im Bearbeiten-Sheet; Mobile-UI-Verifikation gemäß `AGENTS.md` (390 × 844 pt, Light/Dark, `AppThemeBinding`, 44×44-Touch-Targets).

## Implementierungsansatz

- Der naheliegende Erweiterungspunkt ist das Bearbeiten-Sheet der `FeedDetailPage`: `FeedDetailViewModel.Edit`/`SaveEditAsync`/`ResetEditForm` pflegen heute `EditUrl` und `EditNotificationsEnabled`; die Stichwort-Verwaltung folgt demselben Muster wie `SettingsViewModel.AddKeywordAsync`/`RemoveKeywordAsync` (Trimmen, `MaxKeywordLength = 500`, case-insensitive Duplikatprüfung, sofortiges Persistieren über das Repository).
- Persistenz: bestehende `keywords`-Tabelle um nullable `feed_id`-Spalte erweitern (eine Tabelle, ein Repository, einfache Union-Abfrage) oder separate Tabelle — die Entscheidung hängt von der Duplikat-Semantik ab und ist eine offene Frage. Beim gemeinsamen Tabellenansatz ist der Unique-Index auf `keyword_text` zwingend anzupassen.
- Filteranwendung: `FeedSyncService.RunSyncAsync` lädt die Stichwortliste einmal pro Sync-Lauf (`GetKeywordTextsAsync`) und reicht sie als `CollectContext.KeywordTexts` in `CollectNewItems`; für feed-spezifische Listen wird die Methode mit `feed.Id` parametrisiert und liefert die wirksame Menge. `IKeywordMatcher.MatchesAny` bleibt unverändert.
- Abhängigkeiten: `FeedSyncService`, `NotificationService` und `RetentionCleanupService` nutzen `IKeywordFilter` — eine Signaturänderung zieht Anpassungen an allen drei Stellen (und deren Tests) nach sich. `FeedSyncService.UpdateFeedHealthAsync` rekonstruiert das `Feed`-Objekt feldweise; ein Keyword-Bezug am `Feed`-Modell müsste dort explizit mitgeführt werden (entfällt bei separater Stichwort-Tabelle).
- Annahmen (explizit gekennzeichnet):
  - Feed-spezifische Stichworte wirken wie der globale Filter als Blacklist beim Feed-Abruf (Treffer werden verworfen, nicht gespeichert).
  - Die globale Stichwortliste bleibt bestehen und gilt weiterhin für alle Feeds; feed-spezifische Stichworte ergänzen sie (Union).
  - Die Pflege erfolgt im Feed-Kontext (`FeedDetailPage`), nicht in den globalen Einstellungen.

## Konfiguration

Konfigurationsebene: pro Datensatz (Feed), persistent in der SQLite-Datenbank des `ReporterDbContext` — analog der bestehenden feed-spezifischen Einstellung `Feed.NotificationsEnabled` (Migration `AddFeedNotificationsEnabled` als Vorbild). Die Stichworte selbst gehören zweckmäßig in die `keywords`-Tabelle (mit nullablem `feed_id`) oder eine eigene Feed-Stichwort-Tabelle; die Match-Semantik bleibt gemäß Business Rule „Keyword-Matching ist fest verdrahtet" nicht konfigurierbar.

## Offene Fragen

1. **Begriffsklärung:** Die Anforderung spricht davon, die Feed-Einstellung „um einen Sichten" zu erweitern — gemeint ist vermutlich „um einen Stichwort-Filter". Bitte bestätigen, dass ein Stichwort-Filter pro Feed gemeint ist und nicht z. B. eine zusätzliche Ansicht/Sichtweise.
2. **Filter-Semantik:** Wirken feed-spezifische Stichworte additiv zur globalen Liste (Union: Artikel wird verworfen, wenn globale ODER Feed-Stichworte matchen), oder ersetzen sie den globalen Filter für diesen Feed?
3. **Wirkungsbereich:** Gilt der Feed-Filter nur beim Feed-Abruf (`FeedSyncService`), oder auch für die Benachrichtigungs-Unterdrückung (`NotificationService`) und die Keyword-Löschregel auf gespeicherte Bestandstreffer (`RetentionCleanupService`)? Letztere ist aktuell global implementiert; feed-spezifische Stichworte würden dort eine Gruppierung der Löschkandidaten pro Feed erfordern.
4. **UI-Platzierung:** Soll die Stichwort-Pflege im bestehenden Bearbeiten-Sheet („Feed bearbeiten") der `FeedDetailPage` erfolgen, oder an anderer Stelle (z. B. eigener Menüpunkt in den Feed-Aktionen)? Sollen feed-spezifische Stichworte zusätzlich in der globalen `SettingsPage` sichtbar sein?
5. **Duplikat-Regeln:** Darf dasselbe Stichwort gleichzeitig global und in einem oder mehreren Feeds existieren? (Der aktuelle Unique-Index auf `keywords.keyword_text` verbietet Duplikate tabellenweit und müsste bei einer gemeinsamen Tabelle auf `(feed_id, keyword_text)` geändert werden.)
6. **Filter-Richtung:** Ist — wie beim globalen Filter — ausschließlich eine Blacklist gemeint, oder soll der Feed-Filter auch als Whitelist konfigurierbar sein (nur Artikel mit Treffer behalten)?
