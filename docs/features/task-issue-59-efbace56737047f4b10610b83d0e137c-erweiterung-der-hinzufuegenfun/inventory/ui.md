<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# UI — Bestandsaufnahme

## `FeedsPage.xaml`
Datei: `src/Reporter/Views/FeedsPage.xaml` (278 Zeilen)

`ContentPage` mit `Shell.NavBarIsVisible="False"`, Wurzel-`Grid` `RowDefinitions="Auto,*"`, `Padding="16,8"`, `RowSpacing="12"`. Die Seite ist aktuell eine kombinierte Formular-plus-Liste-Ansicht.

### Bereich 1 — `VerticalStackLayout` (Grid.Row 0, Zeilen 14–112)

| Element | Zeilen | Binding / Inhalt |
|---------|--------|------------------|
| `Label` Seitentitel | 15–16 | `PageTitleFeeds`, `HeadlineStyle` |
| **`Border`-Formularkarte** (dauerhaft sichtbar) | 17–79 | `Padding=16`, `RoundRectangle 12`, `AppThemeBinding` `LightSurfaceContainer`/`DarkSurfaceContainer` |
| ↳ `Entry` `NewUrl` | 21–24 | `PlaceholderFeedSearch`, `Keyboard="Url"`, `ReturnCommand="{Binding SearchCommand}"` |
| ↳ `Entry` `NewTitle` | 25–27 | `PlaceholderFeedTitle`, `ReturnCommand="{Binding SaveCommand}"` — laut Anforderung zu entfernen |
| ↳ `Picker` `Categories`/`SelectedCategory` | 28–31 | `LabelFeedCategory`, `ItemDisplayBinding="{Binding Name}"` — Verbleib laut Anforderung offen |
| ↳ `Grid` Notification-Switch | 32–52 | `FeedNotificationsLabel`/`FeedNotificationsHint`, `Switch` `FeedNotificationsEnabled` (44×44, `SemanticProperties.Description`), `IsEnabled`/`Opacity`-Trigger auf `NotificationsSupported` — Verbleib offen |
| ↳ `Border` iOS-Hinweis | 53–66 | `NotificationsIosOnlyHint`, `DataTrigger NotificationsSupported=False` → `IsVisible` |
| ↳ `Label` `ErrorMessage` | 67–70 | `IsVisible="{Binding HasError}"`, `LightError`/`DarkError` |
| ↳ `ActivityIndicator` | 71–73 | `IsVisible`/`IsRunning="{Binding IsSearching}"` |
| ↳ `Button` „Suchen" | 74–75 | `ButtonSearch`, `SearchCommand` |
| ↳ `Button` „Speichern" | 76–77 | `ButtonSave`, `SaveCommand` — laut Anforderung zu entfernen |
| `Border` Offline-Banner | 80–93 | `OfflineHint`, `DataTrigger IsOnline=False` → `IsVisible` |
| `Label` `FeedSearchOfflineHint` | 94–103 | `DataTrigger IsOnline=False` → `IsVisible` |
| `Label` `SearchErrorMessage` | 104–107 | `IsVisible="{Binding HasSearchError}"` |
| `Label` `SyncErrorMessage` | 108–111 | `IsVisible="{Binding HasSyncError}"` |

### Bereich 2 — Trefferansicht (Grid.Row 1, Zeilen 114–184)

`Grid RowDefinitions="*,Auto,Auto"`, `IsVisible=False` + `DataTrigger ShowSearchResults=True` → sichtbar; **ersetzt** aktuell die Feed-Liste (gleiche Grid-Zeile).

- `CollectionView` `SearchResults` (122–173): Karten-`DataTemplate` mit `TapGestureRecognizer Tapped="OnSearchResultTapped"` (`CommandParameter="{Binding .}"`), Titel-`Label` (Trigger `StringNotEmptyToBoolConverter`: `Title` sonst `FeedUrl`), `Description` (Trigger leer → `IsVisible=False`), Meta-Zeile `SiteName` • `SiteUrl`, `FeedUrl`-Zeile. `EmptyView` = `FeedSearchNoResults`.
- `Label` `FeedSearchAttribution` (174–178), `Button` `ButtonCloseSearchResults` → `CloseSearchResultsCommand` (179–183, `MinimumHeightRequest=44`).

### Bereich 3 — Feed-Liste (Grid.Row 1, Zeilen 186–276)

`RefreshView` (`IsRefreshing="{Binding IsSyncing}"`, `Command="{Binding RefreshAllCommand}"`), `DataTrigger ShowSearchResults=True` → `IsVisible=False`. Darin `CollectionView` `Feeds` mit `EmptyView` `PlaceholderFeeds`.

Listeneintrag (Karte, Zeilen 197–273): `TapGestureRecognizer Tapped="OnFeedTapped"` (`CommandParameter="{Binding .}"`), `Title`-`Label`, `BoxView` Status-Punkt (12×12, Trigger auf `HealthStatus` `OK`/`Warning`/`Error` → `LightStatus*`/`DarkStatus*`), Meta-Zeile `CategoryName` (`TargetNullValue='—'`) • `LastCheckedAt {0:g}`, `LabelFeedUnreadCount` + `UnreadCount`, Status-Text-`Label` mit `HealthStatus*Label`-Triggern.

**Nicht vorhanden:** „+"-Button / `OpenAddForm`-Mechanismus, Sichtbarkeitssteuerung der Formularkarte, Bottom-Sheet/Modal-Struktur, Kontextmenü-Einträge „Umbenennen"/„Kategorie ändern".

## Design-Entwurf (UI-Referenz)

Verzeichnis: `design-draft/stitch_local_rss_feed_reader/feeds_health_status/`
- `screen.png` — Referenz-Screenshot der Feeds-Seite (Health-Status-Variante).
- `code.html` — HTML/CSS/JS-Prototyp (Tailwind, deutsch). Relevante Elemente:
  - `id="btn-open-add-feed"` (Zeilen 9–12): primärer Button voller Breite über der Liste, Beschriftung „+ Feed per URL hinzufügen", Icon `add_circle`.
  - `id="modal-add-feed"` (Zeilen 313–348): **Bottom-Sheet** (`fixed inset-0 flex items-end`, Karte `rounded-t-2xl`), Titel „Feed per URL hinzufügen", Schließen-Button (`btn-close-add-modal`), Feld `feed-url-input` (Label „RSS / Atom Feed URL"), `select` `feed-category-select` (Label „Kategorie (optional)"), Submit-Button `btn-submit-feed` „Feed abonnieren". Hinweis: Der Entwurf enthält eine optionale Kategorie-Auswahl im Sheet — die Anforderung nennt nur URL + „Suchen" (Offene Frage 1).
  - `id="modal-edit-feed"` (Zeilen 349–379): Bottom-Sheet „Feed anpassen" mit Feldern `edit-feed-title` („Name des Feeds") und `edit-feed-url` („Feed-URL"), Speichern-Button `btn-save-edit-feed` „Änderungen speichern". Wird aus den Karten via `openEditModal(title, url)` geöffnet (Titel vorbelegt).
  - Pro Feed-Karte Quick-Action-Zeile mit Icon-Buttons `refresh`, `edit` (title „Umbenennen / Bearbeiten"), `delete` sowie `more_vert`-Menü-Button (`data-feed-action="menu"`).
  - `id="toast-notification"` (Zeilen 380–384): Toast-Benachrichtigungen.
  - JS (Zeilen 386–497): `openModal`/`closeModal` (Slide-up-Animation), Backdrop-Click schließt Modale, `confirmDeleteFeed` via `window.confirm`.

Dark-Mode-Pendant: `design-draft/stitch_local_rss_feed_reader/feeds_health_status_dark_mode/` (`code.html` mit identischem `modal-add-feed`, dunkler Farbpalette). Die aktuelle `FeedsPage.xaml` nutzt bereits `AppThemeBinding` für alle Surface-/Textfarben.

Hinweis zu AGENTS.md (Mobile UI Design Review): Touch-Ziele ≥44×44 pt, keine verschachtelten `CollectionView`/`ScrollView` (aktuell erfüllt: `RefreshView` > `CollectionView`, kein `ScrollView`), Liste füllt Resthöhe via `Grid.Row="*"`, Vergleich mit `screen.png` und dokumentierte manuelle Verifikation (390×844 pt) sind bei Umsetzung erforderlich.
