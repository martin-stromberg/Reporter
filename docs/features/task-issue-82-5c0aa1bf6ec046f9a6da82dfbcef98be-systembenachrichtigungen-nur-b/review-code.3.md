<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Keine Befunde

## Verifizierung der Iteration-2-Befunde (in Iteration 3 behoben)

- **`BGTaskScheduler.Register`-Rückgabewert geprüft + protokolliert:** `RegisterBackgroundFetchTask` (`AppDelegate.cs:42-55`) wertet den `bool`-Rückgabewert von `BGTaskScheduler.Shared.Register` aus; bei `false` wird `Debug.WriteLine` mit dem Identifier geschrieben und — über `IPlatformApplication.Current?.Services?.GetService<IDebugLogService>()` (null-sicher aufgelöst) — ein `IDebugLogService`-Eintrag (`DebugLogCategory.Sync`, `DebugLogLevel.Warning`) mit dem Identifier als Details. `IPlatformApplication.Current` ist im `MauiUIApplicationDelegate`-Konstruktor gesetzt, `Services` ist nach `base.FinishedLaunching` befüllt — die Auflösung greift also zum Aufrufzeitpunkt.
- **Null-Runner-Zweig protokolliert:** Der `else`-Zweig bei nicht auflösbarem `IScheduledSyncRunner` (`AppDelegate.cs:74-78`) schreibt `Debug.WriteLine` plus `IDebugLogService`-Warning (`"Scheduled sync runner not resolved"`, `details: null`) — `debugLogService` wird eine Zeile vorher aufgelöst, die Signatur `LogAsync(string, string, string? details = null, string level = Info, …)` (`IDebugLogService.cs:49`) akzeptiert `null`. `SetTaskCompleted(false)` läuft weiterhin im `finally`.

Beide Fixes sind korrekt umgesetzt und haben keine neuen Probleme eingeführt: Die `LogAsync`-Aufrufe sind fire-and-forget (`_ =`) wie überall im Codebase-Muster, `success` bleibt in beiden Pfaden `false`, und die Service-Auflösung im Fehlerpfad der Registrierung ist komplett null-konditional.

## Verifizierung der Iteration-1-Befunde (unverändert behoben)

- `IsSupported` wird in `AutoRefreshService.ApplySettingsAsync` (`AutoRefreshService.cs:84`) ausgewertet; Test `ApplySettings_BackgroundRefreshUnsupported_DoesNotForward` deckt den Pfad ab.
- Clamp-Grenzen liegen zentral in `SettingsValues` (`SettingsValues.cs:55-95`: `MinRefreshIntervalMinutes`/`MaxRefreshIntervalMinutes`/`ClampRefreshIntervalMinutes`); beide Verwender referenzieren sie (`AutoRefreshService.cs:100`, `BackgroundRefreshService.cs:53`).
- Orchestrierung in `ScheduledSyncRunner` (`Reporter.Core`) mit vier Tests — alle bestehen.
- `HandleRefreshTaskAsync`-catch protokolliert per `Debug.WriteLine` + `IDebugLogService`-Warning (`AppDelegate.cs:80-86`).

## Zusatzprüfungen (ohne Befund)

- **iOS-API-Vertrag (Sichtprüfung, auf Windows nicht kompilierbar):** `BGTaskScheduler.Shared.Register(string, DispatchQueue?, Action<BGTask>)` → `bool` (`queue` ist `[NullAllowed]`, `null` = Haupt-Queue), `BGTask.ExpirationHandler` (`Action`, Methodengruppe `cts.Cancel`), `SetTaskCompleted(bool)`, `Submit(BGAppRefreshTaskRequest, out NSError)` → `bool`, `Cancel(string)`, `NSDate.FromTimeIntervalSinceNow`, `UNNotificationPresentationOptions.None` entsprechen den .NET-iOS-Bindings. `IPlatformApplication`/`GetService<T>` sind über `ImplicitUsings` (`Reporter.csproj:26`) + `Microsoft.Extensions.DependencyInjection` abgedeckt; alle Usings in `AppDelegate.cs` werden benötigt. Registrierung liegt innerhalb von `FinishedLaunching` (Apple-Vorgabe), `Info.plist` enthält `UIBackgroundModes`=`fetch` und `BGTaskSchedulerPermittedIdentifiers` identisch zu `RefreshTaskIdentifier`.
- **Defensiver `task is BGAppRefreshTask`-Check ohne `else` (`AppDelegate.cs:44`):** Für den Identifier wird ausschließlich eine `BGAppRefreshTaskRequest` submitted — ein andersartiger `BGTask` ist praktisch unerreichbar. Kein Befund; dokumentiert, falls später weitere Task-Typen unter dem Identifier geplant werden (dann `else` mit `SetTaskCompleted(false)` + Log nötig).
- **`ScheduledSyncRunner`:** Neuplanung läuft bewusst mit `CancellationToken.None` (Kommentar `ScheduledSyncRunner.cs:49-50`) — eine abgelaufene Task-Frist verhindert den Folgeabruf nicht; `success` spiegelt nur das Sync-Ergebnis, Neuplanungs-Fehler degradieren auf Warning ohne das Ergebnis zu verfälschen (Test `RunAsync_ReschedulingFails_ReturnsTrue`).
- **DI:** `IBackgroundRefreshService`/`IScheduledSyncRunner` in `MauiProgram.cs:67-68` registriert; alle Konstruktor-Abhängigkeiten (`IFeedSyncService`, `ISettingsRepository`, `INetworkStatusService`, `IBackgroundRefreshService`, optional `IDebugLogService`/`TimeProvider` per Default-Parameter) sind auflösbar; `ServiceCollectionTests` spiegelt beide Registrierungen.
- **`RaiseUiActionRequested`:** Muster wird in der Codebasis nicht verwendet — Regel nicht zutreffend.
- **Tests/Build:** `dotnet test` für `ScheduledSyncRunnerTests`, `AutoRefreshServiceTests(,_DebugLog)` und `ServiceCollectionTests`: 31/31 bestanden.
- **Lizenzheader:** Alle neuen Quelldateien tragen den PolyForm-Header.

## Geprüfte Dateien

Quelldateien:
- `src/Reporter.Core/Interfaces/IBackgroundRefreshService.cs` (neu)
- `src/Reporter.Core/Interfaces/IScheduledSyncRunner.cs` (neu)
- `src/Reporter.Core/Services/ScheduledSyncRunner.cs` (neu)
- `src/Reporter.Core/Services/AutoRefreshService.cs` (geändert)
- `src/Reporter.Core/Models/SettingsValues.cs` (geändert)
- `src/Reporter/Services/BackgroundRefreshService.cs` (neu)
- `src/Reporter/MauiProgram.cs` (geändert)
- `src/Reporter/Platforms/iOS/AppDelegate.cs` (geändert)
- `src/Reporter/Platforms/iOS/NotificationDelegate.cs` (geändert)
- `src/Reporter/Platforms/iOS/Info.plist` (geändert)
- `src/Reporter.Tests/FakeBackgroundRefreshService.cs` (neu)
- `src/Reporter.Tests/ScheduledSyncRunnerTests.cs` (neu)
- `src/Reporter.Tests/AutoRefreshServiceTests.cs` (geändert)
- `src/Reporter.Tests/AutoRefreshServiceTests_DebugLog.cs` (geändert)
- `src/Reporter.Tests/ServiceCollectionTests.cs` (geändert)

Dokumentation (Konsistenz zum Code geprüft, keine Befunde):
- `docs/help/benachrichtigungen/ablauf-technisch.md`
- `docs/help/benachrichtigungen/architektur.md`
- `docs/help/einstellungen/ablauf-technisch.md`
- `docs/help/einstellungen/architektur.md`
