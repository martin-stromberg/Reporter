<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-10-16
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — alle 33 Tasks sind erledigt (`review.md`: `Vollständig umgesetzt`).

## Code-Review-Befunde

Keine (`review-code.md`: `Keine Befunde`).

## Usability-Befunde

Keine (`review-usability.md`: `Keine Befunde`).

## Fehlgeschlagene Tests

- [ ] `Reporter.E2ETests.FeedDetailTests.FeedDetail_MetaInfo_WrapsInsteadOfOverlappingActionsButton` — `System.TimeoutException : Element not found within 00:00:20: name 'meta-wrap-target.xml'`. **Vorbestehender, feature-fremder Testfehler** (Issue #127, Commit `146e2fd`, auf `staging` unverändert): Der Test übergibt den Pre-Sync-Kartentitel `meta-wrap-target.xml` an `OpenFeedDetail`, obwohl die Karte nach `SyncAllViaUnreadTab()` den Kanaltitel `Stub Feed meta-wrap-target` trägt. **Anwender-Entscheid (2026-10-16):** Nicht in diesem Feature-Branch beheben — Korrektur erfolgt auf einem eigenen Branch nach dem Merge dieses PRs.
