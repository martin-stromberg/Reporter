<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Anwendung — Beschreibung

## Zweck

Reporter bietet einen zentralen Ort, um RSS- und Atom-Feeds zu abonnieren, Artikel zu lesen und Beiträge für später zu merken. Die App folgt dem System-Design (Light/Dark) und unterstützt Deutsch und Englisch — wahlweise entsprechend der Systemsprache oder über eine manuelle Sprachauswahl in den Einstellungen (siehe [Sprache](sprache.md)). Bereits synchronisierte Artikel bleiben ohne Internetverbindung vollständig lesbar (siehe [Offline lesen](offline.md)).

## Funktionsweise

Nach dem Start erscheint die untere Navigationsleiste mit fünf Bereichen — jeder Tab wird mit einem eigenen Symbol und seinem Namen dargestellt:

- **Ungelesen** — Zeigt neue Artikel, die noch nicht gelesen wurden; die Sortierrichtung (*Neueste zuerst* oder *Älteste zuerst*) wählst du in den Einstellungen. Eine horizontal scrollbare Filterleiste mit Chips (Kategoriename plus Anzahl ungelesener Artikel) grenzt die Liste auf eine Kategorie ein.
- **Feeds** — Zeigt die abonnierten Feeds und deren Gesundheitsstatus als kompaktes Status-Badge (**In Ordnung**, **Warnung**, **Fehler**) auf jeder Feed-Karte; links trägt jede Karte das Feed-Symbol (Favicon der Website oder ein Kreis mit dem Anfangsbuchstaben). Über **+ Feed per URL hinzufügen** fügst du neue Feeds per Suche hinzu — eine Website-Adresse genügt — oder direkt per URL; ein Tipp auf eine Feed-Karte öffnet deren Detailansicht mit allen Artikeln des Feeds, einer Titelsuche und dem Menü **Feed-Aktionen** (**Aktualisieren**, **Umbenennen**, **Kategorie ändern**, **Bearbeiten**, **Löschen** — bei einem Feed im Status **Fehler** zusätzlich **Fehlerdetails anzeigen**, ein Dialog mit dem verständlichen Grund und der technischen Meldung des letzten fehlgeschlagenen Abrufs). Details siehe [Feeddetailansicht](feeddetailansicht.md), [Feeds suchen und hinzufügen](feed-suche.md) und [Feeds synchronisieren](synchronisation.md).
- **Später** — Zeigt Artikel, die du dir für später merkst; weitere Einträge laden beim Scrollen automatisch nach.
- **Kategorien** — Verwaltet Kategorien für Feeds.
- **Einstellungen** — Konfiguriert Aufbewahrungsdauer, Keyword-Filter, automatische Hintergrund-Aktualisierung, den Abruf beim Programmstart, die Sortierung der Ungelesen-Liste, automatisches Als-gelesen-Markieren, Benachrichtigungen mit Ruhezeiten, das Farbschema und die Sprache. Unter **Diagnose & Support** sammelst du bei Bedarf Fehlerinformationen der laufenden Sitzung und sendest sie als vorbefüllten E-Mail-Entwurf an den Support. Details siehe [Einstellungen](../einstellungen/index.md).

Beim allerersten Start (frische Installation mit leerer Datenbank) richtet Reporter bereits einen Beispiel-Feed ein: die Kategorie **News** mit dem Feed **Apple Newsroom** (`https://www.apple.com/newsroom/rss-feed.rss`). Der Feed erscheint wie jeder andere als Karte unter **Feeds**, wird beim Start-Abruf synchronisiert und lässt sich jederzeit bearbeiten oder löschen; Benachrichtigungen sind für ihn vorsorglich ausgeschaltet.

Artikelkarten zeigen neben Titel und Teasertext ein Bild — das beim Abruf lokal gespeicherte Artikelbild (bleibt auch offline sichtbar), ersatzweise das Favicon des Feeds oder einen Kreis mit dem Anfangsbuchstaben des Feeds — sowie eine geschätzte Lesezeit (bei höchstens einer Minute entfällt die Angabe) und bei ungelesenen Artikeln einen kleinen Punkt in der Kopfzeile; gemerkte Artikel erkennst du am ausgefüllten Lesezeichen. In der Artikeldetailansicht schwebt die Aktionsleiste (Zurück, Lesezeichen, Schriftgröße, Gelesen-Markierung, Teilen, Im Browser öffnen) als abgerundete Leiste über dem Seitenrand. Die App ist für die Bedienung per Screenreader vorbereitet und folgt der Schriftgrößen-Einstellung des Geräts — siehe [Barrierefreiheit](barrierefreiheit.md).

Gelesene Artikel werden nach Ablauf der Aufbewahrungsfrist beim App-Start automatisch entfernt. Neue Artikel, die ein Filter-Schlagwort enthalten, werden bereits beim Abruf verworfen und erscheinen nicht in den Listen; bereits gespeicherte Treffer werden nach dem Lesen fristbasiert entfernt. Die Schlagworte pflegst du global in den **Einstellungen** und zusätzlich pro Feed in dessen Formular **Feed bearbeiten** — Feed-Schlagworte wirken nur auf den eigenen Feed und ergänzen die globale Liste. Ungelesene Artikel und Artikel, die du dir für später gemerkt hast, bleiben dabei immer erhalten.

Auf iOS benachrichtigt dich die App über neue Artikel, sobald der automatische Hintergrund-Abgleich bei geschlossener App neue Artikel findet — pro Artikel oder gesammelt pro Feed, mit optionaler Ruhezeit und Schlagwort-Filter; bei geöffneter App erscheint bewusst keine Mitteilung. Details siehe [Benachrichtigungen](../benachrichtigungen/index.md).

## Beispiele

- Du öffnest die App und siehst auf dem Tab **Ungelesen** die aktuellsten Artikel deiner Feeds.
- Du markierst einen interessanten Artikel für später und findest ihn unter **Später** wieder.

## Einschränkungen

- Die App benötigt noch keine Anmeldung.
- Der Beispiel-Feed **Apple Newsroom** wird nur beim allerersten Start mit leerer Datenbank angelegt — löschst du ihn, kommt er nicht wieder (Ausnahme: Die App-Daten werden vollständig zurückgesetzt, dann startet die App wieder wie neu).
