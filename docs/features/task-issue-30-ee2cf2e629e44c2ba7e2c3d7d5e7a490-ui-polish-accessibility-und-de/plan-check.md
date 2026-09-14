<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Sichtbare Filter-Chip-Leiste auf `UnreadPage` (horizontal scrollbar, Pill-Chips mit Count-Kapsel, Aktiv-/Inaktiv-Zustand) statt nur `DisplayActionSheet` | Designentscheidung „Chip-Leiste"/„Kategorieauswahl Interaktion"; Programmablauf „Kategoriefilter über Chip-Leiste"; Umsetzungsschritt 8; Tasks 21–22 (Funnel-`Border`, `OnFilterClicked`, `SelectedCategoryText` entfallen — verbindlich entschieden) | Unit: `SelectCategoryCommand_UpdatesChipSelectionState` (`UnreadViewModelTests`); E2E manuell `manual-unread-*` (Tap → Filterung, Aktiv-Zustand, horizontales Scrollen, Light + Dark) | Abgedeckt |
| Floating Reader Control Bar auf `ArticleDetailPage` (52 pt, `rounded-full`, Ränder, transluzent, Safe-Area) | Designentscheidung „Floating Reader Control Bar" (MAUI-Annäherung ohne Backdrop-Blur als Entscheidung festgelegt); Schritt 11; Task 29 | E2E manuell `manual-detail-*` (Light + Dark, alle 6 Aktionen, `BookmarkGold`-Toggle) | Abgedeckt |
| Micro-Pill-Status-Badges / Health-Indikatoren auf `FeedsPage` (6-pt-Dot, `label-meta`, 10-%-Tint, dunklerer Statustext) + Feed-/Suchtreffer-Karten | Designentscheidung „Health-Status"; Schritt 10; Tasks 27–28; neue `Status*Tint`-/`Status*Text`-Tokens (Task 3) | E2E manuell `manual-feeds-*` mit `HealthStatus`-Variation per SQLite-Seed/Feed-Fehler | Abgedeckt |
| `ArticleCardView`-Abgleich: `BorderSubtle`-Hairline, 6-pt-`Secondary`-Ungelesen-Dot, echte Lesezeit statt `"—"`, `BookmarkGold`-Fill | Designentscheidungen „Ungelesen-Dot"/„Lesezeit"/„Karten-Token-Rolle"; Schritte 4 (`ReadingTimeEstimator`, `ItemListItem.ReadingTimeText`) und 9; Tasks 9–11, 24–26 | `ReadingTimeEstimatorTests` (neu), `ItemRepositoryTests`-Projektionen, visueller E2E über Karten-Screenshots | Abgedeckt |
| Genereller Token-/Shape-/Spacing-Abgleich `LaterPage`, `CategoriesPage`, `SettingsPage` (Karten-Radien, Hairline-Borders, Touch-Ziele) | Designentscheidung „CategoriesPage ohne eigenen Entwurf" (Karten-Listen-Muster übernehmen); Schritt 12; Tasks 30–32; impliziter `Border`-Style → `BorderSubtle` (Task 7) | Manueller E2E (alle Tabs, Light + Dark) + Touch-Ziel-Abgleich im Audit | Abgedeckt |
| `AppShell`-Tab-Icons/-Labels auf Design- und Accessibility-Konformität | Designentscheidung „Tab-Icons" (5 handerstellte `tab_*.svg`, Einfärbung über impliziten `Shell`-Style); Schritt 13; Task 33 | E2E Icon-/Tab-Bar-Szenario (`manual-icon-*`) | Abgedeckt |
| Programmsymbol: `appicon.svg`/`appiconfg.svg` ersetzen, `MauiIcon`-/`MauiSplashScreen`-`Color` auf Design-Palette, `dotnet_bot.png` entfernen | Designentscheidung „Programmsymbol / Splash" (handerstellter SVG-Nachbau nach Referenz-PNG, `#1e293b`); Schritt 13; Task 34 | E2E Screenshot-Vergleich mit Referenz-`screen.png` (`manual-icon-*`) | Abgedeckt |
| Token-Set vervollständigen: `DarkOnSurfaceVariant`-Verifikation, Status-Badge-Tints, `BorderSubtle`-Rollen, Typo-Skala (`label-md`/`-sm`/`-meta`, `body-reading`) | Schritt 3; Tasks 3–8 (`LabelMdStyle`/`LabelSmStyle`/`LabelMetaStyle`, `MetaStyle` → `TextSecondary`, `FontAutoScalingEnabled="True"`); `body-reading` via WebView-Verifikation im Audit (`Styles.xaml`-Abschnitt) | Kontrast-Audit (dokumentiert) + Accessibility-Insights-FastPass + visueller E2E Light/Dark | Abgedeckt |
| Accessibility: `SemanticProperties` für alle icon-only/tap-basierten Elemente (Refresh-/Filter-/MarkAll-Buttons, `ArticleCardView`-Aktionen + Karten-Overlay, Feed-/Kategorie-/Suchtreffer-Karten, Sheet-Backdrop, Chips) | Schritte 2 (resx `AccessibilityDismissSheet`, `AccessibilityTapForActions`), 8–12; Tasks 2, 21, 23, 26, 28, 31; alle übrigen Labels über vorhandene Schlüssel (`ButtonRefresh`, `ButtonMarkAllRead`, `ArticleBookmarkSet`/`Remove`, `ArticleMarkAsRead`, `LabelCategoryName`, `PlaceholderFeedSearch` — im Repo verifiziert) | E2E: UIA-`Name`-Prüfung aller Elemente + FastPass + Narrator-Durchlauf (`manual-a11y-*`) | Abgedeckt |
| Kontraste ≥ Richtwerte (Fließtext ≥ 4,5:1, Icons/Dots ≥ 3:1 mit Text-Äquivalent) | Designentscheidung „Kontrast `TextMuted`/`MetaStyle`" (`MetaStyle` → `TextSecondary`, `Status*Text`-Tokens); Audit Schritt 14 | FastPass-Definition „kritisch = FastPass-Fehler (Kontrast < 4,5:1/< 3:1)" + Dark/Light-E2E | Abgedeckt |
| Screenreader-Bedienbarkeit / dynamische Schriftgrößen | Designentscheidung „Dynamische Schriftgrößen" (Default `true`, explizite Setter); Task 8; Audit Schritt 14 | Narrator-Durchlauf + `FontAutoScalingEnabled`-Audit (dokumentiert) | Abgedeckt |
| Performance: flüssige Infinity-Listen (`LaterViewModel`-Paging, `CollectionView`-Template-/Image-Caching-Prüfung) | Designentscheidung „LaterViewModel-Paging"; Schritte 5 + 7; Tasks 12, 14, 18–19, 30, 35 | Unit: `LoadMoreCommand_AppendsNextPage`, `…_WhenNoMoreItems_DoesNothing`, `GetSavedForLaterAsync_Paged_ReturnsPage`; E2E: Scroll-Nachladen mit > 20 Seed-Items (`manual-later-*`) | Abgedeckt |
| Performance: schnelle DB-Zugriffe (N+1 → In-Memory-Dedup + Batch-`AddRangeAsync` in `FeedSyncService.RunSyncAsync`) | Designentscheidung „Sync-Batch-Insert"; Programmablauf „Batch-Sync"; Schritte 5–6; Tasks 12–17 (`AddRangeAsync`, `GetByGuidOrHashAsync`-Entfernung) | Unit: `SyncFeedAsync_DuplicatesWithinSameDocument_InsertsOnce`, `…_MixedNewAndExisting_BatchInsertsOnlyNew`, `AddRangeAsync_InsertsAllItems`, `…_EmptyList_DoesNothing`; bestehender `SyncFeedAsync_Duplicates_SkipsExistingItems` bleibt | Abgedeckt |
| Performance: nicht blockierender Sync (`AutoRefreshService`/Sync ohne UI-Thread-Marshal) | Schritt 14; Task 35 (marshal-Freiheit verifizieren und dokumentieren — Ist-Zustand laut Bestandsaufnahme bereits `ConfigureAwait(false)`/`PeriodicTimer`) | Audit-Dokumentation + manuelle Sync-Performance-Beobachtung im E2E (kein hartes Budget — begründete Entscheidung) | Abgedeckt |
| Fehlerisolation im Sync beibehalten (`SyncFeedAsync`-Exception → `FeedHealth.Error` + `SyncLog`, Isolation pro Feed, lokalisierte `SyncStatusError`) | Schritt 6 („Fehlerisolation/Statuslogik unverändert"); Seiteneffekt „atomarer Insert-Lauf" dokumentiert | Bestehende `FeedSyncServiceTests` (Offline, InvalidXml, Unreachable, `SyncAllAsync`-Isolation) bleiben im Plan benannt | Abgedeckt |
| Release-Reife: Tests auf Windows und iOS | Schritt 16; Tasks 41–43 — Windows: `dotnet test` + `Run-StaticChecks.ps1` + manuelle Verifikation; iOS: dokumentierte Folgeaufgabe (macOS-Bedarfsbegründung, Präzedenz Issues #27/#28 — beantwortet die offene Frage der Anforderung) | `dotnet test --configuration Release`, `Run-StaticChecks.ps1` Exit 0, `test-results.md`-Protokoll; iOS-Vermerk in `test-results.md` | Abgedeckt |
| `IItemRepository`/`ItemRepository`: paged `GetSavedForLaterAsync`, `AddRangeAsync`; bestehende Optimierungen (`AsNoTracking`, `ExecuteUpdate/Delete`, `Skip`/`Take`) beibehalten | Abschnitt „Änderungen an bestehenden Klassen"; Schritt 5; Tasks 12–16; `GetByGuidOrHashAsync` + parameterloses `GetSavedForLaterAsync` entfallen | `ItemRepositoryTests`-Erweiterung/-Umstellung; `FailingItemRepository`-Anpassung explizit geplant (Task 40, Seiteneffekte) | Abgedeckt |
| `FeedHealth`-/`CategoryFilterItem`-Konstanten bleiben, Badge-/Chip-Mapping | Designentscheidungen; `DataTrigger` auf `FeedHealth`-Literale und `IsSelected` unverändert | Bestehende ViewModel-Tests + visueller E2E | Abgedeckt |
| Manuelle UI-Verifikation gemäß AGENTS.md (390 × 844 pt, Light + Dark, Screenshots in `test-results.md` + `docs/help/anwendung/mobile-ui-design.md`, Regressions-Checklisten vorangegangener Issues) | Schritt 16; Task 41; E2E-Abschnitt inkl. UIA-Vorgehen (`test-results/issue-59/uia.ps1`-Muster — im Repo vorhanden) und Regressionshinweis | Verbindlicher Nachweisplan inkl. FastPass/Narrator und Ablageort `test-results/issue-30/` | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine — Status: Plan vollständig.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Kategoriefilter über Chip-Leiste (Tap → Filterung, Aktiv-Zustand, horizontales Overflow-Scrolling) | Manuell: `manual-unread-*` (Light + Dark), UIA-Interaktion | Abgedeckt |
| Floating Reader Control Bar (Pill-Geometrie, Transluzenz, Schatten, Safe-Area, 6 Aktionen inkl. `BookmarkGold`-Toggle) | Manuell: `manual-detail-*` (Light + Dark) | Abgedeckt |
| Feed-Health-Badges (`OK`/`Warning`/`Error`), Karten-Tap → ActionSheet, Backdrop-Tap schließt Sheet | Manuell: `manual-feeds-*`, `HealthStatus` per SQLite-Seed variiert | Abgedeckt |
| Später-Liste Infinity-Paging (> 20 Items, Scroll-Schwelle, in-place Entfernen) | Manuell: `manual-later-*` mit Seed-Daten | Abgedeckt |
| Screenreader/Accessibility (alle icon-only/tap-Elemente, „keine kritischen Fehler") | UIA-`Name`-Inspektion + Accessibility Insights FastPass + Narrator (`manual-a11y-*`) | Abgedeckt |
| Kontrast/Dark-Mode über alle 5 Tabs + Detailseite | Manuell: `manual-dark-*`/`manual-light-*` gegen `screen.png`-Referenzen | Abgedeckt |
| Programmsymbol/Tab-Icons/Splash | Manuell: `manual-icon-*` inkl. Screenshot-Vergleich mit Referenz-PNG | Abgedeckt |
| Nicht blockierender Sync / Scroll-Performance | Manuelle Beobachtung (Pull-to-Refresh ohne Freeze), optional Sync-Dauer vorher/nachher | Abgedeckt |
| Automatisierte UI-Tests | Nicht erforderlich — begründet: Repo hat keine UI-Testsuite; AGENTS.md definiert dokumentierte manuelle Verifikation (390 × 844 pt, Screenshots) als Nachweisweg; Plan benennt Setup (Fenstergröße via `GetWindowRect`, UIA/`mouse_event`, Theme-Wechsel via SQLite), auslösende Aktion und sichtbares Ergebnis je Szenario | Nicht erforderlich mit Begründung |
| iOS-Verifikation | Als dokumentierte Folgeaufgabe in `test-results.md` — begründet (macOS-Bedarf, Präzedenz #27/#28, offene Frage der Anforderung verbindlich entschieden) | Nicht erforderlich mit Begründung (s. Hinweise) |

## Fehlende oder unvollständige Planbestandteile

Keine — Status: Plan vollständig.

## Hinweise

- **iOS-Nachweis:** Die Anforderung nennt „Tests auf iOS und Windows" als Release-Nachweis, stellt die iOS-Durchführung aber selbst als offene Frage. Der Plan entscheidet sie verbindlich als dokumentierte Folgeaufgabe (Präzedenz #27/#28). Falls der Issue die iOS-Abnahme verbindlich einfordert, wäre das nachzuplanen.
- **Robustheit (Implementierungsansatz Schritt 6 der Anforderung):** „Restliche unbehandelte Pfade (Feed-Suche, Direkt-Add, WebView) schließen" hat keinen eigenen Umsetzungsschritt; die vorhandene Fehlerisolation ist laut Bestandsaufnahme bereits implementiert und durch `FeedsViewModelTests`/`FeedSyncServiceTests` abgedeckt. Empfehlung: die Verifikation in Schritt 14/16 explizit dokumentieren.
- **Chip-Accessibility:** `SemanticProperties.Description` der Chips bindet nur `Name`; die Count-Kapsel wird dem Screenreader ggf. nicht angesagt — Detail für die Umsetzung (z. B. Description inkl. `Count`).
- **Shape-Detailwerte:** Entwurfs-Konstanten für Thumbnails (10) und Favicons (6) sind im Plan nicht einzeln benannt; über die Abweichungsliste in Schritt 1 sicherstellen.
- Stichproben-Verifikation bestanden: alle im Plan referenzierten resx-Schlüssel, `Resources/Splash/splash.svg`, `test-results/issue-59/uia.ps1` sowie sämtliche `design-draft`-Referenzen existieren im Repo.
