<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell

Datenmodellklassen und EF-Core-Mappings, die für die Anforderung „Artikelbilder lokal speichern" relevant sind. Es existieren zwei SQLite-Datenbanken: `reporter.db` (Nutzdaten, im Backup enthalten) und `reporter-content.db` (re-downloadbare Artikelinhalte, vom Backup ausgeschlossen — siehe `BackupExclusionPlan`).

## `ItemContent` (Entity)

Datei: `src/Reporter.Data/Entities/ItemContent.cs`

Zeile der Tabelle `item_contents` in `reporter-content.db`. Keine Datenbank-Fremdschlüssel zur `items`-Tabelle (datenbankübergreifend); `ItemId` referenziert `items.id` nur logisch.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `ItemId` | `Guid` | Primärschlüssel; entspricht `items.id` in `reporter.db` |
| `ContentHtml` | `string?` | HTML-Inhalt für Offline-Lesen |

Es existieren **keine** Bild-Spalten (kein `byte[]`, kein MIME-Typ, keine Origin-URL).

## `Item` (Entity)

Datei: `src/Reporter.Data/Entities/Item.cs`

Zeile der Tabelle `items` in `reporter.db`. Die frühere `content_html`-Spalte wurde per Migration `20260918022834_DropItemContentHtml` entfernt; Inhalte liegen ausschließlich im Content-Store.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel |
| `FeedId` | `Guid` | FK auf `feeds.id`, `DeleteBehavior.Cascade` |
| `Title` | `string` | Titel (max. 500, required) |
| `Link` | `string?` | Link zum Originalartikel (max. 2048) |
| `PublishedAt` | `DateTime?` | Veröffentlichungszeitpunkt |
| `GuidOrHash` | `string` | Original-GUID oder SHA256-Hash (max. 500, required); eindeutig je Feed über Index `(FeedId, GuidOrHash)` |
| `IsRead` | `bool` | Gelesen-Status |
| `IsSavedForLater` | `bool` | Gemerkt-Status |
| `ReadAt` | `DateTime?` | Zeitpunkt des Lesens |
| `Feed` | `Feed` | Navigation zum Feed |

## `ContentDbContext`

Datei: `src/Reporter.Data/ContentDbContext.cs`

EF-Core-Kontext für `reporter-content.db`. Enthält aktuell genau ein `DbSet<ItemContent> ItemContents`; das Mapping in `ConfigureItemContent` legt die Tabelle `item_contents` mit Spalten `item_id` (PK) und `content_html` an. Bestehende Content-Migrationen liegen unter `src/Reporter.Data/Migrations/Content/` — bislang nur `20260918022828_InitialContentCreate` (erzeugt `item_contents` mit `item_id`/`content_html`).

## `ItemContentEntry` (Record)

Datei: `src/Reporter.Core/Models/ItemContentEntry.cs`

| Positionsparameter | Typ | Beschreibung / Zweck |
|--------------------|-----|----------------------|
| `ItemId` | `Guid` | Item, zu dem der Inhalt gehört |
| `ContentHtml` | `string?` | HTML-Inhalt; `null` entfernt einen gespeicherten Eintrag (Upsert-Semantik von `IItemContentStore.SetRangeAsync`) |

Transport-Record für Batch-Schreibvorgänge; keine Bild-Felder vorhanden.

## `Item` (Domain-Modell)

Datei: `src/Reporter.Core/Models/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Item-ID (required, init) |
| `FeedId` | `Guid` | Feed-ID (required, init) |
| `Title` | `string` | Titel (required, init) |
| `Link` | `string?` | Link zum Originalartikel |
| `PublishedAt` | `DateTime?` | Veröffentlichungszeitpunkt |
| `GuidOrHash` | `string` | Original-GUID/Hash (required, init) |
| `IsRead` | `bool` | Gelesen-Status (required, init) |
| `IsSavedForLater` | `bool` | Gemerkt-Status (required, init) |
| `ReadAt` | `DateTime?` | Lesezeitpunkt |
| `ContentHtml` | `string?` | HTML-Inhalt; wird von `ItemRepository` aus dem Content-Store hydratisiert |

Kein Bild-Feld vorhanden; `ArticleDetailViewModel` erhält das `Item` über `IItemRepository.GetByIdAsync`.

## `ItemListItem`

Datei: `src/Reporter.Core/Models/ItemListItem.cs`

Listen-Darstellung eines Artikels (Ungelesen-/Später-Ansicht, gebunden von `ArticleCardView`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Item-ID (required, init) |
| `FeedId` | `Guid` | Feed-ID (required, init) |
| `Title` | `string` | Titel (required, init) |
| `Link` | `string?` | Link zum Originalartikel |
| `PublishedAt` | `DateTime?` | Veröffentlichungszeitpunkt |
| `IsRead` | `bool` | Gelesen-Status (required, init) |
| `IsSavedForLater` | `bool` | Gemerkt-Status (required, init) |
| `FeedTitle` | `string` | Anzeigetitel des Feeds |
| `CategoryId` | `Guid?` | Kategorie des Feeds |
| `CategoryName` | `string?` | Kategoriename |
| `ImageUrl` | `string?` | Remote-Bild-URL, von `ItemRepository.ExtractImageUrl` aus dem ersten `<img src>` des `ContentHtml` extrahiert |
| `FeedFaviconUrl` | `string?` | Favicon-URL des Feeds (Fallback-Stufe 2 in `ArticleCardView`) |
| `FeedInitial` | `string` | Anfangsbuchstabe des Feed-Titels über `FeedAvatar.Initial` (Fallback-Stufe 3) |
| `Summary` | `string?` | Klartext-Teaser (max. 120 Zeichen) |
| `ReadingTimeText` | `string?` | Lesezeitschätzung |
| `CopyWith(bool? isRead, bool? isSavedForLater)` | `ItemListItem` | Kopiert alle Felder inkl. `ImageUrl`; neue Bild-Felder müssten hier mitgeführt werden |

Kein Feld für lokale Bilddaten (`byte[]`/`ImageSource`) vorhanden.

## `Feed` (Domain-Modell)

Datei: `src/Reporter.Core/Models/Feed.cs`

Für die Anforderung relevante Eigenschaft: `FaviconUrl` (`string?`) — wird im `ItemListItem` als `FeedFaviconUrl` projiziert und ist Fallback in der Thumbnail-Kaskade. `LastErrorKind`/`LastErrorMessage` speichern den letzten Sync-Fehler.

## `ContentDatabasePath` / `DatabasePath`

Dateien: `src/Reporter.Core/Models/ContentDatabasePath.cs`, `src/Reporter.Core/Models/DatabasePath.cs`

Pfad-Träger für `reporter-content.db` bzw. `reporter.db` im DI-Container (`MauiProgram`). Die Content-Datenbank liegt immer im selben Verzeichnis wie `reporter.db`; `BackupExclusionPlan.ExcludedPaths` schließt sie samt `-wal`/`-shm`-Sidecars vom iCloud-Backup aus — gespeicherte Bilder in `item_contents` wären automatisch abgedeckt.
