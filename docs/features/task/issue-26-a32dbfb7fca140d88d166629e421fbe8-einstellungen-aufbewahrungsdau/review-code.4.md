# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### `ArticleDetailViewModel.cs` (ArticleDetailViewModel)

- **Hardcodierte Werte / inkonsistente Lokalisierung** — `AutoMarkReadLabel` mischt in derselben Zuweisung einen hartcodierten deutschen Format-String mit einem lokalisierten Ressourcen-Key: `AutoMarkReadLabel = IsAutoMarkReadAvailable ? $"Auto-Gelesen ({_autoMarkReadDelaySeconds} s)" : AppResources.ArticleAutoMarkReadDisabled;` (Zeilen 266–268; ebenso der Feldinitialisierer `"Auto-Gelesen (5 s)"` in Zeile 43). Da `ArticleAutoMarkReadDisabled` in dieser Runde für Deutsch **und** Englisch angelegt wurde, zeigt dasselbe UI-Element je nach Zustand unterschiedliche Sprachen: deaktiviert → „Auto-read (disabled in settings)" (englisch unter EN-Locale), aktiviert → „Auto-Gelesen (5 s)" (immer deutsch). Schweregrad: niedrig.

  Empfehlung: Ressourcen-Key für den aktivierten Zustand ergänzen (z. B. `ArticleAutoMarkReadDelayFormat` = „Auto-Gelesen ({0} s)" / „Auto-read ({0} s)" in beiden resx-Dateien + Designer) und `AutoMarkReadLabel` über `string.Format(CultureInfo.CurrentCulture, ...)` setzen; Feldinitialisierer ebenfalls auf den Key umstellen.

## Geprüfte Schwerpunkte dieser Runde (ohne Befund)

- **`SettingsValues.IsAutoMarkReadEnabled`** (`src/Reporter.Core/Models/SettingsValues.cs`, Zeilen 43–46): Semantik deckt sich exakt mit dem bisherigen `!= "off"`-Gate — `null`, `"on_open"`, `"on_scroll"` und unbekannte Legacy-Werte → `true`, nur `"off"` → `false`. Alle Produktiv-Stellen (`SettingsViewModel.LoadAsync` Zeile 371, `ArticleDetailViewModel` Zeilen 254/262) nutzen die Konstanten/Methode; verbleibende Literale nur in Migrations/Snapshot (korrekt historisch), Tests und Doku.
- **`QuietHoursEnabled`-Session-Retention** (`SettingsViewModel.cs`, Zeilen 270–289, 375–377, 429–430): Setter füllt beim Aktivieren via `??=` Defaults (22:00/07:00) ohne Session-Werte zu überschreiben; beim Deaktivieren bleiben `_quietHoursStart`/`_quietHoursEnd` erhalten, `PersistAsync` schreibt `null`. `LoadAsync`-`??`-Reihenfolge korrekt: Zeiten werden vor `QuietHoursEnabled` gesetzt, sodass `??=` partielle Persistenzstände (nur Start oder nur End) mit Session-/Default-Werten auffüllt; `_isLoading` unterdrückt Persistierung während des Ladens. `_persistLock`-Interaktion sauber: Persist-Snapshot wird innerhalb des Locks gegen den zuletzt gespeicherten Stand verglichen; Feld-Mutationen via `??=` erfolgen vor `PersistOnChange()`. Edge Cases (Toggle off→on stellt Session-Werte wieder her, Reload bei ausgeschalteter Ruhezeit behält Session-Werte) durch neue Tests abgedeckt.
- **resx-Konsistenz**: `ArticleAutoMarkReadDisabled` und `SettingsKeywordRemoveFormat` in `AppResources.resx`, `AppResources.de.resx` und `AppResources.Designer.cs` vorhanden; vollständige Key-Parität DE/EN verifiziert (diff der `data name`-Listen leer); `{0}`-Platzhalter in beiden Sprachen vorhanden, `StringFormat`-Verwendung in `SettingsPage.xaml` (Zeile 92) korrekt.
- **`ArticleDetailPage.xaml`** (Zeilen 37–50): `IsEnabled`-Bindung auf `IsAutoMarkReadAvailable`, Opacity-Trigger 0.4 konsistent mit dem Disabled-Pattern der Settings-Seite, `SemanticProperties.Description` an `AutoMarkReadLabel` gebunden.
- **Testqualität**: `SettingsValuesTests_AutoMarkRead` (Theory, ein fachlicher Fall pro InlineData, AAA), neue QuietHours-Tests (`QuietHoursEnabled_TurnedOff_PersistsNull`, `QuietHoursEnabled_ToggledOffAndOn_RestoresSessionValues`, `QuietHoursEnabled_TurnedOff_ReloadKeepsSessionValues`, `QuietHoursEnabled_TurnedOn_AppliesDefaults`, `QuietHoursEnabled_TurnedOn_KeepsExistingValues`) prüfen jeweils genau einen Fall mit `WaitUntilAsync` gegen das echte Repository; E2E-Anpassung (`QuietHoursEnabled = true` vor dem Setzen der Zeiten) korrekt.
- **Toter Code**: `_autoMarkReadMode` vollständig entfernt; `SettingsValues.AutoMarkReadOnScroll` weiterhin als Entity-Default referenziert (`src/Reporter.Data/Entities/Settings.cs`, Zeile 29). Hinweis außerhalb des Branch-Umfangs: `ToggleAutoMarkReadCommand` ist weiterhin ungenutzt, stammt aber aus einem älteren Feature (Commit 6be93a1, Vorfahre von `origin/staging`).
- **Zusatz-Prüfregel (UI-Aktions-Events)**: Keine `RaiseUiActionRequested`-artigen Events in der Codebasis vorhanden — nicht anwendbar.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Reporter.Core/Models/Settings.cs`
- `src/Reporter.Core/Models/SettingsValues.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Services/AutoRefreshService.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
- `src/Reporter.Data/Entities/Settings.cs`
- `src/Reporter/Services/AppThemeService.cs`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter/Views/ArticleDetailPage.xaml`
- `src/Reporter/Views/SettingsPage.xaml`
- `src/Reporter.Tests/AutoRefreshServiceTests.cs`
- `src/Reporter.Tests/SettingsValuesTests_AutoMarkRead.cs`
- `src/Reporter.Tests/SettingsViewModelTests_E2E.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Load.cs`
- `src/Reporter.Tests/SettingsViewModelTests_Persist.cs`
- `src/Reporter.Tests/TestWaitHelper.cs`
