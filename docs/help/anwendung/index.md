<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Anwendung

Reporter ist ein lokaler RSS-/Atom-Feed-Reader als .NET MAUI-App. Feeds können über die Feeds-Seite manuell abgerufen und in die lokale Datenbank synchronisiert werden; neue ungelesene Artikel erscheinen danach in der Datenbank. Beim Abruf werden neben den Artikelinhalten auch die Artikelbilder lokal gespeichert, damit sie offline verfügbar bleiben.

## Inhalt

- [Beschreibung](beschreibung.md)
- [Ablauf für Anwender](ablauf-anwender.md)
- [Ungelesen](ungelesen.md)
- [Kategorien verwalten](kategorien.md)
- [Feeds suchen und hinzufügen](feed-suche.md)
- [Feed-Suche — Technischer Ablauf](feed-suche-technisch.md)
- [Feeds synchronisieren](synchronisation.md)
- [Feeddetailansicht](feeddetailansicht.md)
- [Feeddetailansicht — Technischer Ablauf](feeddetailansicht-technisch.md)
- [Artikeldetailansicht](artikeldetailansicht.md)
- [Später — Artikel für später merken](spaeter.md)
- [Offline lesen](offline.md)
- [Sprache (Deutsch / Englisch)](sprache.md)
- [Barrierefreiheit](barrierefreiheit.md)
- [Aufbewahrung und automatisches Aufräumen](aufbewahrung.md)
- [Architektur](architektur.md)
- [Datenmodell](datenmodell.md)
- [Mobile-UI-Design](mobile-ui-design.md)

## Verwandte Bereiche

- [Einstellungen](../einstellungen/index.md) — Aufbewahrungsdauer, globale Schlagwort-Filter (feed-spezifische Schlagworte werden im Bearbeiten-Formular der [Feeddetailansicht](feeddetailansicht.md) gepflegt), automatische Aktualisierung, Start-Abruf, Ungelesen-Sortierung, Lesefluss, Benachrichtigungen, Erscheinungsbild, Sprache und Diagnose & Support (Debug-Sammlung, Debugbericht per E-Mail) konfigurieren.
- [Benachrichtigungen](../benachrichtigungen/index.md) — Lokale iOS-Benachrichtigungen über neue Artikel nach dem Feed-Abgleich.
