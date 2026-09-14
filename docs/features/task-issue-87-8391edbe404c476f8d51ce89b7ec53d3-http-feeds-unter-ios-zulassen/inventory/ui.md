<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# UI — Bestandsaufnahme

## `src/Reporter/Views/FeedsPage.xaml`

Feed-Karte (`CollectionView.ItemTemplate`, `:142-300`):

- `TapGestureRecognizer Tapped="OnFeedTapped"` auf der `Border`-Karte (`:153-156`).
- Status-Punkt (`BoxView`, `:207-224`) + Status-Pille (`Border` mit `RoundRectangle 9999`, `:248-295`), beide mit `DataTrigger` auf `HealthStatus` (`OK`/`Warning`/`Error`), Farben per `AppThemeBinding` (`LightStatus*`/`DarkStatus*`), Texte `HealthStatusOkLabel`/`WarningLabel`/`ErrorLabel` (`:279-293`).
- Weitere Zeilen: Titel, `CategoryName`, `LastCheckedAt`, `UnreadCount`, Favicon/`FeedInitial`-Fallback.
- **Keine** Fehlerdetail-Zeile und kein Tap auf dem Badge; die Karte hat kein gebundenes Property für eine Fehlermeldung.
- Fehleranzeige oberhalb der Liste: `Label Text="{Binding SyncErrorMessage}"` (`:53-56`) — generischer Text aus `AppResources.SyncStatusError`, `IsVisible="{Binding HasSyncError}"`.
- Add/Edit-Sheet (`:303-434`) mit `NewUrlEntry` (`Keyboard="Url"`, `ReturnCommand="{Binding SearchCommand}"`), `ErrorMessage`/`SearchErrorMessage`-Labels, Edit-only-Bereich mit `FeedNotificationsEnabled`-Switch und `SaveCommand`.
- Mobile-Konventionen bereits umgesetzt: `MinimumHeightRequest="44"` an Buttons/Switch, `AppThemeBinding`, `Grid` mit `RowDefinitions="Auto,*"`, kein verschachteltes `ScrollView`/`CollectionView`.

## `src/Reporter/Views/FeedsPage.xaml.cs`

| Methode | Kurzbeschreibung |
|---------|------------------|
| `OnFeedTapped` (`:56-93`) | `DisplayActionSheetAsync` mit `ActionSheetTitleFeed` + Aktionen `ButtonRefresh`, `ButtonRename`, `ButtonChangeCategory`, `ButtonEdit`, `ButtonDelete` — **kein** Eintrag für Fehlerdetails; kein Bezug zu `HealthStatus` |
| `RenameFeedAsync` (`:101-114`) | `DisplayPromptAsync` → `viewModel.RenameFeedAsync` |
| `ChangeCategoryAsync` (`:122-149`) | `DisplayActionSheetAsync` mit `MakeUniqueOptionLabels` → `ChangeFeedCategoryAsync` |
| `ConfirmDeleteFeedAsync` (`:157-169`) | `DisplayAlertAsync` (`ConfirmDeleteFeedTitle`/`Message`, `ButtonYes`/`ButtonNo`) — vorhandenes Alert-Muster |
| `OnSearchResultTapped` (`:177-195`) | `DisplayAlertAsync` (`ConfirmSubscribeFeedTitle`/`Message`) → `SubscribeResultCommand` |
| `ConfirmDirectAddAsync` (`:215-222`) | `DisplayAlertAsync`-Callback, im Konstruktor an `viewModel.ConfirmDirectAddAsync` verdrahtet (`:27`) |
| `OnAppearing`/`OnDisappearing` | `PropertyChanged`-Subscription (Singleton-ViewModel + transiente Page), `LoadCommand.Execute` |
| `OnViewModelPropertyChanged` | Fokussiert `NewUrlEntry` bei `ShowAddForm` |
| `OnBackButtonPressed` | Schließt Add-Sheet |

## `AppResources`-Schlüssel (Auszug, für diese Anforderung relevant)

Dateien: `src/Reporter.Core/Resources/Strings/AppResources.resx` (EN), `AppResources.de.resx` (DE), generierter `AppResources.Designer.cs`.

Vorhanden (EN → DE):

| Schlüssel | EN | DE |
|-----------|-----|-----|
| `SyncStatusError` | "Synchronization failed." | "Synchronisation fehlgeschlagen." |
| `HealthStatusOkLabel` / `WarningLabel` / `ErrorLabel` | "OK" / "Warning" / "Error" | …/ "Fehler" |
| `ErrorFeedUrlInvalid` | "Please enter a valid feed URL." | "Bitte gib eine gültige Feed-URL ein." |
| `ErrorFeedDuplicate`, `ErrorFeedTitleEmpty` | Duplikat-/Titel-Fehler | vorhanden |
| `ActionSheetTitleFeed`, `ButtonRefresh`, `ButtonRename`, `ButtonChangeCategory`, `ButtonEdit`, `ButtonDelete`, `ButtonCancel`, `ButtonYes`, `ButtonNo`, `ButtonOk` | Action-Sheet-/Dialog-Texte | vorhanden |
| `ConfirmDeleteFeedTitle`/`Message`, `ConfirmSubscribeFeedTitle`/`Message` | Dialog-Muster | vorhanden |
| `OfflineHint`, `FeedSearchOfflineHint`, `FeedSearchUnavailable`, `FeedSearchUnavailableRetry`, `FeedSearchNoResults*` | Offline-/Suche-Hinweise | vorhanden |
| `LabelFeedHealthStatus`, `AccessibilityTapForActions`, `AccessibilityDismissSheet` | Labels/A11y | vorhanden |

**Nicht vorhanden:** Schlüssel für Fehlerdetail-Titel/-Dialog, ATS-spezifische oder anderweitig gemappte, verständliche Fehlertexte.

## Design-Referenz

`design-draft/stitch_local_rss_feed_reader/feeds_health_status/screen.png` und `..._dark_mode/screen.png` existieren (Feed-Karten mit Health-Badge); ein Entwurfs-Screen für eine Fehlerdetail-Anzeige ist nicht vorhanden.
