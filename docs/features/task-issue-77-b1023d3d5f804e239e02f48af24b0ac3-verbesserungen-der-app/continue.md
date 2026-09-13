<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-09-13
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [ ] `FeedSyncService.cs` — Fehlerbehandlung / fehlende Cancellation-Weitergabe: `TryFindFaviconUrlAsync` (Zeile 311) nimmt keinen `CancellationToken` entgegen und ruft `_feedIconService.TryFindFaviconUrlAsync(feedUrl, siteUrl)` ohne ihn auf, obwohl die Interface-Signatur einen Token-Parameter anbietet; eine abgebrochene Synchronisation wartet dadurch bis zu zwei HTTP-Roundtrips ab. Empfehlung: Token-Parameter ergänzen und an `_feedIconService.TryFindFaviconUrlAsync(feedUrl, siteUrl, cancellationToken)` sowie am Aufruf in `RunSyncAsync` (Zeile 173) durchreichen.
- [ ] `ReadingTimeEstimator.cs` — Toter Code: `Math.Max(1, ...)` in Zeile 34 ist seit dem Early-Return `minutes <= 1 → string.Empty` wirkungslos. Empfehlung: Klemmung entfernen (`var minutes = (int)Math.Round(wordCount / WordsPerMinute);`) — Verhalten bleibt identisch.

## Usability-Befunde

Keine.

## Fehlgeschlagene Tests

Keine.
