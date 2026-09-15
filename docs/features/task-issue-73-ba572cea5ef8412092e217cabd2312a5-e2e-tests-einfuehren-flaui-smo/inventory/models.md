<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodelle

Modelle, die als `x:DataType`-Ziele bzw. als Binding-Quellen der betroffenen Views
dienen, plus das Matching-Enum der Feed-Suche.

## `FeedListItem`

Datei: `src/Reporter.Core/Models/FeedListItem.cs` — Elementtyp des `Feeds`-Templates
in `FeedsPage.xaml` und Parameter von `OnFeedTapped`/`RefreshCommand`/`EditCommand`/`DeleteCommand`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required init) | Feed-ID; `Guid.Empty` markiert den Pseudo-Eintrag „Keine Kategorie" nur bei `Category`, nicht hier |
| `Title` | `string` (required init) | Anzeigetitel (Karten-Binding, `SemanticProperties.Description`, Rename-Prompt-Initialwert) |
| `Url` | `string` (required init) | Feed-URL |
| `CategoryId` | `Guid?` | zugeordnete Kategorie |
| `CategoryName` | `string?` | Kategorieanzeige (TargetNullValue `—` im Template) |
| `LastCheckedAt` | `DateTime?` | letzter Abruf (`{0:g}`-Format im Template) |
| `HealthStatus` | `string?` | `FeedHealth`-Wert (`OK`/`Warning`/`Error`); steuert DataTrigger der Status-Pille |
| `HealthLastChange` | `DateTime?` | Zeitpunkt der letzten Statusänderung |
| `UnreadCount` | `int` (required init) | Ungelesen-Zähler |
| `NotificationsEnabled` | `bool` (required init) | Feed-Benachrichtigungen (Edit-Sheet-Switch) |
| `FaviconUrl` | `string?` | Favicon der Site (MultiTrigger mit `IsOnline`) |
| `LastErrorKind` | `string?` | `FeedSyncErrorKind`-Wert des letzten Sync-Fehlers |
| `LastErrorMessage` | `string?` | technische Fehlermeldung des letzten Syncs |
| `FeedInitial` | `string` (get only) | Fallback-Avatar via `FeedAvatar.Initial(Title)` |

## `FeedSearchResult`

Datei: `src/Reporter.Core/Models/FeedSearchResult.cs` — Elementtyp des
`SearchResults`-Templates in `FeedsPage.xaml` und Parameter von
`OnSearchResultTapped`/`SubscribeResultCommand`. Reines In-Memory-Modell.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Title` | `string?` | Feed-Titel aus dem Directory |
| `Description` | `string?` | Beschreibung (nur Kartenanzeige, nie persistiert) |
| `SiteName` | `string?` | Site-Name (TargetNullValue/FallbackValue `—`) |
| `SiteUrl` | `string?` | Site-URL; geht in die Favicon-Auflösung beim Abonnieren ein |
| `FeedUrl` | `string` (required init) | Feed-URL; Dedup-/Sortierschlüssel |
| `Score` | `double` | Directory-Relevanz (Autodiscovery: 0) |
| `MatchKind` | `FeedSearchMatchKind` | Trefferart; bestimmt die Sortierreihenfolge |
| `DisplayTitle` | `string` (get only) | `Title`, sonst `FeedUrl` — Karten- und `SemanticProperties.Description`-Binding |

## `CategoryWithCount`

Datei: `src/Reporter.Core/Models/CategoryWithCount.cs` — Elementtyp des
`Categories`-Templates in `CategoriesPage.xaml` und Parameter von `OnCategoryTapped`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required init) | Kategorie-ID |
| `Name` | `string` (required init) | Name (Karten-Binding, `SemanticProperties.Description`) |
| `FeedCount` | `int` (required init) | Anzahl zugeordneter Feeds |

Hinweis: Das „Keine Kategorie"-Element im Kategorie-ActionSheet ist **kein**
`CategoryWithCount`, sondern ein `Category` mit `Id == Guid.Empty` und
`Name = AppResources.CategoryNone`, erzeugt in `FeedsViewModel.LoadAsync`
(`FeedsViewModel.cs` Zeilen 277–282).

## `ItemListItem`

Datei: `src/Reporter.Core/Models/ItemListItem.cs` — Binding-Kontext von
`ArticleCardView.xaml` (instanziiert in den `x:DataType="models:ItemListItem"`-Templates
von `UnreadPage`/`LaterPage`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required init) | Item-ID (Route `articledetail?itemId=…`) |
| `FeedId` | `Guid` (required init) | Feed-Zugehörigkeit |
| `Title` | `string` (required init) | Titel (Karten-Description) |
| `Link` | `string?` | Original-Link |
| `PublishedAt` | `DateTime?` | Datum (`{0:g}`-Binding) |
| `IsRead` | `bool` (required init) | Ungelesen-Punkt (DataTrigger) |
| `IsSavedForLater` | `bool` (required init) | Lesezeichenzustand (Path-Fill-Trigger, Description-Wechsel) |
| `FeedTitle` | `string` (= `string.Empty`) | Feed-Anzeigename |
| `CategoryId` / `CategoryName` | `Guid?` / `string?` | Kategoriebadge (Trigger auf `""` und `{x:Null}`) |
| `ImageUrl` / `FeedFaviconUrl` | `string?` | Thumbnail / Favicon-Fallback (MultiTrigger) |
| `FeedInitial` | `string` (get only) | Fallback-Avatar via `FeedAvatar.Initial(FeedTitle)` |
| `Summary` | `string?` | Kurztext (`StringNotEmptyToBoolConverter`) |
| `ReadingTimeText` | `string?` | Lesezeit-Anzeige |
| `CopyWith(isRead, isSavedForLater)` | Methode | Kopie mit geändertem Lese-/Merker-Status |

## `FeedHealth` (Konstanten, kein Enum)

Datei: `src/Reporter.Core/Services/FeedHealth.cs` — `public static class` mit
`const string Ok = "OK"`, `Warning = "Warning"`, `Error = "Error"` und Helper
`Changed(current, next)`. Diese String-Literale erscheinen als `Value` in den
`HealthStatus`-DataTriggern von `FeedsPage.xaml`.
