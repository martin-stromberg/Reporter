<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Keine Befunde

Iteration 4 — ausgelöst durch die Remote-iOS-Verifikation (Pair-to-Mac, iPhone-17-Simulator, iOS 26.5), die einen echten Laufzeitfehler im Feature-Code aufgedeckt hat. Der Befund wurde behoben und erneut auf dem Simulator verifiziert.

## Iteration-4-Befund (behoben)

### AppDelegate.cs — synchroner `BGTaskScheduler.Register`-Aufruf ließ die App beim Start abstürzen

**Befund:** `RegisterBackgroundFetchTask()` wurde synchron aus `FinishedLaunching` aufgerufen (nach `base.`). Auf iOS 26.5 (Simulator) führte das reproduzierbar zum Abbruch beim App-Start: `EXC_BREAKPOINT`/`SIGKILL`, nativer Stack `_os_unfair_lock_recursive_abort` → `-[BGTaskScheduler _handleAppLaunch]` → `+[BGTaskScheduler _applicationDidFinishLaunching:]_block_invoke` (Crash-Reports `Reporter-2026-09-15-0949xx` bis `-095943.ips`). Der synchrone Aufruf kollidiert mit dem internen Launch-Handling des Schedulers (rekursiver unfair-lock).

**Fix:** `FinishedLaunching` (`AppDelegate.cs:33-42`) ruft `base.FinishedLaunching` auf und verlagert die Registrierung per `DispatchQueue.MainQueue.DispatchAsync(RegisterBackgroundFetchTask)` auf den nächsten Main-Queue-Durchlauf; zusätzlich wurde die Signatur-Nullability korrigiert (`NSDictionary launchOptions` → `NSDictionary? launchOptions`, behebt CS8765) und `using CoreFoundation;` ergänzt.

**Bewertung:** Der Fix ist korrekt und verträglich mit dem Apple-Vertrag:

- Apple verlangt die Registrierung „bevor die App das Launching abschließt", damit ein Handler vorliegt, wenn ein Task beim Start zugestellt wird. Für `BGAppRefreshTask` ist das unkritisch: Der Task feuert frühestens nach `earliestBeginDate` (hier: konfiguriertes Intervall ≥ 1 min), zu dem Zeitpunkt ist die Registrierung längst erfolgt — die Registrierung ist die Voraussetzung für das *Feuern*, nicht umgekehrt. Die Verifikation bestätigt das: Im Simulator-Log erscheint nach jedem Start `Handling app launch` ohne Assertion und anschließend `submitTaskRequest:`/`cancelTaskRequestWithIdentifier:` — die Registrierung ist also wirksam, bevor `Submit`/`Cancel` aufgerufen werden (die iOS-seitige `_handleSubmissionWithoutRegistrationForTaskRequest`-Assertion bleibt aus).
- Die Fehlerpfade (`registered == false`, nicht auflösbarer `IScheduledSyncRunner`) bleiben wie in Iteration 3 geprüft unverändert.
- `DispatchQueue`/`CoreFoundation`-Using werden benötigt; Format-/Analyse-Checks grün.

**Verifikation auf dem Simulator (nach dem Fix):** App startet mehrfach stabil (PIDs 43783/44711/45544/45586/45845), registriert den Task, submitted/cancelt je nach `AutoRefreshEnabled`; Vordergrund-Mitteilungen werden weiterhin mit `UNNotificationPresentationOptions.None` unterdrückt (`willPresentNotification` → response `0`, `shouldPresentAlert:0`, `shouldPlaySound:0`). Der einzige spätere SIGSEGV (10:32) entstand durch lldb-Detach am suspendierten Prozess (Main-Thread im `mach_msg`-Runloop, keine App-Frames) und ein SIGABRT (10:22) durch einen fehlerhaften lldb-Selektor (`+[BGTaskScheduler shared]` statt `sharedScheduler`) — beides Test-Artefakte, kein App-Code beteiligt.

## Verifizierung der Iteration-3/2/1-Befunde (unverändert behoben)

- `Register`-Rückgabewert-Auswertung + `IDebugLogService`-Protokollierung (Iteration 2) — unverändert in `AppDelegate.cs:54-60`.
- Null-Runner-Zweig protokolliert (Iteration 2) — unverändert `AppDelegate.cs:78-82`.
- `IsSupported`-Auswertung, zentrale Clamp-Grenzen, `ScheduledSyncRunner`-Orchestrierung mit `CancellationToken.None`, `HandleRefreshTaskAsync`-catch-Protokollierung (Iteration 1) — unverändert; Tests bestehen weiterhin (491/491).

## Geprüfte Dateien

- `src/Reporter/Platforms/iOS/AppDelegate.cs` (geändert: Deferred-Registrierung, `NSDictionary?`)
