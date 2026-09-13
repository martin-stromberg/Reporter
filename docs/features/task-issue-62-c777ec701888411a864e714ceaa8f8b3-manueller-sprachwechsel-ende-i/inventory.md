<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Manueller Sprachwechsel (EN/DE) in den Einstellungen (#62)

Analysiert wurde der bestehende Projektcode der .NET-MAUI-App `Reporter` (Projekte `Reporter`, `Reporter.Core`, `Reporter.Data`, `Reporter.Tests` unter `src/`) bezogen auf die übersetzte Anforderung in [requirement.md](requirement.md): persistierte Sprachwahl (System/Deutsch/English) in den Einstellungen, Anwendung auf `CultureInfo.CurrentUICulture` beim App-Start, Neustart-Hinweis statt Laufzeit-Umschaltung.

## Zusammenfassung

- **Vorhanden:** Das `Theme`-Muster ist als Referenzimplementierung vollständig durch alle Schichten gezogen: `Settings.Theme` (Entity `src/Reporter.Data/Entities/Settings.cs`, Domain-Modell `src/Reporter.Core/Models/Settings.cs`), Spalte `theme` via Migration `AddSettingsAutoRefreshAndTheme`, `SettingsValues.Theme*`-Konstanten, `ThemeOption`-Picker-Optionsklasse, `SelectedTheme`/`ThemeOptions` im `SettingsViewModel` mit `PersistOnChange()`/`PersistAsync()` unter `_persistLock`, `Picker` in `SettingsPage.xaml` und `IAppThemeService.ApplyTheme` beim Start in `App.OnStart`.
- **Mehrsprachigkeit (Issue #28):** Vollständig implementiert über `AppResources.resx` (neutral/Englisch, 149 Schlüssel) und `AppResources.de.resx` (Deutsch, 149 Schlüssel) in `src/Reporter.Core/Resources/Strings/` plus generiertem `AppResources.Designer.cs` mit statischer `Culture`-Property. Alle sichtbaren Texte sind über `{x:Static strings:AppResources.*}` gebunden (6 XAML-Dateien, u. a. `SettingsPage.xaml` mit 51 Vorkommen). Es existiert **kein** Code, der `CultureInfo.CurrentUICulture` oder `CurrentCulture` explizit setzt — die App folgt ausschließlich der Systemsprache mit Englisch-Fallback.
- **Fehlend:** Keine `Language`-Property in `Settings` (Entity und Domain-Modell), keine `SettingsValues.Language*`-Konstanten, keine `language`-Spalte bzw. Migration `AddSettingsLanguage`, kein `LanguageOption`-Typ, keine `LanguageOptions`/`SelectedLanguage`-Member im `SettingsViewModel`, kein Sprach-Picker und kein Neustart-Hinweis in `SettingsPage.xaml`, keine Kultur-Setzung in `App`/`MauiProgram`, kein `ILanguageService`-Interface, kein `language`-Parameter in `TestSettingsHelper`. Die Hilfedatei `docs/help/anwendung/sprache.md` beschreibt ausdrücklich, dass es keine Sprach-Auswahl gibt.
- **Bestätigt aus der Anforderung:** `App.OnStart` liest `settings` bereits asynchron über `ISettingsRepository` und ruft `IAppThemeService.ApplyTheme(settings.Theme)` auf (`src/Reporter/App.xaml.cs` Zeilen 53–64); `CreateWindow` (AppShell-Auflösung) läuft vor `OnStart`; `SettingsViewModel` ist als Singleton registriert (`MauiProgram.cs` Zeile 66), die Options-Labels werden einmalig im Konstruktor aus `AppResources` gelesen (Zeilen 99–104).
- **Korrektur/Präzisierung zur Anforderung:** `AppShell.xaml` selbst enthält zwar keine `{x:Static}`-Bindungen, aber `AppShell.xaml.cs` liest die Tab-Titel direkt aus `AppResources.Tab*` und instanziiert alle fünf Seiten (inkl. `SettingsPage` → Singleton-`SettingsViewModel`) **eager im Konstruktor** — also bereits in `CreateWindow`, vor `OnStart`. Für einen wirksamen Sprachwechsel muss die Kultur daher vor `CreateWindow` gesetzt sein oder ein Neustart ist zwingend.

**Test-Ausgangszustand:** `dotnet build Reporter.sln` erfolgreich (0 Fehler, 1 bekannte Warnung CS8765); `dotnet test` auf `src/Reporter.Tests/Reporter.Tests.csproj`: **316 erfolgreich, 0 fehlgeschlagen, 0 übersprungen** (Exit-Code 0). Es gibt keine nachgewiesenen Testfehler. Details und Nachweise in [Tests](inventory/tests.md).

## Details

- [Datenmodell und Persistenzschema](inventory/models.md) — `Settings`-Entity/-Modell, `SettingsValues`-Konstanten, `ConfigureSettings`, Migrationen, Optionsklassen
- [Logik](inventory/logic.md) — `SettingsRepository`, `SettingsViewModel`, `AppThemeService`, App-Start (`App.xaml.cs`, `MauiProgram`), `SettingsPage`-Code-Behind
- [Interfaces](inventory/interfaces.md) — `ISettingsRepository`, `IAppThemeService`
- [UI](inventory/ui.md) — `SettingsPage.xaml`-Struktur, Picker-/Info-Border-Muster, `AppShell.xaml`
- [Lokalisierung](inventory/localization.md) — `AppResources`-Resx/Designer, Culture-Handling, `sprache.md`
- [Tests](inventory/tests.md) — Test-Ausgangszustand mit Nachweisen, Testklassen und Hilfsmethoden

Hinweis: Es existieren keine Enums im betroffenen Bereich; die sprachbezogenen Werte werden als String-Konstanten in `SettingsValues` geführt (siehe [Datenmodell](inventory/models.md)).
