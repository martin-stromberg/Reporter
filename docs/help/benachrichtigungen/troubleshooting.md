<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Benachrichtigungen — Fehlerbehebung

## Benachrichtigungen kommen nicht an

**Symptom:** Neue Artikel werden synchronisiert, aber es erscheint keine Benachrichtigung.

**Ursache:** Die Entscheidungskette in `NotificationService.NotifyNewItemsAsync` hat vier Abbruchbedingungen; zusätzlich kann die iOS-Berechtigung fehlen oder der Sync gar keine neuen Artikel geliefert haben. Zwei neue, nicht-offensichtliche Fälle kommen hinzu: Lief der Sync bei **geöffneter App**, ist die vollständige Unterdrückung durch `NotificationDelegate.WillPresentNotification` → `UNNotificationPresentationOptions.None` das gewollte Verhalten (kein Banner, kein Sound, kein Mitteilungszentrum-Eintrag); und lief der Sync **gar nicht** — etwa weil der `BGAppRefreshTask` nicht eingeplant war —, gibt es schlicht keine neuen Artikel.

**Lösung:**
1. Datenbank prüfen: `settings.notifications_enabled = 1` und `feeds.notifications_enabled = 1` für den betroffenen Feed.
2. Ruhezeit prüfen: `settings.quiet_hours_start`/`quiet_hours_end` — liegt die lokale Uhrzeit im Intervall (Wrap-around beachten: `Start > End` bedeutet über Mitternacht), wird verworfen.
3. Keyword-Filter prüfen: Enthält `Title` oder `ContentHtml` des Artikels ein Schlagwort aus `keywords` (Teilwort, `OrdinalIgnoreCase`), wird er nicht benachrichtigt — regulär wird ein solcher Treffer bereits beim Einspeichern in `FeedSyncService` verworfen und gar nicht erst gespeichert (Ausweisung als `, N filtered` in der `SyncLog.Message`).
4. iOS-Berechtigungsstatus prüfen: `UNAuthorizationStatus` via `GetNotificationSettingsAsync` — `Denied` unterdrückt den Versand still (`EnsureAuthorizedAsync` bricht ab). In der App sichtbar über `SettingsViewModel.NotificationPermissionDenied` (Hinweiszeile „Einstellungen öffnen"); `NotDetermined` zeigt stattdessen die neutrale Zeile „Benachrichtigungen erlauben" (`NotificationPermissionNotDetermined`).
5. Abgleich-Kontext prüfen: Sichtbar wird eine Mitteilung nur, wenn der Sync aus dem `BGAppRefreshTask` kam — Vordergrund-Syncs (manuell, Timer, Start-Abruf) werden in `WillPresentNotification` mit `None` unterdrückt. Zum Testen den Task per lldb simulieren: `_simulateLaunchForTaskWithIdentifier:@"de.martinstromberg.reporter.feedrefresh"`.
6. Debug-Ausgabe prüfen: `FeedSyncService notification failed: …` (Fehler im Benachrichtigungspfad), `Failed to request notification authorization: …`, `Failed to query notification authorization: …`; im Session-Debug-Log (`debug_log_entries`, Kategorie `Sync`) zusätzlich `Background refresh sync failed`, `Background refresh rescheduling failed`, `Background refresh scheduling failed`, `Background task registration failed`, `Background refresh task failed` und `Scheduled sync runner not resolved`.
7. Plattform prüfen: Auf Nicht-iOS-Targets ist `LocalNotificationService.IsSupported == false` und `ShowAsync` ein No-Op — die Benachrichtigungs-Schalter in Einstellungen und Feed-Formular sind dort deaktiviert (`NotificationsSupported == false`), mit Hinweis „derzeit nur auf iOS verfügbar".

> **Hinweis:** Benachrichtigungen entstehen nur, wenn der Sync tatsächlich neue `Item`s einfügt (`newItemEntities.Count > 0`) — ein zweiter Sync ohne neue Artikel sendet nichts (Dedup auf DB-Ebene).

## Hintergrundabruf (`BGAppRefreshTask`) läuft nicht

**Symptom:** Bei geschlossener App werden Feeds nie abgerufen — es kommen weder neue Artikel noch Benachrichtigungen.

**Ursache:** Mehrere Voraussetzungen müssen zusammenkommen: `settings.auto_refresh_enabled = 1` (der Task ist an den Auto-Refresh-Schalter gekoppelt), die `Info.plist`-Einträge `UIBackgroundModes`/`fetch` und `BGTaskSchedulerPermittedIdentifiers` müssen vorhanden sein und exakt `BackgroundRefreshService.RefreshTaskIdentifier` (`de.martinstromberg.reporter.feedrefresh`) entsprechen, und die iOS-Systemoption **Hintergrundaktualisierung** muss für die App freigegeben sein (`UIApplication.BackgroundRefreshStatus` wird nicht ausgewertet — ein dort deaktivierter Abruf bleibt in der App unsichtbar). Zusätzlich legt iOS den tatsächlichen Ausführungszeitpunkt systemseitig fest — `EarliestBeginDate` ist nur eine Untergrenze.

**Lösung:**
1. `settings.auto_refresh_enabled` und `refresh_interval_minutes` in der Datenbank prüfen (letzteres wird via `SettingsValues.ClampRefreshIntervalMinutes` auf 1–1440 geclamppt).
2. Identifier-Konsistenz prüfen: `BGTaskSchedulerPermittedIdentifiers` in `Info.plist` muss der Konstante `RefreshTaskIdentifier` entsprechen — bei Abweichung schlägt `BGTaskScheduler.Shared.Register`/`Submit` fehl.
3. Debug-Ausgabe/Session-Log auf `AppDelegate background task registration failed` bzw. `Background task registration failed` (Kategorie `Sync`, `Warning`) und `BackgroundRefreshService submit failed` / `Background refresh scheduling failed` prüfen.
4. Task-Ausführung simulieren (lldb an den App-Prozess): `e -l objc -- (void)[[BGTaskScheduler sharedScheduler] _simulateLaunchForTaskWithIdentifier:@"de.martinstromberg.reporter.feedrefresh"]` — der Handler `AppDelegate.HandleRefreshTaskAsync` muss `IScheduledSyncRunner.RunAsync` ausführen und `SetTaskCompleted` aufrufen; `_simulateExpirationForTaskWithIdentifier` prüft die Kancellation über den `ExpirationHandler`.
5. Systemoption prüfen: **Einstellungen → Allgemein → Hintergrundaktualisierung** (global und für die App) muss erlaubt sein.
6. Beachten: Auch bei korrekter Einplanung kann iOS den Task über Stunden nicht ausführen (Nutzungsverhalten, Energiestatus, Low-Power-Modus) — das ist systembedingt und kein Fehler der App. `ScheduledSyncRunner.RunAsync` plant den Folgeabruf nach jedem Lauf neu — auch im Fehler- und Kancellierungsfall —, sodass sich der Abruf nicht „totläuft".

## iOS-Berechtigungsdialog erscheint nicht

**Symptom:** Beim Einschalten von **Benachrichtigungen** in den Einstellungen kommt kein System-Dialog.

**Ursache:** iOS zeigt `RequestAuthorizationAsync` nur einmal pro Installation (`NotDetermined`). War die Berechtigung bereits erteilt oder verweigert, erscheint kein Dialog. Zusätzlich unterdrückt der `_isLoading`-Guard in `SettingsViewModel` die Anfrage beim Befüllen durch `LoadAsync`.

**Lösung:**
1. System-Einstellungen der App prüfen — ist der Status bereits `Authorized`/`Provisional`/`Ephemeral`, ist kein Dialog nötig.
2. Bei `Denied`: In den iOS-Einstellungen der App Benachrichtigungen freischalten (die Hinweiszeile in den App-Einstellungen führt per **Einstellungen öffnen** direkt dorthin).
3. Für einen erneuten Dialog die App neu installieren oder den Status via `xcrun simctl privacy … reset` zurücksetzen (nur Simulator).

## Hinweiszeile „Einstellungen öffnen" bleibt trotz erteilter Berechtigung sichtbar

**Symptom:** Die rote Hinweiszeile in der Karte **Benachrichtigungen & Ruhezeiten** verschwindet nicht, obwohl iOS die Berechtigung erteilt hat.

**Ursache:** `NotificationPermissionDenied` wird nur beim Laden der Einstellungsseite (`LoadAsync` → `RefreshNotificationPermissionAsync` via `GetAuthorizationStatusAsync`) und beim Umschalten des Hauptschalters bzw. über die „Benachrichtigungen erlauben"-Zeile aktualisiert. Eine Änderung in den iOS-Systemeinstellungen, während die Seite geöffnet ist, wird nicht live erkannt.

**Lösung:**
1. Einstellungen-Seite verlassen und erneut öffnen — `OnAppearing` lädt neu und aktualisiert den Status.
2. Prüfen, ob `GetAuthorizationStatusAsync` eine Exception wirft (Debug-Ausgabe `Failed to query notification authorization`) — dann bleiben beide Status-Flags `false`.

## Sammel-Benachrichtigung dupliziert sich im Mitteilungszentrum

**Symptom:** Für denselben Feed liegen mehrere Sammel-Benachrichtigungen im Mitteilungszentrum.

**Ursache:** Der Sammel-Identifier `{feedId}-{SHA256(sortierte ItemIds)}` ändert sich mit jedem veränderten Artikelbestand — das ist beabsichtigt: neue Artikel erzeugen eine neue Benachrichtigung statt die alte still zu ersetzen. Wirkliche Dubletten (identischer Bestand, zwei Benachrichtigungen) deuten auf unterschiedliche Identifier hin.

**Lösung:**
1. Prüfen, ob sich der Artikelbestand zwischen den Syncs tatsächlich geändert hat (dann korrekt).
2. Sicherstellen, dass die Identifier-Bildung deterministisch bleibt: `BuildSummaryIdentifier` sortiert die `Item.Id`s ordinal vor dem Hash.

## Antippen einer Benachrichtigung öffnet den Browser statt der App

**Symptom:** Nach dem Antippen startet der Browser mit dem Artikel-Link statt der Artikeldetailansicht.

**Ursache:** Der `Launcher`-Fallback greift, wenn `Shell.Current` zum Zeitpunkt von `DidReceiveNotificationResponse` noch nicht bereit ist (Kaltstart durch die Benachrichtigung) oder die Navigation fehlschlug — Voraussetzung ist außerdem ein gesetzter `link` im `UserInfo` (nur Einzel-Benachrichtigungen).

**Lösung:**
1. Verhalten tritt nur bei Kaltstart über die Benachrichtigung auf; ist die App bereits offen, navigiert `Shell.Current.GoToAsync` direkt.
2. Prüfen, ob die Routen existieren: `Routing.RegisterRoute("articledetail")` und `ShellContent.Route = "unread"` in `AppShell`.
3. Bei Sammel-Benachrichtigungen existiert kein `link`-Fallback — dort wird auf `//unread` navigiert oder gar nichts geöffnet (wenn die Shell fehlt).
