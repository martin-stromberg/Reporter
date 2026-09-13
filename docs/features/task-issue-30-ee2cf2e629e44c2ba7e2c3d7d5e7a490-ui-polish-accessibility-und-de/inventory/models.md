<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Datenmodellklassen

## `CategoryFilterItem`
Datei: `src/Reporter.Core/Models/CategoryFilterItem.cs`

`ObservableObject` (CommunityToolkit.Mvvm); trägt bereits alles für eine Chip-Leiste inkl. Aktiv-Zustand und Count-Kapsel. Wird befüllt von `UnreadViewModel.LoadAsync` (erster Eintrag „Alle" mit `CategoryId = null`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `CategoryId` | `Guid?` | Kategorie-ID; `null` = „Alle" |
| `Name` | `string` | Anzeigename des Filters |
| `Count` | `int` (`[ObservableProperty]`) | Anzahl ungelesener Artikel der Kategorie |
| `IsSelected` | `bool` (`[ObservableProperty]`) | Aktiv-Zustand des Chips; wird von `UnreadViewModel.UpdateCategorySelection` gepflegt |

## `ItemListItem`
Datei: `src/Reporter.Core/Models/ItemListItem.cs`

Listendarstellung eines Artikels; liefert `ItemRepository.GetUnreadByDateAsync(page, pageSize, categoryId)` und `GetSavedForLaterAsync` (inkl. extrahiertem `ImageUrl`/`Summary` aus `ContentHtml`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Item-ID |
| `FeedId` | `Guid` (required, init) | Zugehöriger Feed |
| `Title` | `string` (required, init) | Titel |
| `Link` | `string?` | Originallink |
| `PublishedAt` | `DateTime?` | Veröffentlichungszeitpunkt |
| `IsRead` | `bool` (required, init) | Gelesen-Flag |
| `IsSavedForLater` | `bool` (required, init) | Später-Flag (Bookmark-Status auf `ArticleCardView`) |
| `FeedTitle` | `string` | Anzeigename des Feeds |
| `CategoryId` | `Guid?` | Kategorie des Feeds |
| `CategoryName` | `string?` | Kategoriename (Kategorie-Pill auf der Karte) |
| `ImageUrl` | `string?` | Aus `ContentHtml` extrahierte Thumbnail-URL |
| `Summary` | `string?` | Plain-Text-Auszug (max. 120 Zeichen) |

## `FeedListItem`
Datei: `src/Reporter.Core/Models/FeedListItem.cs`

Feed-Zeile auf `FeedsPage`; `HealthStatus` steuert Dot + Statustext (künftig Pill-Badge).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Feed-ID |
| `Title` | `string` (required, init) | Anzeigetitel |
| `Url` | `string` (required, init) | Feed-URL |
| `CategoryId` | `Guid?` | Kategorie-ID |
| `CategoryName` | `string?` | Kategoriename |
| `LastCheckedAt` | `DateTime?` | Letzter Sync-Zeitpunkt |
| `HealthStatus` | `string?` | `FeedHealth`-Konstante (`OK`/`Warning`/`Error`) |
| `HealthLastChange` | `DateTime?` | Zeitpunkt des letzten Statuswechsels |
| `UnreadCount` | `int` (required, init) | Ungelesene Artikel |
| `NotificationsEnabled` | `bool` (required, init) | Feed-Benachrichtigungen |

## `Feed`
Datei: `src/Reporter.Core/Models/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Feed-ID |
| `Url` | `string` (required, init) | Feed-URL |
| `Title` | `string` (required, init) | Titel (kann Platzhalter = URL/Host/Dateiname sein, s. `FeedTitleFallback`) |
| `CategoryId` | `Guid?` | Kategorie-ID |
| `LastCheckedAt` | `DateTime?` | Letzter Sync |
| `HealthStatus` | `string?` | `FeedHealth`-Konstante |
| `HealthLastChange` | `DateTime?` | Letzter Statuswechsel |
| `NotificationsEnabled` | `bool` (required, init) | Benachrichtigungen aktiv |

## `Item`
Datei: `src/Reporter.Core/Models/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Item-ID |
| `FeedId` | `Guid` (required, init) | Feed-ID |
| `Title` | `string` (required, init) | Titel |
| `Link` | `string?` | Originallink |
| `PublishedAt` | `DateTime?` | Veröffentlichung |
| `GuidOrHash` | `string` (required, init) | Dedup-Schlüssel (GUID oder SHA256-Hash aus Titel/Link/Datum) |
| `IsRead` | `bool` (required, init) | Gelesen |
| `IsSavedForLater` | `bool` (required, init) | Später |
| `ReadAt` | `DateTime?` | Lesezeitpunkt (Retention-Referenz) |
| `ContentHtml` | `string?` | HTML-Inhalt für Offline-Lesemodus |

## `Settings`
Datei: `src/Reporter.Core/Models/Settings.cs`

Singleton (`DefaultId`); u. a. `RetentionDays`, `AutoMarkReadMode`, `AutoMarkReadDelaySeconds`, `NotificationsEnabled`, `NotificationSummaryEnabled`, `QuietHoursStart`/`QuietHoursEnd`, `AutoRefreshEnabled`, `RefreshIntervalMinutes`, `Theme` (`system`/`light`/`dark`), `Language` (`system`/`de`/`en`).

## `FeedSearchResult`
Datei: `src/Reporter.Core/Models/FeedSearchResult.cs`

In-Memory-Suchtreffer für die Karten auf `FeedsPage`: `Title?`, `Description?`, `SiteName?`, `SiteUrl?`, `FeedUrl` (required), `Score`, `MatchKind` (`FeedSearchMatchKind`).

## `CategoryWithCount`
Datei: `src/Reporter.Core/Models/CategoryWithCount.cs`

Kategorien-Liste auf `CategoriesPage`: `Id`, `Name`, `FeedCount` (alle required, init).

## `SyncResult`
Datei: `src/Reporter.Core/Services/SyncResult.cs`

Record `SyncResult(string Status, int NewItems, string? Message = null)` — Rückgabe von `IFeedSyncService.SyncFeedAsync`/`SyncAllAsync`.
