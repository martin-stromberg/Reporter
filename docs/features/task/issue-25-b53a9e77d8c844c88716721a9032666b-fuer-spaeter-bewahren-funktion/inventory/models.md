# Datenmodell – Bestandsaufnahme

## `Item` (Entity)
Datei: `src/Reporter.Data/Entities/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel |
| `FeedId` | `Guid` | FK auf `Feed` (required, `DeleteBehavior.Cascade`) |
| `Title` | `string` | Artikeltitel (max. 500, required) |
| `Link` | `string?` | Artikel-Link (max. 2048) |
| `PublishedAt` | `DateTime?` | Veröffentlichungszeitpunkt — Grundlage der Sortierung in `GetSavedForLaterAsync` |
| `GuidOrHash` | `string` | Original-GUID oder Hash (max. 500, required; Unique-Index zusammen mit `FeedId`) |
| `IsRead` | `bool` | Gelesen-Status |
| `IsSavedForLater` | `bool` | **Bewahrungsstatus (Feature-Flag des Issues)** |
| `ReadAt` | `DateTime?` | Zeitpunkt des Gelesen-Markierens |
| `ContentHtml` | `string?` | HTML-Inhalt für Offline-Lektüre |
| `Feed` | `Feed` | Navigation zum Feed |

Mapping in `src/Reporter.Data/ReporterDbContext.cs`, `ConfigureItem` (Zeilen 92–109): Tabelle `items`, `IsSavedForLater` → Spalte `is_saved_for_later` (non-nullable `INTEGER`, nicht required-markiert, aber bool = nicht-nullbar). Spalte seit Migration `InitialCreate` vorhanden (`src/Reporter.Data/Migrations/20260909214617_InitialCreate.cs`, Zeile 89). **Kein `SavedAt`-Feld** — weder Entity noch Modell noch Migration enthalten ein Speicherdatum (Grep `SavedAt|saved_at`: nur Fundstellen in `requirement.md`).

Beziehung: `entity.HasOne(e => e.Feed).WithMany().HasForeignKey(e => e.FeedId).IsRequired().OnDelete(DeleteBehavior.Cascade)` (`ReporterDbContext.cs:107`) — das Löschen eines Feeds entfernt alle Items kaskadenartig, **einschließlich bewahrter** Artikel.

## `Item` (Domänenmodell)
Datei: `src/Reporter.Core/Models/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required init) | Eindeutige ID |
| `FeedId` | `Guid` (required init) | Zugehöriger Feed |
| `Title` | `string` (required init) | Titel |
| `Link` | `string?` (init) | Artikel-Link |
| `PublishedAt` | `DateTime?` (init) | Veröffentlichungszeitpunkt |
| `GuidOrHash` | `string` (required init) | Original-GUID/Hash |
| `IsRead` | `bool` (required init) | Gelesen-Status |
| `IsSavedForLater` | `bool` (required init) | Bewahrungsstatus |
| `ReadAt` | `DateTime?` (init) | Lesezeitpunkt |
| `ContentHtml` | `string?` (init) | HTML-Inhalt |

Immutable (`init`-only); Änderungen erzeugen Kopien, z. B. `ArticleDetailViewModel.CreateItemCopy`.

## `ItemListItem` (Listen-Modell)
Datei: `src/Reporter.Core/Models/ItemListItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required init) | Eindeutige ID |
| `FeedId` | `Guid` (required init) | Zugehöriger Feed |
| `Title` | `string` (required init) | Titel |
| `Link` | `string?` (init) | Artikel-Link |
| `PublishedAt` | `DateTime?` (init) | Veröffentlichungszeitpunkt (Sortierschlüssel der Später-Liste) |
| `IsRead` | `bool` (required init) | Gelesen-Status |
| `IsSavedForLater` | `bool` (required init) | Bewahrungsstatus — steuert `DataTrigger` des Bookmark-Icons in `ArticleCardView` |
| `FeedTitle` | `string` (init, default `""`) | Anzeigename des Feeds |
| `CategoryId` | `Guid?` (init) | Kategorie des Feeds |
| `CategoryName` | `string?` (init) | Kategoriename (Badge in `ArticleCardView`) |
| `ImageUrl` | `string?` (init) | Aus `ContentHtml` extrahierte Bild-URL |
| `Summary` | `string?` (init) | Plain-Text-Auszug (max. 120 Zeichen + „…“) |

Wird von `ItemRepository.GetSavedForLaterAsync` und `ItemRepository.GetUnreadByDateAsync(page,…)` befüllt; Binding-Quelle für `LaterPage`/`UnreadPage`.

## `Settings` (Entity)
Datei: `src/Reporter.Data/Entities/Settings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Singleton-ID, `DefaultId = a1f5c6d2-…` |
| `RetentionDays` | `int` (default `30`) | **Aufbewahrungsdauer in Tagen — wird im Produktivcode nirgends ausgewertet** |
| `AutoMarkReadMode` | `string?` (default `"on_scroll"`) | Auto-Gelesen-Modus |
| `AutoMarkReadDelaySeconds` | `int` (default `5`) | Verzögerung Auto-Gelesen |
| `NotificationsEnabled` | `bool` (default `true`) | Benachrichtigungen |
| `QuietHoursStart` / `QuietHoursEnd` | `TimeSpan?` | Ruhezeiten |

Mapping `ConfigureSettings` (`ReporterDbContext.cs:120–133`): Tabelle `settings`, Spalte `retention_days` (required), Seed-Datensatz via `entity.HasData(new Settings())` (Default 30 Tage, in Migration `InitialCreate` enthalten).

## `Settings` (Domänenmodell)
Datei: `src/Reporter.Core/Models/Settings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `DefaultId` | `static readonly Guid` | Singleton-ID `a1f5c6d2-4b3e-4c8f-9d2a-1b2c3d4e5f6a` |
| `Id` | `Guid` (required init) | Datensatz-ID |
| `RetentionDays` | `int` (required init) | Aufbewahrungsdauer — nur persistiert, keine Auswertung |
| `AutoMarkReadMode` | `string?` (init) | Auto-Gelesen-Modus |
| `AutoMarkReadDelaySeconds` | `int` (required init) | Verzögerung Auto-Gelesen (wird in `ArticleDetailViewModel.LoadAsync` genutzt) |
| `NotificationsEnabled` | `bool` (required init) | Benachrichtigungen |
| `QuietHoursStart` / `QuietHoursEnd` | `TimeSpan?` (init) | Ruhezeiten |

## Weitere relevante Modelle (Kontext)

- `Reporter.Core.Models.Feed` / `FeedListItem` — werden von `GetSavedForLaterAsync` über `i.Feed.Title`, `i.Feed.CategoryId`, `i.Feed.Category.Name` projiziert.
- `Reporter.Core.Models.SyncLog` — Protokollierung der Sync-Läufe, `FeedId`-FK mit `DeleteBehavior.SetNull`.
- `FeedHealth` (`src/Reporter.Core/Services/FeedHealth.cs`) ist **kein Enum**, sondern eine statische Klasse mit String-Konstanten `Ok`/`Warning`/`Error`; es existieren keine Enums im Codebestand.
