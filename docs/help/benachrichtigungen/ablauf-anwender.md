<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Benachrichtigungen — Ablauf für Anwender

## Voraussetzungen

- Reporter ist auf einem iOS-Gerät installiert.
- Mindestens ein Feed ist auf der Seite **Feeds** angelegt.
- Die **Automatische Hintergrund-Aktualisierung** ist in den **Einstellungen** eingeschaltet — nur dieser Abgleich erzeugt sichtbare Benachrichtigungen, weil iOS ihn auch bei geschlossener App ausführen kann.

## Schritt-für-Schritt-Anleitung

### 1. Benachrichtigungen einschalten

1. Öffne den Tab **Einstellungen** (bei schmalen Fenstern im Menü **Mehr**).
2. Scrolle zur Karte **Benachrichtigungen & Ruhezeiten**.
3. Schalte **Benachrichtigungen** („Bei neuen Artikeln benachrichtigen") ein.
4. Beim ersten Einschalten fragt iOS per System-Dialog, ob die App Benachrichtigungen senden darf — tippe auf **Erlauben**.

> **Hinweis:** Verweigerst du die Berechtigung, erscheint in der Karte eine Hinweiszeile (*Benachrichtigungen sind für diese App in den Systemeinstellungen deaktiviert …*) mit der Schaltfläche **Einstellungen öffnen**. Darüber gelangst du direkt in die iOS-Einstellungen der App und kannst Benachrichtigungen nachträglich freischalten.

### 2. Optional: Sammel-Benachrichtigung wählen

- Schalte **Sammel-Benachrichtigung** („Eine Benachrichtigung pro Feed statt pro Artikel") ein, wenn du pro Feed-Abgleich nur eine gemeinsame Meldung erhalten möchtest.
- Der Schalter ist nur bei eingeschalteten Benachrichtigungen bedienbar; bei ausgeschaltetem Hauptschalter ist er abgedunkelt.

### 3. Optional: Ruhezeit festlegen

- Schalte **Ruhezeit (Nicht stören)** ein und wähle über **VON** und **BIS** den Zeitraum ohne Benachrichtigungen (z. B. 22:00 bis 07:00 Uhr — Bereiche über Mitternacht sind erlaubt).

### 4. Optional: Einzelne Feeds stummschalten

1. Öffne die Seite **Feeds**.
2. Tippe auf den Feed und wähle im Aktionsmenü **Bearbeiten**.
3. Schalte **Benachrichtigungen** („Bei neuen Artikeln dieses Feeds benachrichtigen") aus und tippe auf **Speichern**.

### 5. Benachrichtigung empfangen und öffnen

- Erscheint eine Benachrichtigung, hat der automatische Hintergrund-Abgleich neue Artikel gefunden — also während die App nicht geöffnet war. Wann iOS den Abgleich tatsächlich ausführt, bestimmt das System; das **Abruf-Intervall** ist dabei nur eine Mindestpause.
- Solange du die App verwendest, erscheint bei neuen Artikeln bewusst keine Mitteilung — weder beim manuellen Aktualisieren noch beim Timer- oder Start-Abgleich. Die Artikel stehen dann direkt in der Ansicht **Ungelesen** bereit.
- Tippe die Benachrichtigung an: Bei einer Einzel-Benachrichtigung öffnet sich der Artikel in der App; bei einer Sammel-Benachrichtigung die Ansicht **Ungelesen**.

## Ergebnis

Du erhältst bei neuen Artikeln eine Benachrichtigung mit Feed-Name und Artikeltitel (oder eine Sammel-Meldung pro Feed), sobald der Hintergrund-Abgleich neue Artikel findet, und gelangst per Antippen direkt zum Artikel bzw. zur Ungelesen-Liste. Bei geöffneter App bleibt die App still — neue Artikel erkennst du dort an der Ungelesen-Liste.

## Barrierefreiheit

- Alle neuen Schalter tragen eine Screenreader-Beschriftung (die Beschriftung der jeweiligen Zeile) und erfüllen die Mindest-Tippfläche von 44 × 44 pt.
- Die Schaltfläche **Einstellungen öffnen** in der Berechtigungs-Hinweiszeile ist ebenfalls per Screenreader erreichbar.
