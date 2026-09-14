<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Views / UI

## `SettingsPage.xaml`
Datei: `src/Reporter/Views/SettingsPage.xaml` (514 Zeilen)

Aufbau: `ContentPage` mit `Shell.NavBarIsVisible="False"`, `Grid RowDefinitions="Auto,*"` (Headline + `ScrollView` mit `VerticalStackLayout Spacing="16"`). **Es existiert kein Diagnose-/Support-/Debug-Abschnitt.**

Bestehende Sektionen (Reihenfolge im ScrollView):

| Sektion | Ressourcen-Schlüssel | Inhalt |
|---------|--------------------|--------|
| Aufbewahrungsdauer | `SettingsSectionRetention` | Slider + Info-`Border` |
| Keyword-Filter | `SettingsSectionKeywords` | `Entry` + Add-`Button`, Chip-`FlexLayout`, Info-/Status-Zeilen, Fehler-`Label` (`HasError`/`ErrorMessage`) |
| Synchronisation & Lesefluss | `SettingsSectionSync` | `Switch`-Zeilen (AutoRefresh, RefreshOnStartup, AutoMarkRead) + `Picker`-Zeilen (Intervall, SortOrder, Delay) in deaktivierbaren Hint-`Border`s |
| Benachrichtigungen & Ruhezeiten | `SettingsSectionNotifications` | `Switch` + Plattform-Hinweis-`Border` (`NotificationsSupported`-Trigger), `NotDetermined`-/`Denied`-`Border` mit `MultiTrigger` + Aktions-`Button`, Summary/QuietHours-`Switch`es, `TimePicker`s |
| Erscheinungsbild | `SettingsSectionAppearance` | Theme-`Picker` |
| Sprache | `SettingsSectionLanguage` | Sprach-`Picker` + Restart-Hint-`Border` (`LanguageRestartHintVisible`) |

Sektions-Konvention (durchgehend angewendet, für den neuen Debug-Abschnitt relevant):

- `VerticalStackLayout Spacing="6"` → `Label` mit `UiLabelStyle` als Section-Header → `Border Padding="16"`, `StrokeShape="RoundRectangle 12"`, `Background="{AppThemeBinding Light=LightSurfaceCard, Dark=DarkSurfaceCard}"`.
- Schalter-Zeilen: `Grid ColumnDefinitions="*,Auto"` mit Label-`VerticalStackLayout` (Label + `MetaStyle`-Hint) links und `Switch` rechts (`MinimumWidthRequest`/`MinimumHeightRequest="44"`, `SemanticProperties.Description`).
- Hinweis-/Fehlerflächen: innerer `Border Padding="10"`, `RoundRectangle 8`, `Stroke="Transparent"`, `SurfaceSubtle`-`AppThemeBinding`, `IsVisible="False"` + `DataTrigger`/`MultiTrigger` zum Einblenden (z. B. `NotificationsIosOnlyHint`, `NotificationNotDetermined*`/`Denied*`-Zeilen mit Aktions-`Button`).
- Deaktivierungs-Muster: `IsEnabled`-Binding + `Opacity=0.4` per `DataTrigger`.
- Alle Controls haben `SemanticProperties.Description` und ≥ 44-pt-Touch-Ziele; Dark Mode überall via `AppThemeBinding`.

## `SettingsPage.xaml.cs`
Siehe [logic.md](logic.md) — Event-Binding `NotificationAuthorizationDenied` → `DisplayAlertAsync` (Muster für den Fehlerpfad „kein Mail-Client"); `AppInfo.Current.ShowSettingsUI()` ist die einzige bestehende Essentials-Nutzung.

## Lokalisierungsressourcen
Dateien: `src/Reporter.Core/Resources/Strings/AppResources.resx`, `AppResources.de.resx`, generierter `AppResources.Designer.cs` (`PublicResXFileCodeGenerator`, Namespace `Reporter.Core.Resources.Strings`).

- Vorhandene `Settings*`-Schlüssel decken alle aktuellen Sektionen/Labels/Hints ab (`SettingsSectionRetention` … `SettingsLanguageRestartHint`); Benachrichtigungs-Fehlerpfad über `NotificationDeniedTitle`/`NotificationDeniedMessage`/`NotificationDeniedOpenSettings`, `NotificationNotDeterminedMessage`/`NotificationNotDeterminedAllow`, `NotificationsIosOnlyHint`; generische Buttons `ButtonCancel`, `ButtonOk`, `ButtonYes`/`ButtonNo` u. a.
- **Keine Debug-/Support-/E-Mail-Schlüssel vorhanden** — neue Schlüssel müssten in beiden resx-Dateien ergänzt und im Designer generiert werden.
- XAML bindet statisch via `{x:Static strings:AppResources.*}`.

## Design-Referenz
Unter `design-draft/stitch_local_rss_feed_reader/` existieren `einstellungen_filter/screen.png` und `einstellungen_filter_dark_mode/screen.png` als Referenz für die Settings-Kartenoptik. **Ein eigener Entwurfs-Screen für einen Debug-/Diagnose-Abschnitt existiert nicht.**
