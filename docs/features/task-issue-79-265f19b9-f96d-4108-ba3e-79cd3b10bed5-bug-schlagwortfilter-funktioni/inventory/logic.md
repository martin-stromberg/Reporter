<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik — Bestandsaufnahme

Betroffene Logikklassen für den Schlagwortfilter. Kernaussage der Bestandsaufnahme: Die `keywords`-Liste wird aktuell nur in `NotificationService` (Benachrichtigungs-Unterdrückung) und `RetentionCleanupService` (fristbasierte Löschung) ausgewertet — **nicht** im Einspeicherungspfad `FeedSyncService`.

## `FeedSyncService`

Datei: `src/Reporter.Core/Services/FeedSyncService.cs`

Konstruktor-Abhängigkeiten (Zeilen 35–49): `IFeedRepository`, `IItemRepository`, `ISyncLogRepository`, `HttpClient`, `INotificationService`, `INetworkStatusService`. **Kein** `IKeywordRepository`, **kein** `IKeywordMatcher` — der Keyword-Filter greift hier nicht.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(Guid feedId, CancellationToken)` | `public` | Offline-Frühabbruch, `SyncLog` anlegen, Feed laden, `RunSyncAsync` im `try/catch`; Fehler → `FeedHealth.Error` + Log. |
| `SyncAllAsync(CancellationToken)` | `public` | Iteriert alle Feeds, ruft pro Feed `SyncFeedAsync`, aggregiert Status/NewItems/Messages. |
| `RunSyncAsync(Feed, SyncLog, CancellationToken)` | `private` | Kernlogik (Zeilen 128–215): lädt `existingItems` via `_itemRepository.GetByFeedAsync`, parst Feed via `SyndicationFeed.Load`, dedupliziert über `knownKeys` (`GuidOrHash`), baut `newItemEntities` **ohne Keyword-Prüfung**, speichert per `_itemRepository.AddRangeAsync`, aktualisiert Health/Log, ruft `_notificationService.NotifyNewItemsAsync` für alle neuen Items (fehlerisoliert). |
| `IsHostPlaceholderTitle(Feed)` | `private static` | Erkennt URL-Host als Platzhaltertitel. |
| `DetermineStatus(int, int, int, DateTime)` | `private static` | `Warning` bei < 50 % Abrufmenge oder > 30 Tage ohne neue Items, sonst `Ok`. |
| `UpdateFeedHealthAsync(Feed, string, string?)` | `private` | Persistiert `HealthStatus`/`HealthLastChange`/`LastCheckedAt` und ggf. aufgelösten Titel. |
| `UpdateLogAsync(SyncLog, string, string?)` | `private` | Persistiert `FinishedAt`/`Status`/`Message` im `SyncLog`. |
| `GetContentHtml(SyndicationItem)` | `private static` | Liefert `Content.Text` (TextSyndicationContent) oder `Summary.Text` als HTML. |
| `NormalizeGuidOrHash(SyndicationItem, string?, DateTime?)` | `private static` | Item-`Id` (≤ 500 Zeichen) oder SHA-256-Hash aus `title|link|publishedAt` als `GuidOrHash`. |

Abonnierte Events: keine. Publizierte Events: keine.
Aufgerufen von: `FeedsViewModel` (`SyncFeedAsync`/`SyncAllAsync`), `UnreadViewModel` (`SyncAllAsync`, Zeile 357), `AutoRefreshService` (`SyncAllAsync`, Zeile 128), `App` (indirekt über DI-Singleton).

## `KeywordMatcher`

Datei: `src/Reporter.Core/Services/KeywordMatcher.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `MatchesAny(string? title, string? contentHtml, IEnumerable<string> keywords)` | `public` | Teilwort-Match per `Contains` mit `StringComparison.OrdinalIgnoreCase` auf `title` und `contentHtml`; leere/Whitespace-Keywords werden übersprungen; `Link` wird nicht geprüft. |

Verwendet von: `NotificationService.NotifyNewItemsAsync` (Zeile 71), `RetentionCleanupService.CleanupAsync` (Zeile 57). Registriert als Singleton `IKeywordMatcher → KeywordMatcher` in `MauiProgram` (Zeile 58).

## `RetentionCleanupService`

Datei: `src/Reporter.Core/Services/RetentionCleanupService.cs`

Konstruktor-Abhängigkeiten (Zeilen 24–34): `ISettingsRepository`, `IItemRepository`, `IKeywordRepository`, `IKeywordMatcher`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CleanupAsync(CancellationToken)` | `public` | Zweistufig (Zeilen 37–67): (1) `_itemRepository.DeleteExpiredAsync(cutoff)` — gelesene, nicht gemerkte Items mit `(ReadAt ?? PublishedAt) < cutoff`; (2) Keyword-Regel — `_keywordRepository.GetAllAsync`, bei leerer Liste Ende; `GetExpiredKeywordCandidatesAsync(cutoff)` (`IsRead && !IsSavedForLater && (PublishedAt ?? ReadAt) < cutoff`), In-Memory-Match via `_keywordMatcher.MatchesAny`, Löschung per `DeleteRangeAsync`. Rückgabe: Gesamtzahl gelöschter Items. |

Aufgerufen von: `App.OnStart` (Zeile 44–45) — einmalig beim App-Start, fehlerisoliert.

## `NotificationService`

Datei: `src/Reporter.Core/Services/NotificationService.cs`

Konstruktor-Abhängigkeiten (Zeilen 35–47): `ISettingsRepository`, `IKeywordRepository`, `IKeywordMatcher`, `ILocalNotificationService`, optional `TimeProvider`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `NotifyNewItemsAsync(Feed, IReadOnlyList<Item>, CancellationToken)` | `public` | Entscheidungskette (Zeilen 50–92): `feed.NotificationsEnabled` → leere Liste → globales `settings.NotificationsEnabled` → Ruhezeit → Keyword-Filter (`_keywordRepository.GetAllAsync` + `_keywordMatcher.MatchesAny`, Treffer werden aus `candidates` entfernt) → leere Restliste → Summary- oder Einzel-Modus via `_localNotificationService.ShowAsync`. |
| `BuildItemUserInfo(Item)` | `private static` | `userInfo` mit `itemId` und ggf. `link`. |
| `IsQuietHoursActive(Settings)` | `private` | Ruhezeiten-Prüfung inkl. Mitternachts-Wrap-around. |
| `BuildSummaryIdentifier(Guid, IReadOnlyList<Item>)` | `private static` | Stabiler Summary-Identifier (`feedId`-SHA-256 über sortierte Item-IDs). |
| `Truncate(string, int)` | `private static` | Kürzt auf 160 Zeichen mit `…`. |

Aufgerufen von: `FeedSyncService.RunSyncAsync` (Zeile 205).

## `AutoRefreshService`

Datei: `src/Reporter.Core/Services/AutoRefreshService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `StartAsync(CancellationToken)` | `public` | Liest Settings, startet Refresh-Loop. |
| `ApplySettingsAsync(Settings)` | `public` | Stoppt und startet den Loop gemäß `AutoRefreshEnabled`/`RefreshIntervalMinutes` (Clamp 1–1440). |
| `StopAsync()` | `public` | Stoppt den Loop. |
| `StopLoopAsync()` | `private` | Cancel + Await + Dispose des Loop-CTS. |
| `RunLoopAsync(TimeSpan, CancellationToken)` | `private` | `PeriodicTimer`-Loop; bei Offline Skip; ruft `_feedSyncService.SyncAllAsync` (fehlerisoliert). |

## `SettingsViewModel` (Keyword-relevante Member)

Datei: `src/Reporter.Core/ViewModels/SettingsViewModel.cs`

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `Keywords` | `public` (Property) | `ObservableCollection<Keyword>` — Chips-Quelle der `SettingsPage`. |
| `NewKeywordText` | `public` (Property) | Eingabefeld-Text; setzt `HasError` zurück. |
| `AddKeywordCommand` → `AddKeywordAsync()` | `public` / `private` | Validierung (leer → `ErrorKeywordEmpty`; > 500 → `ErrorKeywordTooLong`; Dublette `OrdinalIgnoreCase` → `ErrorKeywordDuplicate`), dann `_keywordRepository.AddAsync` + `Keywords.Add` (Zeilen 713–749). |
| `RemoveKeywordCommand` → `RemoveKeywordAsync(Keyword?)` | `public` / `private` | `_keywordRepository.DeleteAsync(keyword.Id)` + `Keywords.Remove` (Zeilen 751–767). |
| `LoadAsync()` | `private` | Lädt Settings und Keyword-Liste via `_keywordRepository.GetAllAsync` in `Keywords` (Zeilen 517–522). |

Abhängigkeiten: `ISettingsRepository`, `IKeywordRepository`, `IAutoRefreshService`, `IAppThemeService`, optional `ILocalNotificationService`, `TimeProvider`.

## Listensicht-ViewModels

- `UnreadViewModel` (`src/Reporter.Core/ViewModels/UnreadViewModel.cs`): lädt via `IItemRepository.GetUnreadByDateAsync(page, PageSize, categoryId)` (Zeile 304) und `GetUnreadCountAsync` — ungefiltert, jeder gespeicherte ungelesene Artikel erscheint.
- `LaterViewModel` (`src/Reporter.Core/ViewModels/LaterViewModel.cs`): lädt via `IItemRepository.GetSavedForLaterAsync(_currentPage, PageSize)` (Zeile 175).
- `CategoriesViewModel` (`src/Reporter.Core/ViewModels/CategoriesViewModel.cs`): nur `ICategoryRepository` — keine Item-Abfrage; Kategoriegefilterte Listen entstehen über `UnreadViewModel` mit `categoryId`.

## `App`

Datei: `src/Reporter/App.xaml.cs`

`OnStart` (Zeilen 34–87): `MigrateAsync`, dann drei fehlerisolierte Blöcke — `IRetentionCleanupService.CleanupAsync` (einziger Auslöser der Keyword-Löschregel), Theme-Anwendung, `INetworkStatusService`-Auflösung, `IAutoRefreshService.StartAsync`.

## `MauiProgram` (DI-Registrierung)

Datei: `src/Reporter/MauiProgram.cs` (Zeilen 46–76)

Alle Services als Singletons; relevant: `IKeywordRepository → KeywordRepository` (51), `IFeedSyncService → FeedSyncService` (56), `IRetentionCleanupService → RetentionCleanupService` (57), `IKeywordMatcher → KeywordMatcher` (58), `INotificationService → NotificationService` (61). `IKeywordRepository` und `IKeywordMatcher` sind damit bereits für die Auflösung eines erweiterten `FeedSyncService`-Konstruktors verfügbar.
