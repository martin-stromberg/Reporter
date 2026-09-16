<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell

Domänenmodelle liegen in `Reporter.Core.Models`, die EF-Core-Entities in `Reporter.Data.Entities`. Repositories mappen zwischen beiden. Für die Anforderung relevant: `Category` und `Feed` (Seed-Ziele), `Settings` (potenzielles First-Run-Flag sowie Defaults für Start-Abruf und Benachrichtigungen), `FeedListItem`/`CategoryWithCount` (Anzeige der geseedeten Daten).

## `Category` (Domänenmodell)

Datei: `src/Reporter.Core/Models/Category.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige ID der Kategorie |
| `Name` | `string` (required, init) | Name der Kategorie |

## `Category` (Entity)

Datei: `src/Reporter.Data/Entities/Category.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel |
| `Name` | `string` (Default `string.Empty`) | Kategoriename; Spalte `categories.name`, `maxLength: 500`, `IsRequired`, **Unique-Index** (`ReporterDbContext.cs:81`, Migration `IX_categories_name`, keine NOCASE-Kollation → case-sensitiv) |

## `Feed` (Domänenmodell)

Datei: `src/Reporter.Core/Models/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige ID des Feeds |
| `Url` | `string` (required, init) | Feed-URL |
| `Title` | `string` (required, init) | Anzeigetitel |
| `CategoryId` | `Guid?` (init) | Optionale Kategorie-Zuordnung |
| `LastCheckedAt` | `DateTime?` (init) | Zeitpunkt des letzten Abrufs; `null` = noch nie synchronisiert |
| `HealthStatus` | `string?` (init) | `FeedHealth`-Wert (`"OK"`/`"Warning"`/`"Error"`) |
| `HealthLastChange` | `DateTime?` (init) | Zeitpunkt der letzten Statusänderung |
| `NotificationsEnabled` | `bool` (required, init) | Feed-spezifischer Benachrichtigungsschalter |
| `FaviconUrl` | `string?` (init) | Favicon der Feed-Website; wird beim ersten erfolgreichen Sync von `FeedSyncService` nachgezogen (`FeedSyncService.cs:196`) |
| `LastErrorKind` | `string?` (init) | `FeedSyncErrorKind`-Wert des letzten Sync-Fehlers, `null` bei Erfolg |
| `LastErrorMessage` | `string?` (init) | Technische Meldung des letzten Sync-Fehlers |

## `Feed` (Entity)

Datei: `src/Reporter.Data/Entities/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel |
| `Url` | `string` | Spalte `feeds.url`, `maxLength: 2048`, `IsRequired`, **Unique-Index** (`ReporterDbContext.cs:100`) |
| `Title` | `string` | `feeds.title`, `maxLength: 500`, `IsRequired` |
| `CategoryId` | `Guid?` | `feeds.category_id`, FK auf `categories` mit `DeleteBehavior.SetNull` (`ReporterDbContext.cs:101`) — Kategorie-Löschung löst die Zuordnung, Feed bleibt |
| `LastCheckedAt` | `DateTime?` | `feeds.last_checked_at` |
| `HealthStatus` | `string?` | `feeds.health_status`, `maxLength: 50` |
| `HealthLastChange` | `DateTime?` | `feeds.health_last_change` |
| `NotificationsEnabled` | `bool` (Default `true`) | `feeds.notifications_enabled`, `IsRequired`, `HasDefaultValue(true)` |
| `FaviconUrl` | `string?` | `feeds.favicon_url`, `maxLength: 2048` |
| `LastErrorKind` | `string?` | `feeds.last_error_kind`, `maxLength: 50` |
| `LastErrorMessage` | `string?` | `feeds.last_error_message` |
| `Category` | `Category?` | Navigation zur Kategorie |

## `Settings` (Domänenmodell)

Datei: `src/Reporter.Core/Models/Settings.cs`

Singleton-Datensatz (`DefaultId = a1f5c6d2-…`). Für die Anforderung relevante Eigenschaften:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required) | Singleton-ID (`DefaultId`) |
| `NotificationsEnabled` | `bool` (required) | Globaler Benachrichtigungsschalter — Entity-Default `true` |
| `AutoRefreshEnabled` | `bool` (required) | Hintergrund-Aktualisierung — Entity-Default `true` |
| `RefreshIntervalMinutes` | `int` (required) | Abrufintervall — Entity-Default `30` |
| `RefreshOnStartupEnabled` | `bool` (required) | Start-Abruf — Entity-Default `true`; steuert `AutoRefreshService.RunStartupSyncAsync` |
| `NotificationSummaryEnabled` | `bool` (required) | Sammel- statt Einzelbenachrichtigungen — Entity-Default `false` |
| `DebugCollectionEnabled` | `bool` (required) | Session-Debug-Log — Entity-Default `false` |
| `RetentionDays`, `AutoMarkReadMode`, `AutoMarkReadDelaySeconds`, `QuietHoursStart`/`End`, `UnreadSortOrder`, `Theme`, `Language` | diverse | Nicht direkt relevant; volle Liste in den Dateien |

## `Settings` (Entity)

Datei: `src/Reporter.Data/Entities/Settings.cs`

Wichtig: `ReporterDbContext.ConfigureSettings` enthält **`entity.HasData(new Settings())`** (`ReporterDbContext.cs:152`) — die Tabelle `settings` wird per Migration mit einer Default-Zeile vorbefüllt (einziger vorhandener `HasData`-Seed im Projekt; `InitialCreate.cs:128` `InsertData`). Das ist der bestehende Präzedenzfall für deklaratives Seeden — für den Demo-Feed laut Anforderung nicht empfohlen, da die Migration auch Bestandsinstallationen treffen und in allen `EnsureCreated`-Test-DBs landen würde. `id` ist `ValueGeneratedNever`. Es gibt **keine** Spalte für ein First-Run-Flag; Variante (d) der Anforderung würde eine neue Spalte plus Migration erfordern.

## `FeedListItem` (Anzeige-Projektion)

Datei: `src/Reporter.Core/Models/FeedListItem.cs`

Von `IFeedRepository.GetAllWithDetailsAsync` projiziert; die Feed-Karte in `FeedsPage` bindet daran. Felder: `Id`, `Title`, `Url`, `CategoryId`, `CategoryName` (Join auf `categories.name`), `LastCheckedAt`, `HealthStatus`, `HealthLastChange`, `UnreadCount`, `NotificationsEnabled`, `FaviconUrl`, `LastErrorKind`, `LastErrorMessage`, `FeedInitial` (Avatar-Fallback via `FeedAvatar.Initial`). Ein geseedeter Demo-Feed erscheint hier automatisch mit `CategoryName = "News"`.

## `CategoryWithCount` (Anzeige-Projektion)

Datei: `src/Reporter.Core/Models/CategoryWithCount.cs`

Von `ICategoryRepository.GetAllWithFeedCountAsync` projiziert (`GroupJoin` auf `feeds`): `Id`, `Name`, `FeedCount`. Die geseedete Kategorie „News" erscheint in `CategoriesPage` mit `FeedCount = 1`.

## `ReporterDbContext` (Mapping-Übersicht)

Datei: `src/Reporter.Data/ReporterDbContext.cs`

- DbSets: `Feeds`, `Categories`, `Items`, `Keywords`, `Settings`, `SyncLogs`, `DebugLogEntries`.
- Unique-Indizes: `categories.name` (Zeile 81), `feeds.url` (Zeile 100), `keywords.keyword_text` (Zeile 129), `items`(`feed_id`, `guid_or_hash`) (Zeile 120).
- Delete-Verhalten: `feeds.category_id` → `SetNull` (Zeile 101), `items.feed_id` → `Cascade` (Zeile 119), `sync_logs.feed_id` → `SetNull` (Zeile 166).
- Migrationen: 10 vorhanden unter `src/Reporter.Data/Migrations/` (`20260909214617_InitialCreate` bis `20260914183510_AddFeedLastError`), zuletzt `AddFeedLastError`. `ReporterDbContextFactory` (Design-Time) nutzt `Data Source=reporter.db`.
- `TestDbContextFactory` (Tests) nutzt `EnsureCreated()` statt Migrationen — ein `HasData`-Seed auf `feeds`/`categories` würde alle Test-Datenbanken vorbelegen.
