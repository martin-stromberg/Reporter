<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Aufgaben

Erstellt am: 2026-09-13
Abbruchgrund: Maximale Iterationsanzahl erreicht
Aktualisiert am: 2026-09-13 (Fortsetzungslauf — lösbare Punkte abgearbeitet)

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einer geeigneten Umgebung bearbeitet werden.

## Offene Planelemente

Keine — `review.md` trägt den Status `Vollständig umgesetzt`.

## Code-Review-Befunde

- [x] `src/Reporter.Tests/LaterViewModelTests.cs` — `FailingItemRepository`/`GatedItemRepository` duplizierten den `IItemRepository`-Delegations-Boilerplate (zusammen mit `UnreadViewModelTests.FailingItemRepository` dreifach). **Erledigt:** Neue Basisklasse `src/Reporter.Tests/DelegatingItemRepository.cs` mit `virtual`-Membern, die an das innere Repository delegieren; alle drei Fakes erben davon und überschreiben nur die jeweils relevante Methode. `review-code.md` (Lauf 4): `Keine Befunde`.

## Usability-Befunde

Keine — `review-usability.md` trägt den Status `Keine Befunde`.

- [x] `src/Reporter/Views/CategoriesPage.xaml.cs` — Der Lösch-Dialog für Kategorien verwendete `ConfirmDeleteFeedTitle` („Feed löschen?"). **Erledigt:** Neuer Schlüssel `ConfirmDeleteCategoryTitle` („Kategorie löschen?" / „Delete category?") in `AppResources.resx`/`AppResources.de.resx` + `AppResources.Designer.cs`; `CategoriesPage.xaml.cs` nutzt ihn jetzt.

## Fehlgeschlagene Tests / Ausstehende Verifikationen

Keine automatisierten Testfehlschläge (350/350 grün, Static Checks Exit 0). Folgende Verifikationen sind in dieser Umgebung nicht ausführbar und bleiben ausstehend:

- [ ] Accessibility Insights FastPass (Windows, 390 × 844 pt) — Tool nicht installiert; formale Kontrastmessung (4,5:1 Fließtext / 3:1 Icons) ausstehend. Verbindlicher „keine kritischen Fehler"-Nachweis.
- [ ] Narrator-Durchlauf — interaktiver Screenreader-Test nicht ausführbar.
- [ ] iOS-Verifikation (`net10.0-ios`-Build, `scripts/iOS-Deployment.ps1`, Simulator-Screenshots) — erfordert macOS (dokumentierte Folgeaufgabe, Präzedenz #27/#28).
- [ ] Splash-Screen-Laufzeitnachweis — konfiguriert (`#1e293b` + Motiv), zur Laufzeit zu kurz sichtbar für Screenshot.
- [ ] `FeedSearchResult.DisplayTitle` zur Laufzeit — feedsearch.dev lieferte keine Treffer; bislang nur code-seitig verifiziert (`FeedsPage.xaml:83`, `FeedSearchResult.cs`).
