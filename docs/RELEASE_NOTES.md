<!-- Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details. -->

# Release Notes

## Important Notes Before Update

- There are no special notices.

## What's New

- Feed detail view: for feeds with "Warning" status the actions menu now offers "Show message" — a "Sync message" dialog names the reason (e.g. the feed returned far fewer articles than are stored, or has not published new items for more than 30 days) and shows the technical detail; previously "Show error details" was only available for feeds with an error.
- The former "Show error details" action is now called "Show message" (dialog "Sync message") and covers errors and warnings alike; error and warning details are stored in the same unified fields (automatic database migration on app start).
- Fixed: a manual refresh of a feed running in parallel with an automatic full sync could fail with a duplicate-key error or overwrite the feed's status and message with stale values — syncs of the same feed are now serialized.

## Wichtige Hinweise vor dem Update

- Es gibt keine besonderen Hinweise.

## Neuerungen

- Feed-Detailansicht: Bei Feeds mit Status „Warnung" bietet das Aktionsmenü jetzt „Meldung anzeigen" — ein Dialog „Synchronisierungsmeldung" nennt den Grund (z. B. liefert der Feed deutlich weniger Artikel als gespeichert sind oder hat seit über 30 Tagen keine neuen Beiträge veröffentlicht) und zeigt die technische Detailmeldung; bisher gab es „Fehlerdetails anzeigen" nur bei Fehlern.
- Die bisherige Aktion „Fehlerdetails anzeigen" heißt jetzt „Meldung anzeigen" (Dialog „Synchronisierungsmeldung") und gilt für Fehler und Warnungen gleichermaßen; Fehler- und Warnungsdetails werden in denselben vereinheitlichten Feldern gespeichert (automatische Datenbankmigration beim App-Start).
- Behoben: Ein manueller Abruf eines Feeds parallel zum automatischen Gesamt-Abgleich konnte mit einem Doppelschlüssel-Fehler fehlschlagen oder Status und Meldung des Feeds mit veralteten Werten überschreiben — Synchronisationen desselben Feeds werden jetzt serialisiert.
