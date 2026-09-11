# Datenmodell — Bestandsaufnahme

## `Settings` (Entity)

Datei: `src/Reporter.Data/Entities/Settings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `DefaultId` | `static readonly Guid` | Singleton-ID `a1f5c6d2-4b3e-4c8f-9d2a-1b2c3d4e5f6a` (Zeile 11) |
| `Id` | `Guid` | Primärschlüssel, Standard `DefaultId` (Zeile 16) |
| `RetentionDays` | `int` | Aufbewahrungsdauer in Tagen, Standard `30` (Zeile 21) |
| `AutoMarkReadMode` | `string?` | Auto-Gelesen-Modus, Standard `"on_scroll"` (Zeile 26) |
| `AutoMarkReadDelaySeconds` | `int` | Verzögerung bis Auto-Gelesen, Standard `5` (Zeile 31) |
| `NotificationsEnabled` | `bool` | Benachrichtigungen ein/aus, Standard `true` (Zeile 36) |
| `QuietHoursStart` | `TimeSpan?` | Ruhezeit-Beginn, Standard `null` (Zeile 41) |
| `QuietHoursEnd` | `TimeSpan?` | Ruhezeit-Ende, Standard `null` (Zeile 46) |

Mutable POCO-Entität (`get; set;`). Die von der Anforderung geforderten Felder `AutoRefreshEnabled`, `RefreshIntervalMinutes`, `Theme` (bzw. `AutoMarkReadOnOpenEnabled`) existieren **nicht**.

## `Settings` (Core-Modell)

Datei: `src/Reporter.Core/Models/Settings.cs`

Gleiche Eigenschaften wie die Entität, aber als **immutable** Modellklasse: `Id`, `RetentionDays`, `AutoMarkReadDelaySeconds` und `NotificationsEnabled` sind `required init`-Pflichtfelder; `AutoMarkReadMode`, `QuietHoursStart`, `QuietHoursEnd` sind optionale `init`-Properties. `DefaultId` ist identisch definiert (Zeile 11). Neue `required`-Felder betreffen alle Konstruktionsstellen (u. a. `RetentionCleanupServiceTests.SetRetentionDaysAsync` Zeilen 39–48, `SettingsRepositoryTests` Zeilen 71–77/93–99, `ArticleDetailViewModel`-Fallback Zeilen 238–246).

## `Keyword` (Entity / Core-Modell)

Dateien: `src/Reporter.Data/Entities/Keyword.cs`, `src/Reporter.Core/Models/Keyword.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel (Entity: `set`, Core: `required init`) |
| `KeywordText` | `string` | Keyword-Text (Entity: `set`, Default `""`; Core: `required init`) |

## `Item` (Entity / Core-Modell)

Dateien: `src/Reporter.Data/Entities/Item.cs`, `src\Reporter.Core\Models\Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel |
| `FeedId` | `Guid` | FK auf `Feed`, `DeleteBehavior.Cascade` |
| `Title` | `string` | Titel, max. 500, required |
| `Link` | `string?` | Original-Link, max. 2048 |
| `PublishedAt` | `DateTime?` | Veröffentlichungszeitpunkt (Fallback-Zeitstempel für Retention) |
| `GuidOrHash` | `string` | Original-GUID/Hash, Unique-Index zusammen mit `FeedId` |
| `IsRead` | `bool` | Gelesen-Flag — Teil der Lösch-Invariante (`DeleteExpiredAsync` löscht nur `IsRead == true`) |
| `IsSavedForLater` | `bool` | „Für später"-Flag — wird niemals automatisch gelöscht |
| `ReadAt` | `DateTime?` | Gelesen-Zeitpunkt — primärer Retention-Zeitstempel |
| `ContentHtml` | `string?` | HTML-Inhalt (potenzielles Keyword-Match-Feld) |
| `Feed` | `Feed` | Navigation (nur Entity) |

Ein Keyword-Flag wie `IsKeywordFiltered` existiert **nicht**.

## DbContext-Konfiguration

Datei: `src/Reporter.Data/ReporterDbContext.cs`

- `DbSet`s: `Feeds`, `Categories`, `Items`, `Keywords`, `Settings`, `SyncLogs` (Zeilen 24–49).
- `ConfigureSettings` (Zeilen 120–133): Tabelle `settings`, Spalten `id` (`ValueGeneratedNever`), `retention_days` (required), `auto_mark_read_mode` (max. 50, nullable), `auto_mark_read_delay_seconds` (required), `notifications_enabled` (required), `quiet_hours_start`/`quiet_hours_end` (nullable). `entity.HasData(new Settings())` seedet den Singleton-Datensatz.
- `ConfigureKeyword` (Zeilen 111–118): Tabelle `keywords`, `keyword_text` max. 500 required, **Unique-Index** auf `keyword_text`.
- `ConfigureItem` (Zeilen 92–109): Tabelle `items`, Unique-Index `{FeedId, GuidOrHash}`, FK `Feed` mit Cascade.

## Migrationsstand

Datei: `src/Reporter.Data/Migrations/20260909214617_InitialCreate.cs` (+ `.Designer.cs`, `ReporterDbContextModelSnapshot.cs`)

Einzige vorhandene Migration. `settings`-Tabelle enthält genau die oben genannten sechs Fachspalten + `id` (Zeilen 39–52); Seed-Datensatz in Zeilen 127–128. Für neue `settings`-Spalten ist eine neue Migration erforderlich. `App.OnStart` führt `context.Database.MigrateAsync()` aus (`src/Reporter/App.xaml.cs` Zeile 38); `ReporterDbContextFactory` (`src/Reporter.Data/ReporterDbContextFactory.cs`) dient als Design-Time-Factory.

## Enum-Konvention

Es existieren **keine Enums** im Projekt. Statuswerte werden als String-Konstanten gehalten: `FeedHealth` (`src/Reporter.Core/Services/FeedHealth.cs`) mit `Ok = "OK"`, `Warning = "Warning"`, `Error = "Error"` plus Helper `Changed(current, next)`. `AutoMarkReadMode` folgt derselben Konvention (`"on_scroll"` als Standard).
