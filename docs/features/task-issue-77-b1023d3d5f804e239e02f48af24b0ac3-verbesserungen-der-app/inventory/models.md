<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell — Bestandsaufnahme (Issue #77, R1–R8)

## `Settings` (Domänenmodell)
Datei: `src/Reporter.Core/Models/Settings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `DefaultId` | `static readonly Guid` | Singleton-ID `a1f5c6d2-…` |
| `Id` | `Guid` (required, init) | Datensatz-ID |
| `RetentionDays` | `int` (required) | Aufbewahrungsdauer in Tagen |
| `AutoMarkReadMode` | `string?` | Auto-Als-gelesen-Modus (`SettingsValues.AutoMarkRead*`) |
| `AutoMarkReadDelaySeconds` | `int` (required) | Verzögerung bis zum automatischen Markieren |
| `NotificationsEnabled` | `bool` (required) | Globaler Benachrichtigungs-Schalter (R8-Kontext) |
| `QuietHoursStart` / `QuietHoursEnd` | `TimeSpan?` | Ruhezeiten |
| `AutoRefreshEnabled` | `bool` (required) | Periodischer Abruf im laufenden Betrieb (R3-Kontext: nur Intervall-Loop, kein Start-Abruf) |
| `RefreshIntervalMinutes` | `int` (required) | Abruf-Intervall |
| `Theme` | `string?` | `"system"`/`"light"`/`"dark"`, Default `SettingsValues.ThemeSystem` |
| `NotificationSummaryEnabled` | `bool` (required) | Sammel-Benachrichtigung pro Feed |
| `Language` | `string?` | `"system"`/`"de"`/`"en"`, Default `SettingsValues.LanguageSystem` (R5-Kontext) |

**Fehlt für die Anforderung:** `RefreshOnStartupEnabled` (R3), Sortierrichtung `UnreadSortOrder`/`SortOrder` (R4), `DebugInfoEnabled` o. ä. (R7).

## `Settings` (EF-Core-Entity)
Datei: `src/Reporter.Data/Entities/Settings.cs`

Spiegelt das Domänenmodell 1:1 (set-Accessor statt init, Defaults direkt an den Properties: `RetentionDays = 30`, `AutoMarkReadMode = AutoMarkReadOnScroll`, `AutoMarkReadDelaySeconds = 5`, `NotificationsEnabled = true`, `AutoRefreshEnabled = true`, `RefreshIntervalMinutes = 30`, `Theme = ThemeSystem`, `NotificationSummaryEnabled = false`, `Language = LanguageSystem`). Spalten-Mapping in `ReporterDbContext.ConfigureSettings` (`src/Reporter.Data/ReporterDbContext.cs`, Zeilen 123–141), Tabelle `settings`, Seed via `entity.HasData(new Settings())`.

Bestehende Migrationen (`src/Reporter.Data/Migrations/`): `InitialCreate`, `AddSettingsAutoRefreshAndTheme`, `AddFeedNotificationsEnabled`, `AddSettingsNotificationSummary`, `AddSettingsLanguage`. Neue Eigenschaften benötigen jeweils eine eigene Migration.

## `Feed` (Domänenmodell)
Datei: `src/Reporter.Core/Models/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required) | Feed-ID |
| `Url` | `string` (required) | Feed-URL (eindeutig) |
| `Title` | `string` (required) | Anzeigetitel |
| `CategoryId` | `Guid?` | Kategorie-Zuordnung |
| `LastCheckedAt` | `DateTime?` | Letzter Abruf |
| `HealthStatus` | `string?` | `FeedHealth.Ok`/`Warning`/`Error` |
| `HealthLastChange` | `DateTime?` | Zeitpunkt der letzten Statusänderung |
| `NotificationsEnabled` | `bool` (required) | Feed-spezifischer Benachrichtigungs-Schalter |

**Fehlt für R2:** keinerlei Bild-/Favicon-Eigenschaft (`FaviconUrl`/`ImageUrl` o. ä.).

## `Feed` (EF-Core-Entity)
Datei: `src/Reporter.Data/Entities/Feed.cs`

Gleiche Felder wie das Domänenmodell plus Navigation `Category` (`Category?`). Tabelle `feeds`, Mapping in `ReporterDbContext.ConfigureFeed` (Zeilen 78–93): `url` (max. 2048, required, unique Index), `title` (max. 500, required), `notifications_enabled` (Default `true`). Keine Favicon-/Bild-Spalte.

## `ItemListItem`
Datei: `src/Reporter.Core/Models/ItemListItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id`, `FeedId`, `Title`, `IsRead`, `IsSavedForLater` | `Guid`/`string`/`bool` (required, init) | Basisdaten der Karte |
| `Link` | `string?` | Original-Artikel-URL |
| `PublishedAt` | `DateTime?` | Veröffentlichungsdatum (Sortierschlüssel, R4) |
| `FeedTitle` | `string` | Feed-Anzeigetitel (für Initialen-Fallback R2 relevant) |
| `CategoryId` / `CategoryName` | `Guid?`/`string?` | Kategorie-Badge |
| `ImageUrl` | `string?` | Aus `ContentHtml` extrahiertes erstes `<img>` (`ItemRepository.ExtractImageUrl`); `null` bei bildlosem Inhalt (R1/R2-Anker) |
| `Summary` | `string?` | Plain-Text-Auszug (max. 120 Zeichen) |
| `ReadingTimeText` | `string?` | Formatierter Lesezeit-Text via `ReadingTimeEstimator.EstimateText` (R1-Anker) |
| `CopyWith(isRead, isSavedForLater)` | Methode | Unveränderliche Kopie; neue Felder müssen hier mitgezogen werden (R2) |

**Fehlt für R2:** kein Feld für Feed-Favicon/Ersatzbild (z. B. `FeedImageUrl`).

## `FeedListItem`
Datei: `src/Reporter.Core/Models/FeedListItem.cs`

`Id`, `Title`, `Url` (required), `CategoryId`, `CategoryName`, `LastCheckedAt`, `HealthStatus`, `HealthLastChange`, `UnreadCount` (required), `NotificationsEnabled` (required). Kein Bild-/Icon-Feld (relevant, falls das Standardbild auch auf der Feeds-Seite erscheinen soll — offene Frage in R2).

## `FeedSearchResult`
Datei: `src/Reporter.Core/Models/FeedSearchResult.cs`

Reines In-Memory-Modell: `Title?`, `Description?`, `SiteName?`, `SiteUrl?`, `FeedUrl` (required), `Score` (`double`), `MatchKind` (`FeedSearchMatchKind`), `DisplayTitle` (computed). `SiteUrl` ist die vorhandene Quelle für die „Webseite des Feeds" bei der Favicon-Suche (R2); wird aktuell **nicht** persistiert — `SubscribeResultAsync` übergibt nur `FeedUrl` + Titel an `TryPersistNewFeedAsync`.

## `SyncLog` (Domänenmodell + Entity)
Dateien: `src/Reporter.Core/Models/SyncLog.cs`, `src/Reporter.Data/Entities/SyncLog.cs`

`Id` (required), `FeedId?`, `StartedAt?`, `FinishedAt?`, `Status?`, `Message?`. Tabelle `sync_logs` (`ReporterDbContext.ConfigureSyncLog`, Zeilen 143–155). Einzige persistierte Protokollquelle — relevant als Datenquelle für das Debug-Paket (R7).

## `SettingsValues` (Konstanten)
Datei: `src/Reporter.Core/Models/SettingsValues.cs`

`AutoMarkReadOnOpen`/`OnScroll`/`Off`, `ThemeSystem`/`ThemeLight`/`ThemeDark`, `LanguageSystem`/`LanguageGerman`/`LanguageEnglish`, Hilfsmethode `IsAutoMarkReadEnabled(string?)`. **Fehlt:** Konstanten für die Sortierrichtung (R4, z. B. `"desc"`/`"asc"`).

## Optionsklassen (Picker-Muster, R4-Vorbild)
Dateien: `src/Reporter.Core/ViewModels/RefreshIntervalOption.cs` (`Minutes` + `Label`), `LanguageOption.cs` (`Value` + `Label`), `ThemeOption.cs`, `AutoMarkReadDelayOption.cs` (`Seconds` + `Label`). Eine neue `SortOrderOption` ist noch nicht vorhanden.
