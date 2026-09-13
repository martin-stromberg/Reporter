<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-09-13
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [ ] `src/Reporter.Tests/LaterViewModelTests.cs:451` — `FailingItemRepository` ist ~95 % identisch mit `UnreadViewModelTests.FailingItemRepository` (`src/Reporter.Tests/UnreadViewModelTests.cs:521`); `GatedItemRepository` (`LaterViewModelTests.cs:375`) wiederholt denselben Boilerplate ein drittes Mal. Empfehlung: gemeinsame `DelegatingItemRepository`-Basisklasse mit `virtual`-Membern im Testprojekt. (Schwere: niedrig)

## Usability-Befunde

Keine — `review-usability.md` trägt den Status `Keine Befunde`.

Hinweis außerhalb des Feature-Umfangs (Datei in diesem Branch unverändert, ggf. separates Issue):
- [ ] `src/Reporter/Views/CategoriesPage.xaml.cs:62-66` — Der Lösch-Dialog für Kategorien verwendet `ConfirmDeleteFeedTitle` = „Feed löschen?"; für Endanwender irreführend beschriftet.

## Fehlgeschlagene Tests / Ausstehende Verifikationen

Keine automatisierten Testfehlschläge (350/350 grün, Static Checks Exit 0). Folgende Verifikationen sind in dieser Umgebung nicht ausführbar und bleiben ausstehend:

- [ ] Accessibility Insights FastPass (Windows, 390 × 844 pt) — Tool nicht installiert; formale Kontrastmessung (4,5:1 Fließtext / 3:1 Icons) ausstehend. Verbindlicher „keine kritischen Fehler"-Nachweis.
- [ ] Narrator-Durchlauf — interaktiver Screenreader-Test nicht ausführbar.
- [ ] iOS-Verifikation (`net10.0-ios`-Build, `scripts/iOS-Deployment.ps1`, Simulator-Screenshots) — erfordert macOS (dokumentierte Folgeaufgabe, Präzedenz #27/#28).
- [ ] Splash-Screen-Laufzeitnachweis — konfiguriert (`#1e293b` + Motiv), zur Laufzeit zu kurz sichtbar für Screenshot.
- [ ] `FeedSearchResult.DisplayTitle` zur Laufzeit — feedsearch.dev lieferte keine Treffer; bislang nur code-seitig verifiziert (`FeedsPage.xaml:83`, `FeedSearchResult.cs`).
