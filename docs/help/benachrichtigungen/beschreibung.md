<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Benachrichtigungen — Beschreibung

## Zweck

Reporter benachrichtigt dich auf dem iPhone/iPad über neue Artikel in deinen Feeds — mit Banner, Ton und Eintrag in der Mitteilungszentrale. Sichtbar wird eine Benachrichtigung allerdings nur, wenn der Abgleich im Hintergrund durch das System stattfindet, also während die App nicht geöffnet ist. So entgeht dir kein neuer Beitrag, ohne dass die App dich bei der Nutzung mit Bannern unterbricht.

## Funktionsweise

Nach jedem Feed-Abgleich — ob manuell über **Aktualisieren** im Menü **Feed-Aktionen** der Feeddetailansicht, das Herunterziehen der Feed-Liste oder automatisch — prüft die App, ob neue Artikel gespeichert wurden, und wendet die Benachrichtigungsregeln an. Eine sichtbare Mitteilung entsteht dabei nur aus dem automatischen Hintergrund-Abgleich, den iOS ausführt, während die App nicht geöffnet ist. Läuft der Abgleich dagegen bei geöffneter App (manuell, per Timer oder beim Programmstart), erscheinen neue Artikel still in den Listen — ohne Banner, ohne Ton und ohne Eintrag im Mitteilungszentrum. Für jeden neuen Artikel entscheiden vier Bedingungen, ob eine Benachrichtigung entsteht:

1. **Benachrichtigungen** in den **Einstellungen** (Karte **Benachrichtigungen & Ruhezeiten**) müssen eingeschaltet sein.
2. Der Schalter **Benachrichtigungen** des betreffenden Feeds (Seite **Feeds** → Feed antippen → **Aktionen** → **Bearbeiten**) muss eingeschaltet sein — so lassen sich einzelne Feeds stummschalten.
3. Es darf gerade keine **Ruhezeit (Nicht stören)** laufen. Benachrichtigungen in der Ruhezeit werden verworfen, nicht nachgeholt.
4. Der Artikel darf kein Filter-Schlagwort in Titel oder Inhalt enthalten — solche Treffer werden bereits beim Abruf verworfen und erscheinen nicht in den Listen. Wirksam sind dabei die globalen Schlagworte aus den **Einstellungen** und die Schlagworte des jeweiligen Feeds aus dessen Formular **Feed bearbeiten** zusammen; ein Feed-Schlagwort gilt nur für seinen eigenen Feed.

Pro Artikel erscheint höchstens eine Benachrichtigung. Der Titel der Benachrichtigung ist der Feed-Name, der Text der Artikeltitel.

Über den Schalter **Sammel-Benachrichtigung** in den Einstellungen wählst du stattdessen eine einzige Benachrichtigung pro Feed und Abgleich (z. B. „3 neue Artikel: Titel 1, Titel 2, …").

Tippst du eine Benachrichtigung an, öffnet die App direkt den Artikel; bei einer Sammel-Benachrichtigung öffnet sich die Ansicht **Ungelesen**.

## Beispiele

- Du abonnierst einen News-Feed: Findet der Hintergrund-Abgleich neue Artikel — etwa während die App geschlossen ist —, erscheint eine Benachrichtigung mit dem Feed-Namen und dem Artikeltitel.
- Ein Feed postet sehr häufig: Schalte **Sammel-Benachrichtigung** ein — statt fünf Einzelmeldungen kommt eine Benachrichtigung „5 neue Artikel: …".
- Ein lauter Feed soll still bleiben: Tippe den Feed auf der Seite **Feeds** an, wähle **Bearbeiten** und schalte **Benachrichtigungen** aus — andere Feeds benachrichtigen weiterhin.
- Nachts willst du Ruhe: Aktiviere **Ruhezeit (Nicht stören)** von 22:00 bis 07:00 Uhr — in diesem Zeitraum erscheinen keine Benachrichtigungen.

## Einschränkungen

- Lokale Benachrichtigungen gibt es nur auf iOS. Auf anderen Plattformen ist die Funktion wirkungslos — die Schalter lassen sich zwar setzen, es erscheint aber keine Benachrichtigung.
- Sichtbare Benachrichtigungen erzeugt ausschließlich der automatische Hintergrund-Abgleich. Neue Artikel aus einem Abgleich bei geöffneter App (Herunterziehen der Liste, **Aktualisieren** im Feed-Menü, der periodische Abgleich oder der Start-Abruf) stehen in **Ungelesen** bereit, lösen aber bewusst keine Mitteilung aus.
- Der Hintergrund-Abgleich ist an die **Automatische Hintergrund-Aktualisierung** gekoppelt: Ist der Schalter aus, läuft bei geschlossener App kein Abgleich — und es kommen keine Benachrichtigungen. Den tatsächlichen Zeitpunkt bestimmt iOS anhand von Nutzungsverhalten und Energiestatus; das **Abruf-Intervall** gilt nur als Mindestpause, nicht als Garantie. Ist die Option **Hintergrundaktualisierung** in den iOS-Systemeinstellungen für Reporter (oder generell) deaktiviert, läuft der Abgleich nie.
- Während der Ruhezeit verworfene Benachrichtigungen werden nicht nachgeholt.
- Wurde die System-Berechtigung für Benachrichtigungen verweigert, zeigt die Einstellungsseite eine Hinweiszeile mit der Schaltfläche **Einstellungen öffnen**; erst nach dem Freischalten in den iOS-Systemeinstellungen kommen Benachrichtigungen an.
