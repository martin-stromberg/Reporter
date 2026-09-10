# Anforderung: Kategorieverwaltung

## Ziel
Nutzer können Kategorien anlegen, bearbeiten, löschen und Feeds späteren Kategorien zuordnen.

## Scope
- UI-Seite/Routen für Kategorieverwaltung (Liste, Hinzufügen, Bearbeiten, Löschen).
- Validierung: Name darf nicht leer sein, Duplikate verhindern.
- Löschen: Bei zugeordneten Feeds Kategorie-Referenz auf NULL setzen oder Löschen verweigern (Verhalten zu definieren).
- Integration mit `ICategoryRepository`.

## Akzeptanzkriterien
- Kategorien lassen sich erstellen, umbenennen und löschen.
- Änderungen werden in SQLite persistiert.
- Feeds ohne Kategorie bleiben funktionsfähig.
- Liste zeigt Kategorienamen und Anzahl zugeordneter Feeds.

## Lieferzustand
App zeigt Kategorieverwaltung; Feeds können später Kategorien zugeordnet werden.

## Notizen
- Feed-Kategorie-Zuordnung erfolgt im Feed-Management-Arbeitspaket.
