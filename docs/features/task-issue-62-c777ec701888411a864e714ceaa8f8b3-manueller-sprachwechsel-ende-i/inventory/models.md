<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell und Persistenzschema

## `Settings` (Entity)

Datei: `src/Reporter.Data/Entities/Settings.cs`

Singleton-Einstellungsdatensatz (Tabelle `settings`). Eine Property `Language` existiert **nicht**.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `DefaultId` (statisch, readonly) | `Guid` | Singleton-ID `a1f5c6d2-4b3e-4c8f-9d2a-1b2c3d4e5f6a` (Zeile 16) |
| `Id` | `Guid` | Primärschlüssel, Default `DefaultId` |
| `RetentionDays` | `int` | Aufbewahrungsdauer in Tagen, Default `30` |
| `AutoMarkReadMode` | `string?` | Auto-als-gelesen-Modus, Default `SettingsValues.AutoMarkReadOnScroll` |
| `AutoMarkReadDelaySeconds` | `int` | Verzögerung bis zum Markieren, Default `5` |
| `NotificationsEnabled` | `bool` | Benachrichtigungen aktiv, Default `true` |
| `QuietHoursStart` | `TimeSpan?` | Beginn der Ruhezeiten |
| `QuietHoursEnd` | `TimeSpan?` | Ende der Ruhezeiten |
| `AutoRefreshEnabled` | `bool` | Hintergrund-Aktualisierung aktiv, Default `true` |
| `RefreshIntervalMinutes` | `int` | Aktualisierungsintervall in Minuten, Default `30` |
| `Theme` | `string?` | Erscheinungsbild (`"system"`/`"light"`/`"dark"`), Default `SettingsValues.ThemeSystem` — Vorbildmuster für `Language` |
| `NotificationSummaryEnabled` | `bool` | Sammelbenachrichtigung pro Feed, Default `false` |

## `Settings` (Domain-Modell)

Datei: `src/Reporter.Core/Models/Settings.cs`

Immutable Variante (`init`-only, Pflichtfelder `required`). Eine Property `Language` existiert **nicht**.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `DefaultId` (statisch, readonly) | `Guid` | Singleton-ID `a1f5c6d2-4b3e-4c8f-9d2a-1b2c3d4e5f6a` |
| `Id` | `Guid` (required, init) | Primärschlüssel |
| `RetentionDays` | `int` (required, init) | Aufbewahrungsdauer |
| `AutoMarkReadMode` | `string?` (init) | Auto-als-gelesen-Modus |
| `AutoMarkReadDelaySeconds` | `int` (required, init) | Markier-Verzögerung |
| `NotificationsEnabled` | `bool` (required, init) | Benachrichtigungen aktiv |
| `QuietHoursStart` / `QuietHoursEnd` | `TimeSpan?` (init) | Ruhezeiten |
| `AutoRefreshEnabled` | `bool` (required, init) | Hintergrund-Aktualisierung |
| `RefreshIntervalMinutes` | `int` (required, init) | Aktualisierungsintervall |
| `Theme` | `string?` (init), Default `SettingsValues.ThemeSystem` | Erscheinungsbild — Vorbildmuster für `Language` |
| `NotificationSummaryEnabled` | `bool` (required, init) | Sammelbenachrichtigung |

Querverweise: Wird erzeugt in `SettingsRepository.MapToModel` (`src/Reporter.Data/Repositories/SettingsRepository.cs` Zeilen 69–85) und feldweise neu aufgebaut in `SettingsViewModel.PersistAsync` (`src/Reporter.Core/ViewModels/SettingsViewModel.cs` Zeilen 645–658).

## `SettingsValues` (Wertekonstanten, enum-äquivalent)

Datei: `src/Reporter.Core/Models/SettingsValues.cs`

Statische Klasse mit den persistierten String-Werten. `Language*`-Konstanten existieren **nicht**.

| Konstante | Wert | Bedeutung |
|-----------|------|-----------|
| `AutoMarkReadOnOpen` | `"on_open"` | Beim Öffnen als gelesen markieren |
| `AutoMarkReadOnScroll` | `"on_scroll"` | Beim Scrollen markieren (Legacy-Seed-Default) |
| `AutoMarkReadOff` | `"off"` | Automatik deaktiviert |
| `ThemeSystem` | `"system"` | Erscheinungsbild folgt dem System |
| `ThemeLight` | `"light"` | Helles Erscheinungsbild |
| `ThemeDark` | `"dark"` | Dunkles Erscheinungsbild |

Methode: `IsAutoMarkReadEnabled(string? autoMarkReadMode)` → `bool` (`true` für jeden Wert außer `AutoMarkReadOff`).

## `ReporterDbContext.ConfigureSettings`

Datei: `src/Reporter.Data/ReporterDbContext.cs` (Zeilen 123–140)

Mapping der Entity auf die Tabelle `settings` mit Snake-Case-Spalten. Relevante Konventionen:

- `Id`: Spalte `id`, `ValueGeneratedNever()`
- `AutoMarkReadMode`: Spalte `auto_mark_read_mode`, `HasMaxLength(50)`, nullable
- `Theme`: Spalte `theme`, `HasMaxLength(50)`, nullable (Zeile 136) — Vorbild für eine `language`-Spalte
- `AutoRefreshEnabled`/`NotificationSummaryEnabled`: `IsRequired().HasDefaultValue(...)`
- `entity.HasData(new Settings())` (Zeile 139): Seed des Singleton-Datensatzes aus den Property-Defaults
- Kein `language`-Mapping im ModelSnapshot (`ReporterDbContextModelSnapshot.cs`, Settings-Block ab Zeile ~232)

## Migrationen (`src/Reporter.Data/Migrations/`)

| Migration | Inhalt |
|-----------|--------|
| `20260909214617_InitialCreate` | Erstes Schema inkl. `settings`-Tabelle und Seed |
| `20260911080630_AddSettingsAutoRefreshAndTheme` | `AddColumn` für `auto_refresh_enabled` (INTEGER, Default true), `refresh_interval_minutes` (INTEGER, Default 30), `theme` (TEXT, maxLength 50, nullable) plus `UpdateData` auf den Singleton-Datensatz (Zeilen 37–42) — direktes Muster für `AddSettingsLanguage` |
| `20260911181811_AddFeedNotificationsEnabled` | `notifications_enabled` auf `feeds` |
| `20260911181850_AddSettingsNotificationSummary` | `notification_summary_enabled` auf `settings` |

Eine Migration `AddSettingsLanguage` existiert **nicht**.

## Optionsklassen (Picker-ViewModel-Datenhalter)

Namespace `Reporter.Core.ViewModels` — einfache `required`-`init`-Records ohne Logik:

| Klasse | Datei | Properties |
|--------|-------|------------|
| `ThemeOption` | `src/Reporter.Core/ViewModels/ThemeOption.cs` | `Value` (`string`, persistierter Wert), `Label` (`string`, lokalisiert) — Vorbild für `LanguageOption` |
| `RefreshIntervalOption` | `src/Reporter.Core/ViewModels/RefreshIntervalOption.cs` | `Minutes` (`int`), `Label` (`string`) |
| `AutoMarkReadDelayOption` | `src/Reporter.Core/ViewModels/AutoMarkReadDelayOption.cs` | `Seconds` (`int`), `Label` (`string`) |

Eine Klasse `LanguageOption` existiert **nicht** (Verzeichnis `src/Reporter.Core/ViewModels/` enthält kein `LanguageOption.cs`).
