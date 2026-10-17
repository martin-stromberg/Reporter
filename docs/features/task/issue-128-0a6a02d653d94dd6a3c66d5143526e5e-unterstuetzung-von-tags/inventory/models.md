<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell — Bestandsaufnahme

## `Item` (Entity)

Datei: `src/Reporter.Data/Entities/Item.cs`

EF-Core-Entity für die Tabelle `items` in der Haupt-Datenbank (`reporter.db`, `ReporterDbContext`). Es existiert **keine** `Tags`-Eigenschaft.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel (`id`) |
| `FeedId` | `Guid` | Fremdschlüssel auf `feeds` (`feed_id`), Cascade-Delete, Teil des Unique-Index `(feed_id, guid_or_hash)` |
| `Title` | `string` | Titel (`title`, max 500, required) — einziges Feld, das die Artikelsuche derzeit durchsucht |
| `Link` | `string?` | Artikel-URL (`link`, max 2048) |
| `PublishedAt` | `DateTime?` | Veröffentlichungszeitpunkt (`published_at`) |
| `GuidOrHash` | `string` | Dedup-Schlüssel (`guid_or_hash`, max 500, required) |
| `IsRead` | `bool` | Gelesen-Flag (`is_read`) |
| `IsSavedForLater` | `bool` | Merken-Flag (`is_saved_for_later`) |
| `ReadAt` | `DateTime?` | Lesezeitpunkt (`read_at`) |
| `Feed` | `Feed` | Navigation zum Feed |

## `Item` (Domain-Modell)

Datei: `src/Reporter.Core/Models/Item.cs`

Domain-Modell mit `init`-Only-Properties; `Image` ist als dokumentierte Ausnahme settable. Es existiert **keine** `Tags`-Eigenschaft.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Item-ID |
| `FeedId` | `Guid` (required, init) | Feed-ID |
| `Title` | `string` (required, init) | Titel |
| `Link` | `string?` (init) | Artikel-URL |
| `PublishedAt` | `DateTime?` (init) | Veröffentlichungszeitpunkt |
| `GuidOrHash` | `string` (required, init) | Dedup-Schlüssel |
| `IsRead` | `bool` (required, init) | Gelesen-Flag |
| `IsSavedForLater` | `bool` (required, init) | Merken-Flag |
| `ReadAt` | `DateTime?` (init) | Lesezeitpunkt |
| `ContentHtml` | `string?` (init) | HTML-Inhalt; physisch im Content-Store (`reporter-content.db`), nicht in `items` |
| `Image` | `ItemImage?` (get/set — Ausnahme vom init-Only-Muster) | Lokales Artikelbild; `FeedSyncService` setzt es nach dem Download auf dem bereits konstruierten Objekt |

## `ItemListItem`

Datei: `src/Reporter.Core/Models/ItemListItem.cs`

Flache Listen-Projektion für die paged Artikellisten (Ungelesen-, Feed-Detail- und Später-Ansicht). Enthält u. a. `Id`, `FeedId`, `Title`, `Link`, `PublishedAt`, `IsRead`, `IsSavedForLater`, `FeedTitle`, `FeedFaviconUrl`, `CategoryId`, `CategoryName`, `ImageUrl`, `LocalImageLoader`, `Summary`, `ReadingTimeText` sowie `CopyWith(...)`. **Keine** Tags-Eigenschaft — die Suche läuft DB-seitig, die projizierte Zeile (`ItemListRow` in `ItemRepository`) enthält die Felder nur zur Darstellung.

## `ItemContent` (Entity, Content-Datenbank)

Datei: `src/Reporter.Data/Entities/ItemContent.cs`

Entity der separaten Content-Datenbank (`reporter-content.db`, `ContentDbContext`): `ItemId`, `ContentHtml`, `ImageData`, `ImageContentType`, `ImageUrl`. Zweck: re-downloadbare Bulk-Daten außerhalb der in Cloud-Backups eingeschlossenen Haupt-Datenbank. Kein DB-übergreifender Fremdschlüssel.

## `ItemContentEntry`

Datei: `src/Reporter.Core/Models/ItemContentEntry.cs`

`record ItemContentEntry(Guid ItemId, string? ContentHtml, ItemImage? Image = null)` — Transport-Record für `IItemContentStore.SetRangeAsync`. Enthält nur Content/Bild, keine Metadaten.

## EF-Core-Mapping und Migrationen

- `ReporterDbContext.ConfigureItem` (`src/Reporter.Data/ReporterDbContext.cs`, Zeilen 104–120): mappt alle `Item`-Entity-Spalten; eine `tags`-Spalte existiert nicht.
- `ReporterDbContext.OnModelCreating` (Zeilen 62–73) ruft `ConfigureItem` auf.
- Letzte vorhandene Migration der Haupt-DB: `20261008063336_AddSettingsUnreadListLayout`; Model-Snapshot: `src/Reporter.Data/Migrations/ReporterDbContextModelSnapshot.cs` (enthält die `items`-Spalten `guid_or_hash` in Zeile 160, `read_at` in Zeile 181 u. a.). Content-DB-Migrationen liegen separat unter `src/Reporter.Data/Migrations/Content/` mit eigenem `ContentDbContextModelSnapshot`.
- `ContentDbContext` (`src/Reporter.Data/ContentDbContext.cs`) mappt `ItemContent` auf die Tabelle `item_contents` — für die Anforderung laut Übersetzung voraussichtlich nicht betroffen.
