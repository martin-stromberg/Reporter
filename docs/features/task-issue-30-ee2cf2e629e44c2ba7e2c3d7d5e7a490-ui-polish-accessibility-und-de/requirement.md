<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: UI-Polish, Accessibility und Design-System-Vollständigkeit (Issue #30)

## Fachliche Zusammenfassung

Abschließender Qualitäts- und Konsistenz-Pass über die gesamte .NET-MAUI-App `Reporter`: Das Design-System aus `design-draft/stitch_local_rss_feed_reader/` (Tokens in `editorial_feed/DESIGN.md` und `reporter_editorial_dark/DESIGN.md`, Screens als `screen.png`/`code.html`) ist vollständig und lückenlos auf alle Seiten anzuwenden — Typografie (Newsreader/Inter), Farbtokens, Spacing, Shape/`RoundRectangle`-Radien, Filter-Chips, Karten, Status-Badges/Health-Indikatoren, die Floating Reader Control Bar und das Programmsymbol. Ergänzend sind Dark-Mode-Feinschliff, Accessibility (`SemanticProperties`, Kontraste ≥ Richtwerte, Screenreader-Bedienbarkeit, dynamische Schriftgrößen) und Performance (flüssige Infinity-Listen, schnelle DB-Zugriffe, nicht blockierender Sync) sicherzustellen sowie die Release-Reife durch Tests auf iOS und Windows zu belegen.

## Betroffene Klassen und Komponenten

### UI-Komponenten / Views (`src/Reporter/Views/`)

- `UnreadPage.xaml` / `UnreadPage.xaml.cs` — Filter-Chips: Der Entwurf (`ungelesen_dashboard/code.html`, `filter-chip`-Buttons mit Count-Kapsel) sieht eine horizontal scrollbare Pill-Chip-Leiste vor; aktuell erfolgt die Kategorieauswahl über einen Funnel-Icon-`Border` → `OnFilterClicked` → `DisplayActionSheetAsync` plus `SelectedCategoryText`-`Label`. Die Auswahl-/Identifikationsinteraktion (Kategoriefilter über `UnreadViewModel.Categories` vom Typ `ObservableCollection<CategoryFilterItem>` mit `IsSelected`/`Count` und `SelectCategoryCommand`) ist als eigener Fachpunkt umzusetzen — als sichtbare Chip-Leiste statt nur ActionSheet.
- `ArticleDetailPage.xaml` — Floating Reader Control Bar: Aktuell flache `Grid`-Leiste (`Grid.Row="5"`, solide `SurfaceContainer`-Fläche, 6 Aktionen). Entwurf: schwebende Pill-Bar (52 pt Höhe, `rounded-full`, 16 pt Ränder, transluzent/Frosted-Glass, Safe-Area-Padding).
- `FeedsPage.xaml` — Status-Badges/Health-Indikatoren: Aktuell 12-pt-`BoxView`-Dot plus Vollbreiten-`Label` mit `DataTrigger` auf `FeedHealth.Ok`/`Warning`/`Error`. Entwurf: Micro-Pill-Badge (Padding 2×8, 6-pt-Dot, `label-meta`-Text, Statusfarbe als 10-%-Tint-Hintergrund, dunklerer Statustext). Ebenfalls betroffen: Feed-Karten und Suchtreffer-Karten (`Border` + `RoundRectangle 12`).
- `ArticleCardView.xaml` — Artikelkarte (bereits `RoundRectangle 16`, entspricht `rounded-lg`): Abgleich von Hairline-Stroke (`BorderSubtle` statt `Outline`), Ungelesen-Dot (6 pt, Cobalt/`Secondary`), Lesezeit-/Meta-Zeile (aktuell hartkodiertes `"—"`-Label), Bookmark-Gold (`BookmarkGold`) vs. aktuell `Primary`-Fill.
- `LaterPage.xaml`, `CategoriesPage.xaml`, `SettingsPage.xaml` — genereller Token-/Shape-/Spacing-Abgleich (Karten-Radien, Hairline-Borders, Touch-Ziele).
- `AppShell.xaml.cs` — Tab-Bar-Styling erfolgt über implizite `Shell`-Styles in `Styles.xaml`; Tab-Icons/-Labels auf Design- und Accessibility-Konformität prüfen.

### Ressourcen / Design-Tokens

- `src/Reporter/Resources/Styles/Colors.xaml` — Token-Set bereits aus beiden `DESIGN.md`-Dateien übernommen; Feinschliff/Vollständigkeit prüfen (z. B. `DarkOnSurfaceVariant` `#d8c3ad` gegen den Entwurf verifizieren, Status-Badge-Tints, `BorderSubtle`/`Hairline`-Rollen konsistent verwenden).
- `src/Reporter/Resources/Styles/Styles.xaml` — Typografie-Styles (`DisplayStyle`, `HeadlineStyle`, `HeadlineSmallStyle`, `BodyStyle`, `BodySmallStyle`, `MetaStyle`, `UiLabelStyle`) gegen die `typography`-Skala des Entwurfs vervollständigen (u. a. `label-md`/`label-sm`/`label-meta`, `body-reading` für den Lesemodus); Shape-Konstanten (Karten 16, Controls 8–12, Chips/Badges `full`, Thumbnails 10, Favicons 6) konsistent anwenden.
- `src/Reporter/Resources/AppIcon/appicon.svg` + `appiconfg.svg` — aktuell noch .NET-Default-Icon (lila `#512BD4` + „NET"-Schriftzug); durch das Logo aus `take_the_existing_logo_design_from_the_reference_image_data_image_image_13_the/screen.png` ersetzen.
- `src/Reporter/Reporter.csproj` — `MauiIcon`-`Color="#512BD4"` auf die Design-Palette anpassen; ggf. `Platforms/iOS/Info.plist`, `Platforms/Android/AndroidManifest.xml` (Icon-Referenzen).
- `src/Reporter.Core/Resources/AppResources.resx` + `AppResources.de.resx` — neue Schlüssel für fehlende Accessibility-Labels (icon-only Buttons auf `UnreadPage`, `ArticleCardView`-Aktionsbuttons, Karten-Tap-Overlays) und ggf. Chip-/Badge-Texte.

### Logikklassen / Services / ViewModels

- `src/Reporter.Core/ViewModels/UnreadViewModel.cs` — `Categories`/`CategoryFilterItem`/`SelectCategoryCommand` für die Chip-Leiste wiederverwenden; `SelectedCategoryText` evtl. durch sichtbare Chips obsolet.
- `src/Reporter.Core/ViewModels/LaterViewModel.cs` — lädt über `IItemRepository.GetSavedForLaterAsync` aktuell die komplette Liste ohne Paging; für flüssige Infinity-Listen ggf. Paging analog `UnreadViewModel` (`PageSize`, `LoadMoreCommand`, `RemainingItemsThreshold`).
- `src/Reporter.Core/Services/FeedSyncService.cs` — `RunSyncAsync` führt pro Feed-Item `IItemRepository.GetByGuidOrHashAsync` + `AddAsync` aus (N+1-Muster; jeder `AddAsync`-Aufruf öffnet einen eigenen `ReporterDbContext` + `SaveChanges`). `existingItems` wird bereits am Anfang via `GetByFeedAsync` geladen — In-Memory-Dedup per HashSet und Batch-Insert naheliegend (schnelle DB-Zugriffe, kein UI-Block beim Sync).
- `src/Reporter.Data/Repositories/ItemRepository.cs` + `src/Reporter.Core/Interfaces/IItemRepository.cs` — ggf. neue Member: paged `GetSavedForLaterAsync(page, pageSize)`, Batch-`AddRangeAsync`; bestehende Optimierungen (`AsNoTracking`, `ExecuteUpdateAsync`, `ExecuteDeleteAsync`, `Skip`/`Take`-Paging in `GetUnreadByDateAsync`) beibehalten.
- `src/Reporter.Core/Services/AutoRefreshService.cs` — `PeriodicTimer`-Hintergrund-Sync: verifizieren, dass keine UI-Thread-Marshals stattfinden und der Sync die UI nicht blockiert.
- `src/Reporter/Services/AppThemeService.cs` / `IAppThemeService` — Theme-Anwendung (`Settings.Theme`: system/light/dark) ist vorhanden; nur Feinschliff.
- `ArticleHtmlSanitizer` / `WebViewNavigationGuard` — WebView-Dark-Mode (`prefers-color-scheme`) bereits vorhanden; im Zuge des Dark-Mode-Feinschliffs mitverifizieren.

### Enums / Konstanten

- `FeedHealth` (`OK`/`Warning`/`Error`) — Konstanten bleiben; die Badge-Darstellung mappt sie auf die `Status*`-/Tint-Tokens.
- `CategoryFilterItem` — trägt bereits `IsSelected`/`Count` für den Chip-Aktiv-Zustand inkl. Count-Kapsel.

### Tests

- `src/Reporter.Tests` — `UnreadViewModelTests` (Chip-Auswahl/Paging), `LaterViewModelTests` (Paging), `FeedSyncServiceTests` (Batch-Insert/Dedup, Fehlerisolation je Feed), `ItemRepositoryTests` (neue Repository-Member). UI-nahe Änderungen ohne Unit-Test-Abdeckung: manuelle Verifikation am 390 × 844-pt-Windows-Fenster (Light + Dark) mit Screenshots in `test-results.md` und `docs/help/anwendung/mobile-ui-design.md` gemäß `AGENTS.md`; iOS-Verifikation über `scripts/iOS-Deployment.ps1`.

## Implementierungsansatz

1. **Audit:** Alle Screens gegen die Referenzen `design-draft/stitch_local_rss_feed_reader/{ungelesen_dashboard, feeds_health_status, f_r_sp_ter_bewahren, artikel_lesemodus, einstellungen_filter}[_dark_mode]/screen.png` + `code.html` abgleichen; Abweichungsliste erstellen (für `CategoriesPage` existiert kein eigener Entwurf — Annahme: Muster der anderen Karten-Listen übernehmen).
2. **Tokens vervollständigen:** Fehlende Farb-/Typo-/Shape-Tokens in `Colors.xaml`/`Styles.xaml` ergänzen; `AppThemeBinding` überall (AGENTS.md-Regel), Dark-Werte aus `reporter_editorial_dark/DESIGN.md`.
3. **Komponenten nachziehen:**
   - Chip-Leiste auf `UnreadPage` (horizontal scrollende `CollectionView` oder `FlexLayout`/`BindableLayout` über `Categories`; Aktiv-Zustand via `IsSelected`-`DataTrigger`: dunkle `PrimaryContainer`-Fläche + `OnPrimary`-Text + Count-Kapsel; inaktiv: `SurfaceCard` + Hairline-Border). Entscheidung, ob `OnFilterClicked`/`DisplayActionSheet` entfällt oder als Overflow bleibt.
   - Pill-Badge für `HealthStatus` in `FeedsPage` (bestehende `DataTrigger`-Struktur umfärben/umformen).
   - Floating Bar in `ArticleDetailPage` als pill-förmiger `Border` mit Margins, `Shadow`/Elevation und transluzenter `AppThemeBinding`-Fläche (MAUI hat kein Backdrop-Blur — Annäherung, siehe Offene Fragen).
   - Programmsymbol: `appicon.svg`/`appiconfg.svg` ersetzen, `MauiIcon`-Hintergrund in `Reporter.csproj` anpassen.
4. **Accessibility-Pass:** `SemanticProperties.Description` (bzw. `AutomationProperties`) für alle icon-only/tap-basierten Elemente ohne Text (Refresh-/Filter-/MarkAll-Buttons, `ArticleCardView`-Aktions-`Border` und Karten-Overlay-`Grid`, Feed-/Kategorie-/Suchtreffer-Karten, Sheet-Backdrop); Kontrastprüfung der Text-/Statusfarben (`TextMuted` `#94a3b8`, Status-Dots ≥ 3:1 mit Text-/Icon-Äquivalent); `FontAutoScalingEnabled`-Audit für dynamische Schriftgrößen; Touch-Ziele ≥ 44 pt nachmessen.
5. **Performance-Pass:** Batch-Insert + In-Memory-Dedup in `FeedSyncService.RunSyncAsync`; Paging in `LaterViewModel`; `CollectionView`-Templates auf schwere Elemente prüfen (`Image`-Caching); sicherstellen, dass `AutoRefreshService`-Ticks und Sync-Pfade nicht auf den UI-Thread marshaln.
6. **Robustheit:** Bestehende Fehlerisolation verifizieren — `SyncFeedAsync` fängt alle Nicht-Cancel-Exceptions → `FeedHealth.Error` + `SyncLog`; `SyncAllAsync` isoliert pro Feed; ViewModels mappen auf lokalisierte `SyncStatusError`-Meldungen; restliche unbehandelte Pfade (Feed-Suche, Direkt-Add, WebView) schließen.
7. **Verifikation:** `dotnet test` (`Reporter.Tests`), `.\scripts\Run-StaticChecks.ps1` (Format/Security/Static Analysis inkl. MAUI-Release-Build), manuelle UI-Verifikation 390 × 844 pt Light + Dark inkl. Screenshots, Regression: alle Akzeptanzkriterien vorangegangener Issues (`mobile-ui-design.md`-Checkliste als Referenz).

## Konfiguration

Kein neuer Konfigurationsbedarf identifiziert. Das Farbschema ist bereits benutzerspezifisch über `Settings.Theme` (Persistenz via `ISettingsRepository`, Anwendung via `IAppThemeService`, Auswahl-Picker auf `SettingsPage`) konfigurierbar; dynamische Schriftgrößen und Screenreader folgen den OS-Einstellungen (keine App-seitige Option gefordert).

## Offene Fragen

- **Accessibility-Scanner:** Welches Werkzeug gilt als Referenz für „keine kritischen Fehler" (z. B. Accessibility Insights for Windows, Xcode Accessibility Inspector)? Wie ist „kritisch" definiert?
- **Filter-Chips vs. Funnel-Button:** Ersetzt die Chip-Leiste den bisherigen `DisplayActionSheet`-Flow auf `UnreadPage` vollständig oder ergänzt sie ihn (Overflow bei vielen Kategorien)?
- **Programmsymbol:** Liegt das finale Icon als SVG/Asset vor oder soll es anhand der Referenz-`screen.png` nachgebaut werden? Wer liefert das Ausgangsmaterial?
- **Floating Bar:** Backdrop-Blur/Frosted Glass ist in .NET MAUI nicht nativ verfügbar — ist eine semi-transparente Fläche mit Hairline-Border und Schatten als Annäherung akzeptabel?
- **Performance-Kriterien:** „Flüssig scrollen" und „schnelle Datenbankzugriffe" sind nicht quantifiziert — gelten konkrete Zielwerte (z. B. Seitenladezeit, Frame-Rater) oder genügt die dokumentierte manuelle Verifikation?
- **iOS-Verifikation:** Die iOS-Tests (`scripts/iOS-Deployment.ps1`, Simulator-Screenshots) erfordern macOS — wer führt sie durch bzw. auf welchem Gerät? Bisherige Issues haben diese Verifikation als Folgeaufgabe offen gelassen.
- **Kategorien-Seite:** Es existiert kein eigener Entwurfs-Screen — gilt die Annahme, das Karten-Listen-Muster der anderen Seiten zu übernehmen?
