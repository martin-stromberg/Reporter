<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: Lesepakete (GitHub Issue #164)

## Fachliche Zusammenfassung

Die Startseite **Ungelesen** (`UnreadPage` / `UnreadViewModel`) lädt ungelesene Artikel bislang seitenübergreifend über alle Tage hinweg (`PageSize = 20`, `GetUnreadByDateAsync`). Neu werden ungelesene Artikel als **Lesepaket pro Kalendertag** geladen: Beim frischen Laden der Liste bestimmt das `PublishedAt`-Datum des ersten Artikels (aufsteigende Sortierung = ältester ungelesener Artikel) den anzuzeigenden Tag; nur Artikel dieses Tages werden dargestellt. Liefert der Tag weniger als 5 Artikel, wird zusätzlich der Folgetag geladen. Der Schalter **Alles gelesen** (Doppelhaken-Symbol, `MarkAllReadCommand`) markiert künftig nur noch die Artikel des aktuell geladenen Tagespakets statt aller ungelesenen Artikel.

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein „Lesepaket" — die Menge aller ungelesenen Artikel (`Item`/`ItemListItem` mit `IsRead == false`) eines Kalendertags, bestimmt durch `PublishedAt` (Ortsdatum), eingeschränkt auf den aktuell gewählten Kategoriefilter (`SelectedCategory.CategoryId`). Enthält der Tag weniger als 5 Artikel, erweitert sich das Paket um die Artikel des nächsten Kalendertags mit ungelesenen Artikeln.
- **Auslösende Ereignisse:**
  - Erstes Anzeigen der Seite (`OnAppearing` → `LoadCommand`/`LoadAsync`).
  - Pull-to-Refresh bzw. Aktualisieren-Schaltfläche (`RefreshCommand`/`RefreshAsync` → `LoadAsync`).
  - Wechsel des Kategoriefilters (`SelectCategoryCommand`/`SelectCategoryAsync` — frischer Neuaufbau der Liste).
  - Abschluss von **Alles gelesen** (`MarkAllReadCommand` → `LoadAsync` lädt das nächste Tagespaket).
  - Leerwerden der Liste durch einzelnes Als-gelesen-Markieren (`MarkReadAsync` → bestehender `LoadAsync`-Aufruf bei `Articles.Count == 0`).
- **Erwartete Wirkung:**
  - Die Liste/Kartenansicht zeigt nach einem frischen Laden nur die Artikel des Lesepakets (erster Tag, ggf. plus Folgetag bei <5 Artikeln), nicht alle ungelesenen Artikel.
  - Die Bestätigung über das Doppelhakensymbol markiert nur die Artikel des aktuell geladenen Tagespakets als gelesen; danach lädt ein frischer Abruf automatisch das Paket des Folgetags.
  - Die Nachlade-Entscheidung (<5-Schwelle) wird ausschließlich beim frischen Neuladen der Liste getroffen — niemals dadurch, dass ein einzelner Artikel beim Als-gelesen-Markieren aus der Liste entfernt wird.
- **Umgang mit fehlenden Objekten:**
  - `PublishedAt` ist `DateTime?`; Artikel ohne Veröffentlichungsdatum haben keinen zuordenbaren Kalendertag. Zu klären bzw. als Annahme festzulegen (Vorschlag: auf den ältesten vorhandenen Tag bzw. Tagesanfang mappen), siehe Offene Fragen.
  - Keine weiteren fachlichen Objekte nötig: Feeds, Kategorien und Einstellungen existieren bereits; kein neues Konfigurationsobjekt wird vorausgesetzt.
- **Scope-Ausschlüsse:**
  - Keine sichtbaren Tagesgruppen-Header oder Abschnittstrenner in der Liste — die Tageweise-Begrenzung ist ein reines Lade-/Markierverhalten.
  - Kein automatisches „Überspringen" älterer Tage — es wird immer das älteste vorhandene Lesepaket gezeigt.
  - Die Anforderung beschreibt ausschließlich die aufsteigende Sortierung („Beiträge nach Datum aufsteigend sortiert"). Das Verhalten bei absteigender Sortierung (`SettingsValues.SortOrderDescending`) ist nicht spezifiziert, siehe Offene Fragen.
  - Keine Änderung am Karten-Layout, an den Filter-Tabs, am Pull-to-Refresh oder am `LaterPage`-/`FeedDetailPage`-Verhalten.
  - Keine neue Benutzereinstellung für die Tageweise-Anzeige oder die 5er-Schwelle (Festlegung durch die Anforderung, kein Konfigurationsbedarf genannt).

## Betroffene Klassen und Komponenten

- **`src/Reporter.Core/Interfaces/IItemRepository.cs`**: Neue oder erweiterte Member voraussichtlich nötig:
  - Tag-begrenzte Abfrage der ungelesenen Artikel (z. B. `GetUnreadByDayAsync(...)` mit Datumsparameter oder Rückgabe des ältesten ungelesenen Datums, z. B. `GetOldestUnreadDateAsync(...)`).
  - Tag-begrenzte Massenmarkierung (z. B. Erweiterung von `MarkAllAsReadAsync(Guid? categoryId)` um einen Datumsparameter oder neue Methode wie `MarkDayAsReadAsync(DateTime day, Guid? categoryId)`).
- **`src/Reporter.Data/Repositories/ItemRepository.cs`**: Implementierung der tag-begrenzten Abfragen/Markierung auf Basis von `PublishedAt`; Beachtung der Sortier-Tie-Breaker (`Id`) und des Kategoriefilters über `Feed.CategoryId`.
- **`src/Reporter.Core/ViewModels/UnreadViewModel.cs`**:
  - `LoadAsync`/`LoadPageAsync`: Bestimmung des aktuellen Tagespakets beim frischen Laden; Umsetzung der <5-Regel (Folgetag nachladen); Verwaltung des Tageskontexts (z. B. neues Feld für das Paket-Datum bzw. die Paket-Tage).
  - `LoadMoreAsync`: Verhalten am Paketende festlegen (siehe Offene Fragen).
  - `MarkAllReadAsync`: Einschränkung auf das geladene Tagespaket statt `MarkAllAsReadAsync(SelectedCategory?.CategoryId)` über alle Tage.
  - `MarkReadAsync`: Sicherstellen, dass das Entfernen einzelner Artikel keine Neubewertung der <5-Regel auslöst; ein Reload erfolgt nur beim Leerwerden der Liste (bestehendes Verhalten) und zählt dann als frisches Neuladen.
- **`src/Reporter/Views/UnreadPage.xaml` / `UnreadPage.xaml.cs`**: `OnMarkAllReadTapped` zeigt aktuell `UnreadBadgeText` im `DisplayActionSheetAsync`; ggf. Anpassung des Bestätigungstexts auf den Tagespaket-Umfang (Anzahl/Tag statt Gesamtzahl) — abhängig von der UX-Entscheidung.
- **`src/Reporter.Core/Resources/Strings/AppResources*.resx`**: Ggf. neue lokalisierte Strings für die Bestätigung (z. B. „Artikel des Tages als gelesen markieren") in `AppResources.resx` und `AppResources.de.resx`.
- **Tests:**
  - `src/Reporter.Tests/UnreadViewModelTests*.cs`: Tagespaket-Bildung beim Laden, <5-Regel nur beim frischen Laden, kein Folgetag-Nachladen beim Entfernen gelesener Artikel, `MarkAllReadCommand` markiert nur das Paket.
  - `src/Reporter.Tests/ItemRepositoryTests.cs`: Tag-Grenzen (inkl. Tageswechsel), Kategoriefilter, `PublishedAt == null`, Tie-Breaker-Sortierung.
  - `src/Reporter.Tests/DelegatingItemRepository.cs`: Um neue Interface-Member zu erweitern.
  - `src/Reporter.E2ETests/`: Akzeptanztest für den Happy Path (ältester Tag → Anzeige → Doppelhaken → nächster Tag wird geladen).

## Implementierungsansatz

- Erweiterungspunkt ist die Lade-Pipeline in `UnreadViewModel.LoadPageAsync`, die aktuell `IItemRepository.GetUnreadByDateAsync(page, pageSize, categoryId, ascending)` aufruft. Die Tag-Begrenzung gehört fachlich in die Repository-Schicht (Datenbank-Filter auf `PublishedAt`-Datum + Kategorie), damit Paging, Zähler (`GetUnreadCountAsync`) und Markierung konsistent bleiben.
- Das ViewModel hält den aktuellen Paket-Kontext (Datum des ersten Artikels, ggf. zweites Datum bei <5-Ausweitung). Die <5-Entscheidung ist an den frischen Ladevorgang gebunden (`LoadAsync`/`SelectCategoryAsync`/`RefreshAsync`-Pfad), nicht an die inkrementelle Änderung der `Articles`-Collection.
- `MarkAllReadAsync` ruft künftig eine tag-begrenzte Markierung mit dem Paket-Datum + `SelectedCategory?.CategoryId` auf und stößt danach `LoadAsync` an, das automatisch das nächste Lesepaket lädt.
- Abhängigkeiten: bestehende Settings (`UnreadSortOrder` via `ISettingsRepository`), `ICategoryRepository` für Filter-Chips, `IFeedSyncService` für Refresh. Keine Migration nötig, sofern kein neues Setting entsteht (siehe Konfiguration).

## Konfiguration

Keine neue Konfiguration vorgesehen: Die 5er-Schwelle ist ein fester fachlicher Wert aus der Anforderung (als Konstante im ViewModel/Service, analog `PageSize`). Die bestehende Einstellung `UnreadSortOrder` bleibt unverändert; ihr Zusammenspiel mit der Tageweise-Anzeige ist unter Offene Fragen geklärt bzw. zu entscheiden.

## Offene Fragen

1. **Absteigende Sortierung:** Die Anforderung setzt aufsteigende Sortierung voraus. Gilt die Tagespaket-Logik nur bei `UnreadSortOrder == SortOrderAscending`, oder soll sie bei absteigender Sortierung analog vom neuesten Tag ausgehen — oder bleibt absteigend unverändert ohne Tagesbegrenzung?
2. **Nachladen am Paketende:** Wenn ein Tag mehr Artikel als `PageSize` (20) enthält, lädt `LoadMoreCommand` vermutlich die restlichen Artikel desselben Tages nach (Annahme). Soll nach dem letzten Artikel des Tages automatisch der Folgetag anschließen, oder endet die Liste am Paketende bis zum nächsten Reload/„Alles gelesen"?
3. **`PublishedAt == null`:** Welchem Tag werden ungelesene Artikel ohne Veröffentlichungsdatum zugeordnet (ältester Tag, heutiger Tag, eigener Fall)?
4. **Umfang von „Alles gelesen" bei erweitertem Paket:** Wenn wegen der <5-Regel der Folgetag mitgeladen wurde, markiert der Doppelhaken nur den ersten („aktuell zu ladenden") Tag oder das gesamte sichtbare Paket? Wortlaut der Anforderung („nur die Artikel des aktuell zu ladenden Tages") deutet auf den ersten Tag; die Nutzererwartung könnte auch das sichtbare Paket sein.
5. **Kalendertags-Definition:** Tagesgrenze in lokaler Gerätezeit (Annahme) vs. UTC — `PublishedAt` wird als `DateTime` gespeichert; die Umrechnung ist festzulegen.
6. **Anzeige des Paket-Tags:** Soll der Nutzer erkennen können, welcher Tag gerade geladen ist (z. B. im Bestätigungsdialog des Doppelhakens oder in der Statuszeile)? Die Anforderung verlangt keine sichtbare Tagesanzeige — Klärung empfohlen, damit der eingeschränkte „Alles gelesen"-Umfang nicht überrascht.
