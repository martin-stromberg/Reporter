<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

Issue #30 — UI-Polish, Accessibility und Design-System-Vollständigkeit.
Lauf 1: 2026-09-13, Windows 11, unpackaged `win-x64`-Release-Build, Fenster 390 × 844 pt (per `GetWindowRect` verifiziert), deutsch lokalisierte UI.
Lauf 2 (Iteration 2): 2026-09-13, gleiche Umgebung, nach den Review-/UIA-Fixes (`LaterViewModel`-Lock, gemeinsame `ItemRepository`-Projektion, `ItemListItem.CopyWith`, transparente `Button`-Controls in der Floating Bar, Chip-`MinimumHeightRequest`, neue Accessibility-resx-Schlüssel, `FeedSearchResult.DisplayTitle`).
Lauf 3 (Iteration 3): 2026-09-13, gleiche Umgebung, nach den Review-Fixes der zweiten Runde (`LaterViewModel` `ErrorMessage`/`HasError` + try/catch, `LaterPage` Fehler-Label + `OnAppearing`-Absicherung, `DisplayTitle`-Binding der Treffer-Überschrift, `AccessibilityCategoryFilterSingular` EN/DE, `UnreadCount`-Dekrement in `UnreadViewModel.MarkReadAsync`, Doku-Eintrag in `mobile-ui-design.md`). Kein erneuter manueller E2E-Lauf — die E2E-Nachweise aus Iteration 2 stehen.
Lauf 4 (Fortsetzungslauf `continue.md`, dieser Lauf): 2026-09-13, gleiche Umgebung, nach den `continue.md`-Nacharbeiten (neue Basisklasse `DelegatingItemRepository` für die drei delegierenden Test-Fakes; neuer resx-Schlüssel `ConfirmDeleteCategoryTitle` EN/DE + Verwendung im `CategoriesPage`-Lösch-Dialog). Kein erneuter manueller E2E-Lauf — die Änderungen betreffen Testinfrastruktur und eine Dialogbeschriftung; die E2E-Nachweise aus Iteration 2 stehen.

## Ergebnis

**Status:** Fehler vorhanden

Begründung (Lauf 4): Alle 350 Unit-Tests bestehen, `dotnet build Reporter.sln` (Debug) läuft mit 0 Fehlern (1 pre-existing CS8765-Warnung in `Platforms/iOS/AppDelegate.cs`), `Run-StaticChecks.ps1` läuft mit Exit-Code 0 durch. Beide lösbbaren `continue.md`-Punkte sind umgesetzt (`DelegatingItemRepository`-Basisklasse, `ConfirmDeleteCategoryTitle` im Kategorie-Lösch-Dialog). Ein erneuter manueller E2E-Lauf war nicht nötig — die E2E-Nachweise aus Iteration 2 (Screenshots, UIA-Fix) stehen. Der Status bleibt „Fehler vorhanden", weil geplante Pflicht-E2E-Nachweise in dieser Umgebung weiterhin nicht ausführbar sind: Accessibility-Insights-FastPass (Tool nicht installiert), Narrator-Durchlauf (interaktiv), iOS-Verifikation (macOS erforderlich), Splash-Laufzeitnachweis und `DisplayTitle`-Laufzeitnachweis.

## Fehlgeschlagene Tests

### Unit-Tests

Keine — 350 von 350 bestanden (2 neue Tests in Iteration 3: `LaterViewModelTests.LoadCommand_WhenRepositoryFails_SetsLocalizedErrorMessage`, `UnreadViewModelTests.MarkReadCommand_DecrementsUnreadCount`; Lauf 4 ohne neue Tests — nur Refactoring der Test-Fakes auf `DelegatingItemRepository`).

### Statische Checks

Keine — `.\scripts\Run-StaticChecks.ps1` vollständig mit Exit-Code 0 (Format, Lizenzheader, Security, Static-Analysis-Release-Build ohne Befund; erneut in Iteration 3 und in Lauf 4 ausgeführt). Ein Iteration-2-Artefakt (`test-results/issue-30/iter2-uia-detail.txt`) erhielt nachträglich den Lizenzheader über `node scripts/add-license-headers.mjs`.

### Manuelle Verifikation

Keine — der Iteration-1-Befund „Floating-Bar-Aktionen ohne UIA-Exposure" ist behoben (s. Iteration-2-Tabelle unten). Iteration 3 enthielt nur ViewModel-/XAML-/resx-/Doku-Änderungen ohne neuen E2E-Lauf. Verbleibende Lücken sind unter „Ausstehende Verifikationen" gelistet.

## Ausstehende Verifikationen

- **Accessibility Insights FastPass** — nicht ausführbar: „Accessibility Insights for Windows" ist auf diesem Arbeitsplatz nicht installiert (Installationspfade geprüft). Der verbindliche FastPass-Nachweis („keine kritischen Fehler") steht aus; die UIA-`Name`-Vollständigkeit ist inzwischen manuell über alle Seiten verifiziert, eine formale Kontrastmessung (4,5:1/3:1) bleibt dem FastPass vorbehalten.
- **Narrator-Durchlauf** — nicht ausgeführt (interaktiver Screenreader-Durchlauf über Ungelesen + Artikeldetail; Sprachausgabe in dieser Umgebung nicht verifizierbar).
- **iOS-Verifikation** — ausstehend (erfordert macOS; dokumentierte Folgeaufgabe wie bei Issues #27/#28).
- **Splash-Screen** — konfiguriert (`MauiSplashScreen Color="#1e293b"`, neues `splash.svg` mit Logo-Motiv), zur Laufzeit zu kurz sichtbar für einen Screenshot-Nachweis.
- **`FeedSearchResult.DisplayTitle` zur Laufzeit** — die Feed-Suche lieferte in dieser Umgebung keine Treffer (feedsearch.dev ohne Ergebnis), daher nur code-seitig verifiziert (`FeedsPage.xaml` bindet `SemanticProperties.Description="{Binding DisplayTitle}"` und die Treffer-Überschrift `Text="{Binding DisplayTitle}"`; Fallback `Title ?? FeedUrl` in `FeedSearchResult.cs:48`).

## E2E-Abdeckung

Alle geplanten Pflicht-Szenarien (manuell, 390 × 844 pt, UIA + `mouse_event` nach Muster `test-results/issue-59/uia.ps1`, Theme-Wechsel über `settings.theme` in der SQLite-DB `…\AppData\Local\User Name\com.companyname.reporter\Data\reporter.db` mit Neustart). Screenshots unter `test-results/issue-30/` (Iteration 1: `manual-*`, Iteration 2: `iter2-*`).

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Ungelesen: Chip-Leiste (Tap → Filterung, Aktiv-Zustand, horizontales Scrollen) | Iteration 1: `manual-unread-01..05`; Iteration 2: `iter2-unread-01-initial.png`, `iter2-unread-02-light.png` | Bestanden — Chips mit Name + Count-Kapsel und lokalisierten Accessibility-Descriptions („Filter Alle, 476 ungelesene Artikel, ausgewählt" als ListItem/Group, 44 pt hoch); Tap „Sport" filterte 228 → 55 Artikel (It. 1); horizontales Scrollen verifiziert; Dark + Light |
| Artikeldetail: Floating Reader Control Bar | Iteration 1: `manual-detail-01..02`; Iteration 2: `iter2-detail-01-dark.png`, `iter2-detail-02-light.png`, `iter2-uia-detail.txt` | Bestanden — Pill-Bar gerendert (transluzent, ~52 pt); **UIA-Befund behoben**: alle 6 Aktionen als benannte `Button` im UIA-Baum (Zurück, Lesezeichen setzen/entfernen, Schriftgröße wechseln, Bereits gelesen/Als gelesen markieren, Teilen, Im Browser öffnen) plus Footer-`Button`; `Invoke` auf „Lesezeichen setzen" toggelte zu „Lesezeichen entfernen" (Name aktualisiert sich dynamisch); Dark + Light |
| Feeds: Micro-Pill-Badges + Karten-Tap/Backdrop | Iteration 1: `manual-feeds-01..03`; Iteration 2: `iter2-feeds-01-badges-all-states-dark.png`, `iter2-feeds-02-light.png`, `iter2-feeds-03-search-light.png` | Bestanden — alle drei Zustände verifiziert: `In Ordnung`/`Warnung`/`Fehler` (WDR wurde durch echten Sync-Lauf auf `Warnung` gesetzt); Pixel-verifizierte Token-Werte, Dark: Tint-Bg `#2F2F30`/`#18323C`/`#2F2539`, Text `#F59E0B`/`#10B981`/`#F43F5E`; Light: Tint-Bg `#FEF5E6`/`#E7F8F2`, Text `#B45309`/`#047857`; Karten-Tap → ActionSheet, Backdrop schließt Sheet (It. 1) |
| Später: Infinity-Paging + in-place Entfernen | Iteration 1: `manual-later-01..03`; Iteration 2: `iter2-later-01-bottom-dark.png`, `iter2-later-02-light.png`, SQLite-Seed 25 Items | Bestanden — 25 geseedete Items, Scroll ans Ende lud Seite 2 nach (Item #25 „DSGVO vs. Smart Glasses…" sichtbar); „Lesezeichen entfernen" entfernte Karte in-place (25 → 24 in DB, kein Vollreload) — nach `SemaphoreSlim`-Refactoring erneut verifiziert |
| Accessibility: UIA-`Name` aller icon-only/Tap-Elemente + FastPass + Narrator | UIA-Inspektionen `uia-detail-fixed.txt`, `iter2-uia-detail.txt`; FastPass/Narrator | Teilweise bestanden — alle interaktiven Elemente auf allen Seiten benannt (inkl. Detail-Bar-Buttons nach Fix; Einstellungen: Slider/Switches/Buttons benannt; Kategorien: Karten als benannte Groups, `Name`-Edit); FastPass (Tool nicht installiert) und Narrator nicht ausgeführt |
| Kontrast/Dark-Mode über alle 5 Tabs + Detail | `manual-*` (It. 1), `iter2-*-light.png`, `iter2-*-dark.png` | Bestanden (visuell) — beide Themes für alle Seiten; Pixel-Stichproben Light `#F7F9FB`-Fläche, Dark `#0B0F17`/Karten `#182234`; formale Kontrastmessung bleibt FastPass vorbehalten |
| Programmsymbol + Tab-Icons | EXE-Icon-Extraktion `iter2-exe-icon.png`, Referenz-Vergleich `…_13_the/screen.png`, UIA `Image`-Elemente | Bestanden — Referenz-Motiv (dunkles Slate-Tile `#203040`-Bucket + helles Glyph + Amber-Akzent `#D09040`) deckt sich mit der Umsetzung (`#1e293b`-Hintergrund, weißes Newspaper-Glyph, `#f59e0b`-Dot in `appiconfg.svg`); EXE-Icon trägt die Marke (32×32: Slate-Bg + helles Glyph); Tab-Icons als 16×16-`Image` sichtbar; Splash nicht zur Laufzeit beobachtbar |
| Sync-Performance / nicht blockierender Sync | Iteration 2: „Aktualisieren"-Klick über 7 Feeds (5 real + 2 fehlschlagend), UIA-Scans während des Syncs | Bestanden — Sync lief ~4 s, UI blieb reaktiv (UIA-Full-Scans konstant 35–91 ms, keine Blockade); Fehlerisolation sichtbar („Synchronisation fehlgeschlagen" für example.com-Feeds, Zähler 475 → 476 durch erfolgreiche Teilfeeds); Batch-Pfad zusätzlich durch Unit-Tests abgedeckt |
| Ungelesen-Dot (6 pt, `Secondary`) auf `ArticleCardView` | Pixel-Check `manual-unread-01` | Bestanden — `#4EDEA3` (`DarkSecondary`) an der Dot-Position in der Karten-Kopfzeile |
| Lesezeit auf `ArticleCardView` | UIA-Text | Bestanden — „1 Min. Lesezeit" auf allen Karten sichtbar (auch Iteration 2) |
| Feed-Suche: `DisplayTitle` in Suchtreffer-Karten | Code-Review + Laufzeitversuch `iter2-feeds-03-search-light.png` | Teilweise — Bindung auf `DisplayTitle` (Titel, Fallback FeedUrl) code-seitig verifiziert; Laufzeit-Nachweis nicht möglich (feedsearch.dev lieferte keine Treffer in dieser Umgebung) |

## Zusammenfassung

- Gesamt (Unit): 350
- Bestanden: 350
- Fehlgeschlagen: 0
- Übersprungen: 0

Manuelle E2E-Szenarien: 8 Pflicht-Szenarien + 2 Einzelnachweise — Stand Iteration 3 unverändert zu Iteration 2 (kein erneuter E2E-Lauf): 8 bestanden, 1 teilweise (Accessibility: UIA-`Name` vollständig, FastPass/Narrator ausstehend), Feed-Suche-`DisplayTitle` nur code-seitig. Ausstehend: FastPass, Narrator, iOS, Splash-Laufzeitnachweis, `DisplayTitle`-Laufzeitnachweis. Der `mobile-ui-design.md`-Eintrag ist seit Iteration 3 vorhanden (Abschnitt „UI-Polish & Accessibility (issue-30)").

Ausgeführte Läufe:

| Lauf | Befehl | Ergebnis |
|------|--------|----------|
| Build (Debug, Solution, Lauf 4) | `dotnet build Reporter.sln` | Erfolgreich, 1 Warnung (pre-existing CS8765 in `Platforms/iOS/AppDelegate.cs`), 0 Fehler |
| Tests (Lauf 4) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --no-build --logger "console;verbosity=normal"` | 350 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Static Checks (Lauf 4) | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Lizenzheader, Security, Static-Analysis-Release-Build 0 Warnungen/0 Fehler) |
| Build (Release, Solution, It. 3) | `dotnet build Reporter.sln --configuration Release` | Erfolgreich, 1 Warnung (pre-existing CS8765 in `Platforms/iOS/AppDelegate.cs`), 0 Fehler |
| Tests mit Coverage (It. 3) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release --collect:"XPlat Code Coverage" --settings src/Reporter.Tests/coverlet.runsettings` | 350 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Static Checks (It. 3) | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Lizenzheader, Security, Static-Analysis-Release-Build) |
| Build (Release, Solution, It. 2) | `dotnet build Reporter.sln --configuration Release` | Erfolgreich, 1 Warnung (pre-existing CS8765 in `Platforms/iOS/AppDelegate.cs`), 0 Fehler |
| Tests mit Coverage (It. 2) | `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release --collect:"XPlat Code Coverage" --settings src/Reporter.Tests/coverlet.runsettings` | 348 bestanden, 0 fehlgeschlagen, 0 übersprungen |
| Static Checks (It. 2) | `.\scripts\Run-StaticChecks.ps1` | Exit-Code 0 (Format, Lizenzheader, Security, Static-Analysis-Release-Build) |
| Manuelle UI-Verifikation (It. 2) | App `win-x64` Release, 390 × 844 pt, UIA + Screenshots, SQLite-Seeds | Durchgeführt, s. E2E-Abdeckung |

## Testabdeckung

**Abdeckung:** 90,8 % Zeilenabdeckung gesamt (Cobertura, `src/Reporter.Tests/TestResults/f33e3652-1498-4030-937a-76996c7c577c/coverage.cobertura.xml`)

| Datei | Abdeckung |
|-------|-----------|
| `Reporter.Core\Services\FeedSearchUnavailableException.cs` | 33,3 % |
| `Reporter.Core\Models\CategoryFilterItem.cs` | 25,0 % |

`AppResources.Designer.cs` (23,4 %) ist generiert und damit ausgenommen.

## Fehlende Tests

Quelle: `Coverage-Daten`

- `src/Reporter.Core/Services/FeedSearchUnavailableException.cs` — 33,3 % Zeilenabdeckung (unter 80 %)
- `src/Reporter.Core/Models/CategoryFilterItem.cs` — 25,0 % Zeilenabdeckung (unter 80 %; v. a. `AccessibilityDescription`-Singular-/Plural-Pfade)

## Iteration 1 — Detailprotokoll (2026-09-13)

Erster Lauf vor den Review-Fixes: 347 Unit-Tests grün, Static Checks zunächst mit Lizenzheader-Befund (`review-usability.md`, danach behoben). Manuelle Verifikation deckte den UIA-Befund auf der Artikeldetailseite auf (6 Aktions-`Border` nicht im UIA-Baum — MAUI/WinUI-Einschränkung bei Elementen hinter `WebView`), die Badge-Zustände nur `OK`/`Error`, Paging/Icons/Tab-Leiste positiv. Vollständiges Protokoll in der E2E-Tabelle oben (Zeilen mit „Iteration 1") sowie den `manual-*`-Screenshots und `uia-detail-dark.txt` unter `test-results/issue-30/`.

## Iteration 2 — Review-Fixes (2026-09-13)

Nacharbeiten aus `review-code.md` und `review-usability.md` sowie dem UIA-Befund aus Iteration 1.

| Befund | Fix | Verifikation |
|--------|-----|--------------|
| `LaterViewModel.LoadAsync` setzt Paging-State zurück, während `LoadMoreAsync` noch läuft (stale Seite + falscher `_currentPage`) | `SemaphoreSlim _loadLock` serialisiert `LoadAsync`/`LoadMoreAsync`; `LoadAsync` wartet den laufenden Seitenabruf ab, setzt dann zurück und lädt Seite 0 über `LoadPageCoreAsync` | Regressionstest `LaterViewModelTests.LoadCommand_WhenLoadMoreInFlight_ReloadsFirstPage` grün; Paging zusätzlich manuell re-verifiziert (25 Items, Seite 2 geladen) |
| Duplizierte Projektion in `ItemRepository.GetUnreadByDateAsync`/`GetSavedForLaterAsync` | Gemeinsame Helfer `SelectListItemRows` (IQueryable-Projektion in `ItemListRow`) + `MapToListItem` | Unit-Tests grün |
| Duplizierte 14-Property-Kopien in `LaterViewModel.MarkReadAsync`/`UnreadViewModel.ToggleSavedAsync` | `ItemListItem.CopyWith(isRead:, isSavedForLater:)` | Unit-Tests grün |
| Chip-`CollectionView` mit festem `HeightRequest="44"` clippt bei Font-Skalierung | `MinimumHeightRequest="44"` | Build; Layout bei 390 × 844 pt verifiziert (`iter2-unread-*`) |
| Chip-Accessibility nur `{Binding Name}` | `CategoryFilterItem.AccessibilityDescription` (lokalisiert: „Filter {0}, {1} ungelesene Artikel, {2}" + ausgewählt/nicht ausgewählt; `NotifyPropertyChangedFor` auf Count/IsSelected) | UIA `list` zeigt „Filter Alle, 476 ungelesene Artikel, ausgewählt" usw. als ListItem/Group (It. 2) |
| Artikel-Karten-Overlay: `AccessibilityTapForActions` („Tippen für Aktionen"), tappt aber zum Detail | Neuer String `AccessibilityOpenArticle` („Tippen, um den Artikel zu öffnen" / „Double-tap to open the article") als `SemanticProperties.Hint` | Build; UIA-`Name` der Overlay-Groups = Titel (It. 2) |
| Feed-Suchtreffer: `Description="{Binding FeedUrl}"` (technische URL) | `FeedSearchResult.DisplayTitle` (Titel, Fallback FeedUrl) | Code-seitig verifiziert; Laufzeit-Nachweis ohne Suchtreffer nicht möglich (s. Ausstehende Verifikationen) |
| Floating-Bar-Aktionen nicht im Windows-UIA-Steuerbaum | Footer + Bar in der XAML-Dokumentreihenfolge VOR das `WebView` gezogen (Grid.Row unverändert) und die 6 Gesten-`Border` durch echte `Button`-Controls (transparent, über den unveränderten `Path`/`Label`-Icons, 44 × 44) ersetzt | It. 1: `uia-detail-fixed.txt` + `uia-fix-detail-bar.png`; It. 2 erneut: `iter2-uia-detail.txt` — alle 6 Aktionen als `Button` mit Namen (Dark + Light), `Invoke` „Lesezeichen setzen" → „Lesezeichen entfernen" funktional |

## Iteration 3 — Review-Fixes (2026-09-13)

Nacharbeiten aus `review-code.2.md`/`review-usability.2.md` (zweite Review-Runde). Kein erneuter manueller E2E-Lauf — die Nachweise aus Iteration 2 stehen; die Änderungen betreffen ViewModel-Logik, ein Label-Binding, resx-Strings und Doku.

| Befund | Fix | Verifikation |
|--------|-----|--------------|
| `LaterViewModel` ohne Fehlerbehandlung — Repository-Fehler ließen die Seite still leer | `ErrorMessage`/`HasError` ergänzt; `LoadPageCoreAsync` mit try/catch (`Debug.WriteLine` + lokalisiertes `AppResources.ErrorLoadFailed`, `HasMore = false`), `LoadAsync` setzt `ErrorMessage` zurück | Neuer Test `LaterViewModelTests.LoadCommand_WhenRepositoryFails_SetsLocalizedErrorMessage` grün (`HasError`, `ErrorLoadFailed`, leere Liste, `HasMore = false`) |
| `LaterPage` zeigte Ladefehler nicht an; `OnAppearing` ungeschützt | Fehler-`Label` (`Text="{Binding ErrorMessage}"`, `IsVisible="{Binding HasError}"`) im Header; `OnAppearing` mit try/catch um `LoadCommand.ExecuteAsync` (`Debug.WriteLine`) | Code-seitig verifiziert (`LaterPage.xaml:33-35`, `LaterPage.xaml.cs:24-36`); Build + Tests grün |
| Doppelte Fallback-Logik für Suchtreffer-Titel (`DisplayTitle` vs. `Label` + `DataTrigger`) | Treffer-Überschrift bindet direkt `Text="{Binding DisplayTitle}"`, `DataTrigger` entfernt | Code-seitig verifiziert (`FeedsPage.xaml:83`); Laufzeit-Nachweis weiterhin ausstehend (s. Ausstehende Verifikationen) |
| Chip-Accessibility ohne Singular-Form („1 ungelesene Artikel") | Neuer resx-Schlüssel `AccessibilityCategoryFilterSingular` (EN + DE); `CategoryFilterItem.AccessibilityDescription` wählt bei `Count == 1` die Singular-Form | Code-seitig verifiziert (`CategoryFilterItem.cs:44`, `AppResources.resx`/`.de.resx`); Unit-Tests grün |
| `UnreadViewModel.MarkReadAsync` ließ Header-Zähler `UnreadCount`/`UnreadCountText` stale | `UnreadCount = Math.Max(0, UnreadCount - 1)` + `UpdateUnreadCountText()` nach dem Entfernen | Neuer Test `UnreadViewModelTests.MarkReadCommand_DecrementsUnreadCount` grün (2 → 1, Text aktualisiert) |
| Abschnitt „UI-Polish (issue-30)" in `docs/help/anwendung/mobile-ui-design.md` fehlte | Abschnitt „UI-Polish & Accessibility (issue-30)" ergänzt (Verifikationsumgebung, Seitenbefunde, Screenshot-Pfade, offene Punkte) | Datei geprüft (`mobile-ui-design.md:113-122`) |
