<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Einstellungen — Fehlerbehebung

## Schlagwort lässt sich nicht hinzufügen

**Symptom:** Beim Tippen auf **+ Hinzufügen** erscheint eine rote Fehlermeldung unter dem Eingabefeld, das Schlagwort wird nicht als Chip angelegt.

**Ursache:** Die Eingabe ist ungültig.

**Lösung:**
1. *Bitte gib ein Schlagwort ein.* → Text in das Feld **Schlagwort eingeben…** eingeben.
2. *Dieses Schlagwort existiert bereits.* → Das Schlagwort ist bereits als Chip vorhanden; Groß-/Kleinschreibung spielt dabei keine Rolle.
3. *Das Schlagwort darf höchstens 500 Zeichen lang sein.* → Begriff kürzen.

## Artikel wurde nicht automatisch gelöscht

**Symptom:** Ein Artikel ist älter als die eingestellte Aufbewahrungsdauer, aber noch vorhanden.

**Ursache:** Ungelesene Artikel und für später gemerkte Artikel werden nie automatisch gelöscht. Außerdem läuft das Aufräumen nur beim Start der App.

**Lösung:**
1. App einmal vollständig schließen und neu starten.
2. Prüfen, ob der Artikel ungelesen ist oder mit einem Lesezeichen unter **Später** gemerkt wurde.
3. Beim Keyword-Filter zusätzlich prüfen, ob der Artikel tatsächlich gelesen wurde — gefilterte, aber ungelesene Artikel bleiben erhalten.

## Artikel wird nicht automatisch als gelesen markiert

**Symptom:** Ein geöffneter Artikel bleibt ungelesen.

**Ursache:** Der Schalter **Automatisch als gelesen markieren** in den Einstellungen ist aus, oder der Schalter **Auto-Gelesen** in der Artikeldetailansicht wurde für die aktuelle Ansicht abgeschaltet. Ist der lokale Schalter abgedunkelt und mit *Auto-Gelesen (in den Einstellungen deaktiviert)* beschriftet, ist die globale Option aus.

**Lösung:**
1. In den **Einstellungen** den Schalter **Automatisch als gelesen markieren** einschalten.
2. In der Artikeldetailansicht den Schalter **Auto-Gelesen** prüfen.
3. Gewünschte **Verzögerung bis Markierung** wählen — mit *Sofort* wird direkt beim Öffnen markiert.

## Feeds werden nicht automatisch aktualisiert

**Symptom:** Neue Artikel erscheinen erst nach manuellem Aktualisieren.

**Ursache:** Der Schalter **Automatische Hintergrund-Aktualisierung** ist aus, oder die App wurde zwischenzeitlich geschlossen — die automatische Aktualisierung läuft nur bei geöffneter App.

**Lösung:**
1. In den **Einstellungen** den Schalter **Automatische Hintergrund-Aktualisierung** einschalten.
2. Das gewünschte **Abruf-Intervall** wählen.
3. Alternativ auf der Seite **Feeds** manuell über **Alle aktualisieren** abrufen.

## Es kommen keine Benachrichtigungen

**Symptom:** Neue Artikel erscheinen, aber es gibt keine Benachrichtigung.

**Ursache:** Der Schalter **Benachrichtigungen** ist aus, die iOS-Berechtigung wurde verweigert (Hinweiszeile mit **Einstellungen öffnen** sichtbar) oder noch nie angefragt (Zeile mit **Benachrichtigungen erlauben** sichtbar), eine **Ruhezeit** läuft gerade, der Feed ist einzeln stummgeschaltet oder ein Schlagwort-Filter greift.

**Lösung:**
1. In den **Einstellungen** unter **Benachrichtigungen & Ruhezeiten** den Schalter **Benachrichtigungen** prüfen; bei sichtbarer Hinweiszeile **Benachrichtigungen erlauben** tippen (öffnet den iOS-Dialog) bzw. **Einstellungen öffnen** tippen und die Berechtigung in iOS freischalten.
2. **Ruhezeit (Nicht stören)** und den Feed-Schalter **Benachrichtigungen** (Seite **Feeds** → Feed → **Bearbeiten**) prüfen.
3. Ausführliche Hilfe siehe [Benachrichtigungen — Fehlerbehebung](../benachrichtigungen/fehlerbehebung-anwender.md).

## Erscheinungsbild wechselt nicht

**Symptom:** Die App bleibt hell oder dunkel, obwohl ein anderes **Farbschema** gewählt wurde.

**Ursache:** Bei der Auswahl *System* folgt die App der Einstellung des Betriebssystems.

**Lösung:**
1. Unter **Erscheinungsbild** → **Farbschema** gezielt *Hell* oder *Dunkel* wählen, um das System zu übersteuern.
2. Bei *System* die Einstellung des Geräts prüfen.

## Wann Hilfe nötig ist

Wenn Einstellungen trotz korrekter Bedienung nicht gespeichert bleiben, Artikel wider Erwarten gelöscht werden oder die App Fehler zeigt, die hier nicht beschrieben sind, wende dich an den Support bzw. die Entwicklung und gib an, welche Einstellung betroffen ist und was du zuletzt geändert hast.
