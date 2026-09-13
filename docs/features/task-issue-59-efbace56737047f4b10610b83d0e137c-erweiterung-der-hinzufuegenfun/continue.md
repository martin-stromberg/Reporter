<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-09-13
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine.

## Code-Review-Befunde

- [ ] `FeedsViewModel.Search.cs` — Doppelter Code: Stale-Check `!string.Equals(NewUrl.Trim(), input, StringComparison.Ordinal)` identisch in Try- (Z. 131–136) und Catch-Zweig (Z. 153–156) von `SearchAsync` dupliziert. Empfehlung: Hilfsmethode `IsStaleInput(input)` extrahieren.
- [ ] `FeedsViewModel.Search.cs` / `FeedSyncService.cs` — Fachliche Konsistenz: `OfferDirectAddAsync` (Z. 228–232) setzt `NewTitle = hostUri.Host`; die Platzhalter-Erkennung in `FeedSyncService` (`isPlaceholderTitle`) löst den Dokumenttitel nur bei leerem Titel oder Titel == URL auf. Direkt hinzugefügte Feeds behalten dauerhaft den Hostnamen, während titellose Abo-Feeds beim Sync den echten Titel erhalten.

## Usability-Befunde

Keine.

## Fehlgeschlagene Tests

Keine.
