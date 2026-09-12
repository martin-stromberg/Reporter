<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Benachrichtigungen — Fehlerbehebung

## Es kommen keine Benachrichtigungen an

**Symptom:** Neue Artikel erscheinen unter **Ungelesen**, aber es gibt keine Benachrichtigung.

**Ursache:** Eine der Bedingungen ist nicht erfüllt — Benachrichtigungen ausgeschaltet, System-Berechtigung fehlt, Ruhezeit aktiv, Feed stummgeschaltet oder ein Schlagwort-Filter greift.

**Lösung:**
1. Öffne **Einstellungen** → Karte **Benachrichtigungen & Ruhezeiten** und prüfe, ob **Benachrichtigungen** eingeschaltet ist.
2. Siehst du darunter eine Hinweiszeile *„Benachrichtigungen sind für diese App in den Systemeinstellungen deaktiviert …"*, tippe auf **Einstellungen öffnen** und erlaube Benachrichtigungen in den iOS-Einstellungen der App. Steht dort stattdessen *„Benachrichtigungen sind noch nicht erlaubt …"*, tippe auf **Benachrichtigungen erlauben** — iOS fragt dann direkt nach.
3. Prüfe die **Ruhezeit (Nicht stören)**: Liegt die aktuelle Uhrzeit zwischen **VON** und **BIS**, werden Benachrichtigungen verworfen — sie werden nicht nachgeholt.
4. Öffne **Feeds**, tippe den betroffenen Feed an, wähle **Bearbeiten** und prüfe den Schalter **Benachrichtigungen** des Feeds.
5. Prüfe die **Schlagwort-Filter**: Enthält der Artikel ein eingerichtetes Schlagwort in Titel oder Text, wird er nicht benachrichtigt.
6. Beachte: Benachrichtigungen entstehen nur bei einem Feed-Abgleich. Läuft die automatische Aktualisierung nicht (App geschlossen), gibt es auch keine neuen Artikel und damit keine Benachrichtigung.

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

Wenn trotz eingeschalteter Benachrichtigungen, erteilter iOS-Berechtigung und deaktivierter Ruhezeit weiterhin keine Benachrichtigungen ankommen, oder wenn die App beim Antippen einer Benachrichtigung abstürzt bzw. gar nicht reagiert, wende dich an den Support bzw. die Entwicklung. Gib dabei an, welcher Feed betroffen ist, welche Schalter gesetzt sind und ob die Hinweiszeile in den Einstellungen sichtbar war.
