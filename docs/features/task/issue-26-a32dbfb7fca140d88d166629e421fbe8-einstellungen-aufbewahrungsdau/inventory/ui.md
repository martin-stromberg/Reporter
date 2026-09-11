# UI und Design-Entwurf — Bestandsaufnahme

## `SettingsPage` (Ist-Zustand: Platzhalter)

Dateien: `src/Reporter/Views/SettingsPage.xaml`, `src/Reporter/Views/SettingsPage.xaml.cs`

- `ContentPage` mit `Title="{x:Static strings:AppResources.PageTitleSettings}"`, `Shell.NavBarIsVisible="False"`.
- Layout: `Grid RowDefinitions="Auto,*"`, `Padding="16,8"`, `RowSpacing="12"`; Headline-`Label` mit `HeadlineStyle`; `ScrollView` in Row 1 mit einer einzigen `Border`-Karte (`RoundRectangle 12`, `AppThemeBinding` auf `LightSurfaceContainer`/`DarkSurfaceContainer`), die `PageTitleSettings` + `PlaceholderSettings` anzeigt.
- Code-Behind: injiziert `SettingsViewModel` als `BindingContext`, führt in `OnAppearing` `LoadCommand.Execute(null)` aus.
- Navigation: `SettingsPage` ist fünfter Tab in `AppShell.xaml.cs` (Zeilen 34–35, programmatisch aufgebaute `TabBar`: Unread, Feeds, Later, Categories, Settings). Route `articledetail` ist die einzige registrierte (`AppShell.xaml.cs` Zeile 20); `AppShell.xaml` selbst ist leer.

## Etablierte UI-Muster (andere Pages)

Alle Pages folgen demselben Grundgerüst:

- **Layout:** `Grid RowDefinitions="Auto,*"`, `Padding="16,8"`, `RowSpacing="12"` — Header `Auto`, Inhalt füllt `*`.
- **Header:** `Label` mit `HeadlineStyle` + `{x:Static strings:AppResources.PageTitle*}`; `Shell.NavBarIsVisible="False"`.
- **Listen:** kartenbasierte `CollectionView` (`Border` mit `RoundRectangle 12`, `AppThemeBinding`-Hintergrund), `EmptyView` mit lokalisiertem Platzhalter-String; kein verschachteltes `ScrollView`/`CollectionView` (Ausnahme Ist-Zustand: `SettingsPage` nutzt `ScrollView` ohne CollectionView).
- **Aktionen:** `TapGestureRecognizer` (Command-Binding oder `Tapped`-Event) + `DisplayActionSheetAsync` im Code-Behind — `CategoriesPage.OnCategoryTapped` (`CategoriesPage.xaml.cs` Zeile 46), `FeedsPage.OnFeedTapped` (`FeedsPage.xaml.cs` Zeile 39–46), `UnreadPage.OnFilterClicked` (`UnreadPage.xaml.cs` Zeile 46–54). Kein `SwipeView` im Projekt.
- **Touch-Ziele:** `Border` mit `WidthRequest`/`HeightRequest` 44 bzw. `MinimumHeightRequest`/`MinimumWidthRequest` 44 (z. B. `UnreadPage.xaml` Zeilen 22–24, 74–75).
- **Pull-to-Refresh:** `RefreshView` um `CollectionView` (`UnreadPage`, `FeedsPage`), gebunden an `IsSyncing` + Refresh-Command.
- **Formulare:** `Border`-Karte mit `VerticalStackLayout`, `Entry` + `ReturnCommand`, `Picker` mit `ItemDisplayBinding`, Fehler-`Label` mit `LightError`/`DarkError` + `IsVisible="{Binding HasError}"`, `Button` mit `SaveCommand` (`FeedsPage`, `CategoriesPage`).
- **Paging:** `RemainingItemsThreshold` + `RemainingItemsThresholdReachedCommand` (`UnreadPage`).
- **Templates:** wiederverwendbares `ArticleCardView` (`src/Reporter/Views/ArticleCardView.xaml`) mit `x:DataType` + `x:Reference PageRoot`-Binding an Page-Commands.
- **Icons:** `Path`-Elemente mit `Data`-Geometrien, `AppThemeBinding`-Stroke/Fill — keine Icon-Font.

## Theming und Ressourcen

- `src/Reporter/Resources/Styles/Colors.xaml`: vollständiges `Light*`/`Dark*`-Farbsystem (Surface-/Container-/Primary-/Secondary-/Error-/Status-/Text-Palette, `BookmarkGold` u. a.), plus `AppThemeBinding`-Brushes (`PrimaryBrush`, `SurfaceBrush`, `SurfaceContainerBrush`, `OutlineBrush` u. a.).
- `src/Reporter/Resources/Styles/Styles.xaml`: Label-Styles `DisplayStyle`, `HeadlineStyle`, `HeadlineSmallStyle`, `BodyStyle`, `BodySmallStyle`, `MetaStyle`, `UiLabelStyle`; Converter `StringNotEmptyToBoolConverter` (`src/Reporter/Converters/StringNotEmptyToBoolConverter.cs`).
- Dark Mode ausschließlich über `AppThemeBinding` — `Application.UserAppTheme` wird nicht gesetzt; die App folgt dem System.
- Fonts: Inter (Regular/Medium/SemiBold) + Newsreader (Regular/Medium/SemiBold/Italic), registriert in `MauiProgram` Zeilen 27–36.

## Lokalisierung

`AppResources.resx` + `AppResources.de.resx` (`src/Reporter.Core/Resources/Strings/`), `PublicResXFileCodeGenerator` → `AppResources.Designer.cs`. Zugriff in XAML via `{x:Static strings:AppResources.<Key>}` (XML-Namespace `strings` auf `Reporter.Core.Resources.Strings;assembly=Reporter.Core`). Bestehende Settings-bezogene Keys: `PageTitleSettings`, `TabSettings`, `PlaceholderSettings` — keine weiteren Settings-Labels vorhanden.

## Mobile-UI-Regeln aus `AGENTS.md`

Für jede nutzerseitige MAUI-Seite/-Control/-Flow verpflichtend:

1. Abgleich mit `design-draft/.../screen.png` des jeweiligen Flows.
2. Layout-Verifikation auf mobilem Formfaktor (Windows-Handysize-Fenster 390 × 844 pt — `App.CreateWindow` setzt dies bereits — oder iOS-Simulator-Screenshot via `scripts/iOS-Deployment.ps1`).
3. Keine horizontalen Datentabellen, keine mehreren Text-Buttons in einer Zeile; kartenbasierte `CollectionView` mit `TapGestureRecognizer` + `DisplayActionSheet` oder `SwipeView`.
4. `CollectionView`/`ScrollView` nicht verschachteln; Liste füllt Rest via `Grid`-Row `*`.
5. Touch-Ziele ≥ 44 × 44 pt; Dark Mode via `AppThemeBinding`.
6. Manuelle UI-Verifikation dokumentieren oder automatisierten UI-Test ergänzen; ohne automatisierten Test Screenshot + getestete Größen in `docs/help/anwendung/mobile-ui-design.md` oder `test-results.md` festhalten.

Zusätzlich: `.\scripts\Run-StaticChecks.ps1` muss vor Abschluss mit Exit-Code 0 durchlaufen (Format, Security, Static-Analysis-Build).

## Design-Entwurf `einstellungen_filter`

Verzeichnisse: `design-draft/stitch_local_rss_feed_reader/einstellungen_filter/` (Light) und `einstellungen_filter_dark_mode/` (Dark) — je `screen.png` (Screenshot) + `code.html` (Tailwind-Referenz). Beide Varianten enthalten identische fünf Sektionen:

1. **„Aufbewahrungsdauer & Speicher"** — Slider „Gelesene Artikel aufbewahren" (min 1 / max 365, Wert 30 Tage, Markierungen 1/90/180/365), Hinweistext „Ungelesene und mit Sternchen markierte gespeicherte Artikel bleiben dauerhaft erhalten.", Button „Lokalen Cache leeren" mit Größenanzeige (48 MB).
2. **„Keyword-Filter (Blacklist)"** — Eingabefeld „Schlagwort eingeben…" mit Button „+ Hinzufügen"; aktive Keywords als Chips/Pills (Beispiele „Werbung", „Gewinnspiel", „Sponsoring") mit Entfernen-Button (×); Toggle „Teilwort & Case-Insensitive" (fest aktiviert dargestellt).
3. **„Synchronisation & Lesefluss"** — Toggle „Automatische Hintergrund-Aktualisierung"; Auswahlliste „Abruf-Intervall" (Alle 15 Minuten / Alle 30 Minuten / Stündlich / Alle 4 Stunden, vorausgewählt 30); Toggle „Automatisch als gelesen markieren"; Auswahlliste „Verzögerung bis Markierung" (Sofort / 1 s / 3 s / 5 s, vorausgewählt 3).
4. **„Benachrichtigungen & Ruhezeiten"** — Toggle „Push-Benachrichtigungen"; Ruhezeit-Block mit „VON"/„BIS"-Zeitfeldern (Beispiel 22:00–07:00, über Mitternacht) und Status-Badge „Aktiv".
5. **„Datenbank & Datensicherung"** — SQLite-Statuszeile (Einträge, Größe, Badge „Normal"), Buttons „OPML Export" und „JSON Backup".

Interaktion im Entwurf: Toast „Änderungen gespeichert" als Feedback; deaktivierte Optionszeilen werden ausgegraut (`opacity 0.4`, `pointer-events: none`). Karten-Optik (`bg-surface-card`, `rounded-xl`), Sektions-Header mit Icon + Label — deckt sich mit dem Border-Karten-Muster der bestehenden Pages.

Weitere vorhandene Entwürfe im selben Verzeichnis: `ungelesen_dashboard`, `feeds_health_status`, `f_r_sp_ter_bewahren`, `artikel_lesemodus` (je Light/Dark) sowie `DESIGN.md`-Dateien (`editorial_feed`, `reporter_editorial_dark`) und ein Logo-Referenz-Screenshot.
