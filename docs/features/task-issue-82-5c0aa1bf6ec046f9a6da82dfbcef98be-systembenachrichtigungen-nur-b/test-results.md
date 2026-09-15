<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

Alle 491 automatisierten Tests sind bestanden (0 fehlgeschlagen, 0 übersprungen), darunter die für dieses Feature neu hinzugekommenen Tests `ScheduledSyncRunnerTests` (4 Tests: `RunAsync_SyncSucceeds_ReturnsTrueAndReschedules`, `RunAsync_SyncFails_ReturnsFalseAndStillReschedules`, `RunAsync_SyncCancelled_ReturnsFalseAndStillReschedules`, `RunAsync_ReschedulingFails_ReturnsTrue`), die `IBackgroundRefreshService`-Weiterleitungs-Tests in `AutoRefreshServiceTests` (`ApplySettings_ForwardsToBackgroundRefresh`, `ApplySettings_WhenAutoRefreshDisabled_StillForwardsToBackgroundRefresh`, `ApplySettings_BackgroundRefreshThrows_TimerStillConfigured`, `ApplySettings_BackgroundRefreshUnsupported_DoesNotForward`) und `ServiceCollectionTests.AddReporterServices_ResolvesScheduledSyncRunner`. Der Solution-Build lief mit 0 Warnungen und 0 Fehlern durch; `.\scripts\Run-StaticChecks.ps1` (Format, Lizenzheader, Security-Scan, statische Analyse mit `TreatWarningsAsErrors`) ist vollständig grün.

Der Status ist dennoch „Fehler vorhanden", weil von den sieben im Plan als Pflicht markierten E2E-Szenarien auch nach der Remote-Verifikation drei vollständig offen und zwei nur teilweise belegt sind. Die Remote-Verifikation hat dabei einen echten Laufzeitfehler aufgedeckt (synchroner `BGTaskScheduler.Register`-Aufruf → `_os_unfair_lock_recursive_abort` beim Start), der behoben wurde — die verbleibenden Lücken sind Simulator-Limitationen und müssen auf einem physischen iOS-Gerät nachgeholt werden.

## Fehlgeschlagene Tests

### Automatisierte Tests

Keine — 491/491 bestanden.

### Manuelle iOS-E2E-Szenarien (Pflicht laut Plan, teilweise auf dem Simulator verifiziert — siehe „Remote-iOS-Verifikation")

- **Manueller Sync (Pull-to-Refresh) bei Vordergrund-App erzeugt kein Banner/keinen Sound/keinen Mitteilungszentrum-Eintrag** — Teilweise verifiziert: Die Unterdrückung aller Vordergrund-Mitteilungen ist auf Delegate-Ebene belegt (`willPresentNotification` → `UNNotificationPresentationOptions.None`), die konkrete Pull-to-Refresh-Geste wurde nicht separat ausgelöst
- **Timer-Abruf (`AutoRefreshService`) bei laufender App erzeugt keine Systemmitteilung** — Teilweise verifiziert (Simulator-Logs: Timer-Sync lief mit `refresh_interval_minutes=1` wiederkehrend, keine Zustellung; die Unterdrückung selbst ist auf Delegate-Ebene belegt — eine konkret vom Timer-Tick erzeugte Mitteilung ist nicht bewiesen, da keine neuen Artikel hinzukamen)
- **Start-Abruf bei aktiviertem `RefreshOnStartupEnabled` (Kaltstart in den Vordergrund) erzeugt keine Systemmitteilung** — Verifiziert (Simulator-Logs: `refresh_on_startup_enabled=1`, Kaltstart-Sync lief, `willPresentNotification` → response `0`, `shouldPresentAlert:0`, `shouldPlaySound:0`, 0 zugestellt)
- **Simulierter `BGAppRefreshTask` (`lldb` `_simulateLaunchForTaskWithIdentifier:@"de.martinstromberg.reporter.feedrefresh"`) führt Sync aus und erzeugt sichtbare Mitteilung** — Nicht ausgeführt: `BGTaskScheduler.Submit` lehnt auf dem Simulator mit `BGTaskSchedulerErrorDomain Code=1` (Unavailable) ab; ohne Pending-Request verweigert `_simulateLaunchForTaskWithIdentifier` die Simulation. Der OS-Zustellungspfad (Banner bei suspendierter App) ist separat über eine getimte Mitteilung belegt. Physisches Gerät erforderlich
- **Task-Expiration (`_simulateExpirationForTaskWithIdentifier`) kancelliert den Sync, `SetTaskCompleted` läuft, App bleibt stabil** — Nicht ausgeführt: gleiche Simulator-Limitation; physisches Gerät erforderlich
- **`AutoRefreshEnabled` aus → kein `BGAppRefreshTask` eingeplant; Neuplanung nach Wiederaktivierung** — Verifiziert auf App-Ebene (Simulator-Logs: `auto_refresh_enabled=0` → `cancelTaskRequestWithIdentifier:` ohne `submitTaskRequest`; `=1` → `submitTaskRequest:` mit `earliestBeginDate` = jetzt + Intervall). Die tatsächliche OS-Einplanung ist durch die Simulator-Limitation nicht belegt
- **Antippen einer Hintergrund-Mitteilung navigiert wie bisher (`articledetail`/`//unread`, Link-Fallback)** — Nicht ausgeführt: keine UI-Eingabe über SSH/simctl möglich (kein Accessibility-Zugriff); physisches Gerät/GUI-Sitzung erforderlich

## Remote-iOS-Verifikation (2026-09-15, Pair-to-Mac)

**Umgebung:** macOS 26.6.2 (arm64), Xcode 26.6, iPhone-17-Simulator mit iOS 26.5 (UDID `3FBA60E3-F11D-46BB-9F9E-DA85183A672F`), Build `net10.0-ios`/`iossimulator-arm64` via Pair-to-Mac (Remote-.NET unter `/Users/mstromberg/Library/Caches/maui/PairToMac/SDKs/dotnet/`).

**Durchgeführte Schritte und Ergebnisse:**

1. **iOS-Build:** erfolgreich, 0 Warnungen (inkl. Behobenem `CS8765` — Signatur-Nullability `NSDictionary? launchOptions`).
2. **Gefundener und behobener Laufzeitfehler:** Die ursprüngliche synchrone `BGTaskScheduler.Shared.Register`-Ausführung in `FinishedLaunching` führte beim Start reproduzierbar zu `EXC_BREAKPOINT`/`_os_unfair_lock_recursive_abort` (Crash-Reports bis 09:59). Fix: Registrierung per `DispatchQueue.MainQueue.DispatchAsync` auf den nächsten Main-Queue-Durchlauf verlagert (`AppDelegate.cs:33-42`). Danach startete die App mehrfach stabil und das Scheduler-Log zeigt `Handling app launch` ohne Assertion.
3. **Vordergrund-Unterdrückung belegt (Szenario 3; derselbe Delegate-Mechanismus greift für Szenarien 1+2):** Beim Kaltstart mit `refresh_on_startup_enabled=1` lief der Sync (Feed-Abrufe gegen localhost:8080), die erzeugten Mitteilungs-Requests wurden via `addNotificationRequest` eingereicht, `willPresentNotification` antwortete mit `0` (= `UNNotificationPresentationOptions.None`), die Records zeigen `shouldPresentAlert:0`/`shouldPlaySound:0`, zugestellt wurde nichts. Der 1-Minuten-Timer (`auto_refresh_enabled=1`, `refresh_interval_minutes=1`) lief ebenfalls (wiederkehrende Abrufe im Log), ohne dass eine Zustellung erfolgte.
4. **`AutoRefreshEnabled`-Kopplung belegt (Szenario 6, App-Ebene):** `auto_refresh_enabled=0` + Relaunch → `cancelTaskRequestWithIdentifier: de.martinstromberg.reporter.feedrefresh`, kein `submitTaskRequest`. `=1` + Relaunch → `submitTaskRequest:` mit `earliestBeginDate` = Startzeit + 1 min (Intervall wird übernommen).
5. **OS-Zustellungspfad belegt (Teil von Szenario 4):** Eine per lldb injizierte `UNTimeIntervalNotificationTrigger`-Mitteilung (6 s) feuerte, während die App suspendiert war → SpringBoard/BannerKit posteten einen echten Banner (`did appear as banner`, Auto-Dismiss via `SBBannerRevocationReasonBannerDestinationDismissTimer`). Damit ist belegt: Die Unterdrückung gilt nur im Vordergrund (wie gefordert), Mitteilungen bei suspendierter App werden vom OS angezeigt.
6. **Simulator-Limitation:** `BGTaskScheduler.Submit` schlägt mit `BGTaskSchedulerErrorDomain Code=1` (`Unavailable`) fehl, obwohl `UIApplication.backgroundRefreshStatus` = `2` (Available) ist — die Einstellung ist also nicht die Ursache, der Simulator unterstützt das Scheduling schlicht nicht. `getPendingTaskRequestsWithCompletionHandler:` lieferte `0` Requests; `_simulateLaunchForTaskWithIdentifier`/`_simulateExpirationForTaskWithIdentifier` verweigern ohne Pending-Request (`No task request … has been scheduled`). Auch der private Pfad `_unsafe_submitTaskRequest:` wurde erfolglos geprüft. Die Handler-Ausführung selbst ist damit auf dem Simulator nicht testbar — physisches Gerät erforderlich.
7. **Stabilität:** Vorder-/Hintergrund-Wechsel ohne Debugger → kein Crash. Die fünf Simulator-Crash-Reports verteilen sich auf: 3× Pre-Fix-`unfair_lock`-Abbruch (09:49/09:57/09:59), 1× lldb-Selektor-Tippfehler (`+[BGTaskScheduler shared]` statt `sharedScheduler` → `NSInvalidArgumentException`, 10:22), 1× lldb-Detach-SIGSEGV am suspendierten Prozess (Main-Thread im `mach_msg`-Runloop, 10:32) — letztere beiden sind Test-Artefakte ohne App-Code-Beteiligung.

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Manueller Sync (Pull-to-Refresh) bei laufender Vordergrund-App mit neuen Artikeln erzeugt kein Banner/keinen Sound/keinen Mitteilungszentrum-Eintrag | Manuell via `scripts/iOS-Deployment.ps1` | Teilweise verifiziert (iPhone-17-Simulator, iOS 26.5): Unterdrückung auf Delegate-Ebene belegt (`willPresentNotification` → `None`); die Pull-to-Refresh-Geste selbst wurde nicht ausgelöst |
| Timer-Abruf (`AutoRefreshService`) bei laufender App erzeugt keine Systemmitteilung | Manuell via `scripts/iOS-Deployment.ps1` | Teilweise verifiziert (iPhone-17-Simulator, iOS 26.5): Timer-Sync lief wiederkehrend, keine Zustellung; eine konkret vom Timer erzeugte Mitteilung ist nicht bewiesen (keine neuen Artikel) — Unterdrückung selbst auf Delegate-Ebene belegt |
| Start-Abruf bei aktiviertem `RefreshOnStartupEnabled`: Kaltstart in den Vordergrund mit neuen Artikeln → `RunStartupSyncAsync` läuft, keine Systemmitteilung | Manuell via `scripts/iOS-Deployment.ps1` | Verifiziert (iPhone-17-Simulator, iOS 26.5): Kaltstart-Sync lief, `willPresentNotification` → `0`, keine Zustellung |
| Simulierter `BGAppRefreshTask` (`lldb` `_simulateLaunchForTaskWithIdentifier`) führt Sync aus und erzeugt bei neuen Artikeln eine sichtbare Mitteilung | Manuell via `scripts/iOS-Deployment.ps1` + lldb | Nicht ausführbar auf dem Simulator (`BGTaskSchedulerErrorDomain Code=1`, kein Pending-Request); OS-Zustellungspfad (Banner bei suspendierter App) separat belegt; physisches Gerät erforderlich |
| Task-Expiration (`_simulateExpirationForTaskWithIdentifier`) → Sync kancelliert, `SetTaskCompleted` läuft, App bleibt stabil | Manuell via `scripts/iOS-Deployment.ps1` + lldb | Nicht ausführbar auf dem Simulator (kein Pending-Request); physisches Gerät erforderlich |
| `AutoRefreshEnabled` aus → kein `BGAppRefreshTask` eingeplant; Neuplanung nach Wiederaktivierung | Manuell via `scripts/iOS-Deployment.ps1` | Verifiziert auf App-Ebene (iPhone-17-Simulator): aus → `cancelTaskRequestWithIdentifier`, an → `submitTaskRequest` mit korrektem `earliestBeginDate`; OS-Einplanung selbst Simulator-limitiert |
| Antippen einer Hintergrund-Mitteilung navigiert wie bisher (`articledetail`/`//unread`, Link-Fallback) | Manuell via `scripts/iOS-Deployment.ps1` | Nicht ausgeführt (keine UI-Eingabe über SSH/simctl ohne Accessibility-Zugriff; physisches Gerät/GUI-Sitzung erforderlich) |

## Zusammenfassung

- Gesamt: 491
- Bestanden: 491
- Fehlgeschlagen: 0
- Übersprungen: 0
- Manuelle E2E-Pflichtszenarien: 1 verifiziert (Start-Abruf), 3 teilweise verifiziert (Pull-to-Refresh, Timer, `AutoRefreshEnabled`-Kopplung), 3 nicht ausgeführt (simulierter `BGAppRefreshTask`, Expiration, Tap-Navigation — Simulator-Limitation bzw. fehlende UI-Eingabe, siehe oben)

Ausgeführte Befehle (identisch zum CI-Job `build-and-test`, Env `IncludeIosTarget=false`, `IncludeAndroidTarget=false`):

1. `dotnet build Reporter.sln --configuration Release` → 0 Warnungen, 0 Fehler
2. `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release --no-build --settings src/Reporter.Tests/coverlet.runsettings --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"` → 491/491 bestanden in 3,5 s; Coverage: `src/Reporter.Tests/TestResults/32d23a01-3ad6-43c5-be2f-dd14331c3283/coverage.cobertura.xml`
3. `dotnet test src/Reporter.Tests/Reporter.Tests.csproj` (Nachlauf nach dem AppDelegate-Fix, Debug) → 491/491 bestanden
4. `.\scripts\Run-StaticChecks.ps1` → alle Checks bestanden (Format, Lizenzheader, Security-Scan ohne Befunde, statische Analyse mit 0 Warnungen/0 Fehlern), Exit-Code 0

Remote-iOS-Build/Verifikation (Pair-to-Mac, iPhone-17-Simulator, iOS 26.5): Build `net10.0-ios`/`iossimulator-arm64` erfolgreich (0 Warnungen); App-Lauf stabil nach dem Deferred-Registration-Fix; Details siehe Abschnitt „Remote-iOS-Verifikation".

## Testabdeckung

**Abdeckung:** 91,9 % (Zeilen, gesamt laut `coverage.cobertura.xml`; Reporter.Core 89,5 %, Reporter.Data 99,0 %; Branch-Abdeckung 87,5 %; EF-Core-Migrationsdateien über `coverlet.runsettings` ausgeschlossen)

| Datei | Abdeckung |
|-------|-----------|
| `src/Reporter.Core/Resources/Strings/AppResources.Designer.cs` | 26,2 % (generierte Datei) |
| `src/Reporter.Core/Models/CategoryFilterItem.cs` | 28,6 % |
| `src/Reporter.Core/Services/FeedSearchUnavailableException.cs` | 33,3 % |

Alle weiteren Quelldateien liegen bei mindestens 80 % Zeilenabdeckung (nach Aggregation partialer Klassen/Statemachines pro Datei). `AutoRefreshService.cs` einschließlich der `IBackgroundRefreshService`-Weiterleitung (93 %) und `ScheduledSyncRunner.cs` (100 %) sind durch die neuen Tests abgedeckt. `BackgroundRefreshService.cs` liegt im MAUI-Projekt (`src/Reporter/`), das von `Reporter.Tests` nicht referenziert wird, und ist `#if IOS`-dominiert — nicht coverage-messbar (plattformbedingt, siehe Plan).

## Fehlende Tests

Quelle: `Coverage-Daten`

Keine Quelldatei mit 0 % Zeilenabdeckung gefunden. Die generierte Datei `AppResources.Designer.cs` (26,2 %) wird gemäß Konvention nicht als fehlende Testabdeckung gewertet; `CategoryFilterItem.cs` (28,6 %) und `FeedSearchUnavailableException.cs` (33,3 %) sind teilweise abgedeckt.

Die verbleibende Nachweislücke betrifft drei der sieben manuellen iOS-E2E-Pflichtszenarien vollständig (simulierter `BGAppRefreshTask`, Task-Expiration, Tap-Navigation) und zwei teilweise (Pull-to-Refresh-Geste nicht separat ausgelöst; `AutoRefreshEnabled`-OS-Einplanung durch die Simulator-Limitation nicht belegt). Diese Nachweise erfordern ein physisches iOS-Gerät bzw. eine macOS-GUI-Sitzung und sind nachzuholen, bevor die Anforderung als vollständig verifiziert gilt.
