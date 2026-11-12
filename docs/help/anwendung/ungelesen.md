<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Ungelesen

Die Startseite **Ungelesen** zeigt ungelesene Artikel aller Feeds, nach Veröffentlichungsdatum sortiert — tageweise als **Lesepaket**: Beim Laden der Seite werden nur die Artikel eines Kalendertags (bei wenigen Artikeln zweier Tage) angezeigt, nicht die gesamte Ungelesen-Liste. Die Sortierrichtung stellst du in den **Einstellungen** über **Sortierung der ungelesenen Artikel** ein: *Neueste zuerst* (Voreinstellung) oder *Älteste zuerst*.

## Lesepakete

- **Ein Tag pro Paket:** Beim Laden der Seite entscheidet der erste Artikel in der gewählten Reihenfolge, welcher Tag das Lesepaket bildet — bei *Neueste zuerst* der neueste Tag mit ungelesenen Artikeln (meist der aktuelle Tag), bei *Älteste zuerst* der älteste. Enthält dieser Tag weniger als fünf ungelesene Artikel, wird zusätzlich der nächste Tag mit ungelesenen Artikeln mitgeladen; die Liste endet dann am Ende des Pakets. Leere Tage dazwischen werden übersprungen.
- **Paket gelesen:** Der Doppelhaken-Button im Kopfbereich markiert nur das aktuelle Lesepaket als gelesen. Die Rückfrage nennt die Paket-Größe und den oder die betroffenen Tage (z. B. **„6 Artikel (10.03.2026) als gelesen markieren?"** oder **„6 Artikel (08.03.2026 + 09.03.2026) als gelesen markieren?"**); mit **Paket gelesen** bestätigst du. Danach lädt die Seite automatisch das nächste Paket.
- **Nachrücken beim Markieren:** Markierst du einzelne Artikel als gelesen, bleibt das Paket unverändert — es rücken keine Artikel weiterer Tage nach. Verschwindet dadurch der letzte sichtbare Artikel, lädt die Seite automatisch das nächste Paket.
- **Artikel ohne Datum:** Liefert ein Feed kein Veröffentlichungsdatum, bilden diese Artikel ein eigenes Paket — im Dialog als **„ohne Datum"** benannt. Bei *Älteste zuerst* kommt es als erstes Paket dran, bei *Neueste zuerst* als letztes.
- **Zähler:** Die Statuszeile und die Filter-Tabs zeigen weiterhin die Gesamtzahl aller ungelesenen Artikel über alle Tage hinweg. Nur die Rückfrage des Doppelhakens und die Positionsanzeige der Kartenansicht („x von y") beziehen sich auf das Paket.

## Aufbau

- **Kopfbereich**: Über dem großen Seitentitel stehen Wochentag und Datum, rechts daneben drei Symbol-Schaltflächen: **Aktualisieren** (dreht sich während des Abrufs), **Ansicht wechseln** (Liste ↔ Karten) und der Doppelhaken **Paket gelesen** (markiert das aktuelle Lesepaket als gelesen). Darunter zeigt eine Statuszeile die Anzahl ungelesener Artikel und den Stand der Synchronisation (z. B. **„Aktualisiert vor 3 Min."**, **„Wird aktualisiert …"** oder **„Offline"**). Ein farbiger Punkt fasst den Zustand zusammen: grün = in Ordnung, gelb = Abruf läuft, rot = letzter Abruf fehlgeschlagen, grau = offline.
- **Filter-Tabs**: Eine horizontal scrollbare Tab-Leiste mit allen Kategorien und der jeweiligen Anzahl ungelesener Artikel. **Alle** steht an erster Stelle. Der aktive Tab wird von einer Pille hinterlegt, die beim Wechsel animiert zum neuen Tab gleitet; der gewählte Tab wird dabei in den sichtbaren Bereich gescrollt. Ein Wechsel lädt das Lesepaket für die gewählte Kategorie neu.
- **Leerer Zustand**: Gibt es keine ungelesenen Artikel, erscheint **„Alles gelesen"** mit einem kurzen Hinweis. Beim allerersten Laden zeigt ein pulsierendes Platzhalter-Skelett, dass Inhalte geladen werden.

## Zwei Ansichten

Ob die Startseite als **Liste** oder als **Karten** erscheint, stellst du in den **Einstellungen** unter **Ansicht der Startseite** ein — oder direkt über die Schaltfläche **Ansicht wechseln** im Kopfbereich. Die Wahl wird gespeichert.

### Liste

Zum schnellen Überfliegen. Jede Zeile zeigt Quelle (Favicon oder Anfangsbuchstabe), relative Zeit (z. B. **„vor 25 Min."**, **„gestern"**), die Überschrift, einen zweizeiligen Teaser und — falls vorhanden — ein Vorschaubild. Darunter stehen Kategorie, geschätzte Lesezeit sowie **Lesezeichen** und **Als gelesen**.

- **Antippen** öffnet die [Artikeldetailansicht](artikeldetailansicht.md).
- **Nach rechts wischen** merkt den Artikel für [Später](spaeter.md).
- **Nach links wischen** markiert den Artikel als gelesen; die Zeile blendet sich aus.
- **Herunterziehen** synchronisiert alle Feeds (Pull-to-Refresh) und lädt das Lesepaket neu; am Listenende werden weitere Artikel des Pakets nachgeladen — am Paketende endet die Liste.

### Karten

Zum konzentrierten Durcharbeiten, Artikel für Artikel. Ein Kartenstapel zeigt jeweils einen Artikel groß mit Bild (ohne Bild: typografisches Cover mit dem Feed-Namen), Überschrift und Teaser; die nächste Karte liegt sichtbar dahinter. Unter dem Stapel stehen die Position (z. B. **„3 von 42"**) und die Aktionsleiste: **Vorheriger Artikel**, **Lesezeichen**, **Lesen** (öffnet den Artikel), **Als gelesen markieren**, **Nächster Artikel**.

- **Nach links wischen**: nächster Artikel.
- **Nach rechts wischen**: vorheriger Artikel.
- **Nach oben wischen**: als gelesen markieren — die Karte fliegt hinaus, die nächste rückt nach.
- Die Karte folgt dem Finger; unterhalb der Schwelle federt sie zurück. Kurz vor dem Ende des Stapels werden weitere Artikel des Pakets nachgeladen — der Stapel endet am Paketende, die Positionsanzeige zählt nur das Paket. Unter Windows funktioniert das Wischen auch mit der Maus.

**Paket gelesen** fragt vor dem Markieren kurz nach und nennt dabei Anzahl und Tag(e) des aktuellen Pakets. Ohne Internetverbindung ist **Aktualisieren** abgedunkelt und der Abruf wird übersprungen (siehe [Offline lesen](offline.md)). Artikelbilder werden beim Abruf lokal gespeichert und bleiben offline sichtbar; Bilder, die nur online verfügbar sind, werden offline ausgeblendet.

## Barrierefreiheit

Alle Schaltflächen und Karten auf dieser Seite sind für Screenreader beschriftet — die Filter-Chips nennen Kategoriename, Anzahl und Auswahlzustand, die Karten den Artikeltitel mit dem Hinweis **„Tippen, um den Artikel zu öffnen"**. Details siehe [Barrierefreiheit](barrierefreiheit.md).
