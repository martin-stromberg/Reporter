<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-09-15
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

Hinweis: Alle offenen Punkte sind Umgebungslimitationen — sie erfordern ein
macOS-System mit iOS-Gerät/Simulator (`scripts/iOS-Deployment.ps1`, lldb) und
sind unter Windows nicht ausführbar. Der gesamte Code, alle automatisierten
Tests (491/491) und die statischen Checks sind grün.

## Offene Planelemente

- [ ] E2E-Pflichtszenario (Tasks #23, Plan-Tabelle „E2E-Tests"): Manuelle iOS-Verifikation via `scripts/iOS-Deployment.ps1` — Vordergrund-Syncs (manuell + Timer) ohne Banner/Sound/Mitteilungszentrum; simulierter `BGAppRefreshTask` (`_simulateLaunchForTaskWithIdentifier`) erzeugt Mitteilung; Expiration (`_simulateExpirationForTaskWithIdentifier`) stabil; `AutoRefreshEnabled`-Kopplung; Tap-Navigation unverändert — nicht ausführbar unter Windows (macOS/iOS-Gerät erforderlich); als ausstehend in `test-results.md` dokumentiert.
- [ ] E2E-Pflichtszenario (Task #24, Plan-Tabelle „E2E-Tests"): Kaltstart in den Vordergrund mit aktiviertem `RefreshOnStartupEnabled` und neuen Artikeln → `RunStartupSyncAsync` läuft, aber keine Systemmitteilung — nicht ausführbar unter Windows; als ausstehend in `test-results.md` dokumentiert.

## Code-Review-Befunde

Keine.

## Usability-Befunde

Keine.

## Fehlgeschlagene Tests

- [ ] Manueller Sync (Pull-to-Refresh) bei Vordergrund-App erzeugt kein Banner/keinen Sound/keinen Mitteilungszentrum-Eintrag — Nicht ausgeführt: macOS/iOS-Gerät erforderlich (`scripts/iOS-Deployment.ps1` unter Windows nicht ausführbar)
- [ ] Timer-Abruf (`AutoRefreshService`) bei laufender App erzeugt keine Systemmitteilung — Nicht ausgeführt: macOS/iOS-Gerät erforderlich
- [ ] Start-Abruf bei aktiviertem `RefreshOnStartupEnabled` (Kaltstart in den Vordergrund) erzeugt keine Systemmitteilung — Nicht ausgeführt: macOS/iOS-Gerät erforderlich
- [ ] Simulierter `BGAppRefreshTask` (`lldb` `_simulateLaunchForTaskWithIdentifier:@"de.martinstromberg.reporter.feedrefresh"`) führt Sync aus und erzeugt sichtbare Mitteilung — Nicht ausgeführt: macOS/iOS-Gerät erforderlich
- [ ] Task-Expiration (`_simulateExpirationForTaskWithIdentifier`) kancelliert den Sync, `SetTaskCompleted` läuft, App bleibt stabil — Nicht ausgeführt: macOS/iOS-Gerät erforderlich
- [ ] `AutoRefreshEnabled` aus → kein `BGAppRefreshTask` eingeplant; Neuplanung nach Wiederaktivierung — Nicht ausgeführt: macOS/iOS-Gerät erforderlich
- [ ] Antippen einer Hintergrund-Mitteilung navigiert wie bisher (`articledetail`/`//unread`, Link-Fallback) — Nicht ausgeführt: macOS/iOS-Gerät erforderlich
