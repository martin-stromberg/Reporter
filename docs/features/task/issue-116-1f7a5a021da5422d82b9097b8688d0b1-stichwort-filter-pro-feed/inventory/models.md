<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Datenmodell

Betroffene Datenmodellklassen der Anforderung „Stichwort-Filter pro Feed" (Issue #116). Stand: Branch `task/issue-116-1f7a5a021da5422d82b9097b8688d0b1-stichwort-filter-pro-feed`, Commit `1bd324aa2dd024c601ff6c975942eaf86a7d3e28`.

## Entities (`src/Reporter.Data/Entities/`)

### `Keyword`
Datei: `src/Reporter.Data/Entities/Keyword.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel (`keywords.id`). |
| `KeywordText` | `string` | Stichwort-Text (`keywords.keyword_text`, max. 500, required, Unique-Index). |

Es existiert **keine** Feed-Zuordnung (kein `FeedId`, keine Navigation zu `Feed`). Alle Einträge sind faktisch global.

### `Feed`
Datei: `src/Reporter.Data/Entities/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel (`feeds.id`). |
| `Url` | `string` | Feed-URL (`feeds.url`, max. 2048, required, Unique-Index). |
| `Title` | `string` | Anzeigetitel (`feeds.title`, max. 500, required). |
| `CategoryId` | `Guid?` | Optionale Kategorie (`feeds.category_id`, FK mit `DeleteBehavior.SetNull`). |
| `LastCheckedAt` | `DateTime?` | Zeitpunkt des letzten Abrufs (`feeds.last_checked_at`). |
| `HealthStatus` | `string?` | Gesundheitsstatus (`feeds.health_status`, max. 50). |
| `HealthLastChange` | `DateTime?` | Zeitpunkt der letzten Statusänderung (`feeds.health_last_change`). |
| `NotificationsEnabled` | `bool` | Feed-spezifische Einstellung (`feeds.notifications_enabled`, required, Default `true`) — Vorbild für die feed-spezifische Stichwort-Einstellung. |
| `FaviconUrl` | `string?` | Favicon-URL (`feeds.favicon_url`, max. 2048). |
| `LastErrorKind` | `string?` | Kategorie des letzten Sync-Fehlers (`feeds.last_error_kind`, max. 50). |
| `LastErrorMessage` | `string?` | Technische Meldung des letzten Sync-Fehlers (`feeds.last_error_message`). |
| `Category` | `Category?` | Navigation zur Kategorie. |

Es existiert keine Navigation/Eigenschaft für Stichworte.

### `Item`
Datei: `src/Reporter.Data/Entities/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel (`items.id`). |
| `FeedId` | `Guid` | Pflicht-FK auf `feeds` (`items.feed_id`, `DeleteBehavior.Cascade`, Teil des Unique-Index `(feed_id, guid_or_hash)`). |
| `Title` | `string` | Artikeltitel (`items.title`, max. 500, required) — Match-Ziel des Keyword-Filters. |
| `Link` | `string?` | Artikel-Link (`items.link`, max. 2048). |
| `PublishedAt` | `DateTime?` | Veröffentlichungszeitpunkt (`items.published_at`). |
| `GuidOrHash` | `string` | Dedup-Schlüssel (`items.guid_or_hash`, max. 500, required). |
| `IsRead` | `bool` | Gelesen-Flag (`items.is_read`). |
| `IsSavedForLater` | `bool` | „Später lesen"-Flag (`items.is_saved_for_later`) — schützt vor Retention-Löschung. |
| `ReadAt` | `DateTime?` | Lesezeitpunkt (`items.read_at`). |
| `Feed` | `Feed` | Navigation zum Feed. |

Der Artikel-HTML-Inhalt (`ContentHtml`, zweites Match-Ziel) liegt **nicht** in dieser Tabelle, sondern in der separaten Content-Datenbank (`ContentDbContext`, `ItemContent`-Entität).

## `ReporterDbContext`-Konfiguration
Datei: `src/Reporter.Data/ReporterDbContext.cs`

- `ConfigureKeyword` (Z. 122–129): Tabelle `keywords`, Spalten `id`, `keyword_text` (max. 500, required), **Unique-Index auf `keyword_text`** — tabellenweite Eindeutigkeit, verbietet dasselbe Stichwort auch in unterschiedlichen Feeds.
- `ConfigureFeed` (Z. 84–102): Tabelle `feeds`; `notifications_enabled` required mit `HasDefaultValue(true)`; FK `CategoryId` mit `DeleteBehavior.SetNull`.
- `ConfigureItem` (Z. 104–120): Tabelle `items`; FK `FeedId` required mit `DeleteBehavior.Cascade`; Unique-Index `(feed_id, guid_or_hash)`.
- `DbSet<Keyword> Keywords` (Z. 41).

## Migrationen
Verzeichnis: `src/Reporter.Data/Migrations/`

Vorhandene Migrationen (Auswahl, Konvention `<timestamp>_<Name>`):

| Migration | Relevanz |
|-----------|----------|
| `20260909214617_InitialCreate` | Legt u. a. `keywords`-Tabelle mit Unique-Index auf `keyword_text` an. |
| `20260911181811_AddFeedNotificationsEnabled` | Vorbild für feed-spezifische Einstellung: `AddColumn<bool>` mit `defaultValue: true`. |
| `20260918022834_DropItemContentHtml` | Letzte Migration; verschiebt Item-Inhalt in die Content-DB. |

Keine Migration mit Feed-Bezug für `keywords` vorhanden.

## Domain-Modelle (`src/Reporter.Core/Models/`)

### `Keyword`
Datei: `src/Reporter.Core/Models/Keyword.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige ID. |
| `KeywordText` | `string` (required, init) | Stichwort-Text. |

Keine Feed-Zuordnung vorhanden.

### `Feed`
Datei: `src/Reporter.Core/Models/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Feed-ID. |
| `Url` | `string` (required, init) | Feed-URL. |
| `Title` | `string` (required, init) | Anzeigetitel. |
| `CategoryId` | `Guid?` (init) | Kategorie-ID. |
| `LastCheckedAt` | `DateTime?` (init) | Letzter Abruf. |
| `HealthStatus` | `string?` (init) | Gesundheitsstatus. |
| `HealthLastChange` | `DateTime?` (init) | Letzte Statusänderung. |
| `NotificationsEnabled` | `bool` (required, init) | Feed-spezifische Benachrichtigungs-Einstellung. |
| `FaviconUrl` | `string?` (init) | Favicon-URL. |
| `LastErrorKind` | `string?` (init) | Letzte Fehlerkategorie. |
| `LastErrorMessage` | `string?` (init) | Letzte Fehlermeldung. |

### `FeedListItem`
Datei: `src/Reporter.Core/Models/FeedListItem.cs`

Flaches Listen-/Detail-Modell; zusätzlich zu den `Feed`-Feldern: `CategoryName` (`string?`), `UnreadCount` (`int`, required), `FeedInitial` (berechnete Anzeige-Eigenschaft). Enthält keine Stichwort-Angabe.

### `Item`
Datei: `src/Reporter.Core/Models/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Item-ID. |
| `FeedId` | `Guid` (required, init) | Zugehöriger Feed — Grundlage für eine feed-spezifische Filterzuordnung. |
| `Title` | `string` (required, init) | Match-Ziel des Keyword-Filters. |
| `Link` | `string?` (init) | Artikel-Link. |
| `PublishedAt` | `DateTime?` (init) | Veröffentlichungszeitpunkt. |
| `GuidOrHash` | `string` (required, init) | Dedup-Schlüssel. |
| `IsRead` | `bool` (required, init) | Gelesen-Flag. |
| `IsSavedForLater` | `bool` (required, init) | „Später lesen"-Flag. |
| `ReadAt` | `DateTime?` (init) | Lesezeitpunkt. |
| `ContentHtml` | `string?` (init) | Artikel-HTML (aus Content-DB gemerged) — zweites Match-Ziel. |
| `Image` | `ItemImage?` (set) | Artikelbild. |
