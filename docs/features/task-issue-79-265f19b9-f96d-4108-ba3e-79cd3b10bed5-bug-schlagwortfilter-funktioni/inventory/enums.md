<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Statuswerte — Bestandsaufnahme

Im betroffenen Bereich gibt es keinen echten `enum`; die Sync-Statuswerte sind als Konstanten einer statischen Klasse definiert.

## `FeedHealth` (statische Konstantenklasse, kein Enum)

Datei: `src/Reporter.Core/Services/FeedHealth.cs`

| Wert | Bedeutung |
|------|-----------|
| `Ok = "OK"` | Feed synchronisiert erfolgreich. |
| `Warning = "Warning"` | Warnung — z. B. deutlich weniger Items abgerufen als gespeichert oder > 30 Tage keine neuen Items (`FeedSyncService.DetermineStatus`). |
| `Error = "Error"` | Synchronisation fehlgeschlagen (offline, Feed nicht gefunden, HTTP-/Parse-Fehler). |

Hilfsmethode: `Changed(string? current, string? next)` — Vergleich für `HealthLastChange`-Pflege.

Verwendung: `SyncResult.Status`, `SyncLog.Status`, `Feed.HealthStatus`; gesetzt/aggregiert in `FeedSyncService` (`SyncFeedAsync`, `SyncAllAsync`, `DetermineStatus`, `UpdateFeedHealthAsync`, `UpdateLogAsync`).

## `SyncResult` (Record, zugehöriger Datentyp)

Datei: `src/Reporter.Core/Services/SyncResult.cs`

`record SyncResult(string Status, int NewItems, string? Message = null)` — `NewItems` zählt aktuell die **gespeicherten** neuen Items (`newItemEntities.Count`); ein separater Zähler für verworfene/gefilterte Items existiert nicht.
