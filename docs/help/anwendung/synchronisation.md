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

### Alle Feeds aktualisieren

1. Öffne die Seite **Feeds**.
2. Tippe auf **Alle aktualisieren** oberhalb der Feed-Liste.
3. Alle konfigurierten Feeds werden nacheinander abgerufen.

## Gesundheitsstatus

| Status | Bedeutung |
|--------|-----------|
| OK | Abruf erfolgreich, XML gültig. |
| Warning | Abruf erfolgreich, aber auffällig wenige Items oder seit längerer Zeit keine neuen Artikel (>30 Tage). |
| Error | Feed nicht erreichbar oder XML nicht verarbeitbar. |

## Verhalten bei Fehlern

- Bestehende Artikel werden bei einem Fehler **nicht** gelöscht.
- Für jeden Abruf wird ein `SyncLog`-Eintrag mit Status, Zeitstempel und ggf. Fehlermeldung gespeichert.
