<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Benachrichtigungen — Beschreibung

## Zweck

Reporter benachrichtigt dich auf dem iPhone/iPad über neue Artikel in deinen Feeds — mit Banner, Ton und Mitteilungszentrale, auch während die App geöffnet ist. So entgeht dir kein neuer Beitrag, ohne dass du die Feeds manuell aktualisieren musst.

## Funktionsweise

Nach jedem Feed-Abgleich — egal ob manuell über **Aktualisieren** im Feed-Kontextmenü, das Herunterziehen der Feed-Liste oder automatisch im Hintergrund — prüft die App, ob neue Artikel gespeichert wurden. Für jeden neuen Artikel entscheiden vier Bedingungen, ob eine Benachrichtigung erscheint:

1. **Benachrichtigungen** in den **Einstellungen** (Karte **Benachrichtigungen & Ruhezeiten**) müssen eingeschaltet sein.
2. Der Schalter **Benachrichtigungen** des betreffenden Feeds (Seite **Feeds** → Feed antippen → **Bearbeiten**) muss eingeschaltet sein — so lassen sich einzelne Feeds stummschalten.
3. Es darf gerade keine **Ruhezeit (Nicht stören)** laufen. Benachrichtigungen in der Ruhezeit werden verworfen, nicht nachgeholt.
4. Der Artikel darf kein Filter-Schlagwort in Titel oder Inhalt enthalten — dafür gilt dieselbe Schlagwort-Liste wie beim automatischen Löschen.

Pro Artikel erscheint höchstens eine Benachrichtigung. Der Titel der Benachrichtigung ist der Feed-Name, der Text der Artikeltitel.

Über den Schalter **Sammel-Benachrichtigung** in den Einstellungen wählst du stattdessen eine einzige Benachrichtigung pro Feed und Abgleich (z. B. „3 neue Artikel: Titel 1, Titel 2, …").

Tippst du eine Benachrichtigung an, öffnet die App direkt den Artikel; bei einer Sammel-Benachrichtigung öffnet sich die Ansicht **Ungelesen**.

## Beispiele

- Du abonnierst einen News-Feed: Bei jedem neuen Artikel erscheint sofort eine Benachrichtigung mit dem Feed-Namen und dem Artikeltitel.
- Ein Feed postet sehr häufig: Schalte **Sammel-Benachrichtigung** ein — statt fünf Einzelmeldungen kommt eine Benachrichtigung „5 neue Artikel: …".
- Ein lauter Feed soll still bleiben: Tippe den Feed auf der Seite **Feeds** an, wähle **Bearbeiten** und schalte **Benachrichtigungen** aus — andere Feeds benachrichtigen weiterhin.
- Nachts willst du Ruhe: Aktiviere **Ruhezeit (Nicht stören)** von 22:00 bis 07:00 Uhr — in diesem Zeitraum erscheinen keine Benachrichtigungen.

## Einschränkungen

- Lokale Benachrichtigungen gibt es nur auf iOS. Auf anderen Plattformen ist die Funktion wirkungslos — die Schalter lassen sich zwar setzen, es erscheint aber keine Benachrichtigung.
- Benachrichtigungen entstehen nur bei einem Feed-Abgleich. Die automatische Hintergrund-Aktualisierung läuft nur bei geöffneter App — ist die App geschlossen, werden neue Artikel (und damit Benachrichtigungen) erst beim nächsten Öffnen erzeugt.
- Während der Ruhezeit verworfene Benachrichtigungen werden nicht nachgeholt.
- Wurde die System-Berechtigung für Benachrichtigungen verweigert, zeigt die Einstellungsseite eine Hinweiszeile mit der Schaltfläche **Einstellungen öffnen**; erst nach dem Freischalten in den iOS-Systemeinstellungen kommen Benachrichtigungen an.
