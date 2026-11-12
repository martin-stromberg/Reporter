<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell — Bestandsaufnahme Lesepakete

## `Item` (Domänenmodell)

Datei: `src/Reporter.Core/Models/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige Artikel-ID; dient im Repository als Tie-Breaker der Sortierung. |
| `FeedId` | `Guid` (required, init) | Zugehöriger Feed; der Kategoriefilter wird über `Feed.CategoryId` aufgelöst. |
| `Title` | `string` (required, init) | Artikeltitel. |
| `Link` | `string?` (init) | Link zum Originalartikel. |
| `PublishedAt` | `DateTime?` (init) | Veröffentlichungszeitpunkt; wird vom `FeedSyncService` als `PublishDate.UtcDateTime` (UTC) gespeichert, `null` wenn der Feed kein Datum liefert (`DateTimeOffset.MinValue`). Grundlage der Tag-Zuordnung für Lesepakete. |
| `GuidOrHash` | `string` (required, init) | Deduplikationsschlüssel aus dem Feed. |
| `IsRead` | `bool` (required, init) | Gelesen-Flag; Filterkriterium der Ungelesen-Liste (`!i.IsRead`). |
| `IsSavedForLater` | `bool` (required, init) | „Später lesen"-Flag. |
| `ReadAt` | `DateTime?` (init) | Zeitpunkt des Als-gelesen-Markierens (UTC, gesetzt via `DateTime.UtcNow`). |
| `ContentHtml` | `string?` (init) | HTML-Inhalt (im separaten Content-Store). |
| `Tags` | `IReadOnlyList<string>` (init, Default `[]`) | Normalisierte Schlagworte. |
| `Image` | `ItemImage?` (get/set) | Lokal gespeichertes Artikelbild. |

## `ItemListItem` (Anzeige-Projektion)

Datei: `src/Reporter.Core/Models/ItemListItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Artikel-ID. |
| `FeedId` | `Guid` (required, init) | Feed-ID. |
| `Title` | `string` (required, init) | Anzeigetitel. |
| `Link` | `string?` (init) | Artikel-Link. |
| `PublishedAt` | `DateTime?` (init) | Veröffentlichungszeitpunkt — im ViewModel für die Tag-Bestimmung des Lesepakets verfügbar. |
| `IsRead` | `bool` (required, init) | Gelesen-Flag. |
| `IsSavedForLater` | `bool` (required, init) | Gespeichert-Flag. |
| `FeedTitle` | `string` (init, Default `""`) | Anzeigename des Feeds. |
| `CategoryId` | `Guid?` (init) | Kategorie des Feeds; wird u. a. in `UnreadViewModel.MarkReadAsync` zum Mitgezählen der Chips genutzt. |
| `CategoryName` | `string?` (init) | Kategoriename oder `null`. |
| `ImageUrl` | `string?` (init) | Aus dem Inhalt extrahierte Bild-URL. |
| `LocalImageLoader` | `Func<CancellationToken, Task<Stream>>?` (init) | Lazy-Loader für lokale Bilddaten. |
| `HasLocalImage` | `bool` (get) | `true`, wenn ein `LocalImageLoader` vorhanden ist. |
| `FeedFaviconUrl` | `string?` (init) | Favicon-URL des Feeds. |
| `FeedInitial` | `string` (get) | Fallback-Avatar (Anfangsbuchstabe via `FeedAvatar.Initial`). |
| `Summary` | `string?` (init) | Klartext-Kurzfassung. |
| `ReadingTimeText` | `string?` (init) | Lesezeit-Schätzung. |
| `CopyWith(bool?, bool?)` | `ItemListItem` | Kopie mit überschriebenem `IsRead`/`IsSavedForLater`. |

## `Item` (EF-Core-Entity)

Datei: `src/Reporter.Data/Entities/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel. |
| `FeedId` | `Guid` | Fremdschlüssel auf `Feed`. |
| `Title` | `string` | Titel. |
| `Link` | `string?` | Link. |
| `PublishedAt` | `DateTime?` | Veröffentlichungszeitpunkt (UTC, siehe `FeedSyncService`). SQLite-Sortierung: `NULL` sortiert bei `OrderBy` (aufsteigend) zuerst — durch Test `GetUnreadByDateAsync_Ascending_NullPublishedAt_SortsFirst` dokumentiert. |
| `GuidOrHash` | `string` | Dedup-Schlüssel. |
| `IsRead` | `bool` | Gelesen-Flag. |
| `IsSavedForLater` | `bool` | Gespeichert-Flag. |
| `ReadAt` | `DateTime?` | Lese-Zeitpunkt. |
| `Tags` | `string?` | Serialisierte Tags (U+001F-Trenner). |
| `Feed` | `Feed` (Navigation) | Ermöglicht `i.Feed.CategoryId` in Projektionen/Filtern. |

## `Feed` (EF-Core-Entity, Ausschnitt)

Datei: `src/Reporter.Data/Entities/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `CategoryId` | `Guid?` (Zeile 28) | Kategoriezuordnung des Feeds; Grundlage des Kategoriefilters in den Ungelesen-Abfragen (`feedIds.Contains(i.FeedId)` bzw. `i.Feed.CategoryId`). |

## `ItemListRow` (private Projektionsklasse)

Datei: `src/Reporter.Data/Repositories/ItemRepository.cs` (Zeilen 558–581, private in `ItemRepository`)

Flache Zeile für die seitenweise `ItemListItem`-Projektion (`SelectListItemRows`): `Id`, `FeedId`, `Title`, `Link`, `PublishedAt`, `IsRead`, `IsSavedForLater`, `FeedTitle`, `FeedFaviconUrl`, `CategoryId`, `CategoryName`. Wird von `GetUnreadByDateAsync`- und `GetSavedForLaterAsync`-Paging gemeinsam genutzt.

## `CategoryFilterItem` (Filter-Chip)

Datei: `src/Reporter.Core/Models/CategoryFilterItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `CategoryId` | `Guid?` | `null` steht für den Chip „Alle". |
| `Name` | `string` | Anzeigename. |
| `Count` | `int` (`[ObservableProperty]`) | Anzahl ungelesener Artikel des Filters. |
| `IsSelected` | `bool` (`[ObservableProperty]`) | Auswahlzustand. |
| `AccessibilityDescription` | `string` (get) | Lokalisierte Screenreader-Beschreibung. |

## `Settings` (persistierte Einstellungen, relevante Ausschnitte)

Datei: `src/Reporter.Core/Models/Settings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `UnreadSortOrder` | `string?` (init, Default `"desc"`, Zeile 69) | Sortierreihenfolge der Ungelesen-Liste (`SettingsValues.SortOrderAscending`/`SortOrderDescending`); wird in `UnreadViewModel.LoadPageAsync` gelesen. |
| `UnreadListLayout` | `string?` (init, Default `"list"`, Zeile 74) | Listen- vs. Karten-Layout. |
| `WithUnreadListLayout(string)` | `Settings` | Kopie mit geändertem Layout (alle übrigen Werte unverändert, inkl. `UnreadSortOrder`). |
