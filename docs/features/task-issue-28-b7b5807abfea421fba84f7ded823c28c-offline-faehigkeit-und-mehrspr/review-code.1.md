# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### ArticleDetailPage.xaml.cs (ArticleDetailPage)

- **Fehlerbehandlung / Event-Verdrahtung** — `OnWebViewNavigating` (Zeilen 69–78) bricht im Offline-Zustand **jede** WebView-Navigation ab (`e.Cancel = true`) und zeigt ungeprüft einen Alert. Der Handler unterscheidet nicht zwischen dem initialen `HtmlWebViewSource`-Load und einer echten Link-Navigation: Weder `e.Url` noch `e.NavigationEvent` werden ausgewertet. Auf iOS feuert `WKWebView.decidePolicyForNavigationAction` auch für `LoadHtmlString` (`about:blank`), auf Windows feuert WebView2 `NavigationStarting` auch für `NavigateToString` — die App zielt auf beide Plattformen. Folge: Der lokal gespeicherte Artikel wird offline gar nicht gerendert und es erscheint ein Hinweisdialog ohne Benutzeraktion. Derselbe Abbruch trifft auch den Reload aus `ArticleDetailViewModel.OnConnectivityChanged` → `RebuildHtml()` beim Wechsel online → offline, sodass die offline-bereinigte HTML-Variante nie angezeigt wird. Damit ist der Usability-Befund 1 aus `review-usability.md` technisch bestätigt.

  Empfehlung: Nur echte externe Navigationen abbrechen — z. B. `e.Cancel = true` und Alert nur, wenn `e.Url` mit `http`/`https` beginnt (lokaler Load meldet `about:blank` bzw. `data:`), alternativ `e.NavigationEvent` auswerten. Manuell auf iOS und Windows im Offline-Zustand verifizieren.

### ArticleDetailViewModel.cs (ArticleDetailViewModel)

- **Fehlerbehandlung** — `ErrorMessage` wird nie zurückgesetzt: `OpenInBrowserAsync` (Zeilen 530–552) setzt offline `ErrorMessage = AppResources.OfflineHint`, aber weder `OnConnectivityChanged` (Zeilen 524–528, setzt nur `IsOnline` + `RebuildHtml`) noch ein erfolgreicher `Browser.OpenAsync`-Aufruf leeren die Meldung wieder. Die rote Fehlermeldung bleibt also stehen, obwohl das Gerät längst wieder online ist. Zusätzlich wird im `catch`-Zweig `ErrorMessage = ex.Message` gesetzt — eine rohe, nicht lokalisierte Exception-Meldung, die die gerade eingeführte Mehrsprachigkeit unterläuft. Usability-Befund 2 aus `review-usability.md` technisch bestätigt.

  Empfehlung: `ErrorMessage = string.Empty` in `OnConnectivityChanged` und nach erfolgreichem `Browser.OpenAsync` setzen; im `catch` eine lokalisierte generische Meldung (z. B. neuer Schlüssel `ErrorOpenInBrowserFailed`) anzeigen und `ex` nur ins Debug-Log schreiben.

### FeedsViewModel.cs (FeedsViewModel)

- **Kopplung / Vermischte Verantwortlichkeiten** — `RefreshAsync` (Zeilen 345–349) und `RefreshAllAsync` (Zeilen 381–385) schreiben den Offline-/Sync-Fehler in dieselbe `ErrorMessage`-Property wie die Formular-Validierung in `SaveAsync` (`ErrorFeedUrlInvalid`, `ErrorFeedTitleEmpty`, `ErrorFeedDuplicate`). Das `ErrorMessage`-Label liegt in `FeedsPage.xaml` (Zeilen 65–68) **innerhalb** der Eingabekarte — ein Sync-Fehler wird damit als Formularfehler dargestellt. Usability-Befund 3 aus `review-usability.md` technisch bestätigt.

  Empfehlung: Sync-/Refresh-Fehler über eine separate Property (z. B. `SyncErrorMessage`) führen und in `FeedsPage.xaml` außerhalb der Eingabekarte (unter dem Offline-Banner, über der Liste) rendern; `ErrorMessage` der Formular-Validierung vorbehalten.

### UnreadViewModel.cs / FeedsViewModel.cs / LaterViewModel.cs / ArticleDetailViewModel.cs (ViewModels)

- **Doppelter Code** — Das Muster `_isOnline`-Feld + Initialisierung aus `networkStatusService.IsOnline` + `ConnectivityChanged`-Abo + `IsOnline`-Property + `OnConnectivityChanged`-Handler ist in `UnreadViewModel` (Zeilen 29, 50–51, 152–156, 455–458), `FeedsViewModel` (Zeilen 26, 51–52, 148–152, 333–336) und `LaterViewModel` (Zeilen 17, 26–28, 62–65, 77–80) nahezu identisch wiederholt; `ArticleDetailViewModel` enthält eine Variante davon (Attach/Detach). Die Basisklasse `BaseViewModel` ist derzeit leer.

  Empfehlung: Das Connectivity-Muster in `BaseViewModel` kapseln (z. B. `protected void TrackConnectivity(INetworkStatusService service)` plus `protected bool IsOnline`/`protected virtual void OnConnectivityChanged()`), sodass jede abgeleitete Klasse nur noch die spezifische Reaktion (z. B. `RebuildHtml()`) implementiert.

### ArticleHtmlSanitizer.cs (ArticleHtmlSanitizer)

- **Fehlerbehandlung / unvollständige Bereinigung** — `DangerousTagsRegex` (Zeile 16) entfernt nur die öffnenden Tags von `applet`, `audio` und `video`; die zugehörigen schließenden Tags (`</audio>`, `</video>`, `</applet>`) verbleiben als verwaistes Markup im Ausgabe-HTML. Konsistent wäre das Vorgehen der anderen Regexes (`.*?</tag>`).

  Empfehlung: Für Elemente mit schließendem Tag ein Paar-Regex wie bei `ScriptTagRegex`/`FormTagRegex` verwenden (`<(applet|audio|video)[^>]*>.*?</\1\s*>`) bzw. schließende Tags zusätzlich entfernen.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `src/Reporter.Core/Interfaces/INetworkStatusService.cs`
- `src/Reporter.Core/Services/ArticleHtmlSanitizer.cs`
- `src/Reporter.Core/Services/AutoRefreshService.cs`
- `src/Reporter.Core/Services/FeedSyncService.cs`
- `src/Reporter.Core/ViewModels/CategoriesViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/LaterViewModel.cs`
- `src/Reporter.Core/ViewModels/UnreadViewModel.cs`
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
- `src/Reporter.Tests/AutoRefreshServiceTests.cs`
- `src/Reporter.Tests/FeedSyncServiceTests.cs`
- `src/Reporter.Tests/FeedsViewModelTests.cs`
- `src/Reporter.Tests/LaterViewModelTests.cs`
- `src/Reporter.Tests/UnreadViewModelTests.cs`
- `test-results.md`

Geprüfte Event-/Command-Verdrahtung (ohne Befund): `ConnectivityChanged`-Abos der Singleton-ViewModels (`Unread`, `Feeds`, `Later`) haben dieselbe Lebensdauer wie der Singleton-`NetworkStatusService` → kein Leak; das transiente `ArticleDetailViewModel` meldet sich über `AttachConnectivity`/`DetachConnectivity` in `OnAppearing`/`OnDisappearing` korrekt an/ab; `NetworkStatusService` marshalt das Event per `MainThread.BeginInvokeOnMainThread` auf den UI-Thread; `Navigating` ist in `ArticleDetailPage.xaml` verdrahtet; alle neuen `DataTrigger`-Bindings (`IsOnline`, `x:Reference Card`/`PageRoot`) und `x:Static`-Ressourcen sind korrekt aufgelöst.
