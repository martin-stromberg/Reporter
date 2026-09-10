# Übersetzte Anforderung

## Ausgangsissue
- **Kennung:** #22
- **Titel:** RSS/Atom-Synchronisation, Artikelabruf und Feed-Health

## Ziel
Feeds werden abgerufen, geparst und neue Artikel werden erkannt, gespeichert und mit einem Health-Status versehen.

## Funktionaler Scope
1. Sync-Service (Hintergrund/Vordergrund), der einzelne oder alle Feeds abruft.
2. RSS/Atom-Parsing mit `System.ServiceModel.Syndication`.
3. Artikel-Deduplizierung anhand GUID, Link, Hash und/oder Timestamp.
4. Speicherung von Artikel-Metadaten und Volltext-HTML (aus Feed-Inhalt) für Offline-Lesen.
5. Aktualisierung der Feed-Felder `last_checked_at`, `health_status`, `health_last_change`.
6. Sync-Log-Einträge für jeden Abruf (Erfolg, Fehler, Anzahl neuer Artikel).
7. Fehlerbehandlung:
   - Feed nicht erreichbar → bestehende Artikel bleiben, Health = Fehler.
   - Feed liefert deutlich weniger Items als zuvor → keine Löschung, Health = Warnung.
   - Ungültiges XML → Health = Fehler.
8. Initiale Health-Logik:
   - OK: Abruf und XML gültig.
   - Warnung: deutlich weniger Items als vorher oder lange keine neuen Artikel.
   - Fehler: nicht erreichbar oder ungültiges XML.
9. Der Abruf darf die UI nicht blockieren (async/Background).

## Akzeptanzkriterien
- Manuelles Refresh eines Feeds oder aller Feeds speichert neue Artikel korrekt.
- Duplikate werden nicht erneut eingefügt.
- Bei Fehler bleiben bestehende Artikel erhalten.
- Der Health-Status wird korrekt aktualisiert und im Feed-Repository gespeichert.
- Jedem Sync-Lauf wird ein Sync-Log-Eintrag hinzugefügt.

## Lieferzustand
Feeds können aktualisiert werden; ungelesene Artikel erscheinen in der Datenbank.

## Nicht im Scope
- Volltext-Abruf von Quellseiten (spätere Optimierung).
- Automatische Hintergrund-Sync-Scheduler (manueller Refresh zunächst ausreichend).
