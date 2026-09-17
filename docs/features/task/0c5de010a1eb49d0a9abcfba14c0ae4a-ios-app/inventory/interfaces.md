<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme — Interfaces

Die Anforderung erwartet keine neuen Interfaces. Für den iOS-Backup-Ausschluss (Punkt 8) bietet sich das bestehende Gateway-Muster an (Interface in `Reporter.Core`, Implementierung in `src/Reporter/Services/` mit `#if IOS` und No-op auf anderen Targets). Die folgenden Contracts sind für die Anforderung relevant.

## `IDebugLogService`
Datei: `src/Reporter.Core/Interfaces/IDebugLogService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsEnabled` (Property) | — | `bool` | ob die Session-Debug-Sammlung aktiv ist |
| `BeginSessionAsync` | `CancellationToken` | `Task` | Session-Reset (behält `Error`-Einträge), lädt `Settings.DebugCollectionEnabled`; muss **nach** den Migrationen laufen |
| `SetEnabled` | `bool enabled` | `void` | Sammlung zur Laufzeit schalten |
| `LogAsync` | `string category, string message, string? details = null, string level = DebugLogLevel.Info, CancellationToken` | `Task` | Logeintrag schreiben; No-op bei deaktivierter Sammlung, wirft nie |

Relevanz: Logging-Muster für die `MigrateAsync`-Absicherung (Punkt 6) — die Implementierung `DebugLogService` ist selbst fehlerisoliert.

## `INetworkStatusService`
Datei: `src/Reporter.Core/Interfaces/INetworkStatusService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ConnectivityChanged` (Event) | — | `EventHandler?` | Online-/Offline-Wechsel, auf dem UI-Thread ausgelöst |
| `IsOnline` (Property) | — | `bool` | aktueller Netzwerkzustand |

Relevanz: Online-Guard für das neue WebView-Navigationsverhalten (Punkt 3, `_viewModel.IsOnline`) und für `OpenInBrowserAsync`.

## `IDemoContentService`
Datei: `src/Reporter.Core/Interfaces/IDemoContentService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `EnsureSeededAsync` | `CancellationToken` | `Task` | Demo-Seed beim ersten Start; No-op danach und bei `REPORTER_DISABLE_DEMO_SEED` |

Relevanz: Review-Hinweis „kein Login erforderlich" (Punkt 12).

## `IDebugReportService`
Datei: `src/Reporter.Core/Interfaces/IDebugReportService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsSupported` (Property) | — | `bool` | Mail-Compose-Verfügbarkeit |
| `SendReportAsync` | `CancellationToken` | `Task<bool>` | Debugbericht an den System-Mail-Client übergeben |

Relevanz: einziger vom Anwender ausgelöste Datenabfluss (E-Mail) — Inhalt für Datenschutzerklärung und App-Privacy-Label (Punkte 1/2).

## `IEmailService`
Datei: `src/Reporter.Core/Interfaces/IEmailService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsSupported` (Property) | — | `bool` | Compose-Unterstützung der Plattform |
| `ComposeAsync` | `string recipient, string subject, string body, CancellationToken` | `Task<bool>` | vorbefüllten Mail-Entwurf im System-Mail-Client öffnen |

## `IDeviceInfoProvider`
Datei: `src/Reporter.Core/Interfaces/IDeviceInfoProvider.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetSnapshot` | — | `AppDeviceInfo` | App-/Geräte-Informationen für den Debugbericht |

## `IFeedSearchService`
Datei: `src/Reporter.Core/Interfaces/IFeedSearchService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchAsync` | `string query, CancellationToken` | `Task<IReadOnlyList<FeedSearchResult>>` | Feed-Suche über `feedsearch.dev` + Autodiscovery; `FeedSearchUnavailableException` bei Ausfall beider Quellen |

Relevanz: Datenfluss an Fremdhost (`feedsearch.dev`) für die Datenschutz-Doku.

## `IFeedSyncService`
Datei: `src/Reporter.Core/Interfaces/IFeedSyncService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SyncFeedAsync` | `Guid feedId, CancellationToken` | `Task<SyncResult>` | einen Feed abrufen und speichern |
| `SyncAllAsync` | `CancellationToken` | `Task<SyncResult>` | alle Feeds synchronisieren |

## `IFeedIconService`
Datei: `src/Reporter.Core/Interfaces/IFeedIconService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `FindFaviconUrlAsync` | `string siteUrl, CancellationToken` | `Task<string?>` | verifizierte Favicon-URL der Website |
| `TryFindFaviconUrlAsync` | `string feedUrl, string? siteUrl, CancellationToken` | `Task<string?>` | fehlerisolierter Lookup mit Site-Auflösung |

## `ILocalNotificationService`
Datei: `src/Reporter.Core/Interfaces/ILocalNotificationService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsSupported` (Property) | — | `bool` | Plattform-Support (nur iOS `true`) |
| `RequestAuthorizationAsync` | `CancellationToken` | `Task<bool>` | Nutzer-Autorisierung anfordern |
| `GetAuthorizationStatusAsync` | `CancellationToken` | `Task<NotificationAuthorizationStatus>` | Status ohne Prompt abfragen |
| `ShowAsync` | `string title, string body, string identifier, IReadOnlyDictionary<string,string>? userInfo = null, CancellationToken` | `Task` | lokale Mitteilung anzeigen (gleiche `identifier` ersetzt statt dupliziert) |

## `IBackgroundRefreshService`
Datei: `src/Reporter.Core/Interfaces/IBackgroundRefreshService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsSupported` (Property) | — | `bool` | Plattform-Support (nur iOS `true`) |
| `ApplySettingsAsync` | `Settings settings, CancellationToken` | `Task` | `BGAppRefreshTask` ein-/ausplanen (`Submit`/`Cancel`) |

Beide Gateway-Interfaces (`ILocalNotificationService`, `IBackgroundRefreshService`) sind die Vorlage für einen etwaigen Backup-Ausschluss-Service (Punkt 8): iOS-Implementierung unter `#if IOS`, `IsSupported`-Flag, No-op sonst.
