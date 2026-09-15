<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Benachrichtigungen — Architektur

## Beteiligte Komponenten

| Komponente | Typ | Rolle |
|------------|-----|-------|
| `INotificationService` (`src/Reporter.Core/Interfaces/INotificationService.cs`) | Interface | Entscheidungsservice: `NotifyNewItemsAsync(Feed, IReadOnlyList<Item>, CancellationToken)` |
| `NotificationService` (`src/Reporter.Core/Services/NotificationService.cs`) | Klasse | Regelauswertung (Feed-Schalter, globaler Schalter, Ruhezeit via `TimeProvider`, Keyword-Filter) und Modus-Verzweigung Einzel-/Sammel-Versand inkl. Identifier-Bildung |
| `ILocalNotificationService` (`src/Reporter.Core/Interfaces/ILocalNotificationService.cs`) | Interface | Plattformabstraktion: `IsSupported`, `RequestAuthorizationAsync`, `GetAuthorizationStatusAsync` → `NotificationAuthorizationStatus` (`Unsupported`/`NotDetermined`/`Denied`/`Authorized`), `ShowAsync(title, body, identifier, userInfo?, ct)` |
| `NotificationAuthorizationStatus` (`src/Reporter.Core/Models/NotificationAuthorizationStatus.cs`) | Enum | Berechtigungsstatus für die Einstellungs-UI — unterscheidet `NotDetermined` (nie angefragt) von `Denied` (verweigert) |
| `LocalNotificationService` (`src/Reporter/Services/LocalNotificationService.cs`) | Klasse | iOS-Implementierung über `UserNotifications` (`#if IOS`); No-Op auf anderen Targets |
| `IBackgroundRefreshService` (`src/Reporter.Core/Interfaces/IBackgroundRefreshService.cs`) | Interface | Gateway für den OS-seitigen Hintergrundabruf: `IsSupported`, `ApplySettingsAsync(Settings, CancellationToken)` (Task einplanen/abmelden) |
| `BackgroundRefreshService` (`src/Reporter/Services/BackgroundRefreshService.cs`) | Klasse | iOS-Implementierung über `BackgroundTasks` (`#if IOS`): plant `BGAppRefreshTask` via `BGTaskScheduler` (`RefreshTaskIdentifier` `de.martinstromberg.reporter.feedrefresh`); No-Op auf anderen Targets |
| `IScheduledSyncRunner` / `ScheduledSyncRunner` (`src/Reporter.Core`) | Interface + Klasse | Plattformneutrale Orchestrierung eines ausgelösten Hintergrund-Tasks: führt `SyncAllAsync` fehlerisoliert aus (`RunAsync` → `Task<bool>` für `SetTaskCompleted`) und plant den Folgeabruf aus den persistierten Settings über `IBackgroundRefreshService.ApplySettingsAsync` neu — auch im Fehler- und Kancellierungsfall |
| `NotificationDelegate` (`src/Reporter/Platforms/iOS/NotificationDelegate.cs`) | Klasse | `UNUserNotificationCenterDelegate`: unterdrückt die Vordergrund-Darstellung (`UNNotificationPresentationOptions.None` — Mitteilungen erscheinen nur aus dem OS-Hintergrundabruf) und Tap-Handling mit In-App-Navigation |
| `AppDelegate` (`src/Reporter/Platforms/iOS/AppDelegate.cs`) | Klasse | Registriert den `NotificationDelegate` und den `BGAppRefreshTask` beim `BGTaskScheduler` in `FinishedLaunching` (`RegisterBackgroundFetchTask`); `HandleRefreshTaskAsync` setzt den `ExpirationHandler`, löst `IScheduledSyncRunner` via `IPlatformApplication.Current.Services` auf und ruft `SetTaskCompleted` |
| `FeedSyncService` (`Reporter.Core`) | Service | Aufrufer: sammelt neue `Item`s in `RunSyncAsync` und ruft `INotificationService` fehlerisoliert auf |
| `SettingsViewModel` (`Reporter.Core`) | ViewModel | Hauptschalter, Modus-Schalter, Berechtigungsanfrage beim Einschalten bzw. über `RequestNotificationPermissionCommand`, Status-Flags `NotificationPermissionDenied`/`NotificationPermissionNotDetermined` für die Hinweiszeilen, `NotificationsSupported`/`NotificationControlsEnabled` für die Plattform-Deaktivierung |
| `FeedsViewModel` (`Reporter.Core`) | ViewModel | Pro-Feed-Schalter `FeedNotificationsEnabled` im Feed-Formular, `NotificationsSupported` deaktiviert den Schalter auf Nicht-iOS-Plattformen |
| `AppResources` (`Reporter.Core/Resources/Strings`) | Ressourcen | Lokalisierte Labels, Hinweise und `NotificationSummaryFormat` (EN/DE) |

## Abhängigkeiten

- `NotificationService` hängt von `ISettingsRepository`, `IKeywordFilter`, `ILocalNotificationService` und `TimeProvider` ab (Standard `TimeProvider.System`; Tests injizieren `FakeTimeProvider`).
- `FeedSyncService` hat `INotificationService` als fünfte Konstruktor-Abhängigkeit.
- `SettingsViewModel` und `FeedsViewModel` haben `ILocalNotificationService` als **optionale** Abhängigkeit (nullable Parameter) — ältere Tests und Nicht-iOS-Szenarien funktionieren ohne.
- `BackgroundRefreshService` hängt nur von `IDebugLogService?` ab (optional); `ScheduledSyncRunner` hängt von `IFeedSyncService`, `ISettingsRepository`, `IBackgroundRefreshService` und optional `IDebugLogService` ab; `AutoRefreshService` hält `IBackgroundRefreshService` als Pflicht-Abhängigkeit und leitet `ApplySettingsAsync` weiter — nur bei `IsSupported == true` (Kopplung des OS-Tasks an `AutoRefreshEnabled`/`RefreshIntervalMinutes`).
- Registrierung in `MauiProgram.CreateMauiApp` als Singletons: `INotificationService → NotificationService`, `ILocalNotificationService → LocalNotificationService`, `IBackgroundRefreshService → BackgroundRefreshService`, `IScheduledSyncRunner → ScheduledSyncRunner`.
- Externe Systeme: das lokale iOS-`UserNotifications`-Framework (lokale, nicht entfernte Benachrichtigungen — kein Push-Server, kein Netzwerkzugriff) sowie das iOS-`BackgroundTasks`-Framework (`BGTaskScheduler`/`BGAppRefreshTask`). Kein NuGet-Paket erforderlich.
- `Platforms/iOS/Info.plist` deklariert `UIBackgroundModes` mit `fetch` und `BGTaskSchedulerPermittedIdentifiers` mit `de.martinstromberg.reporter.feedrefresh` (muss exakt `BackgroundRefreshService.RefreshTaskIdentifier` entsprechen); `PrivacyInfo.xcprivacy` bleibt unverändert (`UserNotifications`/`BackgroundTasks` sind keine Required-Reason-APIs).

## Datenfluss

```mermaid
graph TD
    FS[FeedSyncService.RunSyncAsync] --> NS[NotificationService]
    NS --> SR[ISettingsRepository]
    NS --> KF[IKeywordFilter]
    KF --> KR[IKeywordRepository]
    KF --> KM[IKeywordMatcher]
    NS --> TP[TimeProvider]
    NS --> LNS[ILocalNotificationService]
    SVM[SettingsViewModel] --> LNS
    LNS --> LNSI[LocalNotificationService]
    LNSI --> UNC[UNUserNotificationCenter iOS]
    UNC --> ND[NotificationDelegate]
    ND --> SH[Shell.Current.GoToAsync<br/>articledetail / unread]
    ND --> LAU[Launcher.OpenAsync<br/>Link-Fallback]
    ARS[AutoRefreshService] --> BRS[IBackgroundRefreshService]
    BRS --> BRSI[BackgroundRefreshService]
    BRSI --> BGT[BGTaskScheduler iOS]
    AD[AppDelegate] --> BGT
    BGT --> AD
    AD --> SSR[IScheduledSyncRunner]
    SSR --> SSRI[ScheduledSyncRunner]
    SSRI --> BRS
    SSRI --> FS
    SSRI --> SR
```

Daten entstehen beim Feed-Abruf (`Item`s) — Keyword-Treffer verwirft `FeedSyncService` bereits beim Einspeichern —, werden in `NotificationService` gefiltert (Tiefenverteidigung) und — je nach Modus — als einzelne oder als Sammel-Benachrichtigung an iOS übergeben; `UserInfo` transportiert `itemId`/`link` bzw. `feedId` für das Tap-Handling zurück in die App. Sichtbar wird eine Mitteilung nur, wenn der Sync im OS-Hintergrund lief: `NotificationDelegate.WillPresentNotification` unterdrückt die Vordergrund-Darstellung vollständig. Der `BGAppRefreshTask` wird über `AutoRefreshService.ApplySettingsAsync` → `IBackgroundRefreshService.ApplySettingsAsync` (nur bei `IsSupported == true`) an `AutoRefreshEnabled`/`RefreshIntervalMinutes` gekoppelt; im Task führt `ScheduledSyncRunner.RunAsync` den regulären Sync-Pfad aus und plant den Folgeabruf.

## Skalierung und Zuverlässigkeit

- **Fehlerisolierung:** Der Benachrichtigungsaufruf läuft in einem eigenen `try/catch` innerhalb `RunSyncAsync` — ein iOS-Fehler beeinflusst weder Sync-Ergebnis noch Feed-Health.
- **Dedup über Identifier:** iOS ersetzt Requests mit identischem Identifier. Einzelmodus: `Item.Id` (stabil, DB-seitig ohnehin unique pro Sync). Sammelmodus: `{feedId}-{SHA256(sortierte ItemIds)}` — gleicher Artikelbestand ersetzt die bestehende Sammel-Benachrichtigung, neuer Bestand erzeugt eine neue.
- **Plattform-Trennung:** Sämtliche Entscheidungslogik liegt in `Reporter.Core` (unit-testbar); der iOS-spezifische Code ist auf `LocalNotificationService` (`#if IOS`) und `NotificationDelegate`/`AppDelegate` unter `Platforms/iOS` beschränkt.
- **Schwache Delegate-Referenz:** `UNUserNotificationCenter.Delegate` ist weak — `AppDelegate` hält die `NotificationDelegate`-Instanz in einem Feld.
