<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Barrierefreiheit

Reporter ist für die Bedienung mit den Eingabehilfen des Geräts vorbereitet: Screenreader-Unterstützung für alle bedienbaren Elemente, dynamische Schriftgrößen, ausreichend große Tippflächen und kontraststarke Farben in beiden Farbschemas.

## Screenreader

Aktivierst du den Screenreader deines Geräts (z. B. VoiceOver unter iOS, TalkBack unter Android, Narrator unter Windows), erhält jedes bedienbare Element eine gesprochene Beschriftung — auch Elemente, die nur ein Symbol zeigen:

- **Ungelesen:** Die Filter-Chips nennen Kategoriename, Anzahl der ungelesenen Artikel und den Auswahlzustand (z. B. *„Filter Alle, 476 ungelesene Artikel, ausgewählt"*). Der Aktualisieren-Button heißt **Aktualisieren**, die Schaltfläche oben rechts **Alles gelesen**.
- **Artikelkarten:** Eine Karte nennt den Artikeltitel mit dem Hinweis **„Tippen, um den Artikel zu öffnen"**. Die Symbole darunter sind als **Lesezeichen setzen** bzw. **Lesezeichen entfernen** und **Als gelesen markieren** beschriftet.
- **Artikeldetailansicht:** Die sechs Symbole der schwebenden Aktionsleiste heißen **Zurück**, **Lesezeichen setzen**/**Lesezeichen entfernen**, **Schriftgröße wechseln**, **Als gelesen markieren**/**Bereits gelesen**, **Teilen** und **Im Browser öffnen**. Die Beschriftungen von Lesezeichen und Gelesen-Markierung passen sich dem aktuellen Zustand an.
- **Feeds:** Feed-Karten nennen den Feed-Titel mit dem Hinweis **„Tippen, um die Feed-Details zu öffnen"**, Trefferkarten der Suche den Titel mit **„Tippen für Aktionen"**; das abgedunkelte Hintergrundfeld des Hinzufügen-Formulars ist als **Schließen** beschriftet, und das Eingabefeld trägt seine Beschreibung **„Feed-URL oder Website-Adresse…"**.
- **Feeddetailansicht:** Der Zurück-Pfeil heißt **Zurück**, der Menü-Button **Aktionen** und das Suchfeld trägt seine Beschreibung **„Artikel in diesem Feed suchen…"**; im Formular **Feed bearbeiten** ist das Adressfeld als **„Feed-URL"** beschriftet, der Benachrichtigungs-Schalter als **Benachrichtigungen** und das abgedunkelte Hintergrundfeld als **Schließen**.
- **Kategorien:** Karten nennen den Kategorienamen mit dem Hinweis **„Tippen für Aktionen"**; das Eingabefeld ist als **Name** beschriftet.

## Schriftgröße

Alle Texte folgen der Schriftgrößen-Einstellung des Geräts — stellst du im System eine größere Schrift ein, wachsen Überschriften, Fließtext und Beschriftungen entsprechend mit. In der Artikeldetailansicht lässt sich die Schrift des Artikelinhalts zusätzlich über das Symbol **Schriftgröße wechseln** in der schwebenden Aktionsleiste anpassen.

## Tippflächen und Bedienung

Alle tippbaren Elemente — Symbole, Chips, Karten, Schalter und Regler — sind mindestens 44 × 44 pt groß, damit sie sich sicher treffen lassen. Aktionen auf Karten erreichst du über einen Tipp auf die Karte (Menü mit Aktionen) oder über die Symbole auf der Karte.

## Farben und Kontraste

- Texte sind auf hellem wie auf dunklem Hintergrund kontrastreich gesetzt; Hinweistexte verwenden eine kräftigere Farbe statt des früheren sehr hellen Graus.
- Der Gesundheitsstatus von Feeds wird nicht allein über die Farbe vermittelt: Das Status-Badge kombiniert einen farbigen Punkt mit dem Text **In Ordnung**, **Warnung** oder **Fehler**.
- Der gewählte Filter-Chip ist zusätzlich zur Füllfarbe über die gesprochene Beschriftung (*ausgewählt*/*nicht ausgewählt*) erkennbar.
- Ungelesene Artikel tragen auf der Karte einen kleinen Punkt; gemerkte Artikel zeigen ein ausgefülltes Lesezeichen — beides ergänzend zum Text, nicht als alleiniges Merkmal.

## Einschränkungen

- Der Artikelinhalt in der Detailansicht stammt aus dem gespeicherten Feed-Text. Externe Bilder behalten ihre ursprüngliche Darstellung und werden im dunklen Farbschema nicht invertiert.
- Die App-eigene Lesezeit- und Hinweistexte sind lokalisiert (Deutsch/Englisch); die Inhalte der Artikel selbst erscheinen in der Sprache des jeweiligen Feeds.
