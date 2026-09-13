<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Treffer-Artikel (Keyword-Match auf `Title`/`ContentHtml`, Teilwort, `OrdinalIgnoreCase`) dürfen nicht in den Listen (Ungelesen, Kategorien, Später lesen) geführt werden — Filterung spätestens beim Einspeichern | Ingest-Filter in `FeedSyncService.RunSyncAsync` (Designentscheidung, Programmablauf Schritte 2–5, Umsetzungsreihenfolge Schritte 1–2): Keywords pro Lauf via `IKeywordRepository.GetAllAsync` laden, `MatchesAny` pro Item, Treffer via `continue` nicht in `newItemEntities` | `SyncFeedAsync_KeywordTitleMatch_NotSaved` (prüft u. a. `GetUnreadByDateAsync`), `SyncFeedAsync_KeywordContentHtmlMatch_NotSaved`, `SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved`, E2E `E2E_KeywordFilter_MatchedItemNotInUnreadList` | Abgedeckt |
| Treffer werden weder gespeichert noch benachrichtigt | Programmablauf Schritte 4/8: gefilterte Items erreichen `AddRangeAsync` und `NotifyNewItemsAsync` nicht; Keyword-Check im `NotificationService` bleibt als Tiefenverteidigung | `SyncFeedAsync_KeywordFiltered_NotNotified` (echter `NotificationService` + `FakeLocalNotificationService`), E2E `E2E_KeywordFilter_MatchedItemNoNotification` | Abgedeckt |
| Match-Semantik unverändert wiederverwenden (`IKeywordMatcher`/`KeywordMatcher.MatchesAny`) | Wiederverwendung der bestehenden Singleton-Implementierung als neue Konstruktor-Abhängigkeit | Bestehende `KeywordMatcherTests` unverändert gültig; ContentHtml-Match zusätzlich durch `SyncFeedAsync_KeywordContentHtmlMatch_NotSaved` auf Service-Ebene | Abgedeckt |
| Nicht-Treffer- und Normalverhalten bleibt unverändert | `AddRangeAsync` speichert Nicht-Treffer unverändert (Programmablauf Schritt 5) | `SyncFeedAsync_KeywordNoMatch_SavesNormally`, `SyncFeedAsync_EmptyKeywords_SavesAll`; bestehende `FeedSyncServiceTests` laufen über angepasste Fabriken weiter | Abgedeckt |
| Leere Keyword-Liste → kein Mehraufwand | Designentscheidung „Ladezeitpunkt" + Programmablauf Schritt 4 (Match nur bei nicht-leerer Liste) | `SyncFeedAsync_EmptyKeywords_SavesAll` | Abgedeckt |
| Deterministisches Re-Matching bei Folge-Syncs (kein Duplikat, `knownKeys` unverändert) | Programmablauf Schritt 4 (gefilterte Items bleiben außerhalb der Persistenz, werden je Sync erneut gematcht) | `SyncFeedAsync_KeywordFiltered_ResyncStaysFiltered` | Abgedeckt |
| Kein Health-False-Positive (`DetermineStatus` zählt `fetchedCount` inkl. gefilterter Items) | Programmablauf Schritt 6 + Seiteneffekt „Feed-Health" (inkl. dokumentierter Verhaltensänderung bei dauerhaft komplett gefilterten Feeds) | Bestehende `DetermineStatus`-bezogene Tests (`SyncFeedAsync_FewerItems_SetsWarning`, `…_NoNewItemsForThirtyDays_SetsWarning`) bleiben unverändert gültig | Abgedeckt |
| Fehlerisolation: Fehler beim Keyword-Laden → `FeedHealth.Error` + `SyncLog` | Programmablauf Schritt 9 (bestehender `try/catch` in `SyncFeedAsync`) | Kein dedizierter Negativtest geplant — siehe Hinweise | Abgedeckt |
| `RetentionCleanupService`-Keyword-Regel bleibt unverändert (bestätigter Beschluss); Lösch-Invariante „niemals ungelesen, niemals gemerkt" gewahrt | Designentscheidung + Programmablauf „Aufbewahrungs-Cleanup beim App-Start (unverändert)" | Bestehende Tests `CleanupAsync_KeywordMatch_KeepsUnreadAndSaved`, `CleanupAsync_DeletesKeywordMatchedExpired`, `CleanupAsync_KeywordMatch_UsesPublishedAtOverReadAt` bleiben unverändert gültig | Abgedeckt |
| Bestandsdaten unangetastet; nachträglich erfasste Keywords lassen Bestandstreffer sichtbar (bestätigte Beschlüsse) | Designentscheidung + Seiteneffekt „Bestandsdaten"; Doku-Aktualisierung beschreibt das Verhalten transparent | Kein Test erforderlich (bewusste Nicht-Änderung; Verbleib der Altdaten-Löschregel ist durch `RetentionCleanupServiceTests` abgesichert) | Abgedeckt |
| Verworfene Treffer werden in `SyncLog.Message` ausgewiesen (bestätigter Beschluss, z. B. „Synchronized 6 items, 5 new, 1 filtered.") | Designentscheidung „Ausweisung verworfener Treffer"; Programmablauf Schritt 7; `UpdateLogAsync` ist jetzt korrekt als geänderte Methode geführt; eigener Umsetzungsschritt 3 („Gefilterte Anzahl in `SyncLog.Message` ausweisen") | `SyncFeedAsync_KeywordFiltered_LogCountsFiltered` (Message enthält `, N filtered`; `result.Message` identisch), E2E `E2E_KeywordFilter_SyncLogReportsFiltered` | Abgedeckt |
| Atom-Parsing-Pfad abgedeckt (bestätigter Beschluss — Beispielfeed golem.de ist `ATOM1.0`) | Umsetzungsreihenfolge Schritt 4: `AtomXml`-Hilfsmethode nach dem Muster von `RssXml` ergänzen | `SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved` + Hilfsmethode `AtomXml` in der Tests-Tabelle | Abgedeckt |
| Hinweistext `SettingsKeywordInfo` (EN/DE) an neue Filter-beim-Speichern-Semantik anpassen | Änderungsabschnitt „Ressourcentexte" (nur `<value>`, `AppResources.Designer.cs` unverändert) + Umsetzungsreihenfolge Schritt 7 | Ressourcen-Textänderung; kein Test erforderlich | Abgedeckt |
| Hilfsdokumentation (`docs/help/…`) auf neue Semantik aktualisieren | Vollständige Dateiliste inkl. Zeilenverweisen (beschreibung, einrichtung-anwender, fehlerbehebung-anwender, business-rules, ablauf-technisch, architektur, troubleshooting, aufbewahrung, benachrichtigungen/business-rules + Sichtungsliste) + Umsetzungsreihenfolge Schritt 8 | — | Abgedeckt |
| `IFeedSyncService`-Signatur stabil; erweiterter Konstruktor über DI auflösbar (beide Abhängigkeiten bereits als Singletons registriert) | Umsetzungsreihenfolge Schritt 1; Voraussetzung in `MauiProgram` (Zeilen 51/58) verifiziert | `AddReporterServices_ResolvesFeedSyncService` in `ServiceCollectionTests` | Abgedeckt |
| Keine Datenbankmigration, kein persistiertes Filter-Flag, keine Konfigurations-/Validierungsänderung | Abschnitte „Datenbankmigrationen: Keine", „Validierungsregeln: Keine", „Konfigurationsänderungen: Keine" | — | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine — die beiden in `plan-check.1.md` gemeldeten Testlücken (`SyncLog`-Ausweisung, Atom-Pfad) sind in den aktualisierten Plan eingearbeitet.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Schlagwort in den Einstellungen anlegen → Feed synchronisieren → Treffer-Artikel erscheint nicht in der Ungelesen-Liste, Nicht-Treffer schon | `KeywordFilterTests_E2E.E2E_KeywordFilter_MatchedItemNotInUnreadList` (`SettingsViewModel.AddKeywordAsync` → `FeedSyncService.SyncFeedAsync` → `ItemRepository.GetUnreadByDateAsync`, SQLite In-Memory; Konvention `SettingsViewModelTests_E2E`) | Abgedeckt |
| Gefiltertes Item erzeugt keine Benachrichtigung (benutzerseitig sichtbares Verhalten über Service-Grenzen) | `KeywordFilterTests_E2E.E2E_KeywordFilter_MatchedItemNoNotification` (echter `NotificationService` + `FakeLocalNotificationService` im Verbund) | Abgedeckt |
| Ausweisung verworfener Treffer im Sync-Verlauf (`SyncLog.Message`, bestätigter Beschluss) | `KeywordFilterTests_E2E.E2E_KeywordFilter_SyncLogReportsFiltered` (persistierte `SyncLog.Message` weist die Anzahl gefilterter Items aus) | Abgedeckt |
| Listen „Kategorien" und „Später lesen" ohne Treffer | Indirekt abgedeckt: Filterung erfolgt beim Einspeichern, sodass alle Lesezugriffe (`GetSavedForLaterAsync`, `GetByCategoryAsync`, Detailansicht) automatisch trefferfrei sind; `GetUnreadByDateAsync` wird im E2E-Test als Datenquelle des `UnreadViewModel` geprüft | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

Keine — beide in `plan-check.1.md` gemeldeten Planlücken sind geschlossen:

- Beschluss zur `SyncLog`-Ausweisung ist jetzt als eigener Umsetzungsschritt 3 verankert, `UpdateLogAsync` ist unter „Geänderte Methoden" (statt „Unverändert") geführt, und das `SyncAllAsync`-Aggregat ist explizit beschrieben.
- Beschluss zum Atom-Pfad ist in der Umsetzungsreihenfolge (Schritt 4) und in der Tests-Tabelle (`AtomXml`-Hilfsmethode + `SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved`) verankert.

## Hinweise

- Ein dedizierter Negativtest für den Fehlerpfad „`IKeywordRepository.GetAllAsync` wirft → `FeedHealth.Error` + `SyncLog`" wäre sinnvoll, da der Ingest-Filter eine neue Fehlerquelle in `RunSyncAsync` einführt; die Anforderung beschreibt die Fehlerisolation nur als bestehendes Verhalten, daher nicht als harte Lücke gewertet. Dafür fehlt bislang ein werfendes `IKeywordRepository`-Test-Double (Analogon zu `DelegatingItemRepository`).
- Bei der Umsetzung ist zu prüfen, ob die Projektregel „Mobile UI Design Review" (`AGENTS.md`) durch die Textänderung `SettingsKeywordInfo` auf der `SettingsPage` ausgelöst wird; ggf. ist die manuelle UI-Verifikation zu dokumentieren. Die Änderung ist rein textuell, eine Lücke im Sinne der Anforderung liegt nicht vor.
- Bei der Formulierung der erweiterten Sync-Message ist der bestehende Message-Stil (`Synchronized N items, M new.`) konsistent fortzuführen — der Plan nennt bereits ein passendes Beispiel (`Synchronized 6 items, 5 new, 1 filtered.`) und beschreibt die Übernahme in das `SyncAllAsync`-Aggregat.
