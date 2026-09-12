# Tasks: Offline-Fähigkeit und Mehrsprachigkeit (EN/DE) — Issue #28

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Infrastruktur | `INetworkStatusService`-Interface in `src/Reporter.Core/Interfaces/` anlegen (`bool IsOnline`, `event EventHandler? ConnectivityChanged`) | Offen | — |
| 2 | Infrastruktur | `NetworkStatusService` in `src/Reporter/Services/` implementieren (`Connectivity.Current`, `ConnectivityChanged`-Abo, `MainThread`-Dispatch) | Offen | — |
| 3 | Infrastruktur | `NetworkStatusService` als `AddSingleton<INetworkStatusService, NetworkStatusService>()` in `MauiProgram` registrieren | Offen | — |
| 4 | Infrastruktur | `INetworkStatusService` in `App.OnStart` resolven (try/catch-Block, frühes Monitoring) | Offen | — |
| 5 | Lokalisierung | Neue Schlüssel `ArticleTitleFallback`, `ArticleReadLabel`, `ArticleFullContentAvailable`, `ArticleOpenInBrowser`, `AccessibilityBack`, `AccessibilityFontSize`, `AccessibilityShare` in `AppResources.resx` (EN) + `AppResources.de.resx` (DE) anlegen | Offen | — |
| 6 | Lokalisierung | Neue Schlüssel `ArticleBookmarkSet`, `ArticleBookmarkRemove`, `ArticleMarkAsRead`, `ArticleAlreadyRead`, `ArticleReadingTimeFormat` in beiden ResX-Dateien anlegen | Offen | — |
| 7 | Lokalisierung | Neue Schlüssel `OfflineHint`, `ArticleOfflineLinksDisabled`, `ButtonOk` in beiden ResX-Dateien anlegen | Offen | — |
| 8 | Lokalisierung | Neue Schlüssel `ErrorCategoryNameEmpty`, `ErrorCategoryDuplicate` in beiden ResX-Dateien anlegen | Offen | — |
| 9 | Lokalisierung | `AppResources.Designer.cs` mit den neuen Schlüsseln neu generieren | Offen | — |
| 10 | Lokalisierung | `ArticleDetailPage.xaml`: `xmlns:strings` ergänzen und alle hartcodierten Texte (Z. 9, 26, 128, 139, 160, 212, 257, 280) auf `{x:Static strings:AppResources.*}` umstellen | Offen | — |
| 11 | Lokalisierung | `ArticleDetailViewModel.cs`: `BookmarkButtonLabel`, `MarkAsReadButtonLabel` und `CalculateReadingTime` auf `AppResources.*` umstellen | Offen | — |
| 12 | Lokalisierung | `CategoriesViewModel.cs`: hartcodierte `ErrorMessage`-Literale (Z. 120, 128) durch `AppResources.ErrorCategoryNameEmpty`/`ErrorCategoryDuplicate` ersetzen | Offen | — |
| 13 | Offline-Logik | `ArticleHtmlSanitizer` in `src/Reporter.Core/Services/` anlegen: Sanitize-Regex-Pipeline aus `ArticleDetailViewModel` umziehen, `Sanitize(string?, bool forOffline)` mit `<a>`-Neutralisierung und `<img>`-Entfernung im Offline-Modus | Offen | — |
| 14 | Offline-Logik | `UnreadViewModel`: `INetworkStatusService`-Abhängigkeit, `IsOnline`-Property, `ConnectivityChanged`-Abo, Offline-Frühabbruch in `RefreshAsync` (`ErrorMessage = AppResources.OfflineHint`) | Offen | — |
| 15 | Offline-Logik | `FeedsViewModel`: `INetworkStatusService`-Abhängigkeit, `IsOnline`-Property, `ConnectivityChanged`-Abo, Offline-Frühabbruch in `RefreshAsync` und `RefreshAllAsync` | Offen | — |
| 16 | Offline-Logik | `LaterViewModel`: `INetworkStatusService`-Abhängigkeit, `IsOnline`-Property, `ConnectivityChanged`-Abo (liefert Status für `ArticleCardView`-Thumbnail-Ausblendung) | Offen | — |
| 17 | Offline-Logik | `FeedSyncService`: `INetworkStatusService`-Abhängigkeit, Offline-Frühabbruch in `SyncFeedAsync`/`SyncAllAsync` (`SyncResult(FeedHealth.Error, 0, AppResources.OfflineHint)` ohne `SyncLog`/`HealthStatus`-Änderung) | Offen | — |
| 18 | Offline-Logik | `AutoRefreshService`: `INetworkStatusService`-Abhängigkeit, `RunLoopAsync`-Tick bei `!IsOnline` überspringen | Offen | — |
| 19 | Offline-Logik | `ArticleDetailViewModel`: `INetworkStatusService`-Abhängigkeit, `IsOnline`, `ErrorMessage`/`HasError`, `AttachConnectivity()`/`DetachConnectivity()` (idempotentes `ConnectivityChanged`-Abo, `Detach` inkl. `CancelAutoMarkRead`), `ConnectivityChanged`→`RebuildHtml`, `SanitizeHtml`-Delegation mit Offline-Flag (`forOffline`: Link-Neutralisierung + `<img>`-Entfernung), `OpenInBrowserAsync`-Offline-Guard + try/catch | Offen | — |
| 20 | UI | `ArticleDetailPage.xaml`: `Navigating="OnWebViewNavigating"` am `ArticleWebView`, `ErrorMessage`-Label und `ArticleOfflineLinksDisabled`-Hinweis-Label mit `DataTrigger` `IsOnline == False` ergänzen | Offen | — |
| 21 | UI | `ArticleDetailPage.xaml.cs`: `OnWebViewNavigating`-Handler (offline → `e.Cancel = true` + lokalisierter `DisplayAlertAsync`), `OnAppearing` ruft `_viewModel.AttachConnectivity()`, `OnDisappearing` ruft `_viewModel.DetachConnectivity()` | Offen | — |
| 22 | UI | `UnreadPage.xaml`: `DataTrigger` `IsOnline == False` am Sync-Button (Opacity/Stroke-Dimmung), `OfflineHint`-Label, `ErrorMessage`-Label ergänzen; `IsOnline="{Binding BindingContext.IsOnline, Source={x:Reference PageRoot}}"` am `ArticleCardView` binden | Offen | — |
| 23 | UI | `FeedsPage.xaml`: persistenter `OfflineHint`-`Border` als Seiten-Banner unterhalb der Eingabekarte (`Grid.Row="0"`, oberhalb der Feed-Liste) mit `DataTrigger` `IsOnline == False` nach `NotificationsIosOnlyHint`-Muster | Offen | — |
| 24 | UI | `ArticleCardView`: `IsOnline`-`BindableProperty` (Standard `true`) in `.xaml.cs` anlegen; `DataTrigger` `IsOnline == False` → `IsVisible = false` am Thumbnail-`Border` (Z. 66–78, `Source={x:Reference Card}`) | Offen | — |
| 25 | UI | `LaterPage.xaml`: `IsOnline="{Binding BindingContext.IsOnline, Source={x:Reference PageRoot}}"` am `ArticleCardView` binden | Offen | — |
| 26 | Tests | `FakeNetworkStatusService` in `src/Reporter.Tests/` anlegen (setzbares `IsOnline`, `RaiseConnectivityChanged()`) | Offen | — |
| 27 | Tests | Konstruktor-Aufrufstellen anpassen: `UnreadViewModelTests` (Z. 30), `FeedsViewModelTests` (Z. 40), `LaterViewModelTests` (Z. 23), `AutoRefreshServiceTests` (Z. 28), `FeedSyncServiceTests` (Z. 71, 78) | Offen | — |
| 28 | Tests | `UnreadViewModelTests`: `RefreshCommand_WhenOffline_SetsOfflineHintAndSkipsSync`, `ConnectivityChanged_UpdatesIsOnline`, `RefreshCommand_WhenBackOnline_InvokesSyncService` | Offen | — |
| 29 | Tests | `FeedsViewModelTests`: `RefreshAllCommand_WhenOffline_SetsOfflineHintAndSkipsSync`, `RefreshCommand_WhenOffline_SetsOfflineHintAndSkipsSync`, `ConnectivityChanged_UpdatesIsOnline` | Offen | — |
| 30 | Tests | `LaterViewModelTests`: `ConnectivityChanged_UpdatesIsOnline` | Offen | — |
| 31 | Tests | `AutoRefreshServiceTests`: `Tick_WhenOffline_SkipsSyncAll`, `Tick_WhenBackOnline_ResumesSync` | Offen | — |
| 32 | Tests | `FeedSyncServiceTests`: `SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog`, `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange` | Offen | — |
| 33 | Tests | `ArticleHtmlSanitizerTests` neu: `Sanitize_RemovesScriptAndEventHandlers`, `Sanitize_Online_KeepsAnchorAndImgTags`, `Sanitize_Offline_NeutralizesAnchorTags_KeepsText`, `Sanitize_Offline_RemovesImgTags`, `Sanitize_NullOrWhitespace_ReturnsInput` | Offen | — |
| 34 | E2E-Tests | Manuelle Verifikation: App komplett offline starten, alle Lesepfade prüfen (Ungelesen/Später/Kategorien/Feeds/Artikeldetail), Listen-Thumbnails offline ausgeblendet — Screenshot, `test-results.md` | Offen | — |
| 35 | E2E-Tests | Manuelle Verifikation: Offline-Status am Sync-Button (Ungelesen), Sync-Tap + Pull-to-Refresh → lokalisierter Hinweis, kein `SyncLog`-Rauschen — Screenshot, `test-results.md` | Offen | — |
| 36 | E2E-Tests | Manuelle Verifikation: WebView-Links offline nicht klickbar/neutralisiert, Rest-Navigation → lokalisierter Alert — Screenshot, `test-results.md` | Offen | — |
| 37 | E2E-Tests | Manuelle Verifikation: Artikeldetail offline ohne externe `<img>`-Bilder (entfernt, keine leeren Platzhalter), Text vollständig lesbar — Screenshot, `test-results.md` | Offen | — |
| 38 | E2E-Tests | Manuelle Verifikation: Online↔Offline-Wechsel zur Laufzeit ohne Neustart (Status + Link-Verhalten folgen) | Offen | — |
| 39 | E2E-Tests | Manuelle Verifikation: Systemsprache DE → alle Texte deutsch; EN → englisch; Drittsprache → EN-Fallback — Screenshots, `test-results.md` | Offen | — |
| 40 | E2E-Tests | Manuelle Verifikation: Feeds-Pull-to-Refresh und Einzel-Sync offline → lokalisierter Hinweis, kein Absturz | Offen | — |
| 41 | E2E-Tests | Manuelle Verifikation: Feeds-Seite zeigt offline ohne Nutzeraktion den `OfflineHint`-Banner oberhalb der Liste; Banner verschwindet nach Netzrückkehr — Screenshot, `test-results.md` | Offen | — |
| 42 | E2E-Tests | Manuelle Verifikation: Artikeldetail offline → „Im Browser öffnen" (Footer + Aktionsleiste) → lokalisierter Hinweis im `ErrorMessage`-Label statt Browser-Öffnung, kein Absturz; `ArticleOfflineLinksDisabled`-Label sichtbar — Screenshot, `test-results.md` | Offen | — |
| 43 | Abschluss | `.\scripts\Run-StaticChecks.ps1`, `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release`, `translation-check.py` — alle grün; UI-Verifikation in `test-results.md`/`docs/help/anwendung/mobile-ui-design.md` dokumentiert (390×844 pt Windows, ggf. iOS-Simulator) | Offen | — |
