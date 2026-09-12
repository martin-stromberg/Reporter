← [Zurück zur Übersicht](index.md)

# Einstellungen — Einrichtung

## Zweck

Alle Einstellungen findest du auf dem Tab **Einstellungen**. Jede Änderung wird sofort gespeichert — es gibt keinen separaten Speichern-Schritt.

## Einstellungen

| Einstellung | Bedeutung |
|-------------|-----------|
| **Gelesene Artikel aufbewahren** | Schieberegler von 1 bis 365 Tagen (Voreinstellung 30 Tage). Bestimmt, wie lange gelesene Artikel erhalten bleiben, bevor sie beim App-Start gelöscht werden. |
| **Schlagwort eingeben…** / **+ Hinzufügen** | Legt ein Schlagwort für den Keyword-Filter an. Gelesene Artikel mit diesem Schlagwort in Titel oder Inhalt werden nach Ablauf der Aufbewahrungsfrist gelöscht. |
| **Teilwort, Groß-/Kleinschreibung egal** | Mit dem Badge **Immer aktiv** gekennzeichnet: Der Filter findet das Schlagwort auch als Wortbestandteil und unabhängig von Groß-/Kleinschreibung. Nicht abschaltbar. |
| **Automatische Hintergrund-Aktualisierung** | Ein/Aus (Voreinstellung ein). Lädt alle Feeds periodisch nach, solange die App geöffnet ist. |
| **Abruf-Intervall** | *Alle 15 Minuten*, *Alle 30 Minuten* (Voreinstellung), *Stündlich* oder *Alle 4 Stunden*. Nur aktiv, wenn die automatische Aktualisierung eingeschaltet ist. |
| **Automatisch als gelesen markieren** | Ein/Aus (Voreinstellung ein). Markiert geöffnete Artikel automatisch als gelesen. |
| **Verzögerung bis Markierung** | *Sofort*, *1 Sekunde*, *3 Sekunden* oder *5 Sekunden* (Voreinstellung). Nur aktiv, wenn die automatische Markierung eingeschaltet ist. |
| **Benachrichtigungen** | Ein/Aus (Voreinstellung ein). Grundschalter für Benachrichtigungen. Beim ersten Einschalten fragt iOS die Berechtigung ab; wurde sie verweigert, erscheint eine Hinweiszeile mit der Schaltfläche **Einstellungen öffnen** zu den iOS-Einstellungen der App. Nur auf iOS bedienbar — auf anderen Plattformen ist der Schalter deaktiviert, mit Hinweis auf die iOS-Verfügbarkeit. |
| **Sammel-Benachrichtigung** | Ein/Aus (Voreinstellung aus). Aus = eine Benachrichtigung pro neuem Artikel; Ein = eine gemeinsame Benachrichtigung pro Feed und Abgleich. Nur bei eingeschaltetem Hauptschalter bedienbar. |
| **Ruhezeit (Nicht stören)** | Ein/Aus-Schalter für einen Zeitraum ohne Benachrichtigungen. Schaltet die Uhrzeitfelder **VON** und **BIS** frei; beim ersten Einschalten sind 22:00 bis 07:00 Uhr voreingestellt. Bereiche über Mitternacht (z. B. 22:00–07:00 Uhr) sind zulässig. In der Ruhezeit anfallende Benachrichtigungen werden verworfen, nicht nachgeholt. Beim Ausschalten gilt keine Ruhezeit mehr; die eingestellten Zeiten bleiben sichtbar und werden beim Wiedereinschalten derselben Sitzung wieder verwendet. |
| **Farbschema** | *System* (Voreinstellung), *Hell* oder *Dunkel*. Wirkt sofort auf die gesamte App. |

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

## Hinweise

- Alle Einstellungen gelten anwendungsweit, nicht pro Feed — mit einer Ausnahme: Benachrichtigungen lassen sich zusätzlich pro Feed im Feed-Formular auf der Seite **Feeds** steuern (Schalter **Benachrichtigungen**).
- Benachrichtigungen benötigen auf iOS eine System-Berechtigung; sie wird beim ersten Einschalten des Hauptschalters angefragt (nicht beim App-Start). Bei Verweigerung weist eine Hinweiszeile mit **Einstellungen öffnen** darauf hin; wurde die Berechtigung noch nie angefragt, bietet eine neutrale Zeile **Benachrichtigungen erlauben** den direkten Weg zum System-Dialog.
- Die automatische Hintergrund-Aktualisierung läuft nur bei geöffneter App.
- Ungelesene und für später gemerkte Artikel sind von jeder automatischen Löschung ausgenommen.
- Optionen unter einem ausgeschalteten Schalter (Abruf-Intervall, Verzögerung, Ruhezeit) sind abgedunkelt und nicht bedienbar.
- Der **×**-Schalter zum Entfernen eines Schlagworts trägt eine Screenreader-Beschriftung („Schlagwort … entfernen").
