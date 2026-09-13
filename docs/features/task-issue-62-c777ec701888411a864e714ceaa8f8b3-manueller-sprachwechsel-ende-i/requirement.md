<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: Manueller Sprachwechsel (EN/DE) in den Einstellungen (#62)

## Fachliche Zusammenfassung

Die App lokalisiert seit Issue #28 alle sichtbaren Texte über `AppResources` (`src/Reporter.Core/Resources/Strings/AppResources.resx` + `AppResources.de.resx`) und folgt dabei der `CultureInfo.CurrentUICulture`, d. h. der Systemsprache (Fallback: Englisch). Neu soll der Anwender die Sprache unabhängig vom System in den Einstellungen wählen können: **System** (bisheriges Verhalten), **Deutsch** oder **English**. Die Auswahl wird als weiteres Feld der Singleton-`Settings`-Entität persistiert (analog zu `Settings.Theme`) und beim App-Start auf `CultureInfo.CurrentUICulture` angewendet. Da sämtliche XAML-Texte über `{x:Static strings:AppResources.*}` gebunden sind und die ViewModels (`SettingsViewModel` u. a.) als Singletons laufen, erfolgt keine Laufzeit-Umschaltung — stattdessen ist nach dem Wechsel ein App-Neustart erforderlich, auf den in der UI hingewiesen wird.

## Betroffene Klassen und Komponenten

**Datenmodell / Persistenz (`Reporter.Data`, `Reporter.Core`):**

- `Reporter.Data.Entities.Settings` (`src/Reporter.Data/Entities/Settings.cs`): neue Property `Language` (`string?`, Default `"system"` — analog `Theme`).
- `Reporter.Core.Models.Settings` (`src/Reporter.Core/Models/Settings.cs`): neue Property `Language` (`string?`).
- `Reporter.Core.Models.SettingsValues` (`src/Reporter.Core/Models/SettingsValues.cs`): neue Konstanten für die persistierten Werte, voraussichtlich `LanguageSystem = "system"`, `LanguageGerman = "de"`, `LanguageEnglish = "en"` (Konvention wie `ThemeSystem`/`ThemeLight`/`ThemeDark`).
- `Reporter.Data.ReporterDbContext` (`src/Reporter.Data/ReporterDbContext.cs`, `ConfigureSettings`): neue Spalte `language` (`TEXT`, `HasMaxLength`, analog `theme`); Seed-Update via `HasData`.
- Neue EF-Core-Migration `AddSettingsLanguage` (`src/Reporter.Data/Migrations/`): `AddColumn<string>` auf Tabelle `settings` plus `UpdateData` für den Singleton-Datensatz — Muster: `20260911080630_AddSettingsAutoRefreshAndTheme.cs`.
- `Reporter.Data.Repositories.SettingsRepository` (`src/Reporter.Data/Repositories/SettingsRepository.cs`): `Language` in `SaveAsync` und `MapToModel` ergänzen.

**ViewModel / UI (`Reporter.Core`, `Reporter`):**

- Neue Klasse `LanguageOption` (`src/Reporter.Core/ViewModels/LanguageOption.cs`): `Value` + `Label`, analog `ThemeOption`.
- `SettingsViewModel` (`src/Reporter.Core/ViewModels/SettingsViewModel.cs`): neue `IReadOnlyList<LanguageOption> LanguageOptions` (Labels aus `AppResources`), neue bindbare Property `SelectedLanguage` mit `PersistOnChange()`-Muster; `LoadAsync` mappt `settings.Language` auf die Option (Fallback `system`); `PersistAsync` schreibt `Language` in das neue `Settings`-Objekt.
- `SettingsPage` (`src/Reporter/Views/SettingsPage.xaml`): neuer `Picker` (`ItemsSource="{Binding LanguageOptions}"`, `SelectedItem="{Binding SelectedLanguage}"`, `ItemDisplayBinding="{Binding Label}"`) — voraussichtlich als eigene Sektion „Sprache" oder in der Karte „Erscheinungsbild"; dazu ein Hinweistext, dass ein App-Neustart erforderlich ist (Muster: vorhandene Info-`Border` wie `SettingsRetentionInfo`).
- `AppResources.resx` / `AppResources.de.resx` + regeneriertes `AppResources.Designer.cs`: neue Schlüssel, z. B. `SettingsSectionLanguage`/`SettingsLanguageLabel`, `SettingsLanguageSystem`, `SettingsLanguageGerman`, `SettingsLanguageEnglish`, `SettingsLanguageRestartHint`.

**App-Start (`Reporter`):**

- `App.xaml.cs` (`OnStart`) bzw. `MauiProgram.CreateMauiApp`: `CultureInfo.CurrentUICulture` aus `settings.Language` setzen. Alternativ `AppResources.Culture` (statische Property, existiert bereits im Designer) — die Anforderung nennt explizit `CurrentUICulture`.

**Tests (`Reporter.Tests`):**

- `SettingsViewModelTests_Load` / `SettingsViewModelTests_Persist`: Laden und Sofort-Persistieren der Sprachauswahl; insbesondere dass `PersistAsync` `Language` nicht verliert, wenn andere Einstellungen geändert werden.
- `SettingsRepositoryTests`: Roundtrip der neuen Spalte.
- `TestSettingsHelper` (`src/Reporter.Tests/TestSettingsHelper.cs`): ggf. um optionalen `language`-Parameter erweitern.

**Dokumentation (Folgeschritt):**

- `docs/help/anwendung/sprache.md` beschreibt aktuell explizit „Es gibt keine Sprach-Auswahl in den Einstellungen" — muss im Doku-Schritt angepasst werden.

## Implementierungsansatz

Das Feature folgt eins-zu-eins dem etablierten `Theme`-Muster (Issue: Erscheinungsbild-Umschaltung), das alle beteiligten Schichten bereits durchzieht:

1. **Persistenzschicht:** `Language` als nullable `string` in Entity + Domain-Model, Spalte `language` via Migration `AddSettingsLanguage`, Mapping im `SettingsRepository`. Kein neues Interface nötig — `ISettingsRepository` bleibt unverändert.
2. **ViewModel:** `LanguageOptions`-Liste im Konstruktor mit lokalisierten Labels befüllen (Reihenfolge: System, Deutsch, English); `SelectedLanguage` nutzt das vorhandene `PersistOnChange()`/`PersistAsync()` unter `_persistLock`; in `LoadAsync` unter `_isLoading`-Schutz selektieren. Kein Debounce nötig (einfache Auswahl, wie `SelectedTheme`).
3. **Kultur-Anwendung beim Start:** In `App.OnStart` wird bereits `settings` gelesen und `IAppThemeService.ApplyTheme(settings.Theme)` aufgerufen — der Sprachblock kann dort ergänzt werden. Zu beachten ist jedoch die Reihenfolge: `CreateWindow` (und damit die `AppShell`-Erzeugung) läuft **vor** `OnStart`, und `PersistAsync`-unabhängige `{x:Static}`-Bindungen sowie Singleton-ViewModels lesen `AppResources` einmalig. Für einen wirksamen Wechsel genügt es daher, die Kultur möglichst früh im Startpfad zu setzen — konkrete Platzierung (z. B. synchroner Resolve im `App`-Konstruktor vs. `OnStart`) ist in der Umsetzungsplanung zu entscheiden. Ein Neustart-Mechanismus oder Shell-Neuaufbau ist explizit nicht Teil des Scopes.
4. **UI:** `Picker` + Hinweis-`Border` nach den bestehenden Karten-/Spacing-Konventionen der `SettingsPage` (mobiles Layout, `MinimumHeightRequest="44"`, `SemanticProperties.Description`, `AppThemeBinding`-Farben).
5. **Abhängigkeiten:** Keine neuen Services oder Interfaces zwingend erforderlich. Falls die Kultur-Anwendung testbar gekapselt werden soll, wäre ein `ILanguageService` analog `IAppThemeService` denkbar — `CultureInfo.CurrentUICulture` ist aber Core-kompatibel (`net10.0`), sodass die Logik auch direkt in `App`/`Reporter.Core` liegen kann.

## Konfiguration

Benutzerspezifische Anwendungseinstellung: Die Sprachwahl wird im Singleton-Datensatz der `settings`-Tabelle persistiert (`Settings.Language`, Werte `system`/`de`/`en`) und über die Einstellungen-UI geändert. Default `system` = bisheriges Verhalten (Systemsprache, Fallback Englisch). Unbekannte persistierte Werte fallen auf `system` zurück (Konvention aus `IAppThemeService.ApplyTheme`).

## Offene Fragen

1. **Nur `CurrentUICulture` oder auch `CurrentCulture`?** Die Anforderung nennt nur `CurrentUICulture`. Formatierungen (`string.Format(CultureInfo.CurrentCulture, …)`, Datumsanzeigen z. B. in `ArticleDetailViewModel`, `UnreadViewModel`) folgen jedoch `CurrentCulture` — bei manueller Sprachwahl blieben Zahlen-/Datumsformate sonst systemsprachlich. Annahme: beide werden gesetzt, sofern nicht anders gewünscht.
2. **Zeitpunkt der Kultur-Anwendung:** `App.OnStart` läuft nach `CreateWindow`; `ISettingsRepository.GetAsync` ist asynchron und setzt eine migrierte Datenbank voraus. Zu klären, ob die Kultur synchron vor `CreateWindow` (z. B. im `App`-Konstruktor) angewendet wird oder `OnStart` ausreicht — Annahme: `OnStart` genügt, da ohnehin ein Neustart erforderlich ist und die AppShell selbst keine lokalisierten Strings bindet (geprüft: `AppShell.xaml` enthält kein `{x:Static}` auf `AppResources`).
3. **Sichtbarkeit des Neustart-Hinweises:** Immer unter dem Picker anzeigen (einfachste Lesart) oder nur, wenn die Auswahl von der aktuell wirksamen Sprache abweicht? Annahme: statischer Hinweis analog der übrigen Info-Texte in den Einstellungen.
4. **Platzierung in der Einstellungen-Seite:** Eigene Sektions-Karte „Sprache" oder Aufnahme in die Karte „Erscheinungsbild"? Annahme: eigene Sektion oder — falls gewünscht — Erweiterung der Erscheinungsbild-Karte; beides folgt dem vorhandenen Layout.
5. **Persistierte Werte:** `system`/`de`/`en` (BCP-47-Sprachcodes) als Annahme in Anlehnung an das `Theme`-Schema; alternativ `de-DE`/`en-US` — Auswirkung auf `new CultureInfo(value)`.
