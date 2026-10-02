<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-10-02
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

Keine — `review-code.md` trägt den Status `Keine Befunde` (Iteration 3).

## Usability-Befunde

Keine — `review-usability.md` trägt den Status `Keine Befunde` (Iteration 3).

## Fehlgeschlagene Tests

- [ ] `FeedDetailTests.FeedDetail_Edit_FeedKeywordValidation_ShowsErrors` — TimeoutException beim Warten auf `ErrorKeywordDuplicate` / Element `name 'e2e-dup'` (`FeedDetailTests.cs:632`/`635`). Flaky, feature-fremd: fehlgeschlagen in den Suitenläufen der Iterationen 2 und 3, bestanden in Iteration 1 und in isolierten Einzelläufen. Der betroffene Codepfad (Edit-Sheet-Stichworte) wurde von diesem Feature nicht berührt. Mögliche Folgeaufgabe: Stabilisierung des Tests (z. B. längere Timeouts/Retry in den UI-Test-Hilfsmethoden).
