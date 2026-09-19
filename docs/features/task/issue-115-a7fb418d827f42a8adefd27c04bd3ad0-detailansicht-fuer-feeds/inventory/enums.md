<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Enums / Konstanten — Bestandsaufnahme

Im betroffenen Bereich existieren **keine echten `enum`-Typen**. Statuswerte werden als `string`-Konstanten in statischen Klassen gehalten und in den Modellen als `string?` persistiert.

## `FeedHealth`
Datei: `src/Reporter.Core/Services/FeedHealth.cs`

`public static class FeedHealth` — String-Konstanten für `Feed.HealthStatus`/`FeedListItem.HealthStatus`.

| Wert | Bedeutung |
|------|-----------|
| `Ok` = `"OK"` | Feed gesund |
| `Warning` = `"Warning"` | Feed mit Warnungen |
| `Error` = `"Error"` | Feed mit Fehlern — steuert in `FeedsPage.OnFeedTapped` den Eintrag „Fehlerdetails anzeigen" und die rote Statusanzeige |

Hilfsmethode: `Changed(string? current, string? next)` — Statusvergleich.

## `FeedSyncErrorKind`
Datei: `src/Reporter.Core/Services/FeedSyncErrorKind.cs`

`public static class FeedSyncErrorKind` — String-Konstanten für `Feed.LastErrorKind`/`FeedListItem.LastErrorKind`; `FeedsViewModel.GetFeedErrorMessage` mappt sie auf `AppResources.FeedErrorKind*`.

| Wert | Bedeutung |
|------|-----------|
| `InsecureHttpBlocked` = `"InsecureHttpBlocked"` | Fehlgeschlagener Request auf `http`-URL ohne HTTP-Antwort |
| `HttpStatus` = `"HttpStatus"` | Nicht-Erfolgs-HTTP-Statuscode |
| `Network` = `"Network"` | Netzwerk-/Verbindungsfehler ohne HTTP-Antwort |
| `Parse` = `"Parse"` | Feed-Dokument nicht parsebar |
| `Unknown` = `"Unknown"` | Sonstiger Fehler |

Hilfsmethode: `Classify(Exception ex, string feedUrl)` — mappt `HttpRequestException` (mit/ohne `StatusCode`), `XmlException` und Rest auf die Kategorien.
