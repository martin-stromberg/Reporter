<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Logik — Bestandsaufnahme

Betroffene Logikklassen für die Anforderung „Ausgabe der Warnung" (Issue #126). Der zentrale Befund: `FeedSyncService.DetermineStatus` entscheidet die zwei Warnungsauslöser, verwirft aber die konkrete Ursache — nur der Statusstring wird zurückgegeben.

## `FeedSyncService`

Datei: `src/Reporter.Core/Services/FeedSyncService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SyncFeedAsync(Guid, CancellationToken)` | `public` | Sync eines Feeds: Offline-Early-Return, `SyncLog` anlegen, `RunSyncAsync` aufrufen, Exception-Catch → `FeedSyncErrorKind.Classify` + `UpdateFeedHealthAsync(Error)` + `UpdateLogAsync(Error)` + Debug-Log. |
| `SyncAllAsync(CancellationToken)` | `public` | Alle Feeds sequenziell (Semaphore `_syncAllLock`), aggregiert `SyncResult`-Status (`Error` > `Warning` > `Ok`). |
| `RunSyncAsync(Feed, SyncLog, CancellationToken)` | `private` | Kern-Sync: Bestand laden (`GetByFeedAsync`), Feed laden/parsen, `CollectNewItems`, Bilder laden, `DetermineStatus` aufrufen, Nachricht bauen (bei `Warning`: Suffix `" Health warning triggered."`), `UpdateFeedHealthAsync`, `UpdateLogAsync`, Benachrichtigungen. Zeilen 166–251. |
| `DetermineStatus(int newItems, int fetchedCount, int existingCount, DateTime lastPublishedAt)` | `private static` | Liefert nur den Statusstring: `Warning` bei `fetchedCount < existingCount * 0.5 && existingCount > 0` (Zeilen 411–414) oder `newItems == 0 && lastPublishedAt != default && lastPublishedAt < now − 30 d` (Zeilen 416–420), sonst `Ok`. Die konkrete Ursache geht verloren. Zeilen 409–423. |
| `UpdateFeedHealthAsync(Feed, string, FeedHealthUpdate)` | `private` | Schreibt `LastCheckedAt`, `HealthStatus`, `HealthLastChange` (nur bei Statuswechsel via `FeedHealth.Changed`), `FaviconUrl`, `LastErrorKind = update.ErrorKind`, `LastErrorMessage = update.ErrorMessage` per `IFeedRepository.UpdateAsync`. Zeilen 425–447. |
| `CollectNewItems(CollectContext, CancellationToken)` | `private` | Dedup per `GuidOrHash`, Keyword-Filter, Content-/Bild-Backfill-Kandidaten. |
| `DownloadImagesAsync(...)` | `private` | Sequenzielle Bild-Downloads; Fehler nur Debug-Log (Zeilen 197–203), kein Einfluss auf den Status. |
| `ResolveFeedTitle(Feed, SyndicationFeed)` | `private static` | Ersetzt Platzhaltertitel durch Dokumenttitel. |
| `IsHostPlaceholderTitle(Feed)` | `private static` | Prüft Host-Platzhaltertitel. |
| `UpdateLogAsync(SyncLog, string, string?)` | `private` | Schreibt Status + Meldung in den `SyncLog` (Zeilen 462–473). |
| `GetContentHtml(SyndicationItem)` | `private static` | Extrahiert HTML-Inhalt. |
| `NormalizeGuidOrHash(SyndicationItem, string?, DateTime?)` | `private static` | Stabiler Item-Schlüssel (GUID oder SHA256-Hash). |
| `PrepareFeedReader(XmlReader)` | `private static` | Atom-0.3-Normalisierung. |
| `TryFindFaviconUrlAsync(string, SyndicationFeed, CancellationToken)` | `private` | Favicon-Backfill via `IFeedIconService` (isoliert). |

Abonnierte Events: keine. Publizierte Events: keine.
Records: `CollectContext`, `CollectResult` (private, Zeilen 525–545).

## `FeedRepository`

Datei: `src/Reporter.Data/Repositories/FeedRepository.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `GetAllAsync()` | `public` | Alle Feeds, sortiert nach Titel (`MapToModel`). |
| `GetByIdAsync(Guid)` | `public` | Feed per ID (`MapToModel`). |
| `AddAsync(Feed)` | `public` | Insert via `MapToEntity`. |
| `UpdateAsync(Feed)` | `public` | Schreibt u. a. `HealthStatus`, `HealthLastChange`, `LastErrorKind`, `LastErrorMessage` (Zeilen 68–78); No-Op bei unbekannter ID. |
| `DeleteAsync(Guid)` | `public` | Löscht Feed; Items per DB-Kaskade, Inhalte explizit via `IItemContentStore.DeleteRangeAsync`. |
| `GetAllWithDetailsAsync()` | `public` | Projektion auf `FeedListItem` inkl. `CategoryName`, `UnreadCount` und den Fehlerfeldern `LastErrorKind`/`LastErrorMessage` (Zeilen 104–131). |
| `GetByUrlAsync(string)` | `public` | Feed per URL (`MapToModel`). |
| `MapToModel(FeedEntity)` | `private static` | Entity → Domänenmodell, inkl. `LastErrorKind`/`LastErrorMessage` (Zeilen 143–159). |
| `MapToEntity(Feed)` | `private static` | Domänenmodell → Entity, inkl. `LastErrorKind`/`LastErrorMessage` (Zeilen 161–177). |

## `FeedDetailViewModel`

Datei: `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync(Guid)` | `public` | Lädt Kategorien, Feed via `GetAllWithDetailsAsync` (→ `FeedListItem` mit Fehlerfeldern), Feed-Keywords, erste Artikelseite. |
| `GetFeedErrorMessage(FeedListItem)` | `public` | Mappt `LastErrorKind` auf `AppResources.FeedErrorKind*` (Fallback `FeedErrorKindUnknown`) und hängt `LastErrorMessage` als zweiten Absatz an. Zeilen 483–505. **Es gibt kein `GetFeedWarningMessage`-Pendant.** |
| `MakeUniqueOptionLabels(IReadOnlyList<string>)` | `public static` | Kollisionsfreie Aktionsblatt-/Picker-Labels. |
| `RenameFeedAsync(FeedListItem?, string?)` | `public` | `UpdateAsync` via `ToFeed` — übernimmt `LastError*` aus `FeedListItem`. |
| `ChangeFeedCategoryAsync(FeedListItem?, Category?)` | `public` | `UpdateAsync` via `ToFeed`. |
| `SaveEditAsync` (`SaveEditCommand`) | `private` | Edit-Sheet speichern via `ToFeed`. |
| `ToFeed(FeedListItem, url, title, categoryId, notificationsEnabled)` | `private static` | Baut `Feed` für Teil-Updates; übernimmt `HealthStatus`, `HealthLastChange`, `LastErrorKind`, `LastErrorMessage` unverändert (Zeilen 850–866). Neue Feed-Felder müssten hier mitgeführt werden, sonst gehen sie bei Rename/Kategorie/Edit verloren. |
| `RefreshAsync` | `private` | `RunFeedSyncAsync(() => _feedSyncService.SyncFeedAsync(_feedId))` + `ReloadFeedAsync` + Listenneustart (Zeilen 677–684). |
| `ReloadFeedAsync` | `private` | Lädt `Feed` erneut über `GetAllWithDetailsAsync`. |
| `OnConnectivityChanged(bool)` | `protected override` | Leert `SyncErrorMessage`. |
| `SetIsSyncing(bool)` / `ReportSyncError(string)` | `protected override` | Hooks für `BaseViewModel.RunFeedSyncAsync`. |

## `FeedDetailPage` (Code-Behind)

Datei: `src/Reporter/Views/FeedDetailPage.xaml.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `OnFeedActionsClicked(object?, EventArgs)` | `private async void` | Baut das Aktionsblatt: `ButtonRefresh`, `ButtonRename`, `ButtonChangeCategory`, `ButtonEdit`, **`ButtonShowErrorDetails` nur bei `HealthStatus == FeedHealth.Error`** (Zeilen 116–119), `ButtonDelete`; dispatcht die Auswahl. Kein Eintrag bei `FeedHealth.Warning`. |
| `ShowFeedErrorDetailsAsync(FeedListItem)` | `private` | `DisplayAlertAsync` mit Titel `FeedErrorDetailsTitle`, Body `ViewModel.GetFeedErrorMessage(feed)` (Zeilen 242–248). |
| `RenameFeedAsync`, `ChangeCategoryAsync`, `ConfirmDeleteFeedAsync` | `private` | Prompt/Picker/Confirm-Dialoge. |
| `ApplyQueryAttributes`, `OnAppearing`/`OnDisappearing`, `OnBackButtonPressed`, `OnViewModelPropertyChanged` | `public`/`protected`/`private` | Navigation, Connectivity-Tracking, Edit-Sheet-Verhalten. |

## `BaseViewModel`

Datei: `src/Reporter.Core\ViewModels\BaseViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `RunFeedSyncAsync(Func<Task<SyncResult>>)` | `protected` | Einmal-Sync mit `_isSyncInProgress`-Guard und Offline-Early-Return; meldet `SyncStatusError` nur bei `Status == FeedHealth.Error` oder Exception — `FeedHealth.Warning` löst bewusst keine Fehlerzeile aus (Zeilen 147–188). |
| `TrackConnectivity`/`UntrackConnectivity`/`RefreshConnectivityStatus` | `protected` | Connectivity-Tracking via `INetworkStatusService.ConnectivityChanged`. |
| `OnConnectivityChanged`/`SetIsSyncing`/`ReportSyncError` | `protected virtual` | Hooks für abgeleitete ViewModels. |
| `LoadCategoriesWithNoneAsync` | `protected static` | Kategorien inkl. Pseudo-Eintrag. |

Abonnierte Events: `INetworkStatusService.ConnectivityChanged` → `HandleConnectivityChanged`.

## Weitere berührte Klassen

- `FeedsViewModel` (`src/Reporter.Core/ViewModels/FeedsViewModel.cs`, Zeilen 219–235): `SyncAsync(Func<Task<SyncResult>>)` ruft `RunFeedSyncAsync` mit `SyncAllAsync` — nur Status-Aggregation, keine Warnungsdetails.
- `FeedsViewModel.Search` (`FeedsViewModel.Search.cs`, Zeilen 290–318): `TryPersistNewFeedAsync` legt neue Feeds mit `HealthStatus = FeedHealth.Ok` an (keine Fehlerfelder gesetzt).
- `UnreadViewModel.RefreshAsync` (`UnreadViewModel.cs`, Zeilen 344–388): `SyncAllAsync`; `SyncStatusError` nur bei `FeedHealth.Error`.
- `ScheduledSyncRunner.RunAsync` (`src/Reporter.Core/Services/ScheduledSyncRunner.cs`, Zeilen 35–65): `success = result.Status != FeedHealth.Error` — `Warning` zählt als Erfolg.
- `DemoContentService` (`src/Reporter.Core/Services/DemoContentService.cs`, Zeilen 79–92): Seed-Feed wird mit `HealthStatus = FeedHealth.Ok`, `LastErrorKind = null`, `LastErrorMessage = null` angelegt — neue Pflichtfelder müssten hier gesetzt werden.
- `DebugReportService.BuildBody` (`src/Reporter.Core/Services/DebugReportService.cs`, Zeilen 181–185): Abschnitt „Feed health" listet `Title | Url | Status | LastCheckedAt | HealthLastChange` — weder `LastErrorKind`/`LastErrorMessage` noch ein Warnungsgrund werden ausgegeben.
- `ReporterDbContext.ConfigureFeed` (`src/Reporter.Data/ReporterDbContext.cs`, Zeilen 84–102): Spalten-Mapping; Muster für neue Spalten (`last_error_kind` TEXT max 50, `last_error_message` TEXT).
- Migration `AddFeedLastError` (`src/Reporter.Data/Migrations/20260914183510_AddFeedLastError.cs`): Referenzmuster für eine neue `AddFeedLastWarning`-Migration (`AddColumn`/`DropColumn` auf `feeds`).
