<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: UI-Komponenten

Betroffene UI-Artefakte der Anforderung „Stichwort-Filter pro Feed" (Issue #116).

## `FeedDetailPage` — Bearbeiten-Sheet („Feed bearbeiten")
Dateien: `src/Reporter/Views/FeedDetailPage.xaml`, `src/Reporter/Views/FeedDetailPage.xaml.cs`

- Route: Shell-Route `feeddetail` (`AppShell.xaml.cs` Z. 23), Parameter `feedId` via `IQueryAttributable.ApplyQueryAttributes` (`FeedDetailPage.xaml.cs` Z. 34–50).
- Öffnen: Feed-Aktions-Sheet (`OnFeedActionsClicked`, Z. 98–162) → `AppResources.ButtonEdit` → `EditCommand`.
- Sheet-Markup (`FeedDetailPage.xaml` Z. 270–348): Overlay-`Grid` mit `DataTrigger` auf `ShowEditForm`; Bottom-Sheet-`Border` mit:
  - Titel `FeedEditSheetTitle` + Cancel-Button (`CloseEditFormCommand`, `MinimumHeightRequest="44"`)
  - Fehler-Label (`ErrorMessage`/`HasError`, `AppThemeBinding` Light/Dark `*Error`)
  - `Entry x:Name="EditUrlEntry"` → `EditUrl`, Placeholder `PlaceholderFeedEditUrl`, `Keyboard="Url"`
  - Notifications-Zeile: `FeedNotificationsLabel`/`FeedNotificationsHint` + `Switch` → `EditNotificationsEnabled`, `IsEnabled="{Binding NotificationsSupported}"`, Touch-Target 44×44, `NotificationsIosOnlyHint`-Infobox bei fehlender Plattform-Unterstützung
  - Save-Button (`SaveEditCommand`, `MinimumHeightRequest="44"`)
- `OnBackButtonPressed` schließt das Sheet statt zu navigieren (Z. 69–78); `PropertyChanged` auf `ShowEditForm` fokussiert `EditUrlEntry` (Z. 250–256).
- **Keine Stichwort-Verwaltung im Sheet vorhanden.**

## `SettingsPage` — globale Keyword-Karte (Muster)
Datei: `src/Reporter/Views/SettingsPage.xaml` (Z. 61–142)

- Abschnitt `SettingsSectionKeywords` mit Card-`Border`:
  - `Grid` aus `Entry` (`NewKeywordText`, Placeholder + `SemanticProperties.Description` = `SettingsKeywordPlaceholder`, `ReturnCommand` = `AddKeywordCommand`) und `Button` (`SettingsKeywordAdd`, `AddKeywordCommand`)
  - Fehler-Label (`ErrorMessage`/`HasError`)
  - `FlexLayout Wrap="Wrap"` mit `BindableLayout.ItemsSource="{Binding Keywords}"` — Chip-`DataTemplate` (`x:DataType="models:Keyword"`): Label `KeywordText` + „×"-`Button` (`RemoveKeywordCommand`, `CommandParameter="{Binding .}"`, `SemanticProperties.Description` = `SettingsKeywordRemoveFormat`, 44×44)
  - Info-Box `SettingsKeywordInfo` und Match-Hinweis `SettingsKeywordMatchLabel`/`SettingsKeywordMatchHint`/`SettingsKeywordMatchStatus`
- Dark Mode durchgehend via `AppThemeBinding`.

## `AppResources` — vorhandene Schlüssel
Dateien: `src/Reporter.Core/Resources/Strings/AppResources.resx` (EN), `AppResources.de.resx` (DE)

Keyword-bezogen: `SettingsSectionKeywords`, `SettingsKeywordPlaceholder`, `SettingsKeywordAdd`, `SettingsKeywordInfo`, `SettingsKeywordMatchLabel`, `SettingsKeywordMatchHint`, `SettingsKeywordMatchStatus`, `SettingsKeywordRemoveFormat`, `ErrorKeywordEmpty`, `ErrorKeywordDuplicate`, `ErrorKeywordTooLong`.

Feed-Edit-bezogen: `FeedEditSheetTitle`, `PlaceholderFeedEditUrl`, `FeedNotificationsLabel`, `FeedNotificationsHint`, `NotificationsIosOnlyHint`, `ButtonEdit`, `ButtonSave`, `ButtonCancel`, `ErrorFeedUrlInvalid`, `ErrorFeedDuplicate`, `ErrorActionFailed`.

**Keine feed-spezifischen Stichwort-Ressourcenschlüssel vorhanden.**
