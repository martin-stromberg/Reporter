<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Tasks: Schlagwortfilter greift nicht — Treffer-Artikel werden weiterhin aufgelistet (Issue #79)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `FeedSyncService`-Konstruktor um `IKeywordRepository` und `IKeywordMatcher` erweitern (Felder `_keywordRepository`, `_keywordMatcher`) | Erledigt | `ServiceCollectionTests.AddReporterServices_ResolvesFeedSyncService` (DI-Auflösung mit erweitertem Konstruktor) |
| 2 | Logik | In `RunSyncAsync` Keyword-Liste via `_keywordRepository.GetAllAsync` laden und `keywordTexts` aufbauen (Frühabbruch bei leerer Liste) | Erledigt | `FeedSyncServiceTests.SyncFeedAsync_EmptyKeywords_SavesAll` |
| 3 | Logik | In `RunSyncAsync` pro `SyndicationItem` `_keywordMatcher.MatchesAny(title, contentHtml, keywordTexts)` auswerten; Treffer verwerfen (`continue`, nicht in `newItemEntities`) und `filteredCount` mitzählen | Erledigt | `FeedSyncServiceTests.SyncFeedAsync_KeywordTitleMatch_NotSaved`, `SyncFeedAsync_KeywordContentHtmlMatch_NotSaved`, `SyncFeedAsync_KeywordNoMatch_SavesNormally`, `SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved` |
| 4 | Logik | Anzahl verworfener Items (`filteredCount`) bei `> 0` an die Sync-Message anhängen (`, N filtered`, Ok- und Warning-Variante); `UpdateLogAsync` persistiert sie im `SyncLog`, `SyncResult.Message` trägt denselben Text | Erledigt | `FeedSyncServiceTests.SyncFeedAsync_KeywordFiltered_LogCountsFiltered`, `KeywordFilterTests_E2E.E2E_KeywordFilter_SyncLogReportsFiltered` |
| 5 | Ressourcen | `SettingsKeywordInfo` in `src/Reporter.Core/Resources/Strings/AppResources.resx` (EN) an die Filter-beim-Speichern-Semantik anpassen | Erledigt | Kein direkter Test |
| 6 | Ressourcen | `SettingsKeywordInfo` in `src/Reporter.Core/Resources/Strings/AppResources.de.resx` (DE) anpassen | Erledigt | Kein direkter Test |
| 7 | Tests | `FeedSyncServiceTests.CreateService`/`CreateFailingService` um Übergabe von `_keywordRepository` und `new KeywordMatcher()` erweitern | Erledigt | Suite kompiliert, alle bestehenden Tests grün |
| 8 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_KeywordTitleMatch_NotSaved` schreiben | Erledigt | Test bestanden |
| 9 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_KeywordContentHtmlMatch_NotSaved` schreiben | Erledigt | Test bestanden |
| 10 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_KeywordNoMatch_SavesNormally` schreiben | Erledigt | Test bestanden |
| 11 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_EmptyKeywords_SavesAll` schreiben | Erledigt | Test bestanden |
| 12 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_KeywordFiltered_NotNotified` schreiben (echter `NotificationService` + `FakeLocalNotificationService`) | Erledigt | Test bestanden |
| 13 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_KeywordFiltered_ResyncStaysFiltered` schreiben | Erledigt | Test bestanden |
| 14 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_KeywordFiltered_LogCountsFiltered` schreiben (`SyncLog.Message` enthält Anzahl gefilterter Items) | Erledigt | Test bestanden |
| 15 | Tests | `FeedSyncServiceTests`: `AtomXml`-Hilfsmethode nach dem Muster von `RssXml` ergänzen (Atom-1.0-Dokument) | Erledigt | Genutzt von `SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved` |
| 16 | Tests | `FeedSyncServiceTests`: `SyncFeedAsync_KeywordTitleMatch_AtomFeed_NotSaved` schreiben (Keyword-Match gegen Atom-Dokument) | Erledigt | Test bestanden |
| 17 | Tests | `ServiceCollectionTests`: Test `AddReporterServices_ResolvesFeedSyncService` ergänzen | Erledigt | Test bestanden |
| 18 | E2E-Tests | `src/Reporter.Tests/KeywordFilterTests_E2E.cs` anlegen: `E2E_KeywordFilter_MatchedItemNotInUnreadList` (Schlagwort anlegen → Sync → Ungelesen-Liste ohne Treffer, Nicht-Treffer vorhanden) | Erledigt | Test bestanden |
| 19 | E2E-Tests | `KeywordFilterTests_E2E`: `E2E_KeywordFilter_MatchedItemNoNotification` schreiben | Erledigt | Test bestanden |
| 20 | E2E-Tests | `KeywordFilterTests_E2E`: `E2E_KeywordFilter_SyncLogReportsFiltered` schreiben (`SyncLog.Message` weist gefilterte Anzahl aus) | Erledigt | Test bestanden |
| 21 | Dokumentation | `docs/help/einstellungen/beschreibung.md` auf Ingest-Filter-Semantik umstellen (Zeilen 16, 26, 35–36) | Erledigt | Kein direkter Test |
| 22 | Dokumentation | `docs/help/einstellungen/einrichtung-anwender.md` aktualisieren (u. a. Zeile 16) | Erledigt | Kein direkter Test |
| 23 | Dokumentation | `docs/help/einstellungen/fehlerbehebung-anwender.md` aktualisieren (u. a. Zeile 25) | Erledigt | Kein direkter Test |
| 24 | Dokumentation | `docs/help/einstellungen/business-rules.md` aktualisieren (Lösch-Invarianten, Keyword-Regel, Matching-Zeitpunkt) | Erledigt | Kein direkter Test |
| 25 | Dokumentation | `docs/help/einstellungen/ablauf-technisch.md` aktualisieren (Abschnitt 4 u. Keyword-Matching-Stellen) | Erledigt | Kein direkter Test |
| 26 | Dokumentation | `docs/help/einstellungen/architektur.md` und `docs/help/einstellungen/troubleshooting.md` aktualisieren | Erledigt | Kein direkter Test |
| 27 | Dokumentation | `docs/help/anwendung/aufbewahrung.md` aktualisieren (Abschnitt 4 inkl. Diagramm und Tabellen) | Erledigt | Kein direkter Test |
| 28 | Dokumentation | `docs/help/benachrichtigungen/business-rules.md` aktualisieren (Zeile 15); restliche Keyword-Treffer in `docs/help` sichten und ggf. mitziehen | Erledigt | Kein direkter Test |
| 29 | Qualität | `.\scripts\Run-StaticChecks.ps1` und `dotnet test src/Reporter.Tests/Reporter.Tests.csproj` ohne Befund ausführen | Erledigt | `dotnet test`: 362 bestanden / 0 fehlgeschlagen; `Run-StaticChecks.ps1`: Format, Lizenzheader, Security, Static Analysis alle ohne Befund (Exit 0) |
