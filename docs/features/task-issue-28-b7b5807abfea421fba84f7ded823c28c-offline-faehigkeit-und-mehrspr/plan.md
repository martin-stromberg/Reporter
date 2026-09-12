# Umsetzungsplan: Offline-Fähigkeit und Mehrsprachigkeit (EN/DE) — Issue #28

## Übersicht

Die App soll ohne Netzwerkverbindung vollständig lesbar bleiben und den Offline-Zustand sichtbar machen: Ein neuer `INetworkStatusService` (Gateway-Muster) erkennt die Konnektivität, die Refresh-Einstiegspunkte (`UnreadViewModel`, `FeedsViewModel`, `AutoRefreshService`, `FeedSyncService`) brechen offline sauber ab statt Fehler zu produzieren, und `UnreadPage` wie `FeedsPage` zeigen einen dauerhaft sichtbaren Offline-Indikator (Anwenderentscheidung). Im `ArticleWebView` werden offline Links deaktiviert sowie externe `<img>`-Bilder aus dem Artikel-HTML entfernt; konsistent dazu blendet `ArticleCardView` seine Listen-Thumbnails offline aus. Parallel wird die ResX-Lokalisierung vervollständigt: Alle verbleibenden hartcodierten UI-Texte (`ArticleDetailPage.xaml`, `ArticleDetailViewModel.cs`, `CategoriesViewModel.cs`) wandern in `AppResources.resx` (EN, neutral/Fallback) und `AppResources.de.resx` (DE); die Sprache folgt der Systemsprache (`CurrentUICulture`). Ein manueller Sprachwechsel ist bewusst nicht Teil dieses Plans — er wurde mit dem Anwender abgestimmt auf ein separates Folge-Issue zurückgestellt (kein `Settings.Language`, kein Sprach-Picker, keine Migration).

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| `INetworkStatusService` | Gateway-Muster: Interface in `Reporter.Core/Interfaces/`, Implementierung `NetworkStatusService` in `src/Reporter/Services/` über `Connectivity.Current`, `AddSingleton` in `MauiProgram` | Entspricht exakt dem etablierten Muster von `IAppThemeService`/`AppThemeService` und `ILocalNotificationService`/`LocalNotificationService`; in `Reporter.Tests` über handgeschriebenes Fake ersetzbar. Kein eigenes Status-Enum nötig — `bool IsOnline` genügt der Anforderung (es gibt nur online/offline). |
| Offline-Behandlung der Refresh-Commands | Frühabbruch mit lokalisiertem `ErrorMessage`-Hinweis (`AppResources.OfflineHint`) statt `CanExecute`-Sperrung; zusätzlich `IsOnline`-Property + `DataTrigger` für den visuellen Offline-Zustand am Button | Die Anforderung lässt beide Varianten zu. `RefreshView` (Pull-to-Refresh) auf `UnreadPage`/`FeedsPage` lässt sich nicht sauber sperren; ein erklärender Hinweis ist informativer als ein stumm deaktivierter Button. `CanExecute` bleibt wie bisher (`!IsSyncing` in `FeedsViewModel`, keine Bedingung in `UnreadViewModel`). |
| Offline-Link-Deaktivierung im WebView | Primär: `<a>`-Tags im Offline-Modus im HTML neutralisieren (durch ihren Text ersetzen). Ergänzend: `Navigating`-Handler in `ArticleDetailPage.xaml.cs` bricht Rest-Navigation mit `e.Cancel = true` ab und zeigt einen lokalisierten `DisplayAlertAsync`-Hinweis | Erfüllt beide Anforderungsvarianten („deaktiviert" / „durch Hinweis ersetzt") und das Akzeptanzkriterium „Links nicht klickbar" direkt; der `Navigating`-Handler deckt veraltete `HtmlSource`-Stände (Seite wurde online geladen, dann offline) ab. Online-Verhalten bleibt unverändert (interne Navigation wie bisher). |
| HTML-Aufbereitung | Neue statische Klasse `ArticleHtmlSanitizer` in `src/Reporter.Core/Services/`; `ArticleDetailViewModel.SanitizeHtml` delegiert | `ArticleDetailViewModel` liegt im MAUI-Projekt (`src/Reporter/ViewModels/`) und ist von `Reporter.Tests` nicht referenzierbar. Die reine String-Pipeline (bestehende Sanitize-Regexe + neue Offline-Eingriffe) gehört in `Reporter.Core`, damit der Offline-Pfad unit-testbar wird. |
| Externe `<img>`-Bilder offline | Im Offline-Modus werden `<img>`-Elemente vollständig aus dem Artikel-HTML entfernt (in `ArticleHtmlSanitizer`/`RebuildHtml`); online bleiben sie erhalten (CSP `img-src * data: blob:` unverändert) | Anwenderentscheidung (abweichend vom ursprünglichen Vorschlag „belassen"): vermeidet leere Bild-Platzhalter und Lücken im Offline-Layout; der Text bleibt vollständig lesbar. Umsetzung über denselben Offline-Schalter wie die Link-Neutralisierung (`forOffline`). |
| Event-Dispatching `ConnectivityChanged` | `NetworkStatusService` dispatcht `Connectivity.ConnectivityChanged` per `MainThread.BeginInvokeOnMainThread` auf den UI-Thread, bevor das eigene `ConnectivityChanged`-Event ausgelöst wird | MAUI kann das Event auf einem Hintergrund-Thread auslösen; `Reporter.Core` hat keinen MAUI-Bezug und die ViewModels aktualisieren UI-gebundene Properties. In Tests löst `FakeNetworkStatusService` synchron aus. |
| Sprachmechanismus | Neutrale `AppResources.resx` = Englisch = Fallback für alle Nicht-DE-Systems­sprachen; `AppResources.de.resx` = Deutsch; Auswahl automatisch über `CultureInfo.CurrentUICulture` durch den `ResourceManager` | Standard-.NET-ResX-Verhalten, kein eigener Code nötig. Die statische `Culture`-Eigenschaft im `AppResources.Designer.cs` bleibt ungenutzt, da der manuelle Sprachwechsel zurückgestellt ist. |
| `SyncLog`-Persistenz bei Offline | `FeedSyncService.SyncAllAsync`/`SyncFeedAsync` brechen offline früh ab, **ohne** `SyncLog`-Eintrag und **ohne** `HealthStatus`-Änderung | Offline ist kein Feed-Fehler; verhindert das im Inventory beschriebene Fehler-Rauschen in `sync_logs`. |
| Persistenter Offline-Indikator auf `FeedsPage` | Neuer `OfflineHint`-`Border` als Seiten-Banner: drittes Element des oberen `VerticalStackLayout` (unterhalb der Eingabekarte, `Grid.Row="0"`), `IsVisible` über `DataTrigger` `IsOnline == False` — Muster `NotificationsIosOnlyHint`-Border (`FeedsPage.xaml` Z. 51–64). Das bestehende `ErrorMessage`-Label (Z. 65–68) bleibt innerhalb der Eingabekarte | Anwenderentscheidung „Offline-Status auf UnreadPage **und** FeedsPage" verlangt einen ohne Nutzeraktion sichtbaren Indikator — der reaktive `ErrorMessage`-Hinweis allein genügt nicht. Das `ErrorMessage`-Label liegt innerhalb der Feed-Eingabekarte und ist der Formular-/Fehlerkanal; ein persistenter Seiten-Status gehört nicht in den Formular-Kontext, daher separate Positionierung oberhalb der Feed-Liste. |
| `ArticleCardView`-Thumbnails offline | Thumbnail-`Border` (`ArticleCardView.xaml` Z. 66–78) wird offline ausgeblendet: neues `BindableProperty IsOnline` (Standard `true`) auf der Card + `DataTrigger` `IsOnline == False` → `IsVisible = false`; gebunden von den Seiten über `{Binding BindingContext.IsOnline, Source={x:Reference PageRoot}}` (etabliertes Muster der Command-Bindungen). `LaterViewModel` erhält dafür `INetworkStatusService`/`IsOnline` analog `UnreadViewModel` | Konsistent mit der Anwenderentscheidung, `<img>`-Tags offline zu entfernen: Offline sollen keine leeren 80×80-Platzhalter-Rahmen sichtbar bleiben (saubere Offline-Darstellung). Die Alternative „belassen" würde leere Rahmen zeigen und widerspräche dieser Linie. |
| `ConnectivityChanged`-Lebensdauer `ArticleDetailViewModel` | Sichtbarkeitsgebundenes Abonnement statt Konstruktor-Abo + `IDisposable`: `AttachConnectivity()` in `ArticleDetailPage.OnAppearing`, `DetachConnectivity()` in `OnDisappearing` (beide idempotent; `Detach` enthält `CancelAutoMarkRead()`); `Attach` liest `IsOnline` neu und baut `HtmlSource` bei Statuswechsel neu auf | `OnDisappearing` feuert auch bei temporärer Überdeckung — ein nur im Konstruktor gelegtes Abo mit einmaliger `Dispose`-Abmeldung würde den `ConnectivityChanged`→`RebuildHtml`-Pfad danach still abschalten, obwohl das ViewModel weiterlebt. Entspricht dem bestehenden `SettingsPage`-Muster (Abo in `OnAppearing`, Abmeldung in `OnDisappearing`, `SettingsPage.xaml.cs` Z. 22–42). |

## Programmabläufe

### Netzwerkstatus erfassen und propagieren

1. `MauiProgram.CreateMauiApp` registriert `INetworkStatusService` → `NetworkStatusService` als Singleton (neben den bestehenden Gateway-Registrierungen, Zeilen 42–70).
2. `NetworkStatusService` liest im Getter `IsOnline` den Wert `Connectivity.Current.NetworkAccess == NetworkAccess.Internet`; der Konstruktor abonniert `Connectivity.ConnectivityChanged`.
3. Bei jeder Änderung dispatcht der Service per `MainThread.BeginInvokeOnMainThread` und löst `ConnectivityChanged` (eigenes Event, `EventHandler`) aus.
4. `App.OnStart` resolved `INetworkStatusService` im bestehenden try/catch-Muster, damit das Monitoring früh startet.
5. `UnreadViewModel`, `FeedsViewModel` und `LaterViewModel` (Singletons) erhalten den Service per Konstruktor-Injection, initialisieren `IsOnline` aus dem Service und abonnieren `ConnectivityChanged` im Konstruktor: Der Handler setzt `IsOnline` neu. `ArticleDetailViewModel` (transient) liest `IsOnline` im Konstruktor initial und verwaltet das Abonnement sichtbarkeitsgebunden über `AttachConnectivity()`/`DetachConnectivity()` (siehe Ablauf „Artikeldetail im Offline-Modus"); sein `ConnectivityChanged`-Handler aktualisiert `IsOnline` und ruft zusätzlich `RebuildHtml()` auf (Link-Neutralisierung und `<img>`-Entfernung folgen dem neuen Status).

Beteiligte Klassen/Komponenten: `INetworkStatusService`, `NetworkStatusService`, `MauiProgram`, `App`, `UnreadViewModel`, `FeedsViewModel`, `LaterViewModel`, `ArticleDetailViewModel`.

### Manueller Sync im Offline-Modus (Ungelesen-Seite)

1. Nutzer tippt den Sync-Button (`Border`+`Path`+`TapGestureRecognizer`, `UnreadPage.xaml` Zeilen 21–41) oder zieht Pull-to-Refresh (`RefreshView`, Zeilen 105–107) → `RefreshCommand` → `UnreadViewModel.RefreshAsync`.
2. `RefreshAsync` prüft `IsOnline` als Erstes: Bei Offline `ErrorMessage = AppResources.OfflineHint` und `return` — kein `IsSyncing`-Wechsel, kein `SyncAllAsync`-Aufruf, kein `LastSyncText`-Update, kein `LoadAsync`.
3. Die Seite zeigt den Hinweis über ein neues `ErrorMessage`-Label (Muster: `FeedsPage.xaml` Zeilen 65–68); der Sync-Button erhält einen `DataTrigger` auf `IsOnline == False` (Opacity reduziert, Muster: `IsAutoMarkReadAvailable`-Trigger `ArticleDetailPage.xaml` Zeilen 45–49 bzw. `NotificationsSupported`-Trigger `FeedsPage.xaml` Zeilen 33–37) sowie einen sichtbaren `OfflineHint`-Hinweis-Label (Muster: `NotificationsIosOnlyHint`-Border `FeedsPage.xaml` Zeilen 51–64).
4. Online-Pfad unverändert: `SyncAllAsync` → `FeedHealth.Error` → `ErrorMessage = result.Message ?? AppResources.SyncStatusError`.

Beteiligte Klassen/Komponenten: `UnreadViewModel`, `UnreadPage.xaml`, `INetworkStatusService`, `AppResources.OfflineHint`.

### Manueller Sync im Offline-Modus (Feeds-Seite)

1. Persistenter Indikator ohne Nutzeraktion: Neuer `OfflineHint`-`Border` als Seiten-Banner — drittes Element des oberen `VerticalStackLayout` (unterhalb der Eingabekarte `Border` Z. 15–72, `Grid.Row="0"`, damit oberhalb der Feed-Liste sichtbar); `IsVisible` über `DataTrigger` `Binding="{Binding IsOnline}"` `Value="False"` (Muster: `NotificationsIosOnlyHint`-Border `FeedsPage.xaml` Z. 51–64). Er erscheint dauerhaft, solange `IsOnline == false`, und verschwindet automatisch bei Netzrückkehr (`ConnectivityChanged`→`IsOnline`-Update im `FeedsViewModel`).
2. Pull-to-Refresh (`RefreshView` → `RefreshAllCommand`, `FeedsPage.xaml` Zeilen 75–77) oder Feed-Aktion „Aktualisieren" (`OnFeedTapped` → `DisplayActionSheetAsync` → `RefreshCommand`, `FeedsPage.xaml.cs` Zeilen 46–57).
3. `FeedsViewModel.RefreshAllAsync`/`RefreshAsync` prüfen `IsOnline` vor `IsSyncing = true`: Bei Offline `ErrorMessage = AppResources.OfflineHint` und `return`.
4. Das vorhandene `ErrorMessage`-Label (`FeedsPage.xaml` Z. 65–68) zeigt den lokalisierten Hinweis des Frühabbruchs — es bleibt bewusst innerhalb der Eingabekarte (reaktiver Formular-/Fehlerkanal); der persistente Status läuft getrennt über das Banner aus Schritt 1.

Beteiligte Klassen/Komponenten: `FeedsViewModel`, `FeedsPage.xaml`, `INetworkStatusService`, `AppResources.OfflineHint`.

### Hintergrund-Sync im Offline-Modus

1. `AutoRefreshService.RunLoopAsync` wartet auf den `PeriodicTimer`-Tick.
2. Pro Tick: `if (!_networkStatusService.IsOnline) { continue; }` — der Tick wird übersprungen, `SyncAllAsync` wird nicht aufgerufen, kein `SyncLog`-Eintrag entsteht.
3. Kommt das Netz zurück, synchronisiert der nächste Tick regulär (kein Sonderpfad nötig).

Beteiligte Klassen/Komponenten: `AutoRefreshService`, `INetworkStatusService`, `IFeedSyncService`.

### Defensiver Offline-Guard im Sync-Service

1. `FeedSyncService.SyncAllAsync` und `FeedSyncService.SyncFeedAsync` prüfen `IsOnline` vor jeder Arbeit.
2. Bei Offline: Rückgabe `new SyncResult(FeedHealth.Error, 0, AppResources.OfflineHint)` — **ohne** `SyncLog`-Anlage, **ohne** `HealthStatus`-Update, **ohne** HTTP-Aufruf.
3. Damit sind auch künftige Aufrufer abgesichert; die vorhandene generische Exception→`SyncResult(FeedHealth.Error)`-Abbildung (Zeilen 64–74) bleibt für echte Netzwerkfehler im Übergang (Online laut Status, Abruf schlägt dennoch fehl) bestehen — Crash-Freiheit ist weiterhin gegeben.

Beteiligte Klassen/Komponenten: `FeedSyncService`, `INetworkStatusService`, `SyncResult`, `ISyncLogRepository`.

### Artikeldetail im Offline-Modus

1. `ArticleDetailPage.ApplyQueryAttributes` → `ArticleDetailViewModel.LoadAsync` → `RebuildHtml`.
2. `RebuildHtml` ruft `ArticleHtmlSanitizer.Sanitize(content, forOffline: !IsOnline)`: Bei Offline werden `<a href="…">Text</a>` durch `Text` ersetzt (plus Entfernung verwaister `<a …>`/`</a>`-Reste) — Links sind im gerenderten HTML nicht mehr klickbar — und `<img>`-Elemente werden vollständig aus dem HTML entfernt (externe Bilder laden offline nicht; so entstehen keine leeren Platzhalter). Online bleiben `<a>` und `<img>` unverändert.
3. Zusätzlich hängt am `ArticleWebView` der neue `Navigating`-Handler `OnWebViewNavigating` (`ArticleDetailPage.xaml.cs`): Bei `!_viewModel.IsOnline` → `e.Cancel = true` und einmaliger lokalisierter `DisplayAlertAsync(AppResources.OfflineHint, AppResources.ArticleOfflineLinksDisabled, AppResources.ButtonOk)` — Sicherheitsnetz für HTML, das vor dem Offline-Wechsel gerendert wurde. Online bleibt das bisherige Verhalten (interne Navigation) unverändert.
4. Bei `ConnectivityChanged` baut `ArticleDetailViewModel` `HtmlSource` neu auf → Links und Bilder erscheinen/verschwinden ohne Seiten-Reload durch den Nutzer.
5. `OpenInBrowserAsync` erhält denselben Offline-Guard: Bei Offline `ErrorMessage = AppResources.OfflineHint` statt `Browser.OpenAsync`; der Online-Pfad wird zusätzlich try/catch-geschützt (Akzeptanzkriterium „keine Abstürze"). `ShareAsync` bleibt unverändert (Share-Sheet benötigt kein Netz).
6. Sichtbarkeitsgebundenes Abonnement: `ArticleDetailPage.OnAppearing` ruft `_viewModel.AttachConnectivity()` — idempotent via Guard-Flag; abonniert `ConnectivityChanged`, liest `IsOnline` aus dem Service neu und ruft `RebuildHtml()` auf, falls sich der Status seit der letzten Abmeldung geändert hat. `ArticleDetailPage.OnDisappearing` ruft `_viewModel.DetachConnectivity()` — meldet das Event ab und ruft `CancelAutoMarkRead()` auf (ersetzt den bisherigen Einzelaufruf). Da `OnDisappearing` auch bei temporärer Überdeckung feuert, stellt das Wieder-Abonnieren in `OnAppearing` sicher, dass der `ConnectivityChanged`→`RebuildHtml`-Pfad zuverlässig funktioniert (Muster: `SettingsPage.xaml.cs` Abo/Abmeldung Z. 22–42; Leak-Verhütung, da das ViewModel transient ist).

Beteiligte Klassen/Komponenten: `ArticleDetailViewModel`, `ArticleDetailPage`, `ArticleHtmlSanitizer`, `INetworkStatusService`.

### Lokalisierung vervollständigen

1. Neue Schlüssel werden in `AppResources.resx` (EN) und `AppResources.de.resx` (DE) angelegt (Liste unter „Änderungen an bestehenden Klassen" → Ressourcen); `AppResources.Designer.cs` wird neu generiert (`PublicResXFileCodeGenerator`).
2. `ArticleDetailPage.xaml` ersetzt alle hartcodierten Texte durch `{x:Static strings:AppResources.*}` — dazu muss der Namespace `xmlns:strings="clr-namespace:Reporter.Core.Resources.Strings;assembly=Reporter.Core"` ergänzt werden (bisher nicht deklariert).
3. `ArticleDetailViewModel` ersetzt `BookmarkButtonLabel`, `MarkAsReadButtonLabel` und `CalculateReadingTime` durch `AppResources.*` bzw. `string.Format(CultureInfo.CurrentCulture, AppResources.ArticleReadingTimeFormat, minutes)` (bestehendes Format-Muster, vgl. `AutoMarkReadLabel` Zeile 268).
4. `CategoriesViewModel` ersetzt die zwei hartcodierten Validierungsmeldungen (Zeilen 120, 128) durch `AppResources.ErrorCategoryNameEmpty`/`ErrorCategoryDuplicate` — entdeckte Lücke, nicht im Inventory gelistet.
5. Der Pre-Commit-Hook `.githooks/translation-check.py` erzwingt den DE/EN-Schlüsselgleichstand.
6. Die Sprachwahl erfolgt automatisch über `CultureInfo.CurrentUICulture` (neutral/EN = Fallback); kein Laufzeit-Code nötig — Verifikation über manuelle Tests.

Beteiligte Klassen/Komponenten: `AppResources.resx`, `AppResources.de.resx`, `AppResources.Designer.cs`, `ArticleDetailPage.xaml`, `ArticleDetailViewModel`, `CategoriesViewModel`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `INetworkStatusService` (`src/Reporter.Core/Interfaces/INetworkStatusService.cs`) | Interface | Abstraktion des Netzwerkzustands: `bool IsOnline { get; }` + `event EventHandler? ConnectivityChanged` |
| `NetworkStatusService` (`src/Reporter/Services/NetworkStatusService.cs`) | Klasse | MAUI-Implementierung über `Connectivity.Current`/`ConnectivityChanged` mit UI-Thread-Dispatch via `MainThread.BeginInvokeOnMainThread` |
| `ArticleHtmlSanitizer` (`src/Reporter.Core/Services/ArticleHtmlSanitizer.cs`) | statische Klasse | Übernimmt die Sanitize-Regex-Pipeline aus `ArticleDetailViewModel`; `Sanitize(string? html, bool forOffline)` ersetzt bei `forOffline` `<a>`-Tags durch ihren Text und entfernt `<img>`-Elemente. Unit-testbar in `Reporter.Tests` |
| `FakeNetworkStatusService` (`src/Reporter.Tests/FakeNetworkStatusService.cs`) | Test-Fake | Setzbares `IsOnline`, Methode `RaiseConnectivityChanged()` zum synchronen Auslösen des Events |

## Änderungen an bestehenden Klassen

### `UnreadViewModel` (ViewModel, `src/Reporter.Core/ViewModels/UnreadViewModel.cs`)

- **Neue Abhängigkeit:** `INetworkStatusService` (required, vor den bestehenden optional-losen Parametern anfügen).
- **Neue Eigenschaften:** `IsOnline` (`bool`, `SetProperty`) — Ausgangswert aus dem Service, treibt den `DataTrigger` am Sync-Button.
- **Neue Event-Handler:** `ConnectivityChanged` im Konstruktor abonnieren → `IsOnline` aktualisieren. Kein Unsubscribe nötig (Singleton-Lebensdauer).
- **Geänderte Methoden:** `RefreshAsync` — Frühabbruch `if (!IsOnline) { ErrorMessage = AppResources.OfflineHint; return; }` vor `IsSyncing = true`.

### `FeedsViewModel` (ViewModel, `src/Reporter.Core/ViewModels/FeedsViewModel.cs`)

- **Neue Abhängigkeit:** `INetworkStatusService` (required, vor `ILocalNotificationService?` einfügen).
- **Neue Eigenschaften:** `IsOnline` (`bool`, `SetProperty`) — Ausgangswert aus dem Service; wird vom persistenten `OfflineHint`-Banner auf `FeedsPage` gebunden (dauerhaft sichtbarer Offline-Status, Anwenderentscheidung).
- **Neue Event-Handler:** `ConnectivityChanged` im Konstruktor abonnieren → `IsOnline` aktualisieren, damit das Banner ohne Nutzeraktion auf Statuswechsel reagiert (Singleton — kein Unsubscribe).
- **Geänderte Methoden:** `RefreshAsync` und `RefreshAllAsync` — jeweils Frühabbruch mit `ErrorMessage = AppResources.OfflineHint` bei Offline.

### `LaterViewModel` (ViewModel, `src/Reporter.Core/ViewModels/LaterViewModel.cs`)

- **Neue Abhängigkeit:** `INetworkStatusService` (required, nach `IItemRepository`).
- **Neue Eigenschaften:** `IsOnline` (`bool`, `SetProperty`) — von `LaterPage.xaml` an `ArticleCardView.IsOnline` gebunden (Thumbnail-Ausblendung offline).
- **Neue Event-Handler:** `ConnectivityChanged` im Konstruktor abonnieren → `IsOnline` aktualisieren (Singleton — kein Unsubscribe).

### `ArticleDetailViewModel` (ViewModel, `src/Reporter/ViewModels/ArticleDetailViewModel.cs`)

- **Neue Abhängigkeit:** `INetworkStatusService` (required).
- **Neue Eigenschaften:** `IsOnline` (`bool`); `ErrorMessage` (`string`, private Setter) und `HasError` (`bool`, wie bei `UnreadViewModel`/`FeedsViewModel`) — für den Offline-Hinweis bei `OpenInBrowser`.
- **Neue Event-Handler:** `ConnectivityChanged`-Handler → `IsOnline` aktualisieren + `RebuildHtml()` aufrufen; das Abonnement erfolgt über `AttachConnectivity()` (nicht im Konstruktor).
- **Neue Methoden:** `AttachConnectivity()` — idempotent (Guard-Flag `_isConnectivityAttached`); abonniert `ConnectivityChanged`, liest `IsOnline` aus dem Service neu und ruft `RebuildHtml()` auf, falls sich der Status geändert hat. `DetachConnectivity()` — meldet `ConnectivityChanged` ab (Guard zurücksetzen) und ruft `CancelAutoMarkRead()` auf. Ersetzt den ursprünglich geplanten `IDisposable`/`Dispose`-Ansatz: `OnDisappearing` feuert auch bei temporärer Überdeckung, daher muss das Abonnement bei `OnAppearing` erneuert werden können; das ViewModel ist `AddTransient` registriert, ohne Abmeldung hielte das Singleton-Event jede besuchte Detailseite fest.
- **Geänderte Eigenschaften:** `BookmarkButtonLabel` → `AppResources.ArticleBookmarkRemove`/`AppResources.ArticleBookmarkSet` (ersetzt hartcodiertes Deutsch, Zeile 191); `MarkAsReadButtonLabel` → `AppResources.ArticleAlreadyRead`/`AppResources.ArticleMarkAsRead` (Zeile 196).
- **Geänderte Methoden:**
  - `CalculateReadingTime` → `string.Format(CultureInfo.CurrentCulture, AppResources.ArticleReadingTimeFormat, minutes)` (ersetzt `$"{minutes} Min. Lesezeit"`, Zeile 320).
  - `SanitizeHtml` → delegiert an `ArticleHtmlSanitizer.Sanitize(html, forOffline: !IsOnline)`; die statischen Regex-Felder (Zeilen 22–31) ziehen in die neue Klasse um; neue Regexes für die `<a>`-Neutralisierung und die `<img>`-Entfernung kommen dort hinzu.
  - `RebuildHtml` → nutzt den Sanitizer mit dem Offline-Flag (`forOffline: !IsOnline`); HTML-Wrapper (CSP inkl. `img-src *`, Schriftgrößen) unverändert — die `<img>`-Entfernung erfolgt ausschließlich über den Sanitizer.
  - `OpenInBrowserAsync` → Offline-Guard (`ErrorMessage = AppResources.OfflineHint`, kein `Browser.OpenAsync`) + try/catch um den Browser-Aufruf.

### `FeedSyncService` (Service, `src/Reporter.Core/Services/FeedSyncService.cs`)

- **Neue Abhängigkeit:** `INetworkStatusService` (required, am Ende der Parameterliste).
- **Geänderte Methoden:** `SyncFeedAsync` und `SyncAllAsync` — `IsOnline`-Prüfung ganz am Anfang; bei Offline Rückgabe `SyncResult(FeedHealth.Error, 0, AppResources.OfflineHint)` ohne `SyncLog`-Eintrag, ohne `HealthStatus`-Änderung, ohne HTTP-Zugriff.
- **Hinweis:** Die übrigen englisch hartcodierten `SyncLog`-Meldungen bleiben bewusst unverändert — persistierte technische Diagnosedaten werden nicht lokalisiert (mit Anwender geklärt).

### `AutoRefreshService` (Service, `src/Reporter.Core/Services/AutoRefreshService.cs`)

- **Neue Abhängigkeit:** `INetworkStatusService` (required, vor `TimeProvider?` einfügen).
- **Geänderte Methoden:** `RunLoopAsync` — pro `PeriodicTimer`-Tick bei `!IsOnline` `continue` (kein Sync, kein `SyncLog`-Rauschen).

### `ArticleDetailPage.xaml` (View, `src/Reporter/Views/ArticleDetailPage.xaml`)

- `xmlns:strings="clr-namespace:Reporter.Core.Resources.Strings;assembly=Reporter.Core"` ergänzen.
- Ersetzungen: `TargetNullValue='Artikel'` → `{x:Static strings:AppResources.ArticleTitleFallback}` (Z. 9); `"Gelesen"` → `ArticleReadLabel` (Z. 26); `"Vollständiger Artikel verfügbar"` → `ArticleFullContentAvailable` (Z. 128); `"Im Browser öffnen"` → `ArticleOpenInBrowser` (Z. 139, 280); `SemanticProperties.Description` `"Zurück"`/`"Schriftgröße wechseln"`/`"Teilen"` → `AccessibilityBack`/`AccessibilityFontSize`/`AccessibilityShare` (Z. 160, 212, 257). `"•"`-Trennzeichen bleiben (sprachneutral).
- `ArticleWebView` (Z. 108–115): Attribut `Navigating="OnWebViewNavigating"` ergänzen.
- Neu: `ErrorMessage`-Label (Muster `FeedsPage.xaml` Z. 65–68) sowie Offline-Hinweis-Label `ArticleOfflineLinksDisabled` mit `DataTrigger` `IsOnline == False` (Muster `NotificationsIosOnlyHint`-Border `FeedsPage.xaml` Z. 51–64).
- Design-Referenz für die manuelle UI-Verifikation: `design-draft/stitch_local_rss_feed_reader/artikel_lesemodus/screen.png` (plus `_dark_mode`-Variante); `AppThemeBinding` für alle neuen Elemente verwenden.

### `ArticleDetailPage.xaml.cs` (Code-Behind)

- **Neue Event-Handler:** `OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)` — bei `!_viewModel.IsOnline`: `e.Cancel = true` und `DisplayAlertAsync(AppResources.OfflineHint, AppResources.ArticleOfflineLinksDisabled, AppResources.ButtonOk)` (bestehendes Alert-Muster, vgl. `SettingsPage.xaml.cs` Z. 46).
- **Neue Methoden-Overrides:** `OnAppearing` — `_viewModel.AttachConnectivity()` (erneutes Abonnieren nach temporärer Überdeckung; frischt `IsOnline`/`HtmlSource` bei Statuswechsel auf).
- **Geänderte Methoden:** `OnDisappearing` — `_viewModel.DetachConnectivity()` statt nur `CancelAutoMarkRead()` (enthält die Event-Abmeldung).

### `UnreadPage.xaml` (View, `src/Reporter/Views/UnreadPage.xaml`)

- Sync-Button-`Border` (Z. 21–41): `DataTrigger` auf `IsOnline == False` → `Opacity = 0.4` und gedimmte `Path`-`Stroke`-Farbe → „Sync-Button zeigt Offline-Status".
- Neu: `OfflineHint`-Label in der Statuszeile (bei `Grid.Column="2"`, Z. 61–68), `IsVisible` über `DataTrigger` `IsOnline == False`.
- Neu: `ErrorMessage`-Label (`Text="{Binding ErrorMessage}"`, `IsVisible="{Binding HasError}"`, Fehlerfarbe per `AppThemeBinding`) — zeigt den Offline-Hinweis des Frühabbruchs und bisher unsichtbare Sync-Fehler.
- `ArticleCardView` im `DataTemplate` (Z. 115): `IsOnline="{Binding BindingContext.IsOnline, Source={x:Reference PageRoot}}"` ergänzen — gleiches `Source`-Muster wie die benachbarten Command-Bindungen (Thumbnail-Ausblendung offline).
- Design-Referenz: `design-draft/stitch_local_rss_feed_reader/ungelesen_dashboard/screen.png`; bestehendes `DataTrigger`-Muster aus `FeedsPage.xaml` wiederverwenden; keine neuen Zeilen mit mehreren Text-Buttons (Mobile-UI-Regel).

### `FeedsPage.xaml` (View, `src/Reporter/Views/FeedsPage.xaml`)

- Neu: Persistenter `OfflineHint`-`Border` als Seiten-Banner — drittes Element des oberen `VerticalStackLayout` (nach der Eingabekarte `Border` Z. 15–72, `Grid.Row="0"`), damit unterhalb der Eingabekarte und oberhalb der Feed-Liste dauerhaft sichtbar. `IsVisible="False"` mit `Border.Triggers`/`DataTrigger` `Binding="{Binding IsOnline}"` `Value="False"` → `Setter IsVisible = true`; Inhalt: `Label Text="{x:Static strings:AppResources.OfflineHint}"`. Styling exakt nach `NotificationsIosOnlyHint`-Muster (Z. 51–64: `Padding="10"`, `StrokeShape="RoundRectangle 8"`, `Stroke="Transparent"`, `SurfaceSubtle`-Hintergrund, `BodySmallStyle`, `TextSecondary`-Farbe per `AppThemeBinding`).
- Platzierung: Der Banner liegt bewusst **außerhalb** der Eingabekarte — das darin liegende `ErrorMessage`-Label (Z. 65–68) bleibt der reaktive Formular-/Sync-Fehlerkanal, der persistente Seiten-Status wird separat geführt (vermeidet Verwechslung mit Validierungsfehlern).
- `xmlns:strings` ist bereits deklariert (Z. 4) — kein Namespace-Eintrag nötig.

### `LaterPage.xaml` (View, `src/Reporter/Views/LaterPage.xaml`)

- `ArticleCardView` im `DataTemplate` (Z. 22): `IsOnline="{Binding BindingContext.IsOnline, Source={x:Reference PageRoot}}"` ergänzen (Thumbnail-Ausblendung offline, analog `UnreadPage`; erfordert `LaterViewModel.IsOnline`).

### `ArticleCardView` (View, `src/Reporter/Views/ArticleCardView.xaml` + `.xaml.cs`)

- **Neue BindableProperty:** `IsOnline` (`bool`, Standard `true`) in `ArticleCardView.xaml.cs` — Deklaration und CLR-Wrapper nach dem Muster der bestehenden `BindableProperty`s (Z. 18–40/53–75); Default `true`, damit ungebundene Verwendungen unverändert bleiben.
- **XAML:** Der Thumbnail-`Border` (Z. 66–78) erhält `Border.Triggers` mit `DataTrigger TargetType="Border" Binding="{Binding IsOnline, Source={x:Reference Card}}" Value="False"` → `Setter IsVisible = false`. `x:Name="Card"` existiert bereits (Z. 5). `Image Source="{Binding ImageUrl}"` bleibt unverändert — offline kollabiert die Box und der Text nutzt die Breite.

### `MauiProgram` (`src/Reporter/MauiProgram.cs`)

- **Neue Registrierung:** `.AddSingleton<INetworkStatusService, NetworkStatusService>()` im Service-Block (Z. 42–70).

### `App.xaml.cs`

- **Geänderte Methoden:** `OnStart` — neuer try/catch-Block: `INetworkStatusService` resolven, damit das Connectivity-Monitoring beim Start beginnt (Muster der bestehenden Blöcke Z. 51–62, 64–73).

### `CategoriesViewModel` (ViewModel, `src/Reporter.Core/ViewModels/CategoriesViewModel.cs`)

- **Geänderte Methoden:** `SaveAsync` — `ErrorMessage = "The category name cannot be empty."` (Z. 120) → `AppResources.ErrorCategoryNameEmpty`; `"A category with this name already exists."` (Z. 128) → `AppResources.ErrorCategoryDuplicate` (Namensmuster analog `ErrorFeedTitleEmpty`/`ErrorFeedDuplicate`).

### `AppResources.resx` / `AppResources.de.resx` / `AppResources.Designer.cs` (`src/Reporter.Core/Resources/Strings/`)

- **Neue Schlüssel (je EN/DE):**

| Schlüssel | EN (neutral) | DE | Verwendung |
|-----------|--------------|-----|------------|
| `ArticleTitleFallback` | `Article` | `Artikel` | `Title`-`TargetNullValue` |
| `ArticleReadLabel` | `Read` | `Gelesen` | Status-Pill |
| `ArticleFullContentAvailable` | `Full article available` | `Vollständiger Artikel verfügbar` | Quellen-Footer |
| `ArticleOpenInBrowser` | `Open in browser` | `Im Browser öffnen` | Button-Text + `SemanticProperties` |
| `AccessibilityBack` | `Back` | `Zurück` | `SemanticProperties` |
| `AccessibilityFontSize` | `Toggle font size` | `Schriftgröße wechseln` | `SemanticProperties` |
| `AccessibilityShare` | `Share` | `Teilen` | `SemanticProperties` |
| `ArticleBookmarkSet` | `Add bookmark` | `Lesezeichen setzen` | `BookmarkButtonLabel` |
| `ArticleBookmarkRemove` | `Remove bookmark` | `Lesezeichen entfernen` | `BookmarkButtonLabel` |
| `ArticleMarkAsRead` | `Mark as read` | `Als gelesen markieren` | `MarkAsReadButtonLabel` |
| `ArticleAlreadyRead` | `Already read` | `Bereits gelesen` | `MarkAsReadButtonLabel` |
| `ArticleReadingTimeFormat` | `{0} min read` | `{0} Min. Lesezeit` | `CalculateReadingTime` |
| `ArticleOfflineLinksDisabled` | `Links are disabled while you are offline.` | `Links sind im Offline-Modus deaktiviert.` | Navigating-Hinweis + Offline-Label auf der Detailseite |
| `OfflineHint` | `No internet connection.` | `Keine Internetverbindung.` | Sync-Frühabbruch, Offline-Indikator (UnreadPage-Label + FeedsPage-Banner), `SyncResult`, `OpenInBrowser`-Guard, Alert-Titel |
| `ButtonOk` | `OK` | `OK` | `DisplayAlertAsync`-Schaltfläche |
| `ErrorCategoryNameEmpty` | `The category name cannot be empty.` | `Der Kategoriename darf nicht leer sein.` | `CategoriesViewModel.SaveAsync` |
| `ErrorCategoryDuplicate` | `A category with this name already exists.` | `Eine Kategorie mit diesem Namen existiert bereits.` | `CategoriesViewModel.SaveAsync` |

- `AppResources.Designer.cs` muss nach dem Anlegen der Schlüssel neu generiert werden (VS erzeugt ihn beim Speichern der `.resx`; ohne VS manuell per Generator oder die statischen Properties ergänzen).

## Datenbankmigrationen

Keine. (Der zurückgestellte manuelle Sprachwechsel mit `Settings.Language` würde in einem Folge-Issue eine eigene Migration erfordern.)

## Validierungsregeln

Keine — es werden keine neuen oder geänderten Eingaben verarbeitet.

## Konfigurationsänderungen

Keine. Offline-Verhalten ist immer aktiv (rein zustandsabhängig); die Sprache folgt der Systemeinstellung ohne Konfigurationseintrag.

## Seiteneffekte und Risiken

- **Konstruktor-Signaturen:** `UnreadViewModel`, `FeedsViewModel`, `LaterViewModel`, `ArticleDetailViewModel`, `FeedSyncService`, `AutoRefreshService` erhalten eine neue Pflicht-Abhängigkeit — alle Test-Aufrufstellen (`UnreadViewModelTests` Z. 30, `FeedsViewModelTests` Z. 40, `LaterViewModelTests` Z. 23, `AutoRefreshServiceTests` Z. 28, `FeedSyncServiceTests` Z. 71/78) müssen angepasst werden; `MauiProgram` resolved automatisch.
- **Event-Lebensdauer:** `ArticleDetailViewModel` ist `AddTransient` — die `ConnectivityChanged`-Abmeldung erfolgt über `DetachConnectivity()` in `OnDisappearing`; `OnDisappearing` feuert auch bei temporärer Überdeckung, daher ist das erneute Abonnieren in `OnAppearing` (`AttachConnectivity`) zwingend, sonst reagiert `IsOnline`/`RebuildHtml` nach dem Zurückkehren nicht mehr auf Statuswechsel. `UnreadViewModel`/`FeedsViewModel`/`LaterViewModel` sind Singletons — dort unproblematisch.
- **Listen-Thumbnails offline ausgeblendet:** `ArticleCardView` blendet den 80×80-Thumbnail-`Border` offline aus — gewollte, sichtbare Verhaltensänderung auf Ungelesen und Später (konsistent zur `<img>`-Entfernung im Artikel); Online-Darstellung unverändert.
- **Threading:** `Connectivity.ConnectivityChanged` kann auf Hintergrund-Threads feuern; der Dispatch erfolgt zentral im `NetworkStatusService`. Ohne ihn drohen UI-Thread-Verletzungen in den VMs.
- **`UnreadPage` zeigte `ErrorMessage` bisher nie an:** Das neue Fehler-Label macht auch bislang stille Sync-Fehler sichtbar — gewollt, aber sichtbare Verhaltensänderung.
- **`<a>`-Neutralisierung und `<img>`-Entfernung:** Greifen nur bei `IsOnline == false`; Online-Rendering unverändert. Regex-basiert wie die bestehende `SanitizeHtml`-Pipeline (best-effort, dokumentierte Einschränkung bleibt).
- **`AppResources.Designer.cs`:** checked-in generierte Datei — muss mit den neuen Schlüsseln regeneriert werden, sonst baut das Projekt nicht (Zugriff auf `AppResources.OfflineHint` usw.).
- **`translation-check.py` (pre-commit):** Schlüsselgleichstand neutral↔de wird erzwungen; neue Schlüssel müssen in beiden Dateien stehen.
- **Persistierte `SyncLog`-Meldungen** bleiben englisch hartcodiert — mit Anwender geklärt: technische Diagnosedaten werden nicht lokalisiert.
- **Externe `<img>` im Artikel-HTML** werden offline vollständig entfernt (mit Anwender geklärt): Artikel, deren Aussagekraft hauptsächlich auf Bildern beruht, erscheinen offline entsprechend gekürzt. CSP `img-src * data: blob:` bleibt für den Online-Modus unverändert.

## Umsetzungsreihenfolge

1. **`INetworkStatusService` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Interface in `src/Reporter.Core/Interfaces/` mit `bool IsOnline` + `event EventHandler? ConnectivityChanged`.

2. **`ArticleHtmlSanitizer` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Statische Klasse in `src/Reporter.Core/Services/`; Sanitize-Regex-Pipeline aus `ArticleDetailViewModel` (Z. 22–31, 329–346) umziehen; `Sanitize(string? html, bool forOffline)` — bei `forOffline` neue Regexes zur `<a>`-Neutralisierung und zur Entfernung von `<img>`-Elementen (auch selbstschließend).

3. **Neue ResX-Schlüssel anlegen + `AppResources.Designer.cs` regenerieren**
   - Voraussetzungen: Keine.
   - Beschreibung: Alle 17 Schlüssel aus der Tabelle oben in `AppResources.resx` (EN) und `AppResources.de.resx` (DE) eintragen; Designer-Datei neu erzeugen. Muss vor allen Schritten erfolgen, die `AppResources.*`-Schlüssel verwenden.

4. **`NetworkStatusService` implementieren + DI-Registrierung**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `src/Reporter/Services/NetworkStatusService.cs` über `Connectivity.Current`, `ConnectivityChanged`-Abo mit `MainThread`-Dispatch; `.AddSingleton<INetworkStatusService, NetworkStatusService>()` in `MauiProgram`; `App.OnStart` resolved den Service im try/catch-Block.

5. **`UnreadViewModel` offline-fähig machen**
   - Voraussetzungen: Schritte 1, 3.
   - Beschreibung: Neue Abhängigkeit, `IsOnline`, Event-Abo, Offline-Frühabbruch in `RefreshAsync`.

6. **`FeedsViewModel` offline-fähig machen**
   - Voraussetzungen: Schritte 1, 3.
   - Beschreibung: Analog Schritt 5 für `RefreshAsync`/`RefreshAllAsync`; `IsOnline` treibt das FeedsPage-Banner.

7. **`LaterViewModel` offline-fähig machen**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Neue Abhängigkeit, `IsOnline`, `ConnectivityChanged`-Abo — liefert den Status für die `ArticleCardView`-Thumbnail-Ausblendung auf der Später-Seite.

8. **`FeedSyncService` Offline-Guard**
   - Voraussetzungen: Schritte 1, 3.
   - Beschreibung: `IsOnline`-Frühprüfung in `SyncFeedAsync`/`SyncAllAsync` ohne `SyncLog`-Schreibzugriff.

9. **`AutoRefreshService` Offline-Skip**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Neue Abhängigkeit; `RunLoopAsync` überspringt Ticks bei `!IsOnline`.

10. **`ArticleDetailViewModel` anpassen (Offline + Lokalisierung)**
    - Voraussetzungen: Schritte 1, 2, 3.
    - Beschreibung: Neue Abhängigkeit, `IsOnline`, `ErrorMessage`/`HasError`, `AttachConnectivity()`/`DetachConnectivity()` (idempotentes Abo, `Detach` inkl. `CancelAutoMarkRead`), `ConnectivityChanged`→`RebuildHtml`, `SanitizeHtml`-Delegation, lokalisierte Labels (`BookmarkButtonLabel`, `MarkAsReadButtonLabel`, `ArticleReadingTimeFormat`), `OpenInBrowserAsync`-Guard.

11. **`ArticleDetailPage` anpassen**
    - Voraussetzungen: Schritte 3, 10.
    - Beschreibung: `xmlns:strings` ergänzen, alle hartcodierten Texte auf `{x:Static}` umstellen, `Navigating="OnWebViewNavigating"` am `ArticleWebView`, `OnWebViewNavigating` in der `.xaml.cs` (Offline → `e.Cancel` + lokalisierter Alert), `OnAppearing` ruft `AttachConnectivity`, `OnDisappearing` ruft `DetachConnectivity`, `ErrorMessage`- und Offline-Hinweis-Label.

12. **`FeedsPage.xaml` persistenter Offline-Indikator**
    - Voraussetzungen: Schritte 3, 6.
    - Beschreibung: `OfflineHint`-`Border` als Seiten-Banner unterhalb der Eingabekarte mit `DataTrigger` `IsOnline == False` — nach `NotificationsIosOnlyHint`-Muster (Z. 51–64).

13. **`ArticleCardView` Thumbnail-Ausblendung**
    - Voraussetzungen: Keine.
    - Beschreibung: `IsOnline`-`BindableProperty` (Standard `true`) in `ArticleCardView.xaml.cs`; `DataTrigger` `IsOnline == False` → `IsVisible = false` am Thumbnail-`Border` (Z. 66–78, Binding via `Source={x:Reference Card}`).

14. **`UnreadPage.xaml` und `LaterPage.xaml` anpassen**
    - Voraussetzungen: Schritte 3, 5, 7, 13.
    - Beschreibung: `UnreadPage`: `DataTrigger` auf `IsOnline == False` am Sync-Button (Opacity/Stroke-Dimmung), `OfflineHint`-Label, `ErrorMessage`-Label — jeweils nach den dokumentierten `FeedsPage`/`ArticleDetailPage`-Trigger-Mustern; außerdem `IsOnline="{Binding BindingContext.IsOnline, Source={x:Reference PageRoot}}"` am `ArticleCardView`. `LaterPage`: dieselbe `IsOnline`-Bindung am `ArticleCardView`.

15. **`CategoriesViewModel` lokalisieren**
    - Voraussetzungen: Schritt 3.
    - Beschreibung: Zwei `ErrorMessage`-Literale durch `AppResources.ErrorCategoryNameEmpty`/`ErrorCategoryDuplicate` ersetzen.

16. **`FakeNetworkStatusService` + Testanpassungen + neue Unit-Tests**
    - Voraussetzungen: Schritte 1–10, 15.
    - Beschreibung: Fake anlegen; Konstruktor-Aufrufstellen in `UnreadViewModelTests`, `FeedsViewModelTests`, `LaterViewModelTests`, `AutoRefreshServiceTests`, `FeedSyncServiceTests` aktualisieren; neue Tests gemäß Tabelle unten.

17. **Manuelle UI-Verifikation + Dokumentation**
    - Voraussetzungen: Schritte 1–15; lauffähiger Windows-Build (390×844 pt) bzw. iOS-Simulator via `scripts/iOS-Deployment.ps1`.
    - Beschreibung: Szenarien aus dem E2E-Abschnitt durchspielen, Screenshots aufnehmen, getestete Formfaktoren in `test-results.md` bzw. `docs/help/anwendung/mobile-ui-design.md` dokumentieren (AGENTS.md-Regel).

18. **Abschluss-Checks**
    - Voraussetzungen: Schritte 1–17.
    - Beschreibung: `.\scripts\Run-StaticChecks.ps1` (Format/Security/Static Analysis), `dotnet test src/Reporter.Tests/Reporter.Tests.csproj -c Release`, Übersetzungs-Hook (`translation-check.py`) — alle müssen grün sein.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `FakeNetworkStatusService` | — (Hilfsklasse, `src/Reporter.Tests/FakeNetworkStatusService.cs`) | Setzbares `IsOnline`, `RaiseConnectivityChanged()` zum synchronen Event-Auslösen |
| `RefreshCommand_WhenOffline_SetsOfflineHintAndSkipsSync` | `UnreadViewModelTests` | Offline → `SyncAllAsync` wird nicht aufgerufen, `ErrorMessage == AppResources.OfflineHint`, `HasError == true` |
| `ConnectivityChanged_UpdatesIsOnline` | `UnreadViewModelTests` | `RaiseConnectivityChanged()` → `IsOnline` folgt dem Fake-Status |
| `RefreshCommand_WhenBackOnline_InvokesSyncService` | `UnreadViewModelTests` | Nach Rückkehr des Netzes läuft der Sync-Pfad wieder regulär |
| `RefreshAllCommand_WhenOffline_SetsOfflineHintAndSkipsSync` | `FeedsViewModelTests` | `SyncAllAsync` nicht aufgerufen, `ErrorMessage == AppResources.OfflineHint` |
| `RefreshCommand_WhenOffline_SetsOfflineHintAndSkipsSync` | `FeedsViewModelTests` | `SyncFeedAsync` nicht aufgerufen, lokalisierter Hinweis gesetzt |
| `ConnectivityChanged_UpdatesIsOnline` | `FeedsViewModelTests` | `RaiseConnectivityChanged()` → `IsOnline` folgt dem Fake-Status (treibt das persistente FeedsPage-Banner) |
| `ConnectivityChanged_UpdatesIsOnline` | `LaterViewModelTests` | `RaiseConnectivityChanged()` → `IsOnline` folgt dem Fake-Status (Thumbnail-Ausblendung) |
| `Tick_WhenOffline_SkipsSyncAll` | `AutoRefreshServiceTests` | `FakeTimeProvider`-Tick bei `IsOnline == false` → `SyncAllCallCount == 0` |
| `Tick_WhenBackOnline_ResumesSync` | `AutoRefreshServiceTests` | Nach Statuswechsel zurück auf online synchronisiert der nächste Tick |
| `SyncAllAsync_WhenOffline_ReturnsErrorWithoutSyncLog` | `FeedSyncServiceTests` | `SyncResult.Status == FeedHealth.Error`, Message = `OfflineHint`, keine `sync_logs`-Zeilen, `HealthStatus` unverändert |
| `SyncFeedAsync_WhenOffline_ReturnsErrorWithoutHealthChange` | `FeedSyncServiceTests` | Kein `SyncLog`, kein `HealthStatus`-Update, kein HTTP-Aufruf (`FakeHttpMessageHandler` nicht erreicht) |
| `Sanitize_RemovesScriptAndEventHandlers` | `ArticleHtmlSanitizerTests` (neue Datei) | Umgezogene Pipeline: `script`/`on*`/`javascript:` werden entfernt (Online-Modus, Links und Bilder bleiben) |
| `Sanitize_Online_KeepsAnchorAndImgTags` | `ArticleHtmlSanitizerTests` | `forOffline: false` → `<a href>` und `<img>` bleiben erhalten |
| `Sanitize_Offline_NeutralizesAnchorTags_KeepsText` | `ArticleHtmlSanitizerTests` | `forOffline: true` → `<a href="…">Text</a>` wird zu `Text`, keine `href`/`</a>`-Reste |
| `Sanitize_Offline_RemovesImgTags` | `ArticleHtmlSanitizerTests` | `forOffline: true` → `<img …>`-Elemente (auch selbstschließend, mit beliebigen Attributen) werden vollständig entfernt, umliegender Text bleibt erhalten |
| `Sanitize_NullOrWhitespace_ReturnsInput` | `ArticleHtmlSanitizerTests` | Randfälle der Eingabe |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `UnreadViewModelTests` (Konstruktor-Aufruf Z. 30) | Neue Pflicht-Abhängigkeit `INetworkStatusService` → `FakeNetworkStatusService` übergeben |
| `FeedsViewModelTests` (Konstruktor-Aufruf Z. 40) | Dito |
| `LaterViewModelTests` (Konstruktor-Aufruf Z. 23) | Dito |
| `AutoRefreshServiceTests` (Konstruktor-Aufruf Z. 28) | Dito |
| `FeedSyncServiceTests` (Aufrufe Z. 71, 78) | Dito |
| `RefreshCommand_InvokesSyncService` (`UnreadViewModelTests`), `RefreshCommand_InvokesSyncService_AndReloadsList` / `RefreshAllCommand_InvokesSyncService_AndReloadsList` (`FeedsViewModelTests`) | Bleiben fachlich gültig, da `FakeNetworkStatusService` standardmäßig `IsOnline == true` liefert — nur Konstruktor-Anpassung nötig |

### E2E-Tests (primärer Funktionsnachweis)

Es existiert **keine automatisierte UI-Test-Infrastruktur** (`Reporter.Tests` referenziert das MAUI-Projekt nicht; kein Appium/UITest-Projekt im Repo). Gemäß AGENTS.md ist daher **dokumentierte manuelle UI-Verifikation mit Screenshots** der geforderte Nachweis. Die Szenarien werden auf dem Windows-Target im Handysize-Fenster 390×844 pt (`App.CreateWindow` setzt das bereits) und — sofern verfügbar — im iOS-Simulator via `scripts/iOS-Deployment.ps1` durchgespielt; Ergebnis-Dokumentation in `test-results.md` und/oder `docs/help/anwendung/mobile-ui-design.md`. Referenz-Designs: `design-draft/stitch_local_rss_feed_reader/ungelesen_dashboard/screen.png`, `.../artikel_lesemodus/screen.png`.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | App komplett offline (Flugmodus) starten → Ungelesen/Später/Kategorien/Feeds zeigen synchronisierte Inhalte; Listen-Thumbnails (`ArticleCardView`) sind offline ausgeblendet (keine leeren Bild-Boxen); Artikeldetail öffnet sich lesbar | Manuelle Verifikation → `test-results.md` | „App zeigt bereits synchronisierte Artikel ohne Netzwerk an" + saubere Offline-Darstellung | Nutzerfluss über SQLite-Lesepfade + WebView-Rendering — nicht unit-testbar |
| Pflicht | Offline: Sync-Button auf Ungelesen zeigt gedimmten Zustand + `OfflineHint`; Tap und Pull-to-Refresh → lokalisierter Hinweis, kein Absturz, kein neuer `SyncLog`-Fehler | Manuelle Verifikation → `test-results.md` | „Synchronisations-Button zeigt Offline-Status an", „keine Abstürze" | Visueller Zustand (`DataTrigger`) nur in echter UI prüfbar |
| Pflicht | Offline: Links im `ArticleWebView` sind nicht klickbar (neutralisiert); Tap auf evtl. verbliebene Links → lokalisierter Alert-Hinweis | Manuelle Verifikation → `test-results.md` | „Links im Artikelinhalt sind im Offline-Modus nicht klickbar" | WebView-`Navigating`-Verhalten ist plattformabhängig, nicht unit-testbar |
| Pflicht | Offline: Artikeldetail rendert ohne externe `<img>`-Bilder — die Elemente sind aus dem HTML entfernt (keine leeren Platzhalter); Artikeltext bleibt vollständig lesbar | Manuelle Verifikation → `test-results.md` | Offline-Lesbarkeit ohne defekte Bilddarstellung (Anwenderentscheidung) | Rendering-Ergebnis der Sanitize-Pipeline im echten WebView nur per UI prüfbar |
| Pflicht | Online→Offline→Online-Wechsel zur Laufzeit (Netz im laufenden Betrieb trennen/verbinden) → Statusanzeige und Link-Verhalten folgen ohne App-Neustart | Manuelle Verifikation → `test-results.md` | Offline-Erkennung dynamisch | Prüft `ConnectivityChanged`-Propagation und `RebuildHtml`-Pfad |
| Pflicht | Systemsprache Deutsch → alle UI-Texte deutsch inkl. Detailseite (Status-Pill, Buttons, Accessibility-Labels, Lesezeit); Systemsprache Englisch → englisch; Drittsprache (z. B. Französisch) → englischer Fallback | Manuelle Verifikation mit Screenshots → `test-results.md` | „UI-Texte erscheinen auf Deutsch oder Englisch entsprechend der Systemsprache" | `CurrentUICulture`-Auflösung der ResX nur auf echtem Gerät/OS verifizierbar |
| Pflicht | Offline: Feeds-Seite zeigt ohne Nutzeraktion den `OfflineHint`-Banner oberhalb der Liste; Banner verschwindet nach Netzrückkehr zur Laufzeit | Manuelle Verifikation → `test-results.md` | Anwenderentscheidung „Sync-Offline-Status auf UnreadPage **und** FeedsPage" (persistenter Indikator) | Persistenter visueller Zustand (`DataTrigger` auf `FeedsViewModel.IsOnline`) nur in echter UI prüfbar |
| Pflicht | Offline: Pull-to-Refresh und Einzel-Feed-Aktualisierung auf Feeds → lokalisierter Hinweis, kein Absturz | Manuelle Verifikation → `test-results.md` | „Keine Abstürze bei fehlender Netzwerkverbindung" | Benutzerfluss über `RefreshView`/ActionSheet |
| Pflicht | Offline: Artikeldetail → „Im Browser öffnen" tippen (Footer-Button und Aktionsleisten-Icon) → lokalisierter Hinweis im `ErrorMessage`-Label statt Browser-Öffnung, kein Absturz; `ArticleOfflineLinksDisabled`-Offline-Label sichtbar | Manuelle Verifikation → `test-results.md` | „Keine Abstürze bei fehlender Netzwerkverbindung", OpenInBrowser-Guard (Anwenderentscheidung) | `ArticleDetailViewModel` liegt im MAUI-Projekt und ist von `Reporter.Tests` nicht erreichbar — der Guard ist nur manuell verifizierbar |
| Soll | iOS-Simulator-Lauf (via `scripts/iOS-Deployment.ps1`) mit denselben Kern-Szenarien, falls macOS-Host verfügbar; sonst dokumentierte Einschränkung | Manuelle Verifikation → `test-results.md` | Plattform-Abdeckung | `Connectivity`-/WebView-Verhalten kann plattformspezifisch abweichen |

Bestehende E2E-Tests, die angepasst werden müssen: Keine — es gibt keinen automatisierten UI-Testbestand; `SettingsViewModelTests_E2E` (Persist-Roundtrips) ist unverändert lauffähig.

## Offene Punkte

Keine — alle Punkte wurden mit dem Anwender geklärt und sind als Designentscheidungen bzw. Programmablaufdetails im Plan eingearbeitet.
