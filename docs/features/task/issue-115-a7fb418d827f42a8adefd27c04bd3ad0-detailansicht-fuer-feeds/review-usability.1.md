<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### FeedsPage.xaml (Feed-Karte in der Übersicht)

- **Erreichbarkeit** — Die Feed-Karte trägt weiterhin den Screenreader-Hinweis `SemanticProperties.Hint="{x:Static strings:AppResources.AccessibilityTapForActions}"` („Tippen für Aktionen" / „Double-tap for actions", `FeedsPage.xaml` Zeile 153). Ein Tap öffnet jetzt jedoch die Feed-Detailansicht (`FeedsPage.xaml.cs` `OnFeedTapped` → `feeddetail?feedId=…`) und kein Aktionsblatt mehr. Eine nicht-technische Anwenderin mit Screenreader erhält eine falsche Ansage: Ihr werden „Aktionen" versprochen, tatsächlich navigiert die App auf eine andere Seite. Die Aktionen selbst sind dort zwar über den klar beschrifteten Button „Aktionen" erreichbar, die angekündigte Interaktion stimmt aber nicht mit dem tatsächlichen Verhalten überein.

  Empfehlung: Für die Feed-Karte einen eigenen Resource-Schlüssel mit korrektem Hinweis einführen, z. B. `AccessibilityOpenFeedDetails` („Tippen öffnet die Feed-Details" / „Double-tap to open feed details"). Den bestehenden Schlüssel `AccessibilityTapForActions` nicht umbenennen oder umtexten — er wird an Stellen verwendet, an denen die Ansage weiterhin korrekt ist (`CategoriesPage.xaml` Zeile 49, Suchergebnis-Karte `FeedsPage.xaml` Zeile 79).

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- Feed-Karte in `FeedsPage` antippen → Feed-Detailansicht öffnet sich statt Aktionsblatt → Befund vorhanden (veraltete Screenreader-Ansage, siehe oben; die sichtbare Bedienung selbst ist unauffällig — Tap auf benannte Karte, keine Kennung nötig)
- Alle Beiträge des Feeds (gelesene wie ungelesene) absteigend nach Datum in einer nachladenden Liste durchsehen → unauffällig (`CollectionView` mit `RemainingItemsThresholdReachedCommand`, `PageSize = 20`, `GetByFeedAsync` mit `OrderByDescending(PublishedAt)`)
- Ältere Beiträge über eine Suche wiederfinden → unauffällig (`SearchBar` mit lokalisiertem Platzhalter „Artikel in diesem Feed suchen…", filtert titelbasiert über die Repository-Abfrage und setzt das Paging zurück)
- Feed aktualisieren → unauffällig (Eintrag „Aktualisieren" im Aktionsblatt hinter dem „Aktionen"-Button; zusätzlich Pull-to-Refresh über `RefreshView`)
- Feed umbenennen → unauffällig (Eintrag „Umbenennen" öffnet `DisplayPromptAsync` mit vorbefülltem Klartext-Titel, keine Id-Eingabe)
- Kategorie ändern → unauffällig (Auswahl über Aktionsblatt mit Kategorie-Namen in Klartext inkl. „Keine Kategorie"-Eintrag; kollisionsfreie Labels bei Namensgleichheit)
- Feed bearbeiten → unauffällig (eigenes Bottom Sheet „Feed bearbeiten" mit URL-Feld und Benachrichtigungs-Schalter, „Speichern"/„Abbrechen"; die Feed-URL ist ein dem Anwender bekannter Wert, keine interne Kennung)
- Fehlerdetails anzeigen → unauffällig (Eintrag erscheint nur bei `HealthStatus == Error`, zeigt lokalisierte Fehlerkategorie plus gespeicherte Rohmeldung)
- Feed löschen → unauffällig (Eintrag „Löschen" mit Ja/Nein-Bestätigung, danach Rücknavigation zur Übersicht)
- Beitragskarte antippen → Artikeldetail → unauffällig (wiederverwendete `ArticleCardView` mit Standard-`OpenArticleCommand` → `articledetail?itemId=…`; `MarkReadCommand`/`ToggleSavedCommand` angebunden)
- Zurück zur Feed-Übersicht → unauffällig (eigener Zurück-Button mit 44 × 44 pt Touch-Target und `AccessibilityBack`-Beschreibung, da `Shell.NavBarIsVisible="False"`)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:

- `src/Reporter/Views/FeedDetailPage.xaml` (neu)
- `src/Reporter/Views/FeedDetailPage.xaml.cs` (neu)
- `src/Reporter/Views/FeedsPage.xaml` (geändert)
- `src/Reporter/Views/FeedsPage.xaml.cs` (geändert)
- `src/Reporter.Core/ViewModels/FeedDetailViewModel.cs` (neu)
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs` (geändert)
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs` (geändert)
- `src/Reporter.Core/Resources/Strings/AppResources.resx` (geändert)
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx` (geändert)
- `src/Reporter/AppShell.xaml.cs` (geändert, Route `feeddetail`)
- `src/Reporter/MauiProgram.cs` (geändert, DI-Registrierung)
- `src/Reporter/Views/ArticleCardView.xaml.cs` (wiederverwendete Komponente, unverändert)
