<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Interfaces und Enums

## `IFeedSearchService`

Datei: `src/Reporter.Core/Interfaces/IFeedSearchService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchAsync` | `string query`, `CancellationToken cancellationToken = default` | `Task<IReadOnlyList<FeedSearchResult>>` | Sucht Feeds im feedsearch.dev-Directory und per Autodiscovery auf der eingegebenen Site; wirft `FeedSearchUnavailableException`, wenn beide Quellen unerreichbar sind |

Implementierung: `FeedSearchService` (`src/Reporter.Core/Services/FeedSearchService.cs`).
DI-Registrierung: `MauiProgram.cs` Zeile 56 als Singleton.
Test-Double: `FakeFeedSearchService` (`src/Reporter.Tests/FakeFeedSearchService.cs`).
Laut Anforderung bleibt das Interface unverändert — nur die konkrete Klasse bekommt
einen konfigurierbaren Endpunkt.

## `FeedSearchMatchKind`

Datei: `src/Reporter.Core/Models/FeedSearchMatchKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `ExactUrl` | Feed-URL entspricht exakt der Eingabe, oder die Eingabe ist selbst ein Feed-Dokument |
| `Directory` | Treffer aus dem feedsearch.dev-Directory |
| `Discovered` | per `<link rel="alternate">` oder Well-Known-Pfaden auf der Site gefunden |

Die Deklarationsreihenfolge definiert die Sortierreihenfolge in
`FeedSearchService.SearchAsync` (`ExactUrl` zuerst).

## Weitere berührte Contracts (Kontext, unverändert)

- `IFeedSyncService` — `SyncFeedAsync(Guid)`, `SyncAllAsync()`; E2E-relevant, weil
  Refresh-/Add-Flows echte HTTP-Abrufe der Feed-URLs auslösen.
- `IFeedIconService` — `TryFindFaviconUrlAsync(feedUrl, siteUrl)`; wird in allen
  Add-Flows online aufgerufen.
- `INetworkStatusService` — `IsOnline`/Connectivity-Events; steuert `IsOnline` in
  `BaseViewModel`, das die Offline-Trigger und `SearchCommand.CanExecute` speist.
- `ILocalNotificationService` — `IsSupported` steuert `NotificationsSupported`
  (Switch-Enablement und Hinweisbanner im Edit-Sheet).
