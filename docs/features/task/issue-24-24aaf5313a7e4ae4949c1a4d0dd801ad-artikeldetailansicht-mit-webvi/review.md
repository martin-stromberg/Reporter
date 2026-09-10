# Review-Plan: Issue #24 – Artikeldetailansicht mit WebView und Lesestatus

## Status
**Offene Aufgaben vorhanden**

## Zusammenfassung
Die Kerngeschäftslogik der Artikeldetailansicht ist in `ArticleDetailViewModel`, `ArticleDetailPage`, `AppShell` und `MauiProgram` vorhanden. Die meisten Plan-Punkte sind umgesetzt: Routing, HTML-Wrapper, automatisches Gelesen-Markieren, Lesezeichen, Teilen, Browser-Öffnen, Floating Bottom Action Bar mit 44 × 44 pt Touch-Targets, `AppThemeBinding` und Dark-Mode-CSS. Es verbleiben jedoch konkrete Abweichungen bei den Schriftarten im WebView, der geplanten ViewModel-Oberfläche und den Verifikationsartefakten.

## Offene / unvollständige Plan-Punkte

### 1. Schriftarten im WebView-CSS nicht mit `MauiProgram.cs` gekoppelt
**Plan:** Schritt 5 – Schriftart Newsreader/Inter einbinden (vgl. `MauiProgram.cs`-Schriftregistrierungen).  
**Befund:** `MauiProgram.cs` Zeilen 29-36 registrieren die Fonts unter den Alias-Namen `InterRegular`, `NewsreaderRegular` usw. In `ArticleDetailViewModel.cs` `RebuildHtml()` Zeilen 348 und 362 wird im generierten HTML `font-family: 'Newsreader', Georgia, 'Times New Roman', serif` verwendet. Es fehlt eine `@font-face`-Regel, die die in MAUI registrierten Font-Dateien in den WebView lädt; ohne sie greifen Systemschriften/Fallback und die Darstellung entspricht nicht zwingend dem Design-Entwurf.

### 2. Font-Size-Befehle weichen vom Plan ab
**Plan:** Schritt 2 – `IncreaseFontSizeCommand` / `DecreaseFontSizeCommand`.  
**Befund:** `ArticleDetailViewModel.cs` Zeilen 67, 219, 488-492 stellen nur ein `ToggleFontSizeCommand` bereit, das zwischen A und A+ umschaltet. Die geplanten separaten Befehle für inkrementelles Vergrößern/Verkleinern fehlen.

### 3. Geplante ViewModel-Properties fehlen als eigenständige Member
**Plan:** Schritt 2 – Properties: `Item`, `ContentHtml`, `Title`, `FeedName`, `PublishedAt`, `IsRead`, `IsSavedForLater`, `IsAutoMarkRead`, `ReadingTime`, `FontSizeIndex`, `FeedIconUrl` (optional).  
**Befund:** In `ArticleDetailViewModel.cs` Zeile 125 ff. ist `HtmlSource` anstelle von `ContentHtml` vorhanden; `Title`, `IsRead` und `IsSavedForLater` werden nicht als Top-Level-Properties exponiert, sondern nur über `Item` indirekt zugänglich. Funktional reicht `Item` aus, entspricht aber nicht der geplanten Schnittstelle.

### 4. UI-Review-Screenshots nicht im Markdown nachweisbar
**Plan:** Schritt 7 und Verifikationsartefakte – Manuelle UI-Verifikation mit Screenshot in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` dokumentieren.  
**Befund:** `docs/help/anwendung/mobile-ui-design.md` Zeilen 63-71 enthält textliche Beschreibungen (getestete Größen, Layout-Prüfungen, Dark Mode), aber keine eingefügten Screenshot-Dateien oder Bildverweise. Der geforderte visuelle Nachweis fehlt damit.

### 5. Pfad/Namespace-Abweichung des ViewModels
**Plan:** Schritt 2 – `src/Reporter.Core/ViewModels/ArticleDetailViewModel.cs`.  
**Befund:** Die Datei ist in `src/Reporter/ViewModels/ArticleDetailViewModel.cs` abgelegt (Namespace `Reporter.Core.ViewModels`). Sie ist zwar in `MauiProgram.cs` registriert, der physische Pfad entspricht aber nicht dem Plan.

## Umgesetzte Plan-Punkte (Auszug)

- `ArticleDetailViewModel.cs` nutzt `IItemRepository.GetByIdAsync`, `MarkAsReadAsync`, `ToggleSavedForLaterAsync`, `Browser.OpenAsync` und `Share.RequestAsync` (Schritt 2).
- `ArticleDetailPage.xaml` Zeile 7-290: `Shell.NavBarIsVisible="False"`, `Grid RowDefinitions="Auto,Auto,*,Auto,Auto"`, WebView mit `HtmlWebViewSource`, Quellen-Footer-Karte und Floating Bottom Action Bar (Schritt 3).
- Route `articledetail` in `AppShell.xaml.cs` Zeile 20; DI-Registrierung in `MauiProgram.cs` Zeilen 56-57 (Schritt 4).
- `ArticleDetailPage.xaml.cs` Zeilen 26-39 nimmt Query-Parameter `itemId` entgegen und übergibt sie an `LoadAsync` (Schritt 4).
- Auto-Gelesen mit konfigurierbarer Verzögerung aus `ISettingsRepository` und 5-Sekunden-Fallback, inkl. `Auto-Gelesen`-Toggle und Cancellation in `OnDisappearing` (Schritt 6).
- `OpenArticleCommand` in `ArticleCardView.xaml.cs` Zeilen 77-92 ruft `Shell.Current.GoToAsync($"articledetail?itemId={item.Id}")` auf (Schritt 4).
- Lesezeitberechnung in `ArticleDetailViewModel.cs` Zeilen 292-303 aus Wortanzahl des `ContentHtml` (Schritt 2).
- `WebView`-CSS in `ArticleDetailViewModel.cs` Zeilen 344-374 enthält `<meta name='color-scheme' content='light dark'>` und `@media (prefers-color-scheme: dark)` (Schritt 5).
- Floating Bottom Action Bar in `ArticleDetailPage.xaml` Zeilen 141-288 verwendet durchgehend 44 × 44 pt `Border`-Touch-Targets und `AppThemeBinding` (Designvorgaben 5/6).
