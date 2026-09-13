<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Einstellungen — Beschreibung

## Zweck

Auf der Seite **Einstellungen** steuerst du, wie Reporter mit deinen Artikeln und Feeds umgeht: wie lange gelesene Artikel aufbewahrt werden, welche Schlagworte als Filter gelten, ob und wie oft Feeds automatisch aktualisiert werden, wann geöffnete Artikel als gelesen markiert werden, ob Benachrichtigungen mit Ruhezeiten gelten, welches Farbschema die App verwendet und in welcher Sprache sie erscheint.

## Funktionsweise

Öffne den Tab **Einstellungen** (bei schmalen Fenstern im Menü **Mehr**). Die Seite ist in sechs Karten gegliedert:

- **Aufbewahrungsdauer & Speicher** — Der Schieberegler **Gelesene Artikel aufbewahren** legt fest, wie viele Tage gelesene Artikel erhalten bleiben (1 bis 365 Tage, Voreinstellung 30 Tage). Beim App-Start entfernt Reporter gelesene Artikel, deren Frist abgelaufen ist. Der Hinweis unter dem Regler weist darauf hin, dass ungelesene und gespeicherte Artikel dauerhaft erhalten bleiben.
- **Schlagwort-Filter** — Über das Feld **Schlagwort eingeben…** und die Schaltfläche **+ Hinzufügen** legst du Schlagworte an, die als Chips unter dem Feld erscheinen. Ein Tipp auf das **×** in einem Chip entfernt das Schlagwort wieder. Gelesene Artikel, die ein Schlagwort in Titel oder Inhalt enthalten, werden nach Ablauf der Aufbewahrungsfrist gelöscht. Neben **Teilwort, Groß-/Kleinschreibung egal** zeigt ein **Immer aktiv**-Badge, dass diese Erkennung fest eingebaut und nicht abschaltbar ist: Der Filter findet Schlagworte auch als Wortbestandteil und unabhängig von Groß- und Kleinschreibung.
- **Synchronisation & Lesefluss** — Der Schalter **Automatische Hintergrund-Aktualisierung** lässt die App Feeds periodisch neu laden, solange sie geöffnet ist. Darunter wählst du das **Abruf-Intervall**: *Alle 15 Minuten*, *Alle 30 Minuten* (Voreinstellung), *Stündlich* oder *Alle 4 Stunden*. Der Schalter **Automatisch als gelesen markieren** steuert, ob ein geöffneter Artikel automatisch als gelesen gilt; die **Verzögerung bis Markierung** wählst du zwischen *Sofort*, *1 Sekunde*, *3 Sekunden* und *5 Sekunden* (Voreinstellung).
- **Benachrichtigungen & Ruhezeiten** — Der Schalter **Benachrichtigungen** schaltet Benachrichtigungen grundsätzlich ein oder aus. Beim ersten Einschalten fragt iOS per System-Dialog nach der Berechtigung; wurde sie noch nie angefragt, erscheint eine neutrale Hinweiszeile mit der Schaltfläche **Benachrichtigungen erlauben**, die den System-Dialog auslöst; wurde sie verweigert, erscheint eine Hinweiszeile mit der Schaltfläche **Einstellungen öffnen**, die direkt in die iOS-Einstellungen der App führt. Auf Plattformen ohne Benachrichtigungs-Unterstützung sind die Schalter abgedunkelt und ein Hinweis „derzeit nur auf iOS verfügbar" wird eingeblendet. Darunter wählst du mit **Sammel-Benachrichtigung**, ob pro neuem Artikel eine eigene Benachrichtigung kommt oder eine gemeinsame pro Feed. Der Schalter **Ruhezeit (Nicht stören)** aktiviert einen Zeitraum, in dem keine Benachrichtigungen kommen; die Uhrzeitfelder **VON** und **BIS** lassen sich nur bei eingeschalteter Ruhezeit bedienen. Beim ersten Einschalten sind 22:00 bis 07:00 Uhr voreingestellt. Schaltest du die Ruhezeit aus, bleiben deine zuletzt gewählten Zeiten sichtbar und werden beim Wiedereinschalten innerhalb derselben Sitzung wieder verwendet. Details zum Benachrichtigungsverhalten siehe [Benachrichtigungen](../benachrichtigungen/index.md).
- **Erscheinungsbild** — Der Auswahldialog **Farbschema** bietet *System* (Voreinstellung), *Hell* und *Dunkel*. Die Auswahl wirkt sofort auf die gesamte App.
- **Sprache** — Das Auswahlfeld **Sprache** bietet *System* (Voreinstellung), *Deutsch* und *Englisch*. Bei *System* folgt die App der Gerätesprache (Englisch als Ersatzsprache bei anderen Systemsprachen). Die Auswahl wird sofort gespeichert, wirkt aber erst nach einem Neustart der App — darauf weist der Hinweis „Die neue Sprache wird nach einem Neustart der App wirksam." unter dem Auswahlfeld hin. Details siehe [Sprache](../anwendung/sprache.md).

Jede Änderung wird sofort gespeichert — nur die Sprachauswahl wirkt zusätzlich erst nach einem Neustart. Beim Schieberegler für die Aufbewahrungsdauer rastet der Wert auf ganze Tage ein und wird nach jeder Änderung kurz verzögert gespeichert — nicht nur beim Loslassen des Reglers. Optionen, die zu einem ausgeschalteten Schalter gehören (z. B. das Abruf-Intervall bei deaktivierter Hintergrund-Aktualisierung oder die Uhrzeitfelder bei ausgeschalteter Ruhezeit), erscheinen abgedunkelt und lassen sich nicht bedienen.

## Beispiele

- Du möchtest keine Sportartikel mehr vorhalten: Lege das Schlagwort „Fußball" an. Gelesene Artikel mit „Fußball" im Titel oder Text werden nach Ablauf der Aufbewahrungsfrist automatisch entfernt.
- Du liest abends im Dunkeln: Stelle das **Farbschema** auf *Dunkel* — die App wechselt sofort das Erscheinungsbild, ohne auf die Systemeinstellung zu warten.
- Dein Gerät läuft auf Englisch, du möchtest die App aber auf Deutsch: Stelle die **Sprache** auf *Deutsch* und starte die App neu — alle Texte erscheinen danach auf Deutsch.
- Du willst Artikel bewusst als gelesen markieren: Schalte **Automatisch als gelesen markieren** aus. Geöffnete Artikel bleiben dann ungelesen, bis du sie selbst markierst.
- Du willst Benachrichtigungen nachts nicht hören: Schalte die **Ruhezeit (Nicht stören)** ein und stelle **VON** auf 22:00 und **BIS** auf 07:00 Uhr.
- Ein Feed postet sehr häufig: Schalte **Sammel-Benachrichtigung** ein — pro Abgleich kommt dann nur eine gemeinsame Benachrichtigung („3 neue Artikel: …").

## Einschränkungen

- Der Keyword-Filter löscht nur **gelesene** Artikel. Ungelesene Artikel und für später gemerkte Artikel werden nie automatisch gelöscht — auch nicht durch den Keyword-Filter.
- Gefilterte Artikel werden nicht ausgeblendet, sondern erst nach Ablauf der Aufbewahrungsfrist gelöscht; die Frist zählt dabei ab dem Veröffentlichungsdatum des Artikels.
- Das Aufräumen läuft einmalig beim App-Start, nicht kontinuierlich im Hintergrund.
- Die automatische Hintergrund-Aktualisierung läuft nur, solange die App geöffnet ist. Nach dem Schließen der App werden Feeds erst beim nächsten Öffnen wieder aktualisiert.
- Benachrichtigungen gibt es nur auf iOS; auf anderen Plattformen sind die Schalter deaktiviert und ein entsprechender Hinweis wird eingeblendet. Während der Ruhezeit verworfene Benachrichtigungen werden nicht nachgeholt. Pro Feed lässt sich der Empfang zusätzlich im Feed-Formular auf der Seite **Feeds** stummschalten.
- Die Sprachauswahl wirkt erst nach einem Neustart der App — es gibt keine Umschaltung im laufenden Betrieb. Zur Auswahl stehen nur Deutsch und Englisch.
- Der Schalter **Auto-Gelesen** in der Artikeldetailansicht kann die automatische Markierung zusätzlich für die aktuelle Lesesitzung abwählen. Ist **Automatisch als gelesen markieren** in den Einstellungen aus, ist der Schalter in der Detailansicht abgedunkelt und nicht bedienbar; die Beschriftung lautet dann *Auto-Gelesen (in den Einstellungen deaktiviert)*.
