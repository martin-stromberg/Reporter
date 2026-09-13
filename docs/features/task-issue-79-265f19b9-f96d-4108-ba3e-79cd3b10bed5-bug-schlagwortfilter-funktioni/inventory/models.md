<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell — Bestandsaufnahme

Betroffene Modelle (Domänenschicht `Reporter.Core`) und Persistenz-Entitäten (`Reporter.Data`) für den Schlagwortfilter.

## `Keyword` (Domänenmodell)

Datei: `src/Reporter.Core/Models/Keyword.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige Kennung des Schlagworts. |
| `KeywordText` | `string` (required, init) | Schlagwort-Text, der vom `KeywordMatcher` als Teilwort gematcht wird. |

## `Item` (Domänenmodell)

Datei: `src/Reporter.Core/Models/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige Kennung des Artikels. |
| `FeedId` | `Guid` (required, init) | Zugehöriger Feed. |
| `Title` | `string` (required, init) | Artikeltitel — wird vom `KeywordMatcher` gematcht. |
| `Link` | `string?` (init) | Link zum Originalartikel — wird **nicht** gematcht. |
| `PublishedAt` | `DateTime?` (init) | Veröffentlichungsdatum — Fristbasis der Keyword-Löschregel im `RetentionCleanupService`. |
| `GuidOrHash` | `string` (required, init) | Original-GUID oder Hash — Dedup-Schlüssel in `FeedSyncService.RunSyncAsync` (`knownKeys`). |
| `IsRead` | `bool` (required, init) | Gelesen-Flag — neue Items werden mit `false` gespeichert; Bedingung beider Löschregeln. |
| `IsSavedForLater` | `bool` (required, init) | Merken-Flag — neue Items werden mit `false` gespeichert; Lösch-Invariante. |
| `ReadAt` | `DateTime?` (init) | Zeitpunkt des Lesens — Fallback-Fristbasis. |
| `ContentHtml` | `string?` (init) | HTML-Inhalt für Offline-Lesen — wird vom `KeywordMatcher` gematcht. |

Es existiert **kein** persistiertes Filter-Flag am `Item`; ein Treffer ist an den gespeicherten Daten nicht erkennbar.

## `ItemListItem` (Listen-Projektion)

Datei: `src/Reporter.Core/Models/ItemListItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Artikel-Kennung. |
| `FeedId` | `Guid` (required, init) | Zugehöriger Feed. |
| `Title` | `string` (required, init) | Anzeigetitel. |
| `Link` | `string?` (init) | Link zum Originalartikel. |
| `PublishedAt` | `DateTime?` (init) | Veröffentlichungsdatum (Sortierung). |
| `IsRead` | `bool` (required, init) | Gelesen-Flag. |
| `IsSavedForLater` | `bool` (required, init) | Merken-Flag. |
| `FeedTitle` | `string` (init, Default `""`) | Anzeigetitel des Feeds. |
| `CategoryId` | `Guid?` (init) | Kategorie des Feeds. |
| `CategoryName` | `string?` (init) | Anzeigename der Kategorie. |
| `ImageUrl` | `string?` (init) | Aus `ContentHtml` extrahierte Bild-URL. |
| `Summary` | `string?` (init) | Aus `ContentHtml` extrahierte Klartext-Zusammenfassung. |
| `ReadingTimeText` | `string?` (init) | Formatierte Lesezeit-Schätzung. |

Methode: `CopyWith(bool? isRead = null, bool? isSavedForLater = null)` — Kopie mit geändertem Lese-/Merken-Status.

## `Keyword` (EF-Core-Entität)

Datei: `src/Reporter.Data/Entities/Keyword.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (get/set) | Primärschlüssel (`keywords.id`). |
| `KeywordText` | `string` (get/set, Default `""`) | Schlagwort (`keywords.keyword_text`, max. 500, `IsRequired`, Unique-Index). |

## `Item` (EF-Core-Entität)

Datei: `src/Reporter.Data/Entities/Item.cs`

Spiegelt das Domänenmodell (`Id`, `FeedId`, `Title`, `Link`, `PublishedAt`, `GuidOrHash`, `IsRead`, `IsSavedForLater`, `ReadAt`, `ContentHtml`) als setzbare Eigenschaften; zusätzlich Navigation `Feed` (`Feed Feed`, `DeleteBehavior.Cascade`). Tabelle `items`; Unique-Index auf `{ FeedId, GuidOrHash }` — die Dedup-Grundlage des Syncs.

## `ReporterDbContext`

Datei: `src/Reporter.Data/ReporterDbContext.cs`

- `DbSet<Keyword> Keywords` → Tabelle `keywords` (`ConfigureKeyword`, Zeilen 114–121).
- `DbSet<Item> Items` → Tabelle `items` (`ConfigureItem`, Zeilen 95–112).
- Weitere Sets: `Feeds`, `Categories`, `Settings`, `SyncLogs`.
- Bestehende Migrationen unter `src/Reporter.Data/Migrations/` (Stand: `20260913110816_AddSettingsLanguage`); die `keywords`-Tabelle ist seit `InitialCreate` vorhanden.
