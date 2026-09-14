<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell — Bestandsaufnahme

## `Feed` (Core-Modell)

Datei: `src/Reporter.Core/Models/Feed.cs`

Alle Eigenschaften sind `init`-only (unveränderliches Modell). Es existiert **keine** Eigenschaft für eine Fehlermeldung (z. B. `LastErrorMessage`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required) | Eindeutige Feed-ID |
| `Url` | `string` (required) | Feed-URL; Scheme ist nicht eingeschränkt, `http`/`https` werden an anderen Stellen validiert |
| `Title` | `string` (required) | Anzeigetitel |
| `CategoryId` | `Guid?` | Optionale Kategorie |
| `LastCheckedAt` | `DateTime?` | Zeitpunkt des letzten Syncs |
| `HealthStatus` | `string?` | Health-Status (`FeedHealth`-Konstanten) |
| `HealthLastChange` | `DateTime?` | Zeitpunkt der letzten Statusänderung |
| `NotificationsEnabled` | `bool` (required) | Benachrichtigungen für diesen Feed |
| `FaviconUrl` | `string?` | Favicon-URL der Website (durch `AddFeedFaviconUrl`-Migration ergänzt) |

Wird geschrieben von `FeedRepository.UpdateAsync`/`MapToEntity`, gelesen u. a. in `FeedSyncService.RunSyncAsync` und `FeedsViewModel`.

## `FeedListItem` (Core-Modell)

Datei: `src/Reporter.Core/Models/FeedListItem.cs`

Anzeigemodell der Feed-Karte. Enthält **keine** Fehlermeldungs-Eigenschaft.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required) | Feed-ID |
| `Title` | `string` (required) | Anzeigetitel |
| `Url` | `string` (required) | Feed-URL |
| `CategoryId` | `Guid?` | Kategorie-ID |
| `CategoryName` | `string?` | Kategoriename für die Anzeige |
| `LastCheckedAt` | `DateTime?` | Letzter Sync-Zeitpunkt |
| `HealthStatus` | `string?` | Health-Status; steuert Badge-Farbe/-Text in `FeedsPage.xaml` per `DataTrigger` |
| `HealthLastChange` | `DateTime?` | Zeitpunkt der letzten Statusänderung |
| `UnreadCount` | `int` (required) | Anzahl ungelesener Einträge |
| `NotificationsEnabled` | `bool` (required) | Benachrichtigungs-Flag |
| `FaviconUrl` | `string?` | Favicon-URL |
| `FeedInitial` | `string` (get) | Fallback-Avatar-Buchstabe via `FeedAvatar.Initial(Title)` |

Wird in `FeedRepository.GetAllWithDetailsAsync` (`FeedRepository.cs:99-112`) per Projektion befüllt; gebunden in `FeedsPage.xaml` und `FeedsViewModel.Feeds`.

## `SyncLog` (Core-Modell)

Datei: `src/Reporter.Core/Models/SyncLog.cs`

Trägt bereits die persistierte Fehlermeldung; es fehlt nur ein Feed-bezogener Lesezugriff.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required) | Log-ID |
| `FeedId` | `Guid?` | Zugehöriger Feed (nullable) |
| `StartedAt` | `DateTime?` | Sync-Start |
| `FinishedAt` | `DateTime?` | Sync-Ende |
| `Status` | `string?` | Sync-Status (`FeedHealth`-Konstanten) |
| `Message` | `string?` | Status-/Fehlermeldung; `FeedSyncService` schreibt `"Synchronization failed: {ex.Message}"` (`FeedSyncService.cs:94`) bzw. Erfolgstexte wie `"Synchronized N items, M new."` |

## `Feed` (Entity)

Datei: `src/Reporter.Data/Entities/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel |
| `Url` | `string` | Feed-URL |
| `Title` | `string` | Titel |
| `CategoryId` | `Guid?` | Kategorie-FK |
| `LastCheckedAt` | `DateTime?` | Letzter Sync |
| `HealthStatus` | `string?` | Health-Status |
| `HealthLastChange` | `DateTime?` | Letzte Statusänderung |
| `NotificationsEnabled` | `bool` | Default `true` |
| `FaviconUrl` | `string?` | Favicon-URL |
| `Category` | `Category?` | Navigation |

## `SyncLog` (Entity)

Datei: `src/Reporter.Data/Entities/SyncLog.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel |
| `FeedId` | `Guid?` | Feed-FK (`feed_id`) |
| `StartedAt` / `FinishedAt` | `DateTime?` | Zeitstempel |
| `Status` | `string?` | Status (`status`, `HasMaxLength(50)`) |
| `Message` | `string?` | Meldung (`message`, ohne Längenbegrenzung) |
| `Feed` | `Feed?` | Navigation |

## `ReporterDbContext`-Mapping

Datei: `src/Reporter.Data/ReporterDbContext.cs`

- `ConfigureFeed` (`:84-100`): Tabelle `feeds`, Spalten `id`, `url` (max 2048, required, Unique-Index), `title` (max 500), `category_id`, `last_checked_at`, `health_status` (max 50), `health_last_change`, `notifications_enabled` (Default `true`), `favicon_url` (max 2048). **Keine** `last_error_message`-Spalte.
- `ConfigureSyncLog` (`:153-165`): Tabelle `sync_logs`, Spalten `id`, `feed_id`, `started_at`, `finished_at`, `status` (max 50), `message`; FK `FeedId` → `feeds` mit `DeleteBehavior.SetNull`. **Kein** Index auf `feed_id`.
- Namenskonvention: snake_case-Spaltennamen.

## Migrationen

Datei: `src/Reporter.Data/Migrations/`

Vorhandene `AddFeed*`-Referenz: `20260913211308_AddFeedFaviconUrl.cs` (fügt `favicon_url` TEXT, max 2048, nullable hinzu). Letzte Migrationen außerdem: `AddSettingsStartupRefreshAndSortOrder`, `AddSettingsDebugCollection`, `AddDebugLogEntries`. `context.Database.Migrate()` läuft beim App-Start in `MauiProgram.ApplyPersistedLanguage` (`MauiProgram.cs:110`).

## Weitere relevante Modelle

- `SyncResult` (`src/Reporter.Core/Services/SyncResult.cs`): `record SyncResult(string Status, int NewItems, string? Message = null)` — transportiert die Fehlermeldung bereits Richtung ViewModel, wird dort aber durch `AppResources.SyncStatusError` ersetzt.
- `FeedSearchResult` (`src/Reporter.Core/Models/FeedSearchResult.cs`): `FeedUrl` (required), `SiteUrl`, `Title`, `Description`, `SiteName`, `Score`, `MatchKind`, `DisplayTitle` — kann `http`-URLs enthalten.
