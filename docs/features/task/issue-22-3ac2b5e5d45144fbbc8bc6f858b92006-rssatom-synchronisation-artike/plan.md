# Umsetzungsplan

## Offene Punkte
Keine.

## Änderungen

### 1. Domain-Modell
- `Reporter.Core/Models/FeedHealth.cs`: Konstanten `Ok`, `Warning`, `Error` sowie Hilfsmethode `Changed(string? current, string? next)`.

### 2. Repository-Erweiterungen
- `IItemRepository.GetByGuidOrHashAsync(Guid feedId, string guidOrHash)` – prüft Dubletten pro Feed.
- `ItemRepository.GetByGuidOrHashAsync(...)` implementieren.

### 3. Sync-Service
- Neues `Reporter.Core/Interfaces/IFeedSyncService.cs`:
  - `Task<SyncResult> SyncFeedAsync(Guid feedId, CancellationToken cancellationToken = default)`
  - `Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default)`
- `SyncResult` (Record in Core): `NewItems`, `Status`, `Message`.
- `Reporter.Core/Services/FeedSyncService.cs`:
  - Lädt Feed-XML per `HttpClient`.
  - Parst mit `System.ServiceModel.Syndication.SyndicationFeed`.
  - Bestimmt `GuidOrHash` (`item.Id` oder SHA256-Hash aus Link + `PublishedAt`).
  - Fügt neue `Item`-Entitäten ein, sofern `GetByGuidOrHashAsync` kein Duplikat findet.
  - Aktualisiert `Feed.LastCheckedAt` und `Feed.HealthStatus`/`HealthLastChange`.
  - Schreibt `SyncLog`-Eintrag mit `StartedAt`, `FinishedAt`, `Status`, `Message`.
  - Health-Berechnung:
    - `OK`: Abruf & Parse erfolgreich.
    - `Error`: `HttpRequestException`, `TimeoutException`, `XmlException`/ungültiges XML oder sonstige Ausnahme.
    - `Warning`: erfolgreicher Abruf, aber Anzahl Items < 50 % der bisherigen Items für diesen Feed **oder** kein neues Item seit > 30 Tagen.
  - Exception-Handling: unabhängig vom Ergebnis werden bestehende Artikel nicht gelöscht.

### 4. Dependency Injection
- `Reporter.Core.csproj` bekommt PackageReference `System.ServiceModel.Syndication` (Version 10.0.11).
- `MauiProgram.cs`:
  - Registriert `HttpClient` als Singleton.
  - Registriert `IFeedSyncService` -> `FeedSyncService` als Singleton.

### 5. ViewModel / UI
- `FeedsViewModel` erweitern:
  - `RefreshCommand` (`AsyncRelayCommand<FeedListItem?>`) – startet Sync für einen Feed.
  - `RefreshAllCommand` (`AsyncRelayCommand`) – startet Sync für alle Feeds.
  - `IsSyncing`/`SyncStatusMessage` für UI-Status (non-blocking).
- `FeedsPage.xaml`:
  - „Refresh all“-Button über der Feed-Liste.
  - Pro Feed-Zeile „Refresh“-Button.

### 6. Lokalisierung
- `AppResources.resx` und `AppResources.de.resx` erweitern um:
  - `ButtonRefresh`, `ButtonRefreshAll`, `SyncStatusInProgress`, `SyncStatusSuccess`, `SyncStatusError`, `SyncStatusWarning`, `LabelSyncStatus`.
- `AppResources.Designer.cs` aktualisieren.

### 7. Migration
- EF Core Migration `AddFeedSyncFields` ist nicht nötig, da `last_checked_at`, `health_status`, `health_last_change` und `content_html` bereits im Initial-Create vorhanden sind.

### 8. Tests
- `FeedSyncServiceTests` (Integration, In-Memory-SQLite + Fake-HttpClient):
  - **Happy path:** Feed mit RSS-XML wird geparst, zwei neue Items werden gespeichert, Sync-Log hat `Status = OK`.
  - **Duplikate:** Zweites Sync mit gleichem XML fügt keine neuen Items ein, `NewItems = 0`.
  - **Fehler – nicht erreichbar:** `HttpClient` wirft `HttpRequestException`; Health wird `Error`, bestehende Artikel bleiben.
  - **Fehler – ungültiges XML:** Health wird `Error`, Sync-Log enthält Fehler.
  - **Warnung – weniger Items:** Feed hatte 4 Items, liefert nun 1; Health wird `Warning`.
  - **Warnung – keine neuen Artikel seit 30 Tagen:** Health wird `Warning`.
- `FeedsViewModelTests` erweitern:
  - `RefreshCommand_InvokesSyncService_AndReloadsList`.
  - `RefreshAllCommand_InvokesSyncService_AndReloadsList`.
- Plan-E2E / UI-Test:
  - `FeedSyncFlows` (Integration): Simuliere User-Druck auf „Refresh“ für einen Feed und prüfe, dass danach ungelesene Artikel in der Datenbank vorhanden sind. Da kein separates MAUI-E2E-Framework eingerichtet ist, wird die ViewModel-Schicht als UI-Integration getestet.

## Akzeptanzkriterien -> Testfälle
| Kriterium | Test |
|-----------|------|
| Manuelles Refresh speichert neue Artikel | `FeedSyncServiceTests.SyncFeedAsync_ValidRss_CreatesItems` |
| Duplikate werden nicht erneut eingefügt | `FeedSyncServiceTests.SyncFeedAsync_Duplicates_SkipsExistingItems` |
| Bei Fehler bleiben bestehende Artikel erhalten | `FeedSyncServiceTests.SyncFeedAsync_Unreachable_KeepsItemsAndLogsError` |
| Health-Status wird korrekt aktualisiert | `FeedSyncServiceTests.SyncFeedAsync_FewerItems_SetsWarning` / `_InvalidXml_SetsError` |
| Sync-Log protokolliert jeden Lauf | `FeedSyncServiceTests.SyncFeedAsync_WritesSyncLog` |
