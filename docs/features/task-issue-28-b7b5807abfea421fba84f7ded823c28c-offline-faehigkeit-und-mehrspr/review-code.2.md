# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedsViewModel.cs / UnreadViewModel.cs (FeedsViewModel, UnreadViewModel)

- **Fehlerbehandlung / inkonsistentes Verhalten** — Die Offline-/`SyncError`-Meldungen werden bei Wiederkehr der Verbindung nicht zurückgesetzt: `FeedsViewModel.RefreshAsync`/`RefreshAllAsync` (Zeilen 349–353, 385–389) setzen `SyncErrorMessage = AppResources.OfflineHint`, `UnreadViewModel.RefreshAsync` (Zeilen 325–329) setzt `ErrorMessage = AppResources.OfflineHint` — aber keines der beiden ViewModels überschreibt `OnConnectivityChanged(bool)`. `ArticleDetailViewModel` leert `ErrorMessage` im Hook explizit (Zeile 349, Fix aus Runde 1) — hier bleibt „Keine Internetverbindung." stehen, obwohl das Gerät wieder online ist und das `IsOnline`-getriggerte Offline-Banner bereits ausgeblendet wurde. Gleiches gilt für einen zuletzt angezeigten Sync-Fehler nach Statuswechsel.

  Empfehlung: In `FeedsViewModel` und `UnreadViewModel` `OnConnectivityChanged(bool)` überschreiben und `SyncErrorMessage` bzw. `ErrorMessage` bei Statuswechsel leeren (analog `ArticleDetailViewModel`), sodass das Verhalten über die ViewModels konsistent ist.

### FeedsViewModel.cs (FeedsViewModel)

- **Fehlerbehandlung** — `SyncErrorMessage = ex.Message` (Zeilen 368 und 404) zeigt rohe, nicht lokalisierte Exception-Meldungen im neuen Sync-Fehler-Label — dieselbe Befundklasse, die Runde 1 in `ArticleDetailViewModel.OpenInBrowserAsync` beanstandet und dort mit dem lokalisierten Schlüssel `ErrorOpenInBrowserFailed` behoben wurde. Die Zeilen wurden in diesem Branch angefasst (Property umbenannt), das Muster aber nicht angeglichen. Dasselbe Muster liegt außerdem (bereits vorbestehend) in `UnreadViewModel` Zeilen 315 und 344 vor.

  Empfehlung: Statt `ex.Message` eine lokalisierte generische Meldung anzeigen (z. B. `AppResources.SyncStatusError`) und die Exception per `Debug.WriteLine` protokollieren; das Vorgehen aus `OpenInBrowserAsync` übernehmen.

### SettingsViewModel.cs (SettingsViewModel)

- **Klassischer Code Smell (falscher Initialzustand geerbter Property)** — `SettingsViewModel` erbt `IsOnline` von `BaseViewModel`, ruft aber weder `InitConnectivity` noch `TrackConnectivity` auf — die öffentliche Property ist damit dauerhaft `false` (= offline), egal wie der tatsächliche Netzwerkzustand ist. Aktuell bindet `SettingsPage.xaml` die Property nicht, aber jede spätere Verwendung zeigt unbemerkt einen falschen Offline-Zustand; `CategoriesViewModel` (weiterhin `ObservableObject`) zeigt, dass die Codebasis VMs ohne Connectivity-Bedarf nicht an `BaseViewModel` bindet.

  Empfehlung: Entweder im Konstruktor `InitConnectivity`/`TrackConnectivity` aufrufen (benötigt `INetworkStatusService`-Dependency) oder — wenn kein Connectivity-Bedarf besteht — auf `ObservableObject` zurückstellen wie `CategoriesViewModel`.

### BaseViewModelTests_Connectivity.cs (BaseViewModelTests_Connectivity)

- **Namenskonvention** — Der Testklassenname `BaseViewModelTests_Connectivity` enthält einen Unterstrich und weicht vom durchgängigen PascalCase-Schema aller anderen Testklassen ab (`FeedsViewModelTests`, `AutoRefreshServiceTests`, `ArticleHtmlSanitizerTests` …).

  Empfehlung: Umbenennen in `BaseViewModelConnectivityTests` (oder `BaseViewModelTests` mit geschachtelter `Connectivity`-Klasse).

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Reporter.Core/Interfaces/INetworkStatusService.cs`
- `src/Reporter.Core/Services/ArticleHtmlSanitizer.cs`
- `src/Reporter.Core/Services/WebViewNavigationGuard.cs`
- `src/Reporter.Core/Services/AutoRefreshService.cs`
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/ViewModels/BaseViewModel.cs`
- `src/Reporter.Core/ViewModels/CategoriesViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/LaterViewModel.cs`
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs`
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs` (nur Vererbungs-Kontext)
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs`
- `src/Reporter/Services/NetworkStatusService.cs`
- `src/Reporter/ViewModels/ArticleDetailViewModel.cs`
- `src/Reporter/Views/ArticleCardView.xaml` (+ `.xaml.cs`)
- `src/Reporter/Views/ArticleDetailPage.xaml` (+ `.xaml.cs`)
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/LaterPage.xaml`
- `src/Reporter/Views/UnreadPage.xaml`
- `src/Reporter/App.xaml.cs`
- `src/Reporter/MauiProgram.cs`
- `src/Reporter.Tests/FakeNetworkStatusService.cs`
- `src/Reporter.Tests/ArticleHtmlSanitizerTests.cs`
- `src/Reporter.Tests/BaseViewModelTests_Connectivity.cs`
- `src/Reporter.Tests/WebViewNavigationGuardTests.cs`
- `src/Reporter.Tests/AutoRefreshServiceTests.cs`
- `src/Reporter.Tests/FeedSyncServiceTests.cs`
- `src/Reporter.Tests/FeedsViewModelTests.cs`
- `src/Reporter.Tests/LaterViewModelTests.cs`
- `src/Reporter.Tests/UnreadViewModelTests.cs`
- `test-results.md`

**Verifikation der Runde-1-Fixes (alle korrekt und vollständig):**

1. `OnWebViewNavigating` (`ArticleDetailPage.xaml.cs` Zeilen 70–79) bricht nur noch http/https-Navigationen via `WebViewNavigationGuard.IsExternalUrl` ab; lokale `HtmlWebViewSource`-Loads (`about:blank`, `data:`, `file:`) passieren — der ursprüngliche Blocker ist behoben.
2. `ArticleDetailViewModel`: `ErrorMessage` wird in `OnConnectivityChanged` (Zeile 349) und vor dem Browser-Aufruf (Zeile 515) geleert; der `catch` nutzt den lokalisierten `ErrorOpenInBrowserFailed`-Schlüssel + `Debug.WriteLine`.
3. `FeedsViewModel` hat getrennte `SyncErrorMessage`/`HasSyncError` (Zeilen 145–160); das Label in `FeedsPage.xaml` (Zeilen 87–90) liegt außerhalb der Eingabekarte, unter dem Offline-Banner. `ErrorMessage` bleibt der Formular-Validierung vorbehalten.
4. `BaseViewModel` kapselt das Connectivity-Muster (`InitConnectivity`, `TrackConnectivity` ×2, `UntrackConnectivity`, `RefreshConnectivityStatus`, virtueller `OnConnectivityChanged(bool)`-Hook). `IsOnline`-Initialwert kommt aus dem Service; `AttachConnectivity`/`DetachConnectivity` in `ArticleDetailViewModel` sind über `_isConnectivityTracked` idempotent und korrekt an `OnAppearing`/`OnDisappearing` verdrahtet; UI-Threading bleibt über `NetworkStatusService` (`MainThread.BeginInvokeOnMainThread`) gewahrt.
5. `ArticleHtmlSanitizer`: `DangerousElementRegex` entfernt komplette `applet`/`audio`/`video`-Elemente inkl. Inhalt (Backreference `\1`), danach räumt `DangerousTagsRegex` verwaiste öffnende/schließende Tags auf — Reihenfolge korrekt, keine legitimen Inhalte betroffen (durch Tests abgedeckt, 227/227 Tests grün lokal).

**Geprüfte Event-/Command-Verdrahtung (ohne Befund):** Singleton-ViewModels (`Unread`, `Feeds`, `Later`) abonnieren das Singleton-`NetworkStatusService.ConnectivityChanged` für die App-Lebensdauer → kein Leak; transientes `ArticleDetailViewModel` meldet via `AttachConnectivity`/`DetachConnectivity` an/ab; `Navigating="OnWebViewNavigating"` in `ArticleDetailPage.xaml` verdrahtet; alle `DataTrigger`/`x:Reference`-Bindings (`Card`, `PageRoot`) und `x:Static`-ResX-Referenzen lösen auf; alle 18 neuen ResX-Keys existieren in beiden Sprachen und im Designer.
