<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### AppDelegate.cs (AppDelegate)

- **Fehlerbehandlung** — `BGTaskScheduler.Shared.Register(…)` (`AppDelegate.cs:42`) liefert einen `bool`-Rückgabewert, der ignoriert wird. Schlägt die Registrierung fehl (z. B. weil der Identifier nicht in `BGTaskSchedulerPermittedIdentifiers` eingetragen oder die Task-Art nicht erlaubt ist), ist der Ausfall komplett unsichtbar — weder `Debug.WriteLine` noch ein `IDebugLogService`-Eintrag, obwohl der Launch-Handler dann nie aufgerufen wird und der Hintergrundabruf still ausfällt. Alle anderen neuen Fehlerpfade des Features protokollieren.

  Empfehlung: Rückgabewert von `Register` auswerten und bei `false` mindestens `Debug.WriteLine` schreiben (analog `BackgroundRefreshService.cs:63`); optional — sofern `IPlatformApplication.Current?.Services` an dieser Stelle bereits auflösbar ist — einen `IDebugLogService`-Warning-Eintrag (`DebugLogCategory.Sync`) schreiben.

- **Fehlerbehandlung** — Der Null-Zweig `scheduledSyncRunner is not null` (`AppDelegate.cs:62-66`) schließt den Task bei nicht auflösbarem `IScheduledSyncRunner` still mit `SetTaskCompleted(false)` ab, ohne etwas zu protokollieren — obwohl `debugLogService` eine Zeile vorher bereits aufgelöst wurde. Eine fehlende oder kaputte DI-Registrierung würde den Hintergrundabruf lautlos deaktivieren, ohne dass der Fehler im Debug-Log sichtbar wäre.

  Empfehlung: Im `else`-Zweig `Debug.WriteLine` und `_ = debugLogService?.LogAsync(DebugLogCategory.Sync, "Scheduled sync runner not resolved", …, DebugLogLevel.Warning)` ergänzen.

## Verifizierung der Iteration-1-Befunde (alle behoben)

- **`IsSupported` verdrahtet:** `AutoRefreshService.ApplySettingsAsync` (`AutoRefreshService.cs:84`) wertet `IBackgroundRefreshService.IsSupported` aus und leitet nur bei `true` weiter; neuer Test `ApplySettings_BackgroundRefreshUnsupported_DoesNotForward` deckt das ab. `FakeBackgroundRefreshService.RunScheduledSyncResult` wurde entfernt.
- **Clamp-Konstanten zentral:** `SettingsValues` (`SettingsValues.cs:58-94`) enthält `MinRefreshIntervalMinutes`/`MaxRefreshIntervalMinutes`/`ClampRefreshIntervalMinutes`; beide Verwender (`AutoRefreshService.cs:100`, `BackgroundRefreshService.cs:53`) referenzieren die zentrale Stelle; keine weiteren Clamp-Duplikate im Code.
- **Orchestrierung in `Reporter.Core`:** `IScheduledSyncRunner`/`ScheduledSyncRunner` (`ScheduledSyncRunner.cs`) kapseln Sync + Neuplanung + `bool`-Ergebnis; `BackgroundRefreshService` enthält nur noch `BGTaskScheduler`-Aufrufe. Vier neue Tests (`ScheduledSyncRunnerTests.cs`) decken Erfolg, Sync-Fehler (Neuplanung trotzdem, Ergebnis `false`, Error-Log), Kancellierung und Neuplanungs-Fehler (Ergebnis `true`, Warning-Log) ab — alle bestehen (`dotnet test`: 31/31 grün für die betroffenen Testklassen).
- **`AppDelegate`-catch protokolliert:** `HandleRefreshTaskAsync` (`AppDelegate.cs:68-74`) schreibt `Debug.WriteLine` + `IDebugLogService`-Warning; `IDebugLogService` wird vor dem Runner aufgelöst, sodass die Protokollierung auch bei fehlgeschlagener Runner-Auflösung greift.

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

## Zusatzprüfungen (ohne Befund)

- **iOS-API-Vertrag (Sichtprüfung, da auf Windows nicht kompilierbar):** `BGTaskScheduler.Shared.Register(string, DispatchQueue?, Action<BGTask>)` mit `null`-Queue, `BGAppRefreshTaskRequest(string)` + `EarliestBeginDate` (`NSDate.FromTimeIntervalSinceNow`), `Submit(request, out NSError)`, `Cancel(identifier)`, `BGTask.ExpirationHandler` (Methodengruppe `cts.Cancel`) und `SetTaskCompleted(bool)` entsprechen den .NET-iOS-Bindings. Registrierung erfolgt innerhalb von `FinishedLaunching` nach `base.FinishedLaunching` (Apple-Vorgabe eingehalten); `Info.plist` enthält `UIBackgroundModes`=`fetch` und `BGTaskSchedulerPermittedIdentifiers` identisch zu `RefreshTaskIdentifier`. Usings (`BackgroundTasks`, `Foundation`, `Microsoft.Extensions.DependencyInjection`, `Reporter.Core.Interfaces`, `Reporter.Core.Services`, `Reporter.Services`) sind alle erforderlich — `DebugLogCategory`/`DebugLogLevel` liegen in `Reporter.Core.Services`.
- **`ScheduledSyncRunner`:** Ruft `IBackgroundRefreshService.ApplySettingsAsync` bewusst ohne `IsSupported`-Prüfung auf — das Interface dokumentiert No-Op-Implementierungen auf nicht unterstützten Plattformen, der Auftruf ist damit harmlos. Neuplanung läuft mit `CancellationToken.None`, damit eine abgelaufene Task-Frist den Folgeabruf nicht verhindert — dokumentierte Absicht (Kommentar Zeile 49-50).
- **`RaiseUiActionRequested`:** Muster wird in diesem Branch nicht verwendet — Regel nicht zutreffend.
- **DI-Auflösung:** `IScheduledSyncRunner`- und `IBackgroundRefreshService`-Registrierungen in `MauiProgram.cs:67-68` ergänzt; optionale `IDebugLogService`/`TimeProvider`-Parameter werden vom MS-DI-Container über Default-Werte bedient. `ServiceCollectionTests` spiegelt beide Registrierungen.
- **Build/Tests:** `dotnet build` für `Reporter.Core` und `Reporter.Tests` ohne Warnungen/Fehler; `dotnet test` für die betroffenen Testklassen: 31/31 bestanden.
