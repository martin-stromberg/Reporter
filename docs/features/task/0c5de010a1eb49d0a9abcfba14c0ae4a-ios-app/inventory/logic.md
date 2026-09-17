<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme — Logikklassen und Views

## `ArticleDetailPage`
Datei: `src/Reporter/Views/ArticleDetailPage.xaml.cs` (Code-Behind zu `ArticleDetailPage.xaml`)

Kernstelle der Anforderung (Punkt 3). Der `WebView` ist per XAML (`Navigating="OnWebViewNavigating"`, `ArticleDetailPage.xaml` Zeile 333) mit `HtmlWebViewSource Html="{Binding HtmlSource}"` (Zeilen 336–338) verdrahtet.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ArticleDetailPage(ArticleDetailViewModel)` | public | Konstruktor, setzt `BindingContext` |
| `ApplyQueryAttributes(IDictionary<string, object>)` | public | liest `itemId` und startet `LoadAsync` |
| `OnAppearing()` / `OnDisappearing()` | protected override | bindet/löst `AttachConnectivity`/`DetachConnectivity` des ViewModels |
| `LoadAsync(Guid)` | private | lädt den Artikel, `try/catch` mit `Debug.WriteLine` |
| `OnWebViewNavigating(object?, WebNavigatingEventArgs)` | private | **Ist-Zustand (Zeilen 72–81):** kehrt sofort zurück, wenn `_viewModel.IsOnline` **oder** die URL nicht extern ist (`WebViewNavigationGuard.IsExternalUrl`). Nur **offline** wird externe Navigation per `e.Cancel = true` abgebrochen und ein lokalisierter `DisplayAlertAsync` (`AppResources.OfflineHint` + `AppResources.ArticleOfflineLinksDisabled` + `ButtonOk`) gezeigt. **Online laufen externe Links heute im WebView** — keine Umleitung in den System-Browser. |

Abonnierte Events: `WebView.Navigating` (XAML-Event-Handler).
Publizierte Events: keine.

## `WebViewNavigationGuard`
Datei: `src/Reporter.Core/Services/WebViewNavigationGuard.cs`

Statische Klassifikationslogik, die laut Anforderung zentral wiederverwendet werden soll.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `IsExternalUrl(string? url)` | public static | `false` bei `null`/leer/Blank; `true` nur bei `http://`/`https://`-Prefix (`OrdinalIgnoreCase`). `about:blank`, `data:`, `file:`, `ftp:` u. a. gelten als nicht extern. |

## `ArticleDetailViewModel`
Datei: `src/Reporter/ViewModels/ArticleDetailViewModel.cs` (Namespace `Reporter.Core.ViewModels`, Projekt `Reporter`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync(Guid)` | public | lädt Settings (Fallback bei Fehler), `Item` und `Feed`, baut HTML via `RebuildHtml`, startet ggf. `MarkReadDelayedAsync` |
| `CancelAutoMarkRead()` | public | cancelt den Auto-Gelesen-Timer |
| `AttachConnectivity()` / `DetachConnectivity()` | public | sichtbarkeitsgebundenes Connectivity-Tracking (aus `BaseViewModel`) |
| `OnConnectivityChanged(bool)` | protected override | leert `ErrorMessage` und ruft `RebuildHtml` (Links/Bilder kehren bei Netzrückkehr zurück) |
| `RebuildHtml()` | private | `ArticleHtmlSanitizer.Sanitize(content, forOffline: !IsOnline)` + HTML-Template mit CSP (`default-src 'none'`, `img-src * data: blob:`, `script-src 'none'`) |
| `OpenInBrowserAsync()` | private (Command `OpenInBrowserCommand`) | **Referenzmuster (Zeilen 489–513):** Offline-Guard (`ErrorMessage = AppResources.OfflineHint`), sonst `Browser.OpenAsync(Item.Link, BrowserLaunchMode.SystemPreferred)` im `try/catch` mit `AppResources.ErrorOpenInBrowserFailed` |
| `ShareAsync()` | private | `Share.RequestAsync` mit Titel/URI |
| `MarkReadDelayedAsync`, `ToggleSavedForLaterAsync`, `ToggleMarkReadAsync`, `ToggleAutoMarkRead`, `ToggleFontSizeAsync`, `GoBackAsync`, `CreateItemCopy` | private | übrige Aktionslogik |

Relevante Properties: `IsOnline` (aus `BaseViewModel`), `ErrorMessage`/`HasError`, `HtmlSource`, `Item`, `FeedName`, `FeedIconUrl`.

Abonnierte Events: `INetworkStatusService.ConnectivityChanged` (über `BaseViewModel.TrackConnectivity`).
Publizierte Events: `PropertyChanged` (MVVM).

## `BaseViewModel`
Datei: `src/Reporter.Core/ViewModels/BaseViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `IsOnline` | public get / private set | aktueller Netzwerkzustand |
| `InitConnectivity(INetworkStatusService)` | protected | setzt Service + Initialzustand ohne Abo |
| `TrackConnectivity(...)` / `TrackConnectivity()` | protected | abonniert `ConnectivityChanged`, idempotent |
| `UntrackConnectivity()` | protected | Abmeldung, idempotent |
| `RefreshConnectivityStatus()` | protected | liest `IsOnline` neu, ruft `OnConnectivityChanged` nur bei Änderung |
| `OnConnectivityChanged(bool)` | protected virtual | Hook für Ableitungen |

## `ArticleHtmlSanitizer`
Datei: `src/Reporter.Core/Services/ArticleHtmlSanitizer.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Sanitize(string? html, bool forOffline)` | public static | Regex-Pipeline: entfernt `script`/`iframe`/`style`/`object`/`embed`/`form`/`link`/`meta`/`base`/`applet`/`audio`/`video`, `on*`-Handler und `javascript:`; bei `forOffline: true` ersetzt `<a>` durch Inner-Text und entfernt `<img>` |

## `App`
Datei: `src/Reporter/App.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnStart()` | protected override async void | **Punkt 6:** Zeile 42 `await context.Database.MigrateAsync()` **ohne** `try/catch` — ein Migrationsfehler würde den Start durchschlagen lassen. `IDebugLogService` wird erst danach aufgelöst (Zeilen 44–46, `BeginSessionAsync`). Alle weiteren Start-Schritte (Demo-Seed Zeilen 51–61, Retention-Cleanup 63–73, Theme 75–87, `INetworkStatusService`-Resolve 89–99, `IAutoRefreshService.StartAsync` 101–111) folgen dem Muster `try/catch` + `Debug.WriteLine` + `debugLogService.LogAsync(DebugLogCategory.Lifecycle, <Meldung>, ex.ToString(), DebugLogLevel.Error)`. |
| `OnSleep()` / `OnResume()` | protected override | Lifecycle-Logeinträge (`DebugLogLevel.Info`) |
| `CreateWindow(IActivationState?)` | protected override | `Window(AppShell)`; unter Windows 390×844 pt (Handygröße für UI-Checks) |
| `OnUnhandledException` / `OnUnobservedTaskException` | private | fire-and-forget `LogAsync(DebugLogCategory.Exception, …, DebugLogLevel.Error)` |

Abonnierte Events: `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException` (registriert in `OnStart`, Zeilen 48–49).

## `MauiProgram`
Datei: `src/Reporter/MauiProgram.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateMauiApp()` | public static | Fonts, DI-Registrierungen (Zeilen 72–114), **DB-Pfad-Ermittlung Zeilen 44–54** (`REPORTER_DB_PATH`-Override, `FileSystem.AppDataDirectory/reporter.db`, `Directory.CreateDirectory`), `isFirstRun`-Auswertung Zeile 61, `REPORTER_FEEDSEARCH_ENDPOINT` (Zeile 65), `REPORTER_DISABLE_DEMO_SEED` (Zeilen 69–70); ruft nach `builder.Build()` `ApplyPersistedLanguage` |
| `ResolveFeedSearchEndpoint(string?)` | private static | validiert den Endpunkt-Override (nur absolute http/https-URIs) |
| `ResolveDemoSeedSuppressed(string?)` | private static | jeder gesetzte Wert außer `0`/`false` unterdrückt den Seed |
| `ApplyPersistedLanguage(MauiApp)` | private static | **Zeilen 163–179:** `try/catch` um synchrones `context.Database.Migrate()` (Zeile 169) + Sprachanwendung via `AppCulture.Apply(settings.Language)` — bereits abgesichert; erzeugt die DB-Datei vor `App.OnStart` |

## `DebugLogService`
Datei: `src/Reporter.Core/Services/DebugLogService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `MaxStoredEntries` | public const | 500 |
| `IsEnabled` | public | persistenter Sammlungs-Schalter |
| `BeginSessionAsync(CancellationToken)` | public | löscht Vorsession außer `Error`-Einträgen, lädt `DebugCollectionEnabled`, schreibt Starteintrag; fehlerisoliert |
| `SetEnabled(bool)` | public | schaltet die Sammlung, Lifecycle-Eintrag bei Aktivierung |
| `LogAsync(category, message, details, level, ct)` | public | **fehlerisoliert** — `try/catch` mit `Debug.WriteLine`-Fallback (Zeile 105); No-op bei `!_enabled` |

## `DebugLogCategory` / `DebugLogLevel`
Dateien: `src/Reporter.Core/Services/DebugLogCategory.cs`, `DebugLogLevel.cs`

Konstanten-Klassen (keine Enums): `DebugLogCategory` = `Lifecycle`, `Sync`, `Exception`, `Notification`, `Settings`, `Report`; `DebugLogLevel` = `Info`, `Warning`, `Error`. Für Punkt 6 ist `Lifecycle`/`Error` das vorgesehene Muster.

## `DebugReportService`
Datei: `src/Reporter.Core/Services/DebugReportService.cs`

Datenfluss-Quelle für die Datenschutzerklärung (Punkte 1/2):

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DebugReportRecipient` | public static | Empfängeradresse aus Assembly-Metadaten `DebugReportRecipient` (gesetzt in `Directory.Build.props`: `mstromberg84+reporter@gmail.com`), Fallback `debug@example.com` |
| `MaxSyncLogEntries` / `MaxDebugLogEntries` | public const | 50 / 200 |
| `IsSupported` | public | delegiert an `IEmailService.IsSupported` |
| `SendReportAsync(CancellationToken)` | public | sammelt `AppDeviceInfo` (`IDeviceInfoProvider`), `IsOnline`, `Settings`, Feed-Liste **inkl. URLs**, `SyncLog` und Session-`DebugLogEntry`s; übergibt den Body via `IEmailService.ComposeAsync` an den System-Mail-Client (Anwender sendet selbst) |
| `BuildBody(...)` | private | Plain-Text-Body mit Abschnitten AppInfo/Device/Network/Settings/FeedHealth/SyncLog/SessionLog |
| `LogReportEntryAsync(...)` | private | Report-Ereignisse ins Session-Log (`DebugLogCategory.Report`) |

## `DemoContentService`
Datei: `src/Reporter.Core/Services/DemoContentService.cs`

Grundlage des Review-Hinweises „kein Login — Demo-Feed beim ersten Start" (Punkt 12):

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `DemoCategoryName` / `DemoFeedUrl` / `DemoFeedTitle` | public const | `„News"`, `https://www.apple.com/newsroom/rss-feed.rss`, `„Apple Newsroom"` |
| `EnsureSeededAsync(CancellationToken)` | public | No-op außer bei `FirstRunState.ShouldSeedDemoContent`; legt Kategorie „News" (vorhandene wird per `OrdinalIgnoreCase` wiederverwendet) und den Demo-Feed mit `NotificationsEnabled = false` an; erfolgreicher Seed schreibt `Lifecycle`/`Info`-Logeintrag |

Wird aufgerufen von `App.OnStart` (Zeilen 53–54), fehlerisoliert.

## `FeedSearchService`
Datei: `src/Reporter.Core/Services/FeedSearchService.cs`

Datenfluss-Quelle für die Datenschutzerklärung: sendet den eingegebenen Suchbegriff an `https://feedsearch.dev/api/v1/search` (`DirectoryEndpoint`, Zeile 17; Override `REPORTER_FEEDSEARCH_ENDPOINT`) und ruft für die Autodiscovery die anwenderbestimmte Website plus Standard-Feedpfade ab — dabei geht die Geräte-IP an Fremdhosts.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SearchAsync(string, CancellationToken)` | public | Verzeichnisabfrage (`skip_crawl=true`) + Autodiscovery im gemeinsamen 2-s-Budget; `FeedSearchUnavailableException` nur bei Ausfall beider Quellen |
| `SearchDirectoryAsync`, `DiscoverFeedsAsync`, `ProbeStandardPathsAsync`, `ExtractFeedLinks`, `IsFeedMediaType`, `GetString` | private | Directory-GET, `<link rel="alternate">`-Parsing im `<head>`, Probing von `/feed`, `/rss`, `/rss.xml`, `/atom.xml`, `/feed.xml`, `/index.xml` |

## `FeedSyncService` / `FeedIconService`
Dateien: `src/Reporter.Core/Services/FeedSyncService.cs`, `FeedIconService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `FeedSyncService.SyncFeedAsync(Guid, ct)` / `SyncAllAsync(ct)` | public | ruft anwenderbestimmte Feed-URLs per DI-`HttpClient` (30 s Timeout) ab, parst RSS 2.0/Atom 1.0/Atom 0.3, persistiert Items; Fehlerklassifikation via `FeedSyncErrorKind.Classify` |
| `FeedIconService.FindFaviconUrlAsync(siteUrl, ct)` / `TryFindFaviconUrlAsync(feedUrl, siteUrl, ct)` | public | Favicon-Discovery auf der Feed-Website (`<link rel="icon…">` + `/favicon.ico`-Fallback); strikt fehlerisoliert (`null` statt Exception) |

Beide nutzen den gemeinsamen `HttpClient`-Singleton — Netzwerkzugriff auf anwenderdefinierte Fremdhosts (relevant für ATS-Begründung `NSAllowsArbitraryLoads` und Datenschutz-Doku).

## `EmailService` / `DeviceInfoProvider` / `NetworkStatusService`
Dateien: `src/Reporter/Services/EmailService.cs`, `DeviceInfoProvider.cs`, `NetworkStatusService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `EmailService.IsSupported` / `ComposeAsync(...)` | public | System-Mail-Compose via `Microsoft.Maui.ApplicationModel.Communication.Email`; Plattformfehler → `false` |
| `DeviceInfoProvider.GetSnapshot()` | public | `AppDeviceInfo` aus `AppInfo`/`DeviceInfo` |
| `NetworkStatusService` | public class | `IsOnline` + `ConnectivityChanged` über `Connectivity.Current`, Dispatch auf UI-Thread |

## Plattform-Service-Muster (Anknüpfungspunkt Punkt 8)

`LocalNotificationService` (`src/Reporter/Services/LocalNotificationService.cs`) und `BackgroundRefreshService` (`src/Reporter/Services/BackgroundRefreshService.cs`) zeigen das bestehende Gateway-Muster: Interface in `Reporter.Core`, eine gemeinsame Implementierungsdatei mit `#if IOS`-Blöcken (`Foundation`, `UserNotifications`, `BackgroundTasks`, `UIKit`), auf anderen Targets No-op bzw. `IsSupported == false`. `BackgroundRefreshService.RefreshTaskIdentifier = "de.martinstromberg.reporter.feedrefresh"` muss mit `BGTaskSchedulerPermittedIdentifiers` in der `Info.plist` übereinstimmen.

## `NotificationDelegate`
Datei: `src/Reporter/Platforms/iOS/NotificationDelegate.cs` (nur iOS)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `WillPresentNotification(...)` | public override | unterdrückt Vordergrund-Präsentation (`UNNotificationPresentationOptions.None`) |
| `DidReceiveNotificationResponse(...)` | public override async void | Deep-Link-Navigation (`Shell.Current.GoToAsync("articledetail?itemId=…")`/`"//unread"`); Fallback beim Kaltstart: `Launcher.Default.OpenAsync(link)` (**Zeile 73** — alternatives Muster zum Öffnen externer URLs unter iOS) |

## `AppDelegate` (iOS)
Datei: `src/Reporter/Platforms/iOS/AppDelegate.cs` (nur iOS)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `FinishedLaunching(...)` | public override | setzt `NotificationDelegate`, verlagert `RegisterBackgroundFetchTask` auf den nächsten Main-Queue-Durchlauf |
| `RegisterBackgroundFetchTask()` | private | `BGTaskScheduler.Shared.Register(RefreshTaskIdentifier, …)`, fehlerisoliert mit `DebugLogCategory.Sync`-Log |
| `HandleRefreshTaskAsync(BGAppRefreshTask)` | private | führt `IScheduledSyncRunner.RunAsync` fehlerisoliert aus, `SetTaskCompleted(success)` |

## `FeedsPage` / `CategoriesPage` (iPad-Popover-Bezug, Punkt 7)
Dateien: `src/Reporter/Views/FeedsPage.xaml.cs`, `src/Reporter/Views/CategoriesPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `FeedsPage.OnFeedTapped(...)` | private | `DisplayActionSheetAsync` (**Zeile 79**) mit Feed-Aktionen; auf iPad wird das Sheet als Popover gerendert |
| `FeedsPage.ChangeCategoryAsync(...)` | private | zweites `DisplayActionSheetAsync` (**Zeile 154**) für die Kategoriewahl |
| `CategoriesPage.OnCategoryTapped(...)` | private | `DisplayActionSheetAsync` (**Zeile 48**) mit Bearbeiten/Löschen |

Weitere Dialoge: `DisplayPromptAsync` (Rename, Zeile 119), `DisplayAlertAsync` (Lösch-/Fehler-/Subscribe-Bestätigungen).
