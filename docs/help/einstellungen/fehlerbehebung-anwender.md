<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Einstellungen — Fehlerbehebung

## Schlagwort lässt sich nicht hinzufügen

**Symptom:** Beim Tippen auf **+ Hinzufügen** erscheint eine rote Fehlermeldung unter dem Eingabefeld, das Schlagwort wird nicht als Chip angelegt.

**Ursache:** Die Eingabe ist ungültig.

**Lösung:**
1. *Bitte gib ein Schlagwort ein.* → Text in das Feld **Schlagwort eingeben…** eingeben.
2. *Dieses Schlagwort existiert bereits.* → Das Schlagwort ist bereits als Chip vorhanden; Groß-/Kleinschreibung spielt dabei keine Rolle. Die Prüfung gilt pro Liste: Dasselbe Schlagwort darf gleichzeitig global (Einstellungen) und in einem Feed (Formular **Feed bearbeiten**) existieren — dort gilt jeweils eine eigene Duplikat-Prüfung.
3. *Das Schlagwort darf höchstens 500 Zeichen lang sein.* → Begriff kürzen.

> **Hinweis:** Dieselbe Prüfung und dieselben Meldungen gelten für die Schlagwort-Eingabe im Formular **Feed bearbeiten** der Feeddetailansicht — dort angelegte Schlagworte erscheinen nur in diesem Feed und nicht in der Liste der Einstellungen.

## Ein Schlagwort greift nicht wie erwartet

**Symptom:** Neue Artikel eines Feeds werden nicht gefiltert, obwohl das Schlagwort eingerichtet ist — oder Artikel anderer Feeds werden unerwartet gefiltert.

**Ursache:** Der Geltungsbereich passt nicht zur Absicht: Die Schlagwort-Liste in den **Einstellungen** gilt für alle Feeds; ein Schlagwort im Formular **Feed bearbeiten** gilt nur für diesen einen Feed. Für jeden Feed wirken beide Listen zusammen — Feed-Schlagworte können den globalen Filter nicht aufheben, nur ergänzen.

**Lösung:**
1. Soll das Schlagwort alle Feeds betreffen, lege es in den **Einstellungen** unter **Schlagwort-Filter** an.
2. Soll es nur einen Feed betreffen, lege es auf der Seite **Feeds** → Feed antippen → **Aktionen** → **Bearbeiten** unter **Schlagwort-Filter** an — und entferne eine gleichnamige globale Zeile in den **Einstellungen**, falls andere Feeds betroffen sind.
3. Beachte, dass ein frisch angelegtes Schlagwort nur Neuzugänge ab dem nächsten Abruf verwirft; bereits gespeicherte Artikel bleiben sichtbar, bis sie gelesen wurden und die Aufbewahrungsfrist abläuft.

## Artikel wurde nicht automatisch gelöscht

**Symptom:** Ein Artikel ist älter als die eingestellte Aufbewahrungsdauer, aber noch vorhanden.

**Ursache:** Ungelesene Artikel und für später gemerkte Artikel werden nie automatisch gelöscht. Außerdem läuft das Aufräumen nur beim Start der App.

**Lösung:**
1. App einmal vollständig schließen und neu starten.
2. Prüfen, ob der Artikel ungelesen ist oder mit einem Lesezeichen unter **Später** gemerkt wurde.
3. Beim Keyword-Filter zusätzlich beachten: Neu abgerufene Treffer werden bereits beim Abruf verworfen und gar nicht erst gespeichert — sie können daher nicht gelöscht werden. Nur bereits gespeicherte Treffer entfernt das Aufräumen, und auch diese nur, wenn sie gelesen wurden.

## Artikel wird nicht automatisch als gelesen markiert

**Symptom:** Ein geöffneter Artikel bleibt ungelesen.

**Ursache:** Der Schalter **Automatisch als gelesen markieren** in den Einstellungen ist aus, oder der Schalter **Auto-Gelesen** in der Artikeldetailansicht wurde für die aktuelle Ansicht abgeschaltet. Ist der lokale Schalter abgedunkelt und mit *Auto-Gelesen (in den Einstellungen deaktiviert)* beschriftet, ist die globale Option aus.

**Lösung:**
1. In den **Einstellungen** den Schalter **Automatisch als gelesen markieren** einschalten.
2. In der Artikeldetailansicht den Schalter **Auto-Gelesen** prüfen.
3. Gewünschte **Verzögerung bis Markierung** wählen — mit *Sofort* wird direkt beim Öffnen markiert.

## Feeds werden nicht automatisch aktualisiert

**Symptom:** Neue Artikel erscheinen erst nach manuellem Aktualisieren.

**Ursache:** Der Schalter **Automatische Hintergrund-Aktualisierung** ist aus, oder die App wurde zwischenzeitlich geschlossen — der periodische Abgleich innerhalb der App läuft nur bei geöffneter App. Auf iOS kann zwar auch das System bei geschlossener App abgleichen, legt den Zeitpunkt aber selbst fest und benötigt die Freigabe **Hintergrundaktualisierung** in den iOS-Einstellungen. Sollen die Feeds bereits beim Öffnen der App abgerufen werden, ist zusätzlich der Schalter **Beim Programmstart abrufen** zuständig; ohne Internetverbindung wird dieser Abruf übersprungen.

**Lösung:**
1. In den **Einstellungen** den Schalter **Automatische Hintergrund-Aktualisierung** einschalten.
2. Das gewünschte **Abruf-Intervall** wählen; für einen Abruf beim App-Start **Beim Programmstart abrufen** einschalten.
3. Auf iOS zusätzlich in den Systemeinstellungen unter **Allgemein** → **Hintergrundaktualisierung** prüfen, ob die Funktion für Reporter freigegeben ist — sonst läuft der Abgleich bei geschlossener App nie. Das **Abruf-Intervall** ist dabei nur eine Mindestpause; iOS kann den Abgleich deutlich später ausführen.
4. Alternativ auf der Seite **Feeds** die Liste nach unten ziehen (Ziehen zum Aktualisieren) oder einen einzelnen Feed über dessen Detailansicht abrufen (**Feeds** → Feed antippen → **Aktionen** → **Aktualisieren**).

## Feed zeigt Status „Fehler“ — „Unlesbares Feed-Format"

**Symptom:** Eine Feed-Karte trägt das rote Badge **Fehler**; der Dialog **Synchronisierungsfehler** (über **Fehlerdetails anzeigen** im Menü **Feed-Aktionen**) nennt als Grund „Unlesbares Feed-Format".

**Ursache:** Die hinterlegte Adresse liefert Daten, die die App nicht als Feed lesen kann — etwa eine normale Webseite statt eines Feeds oder ein Feed-Format, das die App nicht kennt. Gelesen werden Feeds im RSS- und Atom-Format, einschließlich des älteren Atom-Formats (Atom 0.3).

**Lösung:**
1. Prüfe im Dialog **Synchronisierungsfehler** die technische Meldung im zweiten Absatz — sie verrät, wo das Lesen scheiterte.
2. Vergewissere dich, dass die Adresse wirklich einen Feed liefert: Öffne sie im Browser — ein Feed zeigt Daten im XML-Stil, keine normale Webseite.
3. Hast du eine Website-Adresse statt der eigentlichen Feed-Adresse eingetragen, korrigiere sie über **Feed-Aktionen** → **Bearbeiten** — oder lösche den Feed und füge ihn über **Suchen** neu hinzu: Die App findet die richtige Feed-Adresse meist selbst.
4. Bleibt der Fehler bestehen, nutzt der Anbieter möglicherweise ein nicht unterstütztes Format — notiere die Feed-Adresse und wende dich an den Support bzw. die Entwicklung.

## Ungelesene Artikel erscheinen in unerwünschter Reihenfolge

**Symptom:** Die Liste **Ungelesen** zeigt ältere Artikel zuerst — oder umgekehrt.

**Ursache:** Die Reihenfolge folgt der Einstellung **Sortierung der ungelesenen Artikel** in den **Einstellungen**.

**Lösung:**
1. In den **Einstellungen** unter **Synchronisation & Lesefluss** das Auswahlfeld **Sortierung der ungelesenen Artikel** öffnen.
2. *Neueste zuerst* oder *Älteste zuerst* wählen — die Änderung gilt sofort beim nächsten Laden der Liste.

## Es kommen keine Benachrichtigungen

**Symptom:** Neue Artikel erscheinen, aber es gibt keine Benachrichtigung.

**Ursache:** Der Schalter **Benachrichtigungen** ist aus, die iOS-Berechtigung wurde verweigert (Hinweiszeile mit **Einstellungen öffnen** sichtbar) oder noch nie angefragt (Zeile mit **Benachrichtigungen erlauben** sichtbar), eine **Ruhezeit** läuft gerade, der Feed ist einzeln stummgeschaltet oder ein Schlagwort-Filter greift. Außerdem erscheinen Mitteilungen nur aus dem Hintergrund-Abgleich bei geschlossener App — dafür muss die **Automatische Hintergrund-Aktualisierung** eingeschaltet und die iOS-Option **Hintergrundaktualisierung** freigegeben sein; Abgleiche bei geöffneter App erzeugen bewusst keine Mitteilung.

**Lösung:**
1. In den **Einstellungen** unter **Benachrichtigungen & Ruhezeiten** den Schalter **Benachrichtigungen** prüfen; bei sichtbarer Hinweiszeile **Benachrichtigungen erlauben** tippen (öffnet den iOS-Dialog) bzw. **Einstellungen öffnen** tippen und die Berechtigung in iOS freischalten.
2. **Ruhezeit (Nicht stören)** und den Feed-Schalter **Benachrichtigungen** (Seite **Feeds** → Feed antippen → **Aktionen** → **Bearbeiten**) prüfen.
3. Unter **Synchronisation & Lesefluss** die **Automatische Hintergrund-Aktualisierung** prüfen und in den iOS-Einstellungen (**Allgemein** → **Hintergrundaktualisierung**) die Freigabe für Reporter sicherstellen.
4. Ausführliche Hilfe siehe [Benachrichtigungen — Fehlerbehebung](../benachrichtigungen/fehlerbehebung-anwender.md).

## Erscheinungsbild wechselt nicht

**Symptom:** Die App bleibt hell oder dunkel, obwohl ein anderes **Farbschema** gewählt wurde.

**Ursache:** Bei der Auswahl *System* folgt die App der Einstellung des Betriebssystems.

**Lösung:**
1. Unter **Erscheinungsbild** → **Farbschema** gezielt *Hell* oder *Dunkel* wählen, um das System zu übersteuern.
2. Bei *System* die Einstellung des Geräts prüfen.

## Sprache wechselt nicht

**Symptom:** Nach der Auswahl einer anderen **Sprache** erscheinen die Texte weiterhin in der bisherigen Sprache.

**Ursache:** Die Sprachauswahl wird zwar sofort gespeichert, wirkt aber erst nach einem Neustart der App — darauf weist der Hinweis „Die neue Sprache wird nach einem Neustart der App wirksam." unter dem Auswahlfeld hin. Bei der Auswahl *System* folgt die App außerdem der Systemsprache des Geräts.

**Lösung:**
1. App vollständig schließen und neu starten.
2. Prüfen, ob die gewünschte Option (*Deutsch* oder *Englisch*) unter **Sprache** ausgewählt ist — bei *System* gilt die Gerätesprache.
3. Steht die Gerätesprache auf einer anderen Sprache als Deutsch oder Englisch, zeigt die App bei *System* Englisch an; für Deutsch dann *Deutsch* wählen.

## Schaltfläche „Senden“ für den Debugbericht ist abgedunkelt

**Symptom:** Unter **Diagnose & Support** lässt sich die Schaltfläche **Senden** nicht antippen; darunter steht ein Hinweistext.

**Ursache:** Der Versand ist nur möglich, wenn **Debuginformationen sammeln** eingeschaltet ist — der Hinweis „Aktiviere zuerst …" erklärt das. Steht stattdessen „Auf diesem Gerät ist keine E-Mail-App verfügbar …", ist auf dem Gerät keine E-Mail-App eingerichtet.

**Lösung:**
1. Den Schalter **Debuginformationen sammeln** einschalten — danach wird **Senden** aktiv.
2. Beachten: Das Protokoll umfasst die aktuelle Sitzung (plus übernommene Absturzinformationen der vorherigen). Ein Problem, das vor dem Einschalten passiert ist, muss für den Bericht erneut auftreten.
3. Fehlt eine E-Mail-App, richte auf dem Gerät eine E-Mail-App mit einem Konto ein.

## Dialog „Senden fehlgeschlagen“ beim Debugbericht

**Symptom:** Nach dem Tippen auf **Senden** erscheint der Dialog **Senden fehlgeschlagen** statt des E-Mail-Entwurfs.

**Ursache:** Die E-Mail-App des Geräts konnte nicht geöffnet werden — typischerweise ist keine E-Mail-App eingerichtet oder dem System ist kein Mail-Programm zugeordnet.

**Lösung:**
1. Prüfen, ob auf dem Gerät eine E-Mail-App mit eingerichtetem Konto vorhanden ist.
2. E-Mail-App einmal manuell öffnen und erneut **Senden** tippen.
3. Funktioniert es weiterhin nicht, den Vorgang notieren und den Support informieren (siehe unten).

## Wann Hilfe nötig ist

Wenn Einstellungen trotz korrekter Bedienung nicht gespeichert bleiben, Artikel wider Erwarten gelöscht werden oder die App Fehler zeigt, die hier nicht beschrieben sind, wende dich an den Support bzw. die Entwicklung und gib an, welche Einstellung betroffen ist und was du zuletzt geändert hast.
