← [Zurück zur Übersicht](index.md)

# Benachrichtigungen — Einrichtung

## Zweck

Die Benachrichtigungen werden über drei Schalter gesteuert: einen globalen Schalter und den Modus-Schalter in den **Einstellungen** sowie einen Schalter pro Feed auf der Seite **Feeds**. Zusätzlich entscheiden die Ruhezeit und die Schlagwort-Filter mit, ob eine Benachrichtigung erscheint.

## Einstellungen

| Einstellung | Ort | Bedeutung |
|-------------|-----|-----------|
| **Benachrichtigungen** | Einstellungen, Karte **Benachrichtigungen & Ruhezeiten** | Ein/Aus (Voreinstellung ein). Hauptschalter für alle Benachrichtigungen. Beim ersten Einschalten fragt iOS die Berechtigung ab. |
| **Sammel-Benachrichtigung** | Einstellungen, Karte **Benachrichtigungen & Ruhezeiten** | Ein/Aus (Voreinstellung aus). Aus = eine Benachrichtigung pro neuem Artikel; Ein = eine gemeinsame Benachrichtigung pro Feed und Abgleich. Nur bei eingeschaltetem Hauptschalter bedienbar. |
| **Ruhezeit (Nicht stören)** mit **VON**/**BIS** | Einstellungen, Karte **Benachrichtigungen & Ruhezeiten** | Zeitraum ohne Benachrichtigungen; Bereiche über Mitternacht sind erlaubt. In der Ruhezeit anfallende Benachrichtigungen werden verworfen. |
| **Benachrichtigungen** | Seite **Feeds**, Formular des Feeds (Anlegen und Bearbeiten) | Ein/Aus (Voreinstellung ein). Schaltet Benachrichtigungen nur für diesen Feed. Wirkt nur, wenn der globale Schalter ebenfalls eingeschaltet ist. |
| **Schlagwort-Filter** | Einstellungen, Karte **Schlagwort-Filter** | Artikel, deren Titel oder Inhalt ein Schlagwort enthält, lösen keine Benachrichtigung aus. |
| System-Berechtigung | iOS-Einstellungen des Geräts | Wird beim ersten Einschalten des Hauptschalters angefragt. Bei Verweigerung erscheint in der Karte eine Hinweiszeile mit **Einstellungen öffnen**. |

## Vorgehen

1. Öffne den Tab **Einstellungen** und schalte in der Karte **Benachrichtigungen & Ruhezeiten** den Schalter **Benachrichtigungen** ein.
2. Bestätige den iOS-Berechtigungsdialog mit **Erlauben**.
3. Wähle optional **Sammel-Benachrichtigung** und/oder eine **Ruhezeit**.
4. Öffne die Seite **Feeds** und stelle je Feed über **Bearbeiten** → **Benachrichtigungen** ein, ob der Feed benachrichtigen darf.

## Hinweise

- Die iOS-Berechtigung wird nicht beim App-Start, sondern beim bewussten Einschalten in den Einstellungen angefragt; iOS zeigt den Dialog nur einmal. Ein späteres Verweigern oder Erlauben geschieht über die Systemeinstellungen — die Hinweiszeile in der App weist bei Bedarf darauf hin.
- Pro Artikel erscheint höchstens eine Benachrichtigung; derselbe Artikel wird bei wiederholten Abgleichen nicht erneut gemeldet.
- Ein Moduswechsel (einzeln ↔ gesammelt) wirkt ab dem nächsten Abgleich; bereits im Mitteilungszentrum liegende Benachrichtigungen bleiben unverändert.
