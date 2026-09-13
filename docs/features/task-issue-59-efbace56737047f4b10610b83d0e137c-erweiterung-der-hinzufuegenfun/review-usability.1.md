<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### `src/Reporter/Views/FeedsPage.xaml` + `FeedsViewModel.cs` / `FeedsViewModel.Search.cs` (Feeds-Seite, Hinzufügen-Sheet)

- **Erreichbarkeit** — Der Offline-Hinweis im Hinzufügen-Sheet (`FeedSearchOfflineHint`: „Feed-Suche offline nicht verfügbar — URL direkt hinzufügen bleibt möglich.", FeedsPage.xaml Zeilen 264–273) verspricht eine Aktion, die offline nicht erreichbar ist. Das Sheet enthält nur noch das URL-Feld und den „Suchen"-Button; `SearchCommand` ist jedoch mit `CanExecute = IsOnline && !IsSearching` (FeedsViewModel.cs Zeile 66) offline deaktiviert, und der Direkt-Hinzufügen-Dialog (`OfferDirectAddAsync`, FeedsViewModel.Search.cs) wird ausschließlich aus `SearchAsync` heraus ausgelöst. Eine nicht-technische Anwenderin, die offline eine Feed-URL eintippt, sieht also einen Hinweis „direkt hinzufügen bleibt möglich", findet aber keine Möglichkeit dazu — der einzige Button ist abgeblendet. Die in der Anforderung geforderte Aufgabe „Feed per URL hinzufügen" ist offline nicht erledigbar, obwohl die Oberfläche das Gegenteil behauptet.

  Empfehlung: Entweder im Sheet eine zweite, offline aktive Schaltfläche „URL direkt hinzufügen" anbieten (ruft den bestehenden `ConfirmDirectAddAsync`-/Persist-Pfad ohne Suche auf) oder den Hinweistext offline korrigieren, z. B. „Feed-Suche offline nicht verfügbar — Hinzufügen ist erst wieder mit Internetverbindung möglich."

### `src/Reporter/Views/FeedsPage.xaml.cs` (Kontextmenü „Kategorie ändern")

- **Erreichbarkeit** — Im Auswahldialog „Kategorie ändern" (`OnFeedTapped`, Zeilen 85–99) erscheint der Eintrag zum Entfernen der Kategorie nur als „—" (Pseudo-Eintrag `CategoryNone`, de-Text `—`). Inmitten echter Kategorienamen ist für eine Laiin nicht erkennbar, dass ein Gedankenstrich „keine Kategorie" bedeutet — die Aktion „Kategorie entfernen" ist faktisch unbeschriftet. Betroffen ist die Anforderungs-Interaktion „Kategorie ändern" inkl. Zurücksetzen auf keine Kategorie (`Guid.Empty` → `CategoryId = null`).

  Empfehlung: Dem Pseudo-Eintrag eine sprechende Beschriftung geben, z. B. eigene Ressource „Keine Kategorie" (analog `CategoryNone`, aber mit Klartext statt `—`), damit der Entfernen-Eintrag im Action-Sheet verständlich ist.

### `src/Reporter/Views/FeedsPage.xaml` (Hinzufügen-/Bearbeiten-Sheet)

- **Erreichbarkeit** — Im Edit-Modus („Feed bearbeiten", geöffnet über Kontextmenü „Bearbeiten") bleibt der „Suchen"-Button sichtbar (FeedsPage.xaml Zeilen 282–283 — außerhalb des `IsEditMode`-Triggerteils). Tippt eine Anwenderin nach dem Ändern der URL auf „Suchen" statt „Speichern", schließt sich das Sheet beim Anzeigen der Treffer kommentarlos (`ShowAddForm = false` in `SearchAsync`) und die begonnene Bearbeitung wird ohne Warnung verworfen — „Suchen" ist im Kontext „Feed bearbeiten" zudem semantisch unklar (die Anforderung nennt für den Edit-Modus nur URL-Feld, Benachrichtigungen-Schalter und „Speichern").

  Empfehlung: Den „Suchen"-Button im `IsEditMode` ausblenden (DataTrigger analog zum Benachrichtigungen-Block), damit im Bearbeiten-Dialog nur „Speichern" und „Abbrechen" angeboten werden.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:
- „+"-Button öffnet das Hinzufügen-Formular → unauffällig (vollflächiger Button „+ Feed per URL hinzufügen" oberhalb der Liste, klar beschriftet, ≥ 44 pt; Sheet mit Titel „Feed per URL hinzufügen" und „Abbrechen")
- URL eingeben und „Suchen" auslösen → unauffällig (verständlicher Platzhalter „Feed-URL oder Website-Adresse…", Return-Taste und Button, kein technischer Wert erforderlich)
- Suchtreffer auswählen und abonnieren → unauffällig (tappbare Karte mit Titel/Beschreibung/URL, Bestätigungsdialog mit Klartextname)
- „Zurück zu meinen Feeds" aus der Trefferansicht → unauffällig
- Direkt-Hinzufügen bei erfolgloser Suche / nicht erreichbarer Suche (online) → unauffällig (verständlicher Ja/Nein-Dialog mit der eingegebenen Adresse; Dubletten-Fehlermeldung im Sheet)
- Direkt-Hinzufügen offline → Befund (Hinweis verspricht Aktion, die offline nicht erreichbar ist)
- Feed-Kontextmenü „Umbenennen" → unauffällig (Eingabedialog „Feed umbenennen / Neuer Anzeigetitel", aktueller Titel vorbelegt, OK/Abbrechen)
- Feed-Kontextmenü „Kategorie ändern" → Befund (Auswahl benannter Kategorien ok; Entfernen-Eintrag nur als „—" erkennbar)
- Feed-Kontextmenü „Bearbeiten" → Befund (Sheet im Edit-Modus mit Titel „Feed bearbeiten", Switch + „Speichern" ok; zusätzlich sichtbarer „Suchen"-Button verwirft Bearbeitung lautlos)
- Feeds ohne Titel erhalten Dateinamen als Namen → unauffällig (automatisch, keine Nutzereingabe nötig; Dateiname statt technischer ID/URL als Anzeigename)

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:
- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`

Hinweis: Die 18 Verifikations-Screenshots unter `test-results/issue-59/manual-2-*.png` und `manual-3-*.png` belegen die tatsächlich gebaute Oberfläche (Bottom-Sheet, Kontextmenü, Umbenennen-/Kategorie-Dialoge); die Befunde wurden primär anhand von XAML, Code-Behind und ViewModel geprüft.
