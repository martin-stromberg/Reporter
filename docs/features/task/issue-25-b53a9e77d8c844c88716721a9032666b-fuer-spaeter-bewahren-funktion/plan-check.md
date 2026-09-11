# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Artikel können als „Später" markiert werden (Toggle `IsSavedForLater` über Bookmark-Icon in `ArticleCardView` auf `UnreadPage`) | Programmablauf „Bewahrung umschalten (Liste)" — vorhanden, zu verifizieren (`ArticleCardView` 44 × 44 pt `Border`, `ToggleSavedCommand`, `ToggleSavedForLaterAsync`) | `UnreadViewModelTests.ToggleSavedCommand_TogglesFlagInPlace`, `ItemRepositoryTests.ToggleSavedForLaterAsync_TogglesState` (bestehend) + `_TogglesBackToFalse` (neu); manuell: Pflicht-Szenario 1 | Abgedeckt |
| Bewahrung in der Detailansicht umschaltbar (`ArticleDetailPage` Bottom-Action-Bar) | Programmablauf „Bewahrung umschalten (Detailansicht)" — vorhanden, zu verifizieren (`ToggleSavedForLaterCommand`, `BookmarkButtonLabel`, Gold-Fill-`DataTrigger`) | Kein Unit-Test möglich (`ArticleDetailViewModel` liegt in der MAUI-Assembly, von `Reporter.Tests` nicht referenzierbar — dokumentiert); manuell: Pflicht-Szenario 3; Repository-Ebene über `ToggleSavedForLaterAsync`-Tests | Abgedeckt |
| Eigene Tab-Seite „Später" listet ausschließlich bewahrte Artikel | Programmablauf „Später-Ansicht anzeigen" — `AppShell`-Tab vorhanden, `LoadCommand` in `OnAppearing`, `GetSavedForLaterAsync` filtert `IsSavedForLater` | `LaterViewModelTests.LoadCommand_PopulatesOnlySavedItems`, `ItemRepositoryTests.GetSavedForLaterAsync_ReturnsOnlySaved` (bestehend); manuell: Pflicht-Szenario 1 (Artikel erscheint im Tab) | Abgedeckt |
| Später-Liste absteigend sortiert (Speicherdatum **oder** Veröffentlichungsdatum) | Designentscheidung: bestehende `OrderByDescending(PublishedAt)` erfüllt die Oder-Variante; kein `SavedAt`-Feld, keine Migration | `LaterViewModelTests.LoadCommand_OrdersByPublishedAtDescending`, `ItemRepositoryTests.GetSavedForLaterAsync_OrdersByPublishedAtDescending` | Abgedeckt |
| Entfernen der Bewahrung aus der Später-Ansicht | `LaterViewModel.ToggleSavedCommand` → `ToggleSavedForLaterAsync` + Reload, Eintrag verschwindet; `EmptyView` bei leerer Liste | `LaterViewModelTests.ToggleSavedCommand_RemovesItemFromSavedItems`, `ToggleSavedCommand_NullItem_DoesNothing`; manuell: Pflicht-Szenario 2 inkl. `EmptyView` | Abgedeckt |
| Bewahrte Artikel sind von jeder automatischen Löschung ausgenommen (Retention-Cleanup auf `Settings.RetentionDays`) | Neu: `IItemRepository.DeleteExpiredAsync(cutoff)` mit strukturellem Filter `IsRead && !IsSavedForLater && (ReadAt ?? PublishedAt) < cutoff`; Orchestrierung via `RetentionCleanupService` (liest Settings, `RetentionDays <= 0` → Skip); Aufruf fehlerisoliert in `App.OnStart` nach `MigrateAsync`; DI-Registrierung in `MauiProgram` | `ItemRepositoryTests.DeleteExpiredAsync_*` (5 Tests, inkl. Kerninvariante `KeepsSavedForLaterItems`, NULL-Zeitstempel-Edge-Case implizit über `UsesReadAtOverPublishedAt`/Validierungstabelle), `RetentionCleanupServiceTests.CleanupAsync_*` (3 Tests inkl. `<= 0`-Skip und konfiguriertem Wert) | Abgedeckt |
| MarkRead-Funktionalität auf `LaterPage` bleibt konsistent (Nebenbedingung aus `LaterViewModel`) | Vorhanden, verifiziert | `LaterViewModelTests.MarkReadCommand_SetsReadAndKeepsItemInList` | Abgedeckt |
| UI-Konformität `LaterPage` gegen Design-Draft + AGENTS.md Mobile-UI-Regeln (44 × 44 pt, `AppThemeBinding`, `CollectionView` füllt `Grid`-Row `*`, kein verschachteltes Scrollen) | Umsetzungsschritt 5: Vergleich gegen `f_r_sp_ter_bewahren/screen.png` + `..._dark_mode/screen.png` im 390 × 844 pt Windows-Fenster; Abweichungen als Fix-Commits | Manuell: Pflicht-Szenario 4 (Light + Dark), Screenshots + Fenstergrößen in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` | Abgedeckt |
| Nicht-Anforderung: kein `SavedAt`-Feld / keine Migration | Explizit entschieden und begründet; Abschnitt „Datenbankmigrationen: Keine" | Entfällt (Nicht-Anforderung) | Abgedeckt |
| Nicht-Anforderung: keine neue Konfiguration; `IsSavedForLater`-Ausnahme nicht abschaltbar | Abschnitt „Konfigurationsänderungen: Keine"; `RetentionDays`-Default 30 wird ausgewertet | `RetentionCleanupServiceTests.CleanupAsync_RespectsConfiguredRetentionDays` (konfigurierter Wert statt Default), `CleanupAsync_ZeroOrNegativeRetentionDays_Skips` | Abgedeckt |
| Geklärte Frage 3: Feed-Löschkaskade (`DeleteBehavior.Cascade`) darf bewahrte Artikel mitlöschen (bestätigte Nutzeraktion ≠ automatische Löschung) | Designentscheidung + Risiko dokumentiert; Verhalten unverändert | `FeedRepositoryTests.DeleteAsync_CascadeDeletesSavedItems` (dokumentiert das Verhalten) | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine.

## E2E-Abdeckung

Der Plan enthält eine nachvollziehbare Begründung für das Fehlen automatisierter E2E-Tests: Es existiert keine E2E-/UI-Testinfrastruktur (kein Appium-/UITest-Projekt, keine `AutomationId`s, die MAUI-Assembly `Reporter` ist vom Testprojekt `Reporter.Tests` nicht referenzierbar), und `AGENTS.md` Regel 6 („Mobile UI Design Review") definiert für dieses Projekt dokumentierte manuelle UI-Verifikation (Screenshot + Notiz der getesteten Größen in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md`) ausdrücklich als zulässigen Ersatz für automatisierte UI-Tests. Der Plan setzt diesen Ersatz konkret um (Pflicht-Szenarien-Tabelle mit Nachweisort und Fenstergröße 390 × 844 pt). Kein Benutzerfluss bleibt ohne jede ausführbare Prüfung.

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Artikel auf `UnreadPage` per Bookmark-Icon bewahren → Icon-Färbung → Artikel erscheint im Tab „Später" | Kein automatisierter E2E-Test möglich (Begründung oben); manuelles Pflicht-Szenario 1 mit Screenshot; zusätzlich Unit-Tests `ToggleSavedCommand_TogglesFlagInPlace`, `LoadCommand_PopulatesOnlySavedItems` | Abgedeckt (manuelle Verifikation gemäß AGENTS.md-Regel 6 + Unit-Tests) |
| Bewahrung auf `LaterPage` entfernen → Artikel verschwindet; `EmptyView` bei leerer Liste | Manuelles Pflicht-Szenario 2 mit Screenshot; Unit-Test `ToggleSavedCommand_RemovesItemFromSavedItems` | Abgedeckt (manuelle Verifikation + Unit-Test) |
| Bookmark-Aktion in `ArticleDetailPage`-Bottom-Bar → Label/Icon-Wechsel („Lesezeichen setzen"/„entfernen") | Manuelles Pflicht-Szenario 3 mit Screenshot; `ArticleDetailViewModel` strukturell nicht unit-testbar (MAUI-Assembly) | Abgedeckt (manuelle Verifikation; einzig möglicher Nachweis oberhalb der Repository-Ebene) |
| Design-Draft-Vergleich `LaterPage` Light + Dark (390 × 844 pt), AGENTS.md-Prüfregeln | Manuelles Pflicht-Szenario 4, Screenshots beider Themes | Abgedeckt (AGENTS.md-Pflicht erfüllt) |
| Automatischer Retention-Cleanup beim App-Start; bewahrte Artikel bleiben erhalten | Kein E2E-Szenario — nicht über die UI auslösbar: `RetentionDays` ist in keiner UI konfigurierbar (`SettingsPage` ist Platzhalter), `ReadAt`/`PublishedAt` sind nicht nutzerseitig setzbar; ein sichtbarer Effekt ließe sich nur durch DB-Manipulation außerhalb der App herstellen. Nachweis über Integrationstests `DeleteExpiredAsync_*` und `CleanupAsync_*` gegen echte SQLite-DB | Nicht erforderlich mit Begründung (Logik vollständig über Integrationstests nachgewiesen) |
| Fehlerisolierung des Cleanups in `App.OnStart` (`async void`, darf Start nicht blockieren) | Kein Test möglich (`App` in MAUI-Assembly); als Risiko mit Maßnahme (try/catch) benannt | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

Keine.

## Hinweise

- Die vier „geklärten Entscheidungen" im Plan (Retention-Cleanup wird in diesem Arbeitspaket neu eingeführt; Sortierung bleibt `PublishedAt` ohne `SavedAt`; Feed-Kaskade löscht bewahrte Artikel mit; Frist gilt nur für gelesene Artikel mit Stichtag `ReadAt ?? PublishedAt`) beantworten alle offenen Fragen aus `requirement.md` konsistent. Sie sind Annahmen — in der Nachplanung/Umsetzung sollte ihre Freigabe dokumentiert werden, da sie fachliches Verhalten festlegen (insb. „nur gelesene Artikel" und der mit ihnen verbundene gewollte Datenverlust ab Fristablauf).
- `RetentionCleanupServiceTests` nennt das Setup nur implizit („Settings mit `RetentionDays`, Seeds"). Bei der Umsetzung sollte das etablierte Muster beibehalten werden: echtes `SettingsRepository` auf `TestDbContextFactory`, `SaveAsync` zum Setzen von `RetentionDays`, Feed-/Item-Seeding analog `SeedFeedAsync`.
- Ein manueller Sicht-Check des Cleanup-Effekts (z. B. „bewahrter Artikel bleibt nach Neustart in der Später-Liste") wäre nur mit künstlich gealterten DB-Zeitstempeln möglich und ist daher zurecht nicht Teil der Pflicht-Szenarien; die Integrationstests tragen den Nachweis.
- Die Pflicht-Szenarien laufen alle über die gestartete App — damit ist der fehlerisolierte `OnStart`-Aufruf implizit mit verifiziert (ein Start-Blocker würde jede manuelle Verifikation verhindern).
