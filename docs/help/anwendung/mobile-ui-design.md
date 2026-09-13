<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

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
- Nacharbeiten (continue.md): Berechtigungsstatus unterscheidet jetzt `NotDetermined` (neutrale Zeile + Button **Benachrichtigungen erlauben**, 44-pt-Target, `SemanticProperties.Description`) und `Denied` (Zeile + **Einstellungen öffnen**); auf Nicht-iOS sind die Benachrichtigungs-Schalter (`SettingsPage`-Karte und `FeedsPage`-Formular) per `IsEnabled="{Binding NotificationsSupported}"` deaktiviert, auf Opazität 0,4 abgedunkelt und mit der Zeile „derzeit nur auf iOS verfügbar" versehen — unter Windows damit im laufenden Fenster sichtbar. Statisch geprüft (Muster identisch zu den verifizierten Zeilen); Laufzeit-Screenshots der neuen Zeilen stehen mit der iOS-Verifikation aus.
- **Offen:** iOS-Simulator-Verifikation (`net10.0-ios`, `scripts/iOS-Deployment.ps1`) ist nur auf macOS möglich und steht als Folgeaufgabe aus.

### Offline-Indikatoren & Mehrsprachigkeit (issue-28)
- Statische XAML-Prüfung der Änderungen an `UnreadPage.xaml`, `FeedsPage.xaml`, `LaterPage.xaml`, `ArticleDetailPage.xaml` und `ArticleCardView.xaml` gegen die AGENTS.md-Regeln: Offline-Banner als `Border` + `RoundRectangle 8` mit `AppThemeBinding` (`SurfaceSubtle`/`TextSecondary`), `DataTrigger` auf `IsOnline == false`; Sync-Button auf `UnreadPage` offline auf Opazität 0,4 gedimmt; keine horizontalen Tabellen, keine neuen Text-Button-Reihen, keine Scroll-Verschachtelung; Touch-Ziele unverändert ≥ 44 × 44 pt; alle neuen Texte aus `AppResources` (EN/DE).
- `ArticleCardView`: neues `IsOnline`-`BindableProperty` (Default `true`), Thumbnail-`Border` wird offline per `DataTrigger` ausgeblendet.
- `ArticleDetailPage`: neue Grid-Row für Offline-/Fehlerhinweis oberhalb des `WebView`; `Navigating`-Handler bricht externe Navigation offline ab.
- **Offen — manuelle Laufzeit-Verifikation steht aus:** Die Szenarien erfordern echtes Umschalten Online → Offline → Online auf einem Gerät bzw. im 390 × 844-pt-Fenster und werden manuell nachgeholt. Die vollständige Checkliste der zu verifizierenden Szenarien (Offline-Start, gedimmter Sync-Button, Offline-Banner, Link-Neutralisierung und `<img>`-Entfernung im WebView, „Im Browser öffnen"-Guard, Laufzeit-Statuswechsel, Sprachverhalten DE/EN/Fallback, iOS-Simulator-Lauf) ist in `test-results.md` im Abschnitt **„Issue #28 → Manuelle UI-Verifikation (ausstehend)"** dokumentiert; die ViewModel-/Service-Logik dahinter ist durch 19 neue Unit-Tests abgedeckt und der Release-Build ohne Befund.

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

### Feed-Suche und Hinzufügen-Flow (issue-59)
- Statische XAML-Prüfung der Änderungen an `FeedsPage.xaml` gegen die AGENTS.md-Regeln und den Design-Entwurf `design-draft/stitch_local_rss_feed_reader/feeds_health_status/screen.png`: Trefferliste als kartenbasierte `CollectionView` (`Border` + `RoundRectangle 12`, `AppThemeBinding` `SurfaceContainer`) in `Grid`-Row `*`; `TapGestureRecognizer` + `DisplayAlertAsync`-Bestätigung; „Suchen"- und „Speichern"-Button vertikal gestapelt (keine Text-Button-Reihe); Trefferkarte `MinimumHeightRequest="44"`; `EmptyView` „Keine Feeds gefunden."; Attribution „powered by feedsearch.dev" als Footer sichtbar; Offline-Hint per `DataTrigger` (`IsOnline == false`), Suchen-Button per `CanExecute` deaktiviert; Feed-Liste und Trefferliste blenden sich wechselseitig ein (`ShowSearchResults`).
- Iteration 2 (Review-Nachbearbeitung): sichtbarer Rückweg aus der Trefferansicht als eigener Button „Zurück zu meinen Feeds" (`CloseSearchResultsCommand`, `MinimumHeightRequest="44"`, volle Breite — keine Button-Reihe) unterhalb der Attribution; Fehlerhinweis differenziert — Direkt-Hinzufügen-Angebot (`FeedSearchUnavailable`) nur bei gültiger URL, sonst Retry-Hinweis (`FeedSearchUnavailableRetry`); Direkt-Hinzufügen-Dialog nennt die eingegebene Adresse (`{0}`-Platzhalter in `FeedSearchNoResultsAddUrl`).
- Alle neuen Texte aus `AppResources` (EN/DE); Dark Mode ausschließlich über `AppThemeBinding`.
- **Laufzeit-Verifikation durchgeführt** am Windows-Handy-Fenster 390 × 844 pt (`GetWindowRect`-verifiziert, unpackaged `win-x64`-Release-Build), Interaktion via UI Automation (`test-results/issue-59/uia.ps1`): Domain-Suche `tagesschau.de`/`heise.de` → Trefferkarten + Attribution + Zurück-Button; Treffer-Tap → „Feed abonnieren?"-Dialog → „Ja" → Feed in Liste und SQLite-persistiert; URL ohne Treffer (`https://example.com/`) → Direkt-Hinzufügen-Dialog mit Adresse → Titel-Vorbelegung `example.com` → „Speichern"; Suche nicht erreichbar → Hinweis + Fallback-Dialog; Dublette → „Ein Feed mit dieser URL existiert bereits."; „Zurück zu meinen Feeds" schließt die Trefferansicht. Light- und Dark-Screenshots (`settings.theme` via DB umgeschaltet): `test-results/issue-59/manual-*.png`; Details in `test-results.md`.
- Nicht interaktiv verifizierbar (durch ViewModel-/Service-Tests abgedeckt): Offline-Pfad (`Disable-NetAdapter` ohne Adminrechte nicht möglich) und Titel-Auflösung eines titel-losen Treffers nach erstem Sync (localhost-Discovery aus dem App-Prozess nicht erreichbar).
- Iteration 3 (Listenansicht + Bottom-Sheet): `FeedsPage` auf reine Listenansicht umgebaut — permanente Formularkarte entfernt, Row 0 nur noch Primär-Button „+ Feed per URL hinzufügen" (44 pt), Hinzufügen-/Bearbeiten-Formular als Bottom-Sheet-`Grid`-Overlay (Backdrop-Tap schließt, oberer `Border`-Radius, `DataTrigger` auf `ShowAddForm`/`IsEditMode`, Titel „Feed per URL hinzufügen"/„Feed bearbeiten"); Seiten-`ErrorMessage` außerhalb des Sheets; Fokus auf `NewUrlEntry` beim Öffnen via `Dispatcher`; `OnBackButtonPressed` schließt offenes Sheet; Feed-Kontextmenü um „Umbenennen" (`DisplayPromptAsync` mit Vorbelegung) und „Kategorie ändern" (`DisplayActionSheetAsync` inkl. „—"/`CategoryNone`) erweitert; Dateinamen-Fallback-Titel via `FeedTitleFallback` (Direkt-Hinzufügen, Treffer-Abonnieren, Sync-Platzhalter-Auflösung). **Laufzeit-Verifikation durchgeführt** am Windows-Handy-Fenster 390 × 844 pt, Dark + Light: Sheet-Open/-Close (Button, Backdrop, System-Zurück via XButton1), Suche → Abonnieren, Direkt-Hinzufügen mit Dateinamen-Titel (`mein-feed.xml`), Umbenennen, Kategorie-Wechsel, Edit-Modus, Fehler im offenen Sheet (ungültige URL, Dublette, `FeedSearchUnavailableRetry`), Sync-Platzhalter-Auflösung (`heise-Rubrik-IT-atom.xml` → „heise online IT"). Screenshots: `test-results/issue-59/manual-2-*.png` (Dark), `manual-3-*.png` (Light); Details in `test-results.md`.
- Iteration 4 (Review-Nacharbeiten): Add-Sheet mit zweitem, offline aktivem Button „URL direkt hinzufügen" (`ButtonDirectAdd` → `DirectAddCommand`, persistiert ohne Suche mit `FeedTitleFallback`-Titel); „Suchen" und „URL direkt hinzufügen" im Edit-Modus per `DataTrigger` auf `IsEditMode` ausgeblendet, `SearchCommand` im Edit-Modus deaktiviert (verhindert lautloses Verwerfen auch per Enter/`ReturnCommand`); Kategorie-ActionSheet mit Klartext „Keine Kategorie" statt „—" und Cancel-Kollision durch Filterung gleichnamiger Kategorien ausgeschlossen; `PropertyChanged`-Fokus-Handler in `FeedsPage` an `OnAppearing`/`OnDisappearing` abonniert/abgemeldet. **Laufzeit-Verifikation durchgeführt** am Windows-Handy-Fenster 390 × 844 pt, Dark + Light: Direkt-Add persistiert `direkt-add.xml` ohne Dialog (SQLite-geprüft), Dubletten-Fehler im offenen Sheet, Kategorie-Sheet mit „Keine Kategorie", Edit-Sheet ohne Suchen-/Direkt-Add-Button. Screenshots: `test-results/issue-59/manual-4-*.png`; Details in `test-results.md`.
- Iteration 5 (Review-Nacharbeiten): Sheet-Offline-Hinweis (`FeedSearchOfflineHint`) im Edit-Modus per `DataTrigger` auf `IsEditMode` ausgeblendet (er versprach Aktionen, die dort nicht existieren); `MinimumHeightRequest="44"` an „Suchen"/„URL direkt hinzufügen"/„Speichern" ergänzt; URL-Meta-Zeile der Trefferkarte bei leerem `Title` ausgeblendet (keine doppelte URL-Anzeige); Kategorie-ActionSheet disambiguiert gleichnamige Kategorien per Zählsuffix („News", „News (2)") mit positionsbasierter Auflösung. **Laufzeit-Verifikation durchgeführt** am Windows-Handy-Fenster 390 × 844 pt (Dark): Duplikat-Optionen einzeln wählbar und korrekt zugeordnet (SQLite-geprüft), titel-lose Trefferkarte zeigt URL nur einmal, Edit-Sheet ohne Such-/Direkt-Add-Buttons und ohne Offline-Hinweis, alle Buttons ≥ 44 pt. Screenshots: `test-results/issue-59/manual-5-*.png`; Details in `test-results.md`.
