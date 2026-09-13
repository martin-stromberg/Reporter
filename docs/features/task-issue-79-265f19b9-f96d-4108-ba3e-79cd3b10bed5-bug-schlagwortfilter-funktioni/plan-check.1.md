<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Treffer-Artikel (Keyword-Match auf `Title`/`ContentHtml`, Teilwort, `OrdinalIgnoreCase`) dürfen nicht in den Listen (Ungelesen, Kategorien, Später lesen) geführt werden — Filterung spätestens beim Einspeichern | Ingest-Filter in `FeedSyncService.RunSyncAsync` (Designentscheidung, Programmablauf Schritte 2–5, Umsetzungsreihenfolge Schritte 1–2): Keywords pro Lauf laden, `MatchesAny` pro Item, Treffer via `continue` nicht in `newItemEntities` | `SyncFeedAsync_KeywordTitleMatch_NotSaved` (prüft u. a. `GetUnreadByDateAsync`), `SyncFeedAsync_KeywordContentHtmlMatch_NotSaved`, E2E `E2E_KeywordFilter_MatchedItemNotInUnreadList` | Abgedeckt |
| Treffer werden weder gespeichert noch benachrichtigt | Programmablauf Schritte 4/8: gefilterte Items erreichen `AddRangeAsync` und `NotifyNewItemsAsync` nicht; Keyword-Check im `NotificationService` bleibt als Tiefenverteidigung | `SyncFeedAsync_KeywordFiltered_NotNotified` (echter `NotificationService` + `FakeLocalNotificationService`), E2E `E2E_KeywordFilter_MatchedItemNoNotification` | Abgedeckt |
| Match-Semantik unverändert wiederverwenden (`IKeywordMatcher`/`KeywordMatcher.MatchesAny`) | Wiederverwendung der bestehenden Singleton-Implementierung; neue Konstruktor-Abhängigkeit `IKeywordMatcher` | Bestehende `KeywordMatcherTests` unverändert gültig; ContentHtml-Match durch `SyncFeedAsync_KeywordContentHtmlMatch_NotSaved` | Abgedeckt |
| Nicht-Treffer- und Normalverhalten bleibt unverändert | `AddRangeAsync` speichert Nicht-Treffer unverändert (Programmablauf Schritt 5) | `SyncFeedAsync_KeywordNoMatch_SavesNormally`, `SyncFeedAsync_EmptyKeywords_SavesAll`; bestehende 20 `FeedSyncServiceTests` laufen über angepasste Fabriken weiter | Abgedeckt |
| Leere Keyword-Liste → kein Mehraufwand | Designentscheidung + Programmablauf Schritt 4 (Match nur bei nicht-leerer Liste) | `SyncFeedAsync_EmptyKeywords_SavesAll` | Abgedeckt |
| Deterministisches Re-Matching bei Folge-Syncs (kein Duplikat, `knownKeys` unverändert) | Programmablauf Schritt 4 | `SyncFeedAsync_KeywordFiltered_ResyncStaysFiltered` | Abgedeckt |
| Kein Health-False-Positive (`DetermineStatus` zählt `fetchedCount` inkl. gefilterter Items) | Programmablauf Schritt 6 + Seiteneffekt „Feed-Health" dokumentiert | Bestehende `DetermineStatus`-bezogene Tests (`SyncFeedAsync_FewerItems_SetsWarning`, `…_NoNewItemsForThirtyDays_SetsWarning`) bleiben unverändert | Abgedeckt |
| Fehlerisolation: Fehler beim Keyword-Laden → `FeedHealth.Error` + `SyncLog` | Programmablauf Schritt 9 (bestehender `try/catch` in `SyncFeedAsync`) | Kein dedizierter Negativtest für `IKeywordRepository.GetAllAsync`-Fehler geplant — siehe Hinweise | Abgedeckt |
| `RetentionCleanupService`-Keyword-Regel bleibt unverändert (bestätigter Beschluss #3); Lösch-Invariante „niemals ungelesen, niemals gemerkt" gewahrt | Designentscheidung + Programmablauf „Aufbewahrungs-Cleanup (unverändert)" | Bestehende Tests `CleanupAsync_KeywordMatch_*` bleiben unverändert gültig | Abgedeckt |
| Bestandsdaten unangetastet; nachträglich erfasste Keywords lassen Bestandstreffer sichtbar (bestätigte Beschlüsse #1/#2) | Seiteneffekt „Bestandsdaten" dokumentiert; Doku-Aktualisierung soll Verhalten transparent beschreiben | Kein Test erforderlich (bewusste Nicht-Änderung) | Abgedeckt |
| Verworfene Treffer werden in `SyncLog.Message` ausgewiesen (bestätigter Beschluss #4, z. B. „5 synchronized, 1 filtered") | Nur als „optionaler Zähler" in Programmablauf Schritt 7 erwähnt; kein ausführbarer Schritt in der Umsetzungsreihenfolge; `UpdateLogAsync` steht unter „Unverändert" | Kein Test für die Message-Erweiterung in der Tests-Tabelle | Lücke |
| Atom-Parsing-Pfad abgedeckt (bestätigter Beschluss #5 — Beispielfeed golem.de ist `ATOM1.0`) | `AtomXml`-Hilfsmethode + Atom-Match-Test nur als Empfehlung in Offene Punkte #5 genannt; nicht in Umsetzungsreihenfolge/Tests aufgenommen | Fehlt in der Tests-Tabelle | Lücke |
| Hinweistexte `SettingsKeywordInfo` (EN/DE) an neue Filter-beim-Speichern-Semantik anpassen | Änderungsabschnitt Ressourcentexte + Umsetzungsreihenfolge Schritt 6 | Ressourcen-Textänderung; kein Test erforderlich | Abgedeckt |
| Hilfsdokumentation (`docs/help/…`) auf neue Semantik aktualisieren | Vollständige Dateiliste inkl. Zeilenverweisen + Umsetzungsreihenfolge Schritt 7 | — | Abgedeckt |
| `IFeedSyncService`-Signatur stabil; erweiterter Konstruktor über DI auflösbar | Konstruktor-Erweiterung (Schritt 1); beide Abhängigkeiten bereits als Singletons registriert | `AddReporterServices_ResolvesFeedSyncService` in `ServiceCollectionTests` | Abgedeckt |
| Keine Datenbankmigration, kein persistiertes Filter-Flag, keine Konfigurations-/Validierungsänderung | Abschnitte „Datenbankmigrationen: Keine", „Validierungsregeln: Keine", „Konfigurationsänderungen: Keine" | — | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] Testnachweis zum bestätigten Beschluss #4: Es fehlt ein Test, der die Ausweisung verworfener Treffer in der `SyncLog.Message` prüft (z. B. `SyncFeedAsync_KeywordFiltered_LogCountsFiltered` — Feed mit 1 Treffer + Nicht-Treffern synchronisieren, `SyncLog.Message` enthält die Anzahl gefilterter Items).
- [ ] Testnachweis zum bestätigten Beschluss #5: Die `AtomXml`-Hilfsmethode (nach dem Muster von `RssXml` in `FeedSyncServiceTests`) und ein Keyword-Match-Test gegen ein Atom-Dokument fehlen in der Tests-Tabelle — beide sind nur als Empfehlung in „Offene Punkte" #5 genannt.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Schlagwort in den Einstellungen anlegen → Feed synchronisieren → Treffer-Artikel erscheint nicht in der Ungelesen-Liste, Nicht-Treffer schon | `KeywordFilterTests_E2E.E2E_KeywordFilter_MatchedItemNotInUnreadList` (`SettingsViewModel.AddKeywordAsync` → `FeedSyncService.SyncFeedAsync` → `ItemRepository.GetUnreadByDateAsync`, SQLite In-Memory; Konvention `SettingsViewModelTests_E2E`) | Abgedeckt |
| Gefiltertes Item erzeugt keine Benachrichtigung (benutzerseitig sichtbares Verhalten über Service-Grenzen) | `KeywordFilterTests_E2E.E2E_KeywordFilter_MatchedItemNoNotification` (echter `NotificationService` + `FakeLocalNotificationService`) | Abgedeckt |
| Ausweisung verworfener Treffer im Sync-Verlauf (`SyncLog.Message`, bestätigter Beschluss #4) | Kein Test geplant — weder Service- noch E2E-Ebene | Lücke |
| Listen „Kategorien" und „Später lesen" ohne Treffer | Indirekt abgedeckt: Filterung erfolgt beim Einspeichern, sodass alle Lesezugriffe automatisch trefferfrei sind; `GetUnreadByDateAsync` wird im E2E-Test als Datenquelle des `UnreadViewModel` geprüft | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

- [ ] Bestätigter Beschluss #4 ist nicht als ausführbarer Schritt in der Umsetzungsreihenfolge verankert: Der Zähler für verworfene Items und die Erweiterung der `UpdateLogAsync`-Message fehlen in Schritt 2 bzw. als eigener Schritt; gleichzeitig ist `UpdateLogAsync` in „Änderungen an bestehenden Klassen" unter „Unverändert" gelistet und müsste als geändert geführt werden.
- [ ] Bestätigter Beschluss #5 ist nicht in den Tests-Abschnitt überführt: `AtomXml`-Hilfsmethode fehlt in der Liste der Hilfsmethoden/Testdaten, der Atom-Match-Test fehlt in der Tests-Tabelle und in der Umsetzungsreihenfolge (Schritt 3).

## Hinweise

- Ein dedizierter Negativtest für den Fehlerpfad „`IKeywordRepository.GetAllAsync` wirft → `FeedHealth.Error` + `SyncLog`" wäre sinnvoll, da der Ingest-Filter eine neue Fehlerquelle in `RunSyncAsync` einführt; die Anforderung beschreibt die Fehlerisolation nur als bestehendes Verhalten, daher nicht als harte Lücke gewertet. Für einen solchen Test fehlt bislang ein werfendes `IKeywordRepository`-Test-Double (Analogon zu `DelegatingItemRepository`).
- Die bestätigten Entscheidungen #1–#3 (Bestandsdaten unangetastet, nachträgliche Keywords akzeptabel, `RetentionCleanupService`-Keyword-Regel unverändert) sind konsistent im Plan verankert; die bedingte Anpassung der drei `RetentionCleanupServiceTests` entfällt damit korrekt.
- Sollte Beschluss #4 umgesetzt werden, ist zusätzlich die Formulierung in `SyncAllAsync`-Aggregat-/Message-Texten zu prüfen (Aggregat zählt weiterhin nur `NewItems` gespeicherter Items).
