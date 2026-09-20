<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-09-20
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [x] Verirrte leere Datei `nul` im Repository-Stammverzeichnis (0 Bytes, untracked, Windows-Gerätename) — **bereits behoben:** Datei nach dem Review gelöscht. Verbleibt zur Nachverfolgung hier dokumentiert, da `review-code.md` den Status `Befunde vorhanden` trägt.

## Usability-Befunde

Keine — `review-usability.md` trägt den Status `Keine Befunde`.

## Fehlgeschlagene Tests

- [x] `Reporter.E2ETests.ArticleLinkTests.ExternalLinkInArticle_OpensSystemBrowser` — `System.TimeoutException: Element not found within 00:00:20: name 'link-feed article'`. Preexisting/instabil (in `inventory/tests.md` dokumentiertes Fehlerbild der E2E-Suite): schlug in Iteration 1 und 3 fehl, bestand in Iteration 2 und im isolierten Re-Run. Nicht feature-bezogen — das Feature berührt weder Unread-Liste noch Karten-Rendering noch WebView. **Erledigt im Fortsetzungslauf (2026-09-20):** Isolierter Re-Run (`dotnet test src/Reporter.E2ETests --filter FullyQualifiedName~ArticleLinkTests.ExternalLinkInArticle_OpensSystemBrowser`) bestanden in 15 s — bekannter E2E-Flake, kein Feature-Bezug, keine Produktivcode-Änderung erforderlich.
