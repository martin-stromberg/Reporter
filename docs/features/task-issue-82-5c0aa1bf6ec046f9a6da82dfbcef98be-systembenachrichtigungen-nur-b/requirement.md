<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: Systembenachrichtigungen nur bei Hintergrundabruf (Issue #82)

## Fachliche Zusammenfassung

Lokale Benachrichtigungen über neue Feed-Artikel dürfen künftig nur noch ausgelöst werden, wenn der Feed-Abruf im OS-Hintergrund stattfindet — Abrufe bei laufender App (manuell per Pull-to-Refresh/Einzelabruf, `AutoRefreshService`-Timer, `RefreshOnStartupEnabled`-Start-Abruf) sollen keine Systemmitteilungen mehr erzeugen. Heute ruft `FeedSyncService.RunSyncAsync` (`src/Reporter.Core/Services/FeedSyncService.cs:192`) nach jedem Sync mit neuen Items fehlerisoliert `INotificationService.NotifyNewItemsAsync` auf, und `NotificationDelegate.WillPresentNotification` (`src/Reporter/Platforms/iOS/NotificationDelegate.cs:22`) stellt die Mitteilung auch im Vordergrund als `Banner | List | Sound` dar. Da es derzeit **keinen** OS-seitigen Hintergrundabruf gibt (`docs/help/einstellungen/architektur.md:99`: „es gibt keinen OS-seitigen Background-Fetch"; kein `BGTaskScheduler`, kein `UIBackgroundModes` in `src/Reporter/Platforms/iOS/Info.plist`), umfasst die Anforderung zwei Teilmaßnahmen: (1) die Vordergrund-Darstellung unterdrücken und (2) einen echten iOS-Hintergrundabruf aufbauen — ohne (2) würden nach (1) nie wieder Benachrichtigungen erscheinen.

## Betroffene Klassen und Komponenten

### Bestehende Klassen (Anpassung)

- `src/Reporter/Platforms/iOS/NotificationDelegate.cs` — `WillPresentNotification` liefert heute `UNNotificationPresentationOptions.Banner | List | Sound`; für die Vordergrund-Unterdrückung voraussichtlich auf `UNNotificationPresentationOptions.None` zu ändern (bzw. `List`, falls Mitteilungen weiterhin still ins Mitteilungszentrum sollen — siehe Offene Fragen). `DidReceiveNotificationResponse` (Tap-Handling/Navigation) bleibt unverändert.
- `src/Reporter/Platforms/iOS/Info.plist` — neu: `UIBackgroundModes` mit `fetch` sowie `BGTaskSchedulerPermittedIdentifiers` für den `BGAppRefreshTask`-Identifier.
- `src/Reporter/Platforms/iOS/AppDelegate.cs` — Registrierung des `BGTaskScheduler`-Tasks (`BGTaskScheduler.Shared.Register`), Einplanung via `BGAppRefreshTaskRequest` (`EarliestBeginDate`), Task-Handler mit Expiration-Behandlung (`task.SetTaskCompleted`) und Neuplanung des Folgeabrufs.
- `src/Reporter.Core/Services/FeedSyncService.cs` — bestehender Benachrichtigungspfad bleibt fachlich korrekt (Benachrichtigung bei neuen Items); ggf. Erweiterung um eine Auslöser-Unterscheidung (siehe Variante unten).
- `src/Reporter/MauiProgram.cs` — DI-Registrierung für einen neuen Hintergrundabruf-Service bzw. Sicherstellung, dass `IFeedSyncService` aus dem Task-Handler heraus auflösbar ist.

### Interfaces / Services (neu, Arbeitsnamen)

- `IBackgroundRefreshService` bzw. `IBackgroundFetchService` (neu, `src/Reporter.Core/Interfaces/`) — Gateway für den OS-seitigen Hintergrundabruf nach dem etablierten Gateway-Muster (Interface in Core, Implementierung im MAUI-Projekt, vgl. `ILocalNotificationService`/`IAppThemeService`/`INetworkStatusService`); voraussichtlich mit `IsSupported`-Kennung und Methoden zum Registrieren/Planen (`ScheduleAsync`/`ApplySettingsAsync`). Implementierung unter `src/Reporter/Services/` oder `src/Reporter/Platforms/iOS/`; No-Op auf Windows/Android/MacCatalyst.
- Alternativ (Entwurfsvariante, Annahme): statt einer reinen UI-Unterdrückung in `WillPresentNotification` eine auslöserbasierte Steuerung — z. B. ein `SyncOrigin`/`SyncTrigger`-Enum (`Manual`, `Timer`, `Startup`, `BackgroundFetch`), das `IFeedSyncService.SyncFeedAsync`/`SyncAllAsync` bzw. `INotificationService.NotifyNewItemsAsync` entgegennimmt, sodass `NotificationService` nur bei `BackgroundFetch` benachrichtigt. Diese Variante ändert Interface-Signaturen und Aufrufer (`FeedsViewModel`, `UnreadViewModel`, `AutoRefreshService`, BGTask-Handler).
- Ebenfalls denkbar (Variante): ein `IAppStateService`-Gateway (Vordergrund-/Hintergrundzustand der App), den `NotificationService` oder `LocalNotificationService` vor dem `ShowAsync` auswertet — erforderlich, falls die Unterdrückung nicht über `WillPresentNotification`, sondern im Regelwerk von `NotificationService` erfolgen soll (Reporter.Core hat keine MAUI-Referenz).

### Tests (`src/Reporter.Tests/`)

- `NotificationServiceTests` / `FeedSyncServiceTests` — je nach gewählter Variante: neuer Test „kein `NotifyNewItemsAsync`-Aufruf/keine `ShowAsync`-Ausgabe bei Vordergrund-Auslöser" bzw. Aufruf nur bei Hintergrund-Trigger; bestehende Benachrichtigungstests (`SyncFeedAsync_NewItems_NotifiesWithFeedAndItems`, `SyncFeedAsync_NotificationThrows_SyncStillSucceeds` u. a.) müssen auf die neue Semantik angepasst werden.
- Ggf. neuer Fake (`FakeBackgroundRefreshService`/`FakeAppStateService`) nach dem Muster `FakeLocalNotificationService`/`FakeAutoRefreshService`.
- `ServiceCollectionTests` — um neue Registrierungen erweitern.
- Plattformcode (`AppDelegate`, `NotificationDelegate`, `BGTaskScheduler`-Scheduling, `Info.plist`) ist nicht unit-testbar — manuelle Verifikation auf einem iOS-Gerät (u. a. simuliertes Task-Auslösen via Debugger) über `scripts/iOS-Deployment.ps1`; Ergebnis in `test-results.md` dokumentieren.

## Implementierungsansatz

1. **Vordergrund-Unterdrückung (Teil 1):** In `NotificationDelegate.WillPresentNotification` die übergebenen `UNNotificationPresentationOptions` auf `None` reduzieren. iOS ruft diese Methode nur auf, wenn die App im Vordergrund läuft — Mitteilungen, die im Hintergrund-/Suspend-Zustand zugestellt werden, zeigt iOS automatisch als Banner an; das bestehende Tap-Handling (`DidReceiveNotificationResponse`) ist davon unberührt.
2. **Echter Hintergrundabruf (Teil 2, iOS):** `Info.plist` um `UIBackgroundModes`/`fetch` und `BGTaskSchedulerPermittedIdentifiers` ergänzen; in `AppDelegate.FinishedLaunching` den `BGAppRefreshTask` beim `BGTaskScheduler` registrieren und einplanen. Im Task-Handler den MAUI-`IServiceProvider` nutzen, `IFeedSyncService.SyncAllAsync` fehlerisoliert ausführen (bestehende Regeln — `NotificationsEnabled`, `Feed.NotificationsEnabled`, Ruhezeiten, Keyword-Filter, `NotificationSummaryEnabled` — greifen unverändert in `NotificationService`), danach `SetTaskCompleted` aufrufen und den nächsten Task einplanen. Zu beachten: iOS steuert die tatsächliche Ausführungshäufigkeit von `BGAppRefreshTask` systemseitig — das konfigurierte `RefreshIntervalMinutes` ist bestenfalls eine `EarliestBeginDate`-Untergrenze (Annahme: Intervall ggf. daran koppeln, siehe Offene Fragen).
3. **Auslöser-Unterscheidung (nur falls Variante gewählt):** `IFeedSyncService`-Signatur bzw. `NotifyNewItemsAsync` um einen Auslöser-Parameter erweitern und alle Aufrufer (`FeedsViewModel.SyncAsync`, `UnreadViewModel.RefreshAsync`, `AutoRefreshService.RunLoopAsync`/`RunStartupSyncAsync`, BGTask-Handler) mit dem passenden Wert versorgen.
4. **Fehlerisolierung/Diagnose:** Hintergrundabruf-Läufe nach bestehendem Muster protokollieren (`IDebugLogService`, `DebugLogCategory.Sync`/`Lifecycle`); Fehler im Task dürfen den App-Lebenszyklus nicht beeinträchtigen.
5. **Verifikation:** `dotnet test`, `.\scripts\Run-StaticChecks.ps1`, manuelle iOS-Verifikation (Vordergrund-Sync ohne Banner; Hintergrund-Task erzeugt Mitteilung) sowie Dokumentation der geänderten Timer-Lebensdauer-Aussage in `docs/help/einstellungen/architektur.md` und `docs/help/einstellungen/ablauf-technisch.md` (Abschnitt 5).

## Konfiguration

- **Bestehende Schalter bleiben maßgeblich:** `Settings.NotificationsEnabled` (Hauptschalter inkl. iOS-Berechtigungsanfrage über `ILocalNotificationService.RequestAuthorizationAsync`), `Feed.NotificationsEnabled`, `NotificationSummaryEnabled` und Ruhezeiten (`QuietHoursStart`/`QuietHoursEnd`) steuern weiterhin, ob und wie benachrichtigt wird — die Anforderung ändert nur den Auslöser-/Darstellungskontext, nicht das Regelwerk.
- **Neuer Konfigurationsbedarf nicht ableitbar:** Ob der OS-Hintergrundabruf an `AutoRefreshEnabled`/`RefreshIntervalMinutes` gekoppelt wird oder einen eigenen Schalter/eigenes Intervall erhält, ist aus der Anforderung nicht ableitbar → Offene Frage. Annahme: Wiederverwendung der bestehenden Auto-Refresh-Einstellungen, damit kein zusätzlicher `Settings`-Datensatz/keine Migration nötig wird.
- **Windows/Android:** `LocalNotificationService.IsSupported == false` und keine Hintergrundinfrastruktur — auf diesen Targets bleibt alles No-Op; eine Windows-spezifische Erwartung ist zu klären.

## Offene Fragen

- **Scope des Hintergrundabrufs:** Soll der iOS-`BGTaskScheduler`/`BGAppRefreshTask`-Abruf Teil dieser Umsetzung sein? Ohne ihn bedeutet die reine Vordergrund-Unterdrückung, dass nie wieder Benachrichtigungen erscheinen — die Anforderung wäre dann nur halb erfüllt.
- **Windows-Erwartung:** Auf Windows existiert weder eine lokale Benachrichtigungs- noch eine Hintergrundabruf-Infrastruktur (`LocalNotificationService` ist dort No-Op). Ist Windows vom Issue ausgenommen, oder gibt es eine Erwartung (z. B. Windows-Toast-Notifications via `Microsoft.Windows.AppNotifications`/`CommunityToolkit` bzw. `ToastNotificationManager`)? Analog: Ist das optionale `net10.0-android`-Target im Scope (dort wären `WorkManager`/AlarmManager nötig)?
- **Definition „während die App ausgeführt wird":** Bedeutet das strikt „App im Vordergrund sichtbar" oder jeder laufende Prozess (inkl. iOS-Hintergrundzustand ohne Suspend)? Praktisch relevant für die Abgrenzung Timer-Abruf (App läuft) vs. `BGAppRefreshTask` (OS-Hintergrund).
- **Mitteilungszentrum bei Vordergrund-Sync:** Soll die Unterdrückung `UNNotificationPresentationOptions.None` sein (Mitteilung verschwindet komplett) oder `List` (kein Banner/Sound, aber Eintrag im Mitteilungszentrum sichtbar)?
- **Intervall und Schalter des Hintergrundabrufs:** Wird der `BGAppRefreshTask` an `AutoRefreshEnabled`/`RefreshIntervalMinutes` gekoppelt (iOS behandelt `EarliestBeginDate` nur als Untergrenze — tatsächliche Häufigkeit ist nicht garantiert) oder soll ein separates Verhalten konfiguriert werden? Soll der Task auch laufen, wenn `AutoRefreshEnabled` aus ist?
- **Umsetzungsvariante:** Unterdrückung rein in `NotificationDelegate.WillPresentNotification` (iOS-idiomatisch, minimalinvasiv) oder auslöserbasiert im Regelwerk (`SyncOrigin`-Parameter bis in `NotificationService` — aufwendiger, dafür plattformneutral und unit-testbar)?
