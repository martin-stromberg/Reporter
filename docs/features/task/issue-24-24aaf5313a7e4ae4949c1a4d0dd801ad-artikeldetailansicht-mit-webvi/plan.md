# Implementierungsplan – Artikeldetailansicht mit WebView und Lesestatus

## Aufgabe
- Issue #24: Artikeldetailansicht mit WebView und Lesestatus
- Branch: `task/issue-24-24aaf5313a7e4ae4949c1a4d0dd801ad-artikeldetailansicht-mit-webvi`
- Aufgaben-ID: `24aaf531-3a7e-4ae4-949c-1a4d0dd801ad`

## Ziel
Nutzer können einen Artikel in einer lesefreundlichen Detailansicht öffnen. Beim Öffnen wird der Artikel automatisch als gelesen markiert (sofort oder verzögert). Der Artikel kann über einen Toggle für später gespeichert, über einen Button im externen Browser geöffnet und geteilt werden.

## Ausgangslage
- `Item.ContentHtml` im Domain-Modell und Repository vorhanden.
- `IItemRepository.GetByIdAsync`, `MarkAsReadAsync` und `ToggleSavedForLaterAsync` bereits implementiert.
- `ArticleCardView` und `UnreadPage` existieren; Card-Layout, 44 × 44 pt Touch-Targets und `AppThemeBinding` sind etabliert.
- `ArticleDetailPage` und `ArticleDetailViewModel` fehlen.
- `AppShell.xaml` ist leer; Routing-Registrierung notwendig.
- Design-Draft für Light- und Dark-Mode liegt unter `design-draft/stitch_local_rss_feed_reader/artikel_lesemodus/` vor.

## Designvorgaben (Mobile UI Design Review)
1. Design-Draft-Vergleich gegen `design-draft/stitch_local_rss_feed_reader/artikel_lesemodus/screen.png` (Light) und `artikel_lesemodus_dark_mode/screen.png` (Dark).
2. Mobile Formfaktor 390 × 844 pt (Windows handysize) oder iOS-Simulator-Screenshot aus `scripts/iOS-Deployment.ps1` testen.
3. Keine horizontalen Datentabellen oder mehrere Text-Buttons in einer Zeile auf Mobile. Karten-/Bottom-Bar-Layout bevorzugen.
4. Kein verschachteltes `CollectionView`/`ScrollView`. Seite einzeln scrollen; Inhalte via `Grid` Row `*` füllen.
5. Touch-Targets mindestens 44 × 44 pt.
6. Dark Mode über `AppThemeBinding` (`Light...`/`Dark...`) sowie WebView-Dark-CSS (`prefers-color-scheme` oder injected Theme).
7. Manuelle UI-Verifikation mit Screenshot dokumentieren; falls kein automatisierter UI-Test existiert, Ergebnisse in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` festhalten.

## Implementierungsschritte

### 1. Domain-/Daten-Layer prüfen und ergänzen (falls nötig)
- [ ] `src/Reporter.Core/Models/Item.cs` und `IItemRepository` bestätigen, dass `ContentHtml`, `IsRead`, `IsSavedForLater`, `Link`, `PublishedAt`, `Title` vollständig verfügbar sind.
- [ ] `GetByIdAsync` für Detailansicht integrieren; `MarkAsReadAsync`/`ToggleSavedForLaterAsync` direkt nutzen.
- [x] Prüfen, ob `FeedName` / Feed-Icon bereits über `FeedId` verfügbar sind. `FeedName` ist verfügbar, Feed-Icon ist nicht umsetzbar, weil `Feed` kein Icon/Image-Property besitzt (Out-of-Scope für dieses Arbeitspaket; Platzhalter-BoxView wird angezeigt).

### 2. ViewModel erstellen
- [ ] `src/Reporter.Core/ViewModels/ArticleDetailViewModel.cs` anlegen.
- [ ] Properties: `Item`, `ContentHtml`, `Title`, `FeedName`, `PublishedAt`, `IsRead`, `IsSavedForLater`, `IsAutoMarkRead`, `ReadingTime`, `FontSizeIndex`, `FeedIconUrl` (optional).
- [ ] Commands:
  - `GoBackCommand`
  - `ToggleSavedForLaterCommand` → `IItemRepository.ToggleSavedForLaterAsync`
  - `ToggleMarkReadCommand` → `IItemRepository.MarkAsReadAsync`
  - `ToggleAutoMarkReadCommand` → Schaltet lokales `IsAutoMarkRead` und bricht/verzögert automatische Markierung.
  - `OpenInBrowserCommand` → `Launcher.OpenAsync(Item.Link)`
  - `ShareCommand` → `Microsoft.Maui.ApplicationModel.DataTransfer.Share.RequestAsync`
  - `IncreaseFontSizeCommand`/`DecreaseFontSizeCommand`
- [ ] Initialisierung: `GetByIdAsync(itemId)`, dann `MarkAsReadAsync` sofort oder mit Verzögerung.
- [ ] `ReadingTime` aus Wortanzahl des `ContentHtml` berechnen (Fallback: statische Schätzung, falls Text nicht verfügbar).
- [ ] `BaseViewModel` wiederverwenden.

### 3. Detailseite erstellen
- [ ] `src/Reporter/Views/ArticleDetailPage.xaml` (und `.xaml.cs`) anlegen.
- [ ] `Shell.NavBarIsVisible="False"` setzen.
- [ ] Layout als `Grid` mit `RowDefinitions="Auto,Auto,*,Auto,Auto"`:
  - Row 0: Status-Pille (Gelesen-Dot + `Auto-Gelesen`-Toggle).
  - Row 1: Header (`FeedIconUrl`, Feed-Name, Trennzeichen, Datum, Lesezeit, Titel, optionaler Untertitel/Autor).
  - Row 2: `WebView` zur Darstellung von `ContentHtml` (kein innerer `ScrollView`, Native WebView übernimmt Scroll).
  - Row 3: Quellen-Footer-Karte "Vollständiger Artikel verfügbar" mit `Im Browser öffnen`-Button.
  - Row 4: Floating Bottom Action Bar als `Grid` mit 44 × 44 pt Buttons (Zurück, Lesezeichen, Schriftgröße, Gelesen, Teilen, Browser).
- [ ] `WebView.Source` auf `HtmlWebViewSource` setzen; HTML-Wrapper mit CSS für Light/Dark und Schriftgrößen (Newsreader/Inter) erzeugen.
- [ ] Alle Farben und Symbole über `AppThemeBinding` ansprechen.

### 4. Navigation und Routing einrichten
- [ ] Route in `AppShell.xaml.cs` oder `MauiProgram.cs` registrieren (`nameof(ArticleDetailPage)`).
- [ ] In `UnreadViewModel` bzw. `ArticleCardView` `OpenArticleCommand` ergänzen, das `GoToAsync($"articledetail?itemId={id}")` aufruft.
- [ ] Query-Parameter `itemId` in `ArticleDetailPage` entgegennehmen und an ViewModel übergeben.

### 5. HTML-/WebView-Styling
- [ ] Wrapper-HTML generieren: `<html><head><meta name="viewport" content="width=device-width"><style>...</style></head><body>{{ContentHtml}}</body></html>`.
- [ ] CSS über `prefers-color-scheme` oder via Theme-Color-Variablen (Light/Dark) passend zu `AppThemeBinding` Farben gestalten.
- [ ] Schriftart Newsreader/Inter einbinden (vgl. `MauiProgram.cs`-Schriftregistrierungen).
- [ ] Schriftgrößen-Klassen für A/A+ via JavaScript/CSS bereitstellen (zunächst lokal im WebView per JavaScript-Injection, bis Einstellungen-Arbeitspaket bereit ist).

### 6. Automatisch als gelesen markieren
- [ ] Beim Laden der Seite Verzögerungslogik starten.
- [ ] Priorität 1: Verzögerungswert aus dem Einstellungen-Arbeitspaket nutzen (sobald verfügbar).
- [ ] Priorität 2: Solange kein Setting verfügbar ist, Fallback 5 Sekunden verwenden, wenn `IsAutoMarkRead` aktiviert ist.
- [ ] `Auto-Gelesen`-Toggle ermöglicht es dem Nutzer, die automatische Markierung zu deaktivieren; bereits als gelesen markierte Artikel ignorieren.

### 7. Mobile UI Design Review durchführen
- [ ] Seite mit `screen.png` Design-Draft vergleichen.
- [ ] Fenster auf 390 × 844 pt (Windows handysize) oder iOS-Simulator testen.
- [ ] Alle Bottom-Bar-Buttons und die Status-Pille auf ≥44 × 44 pt prüfen.
- [ ] Keine verschachtelten `CollectionView`/`ScrollView`; WebView-Scroll ist primär, native Header/Bottom-Bar bleiben fixiert.
- [ ] Dark Mode umschalten und `AppThemeBinding`-Farbwerte validieren.
- [ ] Screenshots in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` einfügen und getestete Größen notieren.

### 8. Akzeptanzkriterien final prüfen
- [ ] Artikelinhalt wird im WebView korrekt dargestellt.
- [ ] Öffnen markiert Artikel als gelesen (sofort oder verzögert).
- [ ] `Auto-Gelesen`-Toggle ist bedienbar und persistiert sein Verhalten lokal in der Seitensession.
- [ ] Toggle aktualisiert `is_saved_for_later`.
- [ ] Quellenlink öffnet externen Browser.
- [ ] Lesestatus und Lesezeichen werden persistiert.
- [ ] "Im Browser öffnen"-Button funktioniert.
- [ ] Lesezeit und Quellen-Footer-Karte sind sichtbar.

## Entscheidungen/Annahmen
- Detailseite wird als `Shell` `GoToAsync`-Route geöffnet (weil `AppShell.xaml` leer ist und programmatische Navigation etabliert werden muss).
- `ContentHtml` wird in einem generierten HTML-Wrapper geladen, damit Schrift, Dark Mode und Floating Bar dem Design-Draft entsprechen.
- Automatische Gelesen-Verzögerung wird zunächst mit einem 5-Sekunden-Fallback und einem `Auto-Gelesen`-Toggle umgesetzt; sobald das Einstellungen-Arbeitspaket verfügbar ist, wird der konfigurierbare Wert an die Initialisierung in `ArticleDetailViewModel` angebunden (siehe Schritt 6).
- Floating Reader Control Bar und Quellen-Footer-Karte bleiben native MAUI-`Grid`/Border-Elemente außerhalb des WebView, damit Touch-Targets und `AppThemeBinding` sichergestellt sind.
- Feed-Icon ist in diesem Arbeitspaket Out-of-Scope, weil `Feed` (Domain-Modell und Daten-Entität) kein Icon-/Image-Property enthält. Stattdessen wird ein farbiger Platzhalter-BoxView im Header angezeigt.
- Untertitel, Autor- und Tag-Leiste werden nur umgesetzt, wenn die Metadaten im `Item`-Objekt oder dem `ContentHtml` bereits enthalten sind; ansonsten als `Out-of-Scope` für dieses Arbeitspaket markiert.
- Teilen über `Microsoft.Maui.ApplicationModel.DataTransfer.Share` vorausgesetzt.

## Risiken/Offene Punkte
- **Einstellungen-Arbeitspaket nicht verfügbar:** Die konfigurierbare Gelesen-Verzögerung ist noch nicht Teil der App. Es wird ein 5-Sekunden-Fallback plus `Auto-Gelesen`-Toggle genutzt; der spätere Integrationspunkt ist in Schritt 6 dokumentiert.
- **Externe Bilder im `ContentHtml`:** Bilder verlinken auf externe URLs; Offline-Darstellung wird in einem separaten Arbeitspaket behandelt.
- **Zusätzliche Metadaten fehlen:** Feed-Icon, Untertitel, Autor/Tags sind nicht explizit im `Item`-Modell vorhanden. Feed-Name und ggf. Icon müssen über `FeedId` bezogen werden; der Rest wird nur umgesetzt, wenn Daten vorliegen, ansonsten aus dem Scope genommen.
- **Lesezeit-Berechnung ungenau:** Ohne explizite Lesezeit-Eigenschaft wird die Zeit aus der Wortanzahl des `ContentHtml` geschätzt; das HTML-Markup kann die Zählung beeinflussen.
- **HTML-Wrapper-Fonts:** Die .NET MAUI-registrierten Schriftarten Newsreader/Inter müssen ggf. explizit per CSS im generierten HTML referenziert werden; Fallback auf Systemschriften ist vorgesehen.
- **UI-Verifikation ohne automatisierte Tests:** Mobile UI Design Review muss manuell mit Screenshot-Dokumentation erfolgen, bis UI-Tests ergänzt werden.

## Verifikationsartefakte
- Manuelle UI-Verifikation in 390 × 844 pt mit Screenshot.
- Dark-Mode-Screenshot.
- Touch-Target-Prüfung dokumentiert.
- Design-Draft-Vergleichsdokumentation in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md`.
