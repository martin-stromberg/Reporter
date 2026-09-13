<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| R0 — Bewertung, Auswahl und Abspaltung in GitHub-Issues vor Implementierung | Triage-Tabelle mit Begründung für alle acht Punkte; Programmablauf „R0 — Abspaltung in GitHub-Issues"; Schritt 1 der Umsetzungsreihenfolge legt zwei Issues via `gh issue create` an und vermerkt die Nummern in `plan.md`/`todo.md` | Meta-Aufgabe, kein Test erforderlich | Abgedeckt |
| R1 — Lesezeit bei 1-Minuten-Beiträgen in der Auflistung nicht anzeigen | `ReadingTimeEstimator.EstimateText` liefert `string.Empty` bei ≤ 1 Minute; greift über `ItemRepository.MapToListItem` auf **Ungelesen**/**Später** und über `ArticleDetailViewModel` in der Detailansicht (geklärte Erweiterung); keine XAML-Änderung nötig (`StringNotEmptyToBoolConverter`) | `EstimateText_OneMinuteContent_ReturnsEmpty`, `EstimateText_TwoMinuteContent_ReturnsText`, `GetUnreadByDateAsync_OneMinuteReadingTime_ProjectsEmpty`; Anpassung `EstimateText_ShortContent_ReturnsOneMinute`; manuelle Verifikation `UnreadPage`/`ArticleDetailPage` | Abgedeckt |
| R2 — Standardbild: Favicon bei Feed-Anlage suchen und persistent speichern (EF-Migration), Kaskade Favicon → generierter Initialen-Kreis | `Feed.FaviconUrl` + Migration `AddFeedFaviconUrl`; `IFeedIconService`/`FeedIconService` (Link-Tag-Parsing, `/favicon.ico`-Fallback mit Verifikation); Erfassung in `TryPersistNewFeedAsync` für alle drei Anlage-Wege (`siteUrl`-Parameter, `IsOnline`-Guard, fehlerisoliert); Nachhole-Pfad in `FeedSyncService.UpdateFeedHealthAsync` für Bestandsfeeds; Anzeige-Kaskade in `ArticleCardView`, `FeedsPage` und `ArticleDetailPage` (`FeedIconUrl` aus bereits geladenem `feed`); `ToFeed`-Mitnahme gegen Datenverlust bei Teil-Updates | `FeedIconServiceTests` (Link-Tag, relative URLs, Fallback, Fehler), `GetUnreadByDateAsync_ProjectsFeedFaviconUrl`, `GetAllWithDetailsAsync_ProjectsFaviconUrl`, `UpdateAsync_PersistsFaviconUrl`, `TryPersistNewFeed_*` (3 Tests), `RenameFeedAsync_PreservesFaviconUrl`, `SyncFeed_SetsFaviconWhenMissing`; manuelle Verifikation Light + Dark | Abgedeckt |
| R3 — Konfigurierbarer Feed-Abgleich beim Programmstart | `Settings.RefreshOnStartupEnabled` (`bool`, Default `true`) + Migration; `Switch` in Sektion **Synchronisation & Lesefluss**; Sofort-Persistierung via `PersistOnChange`/`PersistAsync`; Start-Sync in `AutoRefreshService.StartAsync` (fehlerisoliert, `IsOnline`-Guard, fire-and-forget, kein Eingriff in `App.OnStart` nötig) | `StartAsync_StartupRefreshEnabled_SyncsImmediately` / `_Disabled_DoesNotSyncImmediately` / `_Offline_SkipsStartupSync`; `Load_PopulatesRefreshOnStartup`, `RefreshOnStartup_Change_Persists`, `SaveAsync_PersistsNewFields`, `E2E_RefreshOnStartup_PersistRoundtrip`; manuelle Verifikation | Abgedeckt (Umsetzung/Tests), aber betroffene Bestandstests unvollständig genannt — siehe Lücken |
| R4 — Konfigurierbare Datums-Sortierung der Startseite **Ungelesen** | `Settings.UnreadSortOrder` (`"desc"`/`"asc"`, `SettingsValues`-Konstanten, Default `"desc"`) + Migration; `SortOrderOption` + `Picker`; `IItemRepository.GetUnreadByDateAsync(…, ascending)` inkl. Tiebreaker-Umkehr; `ISettingsRepository`-Abhängigkeit in `UnreadViewModel`; **Später** bleibt absteigend; `PublishedAt == null`-Verhalten dokumentiert | `GetUnreadByDateAsync_Ascending_*` (Sortierung + NULL-Doku), `LoadPage_PassesSortOrderFromSettings`, `Load_PopulatesSelectedSortOrder`, `InvalidSortOrder_UsesDescFallback`, `SelectedSortOrder_Change_Persists`, `E2E_SortOrder_PersistRoundtrip`; `DelegatingItemRepository`/Fakes mitgezogen | Abgedeckt |
| R5 — Neustart-Hinweis nur nach Sprachänderung | `LanguageRestartHintVisible`-Flag in `SettingsViewModel`; Setzen/Rücksetzen im `SelectedLanguage`-Setter (außerhalb `_isLoading`, Vergleich mit persistiertem Wert, Rückkehr löscht Flag); Reset in `LoadAsync`; `IsVisible`-Binding auf Hinweis-`Border` | `LanguageChange_SetsRestartHint`, `LanguageReverted_ClearsRestartHint`, `Load_ResetsRestartHint`; manuelle Verifikation | Abgedeckt |
| R6 — Neues Programmsymbol (Icon- und SplashScreen-Variante) | Attachments als SVG verifiziert beschaffbar (authentifizierter Download); `appiconfg.svg`/`splash.svg` ersetzen, `appicon.svg`-Hintergrund `#1e293b` bleibt; `<text>`→Pfade wegen Resizetizer; `MauiIcon`/`MauiSplashScreen`/`Color`/`BaseSize` geprüft; `Info.plist`/`Package.appxmanifest` unverändert | Manuelle Sichtprüfung der generierten Assets (Windows-Build, iOS via `scripts/iOS-Deployment.ps1`) — nicht automatisierbar | Abgedeckt |
| R7 — Debuginformationen per E-Mail | **Abgespalten** — Issue-Anlage mit fachlicher Beschreibung in Plan-Schritt 1 verankert | — | Abgedeckt (Abspaltung verankert) |
| R8 — Benachrichtigungen nur bei Hintergrundabruf | **Abgespalten** — Issue-Anlage inkl. Infrastruktur-Befund in Plan-Schritt 1 verankert | — | Abgedeckt (Abspaltung verankert) |

## Fehlende oder unvollständige Testanforderungen

- [ ] **R3 — `AutoRefreshServiceTests` fehlt in „Betroffene bestehende Tests".** Die Klasse instanziiert `new Settings { … }` direkt in `BuildSettings`/`SaveSettingsAsync` (`src/Reporter.Tests/AutoRefreshServiceTests.cs` Zeilen 45 und 60) — mit dem geplanten `required`-Member `Settings.RefreshOnStartupEnabled` schlägt die Kompilierung fehl, solange die Initializer das Feld nicht setzen. Zusätzlich assertiert `StartAsync_InvokesSyncAfterInterval` (Zeilen 81–91) exakt `SyncAllCallCount == 1` nach dem ersten Intervall-Tick; mit dem neuen Sofort-Sync beim Start (Default `true`) wäre der Zähler 2 — der Test ist ohne explizites `RefreshOnStartupEnabled = false` im Setup semantisch gebrochen. Der Plan muss die Klasse als betroffen nennen und festlegen, dass die Test-Settings den Schalter steuern.
- [ ] **R3/R4 — `new Settings`-Initializer außerhalb des `TestSettingsHelper` sind nicht als betroffen genannt.** Weil `RefreshOnStartupEnabled` (und ggf. `UnreadSortOrder`, je nach `required`-Festlegung) als `required` eingeführt wird, brechen alle direkten Objekt-Initializer ohne das Feld: `SettingsRepositoryTests` (`src/Reporter.Tests/SettingsRepositoryTests.cs` Zeilen 73, 100, 143, 170), `RetentionCleanupServiceTests` (Zeile 43), `AutoRefreshServiceTests` (Zeilen 45, 60). Der Plan nennt nur `TestSettingsHelper` — und das lediglich mit „ggf."; die Helper-Erweiterung ist bei `required` zwingend, nicht optional. Die Tabelle „Betroffene bestehende Tests" ist entsprechend zu vervollständigen (oder die `required`-Festlegung im Plan zu überdenken).

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

- [ ] Die Tabelle „Betroffene bestehende Tests" ist zu vervollständigen um `AutoRefreshServiceTests` (Kompilier- und Verhaltensbruch durch `required`-Feld + Sofort-Sync), `SettingsRepositoryTests` und `RetentionCleanupServiceTests` (Kompilierbruch durch `required`-Member `Settings.RefreshOnStartupEnabled` in direkten `new Settings`-Initializern); die `TestSettingsHelper`-Erweiterung ist als Pflicht statt „ggf." zu deklarieren.

## Hinweise

- Die übrigen Planbestandteile sind vollständig und konkret: Umsetzungsreihenfolge mit Voraussetzungen, Seiteneffekte/Risiken (u. a. `ToFeed`-Mitnahme, Startlast, Resizetizer), Migrationen, Validierungsregeln, neue `AppResources`-Schlüssel (EN/DE) und Hilfeseiten-Nachzug sind benannt.
- Für die Initialen-Kreise (Artikelkarte/Feeds-Seite) könnte bei der Umsetzung ein lokalisierter `SemanticProperties.Description`-Schlüssel für Barrierefreiheit sinnvoll sein — im Plan nicht explizit, aber ohne fachliche Auswirkung auf die Vollständigkeit.
- Verifiziert gegen `src/Reporter.Tests`: `GetSavedForLaterAsync_ProjectsReadingTimeText` und `GetUnreadByDateAsync_Paged_ProjectsReadingTimeText` nutzen 400 Wörter (≈ 2 Min.) und bleiben von R1 unberührt — korrekt, dass sie nicht als brechend gelistet sind.
