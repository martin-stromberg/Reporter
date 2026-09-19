<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedDetailPage.xaml (Feed-Detailansicht)

- **Erreichbarkeit** — Die Anforderung verlangt „eine Suche zum Wiederfinden älterer Beiträge". Gibt eine Anwenderin einen Suchbegriff ohne Treffer ein, leert sich die Liste und der `EmptyView` zeigt denselben Text wie bei einem Feed ganz ohne Artikel: „Hier erscheinen Artikel dieses Feeds." (`PlaceholderFeedDetail`, gebunden in `FeedDetailPage.xaml`, `CollectionView.EmptyView`). Für eine Laiin ist nicht erkennbar, ob die Suche gelaufen ist und nichts gefunden hat oder ob der Feed leer/die Suche defekt ist — sie erhält keine Rückmeldung zum Suchergebnis.

  Empfehlung: Bei aktivem `SearchText` einen abweichenden Leer-Text anzeigen, z. B. per `DataTrigger` auf `SearchText` (nicht leer) eine zweite `EmptyView`-Beschriftung mit neuem Resource-Schlüssel nach dem Muster von `FeedSearchNoResults` („Keine Artikel zu diesem Suchbegriff gefunden.").

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- Feed-Karte in der Übersicht antippen → navigiert zur Detailansicht statt zum Aktionsblatt (`FeedsPage.xaml.cs:56-71`, `feeddetail?feedId=…`; Screenreader-Hinweis `AccessibilityOpenFeedDetails` korrekt) → unauffällig
- Alle Beiträge des Feeds ansehen (gelesene wie ungelesene, absteigend nach Datum, ungelesen-Markierung per Punkt-Indikator in `ArticleCardView`) → unauffällig
- Ältere Beiträge durch Nachladen beim Scrollen erreichen (`RemainingItemsThreshold="2"` + `LoadMoreCommand`, `PageSize = 20`) → unauffällig
- Beiträge im Feed suchen (`SearchBar` mit lokalisiertem Platzhalter „Artikel in diesem Feed suchen…", Debounce, Titelfilter in `ItemRepository.GetByFeedAsync`) → Befund vorhanden (Leer-Zustand bei erfolgloser Suche nicht von leerem Feed unterscheidbar)
- Beitrag antippen → Artikeldetail öffnen (`ArticleCardView`-Standardkommando `articledetail?itemId=…`) → unauffällig
- Beitrag als gelesen markieren / für später merken (44×44-pt-Symbole mit `SemanticProperties.Description` auf jeder Karte) → unauffällig
- Feed-Aktionen ausführen: Aktualisieren, Umbenennen, Kategorie ändern, Bearbeiten, Fehlerdetails anzeigen, Löschen — alle über den beschrifteten Button „Aktionen" erreichbar; Umbenennen mit vorausgefülltem Titel-Dialog, Kategorie über Klartext-Namen im Aktionsblatt (kein interner Schlüssel nötig), Fehlerdetails nur bei Fehler-Status → unauffällig
- Feed bearbeiten (eigenes Bottom-Sheet mit vorausgefüllter URL und Benachrichtigungs-Schalter; Abbrechen/Speichern, Schließen per Antippen des dimmenden Hintergrunds oder Zurück-Taste) → unauffällig
- Nach dem Löschen zur Übersicht zurückkehren (`DeleteFeedAsync` → `GoToAsync("..")`) → unauffällig
- Zur Übersicht zurücknavigieren (44×44-pt-Zurück-Schaltfläche mit `AccessibilityBack`-Beschreibung) → unauffällig
- Per Ziehen-Geste aktualisieren (`RefreshView` um die Liste) → unauffällig

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:

- `src/Reporter/Views/FeedDetailPage.xaml` (neu)
- `src/Reporter/Views/FeedDetailPage.xaml.cs` (neu)
- `src/Reporter/Views/FeedsPage.xaml` (geändert)
- `src/Reporter/Views/FeedsPage.xaml.cs` (geändert)
- `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs` (neu)
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`, `FeedsViewModel.Search.cs`, `BaseViewModel.cs` (Diff-Sichtung)
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` (neue Schlüssel, DE + EN vorhanden)
- `src/Reporter.Data/Repositories/ItemRepository.cs`, `src/Reporter.Core/Interfaces/IItemRepository.cs` (Diff-Sichtung: Sortierung/Paging/Suchfilter)
- `src/Reporter/AppShell.xaml.cs`, `src/Reporter/MauiProgram.cs` (Route + DI)
- `src/Reporter/Views/ArticleCardView.xaml` / `.xaml.cs` (wiederverwendete Karten-Komponente, Kontext)
