<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Ungelesen

Die Startseite **Ungelesen** zeigt alle ungelesenen Artikel aller Feeds, nach Veröffentlichungsdatum sortiert. Die Sortierrichtung stellst du in den **Einstellungen** über **Sortierung der ungelesenen Artikel** ein: *Neueste zuerst* (Voreinstellung) oder *Älteste zuerst*.

## Aufbau

- **Kopfbereich**: Über dem großen Seitentitel stehen Wochentag und Datum, rechts daneben drei Symbol-Schaltflächen: **Aktualisieren** (dreht sich während des Abrufs), **Ansicht wechseln** (Liste ↔ Karten) und **Alles gelesen**. Darunter zeigt eine Statuszeile die Anzahl ungelesener Artikel und den Stand der Synchronisation (z. B. **„Aktualisiert vor 3 Min."**, **„Wird aktualisiert …"** oder **„Offline"**). Ein farbiger Punkt fasst den Zustand zusammen: grün = in Ordnung, gelb = Abruf läuft, rot = letzter Abruf fehlgeschlagen, grau = offline.
- **Filter-Tabs**: Eine horizontal scrollbare Tab-Leiste mit allen Kategorien und der jeweiligen Anzahl ungelesener Artikel. **Alle** steht an erster Stelle. Der aktive Tab wird von einer Pille hinterlegt, die beim Wechsel animiert zum neuen Tab gleitet; der gewählte Tab wird dabei in den sichtbaren Bereich gescrollt.
- **Leerer Zustand**: Gibt es keine ungelesenen Artikel, erscheint **„Alles gelesen"** mit einem kurzen Hinweis. Beim allerersten Laden zeigt ein pulsierendes Platzhalter-Skelett, dass Inhalte geladen werden.

## Zwei Ansichten

Ob die Startseite als **Liste** oder als **Karten** erscheint, stellst du in den **Einstellungen** unter **Ansicht der Startseite** ein — oder direkt über die Schaltfläche **Ansicht wechseln** im Kopfbereich. Die Wahl wird gespeichert.

### Liste

Zum schnellen Überfliegen. Jede Zeile zeigt Quelle (Favicon oder Anfangsbuchstabe), relative Zeit (z. B. **„vor 25 Min."**, **„gestern"**), die Überschrift, einen zweizeiligen Teaser und — falls vorhanden — ein Vorschaubild. Darunter stehen Kategorie, geschätzte Lesezeit sowie **Lesezeichen** und **Als gelesen**.

- **Antippen** öffnet die [Artikeldetailansicht](artikeldetailansicht.md).
- **Nach rechts wischen** merkt den Artikel für [Später](spaeter.md).
- **Nach links wischen** markiert den Artikel als gelesen; die Zeile blendet sich aus.
- **Herunterziehen** synchronisiert alle Feeds (Pull-to-Refresh), am Listenende wird automatisch nachgeladen.

### Karten

Zum konzentrierten Durcharbeiten, Artikel für Artikel. Ein Kartenstapel zeigt jeweils einen Artikel groß mit Bild (ohne Bild: typografisches Cover mit dem Feed-Namen), Überschrift und Teaser; die nächste Karte liegt sichtbar dahinter. Unter dem Stapel stehen die Position (z. B. **„3 von 42"**) und die Aktionsleiste: **Vorheriger Artikel**, **Lesezeichen**, **Lesen** (öffnet den Artikel), **Als gelesen markieren**, **Nächster Artikel**.

- **Nach links wischen**: nächster Artikel.
- **Nach rechts wischen**: vorheriger Artikel.
- **Nach oben wischen**: als gelesen markieren — die Karte fliegt hinaus, die nächste rückt nach.
- Die Karte folgt dem Finger; unterhalb der Schwelle federt sie zurück. Kurz vor dem Ende des Stapels werden weitere Artikel nachgeladen. Unter Windows funktioniert das Wischen auch mit der Maus.

**Alles gelesen** fragt vor dem Markieren aller sichtbaren Artikel kurz nach. Ohne Internetverbindung ist **Aktualisieren** abgedunkelt und der Abruf wird übersprungen (siehe [Offline lesen](offline.md)). Artikelbilder werden beim Abruf lokal gespeichert und bleiben offline sichtbar; Bilder, die nur online verfügbar sind, werden offline ausgeblendet.

## Barrierefreiheit

Alle Schaltflächen und Karten auf dieser Seite sind für Screenreader beschriftet — die Filter-Chips nennen Kategoriename, Anzahl und Auswahlzustand, die Karten den Artikeltitel mit dem Hinweis **„Tippen, um den Artikel zu öffnen"**. Details siehe [Barrierefreiheit](barrierefreiheit.md).
