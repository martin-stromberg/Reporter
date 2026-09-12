# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### LaterPage.xaml („Für später"-Tab)

- **Erreichbarkeit** — Die Anforderung verlangt, den Offline-Zustand in der UI sichtbar zu machen. Auf der „Für später"-Seite verändert sich die Oberfläche im Offline-Zustand zwar sichtbar (die Artikel-Thumbnails in `ArticleCardView` werden über `IsOnline="{Binding ...}"` ausgeblendet), aber es gibt auf dieser Seite keinerlei Hinweis auf den Offline-Zustand. Eine nicht-technische Anwenderin sieht, dass Bilder in den Karten plötzlich fehlen, ohne erkennen zu können, warum — anders als auf „Ungelesen" und „Feeds", wo ein beschrifteter Offline-Hinweis eingeblendet wird.

  Empfehlung: Auf `LaterPage.xaml` denselben Offline-Hinweis einblenden wie auf `UnreadPage`/`FeedsPage` (z. B. `Border` mit `DataTrigger` auf `IsOnline == False` und `Label Text="{x:Static strings:AppResources.OfflineHint}"`), damit das Verschwinden der Thumbnails erklärt ist und der Offline-Zustand konsistent auf allen Listen-Seiten sichtbar ist.

### UnreadPage.xaml / FeedsPage.xaml („Ungelesen"- und „Feeds"-Tab)

- **Erreichbarkeit** — Im Offline-Zustand wird bereits ein neutraler grauer Hinweis „Keine Internetverbindung." eingeblendet (Label in der Kopfzeile auf `UnreadPage`, Zeile 79–88; Banner auf `FeedsPage`, Zeile 73–86). Tippt die Anwenderin trotzdem auf den (nur abgedunkelten, weiterhin tap-baren) Sync-Button bzw. zieht Pull-to-Refresh oder wählt im Feed-Aktionsmenü „Aktualisieren", erscheint zusätzlich dieselbe Meldung „Keine Internetverbindung." ein zweites Mal als rote Fehlermeldung (`ErrorMessage`/`SyncErrorMessage` in `LightError`/`DarkError`, `UnreadViewModel.RefreshAsync` Zeile 327, `FeedsViewModel` Zeilen 349/385). Dieselbe Botschaft erscheint also gleichzeitig als neutraler Status und als roter „Fehler" — Rot suggeriert ein Fehlverhalten, obwohl Offline ein normaler Zustand ist; das kann irritieren.

  Empfehlung: Bei `!IsOnline` keinen separaten roten Fehlertext setzen, wenn der neutrale Offline-Hinweis bereits sichtbar ist (Sync einfach ohne weitere Meldung abbrechen), oder die Meldung im selben neutralen Stil wie das Offline-Banner darstellen.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- Artikel ohne Netzwerkverbindung lesen (Ungelesen-/Für-später-Liste → Detailansicht, WebView mit lokalem HTML) → unauffällig
- Offline-Zustand in der UI erkennen / Synchronisations-Button zeigt Offline-Status → Befund vorhanden (LaterPage ohne Hinweis; doppelte Meldung in Info- und Fehlerdarstellung auf Unread/Feeds)
- Feed-Synchronisation auslösen (Sync-Button, Pull-to-Refresh, Feed-Aktionsmenü „Aktualisieren") ohne Netz → Befund vorhanden (identische Meldung doppelt als Hinweis und als roter Fehler, siehe oben)
- Links im Artikelinhalt im Offline-Modus (deaktiviert, `<a>`-Tags neutralisiert + `Navigating`-Abbruch mit lokalisiertem Alert) → unauffällig
- „Im Browser öffnen" und „Teilen" in der Artikeldetailansicht bei Offline → unauffällig („Im Browser öffnen" zeigt lokalisierten Hinweis statt Absturz)
- Alle sichtbaren Texte in System­sprache EN/DE (hardcodierte Texte in `ArticleDetailPage`, `ArticleDetailViewModel`, `CategoriesViewModel` durch `AppResources`-Schlüssel ersetzt, beide ResX-Dateien gepflegt) → unauffällig
- Feed hinzufügen/bearbeiten/löschen auf `FeedsPage` (URL-/Titel-Eingabe, Kategorie-Picker mit Klartext-Namen) → unauffällig (keine internen Kennungen erforderlich)
- Mobile-Regeln aus `AGENTS.md`: keine horizontalen Datentabellen, keine Mehrfach-Textbuttons in einer Zeile (Aktionsleiste nutzt Icon-Buttons), `CollectionView`/`ScrollView` nicht verschachtelt (`RefreshView` > `CollectionView` in `Grid`-Zeile `*`), Touch-Targets ≥ 44 × 44 pt (Sync-/Filter-Buttons 44, Aktionsleiste 44, „Im Browser öffnen" MinimumHeightRequest 44), Dark Mode durchgehend via `AppThemeBinding` → unauffällig

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/UnreadPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml` (+ `FeedsPage.xaml.cs`)
- `src/Reporter/Views/LaterPage.xaml`
- `src/Reporter/Views/ArticleDetailPage.xaml` (+ `ArticleDetailPage.xaml.cs`)
- `src/Reporter/Views/ArticleCardView.xaml` (+ `ArticleCardView.xaml.cs`)
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter.Core/ViewModels/BaseViewModel.cs`
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/LaterViewModel.cs`
- `src/Reporter.Core/ViewModels/CategoriesViewModel.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` / `AppResources.Designer.cs`
- `src/Reporter.Core/Services/WebViewNavigationGuard.cs` (Offline-Link-Klassifizierung für `Navigating`-Handler)
- `src/Reporter.Core/Services/ArticleHtmlSanitizer.cs` (Offline-Neutralisierung von `<a>`/`<img>` im Artikel-HTML)
