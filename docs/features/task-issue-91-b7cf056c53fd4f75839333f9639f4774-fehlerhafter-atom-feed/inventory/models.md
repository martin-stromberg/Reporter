<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell — Bestandsaufnahme

Domänenmodelle und Records, die der Sync-Pfad (`FeedSyncService`) liest bzw. schreibt. Die Persistenz-Entities in `src/Reporter.Data/Entities/` spiegeln `Feed`, `Item` und `SyncLog` nahezu 1:1 (mutable Properties, zusätzliche Navigationseigenschaften); die Repositories mappen zwischen beiden.

## `Feed`
Datei: `src/Reporter.Core/Models/Feed.cs` — Persistenz-Entity: `src/Reporter.Data/Entities/Feed.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige Feed-ID |
| `Url` | `string` (required, init) | Feed-URL; wird an `HttpClient.GetStreamAsync` übergeben |
| `Title` | `string` (required, init) | Anzeigetitel; kann Platzhalter (URL, Host, Dateiname) sein, der beim ersten Sync durch den Dokumenttitel ersetzt wird (`ResolveFeedTitle`) |
| `CategoryId` | `Guid?` | Optionale Kategorie |
| `LastCheckedAt` | `DateTime?` | Zeitpunkt des letzten Sync-Versuchs |
| `HealthStatus` | `string?` | Aktueller Health-Status (`FeedHealth.Ok`/`Warning`/`Error`) |
| `HealthLastChange` | `DateTime?` | Zeitpunkt des letzten Statuswechsels |
| `NotificationsEnabled` | `bool` (required) | Per-Feed-Benachrichtigungsschalter |
| `FaviconUrl` | `string?` | Gefundene Favicon-URL; beim ersten erfolgreichen Sync nachgetragen |
| `LastErrorKind` | `string?` | Kategorie des letzten Sync-Fehlers (`FeedSyncErrorKind`-Wert), `null` nach erfolgreichem Sync |
| `LastErrorMessage` | `string?` | Technische Fehlermeldung des letzten Syncs, `null` nach Erfolg |

## `Item`
Datei: `src/Reporter.Core/Models/Item.cs` — Persistenz-Entity: `src/Reporter.Data/Entities/Item.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige Item-ID |
| `FeedId` | `Guid` (required, init) | Zugehöriger Feed |
| `Title` | `string` (required, init) | Artikeltitel aus `SyndicationItem.Title?.Text` (Fallback: `string.Empty`) |
| `Link` | `string?` | Erster `SyndicationItem.Links`-Eintrag als URI-String |
| `PublishedAt` | `DateTime?` | `SyndicationItem.PublishDate.UtcDateTime`; `null`, wenn `PublishDate == DateTimeOffset.MinValue` — für Atom 0.3 ohne Datumsabbildung wäre dies immer `null` |
| `GuidOrHash` | `string` (required, init) | `SyndicationItem.Id` (≤ 500 Zeichen) oder SHA256-Hash aus `title\|link\|publishedAt`; Dedup-Schlüssel |
| `IsRead` | `bool` (required) | Gelesen-Flag; neue Items `false` |
| `IsSavedForLater` | `bool` (required) | Später-lesen-Flag; neue Items `false` |
| `ReadAt` | `DateTime?` | Zeitpunkt des Lesens (wird vom Sync nicht gesetzt) |
| `ContentHtml` | `string?` | `SyndicationItem.Content` oder `Summary` als `TextSyndicationContent.Text` (`GetContentHtml`) |

## `SyncLog`
Datei: `src/Reporter.Core/Models/SyncLog.cs` — Persistenz-Entity: `src/Reporter.Data/Entities/SyncLog.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Log-Eintrags-ID |
| `FeedId` | `Guid?` | Zugehöriger Feed |
| `StartedAt` | `DateTime?` | Sync-Start (`DateTime.UtcNow` beim Anlegen) |
| `FinishedAt` | `DateTime?` | Sync-Ende (via `UpdateLogAsync`) |
| `Status` | `string?` | Ergebnis-Status (`FeedHealth`-Wert) |
| `Message` | `string?` | Status-/Fehlermeldung (z. B. „Synchronized N items, M new, K filtered.") |

## `SyncResult` (Record)
Datei: `src/Reporter.Core/Services/SyncResult.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Status` | `string` | Ergebnis-Health-Status (`FeedHealth`-Wert) |
| `NewItems` | `int` | Anzahl neu gespeicherter Items |
| `Message` | `string?` | Optionale Status-/Fehlermeldung |

## `FeedHealthUpdate` (Record)
Datei: `src/Reporter.Core/Services/FeedHealthUpdate.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `ResolvedTitle` | `string?` | Aus dem Feed-Dokument aufgelöster Titel |
| `FaviconUrl` | `string?` | Während des Syncs gefundene Favicon-URL |
| `ErrorKind` | `string?` | Klassifizierte Fehlerkategorie oder `null` zum Zurücksetzen |
| `ErrorMessage` | `string?` | Technische Fehlermeldung oder `null` zum Zurücksetzen |
