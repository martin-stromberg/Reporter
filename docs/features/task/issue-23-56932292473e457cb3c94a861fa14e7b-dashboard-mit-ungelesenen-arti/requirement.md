# Übersetzte Anforderung

## Auslöser
Issue #23 – Dashboard mit ungelesenen Artikeln, Kategoriefilter und Pull-to-Refresh

## Ziel
Neue Hauptseite "Ungelesen" (Dashboard), die ausschließlich ungelesene Artikel aller Feeds zeigt, absteigend nach Veröffentlichungsdatum sortiert, mit Kategoriefilter, Pull-to-Refresh und seitenweisem Laden (Infinity-Scroll).

## Funktionale Anforderungen

### 1. Dashboard-Seite
- Neue Seite `UngelesenPage` bzw. Umbenennung der Startseite.
- Liste von Artikelkarten.
- Jede Karte zeigt:
  - Titel
  - Quelle (Feed-Name/URL)
  - Veröffentlichungsdatum
  - Kategorie (falls vorhanden)
  - Ungelesen-Indikator
  - Aktionen: Lesezeichen setzen, Als gelesen markieren

### 2. Pull-to-Refresh
- Pull-to-Refresh auf der Liste löst Synchronisation aller Feeds aus.
- Nach erfolgreichem Sync wird die Liste automatisch neu geladen.

### 3. Infinity-Scroll
- Liste lädt initial eine begrenzte Anzahl (z. B. 20) ungelesener Artikel.
- Beim Erreichen des Listenendes werden weitere 20 Artikel nachgeladen.

### 4. Kategoriefilter
- Horizontale Filter-Chips oben über der Liste.
- `Alle` plus alle bestehenden Kategorien als Chips.
- Auswahl eines Chips filtert die Liste auf Artikel der gewählten Kategorie.
- Auswahl `Alle` zeigt alle ungelesenen Artikel.

### 5. Sammelaktion
- Aktion "Alle als gelesen markieren" ist verfügbar.
- Markiert alle aktuell sichtbaren bzw. alle ungelesenen Artikel als gelesen (gemäß geplanten Implementierungsdetail).

### 6. Navigation
- Tippen auf eine Karte öffnet die Artikeldetailansicht.

## Nicht-funktionale Anforderungen
- Mobile-first-Layout, keine horizontalen Tabellen.
- Touch-Targets mindestens 44 × 44 pt.
- Dark-Mode-Unterstützung via `AppThemeBinding`.
- Flüssiges Scrollen durch Virtualisierung/Paging.

## Akzeptanzkriterien
- Liste zeigt ausschließlich ungelesene Artikel, sortiert nach `published_at` absteigend.
- Pull-to-Refresh aktualisiert die Liste nach Sync.
- Scrollen lädt weitere Artikel nach.
- Kategorie-Filter reduziert die Liste korrekt.
- "Alle als gelesen markieren" funktioniert.

## Abhängigkeiten
- Kategorieverwaltung muss abgeschlossen sein (siehe Vorgänger-Issue #21).
