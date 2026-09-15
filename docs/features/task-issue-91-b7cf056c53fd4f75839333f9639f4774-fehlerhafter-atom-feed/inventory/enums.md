<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Enums und Konstanten — Bestandsaufnahme

Das Projekt verwendet für die sync-relevanten Wertebereiche keine echten `enum`-Typen, sondern statische Klassen mit `const string`-Werten.

## `FeedHealth`
Datei: `src/Reporter.Core/Services/FeedHealth.cs` — statische Klasse, zusätzlich `Changed(current, next)`-Helper.

| Wert | Bedeutung |
|------|-----------|
| `Ok = "OK"` | Feed gesund |
| `Warning = "Warning"` | Feed mit Warnung (Itemschwund > 50 % bzw. keine neuen Items seit > 30 Tagen) |
| `Error = "Error"` | Sync-Fehler (u. a. `Parse` bei nicht lesbarem Dokument) |

## `FeedSyncErrorKind`
Datei: `src/Reporter.Core/Services/FeedSyncErrorKind.cs` — statische Klasse; Werte werden in `Feed.LastErrorKind` persistiert.

| Wert | Bedeutung |
|------|-----------|
| `InsecureHttpBlocked = "InsecureHttpBlocked"` | `http`-Feed ohne HTTP-Antwort (blockierter Klartext-Verkehr, z. B. ATS) |
| `HttpStatus = "HttpStatus"` | Server antwortete mit Nicht-Erfolgs-Statuscode |
| `Network = "Network"` | Netzwerk-/Verbindungsfehler ohne HTTP-Antwort |
| `Parse = "Parse"` | Feed-Dokument nicht parsebar (`XmlException`) — hier landet aktuell der Atom-0.3-Feed |
| `Unknown = "Unknown"` | Sonstiger Fehler |

## `DebugLogCategory`
Datei: `src/Reporter.Core/Services/DebugLogCategory.cs` — statische Klasse; für den Sync relevant:

| Wert | Bedeutung |
|------|-----------|
| `Sync = "Sync"` | Kategorie der Sync-Fehler- und Notification-Warn-Einträge des `FeedSyncService` |

(weitere Werte `Lifecycle`, `Exception`, `Settings`, `Report` für andere Bereiche — nicht sync-relevant)

## `DebugLogLevel`
Datei: `src/Reporter.Core/Services/DebugLogLevel.cs` — statische Klasse.

| Wert | Bedeutung |
|------|-----------|
| `Info = "Info"` | Information |
| `Warning = "Warning"` | Warnung (Notification-Pfad-Fehler) |
| `Error = "Error"` | Fehler (Sync-Fehler in `SyncFeedAsync`-`catch`) |
