<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Ungelesen

Die Startseite **Ungelesen** zeigt alle ungelesenen Artikel aller Feeds, nach Veröffentlichungsdatum sortiert. Die Sortierrichtung stellst du in den **Einstellungen** über **Sortierung der ungelesenen Artikel** ein: *Neueste zuerst* (Voreinstellung) oder *Älteste zuerst*.

## Bedienung

- **Scrollen**: Weitere Artikel werden automatisch nachgeladen, wenn das Listenende erreicht wird (Infinity-Scroll).
- **Pull-to-Refresh**: Herunterziehen der Liste synchronisiert alle Feeds und lädt die Liste neu. Ohne Internetverbindung wird der Aktualisieren-Button abgedunkelt, ein Hinweis **„Keine Internetverbindung."** erscheint in der Statuszeile und der Abruf wird übersprungen (siehe [Offline lesen](offline.md)).
- **Kategoriefilter**: Unter dem Seitentitel liegt eine horizontal scrollbare Leiste mit Filter-Chips. Jeder Chip zeigt den Kategorienamen und in einer kleinen Kapsel die Anzahl der ungelesenen Artikel. Der aktuell gewählte Chip ist dunkel hinterlegt. **Alle** steht an erster Stelle und zeigt alle ungelesenen Artikel. Ein Tipp auf einen Chip filtert die Liste sofort.
- **Aktionen**: Antippen einer Karte öffnet ein Menü mit den Aktionen **Öffnen**, **Lesezeichen** und **Als gelesen**. Wählst du **Öffnen**, wird die [Artikeldetailansicht](artikeldetailansicht.md) angezeigt.
- **Alles gelesen**: Über die Schaltfläche oben rechts werden alle sichtbaren ungelesenen Artikel als gelesen markiert.

## Artikelkarten

Jede Karte zeigt den Feed-Titel, das Veröffentlichungsdatum, den Titel, einen kurzen Teasertext und ein Bild: Enthält der Artikel ein Bild, erscheint es als Vorschaubild; fehlt es, wird stattdessen das Favicon des Feeds gezeigt, und ist auch das nicht bekannt, ein Kreis mit dem Anfangsbuchstaben des Feeds. Darunter steht die geschätzte Lesezeit (z. B. **„3 Min. Lesezeit"**) — bei sehr kurzen Artikeln von höchstens einer Minute Lesezeit entfällt die Angabe. Ungelesene Artikel tragen zusätzlich einen kleinen Punkt in der Kopfzeile neben dem Feed-Titel. Unten rechts auf der Karte liegen das **Lesezeichen** (füllt sich golden, sobald der Artikel unter **Später** gemerkt ist) und die **Als gelesen**-Aktion.

## Barrierefreiheit

Alle Schaltflächen und Karten auf dieser Seite sind für Screenreader beschriftet — die Filter-Chips nennen Kategoriename, Anzahl und Auswahlzustand, die Karten den Artikeltitel mit dem Hinweis **„Tippen, um den Artikel zu öffnen"**. Details siehe [Barrierefreiheit](barrierefreiheit.md).
