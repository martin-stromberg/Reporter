← [Zurück zur Übersicht](index.md)

# Benachrichtigungen — Technischer Ablauf

## Übersicht

Nach jedem erfolgreichen Feed-Sync ruft `FeedSyncService` den `NotificationService` (`Reporter.Core`) mit dem Feed und den neu gespeicherten `Item`s auf. Der Service wertet die Benachrichtigungsregeln aus (Feed-Schalter, globaler Schalter, Ruhezeit, Keyword-Filter) und delegiert die Anzeige an `ILocalNotificationService`, das unter iOS das `UserNotifications`-Framework nutzt. Die iOS-Berechtigung wird beim erstmaligen Einschalten des globalen Schalters in den Einstellungen angefragt sowie lazy vor jedem Versand; `App.OnStart` enthält bewusst **keine** Berechtigungsanfrage. Das Antippen einer Benachrichtigung behandelt der `NotificationDelegate` (iOS) mit In-App-Navigation.

## Ablauf

### 1. Benachrichtigungsprüfung nach Feed-Sync

`FeedSyncService.RunSyncAsync` sammelt die neu eingefügten `Item`s in `newItemEntities` (Dublettenerkennung über `IItemRepository.GetByGuidOrHashAsync`, Unique-Index `(feed_id, guid_or_hash)`). Nach `UpdateFeedHealthAsync` und `UpdateLogAsync` wird bei nicht-leerer Liste `INotificationService.NotifyNewItemsAsync(feed, newItemEntities, cancellationToken)` in einem eigenen `try/catch` (alle Exceptions außer `OperationCanceledException` → `Debug.WriteLine`) aufgerufen — ein Fehler im Benachrichtigungspfad verändert `SyncResult` und Feed-Health nicht. Da `SyncFeedAsync` von allen Sync-Pfaden durchlaufen wird (manuell via `FeedsViewModel.RefreshAsync`/`RefreshAllAsync`, gesamt via `SyncAllAsync`, periodisch via `AutoRefreshService`), ist ein einziger Aufrufpunkt ausreichend.

`NotificationService.NotifyNewItemsAsync` wertet in dieser Reihenfolge aus:

1. `!feed.NotificationsEnabled || newItems.Count == 0` → Abbruch.
2. `ISettingsRepository.GetAsync` → `!settings.NotificationsEnabled` → Abbruch.
3. `IsQuietHoursActive(settings)` → aktive Ruhezeit verwirft alle Kandidaten (kein Nachholen).
4. `IKeywordRepository.GetAllAsync` → `keywordTexts`; pro Item `IKeywordMatcher.MatchesAny(item.Title, item.ContentHtml, keywordTexts)` → Treffer werden entfernt. Leere Restliste → Abbruch.
5. Modus-Verzweigung anhand `settings.NotificationSummaryEnabled`:
   - `false` (Einzelmodus): pro Item `ILocalNotificationService.ShowAsync(feed.Title, item.Title, item.Id.ToString(), userInfo, ct)` mit `userInfo = { itemId, link? }`.
   - `true` (Sammelmodus): genau ein `ShowAsync(feed.Title, body, identifier, { feedId }, ct)`; `body` = `string.Format(AppResources.NotificationSummaryFormat, count, titles)` (de: „{0} neue Artikel: {1}"), `titles` = kommagetrennte Artikeltitel, auf `MaxSummaryTitlesLength` = 160 Zeichen mit „…" gekürzt; `identifier` = `{feedId}-{SHA256-Hex(sortierte Item-Ids)}` (`BuildSummaryIdentifier`).

Beteiligte Komponenten:
- `FeedSyncService.RunSyncAsync` / `SyncFeedAsync` / `SyncAllAsync` — Erweiterungspunkt und Fehlerisolierung
- `NotificationService.NotifyNewItemsAsync` / `IsQuietHoursActive` / `BuildSummaryIdentifier` / `BuildItemUserInfo` / `Truncate` — Entscheidungslogik
- `ISettingsRepository`, `IKeywordRepository`, `IKeywordMatcher` — Regel-Eingaben
- `TimeProvider` — injizierbar (Standard `TimeProvider.System`), steuert die Ruhezeit-Auswertung (`GetLocalNow().TimeOfDay`, lokale Gerätezeit)
- `ILocalNotificationService.ShowAsync` — Plattformanzeige

### 2. Anzeige und Berechtigung unter iOS

`LocalNotificationService` (`src/Reporter/Services`) implementiert `ILocalNotificationService` mit `#if IOS`; auf anderen Targets ist jede Methode ein No-Op und `IsSupported == false`.

- `ShowAsync`: ruft zuerst `EnsureAuthorizedAsync` auf; bei fehlender Berechtigung Rückkehr ohne Anzeige. Danach `UNMutableNotificationContent` (`Title`, `Body`, `Sound = UNNotificationSound.Default`, `UserInfo` via `NSDictionary.FromObjectsAndKeys` aus den String-Paaren) und `UNNotificationRequest.FromIdentifier(identifier, content, trigger: null)` (sofortiger Versand) → `UNUserNotificationCenter.Current.AddNotificationRequestAsync`. iOS ersetzt zugestellte/ausstehende Requests mit identischem Identifier statt zu duplizieren (Dedup).
- `EnsureAuthorizedAsync`: `GetNotificationSettingsAsync` → `NotDetermined` löst `RequestAuthorizationAsync(Alert | Badge | Sound)` aus (lazy Fallback vor jedem Versand); als autorisiert gelten `Authorized`, `Provisional` und `Ephemeral`.
- `RequestAuthorizationAsync` / `IsAuthorizedAsync`: öffentliche Pendants für den Einstellungs-Flow (s. Schritt 3); auf Nicht-iOS immer `false`.

Beteiligte Komponenten:
- `LocalNotificationService` (`IsSupported`, `RequestAuthorizationAsync`, `IsAuthorizedAsync`, `ShowAsync`, `EnsureAuthorizedAsync`, `BuildUserInfo`, `IsAuthorized`)
- `UNUserNotificationCenter`, `UNMutableNotificationContent`, `UNNotificationRequest` (`UserNotifications`-Framework)

### 3. Berechtigungsanfrage in den Einstellungen

Die Anfrage erfolgt **nicht** beim App-Start, sondern kontextuell beim Einschalten des Hauptschalters:

- `SettingsViewModel.NotificationsEnabled`-Setter: beim Wechsel auf `true` (außerhalb von `LoadAsync`, `_isLoading`-Guard) wird `_ = RequestNotificationAuthorizationAsync()` gestartet — bei `IsSupported` ruft das `ILocalNotificationService.RequestAuthorizationAsync`; `NotificationPermissionDenied = !granted`. Beim Ausschalten wird `NotificationPermissionDenied` zurückgesetzt.
- Bei `!granted` feuert das Ereignis `NotificationAuthorizationDenied`; `SettingsPage.xaml.cs` (`OnNotificationAuthorizationDenied`, subscribed in `OnAppearing`, unsubscribed in `OnDisappearing`) zeigt `DisplayAlertAsync` mit `NotificationDeniedTitle`/`NotificationDeniedMessage` und den Schaltflächen `NotificationDeniedOpenSettings`/`ButtonCancel` → `AppInfo.Current.ShowSettingsUI()` öffnet die iOS-App-Einstellungen.
- `SettingsViewModel.LoadAsync` ruft am Ende `RefreshNotificationPermissionAsync`: nur wenn `IsSupported` und `NotificationsEnabled` aktiv sind, wird `IsAuthorizedAsync` abgefragt und `NotificationPermissionDenied = !authorized` gesetzt — so bleibt die Hinweiszeile auch nach einem System-seitigen Widerruf aktuell.
- `SettingsPage.xaml` zeigt die Hinweiszeile (`NotificationDeniedMessage` + Button `NotificationDeniedOpenSettings` → `OnOpenNotificationSettingsClicked` → `AppInfo.Current.ShowSettingsUI()`) per `MultiTrigger` nur bei `NotificationsEnabled && NotificationPermissionDenied`.

Beteiligte Komponenten:
- `SettingsViewModel.NotificationsEnabled` / `RequestNotificationAuthorizationAsync` / `RefreshNotificationPermissionAsync` / `NotificationPermissionDenied` / `NotificationAuthorizationDenied`
- `SettingsPage` (`OnAppearing`, `OnDisappearing`, `OnNotificationAuthorizationDenied`, `OnOpenNotificationSettingsClicked`)
- `ILocalNotificationService.RequestAuthorizationAsync` / `IsAuthorizedAsync`
- `AppInfo.Current.ShowSettingsUI` (MAUI Essentials)

### 4. Vordergrund-Darstellung und Tap-Handling (iOS)

`AppDelegate.FinishedLaunching` setzt `UNUserNotificationCenter.Current.Delegate = _notificationDelegate`; die `NotificationDelegate`-Instanz wird in einem Feld gehalten, weil die `Delegate`-Property schwach referenziert.

- `WillPresentNotification` → `Banner | List | Sound`: Benachrichtigungen sind auch sichtbar, während die App geöffnet ist (funktional nötig, da `AutoRefreshService` nur bei laufender App synct).
- `DidReceiveNotificationResponse`: liest `itemId`, `feedId` und `link` aus `Content.UserInfo`. Route: `itemId` vorhanden → `articledetail?itemId={itemId}` (wie `ArticleCardView`); sonst `feedId` vorhanden → `//unread` (`ShellContent.Route = "unread"` in `AppShell`). Navigation via `MainThread.InvokeOnMainThreadAsync` + `Shell.Current.GoToAsync` mit Null-Check; wenn die Shell beim Kaltstart noch nicht bereit ist und ein `link` existiert, Fallback `Launcher.Default.OpenAsync(link)` (Artikel im Browser). Alle Exceptions werden geschluckt — ein Antippen darf die App nicht abstürzen lassen; `completionHandler()` läuft im `finally`.

Beteiligte Komponenten:
- `AppDelegate.FinishedLaunching` — Delegate-Zuweisung
- `NotificationDelegate.WillPresentNotification` / `DidReceiveNotificationResponse`
- `AppShell` — Routen `articledetail` (`Routing.RegisterRoute`) und `unread` (`ShellContent.Route`)
- `ArticleDetailPage.ApplyQueryAttributes` — verarbeitet `itemId`

### 5. Pro-Feed-Schalter und Modus-Einstellung pflegen

- `FeedsPage.xaml`: `Switch` (`IsToggled="{Binding FeedNotificationsEnabled}"`) mit `FeedNotificationsLabel`/`FeedNotificationsHint` in der Feed-Bearbeitungskarte. `FeedsViewModel.EditAsync` befüllt aus `FeedListItem.NotificationsEnabled`, `SaveAsync` schreibt `NotificationsEnabled = FeedNotificationsEnabled` im Add- und Update-Pfad, `ResetForm` setzt auf `true` zurück.
- `SettingsPage.xaml`: `Switch` (`IsToggled="{Binding NotificationSummaryEnabled}"`) mit `SettingsNotificationSummaryLabel`/`SettingsNotificationSummaryHint` innerhalb des von `NotificationsEnabled` gesteuerten `Border` (erbt Deaktivierung + Opazität 0,4). `SettingsViewModel.NotificationSummaryEnabled` persistiert über `PersistOnChange` → `PersistAsync`; `LoadAsync` befüllt aus `settings.NotificationSummaryEnabled`.

Beteiligte Komponenten:
- `FeedsViewModel.FeedNotificationsEnabled` / `SaveAsync` / `EditAsync` / `ResetForm`
- `FeedRepository.MapToModel` / `MapToEntity` / `UpdateAsync` / `GetAllWithDetailsAsync` (Projektion auf `FeedListItem`)
- `SettingsViewModel.NotificationSummaryEnabled` / `LoadAsync` / `PersistAsync`
- `SettingsRepository.MapToModel` / `SaveAsync`

## Diagramm

```mermaid
flowchart TD
    A[Sync: manuell / AutoRefreshService] --> B[FeedSyncService.RunSyncAsync]
    B --> C{neue Items?}
    C -- Nein --> Z[Ende]
    C -- Ja --> D[NotificationService.NotifyNewItemsAsync]
    D --> E{feed.NotificationsEnabled?}
    E -- Nein --> Z
    E -- Ja --> F{settings.NotificationsEnabled?}
    F -- Nein --> Z
    F -- Ja --> G{Ruhezeit aktiv?}
    G -- Ja --> Z
    G -- Nein --> H[Keyword-Filter entfernt Treffer]
    H --> I{Kandidaten übrig?}
    I -- Nein --> Z
    I -- Ja --> J{NotificationSummaryEnabled?}
    J -- Nein --> K[ShowAsync pro Item<br/>Identifier = Item.Id]
    J -- Ja --> L[ShowAsync einmalig<br/>Identifier = FeedId + Item-Hash]
    K --> M[LocalNotificationService: EnsureAuthorizedAsync]
    L --> M
    M --> N{iOS autorisiert?}
    N -- Nein --> Z
    N -- Ja --> O[UNNotificationRequest anzeigen]
    O --> P[Tap: NotificationDelegate]
    P --> Q{itemId?}
    Q -- Ja --> R[articledetail?itemId=…]
    Q -- Nein --> S[//unread]
```

## Fehlerbehandlung

- `FeedSyncService.RunSyncAsync` kapselt `NotifyNewItemsAsync` in `try/catch` (nur `Debug.WriteLine`) — Benachrichtigungsfehler verfälschen weder `SyncResult` noch `FeedHealth` noch `SyncLog`.
- `SettingsViewModel.RequestNotificationAuthorizationAsync` und `RefreshNotificationPermissionAsync` fangen Exceptions und protokollieren per `Debug.WriteLine`; `NotificationPermissionDenied` fällt im Fehlerfall auf `false` zurück.
- `NotificationDelegate.DidReceiveNotificationResponse` schluckt alle Exceptions (`completionHandler` im `finally`) — ein Antippen lässt die App nie abstürzen; ohne `Shell` greift der `Launcher`-Fallback auf den Artikel-Link.
- `LocalNotificationService` auf Nicht-iOS-Targets: `IsSupported == false`, `RequestAuthorizationAsync`/`IsAuthorizedAsync` liefern `false`, `ShowAsync` ist No-Op — die Entscheidungslogik läuft, ohne dass etwas angezeigt wird.
- Cancellation: `cancellationToken.ThrowIfCancellationRequested` bzw. `WaitAsync(cancellationToken)` an allen Plattform-Aufrufen; `OperationCanceledException` wird im Sync-Pfad nicht gefangen (durchgereicht).
