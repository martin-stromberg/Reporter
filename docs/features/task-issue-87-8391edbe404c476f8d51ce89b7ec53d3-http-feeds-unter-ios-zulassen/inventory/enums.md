<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Enums / Konstanten — Bestandsaufnahme

## `FeedHealth` (statische Klasse, kein Enum)

Datei: `src/Reporter.Core/Services/FeedHealth.cs`

String-Konstanten, die in `Feed.HealthStatus`, `SyncLog.Status` und `SyncResult.Status` persistiert/transportiert werden; die `DataTrigger` in `FeedsPage.xaml` vergleichen gegen die Literalwerte `"OK"`, `"Warning"`, `"Error"`.

| Wert | Bedeutung |
|------|-----------|
| `Ok = "OK"` | Feed fehlerfrei |
| `Warning = "Warning"` | Warnung (Item-Verlust, >30 Tage ohne neue Items) |
| `Error = "Error"` | Sync-Fehler |

Hilfsmethode: `Changed(string? current, string? next)` — Ungleichheitsprüfung für `HealthLastChange`.

## `FeedSearchMatchKind` (Enum)

Datei: `src/Reporter.Core/Models/FeedSearchMatchKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `ExactUrl` | Ergebnis-Feed-URL entspricht der Eingabe bzw. Eingabe ist selbst ein Feeddokument |
| `Directory` | Treffer aus dem feedsearch.dev-Verzeichnis |
| `Discovered` | Per Link-Tags oder Standard-Pfaden auf der Website entdeckt |

Deklarationsreihenfolge = Sortierreihenfolge in `FeedSearchService.SearchAsync`.

Es existiert **kein** Enum für strukturierte Sync-Fehlerkategorien (z. B. ATS/Netzwerk/Parse); Fehler werden aktuell nur als `FeedHealth.Error` + Freitext in `SyncLog.Message`/`SyncResult.Message` abgebildet.
