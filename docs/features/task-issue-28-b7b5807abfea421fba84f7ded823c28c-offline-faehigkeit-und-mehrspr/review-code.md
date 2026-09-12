# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### UnreadViewModel.cs (UnreadViewModel)

- **Fehlerbehandlung / funktionaler Defekt** — `RefreshAsync` (Zeilen 325–330) bricht bei `!IsOnline` ab, ohne `IsSyncing` zurückzusetzen. `RefreshView.IsRefreshing` (`UnreadPage.xaml` Zeile 132) ist `BindingMode.TwoWay`: Bei Pull-to-Refresh schreibt die Bindung `IsSyncing = true` in das ViewModel, BEVOR `RefreshView.Command` ausgeführt wird (MAUI: `BindableObject.SetValueActual` ruft `binding.Apply(fromTarget: true)` vor dem `propertyChanged`-Callback auf, der das Command startet). Der Early-Return lässt `IsSyncing` dauerhaft `true` → endloser Refresh-Spinner; auch `OnConnectivityChanged` (Zeilen 452–456) setzt den Zustand bei Wiederkehr des Netzes nicht zurück, die Seite bleibt „eingefroren". Regression gegenüber HEAD: Der alte Code erreichte immer den `finally { IsSyncing = false; }`-Pfad.

  Empfehlung: Im Offline-Frühreturn `IsSyncing = false` setzen (bzw. den `!IsOnline`-Check so strukturieren, dass ein von der Bindung vorbelegtes `IsSyncing` wieder zurückgenommen wird). Keine `ErrorMessage` setzen — `RefreshCommand_WhenOffline_SkipsSyncWithoutError` verlangt explizit ein stilles Überspringen; das Offline-Banner kommuniziert den Zustand bereits.

- **Fehlerbehandlung** — `ErrorMessage = syncError` (Zeile 355) wird nach `await LoadAsync()` bedingungslos zugewiesen. Setzt `LoadPageAsync` zuvor `ErrorLoadFailed` (Zeile 317) und ist `syncError` leer (Sync erfolgreich), wird die Lade-Fehlermeldung wieder gelöscht — der Ladefehler wird still geschluckt.

  Empfehlung: Nur bei nicht-leerem `syncError` zuweisen, z. B. `if (syncError.Length > 0) { ErrorMessage = syncError; }`.

- **Fehlerbehandlung (gering)** — `OnConnectivityChanged` (Zeilen 452–456) leert `ErrorMessage` bei jedem Konnektivitätswechsel in beide Richtungen. Da `ErrorMessage` der einzige Kanal für Sync- UND Ladefehler ist, wird auch ein legitimer `ErrorLoadFailed` (Repository-/DB-Fehler, nicht konnektivitätsbedingt) beim nächsten Statuswechsel kommentarlos entfernt. `FeedsViewModel` zeigt die sauberere Variante: Dort wird nur der eigene `SyncErrorMessage`-Kanal geleert, `ErrorMessage` bleibt erhalten.

  Empfehlung: Entweder analog zu `FeedsViewModel` einen getrennten Sync-Fehler-Kanal einführen oder das Leeren auf bekannte Konnektivitäts-/Sync-Meldungen beschränken.

### FeedsViewModel.cs (FeedsViewModel)

- **Fehlerbehandlung / funktionaler Defekt** — `RefreshAllAsync` (Zeilen 379–413) ist über seinen einzigen Aufrufer (`RefreshView.Command` = `RefreshAllCommand`, `FeedsPage.xaml` Zeilen 93–95) zur Laufzeit unerreichbar: Die TwoWay-`IsRefreshing`-Bindung setzt `IsSyncing = true`, bevor das Command ausgeführt wird, sodass `if (IsSyncing) return;` (Zeilen 381–384) bei JEDER Pull-Geste greift — auch online. Folgen: Die Synchronisation läuft nie an, `IsSyncing`/`IsRefreshing` bleiben `true` → endloser Spinner, und `RefreshAllCommand` sowie `RefreshCommand` (`CanExecute = !IsSyncing`, Zeilen 56–57) bleiben dauerhaft deaktiviert — auch nach Wiederkehr des Netzes, da `OnConnectivityChanged` (Zeilen 416–419) `IsSyncing` nicht zurücksetzt. Der `IsSyncing`-Guard ist vorbestehend, liegt aber in der im Branch geänderten Methode; der neue `!IsOnline`-Frühreturn (Zeilen 386–389) folgt demselben Leckmuster (kein `IsSyncing = false`) und wäre nach einer Guard-Korrektur der nächste hängende Pfad. Der Unit-Test `RefreshAllCommand_WhenOffline_SkipsSyncWithoutError` deckt das nicht ab, weil er das Command direkt ohne die `IsRefreshing`-Bindung aufruft.

  Empfehlung: Die Frühreturns so umbauen, dass die von der Bindung vorbelegte `IsSyncing`-Property die Ausführung nicht verhindert und in jedem Abbruchpfad zurückgesetzt wird — z. B. den `IsSyncing`-Guard entfernen bzw. auf ein nicht an die View gebundenes Laufzeitflag umstellen und im `!IsOnline`-Pfad `IsSyncing = false` setzen.

- **Doppelter Code** — `RefreshAsync` (Zeilen 343–377) und `RefreshAllAsync` (Zeilen 379–413) bestehen aus einem nahezu identischen ~30-zeiligen Ablauf (`IsSyncing`-Guard → `!IsOnline`-Guard → `IsSyncing = true` + `SyncErrorMessage` leeren → try/catch/finally mit `Debug.WriteLine`/`SyncStatusError` → `LoadAsync()`); einzige Unterschiede sind `SyncFeedAsync(feed.Id)` vs. `SyncAllAsync()` und der Feed-Null-Check. Der Branch hat das Duplikat mit den identischen Offline-Guards weiter vertieft.

  Empfehlung: Den gemeinsamen Ablauf in eine private Hilfsmethode auslagern (z. B. `SyncAsync(Func<Task<SyncResult>>)`), die beide Methoden mit dem jeweiligen Service-Aufruf parametrisieren.

## Verifikationen (Runde 3)

**Technische Verifikation des Usability-Befunds „IsSyncing bleibt hängen" — bestätigt und auf FeedsPage verschärft:**

- `RefreshView.IsRefreshing` ist `BindingMode.TwoWay` (MAUI-Quelle, `RefreshView.cs`): Der Handler setzt `VirtualView.IsRefreshing = true`; `BindableObject.SetValueActual` wendet die Bindung zuerst target→source an (`binding.Apply(fromTarget: true)` → `IsSyncing = true` im VM) und ruft danach `OnIsRefreshingPropertyChanged` auf, das `Command?.Execute(...)` ausführt. `AsyncRelayCommand.Execute` prüft `CanExecute` nicht (CommunityToolkit-Quelle: `ExecuteAsync` ruft `this.execute()` unbedingt auf).
- UnreadPage: `RefreshAsync` hat keinen `IsSyncing`-Guard → online funktioniert Pull-to-Refresh (Sync läuft, `finally` setzt `IsSyncing = false`), offline hängt der Spinner — Usability-Befund exakt bestätigt.
- FeedsPage: `if (IsSyncing) return;` (Zeile 381) wird durch das bindungsgesetzte `IsSyncing` bereits vor dem `!IsOnline`-Guard ausgelöst → Pull-to-Refresh ist auch online ein No-Op mit hängendem Spinner; der Usability-Befund ist damit größer als berichtet.

**Verifikation der Iteration-3-Fixes aus Runde 2 (soweit ohne neuen Befund):**

1. `OnConnectivityChanged`-Overrides vorhanden; `FeedsViewModel` leert gezielt nur `SyncErrorMessage` (legitime `ErrorMessage`/Formular-Validierung bleibt erhalten); für `UnreadViewModel` siehe Befund oben (ein Kanal).
2. `RefreshAsync`-Restrukturierung in `UnreadViewModel`: `syncError`-Sammelvariable + Zuweisung nach `LoadAsync()` ist race-frei (UI-Thread-Sequenz, kein paralleler Zugriff); Restproblem ist nur das bedingungslose Überschreiben (siehe Befund).
3. `SettingsViewModel` wieder auf `ObservableObject` mit eigenen `HasError`/`ErrorMessage`-Properties; kein Connectivity-Verhalten verloren (wurde vorher nie initialisiert).
4. `SyncStatusError`/`ErrorLoadFailed`/`OfflineHint`/`LabelPullToRefresh` in `AppResources.resx`, `AppResources.de.resx` und Designer vorhanden; `Debug.WriteLine` statt `ex.Message` in allen neuen catch-Blöcken.
5. Offline-Frühreturns setzen konsistent keine Fehlermeldung (`…_SkipsSyncWithoutError`-Tests grün).
6. `LaterPage.xaml`-Banner: `DataTrigger` auf `IsOnline` korrekt (erbt `PageRoot`-BindingContext), `AppThemeBinding` für alle Farben, kein Layout-Bruch (`Auto`-Zeile + Stack).
7. Neue Tests vorhanden und aussagekräftig (`RefreshAllCommand_WhenSyncThrows_SetsLocalizedSyncError`, `ConnectivityChanged_ClearsSyncErrorMessage`, `ConnectivityChanged_ClearsErrorMessage`, `…_UpdatesIsOnline`, `…_WhenBackOnline_InvokesSyncService`); Fake-Erweiterungen `NextException`/`NextResult` in den geschachtelten `FakeFeedSyncService`-Klassen korrekt.
8. `ArticleHtmlSanitizer`-Pipeline: `DangerousElementRegex` (komplette `applet`/`audio`/`video`-Elemente via Backreference `\1`) vor `DangerousTagsRegex` (verwaiste Einzeltags inkl. `link`/`meta`/`base`) — Reihenfolge und Vollständigkeit korrekt.
9. Event-Verdrahtung/Leaks/Threading unauffällig: `BaseViewModel`-Guards (`_isConnectivityTracked`) idempotent; `AttachConnectivity`/`DetachConnectivity` an `OnAppearing`/`OnDisappearing` verdrahtet; Singleton-VMs am Singleton-`NetworkStatusService` (kein Leak); `MainThread.BeginInvokeOnMainThread` marshal auf UI-Thread; `Navigating="OnWebViewNavigating"` verdrahtet; DI-Registrierung `INetworkStatusService` vor den VMs; alle Konstruktor-Signaturen in Tests und DI konsistent.
10. Build sauber (0 Warnungen), 231/231 Tests grün.

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
- `src/Reporter.Core/ViewModels/SettingsViewModel.cs`
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
- `src/Reporter.Tests/FakeFeedSyncService.cs`
- `src/Reporter.Tests/ArticleHtmlSanitizerTests.cs`
- `src/Reporter.Tests/BaseViewModelConnectivityTests.cs`
- `src/Reporter.Tests/WebViewNavigationGuardTests.cs`
- `src/Reporter.Tests/AutoRefreshServiceTests.cs`
- `src/Reporter.Tests/FeedSyncServiceTests.cs`
- `src/Reporter.Tests/FeedsViewModelTests.cs`
- `src/Reporter.Tests/LaterViewModelTests.cs`
- `src/Reporter.Tests/UnreadViewModelTests.cs`
