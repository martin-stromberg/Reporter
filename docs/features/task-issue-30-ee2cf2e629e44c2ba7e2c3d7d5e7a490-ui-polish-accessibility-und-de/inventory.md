<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: UI-Polish, Accessibility und Design-System-Vollständigkeit (Issue #30)

Analysiert wurde der gesamte UI-Layer der .NET-MAUI-App `Reporter` (Views, Design-Tokens, Lokalisierung) sowie die für Performance relevanten Sync-/Repository-Pfade — bezogen auf die Anforderung in `requirement.md` und die Design-Referenzen unter `design-draft/stitch_local_rss_feed_reader/`.

## Zusammenfassung

**Vorhanden:**

- Vollständiges Light/Dark-Farbtoken-Set aus beiden `DESIGN.md`-Dateien in `Colors.xaml` (inkl. `BorderSubtle`, `BookmarkGold`, `StatusOk/Warning/Error`); durchgehende `AppThemeBinding`-Nutzung in allen Views und impliziten Styles (`Styles.xaml`); Inter-/Newsreader-Fonts eingebunden.
- Typografie-Basisstyles (`DisplayStyle`, `HeadlineStyle`, `HeadlineSmallStyle`, `BodyStyle`, `BodySmallStyle`, `MetaStyle`, `UiLabelStyle`) und 44-pt-Mindest-Touch-Ziele auf Buttons/Eingabecontrols via impliziter Styles.
- `UnreadViewModel` bringt die komplette Chip-Datenbasis bereits mit: `Categories` (`ObservableCollection<CategoryFilterItem>` mit `IsSelected`/`Count`), `SelectCategoryCommand`, Paging (`PageSize = 20`, `LoadMoreCommand`, `RemainingItemsThreshold = 2` in `UnreadPage`).
- `ArticleDetailPage`: alle 6 Bottom-Bar-Aktionen haben `SemanticProperties.Description`; WebView-HTML enthält `color-scheme`-Meta + `@media (prefers-color-scheme: dark)`; Offline-Linkblock via `WebViewNavigationGuard`.
- `SettingsPage`: `SemanticProperties.Description` auf nahezu allen Eingabecontrols.
- Fehlerisolation im Sync: `SyncFeedAsync` fängt Nicht-Cancel-Exceptions → `FeedHealth.Error` + `SyncLog`; `SyncAllAsync` isoliert pro Feed; ViewModels mappen auf lokalisierte `SyncStatusError`-Meldungen; `AutoRefreshService` läuft ohne UI-Thread-Marshal (`PeriodicTimer`, `ConfigureAwait`).

**Offensichtlich fehlend / abweichend vom Entwurf:**

- `UnreadPage`: keine sichtbare Chip-Leiste — Kategoriefilter nur über Funnel-Icon → `DisplayActionSheetAsync` + `SelectedCategoryText`-Zeile; Refresh-/Filter-/MarkAll-`Border` ohne `SemanticProperties`.
- `ArticleDetailPage`: Bottom-Bar ist eine flache Vollbreiten-`Grid`-Leiste (`SurfaceContainer`), nicht die schwebende Pill-Bar des Entwurfs.
- `FeedsPage`: Health-Status als 12-pt-Dot + Vollbreiten-`Label` statt Micro-Pill-Badge; Feed-/Suchtreffer-/Kategorie-Karten ohne `SemanticProperties`; Sheet-Backdrop ohne Accessibility-Label.
- `ArticleCardView`: `Stroke` = `Outline` statt `BorderSubtle`; kein separater Ungelesen-Dot (nur 8-pt-`Primary`-Akzent); hartkodiertes `"—"` statt Lesezeit; Bookmark-Fill `Primary` statt `BookmarkGold`; Aktions-`Border` und Tap-Overlay ohne `SemanticProperties`.
- `LaterPage`/`LaterViewModel`: kein Paging (`GetSavedForLaterAsync` lädt komplette Liste; `RemainingItemsThreshold`/`LoadMoreCommand` nicht vorhanden; `IItemRepository` kennt weder paged-Variante noch `AddRangeAsync`).
- `FeedSyncService.RunSyncAsync`: N+1-Muster — pro Feed-Item `GetByGuidOrHashAsync` + `AddAsync` (je eigenem `DbContext` + `SaveChanges`); das geladene `existingItems` wird nicht für In-Memory-Dedup genutzt.
- App-Icon ist noch .NET-Default (`#512BD4` + „NET"-Schriftzug; `MauiIcon`- und `MauiSplashScreen`-`Color="#512BD4"`); `dotnet_bot.png` noch vorhanden; `AppShell`-Tabs haben keine Icons.
- `FontAutoScalingEnabled` wird nirgends gesetzt; `label-*`-/`body-reading`-Typo-Styles und Status-Tint-Farben für Badge-Hintergründe fehlen; resx enthält keine Accessibility-Schlüssel für die unbeschrifteten Elemente auf `UnreadPage`/`ArticleCardView`/Karten/Backdrop.

**Test-Ausgangszustand:** `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` → **331/331 Tests erfolgreich, 0 fehlgeschlagen, 0 übersprungen** (Exit-Code 0, 2026-09-13 MESZ, Commit `3f70a3a`). Keine bekannten Fehlschläge; keine automatisierten UI-Tests — UI-/Accessibility-Verifikation bleibt manuell. Nachweis und Details: [Tests](inventory/tests.md).

## Details

- [Views / XAML-Oberflächen](inventory/views.md)
- [Ressourcen / Design-Tokens / Lokalisierung](inventory/resources.md)
- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
