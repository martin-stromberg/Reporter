# Offene Aufgaben

Erstellt am: 2026-09-10
Abbruchgrund: Maximale Iterationsanzahl erreicht (3/3). Der Build und die Unit-Tests sind fehlerfrei; verbleibende Punkte betreffen Detail-Abweichungen gegen den Plan, Code-Qualität, Usability-Vereinfachungen und fehlende E2E-Tests.

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht vollständig abgeschlossen werden und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- [ ] Schriftarten im WebView-CSS mit `MauiProgram.cs`-Fontregistrierungen koppeln (`@font-face` für Newsreader/Inter). `review.md:11-13`
- [ ] Separate `IncreaseFontSizeCommand` / `DecreaseFontSizeCommand` anstelle nur `ToggleFontSizeCommand` bereitstellen. `review.md:15-17`
- [ ] ViewModel-Properties (`ContentHtml`, `Title`, `IsRead`, `IsSavedForLater` usw.) als Top-Level-Properties exponieren, nicht nur über `Item`. `review.md:19-21`
- [ ] UI-Review-Screenshots (Light/Dark/390×844 pt) in `docs/help/anwendung/mobile-ui-design.md` einfügen. `review.md:23-25`
- [ ] ViewModel-Pfad/Namespace konsistent halten (`src/Reporter/ViewModels/` vs. `src/Reporter.Core/ViewModels/`). `review.md:27-29`

## Code-Review-Befunde

- [ ] `ArticleDetailViewModel.cs:285-289` `CancelAutoMarkRead` sollte `_autoMarkCts?.Dispose()` aufrufen, bevor es auf `null` gesetzt wird.
- [ ] `MauiProgram.cs:49` `HttpClient` als Singleton überdenken; `IHttpClientFactory` wäre idiomatischer.
- [ ] `ArticleDetailViewModel.cs:21-30` & `309-326` Regex-basierte HTML-Sanitization durch dedizierte Bibliothek (z. B. `HtmlSanitizer`) ersetzen.
- [ ] `ArticleDetailViewModel.cs:21` `HtmlTagRegex` für Lesezeit-Berechnung robust gegen `>`-Zeichen in Inhalten machen.
- [ ] `AppShell.xaml.cs:20` & `ArticleCardView.xaml.cs:86` Routenname `"articledetail"` in zentrale Konstante auslagern.
- [ ] `ArticleDetailViewModel.cs:416-417` `MarkReadDelayedAsync` Aktualisierung von `Item` ggf. via `MainThread.BeginInvokeOnMainThread` absichern.

## Usability-Befunde

- [ ] Share-Icon gegen Standard-Share-Symbol austauschen. `review-usability.md:83`
- [ ] Action-Bar-Items von `Border` + `TapGestureRecognizer` auf echte `Button`-Controls umstellen (Accessibility). `review-usability.md:84`
- [ ] `ArticleDetailPage` Bottom-Bar Padding: 24 pt unten durch reine Safe-Area-Insets ersetzen. `review-usability.md:85`
- [ ] `UnreadPage.xaml` `SafeAreaEdges="All"` ergänzen. `review-usability.md:86`
- [ ] `Auto-Gelesen`-Switch `SemanticProperties.Description` ergänzen. `review-usability.md:61`
- [ ] `UnreadPage` Toolbar-Icons `SemanticProperties.Description` ergänzen. `review-usability.md:63`
- [ ] Source-Footer-Button `Im Browser öffnen` Layout auf 390 pt Breite prüfen (Redundanz mit Action Bar). `review-usability.md:87`
- [ ] Externe Artikelbilder im Dark Mode im Handbuch erwähnen. `review-usability.md:78`

## Fehlgeschlagene Tests

- [ ] Keine automatisierten E2E-Tests für die Artikeldetailansicht vorhanden (Navigation, WebView, Gelesen, Lesezeichen, Teilen, Browser öffnen, Mobile Layout/Touch-Targets).
