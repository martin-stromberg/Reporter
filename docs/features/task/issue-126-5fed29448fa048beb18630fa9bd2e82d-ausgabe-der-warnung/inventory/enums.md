<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Enums und Konstanten — Bestandsaufnahme

Das Projekt verwendet keine echten `enum`-Typen für Status- und Fehlerkategorien, sondern statische Klassen mit `string`-Konstanten (so persistierbar in `TEXT`-Spalten). Für die Anforderung relevant sind `FeedHealth` (bestehender `Warning`-Wert) und `FeedSyncErrorKind` (Klassifikationsmuster für den Fehlerdetails-Mechanismus; ein `FeedSyncWarningKind`-Pendant existiert noch nicht).

## `FeedHealth`

Datei: `src/Reporter.Core/Services/FeedHealth.cs`

Statische Klasse mit Statuskonstanten und einem Hilfsmember.

| Wert | Bedeutung |
|------|-----------|
| `Ok` = `"OK"` | Gesunder Feed. |
| `Warning` = `"Warning"` | Abruf erfolgreich, aber auffällig: <50 % der gespeicherten Artikel geliefert oder >30 Tage keine neuen Artikel (Entscheidung in `FeedSyncService.DetermineStatus`). |
| `Error` = `"Error"` | Sync-Fehlschlag. |

Hilfsmethode: `Changed(string? current, string? next)` → `bool` — Vergleich für `HealthLastChange`.

## `FeedSyncErrorKind`

Datei: `src/Reporter.Core/Services/FeedSyncErrorKind.cs`

Statische Klasse mit Kategoriekonstanten für den letzten Sync-Fehler plus Klassifikationsmethode.

| Wert | Bedeutung |
|------|-----------|
| `InsecureHttpBlocked` = `"InsecureHttpBlocked"` | `http`-Feed-URL ohne HTTP-Antwort (blockierter/verweigerter Klartext). |
| `HttpStatus` = `"HttpStatus"` | Server antwortete mit Nicht-Erfolgs-Statuscode. |
| `Network` = `"Network"` | Netzwerk-/Verbindungsfehler ohne HTTP-Antwort (nicht-http-URL). |
| `Parse` = `"Parse"` | Feed-Dokument nicht parsbar (`XmlException`). |
| `Unknown` = `"Unknown"` | Sonstige Fehler. |

Methode: `Classify(Exception ex, string feedUrl)` → `string` — bildet die Sync-Exception auf eine Kategorie ab (`HttpRequestException` mit `StatusCode` → `HttpStatus`; ohne `StatusCode` und `http`-URL → `InsecureHttpBlocked`, sonst `Network`; `XmlException` → `Parse`; Rest → `Unknown`).

Verwendung: `FeedSyncService.SyncFeedAsync` (Catch-Pfad) → `Feed.LastErrorKind`; `FeedDetailViewModel.GetFeedErrorMessage` → `AppResources.FeedErrorKind*` (Fallback `FeedErrorKindUnknown` für leere/unbekannte Werte). Dieses Muster ist die Vorlage für eine Warnungs-Klassifikation (`FeedSyncWarningKind` existiert noch nicht).

## Ressourcen-Schlüssel (keine Enums, aber für die Lokalisierung relevant)

Datei: `src/Reporter.Core/Resources/Strings/AppResources.resx` (EN) und `AppResources.de.resx` (DE), generierte Properties in `AppResources.Designer.cs`.

| Schlüssel | EN | DE |
|-----------|----|----|
| `FeedErrorDetailsTitle` | „Sync error" | „Synchronisierungsfehler" |
| `ButtonShowErrorDetails` | „Show error details" | „Fehlerdetails anzeigen" |
| `FeedErrorKindInsecureHttpBlocked` | HTTP-blockiert-Text | dt. Pendant |
| `FeedErrorKindHttpStatus` | „The feed server reported an HTTP error." | „Der Feed-Server hat einen HTTP-Fehler gemeldet." |
| `FeedErrorKindNetwork` | „The feed could not be reached. Check the network connection and the feed address." | „Der Feed konnte nicht erreicht werden. Prüfe die Netzwerkverbindung und die Feed-Adresse." |
| `FeedErrorKindParse` | „The feed format could not be read." | „Das Feed-Format konnte nicht gelesen werden." |
| `FeedErrorKindUnknown` | „The synchronization failed with an unexpected error." | „Die Synchronisation ist mit einem unerwarteten Fehler fehlgeschlagen." |
| `HealthStatusOkLabel` / `HealthStatusWarningLabel` / `HealthStatusErrorLabel` | Badge-Texte | — |
| `LabelFeedHealthStatus` | Badge-Beschriftung | — |

Es existieren keine `FeedWarning*`- oder `ButtonShowWarningDetails`-Schlüssel.
