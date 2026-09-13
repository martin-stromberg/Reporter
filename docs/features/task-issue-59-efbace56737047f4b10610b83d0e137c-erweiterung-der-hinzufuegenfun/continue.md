<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Offene Folgeaufgaben

Die Implementierungs-/Review-Schleife wurde nach 3 Iterationen beendet.
`review.md` (Plan-Review) trägt `Vollständig umgesetzt`,
`review-usability.md` trägt `Keine Befunde`, `test-results.md` trägt
`Keine Fehler` (311 .NET-Tests + 36 Node-Tests grün).
Verbleiben 3 geringfügige Befunde aus `review-code.md` (Iteration 3):

1. **`FeedsViewModel.cs` – `ResetForm` (~Z. 383–392):** Leert
   `ErrorMessage`/`SearchErrorMessage` nicht. `CloseAddFormCommand` (plus
   Backdrop-Tap und `OnBackButtonPressed`) läuft über `ResetForm`; ein im
   Sheet angezeigter Validierungs-/Dubletten-/Such-Fehler bleibt danach im
   Seitenkopf über der Feed-Liste sichtbar (`FeedsPage.xaml` Z. 20–23,
   48–51). `OpenAddForm` leert beide Kanäle explizit — asymmetrisch.
   → Beide Fehlerkanäle in `ResetForm` leeren; Regressionstest:
   Fehler im Sheet erzeugen, Sheet schließen, Header frei.

2. **`FeedsViewModel.cs` – `SaveAsync` (~Z. 331):** `categoryId` kommt aus
   `SelectedCategory`, das seit Entfernen des Pickers an kein UI gebunden
   ist und nur Preserve-State darstellt; `SelectedCategory` kann durch den
   `LoadAsync`-Reset (Z. 292–295) driften. Empfehlung:
   `SelectedFeed.CategoryId` direkt nutzen und `ToFeed` um einen
   `url`-Parameter erweitern, damit `SaveAsync` den Helper wiederverwendet.

3. **`FeedsPage.xaml.cs` – `ChangeCategoryAsync` (Z. 135–156):**
   Zählsuffix-Disambiguierung garantiert keine eindeutigen Optionen — eine
   Kategorie mit Literalnamen wie „News (2)" kollidiert mit dem
   auto-erzeugten Suffix; `options.IndexOf` löst dann auf den falschen
   Eintrag auf. → Auflösung indexbasiert ohne Namensvergleich oder
   kollisionsfreie Suffixe (z. B. fortlaufende Nummer bis eindeutig).
   Hinweis: Kategorie-Namensdubletten sind durch Unique-Index
   `IX_categories_name` praktisch ausgeschlossen (defensive Absicherung).
