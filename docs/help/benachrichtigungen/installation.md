<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Benachrichtigungen — Installation und Konfiguration

## Voraussetzungen

- iOS-Target (`net10.0-ios`): Die Frameworks `UserNotifications` und `BackgroundTasks` sind Teil des iOS-Workloads — kein NuGet-Paket erforderlich.
- `Platforms/iOS/Info.plist` benötigt für den OS-seitigen Hintergrundabruf zwei Einträge: `UIBackgroundModes` mit `fetch` sowie `BGTaskSchedulerPermittedIdentifiers` mit `de.martinstromberg.reporter.feedrefresh` — letzterer muss exakt der Konstante `BackgroundRefreshService.RefreshTaskIdentifier` entsprechen, sonst schlagen `Register`/`Submit` fehl. Für die reine lokale Benachrichtigung selbst sind keine Schlüssel nötig; die Berechtigung wird zur Laufzeit angefragt (`UNUserNotificationCenter.RequestAuthorizationAsync`).
- `PrivacyInfo.xcprivacy` bleibt unverändert — `UserNotifications`/`BackgroundTasks` sind keine Required-Reason-APIs.
- Datenbank-Migrationen werden beim App-Start über `App.OnStart` → `Database.MigrateAsync()` angewendet.

## Installationsschritte

1. App-Update ausrollen — beim nächsten Start migriert sich die SQLite-Datenbank automatisch (s. Konfiguration).
2. Keine weiteren Schritte: Die Berechtigung wird beim ersten Einschalten des Schalters **Benachrichtigungen** in den Einstellungen angefragt (nicht beim App-Start).
3. Auf anderen Plattformen als iOS ist der Dienst ein No-Op (`IsSupported == false`) — keine Konfiguration nötig, keine Fehler.

## Konfiguration

| Parameter | Typ | Standardwert | Beschreibung |
|-----------|-----|--------------|--------------|
| `settings.notifications_enabled` | `bool` | `true` | Globaler Hauptschalter; pflegbar auf der `SettingsPage` (Schalter **Benachrichtigungen**). |
| `settings.notification_summary_enabled` | `bool` | `false` | Benachrichtigungsmodus: `false` = eine Benachrichtigung pro Artikel, `true` = Sammel-Benachrichtigung pro Feed; Schalter **Sammel-Benachrichtigung** auf der `SettingsPage`. |
| `settings.quiet_hours_start` / `settings.quiet_hours_end` | `TimeSpan?` | `null` | Ruhezeit in lokaler Gerätezeit; beide Werte müssen gesetzt sein, `Start == End` gilt als leeres Intervall (keine Ruhezeit). |
| `feeds.notifications_enabled` | `bool` | `true` | Pro-Feed-Schalter; pflegbar im Feed-Formular auf `FeedsPage`. Bestehende Feeds erhalten per Migration `true`. |
| `keywords`-Tabelle | — | — | Bestehender Keyword-Filter; Treffer auf `Title`/`ContentHtml` werden regulär bereits beim Feed-Abruf verworfen (Ingest-Filter in `FeedSyncService`) — der Check in `NotificationService` bleibt als Tiefenverteidigung. |
| `settings.auto_refresh_enabled` | `bool` | `true` | Koppelt den OS-Hintergrundabruf an den Auto-Refresh-Schalter: `false` → `BGTaskScheduler.Shared.Cancel`, `true` → `Submit` mit `EarliestBeginDate` aus dem Intervall. |
| `settings.refresh_interval_minutes` | `int` | `30` | Liefert über `SettingsValues.ClampRefreshIntervalMinutes` (1–1440) die `EarliestBeginDate` des `BGAppRefreshTask`; iOS behandelt sie nur als Untergrenze — der tatsächliche Ausführungszeitpunkt liegt beim System. |

Plattform-Konfiguration (`Platforms/iOS/Info.plist`):

| Eintrag | Typ | Zweck |
|---------|-----|-------|
| `UIBackgroundModes` → `fetch` | `array`/`string` | Deklariert die App für iOS-Hintergrundabrufe (Voraussetzung für `BGAppRefreshTask`). |
| `BGTaskSchedulerPermittedIdentifiers` → `de.martinstromberg.reporter.feedrefresh` | `array`/`string` | Erlaubt die Registrierung des Tasks; muss exakt `BackgroundRefreshService.RefreshTaskIdentifier` entsprechen. |

Migrationen:

| Migration | Änderung |
|-----------|----------|
| `20260911181811_AddFeedNotificationsEnabled` | `feeds.notifications_enabled` (`INTEGER`, `NOT NULL`, `defaultValue: true`) — Bestands-Feeds benachrichtigen weiter. |
| `20260911181850_AddSettingsNotificationSummary` | `settings.notification_summary_enabled` (`INTEGER`, `NOT NULL`, `defaultValue: false`) inkl. `UpdateData` des Settings-Singletons auf `false` — bestehende Installationen behalten den Einzelmodus. |

## Umgebungsvariablen

Keine — das Feature liest keine Umgebungsvariablen.

## Überprüfung

1. `MauiProgram.CreateMauiApp` registriert `INotificationService → NotificationService`, `ILocalNotificationService → LocalNotificationService`, `IBackgroundRefreshService → BackgroundRefreshService` und `IScheduledSyncRunner → ScheduledSyncRunner` als Singletons — fehlt die Registrierung, schlägt die DI-Auflösung beim ersten Sync bzw. beim Hintergrund-Task fehl.
2. Auf iOS: Einstellungen öffnen, **Benachrichtigungen** einschalten → iOS-Berechtigungsdialog erscheint (nur beim ersten Mal).
3. Sync bei geöffneter App mit neuen Artikeln auslösen → **keine** Benachrichtigung erscheint (`NotificationDelegate.WillPresentNotification` → `UNNotificationPresentationOptions.None`).
4. Hintergrundabruf verifizieren: Task per lldb simulieren (`_simulateLaunchForTaskWithIdentifier:@"de.martinstromberg.reporter.feedrefresh"`) → Sync läuft, neue Artikel erzeugen eine sichtbare Mitteilung; `_simulateExpirationForTaskWithIdentifier` prüft die Expiration-Behandlung.
5. Datenbank prüfen: `feeds.notifications_enabled` und `settings.notification_summary_enabled` existieren mit den genannten Defaults.
6. Tests: `dotnet test src/Reporter.Tests` — `NotificationServiceTests`, `FeedSyncServiceTests`, `ScheduledSyncRunnerTests` u. a. decken die Entscheidungslogik ab; die iOS-Anzeige und der `BGTaskScheduler` sind nur manuell auf Gerät/Simulator verifizierbar (`scripts/iOS-Deployment.ps1`, nur macOS).
