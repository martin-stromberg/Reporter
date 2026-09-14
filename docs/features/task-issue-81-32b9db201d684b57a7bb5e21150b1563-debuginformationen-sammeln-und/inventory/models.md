<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell

## `Settings` (Core-Modell)
Datei: `src/Reporter.Core/Models/Settings.cs`

Singleton-Einstellungsdatensatz der Domäne. Alle Properties sind `init`-only; mehrere sind `required`. **Eine Eigenschaft `DebugCollectionEnabled` existiert nicht.**

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `DefaultId` | `static readonly Guid` | Singleton-ID `a1f5c6d2-4b3e-4c8f-9d2a-1b2c3d4e5f6a` |
| `Id` | `required Guid` | Primärschlüssel des Settings-Datensatzes |
| `RetentionDays` | `required int` | Aufbewahrungsdauer für Items in Tagen |
| `AutoMarkReadMode` | `string?` | Auto-Als-gelesen-Modus (`on_open`/`on_scroll`/`off`) |
| `AutoMarkReadDelaySeconds` | `required int` | Verzögerung für Auto-Als-gelesen |
| `NotificationsEnabled` | `required bool` | Globaler Benachrichtigungs-Schalter |
| `QuietHoursStart` | `TimeSpan?` | Beginn der Ruhezeiten |
| `QuietHoursEnd` | `TimeSpan?` | Ende der Ruhezeiten |
| `AutoRefreshEnabled` | `required bool` | Hintergrund-Refresh aktiv |
| `RefreshIntervalMinutes` | `required int` | Refresh-Intervall in Minuten |
| `RefreshOnStartupEnabled` | `required bool` | Refresh beim App-Start |
| `UnreadSortOrder` | `string?` | Sortierung der Ungelesen-Liste, Default `SettingsValues.SortOrderDescending` |
| `Theme` | `string?` | Erscheinungsbild, Default `SettingsValues.ThemeSystem` |
| `NotificationSummaryEnabled` | `required bool` | Sammel-Benachrichtigung pro Feed statt pro Artikel |
| `Language` | `string?` | Sprachwahl, Default `SettingsValues.LanguageSystem` |

## `Settings` (Entity)
Datei: `src/Reporter.Data/Entities/Settings.cs`

Persistenz-Entity mit denselben Feldern (settable, mit C#-Defaults). **Kein `DebugCollectionEnabled`.**

| Eigenschaft | Typ | Default |
|-------------|-----|---------|
| `Id` | `Guid` | `DefaultId` |
| `RetentionDays` | `int` | `30` |
| `AutoMarkReadMode` | `string?` | `SettingsValues.AutoMarkReadOnScroll` |
| `AutoMarkReadDelaySeconds` | `int` | `5` |
| `NotificationsEnabled` | `bool` | `true` |
| `QuietHoursStart` / `QuietHoursEnd` | `TimeSpan?` | `null` |
| `AutoRefreshEnabled` | `bool` | `true` |
| `RefreshIntervalMinutes` | `int` | `30` |
| `RefreshOnStartupEnabled` | `bool` | `true` |
| `UnreadSortOrder` | `string?` | `SettingsValues.SortOrderDescending` |
| `Theme` | `string?` | `SettingsValues.ThemeSystem` |
| `NotificationSummaryEnabled` | `bool` | `false` (C#-Default) |
| `Language` | `string?` | `SettingsValues.LanguageSystem` |

## `SyncLog` (Core-Modell)
Datei: `src/Reporter.Core/Models/SyncLog.cs`

Protokolleintrag einer Feed-Synchronisation — eine der Report-Datenquellen.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `required Guid` | Primärschlüssel |
| `FeedId` | `Guid?` | Optionaler Feed-Bezug |
| `StartedAt` | `DateTime?` | Sync-Start (UTC, von `FeedSyncService` gesetzt) |
| `FinishedAt` | `DateTime?` | Sync-Ende |
| `Status` | `string?` | `FeedHealth.Ok`/`Warning`/`Error` |
| `Message` | `string?` | Meldung/Fehlerdetails (enthält u. a. Feed-Kontext und Fehlermeldungen) |

Die Entity (`src/Reporter.Data/Entities/SyncLog.cs`) hat zusätzlich die Navigation `Feed? Feed` (FK mit `DeleteBehavior.SetNull`).

## `Feed` (Core-Modell)
Datei: `src/Reporter.Core/Models/Feed.cs`

Enthält die für den Report relevanten Health-Felder.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `required Guid` | Primärschlüssel |
| `Url` | `required string` | Feed-URL |
| `Title` | `required string` | Feed-Titel |
| `CategoryId` | `Guid?` | Kategorie-Bezug |
| `LastCheckedAt` | `DateTime?` | Zeitpunkt der letzten Prüfung |
| `HealthStatus` | `string?` | `FeedHealth`-Wert (`OK`/`Warning`/`Error`) |
| `HealthLastChange` | `DateTime?` | Zeitpunkt der letzten Statusänderung |
| `NotificationsEnabled` | `required bool` | Per-Feed-Benachrichtigungen |
| `FaviconUrl` | `string?` | Favicon der Website |

`FeedListItem` (`src/Reporter.Core/Models/FeedListItem.cs`) spiegelt dieselben Health-Felder plus `CategoryName`, `UnreadCount`, `FeedInitial` (Rückgabetyp von `IFeedRepository.GetAllWithDetailsAsync`).

## `SettingsValues` (Konstanten)
Datei: `src/Reporter.Core/Models/SettingsValues.cs`

| Konstante / Member | Wert / Signatur | Bedeutung |
|--------------------|-----------------|-----------|
| `AutoMarkReadOnOpen` | `"on_open"` | Beim Öffnen als gelesen markieren |
| `AutoMarkReadOnScroll` | `"on_scroll"` | Beim Scrollen markieren (Legacy-Seed-Default) |
| `AutoMarkReadOff` | `"off"` | Auto-Markierung aus |
| `ThemeSystem` / `ThemeLight` / `ThemeDark` | `"system"`/`"light"`/`"dark"` | Theme-Werte |
| `LanguageSystem` / `LanguageGerman` / `LanguageEnglish` | `"system"`/`"de"`/`"en"` | Sprachwerte |
| `SortOrderDescending` / `SortOrderAscending` | `"desc"`/`"asc"` | Sortierreihenfolge |
| `IsAutoMarkReadEnabled(string?)` | `bool` | `true` für jeden Modus außer `off` |

## `FeedHealth` (Konstantenklasse)
Datei: `src/Reporter.Core/Services/FeedHealth.cs`

| Konstante / Member | Wert / Signatur | Bedeutung |
|--------------------|-----------------|-----------|
| `Ok` | `"OK"` | Feed fehlerfrei |
| `Warning` | `"Warning"` | Feed mit Warnungen |
| `Error` | `"Error"` | Feed mit Fehlern |
| `Changed(string?, string?)` | `bool` | Statusänderungs-Vergleich |

## Persistenzkonfiguration (`ReporterDbContext`)
Datei: `src/Reporter.Data/ReporterDbContext.cs`

- `ConfigureSettings` (Zeilen 124–144) mappt die `settings`-Tabelle: snake_case-Spalten (`retention_days`, `auto_mark_read_mode`, `notifications_enabled`, `auto_refresh_enabled` mit `HasDefaultValue(true)`, `notification_summary_enabled` mit `HasDefaultValue(false)`, `language` u. a.), `Id` mit `ValueGeneratedNever()`, Seed via `entity.HasData(new Settings())`.
- `ConfigureSyncLog` (Zeilen 146–158): Tabelle `sync_logs`, Spalten `id`, `feed_id`, `started_at`, `finished_at`, `status` (max 50), `message`; optionale Feed-FK mit `SetNull`.
- Vorhandene Migrationen (`src/Reporter.Data/Migrations/`): `InitialCreate`, `AddSettingsAutoRefreshAndTheme`, `AddFeedNotificationsEnabled`, `AddSettingsNotificationSummary`, `AddSettingsLanguage`, `AddFeedFaviconUrl`, `AddSettingsStartupRefreshAndSortOrder` — Namenskonvention `AddSettings*` bzw. `AddFeed*`.
- Design-Time-Factory für `dotnet ef`: `src/Reporter.Data/ReporterDbContextFactory.cs` (`IDesignTimeDbContextFactory<ReporterDbContext>`).
