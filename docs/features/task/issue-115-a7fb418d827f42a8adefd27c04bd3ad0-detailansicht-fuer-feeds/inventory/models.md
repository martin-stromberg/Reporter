<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell — Bestandsaufnahme

Domänenmodelle und EF-Core-Entities, die für die Feed-Detailansicht relevant sind.

## `Feed` (Domänenmodell)
Datei: `src/Reporter.Core/Models/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige Feed-ID — wird als `feedId`-Query-Parameter für die Detailroute benötigt |
| `Url` | `string` (required, init) | Feed-URL |
| `Title` | `string` (required, init) | Anzeigetitel des Feeds |
| `CategoryId` | `Guid?` (init) | Optionale Kategorie-Zuordnung |
| `LastCheckedAt` | `DateTime?` (init) | Zeitpunkt des letzten Syncs |
| `HealthStatus` | `string?` (init) | Gesundheitsstatus; Werte aus `FeedHealth` (`"OK"`, `"Warning"`, `"Error"`) |
| `HealthLastChange` | `DateTime?` (init) | Zeitpunkt der letzten Statusänderung |
| `NotificationsEnabled` | `bool` (required, init) | Per-Feed-Benachrichtigungs-Schalter |
| `FaviconUrl` | `string?` (init) | Favicon der Feed-Website |
| `LastErrorKind` | `string?` (init) | Kategorie des letzten Sync-Fehlers (`FeedSyncErrorKind`-Wert) |
| `LastErrorMessage` | `string?` (init) | Technische Meldung des letzten Sync-Fehlers |

## `FeedListItem`
Datei: `src/Reporter.Core/Models/FeedListItem.cs`

Listen-Darstellung eines Feeds; wird von `FeedRepository.GetAllWithDetailsAsync` projiziert und in `FeedsViewModel.Feeds` gehalten. Alle Aktionen der `FeedsPage` (Aktualisieren, Umbenennen, Kategorie ändern, Bearbeiten, Fehlerdetails, Löschen) erhalten ein `FeedListItem` als Parameter.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Feed-ID |
| `Title` | `string` (required, init) | Anzeigetitel |
| `Url` | `string` (required, init) | Feed-URL |
| `CategoryId` | `Guid?` (init) | Zugeordnete Kategorie |
| `CategoryName` | `string?` (init) | Anzeigename der Kategorie (Join-Projektion) |
| `LastCheckedAt` | `DateTime?` (init) | Letzter Sync-Zeitpunkt |
| `HealthStatus` | `string?` (init) | `FeedHealth`-Wert; steuert in `FeedsPage.OnFeedTapped`, ob „Fehlerdetails anzeigen" angeboten wird |
| `HealthLastChange` | `DateTime?` (init) | Letzte Statusänderung |
| `UnreadCount` | `int` (required, init) | Ungelesene Beiträge (Zähl-Subquery) |
| `NotificationsEnabled` | `bool` (required, init) | Benachrichtigungs-Schalter |
| `FaviconUrl` | `string?` (init) | Favicon-URL |
| `LastErrorKind` | `string?` (init) | `FeedSyncErrorKind`-Wert des letzten Fehlers |
| `LastErrorMessage` | `string?` (init) | Technische Fehlermeldung |
| `FeedInitial` | `string` (get, berechnet) | Fallback-Avatar-Buchstabe via `FeedAvatar.Initial(Title)`; `"?"` bei leerem Titel |

## `Item` (Domänenmodell)
Datei: `src/Reporter.Core/Models/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Beitrags-ID — Query-Parameter `itemId` der Route `articledetail` |
| `FeedId` | `Guid` (required, init) | Feed-Zugehörigkeit; Filterkriterium der geplanten paged-Abfrage |
| `Title` | `string` (required, init) | Beitragstitel; voraussichtliches Suchfeld (offene Frage in der Anforderung) |
| `Link` | `string?` (init) | Original-Link |
| `PublishedAt` | `DateTime?` (init) | Publikationszeitpunkt; Sortierkriterium der Detailansicht |
| `GuidOrHash` | `string` (required, init) | Original-GUID/Hash aus dem Feed-Dokument |
| `IsRead` | `bool` (required, init) | Lesestatus |
| `IsSavedForLater` | `bool` (required, init) | „Später lesen"-Flag |
| `ReadAt` | `DateTime?` (init) | Lesezeitpunkt |
| `ContentHtml` | `string?` (init) | HTML-Inhalt (aus separatem Content-Store hydratisiert) |
| `Image` | `ItemImage?` (get/set) | Lokal gespeichertes Artikelbild; `set` ist bewusste Ausnahme für `FeedSyncService` |

## `ItemListItem`
Datei: `src/Reporter.Core/Models/ItemListItem.cs`

Flaches Listen-Modell für Beitragskarten (`ArticleCardView`); Rückgabetyp der paged-Repository-Abfragen — auch der vorgesehene Rückgabetyp der neuen Feed-Abfrage.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Beitrags-ID |
| `FeedId` | `Guid` (required, init) | Feed-Zugehörigkeit |
| `Title` | `string` (required, init) | Beitragstitel |
| `Link` | `string?` (init) | Original-Link |
| `PublishedAt` | `DateTime?` (init) | Publikationszeitpunkt |
| `IsRead` | `bool` (required, init) | Lesestatus |
| `IsSavedForLater` | `bool` (required, init) | „Später lesen"-Flag |
| `FeedTitle` | `string` (init, Default `""`) | Anzeigetitel des Feeds |
| `CategoryId` | `Guid?` (init) | Kategorie des Feeds |
| `CategoryName` | `string?` (init) | Kategoriename |
| `ImageUrl` | `string?` (init) | Erste Bild-URL aus dem Inhalt (Remote) |
| `LocalImageLoader` | `Func<CancellationToken, Task<Stream>>?` (init) | Lazy-Loader für das lokal gespeicherte Bild; `null` ohne lokales Bild |
| `HasLocalImage` | `bool` (get, berechnet) | `LocalImageLoader is not null` |
| `FeedFaviconUrl` | `string?` (init) | Favicon des Feeds |
| `FeedInitial` | `string` (get, berechnet) | Fallback-Avatar-Buchstabe via `FeedAvatar.Initial(FeedTitle)` |
| `Summary` | `string?` (init) | Plain-Text-Auszug (max. 120 Zeichen + „…") |
| `ReadingTimeText` | `string?` (init) | Formatierter Lesezeit-Hinweis |
| `CopyWith(bool? isRead, bool? isSavedForLater)` | `ItemListItem` | Erzeugt Kopie mit geändertem Lese-/Sammel-Status; von `LaterViewModel.MarkReadAsync`/`ToggleSavedAsync` genutzt |

## `Category`
Datei: `src/Reporter.Core/Models/Category.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Kategorie-ID; `Guid.Empty` markiert in `FeedsViewModel`/`FeedsPage` den Pseudo-Eintrag „Keine Kategorie" |
| `Name` | `string` (required, init) | Anzeigename |

## `SyncResult`
Datei: `src/Reporter.Core/Services/SyncResult.cs`

`public record SyncResult(string Status, int NewItems, string? Message = null);` — Rückgabewert von `IFeedSyncService.SyncFeedAsync`/`SyncAllAsync`; `Status` ist ein `FeedHealth`-Wert.

## `Feed` (EF-Core-Entity)
Datei: `src/Reporter.Data/Entities/Feed.cs`

Spiegelt das Domänenmodell (`Id`, `Url`, `Title`, `CategoryId`, `LastCheckedAt`, `HealthStatus`, `HealthLastChange`, `NotificationsEnabled` Default `true`, `FaviconUrl`, `LastErrorKind`, `LastErrorMessage`) plus Navigation `Category`.

## `Item` (EF-Core-Entity)
Datei: `src/Reporter.Data/Entities/Item.cs`

`Id`, `FeedId`, `Title`, `Link`, `PublishedAt`, `GuidOrHash`, `IsRead`, `IsSavedForLater`, `ReadAt` plus erforderliche Navigation `Feed` (für `SelectListItemRows`-Join auf `Feed.Title`, `Feed.FaviconUrl`, `Feed.CategoryId`, `Feed.Category.Name`). Der HTML-Inhalt liegt seit Migration `DropItemContentHtml` nicht mehr in dieser Tabelle, sondern im `IItemContentStore` (`reporter-content.db`).
