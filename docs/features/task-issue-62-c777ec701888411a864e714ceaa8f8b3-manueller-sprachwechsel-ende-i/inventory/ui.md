<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# UI

## `SettingsPage`

Datei: `src/Reporter/Views/SettingsPage.xaml` (452 Zeilen) — `ContentPage`, `Shell.NavBarIsVisible="False"`, `Grid` mit `RowDefinitions="Auto,*"`, Inhalt in `ScrollView` > `VerticalStackLayout` (Spacing 16).

Alle Texte via `{x:Static strings:AppResources.*}` (51 Vorkommen, `strings`-Namespace = `Reporter.Core.Resources.Strings` aus `Reporter.Core`).

### Sektionen (Reihenfolge)

| Sektion | Ressourcen-Schlüssel | Inhalt |
|---------|--------------------|--------|
| Aufbewahrungsdauer (Zeilen 21–57) | `SettingsSectionRetention` | Slider + `SaveRetentionCommand`, Info-`Border` mit `SettingsRetentionInfo` |
| Keyword-Filter (Zeilen 59–140) | `SettingsSectionKeywords` | Entry + Button, Chip-Liste, Info-`Border` |
| Synchronisation & Lesefluss (Zeilen 142–232) | `SettingsSectionSync` | Switch `AutoRefreshEnabled`, `Picker` `RefreshIntervalOptions`, Switch `AutoMarkReadEnabled`, `Picker` `AutoMarkReadDelayOptions` |
| Benachrichtigungen & Ruhezeiten (Zeilen 234–424) | `SettingsSectionNotifications` | Switches + `TimePicker`s, mehrere getriggerte Hinweis-`Border` |
| Erscheinungsbild (Zeilen 426–447) | `SettingsSectionAppearance` | Einzelnes `Grid` mit `SettingsThemeLabel` + `Picker` `ThemeOptions`/`SelectedTheme` |

Eine Sprach-Sektion bzw. ein `LanguageOptions`-Picker und ein Neustart-Hinweis existieren **nicht**.

### Picker-Muster (Vorbild für Sprachwahl, Zeilen 437–444)

```xml
<Picker ItemsSource="{Binding ThemeOptions}"
        SelectedItem="{Binding SelectedTheme}"
        ItemDisplayBinding="{Binding Label}"
        SemanticProperties.Description="{x:Static strings:AppResources.SettingsThemeLabel}"
        MinimumWidthRequest="44"
        MinimumHeightRequest="44"
        HorizontalTextAlignment="End"/>
```

### Info-/Hinweis-Border-Muster (Vorbild für Neustart-Hinweis, Zeilen 47–54)

`Border` mit `Padding="10"`, `StrokeShape="RoundRectangle 8"`, `Stroke="Transparent"`, `Background="{AppThemeBinding Light={StaticResource LightSurfaceSubtle}, Dark={StaticResource DarkSurfaceSubtle}}"` um ein `Label` mit `BodySmallStyle` und sekundärer Textfarbe — so ist `SettingsRetentionInfo` umgesetzt.

### Layout-Konventionen

- Sektions-Label: `UiLabelStyle`, Karten: `Border` mit `Padding="16"`, `RoundRectangle 12`, `AppThemeBinding` auf `LightSurfaceContainer`/`DarkSurfaceContainer`
- Zeilen: `Grid ColumnDefinitions="*,Auto"` mit Label (ggf. + `MetaStyle`-Hint) links und `Switch`/`Picker` rechts
- Touch-Targets: `MinimumWidthRequest="44"`/`MinimumHeightRequest="44"`, `SemanticProperties.Description` an interaktiven Controls

## `AppShell`

Dateien: `src/Reporter/AppShell.xaml` und `src/Reporter/AppShell.xaml.cs`

- `AppShell.xaml`: leere `Shell` mit statischem `Title="Reporter"`, keine `AppResources`-Bindungen im XAML.
- `AppShell.xaml.cs` (Konstruktor, Zeilen 18–47): baut die `TabBar` **programmatisch** — die Tab-/ShellContent-Titel werden direkt aus `AppResources.Tab*` gelesen (Zeilen 24–37) und die fünf Pages werden **eager** per `services.GetRequiredService<…Page>()` instanziiert (u. a. `SettingsPage` Zeile 37, was den Singleton-`SettingsViewModel` mit auflöst). `Routing.RegisterRoute("articledetail", typeof(ArticleDetailPage))` (Zeile 22).
- Konsequenz für den Startzeitpunkt: Da `App.CreateWindow` die `AppShell` (und damit alle Tabs, Seiten und deren lokalisierte Texte) **vor** `App.OnStart` erzeugt, muss eine manuell gewählte Kultur vor `CreateWindow` wirksam sein, wenn sie ohne Shell-Neuaufbau greifen soll.

## Weitere lokalisierungsgebundene Views

`{x:Static strings:AppResources.*}`-Vorkommen je Datei: `SettingsPage.xaml` 51, `FeedsPage.xaml` 25, `ArticleDetailPage.xaml` 9, `CategoriesPage.xaml` 6, `UnreadPage.xaml` 6, `LaterPage.xaml` 4. `ArticleCardView.xaml` enthält keine. Da `x:Static` beim XAML-Load einmalig auflöst, wirken Sprachänderungen erst nach Neuaufbau der Seiten.
