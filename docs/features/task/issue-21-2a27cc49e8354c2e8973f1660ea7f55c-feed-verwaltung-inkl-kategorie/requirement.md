# Übersetzte Anforderung

## Titel
Feed-Verwaltung inkl. Kategoriezuordnung und Statusanzeige

## Ziel
Nutzer können RSS-Feeds hinzufügen, bearbeiten, löschen und Kategorien zuordnen. Die Feed-Liste zeigt relevante Statusinformationen.

## Funktionale Anforderungen
- Auf der Feeds-Seite (Tab „Feeds“) wird eine Liste aller Feeds dargestellt.
- Jede Zeile zeigt: Anzeigename, Kategorie, letzten Abrufzeitpunkt, Anzahl ungelesener Artikel, Health-Status-Indikator.
- Eingabemaske zum Hinzufügen:
  - URL-Eingabe mit Validierung
  - Prüfung auf Duplikate (URL eindeutig)
  - Anzeigename (optional; Feed-Titel kann später per Sync ermittelt werden)
- Bearbeiten: Anzeigename ändern und Kategorie zuweisen/ändern.
- Löschen: Mit Bestätigungsdialog; zugehörige Artikel werden mitgelöscht (Cascade-Delete bereits im Datenmodell).
- Kategoriezuordnung wählbar (inkl. „Keine Kategorie“).
- Health-Status als farbiger Indikator (OK/Warnung/Fehler); Werte stammen aus der Datenbank und werden im Sync-Arbeitspaket gepflegt.

## Nicht-funktionale Anforderungen
- Wiederverwendung bestehender Architektur (Core-Modelle, Repository, EF Core SQLite, ViewModel, MAUI-XAML).
- Übersetzungs-Ressourcen (RESX) für deutsche und englische Texte.
- XML-Dokumentation für neue öffentliche APIs.
