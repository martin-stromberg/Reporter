<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Offline lesen

Reporter speichert alle abgerufenen Artikel, Feeds und Kategorien lokal auf dem Gerät. Ohne Internetverbindung bleiben bereits synchronisierte Inhalte daher vollständig lesbar — die App zeigt den Offline-Zustand dabei sichtbar an und verhindert Aktionen, die ein Netzwerk benötigen.

## Zweck

Du kannst Reporter auch unterwegs ohne Empfang nutzen — etwa im Flugmodus, in der U-Bahn oder bei einem Netzausfall. Statt Fehlermeldungen oder Abstürzen zeigt die App den Hinweis **„Keine Internetverbindung."** und sperrt die Funktionen, die online nur scheitern würden.

## Funktionsweise

Sobald das Gerät keine Internetverbindung mehr hat, reagiert die App automatisch — ein Neustart ist nicht nötig:

- **Ungelesen:** Der Aktualisieren-Button oben wird abgedunkelt dargestellt, und in der Statuszeile erscheint der Hinweis **„Keine Internetverbindung."**. Ein Tipp auf den Button oder das Herunterziehen der Liste (Ziehen zum Aktualisieren) startet dann keinen Abruf.
- **Feeds:** Unterhalb der Schaltfläche **+ Feed per URL hinzufügen** erscheint ein Hinweis-Banner **„Keine Internetverbindung."**. Das Herunterziehen der Liste und das Aktualisieren eines einzelnen Feeds werden übersprungen. Im Hinzufügen-Formular ist die Schaltfläche **Suchen** deaktiviert, und der Hinweis **„Feed-Suche offline nicht verfügbar — URL direkt hinzufügen bleibt möglich."** wird eingeblendet — das direkte Hinzufügen über **URL direkt hinzufügen** bleibt möglich.
- **Feeddetailansicht:** Auch hier erscheint das Hinweis-Banner **„Keine Internetverbindung."**. Gespeicherte Artikel und die Titelsuche bleiben nutzbar; das Herunterziehen der Liste und **Aktualisieren** im Menü **Feed-Aktionen** werden übersprungen. Das Feed-Symbol weicht offline auf den Kreis mit dem Anfangsbuchstaben aus.
- **Später:** Auch hier erscheint das Hinweis-Banner **„Keine Internetverbindung."**.
- **Listenbilder:** Beim Abruf eines Feeds speichert die App das Artikelbild lokal auf dem Gerät — diese Vorschaubilder bleiben auf den Artikelkarten daher auch offline sichtbar. Nur Karten ohne lokal gespeichertes Bild blenden das Vorschaubild offline aus, damit keine leeren Bildrahmen entstehen — das gilt auch für die Favicons der Feeds. Titel, Teasertext und alle Aktionen bleiben sichtbar. Auf der Seite **Feeds** weicht das Feed-Symbol offline auf den Kreis mit dem Anfangsbuchstaben aus.
- **Abruf beim Start:** Ist in den **Einstellungen** der Schalter **Beim Programmstart abrufen** eingeschaltet, wird der einmalige Abruf beim Öffnen ohne Internetverbindung still übersprungen — es erscheint keine Fehlermeldung.
- **Artikeldetailansicht:** Oberhalb des Artikelinhalts erscheint der Hinweis **„Links sind im Offline-Modus deaktiviert."**. Links im Artikeltext sind nicht anklickbar; externe Bilder werden nicht dargestellt — ein lokal gespeichertes Artikelbild bleibt dagegen eingebettet sichtbar, und der Text bleibt vollständig lesbar. Tippt man dennoch auf einen Rest-Link, erscheint ein Hinweisdialog. (Zum Vergleich: Online werden Links im Artikeltext nicht mehr in der App, sondern im externen Browser geöffnet — siehe [Artikeldetailansicht](artikeldetailansicht.md).) Die Schaltfläche **Im Browser öffnen** zeigt offline ebenfalls den Hinweis **„Keine Internetverbindung."** statt den Browser zu starten.
- **Automatische Aktualisierung:** Die in den **Einstellungen** wählbare Hintergrund-Aktualisierung pausiert ohne Netzwerk automatisch und setzt nach Netzrückkehr mit dem nächsten Intervall fort.

Kehrt die Verbindung zurück, verschwinden alle Hinweise von selbst, und die gesperrten Funktionen stehen wieder zur Verfügung — ohne dass du etwas tun musst.

## Beispiele

- Du öffnest Reporter im Flugzeug: Alle bisher synchronisierten Artikel sind unter **Ungelesen** und **Später** lesbar; nur das Nachladen neuer Artikel entfällt.
- Die Verbindung reißt mitten im Lesen ab: Bereits geöffnete Artikel bleiben lesbar, Links und externe Bilder werden ausgeblendet — ein lokal gespeichertes Artikelbild bleibt sichtbar — und der Offline-Hinweis erscheint. Kommt das Netz zurück, sind Links und Bilder ohne erneutes Öffnen des Artikels wieder da.
- Du ziehst offline an der Artikelliste: Es erscheint keine Fehlermeldung — der Hinweis in der Statuszeile zeigt bereits an, warum nichts passiert.

## Einschränkungen

- **Neue Artikel** können offline nicht abgerufen werden — erst nach Netzrückkehr synchronisiert die App wieder.
- **Nach einer Geräte-Wiederherstellung** können Artikel vorübergehend ohne Volltext und ohne Artikelbild erscheinen: Die Artikel-Metadaten (Titel, Lesestatus, Gemerkt-Status) gehören zu den gesicherten Nutzerdaten, die re-downloadbaren Artikelinhalte und Artikelbilder liegen dagegen in einer separaten Datei, die bewusst nicht ins iCloud-Backup eingeschlossen ist. Fehlende Inhalte und Bilder lädt die App beim nächsten Abruf des betreffenden Feeds automatisch nach — bis dahin zeigt die Detailansicht des betroffenen Artikels Titel und Kopfzeilen, aber einen leeren Textbereich.
- **Externe Bilder** im Artikelinhalt werden offline entfernt dargestellt — mit Ausnahme des beim Abruf lokal gespeicherten Artikelbilds, das eingebettet sichtbar bleibt. Artikel, deren Aussagekraft hauptsächlich auf weiteren Bildern beruht, erscheinen entsprechend gekürzt.
- **Links im Artikeltext** führen offline nicht ins Netz; sie werden als normaler Text dargestellt.
- **Im Browser öffnen** und die Synchronisation benötigen grundsätzlich eine Internetverbindung. **Teilen** bleibt dagegen verfügbar, da dafür kein Netz nötig ist.
- Die **Feed-Suche** auf der Feeds-Seite ist offline nicht verfügbar; Feeds lassen sich offline nur direkt per URL hinzufügen.
- Es gibt keine Einstellung zum Offline-Verhalten — es ist immer aktiv und richtet sich allein nach dem Netzwerkzustand des Geräts.
