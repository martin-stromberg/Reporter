<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Benachrichtigungen — Installation und Konfiguration

## Voraussetzungen

- iOS-Target (`net10.0-ios`): Das `UserNotifications`-Framework ist Teil des iOS-Workloads — kein NuGet-Paket erforderlich.
- `Platforms/iOS/Info.plist` benötigt für lokale Benachrichtigungen **keine** zusätzlichen Schlüssel; die Berechtigung wird zur Laufzeit angefragt (`UNUserNotificationCenter.RequestAuthorizationAsync`).
- `PrivacyInfo.xcprivacy` bleibt unverändert — `UserNotifications` ist keine Required-Reason-API.
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
| `keywords`-Tabelle | — | — | Bestehender Keyword-Filter; Treffer auf `Title`/`ContentHtml` unterdrücken die Benachrichtigung des Artikels. |

Migrationen:

| Migration | Änderung |
|-----------|----------|
| `20260911181811_AddFeedNotificationsEnabled` | `feeds.notifications_enabled` (`INTEGER`, `NOT NULL`, `defaultValue: true`) — Bestands-Feeds benachrichtigen weiter. |
| `20260911181850_AddSettingsNotificationSummary` | `settings.notification_summary_enabled` (`INTEGER`, `NOT NULL`, `defaultValue: false`) inkl. `UpdateData` des Settings-Singletons auf `false` — bestehende Installationen behalten den Einzelmodus. |

## Umgebungsvariablen

Keine — das Feature liest keine Umgebungsvariablen.

## Überprüfung

1. `MauiProgram.CreateMauiApp` registriert `INotificationService → NotificationService` und `ILocalNotificationService → LocalNotificationService` als Singletons — fehlt die Registrierung, schlägt die DI-Auflösung beim ersten Sync fehl.
2. Auf iOS: Einstellungen öffnen, **Benachrichtigungen** einschalten → iOS-Berechtigungsdialog erscheint (nur beim ersten Mal).
3. Sync mit neuen Artikeln auslösen → Benachrichtigung erscheint (auch im Vordergrund als Banner dank `NotificationDelegate.WillPresentNotification`).
4. Datenbank prüfen: `feeds.notifications_enabled` und `settings.notification_summary_enabled` existieren mit den genannten Defaults.
5. Tests: `dotnet test src/Reporter.Tests` — `NotificationServiceTests`, `FeedSyncServiceTests` u. a. decken die Entscheidungslogik ab; die iOS-Anzeige selbst ist nur manuell im Simulator verifizierbar (`scripts/iOS-Deployment.ps1`, nur macOS).
