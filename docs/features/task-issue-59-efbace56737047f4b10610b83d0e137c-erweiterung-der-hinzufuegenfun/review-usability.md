<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Keine Befunde

## Befunde

Keine.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- **Domain oder vollständige URL eingeben und Suche auslösen** → unauffällig. Das Eingabefeld ist mit dem Placeholder „Feed-URL oder Website-Adresse…" klar beschriftet; die Suche ist über den sichtbaren Button „Suchen" und die Eingabetaste (`ReturnCommand`) erreichbar. Es muss keine interne Kennung (Id, GUID, Dateiname) eingegeben oder gewusst werden — nur die öffentliche Adresse der Website, die die Anforderung vorsieht.
- **Treffer als scrollbare Kartenliste ansehen** → unauffällig. `CollectionView` mit Karten (Titel, Beschreibung, Site-Name/URL, Feed-URL in Klartext), leere Liste zeigt „Keine Feeds gefunden." Screenshot `manual-04`/`manual-09`/`manual-11` bestätigt die Darstellung in Dark und Light Mode.
- **Treffer auswählen und abonnieren** → unauffällig. Tap auf die Karte öffnet den Bestätigungsdialog „Feed abonnieren? „{Titel}" abonnieren?" mit Ja/Nein (`OnSearchResultTapped`, `FeedsPage.xaml.cs:87-105`). Anzeigename ist Titel oder Feed-URL — kein technischer Schlüssel. Die Auswahl erfolgt aus benannten Treffern, nicht über eine zu erratende Kennung. Das Tap-auf-Karte-Muster entspricht exakt dem bestehenden Feed-Listen-Muster der App (`OnFeedTapped` → `DisplayActionSheetAsync`), es wurde kein abweichendes primitives Muster erfunden.
- **Direkte URL-Hinzufügung als Fallback** → unauffällig. Bei leerem Trefferergebnis oder nicht erreichbarer Suche mit gültiger URL erscheint der Dialog „Kein Feed gefunden. Möchtest du die Adresse „{0}" direkt hinzufügen?" (Screenshot `manual-03`/`manual-12`). Nach „Ja" steht die URL im Formular und der Titel wird mit dem Host vorbefüllt (`OfferDirectAddAsync`, `FeedsViewModel.Search.cs:210-236`) — die Anwenderin muss den Titel nicht selbst erfinden. Der bisherige „Speichern"-Pfad bleibt mit verständlichen, lokalisierten Validierungsmeldungen („Bitte gib eine gültige Feed-URL ein.", „Bitte gib einen Anzeigetitel ein.") erhalten.
- **Kategorie beim Abonnieren/Hinzufügen zuordnen** → unauffällig. `Picker` mit Klartext-Kategorienamen (`ItemDisplayBinding="{Binding Name}"`), Default „—" (Keine Kategorie). Keine Id-Eingabe.
- **Offline-Verhalten** → unauffällig. `SearchCommand` ist offline deaktiviert (`CanExecute: IsOnline && !IsSearching`); zusätzlich erscheint der Hinweis „Feed-Suche offline nicht verfügbar — URL direkt hinzufügen bleibt möglich." Die Direkt-Hinzufügung über „Speichern" bleibt möglich.
- **Trefferliste verlassen** → unauffällig. Deutlich beschrifteter Button „Zurück zu meinen Feeds" mit Mindesthöhe 44 pt.
- **Freitext-/ungültige Eingabe** → unauffällig im Sinne der Anforderung: landet bewusst (Scope-Entscheidung 1) in der Ansicht „Keine Feeds gefunden"; das Formular validiert weiterhin mit klarer Fehlermeldung. Es wird keine technische Vorbedingung stillschweigend vorausgesetzt.
- **Fehler- und Statusmeldungen** → unauffällig. Alle nutzersichtbaren Texte sind lokalisiert (de + en) und in Laiensprache formuliert: „Die Feed-Suche ist nicht erreichbar. Du kannst die URL direkt hinzufügen.", „Ein Feed mit dieser URL existiert bereits." (Dublettenprüfung auch beim Abonnieren). Keine rohen Exception- oder englischen Technikertexte.
- **Attribution** → unauffällig. „Suche powered by feedsearch.dev" wird unter der Trefferliste angezeigt (Nutzungsbedingung der API erfüllt, für die Bedienung unerheblich).

Zusätzlich verifiziert anhand der manuellen Screenshots unter `test-results/issue-59/manual-*.png` (390 × 844 pt, Dark und Light Mode): Formular, Trefferliste, Abonnieren-Dialog, Direkt-Hinzufügen-Dialog und Fehlerhinweis entsprechen dem gebauten XAML; Karten-Layout statt Tabellen, Touch-Targets ≥ 44 pt, `AppThemeBinding` durchgehend.

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:

- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs`
- `src/Reporter.Core/Resources/Strings/AppResources.resx`
- `src/Reporter.Core/Resources/Strings/AppResources.de.resx`
- `test-results/issue-59/manual-*.png` (Nachweis der tatsächlich gebauten Oberfläche)
