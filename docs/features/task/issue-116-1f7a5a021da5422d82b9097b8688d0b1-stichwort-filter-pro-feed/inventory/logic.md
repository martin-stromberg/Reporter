<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Bestandsaufnahme: Logikklassen und Services

Betroffene Logikklassen der Anforderung „Stichwort-Filter pro Feed" (Issue #116).

## Keyword-Filter-Pipeline

### `KeywordFilter`
Datei: `src/Reporter.Core/Services/KeywordFilter.cs` — implementiert `IKeywordFilter`; DI: Singleton (`MauiProgram` Z. 102).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetKeywordTextsAsync()` | public | Lädt **alle** Stichworte über `IKeywordRepository.GetAllAsync()` und projiziert auf `KeywordText`. Keine Feed-Parametrisierung. Aufgerufen von `FeedSyncService.RunSyncAsync`, `NotificationService.NotifyNewItemsAsync`, `RetentionCleanupService.CleanupAsync`. |
| `MatchesAny(title, contentHtml, keywordTexts)` | public | Delegiert an `IKeywordMatcher.MatchesAny`; liefert `false` bei leerer Liste. |

### `KeywordMatcher`
Datei: `src/Reporter.Core/Services/KeywordMatcher.cs` — implementiert `IKeywordMatcher`; DI: Singleton (`MauiProgram` Z. 101).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `MatchesAny(title, contentHtml, keywords)` | public | Teilwort-Match (`string.Contains` mit `StringComparison.OrdinalIgnoreCase`) auf `Title` und `ContentHtml`; leere/Whitespace-Stichworte werden übersprungen. Fest verdrahtet, nicht konfigurierbar. |

## Sync-Pfad

### `FeedSyncService`
Datei: `src/Reporter.Core/Services/FeedSyncService.cs` — implementiert `IFeedSyncService`; DI: Singleton (`MauiProgram` Z. 95). Konstruktor-Abhängigkeiten u. a. `IKeywordFilter` (Z. 53).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(feedId, ct)` | public | Lädt `Feed` per `IFeedRepository.GetByIdAsync`, ruft `RunSyncAsync`, behandelt Fehler (Health, SyncLog, DebugLog). |
| `SyncAllAsync(ct)` | public | Iteriert alle Feeds (`_feedRepository.GetAllAsync`), ruft je Feed `SyncFeedAsync` unter `_syncAllLock`. |
| `RunSyncAsync(feed, log, ct)` | private | Z. 184: `var keywordTexts = await _keywordFilter.GetKeywordTextsAsync()` — **global**, ohne `feed.Id`. Reicht die Liste als `CollectContext.KeywordTexts` in `CollectNewItems` (Z. 188–190). |
| `CollectNewItems(context, ct)` | private | Z. 310: `_keywordFilter.MatchesAny(title, contentHtml, context.KeywordTexts)` — Treffer werden verworfen (`filteredCount++`), nicht gespeichert. Backfill-Pfade (Content/Bilder) umgehen die Filterung bewusst. |
| `UpdateFeedHealthAsync(feed, status, update)` | private | Rekonstruiert das `Feed`-Domain-Objekt **feldweise** (Z. 433–446) — ein Stichwort-Bezug am `Feed`-Modell müsste hier explizit mitgeführt werden. |
| `UpdateLogAsync(log, status, message)` | private | Persistiert Sync-Log inkl. `", N filtered"`-Suffix aus `CollectResult.FilteredCount` (Z. 217). |

Hilfs-Records: `CollectContext(Feed, FeedItems, ExistingItems, KeywordTexts, ImageItemIds)` (Z. 525), `CollectResult(NewItems, FilteredCount, ContentBackfill, ImageCandidates)` (Z. 541).

### `NotificationService`
Datei: `src/Reporter.Core\Services\NotificationService.cs` — implementiert `INotificationService`; DI: Singleton (`MauiProgram` Z. 105). Abhängigkeit `IKeywordFilter` (Z. 22).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `NotifyNewItemsAsync(feed, newItems, ct)` | public | Z. 50. Prüft `feed.NotificationsEnabled`, globale Einstellung und Ruhezeiten; Z. 70–73: lädt die **globale** Stichwortliste (`GetKeywordTextsAsync` ohne Feed-Parameter) und schließt Treffer aus den Benachrichtigungen aus. Der `Feed`-Parameter ist bereits vorhanden. |

### `RetentionCleanupService`
Datei: `src/Reporter.Core/Services/RetentionCleanupService.cs` — implementiert `IRetentionCleanupService`; DI: Singleton (`MauiProgram` Z. 96). Abhängigkeit `IKeywordFilter` (Z. 14).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CleanupAsync(ct)` | public | Z. 37. Löscht abgelaufene gelesene Items (`DeleteExpiredAsync`), dann Z. 54–67: lädt **globale** Stichwortliste und wendet `MatchesAny` auf `GetExpiredKeywordCandidatesAsync(cutoff)`-Treffer **feed-übergreifend** an (keine Gruppierung nach `Item.FeedId`). |
| `RemoveOrphanedContentAsync(ct)` | private | Räumt verwaiste Content-Einträge auf. |

## Repositories

### `KeywordRepository`
Datei: `src/Reporter.Data/Repositories/KeywordRepository.cs` — implementiert `IKeywordRepository`; DI: Singleton (`MauiProgram` Z. 86).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync()` | public | Alle Stichworte, sortiert nach `KeywordText`. Keine Feed-Filterung. |
| `GetByIdAsync(id)` | public | Einzelnes Stichwort oder `null`. |
| `AddAsync(keyword)` | public | Fügt Entität ein (`MapToEntity`). |
| `UpdateAsync(keyword)` | public | Aktualisiert nur `KeywordText`. |
| `DeleteAsync(id)` | public | Löscht per `FindAsync`/`Remove`. |
| `MapToModel` / `MapToEntity` | private static | Mapping zwischen `Reporter.Data.Entities.Keyword` und `Reporter.Core.Models.Keyword`. |

### `FeedRepository`
Datei: `src/Reporter.Data/Repositories/FeedRepository.cs` — implementiert `IFeedRepository`; DI: Singleton (`MauiProgram` Z. 81).

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync()` | public | Alle Feeds sortiert nach Titel. |
| `GetByIdAsync(id)` | public | Einzelfeed oder `null`. |
| `AddAsync(feed)` | public | Fügt Feed ein. |
| `UpdateAsync(feed)` | public | Feldweises Update inkl. `NotificationsEnabled` (Z. 74). |
| `DeleteAsync(id)` | public | Z. 82–101: entfernt Feed; `items`-Zeilen via DB-Kaskade (`DeleteBehavior.Cascade`), Item-Contents explizit über `_contentStore.DeleteRangeAsync(itemIds)`. **Keine Stichwort-Behandlung** vorhanden. |
| `GetAllWithDetailsAsync()` | public | Projiziert `FeedListItem` inkl. `NotificationsEnabled`, `UnreadCount`, `CategoryName`. |
| `GetByUrlAsync(url)` | public | Feed-Lookup per URL (Duplikatprüfung im Edit-Sheet). |

### `ItemRepository` (relevanter Ausschnitt)
Datei: `src/Reporter.Data/Repositories/ItemRepository.cs` — implementiert `IItemRepository`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetExpiredKeywordCandidatesAsync(cutoff, ct)` | public | Z. 341–350: liefert gelesene, nicht gespeicherte Items mit `(PublishedAt ?? ReadAt) < cutoff` **aller Feeds**, inkl. gemergtem `ContentHtml`. Rückgabe enthält `Item.FeedId`. |
| `DeleteExpiredAsync(cutoff, ct)` | public | Löscht abgelaufene Items. |

## ViewModels

### `SettingsViewModel` (Keyword-Verwaltung, globales Muster)
Datei: `src/Reporter.Core/ViewModels/SettingsViewModel.cs` — `ObservableObject`; DI: Singleton (`MauiProgram` Z. 119). Abhängigkeit `IKeywordRepository` (Z. 31).

| Element | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `MaxKeywordLength = 500` | private const | Längenlimit (Z. 22), deckt sich mit `keyword_text` max. 500. |
| `Keywords` | public `ObservableCollection<Keyword>` | Geladene globale Stichworte (Z. 175), befüllt in `LoadAsync` (Z. 641–646) via `_keywordRepository.GetAllAsync()`. |
| `NewKeywordText` | public | Eingabefeld-Binding; setzt `HasError` zurück (Z. 223–233). |
| `AddKeywordCommand` / `AddKeywordAsync()` | public / private | Z. 840–876: Trim → Leer-Prüfung (`ErrorKeywordEmpty`) → Längenprüfung (`ErrorKeywordTooLong`) → case-insensitive Duplikatprüfung gegen `Keywords` (`ErrorKeywordDuplicate`, `OrdinalIgnoreCase`) → `new Keyword { Id = Guid.NewGuid(), KeywordText = text }` → `_keywordRepository.AddAsync` → Collection-Update. |
| `RemoveKeywordCommand` / `RemoveKeywordAsync(keyword)` | public / private | Z. 878–894: `_keywordRepository.DeleteAsync(keyword.Id)` + Collection-Remove. |
| `HasError` / `ErrorMessage` | public | Validierungsanzeige für das Keyword-Eingabefeld. |

### `FeedDetailViewModel` (Ziel-Ort der feed-spezifischen Pflege)
Datei: `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs` — `BaseViewModel`; DI: Transient (`MauiProgram` Z. 122). Abhängigkeiten: `IItemRepository`, `IFeedRepository`, `IFeedSyncService`, `ICategoryRepository`, `INetworkStatusService`, optional `ILocalNotificationService` — **kein** `IKeywordRepository`/`IKeywordFilter`.

| Element | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `EditCommand` / `Edit()` | public / private | Öffnet das Bearbeiten-Sheet: setzt `EditUrl`, `EditNotificationsEnabled`, `ShowEditForm = true` (Z. 628–639). |
| `SaveEditCommand` / `SaveEditAsync()` | public / private | Z. 641–681: URL-Trim, `FeedUrlValidator.IsValidFeedUrl`, Duplikatprüfung via `GetByUrlAsync`, persistiert via `ToFeed(...)` + `_feedRepository.UpdateAsync`. |
| `CloseEditFormCommand` / `ResetEditForm()` | public / private | Schließt das Sheet und setzt `EditUrl`/`EditNotificationsEnabled` zurück (Z. 683–689). |
| `EditUrl`, `EditNotificationsEnabled`, `ShowEditForm` | public | Sheet-Zustand. |
| `NotificationsSupported` | public | Plattform-Fähigkeit für den Notifications-Switch. |
| `DeleteFeedAsync()` | public | `_feedRepository.DeleteAsync(Feed.Id)` + Navigation zurück (Z. 406–415). |
| `ToFeed(feed, url, title, categoryId, notificationsEnabled)` | private static | Feldweiser Neuaufbau des `Feed`-Modells für Partial-Updates (Z. 730–746). |
| `LoadAsync(feedId)` | public | Lädt Feed via `GetAllWithDetailsAsync`, Kategorien, erste Artikelseite. |

**Keine Keyword-Verwaltung im `FeedDetailViewModel` vorhanden** (keine `Keywords`-Collection, kein Add/Remove-Command).

## DI-Registrierung

### `MauiProgram`
Datei: `src/Reporter/MauiProgram.cs`

| Zeile | Registrierung |
|-------|---------------|
| 81–89 | Repositories als Singletons: `IFeedRepository→FeedRepository`, `IKeywordRepository→KeywordRepository` u. a. |
| 95 | `IFeedSyncService→FeedSyncService` (Singleton) |
| 96 | `IRetentionCleanupService→RetentionCleanupService` (Singleton) |
| 101–102 | `IKeywordMatcher→KeywordMatcher`, `IKeywordFilter→KeywordFilter` (Singletons) |
| 105 | `INotificationService→NotificationService` (Singleton) |
| 119 | `SettingsViewModel` (Singleton) |
| 122–123 | `FeedDetailViewModel`, `FeedDetailPage` (Transient) |

## Shell-Routing
Datei: `src/Reporter/AppShell.xaml.cs` Z. 23: `Routing.RegisterRoute("feeddetail", typeof(FeedDetailPage))`; Aufruf als `feeddetail?feedId={id}` — `FeedDetailPage.ApplyQueryAttributes` liest `feedId` (`FeedDetailPage.xaml.cs` Z. 34–50).

## Events
- `FeedDetailViewModel` publiziert keine eigenen Events; `FeedDetailPage` abonniert `PropertyChanged` (`ShowEditForm` → Fokus auf `EditUrlEntry`, `FeedDetailPage.xaml.cs` Z. 250–256) und setzt `NavigateBackAsync`.
- `SettingsViewModel` publiziert `NotificationAuthorizationDenied`, `DebugReportFailed` (nicht keyword-bezogen).
- Keine Event-Aggregator-/Messaging-Mechanismen im Keyword-Pfad.
