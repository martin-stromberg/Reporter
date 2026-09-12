<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# UI und Ressourcen

## `FeedsPage.xaml`
Datei: `src/Reporter/Views/FeedsPage.xaml`

Struktur: `ContentPage` (`Shell.NavBarIsVisible="False"`) mit `Grid` `RowDefinitions="Auto,*"` — erfüllt bereits die Mobile-UI-Regel „Liste füllt Restbildschirm über `Grid`-Row `*`“.

Formular-Bereich (Row 0, `VerticalStackLayout`):

| Element | Binding / Inhalt |
|---------|------------------|
| `Border` (Karte) | `AppThemeBinding`-Hintergrund (`LightSurfaceContainer`/`DarkSurfaceContainer`) |
| `Entry` `NewUrl` | `PlaceholderFeedUrl`, `Keyboard="Url"`, `ReturnCommand="{Binding SaveCommand}"` |
| `Entry` `NewTitle` | `PlaceholderFeedTitle`, `ReturnCommand="{Binding SaveCommand}"` |
| `Picker` | `ItemsSource=Categories`, `SelectedItem=SelectedCategory`, `ItemDisplayBinding=Name`, Titel `LabelFeedCategory` |
| Notification-`Grid` + `Switch` | `IsEnabled`/`Opacity` über `NotificationsSupported`-DataTrigger; `Switch` `IsToggled=FeedNotificationsEnabled`, `MinimumWidthRequest`/`MinimumHeightRequest` = 44 |
| Hinweis-`Border` | nur sichtbar bei `NotificationsSupported == false` (`NotificationsIosOnlyHint`) |
| Fehler-`Label` | `ErrorMessage` / `IsVisible=HasError`, `AppThemeBinding` Error-Farben |
| `Button` Speichern | `ButtonSave`, `Command=SaveCommand` |
| Offline-`Border` | sichtbar bei `IsOnline == false`, Text `OfflineHint` |
| Sync-Fehler-`Label` | `SyncErrorMessage` / `IsVisible=HasSyncError` |

Feed-Liste (Row 1): `RefreshView` (`IsRefreshing=IsSyncing`, `Command=RefreshAllCommand`) um eine `CollectionView` (`ItemsSource=Feeds`, `EmptyView=PlaceholderFeeds`). ItemTemplate = kartenbasierter `Border` mit `TapGestureRecognizer` → `OnFeedTapped` (`CommandParameter="{Binding .}"`), Titel, Health-Status-Punkt (`BoxView` mit `AppThemeBinding`-Triggern auf `HealthStatus` „OK“/„Warning“/„Error“), Kategorie/letzter Check, Unread-Count, Status-Label.

Nicht vorhanden: Such-Trefferliste, `ActivityIndicator`/Busy-Indikator für eine Suche, eigenes Eingabefeld für Suchtext.

## `FeedsPage.xaml.cs`
Siehe [logic.md](logic.md) — `OnFeedTapped` nutzt `DisplayActionSheetAsync` (Aktualisieren/Bearbeiten/Löschen) und `DisplayAlertAsync` (Lösch-Bestätigung); dieses Muster ist Referenz für die geforderten Fallback-Confirm-Dialoge.

## `AppResources`
Dateien: `src/Reporter.Core/Resources/Strings/AppResources.resx` (Neutral/EN) und `AppResources.de.resx` — je 131 `data`-Schlüssel, paritätisch. Designer-Datei `AppResources.Designer.cs` generiert (`PublicResXFileCodeGenerator`).

Für die Anforderung relevante vorhandene Schlüssel:

| Schlüssel | Verwendung |
|-----------|------------|
| `PlaceholderFeedUrl` | Placeholder des URL-Entry (künftig Such-/URL-Eingabe) |
| `PlaceholderFeedTitle` | Placeholder Titel-Entry |
| `LabelFeedCategory` | Titel des Kategorie-Pickers |
| `CategoryNone` | „Keine Kategorie“-Eintrag (`Guid.Empty`) |
| `ButtonSave`, `ButtonCancel`, `ButtonYes`, `ButtonNo` | Buttons/Dialoge |
| `ErrorFeedUrlInvalid` | Fehlermeldung ungültige URL in `SaveAsync` |
| `ErrorFeedTitleEmpty` | Fehlermeldung leerer Titel |
| `ErrorFeedDuplicate` | Dubletten-Fehlermeldung |
| `OfflineHint` | Offline-Banner (auch `FeedSyncService`-Offline-Message) |
| `SyncStatusError` | Generische Sync-Fehlermeldung |
| `ActionSheetTitleFeed` | Titel des Feed-ActionSheet |
| `ConfirmDeleteFeedTitle`/`ConfirmDeleteFeedMessage` | Lösch-Bestätigung |
| `ButtonRefresh`, `ButtonEdit`, `ButtonDelete` | ActionSheet-Aktionen |
| `FeedNotificationsLabel`, `FeedNotificationsHint`, `NotificationsIosOnlyHint` | Notification-Switch/Hinweis |
| `PlaceholderFeeds` | EmptyView der Feed-Liste |
| `PageTitleFeeds`, `TabFeeds` | Seiten-/Tab-Titel |

Nicht vorhanden (von der Anforderung gefordert): Such-Placeholder, „Kein Feed gefunden. Möchtest du die eingegebene URL direkt hinzufügen?", „Die eingegebene URL ist nicht gültig." (als Such-Kontext-Variante; `ErrorFeedUrlInvalid` existiert bereits), Hinweis bei nicht erreichbarem Verzeichnis, Offline-Hinweis für deaktivierte Suche.
