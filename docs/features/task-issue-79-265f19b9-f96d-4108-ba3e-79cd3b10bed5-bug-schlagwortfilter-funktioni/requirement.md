<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: Schlagwortfilter greift nicht — Treffer-Artikel werden weiterhin aufgelistet (Issue #79)

## Fachliche Zusammenfassung

Der konfigurierte Schlagwortfilter (Blacklist in der `keywords`-Tabelle, Matching via `IKeywordMatcher`/`KeywordMatcher.MatchesAny` — Teilwort-Vergleich mit `OrdinalIgnoreCase` auf `Item.Title` und `Item.ContentHtml`) verhindert aktuell nicht, dass Treffer-Artikel in den Artikellisten erscheinen. Die Keyword-Liste wird nur an zwei Stellen ausgewertet: `NotificationService.NotifyNewItemsAsync` (unterdrückt Benachrichtigungen für Treffer) und `RetentionCleanupService.CleanupAsync` (löscht Treffer, aber ausschließlich bei `IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff`, ausgelöst einmalig in `App.OnStart`). `FeedSyncService.RunSyncAsync` speichert alle Feed-Items ungeprüft, und `ItemRepository.GetUnreadByDateAsync` liefert alle ungelesenen Artikel — ein ungelesener Treffer wie „Anzeige: WLAN-Repeater mit bis zu 2.600 MBit/s" bleibt daher dauerhaft sichtbar, bis er gelesen wird und die Aufbewahrungsfrist ab Veröffentlichungsdatum abgelaufen ist. Zielverhalten: Artikel, die einem konfigurierten Schlagwort entsprechen, dürfen nicht in den Listen (Ungelesen, Kategorien, Später lesen) geführt werden — die Filterung muss spätestens beim Einspeichern neuer Feed-Items greifen.

## Betroffene Klassen und Komponenten

### Logikklassen / Services (`src/Reporter.Core/Services/`)

- `FeedSyncService.cs` — `RunSyncAsync` speichert neue Items heute ohne Keyword-Prüfung per `AddRangeAsync`. Hier ist der Filter einzuziehen: Keywords pro Sync laden (`IKeywordRepository.GetAllAsync`) und pro neuem `SyndicationItem` `IKeywordMatcher.MatchesAny(title, contentHtml, keywordTexts)` aufrufen; Treffer werden nicht in `newItemEntities` aufgenommen. Der Konstruktor benötigt zwei neue Abhängigkeiten: `IKeywordRepository` und `IKeywordMatcher` (beide bereits als Singletons in `MauiProgram` registriert — keine neue DI-Registrierung nötig).
- `KeywordMatcher.cs` / `IKeywordMatcher` (`src/Reporter.Core/Interfaces/IKeywordMatcher.cs`) — Matching-Semantik unverändert wiederverwenden.
- `RetentionCleanupService.cs` — die bestehende Keyword-Löschregel (Schritte 4–7 in `CleanupAsync`) wird für Neuzugänge durch den Ingest-Filter obsolet; in der Planung zu entscheiden, ob der Zweig entfällt, für Bestandsdaten bestehen bleibt oder seine Bedingung erweitert wird.
- `NotificationService.cs` — vermutlich unverändert: gefilterte Artikel werden nicht mehr gespeichert und erreichen den Benachrichtigungskandidaten-Kreis nicht mehr; der dortige Keyword-Check bleibt als Tiefenverteidigung bestehen.

### Interfaces

- `IFeedSyncService` — keine Signaturänderung (`SyncFeedAsync`/`SyncAllAsync` bleiben); nur der konkrete Konstruktor von `FeedSyncService` ist betroffen.
- `IItemRepository` / `IKeywordRepository` — bei reinem Ingest-Filter unverändert. Falls Bestandsdaten nachträglich entfernt werden sollen, wäre ggf. ein neuer Member nötig, der Treffer-Kandidaten ohne `IsRead`-/Frist-Bedingung liefert (siehe Offene Fragen).

### Datenmodellklassen

- `Keyword` (`Id`, `KeywordText`), `Item`, `ItemListItem` — unverändert; kein neues persistiertes Filter-Flag nötig, wenn Treffer gar nicht erst gespeichert werden.
- `Reporter.Data/Entities/Keyword.cs` + `ReporterDbContext` (`keywords`-Tabelle) — unverändert, keine Migration nötig.

### UI-Komponenten / ViewModels

- Keine zwingende UI-Änderung: `SettingsPage.xaml` (Keyword-`Entry`, `+ Hinzufügen`-Button, Chip-Liste via `BindableLayout`) und `SettingsViewModel` (`AddKeywordAsync`/`RemoveKeywordAsync`, `Keywords`) bleiben funktional. `UnreadViewModel`, `LaterViewModel`, `CategoriesViewModel` profitieren automatisch vom Ingest-Filter.
- Hinweistexte in `src/Reporter.Core/Resources/Strings/AppResources.resx` + `AppResources.de.resx` (Sektion „Schlagwort-Filter", Badge „Immer aktiv", Beschreibungstexte) sind an die neue Semantik anzupassen, falls sie weiterhin die Frist-Löschung beschreiben.

### Tests (`src/Reporter.Tests/`)

- `FeedSyncServiceTests.cs` — die Fabrikmethoden `CreateService`/`CreateFailingService` müssen die neuen Konstruktor-Parameter übergeben (`_keywordRepository` und `new KeywordMatcher()` sind im Test-Setup bereits vorhanden). Neue Tests: Treffer-Titel („Anzeige: …") wird nicht gespeichert und nicht aufgelistet; Match auf `ContentHtml`; Nicht-Treffer werden normal gespeichert; leere Keyword-Liste ändert nichts; gefilterte Items lösen keine Benachrichtigung aus und bleiben bei Folge-Syncs gefiltert.
- `RetentionCleanupServiceTests.cs` — betroffen, falls die Keyword-Regel angepasst oder entfernt wird (insb. `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved`, `CleanupAsync_DeletesKeywordMatchedExpired`, `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt`).
- `ServiceCollectionTests.cs` — ggf. um Auflösung des erweiterten `FeedSyncService`-Konstruktors ergänzen (aktuell werden nur Repositories und `FeedSearchService` geprüft).

### Dokumentation (`docs/help/`)

- `einstellungen/beschreibung.md` — beschreibt aktuell explizit die alte Semantik: „Gefilterte Artikel werden nicht ausgeblendet, sondern erst nach Ablauf der Aufbewahrungsfrist gelöscht" und „Der Keyword-Filter löscht nur gelesene Artikel".
- `einstellungen/business-rules.md` (Abschnitte „Lösch-Invarianten", „Unterschiedliche Fristbasis der beiden Löschregeln", „Keyword-Matching ist fest verdrahtet"), `einstellungen/ablauf-technisch.md` (Abschnitt 4), `einstellungen/troubleshooting.md` („Gefilterte Artikel werden nicht gelöscht"), `einstellungen/architektur.md`, `anwendung/aufbewahrung.md` und `benachrichtigungen/business-rules.md` — alle beschreiben die Lösch-/Benachrichtigungs-Semantik und sind auf die neue Filter-beim-Speichern-Semantik zu aktualisieren.

## Implementierungsansatz

Naheliegender Ansatz (Annahme, in der Planung zu verifizieren): **Ingest-Filter** in `FeedSyncService.RunSyncAsync`.

- Keywords einmal pro Feed-Sync (oder einmal pro `SyncAllAsync`-Lauf) über `IKeywordRepository.GetAllAsync` laden; bei leerer Liste entfällt der Mehraufwand vollständig.
- Pro neuem `SyndicationItem` nach `NormalizeGuidOrHash`/`GetContentHtml` `IKeywordMatcher.MatchesAny(title, contentHtml, keywordTexts)` aufrufen; Treffer werden nicht in `newItemEntities` aufgenommen (und daher weder gespeichert, benachrichtigt noch gelistet).
- Dedup-Aspekt: gefilterte Items werden bei jedem Sync erneut gematcht — das ist deterministisch und gewollt, da sich die Keyword-Liste ändern kann; `knownKeys` muss sie nicht aufnehmen (würde nur die Re-Evaluierung innerhalb desselben Dokuments beeinflussen).
- `DetermineStatus` zählt weiterhin `fetchedCount` inklusive gefilterter Items — kein Gesundheits-False-Positive zu erwarten.
- Fehlerisolation unverändert: ein Fehler beim Keyword-Laden läuft in den bestehenden `try/catch` von `SyncFeedAsync` → `FeedHealth.Error` + `SyncLog`.

Alternativansatz (abzuwägen, aber problematisch): Filtern zur Lesezeit in `ItemRepository.GetUnreadByDateAsync`. Das `OrdinalIgnoreCase`-Teilwort-Matching ist nicht zuverlässig nach SQL/SQLite übersetzbar (In-Memory-Matching ist laut `docs/help/einstellungen/architektur.md` bewusst gewählt); clientseitiges Filtern würde Paging (`Skip`/`Take`) und `GetUnreadCountAsync` inkonsistent machen, und die Filterung müsste in allen Lesezugriffen (`GetSavedForLaterAsync`, `GetByCategoryAsync`, Detailansicht) dupliziert werden.

## Konfiguration

Kein neuer Konfigurationsbedarf identifiziert. Die Schlagwortliste bleibt die bestehende, anwendungsweite `keywords`-Tabelle, verwaltet über die Chips auf der `SettingsPage` (`SettingsViewModel`). Die Match-Semantik (Teilwort, `OrdinalIgnoreCase`, Felder `Title` + `ContentHtml`) ist fest verdrahtet und bleibt es — eine per-Keyword- oder globale Umschaltung „Ausblenden vs. fristbasiert Löschen" ist aus der Anforderung nicht ableitbar.

## Offene Fragen

- **Bestandsdaten:** Sollen bereits gespeicherte Treffer-Artikel (ungelesen, innerhalb der Aufbewahrungsfrist) nachträglich entfernt oder ausgeblendet werden? Das kollidiert mit der dokumentierten Lösch-Invariante „niemals ungelesen, niemals gemerkt" (`business-rules.md`) — Entscheidung nötig, ob die Invariante für Keyword-Treffer aufgeweicht wird oder ob die Behebung nur für Neuzugänge gilt.
- **Nachträglich erfasste Keywords:** Wird ein Schlagwort erst angelegt, nachdem ein Treffer-Artikel bereits gespeichert wurde, bleibt der Bestandstreffer sichtbar, solange er ungelesen ist. Ist das akzeptabel, oder soll `RetentionCleanupService`/`CleanupAsync` dann auch ungelesene Treffer entfernen?
- **Keyword-Regel im `RetentionCleanupService`:** Entfällt der Keyword-Zweig ersatzlos (Treffer werden nie gespeichert), bleibt er zur Behandlung von Altdaten bestehen oder wird seine Kandidaten-Bedingung (`IsRead`, `PublishedAt ?? ReadAt < cutoff`) geändert?
- **Sync-Telemetrie:** Sollen verworfene Treffer in `SyncResult`/`SyncLog.Message` separat ausgewiesen werden (z. B. „5 synchronized, 1 filtered"), damit Anwender die Wirkung des Filters nachvollziehen können, oder sollen sie still verworfen werden?
- **Regressionstest:** Soll der genannte Beispielfeed (`https://rss.golem.de/rss.php?feed=ATOM1.0`, Atom-Format) als manueller Verifikationsfall dienen bzw. der Atom-Parsing-Pfad (`SyndicationFeed.Load`) in den neuen Tests abgedeckt werden?
