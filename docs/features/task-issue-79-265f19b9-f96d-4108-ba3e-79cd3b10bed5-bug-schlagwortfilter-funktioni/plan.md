<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Schlagwortfilter greift nicht — Treffer-Artikel werden weiterhin aufgelistet (Issue #79)

## Übersicht

Der Schlagwortfilter wird von der bisherigen Frist-Lösch-Semantik (Treffer werden gespeichert und erst beim Cleanup gelöscht) auf eine **Filter-beim-Einspeichern-Semantik** umgestellt: `FeedSyncService.RunSyncAsync` prüft jedes neue `SyndicationItem` gegen die konfigurierte `keywords`-Liste und verwirft Treffer, bevor sie per `AddRangeAsync` gespeichert werden. Verworfene Treffer werden gezählt und in der `SyncLog.Message` ausgewiesen (z. B. „Synchronized 6 items, 5 new, 1 filtered."). Betroffen sind `FeedSyncService` (zwei neue Konstruktor-Abhängigkeiten, Match in der Ingest-Schleife, erweiterte Log-Message), der Ressourcentext `SettingsKeywordInfo` (EN/DE) und die Hilfsdokumentation. `IFeedSyncService`, `NotificationService`, `RetentionCleanupService`, alle Lesezugriffe (`ItemRepository`), die Datenbank und die Verwaltungs-UI (`SettingsPage`/`SettingsViewModel`) bleiben unverändert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Filterzeitpunkt (`FeedSyncService`) | **Ingest-Filter** in `RunSyncAsync`: Treffer werden gar nicht erst in `newItemEntities` aufgenommen | Filtern zur Lesezeit (`ItemRepository.GetUnreadByDateAsync` u. a.) scheidet aus: Das `OrdinalIgnoreCase`-Teilwort-Matching ist nicht zuverlässig nach SQL/SQLite übersetzbar, clientseitiges Filtern würde Paging (`Skip`/`Take`) und `GetUnreadCountAsync` inkonsistent machen, und der Filter müsste in allen Lesezugriffen (`GetUnreadByDateAsync`, `GetSavedForLaterAsync`, `GetByCategoryAsync`, Detailansicht) dupliziert werden. In-Memory-Matching ist laut `docs/help/einstellungen/architektur.md` die bewusst gewählte Strategie. |
| Ladezeitpunkt der Keyword-Liste | Einmal pro `RunSyncAsync`-Aufruf via `IKeywordRepository.GetAllAsync`; bei leerer Liste entfällt jeder Mehraufwand | Die Liste kann sich zwischen Syncs ändern — kein Caching, immer aktuell. `SyncAllAsync` ruft pro Feed `SyncFeedAsync` → `RunSyncAsync`, daher ein Ladevorgang pro Feed; bei wenigen Feeds und einer kleinen Tabelle vernachlässigbar. Ein Parametrieren über `SyncAllAsync` würde die private Signatur ohne Mehrwert komplizieren. |
| `RetentionCleanupService` Keyword-Zweig | Unverändert bestehen lassen (bestätigter Beschluss) | Der Zweig bereinigt vor dem Fix gespeicherte Altdaten-Treffer fristbasiert und wahrt die dokumentierte Lösch-Invariante („niemals ungelesen, niemals gemerkt"). Für Neuzugänge wird er durch den Ingest-Filter gegenstandslos, kostet aber keinen Mehraufwand (leere Kandidatenmenge). |
| Bestandsdaten / nachträglich erfasste Keywords | Bestehende Treffer bleiben unangetastet in den Listen (bestätigte Beschlüsse); der Fix gilt nur für Neuzugänge | Ein Sofort-Bereinigen bereits gespeicherter ungelesener Treffer (z. B. beim `AddKeywordAsync`) würde die dokumentierte Lösch-Invariante verletzen. Altdaten-Treffer werden fristbasiert über die Keyword-Regel des `RetentionCleanupService` entfernt, sobald sie gelesen wurden; die Doku beschreibt dieses Verhalten transparent. |
| Ausweisung verworfener Treffer | Anzahl verworfener Items an die `SyncLog`-/`SyncResult`-Message anhängen (bestätigter Beschluss) | Reine String-Erweiterung der bestehenden Message (keine Schemaänderung, kein neuer `SyncResult`-Member); `SyncResult.NewItems` bleibt „gespeicherte neue Items". Macht die Filterwirkung für Anwender im Sync-Verlauf nachvollziehbar. |

## Programmabläufe

### Keyword-gefilterter Feed-Sync (Ingest-Filter)

1. `SyncFeedAsync` prüft den Online-Status, legt den `SyncLog` an, lädt den `Feed` und ruft `RunSyncAsync` im bestehenden `try/catch` auf — unverändert.
2. `RunSyncAsync` lädt nach `SyndicationFeed.Load` und vor der Item-Schleife die Keyword-Liste via `_keywordRepository.GetAllAsync()` und projiziert sie auf `keywordTexts` (`k.KeywordText`); zusätzlich wird ein Zähler `filteredCount` mit `0` initialisiert.
3. Pro `SyndicationItem` in der bestehenden `foreach`-Schleife: `link`, `publishedAt` und `guidOrHash` (`NormalizeGuidOrHash`) bestimmen und die `knownKeys`-Dedup-Prüfung durchführen — unverändert.
4. **Neu:** `title` (`feedItem.Title?.Text`) und `contentHtml` (`GetContentHtml(feedItem)`) ermitteln; wenn `keywordTexts` nicht leer ist, `_keywordMatcher.MatchesAny(title, contentHtml, keywordTexts)` aufrufen. Treffer → `filteredCount` erhöhen und `continue`: Das Item wird nicht in `newItemEntities` aufgenommen und damit weder gespeichert, benachrichtigt noch gelistet. Gefilterte Items bleiben außerhalb der Persistenz, werden daher bei jedem Folge-Sync erneut gematcht (deterministisch und gewollt — die Keyword-Liste kann sich geändert haben); sie müssen nicht in `knownKeys` verbleiben.
5. `_itemRepository.AddRangeAsync(newItemEntities)` speichert nur Nicht-Treffer — unverändert.
6. `DetermineStatus(newItems, feedItems.Count, existingCount, lastPublishedAt)` zählt weiterhin die Abrufmenge **inklusive** gefilterter Items — kein Health-False-Positive durch den Filter.
7. **Neu:** Die Sync-Message wird bei `filteredCount > 0` um die Anzahl verworfener Treffer erweitert, z. B. `Synchronized 6 items, 5 new, 1 filtered.` (bzw. die bestehende Warning-Variante mit angehängtem `, N filtered`). `UpdateFeedHealthAsync` läuft unverändert; `UpdateLogAsync` persistiert die erweiterte Message im `SyncLog`, und `SyncResult.Message` trägt denselben Text. `SyncResult.NewItems` zählt weiterhin nur gespeicherte neue Items. `SyncAllAsync` übernimmt die erweiterten Einzel-Messages automatisch in sein Aggregat (`{feed.Title}: {result.Message}`); sein `totalNew`-Zähler bleibt unverändert auf gespeicherte Items begrenzt.
8. `_notificationService.NotifyNewItemsAsync` erhält nur gespeicherte Items — gefilterte Artikel lösen keine Benachrichtigung aus; der dortige Keyword-Check in `NotificationService` bleibt als Tiefenverteidigung unverändert bestehen.
9. Ein Fehler beim Keyword-Laden (`GetAllAsync`) läuft in den bestehenden `catch` in `SyncFeedAsync` → `FeedHealth.Error` + `SyncLog` — bestehende Fehlerisolation.

Beteiligte Klassen/Komponenten: `FeedSyncService`, `IKeywordRepository`/`KeywordRepository`, `IKeywordMatcher`/`KeywordMatcher`, `IItemRepository`, `INotificationService`, `SyncResult`, `SyncLog`, `FeedHealth`

### Aufbewahrungs-Cleanup beim App-Start (unverändert)

`App.OnStart` → `RetentionCleanupService.CleanupAsync`: Der Keyword-Zweig (`GetAllAsync` → `GetExpiredKeywordCandidatesAsync` → `MatchesAny` → `DeleteRangeAsync`) bleibt gemäß bestätigtem Beschluss unverändert und bereinigt vor der Umstellung gespeicherte Treffer-Altdaten fristbasiert. Für Neuzugänge wird er durch den Ingest-Filter faktisch gegenstandslos.

Beteiligte Klassen/Komponenten: `RetentionCleanupService`, `IKeywordRepository`, `IKeywordMatcher`, `IItemRepository`, `App`

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `FeedSyncService` (Service — `src/Reporter.Core/Services/FeedSyncService.cs`)

- **Neue Abhängigkeiten:** `IKeywordRepository keywordRepository` und `IKeywordMatcher keywordMatcher` als Konstruktor-Parameter mit Feldern `_keywordRepository` und `_keywordMatcher` — beide sind bereits als Singletons in `MauiProgram` registriert (Zeilen 51/58), keine neue DI-Registrierung nötig.
- **Geänderte Methoden:**
  - `RunSyncAsync` — lädt die Keyword-Liste pro Lauf und wertet `IKeywordMatcher.MatchesAny` in der Item-Schleife aus; Treffer werden nicht in `newItemEntities` aufgenommen und über `filteredCount` mitgezählt; die Sync-Message wird bei `filteredCount > 0` um `, N filtered` erweitert (Details siehe Programmablauf).
  - `UpdateLogAsync` — die persistierte `SyncLog.Message` enthält nun die Anzahl verworfener Treffer (z. B. `Synchronized 6 items, 5 new, 1 filtered.`). Die Message wird in `RunSyncAsync` zusammengesetzt und an `UpdateLogAsync` übergeben — keine Signatur- oder Schemaänderung, die Methode persistiert die übergebene Message wie bisher.
- **Unverändert:** `SyncFeedAsync`, `SyncAllAsync` (das Aggregat übernimmt die erweiterten Einzel-Messages automatisch; `totalNew` zählt weiterhin nur gespeicherte Items), `DetermineStatus`, `UpdateFeedHealthAsync`, `GetContentHtml`, `NormalizeGuidOrHash`; `IFeedSyncService`-Signatur bleibt stabil.

### `NotificationService` / `RetentionCleanupService` / `ItemRepository` / `SettingsViewModel`

Keine Änderungen. Der Keyword-Check in `NotificationService.NotifyNewItemsAsync` bleibt als Tiefenverteidigung; die Keyword-Löschregel im `RetentionCleanupService` bleibt für Altdaten bestehen; `ItemRepository` erhält keinen neuen Member; die Keyword-Verwaltung im `SettingsViewModel` ist unverändert.

### Ressourcentexte (`src/Reporter.Core/Resources/Strings/AppResources.resx` + `AppResources.de.resx`)

- **`SettingsKeywordInfo`** (resx Zeile 224–226 in beiden Dateien): Wert an die neue Semantik anpassen — gefilterte Artikel werden beim Feed-Abruf verworfen und erscheinen nicht in den Listen (statt „… werden nach Ablauf der Aufbewahrungsfrist … gelöscht"). Nur `<value>` ändert sich; `AppResources.Designer.cs` und die übrigen Keyword-Texte (`SettingsKeywordMatchLabel`, `-Hint`, `-Status`) bleiben unverändert.

### Hilfsdokumentation (`docs/help/`)

Auf die neue Filter-beim-Speichern-Semantik zu aktualisieren (inkl. Hinweis auf die Ausweisung gefilterter Treffer im Sync-Verlauf und darauf, dass Bestandstreffer bis zur fristbasierten Löschung sichtbar bleiben):

- `einstellungen/beschreibung.md` — Zeilen 16, 26, 35–36 beschreiben „löscht nur gelesene Artikel" / „nicht ausgeblendet, sondern … gelöscht".
- `einstellungen/einrichtung-anwender.md` — Zeile 16 (Tabellenzeile zur Filter-Wirkung).
- `einstellungen/fehlerbehebung-anwender.md` — Zeile 25 („gefilterte, aber ungelesene Artikel bleiben erhalten").
- `einstellungen/business-rules.md` — Abschnitte „Lösch-Invarianten" (Zeilen 9–15), „Unterschiedliche Fristbasis" (Zeilen 24–28), „Keyword-Matching ist fest verdrahtet" (Zeile 37: Matching-Zeitpunkt „zur Cleanup-Zeit").
- `einstellungen/ablauf-technisch.md` — Abschnitt 4 und Keyword-Matching-Stellen (u. a. Zeilen 59–101).
- `einstellungen/architektur.md` — Keyword-Filter-Stellen (u. a. Zeilen 15–33, 56–68).
- `einstellungen/troubleshooting.md` — Abschnitt „Gefilterte Artikel werden nicht gelöscht" (Zeilen 29–54).
- `anwendung/aufbewahrung.md` — Abschnitt 4 inkl. Mermaid-Diagramm und Tabellen (Zeilen 7, 32–99).
- `benachrichtigungen/business-rules.md` — Zeile 15 (Verweis auf „identische Semantik wie die Löschregel").
- Restliche Treffer (`anwendung/datenmodell.md`, `anwendung/architektur.md`, `anwendung/synchronisation.md`, `benachrichtigungen/ablauf-technisch.md`, `benachrichtigungen/architektur.md`, Index-Dateien) sind bei der Umsetzung zu sichten und ggf. mitzuziehen.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — die Eingabevalidierung für Schlagworte (`SettingsViewModel.AddKeywordAsync`: leer, > 500 Zeichen, Dublette `OrdinalIgnoreCase`) bleibt unverändert; der Ingest-Filter verarbeitet keine neue Eingabe.

## Konfigurationsänderungen

Keine — die `keywords`-Tabelle bleibt die anwendungsweite Filterliste; die Match-Semantik bleibt fest verdrahtet.

## Seiteneffekte und Risiken

- **Bestandsdaten:** Vor dem Fix gespeicherte Treffer bleiben in den Listen sichtbar, bis sie gelesen werden und die Keyword-Löschregel des `RetentionCleanupService` greift (bestätigte Beschlüsse: Bestandsdaten und nachträglich erfasste Keywords bleiben unangetastet; die Lösch-Invariante bleibt gewahrt).
- **Feed-Health:** `DetermineStatus` zählt `fetchedCount` inklusive gefilterter Items. Ein Feed, dessen Neuzugänge dauerhaft komplett gefiltert werden, liefert `newItems == 0`; die bestehende „> 30 Tage keine neuen Items"-Regel (`lastPublishedAt` aus gespeicherten Items) kann dann `Warning` auslösen — fachlich vertretbar, aber als Verhaltensänderung zu dokumentieren.
- **`SyncResult.NewItems` / `SyncLog.Message`:** `NewItems` zählt nur gespeicherte Items; gefilterte Items reduzieren die angezeigte Neuzugangszahl, werden aber über den `, N filtered`-Zusatz in der Message nachvollziehbar ausgewiesen (bestätigter Beschluss). Das `SyncAllAsync`-Aggregat zählt weiterhin nur gespeicherte Items.
- **Test-Kompilierung:** `FeedSyncServiceTests.CreateService`/`CreateFailingService` müssen die neuen Konstruktor-Parameter übergeben — sonst kompiliert die Suite nicht (alle 20 Tests der Klasse betroffen).
- **`RetentionCleanupService`:** Der Keyword-Zweig bleibt unverändert (bestätigter Beschluss) — die drei `RetentionCleanupServiceTests` zur Keyword-Regel bleiben gültig, keine Anpassung nötig.
- **Benachrichtigungen:** Gefilterte Artikel erreichen `NotifyNewItemsAsync` nicht mehr; die Tiefenverteidigung dort bleibt für Bestands-/Fehlerfälle wirksam.

## Umsetzungsreihenfolge

1. **`FeedSyncService`-Konstruktor erweitern**
   - Voraussetzungen: `IKeywordRepository` (Zeile 51) und `IKeywordMatcher` (Zeile 58) sind in `MauiProgram` als Singletons registriert; `KeywordMatcher.MatchesAny` existiert — alles vorhanden, kein Vorbereitungsschritt nötig.
   - Beschreibung: Parameter `keywordRepository` und `keywordMatcher` samt Feldern ergänzen.

2. **Ingest-Filter in `RunSyncAsync` implementieren**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Keyword-Liste laden, `keywordTexts` aufbauen, Match-Aufruf in der Item-Schleife, Treffer verwerfen (`continue`) und dabei `filteredCount` mitzählen.

3. **Gefilterte Anzahl in `SyncLog.Message` ausweisen**
   - Voraussetzungen: Schritt 2 (`filteredCount` wird in `RunSyncAsync` geführt).
   - Beschreibung: Die in `RunSyncAsync` aufgebaute Sync-Message bei `filteredCount > 0` um `, N filtered` erweitern (Ok- und Warning-Variante); `UpdateLogAsync` persistiert die erweiterte Message, `SyncResult.Message` trägt denselben Text. Keine Signatur- oder Schemaänderung.

4. **Test-Fabriken anpassen und neue `FeedSyncServiceTests` schreiben**
   - Voraussetzungen: Schritte 1–3 (Suite kompiliert erst nach Übergabe der neuen Parameter); `_keywordRepository`-Feld und `new KeywordMatcher()` sind im Test-Setup bereits vorhanden.
   - Beschreibung: `CreateService`/`CreateFailingService` erweitern; `AtomXml`-Hilfsmethode nach dem Muster von `RssXml` ergänzen; neue Keyword-Testfälle inkl. Atom-Match- und SyncLog-Message-Test (siehe Tests).

5. **`ServiceCollectionTests` ergänzen**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Test, der `IFeedSyncService` aus der DI-Registrierung auflöst (erweiterter Konstruktor).

6. **E2E-Test für den Benutzerfluss anlegen**
   - Voraussetzungen: Schritte 1–4; `TestDbContextFactory`, `FakeLocalNotificationService`, `RssXml`-Pattern vorhanden.
   - Beschreibung: `KeywordFilterTests_E2E` — Schlagwort anlegen → Feed synchronisieren → Treffer fehlt in `GetUnreadByDateAsync`, Nicht-Treffer vorhanden, keine Benachrichtigung, `SyncLog.Message` weist gefilterte Anzahl aus (siehe Tests).

7. **Ressourcentext `SettingsKeywordInfo` anpassen (EN + DE)**
   - Voraussetzungen: Keine.
   - Beschreibung: Neue Formulierung für „gefilterte Artikel werden beim Abruf verworfen / erscheinen nicht in den Listen".

8. **Hilfsdokumentation aktualisieren**
   - Voraussetzungen: Schritte 2–3 (finale Semantik inkl. SyncLog-Ausweis).
   - Beschreibung: Die oben gelisteten `docs/help`-Dateien auf die Ingest-Filter-Semantik umstellen (inkl. Ausweisung gefilterter Treffer im Sync-Verlauf und Transparenz zu sichtbaren Bestandstreffern).

9. **Statische Prüfungen und Testsuite ausführen**
   - Voraussetzungen: Schritte 1–8.
   - Beschreibung: `.\scripts\Run-StaticChecks.ps1` (Format, Security, Static Analysis) und `dotnet test src/Reporter.Tests/Reporter.Tests.csproj` müssen ohne Befund durchlaufen (Projektregel in `AGENTS.md`).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `SyncFeedAsync_KeywordTitleMatch_NotSaved` | `FeedSyncServiceTests` | Item mit Treffer-Titel („Anzeige: …") wird nicht gespeichert (`GetByFeedAsync`, `GetUnreadByDateAsync` leer für Treffer); `result.NewItems` zählt nur Nicht-Treffer |
| `SyncFeedAsync_KeywordContentHtmlMatch_NotSaved` | `FeedSyncServiceTests` | Match über `Description`/`ContentHtml` verwirft das Item |
| `SyncFeedAsync_KeywordNoMatch_SavesNormally` | `FeedSyncServiceTests` | Nicht-Treffer wird bei vorhandener Keyword-Liste normal gespeichert |
| `SyncFeedAsync_EmptyKeywords_SavesAll` | `FeedSyncServiceTests` | Leere Keyword-Liste ändert nichts am bisherigen Verhalten |
| `SyncFeedAsync_KeywordFiltered_NotNotified` | `FeedSyncServiceTests` | Gefiltertes Item löst über den echten `NotificationService` + `FakeLocalNotificationService` keine Benachrichtigung aus |
| `SyncFeedAsync_KeywordFiltered_ResyncStaysFiltered` | `FeedSyncServiceTests` | Folge-Sync mit demselben Treffer speichert ihn weiterhin nicht (deterministisches Re-Matching, kein Duplikat) |
| `SyncFeedAsync_KeywordFiltered_LogCountsFiltered` | `FeedSyncServiceTests` | Feed mit 1 Treffer + Nicht-Treffern synchronisieren; `SyncLog.Message` enthält die Anzahl gefilterter Items (z. B. „…, 1 filtered"), `result.Message` trägt denselben Text |
| `SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved` | `FeedSyncServiceTests` | Keyword-Treffer in einem Atom-1.0-Dokument wird nicht gespeichert — deckt den Parsing-Pfad des gemeldeten Beispielfeeds (golem.de `ATOM1.0`) ab |
| `AtomXml` | `FeedSyncServiceTests` (Hilfsmethode) | Generiert ein Atom-1.0-Dokument (`<feed xmlns="http://www.w3.org/2005/Atom">` mit `<entry>`-Elementen: `title`, `link`, `id`, `updated`/`published`, `content`/`summary`) nach dem Muster von `RssXml` |
| `AddReporterServices_ResolvesFeedSyncService` | `ServiceCollectionTests` | `IFeedSyncService` mit erweitertem Konstruktor ist über DI auflösbar |
| `CreateService` / `CreateFailingService` (erweitert) | `FeedSyncServiceTests` (Hilfsmethode) | Übergibt `_keywordRepository` und `new KeywordMatcher()` an den erweiterten Konstruktor |
| `E2E_KeywordFilter_MatchedItemNotInUnreadList` | `KeywordFilterTests_E2E` (neu) | Happy Path des Benutzerflusses (siehe E2E-Abschnitt) |
| `E2E_KeywordFilter_MatchedItemNoNotification` | `KeywordFilterTests_E2E` (neu) | Gefiltertes Item erzeugt keine Benachrichtigung (siehe E2E-Abschnitt) |
| `E2E_KeywordFilter_SyncLogReportsFiltered` | `KeywordFilterTests_E2E` (neu) | `SyncLog.Message` des Benutzerfluss-Syncs weist die Anzahl gefilterter Items aus (siehe E2E-Abschnitt) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Alle Tests in `FeedSyncServiceTests` | `CreateService`/`CreateFailingService` müssen die neuen Konstruktor-Parameter übergeben — ohne Anpassung kompiliert die Klasse nicht |
| `ServiceCollectionTests` | Wird um die `IFeedSyncService`-Auflösung ergänzt (neuer Test, bestehende bleiben) |

`RetentionCleanupServiceTests` (`CleanupAsync_KeywordMatch_KeepsUnreadAndSaved`, `CleanupAsync_DeletesKeywordMatchedExpired`, `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt`) bleiben unverändert gültig — der Keyword-Zweig im `RetentionCleanupService` ist bestätigt unverändert.

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Schlagwort in den Einstellungen anlegen → Feed synchronisieren → Treffer-Artikel erscheint nicht in der Ungelesen-Liste, Nicht-Treffer schon | `src/Reporter.Tests/KeywordFilterTests_E2E.cs` — `E2E_KeywordFilter_MatchedItemNotInUnreadList` | Zielverhalten des Issues: „Artikel, die einem konfigurierten Schlagwort entsprechen, dürfen nicht in den Listen geführt werden" | Der Benutzerfluss überspannt `SettingsViewModel.AddKeywordAsync` → `FeedSyncService.SyncFeedAsync` → `ItemRepository.GetUnreadByDateAsync` (Datenquelle des `UnreadViewModel`); nur ein Ende-zu-Ende-Test über ViewModel → Service → Repository → SQLite belegt das tatsächliche Zusammenwirken — die Repo-Konvention für solche E2E-Tests ist `SettingsViewModelTests_E2E` |
| Pflicht | Gefiltertes Item erzeugt im selben Fluss keine Benachrichtigung | `KeywordFilterTests_E2E` — `E2E_KeywordFilter_MatchedItemNoNotification` (echter `NotificationService` + `FakeLocalNotificationService` im Verbund) | „Treffer werden weder gespeichert noch benachrichtigt" | Benutzerseitig sichtbares Verhalten (keine Push-Meldung), das über Service-Grenzen hinweg entsteht |
| Pflicht | Verworfene Treffer werden im Sync-Verlauf ausgewiesen (`SyncLog.Message` enthält die Anzahl gefilterter Items) | `KeywordFilterTests_E2E` — `E2E_KeywordFilter_SyncLogReportsFiltered` | Anwender können die Filterwirkung im Sync-Verlauf nachvollziehen (bestätigter Beschluss zur Sync-Telemetrie) | Die Ausweisung entsteht im selben Benutzerfluss (Schlagwort anlegen → Sync → Sync-Verlauf-Eintrag) und ist im `SyncLog` benutzerseitig sichtbar; nur der Ende-zu-Ende-Pfad belegt, dass der Zähler tatsächlich in der persistierten Message landet |

Bestehende E2E-Tests (`SettingsViewModelTests_E2E`): **Keine** betroffen — die Keyword-Verwaltungs-API des `SettingsViewModel` ändert sich nicht.

## Offene Punkte

Keine.
