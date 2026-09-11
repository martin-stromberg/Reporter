# Datenmodell

Bestehende Datenmodellklassen, die für die Anforderung „Lokale iOS-Benachrichtigungen mit Ruhezeiten" relevant sind.

## `Reporter.Core.Models.Feed`
Datei: `src/Reporter.Core/Models/Feed.cs`

Domänenmodell eines Feeds. **Keine `NotificationsEnabled`-Eigenschaft vorhanden.**

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige Feed-ID (Zeile 11) |
| `Url` | `string` (required, init) | Feed-URL (Zeile 16) |
| `Title` | `string` (required, init) | Feed-Titel (Zeile 21) |
| `CategoryId` | `Guid?` (init) | Optionale Kategorie (Zeile 26) |
| `LastCheckedAt` | `DateTime?` (init) | Zeitpunkt der letzten Prüfung (Zeile 31) |
| `HealthStatus` | `string?` (init) | Aktueller Health-Status (Werte aus `FeedHealth`, Zeile 36) |
| `HealthLastChange` | `DateTime?` (init) | Zeitpunkt der letzten Statusänderung (Zeile 41) |

## `Reporter.Data.Entities.Feed`
Datei: `src/Reporter.Data/Entities/Feed.cs`

EF-Core-Entität (Tabelle `feeds`). **Keine `NotificationsEnabled`-Eigenschaft vorhanden.**

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel, Spalte `id` (Zeile 11) |
| `Url` | `string` | Spalte `url`, max. 2048, required, Unique-Index (Zeile 16; Mapping `ReporterDbContext.cs:81,88`) |
| `Title` | `string` | Spalte `title`, max. 500, required (Zeile 21; Mapping `ReporterDbContext.cs:82`) |
| `CategoryId` | `Guid?` | Spalte `category_id`, FK `SetNull` (Zeile 26; Mapping `ReporterDbContext.cs:83,89`) |
| `LastCheckedAt` | `DateTime?` | Spalte `last_checked_at` (Zeile 31; Mapping `ReporterDbContext.cs:84`) |
| `HealthStatus` | `string?` | Spalte `health_status`, max. 50 (Zeile 36; Mapping `ReporterDbContext.cs:85`) |
| `HealthLastChange` | `DateTime?` | Spalte `health_last_change` (Zeile 41; Mapping `ReporterDbContext.cs:86`) |
| `Category` | `Category?` | Navigation zur Kategorie (Zeile 46) |

Spalten-Mapping erfolgt in `ReporterDbContext.ConfigureFeed` (`src/Reporter.Data/ReporterDbContext.cs:76-90`). Bestehende Migrationen: `20260909214617_InitialCreate`, `20260911080630_AddSettingsAutoRefreshAndTheme` (Muster für `AddColumn<bool>` mit `defaultValue` + `UpdateData` des Settings-Singletons). Design-Time-Factory für EF-Core-Tools: `ReporterDbContextFactory` (`src/Reporter.Data/ReporterDbContextFactory.cs`).

## `Reporter.Core.Models.FeedListItem`
Datei: `src/Reporter.Core/Models/FeedListItem.cs`

Listen-/Anzeigemodell eines Feeds. **Keine `NotificationsEnabled`-Eigenschaft vorhanden.**

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Feed-ID (Zeile 11) |
| `Title` | `string` (required, init) | Anzeigetitel (Zeile 16) |
| `Url` | `string` (required, init) | Feed-URL (Zeile 21) |
| `CategoryId` | `Guid?` (init) | Kategorie-ID (Zeile 26) |
| `CategoryName` | `string?` (init) | Kategoriename oder `null` (Zeile 31) |
| `LastCheckedAt` | `DateTime?` (init) | Letzte Prüfung (Zeile 36) |
| `HealthStatus` | `string?` (init) | Health-Status (Zeile 41) |
| `HealthLastChange` | `DateTime?` (init) | Letzte Statusänderung (Zeile 46) |
| `UnreadCount` | `int` (required, init) | Anzahl ungelesener Artikel (Zeile 51) |

Wird in `FeedRepository.GetAllWithDetailsAsync` (`src/Reporter.Data/Repositories/FeedRepository.cs:87-110`) per Projektion befüllt — ein neues Feld müsste dort mitprojiziert werden.

## `Reporter.Core.Models.Settings`
Datei: `src/Reporter.Core/Models/Settings.cs`

Singleton-Einstellungen. **Bereits vollständig für die Anforderung vorhanden.**

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `DefaultId` | `static readonly Guid` | Singleton-ID `a1f5c6d2-…-5f6a` (Zeile 12) |
| `Id` | `Guid` (required, init) | Datensatz-ID (Zeile 17) |
| `RetentionDays` | `int` (required, init) | Aufbewahrungsdauer in Tagen (Zeile 22) |
| `AutoMarkReadMode` | `string?` (init) | Modus für automatisches Gelesen-Markieren (Zeile 27) |
| `AutoMarkReadDelaySeconds` | `int` (required, init) | Verzögerung (Zeile 32) |
| `NotificationsEnabled` | `bool` (required, init) | **Globaler Benachrichtigungs-Schalter** (Zeile 37) |
| `QuietHoursStart` | `TimeSpan?` (init) | **Beginn der Ruhezeit, `null` = aus** (Zeile 42) |
| `QuietHoursEnd` | `TimeSpan?` (init) | **Ende der Ruhezeit** (Zeile 47) |
| `AutoRefreshEnabled` | `bool` (required, init) | Hintergrund-Aktualisierung an/aus (Zeile 52) |
| `RefreshIntervalMinutes` | `int` (required, init) | Abrufintervall in Minuten (Zeile 57) |
| `Theme` | `string?` (init, Default `SettingsValues.ThemeSystem`) | Farbschema (Zeile 62) |

## `Reporter.Data.Entities.Settings`
Datei: `src/Reporter.Data/Entities/Settings.cs`

EF-Core-Entität (Tabelle `settings`), Defaults: `NotificationsEnabled = true` (Zeile 39), `QuietHoursStart`/`QuietHoursEnd = null` (Zeilen 44/49). Spalten-Mapping in `ReporterDbContext.ConfigureSettings` (`ReporterDbContext.cs:120-136`): `notifications_enabled` (required, Zeile 128), `quiet_hours_start`/`quiet_hours_end` (nullable, Zeilen 129-130); `entity.HasData(new Settings())` seedet den Singleton (Zeile 135).

Mapping Entity ↔ Modell in `SettingsRepository` (`src/Reporter.Data/Repositories/SettingsRepository.cs:53-80`) — alle drei Benachrichtigungs-Felder werden bereits übertragen.

## `Reporter.Core.Models.Item`
Datei: `src/Reporter.Core/Models/Item.cs`

Artikel-Domänenmodell — Benachrichtigungskandidaten sind neu gespeicherte `Item`s.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Artikel-ID — Kandidat für stabilen Notification-Identifier (Zeile 11) |
| `FeedId` | `Guid` (required, init) | Zugehöriger Feed (Zeile 16) |
| `Title` | `string` (required, init) | Titel — wird von `IKeywordMatcher.MatchesAny` geprüft (Zeile 21) |
| `Link` | `string?` (init) | Artikel-Link (Zeile 26) |
| `PublishedAt` | `DateTime?` (init) | Veröffentlichungszeitpunkt (Zeile 31) |
| `GuidOrHash` | `string` (required, init) | Original-GUID oder SHA256-Hash — Grundlage der Dublettenerkennung (Zeile 36) |
| `IsRead` | `bool` (required, init) | Gelesen-Flag (Zeile 41) |
| `IsSavedForLater` | `bool` (required, init) | Später-lesen-Flag (Zeile 46) |
| `ReadAt` | `DateTime?` (init) | Gelesen-Zeitpunkt (Zeile 51) |
| `ContentHtml` | `string?` (init) | HTML-Inhalt — wird von `IKeywordMatcher.MatchesAny` geprüft (Zeile 56) |

Unique-Index `(feed_id, guid_or_hash)` auf der `items`-Tabelle (`ReporterDbContext.cs:108`) stellt sicher, dass ein Artikel nur einmal gespeichert wird.

## `Reporter.Core.Models.Keyword`
Datei: `src/Reporter.Core/Models/Keyword.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Keyword-ID (Zeile 11) |
| `KeywordText` | `string` (required, init) | Filtertext, Unique-Index auf `keyword_text` (Zeile 16; Mapping `ReporterDbContext.cs:116-117`) |

## `Reporter.Core.Services.SyncResult`
Datei: `src/Reporter.Core/Services/SyncResult.cs`

Positional Record: `SyncResult(string Status, int NewItems, string? Message = null)` — trägt aktuell nur die **Anzahl** neuer Artikel, nicht die `Item`-Liste (Zeile 10).
