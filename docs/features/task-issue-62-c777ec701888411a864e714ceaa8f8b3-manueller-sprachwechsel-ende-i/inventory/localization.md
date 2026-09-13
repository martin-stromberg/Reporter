<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Lokalisierung (Issue #28)

## Ressourcen-Dateien

Verzeichnis: `src/Reporter.Core/Resources/Strings/`

| Datei | Rolle |
|-------|-------|
| `AppResources.resx` | Neutrale Ressource = Englisch, 149 `<data>`-Schlüssel |
| `AppResources.de.resx` | Deutsche Satelliten-Ressource, 149 `<data>`-Schlüssel (Schlüsselmenge identisch) |
| `AppResources.Designer.cs` | Generierte stark typisierte Klasse `AppResources` (namespace `Reporter.Core.Resources.Strings`), generiert via `PublicResXFileCodeGenerator` (Eintrag `EmbeddedResource Update` mit `<Generator>` in `src/Reporter.Core/Reporter.Core.csproj` Zeilen 19–24 — Design-Time-Generierung; neue Schlüssel erfordern eine Regenerierung des Designers) |

Vorhandene relevante Schlüssel (Auszug): `SettingsSectionRetention/Keywords/Sync/Notifications/Appearance`, `SettingsThemeLabel`, `SettingsThemeSystem` (`"System"` en/de), `SettingsThemeLight`, `SettingsThemeDark`, `SettingsRetentionInfo` usw. **Keine** `SettingsLanguage*`- oder `SettingsSectionLanguage`-Schlüssel vorhanden.

`AppResources.Designer.cs` enthält (Zeile 56) die statische Property `Culture` (`get`/`set` auf das interne Feld `resourceCulture`), über die sich die CurrentUICulture für alle Ressourcenzugriffe dieser Klasse überschreiben lässt. Ein `NeutralResourcesLanguage`-Assembly-Attribut ist im Quellcode nicht gesetzt (kein Eintrag in `.csproj`/`Directory.Build.props` gefunden).

## Culture-Handling zur Laufzeit

- **Kein explizites Setzen:** `CultureInfo.CurrentUICulture` oder `CurrentCulture` wird nirgendwo in `src/` gesetzt (Grep: nur Treffer in `AppResources.Designer.cs`-Kommentar und `Culture`-Property). Die Ressourcenauflösung folgt damit automatisch der Systemsprache; nicht abgedeckte Sprachen fallen auf die neutrale Resx (Englisch) zurück.
- **Bindungszeitpunkt:** XAML-Texte via `{x:Static strings:AppResources.*}` lösen beim Laden der Seite einmalig auf; `AppShell.xaml.cs` liest Tab-Titel und erzeugt Pages eager im Konstruktor (in `App.CreateWindow`, vor `App.OnStart`); Singleton-ViewModels (u. a. `SettingsViewModel`) lesen Labels einmalig im Konstruktor.
- **Formatierungen** folgen `CultureInfo.CurrentCulture` (nicht `CurrentUICulture`): u. a. `SettingsViewModel.FormatRetentionDays` (Zeile 462), `UnreadViewModel.LastSyncText` (`{0:g}`, Zeile 388), `ArticleDetailViewModel` (`PublishedAt?.ToString("g", CurrentCulture)`, Zeile 300; Zeitformate Zeilen 285/365), `FeedsPage.xaml.cs` (Zeilen 187/219), `NotificationService` (`NotificationSummaryFormat`, Zeile 81).

## Dokumentation

`docs/help/anwendung/sprache.md` beschreibt den Ist-Zustand: Sprache folgt der Systemsprache, Englisch als Ersatzsprache, „Es gibt keine Sprach-Auswahl in den **Einstellungen**; ein manueller Wechsel ist für ein späteres Release vorgesehen." — muss im Doku-Schritt aktualisiert werden.

`test-results.md` (Zeile 538) enthält einen unerledigten Checklisten-Punkt „Sprachen: System auf Deutsch → deutsche Texte; System auf Englisch → …" (manuelle Verifikation aus Issue #28).
