<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Feeddetailansicht

Tippe auf der Seite **Feeds** auf eine Feed-Karte, um die Detailansicht des Feeds zu öffnen. Sie zeigt alle gespeicherten Artikel dieses Feeds — gelesene wie ungelesene — und bündelt alle Aktionen, die den Feed selbst betreffen.

## Kopfbereich

Oben stehen die Eckdaten des Feeds:

- **Feed-Symbol**: Das Favicon der Website; fehlt es oder ist das Gerät offline, erscheint ein Kreis mit dem Anfangsbuchstaben des Feeds.
- **Titel**: Der Anzeigetitel des Feeds — derselbe Titel steht auch in der Titelzeile des Fensters.
- **Meta-Zeile**: Kategorie, Datum und Uhrzeit des letzten Abrufs sowie die Zahl ungelesener Artikel (**Ungelesen**). Fehlt eine Angabe, steht dort ein Gedankenstrich.
- **Status-Badge**: Der Gesundheitsstatus des Feeds als farbige Pille mit Punkt und Text — **In Ordnung**, **Warnung** oder **Fehler** (Bedeutung siehe [Feeds synchronisieren](synchronisation.md)).
- **Zurück-Pfeil**: Oben links führt zurück zur Feed-Übersicht.
- **Aktionen**: Der Button oben rechts öffnet das Menü **Feed-Aktionen** (siehe unten). Er ist nur aktiv, wenn der Feed geladen werden konnte.

## Artikelliste

Unter dem Kopfbereich erscheinen alle Artikel des Feeds als Kartenliste — die neuesten zuerst, gelesene und ungelesene gemeinsam. Die Karten sehen aus wie auf den Seiten **Ungelesen** und **Später**: Titel, Teasertext, Bild, Lesezeit und die Aktionen **Lesezeichen** und **Als gelesen markieren**.

- **Nachladen beim Scrollen:** Beim Weiterscrollen lädt die Liste automatisch weitere, ältere Artikel nach — du musst nichts antippen. Ein Ladeindikator am unteren Rand zeigt laufende Ladevorgänge.
- **Ziehen zum Aktualisieren:** Ziehst du die Liste nach unten, ruft die App genau diesen Feed neu ab und lädt Kopfdaten und Liste anschließend neu.
- **Leere Liste:** Enthält der Feed noch keine Artikel — etwa weil er noch nie erfolgreich abgerufen wurde — steht in der Liste der Hinweis **„Hier erscheinen Artikel dieses Feeds."**

Ein Tipp auf eine Artikelkarte öffnet die [Artikeldetailansicht](artikeldetailansicht.md); über den Zurück-Pfeil dort gelangst du zur Feeddetailansicht zurück.

## Suche in den Artikeln des Feeds

Das Suchfeld **„Artikel in diesem Feed suchen…"** unter dem Kopfbereich durchsucht die Titel aller Artikel dieses Feeds. Die Liste filtert sich kurz nach der Eingabe automatisch — ein extra Tipp auf „Suchen" ist nicht nötig; das Löschen des Suchtextes zeigt wieder alle Artikel.

- Die Suche findet nur Artikel, die bereits synchronisiert und auf dem Gerät gespeichert sind — sie greift nicht auf die Website des Feeds zu.
- Durchsucht wird nur der Artikeltitel, nicht der Text des Artikels.
- Gibt es keine Treffer, steht in der Liste **„Keine Artikel zu diesem Suchbegriff gefunden."**

## Feed-Aktionen

Der Button **Aktionen** im Kopf öffnet das Menü **Feed-Aktionen**:

- **Aktualisieren** — ruft nur diesen Feed neu ab und lädt danach Kopfdaten und Liste neu.
- **Umbenennen** — öffnet den Dialog **Feed umbenennen**, in dem das Feld **Neuer Anzeigetitel** bereits den bisherigen Titel enthält. **OK** übernimmt den neuen Namen; ein leerer Titel wird mit dem Hinweis **„Bitte gib einen Anzeigetitel ein."** abgelehnt.
- **Kategorie ändern** — zeigt eine Auswahlliste aller Kategorien, an oberster Stelle **Keine Kategorie** zum Entfernen der Zuordnung. Kommt ein Kategoriename mehrfach vor, werden die Einträge nummeriert (z. B. „News", „News (2)"), damit jede Auswahl eindeutig ist.
- **Bearbeiten** — öffnet das Formular **Feed bearbeiten**, das sich von unten über die Seite schiebt: Hier änderst du die Feed-Adresse im Feld **Feed-URL** und den Schalter **Benachrichtigungen**; **Speichern** übernimmt die Werte, **Abbrechen**, ein Tipp auf den abgedunkelten Hintergrund oder die Zurück-Geste schließt ohne Änderung. Eine ungültige Adresse oder eine bereits von einem anderen Feed belegte Adresse wird im offenen Formular beanstandet. Auf Plattformen ohne Benachrichtigungsfunktion ist der Schalter deaktiviert und mit dem Hinweis **„Benachrichtigungen sind derzeit nur auf iOS verfügbar."** versehen.
- **Fehlerdetails anzeigen** — nur vorhanden, wenn der Feed den Status **Fehler** trägt; öffnet den Dialog **Synchronisierungsfehler** mit dem verständlichen Grund und der technischen Meldung des letzten fehlgeschlagenen Abrufs (Details siehe [Feeds synchronisieren](synchronisation.md)).
- **Löschen** — entfernt den Feed nach Rückfrage (**Feed löschen?**) inklusive aller zugehörigen Artikel und kehrt danach zur Feed-Übersicht zurück.

## Offline-Verhalten

Ohne Internetverbindung erscheint unter dem Kopfbereich das Hinweis-Banner **„Keine Internetverbindung."**:

- Alle gespeicherten Artikel bleiben lesbar, und die Titelsuche funktioniert weiterhin — sie durchsucht nur die lokale Ablage.
- **Aktualisieren** und das Ziehen zum Aktualisieren werden übersprungen — es entsteht weder ein Fehlereintrag noch ändert sich der Gesundheitsstatus.
- Das Feed-Symbol weicht auf den Kreis mit dem Anfangsbuchstaben aus; fehlende Artikelbilder bleiben wie auf den anderen Seiten ausgeblendet, lokal gespeicherte Bilder sichtbar.

Details siehe [Offline lesen](offline.md).

## Mobile UI-Prüfung

Die Feeddetailansicht ist für mobile Bildschirme optimiert und wurde am Windows-Handy-Fenster 390 × 844 pt in Light und Dark verifiziert:

- Die Artikelliste füllt den Seitenbereich und scrollt eigenständig; Kopfbereich und Suchfeld bleiben oben sichtbar.
- Zurück-Pfeil, **Aktionen**-Button und die Symbole der Artikelkarten sind mindestens 44 × 44 pt groß.
- Alle Bedienflächen sind für Screenreader beschriftet — Details siehe [Barrierefreiheit](barrierefreiheit.md).

Die Screenshot-Dokumentation der manuellen UI-Prüfung (Light/Dark, 390 × 844 pt) steht in [Mobile-UI-Design](mobile-ui-design.md).
