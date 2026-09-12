# Datenmodell — Bestandsaufnahme

Betroffene Datenmodellklassen für die Anforderung „Offline-Fähigkeit und Mehrsprachigkeit" (Issue #28).

## `Settings` (Domänenmodell)

Datei: `src/Reporter.Core/Models/Settings.cs`

Singleton-Einstellungsdatensatz (`Settings.DefaultId` = `a1f5c6d2-4b3e-4c8f-9d2a-1b2c3d4e5f6a`).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige ID des Datensatzes |
| `RetentionDays` | `int` (required, init) | Aufbewahrungsdauer in Tagen |
| `AutoMarkReadMode` | `string?` (init) | Auto-Gelesen-Modus (`on_open`/`on_scroll`/`off`) |
| `AutoMarkReadDelaySeconds` | `int` (required, init) | Verzögerung bis zur Auto-Markierung |
| `NotificationsEnabled` | `bool` (required, init) | Benachrichtigungen aktiv |
| `QuietHoursStart` | `TimeSpan?` (init) | Beginn der Ruhezeit |
| `QuietHoursEnd` | `TimeSpan?` (init) | Ende der Ruhezeit |
| `AutoRefreshEnabled` | `bool` (required, init) | Hintergrund-Aktualisierung aktiv |
| `RefreshIntervalMinutes` | `int` (required, init) | Abruf-Intervall in Minuten |
| `Theme` | `string?` (init, Default `SettingsValues.ThemeSystem`) | Erscheinungsbild (`system`/`light`/`dark`) |
| `NotificationSummaryEnabled` | `bool` (required, init) | Sammel-Benachrichtigung pro Feed |

**Für die Anforderung relevant:** Es gibt **kein** `Language`-Feld. Ein optionaler manueller Sprachwechsel würde eine neue Eigenschaft plus EF-Core-Migration erfordern.

## `Settings` (EF-Core-Entität)

Datei: `src/Reporter.Data/Entities/Settings.cs`

Persistenz-Pendant zur `settings`-Tabelle (Konfiguration in `src/Reporter.Data/ReporterDbContext.cs`, `ConfigureSettings`, Zeilen 121–138; `entity.HasData(new Settings())` seedet den Singleton-Datensatz). Enthält dieselben Felder wie das Domänenmodell — ebenfalls **kein** `Language`-Feld. Mapping über `SettingsRepository.MapToModel`/`SaveAsync` (`src/Reporter.Data/Repositories/SettingsRepository.cs`, Zeilen 43–84).

## `SettingsValues`

Datei: `src/Reporter.Core/Models/SettingsValues.cs`

Zentrale Konstanten für persistierte Einstellungswerte.

| Konstante | Wert | Beschreibung |
|-----------|------|--------------|
| `AutoMarkReadOnOpen` | `"on_open"` | Auto-Gelesen beim Öffnen |
| `AutoMarkReadOnScroll` | `"on_scroll"` | Legacy-Seed-Default |
| `AutoMarkReadOff` | `"off"` | Auto-Gelesen deaktiviert |
| `ThemeSystem` | `"system"` | Theme folgt dem OS |
| `ThemeLight` | `"light"` | Helles Theme |
| `ThemeDark` | `"dark"` | Dunkles Theme |

Methode: `IsAutoMarkReadEnabled(string? autoMarkReadMode)` — `true` für jeden Modus außer `AutoMarkReadOff`.

**Für die Anforderung relevant:** Es existieren **keine** `LanguageSystem`/`LanguageGerman`/`LanguageEnglish`-Konstanten.

## `Item`

Datei: `src/Reporter.Core/Models/Item.cs`

Artikel-Domänenmodell. Für die Offline-Fähigkeit relevant: `ContentHtml` (`string?`, Zeile 56, „HTML content for offline reading") und `Link` (`string?`, Zeile 26) — Artikel-Inhalte liegen bereits lokal vor.

Weitere Eigenschaften: `Id`, `FeedId`, `Title`, `PublishedAt`, `GuidOrHash`, `IsRead`, `IsSavedForLater`, `ReadAt` (alle `init`-only).

## `Feed`

Datei: `src/Reporter.Core/Models/Feed.cs`

Feed-Domänenmodell: `Id`, `Url`, `Title`, `CategoryId`, `LastCheckedAt`, `HealthStatus` (`string?`, Werte aus `FeedHealth`), `HealthLastChange`, `NotificationsEnabled`.

## `SyncLog`

Datei: `src/Reporter.Core/Models/SyncLog.cs`

Sync-Protokolleintrag: `Id`, `FeedId`, `StartedAt`, `FinishedAt`, `Status` (`string?`, Werte aus `FeedHealth`), `Message` (`string?`).

**Für die Anforderung relevant:** `Message` wird von `FeedSyncService` mit englisch hartcodierten Meldungen befüllt (z. B. `"Synchronization failed: {ex.Message}"`, `FeedSyncService.cs` Zeile 70) und in der Datenbank persistiert (Tabelle `sync_logs`, Spalte `message`).

## `ItemListItem`

Datei: `src/Reporter.Core/Models/ItemListItem.cs`

Anzeigemodell für Artikel-Listen (Ungelesen/Später): `Id`, `FeedId`, `Title`, `Link`, `PublishedAt`, `IsRead`, `IsSavedForLater`, `FeedTitle`, `CategoryId`, `CategoryName`, `ImageUrl`, `Summary`. Kein Offline-/Status-Feld.

## `FeedListItem`

Datei: `src/Reporter.Core/Models/FeedListItem.cs`

Anzeigemodell für Feeds: `Id`, `Title`, `Url`, `CategoryId`, `CategoryName`, `LastCheckedAt`, `HealthStatus`, `HealthLastChange`, `UnreadCount`, `NotificationsEnabled`.

## `CategoryFilterItem`

Datei: `src/Reporter.Core/Models/CategoryFilterItem.cs`

Filter-Chip für die Ungelesen-Seite (`ObservableObject`): `CategoryId` (`null` = „Alle"), `Name`, `Count` (`[ObservableProperty]`), `IsSelected` (`[ObservableProperty]`).

## `ThemeOption`

Datei: `src/Reporter.Core/ViewModels/ThemeOption.cs`

Auswahloption für den Theme-Picker (`required string Value`, `required string Label`). Dient in der Anforderung als Muster für ein optionales `LanguageOption`.
