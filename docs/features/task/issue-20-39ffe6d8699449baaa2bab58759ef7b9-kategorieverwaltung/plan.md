# Umsetzungsplan: Kategorieverwaltung

## Entscheidungen
1. Löschverhalten: Bei zugeordneten Feeds wird die `category_id` per `DeleteBehavior.SetNull` auf NULL gesetzt (bestehendes Schema); Kategorie kann ohne Zusatzabfrage gelöscht werden.
2. Navigation: Neuer Tab „Kategorien“ in `AppShell` als dedizierte Kategorieverwaltung (keine Unterseite der Einstellungen).
3. Feed-Anzahl: `ICategoryRepository.GetAllWithFeedCountAsync()` liefert die Anzahl zugeordneter Feeds, um DB-Ebene abzubilden.
4. Validierung: Im ViewModel; Duplikatprüfung case-insensitiv gegen bestehende Kategorien (exkl. eigener Name beim Bearbeiten).

## Implementierungsschritte
1. `ICategoryRepository` erweitern um `GetAllWithFeedCountAsync()`.
2. `CategoryRepository` implementiert `GetAllWithFeedCountAsync()` via EF-Join/GroupJoin.
3. `CategoryWithCount`-Modell in `Reporter.Core` hinzufügen.
4. `CategoriesViewModel` mit `LoadCategoriesCommand`, `AddCategoryCommand`, `SaveCategoryCommand`, `DeleteCategoryCommand`.
5. `CategoriesPage.xaml` + `.cs` (ListView/CollectionView, Eingabefelder, Validierung).
6. `AppShell.xaml.cs` um Kategorien-Tab erweitern.
7. `MauiProgram.cs`: `CategoriesViewModel` und `CategoriesPage` registrieren.
8. `AppResources.resx`/`.Designer.cs` um Titel/Platzhalter/Labels erweitern.
9. Tests: `CategoryRepositoryTests` + `CategoriesViewModelTests`.

## Testplan

### Service-Level / Integration
- `GetAllWithFeedCountAsync_ReturnsCountOfFeedsPerCategory`: Legt 2 Kategorien und 3 Feeds an, prüft korrekte Zuordnungszahlen (inkl. 0).
- `UpdateAsync_DuplicateName_Throws`: Fügt zwei Kategorien hinzu; versucht auf gleichen Namen umzubenennen → erwartet Exception.
- `DeleteAsync_WithAssignedFeeds_SetsCategoryIdToNull`: Löscht Kategorie; betroffene Feeds haben `CategoryId == null`.

### UI-Level / ViewModel (Ersatz für E2E, da kein E2E-Framework im Projekt vorhanden)
- `AddCommand_WithEmptyName_SetsValidationErrorAndDoesNotAdd`: Leerer Name → `CanSave` false, keine Interaktion mit Repository.
- `AddCommand_WithDuplicateName_SetsValidationError`: Name bereits vergeben → Fehlermeldung.
- `DeleteCommand_RemovesCategoryAndReloadsList`: Löschen löst `DeleteAsync` und `LoadAsync` aus.
- `SaveCommand_UpdatesExistingCategory`: Bearbeiten persistiert neuen Namen.
- `LoadCommand_PopulatesCategoriesWithFeedCounts`: Liste mit Anzahl zugeordneter Feeds.

## Akzeptanzkriterien-Abdeckung
| Kriterium | Test |
|-----------|------|
| Kategorien erstellen/umbenennen/löschen | `SaveCommand_*`, `DeleteCommand_*` |
| Änderungen in SQLite persistieren | `CategoryRepositoryTests` Roundtrip |
| Feeds ohne Kategorie bleiben funktionsfähig | `DeleteAsync_WithAssignedFeeds_SetsCategoryIdToNull` |
| Liste zeigt Namen + Feed-Anzahl | `GetAllWithFeedCountAsync_ReturnsCountOfFeedsPerCategory`, `LoadCommand_PopulatesCategoriesWithFeedCounts` |

## Offene Punkte
- Keine weiteren offenen Punkte.
