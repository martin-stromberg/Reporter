<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: HTTP-Feeds unter iOS zulassen (ATS) und Sync-Fehlerursachen in der UI anzeigen (Issue #87)

Bestandsaufnahme des .NET-MAUI-Projekts `Reporter` (`src/Reporter`, `src/Reporter.Core`, `src/Reporter.Data`) bezogen auf die übersetzte Anforderung `requirement.md` (Issue #87): HTTP-Feed-URLs unter iOS/MacCatalyst trotz App Transport Security (ATS) nutzbar machen und die je Feed persistierte Sync-Fehlermeldung in der UI erreichbar machen.

## Zusammenfassung

- **ATS:** Weder `src/Reporter/Platforms/iOS/Info.plist` noch `src/Reporter/Platforms/MacCatalyst/Info.plist` enthalten einen `NSAppTransportSecurity`-Schlüssel — die Apple-Standardpolicy blockiert Klartext-HTTP für den in `MauiProgram` (`AddSingleton<HttpClient>`, `src/Reporter/MauiProgram.cs:55`) registrierten `HttpClient`. Eine plattformspezifische Handler-Konfiguration existiert nicht.
- **HTTP-Pfade:** `FeedSyncService.RunSyncAsync` lädt den Feed über `_httpClient.GetStreamAsync(feed.Url, …)` (`FeedSyncService.cs:155`) ohne Scheme-Behandlung. `FeedsViewModel.IsValidFeedUrl` akzeptiert `http` und `https`; `TryNormalizeDomainUrl` stuft nur bloße Domain-Eingaben auf `https://` hoch, explizit eingegebene `http://`-URLs bleiben unverändert (`TryPersistNewFeedAsync`, `SaveAsync`). `FeedSearchService` liefert `http`-Ergebnisse aus Verzeichnis und Autodiscovery; `FeedIconService` akzeptiert `http`/`https`.
- **Fehlerursache:** Die Fehlermeldung wird bereits als `SyncLog.Message` persistiert (`"Synchronization failed: {ex.Message}"`, `FeedSyncService.cs:94-96`), verknüpft über `SyncLog.FeedId` → `feed_id`-FK in `ConfigureSyncLog`. Es existiert jedoch **keine** Feed-bezogene Abfrage in `ISyncLogRepository` und **keine** Fehlermeldungs-Eigenschaft an `Feed`, `FeedListItem` oder der `feeds`-Tabelle. In der UI zeigt `FeedsViewModel.SyncAsync` nur den generischen Text `AppResources.SyncStatusError`; die Feed-Karte auf `FeedsPage.xaml` zeigt nur das `HealthStatus`-Badge, `OnFeedTapped` bietet keine Fehlerdetails-Aktion.
- **Fehlt offensichtlich:** `NSAppTransportSecurity`-Konfiguration bzw. HTTPS-Upgrade-Logik; `LastErrorMessage`-Property/Spalte (Variante A) oder per-Feed-`SyncLog`-Abfrage (Variante B); UI-Zugang zur letzten Fehlermeldung; lokalisierbare Fehlertexte für den ATS-Fall.

**Test-Ausgangszustand:** `dotnet test src/Reporter.Tests/Reporter.Tests.csproj --configuration Release` — **462/462 Tests bestanden, 0 fehlgeschlagen, 0 übersprungen** (Exit-Code 0). Nachweis und Details: [inventory/tests.md](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md) — `Feed`, `FeedListItem`, `SyncLog` (Core-Modelle), `Feed`-/`SyncLog`-Entitäten, `ReporterDbContext`-Mapping, Migrationen
- [Logik](inventory/logic.md) — `FeedSyncService`, `FeedsViewModel`(+`.Search`), `FeedSearchService`, `FeedIconService`, `FeedSiteResolver`, `MauiProgram`
- [Enums / Konstanten](inventory/enums.md) — `FeedHealth`, `FeedSearchMatchKind`
- [Interfaces](inventory/interfaces.md) — `ISyncLogRepository`, `IFeedRepository`, `IFeedSyncService`, `IFeedSearchService`, `IFeedIconService`
- [Plattform-Konfiguration](inventory/platform.md) — `Info.plist` iOS/MacCatalyst, `Reporter.csproj`-Targets
- [UI](inventory/ui.md) — `FeedsPage.xaml`/`FeedsPage.xaml.cs`, relevante `AppResources`-Schlüssel
- [Tests](inventory/tests.md) — Test-Ausgangszustand, Testklassen und Hilfsmethoden
