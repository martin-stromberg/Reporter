<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Usability-Review

## Ergebnis

**Status:** Keine Befunde

## Befunde

Keine.

## Geprüfte Interaktionen

Liste der aus der Anforderung geprüften Benutzerinteraktionen:

- Hinzufügen-Formular über „+"-Button öffnen → unauffällig (primärer, voller Breite beschrifteter Button „+ Feed per URL hinzufügen" über der Liste, `FeedsPage.xaml:17`; öffnet Bottom-Sheet mit Titel „Feed per URL hinzufügen", `FeedsPage.xaml:228-261`)
- Feed-URL/Website-Adresse eingeben und suchen → unauffällig (`Entry` mit Platzhalter „Feed-URL oder Website-Adresse…", URL-Tastatur, Return-Taste und „Suchen"-Button, `FeedsPage.xaml:283-299`; offline ist „Suchen" deaktiviert und ein Klartext-Hinweis erklärt die Alternative „URL direkt hinzufügen")
- Suchergebnis aus der Trefferliste auswählen und hinzufügen → unauffällig (Treffer als Karten mit Titel/Beschreibung/Seitenname; Tipp auf die Karte öffnet Bestätigungsdialog „Feed abonnieren? / „X“ abonnieren?" mit dem Treffertitel bzw. bei fehlendem Titel der Feed-URL — identisch zur Karten-Darstellung, `FeedsPage.xaml.cs:185-203`)
- „Zurück zu meinen Feeds" aus der Trefferansicht → unauffällig (beschrifteter Button unter der Trefferliste, `FeedsPage.xaml:129-133`)
- Titelloser Feed erhält Dateinamen als Namen → unauffällig (vollautomatisch über `FeedTitleFallback.GetFallbackTitle`: letztes Pfadsegment → Host → URL; kein Anwendereingriff nötig, `FeedsViewModel.Search.cs:351-353`, `FeedTitleFallback.cs:17-30`)
- Kontextmenü eines Feeds öffnen → unauffällig (Tipp auf Feed-Karte öffnet ActionSheet „Feed-Aktionen" mit klartextlichen Einträgen „Aktualisieren", „Umbenennen", „Kategorie ändern", „Bearbeiten", „Löschen", `FeedsPage.xaml.cs:63-71` — gleiches Muster wie bisher)
- „Umbenennen" → unauffällig (`DisplayPromptAsync` „Feed umbenennen / Neuer Anzeigetitel" mit Vorbelegung des aktuellen Titels, `FeedsPage.xaml.cs:101-114`; leerer Titel → verständliche Fehlermeldung „Bitte gib einen Anzeigetitel ein." über der Liste)
- „Kategorie ändern" → unauffällig (ActionSheet mit Kategorienamen inkl. „Keine Kategorie"-Eintrag zum Entfernen der Zuordnung; gleichnamige Kategorien werden mit Zählsuffix „Name (2)" eindeutig, `FeedsPage.xaml.cs:122-157`; Skalierungsrisiko bei sehr vielen Kategorien ist in der Anforderung explizit akzeptiert)
- „Bearbeiten" (URL/Benachrichtigungen ändern) → unauffällig (gleiches Bottom-Sheet im Edit-Modus mit Titel „Feed bearbeiten", URL-Feld, beschriftetem Benachrichtigungen-Schalter samt Hinweistext und „Speichern"; der für den Add-Modus gedachte Offline-Suchhinweis ist im Edit-Modus ausgeblendet, `FeedsPage.xaml:270-354`)
- „URL direkt hinzufügen" bzw. Direkt-Hinzufügen nach erfolgloser Suche bestätigen → unauffällig (Ja/Nein-Dialog „Kein Feed gefunden. Möchtest du die Adresse „X“ direkt hinzufügen?", `FeedsPage.xaml.cs:223-230`; bei Bestätigung wird sofort mit Dateinamen-Titel gespeichert, Dublette → „Ein Feed mit dieser URL existiert bereits.")

Zusätzlich geprüfte Aspekte:

- **Interne/technische Kennungen:** Keine — weder für Feeds noch für Kategorien muss eine Id, ein technischer Schlüssel oder Dateiname eingegeben oder gewusst werden; die einzige Eingabe ist eine URL bzw. frei wählbarer Titeltext.
- **Auswahl-/Identifikationsaufgaben:** Kategorien werden per Klartextname ausgewählt (ActionSheet); Feeds per Tipp auf die benannte Karte. Kein Roh-Id-Feld vorhanden.
- **Etablierte Muster:** Das Kontextmenü nutzt weiterhin das bestehende `DisplayActionSheet`-Muster der Seite; das Umbenennen-Eingabefeld (`DisplayPromptAsync`) ist ein plattformüblicher, verständlicher Dialog.
- **Erreichbarkeit/Beschriftung:** Alle geforderten Aktionen sind über sichtbare, deutsch beschriftete Bedienelemente erreichbar; Fehlermeldungen sind in Laiensprache formuliert; Touch-Ziele erfüllen ≥ 44 pt (`MinimumHeightRequest="44"` an Buttons/Karten, `FeedsPage.xaml:19,72,132,260,293,302,333-334,353`); Dark Mode über `AppThemeBinding` durchgängig.

## Geprüfte Dateien

Liste aller geprüften UI-Dateien:

- `src/Reporter/Views/FeedsPage.xaml`
- `src/Reporter/Views/FeedsPage.xaml.cs`
- `src/Reporter.Core/ViewModels/FeedsViewModel.cs` (UI-Zustände/Commands: `ShowAddForm`, `IsEditMode`, `OpenAddFormCommand`, `CloseAddFormCommand`, `RenameFeedAsync`, `ChangeFeedCategoryAsync`)
- `src/Reporter.Core/ViewModels/FeedsViewModel.Search.cs` (Such-/Treffer-/Direkt-Hinzufügen-Fluss inkl. Titel-Fallback)
- `src/Reporter.Core/Services/FeedTitleFallback.cs` (Ableitung des Dateinamen-Titels, der dem Anwender angezeigt wird)
- `src/Reporter.Core/Resources/Strings/AppResources.resx` / `AppResources.de.resx` (Beschriftungen und Dialogtexte)
- `src/Reporter.Core/Services/FeedSyncService.cs` (Platzhalter-Erkennung, damit der Dateinamen-Titel später durch den echten Feed-Titel ersetzt wird — anwendersichtbares Verhalten)
