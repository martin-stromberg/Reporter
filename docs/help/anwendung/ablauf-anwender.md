<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Anwendung — Ablauf für Anwender

## Voraussetzungen

- Reporter wurde auf dem Gerät installiert und gestartet.
- Das Gerät hat eine funktionierende Internetverbindung (wird für den späteren Feed-Abruf benötigt).

## Schritt-für-Schritt-Anleitung

### 1. App starten

Tippe auf das App-Icon **Reporter**. Nach dem Start-Screen erscheint die Navigationsleiste am unteren Bildschirmrand.

### 2. Zwischen den Bereichen wechseln

Tippe auf einen der fünf Tabs — jeder Tab zeigt ein Symbol und seinen Namen:

- **Ungelesen** — Übersicht neuer Artikel.
- **Feeds** — Übersicht deiner Feeds. Hier kannst du einen oder alle Feeds aktualisieren.
- **Später** — Artikel, die du für später markiert hast.
- **Kategorien** — Kategorien für Feeds verwalten.
- **Einstellungen** — App-Einstellungen.

> **Hinweis:** Die App ist auch ohne Internetverbindung nutzbar — bereits geladene Artikel bleiben lesbar, und ein Hinweis **„Keine Internetverbindung."** erscheint auf den betroffenen Seiten. Details siehe [Offline lesen](offline.md).

> **Tipp:** Auf **Ungelesen** kannst du die Liste über die Filter-Chips unter dem Seitentitel auf eine Kategorie eingrenzen. Jeder Chip zeigt den Kategorienamen und die Anzahl ungelesener Artikel.

### 3. Feed suchen und hinzufügen

- Öffne **Feeds** und tippe auf **+ Feed per URL hinzufügen** — das Formular schiebt sich von unten über die Seite.
- Gib in das Feld **Feed-URL oder Website-Adresse…** eine Website-Adresse oder Feed-URL ein.
- Tippe auf **Suchen** — die gefundenen Feeds erscheinen als Kartenliste.
- Tippe auf eine Trefferkarte und bestätige **Feed abonnieren?** mit **Ja**. Der Feed erscheint in deiner Liste.
- Alternativ legt **URL direkt hinzufügen** eine bekannte Feed-URL sofort an — ohne Suche und auch offline.
- Details siehe [Feeds suchen und hinzufügen](feed-suche.md).

### 4. Feeds synchronisieren

- Öffne **Feeds**.
- Ziehe die Feed-Liste nach unten (Ziehen zum Aktualisieren), um alle Feeds abzurufen.
- Ist in den **Einstellungen** der Schalter **Beim Programmstart abrufen** eingeschaltet (Voreinstellung), hat die App die Feeds bereits beim Öffnen im Hintergrund abgerufen — ein manuelles Aktualisieren ist dann meist nicht nötig.
- Oder tippe eine Feed-Karte an und wähle **Aktualisieren**, um nur diesen Feed abzurufen.
- Neue ungelesene Artikel werden automatisch in der Datenbank gespeichert und erscheinen unter **Ungelesen**.
- Auf iOS kann dabei eine Benachrichtigung erscheinen — ein Tipp darauf öffnet den Artikel direkt in der App (Details siehe [Benachrichtigungen](../benachrichtigungen/index.md)).

> **Hinweis:** Trägt ein Feed das Badge **Fehler**, findest du den Grund über den Menüeintrag **Fehlerdetails anzeigen** der Feed-Karte (Details siehe [Feeds synchronisieren](synchronisation.md)).

### 5. Artikel für später merken

- Tippe auf einer Artikelkarte auf das Lesezeichen-Symbol, um den Artikel zu merken.
- Alle gemerkten Artikel findest du gesammelt unter **Später**. Ein erneutes Tippen auf das Lesezeichen-Symbol entfernt die Merkung.
- Details siehe [Später — Artikel für später merken](spaeter.md).

## Ergebnis

Der jeweilige Bereich wird im Hauptbereich der App angezeigt. Das Theme passt sich automatisch an das System-Design an, die Sprache folgt der Systemsprache des Geräts oder der manuellen Auswahl unter **Einstellungen** → **Sprache** (Deutsch oder Englisch — siehe [Sprache](sprache.md)).

## Barrierefreiheit

Die App unterstützt den Screenreader des Geräts: Alle Symbole und Karten tragen gesprochene Beschriftungen — Filter-Chips nennen Kategoriename, Anzahl und Auswahlzustand, Artikelkarten den Titel samt Hinweis zum Öffnen, die Symbole der schwebenden Aktionsleiste in der Artikeldetailansicht ihre Aktion. Texte folgen der Schriftgrößen-Einstellung des Geräts, und die Bedienflächen sind mindestens 44 × 44 pt groß. Details siehe [Barrierefreiheit](barrierefreiheit.md).
