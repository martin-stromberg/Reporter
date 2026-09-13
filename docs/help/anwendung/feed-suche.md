<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

← [Zurück zur Übersicht](index.md)

# Feeds suchen und hinzufügen

Auf der Seite **Feeds** findest du neue Feeds, indem du eine Website-Adresse oder Feed-URL eingibst — du musst die genaue Feed-Adresse nicht kennen.

## Zweck

Statt die Feed-Adresse einer Website mühsam herauszusuchen, gibst du einfach die Adresse der Website ein. Die App sucht dann die zugehörigen Feeds und zeigt sie dir zur Auswahl an. Wer die Feed-Adresse bereits kennt, kann sie direkt eintragen — ganz ohne Suche.

## Funktionsweise

Die Seite **Feeds** zeigt standardmäßig nur deine Feed-Liste — jede Karte trägt den Feed-Titel, die Adresse und ein Status-Badge zum Gesundheitszustand (**In Ordnung**, **Warnung**, **Fehler**). Links auf der Karte erscheint das Symbol des Feeds: das Favicon der Website, sofern die App es ermitteln konnte, sonst ein Kreis mit dem Anfangsbuchstaben des Feeds. Über die Schaltfläche **+ Feed per URL hinzufügen** oberhalb der Liste öffnest du ein Formular, das sich von unten über die Seite schiebt (Titel **Feed per URL hinzufügen**):

- Das Eingabefeld **Feed-URL oder Website-Adresse…** dient als kombinierte Such- und URL-Eingabe.
- **Suchen** durchsucht ein öffentliches Feed-Verzeichnis sowie die Website selbst nach verfügbaren Feeds.
- **URL direkt hinzufügen** legt die eingegebene Adresse sofort als Feed an — ohne Suche und auch ohne Internetverbindung.
- Über **Abbrechen**, einen Tipp auf den abgedunkelten Hintergrund oder die Zurück-Geste/Zurück-Taste deines Geräts schließt sich das Formular wieder.

Gibst du eine Website-Adresse (z. B. `tagesschau.de`) oder eine vollständige Adresse mit `https://…` ein und tippst auf **Suchen**, erscheinen die gefundenen Feeds als Liste von Karten. Jede Karte zeigt den Titel, ggf. eine kurze Beschreibung, den Namen und die Adresse der Website sowie die Feed-Adresse. Unterhalb der Liste steht der Hinweis **„Suche powered by feedsearch.dev"** für den verwendeten Suchdienst. Über **Zurück zu meinen Feeds** kehrst du jederzeit aus der Trefferansicht zu deiner Feed-Liste zurück. Änderst du den Text im Eingabefeld, wird die Trefferansicht automatisch geschlossen.

Freitext ohne Adressbezug (z. B. nur ein Stichwort) wird nicht gesucht — dafür ist die Suche nicht gedacht.

## Feed per Suche hinzufügen

1. Öffne die Seite **Feeds** und tippe auf **+ Feed per URL hinzufügen**.
2. Gib in das Feld **Feed-URL oder Website-Adresse…** eine Website-Adresse oder URL ein.
3. Tippe auf **Suchen** oder drücke die Eingabetaste. Während der Suche erscheint ein Ladeindikator.
4. Tippe in der Trefferliste auf die Karte des gewünschten Feeds.
5. Bestätige den Dialog **Feed abonnieren?** mit **Ja** — oder brich mit **Nein** ab.
6. Der Feed erscheint in deiner Feed-Liste.

> **Hinweis:** Anzeigetitel, Kategorie und Benachrichtigungen musst du beim Hinzufügen nicht angeben. Neue Feeds starten ohne Kategorie und mit eingeschalteten Benachrichtigungen; beides kannst du später über das Kontextmenü des Feeds ändern (siehe [Bestehende Feeds verwalten](#bestehende-feeds-verwalten)).

## Feed direkt per URL hinzufügen

Kennst du die Feed-Adresse bereits oder willst du sie ohne Suche übernehmen, trägst du sie in das Eingabefeld ein und tippst auf **URL direkt hinzufügen**. Der Feed wird sofort in der Liste angelegt — ein Suchlauf entfällt. Diese Schaltfläche funktioniert auch offline.

## Feed-Symbol (Favicon)

Beim Anlegen eines Feeds — per Suche oder direkt per URL — versucht die App bei bestehender Internetverbindung, das Favicon der zugehörigen Website zu finden: Sie liest dazu die Symbol-Verweise der Website aus und prüft anschließend die übliche Symbol-Adresse der Website. Das gefundene Symbol wird gespeichert und auf den Feed-Karten sowie in den Artikellisten gezeigt. Ist kein Symbol auffindbar oder bestand beim Anlegen keine Verbindung, erscheint stattdessen ein Kreis mit dem Anfangsbuchstaben des Feeds — ein fehlendes Symbol holt die App beim nächsten erfolgreichen Abruf nach (siehe [Feeds synchronisieren](synchronisation.md)).

## Anzeigetitel und Platzhalter

Feeds, zu denen noch kein Titel bekannt ist — etwa beim direkten Hinzufügen oder bei einem Treffer ohne Titel — erhalten zunächst einen Platzhalter-Namen aus der Adresse: den Dateinamen der Feed-Adresse (z. B. `heise-atom.xml`), bei Adressen ohne Dateipfad den Website-Namen oder notfalls die Adresse selbst. Beim ersten erfolgreichen Abruf ersetzt die App den Platzhalter automatisch durch den echten Titel aus dem Feed. Selbst vergebene Titel (über **Umbenennen**) bleiben unverändert und werden nie überschrieben.

## Keine Treffer

- Bei einer **vollständigen URL** ohne Treffer fragt dich die App: **„Kein Feed gefunden. Möchtest du die Adresse „…" direkt hinzufügen?"** Mit **Ja** wird die Adresse sofort als Feed angelegt; mit **Nein** kehrst du zur Liste zurück.
- Bei einer **Website-Adresse oder Freitext** ohne Treffer erscheint nur die Anzeige **Keine Feeds gefunden.** — über **Zurück zu meinen Feeds** gelangst du zurück zur Liste.

## Suche nicht erreichbar

Ist die Suche vorübergehend nicht erreichbar, erscheint ein Hinweis im geöffneten Formular:

- Bei einer vollständigen URL: **„Die Feed-Suche ist nicht erreichbar. Du kannst die URL direkt hinzufügen."** — zusätzlich wird dir der Dialog zum direkten Hinzufügen angeboten.
- Sonst: **„Die Feed-Suche ist nicht erreichbar. Bitte versuche es später erneut oder gib eine vollständige Feed-URL ein."**

## Offline

Ohne Internetverbindung ist die Suche deaktiviert: Im geöffneten Formular erscheint der Hinweis **„Feed-Suche offline nicht verfügbar — URL direkt hinzufügen bleibt möglich."**, und die Schaltfläche **Suchen** reagiert nicht. Die Schaltfläche **URL direkt hinzufügen** bleibt weiterhin nutzbar. Details siehe [Offline lesen](offline.md).

## Bestehende Feeds verwalten

Ein Tipp auf eine Feed-Karte öffnet das Menü **Feed-Aktionen**:

- **Aktualisieren** — ruft nur diesen Feed neu ab.
- **Umbenennen** — öffnet den Dialog **Feed umbenennen**, in dem das Feld **Neuer Anzeigetitel** bereits den bisherigen Titel enthält. **OK** übernimmt den neuen Namen; ein leerer Titel wird mit dem Hinweis **„Bitte gib einen Anzeigetitel ein."** abgelehnt.
- **Kategorie ändern** — zeigt eine Auswahlliste aller Kategorien, an oberster Stelle **Keine Kategorie** zum Entfernen der Zuordnung. Kommt ein Kategoriename mehrfach vor, werden die Einträge nummeriert (z. B. „News", „News (2)"), damit jede Auswahl eindeutig ist.
- **Bearbeiten** — öffnet dasselbe Formular wie beim Hinzufügen im Bearbeitungsmodus (Titel **Feed bearbeiten**): Hier änderst du die Feed-Adresse und den Schalter **Benachrichtigungen** und schließt mit **Speichern** ab. Die Suchschaltflächen und der Offline-Hinweis sind in diesem Modus ausgeblendet. Auf Plattformen ohne Benachrichtigungsfunktion ist der Schalter deaktiviert und mit dem Hinweis **„Benachrichtigungen sind derzeit nur auf iOS verfügbar."** versehen.
- **Löschen** — entfernt den Feed nach Rückfrage (**Feed löschen?**) inklusive aller zugehörigen Artikel.

## Weitere Hinweise

- Ist ein Feed bereits abonniert, erscheint beim Abonnieren eines Treffers oder beim direkten Hinzufügen der Hinweis **„Ein Feed mit dieser URL existiert bereits."** — es wird kein doppelter Eintrag angelegt; beim direkten Hinzufügen bleibt das Formular zum Korrigieren geöffnet.
- Ungültige Adressen werden mit dem Hinweis **„Bitte gib eine gültige Feed-URL ein."** abgewiesen.
- Die Suche benötigt eine Internetverbindung und findet Feeds nur, wenn die Website sie bekannt macht oder sie im Verzeichnis gelistet sind. Websites ohne hinterlegte Feed-Verweise liefern keine Treffer — nutze dann **URL direkt hinzufügen**.
- Es gibt keine Stichwort- oder Themensuche — Treffer entstehen nur bei einer Website-Adresse oder URL.
