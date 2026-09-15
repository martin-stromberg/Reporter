<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik

## `FeedSyncService`
Datei: `src/Reporter.Core/Services/FeedSyncService.cs` (implementiert `IFeedSyncService`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `FeedSyncService(...)` | public ctor | Injiziert `IFeedRepository`, `IItemRepository`, `ISyncLogRepository`, `HttpClient`, `INotificationService`, `INetworkStatusService`, `IKeywordFilter`, `IFeedIconService`, optional `IDebugLogService`. |
| `SyncFeedAsync(Guid feedId, CancellationToken)` | public | Offline-Guard (`INetworkStatusService.IsOnline` → `SyncResult(Error, 0, OfflineHint)`), legt `SyncLog` an, lädt Feed, ruft `RunSyncAsync`; Fehler → `FeedHealth.Error` + `SyncLog` + `IDebugLogService` (`DebugLogCategory.Sync`). |
| `SyncAllAsync(CancellationToken)` | public | Iteriert alle Feeds, ruft je `SyncFeedAsync`, aggregiert `SyncResult` (Status/NewItems/Message). |
| `RunSyncAsync(Feed, SyncLog, CancellationToken)` | private | Lädt Feed-XML via `HttpClient.GetStreamAsync` + `SyndicationFeed.Load`; `CollectNewItems` (Dedup via `GuidOrHash`, Keyword-Filter); speichert neue Items; aktualisiert Health/Log; **ruft bei `newItemEntities.Count > 0` fehlerisoliert `_notificationService.NotifyNewItemsAsync(feed, newItemEntities, cancellationToken)` auf (Zeilen 188–204)** — Fehler im Benachrichtigungspfad werden per `Debug.WriteLine` + `IDebugLogService` (`DebugLogCategory.Sync`, Warning) protokolliert und verfälschen das Sync-Ergebnis nicht. **Kein Auslöser-Parameter.** |
| `CollectNewItems(...)` | private | Filtert bekannte GUIDs und Keyword-Treffer; erzeugt `Item`-Entities. |
| `ResolveFeedTitle` / `IsHostPlaceholderTitle` | private static | Platzhalter-Titel-Auflösung aus dem Feed-Dokument. |
| `DetermineStatus` | private static | Health-Regel (Warning bei Abruf < 50 % des Bestands bzw. > 30 Tage keine neuen Items). |
| `UpdateFeedHealthAsync` | private | Persistiert Health, ggf. aufgelösten Titel, Favicon, `LastError*`. |
| `TryFindFaviconUrlAsync` | private | Fehlerisolierter Favicon-Lookup über `IFeedIconService`. |
| `UpdateLogAsync` | private | Schließt den `SyncLog`-Eintrag ab. |
| `GetContentHtml` / `NormalizeGuidOrHash` | private static | Content-Extraktion und Dedup-Schlüssel. |

Publizierte/abonnierte Events: keine.

## `NotificationService`
Datei: `src/Reporter.Core/Services/NotificationService.cs` (implementiert `INotificationService`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `NotificationService(...)` | public ctor | Injiziert `ISettingsRepository`, `IKeywordFilter`, `ILocalNotificationService`, optional `TimeProvider`. |
| `NotifyNewItemsAsync(Feed, IReadOnlyList<Item>, CancellationToken)` | public | Entscheidungskette: `feed.NotificationsEnabled` → `settings.NotificationsEnabled` → `IsQuietHoursActive` → Keyword-Filter (`MatchesAny` auf Titel/ContentHtml) → `NotificationSummaryEnabled` (eine Summary-Mitteilung mit stabilem Hash-Identifier + `feedId` im `userInfo`) oder eine Mitteilung pro Item (Identifier = `item.Id`, `userInfo` mit `itemId`/`link`). Ausgabe ausschließlich über `_localNotificationService.ShowAsync`. **Kein App-Zustands-/Auslöser-Bezug.** |
| `BuildItemUserInfo` | private static | Baut `userInfo` (`itemId`, optional `link`). |
| `IsQuietHoursActive` | private | Ruhezeiten-Auswertung über `TimeProvider`. |
| `BuildSummaryIdentifier` | private static | Stabiler Identifier `"{feedId}-{sha256(itemIds)}"`. |
| `Truncate` | private static | Kürzt die Titel-Liste auf 160 Zeichen. |

## `AutoRefreshService`
Datei: `src/Reporter.Core/Services/AutoRefreshService.cs` (implementiert `IAutoRefreshService`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `AutoRefreshService(...)` | public ctor | Injiziert `ISettingsRepository`, `IFeedSyncService`, `INetworkStatusService`, optional `TimeProvider` und `IDebugLogService`. |
| `StartAsync(CancellationToken)` | public | Lädt Settings, ruft `ApplySettingsAsync`; bei `RefreshOnStartupEnabled && IsOnline` startet `RunStartupSyncAsync` fire-and-forget. |
| `RunStartupSyncAsync` | private | Einmaliger Start-Abruf via `_feedSyncService.SyncAllAsync()`; Fehler → `Debug.WriteLine` + `IDebugLogService` (`DebugLogCategory.Sync`, Error). |
| `ApplySettingsAsync(Settings)` | public | Unter `_stateLock`: Loop stoppen; bei `AutoRefreshEnabled` neuen `PeriodicTimer`-Loop mit `RefreshIntervalMinutes` (Clamp 1–1440) starten. |
| `StopAsync` | public | Stoppt den Loop unter `_stateLock`. |
| `StopLoopAsync` | private | Kancelliert `CancellationTokenSource`, awaitet den Loop-Task. |
| `RunLoopAsync(TimeSpan, CancellationToken)` | private | `PeriodicTimer`-Loop; pro Tick Offline-Prüfung, dann `_feedSyncService.SyncAllAsync(cancellationToken)` sequenziell; Fehler pro Tick abgefangen + Debug-Log. |

Hinweis: Der Loop läuft nur, solange die App läuft (`architektur.md` Abschnitt „Timer-Lebensdauer"). Wird von `App.OnStart` fehlerisoliert gestartet und von `SettingsViewModel.PersistAsync` bei Änderungen an `AutoRefreshEnabled`/`RefreshIntervalMinutes` rekonfiguriert.

## `LocalNotificationService`
Datei: `src/Reporter/Services/LocalNotificationService.cs` (implementiert `ILocalNotificationService`, MAUI-Projekt)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `IsSupported` | public get | `true` nur unter `#if IOS`, sonst `false` (No-Op-Plattformen Windows/Android/MacCatalyst). |
| `RequestAuthorizationAsync` | public | iOS: `EnsureAuthorizedAsync` (fragt bei `NotDetermined` `Alert|Badge|Sound` an); andere Plattformen: `false`. |
| `GetAuthorizationStatusAsync` | public | iOS: `UNUserNotificationCenter.Current.GetNotificationSettingsAsync` → `MapStatus`; sonst `Unsupported`. |
| `ShowAsync(title, body, identifier, userInfo, ct)` | public | iOS: `EnsureAuthorizedAsync`, dann `UNMutableNotificationContent` (Titel, Body, `Sound = UNNotificationSound.Default`, `UserInfo`) + `UNNotificationRequest.FromIdentifier` + `AddNotificationRequestAsync`; andere Plattformen: No-Op. |
| `EnsureAuthorizedAsync` / `MapStatus` / `BuildUserInfo` / `IsAuthorized` | private static (`#if IOS`) | Berechtigungs-Hilfen und `NSDictionary`-Aufbau. |

## `App`
Datei: `src/Reporter/App.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `App(IServiceProvider)` | public ctor | Hält den MAUI-`IServiceProvider` (`_services`) — potenzieller Auflösungspunkt für `IFeedSyncService` aus einem BGTask-Handler. |
| `OnStart` | protected override | Migration, `IDebugLogService.BeginSessionAsync`, drei fehlerisolierte Blöcke: Retention-Cleanup, Theme (`IAppThemeService`), `INetworkStatusService`-Init sowie `IAutoRefreshService.StartAsync`. |
| `OnSleep` / `OnResume` | protected override | Loggen via `IDebugLogService` (`DebugLogCategory.Lifecycle`). |
| `CreateWindow` | protected override | Erzeugt `Window(AppShell)`; auf Windows 390×844. |
| `OnUnhandledException` / `OnUnobservedTaskException` | private | Fire-and-forget Logging (`DebugLogCategory.Exception`). |

## `MauiProgram`
Datei: `src/Reporter/MauiProgram.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateMauiApp` | public static | DI-Registrierung aller Services als Singletons — u. a. `IFeedSyncService → FeedSyncService`, `INotificationService → NotificationService`, `ILocalNotificationService → LocalNotificationService`, `IAutoRefreshService → AutoRefreshService`, `IDebugLogService → DebugLogService` (Zeilen 46–83). Kein Hintergrundabruf-Service registriert. |
| `ApplyPersistedLanguage` | private static | Migration + Sprachanwendung vor `CreateWindow`, fehlerisoliert. |

## `AppDelegate` (iOS)
Datei: `src/Reporter/Platforms/iOS/AppDelegate.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `_notificationDelegate` | private readonly Feld | Verwaltete `NotificationDelegate`-Instanz (weak Delegate-Property). |
| `CreateMauiApp` | protected override | `MauiProgram.CreateMauiApp()`. |
| `FinishedLaunching(UIApplication, NSDictionary)` | public override | Setzt `UNUserNotificationCenter.Current.Delegate = _notificationDelegate`; **kein `BGTaskScheduler`-Registrierung/-Scheduling vorhanden.** |

## `NotificationDelegate` (iOS)
Datei: `src/Reporter/Platforms/iOS/NotificationDelegate.cs` (`UNUserNotificationCenterDelegate`)

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `WillPresentNotification` | public override | Gibt **`UNNotificationPresentationOptions.Banner | List | Sound`** zurück (Zeile 22) — Mitteilungen erscheinen heute auch bei Vordergrund-App als Banner mit Ton. |
| `DidReceiveNotificationResponse` | public override async void | Tap-Handling: nur `IsDefaultAction`; liest `itemId`/`feedId`/`link` aus `UserInfo`, navigiert via `Shell.Current.GoToAsync` (`articledetail?itemId=…` oder `//unread`), Fallback `Launcher.OpenAsync(link)`; fehlerisoliert, `completionHandler()` im `finally`. |

## `Info.plist` (iOS)
Datei: `src/Reporter/Platforms/iOS/Info.plist`

Enthält `LSRequiresIPhoneOS`, `UIDeviceFamily`, Orientierungen, `CFBundleIdentifier` (`de.martinstromberg.reporter`), `NSAppTransportSecurity`. **Kein `UIBackgroundModes`, keine `BGTaskSchedulerPermittedIdentifiers`.**

## Sync-Aufrufer (ViewModels)

- `FeedsViewModel` (`src/Reporter.Core/ViewModels/FeedsViewModel.cs`): `RefreshAsync(FeedListItem?)` → `SyncAsync(() => _feedSyncService.SyncFeedAsync(feedId))` (Zeile 385); `RefreshAllAsync` → `SyncAsync(() => _feedSyncService.SyncAllAsync())` (Zeile 390). `SyncAsync` ist ein Reentrancy-Guard (`_isSyncInProgress`) mit Offline-Early-Return und Fehler-Mapping auf `SyncErrorMessage`. Hält optional `ILocalNotificationService` für `NotificationsSupported` (UI-Enablement der `FeedNotificationsEnabled`-Eigenschaft).
- `UnreadViewModel` (`src/Reporter.Core/ViewModels/UnreadViewModel.cs`): `RefreshAsync` → `_feedSyncService.SyncAllAsync()` (Zeile 362), Pull-to-Refresh mit Offline-Guard und Fehler-Mapping.
- `SettingsViewModel` (`src/Reporter.Core/ViewModels/SettingsViewModel.cs`): hält optional `ILocalNotificationService`; `NotificationsEnabled`-Setter startet `RequestNotificationAuthorizationAsync` (Berechtigungsanfrage, Fehler-Flags `NotificationPermissionDenied`/`NotificationPermissionNotDetermined`, Event `NotificationAuthorizationDenied`); `PersistAsync` ruft `IAutoRefreshService.ApplySettingsAsync` bei Änderung der Auto-Refresh-Einstellungen.
