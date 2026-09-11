# Logik

Bestehende Logikklassen und Ankerpunkte, die für die Anforderung „Lokale iOS-Benachrichtigungen mit Ruhezeiten" relevant sind. **Ein Benachrichtigungs-Service existiert noch nicht** — weder ein Entscheidungs-Service in `Reporter.Core` noch ein Plattformdienst für lokale Benachrichtigungen.

## `FeedSyncService`
Datei: `src/Reporter.Core/Services/FeedSyncService.cs` — implementiert `IFeedSyncService`

Konstruktor-Abhängigkeiten (Zeilen 27-37): `IFeedRepository`, `IItemRepository`, `ISyncLogRepository`, `HttpClient`. **Kein `TimeProvider`, keine Benachrichtigungs-Abhängigkeit.**

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(Guid feedId, CancellationToken)` | public | Legt `SyncLog` an, lädt Feed per `IFeedRepository.GetByIdAsync`, delegiert an `RunSyncAsync`; fängt Exceptions → `FeedHealth.Error` (Zeilen 40-70) |
| `SyncAllAsync(CancellationToken)` | public | Iteriert alle Feeds (`IFeedRepository.GetAllAsync`), ruft je Feed `SyncFeedAsync` auf und aggregiert Status/Anzahl (Zeilen 73-104) |
| `RunSyncAsync(Feed, SyncLog, CancellationToken)` | private | Lädt RSS/Atom via `HttpClient`, prüft pro Eintrag Dublette via `IItemRepository.GetByGuidOrHashAsync`, speichert neue `Item`s via `AddAsync`, zählt sie in `newItems` — die Liste selbst wird **nicht** zurückgegeben (Zeilen 106-165) |
| `DetermineStatus(...)` | private static | `Warning` bei deutlich weniger Feed-Einträgen oder >30 Tage ohne neue Artikel, sonst `Ok` (Zeilen 167-181) |
| `UpdateFeedHealthAsync(Feed, string)` | private | Aktualisiert `LastCheckedAt`, `HealthStatus`, `HealthLastChange` via `IFeedRepository.UpdateAsync` — rekonstruiert `Feed` **ohne** weitere Felder; ein neues Feld müsste hier mitgereicht werden (Zeilen 183-201) |
| `UpdateLogAsync(SyncLog, string, string?)` | private | Finalisiert `SyncLog` mit `FinishedAt`, Status, Message (Zeilen 203-214) |
| `GetContentHtml(SyndicationItem)` | private static | Extrahiert `Content` oder `Summary` als HTML (Zeilen 216-229) |
| `NormalizeGuidOrHash(SyndicationItem, string?, DateTime?)` | private static | GUID ≤ 500 Zeichen direkt, sonst SHA256 über `title|link|publishedAt` (Zeilen 231-243) |

Publizierte/abonnierte Events: keine.

**Erweiterungspunkt:** `RunSyncAsync` (Zeilen 124-155) ist die einzige Stelle, an der die tatsächlich neu eingefügten `Item`s materialisiert vorliegen (`item`-Objekte in der Schleife). Rückgabe ist `SyncResult` (Zeile 164).

## `AutoRefreshService`
Datei: `src/Reporter.Core/Services/AutoRefreshService.cs` — implementiert `IAutoRefreshService`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartAsync(CancellationToken)` | public | Lädt Settings via `ISettingsRepository.GetAsync` und ruft `ApplySettingsAsync` (Zeilen 36-40) |
| `ApplySettingsAsync(Settings)` | public | Startet Timer-Loop neu, wenn `AutoRefreshEnabled`; Intervall geclamppt 1-1440 min (Zeilen 43-64) |
| `StopAsync()` | public | Stoppt die Loop (Zeilen 67-78) |
| `RunLoopAsync(TimeSpan, CancellationToken)` | private | `PeriodicTimer` mit injiziertem `TimeProvider`; ruft pro Tick `IFeedSyncService.SyncAllAsync` — der periodische Sync-Pfad für Benachrichtigungen (Zeilen 109-134) |

**Muster:** `TimeProvider? timeProvider = null` im Konstruktor mit Fallback `TimeProvider.System` (Zeilen 28-33) — etabliertes Muster für testbare Zeitauswertung, identisch in `SettingsViewModel` (Zeilen 60-71 dort).

## `KeywordMatcher`
Datei: `src/Reporter.Core/Services/KeywordMatcher.cs` — implementiert `IKeywordMatcher`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `MatchesAny(string? title, string? contentHtml, IEnumerable<string> keywords)` | public | Teilwort-Vergleich `OrdinalIgnoreCase` auf Titel und HTML-Inhalt; leere Keywords werden übersprungen (Zeilen 12-33) |

## `RetentionCleanupService`
Datei: `src/Reporter.Core/Services/RetentionCleanupService.cs` — implementiert `IRetentionCleanupService`

Referenznutzung des Keyword-Filters: `CleanupAsync` lädt Keywords via `IKeywordRepository.GetAllAsync`, bildet `keywordTexts` und ruft `_keywordMatcher.MatchesAny(i.Title, i.ContentHtml, keywordTexts)` pro Kandidat auf (Zeilen 46-57) — dieselbe Aufruf-Signatur, die der Benachrichtigungs-Service benötigt.

## `FeedsViewModel`
Datei: `src/Reporter.Core/ViewModels/FeedsViewModel.cs` — `BaseViewModel`

Abhängigkeiten: `IFeedRepository`, `ICategoryRepository`, `IFeedSyncService` (Zeilen 15-17, 34-45).

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `LoadCommand` / `SaveCommand` / `EditCommand` / `DeleteCommand` / `RefreshCommand` / `RefreshAllCommand` | public | `AsyncRelayCommand`s (Zeilen 50-75) |
| `NewUrl`, `NewTitle` | public | Formularfelder für Anlage/Bearbeitung (Zeilen 80-93). **Kein Property für Pro-Feed-Benachrichtigungen vorhanden.** |
| `ErrorMessage` / `HasError` | public | Validierungsanzeige (Zeilen 98-113) |
| `IsSyncing` | public | Sync-Status, steuert `CanExecute` der Refresh-Commands (Zeilen 118-129) |
| `SelectedFeed` / `SelectedCategory` / `Categories` / `Feeds` | public | Auswahl und Listen (Zeilen 134-165) |
| `SaveAsync` | private | Validierung (URL, Titel, Duplikat via `GetByUrlAsync`), dann `AddAsync` bzw. `UpdateAsync` mit neu konstruiertem `Feed` — ein neues Feed-Feld müsste in beiden Pfaden gesetzt werden (Zeilen 191-252) |
| `EditAsync` | private | Vorbefüllt `NewUrl`/`NewTitle`/`SelectedCategory` aus `FeedListItem` — hier würde der neue Schalter vorbefüllt (Zeilen 254-266) |
| `RefreshAsync` | private | `IFeedSyncService.SyncFeedAsync(feed.Id)` — manueller Einzel-Sync (Zeilen 288-316) |
| `RefreshAllAsync` | private | `IFeedSyncService.SyncAllAsync()` — manueller Gesamt-Sync (Zeilen 318-346) |

## `SettingsViewModel`
Datei: `src/Reporter.Core/ViewModels/SettingsViewModel.cs` — `BaseViewModel`

Für die Anforderung **unverändert nutzbar**: `NotificationsEnabled` (Zeilen 267-277), `QuietHoursEnabled` (reiner UI-Schalter ohne Persistenzspalte, Zeilen 286-305), `QuietHoursStart`/`QuietHoursEnd` (Zeilen 310-335) existieren. `PersistAsync` schreibt `QuietHoursEnabled ? QuietHoursStart : null` (Zeilen 494-535). Defaults `DefaultQuietHoursStart` 22:00 / `DefaultQuietHoursEnd` 07:00 (Zeilen 22-23). `TimeProvider` injizierbar (Zeilen 30, 60-71) für den Retention-Debounce.

## `App` (OnStart)
Datei: `src/Reporter/App.xaml.cs`

`OnStart` (Zeilen 32-74) führt nacheinander in eigenen try/catch-Blöcken aus: `Database.MigrateAsync()` (Zeile 38), `IRetentionCleanupService.CleanupAsync` (Zeilen 42-43), Theme-Anwendung via `IAppThemeService` (Zeilen 53-56), `IAutoRefreshService.StartAsync` (Zeilen 66-67). Möglicher Ankerpunkt für eine Berechtigungsanfrage / Service-Registrierung — derzeit kein Benachrichtigungs-Code.

## `AppThemeService`
Datei: `src/Reporter/Services/AppThemeService.cs` — implementiert `IAppThemeService`

Einzige vorhandene Plattformdienst-Implementierung unter `src/Reporter/Services/`; Muster für einen plattformabhängigen Service mit Interface in `Reporter.Core/Interfaces`.

## `AppDelegate` (iOS)
Datei: `src/Reporter/Platforms/iOS/AppDelegate.cs`

Minimal: `[Register("AppDelegate")]`, nur `CreateMauiApp()` überschrieben (Zeilen 8-16). **Kein `UNUserNotificationCenterDelegate`.** `Platforms/iOS/Info.plist` enthält keine Benachrichtigungs-Schlüssel. `Platforms/iOS/Resources/PrivacyInfo.xcprivacy` existiert.

## `MauiProgram`
Datei: `src/Reporter/MauiProgram.cs`

DI-Registrierung in `CreateMauiApp` (Zeilen 42-68): Repositories, `IFeedSyncService`, `IRetentionCleanupService`, `IKeywordMatcher`, `IAutoRefreshService`, `IAppThemeService` alle als Singleton; ViewModels Singleton, Pages Transient. Neue Services würden konsistent hier registriert.
