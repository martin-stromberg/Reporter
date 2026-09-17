<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Einstellungen — Einrichtung

## Zweck

Alle Einstellungen findest du auf dem Tab **Einstellungen**. Jede Änderung wird sofort gespeichert — es gibt keinen separaten Speichern-Schritt.

## Einstellungen

| Einstellung | Bedeutung |
|-------------|-----------|
| **Gelesene Artikel aufbewahren** | Schieberegler von 1 bis 365 Tagen (Voreinstellung 30 Tage). Bestimmt, wie lange gelesene Artikel erhalten bleiben, bevor sie beim App-Start gelöscht werden. |
| **Schlagwort eingeben…** / **+ Hinzufügen** | Legt ein Schlagwort für den Keyword-Filter an. Neue Artikel mit diesem Schlagwort in Titel oder Inhalt werden beim Feed-Abruf verworfen und erscheinen nicht in den Listen. Bereits gespeicherte Treffer bleiben sichtbar, bis sie gelesen wurden und die Aufbewahrungsfrist abgelaufen ist. |
| **Teilwort, Groß-/Kleinschreibung egal** | Mit dem Badge **Immer aktiv** gekennzeichnet: Der Filter findet das Schlagwort auch als Wortbestandteil und unabhängig von Groß-/Kleinschreibung. Nicht abschaltbar. |
| **Automatische Hintergrund-Aktualisierung** | Ein/Aus (Voreinstellung ein). Lädt alle Feeds periodisch nach, solange die App geöffnet ist; auf iOS erlaubt der Schalter zusätzlich den Abgleich durch das System bei geschlossener App — nur dieser Abgleich erzeugt sichtbare Benachrichtigungen. |
| **Abruf-Intervall** | *Alle 15 Minuten*, *Alle 30 Minuten* (Voreinstellung), *Stündlich* oder *Alle 4 Stunden*. Nur aktiv, wenn die automatische Aktualisierung eingeschaltet ist. Für den System-Abgleich bei geschlossener App gilt der Wert als Mindestpause — den tatsächlichen Zeitpunkt bestimmt iOS. |
| **Beim Programmstart abrufen** | Ein/Aus (Voreinstellung ein). Löst beim Öffnen der App einmalig einen Abruf aller Feeds im Hintergrund aus — unabhängig vom Abruf-Intervall; ohne Internetverbindung wird er übersprungen. |
| **Sortierung der ungelesenen Artikel** | *Neueste zuerst* (Voreinstellung) oder *Älteste zuerst*. Legt die Reihenfolge der Liste **Ungelesen** fest; die Liste **Später** bleibt immer nach neuestem Datum sortiert. |
| **Automatisch als gelesen markieren** | Ein/Aus (Voreinstellung ein). Markiert geöffnete Artikel automatisch als gelesen. |
| **Verzögerung bis Markierung** | *Sofort*, *1 Sekunde*, *3 Sekunden* oder *5 Sekunden* (Voreinstellung). Nur aktiv, wenn die automatische Markierung eingeschaltet ist. |
| **Benachrichtigungen** | Ein/Aus (Voreinstellung ein). Grundschalter für Benachrichtigungen. Beim ersten Einschalten fragt iOS die Berechtigung ab; wurde sie verweigert, erscheint eine Hinweiszeile mit der Schaltfläche **Einstellungen öffnen** zu den iOS-Einstellungen der App. Nur auf iOS bedienbar — auf anderen Plattformen ist der Schalter deaktiviert, mit Hinweis auf die iOS-Verfügbarkeit. |
| **Sammel-Benachrichtigung** | Ein/Aus (Voreinstellung aus). Aus = eine Benachrichtigung pro neuem Artikel; Ein = eine gemeinsame Benachrichtigung pro Feed und Abgleich. Nur bei eingeschaltetem Hauptschalter bedienbar. |
| **Ruhezeit (Nicht stören)** | Ein/Aus-Schalter für einen Zeitraum ohne Benachrichtigungen. Schaltet die Uhrzeitfelder **VON** und **BIS** frei; beim ersten Einschalten sind 22:00 bis 07:00 Uhr voreingestellt. Bereiche über Mitternacht (z. B. 22:00–07:00 Uhr) sind zulässig. In der Ruhezeit anfallende Benachrichtigungen werden verworfen, nicht nachgeholt. Beim Ausschalten gilt keine Ruhezeit mehr; die eingestellten Zeiten bleiben sichtbar und werden beim Wiedereinschalten derselben Sitzung wieder verwendet. |
| **Farbschema** | *System* (Voreinstellung), *Hell* oder *Dunkel*. Wirkt sofort auf die gesamte App. |
| **Sprache** | *System* (Voreinstellung), *Deutsch* oder *Englisch*. Bei *System* folgt die App der Gerätesprache (Englisch als Ersatzsprache). Wird sofort gespeichert, wirkt aber erst nach einem Neustart der App — der Hinweis darauf erscheint unter dem Auswahlfeld, sobald die Auswahl geändert wird, und verschwindet bei Rückwahl der bisherigen Sprache. |
| **Debuginformationen sammeln** | Ein/Aus (Voreinstellung aus). Schaltet ein Protokoll ein, das Fehler und App-Ereignisse der aktuellen Sitzung aufzeichnet (z. B. Sync-Fehler, Abstürze, Wechsel in den Hintergrund). Beim App-Start wird das Protokoll zurückgesetzt; nur Fehler- und Absturzeinträge der vorherigen Sitzung bleiben erhalten. |
| **Debugbericht senden** / **Senden** | Öffnet die E-Mail-App des Geräts mit einem vorbefüllten Entwurf (App-/Geräteinformationen, Online-Status, Einstellungen, Feed-Status, letzte Sync- und Protokolleinträge). Du prüfst den Entwurf und sendest ihn selbst ab. Nur aktiv, wenn **Debuginformationen sammeln** eingeschaltet ist und eine E-Mail-App verfügbar ist. |

## Vorgehen

### Einstellung ändern

1. Öffne den Tab **Einstellungen**.
2. Ändere die gewünschte Option per Schalter, Auswahlfeld, Uhrzeitfeld oder Schieberegler.
3. Die Änderung ist sofort gespeichert und wirksam. Beim Schieberegler rastet der Wert auf ganze Tage ein und wird kurz nach der Änderung gespeichert — auch ohne Loslassen.

### Schlagwort hinzufügen

1. Tippe in das Feld **Schlagwort eingeben…** und gib den Begriff ein.
2. Tippe auf **+ Hinzufügen** oder drücke die Eingabetaste.
3. Das Schlagwort erscheint als Chip unter dem Eingabefeld.

Bei ungültiger Eingabe erscheint eine rote Fehlermeldung unter dem Feld:

- *Bitte gib ein Schlagwort ein.* — das Feld war leer.
- *Dieses Schlagwort existiert bereits.* — Dublette; Groß-/Kleinschreibung wird dabei ignoriert.
- *Das Schlagwort darf höchstens 500 Zeichen lang sein.*

### Schlagwort entfernen

1. Tippe auf das **×** im Chip des Schlagworts.
2. Der Chip verschwindet sofort; der Filter greift nicht mehr für diesen Begriff.

### Debugbericht senden

1. Schalte unter **Diagnose & Support** den Schalter **Debuginformationen sammeln** ein — ab jetzt zeichnet die App Fehler und Ereignisse dieser Sitzung auf.
2. Reproduziere bei Bedarf das Problem, das du melden möchtest (z. B. Feed aktualisieren).
3. Tippe auf **Senden**. Die E-Mail-App des Geräts öffnet sich mit dem vorbefüllten Bericht.
4. Prüfe den Entwurf und sende ihn aus der E-Mail-App ab.

> **Hinweis:** Kann die E-Mail-App nicht geöffnet werden, erscheint der Dialog **Senden fehlgeschlagen**. Prüfe dann, ob auf dem Gerät eine E-Mail-App eingerichtet ist, und versuche es erneut.

## Hinweise

- Alle Einstellungen gelten anwendungsweit, nicht pro Feed — mit einer Ausnahme: Benachrichtigungen lassen sich zusätzlich pro Feed auf der Seite **Feeds** steuern (Feed antippen → **Bearbeiten** → Schalter **Benachrichtigungen**).
- Benachrichtigungen benötigen auf iOS eine System-Berechtigung; sie wird beim ersten Einschalten des Hauptschalters angefragt (nicht beim App-Start). Bei Verweigerung weist eine Hinweiszeile mit **Einstellungen öffnen** darauf hin; wurde die Berechtigung noch nie angefragt, bietet eine neutrale Zeile **Benachrichtigungen erlauben** den direkten Weg zum System-Dialog.
- Der periodische Abgleich der **Automatischen Hintergrund-Aktualisierung** läuft innerhalb der App nur bei geöffneter App; auf iOS kann zusätzlich das System bei geschlossener App abgleichen (Voraussetzung: die iOS-Option **Hintergrundaktualisierung** ist für Reporter freigegeben). Der Start-Abruf über **Beim Programmstart abrufen** läuft einmalig beim Öffnen und verzögert den Start nicht.
- Ungelesene und für später gemerkte Artikel sind von jeder automatischen Löschung ausgenommen.
- Optionen unter einem ausgeschalteten Schalter (Abruf-Intervall, Verzögerung, Ruhezeit) sind abgedunkelt und nicht bedienbar.
- Der **×**-Schalter zum Entfernen eines Schlagworts trägt eine Screenreader-Beschriftung („Schlagwort … entfernen").
- Die **Sprache** wirkt erst nach einem Neustart der App: App vollständig schließen und wieder öffnen, damit alle Texte in der gewählten Sprache erscheinen.
- Der Debugbericht verlässt die App nur als E-Mail-Entwurf, den du selbst absendest — es gibt keinen automatischen Versand. Der Entwurf enthält technische Angaben (u. a. Feed-Adressen und Einstellungswerte), aber keine Artikelinhalte.
- Das Protokoll der Fehlersuche ist sitzungsbezogen: Beim App-Start wird es geleert, Fehler- und Absturzeinträge der vorherigen Sitzung bleiben jedoch erhalten und kommen in den nächsten Bericht. Ein Problem, das vor dem Einschalten der Sammlung passiert ist, muss für den Bericht in der laufenden Sitzung erneut auftreten.
