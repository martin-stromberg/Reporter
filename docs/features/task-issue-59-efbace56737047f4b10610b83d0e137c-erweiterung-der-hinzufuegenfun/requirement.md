<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Übersetzte Anforderung: Unaufdringliches Hinzufügen-Formular, Dateiname als Titel-Fallback, Kontextmenü-Aktionen „Umbenennen“ und „Kategorie ändern“

> **Kundenanforderung (sinngemäß):** Das Hinzufügen-Formular auf der Feeds-Seite soll nicht mehr dauerhaft sichtbar sein. Die Listenansicht zeigt standardmäßig nur die Feed-Liste; ein „+"-Button öffnet das Formular. Das Formular enthält nur noch das URL-Eingabefeld und den „Suchen"-Button; Suchergebnisse werden gelistet und wie bisher hinzugefügt. Feeds ohne Titel bekommen den Dateinamen der Feed-URL (z. B. `heise-atom.xml`) als Namen. Das Feed-Kontextmenü erhält zwei neue Aktionen „Umbenennen" und „Kategorie ändern", die jeweils einen kleinen Dialog öffnen.

## Fachliche Zusammenfassung

Die `FeedsPage` wird von einer kombinierten Formular-plus-Liste-Seite zu einer reinen Listenansicht umgebaut: Das Hinzufügen-Formular (aktuell permanent sichtbare `Border`-Karte oberhalb der `CollectionView`) wird hinter einem „+"-Button verborgen und nur auf Bedarf geöffnet (Design-Entwurf sieht ein Bottom-Sheet-Modal `modal-add-feed` vor). Das Formular wird auf `Entry` (`NewUrl`) + „Suchen"-Button (`SearchCommand`) reduziert — das Titel-Eingabefeld (`NewTitle`) entfällt; Feeds ohne bekannten Titel erhalten als Platzhalter den Dateinamen (letztes Pfadsegment) der Feed-URL statt wie bisher die volle URL bzw. einen manuell eingegebenen Titel. Das Feed-Kontextmenü (`OnFeedTapped`, `DisplayActionSheetAsync`) wird um die Aktionen „Umbenennen" und „Kategorie ändern" erweitert, die jeweils einen kleinen Dialog zum Ändern von `Feed.Title` bzw. `Feed.CategoryId` öffnen.

## Betroffene Klassen und Komponenten

- **UI — `FeedsPage.xaml` (`src/Reporter/Views`):**
  - Die Formular-`Border`-Karte (Zeilen 17–79) wird nicht mehr standardmäßig sichtbar gerendert; stattdessen öffnet ein „+"-Button das Formular als Bottom-Sheet-Overlay entsprechend `design-draft/stitch_local_rss_feed_reader/feeds_health_status/code.html` (`btn-open-add-feed` → `modal-add-feed`) — durch Plan-Designentscheidung festgelegt (siehe Geklärte Punkte / Entscheidungen).
  - Im Formular verbleiben: `Entry` mit `Text="{Binding NewUrl}"` + `ReturnCommand="{Binding SearchCommand}"`, `Button` „Suchen" (`SearchCommand`), `ActivityIndicator` (`IsSearching`) sowie die Such-/Offline-/Fehlerhinweise (`SearchErrorMessage`, `FeedSearchOfflineHint`).
  - Entfallen aus dem Formular: `Entry` `NewTitle`, `Picker` `SelectedCategory`/`Categories`, `Switch` `FeedNotificationsEnabled`, `Button` „Speichern" (`SaveCommand`) — laut Anforderung enthält das Formular nur noch URL-Feld und „Suchen" (geklärt: Kategorie-Picker und Benachrichtigungen-Switch entfallen vollständig; Defaults `CategoryId = null`, `NotificationsEnabled = true`).
  - Position des „+"-Buttons: primäre Schaltfläche „+ Feed per URL hinzufügen" voller Breite über der Liste (durch Plan-Designentscheidung entsprechend dem Design-Entwurf festgelegt, kein Header-Icon).
  - Treffer-`CollectionView` (`SearchResults`, `ShowSearchResults`) und „Zurück zu meinen Feeds" (`CloseSearchResultsCommand`) bleiben funktional erhalten; Platzierung geklärt: Die Trefferansicht ersetzt weiterhin die Feed-Liste auf der Seite, das Sheet schließt sich automatisch, sobald Treffer angezeigt werden.
- **UI — `FeedsPage.xaml.cs` (`src/Reporter/Views`):**
  - `OnFeedTapped`: `DisplayActionSheetAsync` um zwei Einträge erweitern (`AppResources.ButtonRename`, `AppResources.ButtonChangeCategory` — neue Ressourcenschlüssel).
  - Neuer Dialog „Umbenennen": kleiner Eingabedialog für den Titel via `DisplayPromptAsync` (im Codebestand bislang nicht verwendet, neues Pattern — durch Plan-Designentscheidung festgelegt) mit Vorbelegung des aktuellen Titels; Routing an eine neue ViewModel-Methode.
  - Neuer Dialog „Kategorie ändern": kleiner Auswahldialog über `Categories` (inkl. `CategoryNone`-Pseudo-Eintrag `Guid.Empty` → `CategoryId = null`); Umsetzung als `DisplayActionSheetAsync` mit Kategorienamen (geklärt — Skalierungsrisiko bei vielen Kategorien akzeptiert).
  - `ConfirmDirectAddAsync`-Callback: Verhalten ändert sich — ohne Titel-Feld/Speichern-Button wird die bestätigte URL direkt mit Dateinamen-Titel persistiert statt nur ins Formular zurückgeladen (geklärt: „Ja" persistiert sofort).
- **ViewModel — `FeedsViewModel.cs` / `FeedsViewModel.Search.cs` (`src/Reporter.Core/ViewModels`):**
  - Neuer Sichtbarkeitszustand für das Hinzufügen-Formular (z. B. `ShowAddForm`/`IsAddFormVisible`) plus `OpenAddFormCommand`/`CloseAddFormCommand` für den „+"-Button.
  - Neues Command für „Umbenennen", z. B. `RenameFeedCommand`/`RenameAsync(FeedListItem, string newTitle)` → `_feedRepository.UpdateAsync(...)` mit geändertem `Title`, danach `LoadAsync`.
  - Neues Command für „Kategorie ändern", z. B. `ChangeFeedCategoryCommand`/`ChangeCategoryAsync(FeedListItem, Guid? categoryId)` → `_feedRepository.UpdateAsync(...)` mit geändertem `CategoryId`, danach `LoadAsync`.
  - `SubscribeResultAsync`: Titel-Fallback ändern von `result.FeedUrl` auf den Dateinamen (letztes Pfadsegment) der `FeedUrl`; bei `result.Title` weiterhin der Treffer-Titel.
  - Direkt-Hinzufügen-Pfad (`OfferDirectAddAsync`): bei Bestätigung wird der Titel aus dem Dateinamen der URL abgeleitet (Fallback-Kette: letztes Pfadsegment → Host → URL) und der Feed sofort persistiert (geklärt); die `ErrorFeedTitleEmpty`-Validierung entfällt auf diesem Pfad.
  - `EditCommand`/`EditAsync`/`SelectedFeed`: geklärt — „Bearbeiten" bleibt im Kontextmenü und öffnet dasselbe Bottom-Sheet im Edit-Modus (URL-`Entry`, `NotificationsEnabled`-`Switch`, „Speichern"); „Umbenennen" und „Kategorie ändern" decken Titel und Kategorie separat ab.
- **Logik — `FeedSyncService` (`src/Reporter.Core/Services`):**
  - Platzhalter-Erkennung (`IsNullOrWhiteSpace`, `== feed.Url`, `IsHostPlaceholderTitle`, Zeilen 186–219) um den Dateinamen-Platzhalter erweitern, damit `SyndicationFeed.Title` beim ersten Sync weiterhin einen automatisch vergebenen Namen ersetzt (geklärt — der Dateinamen-Titel gilt als Platzhalter und wird durch den echten Feed-Titel ersetzt).
- **Datenmodell:** Keine Änderungen — `Feed` (`Title`, `CategoryId`) und `FeedListItem` (`Title`, `CategoryId`, `CategoryName`) enthalten bereits alles Nötige; `IFeedRepository.UpdateAsync` existiert. Keine Migration erforderlich.
- **Ressourcen — `AppResources.resx` + `AppResources.de.resx` (`src/Reporter.Core/Resources/Strings`):** Neue Schlüssel u. a. für „+"-Button/`ActionAddFeed` (bzw. Beschriftung laut Design „Feed per URL hinzufügen"), `ButtonRename`, `ButtonChangeCategory`, Dialogtitel/-texte für Umbenennen (mit Vorbelegung des aktuellen Titels) und Kategorie ändern, ggf. `FeedAddSheetTitle`; Designer-Datei regenerieren.
- **Tests — `src/Reporter.Tests`:**
  - `FeedsViewModelTests`: neue Tests für Formular-Sichtbarkeit, Dateinamen-Titel-Fallback (Suche-Treffer ohne Titel und Direkt-Hinzufügen), `RenameFeedCommand` (Validierung: leerer Titel → Fehler), `ChangeFeedCategoryCommand` (inkl. `Guid.Empty` → `null`), Dubletten-URL-Prüfung beim Umbenennen entfällt (nur Titel betroffen).
  - `FeedSyncServiceTests`: Dateinamen-Platzhalter wird beim ersten Sync durch `SyndicationFeed.Title` ersetzt; manuell vergebener Titel bleibt unverändert.
  - Bestehende Tests zu `SaveAsync`/`EditAsync` sind an den reduzierten Formular-Umfang anzupassen.

## Implementierungsansatz

- **Erweiterung statt Neubau:** `FeedsPage`/`FeedsViewModel` werden erweitert; der Such-Flow (`SearchCommand`, `SearchAsync`, `IFeedSearchService`, `SubscribeResultCommand`, Treffer-UI mit `OnSearchResultTapped` + `DisplayAlertAsync`-Bestätigung) bleibt unverändert und wird lediglich in den geöffneten Formular-Kontext verlegt.
- **Formular-Sichtbarkeit:** Neues boolsches ViewModel-Flag steuert per `DataTrigger` die Sichtbarkeit der Formular-Karte bzw. des Bottom-Sheets; beim Öffnen `NewUrl`-Feld fokussieren, beim Schließen `ResetForm`. Der `NewUrl`-Setter leert weiterhin den Suchzustand.
- **Dateiname als Titel:** Ableitung aus `Uri.AbsolutePath` — letztes nicht-leeres Pfadsegment (URL-dekodiert), z. B. `https://heise.de/rss/heise-atom.xml` → `heise-atom.xml`; ohne Pfadsegment Fallback auf `Host` (deckt sich mit der bestehenden `IsHostPlaceholderTitle`-Erkennung). Zentrale Hilfsmethode (z. B. `TryGetFileNameTitle`/`GetTitleFallback`) im ViewModel oder als `static` Helper, damit `SubscribeResultAsync` und der Direkt-Hinzufügen-Pfad dieselbe Regel nutzen.
- **Kontextmenü:** `OnFeedTapped` bekommt zwei zusätzliche `DisplayActionSheetAsync`-Einträge. Beide Dialoge laufen über Code-Behind (UI-Abhängigkeit wie bisher bei `ConfirmDirectAddAsync`), das ViewModel erhält nur schlanke Commands (`FeedListItem`-Parameter). „Umbenennen" belegt das Eingabefeld mit dem aktuellen `Title` vor; „Kategorie ändern" nutzt die bereits geladene `Categories`-Liste inklusive `CategoryNone`-Eintrag.
- **Abhängigkeiten:** Keine neuen Services/Interfaces nötig — `IFeedRepository.UpdateAsync`, `ICategoryRepository.GetAllAsync` (bereits via `LoadAsync` gefüllt) und `FeedSyncService` genügen. Einzig neue UI-Patterns (`DisplayPromptAsync` bzw. eigener Dialog) kommen hinzu.
- **Relevante Hooks:** `LoadAsync` liefert `Categories` + `Feeds`; `ResetForm` muss den neuen Formular-Sichtbarkeitszustand berücksichtigen; `OnConnectivityChanged` bleibt für `SearchCommand.CanExecute` relevant.
- **Mobile-UI-Regeln (AGENTS.md):** „+"-Button und Dialoge ≥ 44 × 44 pt Touch-Ziele, `AppThemeBinding` für Dark Mode, keine verschachtelten `CollectionView`/`ScrollView`, Vergleich mit `design-draft/.../feeds_health_status/screen.png` und dokumentierte manuelle UI-Verifikation (390 × 844 pt).

## Konfiguration

Keine Konfiguration erforderlich — die Anforderung enthält keinen konfigurierbaren Aspekt; Verhalten ist fest verdrahtet (Standard bei Neuanlage: `NotificationsEnabled = true`, `CategoryId = null`).

## Geklärte Punkte / Entscheidungen

Vom Anwender bestätigt (Antworten auf die zuvor offenen Fragen):

1. **Bestand des Formulars:** Kategorie-Picker und Benachrichtigungen-Switch entfallen im Hinzufügen-Formular **vollständig**. Neue Feeds starten mit den Defaults `CategoryId = null` und `NotificationsEnabled = true`; Kategorie ist über „Kategorie ändern" nachträglich setzbar, Benachrichtigungen über „Bearbeiten". Die optionale Kategorie-Auswahl des Design-Entwurfs (`modal-add-feed`) wird nicht umgesetzt.
2. **„Bearbeiten"-Aktion:** Der Kontextmenü-Eintrag „Bearbeiten" bleibt bestehen — als Edit-Modus desselben Bottom-Sheets (URL-`Entry` + `NotificationsEnabled`-`Switch` + „Speichern"). Damit bleiben `Url`- und `NotificationsEnabled`-Änderungen erreichbar; Titel und Kategorie laufen über „Umbenennen" bzw. „Kategorie ändern".
3. **Direkt-Hinzufügen ohne Speichern-Button:** Bei „Ja" im „URL direkt hinzufügen?"-Dialog (`ConfirmDirectAddAsync`) wird der Feed **sofort** mit Dateinamen-Titel persistiert — ohne Zwischenschritt ins Formular und ohne „Speichern"-Button. Inkl. Dublettenprüfung (`ErrorFeedDuplicate`).
4. **Dateinamen-Platzhalter vs. Sync-Auflösung:** Der Dateinamen-Titel gilt als **Platzhalter** und wird beim ersten Sync durch den echten `SyndicationFeed.Title` ersetzt — die `FeedSyncService`-Platzhalter-Erkennung wird um den Dateinamen-Fall erweitert. Klarstellung: Suchtreffer bringen normalerweise ihren Titel mit; der Dateiname greift nur bei titellosen Treffern und beim Direkt-Add.
5. **„Kategorie ändern"-Dialog bei vielen Kategorien:** Umsetzung via `DisplayActionSheetAsync` über `Categories` (inkl. `CategoryNone`-Pseudo-Eintrag) — das Skalierungsrisiko bei sehr vielen Kategorien wird akzeptiert.

Durch Plan-Designentscheidungen festgelegt (keine Anwenderklärung erforderlich):

6. **Umsetzung des „+"-Formulars:** In-Page-Bottom-Sheet-Overlay auf `FeedsPage` (Backdrop + unten angedockte Karte), kein eigenes Modal/kein neues NuGet-Paket. Die Trefferliste bleibt ein Seitenbereich (ersetzt weiterhin die Feed-Liste); das Sheet schließt sich automatisch, sobald Treffer angezeigt werden.
7. **Position/Gestalt des „+"-Buttons:** Primär-Button voller Breite über der Liste („+ Feed per URL hinzufügen"), entsprechend dem Design-Entwurf — kein rundes Header-Icon.
