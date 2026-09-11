# Logik – Bestandsaufnahme

## `ItemRepository`
Datei: `src/Reporter.Data/Repositories/ItemRepository.cs` — implementiert `IItemRepository`, nutzt `IDbContextFactory<ReporterDbContext>` (kurzlebige Contexts).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync()` | public | Alle Items, `OrderByDescending(PublishedAt)` |
| `GetByIdAsync(Guid)` | public | Einzelnes Item oder `null` |
| `AddAsync(Item)` | public | Fügt Item hinzu (`MapToEntity`) |
| `UpdateAsync(Item)` | public | Aktualisiert alle Felder **inkl. `IsSavedForLater`** (Zeile 71); kein Fund → no-op |
| `DeleteAsync(Guid)` | public | Löscht Item **ohne `IsSavedForLater`-Prüfung**; kein produktiver Aufrufer (nur `ItemRepositoryTests`) |
| `GetUnreadByDateAsync()` | public | Ungelesene Items, `PublishedAt` absteigend |
| `GetUnreadByDateAsync(int,int,Guid?)` | public | Paged Unread-Liste als `ItemListItem` inkl. `IsSavedForLater`, `ImageUrl`, `Summary` |
| `GetUnreadCountAsync(Guid?)` | public | Anzahl ungelesener Items, optional kategoriegefiltert |
| `MarkAllAsReadAsync(Guid?)` | public | `ExecuteUpdateAsync` → `IsRead=true`, `ReadAt=UtcNow` |
| `ToggleSavedForLaterAsync(Guid)` | public | `FindAsync` → invertiert `IsSavedForLater` → `SaveChanges`; kein Fund → no-op (Zeilen 198–209) |
| `MarkAsReadAsync(Guid)` | public | Setzt `IsRead`/`ReadAt` |
| `GetByFeedAsync(Guid)` / `GetByCategoryAsync(Guid)` | public | Feed-/Kategorie-Listen, `PublishedAt` absteigend |
| `GetSavedForLaterAsync()` | public | **`Where(i => i.IsSavedForLater)` + `OrderByDescending(i => i.PublishedAt)`**, Projektion auf `ItemListItem` inkl. `FeedTitle`/`CategoryName`/`ImageUrl`/`Summary` (Zeilen 254–292) |
| `GetByGuidOrHashAsync(Guid,string)` | public | Duplikatprüfung für `FeedSyncService` |
| `ExtractImageUrl` / `ExtractSummary` | private static | HTML→Bild-URL / Plain-Text-Kurzfassung |
| `MapToModel` / `MapToEntity` | private static | Mapping inkl. `IsSavedForLater` in beiden Richtungen |

Events: keine.

## `LaterViewModel`
Datei: `src/Reporter.Core/ViewModels/LaterViewModel.cs` — Singleton (DI), `BaseViewModel`.

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `LaterViewModel(IItemRepository)` | public ctor | Legt `LoadCommand`, `ToggleSavedCommand`, `MarkReadCommand` an |
| `LoadCommand` | public | `AsyncRelayCommand` → `LoadAsync` |
| `ToggleSavedCommand` | public | `AsyncRelayCommand<ItemListItem?>` → `ToggleSavedAsync` |
| `MarkReadCommand` | public | `AsyncRelayCommand<ItemListItem?>` → `MarkReadAsync` |
| `Title` | public | Initial `AppResources.PageTitleLater` |
| `SavedItems` | public | `IReadOnlyList<ItemListItem>`, benachrichtigt via `SetProperty` |
| `LoadAsync()` | private | `SavedItems = await _itemRepository.GetSavedForLaterAsync()` |
| `ToggleSavedAsync(ItemListItem?)` | private | `ToggleSavedForLaterAsync(item.Id)` + `LoadAsync()` → nicht mehr bewahrte Artikel verschwinden aus der Liste |
| `MarkReadAsync(ItemListItem?)` | private | `MarkAsReadAsync(item.Id)` + `LoadAsync()` |

Events: keine eigenen; `PropertyChanged` über `BaseViewModel`/`ObservableObject`. Kein Error-Handling/Fehlerzustand (im Gegensatz zu `UnreadViewModel`).

## `UnreadViewModel` (feature-relevante Ausschnitte)
Datei: `src/Reporter.Core/ViewModels/UnreadViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ToggleSavedCommand` / `ToggleSavedAsync(ItemListItem?)` | public / private | `ToggleSavedForLaterAsync(item.Id)`, dann In-place-Ersatz des `ItemListItem` in `Articles` mit invertiertem `IsSavedForLater` (Zeilen 368–396) — aktualisiert das Bookmark-Icon ohne Reload |
| `MarkReadAsync(ItemListItem?)` | private | `MarkAsReadAsync` + `Articles.Remove(item)` (Liste enthält nur Ungelesene) |

`RefreshAsync` ruft `FeedSyncService.SyncAllAsync` — einzige produktive Sync-Aufrufstelle neben `FeedsViewModel`.

## `ArticleDetailViewModel` (feature-relevante Ausschnitte)
Datei: `src/Reporter/ViewModels/ArticleDetailViewModel.cs` — Achtung: Datei liegt in `src/Reporter/` (MAUI-Assembly), Namespace `Reporter.Core.ViewModels`; **nicht vom Testprojekt referenzierbar** (`Reporter.Tests` referenziert nur `Reporter.Core` + `Reporter.Data`).

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `ToggleSavedForLaterCommand` / `ToggleSavedForLaterAsync()` | public / private | `ToggleSavedForLaterAsync(Item.Id)` + `Item = CreateItemCopy(..., isSavedForLater: !Item.IsSavedForLater)` (Zeilen 443–452) |
| `BookmarkButtonLabel` | public | `"Lesezeichen entfernen"` / `"Lesezeichen setzen"` je nach `Item.IsSavedForLater`; `OnPropertyChanged` bei `Item`-Wechsel (Zeilen 78–82, 179) |
| `LoadAsync(Guid)` | public | Lädt Settings (Fallback `RetentionDays = 30` hartkodiert, Zeile 241), Item, Feed; startet Auto-Gelesen-Timer |
| `CreateItemCopy(...)` | private | Erzeugt `Item`-Kopie mit geänderten Flags |

## `FeedSyncService` — **keine Löschlogik**
Datei: `src/Reporter.Core/Services/FeedSyncService.cs` — implementiert `IFeedSyncService`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(Guid, CancellationToken)` | public | Sync eines Feeds mit `SyncLog`-Protokollierung |
| `SyncAllAsync(CancellationToken)` | public | Iteriert alle Feeds, aggregiert `SyncResult` |
| `RunSyncAsync(Feed, SyncLog, CancellationToken)` | private | Lädt Feed-XML, legt **nur neue** Items an (`IsSavedForLater = false`, Zeile 149); Duplikate via `GetByGuidOrHashAsync` übersprungen. **Kein `Remove`/`Delete`** — nicht gefetchte oder alte Items bleiben unangetastet |
| `DetermineStatus(...)` | private static | Health-Heuristik (Warning bei stark reduziertem Feed oder > 30 Tage ohne neue Items) |
| `UpdateFeedHealthAsync` / `UpdateLogAsync` | private | Persistiert Health-Status / Sync-Log |

Tests bestätigen das Nicht-Löschen: `SyncFeedAsync_Unreachable_KeepsItemsAndLogsError`, `SyncFeedAsync_InvalidXml_SetsError`.

## `App` — **kein Cleanup beim Start**
Datei: `src/Reporter/App.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnStart()` | protected override | Scope → `ReporterDbContext` → `Database.MigrateAsync()` — **sonst nichts** (kein Retention-Cleanup, kein Sync) |
| `CreateWindow(...)` | protected override | `AppShell`-Fenster; Windows: 390 × 844 pt Smartphone-Größe |

## `MauiProgram` — **kein Hintergrunddienst**
Datei: `src/Reporter/MauiProgram.cs` — `CreateMauiApp()` registriert `AddDbContextFactory<ReporterDbContext>` (SQLite `reporter.db`), Repositories und `IFeedSyncService` als Singletons, ViewModels (`LaterViewModel` Singleton), Pages (`LaterPage` Transient) und `AppShell`. Keine Hosted Services, Timer oder Hintergrund-Synchronisation.

## `FeedsViewModel` — einzige produktive Lösch-Kaskade
Datei: `src/Reporter.Core/ViewModels/FeedsViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DeleteCommand` / `DeleteAsync(FeedListItem?)` | public / private | `IFeedRepository.DeleteAsync(feed.Id)` (Zeilen 268–286) → `FeedRepository` entfernt Feed; via `DeleteBehavior.Cascade` werden **alle Items des Feeds gelöscht, auch bewahrte**. Nutzerinitiierte Aktion ohne Rückfrage im ViewModel. |

## `FeedRepository`
Datei: `src/Reporter.Data/Repositories/FeedRepository.cs` — `DeleteAsync(Guid)` (Zeilen 73–84) entfernt Feed-Entity; Kaskade siehe oben.

## `SettingsRepository`
Datei: `src/Reporter.Data/Repositories/SettingsRepository.cs` — `GetAsync()` legt Singleton-Datensatz bei Bedarf an; `SaveAsync(Settings)` schreibt u. a. `RetentionDays`. Kein Verbraucher von `RetentionDays` in der Anwendung.

## `SettingsViewModel` / `SettingsPage`
Dateien: `src/Reporter.Core/ViewModels/SettingsViewModel.cs`, `src/Reporter/Views/SettingsPage.xaml` — `SettingsViewModel.LoadAsync` lädt `Settings` in Property `Settings`; die Seite ist ein **Platzhalter** (`PlaceholderSettings`), `RetentionDays` ist in keiner UI konfigurierbar.

## `LaterPage`
Dateien: `src/Reporter/Views/LaterPage.xaml` + `.xaml.cs` — `Grid RowDefinitions="Auto,*"`; `CollectionView` (Row `*`) an `SavedItems`, `EmptyView` = `AppResources.PlaceholderLater`; `DataTemplate` nutzt `ArticleCardView` mit `ToggleSavedCommand`/`MarkReadCommand` aus dem Seiten-`BindingContext`; `OnAppearing` führt `LoadCommand` aus. `Shell.NavBarIsVisible="False"`.

## `UnreadPage` (Einbindung)
Datei: `src/Reporter/Views/UnreadPage.xaml` — `ArticleCardView` im `CollectionView`-Template (Zeilen 113–118) mit `ToggleSavedCommand`/`MarkReadCommand` des `UnreadViewModel`.

## `ArticleCardView`
Dateien: `src/Reporter/Views/ArticleCardView.xaml` + `.xaml.cs` — wiederverwendbare Artikelkarte.

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `OpenArticleCommand` | public BindableProperty | Default `AsyncRelayCommand` → `Shell.GoToAsync("articledetail?itemId=…")` |
| `ToggleSavedCommand` | public BindableProperty | Wird von der Page injiziert; an `TapGestureRecognizer` des Bookmark-`Border` (44 × 44) gebunden (XAML Zeilen 116–142) |
| `MarkReadCommand` | public BindableProperty | Check-Icon (44 × 44) |
| Bookmark-`Path` | — | `DataTrigger` auf `IsSavedForLater` → `Fill` = `LightPrimary`/`DarkPrimary` |

Vollflächiger transparenter Tap-Layer öffnet die Detailansicht (XAML Zeilen 168–177). `AppThemeBinding` durchgängig.

## `AppShell`
Datei: `src/Reporter/AppShell.xaml.cs` — Code-Behind baut `TabBar` mit fünf Tabs: **Ungelesen**, **Feeds**, **Später** (`AppResources.TabLater`, `LaterPage` via DI, Zeilen 28–29, 40), **Kategorien**, **Einstellungen**. Route `articledetail` → `ArticleDetailPage` registriert (Zeile 20). `AppShell.xaml` selbst ist leer.

## `ArticleDetailPage`
Dateien: `src/Reporter/Views/ArticleDetailPage.xaml` + `.xaml.cs` — `IQueryAttributable` liest `itemId`, ruft `ViewModel.LoadAsync`; `OnDisappearing` → `CancelAutoMarkRead`. Bottom-Action-Bar (6 × 44 × 44 pt) enthält Bookmark-`Border` mit `TapGestureRecognizer` → `ToggleSavedForLaterCommand`, `SemanticProperties.Description` = `BookmarkButtonLabel`, `DataTrigger` auf `Item.IsSavedForLater` → Gold-Fill (`LightBookmarkGold`/`DarkBookmarkGold`) (XAML Zeilen 168–196).

## Gesamtbefund zur Lösch-Invariante
- Automatische Löschung: **existiert nicht** (kein Aufrufpunkt, keine `RetentionDays`-Auswertung, kein `ExecuteDelete`, kein Hintergrunddienst — verifiziert per Grep über `src/`).
- `IItemRepository.DeleteAsync`: keine `IsSavedForLater`-Prüfung, aber **kein produktiver Aufrufer**.
- `FeedRepository.DeleteAsync` (produktiv via `FeedsViewModel.DeleteCommand`): Cascade löscht bewahrte Items des Feeds mit — bestätigte Nutzeraktion.
