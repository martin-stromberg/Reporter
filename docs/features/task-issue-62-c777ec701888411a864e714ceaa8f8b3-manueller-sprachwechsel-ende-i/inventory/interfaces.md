<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces

## `ISettingsRepository`

Datei: `src/Reporter.Core/Interfaces/ISettingsRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetAsync` | `CancellationToken cancellationToken = default` | `Task<Settings>` | Lädt den Singleton-`Settings`-Datensatz (legt ihn bei Fehlen an) |
| `SaveAsync` | `Settings settings` | `Task` | Speichert die übergebenen Settings, aktualisiert immer den Singleton-Datensatz |

Implementiert von `SettingsRepository` (`src/Reporter.Data/Repositories/SettingsRepository.cs`); konsumiert von `SettingsViewModel`, `App.OnStart` und `TestSettingsHelper`. Das Interface ist feldagnostisch — ein neues `Settings.Language`-Feld erfordert **keine** Interface-Änderung.

## `IAppThemeService`

Datei: `src/Reporter.Core/Interfaces/IAppThemeService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ApplyTheme` | `string? theme` | `void` | Wendet den persistierten Theme-Wert (`"system"`/`"light"`/`"dark"`) auf die laufende App an; unbekannte Werte fallen auf System zurück |

Implementiert von `AppThemeService` (`src/Reporter/Services/AppThemeService.cs`) und `FakeAppThemeService` (`src/Reporter.Tests/FakeAppThemeService.cs`, zeichnet Aufrufe in `AppliedThemes` auf). Aufgerufen von `SettingsViewModel.PersistAsync` und `App.OnStart`. Ein sprachbezogenes Gegenstück (z. B. `ILanguageService`) existiert **nicht**.

Weitere vom `SettingsViewModel` genutzte Interfaces (nur als Konstruktor-Abhängigkeiten relevant, keine Änderung nötig): `IKeywordRepository`, `IAutoRefreshService`, `ILocalNotificationService` (alle unter `src/Reporter.Core/Interfaces/`).
