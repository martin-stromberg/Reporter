<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Release Notes

## Important Notes Before Update

- There are no special notices.

## What's New

- Article tags: when a feed delivers tags or categories for an article, they are stored with it — they appear as a subtle text line at the end of the article text in the detail view, and the article search on the feed detail page matches them in addition to the title. Articles saved before this update pick up their tags automatically on the next refresh while the feed still delivers them (automatic database migration on app start).
- The start page ("Unread") and the "Later" page have been redesigned in an editorial style with new display typography: the start page now has a status header and animated category tabs and offers two switchable layouts — a list with swipe actions (Save / Done) or a card stack to swipe through article by article (next / previous, swipe up to mark as read). The layout can be toggled via a header button or in Settings ("Start page layout").
- The "Later" page follows the same design: category filter with counters, saved articles removed by swiping, read articles remain visible but dimmed — plus relative timestamps and trimmed feed titles.
- Feed detail view: for feeds with "Warning" status the actions menu now offers "Show message" — a "Sync message" dialog names the reason (e.g. the feed contains no articles, or has not published new items for more than 30 days) and shows the technical detail; previously "Show error details" was only available for feeds with an error.
- The former "Show error details" action is now called "Show message" (dialog "Sync message") and covers errors and warnings alike; error and warning details are stored in the same unified fields (automatic database migration on app start).
- Fixed: a manual refresh of a feed running in parallel with an automatic full sync could fail with a duplicate-key error or overwrite the feed's status and message with stale values — syncs of the same feed are now serialized.

## Wichtige Hinweise vor dem Update

- Es gibt keine besonderen Hinweise.

## Neuerungen

- Artikel-Schlagworte: Liefert ein Feed Schlagworte oder Kategorien zu einem Artikel, werden sie am Artikel gespeichert — in der Detailansicht erscheinen sie als abgeschwächte Zeile am Ende des Artikeltexts, und die Artikelsuche der Feed-Detailansicht findet sie zusätzlich zum Titel. Bereits gespeicherte Artikel erhalten ihre Schlagworte beim nächsten Abruf automatisch nachgezogen, solange der Feed sie noch ausliefert (automatische Datenbankmigration beim App-Start).
- Die Startseite („Ungelesen") und die Seite „Später" wurden im Editorial-Stil mit neuer Display-Typografie neu gestaltet: Die Startseite erhält eine Status-Kopfzeile und animierte Kategorie-Tabs und bietet zwei umschaltbare Ansichten — eine Liste mit Wischaktionen (Merken / Gelesen) oder einen Kartenstapel zum Durchwischen Artikel für Artikel (nächster / vorheriger Artikel, nach oben wischen zum Gelesen-Markieren). Die Ansicht lässt sich per Kopf-Button oder in den Einstellungen („Ansicht der Startseite") umschalten.
- Die Seite „Später" folgt demselben Design: Kategoriefilter mit Zählern, gemerkte Artikel per Wischen entfernen, gelesene Artikel bleiben gedämpft sichtbar — dazu relative Zeitangaben und gekürzte Feed-Titel.
- Feed-Detailansicht: Bei Feeds mit Status „Warnung" bietet das Aktionsmenü jetzt „Meldung anzeigen" — ein Dialog „Synchronisierungsmeldung" nennt den Grund (z. B. enthält der Feed keine Artikel oder hat seit über 30 Tagen keine neuen Beiträge veröffentlicht) und zeigt die technische Detailmeldung; bisher gab es „Fehlerdetails anzeigen" nur bei Fehlern.
- Die bisherige Aktion „Fehlerdetails anzeigen" heißt jetzt „Meldung anzeigen" (Dialog „Synchronisierungsmeldung") und gilt für Fehler und Warnungen gleichermaßen; Fehler- und Warnungsdetails werden in denselben vereinheitlichten Feldern gespeichert (automatische Datenbankmigration beim App-Start).
- Behoben: Ein manueller Abruf eines Feeds parallel zum automatischen Gesamt-Abgleich konnte mit einem Doppelschlüssel-Fehler fehlschlagen oder Status und Meldung des Feeds mit veralteten Werten überschreiben — Synchronisationen desselben Feeds werden jetzt serialisiert.
