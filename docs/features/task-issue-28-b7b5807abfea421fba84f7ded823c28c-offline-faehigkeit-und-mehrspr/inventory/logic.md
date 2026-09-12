# Logik — Bestandsaufnahme

Betroffene Logikklassen für die Anforderung „Offline-Fähigkeit und Mehrsprachigkeit" (Issue #28).

## `UnreadViewModel`

Datei: `src/Reporter.Core/ViewModels/UnreadViewModel.cs`

ViewModel der Ungelesen-Seite. Basisklasse `BaseViewModel` (`CommunityToolkit.Mvvm.ComponentModel.ObservableObject`). Konstruktor-Abhängigkeiten: `IItemRepository`, `ICategoryRepository`, `IFeedSyncService` (Zeilen 41–54). **Keine Netzwerk-/Connectivity-Abhängigkeit vorhanden.**

Commands (alle `AsyncRelayCommand`, im Konstruktor verdrahtet): `LoadCommand`, `LoadMoreCommand` (CanExecute `!IsLoading && HasMore`), `RefreshCommand` (ohne CanExecute-Bedingung, Zeile 49), `MarkAllReadCommand`, `SelectCategoryCommand`, `ToggleSavedCommand`, `MarkReadCommand`.

| Methode / Eigenschaft | Sichtbarkeit | Kurzbeschreibung |
|-----------------------|--------------|------------------|
| `LoadAsync` | private | Lädt Kategorien, Filter-Chips und erste Artikel-Seite (nur lokale Repositories) |
| `LoadMoreAsync` / `LoadPageAsync` | private | Paging über `IItemRepository.GetUnreadByDateAsync` |
| `RefreshAsync` | private | `IsSyncing = true` → `IFeedSyncService.SyncAllAsync` → bei `FeedHealth.Error` `ErrorMessage = result.Message ?? AppResources.SyncStatusError`; Exceptions → `ErrorMessage = ex.Message`; danach `LastSyncText` + `LoadAsync` (Zeilen 321–345). Keine Offline-Vorabprüfung |
| `MarkAllReadAsync` | private | `MarkAllAsReadAsync` + Reload |
| `SelectCategoryAsync` / `ToggleSavedAsync` / `MarkReadAsync` | private | Filter- und Artikelaktionen, rein lokal |
| `IsSyncing` | public | Setter ruft `RefreshCommand.NotifyCanExecuteChanged()` (Zeilen 146–156) — CanExecute wird benachrichtigt, obwohl `RefreshCommand` ohne Bedingung erstellt wurde |
| `ErrorMessage` / `HasError` | public | Fehlertext-Anzeige |
| `Title` | public | Initial `AppResources.PageTitleUnread` (Zeile 22) — bereits lokalisiert |
| `UpdateUnreadCountText` | private | `"{n} {AppResources.LabelUnreadArticles} • {LastSyncText}"` (Zeilen 434–439) |

Abonnierte Events: keine. Publizierte Events: keine (nur `INotifyPropertyChanged` via `ObservableObject`).

## `FeedsViewModel`

Datei: `src/Reporter.Core/ViewModels/FeedsViewModel.cs`

ViewModel der Feed-Verwaltung. Abhängigkeiten: `IFeedRepository`, `ICategoryRepository`, `IFeedSyncService`, optional `ILocalNotificationService?` (Zeilen 37–53). **Keine Connectivity-Abhängigkeit.**

Commands: `LoadCommand`, `SaveCommand`, `EditCommand`, `DeleteCommand`, `RefreshCommand` (`AsyncRelayCommand<FeedListItem?>` mit CanExecute `_ => !IsSyncing`, Zeile 51), `RefreshAllCommand` (CanExecute `() => !IsSyncing`, Zeile 52).

| Methode / Eigenschaft | Sichtbarkeit | Kurzbeschreibung |
|-----------------------|--------------|------------------|
| `RefreshAsync` | private | Einzel-Feed-Sync via `SyncFeedAsync`; Fehler → `ErrorMessage = result.Message ?? AppResources.SyncStatusError` (Zeilen 317–345). Keine Offline-Prüfung |
| `RefreshAllAsync` | private | `SyncAllAsync`, gleiches Fehlermuster (Zeilen 347–375) |
| `IsSyncing` | public | Setter benachrichtigt CanExecute beider Refresh-Commands (Zeilen 141–152) |
| `NotificationsSupported` | public | `=> _localNotificationService?.IsSupported == true` — Beispiel für optionale Gateway-Injection |
| `SaveAsync` / `EditAsync` / `DeleteAsync` | private | Formular-Logik; Validierungsmeldungen bereits aus `AppResources` (`ErrorFeedUrlInvalid`, `ErrorFeedTitleEmpty`, `ErrorFeedDuplicate`) |

## `ArticleDetailViewModel`

Datei: `src/Reporter/ViewModels/ArticleDetailViewModel.cs` (Namespace `Reporter.Core.ViewModels`, liegt aber im MAUI-Projekt `Reporter` — **nicht** in `Reporter.Core`; wird von `Reporter.Tests` nicht referenziert)

Abhängigkeiten: `IItemRepository`, `IFeedRepository`, `ISettingsRepository` (Zeilen 57–70). Verwendet MAUI-APIs (`Browser.OpenAsync`, `Share.RequestAsync`, `Shell.Current.GoToAsync`). **Keine Connectivity-Abhängigkeit.**

Commands: `GoBackCommand`, `ToggleSavedForLaterCommand`, `ToggleMarkReadCommand`, `ToggleAutoMarkReadCommand` (`RelayCommand`), `OpenInBrowserCommand`, `ShareCommand`, `ToggleFontSizeCommand`.

| Methode / Eigenschaft | Sichtbarkeit | Kurzbeschreibung |
|-----------------------|--------------|------------------|
| `LoadAsync(Guid itemId)` | public | Lädt Settings (mit Fallback-Defaults), Item und Feed; setzt `AutoMarkReadLabel` lokalisiert; ruft `RebuildHtml`; startet `MarkReadDelayedAsync` (Zeilen 238–296) |
| `RebuildHtml` | private | `SanitizeHtml(Item?.ContentHtml)` + HTML-Wrapper mit CSP (`img-src * data: blob:` erlaubt externe Bilder, Zeile 363); Schriftgröße 19/22 px je nach `FontSizeIndex` (Zeilen 348–400). Keine Link-Neutralisierung |
| `SanitizeHtml` | private | Regex-Pipeline: entfernt `script`/`iframe`/`style`/`object`/`embed`/`form`/`link`/`meta`/`base`/`applet`/`audio`/`video`, `on*`-Attribute und `javascript:` (Zeilen 329–346) |
| `CalculateReadingTime` | private static | Wortzahl / 200 wpm → **`$"{minutes} Min. Lesezeit"` — hartcodiert deutsch** (Zeile 320) |
| `BookmarkButtonLabel` | public | **`"Lesezeichen entfernen"` / `"Lesezeichen setzen"` — hartcodiert deutsch** (Zeile 191) |
| `MarkAsReadButtonLabel` | public | **`"Bereits gelesen"` / `"Als gelesen markieren"` — hartcodiert deutsch** (Zeile 196) |
| `FontSizeLabel` | public | `"A"`/`"A+"` (Zeile 186) |
| `AutoMarkReadLabel` | public | Bereits lokalisiert via `AppResources.ArticleAutoMarkReadDelayFormat`/`ArticleAutoMarkReadDisabled` (Zeilen 43, 267–269) |
| `OpenInBrowserAsync` | private | `Browser.OpenAsync(Item.Link, BrowserLaunchMode.SystemPreferred)` — keine Offline-/Fehlerbehandlung (Zeilen 484–492) |
| `ShareAsync` | private | `Share.RequestAsync` (Zeilen 494–506) |
| `CancelAutoMarkRead` / `MarkReadDelayedAsync` / `OnAutoMarkReadChanged` | public/private | Timer-Logik für Auto-Gelesen |

Abonnierte Events: keine (kein `ConnectivityChanged` o. ä.). `HtmlSource` wird nur bei `LoadAsync`/`FontSizeIndex`-Änderung neu gebaut.

## `SettingsViewModel`

Datei: `src/Reporter.Core/ViewModels/SettingsViewModel.cs`

Abhängigkeiten: `ISettingsRepository`, `IKeywordRepository`, `IAutoRefreshService`, `IAppThemeService`, `TimeProvider?`, `ILocalNotificationService?` (Zeilen 65–108).

| Methode / Eigenschaft | Sichtbarkeit | Kurzbeschreibung |
|-----------------------|--------------|------------------|
| `ThemeOptions` / `SelectedTheme` | public | Optionsliste aus `SettingsValues.Theme*` + `AppResources.SettingsTheme*`; `SelectedTheme`-Setter → `PersistOnChange` (Zeilen 96–101, 427–437) — Muster für einen möglichen Sprach-Picker |
| `PersistAsync` | private | Baut `Settings`-Objekt, `ISettingsRepository.SaveAsync`; bei Theme-Änderung `_appThemeService.ApplyTheme`, bei AutoRefresh-Änderung `_autoRefreshService.ApplySettingsAsync` (Zeilen 636–678) |
| `LoadAsync` | private | Lädt Settings + Keywords, setzt alle Optionen (Zeilen 462–504) |
| `NotificationAuthorizationDenied` | public event `Func<Task>?` | Von `SettingsPage` abonniert (Zeilen 283, `SettingsPage.xaml.cs` Zeilen 28/38) |

Kein Sprach-/Locale-Konzept vorhanden.

## `FeedSyncService`

Datei: `src/Reporter.Core/Services/FeedSyncService.cs`

Implementiert `IFeedSyncService`. Abhängigkeiten: `IFeedRepository`, `IItemRepository`, `ISyncLogRepository`, `HttpClient`, `INotificationService`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|--------------|------------------|
| `SyncFeedAsync` | public | Legt `SyncLog` an, lädt Feed, `RunSyncAsync`; **fängt alle Exceptions außer `OperationCanceledException`** → `FeedHealth.Error` + `SyncLog`-Eintrag (Zeilen 64–74). Kein Crash bei fehlendem Netz, aber Fehler wird als Sync-Fehler protokolliert und an Aufrufer gemeldet |
| `SyncAllAsync` | public | Iteriert alle Feeds, aggregiert Status/Messages (Zeilen 78–109) |
| `RunSyncAsync` | private | HTTP-Abruf via `_httpClient.GetStreamAsync(feed.Url)`, `SyndicationFeed.Load`, Dedupe über `GuidOrHash`, `DetermineStatus`, `UpdateFeedHealthAsync`, `UpdateLogAsync`, `INotificationService.NotifyNewItemsAsync` (try/catch-geschützt, Zeilen 111–186) |
| `DetermineStatus` | private static | `Warning` bei drastisch weniger Items oder >30 Tage ohne neue Items (Zeilen 188–202) |

SyncLog-Meldungen sind englisch hartcodiert (`"Feed not found."`, `"Synchronization failed: …"`, `"Synchronized {0} items, {1} new."` u. a.). **Keine Offline-Vorabprüfung** — Netzwerkfehler laufen in den generischen Exception-Pfad.

## `AutoRefreshService`

Datei: `src/Reporter.Core/Services/AutoRefreshService.cs`

Implementiert `IAutoRefreshService`. Abhängigkeiten: `ISettingsRepository`, `IFeedSyncService`, `TimeProvider?`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|--------------|------------------|
| `StartAsync` | public | Lädt Settings → `ApplySettingsAsync` |
| `ApplySettingsAsync` | public | Stoppt Loop; bei `AutoRefreshEnabled` startet `RunLoopAsync` mit geclamped Intervall 1–1440 min (Zeilen 43–64) |
| `StopAsync` / `StopLoopAsync` | public/private | Beendet den `PeriodicTimer`-Loop sauber |
| `RunLoopAsync` | private | `PeriodicTimer` → pro Tick `_feedSyncService.SyncAllAsync`; Exceptions (außer Cancellation) werden geloggt, Loop läuft weiter (Zeilen 109–134). **Keine Offline-Prüfung** — jeder Tick feuert einen Sync, der offline als `FeedHealth.Error` im `SyncLog` landet |

## `App` (`App.xaml.cs`)

Datei: `src/Reporter/App.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|--------------|------------------|
| `OnStart` | protected override | `Database.MigrateAsync()` → `IRetentionCleanupService.CleanupAsync` (try/catch) → Settings laden + `IAppThemeService.ApplyTheme(settings.Theme)` (try/catch, Zeilen 51–62) → `IAutoRefreshService.StartAsync` (try/catch). **Keine Sprachanwendung, kein Connectivity-Monitoring** |
| `CreateWindow` | protected override | `Window(AppShell)`; unter Windows 390×844 pt |

## `AppShell` (`AppShell.xaml.cs`)

Datei: `src/Reporter/AppShell.xaml.cs`

Baut die `TabBar` programmatisch im Konstruktor; Tab-Titel aus `AppResources.TabUnread`/`TabFeeds`/`TabLater`/`TabCategories`/`TabSettings` (Zeilen 22–35) — bereits lokalisiert, aber einmalig beim Aufbau aufgelöst. Route `articledetail` registriert (Zeile 20); `ShellContent.Route = "unread"` auf dem Ungelesen-Tab.

## `MauiProgram`

Datei: `src/Reporter/MauiProgram.cs`

DI-Registrierung (Zeilen 42–70): Repositories, `HttpClient` (30 s Timeout), `IFeedSyncService`→`FeedSyncService`, `IRetentionCleanupService`, `IKeywordMatcher`, `IAutoRefreshService`, `IAppThemeService`→`AppThemeService`, `INotificationService`→`NotificationService`, `ILocalNotificationService`→`LocalNotificationService`, ViewModels (`ArticleDetailViewModel` als `AddTransient`, Rest `AddSingleton`), Pages, `AppShell`. **Keine Connectivity-/Netzwerk-Service-Registrierung.**

## `ArticleDetailPage`

Dateien: `src/Reporter/Views/ArticleDetailPage.xaml` + `.xaml.cs`

- `IQueryAttributable.ApplyQueryAttributes` liest `itemId` → `_viewModel.LoadAsync` (xaml.cs Zeilen 26–40); `OnDisappearing` → `CancelAutoMarkRead` (Zeilen 43–47).
- **Das `WebView` (`x:Name="ArticleWebView"`, XAML Zeilen 108–115) hat keinen `Navigating`-Handler** — Link-Klicks navigieren intern im WebView (entgegen der Annahme in der Anforderung existiert kein Handler zum Erweitern; er müsste neu ergänzt werden).
- Hartcodierte deutsche Texte im XAML: `TargetNullValue='Artikel'` (Z. 9), `"Gelesen"` (Z. 26), `"Vollständiger Artikel verfügbar"` (Z. 128), `"Im Browser öffnen"` (Z. 139), `SemanticProperties.Description` `"Zurück"` (Z. 160), `"Schriftgröße wechseln"` (Z. 212), `"Teilen"` (Z. 257), `"Im Browser öffnen"` (Z. 280). Trennzeichen-Labels `"•"` (Z. 81, 89) sind sprachneutral.

## `UnreadPage`

Dateien: `src/Reporter/Views/UnreadPage.xaml` + `.xaml.cs`

- Sync-Button: `Border`+`Path` mit `TapGestureRecognizer Command="{Binding RefreshCommand}"` (XAML Zeilen 21–41) — **kein visueller Offline-/Disabled-Zustand**; zusätzlich `RefreshView` (Pull-to-Refresh) an `RefreshCommand` gebunden (Zeilen 105–107).
- Alle Texte bereits über `{x:Static strings:AppResources.*}` bzw. Bindings; `OnFilterClicked` nutzt `AppResources.*` (xaml.cs Zeilen 54–58).

## `FeedsPage`

Dateien: `src/Reporter/Views/FeedsPage.xaml` + `.xaml.cs`

- `RefreshView` → `RefreshAllCommand` (XAML Zeilen 75–77); Feed-Aktionen über `OnFeedTapped` → `DisplayActionSheetAsync` mit `AppResources.*`-Texten (xaml.cs Zeilen 39–75). Vollständig lokalisiert; **kein Offline-Hinweis** am Pull-to-Refresh.

## `SettingsPage`

Dateien: `src/Reporter/Views/SettingsPage.xaml` + `.xaml.cs`

- Sektionen: Aufbewahrungsdauer, Keyword-Filter, Synchronisation & Lesefluss, Benachrichtigungen & Ruhezeiten, Erscheinungsbild (Theme-`Picker`, XAML Zeilen 424–445). Alle Texte aus `AppResources`; **keine Sprach-Sektion**.
- xaml.cs: abonniert `NotificationAuthorizationDenied` → `DisplayAlertAsync` mit `AppResources.*` + `AppInfo.Current.ShowSettingsUI()`.

## `AppThemeService` / `LocalNotificationService` / `NotificationService`

Dateien: `src/Reporter/Services/AppThemeService.cs`, `src/Reporter/Services/LocalNotificationService.cs`, `src/Reporter.Core/Services/NotificationService.cs`

- `AppThemeService.ApplyTheme` setzt `Application.Current.UserAppTheme` anhand `SettingsValues.Theme*` — Referenzmuster für den neuen Gateway-Service (Interface in `Reporter.Core`, Implementierung im MAUI-Projekt).
- `LocalNotificationService` implementiert `ILocalNotificationService` plattformabhängig (`#if IOS`, sonst No-ops/`IsSupported=false`).
- `NotificationService.NotifyNewItemsAsync` wertet Feed-/Global-Schalter, Ruhezeiten und Keyword-Filter aus; Summary-Text via `string.Format(CultureInfo.CurrentCulture, AppResources.NotificationSummaryFormat, …)` (Zeile 79) — bereits lokalisiert.

## ResX-Lokalisierung

- `src/Reporter.Core/Resources/Strings/AppResources.resx` (neutral/Englisch, 112 Schlüssel), `AppResources.de.resx` (Deutsch, 112 Schlüssel — identischer Schlüsselbestand), generierter `AppResources.Designer.cs` (`PublicResXFileCodeGenerator`, `internal`-Namespace `Reporter.Core.Resources.Strings`, statische `Culture`-Eigenschaft vorhanden).
- Verwendete Schlüssel u. a.: `PageTitle*`, `Tab*`, `Placeholder*`, `Button*`, `Error*`, `Settings*`, `HealthStatus*Label`, `ArticleAutoMarkRead*`, `Notification*`, `SyncStatusError`. **Keine Offline-/Sprach-Schlüssel** (`OfflineHint`, `ArticleReadLabel`, `ArticleOpenInBrowser`, `Accessibility*`, `SettingsLanguage*` existieren nicht).
- `.githooks/translation-check.py` (pre-commit) prüft u. a. Schlüsselgleichstand neutral ↔ `.de.resx`.
