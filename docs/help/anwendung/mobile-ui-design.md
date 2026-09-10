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
