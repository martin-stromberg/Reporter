<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

- [x] `FeedTitleFallback` (statische Hilfsklasse, `src/Reporter.Core/Services/FeedTitleFallback.cs`) — angelegt
- [x] Methode `GetFallbackTitle(string url)` in `FeedTitleFallback` — vorhanden (Pfadsegment → Host → URL, `FeedTitleFallback.cs` Z. 17–30)
- [x] Methode `IsFileNamePlaceholderTitle(string title, string url)` in `FeedTitleFallback` — vorhanden (OrdinalIgnoreCase, URL-dekodiert, Z. 39–44)
- [x] Eigenschaft `ShowAddForm` (`bool`) in `FeedsViewModel` — vorhanden (`FeedsViewModel.cs` Z. 197–201)
- [x] Eigenschaft `IsEditMode` (`bool`) in `FeedsViewModel` — vorhanden (Z. 207–211)
- [x] `OpenAddFormCommand` (`RelayCommand`) in `FeedsViewModel` — vorhanden (Z. 69, 106; `OpenAddForm` Z. 539–545 leert `ErrorMessage` **und** `SearchErrorMessage`)
- [x] `CloseAddFormCommand` (`RelayCommand`) → `ResetForm` — vorhanden (Z. 70, 111)
- [x] Methode `RenameFeedAsync(FeedListItem?, string?)` in `FeedsViewModel` — vorhanden (Z. 484–510: Null-Guard, leerer Titel → `ErrorFeedTitleEmpty`, `UpdateAsync`, `ErrorMessage` leeren, `LoadAsync`)
- [x] Methode `ChangeFeedCategoryAsync(FeedListItem?, Category?)` in `FeedsViewModel` — vorhanden (Z. 518–537: Null-Guards, `Guid.Empty` → `CategoryId = null`, `UpdateAsync`, `LoadAsync`)
- [x] `EditAsync` erweitert — setzt `IsEditMode = true` und `ShowAddForm = true` (Z. 361–362)
- [x] `ResetForm` erweitert — setzt `ShowAddForm = false`, `IsEditMode = false` (Z. 392–393); `NewTitle`-Reset bestehen
- [x] `SearchAsync` (`FeedsViewModel.Search.cs`) — `ShowAddForm = false` beim Setzen von `ShowSearchResults = true` im Trefferpfad (Z. 143) und im leeren-Freitext-Pfad (Z. 119); Fehlerpfad lässt `ShowAddForm` unverändert
- [x] `OfferDirectAddAsync` geändert — Bestätigung persistiert sofort (`AddAsync` mit `FeedTitleFallback.GetFallbackTitle(input)`, `CategoryId = null`, `NotificationsEnabled = true`, `HealthStatus.Ok`, dann `ResetForm` + `LoadAsync`, Z. 255–270); Dublette → `ErrorFeedDuplicate` + `SearchErrorMessage` geleert + `ShowAddForm = true` ohne `AddAsync` (Z. 242–251); Ablehnung leert nur Suchzustand (Z. 235–240); Dialog-Exception geschluckt (Z. 229–233)
- [x] `SubscribeResultAsync` geändert — Titel-Fallback `FeedTitleFallback.GetFallbackTitle(result.FeedUrl)` bei leerem Titel (Z. 299); `CategoryId = null`, `NotificationsEnabled = true` fest verdrahtet (Z. 300, 304)
- [x] `FeedSyncService.RunSyncAsync` — `isPlaceholderTitle` um `FeedTitleFallback.IsFileNamePlaceholderTitle(feed.Title, feed.Url)` erweitert (`FeedSyncService.cs` Z. 190); `IsHostPlaceholderTitle` und `UpdateFeedHealthAsync` unverändert
- [x] `FeedsPage.xaml` — permanente Formularkarte entfernt; Seitenebene `Grid.Row 0`: Primär-`Button` `ActionAddFeed` → `OpenAddFormCommand` (`MinimumHeightRequest="44"`, Z. 17–19) + `ErrorMessage`-`Label` (Z. 20–23); `SearchErrorMessage`, `FeedSearchOfflineHint`, `SyncErrorMessage`, Offline-Banner unverändert auf Seitenebene (Z. 24–55)
- [x] `FeedsPage.xaml` — Bottom-Sheet-Overlay: `Grid` mit `Grid.RowSpan="2"`, `IsVisible="False"` + `DataTrigger` auf `ShowAddForm` (Z. 222–228); halbtransparenter `BoxView`-Backdrop mit `AppThemeBinding` + `TapGestureRecognizer` → `CloseAddFormCommand` (Z. 229–234); `Border`-Karte `VerticalOptions="End"`, `StrokeShape="RoundRectangle 12,12,0,0"`, `AppThemeBinding` (Z. 235–238)
- [x] `FeedsPage.xaml` — Sheet-Inhalt: Titel-`Label` `FeedAddSheetTitle`/`FeedEditSheetTitle` per `DataTrigger` auf `IsEditMode` (Z. 242–249), Schließen-`Button` → `CloseAddFormCommand` ≥ 44 pt (Z. 251–254), `Entry NewUrl` mit `x:Name="NewUrlEntry"` + `ReturnCommand="{Binding SearchCommand}"` (Z. 274–278), `ActivityIndicator` `IsSearching` (Z. 279–281), `Button` „Suchen“ (Z. 282–283)
- [x] `FeedsPage.xaml` — Hinweis-/Fehler-Block innerhalb der Sheet-Karte über dem `Entry`: `ErrorMessage`/`HasError`, `SearchErrorMessage`/`HasSearchError`, `FeedSearchOfflineHint` mit `IsOnline=False`-`DataTrigger` — identische Bindings wie Seitenebene (Z. 256–273)
- [x] `FeedsPage.xaml` — Edit-Modus-Block per `DataTrigger` auf `IsEditMode`: Notification-`Switch`-`Grid` (inkl. `NotificationsSupported`-Deaktivierung), iOS-Hinweis-`Border`, `Button` „Speichern“ (`SaveCommand`) (Z. 284–328); `Entry NewTitle`, `Picker Categories`, `SelectedCategory` entfernt
- [x] `FeedsPage.xaml` — unverändert: Treffer-`Grid` (`ShowSearchResults`-Trigger, `OnSearchResultTapped`, Attribution, „Zurück zu meinen Feeds"), Feed-`RefreshView`/`CollectionView`; keine Scroll-/CollectionView-Verschachtelung, Liste in `Grid.Row="*"`
- [x] `FeedsPage.xaml.cs` — `OnFeedTapped` um `ButtonRename` (`DisplayPromptAsync` mit `initialValue: feed.Title` → `RenameFeedAsync`, Z. 71–84) und `ButtonChangeCategory` (`DisplayActionSheetAsync` über `viewModel.Categories.Select(c => c.Name)` inkl. `CategoryNone`, Rückmapping per Index → `ChangeFeedCategoryAsync`, Z. 85–99) erweitert
- [x] `FeedsPage.xaml.cs` — Konstruktor abonniert `viewModel.PropertyChanged`: bei `ShowAddForm`-Wechsel auf `true` `NewUrlEntry.Focus()` via `Dispatcher.Dispatch` (Z. 24–30)
- [x] `FeedsPage.xaml.cs` — `OnBackButtonPressed`-Override: bei `ShowAddForm` `CloseAddFormCommand` ausführen + `true` zurückgeben, sonst `base` (Z. 146–155)
- [x] `AppResources` — neue Schlüssel `ActionAddFeed`, `FeedAddSheetTitle`, `FeedEditSheetTitle`, `ButtonRename`, `ButtonChangeCategory`, `PromptRenameFeedTitle`, `PromptRenameFeedMessage` in `AppResources.resx` (Z. 443–461), `AppResources.de.resx` (Z. 443–461) und `AppResources.Designer.cs` (Z. 1346–1402)
- [x] `FeedTitleFallbackTests` (`src/Reporter.Tests/FeedTitleFallbackTests.cs`) — 8 Tests: Pfadsegment, URL-Dekodierung, Host-Fallback, unparsebare URL, Trailing Slash, Placeholder-Match/-Mismatch, Case-insensitiv
- [x] `FeedsViewModelTests` — neue/angepasste Tests: `OpenAddFormCommand_ShowsForm`, `CloseAddFormCommand_ResetsFormAndHidesForm`, `EditAsync_OpensSheetInEditMode`, `RenameFeedAsync_UpdatesTitle`/`_EmptyTitle_SetsErrorAndKeepsTitle`/`_NullFeed_DoesNothing`, `ChangeFeedCategoryAsync_SetsCategoryId`/`_EmptyGuid_ClearsCategory`/`_NullArguments_DoNothing`, `SubscribeResultCommand_WhenTitleEmpty_StoresFileNameAsPlaceholder` (ersetzt `..._StoresFeedUrlAsPlaceholder`), `SubscribeResultCommand_PersistsFeedFromResult` (Defaults), `SearchCommand_NoResultsAndValidUrl_Confirmed_AddsFeedWithFileNameTitle` (ersetzt `..._PrefillsTitleFromHost`), `SearchCommand_WhenUnavailableAndConfirmedDuplicate_KeepsSheetOpenAndSetsError` (Dubletten-Fall), `SearchCommand_NoResultsAndValidUrl_Declined_KeepsFormState` (angepasst), `SearchCommand_WhenResultsShown_ClosesAddForm`, `SearchCommand_WhenUnavailable_KeepsSheetOpenAndSetsSearchError`, `SaveCommand_EditMode_WhenInvalidUrl_KeepsSheetOpenAndSetsError`, `SaveCommand_EditMode_WhenDuplicate_KeepsSheetOpenAndSetsError`, `DeleteCommand_ResetsFeedNotificationsEnabled` (um `ShowAddForm`/`IsEditMode`-Asserts erweitert); alle zu prüfenden Bestandstests vorhanden und grün
- [x] `FeedSyncServiceTests` — `SyncFeedAsync_WhenTitleIsFileNamePlaceholder_UpdatesTitleFromFeedDocument` hinzugefügt (Z. 557–579); `..._WhenTitleIsPlaceholder_...`, `..._WhenTitleIsHostPlaceholder_...`, `..._WhenTitleIsSet_DoesNotOverwriteTitle` unverändert grün
- [x] Manuelle UI-Verifikation — durchgeführt auf 390 × 844 pt (Dark + Light), 18 Screenshots `test-results/issue-59/manual-2-01..16-*.png` + `manual-3-01/02-*.png`, dokumentiert in `test-results.md` (Abschnitt „Issue #59 (Iteration 3)") und `docs/help/anwendung/mobile-ui-design.md`
- [x] Statische Prüfungen — `Run-StaticChecks.ps1` Exit 0 (in `test-results.md` dokumentiert); im Review erneut verifiziert: `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` → **303/303 bestanden** (0 Fehler, 0 übersprungen)

## Hinweise

- Der im Plan genannte Test `SearchCommand_NoResultsAndValidUrl_Confirmed_WhenDuplicate_SetsError` heißt in der Implementierung `SearchCommand_WhenUnavailableAndConfirmedDuplicate_KeepsSheetOpenAndSetsError` (`FeedsViewModelTests.cs` Z. 642) und exerziert denselben Dubletten-Codepfad in `OfferDirectAddAsync` (Dublettenprüfung nach Bestätigung ist eingabeunabhängig identisch) — fachlich abgedeckt.
- `FeedTitleFallback.TryGetFileName` nutzt `uri.Segments` mit `TrimEnd('/')` + `LastOrDefault(s => s.Length > 0)` — Trailing-Slash- und URL-Dekodierungsfälle sind durch Tests abgedeckt.
- `manual-2-05` und `manual-2-06` heißen beide `manual-2-*-feed-added-dark.png` (doppelter Dateiname in `test-results/issue-59/`, ohne Inhaltskonflikt erkennbar).
