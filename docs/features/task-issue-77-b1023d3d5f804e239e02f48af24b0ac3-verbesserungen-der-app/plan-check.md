<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| R0 — Bewertung, Auswahl und Abspaltung in GitHub-Issues vor Implementierung | Triage-Tabelle mit Begründung für alle acht Punkte; Programmablauf „R0 — Abspaltung in GitHub-Issues"; Schritt 1 der Umsetzungsreihenfolge legt zwei Issues via `gh issue create` an und vermerkt die Nummern in `plan.md`/`todo.md` | Meta-Aufgabe, kein Test erforderlich | Abgedeckt |
| R1 — Lesezeit bei 1-Minuten-Beiträgen in der Auflistung nicht anzeigen | `ReadingTimeEstimator.EstimateText` liefert `string.Empty` bei ≤ 1 Minute; greift über `ItemRepository.MapToListItem` auf **Ungelesen**/**Später** und über `ArticleDetailViewModel` in der Detailansicht (geklärte Erweiterung); keine XAML-Änderung nötig (`StringNotEmptyToBoolConverter`) | `EstimateText_OneMinuteContent_ReturnsEmpty`, `EstimateText_TwoMinuteContent_ReturnsText`, `GetUnreadByDateAsync_OneMinuteReadingTime_ProjectsEmpty`; Anpassung `EstimateText_ShortContent_ReturnsOneMinute`; manuelle Verifikation `UnreadPage`/`ArticleDetailPage` | Abgedeckt |
| R2 — Standardbild: Favicon bei Feed-Anlage suchen und persistent speichern (EF-Migration), Kaskade Favicon → generierter Initialen-Kreis | `Feed.FaviconUrl` + Migration `AddFeedFaviconUrl`; `IFeedIconService`/`FeedIconService` (Link-Tag-Parsing, `/favicon.ico`-Fallback mit Verifikation); Erfassung in `TryPersistNewFeedAsync` für alle drei Anlage-Wege (`siteUrl`-Parameter, `IsOnline`-Guard, fehlerisoliert); Nachhole-Pfad in `FeedSyncService.UpdateFeedHealthAsync` für Bestandsfeeds; Anzeige-Kaskade in `ArticleCardView`, `FeedsPage` und `ArticleDetailPage` (`FeedIconUrl` aus bereits geladenem `feed`); `ToFeed`-Mitnahme gegen Datenverlust bei Teil-Updates | `FeedIconServiceTests` (Link-Tag, relative URLs, Fallback, Fehler), `GetUnreadByDateAsync_ProjectsFeedFaviconUrl`, `GetAllWithDetailsAsync_ProjectsFaviconUrl`, `UpdateAsync_PersistsFaviconUrl`, `TryPersistNewFeed_*` (3 Tests), `RenameFeedAsync_PreservesFaviconUrl`, `SyncFeed_SetsFaviconWhenMissing`; manuelle Verifikation Light + Dark | Abgedeckt |
| R3 — Konfigurierbarer Feed-Abgleich beim Programmstart | `Settings.RefreshOnStartupEnabled` (`bool`, `required`, Default `true`) + Migration; `Switch` in Sektion **Synchronisation & Lesefluss**; Sofort-Persistierung via `PersistOnChange`/`PersistAsync`; Start-Sync in `AutoRefreshService.StartAsync` (fehlerisoliert, `IsOnline`-Guard, fire-and-forget, kein Eingriff in `App.OnStart` nötig) | `StartAsync_StartupRefreshEnabled_SyncsImmediately` / `_Disabled_DoesNotSyncImmediately` / `_Offline_SkipsStartupSync`; `Load_PopulatesRefreshOnStartup`, `RefreshOnStartup_Change_Persists`, `SaveAsync_PersistsNewFields`, `E2E_RefreshOnStartup_PersistRoundtrip`; betroffene `new Settings`-Initializer vollständig benannt | Abgedeckt |
| R4 — Konfigurierbare Datums-Sortierung der Startseite **Ungelesen** | `Settings.UnreadSortOrder` (`"desc"`/`"asc"`, `SettingsValues`-Konstanten, Default `"desc"`) + Migration; `SortOrderOption` + `Picker`; `IItemRepository.GetUnreadByDateAsync(…, ascending)` inkl. Tiebreaker-Umkehr; `ISettingsRepository`-Abhängigkeit in `UnreadViewModel`; **Später** bleibt absteigend; `PublishedAt == null`-Verhalten dokumentiert | `GetUnreadByDateAsync_Ascending_*` (Sortierung + NULL-Doku), `LoadPage_PassesSortOrderFromSettings`, `Load_PopulatesSelectedSortOrder`, `InvalidSortOrder_UsesDescFallback`, `SelectedSortOrder_Change_Persists`, `E2E_SortOrder_PersistRoundtrip`; `DelegatingItemRepository`/Fakes/`UnreadViewModelTests` mitgezogen | Abgedeckt |
| R5 — Neustart-Hinweis nur nach Sprachänderung | `LanguageRestartHintVisible`-Flag in `SettingsViewModel`; Setzen/Rücksetzen im `SelectedLanguage`-Setter (außerhalb `_isLoading`, Vergleich mit persistiertem Wert, Rückkehr löscht Flag); Reset in `LoadAsync`; `IsVisible`-Binding auf Hinweis-`Border` | `LanguageChange_SetsRestartHint`, `LanguageReverted_ClearsRestartHint`, `Load_ResetsRestartHint`; manuelle Verifikation | Abgedeckt |
| R6 — Neues Programmsymbol (Icon- und SplashScreen-Variante) | Attachments als SVG verifiziert beschaffbar (authentifizierter Download); `appiconfg.svg`/`splash.svg` ersetzen, `appicon.svg`-Hintergrund `#1e293b` bleibt; `<text>`→Pfade wegen Resizetizer; `MauiIcon`/`MauiSplashScreen`/`Color`/`BaseSize` geprüft; `Info.plist`/`Package.appxmanifest` unverändert | Manuelle Sichtprüfung der generierten Assets (Windows-Build, iOS via `scripts/iOS-Deployment.ps1`) — nicht automatisierbar | Abgedeckt |
| R7 — Debuginformationen per E-Mail | **Abgespalten** — Issue-Anlage mit fachlicher Beschreibung in Plan-Schritt 1 verankert | — | Abgedeckt (Abspaltung verankert) |
| R8 — Benachrichtigungen nur bei Hintergrundabruf | **Abgespalten** — Issue-Anlage inkl. Infrastruktur-Befund in Plan-Schritt 1 verankert | — | Abgedeckt (Abspaltung verankert) |

## Fehlende oder unvollständige Testanforderungen

Keine.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| R1 — Keine Lesezeit auf Karten und Detailseite bei 1-Minuten-Beitrag | `ItemRepositoryTests` (Datenebene) + manuelle Verifikation `UnreadPage`/`ArticleDetailPage` | Abgedeckt — projektkonform: keine UI-Testautomatisierung vorhanden, manuelle Verifikation mit Screenshot-Doku ist in `AGENTS.md` als Nachweis definiert; Begründung im Plan-Abschnitt „E2E-Tests" verankert |
| R2 — Artikelkarte ohne `ImageUrl` zeigt Favicon bzw. Initialen-Kreis | Manuelle Verifikation (Light + Dark, Screenshot) | Abgedeckt — rein visueller Zustand, Begründung wie oben |
| R2 — Feeds-Seite zeigt Favicon bzw. Initialen-Kreis | Manuelle Verifikation (Light + Dark, Screenshot) | Abgedeckt — Begründung wie oben |
| R3 — Schalter persistiert und triggert Sync beim Start | `E2E_RefreshOnStartup_PersistRoundtrip` (echte SQLite-Repositories) + `AutoRefreshServiceTests` + manuelle Verifikation | Abgedeckt |
| R4 — Sortier-Picker ändert Reihenfolge der Startseite | `E2E_SortOrder_PersistRoundtrip` + `ItemRepositoryTests` + manuelle Verifikation | Abgedeckt |
| R5 — Neustart-Hinweis erscheint erst nach Sprachänderung, verschwindet bei Rückwahl | `SettingsViewModelTests` + manuelle Verifikation | Abgedeckt — Sichtbarkeits-Binding nur UI-seitig prüfbar, Begründung im Plan |
| R6 — Neues Icon/Splash im Build | Manuelle Sichtprüfung Windows-Build (+ iOS via `scripts/iOS-Deployment.ps1`) | Abgedeckt — generierte Assets nur visuell verifizierbar |
| R7/R8 — Abspaltung | Issue-Anlage als Plan-Schritt 1 | Nicht erforderlich — Meta-Aufgabe ohne Benutzerfluss in diesem Lauf |

## Fehlende oder unvollständige Planbestandteile

Keine.

## Hinweise

- Die Lücke aus `plan-check.2.md` ist vollständig eingearbeitet: `KeywordFilterTests_E2E` (`CreateService`, `src/Reporter.Tests/KeywordFilterTests_E2E.cs` Zeile 60) ist nun in „Betroffene bestehende Tests" gelistet — der direkte `new FeedSyncService(...)`-Aufruf erhält ein `IFeedIconService`-Fake. Gegenprobe: `new FeedSyncService(` existiert im gesamten Repo nur in `FeedSyncServiceTests` (Zeilen 63, 70) und `KeywordFilterTests_E2E` (Zeile 60) — beide benannt.
- Verifiziert gegen `src/Reporter.Tests` und `src/Reporter`: Alle `new Settings`-Initializer stimmen exakt mit der Plan-Liste überein (`AutoRefreshServiceTests` Z. 45/60, `SettingsRepositoryTests` Z. 73/100/143/170, `RetentionCleanupServiceTests` Z. 43, `TestSettingsHelper` Z. 34, `SettingsViewModelTests_Load` Z. 50/92, `SettingsViewModelTests_Persist` Z. 387/428/467/527, Produktiv-Fallback `ArticleDetailViewModel` Z. 263); `SettingsRepository.MapToModel` (Z. 72) und `SettingsViewModel.PersistAsync` (Z. 674) sind ohnehin geplante Änderungen; `entity.HasData(new Settings())` im `ReporterDbContext` betrifft den Entity-Typ mit Property-Defaults — im Plan explizit vermerkt.
- `GetUnreadByDateAsync`-Signaturbruch verifiziert: `DelegatingItemRepository` (Z. 45–46) und der `FailingItemRepository`-Override in `UnreadViewModelTests` (Z. 533) sind über „Betroffene bestehende Tests" bzw. „Fakes mitziehen" abgedeckt; alle Aufrufstellen nutzen den optionalen `ascending`-Parameter nicht und bleiben kompilierfähig. `LaterViewModelTests`-Fakes überschreiben nur `GetSavedForLaterAsync` (unverändert) — korrekt nicht gelistet.
- `ServiceCollectionTests.AddReporterServices_ResolvesFeedSyncService` (Z. 63) löst `FeedSyncService` per DI auf und bräche ohne `IFeedIconService`-Registrierung — im Plan als betroffen benannt (Registrierung in `MauiProgram.CreateMauiApp`, Schritt 4). `ISettingsRepository` ist bereits registriert (`MauiProgram` Z. 52) — die neue `UnreadViewModel`-Abhängigkeit löst DI ohne weitere Änderung auf.
- `SettingsViewModel`-, `AutoRefreshService`-, `SettingsRepository`- und `FeedRepository`-Konstruktoren bleiben unverändert — die zahlreichen Test-Instanziierungen (u. a. `SettingsViewModelTests_*`, `KeywordFilterTests_E2E` Z. 41) sind korrekt nicht als brechend gelistet.
- Bei der Umsetzung der Initialen-Kreise (Artikelkarte/Feeds-Seite) ist ein lokalisierter `SemanticProperties.Description`-Schlüssel für Barrierefreiheit erwägenswert — ohne Auswirkung auf die Planvollständigkeit.
