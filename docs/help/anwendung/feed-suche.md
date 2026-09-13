<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Feeds suchen und hinzufügen

Auf der Seite **Feeds** findest du neue Feeds, indem du eine Website-Adresse oder Feed-URL eingibst — du musst die genaue Feed-Adresse nicht kennen.

## Zweck

Statt die Feed-Adresse einer Website mühsam herauszusuchen, gibst du einfach die Adresse der Website ein. Die App sucht dann die zugehörigen Feeds und zeigt sie dir zur Auswahl an. Wer die Feed-Adresse bereits kennt, kann sie weiterhin direkt eintragen.

## Funktionsweise

Das oberste Eingabefeld **Feed-URL oder Website-Adresse…** dient als kombinierte Such- und URL-Eingabe:

- Gibst du eine Website-Adresse (z. B. `tagesschau.de`) oder eine vollständige Adresse mit `https://…` ein und tippst auf **Suchen**, durchsucht die App ein öffentliches Feed-Verzeichnis sowie die Website selbst nach verfügbaren Feeds.
- Die gefundenen Feeds erscheinen als Liste von Karten. Jede Karte zeigt den Titel, ggf. eine kurze Beschreibung, den Namen und die Adresse der Website sowie die Feed-Adresse.
- Unterhalb der Liste steht der Hinweis **„Suche powered by feedsearch.dev"** für den verwendeten Suchdienst.
- Über **Zurück zu meinen Feeds** kehrst du jederzeit aus der Trefferansicht zu deiner Feed-Liste zurück.
- Änderst du den Text im Eingabefeld, wird die Trefferansicht automatisch geschlossen.

Freitext ohne Adressbezug (z. B. nur ein Stichwort) wird nicht gesucht — dafür ist die Suche nicht gedacht.

## Feed per Suche hinzufügen

1. Öffne die Seite **Feeds**.
2. Gib in das Feld **Feed-URL oder Website-Adresse…** eine Website-Adresse oder URL ein.
3. Tippe auf **Suchen** oder drücke die Eingabetaste. Während der Suche erscheint ein Ladeindikator.
4. Tippe in der Trefferliste auf die Karte des gewünschten Feeds.
5. Bestätige den Dialog **Feed abonnieren?** mit **Ja** — oder brich mit **Nein** ab.
6. Der Feed erscheint in deiner Feed-Liste.

> **Hinweis:** Einen Anzeigetitel musst du bei der Suche nicht eingeben — fehlt er im Treffer, übernimmt die App den echten Feed-Titel automatisch beim ersten Abruf. Die im Formular gewählte **Kategorie** und der Schalter **Benachrichtigungen** gelten auch für den über die Suche abonnierten Feed.

## Feed direkt per URL hinzufügen

Kennst du die Feed-Adresse bereits oder findet die Suche nichts, funktioniert der bisherige Weg weiter: URL ins Eingabefeld eintragen, **Anzeigetitel** und optional eine **Kategorie** wählen, dann **Speichern** tippen.

## Keine Treffer

- Bei einer **vollständigen URL** ohne Treffer fragt dich die App: **„Kein Feed gefunden. Möchtest du die Adresse „…" direkt hinzufügen?"** Mit **Ja** bleibt die Adresse im Formular stehen, und das Feld **Anzeigetitel** wird — sofern leer — mit dem Website-Namen vorbefüllt. Du prüfst Titel und Kategorie und schließt mit **Speichern** ab.
- Bei einer **Website-Adresse oder Freitext** ohne Treffer erscheint nur die Anzeige **Keine Feeds gefunden.** — über **Zurück zu meinen Feeds** gelangst du zurück zur Liste.

## Suche nicht erreichbar

Ist die Suche vorübergehend nicht erreichbar, erscheint ein Hinweis unterhalb der Eingabekarte:

- Bei einer vollständigen URL: **„Die Feed-Suche ist nicht erreichbar. Du kannst die URL direkt hinzufügen."** — zusätzlich wird dir der Dialog zum direkten Hinzufügen angeboten.
- Sonst: **„Die Feed-Suche ist nicht erreichbar. Bitte versuche es später erneut oder gib eine vollständige Feed-URL ein."**

## Offline

Ohne Internetverbindung ist die Suche deaktiviert: Unterhalb der Eingabekarte erscheint der Hinweis **„Feed-Suche offline nicht verfügbar — URL direkt hinzufügen bleibt möglich."**, und die Schaltfläche **Suchen** reagiert nicht. Das direkte Hinzufügen per URL über **Speichern** bleibt weiterhin möglich. Details siehe [Offline lesen](offline.md).

## Weitere Hinweise

- Ist ein Feed bereits abonniert, erscheint beim Abonnieren eines Treffers der Hinweis **„Ein Feed mit dieser URL existiert bereits."** — es wird kein doppelter Eintrag angelegt.
- Die Suche benötigt eine Internetverbindung und findet Feeds nur, wenn die Website sie bekannt macht oder sie im Verzeichnis gelistet sind. Websites ohne hinterlegte Feed-Verweise liefern keine Treffer — nutze dann das direkte Hinzufügen per URL.
- Es gibt keine Stichwort- oder Themensuche — Treffer entstehen nur bei einer Website-Adresse oder URL.
