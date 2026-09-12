# Plan-Gegenprüfung

Geprüft: `plan.md` (Issue #28, Offline-Fähigkeit + Mehrsprachigkeit EN/DE) gegen `requirement.md`, `inventory.md` inkl. Detaildokumente sowie die verbindlichen Anwender-Entscheidungen (Sprachwechsel zurückgestellt auf Issue #62; OpenInBrowser offline mit Hinweis-Guard / Share unverändert; Link-Deaktivierung via `<a>`-Neutralisierung + Navigating-Alert; SyncLog bleibt englisch; `<img>`-Tags offline entfernt; Sync-Offline-Status auf UnreadPage **und** FeedsPage). Zeilenangaben des Plans wurden stichprobenartig gegen `src/` verifiziert und sind korrekt.

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| App zeigt bereits synchronisierte Artikel/Kategorien/Feeds ohne Netzwerk an | Bereits durch lokale SQLite-Lesepfade gegeben (Repositories über `IDbContextFactory`); Plan verifiziert dies über das E2E-Szenario „App komplett offline starten" | Manuelle Verifikation → `test-results.md` (E2E-Tabelle, Pflicht) | Abgedeckt |
| Keine Abstürze bei fehlender Netzwerkverbindung | `FeedSyncService`-Offline-Guard (`SyncAllAsync`/`SyncFeedAsync`), `AutoRefreshService`-Tick-Skip, `OpenInBrowserAsync`-Guard + try/catch, bestehende Exception→`SyncResult`-Abbildung bleibt | Unit-Tests `FeedSyncServiceTests`/`AutoRefreshServiceTests` + manuelle Szenarien | Abgedeckt |
| Synchronisations-Button zeigt Offline-Status an (UnreadPage) | `DataTrigger` `IsOnline == False` am Sync-Button (Opacity/Stroke-Dimmung) + `OfflineHint`-Label + `ErrorMessage`-Label in `UnreadPage.xaml`; Frühabbruch in `UnreadViewModel.RefreshAsync` | Unit-Tests `UnreadViewModelTests` (3 neue) + manuelle Verifikation | Abgedeckt |
| Sync-Offline-Status auf FeedsPage (Anwender-Entscheidung „UnreadPage **und** FeedsPage") | Nur reaktiv: `ErrorMessage = AppResources.OfflineHint` nach einem Sync-Versuch (`RefreshAsync`/`RefreshAllAsync`); Plan sagt explizit „keine XAML-Änderung nötig" — **kein persistenter sichtbarer Offline-Indikator auf FeedsPage geplant** (kein gedimmter Zustand, kein dauerhaft sichtbarer `OfflineHint` wie auf UnreadPage) | Nur manuelle Verifikation des reaktiven Hinweises; kein Szenario für einen dauerhaft sichtbaren Offline-Status | Lücke |
| Links im Artikelinhalt sind im Offline-Modus nicht klickbar | `ArticleHtmlSanitizer.Sanitize(html, forOffline)` neutralisiert `<a>`-Tags; ergänzend `Navigating`-Handler `OnWebViewNavigating` mit `e.Cancel = true` + lokalisiertem `DisplayAlertAsync` | Unit-Tests `ArticleHtmlSanitizerTests` + manuelle Verifikation | Abgedeckt |
| `<img>`-Tags offline aus Artikel-HTML entfernt (Anwender-Entscheidung) | `forOffline`-Schalter entfernt `<img>`-Elemente vollständig; CSP `img-src *` bleibt online unverändert | Unit-Test `Sanitize_Offline_RemovesImgTags` + manuelle Verifikation | Abgedeckt |
| „Im Browser öffnen" offline mit Hinweis-Guard; „Teilen" unverändert (Anwender-Entscheidung) | `OpenInBrowserAsync`: Offline → `ErrorMessage = AppResources.OfflineHint` statt `Browser.OpenAsync`, Online-Pfad try/catch; `ShareAsync` unverändert; neue `ErrorMessage`/`HasError`-Properties + `ErrorMessage`-Label auf `ArticleDetailPage` | **Kein Testnachweis** — `ArticleDetailViewModel` liegt im MAUI-Projekt und ist nicht unit-testbar; kein manuelles Verifikationsszenario für „Browser-Button offline tippen → Hinweis, kein Absturz" in der E2E-Tabelle | Lücke (fehlender Testnachweis) |
| UI-Texte erscheinen auf Deutsch oder Englisch entsprechend der Systemsprache; EN = Fallback für Drittsprachen | Alle verbleibenden hartcodierten Texte überführt: `ArticleDetailPage.xaml` (Z. 9, 26, 128, 139, 160, 212, 257, 280 — verifiziert), `ArticleDetailViewModel` (Z. 191, 196, 320 — verifiziert), `CategoriesViewModel` (Z. 120, 128 — verifiziert, nicht im Inventory gelistet, im Plan entdeckt); 17 neue Schlüssel in `AppResources.resx`/`.de.resx`; `Designer.cs`-Regenerierung; `translation-check.py`-Hook | Manuelle Verifikation DE/EN/Fallback mit Screenshots | Abgedeckt |
| Manueller Sprachwechsel bewusst zurückgestellt (Issue #62) | Explizit aus dem Scope genommen: kein `Settings.Language`, kein Picker, keine Migration — dokumentiert in Übersicht und Abschnitt „Datenbankmigrationen: Keine" | Nicht erforderlich | Abgedeckt |
| `SyncLog`-Meldungen bleiben englisch (Anwender-Entscheidung) | Explizit dokumentiert (Abschnitt `FeedSyncService` + Risiken); Offline-Guards schreiben bewusst **keine** `SyncLog`-Einträge | Unit-Tests `SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog` / `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange` | Abgedeckt |
| Kein Fehler-Rauschen im `SyncLog` durch Offline-Syncs (Hintergrund + manuell) | `AutoRefreshService.RunLoopAsync` überspringt Ticks bei `!IsOnline`; `FeedSyncService`-Guards ohne Log/Health-Update | Unit-Tests `AutoRefreshServiceTests` (2 neue) + `FeedSyncServiceTests` (2 neue) | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] **Kein Verifikations-/Testszenario für den `OpenInBrowser`-Offline-Guard** (Anwender-Entscheidung „OpenInBrowser offline mit Hinweis-Guard"; Akzeptanzkriterium „keine Abstürze bei fehlender Netzwerkverbindung"): `ArticleDetailViewModel` liegt in `src/Reporter/` und ist von `Reporter.Tests` nicht erreichbar — die Änderung ist damit weder unit- noch durch ein geplantes manuelles Szenario abgedeckt. Es fehlt ein manuelles E2E-Szenario „Artikeldetail offline → Tap auf 'Im Browser öffnen' → lokalisierter Hinweis im neuen `ErrorMessage`-Label, kein `Browser.OpenAsync`, kein Absturz" (sowie implizit die Sichtbarkeitsprüfung des neuen `ErrorMessage`-/`ArticleOfflineLinksDisabled`-Labels auf der Detailseite).
- [ ] **Kein Verifikationsszenario für einen dauerhaft sichtbaren Offline-Status auf FeedsPage**: Die geplanten manuellen Szenarien prüfen auf Feeds nur den reaktiven Hinweis nach Pull-to-Refresh/Einzel-Sync. Falls (siehe unten) ein persistenter Indikator nachgeplant wird, fehlt auch dessen Verifikation.

## E2E-Abdeckung

Es existiert keine automatisierte UI-Test-Infrastruktur (`Reporter.Tests` referenziert das MAUI-Projekt nicht; kein Appium/UITest-Projekt). Der Plan begründet nachvollziehbar, warum dokumentierte manuelle UI-Verifikation mit Screenshots (gemäß AGENTS.md: 390×844 pt Windows-Handysize, ggf. iOS-Simulator via `scripts/iOS-Deployment.ps1`, Dokumentation in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md`, Referenz-Designs `design-draft/stitch_local_rss_feed_reader/`) der geforderte Nachweis ist. Alle genannten Artefakte existieren im Repo.

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Offline-Start: Ungelesen/Später/Kategorien/Feeds/Artikeldetail lesbar | Manuelle Verifikation → `test-results.md` (Pflicht) | Abgedeckt |
| Sync-Button UnreadPage: Offline-Status (gedimmt + Hinweis), Tap/Pull-to-Refresh → lokalisierter Hinweis, kein `SyncLog`-Rauschen | Manuelle Verifikation → `test-results.md` (Pflicht) | Abgedeckt |
| WebView-Links offline nicht klickbar/neutralisiert, Rest-Navigation → lokalisierter Alert | Manuelle Verifikation → `test-results.md` (Pflicht) | Abgedeckt |
| Artikeldetail offline ohne `<img>`-Platzhalter, Text vollständig lesbar | Manuelle Verifikation → `test-results.md` (Pflicht) | Abgedeckt |
| Online↔Offline-Wechsel zur Laufzeit ohne Neustart (Status + Link-Verhalten folgen) | Manuelle Verifikation → `test-results.md` (Pflicht) | Abgedeckt |
| Systemsprache DE/EN/Drittsprache-Fallback für alle UI-Texte | Manuelle Verifikation mit Screenshots → `test-results.md` (Pflicht) | Abgedeckt |
| Feeds-Seite offline: Pull-to-Refresh + Einzel-Feed-Aktualisierung → lokalisierter Hinweis, kein Absturz | Manuelle Verifikation → `test-results.md` (Pflicht) | Abgedeckt (reaktiver Hinweis) |
| Feeds-Seite: dauerhaft sichtbarer Offline-Status ohne Nutzeraktion | **Nicht geplant** (Plan: „keine XAML-Änderung nötig") | Lücke |
| Artikeldetail offline: „Im Browser öffnen" → Hinweis-Guard statt `Browser.OpenAsync` | **Nicht geplant** | Lücke |
| iOS-Plattform-Abdeckung (Connectivity/WebView-Abweichungen) | Manuelle Verifikation (Soll), dokumentierte Einschränkung falls kein macOS-Host | Abgedeckt |

## Fehlende oder unvollständige Planbestandteile

- [ ] **Persistenter Offline-Status auf FeedsPage fehlt** (Anwender-Entscheidung „Sync-Offline-Status auf UnreadPage UND FeedsPage"): Der Plan implementiert auf FeedsPage ausschließlich den reaktiven `ErrorMessage`-Hinweis nach einem Sync-Versuch und plant explizit keine XAML-Änderung. Auf UnreadPage ist dagegen ein dauerhaft sichtbarer Zustand geplant (gedimmter Sync-Button via `DataTrigger` + `OfflineHint`-Label). Für die Feeds-Seite fehlt ein äquivalentes, ohne Nutzeraktion sichtbares Indikator-Element — z. B. ein `OfflineHint`-Border mit `DataTrigger` auf `FeedsViewModel.IsOnline == False` nach dem dokumentierten `NotificationsIosOnlyHint`-Muster (`FeedsPage.xaml` Z. 51–64). Die geplante `IsOnline`-Property im `FeedsViewModel` wäre dafür bereits vorhanden, wird aber in keinem UI-Element verwendet.

## Hinweise

- `FeedsViewModel`: `ConnectivityChanged`→`IsOnline` ist geplant, aber nur `UnreadViewModelTests` enthält `ConnectivityChanged_UpdatesIsOnline`; ein analoger Test für `FeedsViewModel` wäre konsistent (kein Pflicht-Gap, da die ACs anderweitig abgedeckt sind).
- `ArticleCardView` bindet `ItemListItem.ImageUrl` (Z. 74–77) — Listen-Thumbnails laden offline nicht und hinterlassen leere 80×80-Flächen. Die Anwender-Entscheidung zu `<img>` bezog sich auf das Artikel-HTML in der Detailansicht; für die Listen-Thumbnails gibt es weder Entscheidung noch Planbaustein — ggf. mit dem Anwender klären oder bewusst dokumentieren.
- `ArticleDetailViewModel.Dispose()` wird über `ArticleDetailPage.OnDisappearing` ausgelöst. `OnDisappearing` feuert auch bei temporärer Überdeckung der Seite; danach ist das (noch lebende) ViewModel vom `ConnectivityChanged`-Event abgemeldet und `IsOnline`/`RebuildHtml` folgen Statuswechseln nicht mehr. Edge-Case, bei der Umsetzung prüfen.
- Das auf FeedsPage wiederverwendete `ErrorMessage`-Label (Z. 65–68) liegt **innerhalb** der Feed-Eingabekarte — der Offline-Hinweis erscheint dort im Formular-Kontext, nicht als globaler Seitenhinweis. Funktional korrekt, bei der UI-Verifikation auf Verständlichkeit achten.
- `UnreadPage` zeigt mit dem neuen `ErrorMessage`-Label erstmals auch bisher stille Sync-Fehler — im Plan als gewollte Verhaltensänderung benannt (Risiken); bei der manuellen Verifikation mitprüfen.
- Alle im Plan genannten Code-Zeilenreferenzen (`ArticleDetailPage.xaml` Z. 9/26/108–115/128/139/160/212/257/280, `ArticleDetailViewModel.cs` Z. 191/196/320/329–400/484–506, `UnreadPage.xaml` Z. 21–41/105–107, `FeedsPage.xaml` Z. 33–37/51–64/65–68/75–77, `CategoriesViewModel.cs` Z. 120/128, `FeedSyncService.cs` Z. 64–74, `AutoRefreshService.cs` Z. 109–134, `MauiProgram` Z. 42–70, `App.xaml.cs` Z. 51–62) sowie die Test-Konstruktor-Aufrufstellen (`UnreadViewModelTests` Z. 30, `FeedsViewModelTests` Z. 40, `AutoRefreshServiceTests` Z. 28, `FeedSyncServiceTests` Z. 71/78) wurden gegen den Quellcode verifiziert und stimmen. `CategoriesViewModelTests` assertieren nur `HasError`, nicht den Literaltext — keine Testanpassung nötig (korrekt nicht als betroffen gelistet). Keine `Connectivity`/`NetworkAccess`-Verwendung in `src/` vorhanden (Bestandsaufnahme bestätigt).
- Optional: Anwenderdoku (`docs/help/anwendung/synchronisation.md`, `artikeldetailansicht.md`) beschreibt das geänderte Offline-Verhalten nicht — keine geforderte Planleistung, aber sinnvolle Ergänzung.
