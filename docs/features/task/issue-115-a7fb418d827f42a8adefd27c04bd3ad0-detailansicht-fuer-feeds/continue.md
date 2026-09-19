<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-09-19
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [x] `FeedDetailViewModel.cs` — `SaveEditAsync` (ca. Z. 630–661) und `ReloadFeedAsync` (ca. Z. 595–598) rufen Repositories ohne try/catch auf; ein Repository-Fehler faultet den `ExecutionTask` unbeobachtet, der Nutzer sieht keine Fehlermeldung. Try/catch mit `ErrorMessage` ergänzen (Parität zum im Branch etablierten Muster). — Erledigt: `SaveEditAsync` fängt Repository-Fehler mit `ErrorActionFailed` (Sheet bleibt offen), `ReloadFeedAsync` mit `ErrorLoadFailed`; `RefreshAsync` startet die Liste nur bei erfolgreichem Reload neu, damit die Meldung sichtbar bleibt. Regressionstests `SaveEditCommand_WhenRepositoryThrows_SetsErrorAndKeepsSheetOpen` und `RefreshCommand_WhenReloadThrows_SetsLocalizedErrorMessage` (vor dem Fix rot verifiziert).
- [x] `FeedDbAssertions.cs` — `MarkAllItemsReadAsync` (ca. Z. 204–237) und `DeleteAllFeedsAsync` (ca. Z. 249–285) duplizieren ~25 Zeilen identisches Retry-Gerüst; nur der `CommandText` unterscheidet sich. Gemeinsame `ExecuteWriteWithRetryAsync`-Hilfsmethode extrahieren. — Erledigt: beide Methoden delegieren auf `ExecuteWriteWithRetryAsync` (Z. 228–262).
- [x] `FakeFeedSyncService.cs` — `SyncFeedResult = new("OK", 0)` (ca. Z. 51) nutzt ein Magic-String-Literal, während `SyncAllResult` (ca. Z. 17) `FeedHealth.Ok` verwendet — für dasselbe Konzept. Auf `FeedHealth.Ok` vereinheitlichen. — Erledigt: `SyncFeedResult = new(FeedHealth.Ok, 0)` (Z. 53), vom dritten Code-Review (`review-code.md`) verifiziert.

## Usability-Befunde

Keine.

## Fehlgeschlagene Tests

- [ ] `ArticleImageTests.ArticleImage_StoredLocally_AndShownOnCardAndDetail` — `TimeoutException` auf `ArticleCardThumbnail` (UIA-Timing). Als dokumentiert preexisting-flaky eingestuft (Baseline `inventory/tests.md`: 2× flaky, im Wiederholungslauf grün; isolierter Wiederholungslauf 2/2 bestanden). Nicht feature-bedingt — Stabilität der Suite separat verbessern. Hinweis: In dieser Fortsetzungs-Iteration konnte die E2E-Suite nicht erneut ausgeführt werden (interaktive Windows-Session erforderlich); der Eintrag bleibt daher offen.
