<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Benachrichtigungen — Fehlerbehebung

## Es kommen keine Benachrichtigungen an

**Symptom:** Neue Artikel erscheinen unter **Ungelesen**, aber es gibt keine Benachrichtigung.

**Ursache:** Eine der Bedingungen ist nicht erfüllt — Benachrichtigungen ausgeschaltet, System-Berechtigung fehlt, Ruhezeit aktiv, Feed stummgeschaltet oder ein Schlagwort-Filter greift. Häufig ist es aber schlicht der Abgleich-Zeitpunkt: Benachrichtigungen erscheinen nur, wenn der **automatische Hintergrund-Abgleich** neue Artikel findet — bei geöffneter App erscheint absichtlich keine Mitteilung.

**Lösung:**
1. Öffne **Einstellungen** → Karte **Benachrichtigungen & Ruhezeiten** und prüfe, ob **Benachrichtigungen** eingeschaltet ist.
2. Siehst du darunter eine Hinweiszeile *„Benachrichtigungen sind für diese App in den Systemeinstellungen deaktiviert …"*, tippe auf **Einstellungen öffnen** und erlaube Benachrichtigungen in den iOS-Einstellungen der App. Steht dort stattdessen *„Benachrichtigungen sind noch nicht erlaubt …"*, tippe auf **Benachrichtigungen erlauben** — iOS fragt dann direkt nach.
3. Prüfe die **Ruhezeit (Nicht stören)**: Liegt die aktuelle Uhrzeit zwischen **VON** und **BIS**, werden Benachrichtigungen verworfen — sie werden nicht nachgeholt.
4. Öffne **Feeds**, tippe den betroffenen Feed an, wähle **Bearbeiten** und prüfe den Schalter **Benachrichtigungen** des Feeds.
5. Prüfe die **Schlagwort-Filter**: Enthält der Artikel ein eingerichtetes Schlagwort in Titel oder Text, wird er nicht benachrichtigt — neue Treffer werden bereits beim Abruf verworfen und erscheinen gar nicht erst in den Listen.
6. Prüfe in der Karte **Synchronisation & Lesefluss**, ob die **Automatische Hintergrund-Aktualisierung** eingeschaltet ist — ohne sie läuft bei geschlossener App kein Abgleich und es kommen keine Benachrichtigungen.
7. Prüfe in den iOS-Einstellungen unter **Allgemein** → **Hintergrundaktualisierung**, ob die Funktion für Reporter freigegeben ist, und schalte den **Energiesparmodus** aus — dieser deaktiviert die Hintergrundaktualisierung. Beachte außerdem: iOS legt den Zeitpunkt des Hintergrund-Abgleichs selbst fest — das gewählte **Abruf-Intervall** ist eine Mindestpause, Benachrichtigungen können deutlich später kommen. Beende die App zudem nicht per Wischen aus dem App-Umschalter — nach einem solchen Beenden führt iOS bis zum nächsten Öffnen keinen Hintergrund-Abgleich aus.

## Bei geöffneter App erscheint keine Benachrichtigung

**Symptom:** Du aktualisierst die Feeds per Herunterziehen oder über **Aktualisieren** im Feed-Menü, neue Artikel erscheinen in **Ungelesen** — aber es gibt weder Banner noch Ton.

**Ursache:** Das ist beabsichtigt: Solange die App geöffnet ist, werden Benachrichtigungen vollständig unterdrückt — auch beim automatischen Timer-Abgleich und beim Abgleich beim Programmstart. Sichtbare Mitteilungen kommen nur aus dem Hintergrund-Abgleich, den iOS ausführt, während die App nicht geöffnet ist.

**Lösung:**
1. Kein Fehler — die Artikel stehen in **Ungelesen** bereit.
2. Sollen Mitteilungen bei geschlossener App kommen, prüfe die **Automatische Hintergrund-Aktualisierung** in den Einstellungen und die iOS-Option **Hintergrundaktualisierung** (siehe oben).

## Beim Einschalten kommt kein Berechtigungsdialog

**Symptom:** Du schaltest **Benachrichtigungen** ein, aber iOS fragt nicht nach der Erlaubnis.

**Ursache:** iOS fragt nur einmal pro App-Installation. War die Erlaubnis schon erteilt oder verweigert, kommt kein Dialog mehr.

**Lösung:**
1. Erscheint die Hinweiszeile mit **Benachrichtigungen erlauben**, tippe darauf — iOS zeigt den Dialog.
2. Erscheint die Hinweiszeile mit **Einstellungen öffnen**, war die Berechtigung verweigert — dort freischalten.
3. Erscheint keine Hinweiszeile, ist die Berechtigung bereits erteilt — alles in Ordnung.

## Statt vieler Benachrichtigungen kommt nur eine

**Symptom:** Bei mehreren neuen Artikeln erscheint nur eine Benachrichtigung.

**Ursache:** Der Schalter **Sammel-Benachrichtigung** ist eingeschaltet — dann gibt es bewusst nur eine gemeinsame Meldung pro Feed und Abgleich (z. B. „3 neue Artikel: …").

**Lösung:**
1. Für Einzel-Benachrichtigungen pro Artikel **Sammel-Benachrichtigung** in den Einstellungen ausschalten.

## Beim Antippen öffnet sich der Browser statt der App

**Symptom:** Ein Tipp auf eine Benachrichtigung öffnet den Artikel im Browser.

**Ursache:** Wurde die App durch das Antippen kalt gestartet, ist die App-Navigation manchmal noch nicht bereit — dann öffnet die App den Artikel-Link ersatzweise im Browser.

**Lösung:**
1. Kein Fehler: Beim nächsten Antippen aus geöffneter App heraus gelangst du direkt zum Artikel bzw. zur Ansicht **Ungelesen**.

## Wann Hilfe nötig ist

Wenn trotz eingeschalteter Benachrichtigungen, erteilter iOS-Berechtigung und deaktivierter Ruhezeit weiterhin keine Benachrichtigungen ankommen, oder wenn die App beim Antippen einer Benachrichtigung abstürzt bzw. gar nicht reagiert, wende dich an den Support bzw. die Entwicklung. Gib dabei an, welcher Feed betroffen ist, welche Schalter gesetzt sind und ob die Hinweiszeile in den Einstellungen sichtbar war. Hilfreich ist ein Debugbericht: aktiviere dazu **Debuginformationen sammeln** in den Einstellungen, warte das gewählte Abruf-Intervall ab und sende den Bericht anschließend über **Debugbericht senden** — er zeigt, ob der Hintergrund-Abgleich eingeplant und von iOS ausgeführt wurde und ob eine Benachrichtigung abgesetzt wurde.
