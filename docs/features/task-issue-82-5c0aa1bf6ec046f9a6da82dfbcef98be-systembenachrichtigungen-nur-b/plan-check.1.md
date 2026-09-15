<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Keine Systemmitteilung bei manuellem Abruf (Pull-to-Refresh/Einzelabruf) bei laufender App | `WillPresentNotification` → `UNNotificationPresentationOptions.None` (`NotificationDelegate.cs:22`, verifiziert: heute `Banner \| List \| Sound`); Programmablauf 1 | Manueller E2E auf iOS-Gerät (Pflicht-Szenario 1) + Begründung, warum nur manuell prüfbar | Abgedeckt |
| Keine Systemmitteilung bei `AutoRefreshService`-Timer-Abruf bei laufender App | Gleicher Mechanismus über `WillPresentNotification` → `None`; Timer-Pfad bleibt unverändert (`AutoRefreshService.RunLoopAsync`) | Manueller E2E (Pflicht-Szenario 2) | Abgedeckt |
| Keine Systemmitteilung bei `RefreshOnStartupEnabled`-Start-Abruf (`AutoRefreshService.RunStartupSyncAsync`) | Im Programmablauf 1 als Auslöser mitgenannt; gleicher `WillPresentNotification`-Mechanismus; `App.xaml.cs:92` ruft `StartAsync` (verifiziert) | **Kein E2E-Szenario** — die E2E-Tabelle deckt nur manuellen Sync und Timer-Abruf ab, der in der Anforderung explizit genannte dritte Auslöser fehlt | Lücke |
| Echter iOS-Hintergrundabruf (`BGTaskScheduler`/`BGAppRefreshTask`): Registrierung, Scheduling, Sync-Ausführung | `Info.plist` um `UIBackgroundModes`/`fetch` + `BGTaskSchedulerPermittedIdentifiers` (verifiziert: beide fehlen aktuell, `Info.plist`); `AppDelegate.RegisterBackgroundFetchTask`/`HandleRefreshTaskAsync` (verifiziert: `FinishedLaunching` setzt heute nur den Notification-Delegate); `BackgroundRefreshService` mit `Submit`/`Cancel` + `RunScheduledSyncAsync` | Manueller E2E via lldb `_simulateLaunchForTaskWithIdentifier` (Pflicht-Szenario 3) — Plattformcode ist nicht unit-testbar, Begründung nachvollziehbar | Abgedeckt |
| Expiration-Behandlung und Lebenszyklus-Sicherheit (`SetTaskCompleted`, Neuplanung auch im Fehlerfall) | `ExpirationHandler` + `CancellationTokenSource` im `AppDelegate`; `RunScheduledSyncAsync` fehlerisoliert mit `IDebugLogService` (`DebugLogCategory.Sync`, `Error`) und Neuplanung via `ISettingsRepository` + `ApplySettingsAsync` | Manueller E2E `_simulateExpirationForTaskWithIdentifier` (Pflicht-Szenario 4) | Abgedeckt |
| Bestehendes Benachrichtigungs-Regelwerk (`NotificationsEnabled`, `Feed.NotificationsEnabled`, Ruhezeiten, Keyword-Filter, `NotificationSummaryEnabled`) bleibt maßgeblich | Unverändert — `NotificationService`/`FeedSyncService` werden nicht angefasst; einziger `NotifyNewItemsAsync`-Aufrufer bleibt `FeedSyncService.RunSyncAsync` (`FeedSyncService.cs:192`, verifiziert) | Bestehende `NotificationServiceTests`/`FeedSyncServiceTests` bleiben unverändert gültig (Signaturen stabil, verifiziert) | Abgedeckt |
| Tap-Handling/Navigation (`DidReceiveNotificationResponse`) unverändert | Explizit als unverändert deklariert | E2E-Regressionsszenario 6 (Tap → `articledetail`/`//unread`, Link-Fallback) | Abgedeckt |
| Steuerung des Hintergrundabrufs ohne neuen Schalter (Kopplung an `AutoRefreshEnabled`/`RefreshIntervalMinutes`, Anwender-Entscheidung) | `AutoRefreshService.ApplySettingsAsync` leitet `settings` fehlerisoliert an `IBackgroundRefreshService.ApplySettingsAsync` weiter — vor dem `AutoRefreshEnabled`-Early-Return (verifiziert: Early-Return in `AutoRefreshService.cs:78-81`, Aufrufer `SettingsViewModel.PersistAsync` Zeile 825-828 und `App.OnStart` → `StartAsync` → `ApplySettingsAsync` Zeile 46-47 decken Änderungs- und Startfall ab); `EarliestBeginDate` mit gleichem Clamp 1–1440 (Konstanten verifiziert, Zeilen 14-15) | Unit-Tests `ApplySettings_ForwardsToBackgroundRefresh`, `ApplySettings_WhenAutoRefreshDisabled_StillForwardsToBackgroundRefresh`, `ApplySettings_BackgroundRefreshThrows_TimerStillConfigured`; E2E-Szenario 5 (Schalter aus → kein Task, Wiederaktivierung → Neuplanung) | Abgedeckt |
| Windows/Android/MacCatalyst bleiben No-Op (außer Scope, Anwender-bestätigt) | `IsSupported == false` auf Nicht-iOS-Targets; `#if IOS`-Implementierung nach `LocalNotificationService`-Muster | Implizit über Gateway-Design; kein dedizierter No-Op-Test, aber auch keiner nötig, da `Reporter.Tests` das MAUI-Projekt nicht referenziert | Abgedeckt |
| Fehlerisolierung/Diagnose des Hintergrund-Tasks | `RunScheduledSyncAsync` isoliert Fehler, `IDebugLogService`-Protokollierung, `SetTaskCompleted` läuft immer, Neuplanung auch im Fehlerfall | E2E-Szenario 4 (Expiration) + Szenario 3 (Sync-Ausführung); Weiterleitungs-Fehlerisolierung via Unit-Test | Abgedeckt |
| Verifikation und Doku-Anpassung | Schritt 10: `dotnet test`, `Run-StaticChecks.ps1`, manuelle iOS-Verifikation via `scripts/iOS-Deployment.ps1`, Ergebnisse in `test-results.md`; Schritt 9: vier `docs/help`-Dateien (Anforderung verlangte zwei — übertroffen) | Als Umsetzungsschritte enthalten | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] E2E-Szenario für den `RefreshOnStartupEnabled`-Start-Abruf fehlt: Die Anforderung nennt ausdrücklich drei Vordergrund-Auslöser (manuell, `AutoRefreshService`-Timer, Start-Abruf via `RunStartupSyncAsync`), deren Mitteilungen unterdrückt werden müssen. Die E2E-Tabelle enthält Pflicht-Szenarien für manuellen Sync und Timer-Abruf, aber kein Szenario „App-Start mit aktiviertem `RefreshOnStartupEnabled` und neuen Artikeln → keine Systemmitteilung" (Kaltstart/Warmstart, App kommt dabei in den Vordergrund). Der Start-Abruf ist ein eigener Trigger-Pfad (fire-and-forget aus `AutoRefreshService.StartAsync`, `AutoRefreshService.cs:49-52`) und sollte als manuelles E2E-Szenario ergänzt werden.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Manueller Sync bei Vordergrund-App → keine Mitteilung | Manuell via `scripts/iOS-Deployment.ps1`, `test-results.md` | Abgedeckt |
| Timer-Abruf bei laufender App → keine Mitteilung | Manuell, `test-results.md` | Abgedeckt |
| Start-Abruf (`RefreshOnStartupEnabled`) bei laufender App → keine Mitteilung | — | Lücke |
| `BGAppRefreshTask` (lldb-simuliert) → Sync + sichtbare Mitteilung | Manuell, `_simulateLaunchForTaskWithIdentifier` | Abgedeckt |
| Task-Expiration → Sync kancelliert, `SetTaskCompleted`, App stabil | Manuell, `_simulateExpirationForTaskWithIdentifier` | Abgedeckt |
| `AutoRefreshEnabled` aus/an → Task abgemeldet/neu eingeplant | Manuell, `test-results.md` | Abgedeckt |
| Antippen einer Hintergrund-Mitteilung → Navigation wie bisher | Manuell, `test-results.md` | Abgedeckt |

## Fehlende oder unvollständige Planbestandteile

- [ ] E2E-Pflicht-Szenario „Start-Abruf bei aktiviertem `RefreshOnStartupEnabled` erzeugt keine Systemmitteilung" ergänzen (siehe oben) — betrifft nur den Tests-Abschnitt, die Umsetzungsschritte selbst sind vollständig.

## Hinweise

- Alle Plan-Aussagen wurden stichprobenartig gegen den Code verifiziert und sind korrekt: `NotificationDelegate.cs:22` liefert `Banner | List | Sound`; `AppDelegate.FinishedLaunching` (`AppDelegate.cs:26-30`) registriert heute nur den `UNUserNotificationCenter`-Delegate; `Info.plist` enthält weder `UIBackgroundModes` noch `BGTaskSchedulerPermittedIdentifiers`; `AutoRefreshService`-Konstruktor (Zeile 34) und `ApplySettingsAsync`-Early-Return (Zeilen 78-81) passen zu den geplanten Änderungen; exakt zwei `new AutoRefreshService(...)`-Aufrufstellen existieren (`AutoRefreshServiceTests.cs:32`, `AutoRefreshServiceTests_DebugLog.cs:33`) — die Liste betroffener Tests ist vollständig.
- `ServiceCollectionTests` spiegeln die `MauiProgram`-Registrierung manuell (`AddTestRepositories` + `AddSingleton`-Kette, `ServiceCollectionTests.cs:64-88`) — das geplante `AddReporterServices_ResolvesAutoRefreshService` mit `FakeBackgroundRefreshService` passt zum Muster (`BackgroundRefreshService` selbst liegt im MAUI-Projekt und ist nicht referenzierbar).
- Die Sync-/Neuplanungslogik in `BackgroundRefreshService.RunScheduledSyncAsync` ist mangels MAUI-Referenz in `Reporter.Tests` nicht unit-testbar; die geplante lldb-Simulation ist der einzig gangbare Nachweis und im Plan benannt. Kein Vollständigkeitsmangel.
- Alle offenen Fragen aus `requirement.md` wurden durch Anwender-Entscheidungen verbindlich aufgelöst und sind in „Designentscheidungen" nachvollziehbar dokumentiert (Variante `WillPresentNotification`, `None` statt `List`, Kopplung an Auto-Refresh-Schalter, iOS-only Scope).
- Risiken sind vollständig benannt (iOS-Code baut lokal nicht, systemgesteuerte Ausführungshäufigkeit, iOS-Systemschalter, Identifier-Konsistenz, Lebenszyklus-Sicherheit).
