<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Umsetzungsplan: UI-Polish, Accessibility und Design-System-Vollständigkeit (Issue #30)

## Übersicht

Abschließender Qualitäts- und Konsistenz-Pass über die .NET-MAUI-App `Reporter`: Das Design-System aus `design-draft/stitch_local_rss_feed_reader/` (Tokens in `editorial_feed/DESIGN.md` und `reporter_editorial_dark/DESIGN.md`) wird lückenlos auf alle Seiten angewendet — sichtbare Filter-Chip-Leiste auf `UnreadPage`, Floating Reader Control Bar auf `ArticleDetailPage`, Micro-Pill-Status-Badges auf `FeedsPage`, Karten-/Token-Feinschliff auf allen Seiten, neues Programmsymbol/Splash, Tab-Icons. Ergänzend Accessibility-Pass (`SemanticProperties`, Kontraste, dynamische Schrift) und Performance-Pass (Batch-Insert im Sync, Paging in `LaterViewModel`).

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Chip-Leiste `UnreadPage` | Horizontale `CollectionView` (`ItemsLayout="HorizontalList"`) an `Categories`; Chip = äußerer transparenter `Border` (44-pt-Touch-Target, `MinimumHeightRequest="44"`, `TapGestureRecognizer` → `SelectCategoryCommand`, `CommandParameter="{Binding .}"`, `SemanticProperties.Description="{Binding Name}"`) um inneren sichtbaren Pill-`Border` (`Padding="12,6"`, `RoundRectangle 9999`, Name + Count-Kapsel). Aktiv-Zustand per `DataTrigger` auf `IsSelected` am inneren `Border`: `Primary`-Fläche + `OnPrimary`-Text (Light ≈ `bg-primary-container`/`text-on-primary`, Dark = `bg-primary`/`text-on-primary` des Entwurfs); inaktiv: `SurfaceCard` + `BorderSubtle`-Hairline + `TextSecondary`. Count-Kapsel: innerer Pill-`Border` mit neuem Token `ChipCountCapsule` (aktiv `White @ 20 %` / Dark `Black @ 20 %`, inaktiv `SurfaceSubtle`) und `LabelMetaStyle`. | Entwurf sieht ausschließlich die horizontale Pill-Leiste (`overflow-x-auto`, `shrink-0`); `CollectionView` ist das etablierte Listenmuster im Projekt und erledigt horizontales Overflow-Scrolling bei vielen Kategorien. Der zweischichtige `Border` trennt Touch-Target (≥ 44 pt, AGENTS.md-Regel hat Vorrang vor der ~30-pt-Entwurfshöhe) von der sichtbaren Pill-Geometrie. Alternative `HorizontalScrollView` + `BindableLayout`/`HorizontalStackLayout` wurde verworfen (kein Vorteil, kein bestehendes Muster). |
| Kategorieauswahl Interaktion | Funnel-Icon-`Border`, `OnFilterClicked` (`DisplayActionSheetAsync`) und das `SelectedCategoryText`-`Label` entfallen vollständig; `UnreadViewModel.SelectedCategoryText` wird entfernt. | Der Entwurf (`ungelesen_dashboard/code.html`) kennt kein Funnel-Icon; die Chip-Leiste zeigt Name + Count direkt an. Zwei parallele Auswahlwege wären redundant und würden die Header-Zeile überladen. `SelectCategoryCommand`/`Categories`/`IsSelected`/`Count` bleiben unverändert die Datenbasis. `ActionSheetTitleCategory` bleibt im resx (wird weiterhin von `CategoriesPage` genutzt). |
| Floating Reader Control Bar | `Grid.Row="5"`-Container wird zu einem schwebenden Pill-`Border`: `Margin="16,0,16,12"` (Safe-Area bereits über `SafeAreaEdges="All"` der Seite), `StrokeShape="RoundRectangle 26"`, Höhe ≈ 52–54 pt, `Stroke` = `BorderSubtle`, `Background` = neuer Token `SurfaceCardTranslucent` (`#E6FFFFFF` / `#E6182234`), `Shadow` (weicher Ambient-Schatten). Die 6 vorhandenen Aktions-`Border` (44 × 44, transparent) bleiben im inneren `Grid` unverändert. | .NET MAUI hat kein natives Backdrop-Blur/Frosted-Glass; die Annäherung über halbtransparente Fläche + Hairline + Schatten (Ambient-Layered-Modell, Level 3 des Entwurfs) ist die einzige umsetzbare Variante ohne Drittanbieter-Abhängigkeit — als Entwurfsentscheidung festgelegt, nicht als offener Punkt. |
| Health-Status auf `FeedsPage` | Micro-Pill-Badge ersetzt die Vollbreiten-Statuszeile (Grid.Row 2): linksbündiger `Border` (`Padding="8,2"`, `RoundRectangle 9999`, `HorizontalOptions="Start"`) mit 6-pt-`BoxView`-Dot + `Label` (`LabelMetaStyle`); drei `DataTrigger` auf `HealthStatus` (`OK`/`Warning`/`Error`) setzen `Background` (neue `Status*Tint`-Tokens, 10-%-Alpha), Dot-`Color` (`Status*`) und `TextColor` (neue `Status*Text`-Tokens). Der Titel-Dot wird von 12 auf 8 pt verkleinert und bleibt am `Status*`-Token (deckt sich mit dem 8-px-Inline-Dot des Entwurfs). | Exakt die Badge-Spezifikation aus `editorial_feed/DESIGN.md` §3 (Micro-Pill, 6-px-Dot, `label-meta`, 10-%-Tint, dunklerer Statustext); die bestehende `DataTrigger`-Struktur wird wiederverwendet, nur Tokens/Geometrie ändern sich. |
| Karten-Token-Rolle | Kartenflächen durchgängig `SurfaceCard` + `Stroke` = `BorderSubtle` (Hairline); impliziter `Border`-Style in `Styles.xaml` bekommt `Stroke` = `BorderSubtle` statt `Outline`. `ArticleCardView` behält `SurfaceCard`, wechselt den Stroke zu `BorderSubtle`; `FeedsPage`-/`CategoriesPage`-/`SettingsPage`-Karten und der `ArticleDetailPage`-Quellen-Footer wechseln von `SurfaceContainer`/`Outline` zu `SurfaceCard`/`BorderSubtle`. Hinweis-Boxen (`SurfaceSubtle`, `Stroke="Transparent"`) bleiben. | Entwurf Level 1: reine Kartenfläche mit 1-px-Hairline statt sichtbarem Outline — `BorderSubtle` existiert bereits (`#e2e8f0`/`#334155`), deckt auch `border-hairline` (`rgba(15,23,42,0.06)`) ab; kein neuer Token nötig. |
| Ungelesen-Dot auf `ArticleCardView` | Der bestehende 8-pt-`Primary`-Akzent-Dot in der Kopfzeile wird zum 6-pt-`Secondary`-Ungelesen-Dot (`Cobalt`), per `DataTrigger` auf `IsRead=False` eingeblendet (Default: ausgeblendet). | Entwurf Stream Card: „unread dot indicator (6px cobalt circle)". Auf der `UnreadPage` ist der Dot damit immer sichtbar, auf `LaterPage` nur bei ungelesenen Artikeln. |
| Lesezeit auf `ArticleCardView` | `ItemListItem` erhält `ReadingTimeText` (`string?`), berechnet über neue gemeinsame Hilfsklasse `ReadingTimeEstimator` in `Reporter.Core` (Wortzählung aus `ContentHtml`, 200 wpm, `Math.Max(1, …)`, Formatierung via `AppResources.ArticleReadingTimeFormat`); das hartkodierte `"—"`-`Label` bindet `ReadingTimeText` und wird bei leerem Text per `StringNotEmptyToBoolConverter` ausgeblendet. `ArticleDetailViewModel.CalculateReadingTime` delegiert an `ReadingTimeEstimator`. | Lokalisierung liegt in `Reporter.Core` (resx dort), die Projektionen in `ItemRepository` lesen `ContentHtml` bereits — fertig formatierter Text ist konsistent mit `Summary`/`ImageUrl` (ebenfalls in der Projektion extrahiert) und vermeidet einen neuen Converter. |
| `LaterViewModel`-Paging | Gleiches Muster wie `UnreadViewModel`: `PageSize = 20`, `SavedItems` wird `ObservableCollection<ItemListItem>`, neue Member `IsLoading`/`HasMore`/`LoadMoreCommand`; `LaterPage`-`CollectionView` erhält `RemainingItemsThreshold="2"` + `RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}"`. `ToggleSavedAsync` entfernt den Eintrag in-place aus `SavedItems`, `MarkReadAsync` ersetzt ihn in-place durch eine `IsRead = true`-Kopie. | Exakt das etablierte Paging-Muster von `UnreadPage`/`UnreadViewModel`; in-place-Updates vermeiden Scrollsprünge und Vollreloads (konsistent zu `UnreadViewModel.ToggleSavedAsync`). |
| Sync-Batch-Insert | `RunSyncAsync` nutzt die bereits geladenen `existingItems` für eine In-Memory-`HashSet<string>`-Dedup auf `GuidOrHash` (deckt auch Duplikate innerhalb eines Feed-Dokuments ab, da neue Hashes ins Set eingetragen werden); neue `Item`-Entities werden gesammelt und einmalig über neues `IItemRepository.AddRangeAsync` (ein `DbContext`, ein `SaveChangesAsync`) persistiert. `GetByGuidOrHashAsync` wird aus Interface und Implementierung entfernt (einziger Aufrufer war der N+1-Pfad). | Beseitigt das N+1-Muster (pro Item ein `DbContext` + `SaveChanges`) ohne neue Infrastruktur; `GetByFeedAsync` lädt die Vergleichsdaten bereits. |
| Tab-Icons `AppShell` | Fünf neue `MauiImage`-SVG-Assets unter `Resources/Images/` (`tab_unread.svg`, `tab_feeds.svg`, `tab_later.svg`, `tab_categories.svg`, `tab_settings.svg`) mit einfachen Pfad-Glyphen im Material-Symbols-Stil des Entwurfs (newspaper, rss_feed, bookmark, folder/label, tune); `Icon` wird pro `Tab` im Code-Behind gesetzt, die Einfärbung übernimmt der bestehende implizite `Shell`-Style (`TabBarForegroundColor`/`TabBarUnselectedColor`). | Entwurfs-Tab-Bar zeigt Icon + Label; Material-Symbols-Font ist nicht im Repo gebündelt — handerstellte Vektor-Pfade sind der projektkonforme Weg (kein neues NuGet-Paket). |
| Programmsymbol / Splash | `appicon.svg` (Hintergrund) und `appiconfg.svg` (Vordergrund-Logo) werden als handerstellte Vektor-Umsetzung der Referenz `take_the_existing_logo_design_from_the_reference_image_data_image_image_13_the/screen.png` neu gezeichnet (im Repo liegt kein SVG-Ausgangsmaterial vor — recherchiert); `splash.svg` erhält dasselbe Logo-Motiv; `MauiIcon`-`Color` und `MauiSplashScreen`-`Color` wechseln verbindlich von `#512BD4` auf `#1e293b` (`LightPrimaryContainer`/`PrimaryContainer` der Design-Palette); `dotnet_bot.png` und seine `MauiImage Update`-Zeile entfallen (wird nirgends referenziert). Abnahme des Icon-Motivs über Screenshot-Vergleich mit der Referenz-PNG in der manuellen Verifikation (`test-results/issue-30/`). | Einzige Referenz ist die 1024×1024-PNG; der handerstellte SVG-Vektor-Nachbau ist der projektkonforme Asset-Weg (MauiIcon akzeptiert SVG) — als Entscheidung bestätigt, kein externes Ausgangsmaterial erforderlich. |
| `CategoriesPage` ohne eigenen Entwurf | Bestehendes Karten-Listen-Muster der übrigen Seiten übernehmen (Card-`Border` + `RoundRectangle 12` → `SurfaceCard`/`BorderSubtle`, Meta-Zeile `LabelMetaStyle`/`MetaStyle`, 44-pt-Touch-Ziele, `SemanticProperties`). | In der Anforderung bereits als Annahme formuliert und durch AGENTS.md-Kartenregel gedeckt — keine eigenständige Designfrage. |
| Kontrast `TextMuted`/`MetaStyle` | `MetaStyle` wechselt `TextColor` von `TextMuted` (`#94a3b8`, auf Weiß ≈ 3:1) zu `TextSecondary` (`#64748b`, ≈ 4,8:1); `TextMuted` bleibt für `PlaceholderColor`/Disabled-Zustände (kontrastbefreite Dekorations-/Hint-Texte) erhalten. Badge-Texte nutzen die dunkleren `Status*Text`-Tokens (Light `#047857`/`#B45309`/`#B91C1C`, Dark = `Status*`-Farben), weil `StatusWarning`/`StatusError` als Textfarbe auf Weiß < 4,5:1 liegen. | Kontrast-Richtwert ≥ 4,5:1 für Fließtext / ≥ 3:1 für Icons/Dots (Entwurf: „dots … minimum 3:1 … couple with text"); betrifft alle `MetaStyle`-Labels — gewollter globaler Feinschliff. |
| Dynamische Schriftgrößen | `FontAutoScalingEnabled` bleibt auf dem MAUI-Default (`true`) — Audit-Punkt: prüfen, dass kein Control es deaktiviert, und in den benannten Typo-Styles explizit `True` setzen (dokumentierte Absicht). | Der Wert wird aktuell nirgends gesetzt; Default-on erfüllt die Anforderung, explizite Setter verhindern versehentliches Deaktivieren. |

## Programmabläufe

### Kategoriefilter über Chip-Leiste (Ungelesen)

1. `UnreadPage.OnAppearing` → `UnreadViewModel.LoadAsync` befüllt `Categories` (erster Eintrag `FilterAll` mit `CategoryId = null`) und setzt `IsSelected` über `UpdateCategorySelection`.
2. Die horizontale Chip-`CollectionView` rendert je `CategoryFilterItem` eine Pill mit `Name` + `Count`-Kapsel; `DataTrigger` auf `IsSelected` schalten Aktiv-/Inaktiv-Tokens.
3. Tap auf einen Chip → `SelectCategoryCommand` mit dem `CategoryFilterItem` → `SelectCategoryAsync` setzt `SelectedCategory` (→ `UpdateCategorySelection` schaltet Chip-Styles), resettet Paging und lädt Seite 0 via `LoadPageAsync`.
4. `MarkReadCommand`/`MarkAllReadCommand`/`RefreshCommand` aktualisieren `CategoryFilterItem.Count` weiterhin — die Kapseln folgen über `ObservableObject`-Binding.

Beteiligte Klassen/Komponenten: `UnreadPage`, `UnreadViewModel`, `CategoryFilterItem`, `IItemRepository`, `ICategoryRepository`.

### Floating Reader Control Bar (Artikeldetail)

1. `ArticleDetailPage` bleibt `Grid`-basiert; Zeile 5 enthält statt der flachen Leiste den schwebenden Pill-`Border` (transluzent, Hairline, Schatten, Safe-Area-Margin).
2. Die sechs Aktions-`Border` (Zurück `GoBackCommand`, Bookmark `ToggleSavedForLaterCommand`, Schrift `ToggleFontSizeCommand`, Gelesen `ToggleMarkReadCommand`, Teilen `ShareCommand`, Browser `OpenInBrowserCommand`) bleiben mit ihren `SemanticProperties.Description`-Bindungen und `DataTrigger`n unverändert im inneren 6-Spalten-`Grid`.

Beteiligte Klassen/Komponenten: `ArticleDetailPage`, `ArticleDetailViewModel`.

### Feed-Health-Badge (Feeds)

1. `FeedsViewModel.LoadAsync` befüllt `Feeds` (`FeedListItem.HealthStatus` = `FeedHealth`-Konstante) — unverändert.
2. Das ItemTemplate rendert je Karte das Micro-Pill-Badge; `DataTrigger` auf `HealthStatus` (`"OK"`/`"Warning"`/`"Error"`) mappen auf `Status*Tint`-Hintergrund, `Status*`-Dot und `Status*Text`-Textfarbe; Badge-Text bleibt `HealthStatusOkLabel`/`WarningLabel`/`ErrorLabel`.
3. Tap auf die Karte → `OnFeedTapped` → `DisplayActionSheetAsync` — unverändert, neu mit `SemanticProperties.Description`/`Hint`.

Beteiligte Klassen/Komponenten: `FeedsPage`, `FeedsViewModel`, `FeedListItem`, `FeedHealth`.

### Batch-Sync mit In-Memory-Dedup

1. `SyncFeedAsync` → `RunSyncAsync`: `existingItems = GetByFeedAsync(feed.Id)` wie bisher; zusätzlich `HashSet<string>` über `GuidOrHash` aufbauen.
2. Pro `SyndicationItem`: `guidOrHash = NormalizeGuidOrHash(...)`; Treffer im HashSet → `continue`; sonst `Item` erzeugen, zur Insert-Liste hinzufügen, Hash ins Set aufnehmen.
3. Nach der Schleife: `AddRangeAsync(newItemEntities)` — ein `DbContext`, ein `SaveChangesAsync`.
4. `DetermineStatus`, `UpdateFeedHealthAsync`, `UpdateLogAsync`, `NotifyNewItemsAsync` unverändert.
5. `SyncAllAsync` iteriert weiterhin sequenziell pro Feed (Fehlerisolation bleibt).

Beteiligte Klassen/Komponenten: `FeedSyncService`, `IItemRepository`, `ItemRepository`, `INotificationService`.

### Paging in der Später-Liste

1. `LaterPage.OnAppearing` → `LoadCommand` → `LaterViewModel.LoadAsync`: lädt Seite 0 via `GetSavedForLaterAsync(0, PageSize)`, setzt `SavedItems` (neue `ObservableCollection`), `HasMore = items.Count == PageSize`.
2. Scroll-Anschlag → `RemainingItemsThresholdReachedCommand` → `LoadMoreCommand` → `LoadMoreAsync`: Seite++ und `GetSavedForLaterAsync(page, PageSize)`, Items werden angehängt.
3. `ToggleSavedAsync` → `ToggleSavedForLaterAsync(id)` + `SavedItems.Remove(item)` (in-place); `MarkReadAsync` → `MarkAsReadAsync(id)` + in-place-Austausch durch `IsRead = true`-Kopie.

Beteiligte Klassen/Komponenten: `LaterPage`, `LaterViewModel`, `IItemRepository`, `ItemRepository`, `ArticleCardView`.

### Design-/Accessibility-/Verifikations-Audit

1. Alle fünf Tab-Seiten + `ArticleDetailPage` gegen `screen.png`/`code.html` (Light + Dark) abgleichen; Abweichungsliste im Umsetzungsschritt pflegen.
2. Kontrast-Audit der Textfarben (Ergebnis: `MetaStyle` → `TextSecondary`, `Status*Text`-Tokens für Badges — s. Designentscheidungen) und der Status-Dots (≥ 3:1, mit Text-Äquivalent gekoppelt — durch Badge-Umbau erfüllt).
3. `FontAutoScalingEnabled`-Audit (kein Control deaktiviert es); `SemanticProperties`-Vollständigkeit über alle tap-basierten Elemente ohne sichtbaren Text.
4. `AutoRefreshService`/`FeedSyncService`-Review: kein UI-Thread-Marshal (bereits `ConfigureAwait(false)`, `PeriodicTimer` ohne Dispatcher — verifizieren und im Ergebnis dokumentieren).

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `ReadingTimeEstimator` | Statische Hilfsklasse (`src/Reporter.Core/Services/`) | `EstimateText(string? contentHtml)` — lokalisierte Lesezeit („{0} Min. Lesezeit"/„{0} min read") aus HTML-Wortzählung (200 wpm, min. 1); wird von `ItemRepository`-Projektionen (`ItemListItem.ReadingTimeText`) und `ArticleDetailViewModel` geteilt |

Keine weiteren neuen Klassen — neue resx-Schlüssel, Farbtokens, Styles und SVG-Assets sind Ressourcen, keine Klassen.

## Änderungen an bestehenden Klassen

### `IItemRepository` (Interface)

- **Neue Methoden:** `GetSavedForLaterAsync(int page, int pageSize)` → `Task<IReadOnlyList<ItemListItem>>` — gepaged, `PublishedAt` desc + `ThenBy(Id)`-Stabilisierung, gleiche Projektion wie die parameterlose Variante. `AddRangeAsync(IReadOnlyList<Item> items, CancellationToken cancellationToken = default)` → `Task` — Batch-Insert.
- **Entfernte Methoden:** `GetByGuidOrHashAsync(Guid feedId, string guidOrHash)` — einziger Aufrufer (`RunSyncAsync` N+1-Pfad) entfällt.
- **Geänderte Methoden:** parameterloses `GetSavedForLaterAsync()` bleibt vorerst bestehen, wenn kein weiterer Aufrufer mehr existiert, wird es mit `LaterViewModel`-Umstellung obsolet → wird ebenfalls entfernt und durch die paged Variante ersetzt.

### `ItemRepository` (Klasse)

- **Neue Methoden:** `GetSavedForLaterAsync(int page, int pageSize)` — `Skip`/`Take` + `ThenBy(Id)`, gleiche Projektion inkl. `ReadingTimeText`; `AddRangeAsync` — ein `DbContext` via `_factory`, `context.Items.AddRange`, ein `SaveChangesAsync`.
- **Geänderte Methoden:** Beide `ItemListItem`-Projektionen (`GetUnreadByDateAsync(paged)`, `GetSavedForLaterAsync`) füllen neu `ReadingTimeText` via `ReadingTimeEstimator.EstimateText(e.ContentHtml)`.
- **Entfernte Methoden:** `GetByGuidOrHashAsync` (s. Interface).

### `ItemListItem` (Datenmodellklasse)

- **Neue Eigenschaften:** `ReadingTimeText` (`string?`, init) — vorformatierte Lesezeit für die Karten-Meta-Zeile.

### `FeedSyncService` (Klasse)

- **Geänderte Methoden:** `RunSyncAsync` — In-Memory-Dedup über `HashSet<string>` aus `existingItems` (statt `GetByGuidOrHashAsync` pro Item); neue Items werden gesammelt und einmalig via `AddRangeAsync` eingefügt; Hashes neuer Items werden dem Set hinzugefügt (Dedup innerhalb eines Feed-Dokuments). `existingCount`/`lastPublishedAt`-Berechnung bleibt.

### `LaterViewModel` (Klasse)

- **Neue Eigenschaften:** `IsLoading`, `HasMore` (`bool`); Konstante `PageSize = 20`; Feld `_currentPage`.
- **Neue Methoden/Commands:** `LoadMoreCommand` (`AsyncRelayCommand`, `CanExecute`: `!IsLoading && HasMore`) → `LoadMoreAsync`.
- **Geänderte Eigenschaften:** `SavedItems` wird `ObservableCollection<ItemListItem>` (statt `IReadOnlyList<ItemListItem>`).
- **Geänderte Methoden:** `LoadAsync` — resettet Paging und lädt Seite 0 via `GetSavedForLaterAsync(0, PageSize)`; `ToggleSavedAsync` — entfernt `item` in-place aus `SavedItems` (kein Vollreload); `MarkReadAsync` — ersetzt den Eintrag in-place durch eine Kopie mit `IsRead = true` (Muster wie `UnreadViewModel.ToggleSavedAsync`).

### `UnreadViewModel` (Klasse)

- **Entfernte Eigenschaften:** `SelectedCategoryText` (samt Zuweisungen im `SelectedCategory`-Setter und `LoadPageAsync`) — Anzeige entfällt, Chips zeigen `Name` + `Count` direkt.
- **Unverändert:** `Categories`, `SelectedCategory`, `SelectCategoryCommand`, `UpdateCategorySelection`, `PageSize`, `LoadMoreCommand` — Datenbasis der Chip-Leiste.

### `ArticleDetailViewModel` (Klasse)

- **Geänderte Methoden:** `CalculateReadingTime` → ersetzt durch Aufruf `ReadingTimeEstimator.EstimateText(item.ContentHtml)` (gleiche Berechnung, zentralisiert).

### `UnreadPage` / `UnreadPage.xaml.cs` (View)

- Header-`VerticalStackLayout`: zwischen Aktionszeile und Fehler-Labels eine horizontale `CollectionView` (`ItemsLayout="HorizontalList"`, `ItemsSource="{Binding Categories}"`, ItemTemplate = Chip-`Border` s. Designentscheidung).
- Entfernt: Funnel-`Border` (Grid.Column 1), `OnFilterClicked` im Code-Behind, `SelectedCategoryText`-`Label`; Aktionszeilen-`ColumnDefinitions` entsprechend neu (`Auto,*,Auto`).
- `SemanticProperties.Description` ergänzen: Refresh-`Border` (`ButtonRefresh`), „Alles gelesen"-`Border` (`ButtonMarkAllRead`); Chip-`Border` (`{Binding Name}`).

### `ArticleDetailPage` (View)

- `Grid.Row="5"`-Container → schwebende Pill-Bar (s. Designentscheidung); sechs Aktions-`Border` unverändert.

### `FeedsPage` (View)

- Feed-Karten-Template: Statuszeile (Grid.Row 2) → Micro-Pill-Badge (s. Designentscheidung); Titel-Dot 12 → 8 pt; Karten-Tokens `SurfaceCard`/`BorderSubtle`.
- `SemanticProperties.Description="{Binding Title}"` + `SemanticProperties.Hint` (`AccessibilityTapForActions`) auf Feed- und Suchtreffer-Karten; Backdrop-`BoxView` erhält `SemanticProperties.Description` (`AccessibilityDismissSheet`); `NewUrlEntry` erhält `SemanticProperties.Description` (`PlaceholderFeedSearch`).
- Suchtreffer-Karten: Tokens `SurfaceCard`/`BorderSubtle`.

### `ArticleCardView` (View)

- Karten-`Stroke` → `BorderSubtle`; Kopfzeilen-Dot → 6 pt `Secondary` mit `DataTrigger` `IsRead=False` → sichtbar; Lesezeit-`Label` bindet `ReadingTimeText` (statt `"—"`), Sichtbarkeit via `StringNotEmptyToBoolConverter`; Bookmark-`DataTrigger` `Fill` → `BookmarkGold` (statt `Primary`).
- `SemanticProperties.Description`: Bookmark-`Border` = `ArticleBookmarkSet` mit `DataTrigger` `IsSavedForLater=True` → `ArticleBookmarkRemove`; MarkRead-`Border` = `ArticleMarkAsRead`; Tap-Overlay-`Grid` = `{Binding Title}` + `Hint` (`AccessibilityTapForActions`).

### `LaterPage` (View)

- `CollectionView`: `RemainingItemsThreshold="2"` + `RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}"`; `ActivityIndicator` für `IsLoading` am unteren Rand (Muster `UnreadPage`).

### `CategoriesPage` (View)

- `SemanticProperties.Description` auf `CategoryNameEntry` (`LabelCategoryName`) und auf Karten-`Border` (`{Binding Name}` + `Hint` `AccessibilityTapForActions`); Karten-Tokens `SurfaceCard`/`BorderSubtle`.

### `SettingsPage` (View)

- Sektions-Karten: Tokens `SurfaceCard`/`BorderSubtle`; sonst nur Abgleich (bestehende `SemanticProperties` bleiben).

### `AppShell.xaml.cs` (Code-Behind)

- Pro `Tab`/`ShellContent` `Icon` setzen (fünf neue SVG-Assets, s. Designentscheidung); Titel-Logik unverändert.

### `Colors.xaml` (Ressource)

- Neue Tokens (jeweils `Light*`/`Dark*`): `SurfaceCardTranslucent` (`#E6FFFFFF`/`#E6182234`), `StatusOkTint` (`#1A10B981`/`#1A10B981`), `StatusWarningTint` (`#1AF59E0B`/`#1AF59E0B`), `StatusErrorTint` (`#1AEF4444`/`#1AF43F5E`), `StatusOkText` (`#047857`/`#10b981`), `StatusWarningText` (`#B45309`/`#f59e0b`), `StatusErrorText` (`#B91C1C`/`#f43f5e`), `ChipCountCapsule` (`#33FFFFFF`/`#33000000`).
- `DarkOnSurfaceVariant` (`#d8c3ad`) gegen `reporter_editorial_dark/DESIGN.md` verifizieren — entspricht dem Entwurf, keine Änderung erwartet.

### `Styles.xaml` (Ressource)

- Neue benannte Label-Styles: `LabelMdStyle` (InterMedium 12, `TextSecondary`), `LabelSmStyle` (InterSemiBold 11, `TextSecondary`), `LabelMetaStyle` (InterSemiBold 10, `TextSecondary`); `FontAutoScalingEnabled="True"` in den benannten Typo-Styles explizit setzen.
- `MetaStyle`: `TextColor` → `TextSecondary` (Kontrast).
- Impliziter `Border`-Style: `Stroke` → `BorderSubtle`.
- `body-reading`: kein nativer Style nötig — der Lesemodus rendert im `WebView` mit Newsreader 19 px (in `ArticleDetailViewModel.RebuildHtml` vorhanden, im Audit zu verifizieren).

### `AppResources.resx` / `AppResources.de.resx` (+ Designer)

- Neue Schlüssel: `AccessibilityDismissSheet` („Schließen"/„Close"), `AccessibilityTapForActions` („Tippen für Aktionen"/„Double-tap for actions"). Alle übrigen Accessibility-Texte werden durch vorhandene Schlüssel abgedeckt (`ButtonRefresh`, `ButtonMarkAllRead`, `ArticleBookmarkSet`/`Remove`, `ArticleMarkAsRead`, `LabelFeedActions`-Pattern, `LabelCategoryName`, `PlaceholderFeedSearch`).

### `Reporter.csproj` / App-Assets

- `MauiIcon Color` und `MauiSplashScreen Color`: `#512BD4` → Design-Palette (Vorschlag `#1e293b`); `MauiImage Update="Resources\Images\dotnet_bot.png"` entfernen; Datei `Resources/Images/dotnet_bot.png` löschen; fünf neue `tab_*.svg` unter `Resources/Images/` (werden per `MauiImage Include="Resources\Images\*"` automatisch eingebunden).
- `appicon.svg`, `appiconfg.svg`, `splash.svg`: neue Vektor-Umsetzung des Referenz-Logos.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — es gibt keine neuen oder geänderten Eingaben.

## Konfigurationsänderungen

Keine — `Settings.Theme`/`Settings.Language` und OS-gestützte Schriftskalierung bleiben unverändert.

## Seiteneffekte und Risiken

- **`IItemRepository`-Contract-Änderung:** Der `FailingItemRepository`-Fake in `UnreadViewModelTests` implementiert das Interface manuell — neue Member müssen dort delegiert werden, `GetByGuidOrHashAsync` und `GetSavedForLaterAsync()` (parameterlos) entfallen. Weitere Implementierungen existieren nicht.
- **`MetaStyle`-Farbwechsel** auf `TextSecondary`: wirkt global auf alle Meta-Labels (Zeitstempel, Quellen, Counts) — gewollt, erhöht Kontrast; visueller Regressionscheck nötig.
- **Impliziter `Border`-Stroke** wechselt zu `BorderSubtle`: alle `Border` ohne expliziten `Stroke` werden dezenter — gewollt, aber auf sehr feine Darstellung bei niedriger Helligkeit prüfen; Elemente, die bewusst `Outline` brauchen, müssten ihn explizit setzen (Audit).
- **`SavedItems`-Typwechsel** (`IReadOnlyList` → `ObservableCollection`): Binding-kompatibel; `LaterViewModelTests` prüfen Listeninhalte — in-place-Verhalten muss mit den bestehenden Assertions verträglich bleiben.
- **Entfernen von `OnFilterClicked`/`SelectedCategoryText`:** der ActionSheet-Auswahlweg auf `UnreadPage` entfällt — bewusste Designentscheidung; `UnreadViewModel`-Tests zum Filter bleiben gültig (`SelectCategoryCommand` unverändert).
- **Sync-Änderung:** `SyncFeedAsync_Duplicates_SkipsExistingItems` deckt den gleichen fachlichen Pfad weiter ab; die Reihenfolge „alle Inserts nach dem Parsen" ändert das Verhalten bei Abbruch mitten im Parsing (vorher: teilweise persistiert, nachher: nichts) — als Verbesserung dokumentiert (atomarer Insert-Lauf).
- **Tab-Icons/Icon-SVGs:** Windows-Build lokal prüfbar; iOS-Rendering der `MauiImage`-Icons nur auf macOS verifizierbar.
- **`FontAutoScalingEnabled`-Setter** in Typo-Styles: explizit `True` ist der Default — kein Verhaltensrisiko.

## Umsetzungsreihenfolge

1. **Design-Audit und Abweichungsliste**
   - Voraussetzungen: Keine.
   - Beschreibung: Alle Seiten gegen `design-draft/stitch_local_rss_feed_reader/{ungelesen_dashboard, feeds_health_status, f_r_sp_ter_bewahren, artikel_lesemodus, einstellungen_filter}[_dark_mode]/{screen.png,code.html}` abgleichen; `CategoriesPage` nach Karten-Listen-Muster; Ergebnis als Arbeitsliste für die Folgeschritte.

2. **Neue resx-Schlüssel**
   - Voraussetzungen: Keine.
   - Beschreibung: `AccessibilityDismissSheet`, `AccessibilityTapForActions` in `AppResources.resx` + `AppResources.de.resx` eintragen (Designer wird beim Build generiert).

3. **Farbtokens und Typo-Styles**
   - Voraussetzungen: Keine.
   - Beschreibung: `Colors.xaml` um `SurfaceCardTranslucent`, `Status*Tint`, `Status*Text`, `ChipCountCapsule` (jeweils Light/Dark) ergänzen; `Styles.xaml` um `LabelMdStyle`/`LabelSmStyle`/`LabelMetaStyle` ergänzen, `MetaStyle` → `TextSecondary`, impliziter `Border`-Stroke → `BorderSubtle`, `FontAutoScalingEnabled="True"` in benannten Typo-Styles; `DarkOnSurfaceVariant` verifizieren.

4. **`ReadingTimeEstimator` + `ItemListItem.ReadingTimeText`**
   - Voraussetzungen: Keine.
   - Beschreibung: Hilfsklasse in `src/Reporter.Core/Services/` anlegen (Logik aus `ArticleDetailViewModel.CalculateReadingTime` übernehmen, 200 wpm); `ItemListItem` um `ReadingTimeText` erweitern; `ArticleDetailViewModel` auf die Hilfsklasse umstellen.

5. **`IItemRepository`/`ItemRepository` erweitern und aufräumen**
   - Voraussetzungen: Schritt 4 (Projektionen füllen `ReadingTimeText` gleich mit).
   - Beschreibung: `GetSavedForLaterAsync(int page, int pageSize)` und `AddRangeAsync` einführen; `GetByGuidOrHashAsync` und das parameterlose `GetSavedForLaterAsync()` entfernen (nach Umstellung der Aufrufer in Schritt 6/7); `FailingItemRepository`-Fake anpassen.

6. **`FeedSyncService` Batch-Insert + In-Memory-Dedup**
   - Voraussetzungen: Schritt 5 (`AddRangeAsync`).
   - Beschreibung: `RunSyncAsync` auf HashSet-Dedup + `AddRangeAsync` umstellen; Fehlerisolation/Statuslogik unverändert.

7. **`LaterViewModel`-Paging + `LaterPage`-Anbindung**
   - Voraussetzungen: Schritt 5 (paged `GetSavedForLaterAsync`).
   - Beschreibung: `SavedItems` → `ObservableCollection`, `IsLoading`/`HasMore`/`LoadMoreCommand`/`PageSize`; `LaterPage` um `RemainingItemsThreshold`/`RemainingItemsThresholdReachedCommand`/`ActivityIndicator` ergänzen; in-place Toggle/MarkRead.

8. **`UnreadPage`: Chip-Leiste + Header-A11y**
   - Voraussetzungen: Schritte 2, 3 (resx, Tokens, `LabelMetaStyle`, `ChipCountCapsule`).
   - Beschreibung: Horizontale Chip-`CollectionView` mit Pill-Template + `IsSelected`-`DataTrigger`n; Funnel-`Border`, `OnFilterClicked` und `SelectedCategoryText` entfernen (`UnreadViewModel.SelectedCategoryText` ebenfalls); `SemanticProperties` auf Refresh-/MarkAll-`Border` und Chips.

9. **`ArticleCardView` Polish**
   - Voraussetzungen: Schritte 3, 4 (Tokens, `ReadingTimeText`).
   - Beschreibung: `Stroke` → `BorderSubtle`, 6-pt-`Secondary`-Ungelesen-Dot, `ReadingTimeText`-Bindung, `BookmarkGold`-Fill, `SemanticProperties` auf Aktions-`Border`n + Tap-Overlay.

10. **`FeedsPage`: Micro-Pill-Badge + Karten-A11y**
    - Voraussetzungen: Schritte 2, 3.
    - Beschreibung: Statuszeile → Pill-Badge mit `Status*Tint`/`Status*Text`-Triggern, Titel-Dot 8 pt, `SurfaceCard`/`BorderSubtle`-Tokens auf Feed-/Suchtreffer-Karten, `SemanticProperties`/`Hint` auf Karten, Backdrop, `NewUrlEntry`.

11. **`ArticleDetailPage`: Floating Bar**
    - Voraussetzungen: Schritt 3 (`SurfaceCardTranslucent`, `BorderSubtle`).
    - Beschreibung: Row-5-Container → schwebende Pill-`Border` (52–54 pt, `RoundRectangle 26`, `Margin`, `Shadow`); Aktions-`Border` unverändert.

12. **`LaterPage`/`CategoriesPage`/`SettingsPage` Token- und A11y-Pass**
    - Voraussetzungen: Schritte 2, 3.
    - Beschreibung: Karten-Tokens, `SemanticProperties` (Kategorien-`Entry`, Karten), Touch-Ziel-Abgleich.

13. **Tab-Icons + Programmsymbol/Splash**
    - Voraussetzungen: Keine.
    - Beschreibung: Fünf `tab_*.svg` unter `Resources/Images/` anlegen, `Icon` in `AppShell.xaml.cs` setzen; `appicon.svg`/`appiconfg.svg`/`splash.svg` nach Referenz neu zeichnen; `MauiIcon`/`MauiSplashScreen`-`Color` anpassen; `dotnet_bot.png` entfernen.

14. **Performance-/Accessibility-Audit (Rest)**
    - Voraussetzungen: Schritte 6, 7.
    - Beschreibung: `AutoRefreshService`-UI-Thread-Verhalten verifizieren (bereits marshal-frei — dokumentieren), `CollectionView`-Templates auf schwere Elemente prüfen (`Image`-URI-Caching-Default notieren), `FontAutoScalingEnabled`-Audit abschließen, Kontrast-Audit dokumentieren.

15. **Tests schreiben/anpassen**
    - Voraussetzungen: Schritte 4–8.
    - Beschreibung: Neue Unit-Tests (s. Abschnitt Tests) + `FailingItemRepository`-Anpassung (in Schritt 5 bereits erfolgt).

16. **Verifikation und Doku**
    - Voraussetzungen: Schritte 1–15.
    - Beschreibung: `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release`; `.\scripts\Run-StaticChecks.ps1`; manuelle UI-Verifikation am 390 × 844-pt-Windows-Fenster (Light + Dark) mit UIA + Screenshots → `test-results.md` und `docs/help/anwendung/mobile-ui-design.md` (neuer Abschnitt „UI-Polish (issue-30)"). Accessibility-Nachweis verbindlich über **Accessibility Insights for Windows (FastPass)** auf dem 390 × 844-pt-Fenster plus Narrator-Durchlauf; „kritisch" = FastPass-Fehler (fehlende `Name`-Properties, Kontrast < 4,5:1 Fließtext / < 3:1 Icons) — Ergebnis in `test-results.md` dokumentieren. Performance-Nachweis als dokumentierte manuelle Beobachtung (Scroll-/Sync-Verhalten im 390 × 844-Fenster, kein hartes Budget); optional Sync-Dauer vorher/nachher für einen Referenzfeed in `test-results.md` notieren. Icon-/Splash-Abnahme über Screenshot-Vergleich mit der Referenz-PNG. iOS-Verifikation (`net10.0-ios`-Build, `scripts/iOS-Deployment.ps1`) als dokumentierte Folgeaufgabe in `test-results.md` offen lassen — wie bei Issues #27/#28; der lokale Nachweis bleibt die Windows-Verifikation.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `AddRangeAsync_InsertsAllItems` | `ItemRepositoryTests` | Batch-Insert persistiert alle übergebenen Items (über `GetByFeedAsync`/`GetAllAsync` nachlesen) |
| `AddRangeAsync_EmptyList_DoesNothing` | `ItemRepositoryTests` | Leere Liste → kein Fehler, keine Inserts |
| `GetSavedForLaterAsync_Paged_ReturnsPage` | `ItemRepositoryTests` | Seite 0/1 liefern disjunkte `PageSize`-Blöcke, `PublishedAt` desc, stabil über `ThenBy(Id)` |
| `LoadMoreCommand_AppendsNextPage` | `LaterViewModelTests` | > `PageSize` gespeicherte Items → Nachladen hängt Seite 2 an |
| `LoadMoreCommand_WhenNoMoreItems_DoesNothing` | `LaterViewModelTests` | `HasMore=false` → `LoadMore` lädt nichts nach |
| `ToggleSavedCommand_RemovesItemInPlace` | `LaterViewModelTests` | Eintrag verschwindet ohne Vollreload aus `SavedItems` |
| `MarkReadCommand_UpdatesItemInPlace` | `LaterViewModelTests` | Eintrag bleibt in `SavedItems`, `IsRead = true` |
| `SyncFeedAsync_DuplicatesWithinSameDocument_InsertsOnce` | `FeedSyncServiceTests` | Feed-Dokument mit doppeltem `GuidOrHash` → nur ein Insert (HashSet-Dedup im Dokument) |
| `SyncFeedAsync_MixedNewAndExisting_BatchInsertsOnlyNew` | `FeedSyncServiceTests` | Teilweise bekannte Items → nur neue persistiert (Dedup gegen `existingItems`) |
| `EstimateText_ReturnsMinutesForContent` / `EstimateText_EmptyContent_ReturnsEmpty` | `ReadingTimeEstimatorTests` (neu) | Wortzählung → gerundete Minuten (min. 1), leeres HTML → leerer String; HTML-Tags werden entfernt |
| `SelectCategoryCommand_UpdatesChipSelectionState` | `UnreadViewModelTests` | `IsSelected` wird auf genau einem `CategoryFilterItem` gesetzt (Chip-Aktivzustand) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `FailingItemRepository` (nested in `UnreadViewModelTests`) | `IItemRepository` erhält `GetSavedForLaterAsync(page,pageSize)` + `AddRangeAsync`; `GetByGuidOrHashAsync` + parameterloses `GetSavedForLaterAsync` entfallen — Delegaten anpassen |
| `LaterViewModelTests.ToggleSavedCommand_RemovesItemFromSavedItems` / `MarkReadCommand_SetsReadAndKeepsItemInList` | Verhalten jetzt in-place statt Vollreload — Assertions auf `SavedItems` bleiben gültig, ggf. Typwechsel (`ObservableCollection`) nachziehen |
| `ItemRepositoryTests.GetSavedForLaterAsync_ReturnsOnlySaved` / `…_OrdersByPublishedAtDescending` | Parameterlose Methode entfällt → auf paged Signatur umstellen |
| `FeedSyncServiceTests.SyncFeedAsync_Duplicates_SkipsExistingItems` | Dedup läuft jetzt über In-Memory-HashSet — Test bleibt fachlich gültig, Verhalten verifizieren |
| `ServiceCollectionTests` | DI-Auflösung `IItemRepository` unverändert — kein Bruch erwartet |

### E2E-Tests (primärer Funktionsnachweis)

Es existieren **keine automatisierten UI-Tests** im Repo (`src/Reporter.Tests` deckt nur Core/Data ab). Der primäre Funktionsnachweis für die geänderten Benutzerflüsse erfolgt daher als **dokumentierte manuelle Verifikation** gemäß `AGENTS.md`: App als unpackaged `win-x64`-Build im 390 × 844-pt-Fenster (Fenstergröße per `GetWindowRect` verifizieren, Interaktion via UI Automation + `mouse_event` wie in `test-results/issue-59/uia.ps1` geübt), Theme-Wechsel über `settings.theme` in der SQLite-DB mit Neustart, Screenshots unter `test-results/issue-30/`, Protokoll in `test-results.md` + `docs/help/anwendung/mobile-ui-design.md`. Der Accessibility-Nachweis erfolgt verbindlich über **Accessibility Insights for Windows (FastPass)** auf dem 390 × 844-pt-Fenster plus Narrator-Durchlauf — „kritisch" = FastPass-Fehler (fehlende `Name`-Properties, Kontrast < 4,5:1 Fließtext / < 3:1 Icons). Die iOS-Verifikation bleibt wie bei Issues #27/#28 dokumentierte Folgeaufgabe; der lokale Nachweis ist die Windows-Verifikation.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Ungelesen: Chip-Leiste sichtbar; Tap auf „Technologie"-Chip → Liste filtert, Chip aktiv (dunkle Pill + Count-Kapsel), andere Chips inaktiv; horizontales Scrollen bei vielen Kategorien; Refresh-/„Alles gelesen"-Buttons funktionieren | Manuell: `test-results/issue-30/manual-unread-*.png` (Light + Dark) | Sichtbare Chip-Leiste statt ActionSheet; Design-Abgleich `ungelesen_dashboard` | Reine UI-/Interaktionsverifikation — XAML-Trigger und horizontales Scrollen sind nicht unit-testbar |
| Pflicht | Artikeldetail: schwebende Pill-Bar mit 16-pt-Rändern, transluzenter Fläche, Hairline + Schatten; alle 6 Aktionen erreichbar; Bookmark-Toggle füllt `BookmarkGold` | Manuell: `manual-detail-*.png` (Light + Dark) | Floating Reader Control Bar entsprechend Entwurf | Visuelle Verifikation (Transparenz, Schatten, Safe-Area) nicht automatisierbar |
| Pflicht | Feeds: Micro-Pill-Badges für `OK`/`Warning`/`Error` (10-%-Tint-Hintergrund, 6-pt-Dot, dunklerer Statustext); Karten-Tap öffnet ActionSheet; Backdrop-Tap schließt Sheet | Manuell: `manual-feeds-*.png`; `HealthStatus` per SQLite-Seed/Feed-Fehler variieren | Status-Badges/Health-Indikatoren nach Entwurf | Trigger-Farbwechsel nur visuell prüfbar |
| Pflicht | Später: Liste mit > 20 gespeicherten Artikeln (per SQLite-Seed) → Scroll-Anschlag lädt Seite 2 nach; Bookmark-Entfernung löscht Karte in-place | Manuell: `manual-later-*.png` | Flüssige Infinity-Liste / Paging | Scroll-Schwelle und inkrementelles Anhängen nur im laufenden Fenster nachweisbar |
| Pflicht | Accessibility: UIA-`Name`-Properties der icon-only/Tap-Elemente (Refresh, Alles gelesen, Chips, Karten-Overlay, Feed-/Kategorie-/Suchtreffer-Karten, Backdrop, Bookmark-/Gelesen-Aktionen) sind gesetzt; **Accessibility Insights FastPass** auf allen Seiten ohne kritische Fehler; Narrator-Durchlauf über Ungelesen + Artikeldetail | Manuell: UIA-Inspektion + FastPass-Report + `manual-a11y-*.png` | Screenreader-Bedienbarkeit aller interaktiven Elemente; „keine kritischen Fehler" = FastPass-frei | `SemanticProperties` wirken nur auf Plattform-Ebene — nicht unit-testbar |
| Pflicht | Kontrast/Dark-Mode: alle 5 Tabs + Detail in Light UND Dark gegen `screen.png`-Referenzen; Meta-Texte lesbar (`TextSecondary`), Badge-Texte kontrastsicher | Manuell: `manual-dark-*.png`/`manual-light-*.png` | Kontraste ≥ Richtwerte, `AppThemeBinding`-Vollständigkeit | Visuelle Verifikation |
| Pflicht | Programmsymbol + Tab-Icons: Windows-Fenster-/Taskleisten-Icon und Splash zeigen das neue Logo; Abnahme des Motivs über Screenshot-Vergleich mit der Referenz-PNG; Tab-Bar zeigt 5 Icons + Labels | Manuell: `manual-icon-*.png` inkl. Vergleich mit `take_the_existing_logo_design_…_13_the/screen.png` | Programmsymbol = Referenz-Motiv auf `#1e293b`-Hintergrund; Tab-Icons vorhanden | Build-Asset wirkt erst zur Laufzeit sichtbar |
| Pflicht | Sync-Performance: Refresh über mehrere Feeds mit jeweils vielen neuen Items — UI bleibt reaktiv (kein Freeze während Pull-to-Refresh); Seitenladen + Scrollen flüssig; optional Sync-Dauer vorher/nachher für einen Referenzfeed notieren | Manuell + Beobachtung (kein hartes Budget); ergänzend `dotnet test`-Nachweis des Batch-Pfads | Nicht blockierender Sync, schnelle DB-Zugriffe | End-to-End-Gefühl (Responsiveness) nur manuell beurteilbar |

Bestehende E2E-Tests: **Keine vorhanden** — es gibt keine automatisierte UI-Testsuite, die angepasst werden müsste. Die manuelle Verifikationshistorie in `test-results.md`/`mobile-ui-design.md` wird um den Issue-#30-Abschnitt ergänzt; die dort dokumentierten Regressions-Checklisten (vorangegangene Issues) sind bei der Verifikation mitzuprüfen.

## Offene Punkte

Keine — alle zuvor offenen Punkte wurden verbindlich entschieden und sind im Plan verankert: Accessibility-Referenz (Accessibility Insights for Windows, FastPass + Narrator; „kritisch" = FastPass-Fehler) in Schritt 16 und dem Accessibility-E2E-Szenario; iOS-Verifikation als dokumentierte Folgeaufgabe (Windows-Verifikation bleibt der lokale Nachweis) in Schritt 16; Performance ohne hartes Budget (dokumentierte manuelle Beobachtung, optionale Sync-Dauer-Notiz) in Schritt 16 und dem Performance-E2E-Szenario; finales Icon-Artwork (handerstelltes SVG-Vektor-Motiv nach der PNG-Referenz, `#1e293b`-Hintergrund, Abnahme über Screenshot-Vergleich) in den Designentscheidungen und dem Icon-E2E-Szenario.
