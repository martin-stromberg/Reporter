<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: Unaufdringliches Hinzufügen-Formular, Dateiname als Titel-Fallback, Kontextmenü-Aktionen „Umbenennen“ und „Kategorie ändern“

## Übersicht

Die `FeedsPage` wird von einer kombinierten Formular-plus-Liste-Seite zu einer reinen Listenansicht umgebaut: Ein primärer „+“-Button öffnet das Hinzufügen-Formular als Bottom-Sheet-Overlay (Referenz: `design-draft/stitch_local_rss_feed_reader/feeds_health_status/code.html`, `modal-add-feed`), das nur noch `Entry NewUrl` + „Suchen“ enthält. Feeds ohne Titel erhalten den Dateinamen (letztes Pfadsegment) der Feed-URL als Platzhalter-Titel, den `FeedSyncService` beim ersten Sync weiterhin durch den echten Feed-Titel auflöst. Das Feed-Kontextmenü (`DisplayActionSheetAsync`) erhält die Aktionen „Umbenennen“ (`DisplayPromptAsync`) und „Kategorie ändern“ (`DisplayActionSheetAsync` über `Categories`).

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| „+“-Formular | **In-Page-Bottom-Sheet-Overlay** auf `FeedsPage`: halbtransparenter Backdrop-`BoxView` + unten angedockte `Border`-Karte (`VerticalOptions="End"`), als `Grid`-Overlay mit `Grid.RowSpan="2"` über Titel- und Listenbereich gelegt; Sichtbarkeit per `DataTrigger` auf `ShowAddForm` | `CommunityToolkit.Maui` (Popup/BottomSheet) ist nicht im Projekt — ein neues NuGet-Paket nur für das Sheet wäre unverhältnismäßig. `Navigation.PushModalAsync` erforderte eine eigene `ContentPage` inkl. DI-/Navigation-Verdrahtung und bildet das Bottom-Sheet-Aussehen ohnehin nicht nativ ab. Das Overlay bildet `modal-add-feed` (`fixed inset-0`, `items-end`) am treuesten ab, hält die Treffer-`CollectionView` unverschachtelt (AGENTS.md) und lässt den kompletten Such-Flow im bestehenden ViewModel. |
| Position des „+“-Buttons | **Primär-Button voller Breite über der Liste** („+ Feed per URL hinzufügen“, `Grid.Row 0` unter dem Seitentitel) | Entspricht dem Design-Entwurf `btn-open-add-feed` (volle Breite, `add_circle`-Icon) statt eines runden Header-Icons; einfachste, am Design verifizierbare Variante. |
| Trefferliste relativ zum Sheet | **Trefferansicht bleibt ein Seitenbereich** (ersetzt weiterhin die Feed-Liste in `Grid.Row 1`); das Sheet schließt sich automatisch, sobald `ShowSearchResults = true` wird | „Suchergebnisse werden gelistet und wie bisher hinzugefügt“ — die bestehende, bereits verifizierte Treffer-UI wird 1:1 weitergenutzt. Eine `CollectionView` im Sheet hätte eine Höhenbegrenzung und neue Scroll-Logik erfordert; ein offenes Overlay würde die Treffer verdecken. |
| „Umbenennen“-Dialog | **`DisplayPromptAsync`** im Code-Behind mit Vorbelegung des aktuellen Titels, Routing an `RenameFeedAsync` im ViewModel | Plattform-Standard für einzelne Texteingaben; wäre zwar ein neues Pattern im Codebestand, ist aber die einfachste zulässige Lösung — ein eigener XAML-Dialog wäre Overhead ohne Mehrwert. ViewModel bleibt UI-frei (gleiche Grenze wie `ConfirmDirectAddAsync`). |
| „Kategorie ändern“-Dialog | **`DisplayActionSheetAsync`** mit den Namen aus `Categories` (inkl. `CategoryNone`-Pseudo-Eintrag) im Code-Behind, Routing an `ChangeFeedCategoryAsync` | Konsistent mit den bestehenden `OnFeedTapped`-/`OnCategoryTapped`-/`OnFilterClicked`-Mustern; `Categories` ist bereits geladen. Skalierungsrisiko bei sehr vielen Kategorien vom Anwender akzeptiert (geklärte Entscheidung). |
| Titel-Fallback (Dateiname) | **Zentraler statischer Helper** `FeedTitleFallback.GetFallbackTitle(string url)` in `Reporter.Core`: letztes nicht-leeres, URL-dekodiertes Pfadsegment → `Host` → komplette URL; genutzt von `SubscribeResultAsync`, dem Direkt-Hinzufügen-Pfad und der `FeedSyncService`-Platzhalter-Erkennung | Die Regel wird an drei Stellen benötigt (Suchtreffer ohne Titel, Direkt-Hinzufügen, Sync-Platzhalter-Erkennung); ein gemeinsamer Helper verhindert divergierende Implementierungen und ist direkt unit-testbar. Host-Fallback deckt sich mit der bestehenden `IsHostPlaceholderTitle`-Erkennung, sodass pfadlose URLs (`https://heise.de/` → `heise.de`) nahtlos in die Sync-Auflösung einpassen. Klarstellung des Anwenders: Suchtreffer bringen normalerweise ihren Titel mit — der Dateiname greift nur bei titellosen Treffern und beim Direkt-Hinzufügen. |
| „Bearbeiten“-Kontextmenüpunkt | **Bleibt bestehen, öffnet dasselbe Sheet im Bearbeitungsmodus** (`IsEditMode`): sichtbar dann URL-`Entry`, `Switch FeedNotificationsEnabled` und „Speichern“-`Button`; `EditAsync` lädt wie bisher alle Werte ins ViewModel | „Umbenennen“/„Kategorie ändern“ decken `Title`/`CategoryId` ab, aber nicht `Url` und `NotificationsEnabled` — ohne „Bearbeiten“ gäbe es für beides keine UI mehr. Die Wiederverwendung des Sheets vermeidet einen zweiten Dialog. Vom Anwender bestätigt: „Bearbeiten“ bleibt im Kontextmenü als Edit-Modus desselben Bottom-Sheets (URL + Notifications-Switch + Speichern). |
| Fehler-/Hinweis-Anzeige | **Labels an zwei Orten mit identischen Bindings:** `ErrorMessage`-, `SearchErrorMessage`- und `FeedSearchOfflineHint`-`Label` werden (a) auf Seitenebene im Titel-/Hinweis-Stack belassen bzw. dorthin verschoben (`ErrorMessage`) — für Fehler bei **geschlossenem** Sheet (`ErrorFeedTitleEmpty` aus `RenameFeedAsync`, `ErrorFeedDuplicate` aus `SubscribeResultAsync`) — und (b) zusätzlich **innerhalb der Sheet-Karte** über dem `NewUrl`-`Entry` gerendert — für Fehler/Hinweise bei **geöffnetem** Sheet (`SaveAsync`-Validierung im Edit-Modus, `FeedSearchUnavailableRetry`, Offline-Hinweis bei deaktiviertem `SearchCommand`) | Die Anforderung schreibt die Hinweise ausdrücklich dem Formular zu („Im Formular verbleiben … die Such-/Offline-/Fehlerhinweise"), Fehler entstehen aber auch bei geschlossenem Sheet. Da der Backdrop (`Grid.RowSpan="2"`) bei offenem Sheet die gesamte Seitenebene verdeckt, sind beide Platzierungen nie gleichzeitig sichtbar — die Duplikation ist reine XAML-Wiederholung derselben `IsVisible`-/`DataTrigger`-Bindungen ohne neuen ViewModel-Zustand. Alternativ verworfen: Overlay nur auf `Grid.Row 1` beschränken — würde die Modal-Semantik brechen („+"-Button und Titelbereich blieben sicht- und klickbar) und vom `inset-0`-Referenz-Backdrop abweichen. |
| System-Zurück bei offenem Sheet | **`OnBackButtonPressed`-Override in `FeedsPage`:** bei `ShowAddForm = true` wird `CloseAddFormCommand` ausgeführt und `true` zurückgegeben (Sheet schließt, Seite bleibt); sonst unverändert `base.OnBackButtonPressed()` | Ohne Override würde Android-/Windows-Zurück die Seite verlassen statt das Sheet zu schließen — Bottom-Sheets erwarten üblicherweise „Zurück schließt das Sheet" (Referenz-Entwurf: Backdrop-Klick schließt). |
| Formular-Umfang | **Add-Modus: nur `Entry NewUrl` + „Suchen“** — `Picker`/`Switch`/`NewTitle`/`Speichern` entfallen im Add-Modus vollständig; neue Feeds starten mit `CategoryId = null` und `NotificationsEnabled = true` | Wortlaut der Anforderung („nur noch das URL-Eingabefeld und den ‚Suchen‘-Button“); die optionale Kategorie-Auswahl des Design-Entwurfs ist obsolet, weil „Kategorie ändern“ als Kontextaktion existiert. Vom Anwender bestätigt (geklärte Entscheidung). |

## Programmabläufe

### Hinzufügen-Sheet öffnen und schließen

1. Nutzer tippt den primären „+“-Button → `OpenAddFormCommand` setzt `ShowAddForm = true`, `IsEditMode = false` und leert `ErrorMessage` sowie `SearchErrorMessage` (kein stale Suchfehler im frisch geöffneten Sheet — Umsetzungsnotiz aus `plan-check.md`).
2. `DataTrigger` zeigt das Overlay (Backdrop + Sheet-Karte); das Code-Behind hat im Konstruktor `viewModel.PropertyChanged` abonniert und fokussiert bei `ShowAddForm`-Wechsel auf `true` das `NewUrl`-`Entry` via `Dispatcher` (`NewUrlEntry.Focus()` — Muster: `CategoriesPage` fokussiert `CategoryNameEntry` nach „Bearbeiten“). Der `PropertyChanged`-Weg deckt beide Öffnungspfade ab („+“-Button und `EditAsync` aus dem Kontextmenü) ohne zusätzlichen `Clicked`-Handler.
3. Schließen über „Abbrechen“/Schließen-Button im Sheet oder Tap auf den Backdrop → `CloseAddFormCommand` → `ResetForm` (`ShowAddForm = false`, `IsEditMode = false`, `NewUrl` leeren — dadurch auch Suchzustand-Reset über den `NewUrl`-Setter).
4. Hardware-/System-Zurück bei offenem Sheet: `OnBackButtonPressed` im Code-Behind führt bei `ShowAddForm = true` `CloseAddFormCommand` aus und gibt `true` zurück — das Sheet schließt, die Seite wird nicht verlassen.
5. Fehler-/Hinweiszeile: `ErrorMessage`-, `SearchErrorMessage`- und `FeedSearchOfflineHint`-`Label` sitzen zusätzlich innerhalb der Sheet-Karte (über dem `Entry`, identische Bindings); bei geschlossenem Sheet greifen die Seitenebenen-Labels.

Beteiligte Klassen/Komponenten: `FeedsPage`, `FeedsViewModel`, `OpenAddFormCommand`, `CloseAddFormCommand`, `ShowAddForm`, `IsEditMode`, `ResetForm`, `OnBackButtonPressed`, `PropertyChanged`

### Suche aus dem Sheet

1. `SearchCommand`/`SearchAsync` läuft unverändert (Eingabeklassifikation, `IFeedSearchService`, Stale-Input-Prüfung, Offline-Abbruch).
2. Sobald `ShowSearchResults = true` gesetzt wird, setzt `SearchAsync` zusätzlich `ShowAddForm = false` — das Overlay schließt sich, die Trefferansicht erscheint wie bisher in `Grid.Row 1`.
3. `CloseSearchResultsCommand` („Zurück zu meinen Feeds“) leert die Treffer und kehrt zur Feed-Liste zurück; das Sheet bleibt geschlossen.
4. Offline-/Fehlerhinweise (`FeedSearchOfflineHint`, `SearchErrorMessage`) sind bei geöffnetem Sheet **innerhalb der Sheet-Karte** sichtbar — `FeedSearchOfflineHint` erklärt dort den deaktivierten „Suchen“-Button (`SearchCommand.CanExecute = IsOnline && !IsSearching`). Suchfehler lassen `ShowAddForm` unverändert `true`: Das Sheet bleibt offen und zeigt den Fehler (z. B. `FeedSearchUnavailableRetry` bei Domain-Eingabe ohne Direkt-Hinzufügen-Dialog). Bei geschlossenem Sheet greifen weiterhin die identisch gebundenen Seitenebenen-Labels.

Beteiligte Klassen/Komponenten: `FeedsViewModel.Search`, `SearchAsync`, `ShowSearchResults`, `ShowAddForm`, `IFeedSearchService`, `FeedsPage`

### Suchtreffer abonnieren (mit Dateinamen-Fallback)

1. `OnSearchResultTapped` → `DisplayAlertAsync`-Bestätigung (unverändert).
2. `SubscribeResultAsync`: Dublettenprüfung `GetByUrlAsync` → `ErrorFeedDuplicate` (jetzt auf Seitenebene sichtbar).
3. `Title = !string.IsNullOrWhiteSpace(result.Title) ? result.Title.Trim() : FeedTitleFallback.GetFallbackTitle(result.FeedUrl)` — ersetzt den bisherigen Fallback `result.FeedUrl`.
4. `CategoryId = null`, `NotificationsEnabled = true` (fest verdrahtete Defaults; `SelectedCategory`/`FeedNotificationsEnabled` werden nicht mehr aus dem Add-Formular gelesen).
5. `AddAsync`, Suchzustand leeren, `ResetForm`, `LoadAsync`.

Beteiligte Klassen/Komponenten: `SubscribeResultAsync`, `FeedTitleFallback`, `IFeedRepository`, `FeedsPage`

### URL direkt hinzufügen (Suche ohne Treffer / nicht erreichbar)

1. `SearchAsync` → `OfferDirectAddAsync` bei 0 Treffern oder `FeedSearchUnavailableException` mit direkter URL (unverändert).
2. `ConfirmDirectAddAsync`-Callback (Code-Behind, `DisplayAlertAsync` „URL direkt hinzufügen?“) — unverändert.
3. **Änderung bei Bestätigung:** Statt `NewTitle = host` vorzubelegen persistiert `OfferDirectAddAsync` den Feed sofort: Dublettenprüfung `GetByUrlAsync` → bei Dublette `ErrorMessage = ErrorFeedDuplicate` und `SearchErrorMessage` leeren, kein `AddAsync`, `ShowAddForm` bleibt `true` (Fehler im Sheet sichtbar, Nutzer kann die URL korrigieren); sonst `AddAsync` mit `Title = FeedTitleFallback.GetFallbackTitle(input)`, `CategoryId = null`, `NotificationsEnabled = true`, `HealthStatus = FeedHealth.Ok`; danach `ResetForm` (schließt das Sheet) + `LoadAsync`. Bei Ablehnung bleibt der Zustand wie bisher.
   - *Umsetzungsnotiz (aus `plan-check.md`):* Im Exception-Pfad kann `SearchErrorMessage` (z. B. `FeedSearchUnavailable`) noch gesetzt sein — ohne Leerung wären bei offenem Sheet `ErrorMessage`- und `SearchErrorMessage`-Label gleichzeitig sichtbar. Da das Sheet offen bleibt, muss der Dubletten-Fehler der einzige sichtbare Fehler im Sheet-Hinweisblock sein.
4. Dialog-Exception bleibt geschluckt (bestehender `try/catch`).

Beteiligte Klassen/Komponenten: `OfferDirectAddAsync`, `ConfirmDirectAddAsync` (Callback), `FeedTitleFallback`, `IFeedRepository`, `FeedsPage`

### Feed umbenennen

1. `OnFeedTapped` → `DisplayActionSheetAsync` erhält den Eintrag `ButtonRename`.
2. Bei Auswahl: `DisplayPromptAsync` (Titel `PromptRenameFeedTitle`, Text `PromptRenameFeedMessage`, `initialValue` = aktueller `Feed.Title`, `ButtonOk`/`ButtonCancel`).
3. `null` (Abbrechen) → kein Aufruf. Sonst `viewModel.RenameFeedAsync(feed, result)`.
4. `RenameFeedAsync`: `feed is null` → Abbruch; `string.IsNullOrWhiteSpace(newTitle)` → `ErrorMessage = ErrorFeedTitleEmpty`, Abbruch; sonst `_feedRepository.UpdateAsync` mit geändertem `Title` (alle übrigen Felder aus `feed` übernommen), `ErrorMessage` leeren, `LoadAsync`.
5. Ein manuell vergebener Titel ist kein Platzhalter — `FeedSyncService` überschreibt ihn beim Sync nicht (bestehende Regel greift weiter).

Beteiligte Klassen/Komponenten: `FeedsPage.OnFeedTapped`, `DisplayPromptAsync`, `RenameFeedAsync`, `IFeedRepository.UpdateAsync`, `FeedSyncService`

### Kategorie ändern

1. `OnFeedTapped` → `DisplayActionSheetAsync` erhält den Eintrag `ButtonChangeCategory`.
2. Bei Auswahl: zweites `DisplayActionSheetAsync` (Titel `LabelFeedCategory`, Abbrechen `ButtonCancel`) mit den Namen aller Einträge in `viewModel.Categories` (inkl. `CategoryNone` an Position 0).
3. Abbrechen/unbekannter Eintrag → kein Aufruf; sonst `viewModel.ChangeFeedCategoryAsync(feed, gewählte Category)`.
4. `ChangeFeedCategoryAsync`: `feed is null`/`category is null` → Abbruch; `category.Id == Guid.Empty` → `CategoryId = null`; `_feedRepository.UpdateAsync` mit geändertem `CategoryId` (übrige Felder aus `feed`), `LoadAsync`.

Beteiligte Klassen/Komponenten: `FeedsPage.OnFeedTapped`, `DisplayActionSheetAsync`, `Categories`, `ChangeFeedCategoryAsync`, `IFeedRepository.UpdateAsync`

### Feed bearbeiten (Edit-Modus)

1. `OnFeedTapped` → Eintrag `ButtonEdit` bleibt; `EditAsync` lädt wie bisher `SelectedFeed`, `NewUrl`, `NewTitle`, `FeedNotificationsEnabled`, `SelectedCategory` ins ViewModel und setzt zusätzlich `IsEditMode = true`, `ShowAddForm = true` (Sheet öffnet sich im Edit-Modus mit vorbelegter URL).
2. Im Edit-Modus zeigt das Sheet zusätzlich den `Switch FeedNotificationsEnabled` (inkl. `NotificationsSupported`-Deaktivierung/iOS-Hinweis) und den „Speichern“-`Button`; die Kategorie-Auswahl bleibt auch im Edit-Modus außen vor („Kategorie ändern“-Dialog) — `SelectedCategory` wird von `EditAsync` dennoch korrekt geladen, damit `SaveAsync` die Kategorie unverändert zurückschreibt.
3. `SaveAsync` läuft unverändert (URL-Validierung, `NewTitle`-Prüfung — durch Vorbelegung aus `feed.Title` stets erfüllt —, Dublettenprüfung mit `SelectedFeed`-Ausnahme, `UpdateAsync`). Bei Fehlschlag (`ErrorFeedUrlInvalid`/`ErrorFeedDuplicate`) bleibt `ShowAddForm = true` — das Sheet bleibt offen und `ErrorMessage` erscheint im Hinweisblock der Sheet-Karte; erst im Erfolgsfall schließt `ResetForm` das Sheet.
4. „Umbenennen“ während eines offenen Edit-Zustands: nicht möglich, da das Kontextmenü nur aus der Liste erreichbar ist und das Sheet die Liste verdeckt.

Beteiligte Klassen/Komponenten: `EditAsync`, `IsEditMode`, `ShowAddForm`, `SaveAsync`, `FeedsPage`

### Sync mit erweiterter Platzhalter-Erkennung

1. `RunSyncAsync` ermittelt nach dem Parsen `documentTitle = syndicationFeed.Title?.Text` (unverändert).
2. `isPlaceholderTitle` wird um den Dateinamen-Fall erweitert: `IsNullOrWhiteSpace(feed.Title)` ‖ `Title == Url` ‖ `IsHostPlaceholderTitle(feed)` ‖ **`IsFileNamePlaceholderTitle(feed)`** — letzteres prüft `Title == letztes nicht-leeres Pfadsegment der feed.Url` (OrdinalIgnoreCase, URL-dekodiert, via `FeedTitleFallback`-Logik).
3. Bei gesetztem `documentTitle` und `isPlaceholderTitle` → `resolvedTitle` wird wie bisher in `UpdateFeedHealthAsync` übernommen; manuell vergebene Titel bleiben unberührt.

Beteiligte Klassen/Komponenten: `FeedSyncService.RunSyncAsync`, `IsHostPlaceholderTitle`, `IsFileNamePlaceholderTitle`, `FeedTitleFallback`, `UpdateFeedHealthAsync`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `FeedTitleFallback` (`src/Reporter.Core/Services/FeedTitleFallback.cs`) | statische Hilfsklasse | `GetFallbackTitle(string url)` — Titel-Fallback-Kette (letztes nicht-leeres, URL-dekodiertes Pfadsegment → `Host` → URL); `IsFileNamePlaceholderTitle(string title, string url)` — Vergleich Titel vs. Dateiname für `FeedSyncService` |

## Änderungen an bestehenden Klassen

### `FeedsViewModel` (`src/Reporter.Core/ViewModels/FeedsViewModel.cs`)

- **Neue Eigenschaften:** `ShowAddForm` (`bool`) — Sichtbarkeit des Hinzufügen-Sheets; `IsEditMode` (`bool`) — steuert Sheet-Modus (zusätzliche Felder/„Speichern“).
- **Neue Commands:** `OpenAddFormCommand` (`RelayCommand`) — `ShowAddForm = true`, `IsEditMode = false`, `ErrorMessage` und `SearchErrorMessage` leeren (Letzteres verhindert einen stale Suchfehler im Sheet-Hinweisblock beim erneuten Öffnen); `CloseAddFormCommand` (`RelayCommand`) → `ResetForm`.
- **Neue Methoden:** `RenameFeedAsync(FeedListItem? feed, string? newTitle)` — `Task`; Validierung leerer Titel → `ErrorFeedTitleEmpty`; `UpdateAsync` mit neuem `Title`; `LoadAsync`. `ChangeFeedCategoryAsync(FeedListItem? feed, Category? category)` — `Task`; `Guid.Empty` → `CategoryId = null`; `UpdateAsync`; `LoadAsync`. (Als öffentliche Methoden statt Commands, da beide zwei Parameter benötigen und der Dialog im Code-Behind sitzt — gleiche UI-Grenze wie `ConfirmDirectAddAsync`.)
- **Geänderte Methoden:** `EditAsync` — setzt zusätzlich `IsEditMode = true`, `ShowAddForm = true`. `ResetForm` — setzt zusätzlich `ShowAddForm = false`, `IsEditMode = false`; `NewTitle`-Reset bleibt (Property bleibt VM-intern für den Edit-Pfad bestehen, wird nur nicht mehr in der Add-UI gebunden).
- **Unverändert:** `SaveAsync` (inkl. `NewTitle`-Validierung — im Edit-Modus durch `EditAsync`-Vorbelegung stets erfüllt; der Neuanlage-Zweig bleibt für Tests/API-Vollständigkeit bestehen, ist aber aus der UI nicht mehr erreichbar), `DeleteAsync`, `RefreshAsync`, `SyncAsync`, `OnConnectivityChanged`.

### `FeedsViewModel.Search` (`src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`)

- **Geänderte Methoden:**
  - `SearchAsync` — beim Setzen von `ShowSearchResults = true` (Trefferpfad und leerer-Freitext-Pfad) zusätzlich `ShowAddForm = false`, damit das Overlay die Treffer nicht verdeckt.
  - `OfferDirectAddAsync` — bei Bestätigung direktes Persistieren statt `NewTitle`-Vorbelegung: Dublettenprüfung `GetByUrlAsync` → bei Dublette `ErrorFeedDuplicate` setzen und `SearchErrorMessage` leeren (damit nicht beide Fehlerkanäle gleichzeitig im Sheet-Hinweisblock sichtbar sind), `ShowAddForm` bleibt `true` (kein `AddAsync`/`ResetForm`, Fehler im Sheet sichtbar); sonst `AddAsync` mit `Title = FeedTitleFallback.GetFallbackTitle(input)`, `CategoryId = null`, `NotificationsEnabled = true`, `HealthStatus = FeedHealth.Ok`; anschließend `ResetForm` + `LoadAsync`. Bei Ablehnung nur Suchzustand leeren (bisheriges Verhalten).
  - `SubscribeResultAsync` — Titel-Fallback `result.FeedUrl` → `FeedTitleFallback.GetFallbackTitle(result.FeedUrl)`; `CategoryId`/`NotificationsEnabled` fest auf `null`/`true` (Formularfelder entfallen).

### `FeedsPage` XAML (`src/Reporter/Views/FeedsPage.xaml`)

- **Entfernt aus der Formularkarte (Add-Modus):** `Entry NewTitle`, `Picker Categories`/`SelectedCategory`, `Button` „Speichern“ (`SaveCommand`), Notification-`Switch`-`Grid` + iOS-Hinweis-`Border` — letztere beiden wandern in den Edit-Modus-Block (sichtbar nur bei `IsEditMode = true`).
- **Neu Seitenebene (`Grid.Row 0`):** Primär-`Button` `ActionAddFeed` → `OpenAddFormCommand` (`MinimumHeightRequest="44"`), `ErrorMessage`-`Label` (aus der entfernten Formularkarte hierher verschoben — für Fehler bei geschlossenem Sheet). `SearchErrorMessage`, `FeedSearchOfflineHint`, `SyncErrorMessage` und das Offline-Banner bleiben unverändert auf Seitenebene.
- **Neu Overlay:** `Grid` mit `Grid.RowSpan="2"`, `IsVisible="False"` + `DataTrigger` auf `ShowAddForm`; enthält halbtransparenten `BoxView`-Backdrop (AppThemeBinding, `TapGestureRecognizer` → `CloseAddFormCommand`) und eine `Border`-Karte (`VerticalOptions="End"`, `StrokeShape="RoundRectangle 12,12,0,0"` bzw. obere Ecken rund) mit: Titel-`Label` (`FeedAddSheetTitle` / im Edit-Modus `FeedEditSheetTitle` via `DataTrigger` auf `IsEditMode`), Schließen-`Button` (→ `CloseAddFormCommand`, ≥44 pt), **Hinweis-/Fehler-Block** (drei `Label` mit denselben Bindings wie die Seitenebenen-Labels: `ErrorMessage`/`HasError`, `SearchErrorMessage`/`HasSearchError`, `FeedSearchOfflineHint` mit `IsOnline=False`-`DataTrigger`), `Entry NewUrl` (`ReturnCommand="{Binding SearchCommand}"`, `x:Name` für Fokus), `ActivityIndicator` (`IsSearching`), `Button` „Suchen“, sowie im Edit-Modus per `DataTrigger` sichtbar: Notification-`Switch`-`Grid`, iOS-Hinweis-`Border`, `Button` „Speichern“.
- **Unverändert:** Treffer-`Grid` (`SearchResults`, `ShowSearchResults`-Trigger, `OnSearchResultTapped`, Attribution, „Zurück zu meinen Feeds“), Feed-`RefreshView`/`CollectionView` inkl. Karten-Template, Offline-Banner, `FeedSearchOfflineHint`, `SearchErrorMessage`, `SyncErrorMessage`.
- **Regeln (AGENTS.md):** Alle Touch-Ziele ≥ 44 × 44 pt, `AppThemeBinding` für Sheet/Backdrop, keine `ScrollView`-/`CollectionView`-Verschachtelung im Overlay (nur `VerticalStackLayout`-Formular), Liste füllt Resthöhe via `Grid.Row="*"`, kein mehrspaltiger Text-Button-Row.

### `FeedsPage` Code-Behind (`src/Reporter/Views/FeedsPage.xaml.cs`)

- **Geänderte Methoden:** `OnFeedTapped` — `DisplayActionSheetAsync` um `ButtonRename` und `ButtonChangeCategory` erweitert; Routing: „Umbenennen“ → `DisplayPromptAsync` (Vorbelegung `feed.Title`) → `RenameFeedAsync`; „Kategorie ändern“ → `DisplayActionSheetAsync` über `viewModel.Categories.Select(c => c.Name)` → Rückmapping per Name/Index → `ChangeFeedCategoryAsync`. Konstruktor — zusätzlich `viewModel.PropertyChanged` abonnieren: bei `PropertyName == nameof(ShowAddForm)` und `ShowAddForm == true` `NewUrlEntry.Focus()` via `Dispatcher.Dispatch` aufrufen (deckt „+“-Button- und `EditAsync`-Öffnungspfad ab; Muster: `CategoryNameEntry?.Focus()` in `CategoriesPage`).
- **Neue Methoden:** `OnBackButtonPressed`-Override (`protected override bool`) — bei `viewModel.ShowAddForm` `CloseAddFormCommand` ausführen und `true` zurückgeben; sonst `base.OnBackButtonPressed()`.
- **Unverändert:** `OnAppearing`, `OnSearchResultTapped`, `ConfirmDirectAddAsync`-Callback, Lösch-Bestätigung.

### `FeedSyncService` (`src/Reporter.Core/Services/FeedSyncService.cs`)

- **Geänderte Methoden:** `RunSyncAsync` — `isPlaceholderTitle` um `FeedTitleFallback.IsFileNamePlaceholderTitle(feed.Title, feed.Url)` erweitert.
- **Unverändert:** `IsHostPlaceholderTitle` bleibt (deckt den Host-Fallback bei pfadlosen URLs ab), `UpdateFeedHealthAsync` unverändert.

### `AppResources` (`src/Reporter.Core/Resources/Strings/AppResources.resx` + `AppResources.de.resx` + Designer)

- **Neue Schlüssel:** `ActionAddFeed` („+ Feed per URL hinzufügen“), `FeedAddSheetTitle` („Feed per URL hinzufügen“), `FeedEditSheetTitle` („Feed bearbeiten“), `ButtonRename` („Umbenennen“), `ButtonChangeCategory` („Kategorie ändern“), `PromptRenameFeedTitle` („Feed umbenennen“), `PromptRenameFeedMessage` („Neuer Anzeigetitel“).
- **Vorhandene Schlüssel weiterverwendet:** `ButtonSearch`, `ButtonSave` (Edit-Modus), `ButtonCancel`, `ButtonOk` (für `DisplayPromptAsync` — bereits in beiden resx vorhanden), `LabelFeedCategory` (Kategorie-Sheet-Titel), `ErrorFeedTitleEmpty`, `ErrorFeedDuplicate`, `ErrorFeedUrlInvalid`, `FeedNotificationsLabel`/`FeedNotificationsHint`, `NotificationsIosOnlyHint`, `PlaceholderFeedSearch`, `FeedSearchOfflineHint`, `FeedSearchUnavailableRetry`.
- Designer-Datei `AppResources.Designer.cs` nach Ergänzung regenerieren.

## Datenbankmigrationen

Keine — `Feed.Title`, `Feed.CategoryId`, `Feed.NotificationsEnabled` existieren bereits; `IFeedRepository.UpdateAsync` deckt alle Änderungen ab.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `newTitle` (`RenameFeedAsync`) | Nicht leer/nicht nur Whitespace | `ErrorMessage = ErrorFeedTitleEmpty` (Seitenebene — Sheet ist hier geschlossen), kein Update |
| `url` (`SaveAsync`, Edit-Modus) | Gültige absolute `http(s)`-URL (`IsValidFeedUrl`) | `ErrorFeedUrlInvalid` (bestehend — bei geöffnetem Sheet im Sheet-Hinweisblock sichtbar, Sheet bleibt offen) |
| `url` (`SaveAsync`/`SubscribeResultAsync`/`OfferDirectAddAsync`) | Keine Dublette (`GetByUrlAsync`, Edit-Fall mit `SelectedFeed`-Ausnahme) | `ErrorFeedDuplicate` (bestehend, jetzt auch im Direkt-Hinzufügen-Pfad — dort bei geöffnetem Sheet im Sheet-Hinweisblock sichtbar) |
| `category` (`ChangeFeedCategoryAsync`) | `Guid.Empty` → `null` | — (kein Fehlerfall) |

## Konfigurationsänderungen

Keine — Verhalten ist fest verdrahtet (Defaults: `CategoryId = null`, `NotificationsEnabled = true`).

## Seiteneffekte und Risiken

- **„Bearbeiten“-Flow:** Das Formular im Edit-Modus hat keinen Titel-/Kategorie-Eingriff mehr; `EditAsync` lädt `NewTitle`/`SelectedCategory` weiterhin, damit `SaveAsync` beide Werte unverändert zurückschreibt. Eine versehentliche Leerung ist aus der UI nicht möglich.
- **`Feed.NotificationsEnabled` bei Neuanlage:** Neue Feeds erhalten fest `true` (bisheriger Default); Änderung nur noch über „Bearbeiten“.
- **`Feed.CategoryId` bei Neuanlage:** Neue Feeds erhalten fest `null`; Zuordnung nur noch über „Kategorie ändern“.
- **`FeedSyncService`:** Die erweiterte Platzhalter-Erkennung könnte theoretisch einen manuell gesetzten Titel überschreiben, der zufällig exakt dem Dateinamen der URL entspricht — praktisch unerheblich, da genau dieser Fall dann ohnehin dem Auto-Wert entspricht.
- **`CategoriesPage`/`UnreadPage`:** Unberührt; dortige `DisplayActionSheetAsync`-Muster dienen nur als Vorlage.
- **Doppelte Label-Platzierung:** `ErrorMessage`/`SearchErrorMessage`/`FeedSearchOfflineHint` werden an zwei Stellen (Seitenebene + Sheet-Karte) an dieselben ViewModel-Eigenschaften gebunden — kein neuer Zustand, keine doppelte Anzeige (Backdrop verdeckt die Seitenebene bei offenem Sheet). Bei späteren XAML-Änderungen an diesen Labels sind beide Stellen mitzuziehen.
- **System-Zurück:** `OnBackButtonPressed` ändert das Rückverhalten ausschließlich bei geöffnetem Sheet (Sheet schließt statt Seite zu verlassen); bei geschlossenem Sheet ist das Verhalten unverändert.
- **Bestehende Tests:** `SearchCommand_NoResultsAndValidUrl_Confirmed_PrefillsTitleFromHost`, `SubscribeResultCommand_WhenTitleEmpty_StoresFeedUrlAsPlaceholder` und `SaveCommand_*`/`EditCommand_*`-Tests sind an das neue Verhalten anzupassen (siehe Tests); `DeleteCommand_ResetsFeedNotificationsEnabled` (exerziert das geänderte `ResetForm`) und `SearchCommand_WhenConfirmDirectAddThrows_InvokesDialogOnlyOnce` (exerziert das geänderte `OfferDirectAddAsync`) bleiben voraussichtlich gültig, sind aber zu prüfen bzw. zu ergänzen (siehe Tests).

## Umsetzungsreihenfolge

1. **`FeedTitleFallback`-Helper anlegen** (`src/Reporter.Core/Services/FeedTitleFallback.cs`)
   - Voraussetzungen: Keine (reines .NET, `Uri`-API).
   - Beschreibung: `GetFallbackTitle` (letztes nicht-leeres, URL-dekodiertes Pfadsegment → `Host` → URL) und `IsFileNamePlaceholderTitle` implementieren; mit Unit-Tests absichern.

2. **Neue `AppResources`-Schlüssel in EN+DE eintragen, Designer regenerieren**
   - Voraussetzungen: Keine.
   - Beschreibung: `ActionAddFeed`, `FeedAddSheetTitle`, `FeedEditSheetTitle`, `ButtonRename`, `ButtonChangeCategory`, `PromptRenameFeedTitle`, `PromptRenameFeedMessage` (`ButtonOk` ist bereits in beiden resx vorhanden); Build prüft `x:Static`-Referenzen.

3. **`FeedsViewModel` erweitern (Hauptdatei + Search-Partial)**
   - Voraussetzungen: Schritt 1 (Helper), Schritt 2 (Ressourcen für `ErrorFeedTitleEmpty` bereits vorhanden — keine neuen nötig für den VM).
   - Beschreibung: `ShowAddForm`, `IsEditMode`, `OpenAddFormCommand`, `CloseAddFormCommand`, `RenameFeedAsync`, `ChangeFeedCategoryAsync` hinzufügen; `EditAsync`, `ResetForm`, `SearchAsync`, `OfferDirectAddAsync`, `SubscribeResultAsync` anpassen.

4. **`FeedSyncService` Platzhalter-Erkennung erweitern**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `isPlaceholderTitle` um `FeedTitleFallback.IsFileNamePlaceholderTitle` ergänzen.

5. **`FeedsPage.xaml` umbauen**
   - Voraussetzungen: Schritt 2 (Ressourcen), Schritt 3 (Bindungen).
   - Beschreibung: Formularkarte entfernen, „+“-Button + `ErrorMessage`-Label auf Seitenebene, Overlay-Sheet (Backdrop + Karte) mit Add-/Edit-Modus-Triggers und Hinweis-/Fehler-Block (`ErrorMessage`/`SearchErrorMessage`/`FeedSearchOfflineHint`, identische Bindings) innerhalb der Karte.

6. **`FeedsPage.xaml.cs` erweitern**
   - Voraussetzungen: Schritt 2 (Ressourcen), Schritt 3 (Methoden), Schritt 5 (`x:Name` für Fokus).
   - Beschreibung: `OnFeedTapped` um „Umbenennen“ (`DisplayPromptAsync`) und „Kategorie ändern“ (`DisplayActionSheetAsync`) erweitern; `PropertyChanged`-Subscription für `NewUrl`-Fokus; `OnBackButtonPressed`-Override (Sheet schließen statt Seite verlassen).

7. **Tests anpassen und ergänzen** (`src/Reporter.Tests`)
   - Voraussetzungen: Schritte 1–4 (testbarer Core-Code).
   - Beschreibung: Neue Tests lt. Tabelle; betroffene bestehende Tests anpassen; `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` grün.

8. **Manuelle UI-Verifikation (E2E-Ersatz)**
   - Voraussetzungen: Schritte 5–6 (UI fertig), App unter Windows startbar (Lauf 1 bewiesen: Release-Build, Fenster 390×844 pt, `test-results/issue-59/uia.ps1`).
   - Beschreibung: Szenarien lt. E2E-Tabelle manuell durchspielen, Screenshots unter `test-results/issue-59/` (bzw. neuem Unterordner), Dokumentation in `test-results.md` und `docs/help/anwendung/mobile-ui-design.md`; Vergleich mit `design-draft/.../screen.png`.

9. **`.\scripts\Run-StaticChecks.ps1` ausführen**
   - Voraussetzungen: Alle Codeänderungen abgeschlossen.
   - Beschreibung: Format, Security, Static Analysis (inkl. MAUI-Release-Build) müssen ohne Befund durchlaufen (Exit-Code 0).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `GetFallbackTitle_ReturnsLastPathSegment` / `..._DecodesUrlEncodedSegment` / `..._FallsBackToHost_WhenNoPath` / `..._FallsBackToUrl_WhenUnparseable` / `..._TrailingSlash_UsesLastNonEmptySegment` | `FeedTitleFallbackTests` (neu) | Dateinamen-Ableitung inkl. Pfad-lose-URL-Fall (`https://heise.de/` → `heise.de`) |
| `IsFileNamePlaceholderTitle_*` (Match/No-Match, Case-insensitive) | `FeedTitleFallbackTests` (neu) | Platzhalter-Vergleich für den Sync |
| `OpenAddFormCommand_ShowsForm` / `CloseAddFormCommand_ResetsFormAndHidesForm` | `FeedsViewModelTests` | `ShowAddForm`/`IsEditMode`-Zustandswechsel |
| `RenameFeedAsync_UpdatesTitle` / `..._EmptyTitle_SetsErrorAndKeepsTitle` / `..._NullFeed_DoesNothing` | `FeedsViewModelTests` | Persistenz, Validierung (`ErrorFeedTitleEmpty`), Null-Guards |
| `ChangeFeedCategoryAsync_SetsCategoryId` / `..._EmptyGuid_ClearsCategory` / `..._NullArguments_DoNothing` | `FeedsViewModelTests` | `CategoryId`-Update, `Guid.Empty` → `null` |
| `SubscribeResultCommand_WhenTitleEmpty_StoresFileNameAsPlaceholder` | `FeedsViewModelTests` | `Title = heise-atom.xml` statt voller URL |
| `SearchCommand_NoResultsAndValidUrl_Confirmed_AddsFeedWithFileNameTitle` / `..._Confirmed_WhenDuplicate_SetsError` | `FeedsViewModelTests` | Direkt-Hinzufügen persistiert sofort (Dateinamen-Titel, Defaults `CategoryId=null`/`NotificationsEnabled=true`), Dublettenprüfung |
| `SearchCommand_WhenResultsShown_ClosesAddForm` | `FeedsViewModelTests` | `ShowSearchResults=true` ⇒ `ShowAddForm=false` |
| `SearchCommand_WhenUnavailable_KeepsSheetOpenAndSetsSearchError` | `FeedsViewModelTests` | `FeedSearchUnavailableException` bei Domain-Eingabe: `ShowAddForm` bleibt `true`, `SearchErrorMessage = FeedSearchUnavailableRetry` — Zustandsnachweis für Fehleranzeige bei offenem Sheet |
| `SaveCommand_EditMode_WhenInvalidUrl_KeepsSheetOpenAndSetsError` / `..._WhenDuplicate_KeepsSheetOpenAndSetsError` | `FeedsViewModelTests` | `SaveAsync`-Fehlschlag im Edit-Modus: `ShowAddForm` bleibt `true`, `ErrorMessage = ErrorFeedUrlInvalid` bzw. `ErrorFeedDuplicate`, kein `UpdateAsync` — Sheet bleibt für die Fehleranzeige offen |
| `EditAsync_OpensSheetInEditMode` | `FeedsViewModelTests` | `IsEditMode`/`ShowAddForm` gesetzt, Werte vorbelegt |
| `SyncFeedAsync_WhenTitleIsFileNamePlaceholder_UpdatesTitleFromFeedDocument` | `FeedSyncServiceTests` | `Title = heise-atom.xml` wird durch `SyndicationFeed.Title` ersetzt |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `SearchCommand_NoResultsAndValidUrl_Confirmed_PrefillsTitleFromHost` | Verhalten ändert sich: bestätigtes Direkt-Hinzufügen persistiert sofort statt `NewTitle`-Vorbelegung — wird durch `..._AddsFeedWithFileNameTitle` ersetzt |
| `SearchCommand_NoResultsAndValidUrl_Declined_KeepsFormState` | Prüft ggf. `NewTitle`/Formularzustand — an neues Ablehnungs-Verhalten anpassen (kein Persistieren, Suchzustand geleert) |
| `SubscribeResultCommand_WhenTitleEmpty_StoresFeedUrlAsPlaceholder` | Fallback ändert sich von `FeedUrl` auf Dateiname — ersetzt durch `..._StoresFileNameAsPlaceholder` |
| `SubscribeResultCommand_PersistsFeedFromResult` | `CategoryId`/`NotificationsEnabled` jetzt fest `null`/`true` statt aus `SelectedCategory`/`FeedNotificationsEnabled` — Erwartungen anpassen |
| `SaveCommand_*`/`EditCommand_*`-Tests (`FeedsViewModelTests`) | Bleiben fachlich bestehen (VM-API unverändert); `EditAsync`-Tests um `IsEditMode`/`ShowAddForm`-Erwartung ergänzen. Der `SaveAsync`-Neuanlage-Zweig bleibt aus API-/Testgründen bestehen, obwohl aus der UI unerreichbar — in der Testdoku kurz begründen |
| `DeleteCommand_ResetsFeedNotificationsEnabled` (`FeedsViewModelTests`) | Exerziert das geänderte `ResetForm` — bleibt gültig; um die Erwartung `ShowAddForm = false`/`IsEditMode = false` ergänzen |
| `SearchCommand_WhenConfirmDirectAddThrows_InvokesDialogOnlyOnce` (`FeedsViewModelTests`) | Exerziert das geänderte `OfferDirectAddAsync` — die Dialog-Exception wird weiterhin vor jeder Persistenz geschluckt (kein `AddAsync` im Wurf-Pfad); unverändert gültig, Erwartung ggf. um „kein Feed persistiert" präzisieren |
|| `SyncFeedAsync_WhenTitleIsPlaceholder_UpdatesTitleFromFeedDocument`, `SyncFeedAsync_WhenTitleIsHostPlaceholder_UpdatesTitleFromFeedDocument`, `SyncFeedAsync_WhenTitleIsSet_DoesNotOverwriteTitle` (`FeedSyncServiceTests.cs` Z. 495/526/556) | Alle drei exerzieren die geänderte `isPlaceholderTitle`-Bedingung in `RunSyncAsync` — bleiben unverändert/grün. `..._WhenTitleIsSet_DoesNotOverwriteTitle` evaluiert den neuen `IsFileNamePlaceholderTitle`-Operanden tatsächlich (Titel „Test Feed" vs. URL `https://example.com/rss`) und ist der negative Guard gegen Übermatching der neuen Regel — zugleich der konkrete Nachweis, dass ein manuell vergebener Titel unverändert bleibt |
|| `SubscribeResultCommand_WhenDuplicate_SetsErrorAndDoesNotAdd` (`FeedsViewModelTests.cs` Z. 846) | Betritt das geänderte `SubscribeResultAsync`; der Dubletten-Frühabbruch liegt vor den geänderten Zeilen — Vertrag (`ErrorFeedDuplicate`, kein `AddAsync`, `ShowSearchResults` bleibt `true`) unverändert gültig |
|| `SearchCommand_WhenServiceUnavailable_SetsSearchErrorMessage` (`FeedsViewModelTests.cs` Z. 541) | Ruft bei direkter URL (`https://example.com/rss`) das geänderte `OfferDirectAddAsync` auf (`offerDirectAdd = isDirectUrl = true`, Callback `null` → Frühabbruch vor den geänderten Zeilen) — `SearchErrorMessage` bleibt erhalten und nichts wird persistiert; unverändert gültig |
|| `SearchCommand_PopulatesSearchResults`, `SearchCommand_FreeText_DoesNotCallService_ShowsEmptyResults`, `SearchCommand_DomainInput_NormalizesToHttpsUrl`, `SearchCommand_NoResultsAndDomainInput_DoesNotInvokeConfirm`, `CloseSearchResultsCommand_ExitsResultsView`, `NewUrl_Changed_ClearsSearchState` (`FeedsViewModelTests`) | Durchlaufen den geänderten Trefferpfad von `SearchAsync` (dort wird neu `ShowAddForm = false` gesetzt) — Assertions bleiben gültig, da `ShowAddForm` im Testkontext ohnehin `false` ist; geprüft unverändert/grün |

### E2E-Tests (primärer Funktionsnachweis)

**Begründung, warum keine automatisierten E2E-Tests geplant werden:** Das Projekt hat keine UI-Test-Infrastruktur — das Testprojekt `src/Reporter.Tests` kompiliert die MAUI-App (`src/Reporter`, inkl. `FeedsPage.xaml(.cs)`) nicht, und es existiert kein UI-Test-Projekt/-Framework (Bestandsaufnahme `tests.md`). Als Funktionsnachweis für die Benutzerflüsse dienen daher (a) ViewModel-/Integrationstests gegen SQLite-In-Memory für die gesamte Ablauf-Logik und (b) die nach AGENTS.md vorgeschriebene dokumentierte manuelle UI-Verifikation auf 390 × 844 pt — in Lauf 1 bereits mit dieser Umgebung durchgeführt (App-Start, UI Automation via `test-results/issue-59/uia.ps1`, Screenshots), die Hilfsskripte werden wiederverwendet und ggf. erweitert.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Standard-Ansicht zeigt nur Liste + „+“-Button; Sheet öffnet sich als Bottom-Sheet, Fokus im URL-Feld | Manuell (UIA + Screenshot `manual-*.png`), dokumentiert in `test-results.md`/`mobile-ui-design.md` | Formular nicht mehr dauerhaft sichtbar; „+“ öffnet das Formular | Reine UI-Sichtbarkeit/Layout — vom Testprojekt nicht abgedeckt |
| Pflicht | URL eingeben → „Suchen“ → Sheet schließt, Trefferliste erscheint → Treffer tippen → Bestätigung → Feed in Liste | Manuell + `FeedsViewModelTests.SearchCommand_*`/`SubscribeResultCommand_*` | Suchergebnisse werden gelistet und wie bisher hinzugefügt | Overlay-/Trigger-Zusammenspiel nur sichtbar prüfbar |
| Pflicht | Treffer ohne Titel abonnieren → Feed heißt `heise-atom.xml`; nach Sync echter Titel | `FeedsViewModelTests`/`FeedSyncServiceTests` + manueller Sichtcheck | Dateiname als Titel-Fallback inkl. Sync-Auflösung | Logik per Tests, Anzeige manuell |
| Pflicht | Direkt-Hinzufügen: Suche ohne Treffer → Dialog „direkt hinzufügen?“ → „Ja“ → Feed erscheint sofort in Liste | Manuell + `FeedsViewModelTests` | Direkt-Hinzufügen ohne Titel-Eingabe persistiert | Dialog-Ablauf liegt im Code-Behind |
| Pflicht | Kontextmenü → „Umbenennen“ → Prompt mit Vorbelegung → neuer Titel in Liste | Manuell + `RenameFeedAsync`-Tests | Kontextmenü-Aktion „Umbenennen“ mit kleinem Dialog | `DisplayPromptAsync` ist plattformabhängiges Code-Behind |
| Pflicht | Kontextmenü → „Kategorie ändern“ → Auswahl inkl. „—“ → `CategoryName` in Meta-Zeile ändert sich | Manuell + `ChangeFeedCategoryAsync`-Tests | Kontextmenü-Aktion „Kategorie ändern“ mit Dialog | ActionSheet-Auswahl nur UI-seitig |
| Pflicht | Kontextmenü → „Bearbeiten“ → Sheet im Edit-Modus (URL + Switch + Speichern) → URL ändern speichert | Manuell + `SaveAsync`-Tests | „Bearbeiten“ deckt URL/`NotificationsEnabled` weiter ab | Edit-Modus-Trigger + Sheet-Layout |
| Pflicht | Fehlerfälle bei **geöffnetem** Sheet: „Bearbeiten“ → ungültige bzw. doppelte URL → „Speichern“ → `ErrorMessage` (`ErrorFeedUrlInvalid`/`ErrorFeedDuplicate`) **im Sheet** sichtbar, Sheet bleibt offen; Domain-Eingabe bei nicht erreichbarem Suchdienst → `SearchErrorMessage` (`FeedSearchUnavailableRetry`) im Sheet; Offline → `FeedSearchOfflineHint` im Sheet + „Suchen“-Button deaktiviert | Manuell (Screenshots) + `SaveCommand_EditMode_*`-/`SearchCommand_WhenUnavailable_*`-ViewModel-Tests | „Im Formular verbleiben die Such-/Offline-/Fehlerhinweise“; Edit-Validierung bei offenem Sheet | Die Labels liegen auf zwei Ebenen (Seite vs. Sheet-Karte) — die Sichtbarkeit hinter/innerhalb des Overlays ist nur visuell verifizierbar |
| Sollte | Umbenennen mit leerem Titel → Fehler sichtbar, Titel unverändert | `RenameFeedAsync`-Test + manuell | Validierung leerer Titel | Fehleranzeige auf Seitenebene verifizieren |
| Sollte | System-Zurück (Android/Windows) bei offenem Sheet schließt das Sheet statt die Seite zu verlassen; „+“-Button, Backdrop-Tap und Dark Mode geprüft | Manuell (Light + Dark Screenshots) | Offline-Hinweise, `AppThemeBinding`, ≥44-pt-Touch-Ziele, Design-Abgleich mit `screen.png` | Plattform-/visuelle Verifikation ohne UI-Test-Framework |

Bestehende E2E-Tests: Keine vorhanden — die Suite kennt keine UI-Tests; die manuelle Verifikation aus Lauf 1 wird für die geänderten Flüsse wiederholt.

## Offene Punkte

Keine.
