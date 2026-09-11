# Mobile-UI-Design im Reporter

Diese Checkliste sichert ab, dass neue und bestehende UI-Features im
.NET-MAUI-Client auf mobilen Bildschirmen funktionieren und dem
Design-Entwurf folgen.

## Grundprinzipien

- **Mobile first:** Layouts werden zuerst für ca. 390 × 844 pt (iPhone)
  entworfen und anschließend für Desktop erweitert.
- **Keine horizontalen Tabellen:** Listen auf Mobilgeräten werden als
  vertikale Karten dargestellt, nie als mehrspaltige Tabellen mit vielen
  Aktionsschaltflächen pro Zeile.
- **Touch-Ziele:** Alle tippbaren Elemente mindestens 44 × 44 pt.
- **Lesbare Typografie:** Körpertext mindestens 14 pt, Überschriften
  skaliert über `HeadlineStyle` / `HeadlineSmallStyle`.
- **Saubere Scroll-Bereiche:** `CollectionView`/`ScrollView` dürfen nicht
  in einem weiteren `ScrollView` verschachtelt werden. Seiten mit Eingabe
  + Liste nutzen `Grid` mit `*` für die Liste, damit diese den verfügbaren
  Platz füllt und scrollbar ist.
- **Karten-Layout:** Inhaltsgruppen werden in `Border`-Karten mit
  `Padding="16"` und abgerundeten Ecken (`StrokeShape="RoundRectangle 12"`)
  gruppiert.
- **Aktionen pro Element:** Auf Mobilgeräten werden Aktionen über
  `TapGestureRecognizer` + `DisplayActionSheet` oder `SwipeView` angeboten.
  Mehrere Text-Buttons nebeneinander in einer Zeile sind verboten.
- **Header:** Pro Seite gibt es einen inhaltlichen Header (Titel + optional
  Untertitel) innerhalb der Seite. Die native `Shell`-Navigationsleiste
  wird bei reinen Tab-Seiten ausgeblendet (`Shell.NavBarIsVisible="False"`).

## Akzeptanzkriterien für neue UI-Features

- [ ] Design entspricht dem neuesten `design-draft/.../screen.png`.
- [ ] Seite ist im Windows-Handy-Fenster (`App.xaml.cs` 390 × 844) getestet.
- [ ] Keine horizontalen Scroll-Bereiche durch Tabellen.
- [ ] Touch-Ziele erfüllen 44 × 44 pt.
- [ ] E2E/UI-Test oder dokumentierte manuelle UI-Prüfung ist vorhanden.
- [ ] Dark-Mode-Farben verwenden `AppThemeBinding`.

## Anti-Patterns

| Statt … | Lieber … |
|---|---|
| `VerticalStackLayout` + `CollectionView` mit `HeightRequest` | `Grid` mit `Auto,*` oder `RefreshView` + `CollectionView` ohne feste Höhe |
| Mehrere `Button`s nebeneinander in einer Zeile | `TapGestureRecognizer` + `DisplayActionSheet` oder `SwipeView` |
| `Grid` mit vielen `Auto`-Spalten für Listen | Vertikale Karten mit `Border` |
| Harte `WidthRequest`/`HeightRequest` | `MinimumWidthRequest` / `MinimumHeightRequest` und flexible Layouts |

## UI-Verifizierungen

### Ungelesen-Dashboard (issue-23)
- Geprüft auf 390 × 844 pt (Windows-Handy-Fenster).
- Runder Refresh-Button, Funnel-Filter-Button und "Alles gelesen"-Pill entsprechen dem Design-Entwurf.
- Touch-Ziele der Header-Buttons: 44 × 44 pt.
- Dark-Mode-Farben über `AppThemeBinding`.
- Kartengestützte Artikelliste mit `ArticleCardView`, Bild rechts, Teasertext und Aktions-Icons.

### Später-Liste
- Geprüft auf 390 × 844 pt (Windows-Handy-Fenster).
- Verwendet dieselbe `ArticleCardView` wie das Ungelesen-Dashboard.
- Lesezeichen-Icon füllt sich, wenn der Artikel gespeichert ist.
- Iteration issue-25: Erneute Prüfung gegen `design-draft/stitch_local_rss_feed_reader/f_r_sp_ter_bewahren/screen.png` und `..._dark_mode/screen.png`; AGENTS.md-Regeln erfüllt (44 × 44 pt Touch-Targets, `AppThemeBinding`, `CollectionView` füllt `Grid`-Row `*`, kein verschachteltes Scrollen). Abweichungen zum Entwurf (Info-Banner, Suchfeld, Kategorie-Chips) sind in `test-results.md` dokumentiert.
- Iteration issue-25 (2): Laufzeit-Verifikation am Windows-Handy-Fenster 390 × 844 pt durchgeführt — Bewahren auf `Ungelesen` (Bookmark-Icon füllt sich, Artikel erscheint unter `Später`), Entfernen auf `Später` (Artikel verschwindet, `EmptyView` bei leerer Liste), Bookmark-Toggle in der `ArticleDetailPage`-Bottom-Bar (Icon füllt sich/leert sich) sowie Light- und Dark-Screenshots. Screenshots: `docs/help/anwendung/screenshots/issue-25/manual-*.png`.

### Einstellungen (issue-26)
- Geprüft gegen `design-draft/stitch_local_rss_feed_reader/einstellungen_filter/screen.png` und `einstellungen_filter_dark_mode/screen.png`.
- Fünf Sektions-Karten als `Border` + `RoundRectangle 12` mit `AppThemeBinding SurfaceContainer` im `ScrollView` unter `Grid RowDefinitions="Auto,*"` — Formularseite, kein `CollectionView`, keine Scroll-Verschachtelung.
- Slider 1–365 (`MinimumHeightRequest="44"`) mit `DragCompletedCommand`; Chips via `FlexLayout Wrap="Wrap"` + `BindableLayout`, ×-Button 44 × 44 pt.
- Optionszeilen (Abruf-Intervall, Verzögerung, Ruhezeiten) per `IsEnabled`-Binding + `DataTrigger` (`Opacity` 0,4) ausgegraut; `TimePicker` ×2, drei `Picker`.
- Alle Texte aus `AppResources`, Dark Mode ausschließlich über `AppThemeBinding`, `Shell.NavBarIsVisible="False"`.
- Laufzeit-Verifikation am Windows-Handy-Fenster 390 × 844 pt: alle fünf Sektionen sichtbar (Light + Dark), Theme-Wechsel über „Farbschema" wirkt sofort und wird persistiert, Verzögerung „Sofort" markiert Artikel direkt beim Öffnen. Screenshots: `test-results/issue-26-manual-*.png`; Details in `test-results.md`.
- Scope-Abweichungen zum Entwurf (nicht umgesetzt): „Lokalen Cache leeren", Sektion „Datenbank & Datensicherung", Ruhezeiten-Status-Badge, „Änderungen gespeichert"-Toast.

### Feeds & Einstellungen – Benachrichtigungen (issue-27)
- Geprüft auf 390 × 844 pt (Windows-Handy-Fenster, `GetWindowRect`-verifiziert), Interaktion via UI Automation.
- `FeedsPage`: neues Optionspaar „Benachrichtigungen" (Label + Hint) mit `Switch` rechts, `MinimumWidth/HeightRequest="44"`, `SemanticProperties.Description`; oberhalb von „Speichern", kein zusätzlicher Text-Button.
- Edit-Flow (ActionSheet „Feed-Aktionen" → „Bearbeiten") lädt den Flag in den Switch; Speichern persistiert und Reload zeigt den Wert.
- `SettingsPage`: Zeile „Sammel-Benachrichtigung" + Hint in der Karte „Benachrichtigungen & Ruhezeiten", per `IsEnabled`-Binding/`DataTrigger` mit den übrigen Optionen ausgegraut; Persistenz über App-Neustart verifiziert.
- Keine horizontalen Tabellen, keine Scroll-Verschachtelung; alle neuen Texte aus `AppResources` (EN/DE), Dark Mode per `AppThemeBinding`.
- Screenshots: `test-results/issue-27/manual-*.png`; Details in `test-results.md`.
- **Offen:** iOS-Simulator-Verifikation (`net10.0-ios`, `scripts/iOS-Deployment.ps1`) ist nur auf macOS möglich und steht als Folgeaufgabe aus.

### Artikeldetailansicht (issue-24)
- Geprüft gegen `design-draft/stitch_local_rss_feed_reader/artikel_lesemodus/screen.png` und `artikel_lesemodus_dark_mode/screen.png`.
- Implementiert als `Grid` mit `RowDefinitions="Auto,Auto,*,Auto,Auto"`; der `WebView` füllt die verbleibende Höhe und übernimmt das Scrolling nativ.
- Keine verschachtelten `CollectionView`/`ScrollView`.
- Touch-Ziele der Floating Bottom Action Bar und des `Switch` sind mindestens 44 × 44 pt (`WidthRequest="44"` / `HeightRequest="44"` bzw. `MinimumWidthRequest="44"` `MinimumHeightRequest="44"`).
- Farben und Symbole nutzen durchgehend `AppThemeBinding` (`Light...` / `Dark...`).
- `WebView`-Inhalte verwenden `color-scheme: light dark` und `prefers-color-scheme` CSS für den Dark Mode.
- Getestete Größen: 390 × 844 pt und 768 × 1024 pt.
- Iteration 2: Status-Pille mit grünem Dot, "Auto-Gelesen (X s)"-Beschriftung, Header per `FlexLayout`, 44 × 44 pt Touch-Targets für "Im Browser öffnen" und "Alle gelesen", Safe-Area-Padding am unteren Action-Bar, DI-Registrierung und Auto-Mark-Timer-Disposal ergänzt.
- **Offen:** UI-Review-Screenshots (Light/Dark, 390 × 844 pt) müssen noch in diesen Abschnitt eingefügt werden (siehe `continue.md`).
