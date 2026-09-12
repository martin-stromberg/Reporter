# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

Geprüft wurden alle uncommitteten Änderungen auf `task/issue-28-b7b5807abfea421fba84f7ded823c28c-offline-faehigkeit-und-mehrspr` gegen `plan.md`. Verifikation: `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release` → 208/208 bestanden (Baseline 189, +19 neue Tests).

## Umgesetzte Planelemente

### Neue Objekte

- [x] `INetworkStatusService` (Interface, `src/Reporter.Core/Interfaces/INetworkStatusService.cs`) — angelegt mit `bool IsOnline` + `event EventHandler? ConnectivityChanged`
- [x] `NetworkStatusService` (Klasse, `src/Reporter/Services/NetworkStatusService.cs`) — angelegt; `IsOnline` via `Connectivity.Current.NetworkAccess == NetworkAccess.Internet`, `Connectivity.ConnectivityChanged` im Konstruktor abonniert, Dispatch via `MainThread.BeginInvokeOnMainThread`
- [x] `ArticleHtmlSanitizer` (statische Klasse, `src/Reporter.Core/Services/ArticleHtmlSanitizer.cs`) — angelegt; komplette Sanitize-Regex-Pipeline aus `ArticleDetailViewModel` umgezogen plus neue Regexes `AnchorWithContentRegex`, `AnchorTagRegex`, `ImageTagRegex`; `Sanitize(string?, bool forOffline)` neutralisiert `<a>` und entfernt `<img>` nur bei `forOffline == true`
- [x] `FakeNetworkStatusService` (Test-Fake, `src/Reporter.Tests/FakeNetworkStatusService.cs`) — angelegt mit setzbarem `IsOnline` (Default `true`) und `RaiseConnectivityChanged()`

### `UnreadViewModel` (`src/Reporter.Core/ViewModels/UnreadViewModel.cs`)

- [x] Abhängigkeit `INetworkStatusService` (required, an die Parameterliste angehängt) — vorhanden
- [x] Eigenschaft `IsOnline` (`bool`, `SetProperty`, Initialwert aus dem Service) — vorhanden
- [x] `ConnectivityChanged`-Abo im Konstruktor → `OnConnectivityChanged` aktualisiert `IsOnline` — vorhanden
- [x] `RefreshAsync`: Frühabbruch `if (!IsOnline) { ErrorMessage = AppResources.OfflineHint; return; }` vor `IsSyncing = true` — vorhanden

### `FeedsViewModel` (`src/Reporter.Core/ViewModels/FeedsViewModel.cs`)

- [x] Abhängigkeit `INetworkStatusService` (required, vor `ILocalNotificationService?`) — vorhanden
- [x] Eigenschaft `IsOnline` + `ConnectivityChanged`-Abo im Konstruktor — vorhanden
- [x] Frühabbruch mit `AppResources.OfflineHint` in `RefreshAsync` und `RefreshAllAsync` — vorhanden

### `LaterViewModel` (`src/Reporter.Core/ViewModels/LaterViewModel.cs`)

- [x] Abhängigkeit `INetworkStatusService` (required, nach `IItemRepository`) — vorhanden
- [x] Eigenschaft `IsOnline` + `ConnectivityChanged`-Abo im Konstruktor — vorhanden

### `ArticleDetailViewModel` (`src/Reporter/ViewModels/ArticleDetailViewModel.cs`)

- [x] Abhängigkeit `INetworkStatusService` (required) — vorhanden
- [x] Eigenschaften `IsOnline`, `ErrorMessage` (private Setter, `OnPropertyChanged(nameof(HasError))`), `HasError` — vorhanden
- [x] Methode `AttachConnectivity()` — idempotent via `_isConnectivityAttached`, abonniert `ConnectivityChanged`, liest `IsOnline` neu und ruft `RebuildHtml()` bei Statuswechsel — vorhanden
- [x] Methode `DetachConnectivity()` — Event-Abmeldung + Guard-Reset + `CancelAutoMarkRead()` — vorhanden
- [x] `OnConnectivityChanged`-Handler → `IsOnline` + `RebuildHtml()` — vorhanden
- [x] `BookmarkButtonLabel` → `AppResources.ArticleBookmarkRemove`/`ArticleBookmarkSet` — umgestellt
- [x] `MarkAsReadButtonLabel` → `AppResources.ArticleAlreadyRead`/`ArticleMarkAsRead` — umgestellt
- [x] `CalculateReadingTime` → `string.Format(CultureInfo.CurrentCulture, AppResources.ArticleReadingTimeFormat, minutes)` — umgestellt
- [x] Sanitize-Delegation: `RebuildHtml` ruft `ArticleHtmlSanitizer.Sanitize(Item?.ContentHtml, forOffline: !IsOnline)`; statische Regex-Felder umgezogen — vorhanden (statt `SanitizeHtml`-Methode direkter Aufruf in `RebuildHtml`, funktional identisch)
- [x] `OpenInBrowserAsync`: Offline-Guard (`ErrorMessage = AppResources.OfflineHint`) + try/catch um `Browser.OpenAsync` — vorhanden

### `FeedSyncService` (`src/Reporter.Core/Services/FeedSyncService.cs`)

- [x] Abhängigkeit `INetworkStatusService` (required, am Ende der Parameterliste) — vorhanden
- [x] `SyncFeedAsync` + `SyncAllAsync`: `IsOnline`-Prüfung ganz am Anfang, Rückgabe `new SyncResult(FeedHealth.Error, 0, AppResources.OfflineHint)` vor `SyncLog`-Anlage/`HealthStatus`/HTTP — vorhanden

### `AutoRefreshService` (`src/Reporter.Core/Services/AutoRefreshService.cs`)

- [x] Abhängigkeit `INetworkStatusService` (required, vor `TimeProvider?`) — vorhanden
- [x] `RunLoopAsync`: `if (!_networkStatusService.IsOnline) { continue; }` pro `PeriodicTimer`-Tick — vorhanden

### Views / Code-Behind (`src/Reporter/Views/`)

- [x] `ArticleDetailPage.xaml`: `xmlns:strings` ergänzt; alle hartcodierten Texte ersetzt (`ArticleTitleFallback`, `ArticleReadLabel`, `ArticleFullContentAvailable`, `ArticleOpenInBrowser` ×2, `AccessibilityBack`, `AccessibilityFontSize`, `AccessibilityShare`); `Navigating="OnWebViewNavigating"` am `ArticleWebView`; neue Grid-Row mit `ArticleOfflineLinksDisabled`-`Border` (`DataTrigger` `IsOnline == False`, `NotificationsIosOnlyHint`-Styling) + `ErrorMessage`-`Label` — vorhanden
- [x] `ArticleDetailPage.xaml.cs`: `OnWebViewNavigating` (offline → `e.Cancel = true` + `DisplayAlertAsync(AppResources.OfflineHint, AppResources.ArticleOfflineLinksDisabled, AppResources.ButtonOk)`); `OnAppearing` → `AttachConnectivity()`; `OnDisappearing` → `DetachConnectivity()` — vorhanden
- [x] `UnreadPage.xaml`: `DataTrigger` `IsOnline == False` am Sync-Button-`Border` (`Opacity = 0.4`) + gedimmter `Path`-`Stroke`; `OfflineHint`-`Label` in der Statuszeile; `ErrorMessage`-`Label` (`IsVisible="{Binding HasError}"`); `IsOnline`-Bindung am `ArticleCardView` via `x:Reference PageRoot` — vorhanden
- [x] `FeedsPage.xaml`: persistenter `OfflineHint`-`Border` als drittes Element des oberen `VerticalStackLayout` (`Grid.Row="0"`, unterhalb der Eingabekarte), `DataTrigger` `IsOnline == False`, `NotificationsIosOnlyHint`-Styling — vorhanden
- [x] `LaterPage.xaml`: `IsOnline="{Binding BindingContext.IsOnline, Source={x:Reference PageRoot}}"` am `ArticleCardView` — vorhanden
- [x] `ArticleCardView.xaml.cs`: `IsOnline`-`BindableProperty` (Standard `true`) + CLR-Wrapper — vorhanden
- [x] `ArticleCardView.xaml`: `DataTrigger` `IsOnline == False` → `IsVisible = false` am Thumbnail-`Border` (`Source={x:Reference Card}`) — vorhanden

### Infrastruktur / Lokalisierung

- [x] `MauiProgram`: `.AddSingleton<INetworkStatusService, NetworkStatusService>()` im Service-Block — vorhanden
- [x] `App.OnStart`: `INetworkStatusService`-Resolve im try/catch-Muster — vorhanden
- [x] `CategoriesViewModel.SaveAsync`: beide Literale durch `AppResources.ErrorCategoryNameEmpty`/`ErrorCategoryDuplicate` ersetzt — vorhanden
- [x] Alle 17 neuen ResX-Schlüssel in `AppResources.resx` (EN) und `AppResources.de.resx` (DE) mit den geplanten Texten — vorhanden
- [x] `AppResources.Designer.cs` mit allen 17 neuen statischen Properties regeneriert — vorhanden

### Tests (`src/Reporter.Tests/`)

- [x] Konstruktor-Aufrufstellen in `UnreadViewModelTests`, `FeedsViewModelTests`, `LaterViewModelTests`, `AutoRefreshServiceTests`, `FeedSyncServiceTests` (`CreateService`/`CreateFailingService`) angepasst — vorhanden
- [x] `UnreadViewModelTests`: `RefreshCommand_WhenOffline_SetsOfflineHintAndSkipsSync`, `ConnectivityChanged_UpdatesIsOnline`, `RefreshCommand_WhenBackOnline_InvokesSyncService` — vorhanden, bestanden
- [x] `FeedsViewModelTests`: `RefreshAllCommand_WhenOffline_SetsOfflineHintAndSkipsSync`, `RefreshCommand_WhenOffline_SetsOfflineHintAndSkipsSync`, `ConnectivityChanged_UpdatesIsOnline` — vorhanden, bestanden
- [x] `LaterViewModelTests`: `ConnectivityChanged_UpdatesIsOnline` — vorhanden, bestanden
- [x] `AutoRefreshServiceTests`: `Tick_WhenOffline_SkipsSyncAll`, `Tick_WhenBackOnline_ResumesSync` — vorhanden, bestanden
- [x] `FeedSyncServiceTests`: `SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog`, `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange` — vorhanden, bestanden
- [x] `ArticleHtmlSanitizerTests` (neu): alle fünf geplanten Prüfungen abgedeckt — vorhanden, bestanden

## Hinweise

- **Testnamen in `ArticleHtmlSanitizerTests` weichen leicht vom Plan ab** (z. B. `Sanitize_Offline_RemovesImages` statt `Sanitize_Offline_RemovesImgTags`, `Sanitize_RemovesScriptsAndEventHandlers` als `[Theory]` für beide Modi statt `Sanitize_RemovesScriptAndEventHandlers`). Die geplante Abdeckung ist vollständig gegeben — keine Lücke.
- **Manuelle UI-/E2E-Verifikation weiterhin ausstehend** (Tasks 34–42 in der Tasks-Datei): Die geplanten Szenarien sind in dieser Umgebung nicht ausführbar und in `test-results.md` als ausstehende Checkliste dokumentiert. Das ist vorgesehen und kein Implementierungsdefizit; die zugrunde liegende ViewModel-/Service-Logik ist durch die neuen Unit-Tests abgedeckt.
- `Run-StaticChecks.ps1` und `translation-check.py` sind laut `test-results.md` bereits grün gelaufen (Static Checks Exit 0, 0 fehlende Schlüssel).
