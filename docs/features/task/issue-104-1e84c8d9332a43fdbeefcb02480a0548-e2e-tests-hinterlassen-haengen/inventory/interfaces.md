<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces — für den Shutdown-/Lebenszyklus relevante Contracts

Nur die in der Anforderung genannten bzw. für die Shutdown-Hypothese relevanten Interfaces. Keines davon deklariert einen `Dispose`-/`Quit`-Contract.

## `IAutoRefreshService`

Datei: `src/Reporter.Core/Interfaces/IAutoRefreshService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `StartAsync` | `CancellationToken cancellationToken = default` | `Task` | Startet den Auto-Refresh (Timer-Loop + optionaler Startup-Sync). Aufgerufen von `App.OnStart` (`App.xaml.cs:184`). |
| `ApplySettingsAsync` | `Settings settings` | `Task` | Wendet Settings an (Timer neu konfigurieren bzw. stoppen, OS-Hintergrundabruf spiegeln). |
| `StopAsync` | — | `Task` | Stoppt den Timer-Loop. **Wird im Produktivcode nirgends aufgerufen** — kein Shutdown-Pfad verdrahtet. |

## `INetworkStatusService`

Datei: `src/Reporter.Core/Interfaces/INetworkStatusService.cs`

| Member | Parameter | Typ/Rückgabewert | Zweck |
|--------|-----------|------------------|-------|
| `ConnectivityChanged` | — | `event EventHandler?` | Ereignis bei Konnektivitätswechsel; `NetworkStatusService` marshalled via `MainThread.BeginInvokeOnMainThread`. |
| `IsOnline` | — | `bool` | Aktueller Online-Status. |

## `IDebugLogService`

Datei: `src/Reporter.Core/Interfaces/IDebugLogService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsEnabled` | — | `bool` | Ob das Session-Logging aktiv ist. |
| `BeginSessionAsync` | `CancellationToken cancellationToken = default` | `Task` | Session-Log zurücksetzen (Error-Einträge bleiben), Aktivierung aus Settings laden. |
| `SetEnabled` | `bool enabled` | `void` | Schalter zur Laufzeit. |
| `LogAsync` | `string category, string message, string? details = null, string level = DebugLogLevel.Info, CancellationToken cancellationToken = default` | `Task` | Eintrag schreiben; wird in `App.xaml.cs` an mehreren Stellen **fire-and-forget** aufgerufen (`OnSleep`, `OnResume`, Exception-Handler, Startup-Fehler). |
