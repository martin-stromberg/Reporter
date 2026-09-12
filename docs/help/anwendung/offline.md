← [Zurück zur Übersicht](index.md)

# Offline lesen

Reporter speichert alle abgerufenen Artikel, Feeds und Kategorien lokal auf dem Gerät. Ohne Internetverbindung bleiben bereits synchronisierte Inhalte daher vollständig lesbar — die App zeigt den Offline-Zustand dabei sichtbar an und verhindert Aktionen, die ein Netzwerk benötigen.

## Zweck

Du kannst Reporter auch unterwegs ohne Empfang nutzen — etwa im Flugmodus, in der U-Bahn oder bei einem Netzausfall. Statt Fehlermeldungen oder Abstürzen zeigt die App den Hinweis **„Keine Internetverbindung."** und sperrt die Funktionen, die online nur scheitern würden.

## Funktionsweise

Sobald das Gerät keine Internetverbindung mehr hat, reagiert die App automatisch — ein Neustart ist nicht nötig:

- **Ungelesen:** Der Aktualisieren-Button oben wird abgedunkelt dargestellt, und in der Statuszeile erscheint der Hinweis **„Keine Internetverbindung."**. Ein Tipp auf den Button oder das Herunterziehen der Liste (Ziehen zum Aktualisieren) startet dann keinen Abruf.
- **Feeds:** Unterhalb der Eingabekarte erscheint ein Hinweis-Banner **„Keine Internetverbindung."**. **Alle aktualisieren** und das Aktualisieren eines einzelnen Feeds werden übersprungen.
- **Später:** Auch hier erscheint das Hinweis-Banner **„Keine Internetverbindung."**.
- **Listenbilder:** Die kleinen Vorschaubilder auf den Artikelkarten werden offline ausgeblendet, damit keine leeren Bildrahmen entstehen. Titel, Teasertext und alle Aktionen bleiben sichtbar.
- **Artikeldetailansicht:** Oberhalb des Artikelinhalts erscheint der Hinweis **„Links sind im Offline-Modus deaktiviert."**. Links im Artikeltext sind nicht anklickbar; externe Bilder werden nicht dargestellt — der Text bleibt vollständig lesbar. Tippt man dennoch auf einen Rest-Link, erscheint ein Hinweisdialog. Die Schaltfläche **Im Browser öffnen** zeigt offline ebenfalls den Hinweis **„Keine Internetverbindung."** statt den Browser zu starten.
- **Automatische Aktualisierung:** Die in den **Einstellungen** wählbare Hintergrund-Aktualisierung pausiert ohne Netzwerk automatisch und setzt nach Netzrückkehr mit dem nächsten Intervall fort.

Kehrt die Verbindung zurück, verschwinden alle Hinweise von selbst, und die gesperrten Funktionen stehen wieder zur Verfügung — ohne dass du etwas tun musst.

## Beispiele

- Du öffnest Reporter im Flugzeug: Alle bisher synchronisierten Artikel sind unter **Ungelesen** und **Später** lesbar; nur das Nachladen neuer Artikel entfällt.
- Die Verbindung reißt mitten im Lesen ab: Bereits geöffnete Artikel bleiben lesbar, Links und Bilder werden ausgeblendet, der Offline-Hinweis erscheint. Kommt das Netz zurück, sind Links und Bilder ohne erneutes Öffnen des Artikels wieder da.
- Du ziehst offline an der Artikelliste: Es erscheint keine Fehlermeldung — der Hinweis in der Statuszeile zeigt bereits an, warum nichts passiert.

## Einschränkungen

- **Neue Artikel** können offline nicht abgerufen werden — erst nach Netzrückkehr synchronisiert die App wieder.
- **Externe Bilder** im Artikelinhalt werden offline vollständig entfernt dargestellt. Artikel, deren Aussagekraft hauptsächlich auf Bildern beruht, erscheinen entsprechend gekürzt.
- **Links im Artikeltext** führen offline nicht ins Netz; sie werden als normaler Text dargestellt.
- **Im Browser öffnen** und die Synchronisation benötigen grundsätzlich eine Internetverbindung. **Teilen** bleibt dagegen verfügbar, da dafür kein Netz nötig ist.
- Es gibt keine Einstellung zum Offline-Verhalten — es ist immer aktiv und richtet sich allein nach dem Netzwerkzustand des Geräts.
