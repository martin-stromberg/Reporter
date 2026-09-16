<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-09-15
Abbruchgrund: Maximale Iterationsanzahl erreicht (Fortsetzungslauf 2026-09-15: verbleibende Punkte erfordern physisches iOS-Gerät)

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Stand nach dem Fortsetzungslauf (2026-09-15)

Der Fortsetzungslauf hat über eine Pair-to-Mac-Verbindung (macOS 26.6.2, Xcode 26.6, iPhone-17-Simulator mit iOS 26.5) eine Remote-iOS-Verifikation durchgeführt:

- **iOS-Build erfolgreich** (`net10.0-ios`/`iossimulator-arm64`, 0 Warnungen).
- **Echter Laufzeitfehler gefunden und behoben:** synchroner `BGTaskScheduler.Register`-Aufruf in `FinishedLaunching` → `_os_unfair_lock_recursive_abort` beim Start; Fix per `DispatchQueue.MainQueue.DispatchAsync` (`AppDelegate.cs`), danach stabil.
- **Verifiziert:** Vordergrund-Unterdrückung (Timer- und Startup-Abruf; `willPresentNotification` → `None`, keine Banner/Sounds/Zustellung), `AutoRefreshEnabled`-Kopplung auf App-Ebene (aus → `cancelTaskRequestWithIdentifier`, an → `submitTaskRequest`), OS-Zustellungspfad bei suspendierter App (echter Banner), App-Stabilität.
- **Automatisiert:** 491/491 Tests, `Run-StaticChecks.ps1` grün, iOS-Sim-Build grün.

Die verbleibenden Punkte erfordern ein **physisches iOS-Gerät** (bzw. eine macOS-GUI-Sitzung für die Tap-Navigation): Der Simulator lehnt `BGTaskScheduler.Submit` mit `BGTaskSchedulerErrorDomain Code=1` (Unavailable) ab — trotz `backgroundRefreshStatus = 2` (Available) —, sodass kein Pending-Request entsteht und `_simulateLaunchForTaskWithIdentifier`/`_simulateExpirationForTaskWithIdentifier` die Simulation verweigern. Auch der private Pfad `_unsafe_submitTaskRequest:` wurde erfolglos geprüft.

## Stand nach Geräte-Rückmeldung (2026-09-16)

Der Anwender hat das Feature auf einem physischen Gerät getestet: App gestartet (Abruf lief), alle Beiträge gelesen, Benachrichtigungen + 30-Minuten-Intervall aktiv, App in den Hintergrund gebracht — nach 2 Stunden **keine** Benachrichtigung trotz neuer Artikel. Beim Wiederöffnen war die Startseite leer; nach Navigation erschienen Beiträge (Startseiten-Aktualisierung ist ein separates, später zu lösendes Problem).

Wahrscheinlichste Ursachen: iOS hat den `BGAppRefreshTask` schlicht noch nicht zugestellt (`EarliestBeginDate` ist nur eine Untergrenze — Zustellung heuristikgesteuert), `Hintergrundaktualisierung`/`Energiesparmodus` auf dem Gerät deaktiviert, oder die Mitteilung wurde unterdrückt.

**Gegenmaßnahmen (umgesetzt):**
- Vollständiges Lifecycle-Logging über die ganze Kette (`IDebugLogService`, Kategorien `Sync`/`Notification`): `Background task registered`, `Background refresh scheduled` (inkl. `earliest`), `Background refresh task started`/`completed`, `Background refresh sync started`/`finished`, `Notification posted`/`dropped`/`suppressed`. Damit zeigt ein Debugbericht exakt, an welcher Stelle die Kette abbricht.
- `BackgroundRefreshService` wertet jetzt `UIApplication.BackgroundRefreshStatus` aus — bei `Denied`/`Restricted` (iOS-Option aus/Energiesparmodus/Bildschirmzeit) wird `Submit` übersprungen und als `Warning` protokolliert.
- `ScheduledSyncRunner.RunAsync` wertet jetzt `SyncResult.Status` aus (`Error` → `SetTaskCompleted(false)`).
- `FeedSyncService.SyncAllAsync` serialisiert Gesamt-Syncs per `_syncAllLock` (behebt echte Race: Start-Abruf eines Task-getriggerten Starts parallel zum Task-Handler → doppelte Items/Benachrichtigungen).
- Troubleshooting-Doku erweitert: Debugbericht-Workflow, iOS-Checkliste (Hintergrundaktualisierung, Energiesparmodus, kein Force-Quit), Hinweis dass das Intervall eine Mindestpause ist.

**Verbleibt offen:** Verifikation der tatsächlichen `BGAppRefreshTask`-Zustellung auf dem Gerät des Anwenders — mit dem neuen Logging über einen Debugbericht belegbar.

## Offene Planelemente

- [ ] E2E-Pflichtszenario (Tasks #23, Plan-Tabelle „E2E-Tests") — **teilweise verifiziert**: Vordergrund-Syncs ohne Banner/Sound/Mitteilungszentrum (Simulator-Logs belegt für Timer- und Startup-Pfad; Pull-to-Refresh-Geste nicht separat ausgelöst), `AutoRefreshEnabled`-Kopplung auf Submit/Cancel-Ebene belegt. **Offen:** tatsächliche `BGAppRefreshTask`-Ausführung mit sichtbarer Mitteilung (Simulator: Submit = Code 1 Unavailable), Expiration-Verhalten, Tap-Navigation — physisches iOS-Gerät erforderlich.
- [x] E2E-Pflichtszenario (Task #24, Plan-Tabelle „E2E-Tests"): Kaltstart in den Vordergrund mit `RefreshOnStartupEnabled=1` → `RunStartupSyncAsync` lief (Feed-Abrufe im Log), `willPresentNotification` → response `0`, keine Zustellung — auf dem Simulator verifiziert.

## Code-Review-Befunde

Keine — der in der Remote-Verifikation gefundene Start-Absturz wurde behoben und in `review-code.md` (Iteration 4) geprüft.

## Usability-Befunde

Keine.

## Fehlgeschlagene Tests

- [ ] Manueller Sync (Pull-to-Refresh) bei Vordergrund-App erzeugt kein Banner/keinen Sound/keinen Mitteilungszentrum-Eintrag — Teilweise verifiziert (Unterdrückung auf Delegate-Ebene belegt; Geste selbst nicht ausgelöst). Rest: physisches Gerät/GUI-Sitzung erforderlich
- [ ] Timer-Abruf (`AutoRefreshService`) bei laufender App erzeugt keine Systemmitteilung — Teilweise verifiziert (Timer-Sync lief, keine Zustellung; eine vom Timer erzeugte Mitteilung ist nicht bewiesen — Unterdrückung auf Delegate-Ebene belegt). Rest: physisches Gerät/GUI-Sitzung erforderlich
- [x] Start-Abruf bei aktiviertem `RefreshOnStartupEnabled` (Kaltstart in den Vordergrund) erzeugt keine Systemmitteilung — Verifiziert auf iPhone-17-Simulator (iOS 26.5)
- [ ] Simulierter `BGAppRefreshTask` (`lldb` `_simulateLaunchForTaskWithIdentifier:@"de.martinstromberg.reporter.feedrefresh"`) führt Sync aus und erzeugt sichtbare Mitteilung — Nicht ausführbar auf dem Simulator (`BGTaskSchedulerErrorDomain Code=1`, kein Pending-Request); physisches iOS-Gerät erforderlich. OS-Zustellungspfad (Banner bei suspendierter App) separat belegt
- [ ] Task-Expiration (`_simulateExpirationForTaskWithIdentifier`) kancelliert den Sync, `SetTaskCompleted` läuft, App bleibt stabil — Nicht ausführbar auf dem Simulator (kein Pending-Request); physisches iOS-Gerät erforderlich
- [ ] `AutoRefreshEnabled` aus → kein `BGAppRefreshTask` eingeplant; Neuplanung nach Wiederaktivierung — App-Ebene verifiziert (Cancel/Submit im Log); tatsächliche OS-Einplanung Simulator-limitiert; physisches iOS-Gerät erforderlich
- [ ] Antippen einer Hintergrund-Mitteilung navigiert wie bisher (`articledetail`/`//unread`, Link-Fallback) — Nicht ausgeführt: keine UI-Eingabe über SSH/simctl ohne Accessibility-Zugriff; physisches iOS-Gerät oder macOS-GUI-Sitzung erforderlich
