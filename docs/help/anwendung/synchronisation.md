<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Anwendung — Feeds synchronisieren

## Voraussetzungen

- Der gewünschte Feed wurde auf der Seite **Feeds** angelegt.
- Das Gerät hat eine funktionierende Internetverbindung.

## Schritt-für-Schritt-Anleitung

### Einen einzelnen Feed aktualisieren

1. Öffne die Seite **Feeds**.
2. Tippe die Karte des gewünschten Feeds an und wähle im Menü **Feed-Aktionen** den Eintrag **Aktualisieren**.
3. Die App ruft den Feed ab, parst die RSS-/Atom-Daten und speichert neue Artikel in der Datenbank. Artikel, die ein eingerichtetes Filter-Schlagwort in Titel oder Inhalt enthalten, werden dabei verworfen und erscheinen nicht in den Listen — intern vermerkt der Protokolleintrag des Abrufs, wie viele Artikel gefiltert wurden.
4. Der Gesundheitsstatus des Feeds wird aktualisiert (**In Ordnung**, **Warnung** oder **Fehler**).
5. Auf iOS löst die App für berechtigte neue Artikel eine lokale Benachrichtigung aus — abhängig von den Schaltern in den **Einstellungen** und am Feed, der Ruhezeit und den Schlagwort-Filtern. Details siehe [Benachrichtigungen](../benachrichtigungen/index.md).

### Alle Feeds aktualisieren

1. Öffne die Seite **Feeds**.
2. Ziehe die Feed-Liste nach unten (Ziehen zum Aktualisieren).
3. Alle konfigurierten Feeds werden nacheinander abgerufen.

Der Abruf läuft im Hintergrund — die App bleibt währenddessen bedienbar, und du kannst bereits geladene Artikel weiterlesen. Schlägt ein einzelner Feed fehl, werden die übrigen Feeds trotzdem abgerufen.

### Automatische Aktualisierung

Ist auf der Seite **Einstellungen** der Schalter **Automatische Hintergrund-Aktualisierung** eingeschaltet, ruft die App alle Feeds periodisch im Hintergrund ab — dasselbe wie das Herunterziehen der Liste, nur zeitgesteuert. Das **Abruf-Intervall** lässt sich zwischen *Alle 15 Minuten*, *Alle 30 Minuten*, *Stündlich* und *Alle 4 Stunden* wählen. Die automatische Aktualisierung läuft nur, solange die App geöffnet ist; dauert ein Abruf länger als das eingestellte Intervall, startet der nächste erst nach seinem Abschluss — Abrufe laufen nie parallel. Details siehe [Einstellungen](../einstellungen/index.md).

### Aktualisierung beim App-Start

Der Schalter **Beim Programmstart abrufen** in den **Einstellungen** (Voreinstellung: ein) lässt die App beim Öffnen einmalig alle Feeds abrufen — unabhängig vom Intervall der Hintergrund-Aktualisierung. Der Abruf läuft im Hintergrund und verzögert den Start der App nicht; ohne Internetverbindung wird er übersprungen.

### Feed-Symbole beim Abruf

Beim Anlegen eines Feeds versucht die App, das Favicon der zugehörigen Website zu ermitteln und zu speichern. Fehlt das Symbol noch — etwa weil der Feed offline angelegt wurde —, holt die App es beim nächsten erfolgreichen Abruf nach. Gelingt die Ermittlung nicht, bleibt der Feed funktionsfähig und zeigt stattdessen seinen Anfangsbuchstaben (siehe [Feeds suchen und hinzufügen](feed-suche.md)).

## Anzeigetitel beim ersten Abruf

Wurde ein Feed ohne bekannten Anzeigetitel angelegt — etwa über die **Suche** mit einem Treffer ohne Titel oder über **URL direkt hinzufügen** bzw. den Direkt-Hinzufügen-Dialog (siehe [Feeds suchen und hinzufügen](feed-suche.md)) —, zeigt die Liste zunächst einen Platzhalter aus der Feed-Adresse: den Dateinamen (z. B. `heise-atom.xml`), bei Adressen ohne Dateipfad den Website-Namen oder notfalls die Adresse selbst. Beim ersten erfolgreichen Abruf ersetzt die App den Platzhalter automatisch durch den echten Titel aus dem Feed. Selbst vergebene Titel (über **Umbenennen**) bleiben unverändert und werden nie überschrieben.

## Verhalten ohne Internetverbindung

Hat das Gerät keine Internetverbindung, wird kein Abruf gestartet:

- Auf der Seite **Feeds** erscheint oberhalb der Liste ein Hinweis-Banner **„Keine Internetverbindung."**; auf **Ungelesen** wird der Aktualisieren-Button abgedunkelt und ein Hinweis in der Statuszeile eingeblendet.
- **Aktualisieren** (im Menü **Feed-Aktionen** einer Feed-Karte) und das Herunterziehen der Liste werden übersprungen — es entsteht weder ein Fehlereintrag noch ändert sich der Gesundheitsstatus eines Feeds.
- Die automatische Hintergrund-Aktualisierung pausiert und setzt nach Netzrückkehr mit dem nächsten Intervall fort.

Details zum Offline-Verhalten siehe [Offline lesen](offline.md).

## Gesundheitsstatus

Der Status erscheint auf jeder Feed-Karte als kompaktes Badge mit farbigem Punkt und Text — so bleibt er auch unabhängig von der Farbe erkennbar:

| Status | Bedeutung |
|--------|-----------|
| In Ordnung | Abruf erfolgreich, Feed-Daten gültig. |
| Warnung | Abruf erfolgreich, aber auffällig wenige Artikel oder seit längerer Zeit keine neuen Artikel (über 30 Tage). |
| Fehler | Feed nicht erreichbar oder Feed-Daten nicht verarbeitbar. |

## Verhalten bei Fehlern

- Schlägt eine manuelle Aktualisierung fehl, erscheint oberhalb der Liste eine lokalisierte Fehlermeldung (auf **Ungelesen** und **Feeds**); sie verschwindet beim nächsten Ladevorgang oder bei einem Wechsel der Netzwerkverbindung.
- Bestehende Artikel werden bei einem Fehler **nicht** gelöscht.
- Für jeden Abruf wird ein Protokolleintrag mit Status, Zeitstempel und ggf. Fehlermeldung gespeichert.
