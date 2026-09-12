← [Zurück zur Übersicht](index.md)

# Anwendung — Feeds synchronisieren

## Voraussetzungen

- Der gewünschte Feed wurde auf der Seite **Feeds** angelegt.
- Das Gerät hat eine funktionierende Internetverbindung.

## Schritt-für-Schritt-Anleitung

### Einen einzelnen Feed aktualisieren

1. Öffne die Seite **Feeds**.
2. Tippe in der Zeile des gewünschten Feeds auf **Aktualisieren**.
3. Die App ruft den Feed ab, parst die RSS-/Atom-Daten und speichert neue Artikel in der Datenbank.
4. Der Gesundheitsstatus des Feeds wird aktualisiert (`OK`, `Warning` oder `Error`).
5. Auf iOS löst die App für berechtigte neue Artikel eine lokale Benachrichtigung aus — abhängig von den Schaltern in den **Einstellungen** und am Feed, der Ruhezeit und den Schlagwort-Filtern. Details siehe [Benachrichtigungen](../benachrichtigungen/index.md).

### Alle Feeds aktualisieren

1. Öffne die Seite **Feeds**.
2. Tippe auf **Alle aktualisieren** oberhalb der Feed-Liste.
3. Alle konfigurierten Feeds werden nacheinander abgerufen.

### Automatische Aktualisierung

Ist auf der Seite **Einstellungen** der Schalter **Automatische Hintergrund-Aktualisierung** eingeschaltet, ruft die App alle Feeds periodisch im Hintergrund ab — dasselbe wie **Alle aktualisieren**, nur zeitgesteuert. Das **Abruf-Intervall** lässt sich zwischen *Alle 15 Minuten*, *Alle 30 Minuten*, *Stündlich* und *Alle 4 Stunden* wählen. Die automatische Aktualisierung läuft nur, solange die App geöffnet ist; dauert ein Abruf länger als das eingestellte Intervall, startet der nächste erst nach seinem Abschluss — Abrufe laufen nie parallel. Details siehe [Einstellungen](../einstellungen/index.md).

## Verhalten ohne Internetverbindung

Hat das Gerät keine Internetverbindung, wird kein Abruf gestartet:

- Auf der Seite **Feeds** erscheint oberhalb der Liste ein Hinweis-Banner **„Keine Internetverbindung."**; auf **Ungelesen** wird der Aktualisieren-Button abgedunkelt und ein Hinweis in der Statuszeile eingeblendet.
- **Aktualisieren**, **Alle aktualisieren** und das Herunterziehen der Liste werden übersprungen — es entsteht weder ein Fehlereintrag noch ändert sich der Gesundheitsstatus eines Feeds.
- Die automatische Hintergrund-Aktualisierung pausiert und setzt nach Netzrückkehr mit dem nächsten Intervall fort.

Details zum Offline-Verhalten siehe [Offline lesen](offline.md).

## Gesundheitsstatus

| Status | Bedeutung |
|--------|-----------|
| OK | Abruf erfolgreich, XML gültig. |
| Warning | Abruf erfolgreich, aber auffällig wenige Items oder seit längerer Zeit keine neuen Artikel (>30 Tage). |
| Error | Feed nicht erreichbar oder XML nicht verarbeitbar. |

## Verhalten bei Fehlern

- Bestehende Artikel werden bei einem Fehler **nicht** gelöscht.
- Für jeden Abruf wird ein `SyncLog`-Eintrag mit Status, Zeitstempel und ggf. Fehlermeldung gespeichert.
