<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Datenmodell — Bestandsaufnahme

Betroffene Modellklassen für die Anforderung (Hinzufügen-Formular, Dateiname als Titel-Fallback, Kontextmenü „Umbenennen" / „Kategorie ändern").

## `Feed`
Datei: `src/Reporter.Core/Models/Feed.cs`

Domänenmodell eines Feeds. `Title` und `CategoryId` sind die für „Umbenennen" bzw. „Kategorie ändern" relevanten Eigenschaften.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Eindeutige Feed-ID |
| `Url` | `string` (required, init) | Feed-URL; Grundlage für den Dateinamen-Titel-Fallback (letztes Pfadsegment) |
| `Title` | `string` (required, init) | Anzeigetitel; Ziel der „Umbenennen"-Aktion und der Platzhalter-Auflösung beim Sync |
| `CategoryId` | `Guid?` (init) | Optionale Kategorie; Ziel der „Kategorie ändern"-Aktion; `Guid.Empty`-Pseudo-Eintrag im ViewModel wird zu `null` |
| `LastCheckedAt` | `DateTime?` (init) | Zeitpunkt des letzten Sync |
| `HealthStatus` | `string?` (init) | `FeedHealth.Ok`/`Warning`/`Error` |
| `HealthLastChange` | `DateTime?` (init) | Zeitpunkt der letzten Statusänderung |
| `NotificationsEnabled` | `bool` (required, init) | Pro-Feed-Benachrichtigungsflag; bisher über Formular-Switch `FeedNotificationsEnabled` gesetzt |

Persistenz-Gegenstück: `src/Reporter.Data/Entities/Feed.cs` (gleiche Felder plus Navigation `Category`; `NotificationsEnabled` Default `true`). Kein Schema-Eingriff nötig — alle benötigten Spalten existieren.

## `FeedListItem`
Datei: `src/Reporter.Core/Models/FeedListItem.cs`

Anzeigemodell der Feed-Liste; Parameter-Typ der Kontextmenü-Commands (`EditCommand`, `DeleteCommand`, `RefreshCommand`, `SubscribeResultCommand`-Pendant).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Feed-ID |
| `Title` | `string` (required, init) | Anzeigetitel; Vorbelegung für den „Umbenennen"-Dialog |
| `Url` | `string` (required, init) | Feed-URL |
| `CategoryId` | `Guid?` (init) | Aktuelle Kategorie; Vorbelegung für „Kategorie ändern" |
| `CategoryName` | `string?` (init) | Kategoriename zur Anzeige (`null` → `—` via `TargetNullValue`) |
| `LastCheckedAt` | `DateTime?` (init) | Letzter Sync-Zeitpunkt |
| `HealthStatus` | `string?` (init) | Steuert Status-Punkt und Status-Label im Listeneintrag (DataTrigger auf `OK`/`Warning`/`Error`) |
| `HealthLastChange` | `DateTime?` (init) | Letzte Statusänderung |
| `UnreadCount` | `int` (required, init) | Ungelesene Artikel |
| `NotificationsEnabled` | `bool` (required, init) | Wird von `EditAsync` ins Formular geladen |

## `Category`
Datei: `src/Reporter.Core/Models/Category.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` (required, init) | Kategorie-ID; `Guid.Empty` = Pseudo-Eintrag „Keine" (`AppResources.CategoryNone`), wird in `LoadAsync` als erstes Element eingefügt und beim Speichern zu `CategoryId = null` aufgelöst |
| `Name` | `string` (required, init) | Anzeigename |

## `FeedSearchResult`
Datei: `src/Reporter.Core/Models/FeedSearchResult.cs`

Reines In-Memory-Modell eines Suchtreffers; wird nicht persistiert.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Title` | `string?` (init) | Treffer-Titel; aktueller Fallback bei leerem Titel in `SubscribeResultAsync` ist `FeedUrl` (soll Dateiname werden) |
| `Description` | `string?` (init) | Nur Anzeige in der Trefferkarte |
| `SiteName` | `string?` (init) | Name der Website |
| `SiteUrl` | `string?` (init) | URL der Website |
| `FeedUrl` | `string` (required, init) | Feed-Dokument-URL; Basis für Dateinamen-Fallback |
| `Score` | `double` (init) | Relevanz des Verzeichnisses; Autodiscovery-Treffer `0` |
| `MatchKind` | `FeedSearchMatchKind` (init) | Trefferart, siehe [enums.md](enums.md) |

## `SyncResult`
Datei: `src/Reporter.Core/Services/SyncResult.cs`

Record `SyncResult(string Status, int NewItems, string? Message = null)` — Rückgabewert von `IFeedSyncService`; `Status` ist ein `FeedHealth`-Wert.

## `CategoryWithCount`
Datei: `src/Reporter.Core/Models/CategoryWithCount.cs`

`Id`, `Name`, `FeedCount` — wird von `ICategoryRepository.GetAllWithFeedCountAsync` geliefert (Kategorien-Seite), für die Feed-Liste selbst nicht direkt relevant, gehört aber zum `ICategoryRepository`-Contract.
